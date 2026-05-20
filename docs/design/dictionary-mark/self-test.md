## SelfTest Subsystem Design

The SelfTest subsystem provides self-validation functionality for DictionaryMark. It enables
the tool to verify its own correct operation in the deployment environment, which is a
requirement for tool qualification in regulated environments.

### Overview

The SelfTest subsystem contains one unit, `Validation`, which implements the `--validate` CLI
behavior. It runs a fixed suite of six in-process self-tests, collects results into a structured
`TestResults` object, optionally serializes results to a file, and reports pass/fail counts via
the `Context` output channels. `Validation` has no persistent state and is safe to call multiple
times in the same process.

### Interfaces

**Exposed to the rest of the system:**

- `Validation.Run(Context context)` — static entry point; runs all self-tests and reports
  results. Calls `context.WriteError` for each failed test, which sets `context.ExitCode` to 1.

**Consumed from other items:**

- `Context` (CLI subsystem) — provides output channels (`WriteLine`, `WriteError`) and the
  optional `ResultsFile` path.
- `Program.Run(Context)` (Program unit) — called internally during each self-test to exercise
  the full tool execution path with a crafted argument array.
- `PathHelpers.SafePathCombine` (Utilities subsystem) — combines temporary directory paths with
  test file names while enforcing the path-traversal security boundary.
- `DemaConsulting.TestResults` (OTS) — collects `TestResult` objects and serializes them to TRX
  or JUnit XML when `context.ResultsFile` is set.

### Design

`Validation.Run` creates a `TestResults` collection, then calls six private test methods
sequentially (`RunVersionTest`, `RunHelpTest`, `RunBulletGenerationTest`,
`RunTableGenerationTest`, `RunCustomHeadersTest`, `RunConflictDetectionTest`). Each test method:

1. Creates a temporary directory via `Path.GetTempPath` / `PathHelpers.SafePathCombine`.
2. Constructs a string-array argument list and an in-memory `Context` (using `--silent`).
3. Calls `Program.Run(context)` to exercise the tool.
4. Asserts expected output or exit-code behavior; records a `Passed` or `Failed` result.

After all tests, `Validation.Run` prints totals, calls `context.WriteError` for any failures,
and writes the results file if `context.ResultsFile` is set (format inferred from extension).

### Validation

Executes a set of in-process self-tests and reports the results to the user via the `Context`
output channels. Optionally writes results to a file in TRX or JUnit XML format. The
self-tests cover the following categories of behavior:

- **Version display** — runs `--version` and verifies the output contains a version string.
- **Help display** — runs `--help` and verifies the output contains `Usage:` and `Options:`.
- **Bullet-list generation** — runs the dictionary pipeline with `--format bullets` and
  verifies the output contains the expected bold-term bullet entries.
- **Markdown table generation** — runs the dictionary pipeline with `--format table` and
  verifies the output contains the expected table header and entry rows.
- **Custom column headers** — runs the dictionary pipeline with `--term-header` and
  `--def-header` options and verifies custom header text appears in the output.
- **Conflict detection** — runs the dictionary pipeline with two files that define the same
  term differently and verifies the exit code is non-zero.

`Validation.Run` is a stateless operation: it creates temporary directories and contexts
internally and discards them after each test. It is safe to call sequentially multiple times
within the same process without any reset or re-initialization step.

### Interactions

The SelfTest subsystem is invoked by `Program.Run` when `context.Validate` is `true`. It
depends on:

| Dependency    | Role                                                         |
| ------------- | ------------------------------------------------------------ |
| `Context`     | Provides output channels and the optional results file path. |
| `Program`     | Called internally during self-tests to exercise tool logic.  |
| `PathHelpers` | Used to safely combine temporary directory and file paths.   |
| `TestResults` | Collects test outcomes for optional file-based reporting.    |
