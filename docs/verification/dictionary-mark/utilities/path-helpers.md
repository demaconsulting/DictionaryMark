### PathHelpers Verification

This document describes the unit-level verification design for the `PathHelpers` unit. It
defines test scenarios, dependency usage, and requirement coverage for `PathHelpersTests.cs`.

#### Verification Approach

`PathHelpers` is verified with unit tests in `PathHelpersTests.cs`. All tests exercise
`SafePathCombine` directly with in-memory path strings; no file-system I/O is required.

#### Dependencies

| Dependency  | Usage in Tests                            |
| ----------- | ----------------------------------------- |
| None        | Pure string-level path logic; no I/O.     |

#### Test Scenarios

##### PathHelpers_SafePathCombine_ValidPaths_CombinesCorrectly

**Scenario**: A valid base path and a simple relative path are combined.

**Expected**: Result equals `Path.Combine(basePath, relativePath)`.

##### PathHelpers_SafePathCombine_PathTraversalWithDoubleDots_ThrowsArgumentException

**Scenario**: A relative path with a leading `../` traversal is supplied.

**Expected**: `ArgumentException` is thrown with "Invalid path component" in the message.

##### PathHelpers_SafePathCombine_DoubleDotsInMiddle_ThrowsArgumentException

**Scenario**: A relative path containing `/../../../` in the middle is supplied.

**Expected**: `ArgumentException` is thrown with "Invalid path component" in the message.

##### PathHelpers_SafePathCombine_AbsolutePath_ThrowsArgumentException

**Scenario**: An absolute path is supplied as the relative argument.

**Expected**: `ArgumentException` is thrown with "Invalid path component" in the message.

##### PathHelpers_SafePathCombine_CurrentDirectoryReference_CombinesCorrectly

**Scenario**: A relative path starting with `./` is supplied.

**Expected**: Result equals `Path.Combine(basePath, relativePath)`.

##### PathHelpers_SafePathCombine_NestedPaths_CombinesCorrectly

**Scenario**: A deeply nested relative path with multiple segments is supplied.

**Expected**: Result equals `Path.Combine(basePath, relativePath)`.

##### PathHelpers_SafePathCombine_EmptyRelativePath_ReturnsBasePath

**Scenario**: An empty string is supplied as the relative path.

**Expected**: Result equals `Path.Combine(basePath, "")`, which is the base path.

##### PathHelpers_SafePathCombine_DotDotPrefixedName_CombinesCorrectly

**Scenario**: A directory name that starts with `..` (but is not a traversal, e.g. `..data/`)
is supplied.

**Expected**: Result equals `Path.Combine(basePath, relativePath)`.

##### PathHelpers_SafePathCombine_NullBasePath_ThrowsArgumentNullException

**Scenario**: `null` is supplied as `basePath`.

**Expected**: `ArgumentNullException` is thrown.

##### PathHelpers_SafePathCombine_NullRelativePath_ThrowsArgumentNullException

**Scenario**: `null` is supplied as `relativePath`.

**Expected**: `ArgumentNullException` is thrown.

#### Requirements Coverage

- **`DictionaryMark-PathHelpers-SafePathCombine`**: PathHelpers_SafePathCombine_ValidPaths_CombinesCorrectly,
  PathHelpers_SafePathCombine_CurrentDirectoryReference_CombinesCorrectly,
  PathHelpers_SafePathCombine_NestedPaths_CombinesCorrectly,
  PathHelpers_SafePathCombine_EmptyRelativePath_ReturnsBasePath,
  PathHelpers_SafePathCombine_DotDotPrefixedName_CombinesCorrectly.
- **`DictionaryMark-PathHelpers-NullValidation`**: PathHelpers_SafePathCombine_NullBasePath_ThrowsArgumentNullException,
  PathHelpers_SafePathCombine_NullRelativePath_ThrowsArgumentNullException.
- **`DictionaryMark-PathHelpers-TraversalValidation`**: PathHelpers_SafePathCombine_PathTraversalWithDoubleDots_ThrowsArgumentException,
  PathHelpers_SafePathCombine_DoubleDotsInMiddle_ThrowsArgumentException,
  PathHelpers_SafePathCombine_AbsolutePath_ThrowsArgumentException.
