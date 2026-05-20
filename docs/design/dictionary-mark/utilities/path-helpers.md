### PathHelpers Design

The `PathHelpers` class provides safe path-combination utilities that enforce a security
boundary, preventing path-traversal attacks when joining caller-supplied path components.

#### Purpose

`PathHelpers.SafePathCombine` combines a base path with a relative path and verifies that
the resolved result remains within the base directory:

1. Validates that neither argument is null (throws `ArgumentNullException`).
2. Combines the paths with `Path.Combine`.
3. Resolves both the base path and the combined path to their absolute forms with
   `Path.GetFullPath`.
4. Uses `Path.GetRelativePath` to check whether the combined path escapes the base. If the
   relative form starts with `..` or is rooted, the path escapes and an `ArgumentException`
   is thrown.
5. Returns the combined path (in the original, non-resolved form) when the check passes.

No file-system I/O is performed; only string-level path normalization is applied.
`PathHelpers` is a `static` class and is stateless and thread-safe.

#### Data Model

`PathHelpers` is a `static` class with no instance state.

#### Key Methods

##### SafePathCombine(string basePath, string relativePath) -> string

Combines `basePath` and `relativePath` and returns the result when it stays within
`basePath`.

**Throws:**

- `ArgumentNullException` - when `basePath` or `relativePath` is `null`.
- `ArgumentException` - when the resolved combined path escapes the base directory, or when
  a supplied path is otherwise invalid.
- `NotSupportedException` - when a supplied path contains an unsupported format.
- `PathTooLongException` - when the combined or resolved path exceeds the maximum length.

#### Error Handling

`SafePathCombine` throws exceptions for all error conditions; no error is silently swallowed:

- `ArgumentNullException` — when `basePath` or `relativePath` is null.
- `ArgumentException` — when the resolved combined path escapes the base directory (path-traversal
  attempt), or when a supplied path string is otherwise invalid.
- `NotSupportedException` — when a supplied path contains a format not supported by the current
  platform.
- `PathTooLongException` — when the combined or resolved path exceeds the system-defined maximum
  length.

Callers are expected to catch these exceptions and handle or report them appropriately.

#### Interactions

`PathHelpers` has no external dependencies; it uses only `System.IO.Path` from the .NET BCL.

#### Dependencies

`PathHelpers` has no dependencies on other DictionaryMark units, subsystems, or OTS packages.
It uses only `System.IO.Path` from the .NET base class library for path string normalization.

#### Callers

`TemporaryDirectory` calls `PathHelpers.SafePathCombine` on construction (to build the
unique directory path) and in `GetFilePath` (to enforce the directory boundary for each
caller-supplied relative path).

`Validation` (self-test subsystem) previously called `PathHelpers.SafePathCombine` directly;
it now uses `TemporaryDirectory.GetFilePath`, which delegates to `SafePathCombine`
internally.
