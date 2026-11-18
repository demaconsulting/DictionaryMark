## Cli

![Cli Subsystem Structure](CliView.svg)

### Overview

The Cli subsystem handles command-line argument parsing, configuration files, generation jobs and
program output for DictionaryMark. It contains three units and one supporting data type:

- `Context` encapsulates all argument-parsing logic, selects the root (base) directory, builds the
  file-access policy from `--file-access` and `--allow-root`, builds the generation jobs, and owns
  both output channels (console and log file).
- `ConfigFile` loads and validates the YAML configuration file named by `--config`: formatting
  defaults, the optional `root` key (checked against the file-access never-accessed list), and
  optional table jobs.
- `TableJob` is one resolved generation job: input patterns, output path, base directory, whether
  its paths are judged by the file-access policy, and the effective Markdown options after the
  formatting precedence is applied.
- `FormattingSettings` is a supporting value type: one precedence layer of formatting settings in
  which every member is optional.

Boundaries: parsing `string[]` args into typed properties, reading the configuration file,
turning the command line and the file into a list of jobs, opening an optional log file, routing
normal output through `WriteLine`, routing errors through `WriteError`, and exposing the derived
`ExitCode`. All other subsystems communicate with the rest of the system through a `Context`
instance and the `TableJob` instances it exposes.

### Interfaces

**Context**: The command-line context object that all other subsystems consume.

- *Type*: In-process .NET public API
- *Role*: Provider — other subsystems consume this unit
- *Contract*: `Context.Create(string[] args)` factory method; parsed boolean flags `Version`,
  `Help`, `Silent`, `Validate`; option properties `InputPatterns`, `OutputFile`, `Format`,
  `SortBy`, `SectionHeading`, `TermHeader`, `DefinitionHeader`, `HeadingDepth`, `ResultsFile`,
  `Kind`, `UseHeader`, `WhereHeader`, `TermHeaderSpecified`, `ColumnWidths`, `ConfigFilePath`,
  `RootDirectory`, `CommandLineFormatting`; the file-access policy `FileAccess`; the generation jobs
  `Jobs` and the flag `UsesConfigTables`; output methods `WriteLine(string)` and `WriteError(string)`;
  derived property `ExitCode` (0 or 1).
- *Constraints*: `Context` implements `IDisposable`; callers must dispose to release the log file
  `StreamWriter`. Throws `ArgumentException` for unknown flags, invalid option values (including a
  `--file-access` value that is not a mode), repeated `--config`, `--root` or `--file-access`,
  `--allow-root` combined with `--file-access any` or `no-symlinks`, `--input`/`--output` combined
  with configuration tables, and a column-width count that does not match the table. Throws
  `InvalidOperationException` for an invalid or missing root directory or `--allow-root` folder, an
  invalid configuration file, and an unopenable log file.

**TableJob**: One generation job, consumed by the Dictionary subsystem.

- *Type*: In-process .NET API
- *Role*: Provider — `DictionaryGenerator` runs each job
- *Contract*: `InputPatterns` (as written), `OutputFile` (as written, or `null` for stdout),
  `BaseDirectory` (for relative inputs and output), `Label` (`table job {n} ('{output}')` or
  `null`), `ArePathsJudged` (`true` for configuration jobs), `Options` (`MarkdownOptions`).
- *Constraints*: Immutable.

**OutputFormat**, **SortOrder**, **InputKind** and **MarkdownOptions**: Types from the Dictionary
subsystem.

- *Type*: In-process .NET public API
- *Role*: Consumer — `Context`, `ConfigFile`, `FormattingSettings` and `TableJob` reference these
  types defined in the Dictionary subsystem
- *Contract*: `OutputFormat` is `Bullets` (default) or `Table`; `SortOrder` is `FileOrder`
  (default) or `Alphabetical`; `InputKind` is `Terms` (default) or `Deprecated`; `MarkdownOptions`
  holds the effective formatting of one job, including `ColumnWidths`.
- *Constraints*: N/A

