## YamlDotNet

### Verification Approach

YamlDotNet parses YAML dictionary files into an in-memory representation used by
`YamlDictionaryLoader`. DictionaryMark relies on `YamlStream`, `YamlMappingNode`, and
`YamlScalarNode` from the `YamlDotNet.RepresentationModel` namespace to convert raw YAML text into
flat key-value entries. YamlDotNet is verified indirectly by the `YamlDictionaryLoaderTests` unit
tests. Each scenario drives `YamlDictionaryLoader.Load` with a specific YAML input and asserts the
expected outcome. Because `YamlDictionaryLoader` delegates all parsing to YamlDotNet, correct test
outcomes constitute evidence that the required YamlDotNet APIs function as specified. No separate
qualification testing is required. When this OTS dependency is updated, the full CI pipeline is
re-executed and all test scenarios must continue to pass before the update is accepted.

### Test Scenarios

**YamlDictionaryLoader_Load_ValidFlatYaml_ReturnsEntries**: `YamlDictionaryLoader.Load` is called
with a temporary YAML file containing two flat scalar-to-scalar entries, verifying that
`YamlStream.Load`, `YamlMappingNode.Children`, and `YamlScalarNode.Value` operate correctly. A list
of two `DictionaryEntry` objects with the correct terms and definitions is expected.
This scenario is tested by `YamlDictionaryLoader_Load_ValidFlatYaml_ReturnsEntries`.

**YamlDictionaryLoader_Load_SingleEntry_ReturnsOneEntry**: `YamlDictionaryLoader.Load` is called
with a YAML file containing one scalar mapping entry, verifying basic scalar mapping retrieval via
YamlDotNet. A single-element list with the correct term and definition is expected.
This scenario is tested by `YamlDictionaryLoader_Load_SingleEntry_ReturnsOneEntry`.

**YamlDictionaryLoader_Load_EmptyFile_ReturnsEmptyList**: `YamlDictionaryLoader.Load` is called
with an empty YAML file, verifying that YamlDotNet handles an empty document stream gracefully. An
empty list without error is expected.
This scenario is tested by `YamlDictionaryLoader_Load_EmptyFile_ReturnsEmptyList`.

**YamlDictionaryLoader_Load_NestedYaml_ThrowsInvalidOperationException**:
`YamlDictionaryLoader.Load` is called with a YAML file whose root value is a nested mapping rather
than a scalar, verifying that `YamlMappingNode` pattern-matching correctly identifies non-scalar
values parsed by YamlDotNet. An `InvalidOperationException` is expected.
This scenario is tested by `YamlDictionaryLoader_Load_NestedYaml_ThrowsInvalidOperationException`.

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
