### DictionaryGenerator Design

The `DictionaryGenerator` class orchestrates the full dictionary generation pipeline,
coordinating file discovery, loading, conflict detection, formatting, and output.

#### Overview

`DictionaryGenerator` is the primary orchestrator for dictionary generation. When `Generate`
is called, it resolves input file paths using `GlobMatcher`, loads entries from each file via
`YamlDictionaryLoader`, detects conflicts with `ConflictDetector`, formats output with
`MarkdownFormatter`, and writes the result to the output file or stdout.

#### Data Model

`DictionaryGenerator` is a concrete class with no persistent instance state. All state is
local to each `Generate` call.

| Field/Property | Type | Description |
| -------------- | ---- | ----------- |
| *(none)*       | —    | No instance fields; all state is local to `Generate`. |

#### Methods

##### Generate(Context context)

Orchestrates the full generation pipeline.

**Processing steps:**

1. Resolve input file paths — call `GlobMatcher.GetFiles(context.InputPatterns)`.
2. Load entries — call `YamlDictionaryLoader.Load(filePath)` for each resolved file; collect all entries.
3. Detect conflicts — call `ConflictDetector.Detect(entries)`; if any conflicts found, call
   `context.WriteError` for each and return (no output generated).
4. Format output — call `MarkdownFormatter.Format(entries, options)` where `options` is
   constructed from `context` fields (`Format`, `SectionHeading`, `TermHeader`, `DefinitionHeader`,
   `SortBy`, `HeadingDepth`).
5. Write output — if `context.OutputFile` is set, write the formatted string to that file path;
   otherwise write to `context.WriteLine`.

**Throws:** `InvalidOperationException` — when no input files are found matching the supplied
patterns; writes error via `context.WriteError` and sets exit code 1.

#### Interactions

| Dependency              | Role                                                                   |
| ----------------------- | ---------------------------------------------------------------------- |
| `Context`               | Provides input patterns, output options, output channel, and exit code.|
| `GlobMatcher`           | Resolves input file patterns to concrete file paths.                   |
| `YamlDictionaryLoader`  | Loads flat YAML key-value pairs from each input file.                  |
| `ConflictDetector`      | Detects term conflicts across entries from different files.            |
| `MarkdownFormatter`     | Formats deduplicated entries as a Markdown string.                     |