**PathResolver**: Path resolution from the Utilities subsystem.

- *Type*: In-process .NET API
- *Role*: Consumer — `Context` resolves `--config`, `--root` and `--allow-root`; `ConfigFile`
  resolves `root`
- *Contract*: `ResolvePath` and `ResolveExistingDirectory` (see *PathResolver Design*)
- *Constraints*: N/A

**FileAccessPolicy**: File-access confinement from the Utilities subsystem.

- *Type*: In-process .NET API
- *Role*: Consumer — `Context` parses the mode and creates the policy; `ConfigFile` checks the `root`
  key with `DescribeNeverAccessed`
- *Contract*: `ParseMode`, `Create` and `DescribeNeverAccessed` (see *FileAccessPolicy Design*)
- *Constraints*: The policy is built once per run and is immutable.

### Design

`Context` is created once per invocation by `Program.Main` calling `Context.Create(args)`. The
factory delegates to the private nested `ArgumentParser`, which processes `args` sequentially:
each recognized flag sets a property; unrecognized flags and invalid values throw
`ArgumentException`. Formatting options are recorded only when they are given, as the
command-line `FormattingSettings` layer. After the last argument the parser settles the file-access
mode: the `--file-access` value, or `roots` when only `--allow-root` is given, or `any`; `--allow-root`
with `any` or `no-symlinks` is rejected. `Context.Create` then:

1. Resolves `--root` against the working directory (it must exist).
2. Resolves `--config` against the working directory (never the root) and loads it with
   `ConfigFile.Load`, which also resolves the file's `root` key against the file's directory.
3. Selects the effective root: `--root`, else the configuration `root`, else none.
4. Builds the file-access policy. Under `roots` the allowed folders are the `--allow-root` folders,
   resolved against the working directory (never the root), or else the working directory and the
   configuration file's folder. The configuration file's folder is the trusted base of `no-symlinks`.
   Neither `--root` nor the `root` key adds an allowed folder.
5. Builds the jobs. With configuration tables, one `TableJob` per table, with formatting layers
   command line, job, configuration defaults, and base directory root, else the configuration
   file's directory; `--input` and `--output` are rejected. Without tables, when `--input` was
   given, one command-line `TableJob` with layers command line and configuration defaults, and
   base directory root, else the working directory. Configuration jobs have judged paths; the
   command-line job has trusted paths.
6. Opens the log file if `--log` was specified.

The formatting precedence is therefore applied in one place, `TableJob.Create`:

```mermaid
flowchart LR
    CLI[Command-line options] --> JOB[Table job keys]
    JOB --> DEF[Config file defaults]
    DEF --> BUILTIN[Built-in defaults]
```

The base directory for relative input patterns and output paths follows this precedence:

| Path origin | 1st | 2nd | 3rd (default) |
| :--- | :--- | :--- | :--- |
| Configuration table job `input` / `output` | `--root` | config `root` | configuration file directory |
| Command-line `--input` / `--output` | `--root` | config `root` | working directory |
| `--config`, `--log`, `--results`, `--allow-root` | not rooted | not rooted | working directory |

Configuration validation and root validation are fail-fast: they stop the run before any job runs.

**FormattingSettings** (supporting value type): an immutable record with the nullable members
`Format`, `SectionHeading`, `HeadingDepth`, `SortBy`, `TermHeader`, `DefinitionHeader`, `Kind`,
`UseHeader`, `WhereHeader` and `ColumnWidths`. A `null` member means "not specified at this
layer". `Over(lower)` returns a new layer in which this layer's non-null members win and the lower
layer fills the rest. `Empty` is the layer that specifies nothing.

Output routing: `WriteLine` writes to `Console.Out` (unless `Silent`) and to `_logWriter` (if
open). `WriteError` additionally sets `_hasErrors = true`, writes to `Console.Error` in red
(unless `Silent`), and writes to `_logWriter`. `ExitCode` derives from `_hasErrors` only; it
cannot be reset once an error is recorded. `Context` implements `IDisposable` to close the log
`StreamWriter`.
