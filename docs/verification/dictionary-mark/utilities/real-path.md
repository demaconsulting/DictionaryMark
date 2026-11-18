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
- Requirements use `windows@`, `ubuntu@` and `macos@` source filters, so the evidence for each
  platform's code comes from a run on that platform.

#### Acceptance Criteria

- All unit tests in `RealPathTests.cs` pass with zero failures on Windows, Linux and macOS.
- A test may be skipped only on a platform it does not apply to, and every requirement has at least one
  passing test on each platform its source filter names.

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

**RealPath_TryGetCanonicalDirectory_LinkedDirectory_ReturnsTarget** and
**RealPath_TryGetCanonicalDirectory_MissingDirectory_ReturnsNull**: A linked folder resolves to its
target, and a missing folder has no real location.

**RealPath_TryOpenExistingForWrite_ExistingFile_KeepsContent** and
**RealPath_CreateNewForWrite_ExistingEntry_Throws**: Opening an existing file for writing leaves its
content unchanged, and exclusive creation fails when the name is taken.

**RealPath_IsUnder_BoundaryIsSeparator**, **RealPath_PathsEqual_PlatformCaseRule**,
**RealPath_CommonAncestor_ReturnsDeepestSharedFolder** and
**RealPath_ExpandShortNames_PathWithoutShortName_Unchanged**: These cover the comparison helpers.

- A folder contains itself and its descendants but not `folder2` or its parent.
- Equality is case-insensitive on Windows and macOS and case-sensitive on Linux, and ignores a trailing
  separator.
- The common ancestor compares whole components (`config` and `configs` share only their parent).
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
