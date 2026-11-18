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

Every judged path is checked in its fully resolved form before any file-system operation touches it,
including listing a folder and telling whether an entry exists. The unit keeps this invariant:

- The text rules apply to the text after `${VAR}` expansion, and the full-path, network-link, roots and
  real-path rules to the full, normalized path. Messages show the path as written.
- Before a path is judged, nothing touches it. Judging it touches nothing except reading the targets of links
  on the path, which never follows a link.
- After it is judged, the file system is touched only through a handle (Windows, Linux) or `realpath(3)`
  (macOS), whose result (missing, folder or file, real path) is judged before anything is read, written,
  listed, created or reported as existing through it. There is no by-name existence or attribute probe
  (`File.Exists`, `Directory.Exists`, `File.GetAttributes`) in the unit.
- A glob's literal folder, and every folder link a wildcard would descend into, is judged that way before it is
  listed.

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

**JudgedPath** (`readonly record struct`, declared in `FileAccessPolicy.cs`): the three forms of a judged
path, so each rule is applied to the form it is defined on.

- `Shown` (`string`): the text in a denial. It is the path as written (with `${VAR}`); for a file a glob
  matched, its full path.
- `Expanded` (`string`): the text the written-text rules apply to. It is the path after `${VAR}` expansion;
  for a file a glob matched, the expanded pattern's literal part followed by the matched relative path.
- `FullPath` (`string`): the full, normalized path. The full-path, network-link, roots and real-path rules
  apply to it, and it is what is opened.

The text rules use `Expanded`, not the full path, because of the CanvasNet network decision: a path written as
a UNC path is denied while a relative path resolved against a folder on a share is accessed. For a glob match
the literal part was already judged, so it cannot newly trip a rule; a colon or reserved name in a matched
component is caught on every platform.

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
  Where the links can be read, `CheckNetworkLink` denies it before anything opens, creates, lists or probes
  the path: it walks the path with `RealPath.FindNetworkLinkTarget` and the link reader, and skips the walk when
  the full path is itself a network location. Otherwise `LeadsToNetwork` denies it by the real path of a
  handle: of the opened file, of a listed folder, of the output folder or of an existing output. The share is
  never contacted through a link present when the path is judged; a link swapped in later is reached only by an
  open whose handle is then denied.

