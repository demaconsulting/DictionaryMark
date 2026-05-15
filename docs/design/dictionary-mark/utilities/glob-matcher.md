### GlobMatcher Design

The `GlobMatcher` class resolves file paths from glob patterns using
`Microsoft.Extensions.FileSystemGlobbing`. It returns a sorted, deduplicated list of
absolute file paths matching the supplied patterns.

#### Overview

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

#### Methods

##### GetFiles(IEnumerable\<string\> patterns) → IReadOnlyList\<string\>

Returns a sorted, deduplicated list of absolute file paths matching the supplied patterns.

**Throws:**

- `ArgumentNullException` - when `patterns` is null.
- `ArgumentException` - when any pattern is null or empty.

#### Interactions

| Dependency                                | Role                                                    |
| ----------------------------------------- | ------------------------------------------------------- |
| `Microsoft.Extensions.FileSystemGlobbing` | `Matcher` and `DirectoryInfoWrapper` for glob matching. |
