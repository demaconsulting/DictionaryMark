### MarkdownFormatter Verification

This document describes the unit-level verification design for the `MarkdownFormatter` unit.
It defines test scenarios, dependency usage, and requirement coverage for
`MarkdownFormatterTests.cs`.

#### Verification Approach

`MarkdownFormatter` is verified with unit tests in `MarkdownFormatterTests.cs`. Tests
construct `DictionaryEntry` arrays and `MarkdownOptions` objects with controlled settings,
invoke `MarkdownFormatter.Format`, and assert on the content and ordering of the output string.

#### Dependencies

| Dependency        | Usage in Tests                                                  |
| ----------------- | --------------------------------------------------------------- |
| `DictionaryEntry` | Constructed inline with test term/definition values.            |
| `MarkdownOptions` | Constructed with controlled format, sort, and heading settings. |

#### Test Scenarios

##### MarkdownFormatter_Format_BulletsFormat_GeneratesBulletList

**Scenario**: `OutputFormat.Bullets` with two entries.

**Expected**: Output contains `"- **Alpha**: First letter"` and `"- **Beta**: Second letter"`.

##### MarkdownFormatter_Format_BulletsWithSectionHeading_IncludesHeading

**Scenario**: Bullets format with `SectionHeading = "Glossary"` (default `HeadingDepth = 1`).

**Expected**: Output contains `"# Glossary"` and the bullet entries.

##### MarkdownFormatter_Format_TableFormat_GeneratesTable

**Scenario**: `OutputFormat.Table` with two entries.

**Expected**: Output contains header row, alignment row, and data rows.

##### MarkdownFormatter_Format_TableWithSectionHeading_IncludesHeading

**Scenario**: Table format with `SectionHeading = "Glossary"` (default `HeadingDepth = 1`).

**Expected**: Output contains `"# Glossary"` and table header row.

##### MarkdownFormatter_Format_TableWithCustomHeaders_UsesCustomHeaders

**Scenario**: Table format with `TermHeader = "Word"` and `DefinitionHeader = "Meaning"`.

**Expected**: Output contains `"| Word | Meaning |"`.

##### MarkdownFormatter_Format_AlphabeticalSort_SortsEntries

**Scenario**: Entries in reverse alphabetical order, `SortOrder.Alphabetical`.

**Expected**: `"Alpha"` appears before `"Zeta"` in output.

##### MarkdownFormatter_Format_FileOrder_PreservesOrder

**Scenario**: Entries with `"Zeta"` first, `SortOrder.FileOrder`.

**Expected**: `"Zeta"` appears before `"Alpha"` in output.

##### MarkdownFormatter_Format_TableWithPipeInValue_EscapesPipe

**Scenario**: Entry with `"|"` in term and definition, table format.

**Expected**: Output contains `Term\|WithPipe` and `Def\|WithPipe`.

##### MarkdownFormatter_Format_DuplicateEntries_DeduplicatesKeepingFirst

**Scenario**: Two entries with same term (case-insensitive) but different definitions.

**Expected**: Output contains first definition; second definition absent.

##### MarkdownFormatter_Format_EmptyEntries_ReturnsEmptyOutput

**Scenario**: Empty entry list passed to `Format` with bullet format.

**Expected**: Output is empty or whitespace-only.

##### MarkdownFormatter_Format_EmptyTableEntries_EmitsNaRow

**Scenario**: Empty entry list passed to `Format` with table format.

**Expected**: Output contains `"| N/A | N/A |"`.

##### MarkdownFormatter_Format_HeadingDepth1_EmitsLevelOneHeading

**Scenario**: `SectionHeading = "Glossary"` with `HeadingDepth = 1`.

**Expected**: Output contains `"# Glossary"` and does not contain `"## Glossary"`.

##### MarkdownFormatter_Format_HeadingDepth2_EmitsLevelTwoHeading

**Scenario**: `SectionHeading = "Glossary"` with `HeadingDepth = 2`.

**Expected**: Output contains `"## Glossary"` and does not contain `"### Glossary"`.

##### MarkdownFormatter_Format_HeadingDepth6_EmitsLevelSixHeading

**Scenario**: `SectionHeading = "Glossary"` with `HeadingDepth = 6`.

**Expected**: Output contains `"###### Glossary"`.

#### Requirements Coverage

- **`DictionaryMark-Formatter-Bullets`**: MarkdownFormatter_Format_BulletsFormat_GeneratesBulletList,
  MarkdownFormatter_Format_BulletsWithSectionHeading_IncludesHeading.
- **`DictionaryMark-Formatter-Table`**: MarkdownFormatter_Format_TableFormat_GeneratesTable,
  MarkdownFormatter_Format_TableWithSectionHeading_IncludesHeading,
  MarkdownFormatter_Format_TableWithCustomHeaders_UsesCustomHeaders,
  MarkdownFormatter_Format_TableWithPipeInValue_EscapesPipe.
- **`DictionaryMark-Formatter-Sorting`**: MarkdownFormatter_Format_AlphabeticalSort_SortsEntries,
  MarkdownFormatter_Format_FileOrder_PreservesOrder.
- **`DictionaryMark-Formatter-Deduplication`**: MarkdownFormatter_Format_DuplicateEntries_DeduplicatesKeepingFirst.
- **`DictionaryMark-Formatter-SectionHeading`**: MarkdownFormatter_Format_BulletsWithSectionHeading_IncludesHeading,
  MarkdownFormatter_Format_TableWithSectionHeading_IncludesHeading,
  MarkdownFormatter_Format_HeadingDepth1_EmitsLevelOneHeading,
  MarkdownFormatter_Format_HeadingDepth2_EmitsLevelTwoHeading,
  MarkdownFormatter_Format_HeadingDepth6_EmitsLevelSixHeading.
