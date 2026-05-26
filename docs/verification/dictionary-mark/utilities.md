## Utilities

### Verification Approach

The Utilities subsystem is verified through integration tests in `UtilitiesSubsystemTests.cs`
that exercise `GlobMatcher` and `PathHelpers` together, using real file-system fixtures created
via `TemporaryDirectory`. Individual units are additionally covered by unit-level tests in
`GlobMatcherTests.cs`, `PathHelpersTests.cs`, and `TemporaryDirectoryTests.cs`. No mocking is
applied at the subsystem boundary; all units execute their real logic against the actual file
system and the `Microsoft.Extensions.FileSystemGlobbing` library.

### Test Environment

N/A - standard test environment. Tests use `TemporaryDirectory` to create real file-system
fixtures for glob pattern matching and path combination scenarios; all fixtures are cleaned up
automatically after each test.

### Acceptance Criteria

- All integration tests in `UtilitiesSubsystemTests.cs` pass with zero failures.
- All subsystem requirements have at least one passing test scenario.
- No tests are skipped or marked as expected failures.

### Test Scenarios

**UtilitiesSubsystem_PathResolutionWorkflow_ValidPaths_ResolvesCorrectly**:
`PathHelpers.SafePathCombine` is called with a base path and several valid relative paths. Each
resolved path must be a fully-qualified absolute path that remains within the base directory and
no exception must be thrown, confirming safe path combination works for legitimate inputs.
This scenario is tested by
`UtilitiesSubsystem_PathResolutionWorkflow_ValidPaths_ResolvesCorrectly`.

**UtilitiesSubsystem_PathTraversalValidation_DangerousPaths_ThrowsException**:
`PathHelpers.SafePathCombine` is called with a path-traversal sequence such as
`"../escape.txt"`. An `ArgumentException` must be thrown and no file-system access must be
attempted, confirming that path-traversal attempts are rejected before any I/O occurs.
This scenario is tested by
`UtilitiesSubsystem_PathTraversalValidation_DangerousPaths_ThrowsException`.

**UtilitiesSubsystem_AbsolutePathRejection_ThrowsException**: `PathHelpers.SafePathCombine` is
called with an absolute second argument such as `"/etc/passwd"` on Unix or `C:\Windows\...` on
Windows. An `ArgumentException` must be thrown, confirming that supplying a rooted path as the
relative argument is rejected.
This scenario is tested by `UtilitiesSubsystem_AbsolutePathRejection_ThrowsException`.

**UtilitiesSubsystem_DirectoryCreationWorkflow_ValidPaths_CreatesDirectories**:
Two `TemporaryDirectory` instances are created to supply real base directories. One path is
taken directly from `TemporaryDirectory.DirectoryPath` and a second is computed by calling
`PathHelpers.SafePathCombine` with a subdirectory name. `Directory.CreateDirectory` is then
called with both paths. All directories must be created successfully with no exception thrown,
confirming that `TemporaryDirectory` construction and `PathHelpers.SafePathCombine` integrate
correctly with directory creation.
This scenario is tested by
`UtilitiesSubsystem_DirectoryCreationWorkflow_ValidPaths_CreatesDirectories`.

**UtilitiesSubsystem_GlobMatcher_ResolvesFiles**: `GlobMatcher.GetFiles` is called with an
absolute wildcard pattern such as `{tempDir}/*.yaml` that matches one or more temporary files
created in a temporary directory. The returned list must contain the expected file paths with no
exception thrown, confirming glob pattern resolution operates correctly against a real file
system.
This scenario is tested by `UtilitiesSubsystem_GlobMatcher_ResolvesFiles`.
