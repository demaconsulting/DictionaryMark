## SelfTest

### Verification Approach

The SelfTest subsystem is verified through integration tests in `SelfTestSubsystemTests.cs`
that exercise the full validation workflow end-to-end. Tests create `Context` instances with
validation arguments and invoke `Validation.Run` directly, asserting on exit codes, results
file presence, and file content format. No mocking is applied: `Context` and `Validation`
execute their real logic, and the in-process self-tests within `Validation` call `Program.Run`
with crafted argument arrays as they do in production. Tests that write TRX or JUnit XML
results files use `TemporaryDirectory` for automatic cleanup.

### Test Environment

N/A - standard test environment. Tests that write TRX or JUnit XML results files use
`TemporaryDirectory` for automatic cleanup.

### Acceptance Criteria

- All integration tests in `SelfTestSubsystemTests.cs` pass with exit code zero.
- All subsystem requirements have at least one passing test scenario.
- No tests are skipped or marked as expected failures.

### Test Scenarios

**SelfTestSubsystem_ValidationWorkflow_NoResultFiles_CompletesSuccessfully**: `["--validate",
"--silent"]` is passed to `Context.Create` and `Validation.Run` is called. `context.Validate`
must be true and exit code must be zero, confirming the validation workflow completes
successfully with no results file output.
This scenario is tested by
`SelfTestSubsystem_ValidationWorkflow_NoResultFiles_CompletesSuccessfully`.

**SelfTestSubsystem_ValidationWorkflow_WithTrxFile_GeneratesResults**: `["--validate",
"--silent", "--results", trxFile]` is passed to `Context.Create` and `Validation.Run` is
called. A TRX file must be created at the specified path containing `"<TestRun"` and exit code
must be zero, confirming TRX serialization is functional.
This scenario is tested by `SelfTestSubsystem_ValidationWorkflow_WithTrxFile_GeneratesResults`.

**SelfTestSubsystem_ValidationWorkflow_WithJUnitFile_GeneratesResults**: `["--validate",
"--silent", "--results", junitFile]` is passed to `Context.Create` and `Validation.Run` is
called. A JUnit XML file must be created at the specified path containing `"<testsuites"` and
exit code must be zero, confirming JUnit XML serialization is functional.
This scenario is tested by
`SelfTestSubsystem_ValidationWorkflow_WithJUnitFile_GeneratesResults`.

**SelfTestSubsystem_ValidationWorkflow_WithBothResultFiles_GeneratesBothResults**: Two
separate validation runs are executed, one targeting a TRX file and one targeting a JUnit XML
file. Both files must be created with correct format content and both runs must exit with code
zero, confirming `Validation.Run` is stateless and safe to call sequentially without reset.
This scenario is tested by
`SelfTestSubsystem_ValidationWorkflow_WithBothResultFiles_GeneratesBothResults`.

**SelfTestSubsystem_ValidationWorkflow_WithUnsupportedExtension_EmitsErrorAndNoFile**:
`["--validate", "--results", badFile]` is passed with a `.bad` extension. No file must be
created, exit code must be 1, and stderr must contain `".bad"`, confirming that unsupported
results file extensions are rejected with a meaningful error.
This scenario is tested by
`SelfTestSubsystem_ValidationWorkflow_WithUnsupportedExtension_EmitsErrorAndNoFile`.
