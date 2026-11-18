### RealPath

The `RealPath` unit, which holds all native code of the tool, is verified with unit tests in
`RealPathTests.cs`. Tests open real files, folders, links, devices and named pipes, and assert on the
`HandleFacts` and paths the unit reports. Each platform's native code is verified on that platform.

#### Verification Approach

`RealPath` wraps operating-system calls, so nothing is mocked: the value of the unit is that it reports
what the operating system reports. Expected real paths are computed through `TryGetCanonicalDirectory` of
the containing folder, so the assertions hold where the temporary folder itself is reached through a
link (for example macOS `/var`, which is `/private/var`) or a short Windows name.

The one exception is the link walk of `FindNetworkLinkTarget`, whose overload takes the link reader as a
parameter. Tests pass a fake reader (a dictionary of link targets), so links to a share, NT-form targets,
relative targets, chains, loops and the 63-link limit are verified without a link privilege or a share. A
further test runs the walk with the default reader over a real local folder link.

#### Test Environment

- Folder links are created with the `LinkSupport` test helper: a symbolic link, or on Windows a
  junction when symbolic links need a privilege the account does not hold. These tests therefore run on
  every platform.
- `/dev/null` and named-pipe tests run on Linux and macOS, the `NUL` device test on Windows, and the
  `/private` test on macOS; elsewhere they are skipped with a reason. The named-pipe test fails when the
  open does not finish within 10 seconds.
- The `FindNetworkLinkTarget` tests run on Windows only, because network locations exist only there; elsewhere
  they are skipped with a reason. The injected link reader needs no privilege and no share.
- The stored-letter-case tests open a path in another letter case than stored, so they need a temporary
  folder that ignores case (Windows and macOS by default). They are skipped with a reason where the
  temporary folder distinguishes case (Linux).
- The case-variant sibling test needs a folder that distinguishes case, made by the `CaseSensitiveFolder` test
  helper (see the Test Helpers verification design): an ordinary folder on Linux, per-folder case sensitivity
  on Windows, a case-sensitive APFS disk image on macOS. It is skipped with a reason where none can be made.
- The search-only folder test changes Unix permissions, so it runs on Linux and macOS only.
- Requirements use `windows@`, `ubuntu@` and `macos@` source filters, so the evidence for each
  platform's code comes from a run on that platform.

#### Acceptance Criteria

- All unit tests in `RealPathTests.cs` pass with zero failures on Windows, Linux and macOS.
- A test may be skipped only on a platform it does not apply to, or where the machine cannot make the
  case-sensitive or case-insensitive folder it needs, and every requirement has at least one passing test on
  each platform its source filter names.

#### Test Scenarios

**RealPath_Inspect_PlainFile_ReportsRealPathAndRegular**: A plain file is a regular file whose real path
is its folder's real location followed by its name.

**RealPath_Inspect_FileThroughDirectoryLink_ReportsTarget**: A file opened through a folder link
reports the link target's real path.

**RealPath_Inspect_TempFile_ReportsPrivateLocation** (macOS): A file in the system temporary folder
reports a real path under `/private`.

**RealPath_Inspect_DevNull_IsNotRegular** and **RealPath_OpenForRead_Fifo_DoesNotBlockAndIsNotRegular**
(Linux and macOS), and **RealPath_Inspect_NulDevice_IsNotRegular** (Windows): Devices and named pipes
are reported as not regular files, and opening a pipe without a writer does not block.

**RealPath_OpenForRead_Directory_HandleIsNotRegular** and **RealPath_OpenForRead_MissingFile_ThrowsFileNotFound**:
A folder opens for reading (on Windows with backup semantics) and its handle is reported as not a regular
file; a missing file is reported as `FileNotFoundException`.

**RealPath_Inspect_ClosedHandle_ReportsUnknownFacts**: A handle to a regular file, closed before it is
inspected, cannot be asked; both facts are reported as unknown (null), never as not a regular file.

**RealPath_TryGetCanonicalDirectory_LinkedDirectory_ReturnsTarget** and
**RealPath_TryGetCanonicalDirectory_MissingDirectory_ReturnsNull**: A linked folder resolves to its
target, and a missing folder has no real location.

**RealPath_Inspect_DifferentlyCasedPath_ReportsStoredCase** and
**RealPath_TryGetCanonicalDirectory_DifferentlyCasedPath_ReportsStoredCase** (Windows and macOS): A file
stored as `Glossary/Terms.yaml`, opened as `glossary/terms.yaml`, reports a real path ending in
`Glossary/Terms.yaml`, compared exactly; the folder named `glossary` reports the same real location as the
folder named `Glossary`, ending in `Glossary`.

**RealPath_Inspect_CaseVariantSiblings_ReportsEachExactly**: In a folder that distinguishes case, files in the
sibling folders `allowed` and `ALLOWED` report distinct real paths, each in its own exact letter case.

