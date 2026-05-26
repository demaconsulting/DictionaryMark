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
