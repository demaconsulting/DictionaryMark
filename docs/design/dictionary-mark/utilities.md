## DictionaryMark Utilities Subsystem Design

The Utilities subsystem provides shared helper functionality.

### GlobMatcher

Resolves file paths from glob patterns using `Microsoft.Extensions.FileSystemGlobbing`.
For absolute paths without wildcards, directly checks file existence.
For all other patterns, uses `Matcher` with the current directory as the base.
Returns a sorted, deduplicated list of absolute file paths.
`GlobMatcher.GetFiles` throws `ArgumentException` when a null or empty pattern is supplied.

### PathHelpers

Provides safe path-combination utilities that enforce a security boundary. `SafePathCombine`
combines a base path with a caller-supplied relative path and rejects any result that escapes
the base directory, preventing path-traversal attacks. No file-system I/O is performed.
`PathHelpers.SafePathCombine` throws `ArgumentNullException` when either argument is `null`.
`PathHelpers.SafePathCombine` throws `ArgumentException` when the resolved path would escape
the base directory or when an absolute path is supplied as the relative argument.
