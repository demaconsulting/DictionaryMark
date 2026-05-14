# Validation

The `Validation` class provides the self-validation framework for the Template DotNet Tool.
It runs a suite of internal tests that demonstrate the tool is functioning correctly in the
deployment environment.

## Overview

`Validation.Run` prints a header, executes each test, accumulates results into a
`DemaConsulting.TestResults.TestResults` object, prints aggregate totals, and optionally writes
a results file in TRX or JUnit XML format.

## Data Model

`Validation` holds no instance state. All state is local to `Run` and the private test methods.

## Methods

### Run(Context context)

Orchestrates the validation sequence:

1. Calls `PrintValidationHeader` to emit a Markdown heading and a table with tool and
   environment metadata. The heading level is controlled by `context.HeadingDepth`
   (default `1`, producing a `#` heading; `--depth 2` produces `##`, etc.).
2. Constructs a `TestResults` object named `"Template DotNet Tool Self-Validation"`.
3. Calls each test runner (`RunVersionTest`, `RunHelpTest`).
4. Prints aggregate totals: `Total Tests:`, `Passed:`, and `Failed:`.
   If `failedTests > 0`, the failed count is printed via `WriteError`, which sets
   `context._hasErrors = true` and causes `context.ExitCode` to return `1`.
5. Calls `WriteResultsFile` if `context.ResultsFile` is set.

### RunVersionTest / RunHelpTest

Each test method:

1. Creates a temporary directory via `TemporaryDirectory`.
2. Constructs a log-file path using `PathHelpers.SafePathCombine`.
3. Invokes `Program.Run` with the relevant arguments and captures the output log.
4. Validates the output against expected content.
5. Records pass or fail in the shared `TestResults`.
6. Prints its own pass/fail line to `context` upon completion.

Any exception thrown during steps 1–5 is caught by a broad `catch (Exception)` handler, which
records the failure via `HandleTestException` and continues.
This ensures the test framework remains robust and all remaining tests execute even if one test
fails unexpectedly.

### WriteResultsFile(Context context, TestResults testResults)

Writes `testResults` to `context.ResultsFile`. The format is determined by the file extension:
`.trx` for TRX (MSTest), `.xml` for JUnit. Any other extension causes an error message to be
written to `context` and the method returns without creating a file.

Any exception thrown during file write is caught by a broad `catch (Exception)` handler and reported to `context` via `WriteError`.

### Private Helpers

#### CreateTestResult(string testName)

Creates a `TestResult` object pre-populated with the class name `"Validation"` and code base `"TemplateDotNetTool"`.

#### FinalizeTestResult(TestResult test, DateTime startTime, TestResults testResults)

Sets `test.Duration` to the elapsed time since `startTime` and appends `test` to `testResults.Results`.

#### HandleTestException(TestResult test, Context context, string testName, Exception ex)

Sets `test.Outcome` to `Failed`, records the exception message as `test.ErrorMessage`,
and writes a failure line to `context` via `WriteError`.

#### TemporaryDirectory (nested class)

Manages a temporary directory for test execution. Implements `IDisposable`. The constructor
calls `Directory.CreateDirectory` and wraps `IOException`, `UnauthorizedAccessException`,
and `ArgumentException` in `InvalidOperationException`. `Dispose` attempts a best-effort
deletion of the directory tree; `IOException` and `UnauthorizedAccessException` during
cleanup are silently ignored.

## Interactions

The `Validation` subsystem uses the following dependencies:

- **`Context`**: Output channel for header and summary lines.
- **`Program`**: `Program.Run` called to exercise the tool.
- **`PathHelpers`**: `SafePathCombine` for temp-dir file paths.
- **`TrxSerializer`**: Serializes TestResults to TRX format.
- **`JUnitSerializer`**: Serializes TestResults to JUnit XML format.
- **`DemaConsulting.TestResults`**: TestResults/TestResult/TestOutcome for test state.
