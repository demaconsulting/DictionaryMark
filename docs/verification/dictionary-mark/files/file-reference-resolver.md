### FileReferenceResolver

The `FileReferenceResolver` unit (`FileReferenceResolver`, `FileReferenceOptions`, `FileReference`,
`FileReferenceException` and `FileReferenceFailureKind`) is verified with unit tests in
`FileReferenceResolverTests.cs`, `FileReferenceResolverLinkTests.cs`, `FileReferenceResolverRaceTests.cs`,
`FileReferenceResolverOutputTests.cs` and `FileReferenceExceptionTests.cs`. The tests are adapted from CanvasNet's;
the output-path tests are DictionaryMark's.

#### Verification Approach

The resolver is exercised against real temporary trees and the real operating system. Open failures, real paths, file
types and folder locations that cannot be produced on demand are injected through the internal `FileOpenHooks` seams
(`BeforeOpen`, `Open`, `AfterOpen`, `RealPathOf`, `IsRegularFileOf`, `DirectoryLocationOf`), reached through the
assembly's `InternalsVisibleTo` grant, so no test needs a file permission, a lock or a timing window. The race-free
check is proven by changing the tree inside the seams (a folder swapped for a link after the open, a root replaced by a
link before it) and asserting that the decision follows the opened file. Policies are the real built-in policies and,
for the fail-closed handling of a policy that throws or answers nothing, a recording test policy (`RecordingPolicy`,
derived from `FileAccessPolicy`). The documented dependencies `FileAccessPolicy` and `RealPath` are used unmocked.

#### Test Environment

- **Files**: per-test temporary directories, deleted by the test; `/dev/null` and named pipes on Linux and macOS, the
  null device on Windows.
- **Links**: directory links fall back to an NTFS junction on Windows; a scenario that needs a link the host cannot
  create skips with the reason (not expected on CI).
- **Windows-only scenarios**: legacy device names, device-namespace and volume GUID base directories, the null device,
  and a local path whose real path is a network share; each skips elsewhere and is linked with a `windows@` filter.

#### Acceptance Criteria

- All tests in the five test files pass with zero failures on each operating system of the CI matrix.
- Every `DictionaryMark-FileReferenceResolver-*` requirement has at least one passing test scenario.
- A byte read before the policy allowed the file, a path opened that the pre-check or the text rules should have
  refused, a host directory in a message, or anything created by the output check constitutes a failure.

#### Test Scenarios

**FileReferenceResolver_Resolve_DotDotLeavingTheBase_IsReadUnderAllowAll**: Validates that a `..` that leaves the base
directory is read under the default policy, like Pandoc. This scenario is tested by
`FileReferenceResolver_Resolve_DotDotLeavingTheBase_IsReadUnderAllowAll`.

**FileReferenceResolver_Resolve_AbsolutePath_IsASingleCandidateAndIsRead**: Validates that an absolute reference names
exactly one candidate: it is read where it points, and a missing absolute file is a miss after one attempt only. This
scenario is tested by `FileReferenceResolver_Resolve_AbsolutePath_IsASingleCandidateAndIsRead`.

**FileReferenceResolver_Resolve_BackslashesAndDotSegments_NameOneFile**: Validates that backslashes and `.` and `..`
segments are normalized, so every spelling names one file (the very same instance). This scenario is tested by
`FileReferenceResolver_Resolve_BackslashesAndDotSegments_NameOneFile`.

**FileReferenceResolver_Resolve_HostilePathText_IsRejectedWithoutAnyAccess**: Validates that a reference text that is
never allowed is refused with the matching kind before any open. This scenario is tested by
`FileReferenceResolver_Resolve_HostilePathText_IsRejectedWithoutAnyAccess`.

**FileReferenceResolver_Resolve_DrivePath_IsAbsoluteOnWindowsAndInvalidElsewhere**: Validates that a drive path is an
absolute path on Windows and an invalid path on Linux and macOS, which have no drives. This scenario is tested by
`FileReferenceResolver_Resolve_DrivePath_IsAbsoluteOnWindowsAndInvalidElsewhere`.

**FileReferenceResolver_Resolve_NullPath_ThrowsArgumentNullException**: Validates that a null reference is an argument
error. This scenario is tested by `FileReferenceResolver_Resolve_NullPath_ThrowsArgumentNullException`.

