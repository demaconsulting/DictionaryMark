# SelfTest Subsystem

The `SelfTest` subsystem provides the self-validation framework for the Template DotNet Tool.
It runs a built-in suite of tests to demonstrate the tool is functioning correctly in the
deployment environment.

## Overview

The `SelfTest` subsystem is invoked when the user passes `--validate` on the command line.
It exercises the tool's own capabilities and reports a pass/fail summary. It can also write
test results to a file in TRX or JUnit XML format for integration with CI/CD pipelines.

## Units

The `SelfTest` subsystem contains the following software unit:

| Unit         | File                     | Responsibility                                     |
|--------------|--------------------------|----------------------------------------------------|
| `Validation` | `SelfTest/Validation.cs` | Orchestrating and executing self-validation tests. |

## Interfaces

The `SelfTest` subsystem exposes the following outbound interface to the rest of the tool:

- **`Validation.Run`**: Runs all self-validation tests, prints a summary, and writes results.

## Interactions

The `SelfTest` subsystem uses the following dependencies:

- **`Context`**: Output channel for header lines, test summaries, and errors.
- **`Program`**: `Program.Run` is called internally to exercise the tool.
- **`PathHelpers`**: `SafePathCombine` for constructing log file paths in tests.
- **`DemaConsulting.TestResults.IO`**: TrxSerializer and JUnitSerializer provide TRX and JUnit XML
  serialization for results output.
- **`DemaConsulting.TestResults`**: `TestResults`, `TestResult`, and `TestOutcome` data-model types
  used for accumulating and representing self-validation results.

## Error Handling

`Validation.Run` handles errors in two ways:

- **Unsupported results file extension**: When `context.ResultsFile` has an extension other than
  `.trx` or `.xml`, `WriteResultsFile` calls `context.WriteError` with a descriptive message
  (e.g., `"Error: Unsupported results file format '.json'. Use .trx or .xml extension."`) and
  returns without writing a file. This causes `context.ExitCode` to return 1.
- **Test runner exceptions**: Each test runner (`RunVersionTest`, `RunHelpTest`) wraps its
  execution in a broad `catch (Exception)` handler. Any unexpected exception is recorded as a
  test failure via `HandleTestException`, allowing remaining tests to continue executing.
