### PathHelpers

The `PathHelpers` unit is verified with unit tests in `PathHelpersTests.cs`. All tests exercise
`SafePathCombine` directly with in-memory path strings; no file-system I/O is required.

#### Verification Approach

`PathHelpers` has no external dependencies beyond `System.IO.Path` from the .NET base class
library. No mocking or stubbing is required:

- **`System.IO.Path`** — Used directly and without interception. All path normalization is
  performed in-memory via string manipulation; no file-system state is read or written.

All test inputs and expected outputs are pure strings, making the tests deterministic and
environment-independent.

#### Test Environment

N/A - standard test environment.

#### Acceptance Criteria

- All unit tests in `PathHelpersTests.cs` pass with zero failures.
- All requirements linked to the `PathHelpers` unit have at least one passing test scenario.
- No tests may be skipped or marked as expected failures.

#### Test Scenarios

**PathHelpers_SafePathCombine_ValidPaths_CombinesCorrectly**: Verifies that a valid base path
and a simple relative path are combined and the result equals `Path.Combine(basePath,
relativePath)`, confirming the normal-operation contract. This scenario is tested by
`PathHelpers_SafePathCombine_ValidPaths_CombinesCorrectly`.

**PathHelpers_SafePathCombine_PathTraversalWithDoubleDots_ThrowsArgumentException**: Verifies
that a relative path with a leading `../` traversal causes `SafePathCombine` to throw
`ArgumentException` with "Invalid path component" in the message, blocking the traversal. This
scenario is tested by
`PathHelpers_SafePathCombine_PathTraversalWithDoubleDots_ThrowsArgumentException`.

**PathHelpers_SafePathCombine_DoubleDotsInMiddle_ThrowsArgumentException**: Verifies that a
relative path containing `/../../../` in the middle causes `SafePathCombine` to throw
`ArgumentException` with "Invalid path component" in the message. This scenario is tested by
`PathHelpers_SafePathCombine_DoubleDotsInMiddle_ThrowsArgumentException`.

**PathHelpers_SafePathCombine_AbsolutePath_ThrowsArgumentException**: Verifies that supplying
an absolute path as the `relativePath` argument causes `SafePathCombine` to throw
`ArgumentException` with "Invalid path component" in the message, because an absolute path
always escapes the base directory boundary. This scenario is tested by
`PathHelpers_SafePathCombine_AbsolutePath_ThrowsArgumentException`.

**PathHelpers_SafePathCombine_CurrentDirectoryReference_CombinesCorrectly**: Verifies that a
relative path starting with `./` is accepted and the result equals `Path.Combine(basePath,
relativePath)`. This scenario is tested by
`PathHelpers_SafePathCombine_CurrentDirectoryReference_CombinesCorrectly`.

**PathHelpers_SafePathCombine_NestedPaths_CombinesCorrectly**: Verifies that a deeply nested
relative path with multiple segments is accepted and combined correctly. This scenario is tested
by `PathHelpers_SafePathCombine_NestedPaths_CombinesCorrectly`.

**PathHelpers_SafePathCombine_EmptyRelativePath_ReturnsBasePath**: Verifies that an empty
string supplied as `relativePath` produces a result equivalent to `Path.Combine(basePath, "")`,
which resolves to the base path. This scenario is tested by
`PathHelpers_SafePathCombine_EmptyRelativePath_ReturnsBasePath`.

**PathHelpers_SafePathCombine_DotDotPrefixedName_CombinesCorrectly**: Verifies that a directory
name that starts with `..` but is not a traversal (e.g. `..data/`) is accepted and combined
correctly, confirming the traversal check is precise and does not over-reject valid names. This
scenario is tested by `PathHelpers_SafePathCombine_DotDotPrefixedName_CombinesCorrectly`.

**PathHelpers_SafePathCombine_NullBasePath_ThrowsArgumentNullException**: Verifies that
supplying `null` as `basePath` throws `ArgumentNullException`. This scenario is tested by
`PathHelpers_SafePathCombine_NullBasePath_ThrowsArgumentNullException`.

**PathHelpers_SafePathCombine_NullRelativePath_ThrowsArgumentNullException**: Verifies that
supplying `null` as `relativePath` throws `ArgumentNullException`. This scenario is tested by
`PathHelpers_SafePathCombine_NullRelativePath_ThrowsArgumentNullException`.
