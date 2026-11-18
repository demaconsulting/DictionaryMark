### RealPath

The `RealPath` unit (`RealPath`, `NativeMethods` and `FileOpenHooks`) is verified with unit tests in
`RealPathTests.cs`, adapted from CanvasNet's; the seams in `FileOpenHooks` are exercised by the resolver tests (see
*FileReferenceResolver Verification*).

#### Verification Approach

The unit is verified against the real operating system, because its purpose is to call it: real temporary files and
folders are opened with `RealPath.OpenForRead`, and the facts reported by `RealPath.Inspect` and
`RealPath.TryGetCanonicalDirectory` are compared with what the test created. The tests reach the internal unit through
the assembly's `InternalsVisibleTo` grant. Every scenario that exists on one operating system only runs there and skips
elsewhere with the reason; the CI matrix (Windows, Linux and macOS) runs each of them, and the requirements link them
with source filters, so only runs on the right system count. The named-pipe open runs with no time limit and no
duration is asserted: an open that blocked would never return, which the time limit of the CI job reports.

#### Test Environment

- **Files**: per-test temporary directories, deleted by the test; `/dev/null` on Linux and macOS.
- **Tools**: `mkfifo` (Linux and macOS) for named pipes; `cmd.exe` (Windows) for junctions and short names;
  `mountvol.exe` (Windows) for the volume GUID path.

#### Acceptance Criteria

- All tests in `RealPathTests.cs` that run on an operating system pass there with zero failures.
- Every `DictionaryMark-RealPath-*` requirement has at least one passing test scenario on each system it names.
- A wrong or missing real path where the system can tell it, a real path for a deleted file, a device or pipe reported
  as a regular file, or a blocked open constitutes a failure.

#### Test Scenarios

**RealPath_Inspect_PlainFile_ReportsItsCanonicalPathAndARegularFile**: Validates that a plain file opened for reading is
a regular file whose final path is its full path in the real location of its folder. This scenario is tested by
`RealPath_Inspect_PlainFile_ReportsItsCanonicalPathAndARegularFile`.

**RealPath_Inspect_FileThroughADirectoryLink_ReportsTheTarget**: Validates that a file opened through a directory link
reports the path of the link's target. This scenario is tested by
`RealPath_Inspect_FileThroughADirectoryLink_ReportsTheTarget`.

**RealPath_Inspect_ShortNameSpelling_IsExpanded**: Validates on Windows that a file opened by its short (8.3) spelling
reports its long spelling, and that short names are expanded. Skips where the volume does not create short names. This
scenario is tested by `RealPath_Inspect_ShortNameSpelling_IsExpanded`.

**RealPath_Inspect_TempFileOnMacOS_ReportsThePrivateLocation**: Validates on macOS that the final path comes back
through `proc_pidfdinfo` in the real location of the temporary folder (`/var` and `/tmp` are links to `/private`), which
pins the offset of the path in the kernel's structure. This scenario is tested by
`RealPath_Inspect_TempFileOnMacOS_ReportsThePrivateLocation`.

**RealPath_Inspect_DeletedFile_IsUnavailable**: Validates on Linux that a file deleted after it was opened has no real
path any more (fail closed for a restrictive policy), while it is still a regular file. This scenario is tested by
`RealPath_Inspect_DeletedFile_IsUnavailable`.

**RealPath_Inspect_LiveFileNamedLikeADeletedOne_ReportsItsRealPath**: Validates that a live file whose name ends with
the text Linux appends to a deleted file's link still reports its real path, suffix included: deletion is told from the
open file, not from its name. This scenario is tested by
`RealPath_Inspect_LiveFileNamedLikeADeletedOne_ReportsItsRealPath`.

**RealPath_Inspect_DirectoryAndDevice_AreNotRegularFiles**: Validates that a device and a directory are not regular
files: on Linux and macOS both open without blocking and are reported as such; on Windows the null device is a character
device and a directory cannot be opened as a file at all. This scenario is tested by
`RealPath_Inspect_DirectoryAndDevice_AreNotRegularFiles`.

**RealPath_OpenForRead_Fifo_DoesNotBlockAndIsNotRegular**: Validates on Linux and macOS that opening a named pipe
without a writer returns at once and the handle is not a regular file. The open runs on the test thread with no time
limit: an open that blocked would never return, which the time limit of the CI job reports. This scenario is tested by
`RealPath_OpenForRead_Fifo_DoesNotBlockAndIsNotRegular`.

**RealPath_OpenForRead_MissingFileAndMissingFolder_MapToNotFoundExceptions**: Validates that a missing file and a
missing folder fail with the "not found" exceptions a caller treats as a missing candidate. This scenario is tested by
`RealPath_OpenForRead_MissingFileAndMissingFolder_MapToNotFoundExceptions`.

**RealPath_TryGetCanonicalDirectory_LinkedDirectory_ReturnsTheTarget**: Validates that the real location of a linked
folder is its target, and that a missing folder has none. This scenario is tested by
`RealPath_TryGetCanonicalDirectory_LinkedDirectory_ReturnsTheTarget`.

**RealPath_TryGetCanonicalDirectory_ExecuteOnlyDirectory_ReturnsItsLocation**: Validates on Linux and macOS that a
folder the process may search but not read (execute-only) still has a real location, the same one it has while it is
readable. Skips when permissions are not enforced (root). This scenario is tested by
`RealPath_TryGetCanonicalDirectory_ExecuteOnlyDirectory_ReturnsItsLocation`.

**RealPath_IsNetworkLocation_UncAndLocalPaths_AreClassified**: Validates on Windows that a UNC path is a network
location, while a local path and the device namespaces are not. A mapped network drive cannot be created on CI and is
not covered here. This scenario is tested by `RealPath_IsNetworkLocation_UncAndLocalPaths_AreClassified`.

**RealPath_IsUnder_BoundaryIsTheSeparator**: Validates the comparison rules: a folder contains itself and what lies
below its separator but not a sibling that shares its name as a prefix, trailing separators do not matter, and the
deepest shared folder is found by whole components. This scenario is tested by
`RealPath_IsUnder_BoundaryIsTheSeparator`.

**RealPath_IsOrdinaryExtendedPath_AcceptsOnlyDriveAndVolumeRoots**: Validates the classification of extended-length and
device-namespace paths (pure text, so it runs on every system): only `\\?\` followed by a drive or a volume GUID is an
ordinary extended-length path; every device-namespace spelling is recognized as one. This scenario is tested by
`RealPath_IsOrdinaryExtendedPath_AcceptsOnlyDriveAndVolumeRoots`.

**RealPath_IsWindowsDeviceName_RecognizesEveryReservedSpelling**: Validates that the reserved Windows device names are
recognized in every spelling Windows accepts, in a single component and anywhere in a path, and that names that merely
resemble them are not. The check is pure text, so it runs on every system. This scenario is tested by
`RealPath_IsWindowsDeviceName_RecognizesEveryReservedSpelling`.
