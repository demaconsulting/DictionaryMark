### YamlDictionaryLoader

![Dictionary Subsystem Structure](DictionaryView.svg)

#### Purpose

`YamlDictionaryLoader.Load` reads and parses a YAML file, verifies the root node is a
`YamlMappingNode`, and converts each key-value pair to a `DictionaryEntry`. It enforces
flat-mapping structure — both keys and values must be scalar nodes — and detects duplicate keys
within the same file using a case-insensitive hash set. `YamlDictionaryLoader` is a static class.

#### Data Model

N/A - static class with no instance state.

#### Key Methods

**Load**: Reads the file at `filePath`, parses the YAML, and returns the entries in file order.

- *Parameters*: `string filePath` — absolute or relative path to a YAML file.
- *Returns*: `IReadOnlyList<DictionaryEntry>` — ordered list of `DictionaryEntry` objects, each
  holding a `Term` (`string`) and `Definition` (`string`); immutable.
- *Preconditions*: `filePath` is non-null.
- *Postconditions*: Returns a list of all key-value pairs from the file in document order; throws on
  any structural or I/O error.

Opens the file, creates a `YamlStream`, calls `Load(TextReader)`, retrieves the first document's
root node, pattern-matches it to `YamlMappingNode`, then iterates `Children`, pattern-matching each
key to `YamlScalarNode` (key) and value to `YamlScalarNode` (value). Duplicate keys within the same
file are rejected. Each valid pair becomes a `DictionaryEntry`.

#### Error Handling

- `ArgumentNullException` — thrown when `filePath` is null; must be supplied by the caller.
- `IOException` — thrown when the file cannot be read (not found, access denied, etc.).
- `InvalidOperationException` — thrown when the YAML root is not a mapping node, a key is
  non-scalar, a value is non-scalar (nested structure), or a duplicate key is found within the same
  file.

The caller (`DictionaryGenerator`) catches `IOException` and `InvalidOperationException`, reports
them via `context.WriteError`, and returns without generating output.

#### Dependencies

- **YamlDotNet** — OTS package; `YamlStream`, `YamlMappingNode`, and `YamlScalarNode` APIs used to
  parse the YAML input file.
- **DictionaryEntry** — Dictionary subsystem data model; output type constructed for each valid
  key-value pair.

#### Callers

- **DictionaryGenerator.Generate** — calls `YamlDictionaryLoader.Load(filePath)` once per resolved
  input file; accumulates the returned lists for conflict detection.
