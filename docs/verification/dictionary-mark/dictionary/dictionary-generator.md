### DictionaryGenerator Verification

This document describes the unit-level verification design for the `DictionaryGenerator` unit.
It defines test scenarios, dependency usage, and requirement coverage for
`DictionaryGeneratorTests.cs`.

#### Verification Approach

`DictionaryGenerator` is verified with unit tests in `DictionaryGeneratorTests.cs`. Tests
create temporary YAML files with controlled content, invoke `DictionaryGenerator.Generate`,
and assert on captured console output, exit codes, and error messages.

#### Dependencies

| Dependency  | Usage in Tests                                                          |
| ----------- | ----------------------------------------------------------------------- |
| File system | Temporary YAML files written with controlled content for each test.     |
| `Context`   | Created from controlled argument arrays to drive each test scenario.    |

#### Test Scenarios

##### DictionaryGenerator_Generate_SingleYamlFile_WritesToStdout

**Scenario**: `Generate` is called with a context that specifies a single temporary YAML file
containing `"API: Application Programming Interface\n"`.

**Expected**: Standard output contains `"API"`; exit code is 0.

**Requirement coverage**: `DictionaryMark-DictionaryGenerator-FileLoading`,
`DictionaryMark-DictionaryGenerator-OutputRouting`.

##### DictionaryGenerator_Generate_ConflictingEntries_ReportsError

**Scenario**: `Generate` is called with a context specifying two YAML files that define the
same term (`"API"`) with different definitions.

**Expected**: Standard error contains `"Conflict: term 'API' has multiple definitions"`; exit
code is 1; no Markdown output is written.

**Requirement coverage**: `DictionaryMark-DictionaryGenerator-ConflictReporting`.

##### DictionaryGenerator_Generate_NoInputPatterns_ReportsError

**Scenario**: `Generate` is called with a context created from an empty argument array (no
`--input` flags), so the input pattern list is empty.

**Expected**: Standard error contains `"No input files found"`; exit code is 1.

**Requirement coverage**: `DictionaryMark-DictionaryGenerator-FileLoading`.

#### Requirements Coverage

- **`DictionaryMark-DictionaryGenerator-FileLoading`**:
  DictionaryGenerator_Generate_SingleYamlFile_WritesToStdout,
  DictionaryGenerator_Generate_NoInputPatterns_ReportsError
- **`DictionaryMark-DictionaryGenerator-ConflictReporting`**:
  DictionaryGenerator_Generate_ConflictingEntries_ReportsError
- **`DictionaryMark-DictionaryGenerator-OutputRouting`**:
  DictionaryGenerator_Generate_SingleYamlFile_WritesToStdout
