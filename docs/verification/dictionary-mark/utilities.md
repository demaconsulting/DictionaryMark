## DictionaryMark Utilities Subsystem Verification

This document describes subsystem-level verification for the Utilities subsystem.

### Verification Strategy

The Utilities subsystem is verified through subsystem integration tests in
`UtilitiesSubsystemTests.cs` that exercise `GlobMatcher` and `PathHelpers` together.
Individual unit tests in `GlobMatcherTests.cs` and `PathHelpersTests.cs` cover each
unit in isolation.

### Dependencies

- **`GlobMatcher`**: Invoked directly to test pattern resolution and deduplication.
- **`PathHelpers`**: Invoked directly to test path combination and traversal rejection.
- **File system**: Temporary directories created via `TemporaryDirectory` to provide real file-system fixtures.

### Test Environment

N/A - standard test environment. Tests use `TemporaryDirectory` to create real file-system
fixtures for glob pattern matching and path combination scenarios.

### Acceptance Criteria

All Utilities subsystem tests in `UtilitiesSubsystemTests.cs` pass; all requirements listed
in the Requirements Coverage section have at least one passing test scenario; no tests may be
skipped or marked as expected failures.

### Test Scenarios

#### UtilitiesSubsystem_PathResolutionWorkflow_ValidPaths_ResolvesCorrectly

**Scenario**: `PathHelpers.SafePathCombine` is called with a base path and several valid
relative paths. Each result is verified to be a fully-qualified absolute path that remains
within the base directory.

**Expected**: Resolved paths remain within the base directory; no exception thrown.

#### UtilitiesSubsystem_PathTraversalValidation_DangerousPaths_ThrowsException

**Scenario**: `PathHelpers.SafePathCombine` is called with a path-traversal sequence
(e.g., `"../escape.txt"`).

**Expected**: `ArgumentException` thrown; no file system access attempted.

#### UtilitiesSubsystem_AbsolutePathRejection_ThrowsException

**Scenario**: `PathHelpers.SafePathCombine` is called with an absolute second path argument
(e.g., `"/etc/passwd"` on Unix or `C:\Windows\...` on Windows).

**Expected**: `ArgumentException` thrown.

#### UtilitiesSubsystem_DirectoryCreationWorkflow_ValidPaths_CreatesDirectories

**Scenario**: `PathHelpers.SafePathCombine` is used to compute safe paths within a temporary
base directory, then `Directory.CreateDirectory` is called with those paths.

**Expected**: Directories are created successfully; no exception thrown.

#### UtilitiesSubsystem_GlobMatcher_ResolvesFiles

**Scenario**: `GlobMatcher.GetFiles` is called with an absolute wildcard pattern (for example,
`{tempDir}/*.yaml`) that matches one or more temporary files created in a temporary directory.

**Expected**: The returned list contains the expected file paths; no exception thrown.

### Requirements Coverage

- **`DictionaryMark-Utilities-FileMatching`**: `UtilitiesSubsystem_GlobMatcher_ResolvesFiles`.
- **`DictionaryMark-Utilities-PathSafety`**: `UtilitiesSubsystem_PathResolutionWorkflow_ValidPaths_ResolvesCorrectly`,
  `UtilitiesSubsystem_PathTraversalValidation_DangerousPaths_ThrowsException`,
  `UtilitiesSubsystem_AbsolutePathRejection_ThrowsException`,
  `UtilitiesSubsystem_DirectoryCreationWorkflow_ValidPaths_CreatesDirectories`.
