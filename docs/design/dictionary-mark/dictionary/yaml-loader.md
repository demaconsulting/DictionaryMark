### YamlDictionaryLoader Design

The `YamlDictionaryLoader` class loads dictionary entries from YAML files. It validates that
the file contains a flat key-value mapping and rejects nested structures and duplicate keys.

#### Purpose

`YamlDictionaryLoader.Load` reads and parses the YAML file, verifies the root node is a
`YamlMappingNode`, and converts each key-value pair to a `DictionaryEntry`. It enforces
flat-mapping structure - both keys and values must be scalar nodes - and detects duplicate
keys within the same file using a case-insensitive hash set.

#### Data Model

`YamlDictionaryLoader` is a `static` class with no instance state.

##### DictionaryEntry Data Model

`DictionaryEntry` is a `sealed` class that represents a single dictionary entry loaded from a
YAML file. It is immutable: all properties are set via the constructor and exposed as read-only.

- `DictionaryEntry(string term, string definition)` (Constructor) — Initializes a new entry
  with the given term and definition. Both parameters are required; stored directly without validation.
- `Term` (`string`, Property) — The dictionary term (key). Read-only; set by the constructor.
- `Definition` (`string`, Property) — The term's definition (value). Read-only; set by the constructor.

`DictionaryEntry` has no instance methods beyond those inherited from `object`.

#### Key Methods

##### Load(string filePath) → IReadOnlyList\<DictionaryEntry\>

Reads the file at `filePath`, parses the YAML, and returns the entries in file order.

**Throws:**

- `ArgumentNullException` - when `filePath` is null.
- `IOException` - when the file cannot be read.
- `InvalidOperationException` - when the YAML root is not a mapping, a key is non-scalar,
  a value is non-scalar (nested structure), or a duplicate key is found.

#### Error Handling

`YamlDictionaryLoader.Load` throws exceptions for all detectable error conditions:

- `ArgumentNullException` — when `filePath` is null.
- `IOException` — when the file cannot be read (e.g., not found, access denied).
- `InvalidOperationException` — when the YAML root is not a mapping node, a key is non-scalar,
  a value is non-scalar (nested structure), or a duplicate key is found within the same file.

The caller (`DictionaryGenerator`) catches `IOException` and `InvalidOperationException`,
reports them via `context.WriteError`, and returns without generating output.

#### Interactions

| Dependency        | Role                                               |
| ----------------- | -------------------------------------------------- |
| `YamlDotNet`      | Parses the YAML stream into a node representation. |
| `DictionaryEntry` | Data type returned for each valid key-value pair.  |

#### Dependencies

| Dependency        | Role                                                                                                        |
| ----------------- | ----------------------------------------------------------------------------------------------------------- |
| `YamlDotNet`      | OTS package — `YamlStream`, `YamlMappingNode`, and `YamlScalarNode` APIs used to parse the YAML input file. |
| `DictionaryEntry` | Dictionary subsystem data model — output type constructed for each valid key-value pair.                    |

#### Callers

`DictionaryGenerator.Generate` calls `YamlDictionaryLoader.Load(filePath)` once per resolved
input file in the pipeline's loading step. The returned `IReadOnlyList<DictionaryEntry>` from
each call is accumulated into the full entry list for conflict detection.
