### ConflictDetector Design

The `ConflictDetector` class detects conflicts in dictionary entries where the same term
(case-insensitive) has different definitions across files.

#### Purpose

`ConflictDetector.Detect` iterates over the provided entries, tracking the first definition
seen for each term. If a later entry has the same term but a different definition, a conflict
is reported. Same term + same definition is treated as an allowed deduplication (not a
conflict). Each conflicting term is reported at most once, using a separate `reported` HashSet
to suppress duplicate messages when more than two entries share the same conflicting term.

#### Data Model

`ConflictDetector` is a `static` class with no instance state.

#### Key Methods

##### Detect(IEnumerable\<DictionaryEntry\> entries) → IReadOnlyList\<string\>

Returns a list of conflict error messages (one per conflicting term). Returns an empty list
when no conflicts exist.

**Remarks:**

- Term comparison is case-insensitive.
- Definition comparison is case-sensitive (ordinal); two definitions that differ only in casing
  are treated as different definitions and constitute a conflict.
- Same term + same definition → no conflict (deduplication allowed).
- Same term + different definition → conflict (one message per term).
- Thread-safe; all state is method-local.

**Throws:** `ArgumentNullException` - when `entries` is null.

#### Error Handling

`ConflictDetector.Detect` has one exceptional condition and one non-exceptional error path:

- `ArgumentNullException` — thrown when `entries` is null; the caller must supply a non-null
  enumerable.
- **Conflict detection** — conflicts are not thrown as exceptions. Instead, one error message
  string per conflicting term is appended to the return list. The caller (`DictionaryGenerator`)
  iterates the returned list and calls `context.WriteError` for each message, then returns
  without generating output.

#### Interactions

| Dependency        | Role                                                  |
| ----------------- | ----------------------------------------------------- |
| `DictionaryEntry` | Input data type carrying term and definition strings. |

#### Dependencies

| Dependency        | Role                                                                               |
| ----------------- | ---------------------------------------------------------------------------------- |
| `DictionaryEntry` | Dictionary subsystem data model — carries the term and definition strings scanned for conflicts. |

#### Callers

`DictionaryGenerator.Generate` calls `ConflictDetector.Detect(allEntries)` after all YAML
files have been loaded. It iterates the returned conflict message list and calls
`context.WriteError` for each message, then returns early without generating output.
