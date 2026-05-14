# GlobMatcher Verification

This document describes the unit-level verification design for the `GlobMatcher` unit. It
defines test scenarios, dependency usage, and requirement coverage for `GlobMatcherTests.cs`.

## Verification Approach

`GlobMatcher` is verified with unit tests in `GlobMatcherTests.cs`. Tests use temporary files
and directories to exercise real file-system matching behavior. Paths are cleaned up in
`finally` blocks to avoid test pollution.

## Dependencies

| Dependency  | Usage in Tests                                                            |
| ----------- | ------------------------------------------------------------------------- |
| File system | Temporary files created and deleted per test to verify matching behavior. |

## Test Scenarios

### GlobMatcher_GetFiles_ExistingAbsolutePath_ReturnsFile

**Scenario**: A single absolute path to an existing temp file is passed.

**Expected**: Result contains the file path.

### GlobMatcher_GetFiles_NonExistentAbsolutePath_ReturnsEmpty

**Scenario**: A single absolute path to a non-existent file is passed.

**Expected**: Empty result.

### GlobMatcher_GetFiles_EmptyPatternList_ReturnsEmpty

**Scenario**: Empty pattern list is passed.

**Expected**: Empty result.

### GlobMatcher_GetFiles_NullOrEmptyPattern_ThrowsArgumentException

**Scenario**: A list containing an empty string pattern is passed.

**Expected**: `ArgumentException` is thrown.

### GlobMatcher_GetFiles_DuplicateAbsolutePaths_DeduplicatesResults

**Scenario**: Same absolute path passed twice.

**Expected**: Result contains exactly one entry.

### GlobMatcher_GetFiles_AbsolutePathGlobPattern_ReturnsMatchingFiles

**Scenario**: An absolute path glob pattern (`/tmp/*.yaml`) is passed; a matching temp file
exists.

**Expected**: Result contains the matching file path.

### GlobMatcher_GetFiles_AbsolutePathGlobPattern_NoMatches_ReturnsEmpty

**Scenario**: An absolute path glob pattern pointing to a non-existent directory is passed.

**Expected**: Empty result.

## Requirements Coverage

- **`DictionaryMark-GlobMatcher-GetFiles`**: GlobMatcher_GetFiles_ExistingAbsolutePath_ReturnsFile,
  GlobMatcher_GetFiles_NonExistentAbsolutePath_ReturnsEmpty,
  GlobMatcher_GetFiles_EmptyPatternList_ReturnsEmpty,
  GlobMatcher_GetFiles_DuplicateAbsolutePaths_DeduplicatesResults,
  GlobMatcher_GetFiles_AbsolutePathGlobPattern_ReturnsMatchingFiles,
  GlobMatcher_GetFiles_AbsolutePathGlobPattern_NoMatches_ReturnsEmpty.
- **`DictionaryMark-GlobMatcher-Validation`**: GlobMatcher_GetFiles_NullOrEmptyPattern_ThrowsArgumentException.
