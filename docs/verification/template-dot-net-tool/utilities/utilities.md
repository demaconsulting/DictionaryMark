# Utilities Subsystem Verification

This document describes the subsystem-level verification design for the `Utilities` subsystem. It
defines the integration test approach, subsystem boundary, mocking strategy, and test scenarios
that together verify the `Utilities` subsystem requirements.

## Verification Approach

The `Utilities` subsystem is verified by integration tests defined in `UtilitiesSubsystemTests.cs`.
Each test exercises the `PathHelpers` unit through realistic path-combination workflows, confirming
that valid paths are resolved correctly, traversal attacks are rejected, and the resulting paths
can be used for actual directory creation.

## Dependencies and Mocking Strategy

`PathHelpers` depends only on .NET BCL types for path manipulation. No external dependencies
require mocking at the subsystem level. Temporary directories are used when directory-creation
scenarios require a writable base path.

## Integration Test Scenarios

The following integration test scenarios are defined in `UtilitiesSubsystemTests.cs`.

### UtilitiesSubsystem_PathResolutionWorkflow_ValidPaths_ResolvesCorrectly

**Scenario**: Multiple valid relative path arguments are combined with a base path using
`PathHelpers.SafePathCombine`.

**Expected**: All results are correctly combined paths that remain within the base directory;
no exception is thrown.

### UtilitiesSubsystem_PathTraversalValidation_DangerousPaths_ThrowsException

**Scenario**: Multiple path traversal patterns (e.g., `../`, `subfolder/../../../`) are passed to
`PathHelpers.SafePathCombine`.

**Expected**: An `ArgumentException` is thrown for each traversal pattern; no traversal succeeds.

### UtilitiesSubsystem_AbsolutePathRejection_ThrowsException

**Scenario**: Absolute paths (e.g., `/etc/passwd` and on Windows `C:\Windows\System32\file.txt`)
are passed as the relative path argument to `PathHelpers.SafePathCombine`.

**Expected**: An `ArgumentException` is thrown for each absolute path; no injection succeeds.

### UtilitiesSubsystem_DirectoryCreationWorkflow_ValidPaths_CreatesDirectories

**Scenario**: `PathHelpers.SafePathCombine` is used to compute a nested path, and the resulting
path is passed to `Directory.CreateDirectory`.

**Expected**: The directory is created at the expected location within the base directory.

## Requirements Coverage

The `Template-Utilities-SafePaths` requirement is verified by the following test scenarios:

- (valid path resolution): UtilitiesSubsystem_PathResolutionWorkflow_ValidPaths_ResolvesCorrectly
- (traversal rejection): UtilitiesSubsystem_PathTraversalValidation_DangerousPaths_ThrowsException
- (absolute path rejection): UtilitiesSubsystem_AbsolutePathRejection_ThrowsException
- (directory creation): UtilitiesSubsystem_DirectoryCreationWorkflow_ValidPaths_CreatesDirectories
