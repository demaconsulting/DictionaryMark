### Validation

![SelfTest Subsystem Structure](SelfTestView.svg)

#### Purpose

`Validation.Run` prints a system information header, executes six fixed in-process self-tests,
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
- *Postconditions*: All six tests executed; summary printed; results file written if
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
