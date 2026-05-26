## YamlDotNet

YamlDotNet is a .NET YAML parsing and serialization library. DictionaryMark uses it exclusively
in `YamlDictionaryLoader` to parse YAML input files into an in-memory representation for
traversal.

### Purpose

YamlDotNet is used exclusively in `YamlDictionaryLoader` to parse YAML input files. It provides
YAML 1.1/1.2 compliant parsing that converts raw file content into a structured node tree, which
`YamlDictionaryLoader` traverses to extract flat key-value dictionary entries.

### Features Used

DictionaryMark uses the following APIs from the `YamlDotNet.RepresentationModel` namespace:

- **`YamlStream`** — loads and parses a YAML document from a `TextReader`
- **`YamlStream.Load(TextReader)`** — parses the YAML text into a document collection
- **`YamlStream.Documents`** — accesses the list of parsed YAML documents (at most one expected)
- **`YamlDocument.RootNode`** — retrieves the root node of the parsed document
- **`YamlMappingNode`** — pattern-matched to verify the root is a flat mapping
- **`YamlMappingNode.Children`** — enumerates the key-value pairs within the mapping
- **`YamlScalarNode`** — pattern-matched to verify keys and values are scalars
- **`YamlScalarNode.Value`** — reads the string value of a scalar key or definition

Features not used: serialization, YAML writing, anchors/aliases traversal, merge keys, custom
type converters, schema validation, and streaming deserialization.

### Integration Pattern

YamlDotNet is invoked via direct API calls with no wrapper class:

```csharp
var yaml = new YamlStream();
using var reader = new StringReader(content);
yaml.Load(reader);
```

After loading, `YamlDictionaryLoader` pattern-matches each node type and throws
`InvalidOperationException` for any structure that is not a flat scalar-to-scalar mapping:

```csharp
if (root is not YamlMappingNode mappingNode) { throw ... }
if (child.Key is not YamlScalarNode keyNode) { throw ... }
if (child.Value is not YamlScalarNode valueNode) { throw ... }
```

### Error Handling

YAML parse exceptions thrown by `YamlStream.Load` propagate unchanged through
`YamlDictionaryLoader` and `DictionaryGenerator`; they are not caught in those units and reach
`Program.Main` as unexpected exceptions. Structural violations — non-mapping root, non-scalar
keys or values — are detected by `YamlDictionaryLoader` and thrown as `InvalidOperationException`
with a descriptive message identifying the file and the offending element.

### Design Constraints

- Only the `YamlDotNet.RepresentationModel` namespace is used; the deserializer API is not used,
  avoiding any reflection-based type mapping.
- Multi-document YAML files are accepted by the parser but only the first document is processed;
  additional documents are silently ignored.
- YAML anchors and aliases are resolved transparently by YamlDotNet before `YamlDictionaryLoader`
  traverses the node tree.