**IsDeviceNamespaceFolder(string fullPath)**: A pure text check used by `Context` for each `--allow-root`
folder before the folder is probed. On Windows it is true for a full path in a device namespace (`\\.\`,
`\??\` or `\\?\`) other than an ordinary extended-length drive or volume path, the rule CanvasNet applies to
its allowed roots. It is false on Linux and macOS.

**DescribeNeverAccessedFolder(expandedText, fullPath, out exists)**: The check `ConfigFile` applies to the
`root:` key, which has no policy instance and never grants access, before the key's folder is probed. It applies
`DescribeNeverAccessed` to the expanded text and `DescribeNeverAccessedFullPath` to the full path, and returns
a reason without touching the file system. On Windows, unless the full path is itself a network location, it
walks the path for a link to a network location. Then it opens the folder once (`RealPath.InspectFolder`; on
macOS with `O_DIRECTORY`), also when the full path is itself a network location (the open touches the share
just as an existence check would, which a relative root on a share needs). Unless the full path is a network
location, a real path that leads to a network location is denied. Otherwise `exists` is set from the facts of
that open alone: the state is `Exists` and the entry is not known to be other than a folder (`IsDirectory` is
not false; it is null only on Linux architectures where `fstat` cannot be read). A link or junction whose
target is missing, or a file, is not an existing folder, and `exists` is false whenever a reason is returned.
An internal overload takes a link reader and a folder inspector for tests.

**CheckPath(JudgedPath path)** (internal): Stage 1 of every judgment, link-safe. It applies
`DescribeNeverAccessed` to `Expanded` and `DescribeNeverAccessedFullPath` to `FullPath`, walks `FullPath` for a
link to a network location (`CheckNetworkLink`), and under roots requires `FullPath` as written to be inside an
allowed folder (in its lexical or canonical form, also comparing the long spelling of a path written with short
8.3 Windows names). The denial names `Shown`.

**ResolvePattern(writtenPattern, expandedPattern, rootedPattern)**: Judges an input pattern of a
configuration-file job and resolves it to `JudgedPath` values, judging every folder before it is listed.

1. Apply `DescribeNeverAccessed` to the expanded pattern and `DescribeNeverAccessedFullPath` to the rooted
   pattern, in every mode.
2. A literal (wildcard-free) pattern names one file. Apply `CheckPath` and return it without touching the file
   system; the read decides whether it exists.
3. A glob is split by `GlobMatcher.SplitGlob`: the rooted pattern gives the folder to list
   (`Path.GetFullPath` of its literal part) and the remainder; the expanded pattern gives the literal text
   used for `Expanded`.
4. The literal folder is judged before it is listed (`CheckListedFolder`): `CheckPath` (so under roots it must
   be written inside an allowed folder), then `RealPath.InspectPath` (a handle without access rights). A
   missing entry, or one that is not a folder, gives no matches. Otherwise the folder check of an output's
   folder applies, without the write-only `/dev` rule: on Windows a local folder whose real path is on a
   network location is denied in every mode; a restrictive mode needs the real path; under roots it must be
   inside an allowed folder's real location; under no-symlinks no link may lie below the folder's trusted
   anchor.
5. `GlobMatcher.MatchUnder` matches the remainder below the folder. Before it lists any folder that is a
   symbolic link or junction, it calls back, and the link folder is judged the same way (with `Expanded` set to
   the literal text followed by the link's relative path). A denial throws and fails the pattern, so a link is
   never silently skipped; a link whose target is missing lists nothing.
6. Each match becomes `JudgedPath(fullPath, literalText + "/" + relativePath, fullPath)` and is judged again,
   in full, when it is read.

Under roots, a glob whose literal folder is outside the allowed folders (for example `../**/*.yaml` from the
configuration folder) is therefore denied as a whole, because listing that folder would reveal names outside
the folders.

**ReadAllText(JudgedPath path)**: The judged read.

1. `CheckPath`. The denial for a local path that leads to a network location through a link names the network
   path the link leads to as its real path; the open therefore does not contact the network through a link
   present when the path is judged.
2. Under no-symlinks, capture the anchor and its real location before the open. The anchor is the deepest
   folder the path shares with `TrustedBase`, or the root of the path.
3. Open with `RealPath.OpenForRead`, which does not block on Linux and macOS. A folder opens too (on Windows
   with backup semantics), so it is told by its handle, never by a by-name probe.
4. Inspect the handle. Deny it when it is not a regular file (a folder, or a device node on Linux and macOS),
   or when `LeadsToNetwork(fullPath, realPath)`. In a restrictive mode, deny it when the real path is unknown,
   when (roots) the real path is not inside a folder's canonical location, or when (no-symlinks) the real path
   is not the anchor's real location followed by the written path below the anchor.
5. Read from the same handle as UTF-8 with byte-order-mark detection, like `File.ReadAllText`.

**WriteAllText(JudgedPath path, content)**: The judged write. Nothing is created or written until the
pre-checks pass.

1. `CheckPath`. On Linux and macOS, deny a full output path equal to `/dev` or under `/dev/`, because opening a
   device node for writing can act on the device before its handle can be judged. Then capture the anchor as
   in step 2 of the read.
2. Find the deepest existing parent folder by handles: `RealPath.InspectPath` on each level from the parent
   up. A missing level that is a link (a dangling link) counts as existing with an unknown real path, as does a
   level whose facts cannot be told. An entry that exists but is not a folder counts as missing (creating it
   then fails).
3. Judge that folder by its real location (`CheckFolder`). On Linux and macOS, deny it when its real location is
   `/dev` or under `/dev/`, in every mode, so a link cannot lead an output into `/dev`. Deny a local folder whose
   real location is a network location (`LeadsToNetwork`). In a restrictive mode, deny it when its real location
   is unknown, when (roots) it is outside the allowed folders, or when (no-symlinks) it passes through a link
   below the anchor.
4. When no level is missing, judge an existing output by `RealPath.InspectPath` (which follows any link): a
   missing entry is created, except that a restrictive mode denies a dangling link; a folder is denied; on Linux
   and macOS a real path under `/dev` (a link to a device) is denied in every mode; the real path is judged by
   `CheckRealPath`.
5. Create the missing folders one level at a time, in every mode. When the parent's real location is known,
   each new folder's real location (from `InspectPath`) must be its parent's real location followed by its
   name. This catches a folder swapped for a link while it is created. When it is unknown (mode any on a
   platform that cannot tell), the folders are created without verification.
6. Open an existing output with `RealPath.TryOpenExistingForWrite`, which never truncates (on Linux and macOS
   a folder gives null and is denied as not a regular file). Create a missing one with
   `RealPath.CreateNewForWrite`, whose exclusive create never follows a link at the output name.
7. Judge the handle as in step 4 of the read.
8. Only then truncate the file and write the content as UTF-8 without a byte-order mark, like
   `File.WriteAllText`.

**Differences from CanvasNet.** These are deliberate:

- **`file:////server` reason.** CanvasNet gives the `file:` host reason. DictionaryMark gives the URL reason,
  because no host follows `file://`. Both deny it.
- **The `root:` key.** CanvasNet has no base-folder key in its documents. DictionaryMark applies
  `DescribeNeverAccessedFolder` to the `root:` value (in `ConfigFile`), because it is written configuration
  content like any other path. The text rules apply to the expanded text only, so `root: .` on a share still
  works.
- **`${VAR}` in `--allow-root`.** CanvasNet leaves expansion to the shell. DictionaryMark expands it (in
  `Context`), because every folder option is resolved by `PathResolver`, as `--root` is.
- **Extended-length `--allow-root`.** CanvasNet accepts an ordinary extended-length allowed root (`\\?\C:\`,
  `\\?\Volume{GUID}\`). DictionaryMark rejects it (in `Context`), because `PathResolver.ResolveExistingDirectory`
  refuses `?` as a wildcard in every folder option, `--root` included. The device-namespace rule itself is
  CanvasNet's.
- **Pre-open walk for a link to a network location.** CanvasNet opens first and judges the real path of the
  handle. DictionaryMark also walks the path before anything touches it, because matching a glob and creating
  an output folder touch the disk before the file's own handle exists. The walk is skipped for a path that is
  itself a network location, so its outcomes are the same as CanvasNet's.
- **Glob listing.** CanvasNet has no globs. DictionaryMark judges a glob's literal folder, and every folder link
  a wildcard would descend into, by the rules of a folder before it is listed, and fails the pattern when one is
  denied. A file a glob matched is judged by the written-text rules on the pattern's literal part followed by
  the matched path, which is CanvasNet's rule applied to every name, so on Linux and macOS a matched file whose
  name contains a colon is denied.
- **Write rules.** CanvasNet never writes. DictionaryMark does the following:
  - On Linux and macOS, it denies, before the open, an output under `/dev`, an existing output whose real path
    is under `/dev`, or one whose deepest existing folder has a real path under `/dev`.
  - It creates missing folders one level at a time and verifies each one.
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
- Output folder, or a folder about to be listed: `its folder '{folder}' leads out of the allowed folders
  through a symbolic link or junction (--file-access {mode})`, or `its folder '{folder}' passes through a
  symbolic link or junction (--file-access {mode})`.
- Folder real path unknown: `the real path of the folder '{folder}' cannot be determined, so --file-access
  {mode} cannot be applied`.
- Created folder moved: `the folder '{folder}' changed while it was created, so --file-access {mode} cannot be
  applied`.
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
- Open, create, read, write and listing failures propagate as `IOException` (including
  `FileNotFoundException` and `DirectoryNotFoundException` for a missing input) or
  `UnauthorizedAccessException`. A malformed pattern propagates the `ArgumentException` of `Path.GetFullPath`
  or of the globbing library. The caller reports these as before.
- No exception is caught as `Exception`. Unknown platform facts are fail-closed values (null), not
  exceptions.

Limits that are documented rather than handled:

- A hard link cannot be detected: it is a second name of the same file, so its real path is the path as
  written.
- A folder swapped by another process between its check and the exclusive create can leave an empty file at
  the swapped location, but no content is written there.
- Under roots a configuration file can still overwrite any file inside the allowed folders.
- A by-name open can still pass through a link swapped in between the walk and that open; the handle it
  yields is then judged and denied, so nothing is read, written, listed or created through it. Two operations
  have no handle of their own: listing a folder (bracketed by the folder's handle check just before and each
  match's handle check when it is read) and creating a folder (bracketed by the parent's handle check before
  and the new folder's handle check after). Closing this window fully would need enumeration and creation from
  a handle, which .NET does not offer.
- On Linux and macOS the .NET listing reads the target of a symbolic link entry to tell whether it is a folder
  (metadata only); the link is judged before anything is listed through it.

#### Dependencies

- **RealPath** — opens files, inspects handles and paths (`InspectPath`, `InspectFolder`), captures real
  locations, compares and classifies paths.
- **GlobMatcher** — splits a glob (`SplitGlob`) and matches it below a judged folder (`MatchUnder`), calling
  back before a link folder is listed.
- .NET base class library (`FileStream`, `RandomAccess`, `Directory`, `File`).

#### Callers

- **Context** — `ParseMode` (through its argument parser), `IsDeviceNamespaceFolder` and `Create`, and
  holds the policy as `Context.FileAccess`.
- **ConfigFile** — `DescribeNeverAccessedFolder` for the `root` key, which also tells whether its folder exists.
- **DictionaryGenerator** — `ResolvePattern`, `ReadAllText` and `WriteAllText` for configuration-file jobs, and
  catches `FileAccessDeniedException`.
