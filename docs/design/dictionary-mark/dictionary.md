## Dictionary

![Dictionary Subsystem Structure](DictionaryView.svg)

### Overview

The Dictionary subsystem handles loading, validating, and formatting dictionary entries. It contains
four processing units: `DictionaryGenerator` (pipeline orchestrator), `YamlDictionaryLoader` (YAML
file reader), `ConflictDetector` (cross-file conflict validator), and `MarkdownFormatter` (output
renderer). It also defines six supporting value types: `DictionaryEntry` (immutable approved term
with definition, part of speech, variants, visibility and source), `DeprecatedEntry` (deprecated
term with replacement), `MarkdownOptions` (formatter options bag), `OutputFormat` (output format
enum), `InputKind` (terms or deprecated enum), and `SortOrder` (sort order enum). The boundary spans
everything from resolving input file paths through formatting the final Markdown string.

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
  `context.SortBy`, `context.SectionHeading`, `context.TermHeader`, `context.TermHeaderSpecified`,
  `context.DefinitionHeader`, `context.UseHeader`, `context.WhereHeader`, `context.Kind`,
  `context.HeadingDepth`, `context.WriteError`, `context.WriteLine`.
- *Constraints*: N/A

**GlobMatcher.Resolve**: Strict file pattern resolution from the Utilities subsystem.

- *Type*: In-process .NET public API
- *Role*: Consumer — `DictionaryGenerator` calls this to resolve patterns to file paths
- *Contract*: `Resolve(IEnumerable<string> patterns)` returns a sorted, deduplicated list of absolute
  file paths after expanding `${VAR}` references.
- *Constraints*: Throws `ArgumentException` on empty patterns and `InvalidOperationException` when a
  pattern matches nothing, a literal path does not exist, or a variable is undefined.

**YamlDotNet**: OTS YAML parsing library.

- *Type*: In-process .NET public API (NuGet package)
- *Role*: Consumer — used by `YamlDictionaryLoader` to parse YAML streams
- *Contract*: `YamlStream`, `YamlMappingNode`, `YamlScalarNode` APIs from the
  `YamlDotNet.RepresentationModel` namespace.
- *Constraints*: See *YamlDotNet Integration Design*.

### Design

`DictionaryGenerator.Generate` drives the pipeline in five steps:

1. Call `GlobMatcher.Resolve` for `context.InputPatterns`
   (strict: `${VAR}` expansion, every pattern must match, literal paths must exist); return early
   on `ArgumentException` or `InvalidOperationException`, or when no files are found.
2. Load each resolved file: input files as the selected kind (`context.Kind`, approved terms via
   `YamlDictionaryLoader.Load` or deprecated terms via `LoadDeprecated`); return early on `IOException` or `InvalidOperationException`.
3. Call `ConflictDetector.Detect(approved, deprecated)` to apply the shared conflict rule; report
   each conflict via `context.WriteError` and return early if any are found.
4. Construct a `MarkdownOptions` bag from `context` properties and call
   `MarkdownFormatter.Format` (approved terms) or `MarkdownFormatter.FormatDeprecated` (deprecated
   terms), using only entries from the primary input files, to produce the Markdown string.
5. Write the string to `context.OutputFile` (via `File.WriteAllText`) or to stdout (via
   `context.WriteLine`); on `IOException` or `UnauthorizedAccessException` during file write,
   call `context.WriteError` and return.

#### Rich entries and deprecated terms

An approved-term entry is a definition string or an object with `definition` (required, non-empty,
regardless of `visible`), `pos`, `variants` and `visible`. A deprecated-term entry is an object with
a required `use` and an optional `where`. `DeprecatedEntry` and `InputKind` model the deprecated
kind and the `--kind` selection.
