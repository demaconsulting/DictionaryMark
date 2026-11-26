# DictionaryMark

DictionaryMark is a .NET command-line application that reads YAML dictionary files containing
term entries (plain definitions or rich entries) and deprecated-term entries, and produces
formatted Markdown output as bullet lists or tables.

## Architecture

DictionaryMark is organized into five subsystems and one top-level Program unit.

![System Structure](DictionaryMarkView.svg)

**Program** is the entry point and execution orchestrator. It first offers the arguments to
`HelpCommand`, which answers the `help` command. Otherwise it creates a `Context` from the
command-line arguments, dispatches to version display, help display, self-validation, or
Dictionary generation based on the parsed flags, and returns the exit code.

**Cli Subsystem** contains `Context`, which handles all command-line argument parsing and owns
the output streams for logging and results; `ConfigFile`, which loads the optional YAML
configuration file; `TableJob`, which represents one generation job with its effective
formatting; and `HelpCommand`, which answers the built-in `help` command from embedded help cards.

**Dictionary Subsystem** contains `YamlDictionaryLoader`, `ConflictDetector`, `MarkdownFormatter`,
`DictionaryGenerator`, and supporting data-model types. It reads YAML files, detects term
conflicts, and renders Markdown output.

**SelfTest Subsystem** contains `Validation`, which runs in-process self-tests when the user
invokes the tool with `--validate`.

**Files Subsystem** contains `FileReferenceResolver`, `FileAccessPolicy`, and `RealPath`, adapted from
CanvasNet. It decides which local files the tables of a configuration file may read and write, reads the input files
it allows from the handle it judged, and checks every output path before anything is written. `Context` builds the
policy of a run and `DictionaryGenerator` reads and checks each configuration-table job's files through it.

**Utilities Subsystem** contains `GlobMatcher`, `PathResolver`, `PathHelpers`, and
`TemporaryDirectory`, providing shared file-matching, path-resolution and path-safety services
consumed by the Cli, Dictionary and SelfTest subsystems.

## External Interfaces

DictionaryMark exposes three external interfaces: its command-line argument list, the built-in
`help` command, and the optional YAML configuration file named by `--config`.

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
  `--file-access <any|roots|no-symlinks|roots,no-symlinks>` (which files configuration tables, and relative
  `--input`/`--output` paths a configuration `root` key moves, may read and write;
  the last occurrence wins), `--allow-root <dir>` (an allowed folder, repeatable; implies `roots`),
  and `--results`/`--result <file>` (results file). Returns exit code 0 on
  success, 1 on error.
- *Constraints*: Relative input patterns and output paths are resolved against the root
  directory when one is given, and otherwise against the working directory at invocation time.
  `..` segments are supported before the first wildcard. Missing output directories are created.
  All file I/O uses UTF-8 encoding without BOM. Input errors are strict: a missing file, an empty
  glob match, an undefined `${VAR}` reference, an entry without a definition, or an invalid
  `--kind` value is reported as an error and produces no output. Argument and configuration
  errors are found before `--silent` and `--log` take effect, so they are always printed and never
  logged. The `help` command is accepted as the first argument (see *Help Command*).

**Help Command**: Built-in help, answered from help cards embedded in the tool assembly.

- *Type*: CLI
- *Role*: Provider (the tool answers the `help` command from the shell)
- *Contract*: `dictionarymark help` prints the topic index and `dictionarymark help <topic>` prints
  a topic card with its examples; HelpCards adds `--example`, `--search`, and `--json`. Returns exit
  code 0 for success, 2 for a wrong help argument or an unknown topic, and 70 for a defective card
  catalog.
- *Constraints*: `help` must be the first argument. It is answered before the options are parsed,
  so `--silent` and `--log` do not apply and no banner is printed.

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
  defines tables, `--input` and `--output` are rejected. The table inputs (every glob match) and outputs are judged
  by the file-access policy of the run; the `root` key never widens the allowed folders.

## Dependencies

- **YamlDotNet**: used for YAML parsing in `YamlDictionaryLoader` and `ConfigFile` — see
  *YamlDotNet*
- **Microsoft.Extensions.FileSystemGlobbing**: used for glob-pattern file matching in `GlobMatcher` —
  see *FileSystemGlobbing*
- **DemaConsulting.TestResults**: used for structured test-result reporting in `Validation` —
  see *DemaConsulting.TestResults*
- **DemaConsulting.HelpCards**: used for loading, validating, and serving the built-in help cards
  in `HelpCommand` — see *DemaConsulting.HelpCards*

## Risk Control Measures

N/A — DictionaryMark is a development tool with no patient-safety or safety-critical risk
classification. No software-item segregation is required by the applicable risk framework.

The system has two security boundaries:

- **Path-traversal prevention for the tool's own files**: caller-supplied file paths are validated by
  `PathHelpers.SafePathCombine` before any file-system operation combines a base directory with an untrusted
  relative path (Utilities subsystem).
