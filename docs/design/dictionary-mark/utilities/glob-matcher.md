### GlobMatcher

![Utilities Subsystem Structure](UtilitiesView.svg)

#### Purpose

`GlobMatcher.GetFiles` resolves a list of glob patterns to a sorted, deduplicated list of
absolute file paths. It handles three path categories: absolute paths without wildcards (checked
via `File.Exists`), absolute paths with wildcards (base directory extracted before the first
wildcard character), and relative patterns (matched against `Environment.CurrentDirectory`).
`GlobMatcher` is a static class.

#### Data Model

N/A - static class with no instance state.

#### Key Methods

**GetFiles**: Returns a sorted, deduplicated list of absolute file paths matching the supplied
patterns.

- *Parameters*: `IEnumerable<string> patterns` — collection of file path patterns (absolute or
  relative, with or without wildcards).
- *Returns*: `IReadOnlyList<string>` — sorted, deduplicated list of absolute file paths; empty
  list when no patterns match.
- *Preconditions*: `patterns` is non-null; each individual pattern is non-null and non-empty.
- *Postconditions*: Returned list is sorted with `StringComparer.OrdinalIgnoreCase`; each path
  is absolute; no duplicates (case-insensitive).

For each pattern: (1) reject null or empty with `ArgumentException`; (2) absolute path without
wildcards — check `File.Exists`, add if present; (3) absolute path with wildcards — locate the
non-wildcard base (segment before first `*` or `?`), create `Matcher` with remaining relative
pattern, execute against that base; (4) relative pattern — create `Matcher`, execute against
`Environment.CurrentDirectory`. Results accumulated in a `HashSet<string>` (case-insensitive)
then sorted.

#### Error Handling

- `ArgumentNullException` — thrown when `patterns` is null.
- `ArgumentException` — thrown when any individual pattern is null or empty; caller must validate
  pattern values before passing them.
- Non-matching patterns return no error — empty result is returned silently; `DictionaryGenerator`
  is responsible for detecting and reporting the "no files found" condition.
- File-system errors from `Matcher.Execute` propagate as-is; `DictionaryGenerator` only catches
  `ArgumentException` around `GlobMatcher.GetFiles`, so non-argument exceptions reach
  `Program.Main`.

#### Dependencies

- **Microsoft.Extensions.FileSystemGlobbing** — OTS package; `Matcher` and `DirectoryInfoWrapper`
  APIs used to evaluate glob patterns against the file system.

#### Callers

- **DictionaryGenerator.Generate** — calls `GlobMatcher.GetFiles(context.InputPatterns)` as the
  first step of the pipeline to resolve user-supplied glob patterns to a sorted, deduplicated list
  of concrete file paths.
