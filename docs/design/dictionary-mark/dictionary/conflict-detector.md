### ConflictDetector

![Dictionary Subsystem Structure](DictionaryView.svg)

#### Purpose

`ConflictDetector.Detect` groups approved entries by term (case-insensitive, after whitespace
normalization) and orders each group by source path (ordinal) so the result does not depend on input
order. It compares the first entry of each group with the others: any difference in `definition`
(ordinal), `pos` (set), `variants` (case-insensitive set) or `visible` is a conflict. Identical
duplicates collapse. Deprecated entries are grouped the same way and compared on `use` and `where`.
A term, or an approved variant, that is both approved and deprecated is a conflict. There is no
override precedence. `ConflictDetector` is a static class.

#### Data Model

N/A - static class with no instance state.

#### Key Methods

**Detect**: Applies the shared conflict rule.

- *Parameters*: `IEnumerable<DictionaryEntry> entries` — approved entries from all loaded files;
  `IEnumerable<DeprecatedEntry>? deprecated` — optional deprecated entries.
- *Returns*: `IReadOnlyList<string>` — sorted (ordinal) conflict messages; empty when none.
- *Preconditions*: `entries` is non-null.
- *Postconditions*: Messages have these shapes (sources are full resolved paths):
  `Conflict: term '{T}' differs in {fields} between {A} and {B}.` (fields in the order definition,
  pos, variants, visible), `Conflict: deprecated term '{T}' differs in use, where between {A} and
  {B}.`, `Conflict: term '{T}' is both approved ({A}) and deprecated ({D}).` and
  `Conflict: variant '{V}' of term '{T}' is approved ({A}) but also deprecated ({D}).` An entry
  without a source is shown as `<unknown>`.

Thread-safe — all state is method-local.

#### Error Handling

- `ArgumentNullException` — thrown when `entries` is null; the caller must supply a non-null
  enumerable.
- Conflicts are not thrown as exceptions; they are returned as strings in the result list. The
  caller (`DictionaryGenerator`) iterates the list, calls `context.WriteError` for each message, and
  returns early without generating output.

#### Dependencies

- **DictionaryEntry**, **DeprecatedEntry** — Dictionary subsystem data models; carry the fields
  compared for conflicts and the source path named in messages.

#### Callers

- **DictionaryGenerator.Generate** — calls `ConflictDetector.Detect(approved, deprecated)` after all
  YAML files have been loaded; iterates the returned list and calls `context.WriteError` for each
  message, then returns early without generating output.
