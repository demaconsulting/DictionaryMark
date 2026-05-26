## Cli

### Verification Approach

The Cli subsystem is verified through integration tests in `CliSubsystemTests.cs` that exercise
`Context` and `Program` together. Tests pass controlled argument arrays to `Context.Create`,
then invoke `Program.Run` with the resulting context; captured console output and exit codes are
asserted. No mocking is applied at the subsystem boundary: all collaborators (`Context`,
`Program`, `Validation`, `DictionaryGenerator`) execute their real logic, exercising the
argument-parsing and output-routing paths in combination. Tests that write log or results files
use `TemporaryDirectory` to create and clean up temporary file-system fixtures automatically.

### Test Environment

N/A - standard test environment. Tests that write log or results files use `TemporaryDirectory`
for automatic cleanup.

### Acceptance Criteria

- All integration tests in `CliSubsystemTests.cs` pass with zero failures.
- All subsystem requirements have at least one passing test scenario.
- No tests are skipped or marked as expected failures.

### Test Scenarios

**CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits**: `Context.Create` is
called with `["--version"]` and `Program.Run` is invoked with the resulting context. The output
must contain the version string and exit code must be zero, confirming the version flag is
parsed and dispatched correctly.
This scenario is tested by `CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits`.

**CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits_WithShortVFlag**:
`Context.Create` is called with `["-v"]` and `Program.Run` is invoked. The short alias `-v`
must behave identically to `--version`, producing the version string with exit code zero.
This scenario is tested by
`CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits_WithShortVFlag`.

**CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits**: `Context.Create` is called
with `["--help"]` and `Program.Run` is invoked. The output must contain `"Usage:"` and
`"Options:"`, and the exit code must be zero.
This scenario is tested by `CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits`.

**CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortQuestionFlag**:
`Context.Create` is called with `["-?"]` and `Program.Run` is invoked. The `-?` alias must
produce the same help output as `--help` with exit code zero.
This scenario is tested by
`CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortQuestionFlag`.

**CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortHFlag**: `Context.Create`
is called with `["-h"]` and `Program.Run` is invoked. The `-h` alias must produce the same help
output as `--help` with exit code zero.
This scenario is tested by
`CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortHFlag`.

**CliSubsystem_ValidateFlow_ContextAndProgram_RunsValidationAndExits**: `Context.Create` is
called with `["--validate"]` and `Program.Run` is invoked. The output must contain
`"Total Tests:"`, confirming the validation workflow was executed, and exit code must be zero.
This scenario is tested by `CliSubsystem_ValidateFlow_ContextAndProgram_RunsValidationAndExits`.

**CliSubsystem_SilentFlow_ContextAndProgram_SuppressesOutput**: `Context.Create` is called with
`["--version", "--silent"]` and `Program.Run` is invoked. No console output must be produced
and exit code must be zero, confirming the silent flag suppresses all output.
This scenario is tested by `CliSubsystem_SilentFlow_ContextAndProgram_SuppressesOutput`.

**CliSubsystem_ResultsFlow_ContextAndProgram_WritesResultsFile**: `Context.Create` is called
with `["--validate", "--silent", "--results", trxFile]` and `Program.Run` is invoked. A results
file must be created at the specified path and exit code must be zero.
This scenario is tested by `CliSubsystem_ResultsFlow_ContextAndProgram_WritesResultsFile`.

**CliSubsystem_LogFlow_ContextAndProgram_WritesLogFile**: `Context.Create` is called with
`["--version", "--log", logFile]` and `Program.Run` is invoked. A log file must be created and
contain the version output, and exit code must be zero.
This scenario is tested by `CliSubsystem_LogFlow_ContextAndProgram_WritesLogFile`.

**CliSubsystem_InvalidArgs_ContextAndProgram_RejectsUnknownArgumentsAndExitsNonZero**:
`Program.Main` is invoked with `["--unknown-flag"]`. Exit code must be 1 and stderr must
contain `"--unknown-flag"`, confirming that unrecognized arguments are rejected.
This scenario is tested by
`CliSubsystem_InvalidArgs_ContextAndProgram_RejectsUnknownArgumentsAndExitsNonZero`.

**CliSubsystem_ErrorOutput_ContextAndProgram_WritesErrorToStderr**: `context.WriteError` is
called with `"Test error message"` on a default context. The message must appear on stderr and
exit code must be 1, confirming that error output sets the error flag.
This scenario is tested by `CliSubsystem_ErrorOutput_ContextAndProgram_WritesErrorToStderr`.

**CliSubsystem_ResultAliasFlow_ContextAndProgram_WritesResultsFile**: `Context.Create` is
called with `["--validate", "--silent", "--result", trxFile]` using the legacy `--result` alias.
A results file must be created and exit code must be zero, confirming the alias is accepted.
This scenario is tested by `CliSubsystem_ResultAliasFlow_ContextAndProgram_WritesResultsFile`.

**CliSubsystem_DepthFlow_ContextAndProgram_AdjustsHeadingDepth**: A YAML input file is written
to a temporary directory; `Context.Create` is called with `["--input", inputFile, "--section",
"CLI Depth", "--depth", "2", "--silent", "--log", logFile]` and `Program.Run` is invoked. The
log file must contain `"## CLI Depth"`, confirming that `--depth 2` produces a level-two
heading.
This scenario is tested by `CliSubsystem_DepthFlow_ContextAndProgram_AdjustsHeadingDepth`.
