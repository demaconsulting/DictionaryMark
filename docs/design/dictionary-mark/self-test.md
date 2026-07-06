## SelfTest

![SelfTest Subsystem Structure](SelfTestView.svg)

### Overview

The SelfTest subsystem provides self-validation functionality for DictionaryMark. It enables the
tool to verify its own correct operation in the deployment environment, which is a requirement for
tool qualification in regulated environments. The subsystem contains one unit, `Validation`, which
implements the `--validate` CLI behavior. It runs a fixed suite of six in-process self-tests,
collects results into a `TestResults` object, optionally serializes results to a file, and reports
pass/fail counts. `Validation` has no persistent state and is safe to call multiple times in the
same process.

### Interfaces

**Validation.Run**: Static entry point for the self-validation suite.

- *Type*: In-process .NET public API
- *Role*: Provider — `Program.Run` invokes this when `--validate` is set
- *Contract*: `Validation.Run(Context context)` runs all six self-tests, prints a summary, and
  optionally writes a results file. Calls `context.WriteError` for each failed test, setting
  `context.ExitCode` to 1.
- *Constraints*: `context` must be non-null; throws `ArgumentNullException` otherwise.

**Context**: Output and results file path from the Cli subsystem.

- *Type*: In-process .NET public API
- *Role*: Consumer — `Validation` calls `context.WriteLine`, `context.WriteError`, and reads
  `context.ResultsFile`
- *Contract*: `WriteLine(string)`, `WriteError(string)`, `ResultsFile` property (string? path to
  results file).
- *Constraints*: N/A

**Program.Run**: Full tool execution entry point from the Program unit.

- *Type*: In-process .NET public API
- *Role*: Consumer — each self-test calls `Program.Run` with a crafted context to exercise the
  full tool path
- *Contract*: `Program.Run(Context context)` executes the tool with the supplied context.
- *Constraints*: N/A

**TemporaryDirectory**: Disposable temporary directory helper from the Utilities subsystem.

- *Type*: In-process .NET public API
- *Role*: Consumer — each self-test creates a `TemporaryDirectory` for isolated working files
- *Contract*: Constructor creates a temp directory; `GetFilePath(string)` returns safe paths
  inside it; `Dispose()` cleans up.
- *Constraints*: Must be used in a `using` block to ensure cleanup.

**DemaConsulting.TestResults**: OTS test result collection and serialization package.

- *Type*: In-process .NET public API (NuGet package)
- *Role*: Consumer — used to collect self-test outcomes and serialize to TRX or JUnit XML
- *Contract*: `TestResults`, `TestResult`, `TrxSerializer`, `JUnitSerializer` APIs.
- *Constraints*: See *DemaConsulting.TestResults Integration Design*.

### Design

`Validation.Run` creates a `TestResults` collection, then calls six private test methods
sequentially: `RunVersionTest`, `RunHelpTest`, `RunBulletGenerationTest`,
`RunTableGenerationTest`, `RunCustomHeadersTest`, `RunConflictDetectionTest`. Each test method:

1. Creates a `TemporaryDirectory` scoped to the test.
2. Constructs a string-array argument list and an in-memory `Context` (with `--silent`).
3. Calls `Program.Run(context)` to exercise the tool.
4. Asserts expected output or exit-code behavior; records a `Passed` or `Failed` `TestResult`.

After all tests complete, `Validation.Run` prints pass/fail totals, calls `context.WriteError`
for each failure, and writes the results file if `context.ResultsFile` is set (format inferred
from extension: `.trx` for TRX, `.xml` for JUnit XML).
