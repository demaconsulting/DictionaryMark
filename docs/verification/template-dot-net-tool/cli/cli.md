# Cli Subsystem Verification

This document describes the subsystem-level verification design for the `Cli` subsystem. It
defines the integration test approach, subsystem boundary, mocking strategy, and test scenarios
that together verify the `Cli` subsystem requirements.

## Verification Approach

The `Cli` subsystem boundary at `Program` is verified by integration tests defined in
`CliSubsystemTests.cs`. Each test exercises `Context.Create` and `Program.Run` together, treating
the pair as the observable subsystem interface. Tests pass controlled argument arrays and assert
on captured console output, file system side-effects, and exit codes.

## Dependencies and Mocking Strategy

At the subsystem boundary, `Validation` (part of the `SelfTest` subsystem) is the only external
collaborator that `Program` calls. In scenarios that exercise the `--validate` path, `Validation`
executes its real logic rather than being stubbed. Scenarios that do not involve `--validate` do
not reach `Validation` at all.

No mocking is applied at this level; all collaborators within and directly adjacent to the
subsystem use their real implementations.

## Integration Test Scenarios

The following integration test scenarios are defined in `CliSubsystemTests.cs`.

### CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits

**Scenario**: Arguments `["--version"]` are passed through `Context.Create` and `Program.Run`.

**Expected**: Standard output contains the version string; exit code is 0.

### CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits_WithShortVFlag

**Scenario**: Arguments `["-v"]` are passed through `Context.Create` and `Program.Run`.

**Expected**: Standard output contains the version string; exit code is 0.

### CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits

**Scenario**: Arguments `["--help"]` are passed through `Context.Create` and `Program.Run`.

**Expected**: Standard output contains help text; exit code is 0.

### CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortQuestionFlag

**Scenario**: Arguments `["-?"]` are passed through `Context.Create` and `Program.Run`.

**Expected**: Standard output contains help text; exit code is 0.

### CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortHFlag

**Scenario**: Arguments `["-h"]` are passed through `Context.Create` and `Program.Run`.

**Expected**: Standard output contains help text; exit code is 0.

### CliSubsystem_ValidateFlow_ContextAndProgram_RunsValidationAndExits

**Scenario**: Arguments `["--validate"]` are passed through `Context.Create` and `Program.Run`.

**Expected**: Standard output contains `"Total Tests:"`; exit code is 0.

### CliSubsystem_SilentFlow_ContextAndProgram_SuppressesOutput

**Scenario**: Arguments `["--silent"]` are passed through `Context.Create` and `Program.Run`.

**Expected**: Standard output is empty; exit code is 0.

### CliSubsystem_ResultsFlow_ContextAndProgram_WritesResultsFile

**Scenario**: Arguments `["--validate", "--results", "<tmp>.trx"]` are passed through
`Context.Create` and `Program.Run`.

**Expected**: A results file is created at the specified path; exit code is 0.

### CliSubsystem_LogFlow_ContextAndProgram_WritesLogFile

**Scenario**: Arguments `["--log", "<tmp>.log"]` are passed through `Context.Create` and
`Program.Run`.

**Expected**: A log file is created at the specified path; exit code is 0.

### CliSubsystem_InvalidArgs_ContextAndProgram_RejectsUnknownArgumentsAndExitsNonZero

**Scenario**: The private `Program.Main` entry point is invoked via reflection with
`["--unknown-flag"]`. This exercises the full argument-parsing and error-reporting path
including the `ArgumentException` handler in `Main`.

**Expected**: Exit code is 1; standard error contains an error message including the unknown flag.

### CliSubsystem_ErrorOutput_ContextAndProgram_WritesErrorToStderr

**Scenario**: A `Context` is created with no arguments. `WriteError` is called directly with a
known message string.

**Expected**: Standard error receives the error message; exit code is non-zero.

### CliSubsystem_ResultAliasFlow_ContextAndProgram_WritesResultsFile

**Scenario**: Arguments `["--validate", "--result", "<tmp>.trx"]` (legacy alias) are passed
through `Context.Create` and `Program.Run`.

**Expected**: A results file is created at the specified path; exit code is 0.

### CliSubsystem_DepthFlow_ContextAndProgram_AdjustsHeadingDepth

**Scenario**: Arguments `["--depth", "2"]` are passed through `Context.Create` and `Program.Run`.

**Expected**: The heading depth is set to the specified value; exit code is 0.

## Requirements Coverage

- **`Template-Cli-Version`**: CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits,
  CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits_WithShortVFlag
- **`Template-Cli-Help`**: CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits,
  CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortQuestionFlag,
  CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortHFlag
- **`Template-Cli-Validate`**: CliSubsystem_ValidateFlow_ContextAndProgram_RunsValidationAndExits
- **`Template-Cli-Silent`**: CliSubsystem_SilentFlow_ContextAndProgram_SuppressesOutput
- **`Template-Cli-Results`**: CliSubsystem_ResultsFlow_ContextAndProgram_WritesResultsFile
- **`Template-Cli-Log`**: CliSubsystem_LogFlow_ContextAndProgram_WritesLogFile
- **`Template-Cli-InvalidArgs`**: CliSubsystem_InvalidArgs_ContextAndProgram_RejectsUnknownArgumentsAndExitsNonZero
- **`Template-Cli-ErrorOutput`**: CliSubsystem_ErrorOutput_ContextAndProgram_WritesErrorToStderr
- **`Template-Cli-ArgumentParsing`**: CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits,
  CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits,
  CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortQuestionFlag,
  CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortHFlag,
  CliSubsystem_ValidateFlow_ContextAndProgram_RunsValidationAndExits
- **`Template-Cli-OutputChannels`**: CliSubsystem_SilentFlow_ContextAndProgram_SuppressesOutput,
  CliSubsystem_ErrorOutput_ContextAndProgram_WritesErrorToStderr
- **`Template-Cli-ExitCode`**: CliSubsystem_InvalidArgs_ContextAndProgram_RejectsUnknownArgumentsAndExitsNonZero
- **`Template-Cli-Depth`**: CliSubsystem_DepthFlow_ContextAndProgram_AdjustsHeadingDepth
- **`Template-Cli-ResultAlias`**: CliSubsystem_ResultAliasFlow_ContextAndProgram_WritesResultsFile
