### MarkdownFormatter

![Dictionary Subsystem Structure](DictionaryView.svg)

#### Purpose

`MarkdownFormatter.Format` omits entries whose `Visible` is false, deduplicates the rest (first
occurrence wins, case-insensitive term comparison), optionally sorts alphabetically, and renders the
entries in the requested format (bullet list or Markdown table). `pos` and `variants` are never
rendered. `MarkdownFormatter.FormatDeprecated` does the same for deprecated entries: a table with
the columns `TermHeader` (the `MarkdownOptions` default is `Term`; the generator overrides it to
`Not used` for the CLI unless `--term-header` is given), `UseHeader` (default `Use instead`) and,
only when any entry has a non-empty `where`, `WhereHeader` (default `Where it was used`), or bullets of the
form `- **{Term}**: {Use}` followed by `(where: {Where})` when present. Pipe characters in values
are escaped for safe table cell rendering. When column widths are given, the table separator dash
counts are proportional to the widths. `MarkdownFormatter` is a static class.

#### Data Model

N/A - static class with no instance state. Consumes `MarkdownOptions` with these fields (part of
the Dictionary subsystem data model):

**Format**: `OutputFormat` — selects `Bullets` or `Table` rendering.

**SortOrder**: `SortOrder` — selects `FileOrder` or `Alphabetical` sort.

**SectionHeading**: `string?` — optional heading text; emitted before entries when non-empty.

**TermHeader**: `string` — column header for the term column in table format.

**DefinitionHeader**: `string` — column header for the definition column in table format.

**HeadingDepth**: `int` — heading level (1–6) for the section heading; default 1.

**Kind**, **UseHeader**, **WhereHeader**: `InputKind` and `string` values used by
`FormatDeprecated`; `Kind` is informational for the formatter.

**ColumnWidths**: `IReadOnlyList<int>?` — relative column weights; `null` (default) keeps the fixed
separator. 2 values for terms tables, 3 for deprecated tables; ignored by bullet output.

Named constants: `TargetDashes = 100` (approximate total dash count, so a weight reads as a
percentage) and `MinimumDashes = 3` (conventional minimum that all renderers recognize).

#### Key Methods

**Format**: Returns the formatted Markdown string.

- *Parameters*: `IReadOnlyList<DictionaryEntry> entries` — raw entries (may contain duplicates);
  `MarkdownOptions options` — format, sorting, heading, and header settings.
- *Returns*: `string` — formatted Markdown (bullet list or table, with optional section heading).
- *Preconditions*: `entries` and `options` are non-null.
- *Postconditions*: Returns valid Markdown with all entries deduplicated; an empty entry list
  produces a valid `N/A` table row (not an error).

Processing: (1) Omit invisible entries, then deduplicate — first occurrence of each term (case-insensitive) wins. (2)
Sort — alphabetical when `options.SortOrder == Alphabetical`; otherwise preserve file order. (3) Emit optional section
heading when `options.SectionHeading` is non-empty; heading level from `options.HeadingDepth` (1–6; values outside range
clamped by `Math.Clamp`). (4) Render as bullet list (`FormatBullets`) or table (`FormatTable`) based on
`options.Format`.

**FormatDeprecated**: Returns the formatted Markdown string for deprecated entries.

- *Parameters*: `IReadOnlyList<DeprecatedEntry> entries`; `MarkdownOptions options`.
- *Returns*: `string` — table or bullets, with optional section heading.
- *Preconditions*: `entries` and `options` are non-null.
- *Postconditions*: Entries deduplicated; an empty list produces an `N/A` table row. With
  `ColumnWidths` set (3 values), the separator uses all 3 weights when the Where column is shown,
  and otherwise the first 2 weights (private `DeprecatedColumnWidths`), which `BuildSeparator`
  rescales over their own total. A count other than 3 throws `ArgumentException`.

**FormatBullets** *(private)*: Emits one `- **{Term}**: {Definition}` line per entry.

- *Parameters*: `StringBuilder sb` — output builder;
  `IEnumerable<DictionaryEntry> entries` — deduplicated and sorted entries.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: Bullet list entries appended to `sb`.

**FormatTable** *(private)*: Emits a Markdown table with a header row, alignment row
(from `BuildSeparator`), and one data row per entry. Uses `EscapePipe` on all term and definition
values. When the entry list is empty, emits a single `| N/A | N/A |` row.

- *Parameters*: `StringBuilder sb` — output builder;
  `IEnumerable<DictionaryEntry> entries` — deduplicated and sorted entries;
  `MarkdownOptions options` — provides `TermHeader` and `DefinitionHeader`.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: Table (including header and alignment rows) appended to `sb`.

**BuildSeparator** *(private)*: Builds the table alignment row.

- *Parameters*: `IReadOnlyList<int>? widths` — column weights or `null`; `int columns` — number of
  table columns.
- *Returns*: `string` — the separator row.
- *Preconditions*: When `widths` is non-null, its count equals `columns` (otherwise
  `ArgumentException`); every weight is positive (guaranteed by the Cli subsystem).
- *Postconditions*: Without widths, returns the fixed `| :--- | :--- |` (or three-column) row, so
  default output is byte-for-byte unchanged. With widths, returns `|` followed by ` :{dashes} |`
  per column, which is always a valid pipe-table separator with the left-alignment `:` kept.

Algorithm: `total = Σ weights` (as `long`); for each weight,
`dashes = max(MinimumDashes, (weight * 2 * TargetDashes + total) / (2 * total))`, which is
`weight * 100 / total` rounded half up using only 64-bit integer arithmetic, so it is
deterministic and cannot overflow for any `int` weights. Worked examples:

| Weights | Dash counts | Note |
| :--- | :--- | :--- |
| `25,75` | 25, 75 | exact |
| `1,2` | 33, 67 | rounded |
| `1,1,1` | 33, 33, 33 | total 99 |
| `1,1000` | 3, 100 | minimum wins; total 103 |
| `20,50,30` without Where | 29, 71 | first 2 rescaled |

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
- `ArgumentException` — thrown when table format is used with a column-width count that does not
  match the table (2 for terms, 3 for deprecated). The Cli subsystem validates the count first, so
  this is a defensive contract check.

#### Dependencies

- **DictionaryEntry** — Dictionary subsystem data model; input type carrying term and definition
  strings.
- **MarkdownOptions** — Dictionary subsystem data model; options bag controlling format, sort, and
  heading.
- **OutputFormat** — Dictionary subsystem enum; selects bullet-list vs. table rendering.
- **SortOrder** — Dictionary subsystem enum; selects file-order vs. alphabetical sort.

#### Callers

- **DictionaryGenerator.GenerateJob** — calls `MarkdownFormatter.Format(entries, job.Options)` or
  `FormatDeprecated` as the fourth step of each job's pipeline, after conflict detection confirms
  no conflicts. The options were built by `TableJob`.
