# YamlDictionaryLoader Design

The `YamlDictionaryLoader` class loads dictionary entries from YAML files. It validates that
the file contains a flat key-value mapping and rejects nested structures and duplicate keys.

## Overview

`YamlDictionaryLoader.Load` reads and parses the YAML file, verifies the root node is a
`YamlMappingNode`, and converts each key-value pair to a `DictionaryEntry`. It enforces
flat-mapping structure — both keys and values must be scalar nodes — and detects duplicate
keys within the same file using a case-insensitive hash set.

## Data Model

`YamlDictionaryLoader` is a `static` class with no instance state.

## Methods

### Load(string filePath) → IReadOnlyList\<DictionaryEntry\>

Reads the file at `filePath`, parses the YAML, and returns the entries in file order.

**Throws:**

- `ArgumentNullException` — when `filePath` is null.
- `IOException` — when the file cannot be read.
- `InvalidOperationException` — when the YAML root is not a mapping, a key is non-scalar,
  a value is non-scalar (nested structure), or a duplicate key is found.

## Interactions

| Dependency        | Role                                               |
| ----------------- | -------------------------------------------------- |
| `YamlDotNet`      | Parses the YAML stream into a node representation. |
| `DictionaryEntry` | Data type returned for each valid key-value pair.  |
