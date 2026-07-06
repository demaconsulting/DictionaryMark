### DictionaryGenerator

![Dictionary Subsystem Structure](DictionaryView.svg)

#### Purpose

`DictionaryGenerator` is a static class that orchestrates the full dictionary generation pipeline.
The `Generate` static method resolves input file paths using `GlobMatcher`, loads entries from each
file via `YamlDictionaryLoader`, detects conflicts with `ConflictDetector`, formats output with
`MarkdownFormatter`, and writes the result to the output file or stdout.

#### Data Model

N/A - static class with no instance state.

#### Key Methods

**Generate**: Orchestrates the full generation pipeline.

- *Parameters*: `Context context` — provides input patterns, output path, format options, and output
  channels.
- *Returns*: `void`.
- *Preconditions*: `context` is non-null.
- *Postconditions*: Output written to file or stdout, or `context.WriteError` called and method
  returned early on any error.

Processing steps: (1) Resolve input file paths — call `GlobMatcher.GetFiles(context.InputPatterns)`;
return early on `ArgumentException` or empty result. (2) Load entries — call
`YamlDictionaryLoader.Load(filePath)` for each resolved file; accumulate all entries; return early
on `IOException` or `InvalidOperationException`. (3) Detect conflicts — call
`ConflictDetector.Detect(allEntries)`; if any conflicts found, call `context.WriteError` for each
and return. (4) Format output — construct `MarkdownOptions` from context fields (`Format`,
`SectionHeading`, `TermHeader`, `DefinitionHeader`, `SortBy`, `HeadingDepth`) and call
`MarkdownFormatter.Format(allEntries, options)`. (5) Write output — if `context.OutputFile` is set,
write via `File.WriteAllText`; otherwise write via `context.WriteLine`.

#### Error Handling

`Generate` applies an early-return-on-error strategy: each error condition calls
`context.WriteError` (setting exit code to 1) and returns immediately.

Five error cases:

- **Invalid input pattern** — `ArgumentException` from `GlobMatcher.GetFiles`; reports
  `"Error: Invalid input pattern: {message}"`.
- **No files found** — empty list from `GlobMatcher.GetFiles`; reports
  `"Error: No input files found matching the specified patterns."`.
- **I/O error reading YAML** — `IOException` from `YamlDictionaryLoader.Load`; reports
  `"Error: Failed to read file '{file}': {message}"`.
- **Invalid YAML structure** — `InvalidOperationException` from `YamlDictionaryLoader.Load`;
  reports `"Error: Invalid YAML in file '{file}': {message}"`.
- **I/O or access error writing output** — `IOException` or `UnauthorizedAccessException` from
  `File.WriteAllText`; reports `"Error: Failed to write output file '{file}': {message}"`.

#### Dependencies

- **Context** — CLI subsystem; provides input patterns, output options, output channel, and exit
  code.
- **GlobMatcher** — Utilities subsystem; resolves glob patterns to concrete file paths.
- **YamlDictionaryLoader** — Dictionary subsystem unit; loads flat YAML key-value pairs from each
  file.
- **ConflictDetector** — Dictionary subsystem unit; detects term conflicts across loaded entries.
- **MarkdownFormatter** — Dictionary subsystem unit; formats deduplicated entries as a Markdown
  string.

#### Callers

- **Program.RunToolLogic** — calls `DictionaryGenerator.Generate(context)` when
  `context.InputPatterns` is non-empty. `Generate` is the sole public entry point of the Dictionary
  subsystem pipeline.
