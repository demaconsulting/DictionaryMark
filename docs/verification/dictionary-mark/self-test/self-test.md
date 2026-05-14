# SelfTest Subsystem Verification

This document describes the subsystem-level verification design for the SelfTest subsystem.
It defines test scenarios, dependency usage, and requirement coverage for
`SelfTestSubsystemTests.cs`.

## Verification Approach

The SelfTest subsystem is verified through integration tests that exercise the full validation
workflow end-to-end. Tests create `Context` instances with validation arguments, invoke
`Validation.Run`, and assert on exit codes, results file presence, and file content.

## Dependencies

| Dependency   | Usage in Tests                                                      |
| ------------ | ------------------------------------------------------------------- |
| `Context`    | Created with validation arguments to drive the validation workflow. |
| `Validation` | Invoked directly to exercise the complete self-test workflow.       |

## Test Scenarios

### SelfTestSubsystem_ValidationWorkflow_NoResultFiles_CompletesSuccessfully

**Scenario**: `["--validate", "--silent"]` passed to `Context.Create`; `Validation.Run` called.

**Expected**: `context.Validate` is true; exit code 0.

### SelfTestSubsystem_ValidationWorkflow_WithTrxFile_GeneratesResults

**Scenario**: `["--validate", "--silent", "--results", trxFile]`; `Validation.Run` called.

**Expected**: TRX file created; content contains `"<TestRun"`; exit code 0.

### SelfTestSubsystem_ValidationWorkflow_WithJUnitFile_GeneratesResults

**Scenario**: `["--validate", "--silent", "--results", junitFile]`; `Validation.Run` called.

**Expected**: JUnit XML file created; content contains `"<testsuites"`; exit code 0.

### SelfTestSubsystem_ValidationWorkflow_WithBothResultFiles_GeneratesBothResults

**Scenario**: Two separate validation runs — one for TRX, one for JUnit XML.

**Expected**: Both files created with correct format content; both runs exit 0.

### SelfTestSubsystem_ValidationWorkflow_WithUnsupportedExtension_EmitsErrorAndNoFile

**Scenario**: `["--validate", "--results", badFile]` with `.bad` extension.

**Expected**: No file created; exit code 1; stderr contains `".bad"`.

## Requirements Coverage

- **`DictionaryMark-SelfTest-Subsystem`**: SelfTestSubsystem_ValidationWorkflow_NoResultFiles_CompletesSuccessfully,
  SelfTestSubsystem_ValidationWorkflow_WithTrxFile_GeneratesResults,
  SelfTestSubsystem_ValidationWorkflow_WithJUnitFile_GeneratesResults.
- **`DictionaryMark-Validation-TrxOutput`**: SelfTestSubsystem_ValidationWorkflow_WithTrxFile_GeneratesResults.
- **`DictionaryMark-Validation-JUnitOutput`**: SelfTestSubsystem_ValidationWorkflow_WithJUnitFile_GeneratesResults.
- **`DictionaryMark-Validation-UnsupportedExtension`**: SelfTestSubsystem_ValidationWorkflow_WithUnsupportedExtension_EmitsErrorAndNoFile.
