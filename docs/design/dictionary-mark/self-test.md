## SelfTest Subsystem Design

The SelfTest subsystem provides self-validation functionality for DictionaryMark. It enables
the tool to verify its own correct operation in the deployment environment, which is a
requirement for tool qualification in regulated environments.

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
