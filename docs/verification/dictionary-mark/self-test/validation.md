### Validation

The `Validation` unit is verified with unit tests in `ValidationTests.cs`. Tests create
`Context` instances in silent mode and invoke `Validation.Run`, asserting on exit codes, log
file content, and results file content.

#### Verification Approach

`Validation` is tested with its real dependencies. No mocking is applied:

- **`Context`** — Created from argument arrays in silent mode to suppress console noise during
  tests. Log files are captured via `TemporaryDirectory` to assert on written content and
  summary lines.
- **`Program`** — Exercised directly by `Validation.Run`, which calls `Program.Run` internally
  for each self-test. The full tool execution path is exercised, not a stub.
- **`TemporaryDirectory`** — Used by `Validation` itself during each self-test run, and also
  used by the test infrastructure to hold log and results files for assertion.

#### Test Environment

N/A - standard test environment. Tests that write log or results files use `TemporaryDirectory`
for automatic cleanup.

#### Acceptance Criteria

- All unit tests in `ValidationTests.cs` pass with zero failures.
- All requirements linked to the `Validation` unit have at least one passing test scenario.
- No tests may be skipped or marked as expected failures.

#### Test Scenarios

**Validation_Run_NullContext_ThrowsArgumentNullException**: Verifies that calling
`Validation.Run(null!)` throws `ArgumentNullException`, enforcing the precondition that a
valid context must be supplied. This scenario is tested by
`Validation_Run_NullContext_ThrowsArgumentNullException`.

**Validation_Run_WithSilentContext_PrintsSummary**: Verifies that after `Validation.Run`
completes, the log file contains "Total Tests:", "Passed:", and "Failed:", confirming that the
summary block is always written. This scenario is tested by
`Validation_Run_WithSilentContext_PrintsSummary`.

**Validation_Run_WithSilentContext_ExitCodeIsZero**: Verifies that `Validation.Run` completes
with exit code 0 when all self-tests pass, confirming no spurious error conditions are
introduced. This scenario is tested by `Validation_Run_WithSilentContext_ExitCodeIsZero`.

**Validation_Run_WithSilentContext_VersionDisplayTestPasses**: Verifies that the version-display
self-test passes by checking that the log file contains
"DictionaryMark_VersionDisplay - Passed". This scenario is tested by
`Validation_Run_WithSilentContext_VersionDisplayTestPasses`.

**Validation_Run_WithSilentContext_HelpDisplayTestPasses**: Verifies that the help-display
self-test passes by checking that the log file contains "DictionaryMark_HelpDisplay - Passed".
This scenario is tested by `Validation_Run_WithSilentContext_HelpDisplayTestPasses`.

**Validation_Run_WithSilentContext_BulletGenerationTestPasses**: Verifies that the bullet
generation self-test passes by checking that the log file contains
"DictionaryMark_BulletGeneration - Passed". This scenario is tested by
`Validation_Run_WithSilentContext_BulletGenerationTestPasses`.

**Validation_Run_WithSilentContext_TableGenerationTestPasses**: Verifies that the table
generation self-test passes by checking that the log file contains
"DictionaryMark_TableGeneration - Passed". This scenario is tested by
`Validation_Run_WithSilentContext_TableGenerationTestPasses`.

**Validation_Run_WithSilentContext_CustomHeadersTestPasses**: Verifies that the custom-headers
self-test passes by checking that the log file contains
"DictionaryMark_CustomHeaders - Passed". This scenario is tested by
`Validation_Run_WithSilentContext_CustomHeadersTestPasses`.

**Validation_Run_WithSilentContext_ConflictDetectionTestPasses**: Verifies that the conflict
detection self-test passes by checking that the log file contains
"DictionaryMark_ConflictDetection - Passed". This scenario is tested by
`Validation_Run_WithSilentContext_ConflictDetectionTestPasses`.

**Validation_Run_WithTrxResultsFile_WritesTrxFile**: Verifies that when `--results` points to a
`.trx` file, `Validation.Run` creates a valid TRX results file containing `"<TestRun"`. This
scenario is tested by `Validation_Run_WithTrxResultsFile_WritesTrxFile`.

**Validation_Run_WithXmlResultsFile_WritesXmlFile**: Verifies that when `--results` points to
a `.xml` file, `Validation.Run` creates a valid JUnit XML results file containing
`"<testsuites"`. This scenario is tested by `Validation_Run_WithXmlResultsFile_WritesXmlFile`.

**Validation_Run_WithUnsupportedResultsFormat_DoesNotWriteFile**: Verifies that when `--results`
points to a file with an unsupported extension (e.g. `.json`), no results file is created, exit
code becomes 1, and the log contains "Unsupported results file format". This scenario is tested
by `Validation_Run_WithUnsupportedResultsFormat_DoesNotWriteFile`.
