# Validation Design

The `Validation` class provides self-validation functionality for DictionaryMark, running
in-process tests to verify correct tool operation in the deployment environment.

## Overview

`Validation.Run` prints a system information header, executes a fixed set of self-tests,
prints a pass/fail summary, and optionally writes the results to a TRX or JUnit XML file.
Each test runs the real `Program` logic with temporary YAML input files and a log file to
capture output.

## Data Model

`Validation` is a `static` class with no instance state. Internal helper types are:

| Type                 | Description                                                           |
| -------------------- | --------------------------------------------------------------------- |
| `TemporaryDirectory` | `IDisposable` wrapper that creates and auto-deletes a temp directory. |

## Methods

### Run(Context context)

Entry point. Prints the validation header, runs each self-test, prints a summary, and
optionally writes a results file.

**Throws:** `ArgumentNullException` - when `context` is null.

### PrintValidationHeader(Context context) *(private)*

Writes a Markdown table of system information (tool version, machine name, OS, .NET runtime,
timestamp) to `context`.

### RunVersionTest(Context context, TestResults testResults) *(private)*

Runs `Program.Run` with `["--silent", "--log", logFile, "--version"]`, verifies the log
contains a version string matching `\d+\.\d+\.\d+`, and records the outcome.

### RunHelpTest(Context context, TestResults testResults) *(private)*

Runs `Program.Run` with `["--silent", "--log", logFile, "--help"]`, verifies the log
contains `"Usage:"` and `"Options:"`, and records the outcome.

### RunBulletGenerationTest(Context context, TestResults testResults) *(private)*

Writes a temporary YAML file, runs `Program.Run` with `--format bullets`, and verifies
the output file contains the expected bold term and definition text.

### RunTableGenerationTest(Context context, TestResults testResults) *(private)*

Writes a temporary YAML file, runs `Program.Run` with `--format table`, and verifies
the output file contains the expected table header row and entry row.

### RunCustomHeadersTest(Context context, TestResults testResults) *(private)*

Writes a temporary YAML file, runs `Program.Run` with `--format table --term-header Abbreviation
--def-header Meaning`, and verifies the output file contains the custom header text.

### RunConflictDetectionTest(Context context, TestResults testResults) *(private)*

Writes two YAML files with the same term but different definitions, runs `Program.Run`,
and verifies the exit code is non-zero (conflict detected).

### WriteResultsFile(Context context, TestResults testResults) *(private)*

Serializes `testResults` to the path in `context.ResultsFile`. Supports `.trx` (TRX format)
and `.xml` (JUnit format). Calls `context.WriteError` for unsupported extensions.

## Interactions

| Dependency        | Role                                                            |
| ----------------- | --------------------------------------------------------------- |
| `Context`         | Provides output channels and the optional results file path.    |
| `Program`         | Exercised by each self-test to validate real tool behavior.     |
| `PathHelpers`     | Used to build safe paths for temporary log files.               |
| `TrxSerializer`   | Serializes results to TRX format when `.trx` extension is used. |
| `JUnitSerializer` | Serializes results to JUnit XML when `.xml` extension is used.  |
