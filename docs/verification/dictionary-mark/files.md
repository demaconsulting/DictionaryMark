## Files

The Files subsystem is verified at subsystem level by `FilesSubsystemTests.cs`, which exercises the
`FileReferenceResolver`, `FileAccessPolicy` and `RealPath` units together through their types over real temporary
trees. Each unit is additionally verified by its own unit tests (see *FileReferenceResolver Verification*,
*FileAccessPolicy Verification* and *RealPath Verification*). The tests are adapted from CanvasNet's Files tests.

### Verification Approach

The subsystem tests use a resolver with the default policy and with combined built-in policies over real temporary
trees, the race-free check with the tree changed inside the open seams (`FileOpenHooks`, real implementations supplied
by the tests), the paths that are refused under every policy, the byte limit, the facts a denial carries, and the
output-path check. No mocking is applied at the subsystem boundary: the units call the real operating system. The
consumers of the subsystem (`Context` and `DictionaryGenerator`) verify in their own tests that they build the policy
and map every refusal to their own error; they do not repeat the subsystem's mechanics.

### Test Environment

- **Framework**: xUnit v3 running under the .NET SDK, invoked by `build.ps1` and the CI pipeline on Windows, Linux
  and macOS with .NET 10 and 11.
- **Files**: per-test temporary directories (`FileTestDirectory`), deleted by the test.
- **Links**: directory links fall back to an NTFS junction on Windows, which needs no privilege, so link and race
  scenarios run everywhere; a file symbolic link needs a privilege on Windows, so the one scenario that needs only
  it skips there with the reason.
- **Operating-system scenarios**: named pipes (`mkfifo`) and devices (`/dev/null`) on Linux and macOS, the null device
  and short (8.3) names on Windows, the `/private` location of the temporary folder on macOS, and execute-only folders
  on Linux and macOS (skipped where permissions are not enforced, as root); each skips on the other systems and is
  linked to its requirement with a source filter.

### Acceptance Criteria

- All tests in `FilesSubsystemTests.cs` pass with zero failures on each operating system of the CI matrix.
- Every Files subsystem requirement has at least one passing test scenario.
- A scenario may skip only on an operating system where it does not exist, with the reason, and its requirement uses
  a source filter for the systems where it runs.

### Test Scenarios

**FilesSubsystem_ReferenceResolution_RelativeDotDotAndAbsoluteReferences_AreReadUnderTheDefaultPolicy**: Validates that
relative, dot-dot and absolute references are read under the default policy, and that a missing reference is no file
rather than a failure. This scenario is tested by
`FilesSubsystem_ReferenceResolution_RelativeDotDotAndAbsoluteReferences_AreReadUnderTheDefaultPolicy`.

**FilesSubsystem_AccessPolicy_RootsAndNoSymlinksCombined_ReadsInsideAndDeniesEscapeAndLink**: Validates that roots and
no-symlinks combined read a plain file inside the root and deny an escape with dot-dot and a file reached through a link
inside the root. This scenario is tested by
`FilesSubsystem_AccessPolicy_RootsAndNoSymlinksCombined_ReadsInsideAndDeniesEscapeAndLink`.

**FilesSubsystem_RaceFreeCheck_SwapAfterOpen_DecisionFollowsTheOpenedFile**: Validates that a change of the tree around
the open does not change the decision: it follows the file that was opened. This scenario is tested by
`FilesSubsystem_RaceFreeCheck_SwapAfterOpen_DecisionFollowsTheOpenedFile`.

**FilesSubsystem_AlwaysRefused_NetworkDeviceAndNonRegularReferences_AreRefusedUnderAllowAll**: Validates that network
and device references, a directory, (on Linux and macOS) a device reached by an absolute path, and (on Windows) a legacy
device name and a local path whose opened file lies on a network share are refused even under the default policy. This
scenario is tested by `FilesSubsystem_AlwaysRefused_NetworkDeviceAndNonRegularReferences_AreRefusedUnderAllowAll`.

**FilesSubsystem_BoundedReading_SizeLimit_ReadsAtTheLimitAndRefusesALongerFile**: Validates that a file at the size
limit is read and a file one byte longer is refused as too large, while a file held open by a writer is still read. This
scenario is tested by `FilesSubsystem_BoundedReading_SizeLimit_ReadsAtTheLimitAndRefusesALongerFile`.

**FilesSubsystem_ReferenceFailureReporting_DenialCarriesFactsWithoutHostPathInMessage**: Validates that a denial carries
the reference, the policy, the reason and the paths, while its message names the reference and the policy but no host
directory. This scenario is tested by
`FilesSubsystem_ReferenceFailureReporting_DenialCarriesFactsWithoutHostPathInMessage`.

**FilesSubsystem_OutputPaths_NeverListAndPolicyApplyBeforeAnyWrite**: Validates that an output path gets the never-list
rules under the default policy and the policy under a restrictive one, before anything is created: an output inside the
roots (in a folder that does not exist yet) is allowed, and an output outside them, a network output and a directory are
refused, with nothing created. This scenario is tested by
`FilesSubsystem_OutputPaths_NeverListAndPolicyApplyBeforeAnyWrite`.

**FilesSubsystem_PatternText_NeverListRefusesPatternsBeforeListing**: Validates that an input pattern gets the
never-list text rules without touching the file system: a local glob passes, a `//host/share` pattern is refused as a
network path and a URL-like pattern is refused by the colon rule. This scenario is tested by
`FilesSubsystem_PatternText_NeverListRefusesPatternsBeforeListing`.
