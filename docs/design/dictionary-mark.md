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

**Utilities Subsystem** contains `GlobMatcher`, `PathResolver`, `PathHelpers`, `TemporaryDirectory`,
`FileAccessPolicy` and `RealPath`, providing shared file-matching, path-resolution, path-safety and
file-access confinement services consumed by the Cli, Dictionary and SelfTest subsystems. `RealPath` holds
all platform-specific (native) code of the tool.

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
  `--file-access <any|roots|no-symlinks|roots,no-symlinks>` (file-access mode for configuration-file
  paths), `--allow-root <dir>` (allowed folder, repeatable; implies roots),
  and `--results`/`--result <file>` (results file). Returns exit code 0 on
  success, 1 on error.
- *Constraints*: Relative input patterns and output paths are resolved against the root
  directory when one is given, and otherwise against the working directory at invocation time.
  `..` segments are supported before the first wildcard. Missing output directories are created.
  All file I/O uses UTF-8 encoding without BOM. Input errors are strict: a missing file, an empty
  glob match, an undefined `${VAR}` reference, an entry without a definition, or an invalid
  `--kind` value is reported as an error and produces no output. A configuration-file path that the
  file-access policy denies is reported with the error code `file-access-denied`, the path as written,
  its real path when known and a reason naming the mode; it fails only its job.

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
  defines tables, `--input` and `--output` are rejected. Every input and output path of the file is
  judged by the file-access policy; the `root` key only sets the base directory and never grants access.

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

The system has two security boundaries, both enforced entirely within the Utilities subsystem:

- **Path-traversal prevention**: caller-supplied file paths are validated by
  `PathHelpers.SafePathCombine` before any file-system operation combines a base directory with an
  untrusted relative path.
- **File-access confinement**: a configuration file may come from someone the user does not trust, for
  example a pull request built by CI. `FileAccessPolicy` judges every input and output path the file names
  before it is used:
  - the never-accessed list (URLs, paths written as UNC paths, local paths that lead to a share through a
    link, device paths, any colon other than the drive of an absolute path on Windows, anything that is not a
    regular file) applies in every mode. As in CanvasNet, a path that is itself on a share or mapped network
    drive is accessed, so a configuration file on a share keeps working;
  - `--file-access roots` confines the paths to the allowed folders, and `no-symlinks` refuses symbolic
    links and junctions, both judged by the real path of the opened file.

  Paths given on the command line are trusted and never judged. Reads and writes happen on the handle that
  was judged, and an output is never truncated before it is verified.

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
4. For a configuration job, `FileAccessPolicy.ResolvePattern` judges each input pattern and resolves it,
   judging every folder before it is listed. For the command-line job, `GlobMatcher.Resolve` resolves the
   input patterns against its base directory.
5. `YamlDictionaryLoader` parses each resolved YAML file into a `DictionaryEntry` list. A configuration
   job reads each file through `FileAccessPolicy.ReadAllText`, which judges every form of its path and then
   its handle; a denied file fails the job.
6. `ConflictDetector.Detect` scans the job's combined entry list; any conflict is reported via
   `context.WriteError` and halts output generation for that job.
7. `MarkdownFormatter.Format` renders the validated entries as a Markdown string, sizing table
   columns by the job's column widths when given.
8. The formatted Markdown is written to the job's output file (resolved against its base
   directory, creating missing parent directories), or to stdout if no output file was specified. A
   configuration job writes through `FileAccessPolicy.WriteAllText`, which writes nothing when the output
   or a folder it needs is denied.
9. Errors of configuration jobs are prefixed with `table job {n} ('{output}')`; when any
   configuration job failed, a summary `{failed} of {total} table jobs failed` is reported.

Base directory for relative input patterns and output paths:

| Path origin | 1st | 2nd | 3rd (default) |
| :--- | :--- | :--- | :--- |
| Configuration table job `input` / `output` | `--root` | config `root` | configuration file directory |
| Command-line `--input` / `--output` | `--root` | config `root` | working directory |
| `--config`, `--log`, `--results`, `--allow-root` | not rooted | not rooted | working directory |

## Design Constraints

- **Platform**: runs on any OS supported by the .NET SDK (Windows, Linux, macOS). Platform-specific APIs
  are used only in `RealPath`, which reports the real path of an open file (Windows
  `GetFinalPathNameByHandleW`, Linux `/proc/self/fd`, macOS `proc_pidfdinfo`). Where a platform cannot
  tell the real path, the restrictive file-access modes deny the file.
- **File-access limits**: a hard link cannot be detected, because it is a second name of the same file.
  Under `roots` a configuration file can still overwrite any file inside the allowed folders. A folder
  swapped by another process during a write can leave an empty file at the swapped location, but no
  content is written there.
- **Target frameworks**: net10.0, net11.0 (`RollForward=LatestMajor`); distributed as a .NET global
  tool (`PackAsTool`).
- **Input size**: no enforced limit on the number of YAML files or entries.
- **Output size**: no enforced limit; formatted output is buffered in memory before writing.
- **Encoding**: all file I/O uses UTF-8 without BOM.
- **Exit-code contract**: exit code 0 means no errors recorded; exit code 1 means at least one
  error was written via `context.WriteError`.
