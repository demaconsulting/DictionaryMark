### TableJob

![Cli Subsystem Structure](CliView.svg)

#### Purpose

`TableJob` is one fully resolved unit of generation work: which inputs to read, where to write,
which directory relative paths resolve against, and how to format the output. A run without a
configuration file has at most one command-line job; a configuration file with `tables` produces
one job per table. `TableJob` is the single place where the formatting precedence (command line,
then job, then configuration defaults, then built-in defaults) is applied and where the
column-width count is checked against the effective table shape. Instances are immutable.

#### Data Model

**Index**: `int?` — 1-based position of the job in the configuration file; `null` for the
command-line job. A non-null index identifies a configuration-table job, which is always judged.

**InputPatterns**: `IReadOnlyList<string>` — Input patterns exactly as written; at least one.

**OutputFile**: `string?` — Output path as written; `null` to write to stdout.

**BaseDirectory**: `string` — Directory that relative input patterns and the relative output
resolve against: a root directory, the configuration file's directory, or the working directory.

**BaseDirectoryText**: `string` — The base directory as text before normalization (`${VAR}`
expanded and rooted, `.`, `..` and repeated separators kept); equal to `BaseDirectory` unless the
base is the configuration file's `root` key. The file-access resolver's text rules are given the
job's paths rooted at this text, because normalization can hide a network path (on Linux and macOS
a leading `//` collapses to `/`).

**RelativePathsJudged**: `bool` — `true` when the base directory is the configuration file's `root`
key (no `--root` was given), so a relative path the job names is moved by that untrusted key; set
only for the command-line job.

**HasJudgedPaths**: `bool` — `Index != null || RelativePathsJudged`: the file-access policy judges at
least one path of the job, so the generator creates a resolver for it.

**Label**: `string?` — `table job {index} ('{output}')` for configuration jobs; `null` for the
command-line job, so its error messages stay unchanged.

**Options**: `MarkdownOptions` — Effective formatting options of the job.

#### Key Methods

**Create**: Creates a job by merging formatting layers and validating the result.

- *Parameters*: `int? index`; `IReadOnlyList<string> inputPatterns`; `string? outputFile`;
  `string baseDirectory`; `IReadOnlyList<FormattingSettings?> layers` — highest precedence first,
  `null` elements skipped; `string? baseDirectoryText` — optional, the base directory as text before
  normalization (`null` means `baseDirectory`); `bool baseIsConfigRoot` — optional, `true` when the
  base directory is the configuration file's `root` key (sets `RelativePathsJudged`).
- *Returns*: `TableJob`.
- *Preconditions*: `inputPatterns`, `baseDirectory` and `layers` are not null; `inputPatterns` is
  not empty.
- *Postconditions*: `Options` holds a value for every setting; `Options.ColumnWidths` is either
  `null` or has exactly the number of table columns.

Algorithm:

1. Merge the layers from lowest to highest with `FormattingSettings.Over`, so the first non-null
   value in precedence order wins.
2. Fill unspecified members with the built-in defaults: `Bullets`, no section heading, depth 1,
   `FileOrder`, `Terms`, term header `Term` (or `Not used` when the effective kind is
   `Deprecated` and no layer gave a term header), `Definition`, `Use instead`,
   `Where it was used`, and no column widths.
3. When the effective format is `Table` and widths were given, require exactly 2 widths for
   `Terms` and exactly 3 for `Deprecated`. When the effective format is `Bullets`, the widths are
   dropped (set to `null`) without checking their count.
4. Build the `MarkdownOptions` and the label.

**IsJudged**: Tells whether the file-access policy judges a path the job names.

- *Parameters*: `string expandedPath` — an input pattern or output path after `${VAR}` expansion and
  before it is rooted at the base directory.
- *Returns*: `true` for every path of a configuration-table job (`Index != null`); for a job with
  `RelativePathsJudged`, `true` only when the path is not rooted (`!Path.IsPathRooted`), because a
  rooted path ignores the base directory (`Path.Combine` keeps it as is) and so is not moved by the
  `root` key; `false` otherwise.
- *Preconditions*: `expandedPath` is not null.

#### Error Handling

- `ArgumentNullException` — a required argument is null (also `IsJudged`).
- `ArgumentException` — `inputPatterns` is empty, or the column-width count does not match:
  `{label}: Column widths for terms tables require exactly 2 values, but 3 were given.` (the label
  prefix is omitted for the command-line job). `Context.Create` adds the configuration file prefix
  for configuration jobs; `Program.Main` reports it as `Error: {message}` with exit code 1.

#### Dependencies

- **FormattingSettings** — supporting Cli type; the precedence layers.
- **MarkdownOptions**, **OutputFormat**, **SortOrder**, **InputKind** — Dictionary subsystem types
  that describe the effective formatting.

#### Callers

- **Context.Create** — creates every job.
- **DictionaryGenerator.GenerateJob** — reads `InputPatterns`, `OutputFile`, `BaseDirectory`,
  `BaseDirectoryText`, `Label` and `Options` to run the job, and `HasJudgedPaths` and `IsJudged` to
  decide which paths go through the file-access resolver.
