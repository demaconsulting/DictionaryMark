## Dictionary Subsystem Design

The Dictionary subsystem handles loading, validating, and formatting dictionary entries.

### YamlDictionaryLoader

Reads YAML files with flat string-to-string mappings. Uses YamlDotNet to parse
the YAML stream and converts each key-value pair to a `DictionaryEntry`. Rejects
nested structures and duplicate keys within the same file.

### ConflictDetector

Groups entries by term (case-insensitive) and identifies terms with different
definitions. Same term + same definition is treated as a deduplication (not a conflict).
Returns descriptive error messages for each conflict found.

### MarkdownFormatter

Formats entries as either a bullet list or a Markdown table. Always deduplicates
before formatting (first occurrence wins). Optionally sorts alphabetically.
Escapes pipe characters in table cells.

### DictionaryGenerator

Orchestrates the full pipeline: resolve files via GlobMatcher, load entries,
detect conflicts, format output, and write to file or stdout.
