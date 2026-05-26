## Dictionary

### Overview

The Dictionary subsystem handles loading, validating, and formatting dictionary entries. It contains
four processing units: `DictionaryGenerator` (pipeline orchestrator), `YamlDictionaryLoader` (YAML
file reader), `ConflictDetector` (cross-file conflict validator), and `MarkdownFormatter` (output
renderer). It also defines four supporting value types: `DictionaryEntry` (immutable term-definition
pair), `MarkdownOptions` (formatter options bag), `OutputFormat` (output format enum), and
`SortOrder` (sort order enum). The boundary spans everything from resolving input file paths through
formatting the final Markdown string.

### Interfaces

**DictionaryGenerator.Generate**: The single public entry point for dictionary generation.

- *Type*: In-process .NET public API
- *Role*: Provider — other units invoke this to run the full pipeline
- *Contract*: `Generate(Context context)` runs the pipeline end-to-end using settings from
  `context`; writes output to file or stdout; calls `context.WriteError` and returns early on any
  error.
- *Constraints*: Returns without generating output if any error is detected.

**OutputFormat**: Enum defining the available output formats.

- *Type*: In-process .NET public API
- *Role*: Provider — shared with the Cli subsystem as the type of `context.Format`
- *Contract*: Values `Bullets` (default) and `Table`.
- *Constraints*: N/A

**SortOrder**: Enum defining the available sort orders.

- *Type*: In-process .NET public API
- *Role*: Provider — shared with the Cli subsystem as the type of `context.SortBy`
- *Contract*: Values `FileOrder` (default) and `Alphabetical`.
- *Constraints*: N/A

**Context**: The context object passed in from the Cli subsystem.

- *Type*: In-process .NET public API
- *Role*: Consumer — `DictionaryGenerator` reads input patterns, output options, and output channels
  from `context`
- *Contract*: Uses `context.InputPatterns`, `context.OutputFile`, `context.Format`,
  `context.SortBy`, `context.SectionHeading`, `context.TermHeader`, `context.DefinitionHeader`,
  `context.HeadingDepth`, `context.WriteError`, `context.WriteLine`.
- *Constraints*: N/A

**GlobMatcher.GetFiles**: File pattern resolution from the Utilities subsystem.

- *Type*: In-process .NET public API
- *Role*: Consumer — `DictionaryGenerator` calls this to resolve glob patterns to file paths
- *Contract*: `GetFiles(IEnumerable<string> patterns)` returns sorted, deduplicated list of absolute
  file paths.
- *Constraints*: Throws `ArgumentException` on invalid patterns.

**YamlDotNet**: OTS YAML parsing library.

- *Type*: In-process .NET public API (NuGet package)
- *Role*: Consumer — used by `YamlDictionaryLoader` to parse YAML streams
- *Contract*: `YamlStream`, `YamlMappingNode`, `YamlScalarNode` APIs from the
  `YamlDotNet.RepresentationModel` namespace.
- *Constraints*: See *YamlDotNet Integration Design*.

### Design

`DictionaryGenerator.Generate` drives the pipeline in five steps:

1. Call `GlobMatcher.GetFiles(context.InputPatterns)` to resolve all input file paths; return early
   on `ArgumentException` or when no files are found.
2. For each resolved file, call `YamlDictionaryLoader.Load(file)` to produce a `DictionaryEntry`
   list; return early on `IOException` or `InvalidOperationException`.
3. Call `ConflictDetector.Detect(allEntries)` to find terms with conflicting definitions; report
   each conflict via `context.WriteError` and return early if any are found.
4. Construct a `MarkdownOptions` bag from `context` properties and call
   `MarkdownFormatter.Format(allEntries, options)` to produce the Markdown string.
5. Write the string to `context.OutputFile` (via `File.WriteAllText`) or to stdout (via
   `context.WriteLine`); on `IOException` or `UnauthorizedAccessException` during file write,
   call `context.WriteError` and return.
