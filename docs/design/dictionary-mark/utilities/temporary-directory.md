### TemporaryDirectory Design

The `TemporaryDirectory` class provides a disposable temporary directory that is
automatically created on construction and deleted on disposal. It is used by both
production self-test code (`Validation`) and by unit and integration test infrastructure.

#### Purpose

`TemporaryDirectory` encapsulates three responsibilities:

1. **Construction** — generates a uniquely-named subdirectory path using
   `PathHelpers.SafePathCombine(effectiveBaseDirectory, "tmp-{guid}")` and calls
   `Directory.CreateDirectory` to create it on disk. `effectiveBaseDirectory` is the caller-
   supplied `baseDirectory` when provided, otherwise `Environment.CurrentDirectory`. The
   default uses `Environment.CurrentDirectory` rather than `Path.GetTempPath()` to avoid the
   macOS `/tmp` → `/private/tmp` symlink issue that causes path-comparison failures when the
   OS returns the resolved path instead of the construction path.
2. **File-path resolution** — `GetFilePath(relativePath)` delegates to
   `PathHelpers.SafePathCombine` to enforce the directory boundary, then calls
   `Directory.CreateDirectory` on the parent of the returned path so that any intermediate
   subdirectories are created before the caller writes the file.
3. **Disposal** — `Dispose` calls `Directory.Delete(path, recursive: true)` to remove the
   directory tree. `IOException` and `UnauthorizedAccessException` are suppressed so that
   cleanup failures do not mask the original test or self-test outcome.

#### Data Model

`TemporaryDirectory` is an `internal sealed` class implementing `IDisposable`. It holds a
single `string` property `DirectoryPath` set on construction.

#### Key Methods

##### TemporaryDirectory(string? baseDirectory = null) constructor

Creates a uniquely-named subdirectory under `baseDirectory` when provided; otherwise under
`Environment.CurrentDirectory`.

**Throws:**

- `InvalidOperationException` — wraps any `IOException`, `UnauthorizedAccessException`, or
  `ArgumentException` raised by `Directory.CreateDirectory`.

##### GetFilePath(string relativePath) → string

Returns the absolute path to a file within the temporary directory, creating intermediate
subdirectories as needed.

**Throws:**

- `ArgumentNullException` — when `relativePath` is null (from `PathHelpers.SafePathCombine`).
- `ArgumentException` — when `relativePath` escapes the temporary directory boundary.

##### Dispose()

Deletes the temporary directory and all contents. `IOException` and
`UnauthorizedAccessException` are suppressed.

#### Error Handling

- Constructor failures surface as `InvalidOperationException` with a descriptive message.
- `GetFilePath` propagates `ArgumentNullException` and `ArgumentException` from
  `PathHelpers.SafePathCombine` unchanged.
- `Dispose` silently suppresses I/O and access errors; cleanup failures are non-fatal.

#### Dependencies

| Dependency    | Role                                                                             |
| ------------- | -------------------------------------------------------------------------------- |
| `PathHelpers` | `SafePathCombine` enforces the boundary between the temp dir and relative paths. |

#### Callers

`Validation` (self-test subsystem) uses `TemporaryDirectory` to create isolated working
directories for each self-test scenario, obtaining file paths via `GetFilePath`.

Test classes across the test suite use `TemporaryDirectory` as a `using`-scoped fixture to
create and automatically clean up files needed by individual test cases.
