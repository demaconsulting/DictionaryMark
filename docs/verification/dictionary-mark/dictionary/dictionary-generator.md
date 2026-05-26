### DictionaryGenerator

#### Verification Approach

`DictionaryGenerator` is a `static` class that orchestrates the full dictionary pipeline by coordinating
`GlobMatcher`, `YamlDictionaryLoader`, `ConflictDetector`, and `MarkdownFormatter`. Tests supply a
`Context` constructed from controlled argument arrays and write temporary YAML files to disk using
`TemporaryDirectory`. Console standard output and standard error are redirected with `StringWriter`
instances to capture generated output and error messages. All collaborating units (`GlobMatcher`,
`YamlDictionaryLoader`, `ConflictDetector`, and `MarkdownFormatter`) run with their real implementations
against real files; no mocking or stubbing is used.

#### Test Environment

Tests write temporary YAML files using `TemporaryDirectory` for automatic cleanup after each test. The
test class is marked `[Collection("Sequential")]` to prevent concurrent Console stream redirection from
interfering between tests. No other environment setup is required beyond the standard test runner.

#### Acceptance Criteria

- All unit tests in `DictionaryGeneratorTests.cs` pass with zero failures.
- No tests are skipped or marked as expected failures.

#### Test Scenarios

**DictionaryGenerator_Generate_SingleYamlFile_WritesToStdout**: Verifies that `Generate` called with a
single YAML input file writes formatted Markdown containing the entry term to standard output and exits
with code 0, confirming the normal end-to-end pipeline execution. This scenario is tested by
`DictionaryGenerator_Generate_SingleYamlFile_WritesToStdout`.

**DictionaryGenerator_Generate_ConflictingEntries_ReportsError**: Verifies that when two input files
define the same term with different definitions, `Generate` writes an error message to standard error,
sets exit code 1, and produces no Markdown output, confirming the conflict-reporting early-exit path.
This scenario is tested by `DictionaryGenerator_Generate_ConflictingEntries_ReportsError`.

**DictionaryGenerator_Generate_NoInputPatterns_ReportsError**: Verifies that when no `--input` flags
are provided, `Generate` reports a "No input files found" error to standard error and sets exit code 1,
confirming the empty-pattern error path. This scenario is tested by
`DictionaryGenerator_Generate_NoInputPatterns_ReportsError`.

**DictionaryGenerator_Generate_OutputFile_WritesToFile**: Verifies that when an `--output` file path is
specified, `Generate` creates the output file containing formatted Markdown and exits with code 0,
confirming the file-output routing path as an alternative to writing to standard output. This scenario
is tested by `DictionaryGenerator_Generate_OutputFile_WritesToFile`.
