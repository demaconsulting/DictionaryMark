### PathHelpers

![Utilities Subsystem Structure](UtilitiesView.svg)

#### Purpose

`PathHelpers.SafePathCombine` combines a base path with a relative path and verifies that the
resolved result remains within the base directory, preventing path-traversal attacks. No
file-system I/O is performed — only string-level path normalization. `PathHelpers` is a static
class.

#### Data Model

N/A - static class with no instance state.

#### Key Methods

**SafePathCombine**: Combines `basePath` and `relativePath` and returns the result when it stays
within `basePath`.

- *Parameters*: `string basePath` — the trusted base directory path; `string relativePath` — the
  caller-supplied relative path to combine.
- *Returns*: `string` — the combined path (in original, non-resolved form) when the check passes.
- *Preconditions*: `basePath` and `relativePath` are non-null.
- *Postconditions*: Returns a path that, when resolved to absolute form, remains within `basePath`.
  Throws on any escape attempt or invalid input.

Steps: (1) Validate that neither argument is null (throws `ArgumentNullException`). (2) Combine
the paths with `Path.Combine`. (3) Resolve both the base path and the combined path to absolute
form via `Path.GetFullPath`. (4) Call `Path.GetRelativePath(base, combined)` — if the result
starts with `..` or is rooted, the combined path escapes the base and `ArgumentException` is
thrown. (5) Return the combined path when the check passes.

#### Error Handling

- `ArgumentNullException` — thrown when `basePath` or `relativePath` is null.
- `ArgumentException` — thrown when the resolved combined path escapes the base directory
  (path-traversal attempt), or when a supplied path string is otherwise invalid.
- `NotSupportedException` — thrown when a supplied path contains a format not supported by the
  current platform.
- `PathTooLongException` — thrown when the combined or resolved path exceeds the system-defined
  maximum length.

All exceptions propagate to the caller; callers are expected to catch and handle or report them.

#### Dependencies

N/A - no dependencies on other DictionaryMark units or OTS packages; uses only `System.IO.Path`
from the .NET base class library.

#### Callers

- **TemporaryDirectory** — calls `PathHelpers.SafePathCombine` on construction to build the
  unique directory path, and in `GetFilePath` to enforce the directory boundary for each
  caller-supplied relative path.
