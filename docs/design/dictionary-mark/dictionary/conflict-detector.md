### ConflictDetector

#### Purpose

`ConflictDetector.Detect` iterates over all entries, tracking the first definition seen for each
term (case-insensitive). If a later entry has the same term but a different definition
(case-sensitive comparison), a conflict message is generated. Same term and same definition is
treated as allowed deduplication. Each conflicting term is reported at most once, using a separate
`reported` `HashSet` to suppress duplicate messages when more than two entries share the same
conflicting term. `ConflictDetector` is a static class.

#### Data Model

N/A - static class with no instance state.

#### Key Methods

**Detect**: Scans all entries for terms that have two or more distinct definitions.

- *Parameters*: `IEnumerable<DictionaryEntry> entries` — all entries from all loaded YAML files.
- *Returns*: `IReadOnlyList<string>` — list of human-readable conflict messages (one per conflicting
  term); empty when no conflicts exist.
- *Preconditions*: `entries` is non-null.
- *Postconditions*: Returns a list with one message per conflicting term; same term and same
  definition produces no message.

Term comparison is case-insensitive; definition comparison is case-sensitive (ordinal). Uses a
method-local dictionary mapping each term to its first-seen definition, and a method-local `HashSet`
tracking already-reported terms to suppress duplicate messages. Thread-safe — all state is
method-local.

#### Error Handling

- `ArgumentNullException` — thrown when `entries` is null; the caller must supply a non-null
  enumerable.
- Conflicts are not thrown as exceptions; they are returned as strings in the result list. The
  caller (`DictionaryGenerator`) iterates the list, calls `context.WriteError` for each message, and
  returns early without generating output.

#### Dependencies

- **DictionaryEntry** — Dictionary subsystem data model; carries the term and definition strings
  scanned for conflicts.

#### Callers

- **DictionaryGenerator.Generate** — calls `ConflictDetector.Detect(allEntries)` after all YAML
  files have been loaded; iterates the returned list and calls `context.WriteError` for each
  message, then returns early without generating output.
