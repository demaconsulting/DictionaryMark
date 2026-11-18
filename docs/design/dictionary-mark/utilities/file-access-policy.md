### FileAccessPolicy

![Utilities Subsystem Structure](UtilitiesView.svg)

#### Purpose

`FileAccessPolicy` decides which files named in a configuration file the tool may read and write, and
performs those reads and writes on the handle it has verified. It is the single place where the
`--file-access` mode and the never-accessed list are applied, so every configuration-file path is judged by
the same rules. The rules follow the DEMA Consulting CanvasNet tool. The unit consists of the policy class,
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

**DescribeNeverAccessed(string path)**: A pure text check that returns a reason or null. Backslashes count as
slashes. The rules are checked in this order:

1. A device path: `\\?\`, `\\.\` or `\??\`.
2. A network path: `\\server` or `//server`.
3. A URL: a scheme of two or more characters followed by `://`. A `file:` URL with a host is a network
   location.
4. Any other colon, except the drive of an absolute Windows path. This covers an alternate data stream, a
   drive-relative path, another URI such as `urn:x`, and a drive on a system that has none.
5. A device name: a Windows reserved name in any component (Windows), or a path under `/dev` (elsewhere).

**CheckPattern(writtenPattern, expandedPattern, rootedPattern)**: Runs before an input pattern of a
configuration-file job is matched, because matching touches the disk. It applies the never-accessed list to
the expanded text and to the rooted path (including a mapped network drive). On Windows it then walks the
literal part of the pattern (the folders before the first component with `*` or `?`, or the whole of a
literal pattern) with `RealPath.FindNetworkLinkTarget`, and denies a pattern whose literal part passes through
a link to a network location, before matching. Under roots, a literal (wildcard-free) pattern must also be
inside an allowed folder as written. Each file a glob matches is judged again, individually, when it is read.

**ReadAllText(writtenPath, fullPath)**: The judged read.

1. Apply the never-accessed list to the written and full paths. On Windows, also deny a path that passes
   through a link to a network location: at any component, following a chain of links by their targets
   without opening through them (`RealPath.FindNetworkLinkTarget` with the link reader). The open therefore
   does not contact the network. The denial names the network path the link leads to as its real path.
2. Under roots, deny a full path that is not inside an allowed folder (in its lexical or canonical form, also
   comparing the long spelling of a path written with short 8.3 Windows names).
3. Under no-symlinks, capture the anchor and its real location before the open. The anchor is the deepest
   folder the path shares with `TrustedBase`, or the root of the path.
4. Deny a folder. Open the file with `RealPath.OpenForRead`, which does not block on Linux and macOS.
5. Inspect the handle. Deny it when it is not a regular file, or when its real path is a network or device
   location. In a restrictive mode, deny it when the real path is unknown, when (roots) the real path is not
   inside a folder's canonical location, or when (no-symlinks) the real path is not the anchor's real location
   followed by the written path below the anchor.
6. Read from the same handle as UTF-8 with byte-order-mark detection, like `File.ReadAllText`.

**WriteAllText(writtenPath, fullPath, content)**: The judged write. Nothing is created or written until the
pre-checks pass.

1. Apply steps 1 to 3 of the read to the output.
2. Find the deepest existing parent folder (an entry that is a link, even a dangling one, counts as existing)
   and the missing folders below it. Get the folder's real location. Deny it when it is a network or device
   location. In a restrictive mode, deny it when its real location is unknown, when (roots) it is outside the
   allowed folders, or when (no-symlinks) it passes through a link below the anchor.
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
- Network path: `it is a UNC or network path, which is never accessed`.
- Device: `it is a device path, which is never accessed`.
- Colon: `it names an alternate data stream, a drive-relative path or a URI, which is never accessed`.
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

- **Context** — `ParseMode` (through its argument parser) and `Create`, and holds the policy as
  `Context.FileAccess`.
- **ConfigFile** — `DescribeNeverAccessed` for the `root` key.
- **DictionaryGenerator** — `CheckPattern`, `ReadAllText` and `WriteAllText` for configuration-file jobs, and
  catches `FileAccessDeniedException`.
