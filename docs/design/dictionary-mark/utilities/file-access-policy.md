### FileAccessPolicy

![Utilities Subsystem Structure](UtilitiesView.svg)

#### Purpose

`FileAccessPolicy` decides which files named in a configuration file the tool may read and write, and
performs those reads and writes on the handle it has verified. It is the single place where the
`--file-access` mode and the never-accessed list are applied, so every configuration-file path is judged by
the same rules. The rules follow the DEMA Consulting CanvasNet tool, except for the deliberate differences listed
under Key Methods. The unit consists of the policy class,
the `FileAccessMode` flags enumeration and the `FileAccessDeniedException` that reports a denial.

Paths given on the command line are never passed to this unit: the user who runs the tool chose them.

#### Data Model

**FileAccessMode** (`[Flags]` enumeration):

- `None` (0): the mode written `any`, the default. Only the never-accessed list applies.
- `Roots` (1): both the written and the real path must be inside an allowed folder.
- `NoSymlinks` (2): no symbolic link or junction below the trusted anchor.
- `Roots | NoSymlinks`: the mode written `roots,no-symlinks`.

**FileAccessPolicy** (immutable, thread-safe):

- `Mode` (`FileAccessMode`) and `ModeName` (`string`): the mode and its spelling on the command line.
- `AllowedFolders` (`IReadOnlyList<string>`): the full paths of the allowed folders, as resolved.
- `TrustedBase` (`string?`): the configuration file's folder; it and every folder above it are trusted by
  no-symlinks.
- A private `AllowedFolder` record per folder holds the `Lexical` full path and the `Canonical` real
  location captured once when the policy is created. A folder whose real location cannot be determined has
  no canonical form and allows no file (fail closed).
- A private handle inspector, `Func<SafeFileHandle, HandleFacts>`, defaults to `RealPath.Inspect`. Tests
  inject a double to simulate a platform that cannot tell a fact.
- A private link reader, `Func<string, string?>`, defaults to `RealPath.ReadLinkTarget`. It reads the
  immediate target of a link without following it. Tests inject a double to simulate a link to a network
  location without a link privilege or a share.
- `Any` (static): the default policy, mode any with no folders.

**FileAccessDeniedException** (sealed, derives from `Exception`, not from `IOException`, so input and output
handlers never mistake a denial for a read or write failure):

- `ErrorCode` (`const string`): `file-access-denied`.
- `WrittenPath` (`string`), `RealPath` (`string?`) and `Reason` (`string`).
- Message: `file-access-denied: '{written}' (real path '{real}'): {reason}`. The real-path part is left out
  when the real path is not known.

#### Key Methods

**ParseMode(string text)**: Splits the value at commas, trims each item and lowers its case. `any` is valid
only on its own. Otherwise the items must be distinct and drawn from `roots` and `no-symlinks`, in either
order. Anything else, including an empty item, throws `ArgumentException` with the message
`'{text}' is not a value of --file-access: use any (the default), roots, no-symlinks, or roots,no-symlinks`.

**Create(mode, allowedFolders, trustedBase)**: Normalizes each folder with `Path.GetFullPath`, removes
duplicates and, in a restrictive mode, captures each folder's real location with
`RealPath.TryGetCanonicalDirectory`. The real locations are never looked up again, so a folder renamed and
replaced by a link later cannot widen the policy.

**DescribeNeverAccessed(string path)**: A pure text check of a path as written (after `${VAR}` expansion)
that returns a reason or null. Backslashes count as slashes. The rules are checked in this order:

1. A device path: `\\?\` or `\\.\` on every platform, and `\??\` on Windows.
2. A path written as a network path: `\\server` or `//server`.
3. A URL: a scheme of two or more characters followed by `://`. A `file:` URL with a host is a network
   location.
4. Any other colon, on every platform, except the drive of an absolute path on Windows, as in CanvasNet. This
   covers an alternate data stream, a drive-relative path, another URI such as `urn:x` or `file:/x`, and on
   Linux and macOS a drive path such as `C:/x` and a file name such as `terms:v1.yaml`. A URL is recognized by
   `://` in rule 3, so `docs:stream` is never mistaken for a scheme.
5. On Windows only, a reserved device name in any component.

**DescribeNeverAccessedFullPath(string fullPath)**: A pure text check of the full candidate path (the written
path combined with its base folder). On Windows only, it returns the device reason for a path in a device
namespace or with a reserved device name in any component, unless the path is an ordinary extended-length
drive or volume path (`RealPath.IsOrdinaryExtendedPath`), under which device names are not mapped. The
written text cannot itself be extended-length, so such a full path only comes from a trusted base folder. The
URL, network and colon rules apply only to the text as written, as in CanvasNet, so a path on a share or on a
mapped network drive is not denied here. It returns null on Linux and macOS.

**LeadsToNetwork(string fullPath, string realPath)**: True when `realPath` is a network location and
`fullPath` is not (`RealPath.IsNetworkLocation` for both). It is always false on Linux and macOS.

**Network locations.** The rule is the one CanvasNet uses:

