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
- A glob's literal folder, and every folder a wildcard would descend into, is judged that way before it is
  listed.
- Every never-accessed rule that can be judged from a real path is applied again to the real path of every
  handle: each output and listed folder, an existing output, the `root:` folder, and every opened read and write
  handle. This holds in every mode, also `any`, so a folder swapped for a link between a check and the open is
  still refused before anything is read, truncated or written.

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
  location captured once when the policy is created. A folder whose real location cannot be determined, or
  that is not a folder, has no canonical form and allows no file (fail closed). `ContainsWrittenPath` tests a
  path as written against either form with `RealPath.IsWrittenUnder` (the platform's default letter-case rule,
  a pre-filter).
  `ContainsRealPath` tests a real path against `Canonical` with `RealPath.IsUnderRealPath` (letter case
  significant), the authoritative roots check.
- A private handle inspector, `Func<SafeFileHandle, HandleFacts>`, defaults to `RealPath.Inspect`. Tests
  inject a double to simulate a platform that cannot tell a fact.
- A private link reader, `Func<string, string?>`, defaults to `RealPath.ReadLinkTarget`. It reads the
  immediate target of a link without following it. Tests inject a double to simulate a link to a network
  location without a link privilege or a share.
- A private pre-open action, `Action<string>`, is a no-op by default. It is called with the full path after
  every check and immediately before a read or write handle is opened (after any missing output folder is
  created). Tests inject an action that swaps a folder for a link there, to show that the opened handle is
  judged.
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
`RealPath.TryGetCanonicalDirectory` (from a handle on every platform; on macOS `realpath(3)` only for a folder
that cannot be opened for reading). An entry that is not a folder, for example an allowed folder replaced by
a file after `Context` checked it, gets no real location and allows nothing. The real locations are never
looked up again, so a folder renamed and replaced by a link later cannot widen the policy. An internal overload
also takes the handle inspector, the link reader and the pre-open action, for tests.

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
path combined with its base folder). On Windows, it returns the device reason for a path in a device
namespace or with a reserved device name in any component, unless the path is an ordinary extended-length
drive or volume path (`RealPath.IsOrdinaryExtendedPath`), under which device names are not mapped. The
written text cannot itself be extended-length, so such a full path only comes from a trusted base folder. On
Linux and macOS, it returns the device reason for `/dev` and every path under it, for reads, writes, listed
folders and `root:` alike, so nothing there is ever opened (a DictionaryMark extension; see Differences from
CanvasNet). The URL, network and colon rules apply only to the text as written, as in CanvasNet, so a path on
a share or on a mapped network drive is not denied here.

**LeadsToNetwork(string fullPath, string realPath)**: True when `realPath` is a network location and
`fullPath` is not (`RealPath.IsNetworkLocation` for both). It is always false on Linux and macOS.

**DescribeNeverAccessedRealPath(string fullPath, string realPath)** (internal): The never-accessed rules that
can be judged from a real path, shared by every handle check (`CheckFolder`, `CheckRealPath` and
`DescribeNeverAccessedFolder`) and applied in every mode, also `any`. It returns, in order:

1. The device reason for a real path that is `/dev` or under it, on Linux and macOS.
2. The device reason for a real path in a Windows device namespace (`IsDeviceNamespaceFolder`). This is a
   guard: `RealPath` reports only drive and UNC real paths, so only an injected inspector reaches it.
3. The network-link reason when `LeadsToNetwork(fullPath, realPath)`.

Otherwise it returns null. A folder swapped for a link between a check of the path and its open leads the open
elsewhere, so the path's own checks are not enough: only the handle's real path tells where the open went.

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
just as an existence check would, which a relative root on a share needs). The real path of that open is
judged by `DescribeNeverAccessedRealPath`: a real path under `/dev` on Linux and macOS, one in a device
namespace on Windows, and, unless the full path is itself a network location, one that leads to a network
location are denied. Otherwise `exists` is set from the facts of
that open alone: the state is `Exists` and the entry is not known to be other than a folder (`IsDirectory` is
not false; it is null only on Linux architectures where `fstat` cannot be read). A link or junction whose
target is missing, or a file, is not an existing folder, and `exists` is false whenever a reason is returned.
An internal overload takes a link reader and a folder inspector for tests.

