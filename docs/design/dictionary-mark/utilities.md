## DictionaryMark Utilities Subsystem Design

The Utilities subsystem provides shared helper functionality.

### Overview

The Utilities subsystem contains three helper units: `GlobMatcher`, which resolves glob patterns
to sorted, deduplicated file-path lists; `PathHelpers`, which enforces a path-traversal security
boundary when combining a trusted base path with a caller-supplied relative path; and
`TemporaryDirectory`, a disposable helper for creating and cleaning up temporary working folders.
None of these units depends on any other DictionaryMark subsystem, and they are consumed by other
subsystems.

### Interfaces

**Exposed to the rest of the system:**

- `GlobMatcher.GetFiles(IEnumerable<string> patterns)` — returns a sorted, deduplicated
  `IReadOnlyList<string>` of absolute file paths matching the supplied glob patterns.
  Throws `ArgumentNullException` when `patterns` is null; throws `ArgumentException` when
  any pattern is null or empty.
- `PathHelpers.SafePathCombine(string basePath, string relativePath)` — returns the result of
  `Path.Combine(basePath, relativePath)`, guaranteed to remain within `basePath`.
  Throws `ArgumentNullException` when either argument is null; throws `ArgumentException`
  when the resolved path escapes the base directory or an absolute path is supplied as
  `relativePath`.
- `TemporaryDirectory.TemporaryDirectory(string? baseDirectory = null)` — creates a uniquely
  named temporary directory under `baseDirectory` (or `Environment.CurrentDirectory` when not
  provided). Throws `ArgumentException` for invalid base-directory input and
  `InvalidOperationException` when directory creation fails.
- `TemporaryDirectory.GetFilePath(string relativePath)` — returns a safe file path inside the
  temporary directory and creates any required intermediate directories.
- `TemporaryDirectory.Dispose()` — best-effort recursive cleanup of the temporary directory.

**Consumed from other items:**

- `Microsoft.Extensions.FileSystemGlobbing` (OTS) — `Matcher` and `DirectoryInfoWrapper` are
  used by `GlobMatcher` to evaluate glob patterns against the file system.

### Design

`GlobMatcher.GetFiles` handles three path categories in order:

1. **Absolute path, no wildcards** — checks `File.Exists`; adds if present.
2. **Absolute path with wildcards** — extracts the deepest wildcard-free directory segment as
   the base, constructs a `Matcher` with the remaining relative pattern, and executes it via
   `DirectoryInfoWrapper`.
3. **Relative path** — constructs a `Matcher` and executes it against
   `Environment.CurrentDirectory`.

Results are accumulated in a case-insensitive `HashSet<string>` to deduplicate, then sorted
with `StringComparer.OrdinalIgnoreCase` before returning.

`PathHelpers.SafePathCombine` calls `Path.Combine`, resolves both the base and the combined
result to absolute form via `Path.GetFullPath`, then calls `Path.GetRelativePath(base,
combined)` and rejects the result if it starts with `..` or is itself a rooted path.

### GlobMatcher

Resolves file paths from glob patterns using `Microsoft.Extensions.FileSystemGlobbing`.
For absolute paths without wildcards, directly checks file existence.
For absolute paths with wildcards, extracts the deepest wildcard-free directory segment as
the glob base and uses `Matcher` with that base.
For relative patterns, uses `Matcher` with the current directory as the base.
Returns a sorted, deduplicated list of absolute file paths.
`GlobMatcher.GetFiles` throws `ArgumentException` when a null or empty pattern is supplied.

### PathHelpers

Provides safe path-combination utilities that enforce a security boundary. `SafePathCombine`
combines a base path with a caller-supplied relative path and rejects any result that escapes
the base directory, preventing path-traversal attacks. No file-system I/O is performed.
`PathHelpers.SafePathCombine` throws `ArgumentNullException` when either argument is `null`.
`PathHelpers.SafePathCombine` throws `ArgumentException` when the resolved path would escape
the base directory or when an absolute path is supplied as the relative argument.
