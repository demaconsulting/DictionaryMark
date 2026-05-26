## TestResults

### Verification Approach

DemaConsulting.TestResults accumulates per-test outcomes during the DictionaryMark self-validation
run and serializes them to TRX or JUnit XML output files. DictionaryMark uses `TestResults`,
`TestResult`, `TrxSerializer`, and `JUnitSerializer` from the `DemaConsulting.TestResults` and
`DemaConsulting.TestResults.IO` namespaces, all called through `Validation`. TestResults is
verified indirectly by the `ValidationTests` unit tests. Each scenario drives `Validation.Run` with
a specific context configuration and asserts the expected result-file content or error behavior.
Because `Validation` delegates all result accumulation and serialization to
DemaConsulting.TestResults, correct test outcomes constitute evidence that the required TestResults
APIs function as specified. No separate qualification testing is required. When this OTS dependency
is updated, the full CI pipeline is re-executed and all test scenarios must continue to pass before
the update is accepted.

### Test Scenarios

**Validation_Run_WithTrxResultsFile_WritesTrxFile**: `Validation.Run` is invoked with a context
whose `ResultsFile` path ends in `.trx`, verifying that `TrxSerializer.Serialize` correctly writes
a valid TRX document from the accumulated `TestResults` instance. A TRX file containing a
`<TestRun` element is expected at the specified path.
This scenario is tested by `Validation_Run_WithTrxResultsFile_WritesTrxFile`.

**Validation_Run_WithXmlResultsFile_WritesXmlFile**: `Validation.Run` is invoked with a context
whose `ResultsFile` path ends in `.xml`, verifying that `JUnitSerializer.Serialize` correctly
writes a valid JUnit XML document from the accumulated `TestResults` instance. An XML file
containing a `<testsuites` element is expected at the specified path.
This scenario is tested by `Validation_Run_WithXmlResultsFile_WritesXmlFile`.

**Validation_Run_WithUnsupportedResultsFormat_DoesNotWriteFile**: `Validation.Run` is invoked with
a context whose `ResultsFile` path ends in an unsupported extension, verifying that the
extension-dispatch layer correctly rejects unknown formats without invoking any serializer. No
results file is expected to be created; the exit code is set to 1 and an error message containing
"Unsupported results file format" is written to the log.
This scenario is tested by `Validation_Run_WithUnsupportedResultsFormat_DoesNotWriteFile`.

**Validation_Run_WithSilentContext_PrintsSummary**: `Validation.Run` is invoked with a silent
context and a log file, verifying that `TestResults.Add` correctly accumulates individual
`TestResult` entries and that the summary output reflects the collected data. The log file is
expected to contain lines for "Total Tests:", "Passed:", and "Failed:".
This scenario is tested by `Validation_Run_WithSilentContext_PrintsSummary`.
