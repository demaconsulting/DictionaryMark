### TemporaryDirectory

The `TemporaryDirectory` unit is verified with unit tests in `TemporaryDirectoryTests.cs`.
Tests exercise construction, file-path resolution, path-traversal rejection, and disposal
directly against the file system.

#### Verification Approach

`TemporaryDirectory` is tested with its real dependencies. No mocking is applied:

- **`PathHelpers`** — Exercised indirectly through `GetFilePath`, which delegates to
  `PathHelpers.SafePathCombine`. Tests that call `GetFilePath` with traversal paths verify
  that the rejection propagates correctly without replacing `PathHelpers` with a stub.
- **File system** — Real directories are created on disk during each test. The default
  constructor creates directories under `Environment.CurrentDirectory`; the overload tests
  supply `Path.GetTempPath()` as the base directory. Disposal deletes the directory tree.
  Tests run under `[Collection("Sequential")]` to prevent concurrent file-system interference.

#### Test Environment

Tests run under `[Collection("Sequential")]` because they create and delete real directories
under `Environment.CurrentDirectory` and a caller-supplied base directory (`Path.GetTempPath()`
in constructor overload tests). Sequential execution prevents interference between concurrent
test runs sharing the same file-system locations.

#### Acceptance Criteria

- All unit tests in `TemporaryDirectoryTests.cs` pass with zero failures.
- All requirements linked to the `TemporaryDirectory` unit have at least one passing test
  scenario.
- No tests may be skipped or marked as expected failures.

#### Test Scenarios

**TemporaryDirectory_Constructor_CreatesDirectory**: Verifies that constructing a
`TemporaryDirectory` instance immediately creates the directory on disk, confirming that
`Directory.Exists(DirectoryPath)` returns `true`. This scenario is tested by
`TemporaryDirectory_Constructor_CreatesDirectory`.

**TemporaryDirectory_Constructor_CreatesUniqueDirectories**: Verifies that two `TemporaryDirectory`
instances constructed in sequence receive distinct `DirectoryPath` values, confirming the GUID-
based naming strategy prevents collisions. This scenario is tested by
`TemporaryDirectory_Constructor_CreatesUniqueDirectories`.

**TemporaryDirectory_Constructor_WithBaseDirectory_CreatesDirectoryUnderBase**: Verifies that
when a caller-supplied base directory is provided, the constructed `TemporaryDirectory` creates
its uniquely-named subdirectory inside that base directory, confirming that `DirectoryPath` is
a non-rooted relative child of the supplied base. This scenario is tested by
`TemporaryDirectory_Constructor_WithBaseDirectory_CreatesDirectoryUnderBase`.

**TemporaryDirectory_Constructor_WhitespaceBaseDirectory_ThrowsArgumentException**: Verifies
that supplying a whitespace-only string as `baseDirectory` throws `ArgumentException` directly
from the constructor before any file-system access is attempted. This is a defensive boundary
check with no linked requirement; the test exists to guard against silent misuse of the API.
This scenario is tested by
`TemporaryDirectory_Constructor_WhitespaceBaseDirectory_ThrowsArgumentException`.

**TemporaryDirectory_GetFilePath_SimpleFile_ReturnsPathUnderDirectory**: Verifies that
`GetFilePath("output.md")` returns a path that starts with `DirectoryPath` and ends with
`output.md`, confirming the path is correctly scoped to the temporary directory. This scenario
is tested by `TemporaryDirectory_GetFilePath_SimpleFile_ReturnsPathUnderDirectory`.

**TemporaryDirectory_GetFilePath_NestedPath_CreatesIntermediateDirectories**: Verifies that
calling `GetFilePath` with a multi-segment relative path (e.g. `sub/nested/output.md`) creates
all intermediate directories so the caller can write the file immediately. This scenario is
tested by `TemporaryDirectory_GetFilePath_NestedPath_CreatesIntermediateDirectories`.

**TemporaryDirectory_GetFilePath_TraversalAttempt_ThrowsArgumentException**: Verifies that
calling `GetFilePath("../escaped.txt")` throws `ArgumentException`, confirming that the
path-traversal boundary enforced by `PathHelpers.SafePathCombine` is preserved through
`GetFilePath`. This scenario is tested by
`TemporaryDirectory_GetFilePath_TraversalAttempt_ThrowsArgumentException`.

**TemporaryDirectory_Dispose_DeletesDirectory**: Verifies that after a `TemporaryDirectory` is
created, a file written inside it, and the instance disposed, `Directory.Exists(dirPath)`
returns `false`, confirming recursive deletion on disposal. This scenario is tested by
`TemporaryDirectory_Dispose_DeletesDirectory`.

**TemporaryDirectory_Dispose_AlreadyDeleted_DoesNotThrow**: Verifies that calling `Dispose`
when the underlying directory has already been deleted manually does not throw an exception,
confirming that `IOException` and `UnauthorizedAccessException` are suppressed during cleanup.
This scenario is tested by `TemporaryDirectory_Dispose_AlreadyDeleted_DoesNotThrow`.
