## DictionaryMark Utilities Subsystem Verification

This document describes subsystem-level verification for the Utilities subsystem.

### Verification Approach

The Utilities subsystem is verified through subsystem integration tests in
`UtilitiesSubsystemTests.cs` that exercise `GlobMatcher` and `PathHelpers` together.
Individual unit tests in `GlobMatcherTests.cs` and `PathHelpersTests.cs` cover each
unit in isolation.

### Dependencies

| Dependency    | Usage in Tests                                                       |
|---------------|----------------------------------------------------------------------|
| `GlobMatcher` | Invoked directly to test pattern resolution and deduplication.       |
| `PathHelpers` | Invoked directly to test path combination and traversal rejection.   |

### Test Scenarios

### UtilitiesSubsystem_PathResolutionWorkflow_ValidPaths_ResolvesCorrectly

**Scenario**: `GlobMatcher` resolves a concrete file path; result is passed to
`PathHelpers.SafePathCombine`.

**Expected**: Resolved path matches the expected absolute file path; no exception thrown.

### UtilitiesSubsystem_PathTraversalValidation_DangerousPaths_ThrowsException

**Scenario**: `PathHelpers.SafePathCombine` is called with a path-traversal sequence.

**Expected**: `InvalidOperationException` thrown; no file system access attempted.

### UtilitiesSubsystem_AbsolutePathRejection_ThrowsException

**Scenario**: `PathHelpers.SafePathCombine` is called with an absolute second path argument.

**Expected**: `InvalidOperationException` thrown.

### UtilitiesSubsystem_DirectoryCreationWorkflow_ValidPaths_CreatesDirectories

**Scenario**: `PathHelpers.EnsureDirectoryExists` is called with a valid safe path.

**Expected**: Directory is created; no exception thrown.

### Requirements Coverage

- **`DictionaryMark-Utilities-Subsystem`**: All `UtilitiesSubsystem_*` tests.
