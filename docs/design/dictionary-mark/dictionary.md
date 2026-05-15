## Dictionary Subsystem Design

The Dictionary subsystem handles loading, validating, and formatting dictionary entries.
It is orchestrated by `DictionaryGenerator`, which chains the three specialist units in sequence.

### Data Model

| Type              | Description                                                                                     |
| ----------------- | ----------------------------------------------------------------------------------------------- |
| `DictionaryEntry` | Immutable pair of `Term` (`string`) and `Definition` (`string`).                                |
| `MarkdownOptions` | Options bag consumed by `MarkdownFormatter`. Fields: `Format` (`OutputFormat`), `SectionHeading` (`string?`), `HeadingDepth` (`int`, 1–6), `TermHeader` (`string`), `DefinitionHeader` (`string`), `SortOrder` (`SortOrder`). |
| `OutputFormat`    | Enum: `Bullets` (default) or `Table`.                                                           |
| `SortOrder`       | Enum: `FileOrder` (default) or `Alphabetical`.                                                  |

### YamlDictionaryLoader

Reads YAML files with flat string-to-string mappings. Uses YamlDotNet to parse
the YAML stream and converts each key-value pair to a `DictionaryEntry`. Rejects
nested structures and duplicate keys within the same file.

#### API

##### Load(string filePath) → IReadOnlyList\<DictionaryEntry\>

Loads all entries from the file at `filePath` in document order.

| Parameter  | Type     | Description                        |
| ---------- | -------- | ---------------------------------- |
| `filePath` | `string` | Absolute or relative path to a YAML file. |

**Returns**: ordered list of `DictionaryEntry` objects.

**Throws**:

- `IOException` — file cannot be read.
- `InvalidOperationException` — YAML structure is not a flat key-value mapping, or a key appears more than once.

### ConflictDetector

Groups entries by term (case-insensitive) and identifies terms with different
definitions. Same term + same definition is treated as a deduplication (not a conflict).
Returns descriptive error messages for each conflict found.

#### API

##### Detect(IEnumerable\<DictionaryEntry\> entries) → IReadOnlyList\<string\>

Scans `entries` for terms that have two or more distinct definitions.

| Parameter | Type                              | Description                              |
| --------- | --------------------------------- | ---------------------------------------- |
| `entries` | `IEnumerable<DictionaryEntry>`    | All entries from all loaded YAML files.  |

**Returns**: list of human-readable conflict messages (empty when there are no conflicts).

### MarkdownFormatter

Formats entries as either a bullet list or a Markdown table. Always deduplicates
before formatting (first occurrence wins). Optionally sorts alphabetically.
Escapes pipe characters in table cells.

#### API

##### Format(IReadOnlyList\<DictionaryEntry\> entries, MarkdownOptions options) → string

Formats `entries` as a Markdown string according to `options`.

| Parameter | Type                               | Description                              |
| --------- | ---------------------------------- | ---------------------------------------- |
| `entries` | `IReadOnlyList<DictionaryEntry>`   | Raw entries (may contain duplicates).    |
| `options` | `MarkdownOptions`                  | Format, sorting, heading, and header settings. |

**Returns**: formatted Markdown string (bullet list or table, with optional section heading).

### DictionaryGenerator

Orchestrates the full pipeline: resolve files via `GlobMatcher`, load entries,
detect conflicts, format output, and write to file or stdout.

#### API

##### Generate(Context context)

Runs the complete dictionary generation pipeline using settings from `context`.

| Parameter | Type      | Description                                       |
| --------- | --------- | ------------------------------------------------- |
| `context` | `Context` | Provides input patterns, output path, and format options. |

**Returns**: void. On error, calls `context.WriteError` and returns early.

### Interactions Between Units

```text
DictionaryGenerator.Generate(context)
  ├── GlobMatcher.GetFiles(context.InputPatterns)      → file paths
  ├── YamlDictionaryLoader.Load(file) [per file]       → DictionaryEntry[]
  ├── ConflictDetector.Detect(allEntries)              → conflict messages
  ├── MarkdownFormatter.Format(allEntries, options)    → Markdown string
  └── File.WriteAllText / context.WriteLine            → output
```
