# YamlDictionaryLoader Verification

This document describes the unit-level verification design for the `YamlDictionaryLoader` unit.
It defines test scenarios, dependency usage, and requirement coverage for
`YamlDictionaryLoaderTests.cs`.

## Verification Approach

`YamlDictionaryLoader` is verified with unit tests in `YamlDictionaryLoaderTests.cs`. Tests
create temporary YAML files with controlled content, invoke `YamlDictionaryLoader.Load`, and
assert on the returned entries or expected exceptions.

## Dependencies

| Dependency  | Usage in Tests                                                      |
| ----------- | ------------------------------------------------------------------- |
| File system | Temporary files written with controlled YAML content for each test. |

## Test Scenarios

### YamlDictionaryLoader_Load_ValidFlatYaml_ReturnsEntries

**Scenario**: File contains `"Term1: Definition1\nTerm2: Definition2\n"`.

**Expected**: Two entries returned in file order with correct term and definition values.

### YamlDictionaryLoader_Load_EmptyFile_ReturnsEmptyList

**Scenario**: File is empty.

**Expected**: Empty list returned.

### YamlDictionaryLoader_Load_NonExistentFile_ThrowsFileNotFoundException

**Scenario**: Path points to a non-existent file.

**Expected**: `FileNotFoundException` is thrown.

### YamlDictionaryLoader_Load_NestedYaml_ThrowsInvalidOperationException

**Scenario**: File contains a nested YAML structure (`Term1:\n  nested: value`).

**Expected**: `InvalidOperationException` is thrown.

### YamlDictionaryLoader_Load_ListYaml_ThrowsInvalidOperationException

**Scenario**: File contains a YAML sequence (`- item1\n- item2`).

**Expected**: `InvalidOperationException` is thrown.

### YamlDictionaryLoader_Load_SingleEntry_ReturnsOneEntry

**Scenario**: File contains `"API: Application Programming Interface\n"`.

**Expected**: Single entry with term `"API"` and definition `"Application Programming Interface"`.

## Requirements Coverage

- **`DictionaryMark-YamlLoader-Load`**: All `YamlDictionaryLoader_Load_*` tests.