**CheckPath(JudgedPath path)** (internal): Stage 1 of every judgment, link-safe. It applies
`DescribeNeverAccessed` to `Expanded` and `DescribeNeverAccessedFullPath` to `FullPath`, walks `FullPath` for a
link to a network location (`CheckNetworkLink`), and under roots requires `FullPath` as written to be inside an
allowed folder (in its lexical or canonical form, also comparing the long spelling of a path written with short
8.3 Windows names). This written check uses the platform's default letter-case rule; it is a pre-filter, and
the real path is always judged exactly afterwards (see Letter case below). The denial names `Shown`.

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
   be written inside an allowed folder), then `RealPath.InspectFolder` (one open of the folder; on Windows and
   Linux a handle without access rights, on macOS an `O_DIRECTORY` open, so a file, a named pipe or a device,
   also through a link, is not a folder on any platform). A missing entry, or one that is not a folder, gives
   no matches. Otherwise the folder check of an output's folder (`CheckFolder`) applies: in every mode its real
   path is judged by `DescribeNeverAccessedRealPath` (on Linux and macOS not under `/dev`; on Windows not in a
   device namespace and not a local folder whose real path is on a network location); a restrictive mode needs
   the real path; under roots it must be inside an allowed folder's real location; under no-symlinks no link
   may lie below the folder's trusted anchor.
