# DictionaryMark

DictionaryMark is a .NET command-line application that reads YAML dictionary files containing
term entries (plain definitions or rich entries) and deprecated-term entries, and produces
formatted Markdown output as bullet lists or tables.

## Architecture

DictionaryMark is organized into four subsystems and one top-level Program unit.

![System Structure](DictionaryMarkView.svg)

**Program** is the entry point and execution orchestrator. It creates a `Context` from the
command-line arguments, dispatches to version display, help display, self-validation, or
Dictionary generation based on the parsed flags, and returns the exit code.

**Cli Subsystem** contains `Context`, which handles all command-line argument parsing and owns
the output streams for logging and results; `ConfigFile`, which loads the optional YAML
configuration file; and `TableJob`, which represents one generation job with its effective
formatting.

**Dictionary Subsystem** contains `YamlDictionaryLoader`, `ConflictDetector`, `MarkdownFormatter`,
`DictionaryGenerator`, and supporting data-model types. It reads YAML files, detects term
conflicts, and renders Markdown output.

**SelfTest Subsystem** contains `Validation`, which runs in-process self-tests when the user
invokes the tool with `--validate`.

**Utilities Subsystem** contains `GlobMatcher`, `PathResolver`, `PathHelpers`, and
`TemporaryDirectory`, providing shared file-matching, path-resolution and path-safety services
consumed by the Cli, Dictionary and SelfTest subsystems.

## External Interfaces

DictionaryMark exposes two external interfaces: its command-line argument list and the optional
YAML configuration file named by `--config`.

**Command-Line Interface**: The tool is invoked from a terminal with flags that control input
selection, output formatting, and operational mode.

- *Type*: Command-line interface
- *Role*: Provider
- *Contract*: Accepts the following flags: `-v`/`--version` (version query), `-?`/`-h`/`--help`
  (help display), `--silent` (suppress output), `--validate` (self-validation), `--log <file>`
  (write log), `-i`/`--input <pattern>` (input file pattern, repeatable), `-o`/`--output <file>`
  (output file), `-f`/`--format <bullets|table>` (output format), `-s`/`--section <text>`
  (section heading), `--term-header <text>` (term column header),
  `--def-header`/`--definition-header <text>` (definition column header),
  `--sort <file|alpha>` (sort order), `--depth <n>` (heading depth),
  `--kind <terms|deprecated>` (input kind), `--use-header <text>` (deprecated-table "use instead"
  column header), `--where-header <text>` (deprecated-table "where used" column header),
  `--column-widths <w1,w2[,w3]>` (relative table column widths), `--config <file>` (configuration
  file), `--root <dir>` (base directory for relative input and output paths),
  and `--results`/`--result <file>` (results file). Returns exit code 0 on
  success, 1 on error.
- *Constraints*: Relative input patterns and output paths are resolved against the root
  directory when one is given, and otherwise against the working directory at invocation time.
  `..` segments are supported before the first wildcard. Missing output directories are created.
  All file I/O uses UTF-8 encoding without BOM. Input errors are strict: a missing file, an empty
  glob match, an undefined `${VAR}` reference, an entry without a definition, or an invalid
  `--kind` value is reported as an error and produces no output.

**Configuration File**: A YAML file that supplies formatting defaults, an optional root
directory, and optional table jobs.

- *Type*: File input (YAML, UTF-8)
- *Role*: Consumer
- *Contract*: A mapping with optional top-level formatting keys (`format`, `section`, `depth`,
  `sort`, `term-header`, `def-header`/`definition-header`, `kind`, `use-header`, `where-header`,
  `column-widths`), an optional `root` key, and an optional `tables` sequence of jobs, each with a
  required `input` (string or sequence), a required `output`, and optional formatting keys. See
  *ConfigFile Design* for the full schema.
- *Constraints*: Unknown or misplaced keys are errors. Relative paths in the file resolve against
  the root directory when one is given, and otherwise against the file's directory. When the file
  defines tables, `--input` and `--output` are rejected.

## Dependencies

- **YamlDotNet**: used for YAML parsing in `YamlDictionaryLoader` and `ConfigFile` — see
  *YamlDotNet*
- **Microsoft.Extensions.FileSystemGlobbing**: used for glob-pattern file matching in `GlobMatcher` —
  see *FileSystemGlobbing*
- **DemaConsulting.TestResults**: used for structured test-result reporting in `Validation` —
  see *DemaConsulting.TestResults*

## Risk Control Measures

N/A — DictionaryMark is a development tool with no patient-safety or safety-critical risk
classification. No software-item segregation is required by the applicable risk framework.

The one security boundary in the system is path-traversal prevention: caller-supplied file paths
are validated by `PathHelpers.SafePathCombine` before any file-system operation combines a base
directory with an untrusted relative path. This boundary is enforced entirely within the Utilities
subsystem.

## Data Flow

1. The user invokes the tool; `Program.Main` creates a `Context` from the command-line arguments.
   `Context` resolves `--root`, loads the `--config` file (if any), and builds the generation jobs:
   one per configuration table, or one command-line job for `--input`. Each job carries its
   effective formatting (command line, then job, then configuration defaults, then built-in
   defaults) and its base directory (see the precedence below). Any validation failure stops the
   run here with exit code 1.
2. If `--validate` is present, `Validation.Run` executes in-process self-tests and the tool exits.
   `context.HeadingDepth` controls the Markdown heading level of the validation report header.
3. `DictionaryGenerator` runs every job in turn, continuing after a failed job. For each job:
4. `GlobMatcher.Resolve` resolves the job's input patterns against its base directory.
5. `YamlDictionaryLoader.Load` reads each resolved YAML file and produces a `DictionaryEntry` list.
6. `ConflictDetector.Detect` scans the job's combined entry list; any conflict is reported via
   `context.WriteError` and halts output generation for that job.
7. `MarkdownFormatter.Format` renders the validated entries as a Markdown string, sizing table
   columns by the job's column widths when given.
8. The formatted Markdown is written to the job's output file (resolved against its base
   directory, creating missing parent directories), or to stdout if no output file was specified.
9. Errors of configuration jobs are prefixed with `table job {n} ('{output}')`; when any
   configuration job failed, a summary `{failed} of {total} table jobs failed` is reported.

Base directory for relative input patterns and output paths:

| Path origin | 1st | 2nd | 3rd (default) |
| :--- | :--- | :--- | :--- |
| Configuration table job `input` / `output` | `--root` | config `root` | configuration file directory |
| Command-line `--input` / `--output` | `--root` | config `root` | working directory |
| `--config`, `--log`, `--results` | not rooted | not rooted | working directory |

## Design Constraints

- **Platform**: runs on any OS supported by the .NET SDK (Windows, Linux, macOS); no
  platform-specific APIs are used in core logic.
- **Target frameworks**: net10.0, net11.0 (`RollForward=LatestMajor`); distributed as a .NET global
  tool (`PackAsTool`).
- **Input size**: no enforced limit on the number of YAML files or entries.
- **Output size**: no enforced limit; formatted output is buffered in memory before writing.
- **Encoding**: all file I/O uses UTF-8 without BOM.
- **Exit-code contract**: exit code 0 means no errors recorded; exit code 1 means at least one
  error was written via `context.WriteError`.
