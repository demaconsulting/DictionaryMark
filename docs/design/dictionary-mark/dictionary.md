## Dictionary Subsystem Design

The Dictionary subsystem handles loading, validating, and formatting dictionary entries.
It is orchestrated by `DictionaryGenerator`, which chains the three specialist units in sequence.

### Overview

The Dictionary subsystem contains four units: `DictionaryGenerator` (pipeline orchestrator),
`YamlDictionaryLoader` (YAML file reader), `ConflictDetector` (cross-file conflict validator),
and `MarkdownFormatter` (output renderer). Its boundary spans everything from resolving input
file paths through formatting the final Markdown string. The subsystem also defines three
supporting value types — `DictionaryEntry`, `MarkdownOptions`, and the `OutputFormat` and
`SortOrder` enums — which are shared with the CLI subsystem.

### Interfaces

**Exposed to the rest of the system:**

- `DictionaryGenerator.Generate(Context context)` — the single public entry point; runs the
  full pipeline using settings from `context` and writes output.
- `OutputFormat` enum — shared with the CLI subsystem as the type of `context.Format`.
- `SortOrder` enum — shared with the CLI subsystem as the type of `context.SortBy`.

**Consumed from other items:**

- `Context` (CLI subsystem) — provides `InputPatterns`, `OutputFile`, `Format`, `SortBy`,
  `SectionHeading`, `TermHeader`, `DefinitionHeader`, `HeadingDepth`, `WriteError`, and
  `WriteLine`.
- `GlobMatcher.GetFiles` (Utilities subsystem) — resolves glob patterns to file paths.
- `YamlDotNet` (OTS) — used by `YamlDictionaryLoader` to parse YAML streams.

### Design

`DictionaryGenerator.Generate` drives the pipeline in five steps:

1. Call `GlobMatcher.GetFiles(context.InputPatterns)` to resolve all input file paths; return
   early on `ArgumentException` or when no files are found.
2. For each file, call `YamlDictionaryLoader.Load(file)` to produce a `DictionaryEntry` list;
   return early on `IOException` or `InvalidOperationException`.
3. Call `ConflictDetector.Detect(allEntries)` to find terms with conflicting definitions; report
   each conflict via `context.WriteError` and return early if any are found.
4. Construct a `MarkdownOptions` bag from `context` properties and call
   `MarkdownFormatter.Format(allEntries, options)` to produce the Markdown string.
5. Write the string to `context.OutputFile` (via `File.WriteAllText`) or to stdout (via
   `context.WriteLine`).

### Data Model

- `DictionaryEntry` — Immutable pair of `Term` (`string`) and `Definition` (`string`).
- `MarkdownOptions` — Options bag consumed by `MarkdownFormatter`:
  - `Format` (`OutputFormat`) — output format.
  - `SectionHeading` (`string?`) — optional heading to prepend.
  - `HeadingDepth` (`int`, 1–6) — heading level.
  - `TermHeader` (`string`) — term column header.
  - `DefinitionHeader` (`string`) — definition column header.
  - `SortOrder` (`SortOrder`) — sort order.
- `OutputFormat` — Enum: `Bullets` (default) or `Table`.
- `SortOrder` — Enum: `FileOrder` (default) or `Alphabetical`.

### YamlDictionaryLoader

Reads YAML files with flat string-to-string mappings. Uses YamlDotNet to parse
the YAML stream and converts each key-value pair to a `DictionaryEntry`. Rejects
nested structures and duplicate keys within the same file.

#### Load(string filePath) → IReadOnlyList\<DictionaryEntry\>

Loads all entries from the file at `filePath` in document order.

**Parameter**: `filePath` (`string`) — Absolute or relative path to a YAML file.

**Returns**: ordered list of `DictionaryEntry` objects.

**Throws**:

- `ArgumentNullException` — `filePath` is null.
- `IOException` — file cannot be read.
- `InvalidOperationException` — YAML structure is not a flat key-value mapping, or a key appears more than once.

### ConflictDetector

Groups entries by term (case-insensitive) and identifies terms with different
definitions. Same term + same definition is treated as a deduplication (not a conflict).
Returns descriptive error messages for each conflict found.

#### Detect(IEnumerable\<DictionaryEntry\> entries) → IReadOnlyList\<string\>

Scans `entries` for terms that have two or more distinct definitions.

**Parameter**: `entries` (`IEnumerable<DictionaryEntry>`) — All entries from all loaded YAML files.

**Returns**: list of human-readable conflict messages (empty when there are no conflicts).

### MarkdownFormatter

Formats entries as either a bullet list or a Markdown table. Always deduplicates
before formatting (first occurrence wins). Optionally sorts alphabetically.
Escapes pipe characters in table cells.

#### Format(IReadOnlyList\<DictionaryEntry\> entries, MarkdownOptions options) → string

Formats `entries` as a Markdown string according to `options`.

- `entries` (`IReadOnlyList<DictionaryEntry>`) — Raw entries (may contain duplicates).
- `options` (`MarkdownOptions`) — Format, sorting, heading, and header settings.

**Returns**: formatted Markdown string (bullet list or table, with optional section heading).

### DictionaryGenerator

Orchestrates the full pipeline: resolve files via `GlobMatcher`, load entries,
detect conflicts, format output, and write to file or stdout.

#### Generate(Context context)

Runs the complete dictionary generation pipeline using settings from `context`.

**Parameter**: `context` (`Context`) — Provides input patterns, output path, and format options.

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
