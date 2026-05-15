### Validation Verification

This document describes the unit-level verification design for the `Validation` unit. It
defines test scenarios, dependency usage, and requirement coverage for `ValidationTests.cs`.

#### Verification Approach

`Validation` is verified with unit tests in `ValidationTests.cs`. Tests create `Context`
instances in silent mode (with optional log files) to avoid console noise, invoke
`Validation.Run`, and assert on exit codes, log file content, and results file content.

#### Dependencies

| Dependency  | Usage in Tests                                                                              |
| ----------- | ------------------------------------------------------------------------------------------- |
| `Context`   | Created from argument arrays; exit code and log output are asserted.                        |
| File system | Temporary log and results files created via `TemporaryDirectory` to verify written content. |

#### Test Scenarios

##### Validation_Run_NullContext_ThrowsArgumentNullException

**Scenario**: `Validation.Run(null!)` is called.

**Expected**: `ArgumentNullException` is thrown.

##### Validation_Run_WithSilentContext_PrintsSummary

**Scenario**: `Validation.Run` is called with a silent context that has a log file.

**Expected**: Log file contains "Total Tests:", "Passed:", and "Failed:".

##### Validation_Run_WithSilentContext_ExitCodeIsZero

**Scenario**: `Validation.Run` is called with `["--silent"]`.

**Expected**: Exit code is 0.

##### Validation_Run_WithSilentContext_VersionDisplayTestPasses

**Scenario**: `Validation.Run` is called with `["--silent", "--log", logFile]`.

**Expected**: Log file contains "DictionaryMark_VersionDisplay - Passed".

##### Validation_Run_WithSilentContext_HelpDisplayTestPasses

**Scenario**: `Validation.Run` is called with `["--silent", "--log", logFile]`.

**Expected**: Log file contains "DictionaryMark_HelpDisplay - Passed".

##### Validation_Run_WithSilentContext_BulletGenerationTestPasses

**Scenario**: `Validation.Run` is called with `["--silent", "--log", logFile]`.

**Expected**: Log file contains "DictionaryMark_BulletGeneration - Passed".

##### Validation_Run_WithSilentContext_TableGenerationTestPasses

**Scenario**: `Validation.Run` is called with `["--silent", "--log", logFile]`.

**Expected**: Log file contains "DictionaryMark_TableGeneration - Passed".

##### Validation_Run_WithSilentContext_CustomHeadersTestPasses

**Scenario**: `Validation.Run` is called with `["--silent", "--log", logFile]`.

**Expected**: Log file contains "DictionaryMark_CustomHeaders - Passed".

##### Validation_Run_WithSilentContext_ConflictDetectionTestPasses

**Scenario**: `Validation.Run` is called with `["--silent", "--log", logFile]`.

**Expected**: Log file contains "DictionaryMark_ConflictDetection - Passed".

##### Validation_Run_WithTrxResultsFile_WritesTrxFile

**Scenario**: `Validation.Run` is called with `["--silent", "--results", trxFile]`.

**Expected**: TRX file is created and contains `"<TestRun"`.

##### Validation_Run_WithXmlResultsFile_WritesXmlFile

**Scenario**: `Validation.Run` is called with `["--silent", "--results", xmlFile]`.

**Expected**: XML file is created and contains `"<testsuites"`.

##### Validation_Run_WithUnsupportedResultsFormat_DoesNotWriteFile

**Scenario**: `Validation.Run` is called with `["--silent", "--results", jsonFile]`.

**Expected**: No results file created; exit code 1; log contains "Unsupported results file format".

#### Requirements Coverage

- **`DictionaryMark-Validation-Run`**: Validation_Run_WithSilentContext_PrintsSummary,
  Validation_Run_WithSilentContext_ExitCodeIsZero,
  Validation_Run_NullContext_ThrowsArgumentNullException.
- **`DictionaryMark-Validation-VersionDisplay`**: Validation_Run_WithSilentContext_VersionDisplayTestPasses.
- **`DictionaryMark-Validation-HelpDisplay`**: Validation_Run_WithSilentContext_HelpDisplayTestPasses.
- **`DictionaryMark-Validation-BulletGeneration`**: Validation_Run_WithSilentContext_BulletGenerationTestPasses.
- **`DictionaryMark-Validation-TableGeneration`**: Validation_Run_WithSilentContext_TableGenerationTestPasses.
- **`DictionaryMark-Validation-CustomHeaders`**: Validation_Run_WithSilentContext_CustomHeadersTestPasses.
- **`DictionaryMark-Validation-ConflictDetection`**: Validation_Run_WithSilentContext_ConflictDetectionTestPasses.
- **`DictionaryMark-Validation-TrxOutput`**: Validation_Run_WithTrxResultsFile_WritesTrxFile.
- **`DictionaryMark-Validation-JUnitOutput`**: Validation_Run_WithXmlResultsFile_WritesXmlFile.
- **`DictionaryMark-Validation-UnsupportedExtension`**: Validation_Run_WithUnsupportedResultsFormat_DoesNotWriteFile.
