## xUnit

### Verification Approach

xUnit v3 (`xunit.v3` and `xunit.runner.visualstudio`) is the unit-testing framework used by the
project. It discovers and runs all test methods and writes TRX result files that feed into
requirements traceability via ReqStream. xUnit is verified by self-validation evidence from the CI
pipeline. Each scenario names a specific test method that xUnit must discover, execute, and record
in a TRX result file. A passing pipeline run for all scenarios constitutes evidence that xUnit
correctly discovers and executes test methods and produces TRX output. When this OTS dependency is
updated, the full CI pipeline is re-executed and all test scenarios must continue to pass before the
update is accepted.

### Test Scenarios

**Context_Create_NoArguments_ReturnsDefaultContext**: xUnit discovers and runs this test, which
verifies that `Context` default construction produces the expected default values. xUnit is expected
to execute the test, the test passes, and the result appears in the TRX output.
This scenario is tested by `Context_Create_NoArguments_ReturnsDefaultContext`.

**Context_Create_VersionFlag_SetsVersionTrue**: xUnit discovers and runs this test, which verifies
that passing the `--version` flag sets the `Version` property to true. xUnit is expected to execute
the test, the test passes, and the result appears in the TRX output.
This scenario is tested by `Context_Create_VersionFlag_SetsVersionTrue`.

**Context_Create_SilentFlag_SetsSilentTrue**: xUnit discovers and runs this test, which verifies
that passing the `--silent` flag sets the `Silent` property to true. xUnit is expected to execute
the test, the test passes, and the result appears in the TRX output.
This scenario is tested by `Context_Create_SilentFlag_SetsSilentTrue`.

**Context_Create_LogFlag_OpensLogFile**: xUnit discovers and runs this test, which verifies that
passing the `--log` flag opens a log file. xUnit is expected to execute the test, the test passes,
and the result appears in the TRX output.
This scenario is tested by `Context_Create_LogFlag_OpensLogFile`.

**Context_Create_UnknownArgument_ThrowsArgumentException**: xUnit discovers and runs this test,
which verifies that an unrecognized argument raises an exception. xUnit is expected to execute the
test, the test passes, and the result appears in the TRX output.
This scenario is tested by `Context_Create_UnknownArgument_ThrowsArgumentException`.

**PathHelpers_SafePathCombine_ValidPaths_CombinesCorrectly**: xUnit discovers and runs this test,
which verifies that `SafePathCombine` correctly joins valid path segments. xUnit is expected to
execute the test, the test passes, and the result appears in the TRX output.
This scenario is tested by `PathHelpers_SafePathCombine_ValidPaths_CombinesCorrectly`.

**Program_Run_WithVersionFlag_DisplaysVersionOnly**: xUnit discovers and runs this test, which
verifies that the program prints only version information when invoked with the `--version` flag.
xUnit is expected to execute the test, the test passes, and the result appears in the TRX output.
This scenario is tested by `Program_Run_WithVersionFlag_DisplaysVersionOnly`.

**Validation_Run_WithSilentContext_PrintsSummary**: xUnit discovers and runs this test, which
verifies that `Validation.Run` prints a summary even when the context is configured for silent
operation. xUnit is expected to execute the test, the test passes, and the result appears in the
TRX output.
This scenario is tested by `Validation_Run_WithSilentContext_PrintsSummary`.
