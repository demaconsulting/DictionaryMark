# DictionaryMark Utilities Subsystem Design

The Utilities subsystem provides shared helper functionality.

## GlobMatcher

Resolves file paths from glob patterns using `Microsoft.Extensions.FileSystemGlobbing`.
For absolute paths without wildcards, directly checks file existence.
For all other patterns, uses `Matcher` with the current directory as the base.
Returns a sorted, deduplicated list of absolute file paths.
