# DictionaryMark

DictionaryMark is a .NET command-line application that reads YAML dictionary files containing
term-definition pairs and produces formatted Markdown output as bullet lists or tables.

## Architecture

DictionaryMark is organized into four subsystems and one top-level Program unit.

![System Structure](DictionaryMarkView.svg)

**Program** is the entry point and execution orchestrator. It creates a `Context` from the
command-line arguments, dispatches to version display, help display, self-validation, or
Dictionary generation based on the parsed flags, and returns the exit code.

**Cli Subsystem** contains `Context`, which handles all command-line argument parsing and owns
the output streams for logging and results.

**Dictionary Subsystem** contains `YamlDictionaryLoader`, `ConflictDetector`, `MarkdownFormatter`,
`DictionaryGenerator`, and supporting data-model types. It reads YAML files, detects term
conflicts, and renders Markdown output.

**SelfTest Subsystem** contains `Validation`, which runs in-process self-tests when the user
invokes the tool with `--validate`.

**Utilities Subsystem** contains `GlobMatcher`, `PathHelpers`, and `TemporaryDirectory`, providing
shared file-matching and path-safety services consumed by the Dictionary and SelfTest subsystems.

## External Interfaces

DictionaryMark exposes a single external interface: its command-line argument list.

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
  `--sort <file|alpha>` (sort order), `--depth <n>` (heading depth), and
  `--results`/`--result <file>` (results file). Returns exit code 0 on success, 1 on error.
- *Constraints*: Input patterns are resolved relative to the working directory at invocation
  time. All file I/O uses UTF-8 encoding without BOM.

## Dependencies

- **YamlDotNet**: used for YAML parsing in `YamlDictionaryLoader` — see *YamlDotNet*
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
2. If `--validate` is present, `Validation.Run` executes in-process self-tests and the tool exits.
   `context.HeadingDepth` controls the Markdown heading level of the validation report header.
3. For each `--input` pattern, `GlobMatcher.GetFiles` resolves the pattern to concrete file paths.
4. `YamlDictionaryLoader.Load` reads each resolved YAML file and produces a `DictionaryEntry` list.
5. `ConflictDetector.Detect` scans the combined entry list; any duplicate term is reported via
   `context.WriteError` and halts further output generation.
6. `MarkdownFormatter.Format` renders the validated entries as a Markdown string.
7. The formatted Markdown is written to the file specified by `--output`, or to stdout if no
   output file was specified.

## Design Constraints

- **Platform**: runs on any OS supported by the .NET SDK (Windows, Linux, macOS); no
  platform-specific APIs are used in core logic.
- **Target frameworks**: net8.0, net9.0, net10.0; distributed as a .NET global tool (`PackAsTool`).
- **Input size**: no enforced limit on the number of YAML files or entries.
- **Output size**: no enforced limit; formatted output is buffered in memory before writing.
- **Encoding**: all file I/O uses UTF-8 without BOM.
- **Exit-code contract**: exit code 0 means no errors recorded; exit code 1 means at least one
  error was written via `context.WriteError`.