**RealPath_TryGetCanonicalDirectory_SearchOnlyFolder_ReturnsRealPath** (Linux and macOS): A folder whose owner
may only search it (execute permission only) still has its real location reported, equal to the one reported
while it was readable; on macOS this exercises the `realpath(3)` fallback for a folder that cannot be opened for
reading. The permissions are restored afterwards.

**RealPath_InspectPath_FileFolderMissing_Classified**: From a handle opened without access rights, a missing
file, a path through a missing folder and a folder link whose target was removed are missing; a folder and a
file exist, with their type (unknown on macOS), and the file's real path is its folder's real path followed by
its name.

**RealPath_InspectFolder_FolderFileMissing_Classified**: A folder check reports an existing folder as existing and
a folder on every platform, including macOS (where the open uses `O_DIRECTORY`), with its real path. A file is not
an existing folder: on macOS it is missing, because the open that requires a folder fails with `ENOTDIR`, and
elsewhere it exists as not a folder. A missing path and a folder link whose target was removed are missing.

**RealPath_InspectPath_LongPath_ReportsRealPath** (Windows): A file whose full path is longer than 280
characters is inspected (with the extended-length prefix added internally) and opened for reading.

**RealPath_TryOpenExistingForWrite_ExistingFile_KeepsContent** and
**RealPath_CreateNewForWrite_ExistingEntry_Throws**: Opening an existing file for writing leaves its
content unchanged, and exclusive creation fails when the name is taken.

**RealPath_TryOpenExistingForWrite_Directory_ReturnsNull** (Linux and macOS): Opening a folder for writing
fails with `EISDIR`, which is reported as null (not openable).

**RealPath_IsUnder_BoundaryIsSeparator**, **RealPath_RealPathsEqual_LetterCaseSignificant**,
**RealPath_WrittenPathsEqual_PlatformCaseRule**, **RealPath_IsUnderRealPath_CaseVariantFolder_NotUnder**,
**RealPath_CommonAncestor_ReturnsDeepestSharedFolder**,
**RealPath_CommonAncestor_ComponentsDifferingInCase_NotShared** and
**RealPath_ExpandShortNames_PathWithoutShortName_Unchanged**: These cover the comparison helpers.

- A folder contains itself and its descendants but not `folder2` or its parent, both for real paths and for
  paths as written.
- Real paths differing only in letter case (`Docs` and `DOCS`) are not equal on any platform, and a file in
  `ALLOWED` is not under `allowed` by its real path on any platform.
- Paths as written are equal ignoring case on Windows and macOS and case-sensitive on Linux, so `ALLOWED/a.yaml`
  is under `allowed` as written on Windows and macOS (the pre-filter) and not on Linux.
- Equality ignores a trailing separator.
- The common ancestor compares whole components (`config` and `configs` share only their parent) with letter
  case significant (`Proj` and `proj` share only their parent); on Windows the drive letter is compared ignoring
  case (`c:\base\x` and `C:\base\y` share `c:\base`).
- A path without a short name is returned unchanged.

**RealPath_IsNetworkLocation_UncAndLocal_Classified**: On Windows UNC and `\\?\UNC\` paths are network
locations, while device-namespace and local paths are not. Elsewhere nothing is.

**RealPath_IsWindowsDeviceName_ReservedSpellings**: Reserved names are recognized with extensions,
stream suffixes, trailing spaces, superscript digits and any letter case; `null`, `console`, `COM10`
and `terms.yaml` are not.

**RealPath_IsOrdinaryExtendedPath_Classified**: On every platform, `\\?\C:\docs`, `\\?\C:` and
`\\?\Volume{GUID}\x` are ordinary extended-length paths, while `\\?\UNC\s\x`, `\\?\pipe\x`, `\\.\C:\x`,
`C:\x` and a volume with a malformed GUID are not.

**RealPath_IsLink_SymlinkOrJunction_True**: A folder link (symbolic link or junction) is a link; its
target folder and a missing entry are not.

**RealPath_FindNetworkLinkTarget_IntermediateLinkToShare_ReturnsNetworkPath** (Windows): With a fake link
reader, the walk returns the network path the links lead to, followed by the components after the link.

- A folder link in the middle of the path to `\\server\share\x` is found.
- A target in NT form (`\??\UNC\server\share`) is normalized to `\\server\share`.
- A chain of two links whose second link leads to a share is followed by its targets.
- A relative target (`..\b`) is resolved against the link's folder.
- A chain of 63 links whose last link leads to a share is still found.

**RealPath_FindNetworkLinkTarget_LocalLinksAndLoop_ReturnsNull** (Windows): With a fake link reader, the
walk returns null and ends for a chain of local links (one in NT form `\??\C:\...`), a loop of two links, a
chain of 64 links that would reach a share only after the limit, and a path without links.

**RealPath_FindNetworkLinkTarget_RealLocalLink_ReturnsNull** (Windows): With the default reader, the target
of a real folder link (symbolic link or junction) is read, and a path through the link has no network target.
