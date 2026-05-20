### TemporaryDirectory Verification

This document describes the unit-level verification design for the `TemporaryDirectory` unit.
It defines test scenarios, dependency usage, and requirement coverage for
`TemporaryDirectoryTests.cs`.

#### Verification Strategy

`TemporaryDirectory` is verified with unit tests in `TemporaryDirectoryTests.cs`. Tests
exercise construction, file-path resolution, path-traversal rejection, and disposal directly
against the file system under `Environment.CurrentDirectory`.

#### Dependencies

| Dependency    | Usage in Tests                                                         |
| ------------- | ---------------------------------------------------------------------- |
| `PathHelpers` | Invoked indirectly via `GetFilePath`; traversal rejection is verified. |

#### Test Environment

Tests run under `[Collection("Sequential")]` because they create and delete real directories
under `Environment.CurrentDirectory`. Sequential execution prevents interference between
concurrent test runs sharing the same working directory.

#### Acceptance Criteria

All unit tests in `TemporaryDirectoryTests.cs` pass; all requirements listed in the
Requirements Coverage section have at least one passing test scenario; no tests may be
skipped or marked as expected failures.

#### Test Scenarios

##### TemporaryDirectory_Constructor_CreatesDirectory

**Scenario**: A `TemporaryDirectory` instance is constructed.

**Expected**: `Directory.Exists(DirectoryPath)` returns `true`.

##### TemporaryDirectory_Constructor_CreatesUniqueDirectories

**Scenario**: Two `TemporaryDirectory` instances are constructed in sequence.

**Expected**: Their `DirectoryPath` values are distinct.

##### TemporaryDirectory_GetFilePath_SimpleFile_ReturnsPathUnderDirectory

**Scenario**: `GetFilePath("output.md")` is called.

**Expected**: The returned path starts with `DirectoryPath` and ends with `output.md`.

##### TemporaryDirectory_GetFilePath_NestedPath_CreatesIntermediateDirectories

**Scenario**: `GetFilePath` is called with a multi-segment relative path such as
`sub/nested/output.md`.

**Expected**: The parent directory of the returned path exists on disk.

##### TemporaryDirectory_GetFilePath_TraversalAttempt_ThrowsArgumentException

**Scenario**: `GetFilePath("../escaped.txt")` is called.

**Expected**: `ArgumentException` is thrown.

##### TemporaryDirectory_Dispose_DeletesDirectory

**Scenario**: A `TemporaryDirectory` is created, a file is written inside it, and the
instance is disposed.

**Expected**: `Directory.Exists(dirPath)` returns `false` after disposal.

##### TemporaryDirectory_Dispose_AlreadyDeleted_DoesNotThrow

**Scenario**: The underlying directory is deleted manually before `Dispose` is called.

**Expected**: No exception is thrown.

#### Requirements Coverage

- **`DictionaryMark-TemporaryDirectory-DirectoryCreation`**:
  TemporaryDirectory_Constructor_CreatesDirectory,
  TemporaryDirectory_Constructor_CreatesUniqueDirectories.
- **`DictionaryMark-TemporaryDirectory-FilePathResolution`**:
  TemporaryDirectory_GetFilePath_SimpleFile_ReturnsPathUnderDirectory,
  TemporaryDirectory_GetFilePath_NestedPath_CreatesIntermediateDirectories.
- **`DictionaryMark-TemporaryDirectory-TraversalRejection`**:
  TemporaryDirectory_GetFilePath_TraversalAttempt_ThrowsArgumentException.
- **`DictionaryMark-TemporaryDirectory-Disposal`**:
  TemporaryDirectory_Dispose_DeletesDirectory,
  TemporaryDirectory_Dispose_AlreadyDeleted_DoesNotThrow.
