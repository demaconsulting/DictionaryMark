## YamlDotNet Verification

This document provides the verification evidence for the `YamlDotNet` OTS software item.
Requirements for this OTS item are defined in the YamlDotNet OTS Software Requirements document.

### Required Functionality

YamlDotNet parses YAML dictionary files into an in-memory representation used by
`YamlDictionaryLoader`. DictionaryMark relies on `YamlStream`, `YamlMappingNode`,
and `YamlScalarNode` from the `YamlDotNet.RepresentationModel` namespace to convert
raw YAML text into flat key-value entries. The unit tests for `YamlDictionaryLoader`
exercise all required YamlDotNet APIs through normal class-level calls.

### Qualification Evidence

YamlDotNet is verified indirectly by the `YamlDictionaryLoaderTests` unit tests.Each
scenario drives `YamlDictionaryLoader.Load` with a specific YAML input and asserts the
expected outcome. Because `YamlDictionaryLoader` delegates all parsing to YamlDotNet,
correct test outcomes constitute evidence that the required YamlDotNet APIs function as
specified. No separate qualification testing is required.

### Regression Approach

When this OTS dependency is updated, the full CI pipeline is re-executed. All test scenarios must
continue to pass before the update is accepted.

### Test Scenarios

#### YamlDictionaryLoader_Load_ValidFlatYaml_ReturnsEntries

**Scenario**: `YamlDictionaryLoader.Load` is called with a temporary YAML file containing two
flat scalar-to-scalar entries.

**Expected**: Returns a list of two `DictionaryEntry` objects with the correct terms and
definitions, confirming that `YamlStream.Load`, `YamlMappingNode.Children`, and
`YamlScalarNode.Value` operate correctly.

**Requirement coverage**: `DictionaryMark-OTS-YamlDotNet`.

#### YamlDictionaryLoader_Load_SingleEntry_ReturnsOneEntry

**Scenario**: `YamlDictionaryLoader.Load` is called with a YAML file containing one scalar
mapping entry.

**Expected**: Returns a single-element list with the correct term and definition, confirming
basic scalar mapping retrieval via YamlDotNet.

**Requirement coverage**: `DictionaryMark-OTS-YamlDotNet`.

#### YamlDictionaryLoader_Load_EmptyFile_ReturnsEmptyList

**Scenario**: `YamlDictionaryLoader.Load` is called with an empty YAML file.

**Expected**: Returns an empty list without error, confirming that YamlDotNet handles an
empty document stream gracefully.

**Requirement coverage**: `DictionaryMark-OTS-YamlDotNet`.

#### YamlDictionaryLoader_Load_NestedYaml_ThrowsInvalidOperationException

**Scenario**: `YamlDictionaryLoader.Load` is called with a YAML file whose root value is a
nested mapping rather than a scalar.

**Expected**: Throws `InvalidOperationException`, confirming that `YamlMappingNode`
pattern-matching correctly identifies non-scalar values parsed by YamlDotNet.

**Requirement coverage**: `DictionaryMark-OTS-YamlDotNet`.

#### YamlDictionaryLoader_Load_ListYaml_ThrowsInvalidOperationException

**Scenario**: `YamlDictionaryLoader.Load` is called with a YAML file whose root node is a
sequence (list) rather than a mapping.

**Expected**: Throws `InvalidOperationException`, confirming that `YamlDocument.RootNode`
and `YamlMappingNode` pattern-matching correctly reject non-mapping root nodes.

**Requirement coverage**: `DictionaryMark-OTS-YamlDotNet`.

#### YamlDictionaryLoader_Load_DuplicateKey_ThrowsInvalidOperationException

**Scenario**: `YamlDictionaryLoader.Load` is called with a YAML file containing a
case-insensitively duplicate key.

**Expected**: Throws `InvalidOperationException`, confirming that the duplicate-key
detection layer built on top of YamlDotNet's node enumeration operates correctly.

**Requirement coverage**: `DictionaryMark-OTS-YamlDotNet`.

### Requirements Coverage

- **`DictionaryMark-OTS-YamlDotNet`**: YamlDictionaryLoader_Load_ValidFlatYaml_ReturnsEntries,
  YamlDictionaryLoader_Load_SingleEntry_ReturnsOneEntry,
  YamlDictionaryLoader_Load_EmptyFile_ReturnsEmptyList,
  YamlDictionaryLoader_Load_NestedYaml_ThrowsInvalidOperationException,
  YamlDictionaryLoader_Load_ListYaml_ThrowsInvalidOperationException,
  YamlDictionaryLoader_Load_DuplicateKey_ThrowsInvalidOperationException

### Suitability Conclusion

Based on the evidence above, YamlDotNet is considered suitable for use in the DictionaryMark CI pipeline.