**FileReferenceResolver_Resolve_MissingFileAndDanglingLink_ReturnNull**: Validates that a missing file and a dangling
link are both a miss: the result is `null`. This scenario is tested by
`FileReferenceResolver_Resolve_MissingFileAndDanglingLink_ReturnNull`.

**FileReferenceResolver_Resolve_NoBaseDirectory_ReturnsNull**: Validates that a resolver without a base directory finds
nothing. This scenario is tested by `FileReferenceResolver_Resolve_NoBaseDirectory_ReturnsNull`.

**FileReferenceResolver_Resolve_DirectoryAtFilePath_IsNotARegularFile**: Validates that a directory where a file is
expected is not a regular file, whose message names no host directory. This scenario is tested by
`FileReferenceResolver_Resolve_DirectoryAtFilePath_IsNotARegularFile`.

**FileReferenceResolver_Resolve_InjectedNulHandle_IsNotARegularFile**: Validates on Windows that a device handle (the
null device, injected through the open seam) is not a regular file and is never read. This scenario is tested by
`FileReferenceResolver_Resolve_InjectedNulHandle_IsNotARegularFile`.

**FileReferenceResolver_Resolve_DevNull_IsNotARegularFile**: Validates on Linux and macOS that a device reached through
an absolute reference is not a regular file. This scenario is tested by
`FileReferenceResolver_Resolve_DevNull_IsNotARegularFile`.

**FileReferenceResolver_Resolve_Fifo_IsNotARegularFileWithoutBlocking**: Validates on Linux and macOS that a named pipe
without a writer is refused as not a regular file instead of hanging the open. The resolve runs on the test thread with
no time limit: an open that blocked would never return, which the time limit of the CI job reports. This scenario is
tested by `FileReferenceResolver_Resolve_Fifo_IsNotARegularFileWithoutBlocking`.

**FileReferenceResolver_Resolve_OpenFailure_IsCannotRead**: Validates that an existing file whose open fails is a
`CannotRead` failure. This scenario is tested by `FileReferenceResolver_Resolve_OpenFailure_IsCannotRead`.

**FileReferenceResolver_Resolve_FileNotFoundFromOpen_ReturnsNull**: Validates that "not found" from the open itself is a
miss. This scenario is tested by `FileReferenceResolver_Resolve_FileNotFoundFromOpen_ReturnsNull`.

**FileReferenceResolver_Resolve_FileHeldOpenByWriter_IsStillRead**: Validates that a file held open by a writer with
permissive sharing is still read. This scenario is tested by
`FileReferenceResolver_Resolve_FileHeldOpenByWriter_IsStillRead`.

**FileReferenceResolver_Resolve_MaxFileBytesAtLimitAndOneOver**: Validates the byte limit at its boundary. This scenario
is tested by `FileReferenceResolver_Resolve_MaxFileBytesAtLimitAndOneOver`.

**FileReferenceResolver_ReadBounded_EndlessStream_StopsAtTheBound**: Validates that a stream that never ends stops one
chunk past the byte limit. This scenario is tested by `FileReferenceResolver_ReadBounded_EndlessStream_StopsAtTheBound`.

**FileReferenceResolver_Resolve_SameFileTwice_IsOpenedOnceAndReturnsTheSameInstance**: Validates that one resolver opens
a file and a missing file once, returns the same instance for the same file, and does not read a file again after it
changed. This scenario is tested by `FileReferenceResolver_Resolve_SameFileTwice_IsOpenedOnceAndReturnsTheSameInstance`.

**FileReferenceResolver_Resolve_PolicyDenial_IsAccessDeniedWithFactsAndNothingRead**: Validates what the policy is asked
(base directory - the host-chosen directory the chain started from - requested and real path) and that a denial is
reported with the reason, the hint, the policy name and the paths, without a host directory in its message. This
scenario is tested by `FileReferenceResolver_Resolve_PolicyDenial_IsAccessDeniedWithFactsAndNothingRead`.

**FileReferenceResolver_Resolve_ThrowingOrNullPolicy_IsAccessDeniedAndCancellationPassesThrough**: Validates that a
policy that throws or answers nothing denies the file (fail closed), while a cancellation it raises passes through. This
scenario is tested by `FileReferenceResolver_Resolve_ThrowingOrNullPolicy_IsAccessDeniedAndCancellationPassesThrough`.

**FileReferenceResolver_Resolve_CachedFile_IsReCheckedOnEveryResolveButReadOnce**: Validates that a cached file is read
once but the policy is asked on every resolve, so a later denial is honored. This scenario is tested by
`FileReferenceResolver_Resolve_CachedFile_IsReCheckedOnEveryResolveButReadOnce`.

