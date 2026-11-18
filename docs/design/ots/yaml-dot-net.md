## YamlDotNet

YamlDotNet is a .NET YAML parsing and serialization library. DictionaryMark uses it in
`YamlDictionaryLoader` to parse YAML dictionary files, and in `ConfigFile` to parse the YAML
configuration file, into an in-memory representation for traversal.

### Purpose

YamlDotNet provides YAML 1.1/1.2 compliant parsing that converts raw file content into a
structured node tree. `YamlDictionaryLoader` traverses the tree to extract dictionary entries;
`ConfigFile` traverses it to read formatting defaults, the root directory and table jobs.

### Features Used

DictionaryMark uses the following APIs from the `YamlDotNet.RepresentationModel` namespace:

- **`YamlStream`** — loads and parses a YAML document from a `TextReader`
- **`YamlStream.Load(TextReader)`** — parses the YAML text into a document collection
- **`YamlStream.Documents`** — accesses the list of parsed YAML documents (at most one expected)
- **`YamlDocument.RootNode`** — retrieves the root node of the parsed document
- **`YamlMappingNode`** — pattern-matched to verify the root is a mapping of terms and to read
  object-form entries
- **`YamlMappingNode.Children`** — enumerates the key-value pairs within the mapping
- **`YamlScalarNode`** — pattern-matched to verify keys are scalars and to read definition strings and
  field values
- **`YamlScalarNode.Value`** — reads the string value of a scalar key or definition
- **`YamlSequenceNode`** — pattern-matched by `YamlDictionaryLoader` for `pos` and `variants` lists,
  and by `ConfigFile` for `tables`, sequence `input` values and `column-widths`
- **`YamlException`** — caught by `ConfigFile` to report invalid configuration YAML

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
`InvalidOperationException` for any structure that is not a mapping of scalar terms to a
definition string or an entry object:

```csharp
if (root is not YamlMappingNode mappingNode) { throw ... }
if (key is not YamlScalarNode keyNode) { throw ... }
switch (value) { case YamlScalarNode: ...; case YamlMappingNode: ...; default: throw ...; }
```

### Error Handling

YAML parse exceptions thrown by `YamlStream.Load` propagate unchanged through `YamlDictionaryLoader` and
`DictionaryGenerator`; they are not caught in those units and reach `Program.Main` as unexpected exceptions.
`ConfigFile` instead catches `YamlException` and wraps it in an `InvalidOperationException` whose message names the
configuration file, so an invalid configuration file is reported as an expected error with exit code 1. Structural
violations — non-mapping root, non-scalar keys, or entry values that are neither a string nor an object — are
detected by `YamlDictionaryLoader` and thrown as `InvalidOperationException` with a descriptive message identifying
the file and the offending element.

### Design Constraints

- Only the `YamlDotNet.RepresentationModel` namespace is used; the deserializer API is not used,
  avoiding any reflection-based type mapping.
- Multi-document YAML files are accepted by the parser but only the first document is processed;
  additional documents are silently ignored.
- YAML anchors and aliases are resolved transparently by YamlDotNet before `YamlDictionaryLoader`
  traverses the node tree.
