## YamlDotNet

### Verification Approach

YamlDotNet parses YAML dictionary files into an in-memory representation used by
`YamlDictionaryLoader`. DictionaryMark relies on `YamlStream`, `YamlMappingNode`, `YamlScalarNode`,
and `YamlSequenceNode` from the `YamlDotNet.RepresentationModel` namespace to convert raw YAML text
into term-keyed entries: plain-string definitions, object-form approved-term entries, and
deprecated-term entries. YamlDotNet is verified indirectly by the `YamlDictionaryLoaderTests` unit
tests. Each scenario drives `YamlDictionaryLoader.Load` or `YamlDictionaryLoader.LoadDeprecated`
with a specific YAML input and asserts the expected outcome. Because `YamlDictionaryLoader`
delegates all parsing to YamlDotNet, correct test outcomes constitute evidence that the required
YamlDotNet APIs function as specified. No separate qualification testing is required. When this OTS
dependency is updated, the full CI pipeline is re-executed and all test scenarios must continue to
pass before the update is accepted.

### Test Scenarios

**YamlDictionaryLoader_Load_ValidFlatYaml_ReturnsEntries**: `YamlDictionaryLoader.Load` is called
with a temporary YAML file containing two plain-string entries, verifying that `YamlStream.Load`,
`YamlMappingNode.Children`, and `YamlScalarNode.Value` operate correctly. A list of two
`DictionaryEntry` objects with the correct terms and definitions is expected.
This scenario is tested by `YamlDictionaryLoader_Load_ValidFlatYaml_ReturnsEntries`.

**YamlDictionaryLoader_Load_SingleEntry_ReturnsOneEntry**: `YamlDictionaryLoader.Load` is called
with a YAML file containing one plain-string entry, verifying basic scalar retrieval via YamlDotNet.
A single-element list with the correct term and definition is expected.
This scenario is tested by `YamlDictionaryLoader_Load_SingleEntry_ReturnsOneEntry`.

**YamlDictionaryLoader_Load_EmptyFile_ReturnsEmptyList**: `YamlDictionaryLoader.Load` is called
with an empty YAML file, verifying that YamlDotNet handles an empty document stream gracefully. An
empty list without error is expected.
This scenario is tested by `YamlDictionaryLoader_Load_EmptyFile_ReturnsEmptyList`.

**YamlDictionaryLoader_Load_ObjectEntry_ReadsAllFields**: `YamlDictionaryLoader.Load` is called
with an object-form entry whose `definition`, `pos`, `variants`, and `visible` fields are set, with
`pos` and `variants` written as flow sequences, verifying that YamlDotNet parses a nested
`YamlMappingNode` entry value and its `YamlSequenceNode` and `YamlScalarNode` field values. An entry
with every field read, normalized, and de-duplicated is expected.
This scenario is tested by `YamlDictionaryLoader_Load_ObjectEntry_ReadsAllFields`.

**YamlDictionaryLoader_Load_NestedYaml_ThrowsInvalidOperationException**:
`YamlDictionaryLoader.Load` is called with a YAML file whose entry value is a nested mapping with a
key that is not an object-form field, verifying that YamlDotNet exposes the nested
`YamlMappingNode` keys for validation. An `InvalidOperationException` is expected.
This scenario is tested by `YamlDictionaryLoader_Load_NestedYaml_ThrowsInvalidOperationException`.

**YamlDictionaryLoader_Load_SequenceValue_Throws**: `YamlDictionaryLoader.Load` is called with a
YAML file whose entry value is a sequence, verifying that YamlDotNet parses the value as a
`YamlSequenceNode`, which is neither a definition string nor an object. An
`InvalidOperationException` is expected.
This scenario is tested by `YamlDictionaryLoader_Load_SequenceValue_Throws`.

**YamlDictionaryLoader_Load_ListYaml_ThrowsInvalidOperationException**: `YamlDictionaryLoader.Load`
is called with a YAML file whose root node is a sequence rather than a mapping, verifying that
`YamlDocument.RootNode` and `YamlMappingNode` pattern-matching correctly reject non-mapping root
nodes. An `InvalidOperationException` is expected.
This scenario is tested by `YamlDictionaryLoader_Load_ListYaml_ThrowsInvalidOperationException`.

**YamlDictionaryLoader_Load_DuplicateKey_ThrowsInvalidOperationException**:
`YamlDictionaryLoader.Load` is called with a YAML file containing a case-insensitively duplicate
key, verifying that the duplicate-key detection layer built on top of YamlDotNet's node enumeration
operates correctly. An `InvalidOperationException` is expected.
This scenario is tested by `YamlDictionaryLoader_Load_DuplicateKey_ThrowsInvalidOperationException`.

**YamlDictionaryLoader_LoadDeprecated_ValidEntries_ReturnsEntries**:
`YamlDictionaryLoader.LoadDeprecated` is called with a YAML file of deprecated-term entries written
as flow mappings with `use` and an optional `where`, verifying that YamlDotNet parses flow-style
`YamlMappingNode` entry values. Two `DeprecatedEntry` objects with the correct `use` and `where`
values are expected.
This scenario is tested by `YamlDictionaryLoader_LoadDeprecated_ValidEntries_ReturnsEntries`.