- **File-access confinement for configuration files**: the trust boundary is the command line. Every command-line option
  (including `--file-access`, `--allow-root`, `--root` and the `--config` path) is trusted, and reading the
  configuration file is never judged; the content of the configuration file may come from an untrusted source, so
  everything it names reaches the disk only through `FileReferenceResolver` and `CheckOutputPath`. Its `root:` key is
  resolved as text only (never probed) and only moves where relative table paths point; it never widens the allowed
  folders. Every input and output its tables name is judged by the file-access policy chosen with `--file-access` and
  `--allow-root` (Files subsystem). The policy is built before any input is read; every literal input is handed to
  `FileReferenceResolver` without being looked up first, and every glob match is read through it; the resolver decides
  about the opened file and parses the bytes it judged; every output is checked with `CheckOutputPath` before any folder
  is created. Network, device and alternate-data-stream paths, control characters and non-regular files are refused
  under every mode, including the default `any`. Paths named on the command line are trusted and not judged, except a
  relative `--input` pattern or `--output` path that the configuration file's `root` key moves (no `--root` given, the
  file has a `root` key and no tables): that path is untrusted content and is judged exactly like a table path. Absolute
  command-line paths, every path when `--root` is given, every path when the file has no `root` key, and `--config`,
  `--log` and `--results` stay trusted. Because the file's content is untrusted, a `${VAR}` environment-variable
  reference in its `root`, table `input` or table `output` would let it read the process environment (for example a
  secret) and leak it through a created file name or a message; so under every mode except `any` (`roots`,
  `no-symlinks`, `roots,no-symlinks`, and `--allow-root`, which implies `roots`) `ConfigFile.Load` refuses such a
  reference before anything is expanded, read or created, with an error that names the file, the job and the key and
  that reveals neither the name nor the value of the variable nor whether it is defined; the paths typed on the command
  line keep expanding `${VAR}` in every mode, and the default and `any` modes are unchanged. A message about a judged
  path (a table path, or a relative command-line path moved by `root`) shows the path as written, a glob match relative
  to the base directory, never the absolute host path, so a message shown to the author of the file does not reveal the
  layout of the machine; the Files subsystem's contract (its message echoes only the reference it was given) is
  unchanged, and `DictionaryGenerator` chooses which text to give it. A denial
  by the file-access mode fails only its table job with `file-access-denied`; any other refusal (a never-list path, a
  non-regular file, an input over 16 MiB) fails only its table job with the ordinary read or write error. Known limits:
  the write that follows the output check goes by name, so it is not free of races; each table input pattern is checked
  against the never list before anything is listed, so a network, device or URL-like pattern is refused without
  contacting a host, but glob matching of a wildcard pattern under local folders enumerates folder and file names before
  each match is judged (the one accepted access outside the resolver), so names outside the allowed folders can be
  learned, although no content is read; a hard link cannot be told apart from its file; a judged input is limited to 16
  MiB; and a root that does not exist when the run starts allows nothing.

## Data Flow

1. The user invokes the tool; `Program.Main` first offers the arguments to `HelpCommand.TryRun`.
   When the first argument is `help`, it builds the card catalog, writes the topic index or the
   topic card to the console, and the tool exits with its exit code. Otherwise it returns at once
   without building the catalog, and the following steps apply.
2. `Program.Main` creates a `Context` from the command-line arguments.
   `Context` resolves `--root` and `--config`, builds the file-access policy from `--file-access` and
   `--allow-root`, loads the `--config` file (if any), and builds the generation jobs:
   one per configuration table, or one command-line job for `--input`. Each job carries its
   effective formatting (command line, then job, then configuration defaults, then built-in
   defaults) and its base directory (see the precedence below). When no `--root` is given and the configuration file
   has a `root` key, the command-line job is marked so that its relative paths are judged. Any validation failure stops
   the
   run here with exit code 1.
3. If `--validate` is present, `Validation.Run` executes in-process self-tests and the tool exits.
   `context.HeadingDepth` controls the Markdown heading level of the validation report header.
4. `DictionaryGenerator` runs every job in turn, continuing after a failed job. For each job:
5. The job's input patterns are resolved against its base directory: `GlobMatcher.Resolve` resolves every trusted
   pattern and the judged wildcard patterns; a judged literal pattern (a configuration-table pattern, or a relative
   command-line pattern the configuration `root` key moves) is only expanded, rooted and normalized as text.
6. `YamlDictionaryLoader.Load` reads each resolved YAML file and produces a `DictionaryEntry` list. For a
   judged file, each file is first read through the job's `FileReferenceResolver`, which judges the
   opened file by the policy, and the loader parses the bytes it returned.
7. `ConflictDetector.Detect` scans the job's combined entry list; any conflict is reported via
   `context.WriteError` and halts output generation for that job.
8. `MarkdownFormatter.Format` renders the validated entries as a Markdown string, sizing table
   columns by the job's column widths when given.
9. The formatted Markdown is written to the job's output file (resolved against its base
   directory, creating missing parent directories), or to stdout if no output file was specified. For a
   judged output path (a configuration-table output, or a relative command-line output moved by the configuration `root`
   key), the output path is first checked with `CheckOutputPath`.
10. Errors of configuration jobs are prefixed with `table job {n} ('{output}')`; when any
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
- **Input size**: no enforced limit on the number of YAML files or entries; a configuration-table input file is
  read up to 16 MiB.
- **Output size**: no enforced limit; formatted output is buffered in memory before writing.
- **Encoding**: all file I/O uses UTF-8 without BOM.
- **Exit-code contract**: exit code 0 means no errors recorded; exit code 1 means at least one
  error was written via `context.WriteError`. The `help` command has its own exit codes; see
  *Help Command*.
