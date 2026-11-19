## Dictionary

### Verification Approach

The Dictionary subsystem is verified through integration tests in `DictionarySubsystemTests.cs`
that exercise the complete pipeline from YAML loading through conflict detection to Markdown
formatting. Tests directly instantiate `YamlDictionaryLoader`, `ConflictDetector`, and
`MarkdownFormatter` without mocking, composing them as the subsystem does in production.
Temporary YAML files written inline provide controlled input data for each scenario. Individual
units are additionally covered by unit-level tests in `YamlDictionaryLoaderTests.cs`,
`ConflictDetectorTests.cs`, `MarkdownFormatterTests.cs`, and `DictionaryGeneratorTests.cs`.

### Test Environment

N/A - standard test environment. Tests that load YAML files use `TemporaryDirectory` to create
temporary files that are cleaned up automatically after each test.

### Acceptance Criteria

- All integration tests in `DictionarySubsystemTests.cs` pass with zero failures.
- All subsystem requirements have at least one passing test scenario.
- No tests are skipped or marked as expected failures.

### Test Scenarios

**DictionarySubsystem_BulletGeneration_ValidYaml_GeneratesBulletMarkdown**: A temporary YAML
file containing two entries (`API` and `CI`) is loaded via `YamlDictionaryLoader.Load`;
`MarkdownFormatter.Format` is called with `OutputFormat.Bullets`. The output must contain
`"- **API**: Application Programming Interface"` and `"- **CI**: Continuous Integration"`,
confirming the bullet-list formatting pipeline produces correct Markdown.
This scenario is tested by `DictionarySubsystem_BulletGeneration_ValidYaml_GeneratesBulletMarkdown`.

**DictionarySubsystem_TableGeneration_ValidYaml_GeneratesTableMarkdown**: A temporary YAML file
containing one entry (`API`) is loaded via `YamlDictionaryLoader.Load`; `MarkdownFormatter.Format`
is called with `OutputFormat.Table`. The output must contain `"| Term |"`, `"| API |"`, and
`"Application Programming Interface"`, confirming the table formatting pipeline produces correct
Markdown.
This scenario is tested by `DictionarySubsystem_TableGeneration_ValidYaml_GeneratesTableMarkdown`.

**DictionarySubsystem_ConflictDetection_ConflictingEntries_ReturnsConflicts**: Two temporary
YAML files each defining `API` with a different definition are loaded separately via
`YamlDictionaryLoader.Load`; entries are merged and passed to `ConflictDetector.Detect`. The
result must be a non-empty conflict list containing a message referencing `"API"`, confirming
that cross-file conflicts are detected.
This scenario is tested by
`DictionarySubsystem_ConflictDetection_ConflictingEntries_ReturnsConflicts`.

**DictionarySubsystem_Generation_ValidYaml_GeneratesMarkdown**: A temporary YAML file
containing two entries (`API` and `CI`) is passed to `DictionaryGenerator.Generate` via a
`Context` created with `--input`; standard output is redirected to capture the generated
Markdown. The captured output must contain `"API"` and `"CI"` and `context.ExitCode` must be
zero, confirming the full generation pipeline operates end-to-end.
This scenario is tested by `DictionarySubsystem_Generation_ValidYaml_GeneratesMarkdown`.

**DictionarySubsystem_TableGeneration_ColumnWidths_GeneratesProportionalSeparator**: A temporary YAML
file is loaded with `YamlDictionaryLoader.Load` and formatted by `MarkdownFormatter.Format` as a table
with column widths `1,3`. The output must contain a separator with 25 and 75 dashes followed by the
entry row, confirming that column widths flow from the options through the formatter.
This scenario is tested by
`DictionarySubsystem_TableGeneration_ColumnWidths_GeneratesProportionalSeparator`.

**DictionarySubsystem_Generation_MultipleJobs_GeneratesEachOutput**: A configuration file with two jobs
(table and bullets) writing into a missing `out` directory is passed through `Context` to
`DictionaryGenerator.Generate`. Both outputs must exist with their own formats and the exit code must
be zero, confirming per-job generation, path resolution against the job's base directory and output
directory creation.
This scenario is tested by `DictionarySubsystem_Generation_MultipleJobs_GeneratesEachOutput`.

**DictionarySubsystem_FileAccess_ConfigJobOutsideRoots_DeniedAndOtherJobsRun**: Test that the dictionary subsystem
judges config-file table inputs and outputs by the file-access policy: a table whose input lies outside the allowed
folder fails with file-access-denied, while the other tables still generate their output. This scenario is tested by
`DictionarySubsystem_FileAccess_ConfigJobOutsideRoots_DeniedAndOtherJobsRun`.

**DictionarySubsystem_FileAccess_NetworkPattern_RefusedAndOtherJobsRun**: Test that a configuration table whose input
pattern names a network share is refused before anything is listed and writes nothing, while the other table still
generates its output and the exit code is 1. This scenario is tested by
`DictionarySubsystem_FileAccess_NetworkPattern_RefusedAndOtherJobsRun`.

**DictionarySubsystem_FileAccess_LiteralInputs_JudgedBeforeLookup**: Test that literal configuration inputs are judged
before they are looked up: with the configuration folder as the only allowed root, a missing file outside it fails its
table with `file-access-denied`, a missing file inside it fails its table with `Input file '...' was not found.`, and a
third table still generates its output. This scenario is tested by
`DictionarySubsystem_FileAccess_LiteralInputs_JudgedBeforeLookup`.

**DictionarySubsystem_FileAccess_ConfigRootRelocatedCommandLineJob_DeniedOutsideRoots**: Test that the dictionary
subsystem judges the relative command-line paths that a configuration `root` key relocates: an input and an output
moved outside the allowed folder fail with a non-zero exit code and write nothing. This scenario is tested by
`DictionarySubsystem_FileAccess_ConfigRootRelocatedCommandLineJob_DeniedOutsideRoots`.

**DictionarySubsystem_FileAccess_DeniedConfigPaths_MessagesShowWrittenPaths**: Test that a denied table input
(`../other/more.yaml`), a denied glob match (`../other/*.yaml`) and a denied output (`../out/c.md`) are each reported as
`file-access-denied` with the path as written (the glob match relative to the base directory) after the job label, with
no part of the temporary folder in the output, and that the denied output's folder was not created. The inside-roots
missing input of `DictionarySubsystem_FileAccess_LiteralInputs_JudgedBeforeLookup` is reported as
`Input file 'missing.yaml' was not found.` without the allowed folder. These scenarios are tested by the tests of the
same names.
