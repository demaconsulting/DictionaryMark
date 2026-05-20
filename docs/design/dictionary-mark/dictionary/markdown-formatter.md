### MarkdownFormatter Design

The `MarkdownFormatter` class formats dictionary entries as Markdown text, supporting both
bullet-list and table output formats with optional section headings and sort order.

#### Purpose

`MarkdownFormatter.Format` deduplicates entries (first occurrence wins, case-insensitive),
optionally sorts alphabetically, and then renders the entries in the requested format.
Pipe characters in term and definition values are escaped for safe table cell rendering.

#### Data Model

`MarkdownFormatter` is a `static` class with no instance state.

`MarkdownFormatter` consumes `MarkdownOptions` with the following fields:

- `Format` (`OutputFormat`) — Selects bullet-list (`Bullets`) or table (`Table`) rendering.
- `SortOrder` (`SortOrder`) — Selects file-order or alphabetical sort.
- `SectionHeading` (`string?`) — Optional heading text; emitted before entries when non-empty.
- `TermHeader` (`string`) — Column header for the term column in table format.
- `DefinitionHeader` (`string`) — Column header for the definition column in table format.
- `HeadingDepth` (`int`) — Heading level (1–6) for the section heading; default 1.

#### Key Methods

##### Format(IReadOnlyList\<DictionaryEntry\> entries, MarkdownOptions options) → string

Returns the formatted Markdown string.

**Processing steps:**

1. Deduplicate entries - first occurrence of each term (case-insensitive) wins.
2. Sort - alphabetical by term when `options.SortOrder == Alphabetical`; otherwise preserve
   file order.
3. Emit optional section heading when `options.SectionHeading` is non-empty. The heading
   level is derived from `options.HeadingDepth` (e.g., depth 1 produces `# {SectionHeading}`,
   depth 2 produces `## {SectionHeading}`). Values outside the range 1–6 are clamped to that
   range by `Math.Clamp` before the heading is emitted.
4. Render as bullet list (`FormatBullets`) or table (`FormatTable`) based on `options.Format`.

**Throws:** `ArgumentNullException` - when `entries` or `options` is null.

##### FormatBullets *(private)*

Emits one `- **{Term}**: {Definition}` line per entry.

##### FormatTable *(private)*

Emits a Markdown table with a header row, alignment row (`| :--- | :--- |`), and one data
row per entry. Uses `EscapePipe` on all term and definition values. When the entry list is
empty, a single `| N/A | N/A |` row is emitted in place of data rows to produce a valid
non-empty table.

##### EscapePipe *(private)*

Replaces `|` with `\|` in a string for safe embedding in Markdown table cells.

#### Error Handling

`MarkdownFormatter.Format` throws `ArgumentNullException` when `entries` or `options` is null.
These are programming errors; callers must supply non-null arguments.

No other error conditions exist: heading depth values outside the range 1–6 are silently clamped
by `Math.Clamp`, pipe characters in term or definition values are silently escaped by
`EscapePipe`, and an empty entry list produces a valid `N/A` table row rather than an error.

#### Interactions

| Dependency        | Role                                                                   |
| ----------------- | ---------------------------------------------------------------------- |
| `DictionaryEntry` | Input data type carrying term and definition strings.                  |
| `MarkdownOptions` | Carries the format, sort order, headers, and optional section heading. |
| `OutputFormat`    | Enum selecting bullet-list vs table rendering.                         |
| `SortOrder`       | Enum selecting file-order vs alphabetical sort.                        |

#### Dependencies

| Dependency        | Role                                                                                 |
| ----------------- | ------------------------------------------------------------------------------------ |
| `DictionaryEntry` | Dictionary subsystem data model — input type carrying term and definition strings.   |
| `MarkdownOptions` | Dictionary subsystem data model — options bag controlling format, sort, and heading. |
| `OutputFormat`    | Dictionary subsystem enum — selects bullet-list vs table rendering.                  |
| `SortOrder`       | Dictionary subsystem enum — selects file-order vs alphabetical sort.                 |

#### Callers

`DictionaryGenerator.Generate` constructs a `MarkdownOptions` bag from the `Context`
properties and calls `MarkdownFormatter.Format(allEntries, options)` as the fourth step of
the pipeline, after conflict detection has confirmed no conflicts exist.
