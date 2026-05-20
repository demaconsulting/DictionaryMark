## FileSystemGlobbing Verification

This document provides the verification evidence for the `Microsoft.Extensions.FileSystemGlobbing`
OTS software item. Requirements for this OTS item are defined in the FileSystemGlobbing OTS
Software Requirements document.

### Required Functionality

Microsoft.Extensions.FileSystemGlobbing resolves user-supplied glob patterns to concrete file
paths. DictionaryMark uses `Matcher`, `Matcher.AddInclude`, `Matcher.Execute`,
`PatternMatchingResult.Files`, `FilePatternMatch.Path`, and `DirectoryInfoWrapper` from the
`Microsoft.Extensions.FileSystemGlobbing` and
`Microsoft.Extensions.FileSystemGlobbing.Abstractions` namespaces, all called through
`GlobMatcher`. The unit tests for `GlobMatcher` exercise all required
FileSystemGlobbing APIs through normal class-level calls.

### Qualification Evidence

FileSystemGlobbing is verified indirectly by the `GlobMatcherTests` unit tests.Each scenario
drives `GlobMatcher.GetFiles` with a specific pattern or file-system state and asserts the
expected result. Because `GlobMatcher` delegates all glob execution to FileSystemGlobbing,
correct test outcomes constitute evidence that the required FileSystemGlobbing APIs function as
specified. No separate qualification testing is required.

### Regression Approach

When this OTS dependency is updated, the full CI pipeline is re-executed. All test scenarios must
continue to pass before the update is accepted.

### Test Scenarios

#### GlobMatcher_GetFiles_ExistingAbsolutePath_ReturnsFile

**Scenario**: `GlobMatcher.GetFiles` is called with a list containing one absolute path that
points to a real temporary file.

**Expected**: Returns a list containing that file path, confirming that FileSystemGlobbing
resolves an exact absolute path to a match.

**Requirement coverage**: `DictionaryMark-OTS-FileSystemGlobbing`.

#### GlobMatcher_GetFiles_NonExistentAbsolutePath_ReturnsEmpty

**Scenario**: `GlobMatcher.GetFiles` is called with an absolute path that does not exist on
the file system.

**Expected**: Returns an empty list, confirming that FileSystemGlobbing produces an empty
`PatternMatchingResult.Files` collection for unmatched paths.

**Requirement coverage**: `DictionaryMark-OTS-FileSystemGlobbing`.

#### GlobMatcher_GetFiles_AbsolutePathGlobPattern_ReturnsMatchingFiles

**Scenario**: `GlobMatcher.GetFiles` is called with an absolute-path glob pattern (e.g.,
`/tmp/dir/*.yaml`) when a matching file exists in that directory.

**Expected**: Returns a list containing the matching file, confirming that `Matcher.AddInclude`
and `Matcher.Execute` correctly resolve wildcard patterns rooted at an absolute base directory.

**Requirement coverage**: `DictionaryMark-OTS-FileSystemGlobbing`.

#### GlobMatcher_GetFiles_AbsolutePathGlobPattern_NoMatches_ReturnsEmpty

**Scenario**: `GlobMatcher.GetFiles` is called with an absolute-path glob pattern whose base
directory does not exist.

**Expected**: Returns an empty list, confirming that FileSystemGlobbing handles a non-existent
base directory gracefully.

**Requirement coverage**: `DictionaryMark-OTS-FileSystemGlobbing`.

#### GlobMatcher_GetFiles_RelativeGlobPattern_ReturnsMatchingFiles

**Scenario**: `GlobMatcher.GetFiles` is called with a relative glob pattern (e.g., `*.yaml`)
while the current directory contains a matching file.

**Expected**: Returns a list containing the matching file, confirming that `Matcher.Execute`
with a `DirectoryInfoWrapper` rooted at `Environment.CurrentDirectory` resolves relative
patterns correctly.

**Requirement coverage**: `DictionaryMark-OTS-FileSystemGlobbing`.

#### GlobMatcher_GetFiles_DuplicateAbsolutePaths_DeduplicatesResults

**Scenario**: `GlobMatcher.GetFiles` is called with two identical absolute paths.

**Expected**: Returns a single-element list, confirming that the deduplication layer built on
top of `PatternMatchingResult.Files` operates correctly.

**Requirement coverage**: `DictionaryMark-OTS-FileSystemGlobbing`.

### Requirements Coverage

- **`DictionaryMark-OTS-FileSystemGlobbing`**: GlobMatcher_GetFiles_ExistingAbsolutePath_ReturnsFile,
  GlobMatcher_GetFiles_NonExistentAbsolutePath_ReturnsEmpty,
  GlobMatcher_GetFiles_AbsolutePathGlobPattern_ReturnsMatchingFiles,
  GlobMatcher_GetFiles_AbsolutePathGlobPattern_NoMatches_ReturnsEmpty,
  GlobMatcher_GetFiles_RelativeGlobPattern_ReturnsMatchingFiles,
  GlobMatcher_GetFiles_DuplicateAbsolutePaths_DeduplicatesResults

### Suitability Conclusion

Based on the evidence above, Microsoft.Extensions.FileSystemGlobbing is considered suitable for use
in the DictionaryMark CI pipeline.
