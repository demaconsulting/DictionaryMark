### MarkdownFormatter

#### Verification Approach

`MarkdownFormatter` is a `static` class with no external I/O dependencies. Tests construct
`DictionaryEntry` arrays and `MarkdownOptions` objects inline with controlled values and pass them
directly to `MarkdownFormatter.Format`, asserting on the content and ordering of the returned string.
No mocking or stubbing is required because all behavior is self-contained and deterministic.

#### Test Environment

N/A - standard test environment.

#### Acceptance Criteria

- All unit tests in `MarkdownFormatterTests.cs` pass with zero failures.
- No tests are skipped or marked as expected failures.

#### Test Scenarios

**MarkdownFormatter_Format_BulletsFormat_GeneratesBulletList**: Verifies that `OutputFormat.Bullets`
with two entries produces correctly formatted bullet lines in `- **Term**: Definition` form, confirming
the basic bullet rendering path. This scenario is tested by
`MarkdownFormatter_Format_BulletsFormat_GeneratesBulletList`.

**MarkdownFormatter_Format_BulletsWithSectionHeading_IncludesHeading**: Verifies that bullet format
with `SectionHeading = "Glossary"` and the default `HeadingDepth` of 1 produces a `# Glossary` heading
above the bullet entries, confirming that a level-1 section heading is emitted correctly. This scenario
is tested by `MarkdownFormatter_Format_BulletsWithSectionHeading_IncludesHeading`.

**MarkdownFormatter_Format_TableFormat_GeneratesTable**: Verifies that `OutputFormat.Table` with two
entries produces a complete Markdown table including the header row, alignment row, and data rows,
confirming the basic table rendering path. This scenario is tested by
`MarkdownFormatter_Format_TableFormat_GeneratesTable`.

**MarkdownFormatter_Format_TableWithSectionHeading_IncludesHeading**: Verifies that table format with
`SectionHeading = "Glossary"` and the default `HeadingDepth` of 1 produces a `# Glossary` heading above
the table, confirming that section headings work consistently across output formats. This scenario is
tested by `MarkdownFormatter_Format_TableWithSectionHeading_IncludesHeading`.

**MarkdownFormatter_Format_TableWithCustomHeaders_UsesCustomHeaders**: Verifies that specifying
`TermHeader = "Word"` and `DefinitionHeader = "Meaning"` causes those values to appear as the table
column headers in place of the defaults, confirming that custom header names are honored. This scenario
is tested by `MarkdownFormatter_Format_TableWithCustomHeaders_UsesCustomHeaders`.

**MarkdownFormatter_Format_AlphabeticalSort_SortsEntries**: Verifies that entries supplied in reverse
alphabetical order with `SortOrder.Alphabetical` are reordered so that `"Alpha"` appears before `"Zeta"`
in the output, confirming alphabetical sort behavior. This scenario is tested by
`MarkdownFormatter_Format_AlphabeticalSort_SortsEntries`.

**MarkdownFormatter_Format_FileOrder_PreservesOrder**: Verifies that entries supplied with `"Zeta"`
first and `SortOrder.FileOrder` preserve that original order in the output, confirming that file-order
sort does not reorder entries. This scenario is tested by
`MarkdownFormatter_Format_FileOrder_PreservesOrder`.

**MarkdownFormatter_Format_TableWithPipeInValue_EscapesPipe**: Verifies that pipe characters (`|`) in
term and definition values are replaced with `\|` in table output, confirming that `EscapePipe` prevents
malformed Markdown table cells. This scenario is tested by
`MarkdownFormatter_Format_TableWithPipeInValue_EscapesPipe`.

**MarkdownFormatter_Format_DuplicateEntries_DeduplicatesKeepingFirst**: Verifies that when two entries
share the same term (case-insensitive) but have different definitions, only the first definition appears
in the output and the second is discarded, confirming that deduplication keeps the first occurrence.
This scenario is tested by `MarkdownFormatter_Format_DuplicateEntries_DeduplicatesKeepingFirst`.

**MarkdownFormatter_Format_EmptyEntries_ReturnsEmptyOutput**: Verifies that an empty entry list with
bullet format produces empty or whitespace-only output rather than an error, confirming graceful handling
of the empty-input case. This scenario is tested by
`MarkdownFormatter_Format_EmptyEntries_ReturnsEmptyOutput`.

**MarkdownFormatter_Format_EmptyTableEntries_EmitsNaRow**: Verifies that an empty entry list with table
format produces a `| N/A | N/A |` placeholder data row rather than an empty table body, confirming that
a structurally valid non-empty table is always emitted. This scenario is tested by
`MarkdownFormatter_Format_EmptyTableEntries_EmitsNaRow`.

**MarkdownFormatter_Format_HeadingDepth1_EmitsLevelOneHeading**: Verifies that `HeadingDepth = 1` with
`SectionHeading = "Glossary"` produces `# Glossary` and not `## Glossary`, confirming that the heading
level is applied correctly at the minimum depth. This scenario is tested by
`MarkdownFormatter_Format_HeadingDepth1_EmitsLevelOneHeading`.

**MarkdownFormatter_Format_HeadingDepth2_EmitsLevelTwoHeading**: Verifies that `HeadingDepth = 2` with
`SectionHeading = "Glossary"` produces `## Glossary` and not `### Glossary`, confirming that the heading
level is applied correctly at depth 2. This scenario is tested by
`MarkdownFormatter_Format_HeadingDepth2_EmitsLevelTwoHeading`.

**MarkdownFormatter_Format_HeadingDepth6_EmitsLevelSixHeading**: Verifies that `HeadingDepth = 6` with
`SectionHeading = "Glossary"` produces `###### Glossary`, confirming that the maximum heading depth is
rendered correctly and clamping to the valid range of 1–6 does not corrupt the output. This scenario is
tested by `MarkdownFormatter_Format_HeadingDepth6_EmitsLevelSixHeading`.
