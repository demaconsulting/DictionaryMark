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
- *Contract*: `Generate(Context context)` runs every job in `context.Jobs`; each job writes its
  output to a file or stdout, or calls `context.WriteError` and stops on its first error; the
  remaining jobs still run.
- *Constraints*: A job that fails produces no output.

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
- *Role*: Consumer — `DictionaryGenerator` reads the jobs and output channels from `context`
- *Contract*: Uses `context.Jobs`, `context.UsesConfigTables`, `context.WriteError`,
  `context.WriteLine`.
- *Constraints*: N/A

**TableJob**: One generation job from the Cli subsystem.

- *Type*: In-process .NET API
- *Role*: Consumer — `DictionaryGenerator` runs each job
- *Contract*: Uses `InputPatterns`, `OutputFile`, `BaseDirectory`, `Label` and `Options`.
- *Constraints*: N/A

**PathResolver.ResolvePath**: Literal path resolution from the Utilities subsystem.

- *Type*: In-process .NET API
- *Role*: Consumer — `DictionaryGenerator` resolves each job's output path against its base
  directory
- *Contract*: Returns the normalized full path after `${VAR}` expansion; throws
  `InvalidOperationException` for an undefined variable.
- *Constraints*: N/A

**GlobMatcher.Resolve**: Strict file pattern resolution from the Utilities subsystem.

- *Type*: In-process .NET public API
- *Role*: Consumer — `DictionaryGenerator` calls this to resolve patterns to file paths
- *Contract*: `Resolve(IEnumerable<string> patterns, string baseDirectory)` returns a sorted,
  deduplicated list of absolute file paths after expanding `${VAR}` references and rooting relative
  patterns at the base directory.
- *Constraints*: Throws `ArgumentException` on empty patterns and `InvalidOperationException` when a
  pattern matches nothing, a literal path does not exist, or a variable is undefined.

**FileAccessPolicy**: File-access confinement from the Utilities subsystem.

- *Type*: In-process .NET API
- *Role*: Consumer — `DictionaryGenerator` judges every input pattern, reads every input and writes
  the output of a configuration job through `context.FileAccess`
- *Contract*: `CheckPattern`, `ReadAllText` and `WriteAllText` (see *FileAccessPolicy Design*); a
  denial throws `FileAccessDeniedException`.
- *Constraints*: Used only for configuration jobs (`TableJob.ArePathsJudged`); command-line paths are
  trusted.

**YamlDotNet**: OTS YAML parsing library.

- *Type*: In-process .NET public API (NuGet package)
- *Role*: Consumer — used by `YamlDictionaryLoader` to parse YAML streams
- *Contract*: `YamlStream`, `YamlMappingNode`, `YamlScalarNode` APIs from the
  `YamlDotNet.RepresentationModel` namespace.
- *Constraints*: See *YamlDotNet Integration Design*.

### Design

`DictionaryGenerator.Generate` runs every job in `context.Jobs` in order, continuing after a failed
job, and drives the pipeline of each job (`GenerateJob`) in five steps:

1. For a configuration job, judge each input pattern with `FileAccessPolicy.CheckPattern`. Call
   `GlobMatcher.Resolve` for the job's input patterns and base directory
   (strict: `${VAR}` expansion, every pattern must match, literal paths must exist); return early
   on a file-access denial, `ArgumentException` or `InvalidOperationException`, or when no files are
   found.
2. Load each resolved file: input files as the selected kind (`job.Options.Kind`, approved terms via
   `YamlDictionaryLoader.Load` or deprecated terms via `LoadDeprecated`). A configuration job reads
   each file through `FileAccessPolicy.ReadAllText`, which judges it by its real path, and parses the
   text with `LoadText` or `LoadDeprecatedText`. Return early on a file-access denial, `IOException` or
   `InvalidOperationException`; a denied file is never skipped.
3. Call `ConflictDetector.Detect(approved, deprecated)` to apply the shared conflict rule within the
   job; report each conflict and return early if any are found. Different jobs may use different or
   overlapping dictionaries.
4. Call `MarkdownFormatter.Format` (approved terms) or `MarkdownFormatter.FormatDeprecated`
   (deprecated terms) with `job.Options`, using only entries from the primary input files, to
   produce the Markdown string. When `job.Options.ColumnWidths` is set, table separators are sized
   in proportion to the widths.
5. Write the string to the job's output file — resolved against the job's base directory with
   `PathResolver.ResolvePath`, creating missing parent directories — or to stdout (via
   `context.WriteLine`); on `IOException` or `UnauthorizedAccessException` during file write,
   report the error and return. A configuration job writes through `FileAccessPolicy.WriteAllText`,
   which writes nothing, not even a folder, when the output is denied.

Every job error is written through `context.WriteError`, prefixed with the job label
(`table job {n} ('{output}')`) for configuration jobs. When the jobs come from configuration
tables and any failed, a final `Error: {failed} of {total} table jobs failed.` is reported.

#### Column widths

`MarkdownOptions.ColumnWidths` holds one positive weight per table column, or `null`. Without
widths the separator is the fixed `| :--- | :--- |` row, so default output is unchanged. With
widths, each column gets `max(3, round_half_up(weight * 100 / total))` dashes after the `:`
alignment marker (see *MarkdownFormatter Design*).

#### Rich entries and deprecated terms

An approved-term entry is a definition string or an object with `definition` (required, non-empty,
regardless of `visible`), `pos`, `variants` and `visible`. A deprecated-term entry is an object with
a required `use` and an optional `where`. `DeprecatedEntry` and `InputKind` model the deprecated
kind and the `--kind` selection.
