### GlobMatcher Design

The `GlobMatcher` class resolves file paths from glob patterns using
`Microsoft.Extensions.FileSystemGlobbing`. It returns a sorted, deduplicated list of
absolute file paths matching the supplied patterns.

#### Purpose

`GlobMatcher.GetFiles` processes each pattern in turn:

1. Rejects null or empty patterns with `ArgumentException`.
2. Absolute paths **without** wildcards - checks `File.Exists` directly.
3. Absolute paths **with** wildcards - locates the non-wildcard base directory (the segment
   before the first `*` or `?`), then uses `Matcher` with that base.
4. Relative patterns - uses `Matcher` with `Environment.CurrentDirectory` as the base.

Results are accumulated in a `HashSet<string>` keyed with `StringComparer.OrdinalIgnoreCase`
for case-insensitive deduplication, then returned as a sorted `IReadOnlyList<string>`. On
case-sensitive file systems (such as Linux), paths differing only in case refer to distinct
files but will be merged into a single entry by the deduplicator.

#### Data Model

`GlobMatcher` is a `static` class with no instance state.

#### Key Methods

##### GetFiles(IEnumerable\<string\> patterns) → IReadOnlyList\<string\>

Returns a sorted, deduplicated list of absolute file paths matching the supplied patterns.

**Throws:**

- `ArgumentNullException` - when `patterns` is null.
- `ArgumentException` - when any pattern is null or empty.

#### Error Handling

`GlobMatcher` throws exceptions for invalid input; non-matching patterns produce no error:

- `ArgumentNullException` — thrown when `patterns` is null; the caller must supply a non-null
  enumerable.
- `ArgumentException` — thrown when any individual pattern is null or empty; the caller must
  validate pattern values before passing them.
- **No matches** — an empty result list is returned silently; the caller (`DictionaryGenerator`)
  is responsible for checking the result and reporting the "no files found" condition.

#### Interactions

| Dependency                                | Role                                                    |
| ----------------------------------------- | ------------------------------------------------------- |
| `Microsoft.Extensions.FileSystemGlobbing` | `Matcher` and `DirectoryInfoWrapper` for glob matching. |

#### Dependencies

| Dependency                                      | Role                                                                                              |
| ----------------------------------------------- | ------------------------------------------------------------------------------------------------- |
| `Microsoft.Extensions.FileSystemGlobbing` (OTS) | `Matcher` and `DirectoryInfoWrapper` APIs used to evaluate glob patterns against the file system. |

#### Callers

`DictionaryGenerator.Generate` calls `GlobMatcher.GetFiles(context.InputPatterns)` as the
first step of the pipeline to resolve user-supplied glob patterns to a sorted, deduplicated
list of concrete file paths.
