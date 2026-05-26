### MarkdownFormatter

#### Purpose

`MarkdownFormatter.Format` deduplicates entries (first occurrence wins, case-insensitive term
comparison), optionally sorts alphabetically, and renders the entries in the requested format
(bullet list or Markdown table). Pipe characters in term and definition values are escaped for safe
table cell rendering. `MarkdownFormatter` is a static class.

#### Data Model

N/A - static class with no instance state. Consumes `MarkdownOptions` with these fields (part of
the Dictionary subsystem data model):

**Format**: `OutputFormat` — selects `Bullets` or `Table` rendering.

**SortOrder**: `SortOrder` — selects `FileOrder` or `Alphabetical` sort.

**SectionHeading**: `string?` — optional heading text; emitted before entries when non-empty.

**TermHeader**: `string` — column header for the term column in table format.

**DefinitionHeader**: `string` — column header for the definition column in table format.

**HeadingDepth**: `int` — heading level (1–6) for the section heading; default 1.

#### Key Methods

**Format**: Returns the formatted Markdown string.

- *Parameters*: `IReadOnlyList<DictionaryEntry> entries` — raw entries (may contain duplicates);
  `MarkdownOptions options` — format, sorting, heading, and header settings.
- *Returns*: `string` — formatted Markdown (bullet list or table, with optional section heading).
- *Preconditions*: `entries` and `options` are non-null.
- *Postconditions*: Returns valid Markdown with all entries deduplicated; an empty entry list
  produces a valid `N/A` table row (not an error).

Processing: (1) Deduplicate — first occurrence of each term (case-insensitive) wins. (2) Sort —
alphabetical when `options.SortOrder == Alphabetical`; otherwise preserve file order. (3) Emit
optional section heading when `options.SectionHeading` is non-empty; heading level from
`options.HeadingDepth` (1–6; values outside range clamped by `Math.Clamp`). (4) Render as bullet
list (`FormatBullets`) or table (`FormatTable`) based on `options.Format`.

**FormatBullets** *(private)*: Emits one `- **{Term}**: {Definition}` line per entry.

- *Parameters*: `StringBuilder sb` — output builder;
  `IEnumerable<DictionaryEntry> entries` — deduplicated and sorted entries.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: Bullet list entries appended to `sb`.

**FormatTable** *(private)*: Emits a Markdown table with a header row, alignment row
(`| :--- | :--- |`), and one data row per entry. Uses `EscapePipe` on all term and definition
values. When the entry list is empty, emits a single `| N/A | N/A |` row.

- *Parameters*: `StringBuilder sb` — output builder;
  `IEnumerable<DictionaryEntry> entries` — deduplicated and sorted entries;
  `MarkdownOptions options` — provides `TermHeader` and `DefinitionHeader`.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: Table (including header and alignment rows) appended to `sb`.

**EscapePipe** *(private)*: Replaces `|` with `\|` in a string for safe embedding in Markdown table
cells.

- *Parameters*: `string value` — the string to escape.
- *Returns*: `string` — the escaped string.
- *Preconditions*: None.
- *Postconditions*: All pipe characters in `value` are escaped.

#### Error Handling

- `ArgumentNullException` — thrown when `entries` or `options` is null; these are programming
  errors.
- Heading depth values outside 1–6 are silently clamped by `Math.Clamp`.
- Pipe characters in values are silently escaped by `EscapePipe`.
- An empty entry list produces a valid `N/A` table row rather than an error.

#### Dependencies

- **DictionaryEntry** — Dictionary subsystem data model; input type carrying term and definition
  strings.
- **MarkdownOptions** — Dictionary subsystem data model; options bag controlling format, sort, and
  heading.
- **OutputFormat** — Dictionary subsystem enum; selects bullet-list vs. table rendering.
- **SortOrder** — Dictionary subsystem enum; selects file-order vs. alphabetical sort.

#### Callers

- **DictionaryGenerator.Generate** — constructs a `MarkdownOptions` bag from `Context` properties
  and calls `MarkdownFormatter.Format(allEntries, options)` as the fourth step of the pipeline,
  after conflict detection confirms no conflicts.
