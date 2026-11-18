### PathResolver

The `PathResolver` unit is verified with unit tests in `PathResolverTests.cs`. Tests call each
method with controlled paths, base directories and environment variables, and assert on the
returned path or the exception message.

#### Verification Approach

`PathResolver` has no dependencies to mock. Its collaborators are the .NET path and environment
APIs and, for `ResolveExistingDirectory`, the real file system. Directories and files are created
in a `TemporaryDirectory` so that existence checks run against real paths.

#### Test Environment

Tests use `TemporaryDirectory` for automatic cleanup. Tests of `${VAR}` expansion set a uniquely
named environment variable and remove it in a `finally` block; the test class runs in the
`Sequential` collection.

#### Acceptance Criteria

- All unit tests in `PathResolverTests.cs` pass with zero failures.
- All requirements linked to the `PathResolver` unit have at least one passing test scenario.
- No tests may be skipped or marked as expected failures.

#### Test Scenarios

**PathResolver_ResolvePath_RelativePath_RootedAtBaseDirectory**: A relative path must be rooted
at the base directory. This scenario is tested by
`PathResolver_ResolvePath_RelativePath_RootedAtBaseDirectory`.

**PathResolver_ResolvePath_ParentSegments_Normalized**: `../../c/file.md` from a base two levels
deep must be normalized to the ancestor path. This scenario is tested by
`PathResolver_ResolvePath_ParentSegments_Normalized`.

**PathResolver_ResolvePath_AbsolutePath_IgnoresBaseDirectory**: An absolute path must be returned
unchanged regardless of the base directory. This scenario is tested by
`PathResolver_ResolvePath_AbsolutePath_IgnoresBaseDirectory`.

**PathResolver_ResolvePath_EnvironmentVariable_Expanded**: A `${VAR}` reference must be replaced by
the variable value before rooting. This scenario is tested by
`PathResolver_ResolvePath_EnvironmentVariable_Expanded`.

**PathResolver_ExpandEnvironmentVariables_Undefined_ThrowsNamingVariableAndDescription**: An
undefined variable must give the exact message naming the variable, the description and the path.
This scenario is tested by
`PathResolver_ExpandEnvironmentVariables_Undefined_ThrowsNamingVariableAndDescription`.

**PathResolver_Combine_GlobPattern_NotNormalized**: `../shared/**/*.yaml` must be rooted with its
`..` segment left in place. This scenario is tested by
`PathResolver_Combine_GlobPattern_NotNormalized`.

**PathResolver_Combine_BaseDirectory_NotExpanded**: A base directory containing a `${VAR}` text
must be used literally even when the variable is defined. This scenario is tested by
`PathResolver_Combine_BaseDirectory_NotExpanded`.

**PathResolver_ResolvePath_NullArguments_Throw**: Each null argument must be rejected with
`ArgumentNullException`. This scenario is tested by `PathResolver_ResolvePath_NullArguments_Throw`.

**PathResolver_ResolveExistingDirectory_RelativeDirectory_ReturnsFullPath**,
**PathResolver_ResolveExistingDirectory_ParentSegments_Normalized** and
**PathResolver_ResolveExistingDirectory_EnvironmentVariable_Expanded**: A relative directory, a
sibling reached through `..`, and a `${VAR}` reference must each resolve to the full path of the
existing directory. These scenarios are tested by the tests of the same names.

**PathResolver_ResolveExistingDirectory_MissingDirectory_ThrowsNamingDirectory**: A missing
directory must give the exact message naming the full path, the description and the value. This
scenario is tested by `PathResolver_ResolveExistingDirectory_MissingDirectory_ThrowsNamingDirectory`.

**PathResolver_ResolveExistingDirectory_PathIsFile_Throws**: A path naming an existing file must
be rejected. This scenario is tested by `PathResolver_ResolveExistingDirectory_PathIsFile_Throws`.

**PathResolver_ResolveExistingDirectory_Wildcard_Throws**: A value containing `*` must be rejected
with the capitalized description in the message. This scenario is tested by
`PathResolver_ResolveExistingDirectory_Wildcard_Throws`.

**PathResolver_ResolveExistingDirectory_ExistencePredicate_DecidesWithoutProbe**: With a caller-supplied
existence check, a missing folder that the check accepts is returned by its normalized full path, which is what
the check receives, and an existing folder that the check refuses is reported as missing; a wildcard is rejected
before the check is called. This scenario is tested by
`PathResolver_ResolveExistingDirectory_ExistencePredicate_DecidesWithoutProbe`.

**PathResolver_ResolvePath_NullCharacter_ThrowsInvalidOperationExceptionNamingPath** and
**PathResolver_ResolveExistingDirectory_NullCharacter_ThrowsInvalidOperationExceptionNamingPath**:
A path containing a null character (invalid on every platform) must be reported as
`InvalidOperationException` with `Invalid {description} '{path}': ` and not escape as a raw
path-validation exception. These scenarios are tested by the tests of the same names.

**PathResolver_IsInvalidPathException_PathValidationExceptions_ReturnsTrue** and
**PathResolver_IsInvalidPathException_OtherExceptions_ReturnsFalse**: `ArgumentException`,
`NotSupportedException` and `PathTooLongException` must be classified as path-validation
exceptions; `ArgumentNullException`, `IOException` and `InvalidOperationException` must not. These
scenarios are tested by the tests of the same names.
