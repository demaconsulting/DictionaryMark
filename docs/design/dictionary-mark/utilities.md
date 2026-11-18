## Utilities

![Utilities Subsystem Structure](UtilitiesView.svg)

### Overview

The Utilities subsystem provides shared helper functionality consumed by other subsystems. It
contains four units: `GlobMatcher`, which resolves glob patterns to sorted, deduplicated
file-path lists; `PathResolver`, which resolves user-supplied paths against a base directory
with `${VAR}` expansion and `..` support; `PathHelpers`, which enforces a path-traversal security
boundary when combining a trusted base path with a caller-supplied relative path; and
`TemporaryDirectory`, a disposable helper for creating and cleaning up temporary working folders.
None of these units depends on any other DictionaryMark subsystem.

### Interfaces

**GlobMatcher.GetFiles**: Resolves glob patterns to file paths.

- *Type*: In-process .NET public API
- *Role*: Provider — `DictionaryGenerator` consumes this to resolve input patterns
- *Contract*: `GetFiles(IEnumerable<string> patterns)` returns a sorted, deduplicated
  `IReadOnlyList<string>` of absolute file paths. Throws `ArgumentNullException` when `patterns`
  is null; throws `ArgumentException` when any pattern is null or empty.
- *Constraints*: Results are sorted with `StringComparer.OrdinalIgnoreCase`.

**GlobMatcher.Resolve**: Strictly resolves input patterns against a base directory.

- *Type*: In-process .NET public API
- *Role*: Provider — `DictionaryGenerator` consumes this to resolve each job's input patterns
- *Contract*: `Resolve(IEnumerable<string> patterns, string baseDirectory)` (and
  `Resolve(patterns)`, which uses the working directory) returns the sorted, deduplicated absolute
  paths; every pattern must match. Throws `InvalidOperationException` naming the pattern or path
  when a pattern matches nothing or a variable is undefined.
- *Constraints*: `..` segments are supported anywhere before the first wildcard.

**PathResolver**: Resolves user-supplied paths against a base directory.

- *Type*: In-process .NET public API
- *Role*: Provider — `GlobMatcher`, `DictionaryGenerator`, `Context` and `ConfigFile` consume this
- *Contract*: `ExpandEnvironmentVariables(path, description)`, `Combine(path, baseDirectory,
  description)` (rooted, not normalized; for globs), `ResolvePath(path, baseDirectory,
  description)` (rooted and normalized; for literal paths), `ResolveDirectory(path, baseDirectory,
  description)` (for the configuration `root` key; text only, rejects wildcards, never probes) and
  `ResolveExistingDirectory(path, baseDirectory, description)` (for `--root`; rejects wildcards and
  missing directories).
  Throws `InvalidOperationException` with a message naming the description and path.
- *Constraints*: Only the path argument is expanded, never the base directory. Absolute paths
  ignore the base directory.

**PathHelpers.SafePathCombine**: Enforces path-traversal security boundary.

- *Type*: In-process .NET public API
- *Role*: Provider — `TemporaryDirectory` consumes this to enforce directory boundaries
- *Contract*: `SafePathCombine(string basePath, string relativePath)` returns the combined path,
  guaranteed to remain within `basePath`. Throws `ArgumentNullException` when either argument is
  null; throws `ArgumentException` when the resolved path escapes the base directory or an
  absolute path is supplied as `relativePath`.
- *Constraints*: No file-system I/O performed; string-level path normalization only.

**TemporaryDirectory**: Disposable temporary directory helper.

- *Type*: In-process .NET public API
- *Role*: Provider — `Validation` consumes this for isolated per-test working directories
- *Contract*: Constructor creates a uniquely-named temporary directory under `baseDirectory` (or
  `Environment.CurrentDirectory` when not provided). `GetFilePath(string relativePath)` returns a
  safe file path inside the directory and creates intermediate directories. `Dispose()` performs
  best-effort recursive cleanup.
- *Constraints*: Must be used in a `using` block; `IOException` and `UnauthorizedAccessException`
  from `Dispose` are suppressed.

**Microsoft.Extensions.FileSystemGlobbing**: OTS library consumed by `GlobMatcher`.

- *Type*: In-process .NET public API (NuGet package)
- *Role*: Consumer — `GlobMatcher` uses `Matcher` and `DirectoryInfoWrapper` for glob pattern
  evaluation
- *Contract*: `Matcher`, `DirectoryInfoWrapper`, `PatternMatchingResult.Files` APIs.
- *Constraints*: See *Microsoft.Extensions.FileSystemGlobbing Integration Design*.

### Design

`GlobMatcher.Resolve` uses `PathResolver.Combine` to expand and root every pattern before
matching, so every glob is matched as an absolute glob whose literal base directory may contain
`..` segments. The other units are independent helpers.
`GlobMatcher.GetFiles` handles three path categories in order: (1) absolute path without
wildcards — checks `File.Exists` directly; (2) absolute path with wildcards — extracts the
deepest wildcard-free directory segment as the base, constructs a `Matcher` with the remaining
relative pattern; (3) relative path — constructs a `Matcher` against `Environment.CurrentDirectory`.
Results accumulate in a case-insensitive `HashSet<string>` for deduplication, then sorted before
returning.

`PathHelpers.SafePathCombine` calls `Path.Combine`, resolves both paths to absolute form via
`Path.GetFullPath`, then calls `Path.GetRelativePath(base, combined)` and rejects the result if
it starts with `..` or is itself rooted.

`TemporaryDirectory` composes these: its constructor uses `PathHelpers.SafePathCombine` to build
the unique directory path, and `GetFilePath` delegates to `SafePathCombine` to enforce the
boundary.
