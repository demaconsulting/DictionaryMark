### YamlDictionaryLoader Verification

This document describes the unit-level verification design for the `YamlDictionaryLoader` unit.
It defines test scenarios, dependency usage, and requirement coverage for
`YamlDictionaryLoaderTests.cs`.

#### Verification Strategy

`YamlDictionaryLoader` is verified with unit tests in `YamlDictionaryLoaderTests.cs`. Tests
create temporary YAML files with controlled content, invoke `YamlDictionaryLoader.Load`, and
assert on the returned entries or expected exceptions.

#### Dependencies

| Dependency  | Usage in Tests                                                      |
| ----------- | ------------------------------------------------------------------- |
| File system | Temporary files written with controlled YAML content for each test. |

#### Test Environment

N/A - standard test environment. Tests write temporary YAML files using `TemporaryDirectory`
for automatic cleanup.

#### Acceptance Criteria

All unit tests in `YamlDictionaryLoaderTests.cs` pass; all requirements listed in the
Requirements Coverage section have at least one passing test scenario; no tests may be skipped
or marked as expected failures.

#### Test Scenarios

##### YamlDictionaryLoader_Load_ValidFlatYaml_ReturnsEntries

**Scenario**: File contains `"Term1: Definition1\nTerm2: Definition2\n"`.

**Expected**: Two entries returned in file order with correct term and definition values.

##### YamlDictionaryLoader_Load_EmptyFile_ReturnsEmptyList

**Scenario**: File is empty.

**Expected**: Empty list returned.

##### YamlDictionaryLoader_Load_NonExistentFile_ThrowsFileNotFoundException

**Scenario**: Path points to a non-existent file.

**Expected**: `FileNotFoundException` is thrown.

##### YamlDictionaryLoader_Load_NestedYaml_ThrowsInvalidOperationException

**Scenario**: File contains a nested YAML structure (`Term1:\n  nested: value`).

**Expected**: `InvalidOperationException` is thrown.

##### YamlDictionaryLoader_Load_ListYaml_ThrowsInvalidOperationException

**Scenario**: File contains a YAML sequence (`- item1\n- item2`).

**Expected**: `InvalidOperationException` is thrown.

##### YamlDictionaryLoader_Load_SingleEntry_ReturnsOneEntry

**Scenario**: File contains `"API: Application Programming Interface\n"`.

**Expected**: Single entry with term `"API"` and definition `"Application Programming Interface"`.

##### YamlDictionaryLoader_Load_NullFilePath_ThrowsArgumentNullException

**Scenario**: `null` is passed as the `filePath` argument.

**Expected**: `ArgumentNullException` is thrown.

##### YamlDictionaryLoader_Load_DuplicateKey_ThrowsInvalidOperationException

**Scenario**: File contains two entries with the same key (case-insensitive), e.g. `"Term1"` and `"term1"`.

**Expected**: `InvalidOperationException` is thrown.

#### Requirements Coverage

- **`DictionaryMark-YamlLoader-Load`**: YamlDictionaryLoader_Load_ValidFlatYaml_ReturnsEntries,
  YamlDictionaryLoader_Load_EmptyFile_ReturnsEmptyList,
  YamlDictionaryLoader_Load_SingleEntry_ReturnsOneEntry.
- **`DictionaryMark-YamlLoader-StructureErrors`**: YamlDictionaryLoader_Load_NestedYaml_ThrowsInvalidOperationException,
  YamlDictionaryLoader_Load_ListYaml_ThrowsInvalidOperationException.
- **`DictionaryMark-YamlLoader-FileErrors`**: YamlDictionaryLoader_Load_NonExistentFile_ThrowsFileNotFoundException.
- **`DictionaryMark-YamlLoader-NullInput`**: YamlDictionaryLoader_Load_NullFilePath_ThrowsArgumentNullException.
- **`DictionaryMark-YamlLoader-DuplicateKeys`**: YamlDictionaryLoader_Load_DuplicateKey_ThrowsInvalidOperationException.
