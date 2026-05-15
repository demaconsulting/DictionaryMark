## SelfTest Subsystem Design

The SelfTest subsystem provides self-validation functionality for DictionaryMark. It enables
the tool to verify its own correct operation in the deployment environment, which is a
requirement for tool qualification in regulated environments.

### Validation

Executes a set of in-process self-tests (version display, help display) and reports the
results to the user via the `Context` output channels. Optionally writes results to a file
in TRX or JUnit XML format.

### Interactions

The SelfTest subsystem is invoked by `Program.Run` when `context.Validate` is `true`. It
depends on:

| Dependency    | Role                                                         |
| ------------- | ------------------------------------------------------------ |
| `Context`     | Provides output channels and the optional results file path. |
| `Program`     | Called internally during self-tests to exercise tool logic.  |
| `PathHelpers` | Used to safely combine temporary directory and file paths.   |
| `TestResults` | Collects test outcomes for optional file-based reporting.    |
