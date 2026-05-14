# DictionaryMark Utilities Subsystem Verification

The GlobMatcher utility is verified in `GlobMatcherTests.cs`.

Tests cover absolute path resolution, non-existent paths, empty patterns,
invalid patterns, and deduplication of results.

The PathHelpers utility is verified in `PathHelpersTests.cs`.

Tests cover valid path combinations, path-traversal rejection (double dots, absolute paths,
mid-path traversal), null-input rejection, and edge cases such as empty relative paths and
directory names prefixed with double dots.
