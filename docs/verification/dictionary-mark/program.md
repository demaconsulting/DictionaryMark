## Program Verification

This document describes the unit-level verification design for the `Program` unit. It defines
the test scenarios, dependency usage, and requirement coverage for `Program.cs`.

### Verification Strategy

`Program` is verified with unit tests defined in `ProgramTests.cs`. Because `Program` directly
instantiates `Context` from real arguments and calls `Validation.Run` when needed, no mocking is
required. The tests pass controlled argument arrays and assert on captured console output and exit
codes.

### Dependencies

| Dependency   | Usage in Tests                                                           |
|--------------|--------------------------------------------------------------------------|
| `Context`    | Used directly (not mocked) - created from the argument array under test. |
| `Validation` | Used directly (not mocked) - called when the validate flag is set.       |

No test doubles are introduced at the `Program` level; all collaborators execute their real logic.

### Test Environment

N/A - standard test environment. Tests that write temporary YAML input files use
`TemporaryDirectory` for automatic cleanup.

### Acceptance Criteria

All unit tests in `ProgramTests.cs` pass; all requirements listed in the Requirements Coverage
section have at least one passing test scenario; no tests may be skipped or marked as expected
failures.

### Test Scenarios

#### Program_Run_WithVersionFlag_DisplaysVersionOnly

**Scenario**: `Program.Run` is called with a context created from `["--version"]`.

**Expected**: Standard output contains the version string; the word "Copyright" does not appear;
the banner prefix "DictionaryMark version" does not appear; exit code is 0.

**Requirement coverage**: `DictionaryMark-Program-Version`, `DictionaryMark-Program-ExitCode`.

#### Program_Run_WithHelpFlag_DisplaysUsageInformation

**Scenario**: `Program.Run` is called with a context created from `["--help"]`.

**Expected**: Standard output contains "Usage:", "Options:", "--version", and "--help"; exit code
is 0.

**Requirement coverage**: `DictionaryMark-Program-Help`, `DictionaryMark-Program-ExitCode`.

#### Program_Run_WithValidateFlag_RunsValidation

**Scenario**: `Program.Run` is called with a context created from `["--validate"]`.

**Expected**: Standard output contains "Total Tests:"; exit code is 0.

**Requirement coverage**: `DictionaryMark-Program-Validate`, `DictionaryMark-Program-ExitCode`.

#### Program_Run_NoArguments_DisplaysDefaultBehavior

**Scenario**: `Program.Run` is called with a context created from an empty argument array.

**Expected**: Standard output contains the tool name and copyright notice; exit code is 0.

**Requirement coverage**: `DictionaryMark-Program-NoInputHint`, `DictionaryMark-Program-ExitCode`.

#### Program_Run_WithInputPatterns_InvokesDictionaryGeneration

**Scenario**: `Program.Run` is called with a context created from `["--input", tmpFile]` where
`tmpFile` is a temporary YAML file containing a dictionary entry.

**Expected**: Standard output contains the YAML entry term; exit code is 0.

**Requirement coverage**: `DictionaryMark-Program-GenerateDictionary`, `DictionaryMark-Program-ExitCode`.

#### Program_Version_Read_ReturnsNonEmptyString

**Scenario**: The `Program.Version` static property is read.

**Expected**: The returned string is non-empty and non-null.

**Requirement coverage**: `DictionaryMark-Program-Version`.

#### Program_Main_WithInvalidArgs_ReturnsNonZeroExitCode

**Scenario**: `Program.Main` is invoked with `["--invalid-argument"]`.

**Expected**: Exit code is 1.

**Requirement coverage**: `DictionaryMark-Program-ExitCode`.

#### Program_Run_WithShortVersionFlag_DisplaysVersion

**Scenario**: `Program.Run` is called with a context created from `["-v"]`.

**Expected**: Standard output contains the version string; exit code is 0.

**Requirement coverage**: `DictionaryMark-Program-Version`.

#### Program_Run_WithShortHelpFlag_DisplaysUsage

**Scenario**: `Program.Run` is called with a context created from `["-h"]`.

**Expected**: Standard output contains "Usage:" and "Options:"; exit code is 0.

**Requirement coverage**: `DictionaryMark-Program-Help`.

#### Program_Run_WithQuestionMarkFlag_DisplaysUsage

**Scenario**: `Program.Run` is called with a context created from `["-?"]`.

**Expected**: Standard output contains "Usage:" and "Options:"; exit code is 0.

**Requirement coverage**: `DictionaryMark-Program-Help`.

### Requirements Coverage

- **`DictionaryMark-Program-Version`**: Program_Run_WithVersionFlag_DisplaysVersionOnly,
  Program_Run_WithShortVersionFlag_DisplaysVersion,
  Program_Version_Read_ReturnsNonEmptyString
- **`DictionaryMark-Program-Help`**: Program_Run_WithHelpFlag_DisplaysUsageInformation,
  Program_Run_WithShortHelpFlag_DisplaysUsage,
  Program_Run_WithQuestionMarkFlag_DisplaysUsage
- **`DictionaryMark-Program-Validate`**: Program_Run_WithValidateFlag_RunsValidation
- **`DictionaryMark-Program-ExitCode`**: Program_Main_WithInvalidArgs_ReturnsNonZeroExitCode,
  Program_Run_WithVersionFlag_DisplaysVersionOnly,
  Program_Run_WithHelpFlag_DisplaysUsageInformation,
  Program_Run_WithValidateFlag_RunsValidation,
  Program_Run_NoArguments_DisplaysDefaultBehavior
- **`DictionaryMark-Program-GenerateDictionary`**: Program_Run_WithInputPatterns_InvokesDictionaryGeneration
- **`DictionaryMark-Program-NoInputHint`**: Program_Run_NoArguments_DisplaysDefaultBehavior
