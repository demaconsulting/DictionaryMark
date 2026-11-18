### YamlDictionaryLoader

![Dictionary Subsystem Structure](DictionaryView.svg)

#### Purpose

`YamlDictionaryLoader.Load` reads and parses a YAML file of approved terms, verifies the root node is a
`YamlMappingNode`, and converts each key-value pair to a `DictionaryEntry`. A value is either a
definition string or an object with `definition`, `pos`, `variants` and `visible`. `LoadDeprecated`
does the same for deprecated terms (objects with `use` and optional `where`). Terms are
whitespace-normalized and duplicate keys within a file are detected case-insensitively.
`YamlDictionaryLoader` is a static class.

#### Data Model

N/A - static class with no instance state.

#### Key Methods

**Load**: Reads the file at `filePath`, parses the YAML, and returns the entries in file order.

- *Parameters*: `string filePath` — absolute or relative path to a YAML file.
- *Returns*: `IReadOnlyList<DictionaryEntry>` — ordered list of immutable `DictionaryEntry` objects
  holding `Term`, `Definition`, `Pos` (default `noun`), `Variants`, `Visible` (default true) and
  `Source` (full path).
- *Preconditions*: `filePath` is non-null.
- *Postconditions*: Returns a list of all entries from the file in document order; throws on any
  structural, validation or I/O error.

`definition` is required for every approved term, regardless of `visible`: a plain-string value must
be non-empty, and an object value must have a non-empty `definition`. Null values and missing or
empty definitions are errors naming the term and file. `pos` is one string or a list drawn from
`noun`, `verb`, `adjective`, `adverb` and `proper-noun` (case-insensitive); `variants` is a list of
strings (whitespace-normalized, de-duplicated case-insensitively); `visible` is a boolean or the
strings `true`/`false`. Unknown keys are errors.

**LoadDeprecated**: Same structure for deprecated terms: each value must be an object with a
required non-empty `use` and an optional `where`; no definition. Returns
`IReadOnlyList<DeprecatedEntry>`.

**LoadText** and **LoadDeprecatedText**: Parse YAML text that the caller has already read, with
exactly the same rules as `Load` and `LoadDeprecated`. `Load` and `LoadDeprecated` delegate to them
after reading the file with `File.ReadAllText`.

- *Parameters*: `string content` — the YAML text; `string source` — the full path of the file the
  text came from, recorded as each entry's `Source` and named in error messages.
- *Returns*: the same lists as `Load` and `LoadDeprecated`.
- *Preconditions*: Arguments are non-null.
- *Postconditions*: No I/O is performed.

They exist because the file-access policy reads a configuration-file input from the handle it has
verified; parsing that text, rather than opening the file again by name, keeps the decision and the
read on the same file.

#### Error Handling

- `ArgumentNullException` — thrown when `filePath` is null; must be supplied by the caller.
- `IOException` — thrown when the file cannot be read (not found, access denied, etc.).
- `InvalidOperationException` — thrown when the YAML root is not a mapping node, a key is
  non-scalar or empty, a value is not a string or object, a field is invalid, an unknown key is
  present, a definition or `use` is missing, or a duplicate key is found within the same file. The
  message names the term and the file.

The caller (`DictionaryGenerator`) catches `IOException` and `InvalidOperationException`, reports
them via `context.WriteError`, and returns without generating output.

#### Dependencies

- **YamlDotNet** — OTS package; `YamlStream`, `YamlMappingNode`, and `YamlScalarNode` APIs used to
  parse the YAML input file.
- **DictionaryEntry**, **DeprecatedEntry** — Dictionary subsystem data models; output types
  constructed for each valid entry.

#### Callers

- **DictionaryGenerator.Generate** — calls `YamlDictionaryLoader.Load(filePath)` (or `LoadDeprecated`)
  once per resolved input file of the command-line job, and `LoadText` (or `LoadDeprecatedText`) with the
  text `FileAccessPolicy.ReadAllText` returned for each input file of a configuration job; accumulates
  the returned lists for conflict detection.
