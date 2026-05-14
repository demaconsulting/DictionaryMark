# CLI Subsystem Verification

This document describes the subsystem-level verification design for the CLI subsystem. It
defines test scenarios, dependency usage, and requirement coverage for CLI integration
workflows tested in `CliSubsystemTests.cs`.

## Verification Approach

The CLI subsystem is verified through integration tests that exercise `Context` and `Program`
together. Tests pass controlled argument arrays, capture console output, and assert on parsed
flags, output content, and exit codes. No mocking is used; all collaborators execute real logic.

## Dependencies

| Dependency   | Usage in Tests                                                           |
|--------------|--------------------------------------------------------------------------|
| `Context`    | Created from argument arrays to test flag parsing and output behavior.   |
| `Program`    | Invoked with created contexts to test end-to-end CLI dispatch.           |
| `Validation` | Called indirectly when the validate flag is passed.                      |

## Test Scenarios

### CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits

**Scenario**: `Context.Create(["--version"])` + `Program.Run`.

**Expected**: `context.Version` is true, output contains version string, exit code 0.

### CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits_WithShortVFlag

**Scenario**: `Context.Create(["-v"])` + `Program.Run`.

**Expected**: `context.Version` is true, output contains version string, exit code 0.

### CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits

**Scenario**: `Context.Create(["--help"])` + `Program.Run`.

**Expected**: `context.Help` is true, output contains "Usage:" and "Options:", exit code 0.

### CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortQuestionFlag

**Scenario**: `Context.Create(["-?"])` + `Program.Run`.

**Expected**: `context.Help` is true, output contains "Usage:" and "Options:", exit code 0.

### CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortHFlag

**Scenario**: `Context.Create(["-h"])` + `Program.Run`.

**Expected**: `context.Help` is true, output contains "Usage:" and "Options:", exit code 0.

### CliSubsystem_ValidateFlow_ContextAndProgram_RunsValidationAndExits

**Scenario**: `Context.Create(["--validate"])` + `Program.Run`.

**Expected**: `context.Validate` is true, output contains "Total Tests:", exit code 0.

### CliSubsystem_SilentFlow_ContextAndProgram_SuppressesOutput

**Scenario**: `Context.Create(["--version", "--silent"])` + `Program.Run`.

**Expected**: `context.Silent` is true, no console output produced, exit code 0.

### CliSubsystem_ResultsFlow_ContextAndProgram_WritesResultsFile

**Scenario**: `Context.Create(["--validate", "--silent", "--results", trxFile])` + `Program.Run`.

**Expected**: Results file is created at the specified path, exit code 0.

### CliSubsystem_LogFlow_ContextAndProgram_WritesLogFile

**Scenario**: `Context.Create(["--version", "--log", logFile])` + `Program.Run`.

**Expected**: Log file is created and contains version output, exit code 0.

### CliSubsystem_InvalidArgs_ContextAndProgram_RejectsUnknownArgumentsAndExitsNonZero

**Scenario**: `Program.Main(["--unknown-flag"])`.

**Expected**: Exit code 1, stderr contains "--unknown-flag".

### CliSubsystem_ErrorOutput_ContextAndProgram_WritesErrorToStderr

**Scenario**: `context.WriteError("Test error message")` on a default context.

**Expected**: "Test error message" appears on stderr, exit code 1.

### CliSubsystem_ResultAliasFlow_ContextAndProgram_WritesResultsFile

**Scenario**: `Context.Create(["--validate", "--silent", "--result", trxFile])` + `Program.Run`.

**Expected**: Legacy `--result` alias accepted; results file created, exit code 0.

### CliSubsystem_DepthFlow_ContextAndProgram_AdjustsHeadingDepth

**Scenario**: `Context.Create(["--validate", "--silent", "--depth", "2", "--log", logFile])` + `Program.Run`.

**Expected**: `context.HeadingDepth` is 2, log contains "## DEMA Consulting", exit code 0.

## Requirements Coverage

- **`DictionaryMark-Cli-Context`**: All CliSubsystem tests exercise `Context.Create` flag
  parsing as part of the end-to-end workflow.
- **`DictionaryMark-Program-Version`**: CliSubsystem_VersionFlow_* tests.
- **`DictionaryMark-Program-Help`**: CliSubsystem_HelpFlow_* tests.
- **`DictionaryMark-Program-Validate`**: CliSubsystem_ValidateFlow_ContextAndProgram_RunsValidationAndExits.
- **`DictionaryMark-SelfTest-Subsystem`**: CliSubsystem_ResultsFlow_ContextAndProgram_WritesResultsFile.