**FileReferenceResolver_Resolve_UnderRootsPreCheck_DeniesBeforeTheOpen**: Validates that a roots policy refuses a path
as written outside its roots before the file is opened. This scenario is tested by
`FileReferenceResolver_Resolve_UnderRootsPreCheck_DeniesBeforeTheOpen`.

**FileReferenceResolver_Resolve_RestrictivePolicyWithoutRealPath_IsRealPathUnavailable**: Validates that a restrictive
policy fails closed when the real path is unavailable, while the default policy still reads the file. This scenario is
tested by `FileReferenceResolver_Resolve_RestrictivePolicyWithoutRealPath_IsRealPathUnavailable`.

**FileReferenceResolver_Resolve_UnknownFileType_IsNotARegularFileUnderEveryPolicy**: Validates that a handle whose type
the platform cannot tell is refused as not a regular file under every policy, the default included (fail closed), and
that nothing is read. This scenario is tested by
`FileReferenceResolver_Resolve_UnknownFileType_IsNotARegularFileUnderEveryPolicy`.

**FileReferenceResolver_Resolve_LocalPathWhoseRealPathIsANetworkShare_IsNetworkPathUnderEveryPolicy**: Validates on
Windows that a local path whose opened file lies on a network share (a link to a UNC target, injected through the
real-path seam) is refused as a network path under every policy, without a host directory in its message, and that the
refusal is remembered without opening the file again. This scenario is tested by
`FileReferenceResolver_Resolve_LocalPathWhoseRealPathIsANetworkShare_IsNetworkPathUnderEveryPolicy`.

**FileReferenceResolver_Resolve_LegacyDeviceName_IsInvalidPathAndNoDeviceIsOpened**: Validates on Windows that a
reference with a reserved device name in any component (in any letter case, with an extension, trailing spaces or dots,
the superscript-digit forms, and the console names) is refused as an invalid path before anything is opened, whether or
not this Windows version rewrites the path to a device. This scenario is tested by
`FileReferenceResolver_Resolve_LegacyDeviceName_IsInvalidPathAndNoDeviceIsOpened`.

