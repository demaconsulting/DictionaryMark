### Validation

![SelfTest Subsystem Structure](SelfTestView.svg)

#### Purpose

`Validation.Run` prints a system information header, executes eleven fixed in-process self-tests,
prints a pass/fail summary, and optionally writes results to a TRX or JUnit XML file. Each test
runs the real `Program` logic with temporary YAML input files and a log file to capture output.
`Validation` is a static class with no instance state.

#### Data Model

N/A - static class with no instance state.

#### Key Methods

**Run**: Entry point. Prints the validation header, runs each self-test, prints a summary, and
optionally writes a results file.

- *Parameters*: `Context context` — provides output channels and the optional results file path.
- *Returns*: `void`.
- *Preconditions*: `context` is non-null.
- *Postconditions*: All eleven tests executed; summary printed; results file written if
  `context.ResultsFile` is set; `context.WriteError` called for each failed test.

Throws `ArgumentNullException` when `context` is null.

**PrintValidationHeader** *(private)*: Writes a Markdown table of system information (tool
version, machine name, OS, .NET runtime, timestamp) to `context`.

- *Parameters*: `Context context` — output channel.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: System info table written to context output.

**RunVersionTest** *(private)*: Runs `Program.Run` with `["--silent", "--log", logFile,
"--version"]`, verifies the log contains a version string matching `\d+\.\d+\.\d+`, and records
the outcome in `testResults`.

- *Parameters*: `Context context` — outer context for error reporting; `TestResults testResults`
  — accumulator for test outcomes.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: One `TestResult` added to `testResults`.

**RunHelpTest** *(private)*: Runs `Program.Run` with `["--silent", "--log", logFile, "--help"]`,
verifies the log contains `"Usage:"` and `"Options:"`, and records the outcome.

- *Parameters*: `Context context` — outer context; `TestResults testResults` — accumulator.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: One `TestResult` added.

**RunBulletGenerationTest** *(private)*: Writes a temporary YAML file, runs `Program.Run` with
`--format bullets`, and verifies the output file contains the expected bold-term bullet entries.

- *Parameters*: `Context context` — outer context; `TestResults testResults` — accumulator.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: One `TestResult` added.

**RunTableGenerationTest** *(private)*: Writes a temporary YAML file, runs `Program.Run` with
`--format table`, and verifies the output file contains the expected table header row and entry
row.

- *Parameters*: `Context context` — outer context; `TestResults testResults` — accumulator.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: One `TestResult` added.

**RunCustomHeadersTest** *(private)*: Writes a temporary YAML file, runs `Program.Run` with
`--format table --term-header Abbreviation --def-header Meaning`, and verifies the output
contains the custom header text.

- *Parameters*: `Context context` — outer context; `TestResults testResults` — accumulator.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: One `TestResult` added.

**RunConflictDetectionTest** *(private)*: Writes two YAML files with the same term but different
definitions, runs `Program.Run`, and verifies the exit code is non-zero (conflict detected).

- *Parameters*: `Context context` — outer context; `TestResults testResults` — accumulator.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: One `TestResult` added.

**RunHiddenEntryTest** *(private)*: Generates output from a glossary with a `visible: false` entry
and verifies the entry is omitted while visible entries remain.

- *Parameters*: `Context context`; `TestResults testResults`.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: One `TestResult` added.

**RunDeprecatedTableTest** *(private)*: Generates a deprecated-terms table with `--kind deprecated`
and verifies the `Not used`/`Use instead` columns and rows.

- *Parameters*: `Context context`; `TestResults testResults`.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: One `TestResult` added.

**RunColumnWidthsTest** *(private)* (`DictionaryMark_ColumnWidths`): Generates a table with
`--format table --column-widths 25,75` and verifies the separator row has exactly 25 and 75 dashes
and the entry row is present.

- *Parameters*: `Context context`; `TestResults testResults`.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: One `TestResult` added.

**RunConfigTablesTest** *(private)* (`DictionaryMark_ConfigTables`): Writes a dictionary in
`shared/`, and in `config/` a configuration file with a top-level `format: table` and two jobs that
read `../shared/*.yaml` and `../shared/terms.yaml` and write `../out/table.md` and
`../out/bullets.md` (the second job overrides `format: bullets`). The `out/` directory is not
created beforehand. Runs `Program.Run` with `--config` and verifies both outputs exist with the
table and bullet content. This exercises configuration loading, the formatting precedence,
config-relative `..` path resolution and output directory creation on each deployment platform.

- *Parameters*: `Context context`; `TestResults testResults`.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: One `TestResult` added.

**RunFileAccessRootsTest** *(private)* (`DictionaryMark_FileAccessRoots`): Writes a dictionary and a configuration
file in `config/` with two jobs that read `terms.yaml`: the first writes `inside.md` (inside the folder), the second
writes `../out/table.md` (outside it). Runs `Program.Run` with `--log`, `--config` and `--allow-root config` and
verifies that the exit code is 1, `config/inside.md` holds the bullet entry (a positive control proving the allowed
folder is not simply refusing everything, which matters where the temporary folder is reached through a link, as on
macOS), neither `out/table.md` nor the `out` folder exists, and the log contains `file-access-denied`. This exercises
the file-access confinement of configuration-table outputs on each deployment platform.

- *Parameters*: `Context context`; `TestResults testResults`.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: One `TestResult` added.

**WriteResultsFile** *(private)*: Serializes `testResults` to the path in `context.ResultsFile`.
Supports `.trx` (TRX format) and `.xml` (JUnit format). Calls `context.WriteError` for
unsupported extensions.

- *Parameters*: `Context context` — provides `ResultsFile` path and error channel;
  `TestResults testResults` — results to serialize.
- *Returns*: `void`.
- *Preconditions*: `context.ResultsFile` is non-null.
- *Postconditions*: Results file written, or error reported for unsupported extension.

#### Error Handling

- `ArgumentNullException` — `Run` throws when `context` is null; the caller (`Program.Run`) must
  supply a valid context.
- Test failures — each private test method catches all exceptions internally, records them as
  `Failed` outcomes, and calls `context.WriteError`. No exception propagates out of individual
  test methods.
- Unsupported results file extension — `WriteResultsFile` calls `context.WriteError` (setting
  exit code to 1) rather than throwing.
- Serialization errors from `TrxSerializer` or `JUnitSerializer` — caught within
  `WriteResultsFile` by a top-level `catch (Exception)` handler; `context.WriteError` is called
  with the error message, setting the exit code to 1. No exception propagates to `Program.Main`.

#### Dependencies

- **Context** — CLI subsystem; provides output channels (`WriteLine`, `WriteError`) and
  `ResultsFile` path.
- **Program** — Program unit; `Program.Run` is invoked by each test method to exercise the full
  tool path.
- **TemporaryDirectory** — Utilities subsystem; creates temp folders and resolves safe self-test
  file paths.
- **DemaConsulting.TestResults** — OTS package; `TestResults` and `TestResult` types accumulate
  self-test outcomes; `TrxSerializer` and `JUnitSerializer` serialize results.

#### Callers

- **Program.Run** — calls `Validation.Run(context)` when `context.Validate` is `true`. This is
  the sole production caller. `Validation.Run` itself calls `Program.Run` internally during each
  self-test, making `Program` and `Validation` mutually recursive at test time.