- A path **written** as a UNC path (`\\server\share`, `//server/share`) is never accessed.
- A path that is itself on a network location is accessed. This covers a path on a mapped network drive,
  written or resolved, and a relative path resolved against a configuration file, `--root` or `root:` on a
  share. A mapped network drive counts as a network location, so a path on `Z:\` whose real path names the
  share is not denied.
- On Windows, a local path that leads to a network location through a symbolic link or junction is denied.
  Where the links can be read, `CheckNetworkLink` denies it before anything opens, creates or matches the
  path: it walks the path with `RealPath.FindNetworkLinkTarget` and the link reader, and skips the walk when
  the full path is itself a network location. Otherwise `LeadsToNetwork` denies it after the open, by the
  real path of the handle or of the output folder.

**IsDeviceNamespaceFolder(string fullPath)**: A pure text check used by `Context` for each `--allow-root`
folder before the folder is probed. On Windows it is true for a full path in a device namespace (`\\.\`,
`\??\` or `\\?\`) other than an ordinary extended-length drive or volume path, the rule CanvasNet applies to
its allowed roots. It is false on Linux and macOS.

**CheckPattern(writtenPattern, expandedPattern, rootedPattern)**: Runs before an input pattern of a
configuration-file job is matched, because matching touches the disk. It applies `DescribeNeverAccessed` to
the expanded text and `DescribeNeverAccessedFullPath` to the rooted path. On Windows, unless the rooted
pattern is itself a network location, it then walks the literal part of the pattern (the folders before the
first component with `*` or `?`, or the whole of a literal pattern) and denies a pattern whose literal part
leads to a network location through a link, before matching. Under roots, a literal (wildcard-free) pattern
must also be inside an allowed folder as written. Each file a glob matches is judged again, individually, when
it is read.

**ReadAllText(writtenPath, fullPath)**: The judged read. `writtenPath` is null for a file a glob matched. Its
pattern was already judged by `CheckPattern`, so only the full-path rules apply and the denial names the full
path.

1. Apply `DescribeNeverAccessed` to the written path (when there is one) and `DescribeNeverAccessedFullPath`
   to the full path. On Windows, also deny a local path that leads to a network location through a link: at
   any component, following a chain of links by their targets without opening through them. The open
   therefore does not contact the network. The denial names the network path the link leads to as its real
   path.
2. Under roots, deny a full path that is not inside an allowed folder (in its lexical or canonical form, also
   comparing the long spelling of a path written with short 8.3 Windows names).
3. Under no-symlinks, capture the anchor and its real location before the open. The anchor is the deepest
   folder the path shares with `TrustedBase`, or the root of the path.
4. Deny a folder. Open the file with `RealPath.OpenForRead`, which does not block on Linux and macOS.
5. Inspect the handle. Deny it when it is not a regular file (this is how a device node on Linux and macOS is
   refused), or when `LeadsToNetwork(fullPath, realPath)`. In a restrictive mode, deny it when the real path
   is unknown, when (roots) the real path is not inside a folder's canonical location, or when (no-symlinks)
   the real path is not the anchor's real location followed by the written path below the anchor.
6. Read from the same handle as UTF-8 with byte-order-mark detection, like `File.ReadAllText`.

**WriteAllText(writtenPath, fullPath, content)**: The judged write. Nothing is created or written until the
pre-checks pass.

1. Apply steps 1 and 2 of the read to the output. On Linux and macOS, deny a full output path equal to `/dev`
   or under `/dev/`, because opening a device node for writing can act on the device before its handle can be
   judged. Then capture the anchor as in step 3 of the read.
2. Find the deepest existing parent folder (an entry that is a link, even a dangling one, counts as existing)
   and the missing folders below it. Get the folder's real location. On Linux and macOS, deny it when its real
   location is `/dev` or under `/dev/`, in every mode, so a link cannot lead an output into `/dev`. Deny a local
   folder whose real location is a network location (`LeadsToNetwork`). In a restrictive mode, deny it when its
   real location is unknown, when (roots) it is outside the allowed folders, or when (no-symlinks) it passes
   through a link below the anchor.
3. Deny an existing folder at the output path. In a restrictive mode, deny a dangling link at the output
   path, and judge an existing output by its real location (which follows any link) before opening it.
4. Create the missing folders. In mode any one call creates them all. In a restrictive mode they are created
   one level at a time, and each new folder's real location must be its parent's real location followed by
   its name. This catches a folder swapped for a link while it is created.
5. Open an existing output with `RealPath.TryOpenExistingForWrite`, which never truncates. Create a missing
   one with `RealPath.CreateNewForWrite`, whose exclusive create never follows a link at the output name.
6. Judge the handle as in step 5 of the read.
7. Only then truncate the file and write the content as UTF-8 without a byte-order mark, like
   `File.WriteAllText`.

**Differences from CanvasNet.** These are deliberate:

- **`file:////server` reason.** CanvasNet gives the `file:` host reason. DictionaryMark gives the URL reason,
  because no host follows `file://`. Both deny it.
- **The `root:` key.** CanvasNet has no base-folder key in its documents. DictionaryMark applies the
  written-text rules to the expanded `root:` value (in `ConfigFile`), because it is written configuration
  content like any other path. Only the text is checked, so `root: .` on a share still works.
