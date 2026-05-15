### ConflictDetector Design

The `ConflictDetector` class detects conflicts in dictionary entries where the same term
(case-insensitive) has different definitions across files.

#### Overview

`ConflictDetector.Detect` iterates over the provided entries, tracking the first definition
seen for each term. If a later entry has the same term but a different definition, a conflict
is reported. Same term + same definition is treated as an allowed deduplication (not a
conflict). Each conflicting term is reported at most once, using a separate `reported` HashSet
to suppress duplicate messages when more than two entries share the same conflicting term.

#### Data Model

`ConflictDetector` is a `static` class with no instance state.

#### Methods

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

#### Interactions

| Dependency        | Role                                                  |
| ----------------- | ----------------------------------------------------- |
| `DictionaryEntry` | Input data type carrying term and definition strings. |
