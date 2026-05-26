## FileSystemGlobbing

Microsoft.Extensions.FileSystemGlobbing is a .NET library that provides glob-pattern file
discovery. DictionaryMark uses it exclusively in `GlobMatcher` to resolve user-supplied input
patterns to concrete file paths.

### Purpose

Microsoft.Extensions.FileSystemGlobbing is used exclusively in `GlobMatcher` to match relative
and absolute glob patterns against the file system. It handles standard glob syntax (`*`, `**`,
`?`) and provides cross-platform-consistent matching behavior backed by the real file system.

### Features Used

DictionaryMark uses the following APIs from the `Microsoft.Extensions.FileSystemGlobbing` and
`Microsoft.Extensions.FileSystemGlobbing.Abstractions` namespaces:

- **`Matcher`** — configured with one or more include patterns and executed against a base directory
- **`Matcher.AddInclude(string)`** — registers a relative glob pattern for inclusion
- **`Matcher.Execute(DirectoryInfoBase)`** — executes matching against the supplied directory and
  returns results
- **`PatternMatchingResult.Files`** — enumerates matched file entries, each providing a relative path
- **`FilePatternMatch.Path`** — the relative path of a matched file, combined with the base
  directory to form an absolute path
- **`DirectoryInfoWrapper`** — wraps a `System.IO.DirectoryInfo` instance as the base for matching

Features not used: exclude patterns, `Matcher.Match` static helpers, custom `IFileSystem`
implementations, and file attribute filtering.

### Integration Pattern

For relative patterns, `GlobMatcher` creates a `Matcher` rooted at `Environment.CurrentDirectory`:

```csharp
var matcher = new Matcher();
matcher.AddInclude(pattern);
var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(Environment.CurrentDirectory)));
```

For absolute patterns containing wildcards, `GlobMatcher` first extracts the non-wildcard base
directory from the pattern, then creates a `Matcher` rooted at that directory with the remaining
pattern segment as the include rule. In both cases, matched relative paths are combined with the
base directory and resolved to absolute paths via `Path.GetFullPath` before being added to the
results set.

### Error Handling

`GlobMatcher.GetFiles` throws `ArgumentNullException` when `patterns` is null and
`ArgumentException` when any individual pattern is null or empty; both are checked before
invoking any FileSystemGlobbing APIs. File-system errors encountered during glob execution
propagate as-is from `Matcher.Execute`. Non-matching patterns return an empty
`PatternMatchingResult.Files` collection; `GlobMatcher` treats this as a normal outcome and
returns an empty list to its caller.

### Design Constraints

- Relative patterns are always resolved relative to `Environment.CurrentDirectory` at the time
  of the call; no alternative base directory can be configured.
- The deduplication set uses `StringComparer.OrdinalIgnoreCase`, so on case-sensitive file
  systems two paths that differ only in case are treated as the same key and merged into a
  single entry.
- Only `AddInclude` is used; exclude patterns are not supported in the current design.