5. `GlobMatcher.MatchUnder` matches the remainder below the folder. Before it lists any folder below the literal
   folder, plain or a symbolic link or junction, it calls back, and the folder is judged exactly as in step 4
   (`CheckListedFolder`, with `Expanded` set to the literal text followed by the folder's relative path). A
   denial throws and fails the pattern, so a folder is never silently skipped; a folder that is missing or not
   a folder (including a link whose target is missing) lists nothing. So on Linux and macOS `/d*/x.yaml` is
   denied at `/dev` by its full path, and `sys/d*/x.yaml` with `sys` a link to `/` is denied by the real path
   of `sys/dev`. The cost is one open per folder a wildcard descends into (plus, on Windows, reading the link
   targets of its ancestors, and under no-symlinks one open of its anchor); a folder the pattern never
   descends into is never judged.
6. Each match becomes `JudgedPath(fullPath, literalText + "/" + relativePath, fullPath)` and is judged again,
   in full, when it is read.

Under roots, a glob whose literal folder is outside the allowed folders (for example `../**/*.yaml` from the
configuration folder) is therefore denied as a whole, because listing that folder would reveal names outside
the folders.

**ReadAllText(JudgedPath path)**: The judged read.

1. `CheckPath`. On Linux and macOS this denies a full path under `/dev` before anything is opened. The denial
   for a local path that leads to a network location through a link names the network path the link leads to
   as its real path; the open therefore does not contact the network through a link present when the path is
   judged.
2. Under no-symlinks, capture the anchor and its real location before the open. The anchor is the deepest
   folder the path shares with `TrustedBase`, comparing folder names with letter case significant
   (`RealPath.CommonAncestor`), or the root of the path.
3. Run the pre-open action (a no-op outside tests), then open with `RealPath.OpenForRead`, which does not
   block on Linux and macOS. A folder opens too (on Windows with backup semantics), so it is told by its
   handle, never by a by-name probe.
4. Inspect the handle. Deny it when it is not a regular file (a folder, or a device node on Linux and macOS),
   with the not-a-regular-file reason. Deny it, in every mode, when its type is unknown (a native library or
   entry point that is missing, or a native call that failed, leaves the facts unknown), with the type-unknown
   reason, so an interop failure is never reported as a special file. Then, in every mode including `any` and
   before the mode's rules, deny a known real path that `DescribeNeverAccessedRealPath` describes: on Linux
   and macOS a real path under `/dev` (so a folder swapped for a link to `/dev/shm` after step 1 is refused),
   on Windows one in a device namespace, and a local path whose real path is a network location. A real path
   the platform cannot report is not judged by these rules. In a restrictive mode, deny it when the real path
   is unknown,
   when (roots) the real path is not inside a folder's canonical location (letter case significant), or when
   (no-symlinks) the real path is not the anchor's real location (an exact prefix) followed by the written path
   below the anchor (compared under the platform's default letter-case rule).
5. Read from the same handle as UTF-8 with byte-order-mark detection, like `File.ReadAllText`.

**WriteAllText(JudgedPath path, content)**: The judged write. Nothing is created or written until the
pre-checks pass.

1. `CheckPath`, which on Linux and macOS denies a full output path equal to `/dev` or under `/dev/`
   (`DescribeNeverAccessedFullPath`), because opening a device node for writing can act on the device before
   its handle can be judged. Then capture the anchor as in step 2 of the read.
2. Find the deepest existing parent folder by handles: `RealPath.InspectFolder` on each level from the parent
   up (on macOS an `O_DIRECTORY` open, so a regular file, a named pipe or a device is never taken for an
   existing folder on any platform). A missing level that is a link (a dangling link, or on macOS also a link to
   a file, which the folder open cannot tell apart) counts as existing with an unknown real path, as does a
   level whose facts cannot be told, so a restrictive mode denies it. An entry that exists but is not a folder
   counts as missing (creating it then fails with an I/O error, and nothing is written).
3. Judge that folder by its real location (`CheckFolder`). In every mode, deny a real location that
   `DescribeNeverAccessedRealPath` describes: on Linux and macOS `/dev` or under `/dev/`, so a link cannot lead
   an output into `/dev`; on Windows one in a device namespace, or a local folder whose real location is a
   network location. In a restrictive mode, deny it when its real location
   is unknown, when (roots) it fails the written pre-filter or its real location is outside the allowed folders'
   real locations (letter case significant), or when (no-symlinks) it passes through a link below the anchor.
4. When no level is missing, judge an existing output by `RealPath.InspectPath` (which follows any link): a
   missing entry is created, except that a restrictive mode denies a dangling link; a folder is denied; the real
   path is judged by `CheckRealPath`, whose never-accessed rules deny in every mode, before the open, a link to a
   device under `/dev` on Linux and macOS.
5. Create the missing folders one level at a time, in every mode. When the parent's real location is known,
   each new folder is opened as a folder (`RealPath.InspectFolder`, a handle on every platform, the same source
   as the parent's real location). It must exist as a folder, and its real location must equal exactly (letter
   case significant) its parent's real location followed by its name as written. The folder was just created
   with that name, so the file system stores it so; a difference means another process created or replaced it,
   for example with a link, while it was created. When the parent's real location is unknown (mode any on a
   platform that cannot tell), the folders are created without verification.
6. Run the pre-open action (a no-op outside tests). Open an existing output with
   `RealPath.TryOpenExistingForWrite`, which never truncates (on Linux and macOS
   a folder gives null and is denied as not a regular file). Create a missing one with
   `RealPath.CreateNewForWrite`, whose exclusive create never follows a link at the output name.
7. Judge the handle as in step 4 of the read. The never-accessed rules on its real path apply in every mode,
   also `any`, so a folder swapped after steps 2 to 4 for a link to `/dev` or to a share is refused before
   anything is truncated or written. A new output denied here is left as an empty file (see the limits under
   Error Handling).
8. Only then truncate the file and write the content as UTF-8 without a byte-order mark, like
   `File.WriteAllText`.

**Letter case.** A folder or volume may distinguish letter case on every platform (a Windows folder with
per-folder case sensitivity, a case-sensitive APFS volume, every ordinary Linux file system). The rules are:

- *Roots, real path*: compared with letter case significant on every platform (`RealPath.IsUnderRealPath`). On
  a folder that distinguishes case, a sibling whose name differs from an allowed folder's only in letter case
  (`ALLOWED` beside `allowed`) is a different folder and is outside. On one that ignores case, real paths carry
  the stored letter case (see the RealPath design), so a path written in another letter case is still inside.
  An exact comparison can only refuse a legitimate path (fail closed), never admit one outside.
- *Roots, path as written*: compared with the platform's default rule (ignoring case on Windows and macOS), so
  a path written in another letter case than the folder on disk is not refused before its real path can be
  judged. It is a pre-filter only: every caller (`CheckPath` followed by `CheckHandle` or `CheckExistingOutput`
  and `CheckRealPath` for reads and writes, and `CheckFolder` for output and listed folders) judges the real
  path exactly before anything is read, written, listed or created. The residual: on a folder that
  distinguishes case on Windows or macOS, a case-variant sibling passes the pre-filter and is opened for
  inspection (nothing is read or written) before its real path is refused. Linux's pre-filter is already exact.
- *No-symlinks anchor*: found comparing folder names with letter case significant (the root ignoring case on
  Windows only). Otherwise, on a folder that distinguishes case, a link named as a case variant of a folder
  above the configuration file (`Proj` beside `proj`) would be taken for that folder and trusted, and every path
  through it would match. A path written in another letter case than the configuration folder only gets a
  shallower anchor, which is stricter.
- *No-symlinks match*: the anchor's real location must be an exact prefix of the real path, and the part below
  it is compared with the path as written under the platform's default rule, so a path written in another
  letter case still matches where case is ignored. The residual: on a folder that distinguishes case, a link
  whose target differs from the link's own path only in letter case, within the anchor's tree, is not detected
  by no-symlinks alone. It cannot lead outside the anchor's real tree, and roots still compares exactly when
  combined. Detecting it would need a per-folder case-sensitivity query, which is not done.
- *Created folders*: verified exactly (step 5 of the write).

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
- **Glob listing.** CanvasNet has no globs. DictionaryMark judges a glob's literal folder, and every folder a
  wildcard would descend into, by the rules of a folder before it is listed, and fails the pattern when one is
  denied. A file a glob matched is judged by the written-text rules on the pattern's literal part followed by
  the matched path, which is CanvasNet's rule applied to every name, so on Linux and macOS a matched file whose
  name contains a colon is denied.
- **`/dev` on Linux and macOS, for reads too.** CanvasNet refuses a device only by its handle type, so a
  regular file under `/dev` (for example in `/dev/shm`, a world-writable folder) is readable there.
  DictionaryMark never accesses `/dev` or any path under it, for reads, writes, listed folders and `root:`
  alike. The full path is denied before anything is opened, and the real path of every folder handle and of
  every opened read or write handle is denied, also in mode `any`. The reason is a race: a folder of a judged
  path can be swapped for a link to `/dev` between the check and the open. Only the opened handle's real path
  tells where the open went, so refusing `/dev` on it closes that race, and refusing `/dev` before the open
  keeps a device node from being opened at all.
- **Write rules.** CanvasNet never writes. DictionaryMark does the following:
  - It creates missing folders one level at a time and verifies each one.
  - It creates a missing output exclusively (`CreateNew`).
  - It opens an existing output without truncating it and verifies the handle first.
  - In a restrictive mode, it denies a dangling link at the output path.
- **Named pipe as an output.** CanvasNet never writes. On Linux and macOS the non-blocking open for writing of a
  named pipe with no reader fails with `ENXIO`. DictionaryMark treats that as not a regular file, so the write
  is denied without blocking.
- **Letter case of real paths.** CanvasNet compares real paths, the no-symlinks anchor and its location caches
  ignoring case on Windows and macOS, so on a case-sensitive folder or volume it admits a case-variant sibling
  of an allowed folder and can trust a link named as a case variant of a folder above the configuration file.
  DictionaryMark compares every real path, and the anchor's folder names, with letter case significant, keeps
  the platform rule only for the written pre-filter, and on macOS takes the real location of an allowed folder
  or anchor from a handle (`realpath(3)` only for a folder that cannot be opened for reading), the same source
  as every file's real path.
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
- Type unknown: `the type of the opened file cannot be determined on this system, so it cannot be confirmed
  to be a regular file and is never accessed`.

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
  the swapped location (for example in `/dev/shm`, on a share or outside the allowed folders) when the handle is
  then denied, but no content is written there. The empty file is deliberately not deleted, on any platform:
  Linux and macOS have no delete by handle, so a delete by name could remove another file that took the name in
  the meantime; a delete by handle on Windows would need a different open with delete access, which changes
  the sharing semantics; and either would be one more access to a location the policy has just refused.
- An existing output opened by name after such a swap can reach a device node whose open has side effects
  before its handle is refused. The open is non-blocking and nothing is written.
- In mode `any`, a handle whose real path the platform cannot report (for example a Windows volume without a
  drive letter, or Linux without `/proc`) is not judged by the real-path rules. A restrictive mode denies it,
  and the regular-file rule still applies.
- Under roots a configuration file can still overwrite any file inside the allowed folders.
- A by-name open can still pass through a link swapped in between the walk and that open; the handle it
  yields is then judged and denied, so nothing is read, written, listed or created through it. Two operations
  have no handle of their own: listing a folder (bracketed by the folder's handle check just before, which every
  listed folder gets, and each match's handle check when it is read) and creating a folder (bracketed by the
  parent's handle check before and the new folder's handle check after). Closing this window fully would need
  enumeration and creation from a handle, which .NET does not offer.
- A folder onto which a device file system is mounted elsewhere (a bind mount of `/dev` on Linux, or a second
  mount of the device file system on Linux or macOS) reports the mount point as its real path, not `/dev`, so
  it is not recognized as being under `/dev` and can be listed. Mounting needs privileges; a device node there
  is still refused by its handle type when it is read, but a regular file there (for example a bind mount of
  `/dev/shm`) is not.
- On Linux and macOS the .NET listing reads the target of a symbolic link entry to tell whether it is a folder
  (metadata only); the link is judged before anything is listed through it.
- On macOS a folder is told by an `O_DIRECTORY` read open, so a folder that may be searched but not read, used
  as a glob's literal folder or as an output's deepest existing folder, has an unknown real path: a restrictive
  mode denies it, and mode any proceeds (the listing or the write then reports any error).

#### Dependencies

- **RealPath** — opens files, inspects handles and paths (`InspectPath`, `InspectFolder`), captures real
  locations, compares and classifies paths.
- **GlobMatcher** — splits a glob (`SplitGlob`) and matches it below a judged folder (`MatchUnder`), calling
  back before every folder below it is listed.
- .NET base class library (`FileStream`, `RandomAccess`, `Directory`, `File`).

#### Callers

- **Context** — `ParseMode` (through its argument parser), `IsDeviceNamespaceFolder` and `Create`, and
  holds the policy as `Context.FileAccess`.
- **ConfigFile** — `DescribeNeverAccessedFolder` for the `root` key, which also tells whether its folder exists.
  It is called from inside the existence check of `PathResolver.ResolveExistingDirectory`, so only after the
  wildcard and malformed-path checks pass.
- **DictionaryGenerator** — `ResolvePattern`, `ReadAllText` and `WriteAllText` for configuration-file jobs, and
  catches `FileAccessDeniedException`.
