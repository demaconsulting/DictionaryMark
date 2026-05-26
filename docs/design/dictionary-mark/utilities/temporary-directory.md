### TemporaryDirectory

#### Purpose

`TemporaryDirectory` is an `internal sealed` class implementing `IDisposable` that encapsulates
three responsibilities: (1) creating a uniquely-named temporary subdirectory on construction
using `PathHelpers.SafePathCombine` and `Directory.CreateDirectory`; (2) resolving
caller-supplied relative file paths within the directory safely via `GetFilePath`; (3) deleting
the directory tree on disposal via `Directory.Delete(path, recursive: true)`, suppressing
`IOException` and `UnauthorizedAccessException` so cleanup failures do not mask test outcomes.

The base directory defaults to `Environment.CurrentDirectory` (not `Path.GetTempPath()`) to
avoid the macOS `/tmp` → `/private/tmp` symlink issue that causes path-comparison failures when
the OS returns the resolved path instead of the construction path.

#### Data Model

**DirectoryPath**: `string` — The absolute path of the temporary directory created on
construction. Read-only; set by the constructor. Invariant: the directory at this path exists for
the lifetime of the object (until `Dispose` is called).

#### Key Methods

**TemporaryDirectory** *(constructor)*: Creates a uniquely-named subdirectory under
`baseDirectory` when provided; otherwise under `Environment.CurrentDirectory`.

- *Parameters*: `string? baseDirectory = null` — optional base directory; defaults to
  `Environment.CurrentDirectory`.
- *Returns*: N/A — constructor.
- *Preconditions*: `baseDirectory`, if provided, must not be an empty or whitespace-only string.
- *Postconditions*: A uniquely-named directory (named `tmp-{guid}`) exists on disk at
  `DirectoryPath`.

Throws `ArgumentException` — when `baseDirectory` is a non-null empty or whitespace-only string.

Throws `InvalidOperationException` — wraps any `IOException`, `UnauthorizedAccessException`, or
`ArgumentException` raised by `Directory.CreateDirectory`.

**GetFilePath**: Returns the absolute path to a file within the temporary directory, creating
intermediate subdirectories as needed.

- *Parameters*: `string relativePath` — the relative path within the temporary directory.
- *Returns*: `string` — the absolute path to the file location; intermediate directories are
  created.
- *Preconditions*: `relativePath` is non-null and does not escape the temporary directory
  boundary.
- *Postconditions*: All intermediate directories in the returned path exist on disk.

Delegates to `PathHelpers.SafePathCombine(DirectoryPath, relativePath)` for boundary
enforcement, then calls `Directory.CreateDirectory` on the parent of the returned path.

**Dispose**: Deletes the temporary directory and all contents.

- *Parameters*: N/A — parameterless.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: Best-effort — the temporary directory is deleted if accessible. `IOException`
  and `UnauthorizedAccessException` are suppressed.

#### Error Handling

- Constructor throws `ArgumentException` directly when `baseDirectory` is a non-null
  empty or whitespace-only string.
- Constructor failures caused by file-system errors surface as `InvalidOperationException`
  with a descriptive message, wrapping the underlying `IOException`, `UnauthorizedAccessException`,
  or `ArgumentException` from `Directory.CreateDirectory`.
- `GetFilePath` propagates `ArgumentNullException` (when `relativePath` is null) and
  `ArgumentException` (when `relativePath` escapes the boundary) from
  `PathHelpers.SafePathCombine` unchanged.
- `Dispose` silently suppresses `IOException` and `UnauthorizedAccessException`; cleanup failures
  are non-fatal and do not propagate.

#### Dependencies

- **PathHelpers** — Utilities subsystem unit; `SafePathCombine` enforces the boundary between
  the temp directory and caller-supplied relative paths.

#### Callers

- **Validation** — creates `TemporaryDirectory` instances in `using` blocks for each self-test
  scenario, obtaining file paths via `GetFilePath`.
- Test classes in `test/DemaConsulting.DictionaryMark.Tests/` — use `TemporaryDirectory` as a
  `using`-scoped fixture to create and automatically clean up files needed by individual test
  cases.
