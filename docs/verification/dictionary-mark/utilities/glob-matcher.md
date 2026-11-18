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

Tests use `TemporaryDirectory` to create real file-system fixtures for glob pattern matching. The
`MatchUnder` tests create a folder link with the `LinkSupport` test helper: a symbolic link, or on
Windows a junction when symbolic links need a privilege the account does not hold, so they run on every
platform; they are skipped with a reason only where neither can be created.

#### Acceptance Criteria

- All unit tests in `GlobMatcherTests.cs` pass with zero failures.
- All requirements linked to the `GlobMatcher` unit have at least one passing test scenario.
- No tests may be skipped or marked as expected failures, except a `MatchUnder` test on a machine where
  no folder link can be created.

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

**GlobMatcher_Resolve_LiteralAndGlob_ReturnsFiles**: Literal paths and globs resolve to files. This
scenario is tested by `GlobMatcher_Resolve_LiteralAndGlob_ReturnsFiles`.

**GlobMatcher_Resolve_GlobWithNoMatch_ThrowsNamingPattern**: A glob matching nothing fails naming
the pattern. This scenario is tested by `GlobMatcher_Resolve_GlobWithNoMatch_ThrowsNamingPattern`.

**GlobMatcher_Resolve_MissingFileOrDirectory_ThrowsDistinctErrors**: Missing files and directories
give distinct errors. This scenario is tested by
`GlobMatcher_Resolve_MissingFileOrDirectory_ThrowsDistinctErrors`.

**GlobMatcher_Resolve_RelativeLiteral_ResolvesFullPath**: A relative literal resolves to a full
path. This scenario is tested by `GlobMatcher_Resolve_RelativeLiteral_ResolvesFullPath`.

**GlobMatcher_Resolve_EnvironmentVariable_IsExpandedOrFails**: `${VAR}` is expanded, and an
undefined variable fails. This scenario is tested by
`GlobMatcher_Resolve_EnvironmentVariable_IsExpandedOrFails`.

**GlobMatcher_Resolve_UncPath_ResolvesOnWindows**: UNC paths resolve on Windows. This scenario is
tested by `GlobMatcher_Resolve_UncPath_ResolvesOnWindows`.

**GlobMatcher_Resolve_BaseDirectory_ResolvesRelativePatterns**: A literal and a glob that are relative
to a base directory other than the working directory must resolve to the files under that base
directory. This scenario is tested by `GlobMatcher_Resolve_BaseDirectory_ResolvesRelativePatterns`.

**GlobMatcher_Resolve_LeadingParentSegmentGlob_ReturnsFiles**: `../shared/*.yaml` from a base
directory must match the file in the sibling `shared` directory. This scenario is tested by
`GlobMatcher_Resolve_LeadingParentSegmentGlob_ReturnsFiles`.

**GlobMatcher_Resolve_NestedParentSegmentGlob_ReturnsFiles**: `../../a/**/*.yaml` (a recursive glob
after two `..` segments) and `sub/../../../shared/*.yaml` (`..` after a normal segment, which the
globbing library alone rejects) must both match. This scenario is tested by
`GlobMatcher_Resolve_NestedParentSegmentGlob_ReturnsFiles`.

**GlobMatcher_Resolve_ParentSegmentLiteral_ReturnsFile**: The literal `../shared/terms.yaml` must
resolve against the base directory. This scenario is tested by
`GlobMatcher_Resolve_ParentSegmentLiteral_ReturnsFile`.

**GlobMatcher_SplitGlob_FolderAndPattern**: A glob splits into its literal folder and remainder: `/*.yaml`
gives `/`, `C:/*.yaml` gives `C:/`, `*.yaml` gives an empty folder, `../a/**/*.yaml` keeps `../a`, backslashes
become slashes, and a `?` in a middle component starts the remainder there.

**GlobMatcher_MatchUnder_LinkFolder_CheckedBeforeListing**: Matching `**/*.yaml` below a folder that holds a
real sub-folder and a folder link calls the check once, with the link's full path, and lists the link when the
check allows it (its file is matched with its full path through the link). `*.yaml`, which never descends,
never calls the check, and a missing folder matches nothing.

**GlobMatcher_MatchUnder_RejectedLink_NotListed**: A check that returns false lists nothing in the link, and a
check that throws stops the matching with its exception.

**GlobMatcher_MatchUnder_MissingFolder_ReturnsEmpty**: Matching `**/*.yaml` below a folder that does not exist
matches nothing, without an error, and never calls the check.

**GlobMatcher_MatchUnder_FolderRemovedAfterCheck_MatchesNothingInIt**: Below a folder that holds a real
sub-folder and a folder link, a check that deletes the link's target and then allows it (so the folder is
removed after it was judged and before it is listed) is called once, with the link's full path. The link lists
nothing, without an error, and only the file in the real sub-folder is matched.
