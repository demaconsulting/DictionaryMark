### GlobMatcher

The `GlobMatcher` unit is verified with unit tests in `GlobMatcherTests.cs`. Tests use real
temporary files and directories to exercise file-system glob matching behavior.

#### Verification Approach

`GlobMatcher` is a static class with no injectable dependencies. Tests exercise `GetFiles`
directly against the real file system:

- **`Microsoft.Extensions.FileSystemGlobbing`** — The `Matcher` and `DirectoryInfoWrapper`
  types are exercised through real file-system fixtures created with `TemporaryDirectory`
  (for multi-file directory tests) or `Path.GetTempFileName()` (for single-file tests). No
  mocking is applied; the OTS library must behave correctly against real paths.
- **File system** — Real files and directories are created as test fixtures to verify actual
  matching, deduplication, and sorting behavior. `TemporaryDirectory` provides automatic
  cleanup for directory-based tests.

#### Test Environment

N/A - standard test environment. Tests use `TemporaryDirectory` to create real file-system
fixtures for glob pattern matching.

#### Acceptance Criteria

- All unit tests in `GlobMatcherTests.cs` pass with zero failures.
- All requirements linked to the `GlobMatcher` unit have at least one passing test scenario.
- No tests may be skipped or marked as expected failures.

#### Test Scenarios

**GlobMatcher_GetFiles_NullPatterns_ThrowsArgumentNullException**: Verifies that passing a
null reference as the patterns collection throws `ArgumentNullException`, confirming the null
guard is in place. This scenario is tested by
`GlobMatcher_GetFiles_NullPatterns_ThrowsArgumentNullException`.

**GlobMatcher_GetFiles_ExistingAbsolutePath_ReturnsFile**: Verifies that a single absolute
path to an existing temp file is returned as the sole result, confirming direct file resolution
without glob expansion. This scenario is tested by
`GlobMatcher_GetFiles_ExistingAbsolutePath_ReturnsFile`.

**GlobMatcher_GetFiles_NonExistentAbsolutePath_ReturnsEmpty**: Verifies that a single absolute
path to a non-existent file produces an empty result list rather than an exception, confirming
the "no match is not an error" contract. This scenario is tested by
`GlobMatcher_GetFiles_NonExistentAbsolutePath_ReturnsEmpty`.

**GlobMatcher_GetFiles_EmptyPatternList_ReturnsEmpty**: Verifies that an empty pattern list
produces an empty result list, confirming the method handles the zero-pattern edge case
gracefully. This scenario is tested by `GlobMatcher_GetFiles_EmptyPatternList_ReturnsEmpty`.

**GlobMatcher_GetFiles_NullOrEmptyPattern_ThrowsArgumentException**: Verifies that a list
containing a null element or an empty-string element throws `ArgumentException`, covering both
cases via `[Theory]`. This scenario is tested by
`GlobMatcher_GetFiles_NullOrEmptyPattern_ThrowsArgumentException`.

**GlobMatcher_GetFiles_DuplicateAbsolutePaths_DeduplicatesResults**: Verifies that passing the
same absolute path twice returns exactly one entry in the result, confirming case-insensitive
deduplication is applied. This scenario is tested by
`GlobMatcher_GetFiles_DuplicateAbsolutePaths_DeduplicatesResults`.

**GlobMatcher_GetFiles_AbsolutePathGlobPattern_ReturnsMatchingFiles**: Verifies that an
absolute path glob pattern (e.g. `C:\tmp\abc\*.yaml`) returns matching files found under a
`TemporaryDirectory` fixture, confirming wildcard expansion works for absolute base paths. This
scenario is tested by `GlobMatcher_GetFiles_AbsolutePathGlobPattern_ReturnsMatchingFiles`.

**GlobMatcher_GetFiles_AbsolutePathGlobPattern_NoMatches_ReturnsEmpty**: Verifies that an
absolute path glob pattern pointing to a non-existent directory returns an empty list without
error. This scenario is tested by
`GlobMatcher_GetFiles_AbsolutePathGlobPattern_NoMatches_ReturnsEmpty`.

**GlobMatcher_GetFiles_RelativeGlobPattern_ReturnsMatchingFiles**: Verifies that a relative
glob pattern such as `*.yaml` matches files in `Environment.CurrentDirectory` when a matching
file exists there, and that the returned path is absolute. This scenario is tested by
`GlobMatcher_GetFiles_RelativeGlobPattern_ReturnsMatchingFiles`.
