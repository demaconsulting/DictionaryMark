## Program

### Verification Approach

`Program` is verified through unit tests in `ProgramTests.cs`. Because `Program` directly
instantiates `Context` from real argument arrays and calls `Validation.Run` when the validate
flag is set, no mocking is required. Tests pass controlled argument arrays and assert on
captured standard output content and exit codes. The real collaborators (`Context`,
`Validation`, `DictionaryGenerator`) execute their full logic, which is appropriate for this
orchestration unit whose sole responsibility is dispatching to those collaborators based on
the parsed flags.

### Test Environment

N/A - standard test environment. Tests that write temporary YAML input files use
`TemporaryDirectory` for automatic cleanup.

### Acceptance Criteria

- All unit tests in `ProgramTests.cs` pass with zero failures.
- All unit requirements have at least one passing test scenario.
- No tests are skipped or marked as expected failures.

### Test Scenarios

**Program_Run_WithVersionFlag_DisplaysVersionOnly**: `Program.Run` is called with a context
created from `["--version"]`. Standard output must contain the version string; the word
`"Copyright"` must not appear; exit code must be zero. This confirms that the version flag
short-circuits the banner and dispatches to version-only output.
This scenario is tested by `Program_Run_WithVersionFlag_DisplaysVersionOnly`.

**Program_Run_WithHelpFlag_DisplaysUsageInformation**: `Program.Run` is called with a context
created from `["--help"]`. Standard output must contain `"Usage:"`, `"Options:"`,
`"--version"`, and `"--help"`; exit code must be zero. This confirms the help flag dispatches
to full usage output.
This scenario is tested by `Program_Run_WithHelpFlag_DisplaysUsageInformation`.

**Program_Run_WithValidateFlag_RunsValidation**: `Program.Run` is called with a context created
from `["--validate"]`. Standard output must contain `"Total Tests:"` and exit code must be zero,
confirming the validate flag dispatches to `Validation.Run`.
This scenario is tested by `Program_Run_WithValidateFlag_RunsValidation`.

**Program_Run_NoArguments_DisplaysDefaultBehavior**: `Program.Run` is called with a context
created from an empty argument array. Standard output must contain the tool name and copyright
notice and exit code must be zero, confirming the default path prints the banner and usage hint.
This scenario is tested by `Program_Run_NoArguments_DisplaysDefaultBehavior`.

**Program_Run_WithInputPatterns_InvokesDictionaryGeneration**: `Program.Run` is called with a
context created from `["--input", tmpFile]` where `tmpFile` is a temporary YAML file containing
a dictionary entry. Standard output must contain the entry term and exit code must be zero,
confirming the input flag dispatches to `DictionaryGenerator.Generate`.
This scenario is tested by `Program_Run_WithInputPatterns_InvokesDictionaryGeneration`.

**Program_Version_Read_ReturnsNonEmptyString**: The `Program.Version` static property is read.
The returned string must be non-null and non-empty, confirming the assembly version is resolved
from assembly attributes at runtime.
This scenario is tested by `Program_Version_Read_ReturnsNonEmptyString`.

**Program_Main_WithInvalidArgs_ReturnsNonZeroExitCode**: `Program.Main` is invoked with
`["--invalid-argument"]`. Exit code must be 1, confirming that `ArgumentException` from
`Context.Create` is caught and mapped to a non-zero exit code.
This scenario is tested by `Program_Main_WithInvalidArgs_ReturnsNonZeroExitCode`.

**Program_Run_WithShortVersionFlag_DisplaysVersion**: `Program.Run` is called with a context
created from `["-v"]`. Standard output must contain the version string and exit code must be
zero, confirming the short `-v` alias for `--version` is accepted.
This scenario is tested by `Program_Run_WithShortVersionFlag_DisplaysVersion`.

**Program_Run_WithShortHelpFlag_DisplaysUsage**: `Program.Run` is called with a context
created from `["-h"]`. Standard output must contain `"Usage:"` and `"Options:"` and exit code
must be zero, confirming the short `-h` alias for `--help` is accepted.
This scenario is tested by `Program_Run_WithShortHelpFlag_DisplaysUsage`.

**Program_Run_WithQuestionMarkFlag_DisplaysUsage**: `Program.Run` is called with a context
created from `["-?"]`. Standard output must contain `"Usage:"` and `"Options:"` and exit code
must be zero, confirming the `-?` alias for `--help` is accepted.
This scenario is tested by `Program_Run_WithQuestionMarkFlag_DisplaysUsage`.
