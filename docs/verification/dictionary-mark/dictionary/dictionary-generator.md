### DictionaryGenerator

#### Verification Approach

`DictionaryGenerator` is a `static` class that orchestrates the full dictionary pipeline by coordinating
`GlobMatcher`, `YamlDictionaryLoader`, `ConflictDetector`, and `MarkdownFormatter`. Tests supply a
`Context` constructed from controlled argument arrays and write temporary YAML files to disk using
`TemporaryDirectory`. Console standard output and standard error are redirected with `StringWriter`
instances to capture generated output and error messages. All collaborating units (`GlobMatcher`,
`PathResolver`, `YamlDictionaryLoader`, `ConflictDetector`, and `MarkdownFormatter`) run with their real
implementations against real files; no mocking or stubbing is used. Multi-job scenarios build their jobs
through `Context` and `TableJob` (documented dependencies) from a real configuration file.

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

**DictionaryGenerator_Generate_HiddenEntry_OmittedFromOutput**: A `visible: false` entry is omitted
from output. This scenario is tested by `DictionaryGenerator_Generate_HiddenEntry_OmittedFromOutput`.

**DictionaryGenerator_Generate_HiddenEntryWithoutDefinition_ReportsError**: A hidden entry without a
definition is an error, since a definition is required regardless of visibility. This scenario is
tested by `DictionaryGenerator_Generate_HiddenEntryWithoutDefinition_ReportsError`.

**DictionaryGenerator_Generate_DeprecatedKind_WritesTable**: `--kind deprecated` writes the
deprecated table. This scenario is tested by `DictionaryGenerator_Generate_DeprecatedKind_WritesTable`.

**DictionaryGenerator_Generate_DeprecatedKindCustomHeaders_AreUsed**: Custom deprecated headers are
used. This scenario is tested by `DictionaryGenerator_Generate_DeprecatedKindCustomHeaders_AreUsed`.

**DictionaryGenerator_Generate_MissingInputs_ReportErrors**: Unmatched globs and missing files or
directories are reported with distinct errors. This scenario is tested by
`DictionaryGenerator_Generate_MissingInputs_ReportErrors`.

**DictionaryGenerator_Generate_OneMissingPattern_Fails**: One unmatched pattern among several fails
the run. This scenario is tested by `DictionaryGenerator_Generate_OneMissingPattern_Fails`.

**DictionaryGenerator_Generate_MultipleJobs_WritesEachOutput**: Verifies that a configuration file with
two jobs writes both outputs, each with its own content and format. This scenario is tested by
`DictionaryGenerator_Generate_MultipleJobs_WritesEachOutput`.

**DictionaryGenerator_Generate_OneJobFails_ContinuesAndReportsJobLabel**: Verifies that when the first
job matches no files, its error is prefixed with `table job 1 ('a.md')`, it writes no output, and the
second job still writes its output. This scenario is tested by
`DictionaryGenerator_Generate_OneJobFails_ContinuesAndReportsJobLabel`.

**DictionaryGenerator_Generate_InvalidOutputPathCharacter_ContinuesWithLaterJob**: Verifies that when
the first job's output path contains a null character, the job is reported as an invalid output path
with its label, the summary `1 of 2 table jobs failed` is written, the exit code is 1, and the second
job still writes its output. This scenario is tested by
`DictionaryGenerator_Generate_InvalidOutputPathCharacter_ContinuesWithLaterJob`.

**DictionaryGenerator_Generate_InvalidInputPathCharacter_ContinuesWithLaterJob**: Verifies that when
the first job's input pattern contains a null character, the job is reported as an invalid input
pattern with its label, the exit code is 1, and the second job still writes its output. This
scenario is tested by `DictionaryGenerator_Generate_InvalidInputPathCharacter_ContinuesWithLaterJob`.

**DictionaryGenerator_Generate_OutputParentIsFile_ContinuesWithLaterJob**: Verifies that when the
first job's output lies beneath an existing file, the write failure is reported with the job label
and the output path as written, the exit code is 1, and the second job still writes its output.
This scenario is tested by `DictionaryGenerator_Generate_OutputParentIsFile_ContinuesWithLaterJob`.

**DictionaryGenerator_Generate_WildcardOutputFileNameOnWindows_ContinuesWithLaterJob**: Verifies on
Windows (the test returns early on other platforms, where `*` is a valid file-name character) that
an output path such as `out/*.md`, which normalizes cleanly but is rejected on write, fails only its
own job and the second job still writes its output. This scenario is tested by
`DictionaryGenerator_Generate_WildcardOutputFileNameOnWindows_ContinuesWithLaterJob`.

**DictionaryGenerator_Generate_MalformedYaml_ContinuesWithLaterJob**: Verifies that when the first job
reads a syntactically malformed YAML file, the job is reported as invalid YAML naming the file, writes
no output, the exit code is 1, and the second job still writes its output. This scenario is tested by
`DictionaryGenerator_Generate_MalformedYaml_ContinuesWithLaterJob`.

**DictionaryGenerator_Generate_MalformedDeprecatedYaml_ContinuesWithLaterJob**: Verifies the same
behavior for a malformed deprecated-terms file loaded with `kind: deprecated`. This scenario is tested
by `DictionaryGenerator_Generate_MalformedDeprecatedYaml_ContinuesWithLaterJob`.

An unreadable input file (`UnauthorizedAccessException`) is not tested, because file permissions cannot
be denied reliably on every platform (for example when tests run as root); it is handled by the same
code path as other read errors.

**DictionaryGenerator_Generate_JobsFailed_WritesSummaryAndExitCodeOne**: Verifies that when both jobs
fail, both are reported, the summary `2 of 2 table jobs failed` is written, and the exit code is 1.
This scenario is tested by `DictionaryGenerator_Generate_JobsFailed_WritesSummaryAndExitCodeOne`.

**DictionaryGenerator_Generate_OutputInMissingDirectory_CreatesDirectory**: Verifies that an `--output`
path inside two missing directories is written after the directories are created. This scenario is
tested by `DictionaryGenerator_Generate_OutputInMissingDirectory_CreatesDirectory`.

**DictionaryGenerator_Generate_JobBaseDirectory_ResolvesRelativeInputsAndOutput**: Verifies that a job's
`sub/../../shared/*.yaml` input and `../out/terms.md` output resolve against the job's base directory
(the configuration file's directory). This scenario is tested by
`DictionaryGenerator_Generate_JobBaseDirectory_ResolvesRelativeInputsAndOutput`.

**DictionaryGenerator_Generate_ConflictsDetectedPerJob**: Verifies that two conflicting dictionaries
used by separate jobs produce no error, while one job reading both reports the conflict prefixed with
its label. This scenario is tested by `DictionaryGenerator_Generate_ConflictsDetectedPerJob`.

**DictionaryGenerator_Generate_ColumnWidths_AppliedToOutput**: Verifies that `--column-widths 25,75`
reaches the generated table separator. This scenario is tested by
`DictionaryGenerator_Generate_ColumnWidths_AppliedToOutput`.