**FileReferenceResolver_Resolve_LegacyDeviceNameWithExtendedLengthBaseDirectory_IsInvalidPathAndNoDeviceIsOpened**:
Validates on Windows that with an extended-length base directory (`\\?\`) an absolute reference that the system maps to
a device (`C:/x/NUL` gives `\\.\NUL`) is still refused as an invalid path with nothing opened, while a file under the
extended-length base directory is still found. This scenario is tested by
`FileReferenceResolver_Resolve_LegacyDeviceNameWithExtendedLengthBaseDirectory_IsInvalidPathAndNoDeviceIsOpened`.

**FileReferenceResolver_Resolve_DeviceNamespaceBaseDirectory_IsInvalidPathAndNothingIsOpened**: Validates on Windows
that a base directory in a device namespace other than an ordinary extended-length drive or volume root
(`\\?\GLOBALROOT\`, `\\?\UNC\`, `\\?\pipe\`, `\\.\`) gives no exemption: a relative reference under it is refused as a
device path before anything is opened. This scenario is tested by
`FileReferenceResolver_Resolve_DeviceNamespaceBaseDirectory_IsInvalidPathAndNothingIsOpened`.

**FileReferenceResolver_Resolve_VolumeGuidBaseDirectory_ReadsTheFile**: Validates on Windows that a base directory given
as an ordinary extended-length volume path (`\\?\Volume{GUID}\...`) keeps working: a file under it is read. Skips when
the volume GUID path of the temporary folder's drive cannot be determined. This scenario is tested by
`FileReferenceResolver_Resolve_VolumeGuidBaseDirectory_ReadsTheFile`.

**FileReferenceResolver_Resolve_AbsoluteReferenceDenied_MessageEchoesItButNotTheRealPathOrBase**: Validates that the
message of a denied absolute reference echoes the reference exactly as written, which the author supplied, but never
adds the real path of the opened file or the base directory; those stay in the properties for trusted callers. This
scenario is tested by `FileReferenceResolver_Resolve_AbsoluteReferenceDenied_MessageEchoesItButNotTheRealPathOrBase`.

**FileReferenceResolver_Resolve_InvalidBaseDirectory_IsInvalidBaseDirectory**: Validates that a configured directory
that is not a valid path is reported on first use. This scenario is tested by
`FileReferenceResolver_Resolve_InvalidBaseDirectory_IsInvalidBaseDirectory`.

**FileReferenceResolver_Constructor_InvalidArguments_Throw**: Validates that invalid options are rejected when the
resolver is created. This scenario is tested by `FileReferenceResolver_Constructor_InvalidArguments_Throw`.

**FileReferenceResolver_Resolve_DirectoryLinkUnderAllowAll_IsFollowed**: Validates that the default policy follows a
directory link and reports the real path of the target. This scenario is tested by
`FileReferenceResolver_Resolve_DirectoryLinkUnderAllowAll_IsFollowed`.

**FileReferenceResolver_Resolve_DirectoryLinkUnderNoSymlinks_IsAccessDenied**: Validates that the no-symlinks policy
refuses a file reached through a directory link, whatever its target. This scenario is tested by
`FileReferenceResolver_Resolve_DirectoryLinkUnderNoSymlinks_IsAccessDenied`.

**FileReferenceResolver_Resolve_DirectoryLinkStayingInsideTheRoots_IsAllowed**: Validates that a roots policy follows a
link that stays inside the roots. This scenario is tested by
`FileReferenceResolver_Resolve_DirectoryLinkStayingInsideTheRoots_IsAllowed`.

**FileReferenceResolver_Resolve_DirectoryLinkLeavingTheRoots_IsAccessDenied**: Validates that a roots policy refuses a
link that leads out of the roots, after the open and before the read. This scenario is tested by
`FileReferenceResolver_Resolve_DirectoryLinkLeavingTheRoots_IsAccessDenied`.

**FileReferenceResolver_Resolve_LinkedBaseDirectoryUnderNoSymlinks_IsTrusted**: Validates that the no-symlinks policy
trusts a base directory that is itself reached through a link: only links below the folder a reference starts from
count. This scenario is tested by `FileReferenceResolver_Resolve_LinkedBaseDirectoryUnderNoSymlinks_IsTrusted`.

**FileReferenceResolver_Resolve_FileSymbolicLinkUnderNoSymlinks_IsAccessDeniedOrCapabilitySkipped**: Validates that the
no-symlinks policy refuses a file that is itself a symbolic link. File links need a privilege on Windows, so the test
skips there when the host cannot create one; the same decision is covered on every system by the injected real-path
tests. This scenario is tested by
`FileReferenceResolver_Resolve_FileSymbolicLinkUnderNoSymlinks_IsAccessDeniedOrCapabilitySkipped`.

**FileReferenceResolver_Resolve_FileMovedAndFolderSwappedForOutsideLinkAfterOpen_IsAllowedWithOriginalBytes**: Validates
that when the opened inside file is moved away and its folder is replaced by a link to an outside folder after the open,
the file is still allowed and its original bytes are read: the handle, not the name, decides. This scenario is tested by
`FileReferenceResolver_Resolve_FileMovedAndFolderSwappedForOutsideLinkAfterOpen_IsAllowedWithOriginalBytes`.

**FileReferenceResolver_Resolve_RootReplacedByOutsideLinkBeforeTheOpen_IsAccessDenied**: Validates that a root renamed
and replaced by a link to an outside folder after the pre-check and before the open is denied under each restrictive
policy: the real location of the trusted folder was captured before (when the roots policy was created, and before the
open for the no-symlinks policy) and is never resolved by name again, so the outside file the open reached does not
match it. This scenario is tested by
`FileReferenceResolver_Resolve_RootReplacedByOutsideLinkBeforeTheOpen_IsAccessDenied`.

**FileReferenceResolver_Resolve_AnchorLocationNotCaptured_IsAccessDeniedUnderNoSymlinks**: Validates that the
no-symlinks policy fails closed when the real location of the folder a reference starts from could not be captured
before the open. This scenario is tested by
`FileReferenceResolver_Resolve_AnchorLocationNotCaptured_IsAccessDeniedUnderNoSymlinks`.

**FileReferenceResolver_Resolve_OutsideLinkReplacedByInsideFolderAfterOpen_IsAccessDeniedUnderUnderRoots**: Validates
under the roots policy that when a link to an outside folder is replaced by a real inside folder after the open, the
file is still denied: the handle refers to the outside file. This scenario is tested by
`FileReferenceResolver_Resolve_OutsideLinkReplacedByInsideFolderAfterOpen_IsAccessDeniedUnderUnderRoots`.

**FileReferenceResolver_Resolve_OutsideLinkReplacedByInsideFolderAfterOpen_IsAccessDeniedUnderNoSymlinks**: Validates
the same swap under the no-symlinks policy: the link is gone by name, but the handle was opened through it, so the file
is denied. This scenario is tested by
`FileReferenceResolver_Resolve_OutsideLinkReplacedByInsideFolderAfterOpen_IsAccessDeniedUnderNoSymlinks`.

**FileReferenceResolver_Resolve_InjectedOutsideRealPath_IsAccessDenied**: Validates on a plain tree without links that
the decision uses the real path reported for the handle: an injected outside real path is denied by both restrictive
policies. This scenario is tested by `FileReferenceResolver_Resolve_InjectedOutsideRealPath_IsAccessDenied`.

**FileReferenceResolver_Resolve_InjectedRealPath_IsTheOneInTheRequestAndTheResult**: Validates that the real path
reported for the handle is the one the policy sees and the one the result carries. This scenario is tested by
`FileReferenceResolver_Resolve_InjectedRealPath_IsTheOneInTheRequestAndTheResult`.

**FileReferenceException_Constructor_EveryKind_KeepsFactsAndHidesHostPath**: Validates, for every failure kind, that the
exception keeps its facts and that the message is a sentence that never contains a host path, and contains the reason
only for a rule of the path text. This scenario is tested by
`FileReferenceException_Constructor_EveryKind_KeepsFactsAndHidesHostPath`.

**FileReferenceException_Constructor_NullReference_ThrowsArgumentNullException**: Validates that a null reference is an
argument error. This scenario is tested by
`FileReferenceException_Constructor_NullReference_ThrowsArgumentNullException`.

**FileReferenceException_Constructor_ControlCharacters_AreReplacedInReferenceAndMessage**: Validates that a reference
with control characters, passed straight to the public constructor, is stored and shown with each control character
replaced by `?`. This scenario is tested by
`FileReferenceException_Constructor_ControlCharacters_AreReplacedInReferenceAndMessage`.

**FileReferenceResolver_CheckOutputPath_NullPath_Throws**: Validates that a null output path is an argument error. This
scenario is tested by `FileReferenceResolver_CheckOutputPath_NullPath_Throws`.

**FileReferenceResolver_CheckOutputPath_InsideRootsWithMissingParents_IsAllowedAndCreatesNothing**: Validates that an
output inside the roots, in folders that do not exist yet, is allowed under roots and no-symlinks combined, and that the
check creates nothing. This scenario is tested by
`FileReferenceResolver_CheckOutputPath_InsideRootsWithMissingParents_IsAllowedAndCreatesNothing`.

**FileReferenceResolver_CheckOutputPath_ExistingFileInsideRoots_IsAllowedAndUnchanged**: Validates that an existing
regular output file inside the roots is allowed and left unchanged. This scenario is tested by
`FileReferenceResolver_CheckOutputPath_ExistingFileInsideRoots_IsAllowedAndUnchanged`.

**FileReferenceResolver_CheckOutputPath_OutsideRoots_IsAccessDeniedBeforeAnyOpen**: Validates that an output outside the
roots is denied by the pre-check, before anything is opened. This scenario is tested by
`FileReferenceResolver_CheckOutputPath_OutsideRoots_IsAccessDeniedBeforeAnyOpen`.

**FileReferenceResolver_CheckOutputPath_NeverListText_IsRefusedUnderAllowAll**: Validates that an output path that
breaks a never-list text rule is refused under the default policy before anything is opened. This scenario is tested by
`FileReferenceResolver_CheckOutputPath_NeverListText_IsRefusedUnderAllowAll`.

**FileReferenceResolver_CheckOutputPath_DeviceName_IsInvalidPathOnWindows**: Validates on Windows that an output with a
reserved device name is refused under the default policy before anything is opened. This scenario is tested by
`FileReferenceResolver_CheckOutputPath_DeviceName_IsInvalidPathOnWindows`.

**FileReferenceResolver_CheckOutputPath_ExistingDirectory_IsNotARegularFile**: Validates that an existing directory at
the output path is refused as not a regular file. This scenario is tested by
`FileReferenceResolver_CheckOutputPath_ExistingDirectory_IsNotARegularFile`.

**FileReferenceResolver_CheckOutputPath_ExistingFileThroughDirectoryLinkUnderNoSymlinks_IsAccessDenied**: Validates that
an existing output reached through a directory link is denied under no-symlinks. This scenario is tested by
`FileReferenceResolver_CheckOutputPath_ExistingFileThroughDirectoryLinkUnderNoSymlinks_IsAccessDenied`.

**FileReferenceResolver_CheckOutputPath_MissingFileUnderParentLinkLeavingTheRoots_IsAccessDenied**: Validates that an
output that does not exist yet, below a directory link inside the roots that leads outside them, is denied: its real
path is the real location of the linked folder. This scenario is tested by
`FileReferenceResolver_CheckOutputPath_MissingFileUnderParentLinkLeavingTheRoots_IsAccessDenied`.

**FileReferenceResolver_CheckOutputPath_MissingFileUnderDirectoryLinkUnderNoSymlinks_IsAccessDenied**: Validates that an
output that does not exist yet, below a directory link inside the base directory, is denied under no-symlinks. This
scenario is tested by
`FileReferenceResolver_CheckOutputPath_MissingFileUnderDirectoryLinkUnderNoSymlinks_IsAccessDenied`.

**FileReferenceResolver_CheckOutputPath_DanglingLinkAtOutput_IsRefusedUnderEveryPolicy**: Validates that a dangling link
at the output path is an existing output that cannot be opened, so it is refused under every policy (the default
included) and its target is never created. The link is a file symbolic link, or, where the host cannot create one, a
directory link (a junction on Windows) whose target was removed; the test skips when the host can create neither. This
scenario is tested by `FileReferenceResolver_CheckOutputPath_DanglingLinkAtOutput_IsRefusedUnderEveryPolicy`.

**FileReferenceResolver_CheckOutputPath_RealPathUnavailable_IsRealPathUnavailable**: Validates that a restrictive policy
fails closed when the real path of an output cannot be determined, for an output that does not exist yet and for an
existing one, while the default policy allows both. This scenario is tested by
`FileReferenceResolver_CheckOutputPath_RealPathUnavailable_IsRealPathUnavailable`.

**FileReferenceResolver_CheckOutputPath_ParentOnNetworkShare_IsNetworkPath**: Validates on Windows that an output that
does not exist yet, whose deepest existing folder lies on a network share while the path as written is local, is refused
as a network path under the default policy. This scenario is tested by
`FileReferenceResolver_CheckOutputPath_ParentOnNetworkShare_IsNetworkPath`.

**FileReferenceResolver_CheckOutputPath_ExistingFileOpenFailure_IsCannotRead**: Validates that an existing output whose
open fails is refused as `CannotRead`, with the cause kept. This scenario is tested by
`FileReferenceResolver_CheckOutputPath_ExistingFileOpenFailure_IsCannotRead`.

**FileReferenceResolver_CheckOutputPath_NoBaseDirectory_ThrowsInvalidOperationException**: Validates that a resolver
without a base directory cannot check an output path. This scenario is tested by
`FileReferenceResolver_CheckOutputPath_NoBaseDirectory_ThrowsInvalidOperationException`.

**FileReferenceResolver_CheckPatternText_NullPattern_Throws**: Validates that a null pattern is an argument error.
This scenario is tested by `FileReferenceResolver_CheckPatternText_NullPattern_Throws`.

**FileReferenceResolver_CheckPatternText_LocalGlob_Passes**: Validates that an ordinary local glob passes the text
rules. This scenario is tested by `FileReferenceResolver_CheckPatternText_LocalGlob_Passes`.

**FileReferenceResolver_CheckPatternText_ForwardSlashShare_IsNetworkPath**: Validates that a pattern that starts with
two forward slashes is refused as a network path on every platform. This scenario is tested by
`FileReferenceResolver_CheckPatternText_ForwardSlashShare_IsNetworkPath`.

**FileReferenceResolver_CheckPatternText_BackslashShareOnWindows_IsNetworkPath**: Validates on Windows that a pattern
that starts with two backslashes is refused as a network path. This scenario is tested by
`FileReferenceResolver_CheckPatternText_BackslashShareOnWindows_IsNetworkPath`.

**FileReferenceResolver_CheckPatternText_UrlLikePattern_IsInvalidPath**: Validates that a URL-like pattern rooted at a
local folder is refused by the colon rule. This scenario is tested by
`FileReferenceResolver_CheckPatternText_UrlLikePattern_IsInvalidPath`.
