### Validation Design

The `Validation` class provides self-validation functionality for DictionaryMark, running
in-process tests to verify correct tool operation in the deployment environment.

#### Purpose

`Validation.Run` prints a system information header, executes a fixed set of self-tests,
prints a pass/fail summary, and optionally writes the results to a TRX or JUnit XML file.
Each test runs the real `Program` logic with temporary YAML input files and a log file to
capture output.

#### Data Model

`Validation` is a `static` class with no instance state. Internal helper types are:

| Type                 | Description                                                           |
| -------------------- | --------------------------------------------------------------------- |
| `TemporaryDirectory` | `IDisposable` wrapper that creates and auto-deletes a temp directory. |

#### Key Methods

##### Run(Context context)

Entry point. Prints the validation header, runs each self-test, prints a summary, and
optionally writes a results file.

**Throws:** `ArgumentNullException` - when `context` is null.

##### PrintValidationHeader(Context context) *(private)*

Writes a Markdown table of system information (tool version, machine name, OS, .NET runtime,
timestamp) to `context`.

##### RunVersionTest(Context context, TestResults testResults) *(private)*

Runs `Program.Run` with `["--silent", "--log", logFile, "--version"]`, verifies the log
contains a version string matching `\d+\.\d+\.\d+`, and records the outcome.

##### RunHelpTest(Context context, TestResults testResults) *(private)*

Runs `Program.Run` with `["--silent", "--log", logFile, "--help"]`, verifies the log
contains `"Usage:"` and `"Options:"`, and records the outcome.

##### RunBulletGenerationTest(Context context, TestResults testResults) *(private)*

Writes a temporary YAML file, runs `Program.Run` with `--format bullets`, and verifies
the output file contains the expected bold term and definition text.

##### RunTableGenerationTest(Context context, TestResults testResults) *(private)*

Writes a temporary YAML file, runs `Program.Run` with `--format table`, and verifies
the output file contains the expected table header row and entry row.

##### RunCustomHeadersTest(Context context, TestResults testResults) *(private)*

Writes a temporary YAML file, runs `Program.Run` with `--format table --term-header Abbreviation
--def-header Meaning`, and verifies the output file contains the custom header text.

##### RunConflictDetectionTest(Context context, TestResults testResults) *(private)*

Writes two YAML files with the same term but different definitions, runs `Program.Run`,
and verifies the exit code is non-zero (conflict detected).

##### WriteResultsFile(Context context, TestResults testResults) *(private)*

Serializes `testResults` to the path in `context.ResultsFile`. Supports `.trx` (TRX format)
and `.xml` (JUnit format). Calls `context.WriteError` for unsupported extensions.

#### Error Handling

`Validation` uses two error-signalling mechanisms:

- **Null context** — `Run` throws `ArgumentNullException` when `context` is null. The caller
  (`Program.Run`) must supply a valid context.
- **Test failures** — Each self-test method catches all exceptions internally, records them as
  test failures, and calls `context.WriteError` for each failure. No exception propagates out of
  the individual test methods. Failed tests and unsupported results-file extensions both call
  `context.WriteError`, which sets `context.ExitCode` to 1 as a side effect.

#### Interactions

| Dependency        | Role                                                            |
| ----------------- | --------------------------------------------------------------- |
| `Context`         | Provides output channels and the optional results file path.    |
| `Program`         | Exercised by each self-test to validate real tool behavior.     |
| `PathHelpers`     | Used to build safe paths for temporary log files.               |
| `TrxSerializer`   | Serializes results to TRX format when `.trx` extension is used. |
| `JUnitSerializer` | Serializes results to JUnit XML when `.xml` extension is used.  |

#### Dependencies

| Dependency                         | Role                                                                                      |
| ---------------------------------- | ----------------------------------------------------------------------------------------- |
| `Context`                          | CLI subsystem — provides output channels (`WriteLine`, `WriteError`) and `ResultsFile` path. |
| `Program`                          | Program unit — `Program.Run` is invoked by each test method to exercise the full tool path. |
| `PathHelpers`                      | Utilities subsystem — `SafePathCombine` builds safe log-file paths within temporary directories. |
| `DemaConsulting.TestResults`       | OTS package — `TestResults` and `TestResult` types accumulate self-test outcomes.         |
| `TrxSerializer` / `JUnitSerializer` | OTS package — serialize the result set to TRX or JUnit XML when `ResultsFile` is set.   |

#### Callers

`Program.Run` calls `Validation.Run(context)` when `context.Validate` is `true`. This is the
sole production caller. `Validation.Run` itself calls `Program.Run` internally during each
self-test, making `Program` and `Validation` mutually recursive at test time.
