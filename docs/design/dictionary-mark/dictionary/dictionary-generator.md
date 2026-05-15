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

#### Error Handling

`Generate` applies an early-return-on-error strategy: each error condition calls
`context.WriteError` (which sets the exit code to 1) and returns immediately, with no output
generated. The five error cases are:

- **Invalid input pattern** — `ArgumentException` from `GlobMatcher.GetFiles`;
  calls `context.WriteError("Error: Invalid input pattern: {message}")` and returns.
- **No files found** — Empty list from `GlobMatcher.GetFiles`;
  calls `context.WriteError("Error: No input files found matching the specified patterns.")` and returns.
- **I/O error reading YAML** — `IOException` from `YamlDictionaryLoader.Load`;
  calls `context.WriteError("Error: Failed to read file '{file}': {message}")` and returns.
- **Invalid YAML structure** — `InvalidOperationException` from `YamlDictionaryLoader.Load`;
  calls `context.WriteError("Error: Invalid YAML in file '{file}': {message}")` and returns.
- **I/O or access error writing output** — `IOException` or `UnauthorizedAccessException` from `File.WriteAllText`;
  calls `context.WriteError("Error: Failed to write output file '{file}': {message}")` and returns.

#### Interactions

| Dependency              | Role                                                                   |
| ----------------------- | ---------------------------------------------------------------------- |
| `Context`               | Provides input patterns, output options, output channel, and exit code.|
| `GlobMatcher`           | Resolves input file patterns to concrete file paths.                   |
| `YamlDictionaryLoader`  | Loads flat YAML key-value pairs from each input file.                  |
| `ConflictDetector`      | Detects term conflicts across entries from different files.            |
| `MarkdownFormatter`     | Formats deduplicated entries as a Markdown string.                     |
