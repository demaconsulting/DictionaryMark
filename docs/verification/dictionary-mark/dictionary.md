## Dictionary Subsystem Verification

This document describes subsystem-level verification for the Dictionary subsystem.

### Verification Strategy

The Dictionary subsystem is verified through integration tests in `DictionarySubsystemTests.cs`
that exercise the complete pipeline from YAML loading through conflict detection to Markdown
formatting. Tests directly instantiate `YamlDictionaryLoader`, `ConflictDetector`, and
`MarkdownFormatter` without mocking, composing them as the subsystem does in production.
Temporary YAML files written inline provide controlled input data for each scenario.

### Test Environment

N/A - standard test environment.

### Acceptance Criteria

All Dictionary subsystem tests in `DictionarySubsystemTests.cs` pass; all requirements listed
in the Requirements Coverage section have at least one passing test scenario; no tests may be
skipped or marked as expected failures.

### Components

- `YamlDictionaryLoader` - verified in `YamlDictionaryLoaderTests.cs`
- `ConflictDetector` - verified in `ConflictDetectorTests.cs`
- `MarkdownFormatter` - verified in `MarkdownFormatterTests.cs`
- `DictionaryGenerator` - verified in `DictionaryGeneratorTests.cs`

### Test Scenarios

#### DictionarySubsystem_BulletGeneration_ValidYaml_GeneratesBulletMarkdown

**Scenario**: A temporary YAML file containing two entries (`API` and `CI`) is loaded via
`YamlDictionaryLoader.Load`; `MarkdownFormatter.Format` is called with `OutputFormat.Bullets`.

**Expected**: Output contains `"- **API**: Application Programming Interface"` and
`"- **CI**: Continuous Integration"`.

#### DictionarySubsystem_TableGeneration_ValidYaml_GeneratesTableMarkdown

**Scenario**: A temporary YAML file containing one entry (`API`) is loaded via
`YamlDictionaryLoader.Load`; `MarkdownFormatter.Format` is called with `OutputFormat.Table`.

**Expected**: Output contains `"| Term |"`, `"| API |"`, and
`"Application Programming Interface"`.

#### DictionarySubsystem_ConflictDetection_ConflictingEntries_ReturnsConflicts

**Scenario**: Two temporary YAML files, each defining `API` with a different definition, are
loaded separately via `YamlDictionaryLoader.Load`; entries are merged and passed to
`ConflictDetector.Detect`.

**Expected**: `Detect` returns a non-empty conflict list containing a message referencing
`"API"`.

#### DictionarySubsystem_Generation_ValidYaml_GeneratesMarkdown

**Scenario**: A temporary YAML file containing two entries (`API` and `CI`) is passed to
`DictionaryGenerator.Generate` via a `Context` created with `--input`; standard output is
redirected to capture the generated Markdown.

**Expected**: The captured output contains `"API"` and `"CI"`; `context.ExitCode` is `0`.

### Subsystem Tests

The following subsystem integration tests exercise the Dictionary subsystem pipeline
end-to-end and are mapped to the subsystem requirements below.

### Requirements Coverage

- **`DictionaryMark-Dictionary-YamlLoading`**: DictionarySubsystem_BulletGeneration_ValidYaml_GeneratesBulletMarkdown,
  DictionarySubsystem_TableGeneration_ValidYaml_GeneratesTableMarkdown.
- **`DictionaryMark-Dictionary-ConflictDetection`**: DictionarySubsystem_ConflictDetection_ConflictingEntries_ReturnsConflicts.
- **`DictionaryMark-Dictionary-MarkdownFormatting`**: DictionarySubsystem_BulletGeneration_ValidYaml_GeneratesBulletMarkdown,
  DictionarySubsystem_TableGeneration_ValidYaml_GeneratesTableMarkdown.
- **`DictionaryMark-Dictionary-Generation`**: DictionarySubsystem_Generation_ValidYaml_GeneratesMarkdown.