- **`${VAR}` in `--allow-root`.** CanvasNet leaves expansion to the shell. DictionaryMark expands it (in
  `Context`), because every folder option is resolved by `PathResolver`, as `--root` is.
- **Extended-length `--allow-root`.** CanvasNet accepts an ordinary extended-length allowed root (`\\?\C:\`,
  `\\?\Volume{GUID}\`). DictionaryMark rejects it (in `Context`), because `PathResolver.ResolveExistingDirectory`
  refuses `?` as a wildcard in every folder option, `--root` included. The device-namespace rule itself is
  CanvasNet's.
- **Pre-open walk for a link to a network location.** CanvasNet opens first and judges the real path of the
  handle. DictionaryMark also walks the path before anything touches it, because matching a glob and creating
  an output folder touch the disk before any handle exists. The walk is skipped for a path that is itself a
  network location, so its outcomes are the same as CanvasNet's.
- **Matched glob files.** CanvasNet has no globs. A file a glob matched has no written text, so it gets only
  the full-path rules, as a CanvasNet candidate does. On Linux and macOS a matched file whose name contains a
  colon is therefore read.
- **Write rules.** CanvasNet never writes. DictionaryMark does the following:
  - On Linux and macOS, it denies, before the open, an output under `/dev` or one whose deepest existing folder
    has a real path under `/dev`.
  - It creates a missing output exclusively (`CreateNew`).
  - It opens an existing output without truncating it and verifies the handle first.
  - In a restrictive mode, it denies a dangling link at the output path.
- **Named pipe as an output.** CanvasNet never writes. On Linux and macOS the non-blocking open for writing of a
  named pipe with no reader fails with `ENXIO`. DictionaryMark treats that as not a regular file, so the write
  is denied without blocking.
- **Error format.** CanvasNet reports `file-access-denied` in its own diagnostic shape. DictionaryMark puts
  `file-access-denied: '{written}' (real path '{real}'): {reason}` in the per-job error
  (`Error: table job N ('{written}'): …`), because that is its existing error convention. The code name is the
  same.

Reasons (they name the mode when the mode caused the denial):

- Roots, path as written: `the path as written is outside the allowed folders (--file-access {mode})`.
- Roots, real path: `its real path is outside the allowed folders, so it leads out through a symbolic
  link or junction (--file-access {mode})`.
- No-symlinks: `its real path differs from the path as written, so the path passes through a symbolic
  link or junction (--file-access {mode})`.
- Real path unknown: `the real path of the opened file cannot be determined on this system, so
  --file-access {mode} cannot be applied`.
- Output folder: `its folder '{folder}' leads out of the allowed folders through a symbolic link or
  junction (--file-access {mode})`, or `its folder '{folder}' passes through a symbolic link or junction
  (--file-access {mode})`.
- URL: `it is a URL or URI, which is never accessed`.
- `file:` URL with a host: `it is a file: URI with a host, which names a network location and is never
  accessed`.
- Network path written as such: `it is a UNC or network path, which is never accessed`.
- Local path leading to a network location: `it leads to a network location through a symbolic link or junction,
  which is never accessed`.
- Device: `it is a device path, which is never accessed`.
- Colon: `it contains a colon other than the drive of an absolute path on Windows, which is never accessed`.
- Not a regular file: `it is not a regular file (a folder, device, pipe or other special file), which is
  never accessed`.

#### Error Handling

- A denial throws `FileAccessDeniedException`. The caller reports it as a per-job error.
- `ParseMode` throws `ArgumentException` (a usage error).
- Open, create, read and write failures propagate as `IOException` or `UnauthorizedAccessException`. A
  malformed literal pattern propagates the `ArgumentException` of `Path.GetFullPath`. The caller reports
  these as before.
- No exception is caught as `Exception`. Unknown platform facts are fail-closed values (null), not
  exceptions.

Limits that are documented rather than handled:

- A hard link cannot be detected: it is a second name of the same file, so its real path is the path as
  written.
- A folder swapped by another process between its check and the exclusive create can leave an empty file at
  the swapped location, but no content is written there.
- Under roots a configuration file can still overwrite any file inside the allowed folders.
- On Windows, a glob wildcard segment (`*`, `**`) is enumerated by the glob matcher. A folder link to a
  network share that the enumeration reaches below the literal part of the pattern can be listed while
  matching. Each matched file is still denied before it is read, and the literal part is walked first.

#### Dependencies

- **RealPath** — opens files, inspects handles, captures real locations, compares and classifies paths.
- .NET base class library (`FileStream`, `RandomAccess`, `Directory`, `File`).

#### Callers

- **Context** — `ParseMode` (through its argument parser), `IsDeviceNamespaceFolder` and `Create`, and
  holds the policy as `Context.FileAccess`.
- **ConfigFile** — `DescribeNeverAccessed` for the `root` key.
- **DictionaryGenerator** — `CheckPattern`, `ReadAllText` and `WriteAllText` for configuration-file jobs, and
  catches `FileAccessDeniedException`.
