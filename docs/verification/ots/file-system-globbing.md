## FileSystemGlobbing

### Verification Approach

Microsoft.Extensions.FileSystemGlobbing resolves user-supplied glob patterns to concrete file
paths. DictionaryMark uses `Matcher`, `Matcher.AddInclude`, `Matcher.Execute`,
`PatternMatchingResult.Files`, `FilePatternMatch.Path`, and `DirectoryInfoWrapper` from the
`Microsoft.Extensions.FileSystemGlobbing` and `Microsoft.Extensions.FileSystemGlobbing.Abstractions`
namespaces, all called through `GlobMatcher`. FileSystemGlobbing is verified indirectly by the
`GlobMatcherTests` unit tests. Each scenario drives `GlobMatcher.GetFiles` with a specific pattern
or file-system state and asserts the expected result. Because `GlobMatcher` delegates all glob
execution to FileSystemGlobbing, correct test outcomes constitute evidence that the required
FileSystemGlobbing APIs function as specified. No separate qualification testing is required. When
this OTS dependency is updated, the full CI pipeline is re-executed and all test scenarios must
continue to pass before the update is accepted.

### Test Scenarios

**GlobMatcher_GetFiles_ExistingAbsolutePath_ReturnsFile**: `GlobMatcher.GetFiles` is called with a
list containing one absolute path that points to a real temporary file, verifying that
FileSystemGlobbing resolves an exact absolute path to a match. A list containing that file path is
expected.
This scenario is tested by `GlobMatcher_GetFiles_ExistingAbsolutePath_ReturnsFile`.

**GlobMatcher_GetFiles_NonExistentAbsolutePath_ReturnsEmpty**: `GlobMatcher.GetFiles` is called
with an absolute path that does not exist on the file system, verifying that FileSystemGlobbing
produces an empty result for unmatched paths. An empty list is expected.
This scenario is tested by `GlobMatcher_GetFiles_NonExistentAbsolutePath_ReturnsEmpty`.

**GlobMatcher_GetFiles_AbsolutePathGlobPattern_ReturnsMatchingFiles**: `GlobMatcher.GetFiles` is
called with an absolute-path glob pattern when a matching file exists in that directory, verifying
that `Matcher.AddInclude` and `Matcher.Execute` correctly resolve wildcard patterns rooted at an
absolute base directory. A list containing the matching file is expected.
This scenario is tested by `GlobMatcher_GetFiles_AbsolutePathGlobPattern_ReturnsMatchingFiles`.

**GlobMatcher_GetFiles_AbsolutePathGlobPattern_NoMatches_ReturnsEmpty**: `GlobMatcher.GetFiles` is
called with an absolute-path glob pattern whose base directory does not exist, verifying that
FileSystemGlobbing handles a non-existent base directory gracefully. An empty list is expected.
This scenario is tested by `GlobMatcher_GetFiles_AbsolutePathGlobPattern_NoMatches_ReturnsEmpty`.

**GlobMatcher_GetFiles_RelativeGlobPattern_ReturnsMatchingFiles**: `GlobMatcher.GetFiles` is called
with a relative glob pattern while the current directory contains a matching file, verifying that
`Matcher.Execute` with a `DirectoryInfoWrapper` rooted at `Environment.CurrentDirectory` resolves
relative patterns correctly. A list containing the matching file is expected.
This scenario is tested by `GlobMatcher_GetFiles_RelativeGlobPattern_ReturnsMatchingFiles`.

**GlobMatcher_GetFiles_DuplicateAbsolutePaths_DeduplicatesResults**: `GlobMatcher.GetFiles` is
called with two identical absolute paths, verifying that the deduplication layer built on top of
`PatternMatchingResult.Files` operates correctly. A single-element list is expected.
This scenario is tested by `GlobMatcher_GetFiles_DuplicateAbsolutePaths_DeduplicatesResults`.
