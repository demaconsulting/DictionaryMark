// Copyright (c) DEMA Consulting
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using DemaConsulting.DictionaryMark.Dictionary;

namespace DemaConsulting.DictionaryMark.Tests.Dictionary;

/// <summary>
///     Unit tests for the MarkdownFormatter class.
/// </summary>
public class MarkdownFormatterTests
{
    private static readonly DictionaryEntry[] TwoEntries =
    [
        new DictionaryEntry("Alpha", "First letter"),
        new DictionaryEntry("Beta", "Second letter")
    ];

    /// <summary>
    ///     Test that bullet format generates a bullet list.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_BulletsFormat_GeneratesBulletList()
    {
        // Arrange: two entries and bullet format options
        var options = new MarkdownOptions { Format = OutputFormat.Bullets };

        // Act: format the entries as bullets
        var result = MarkdownFormatter.Format(TwoEntries, options);

        // Assert: output contains both bullet lines
        Assert.Contains("- **Alpha**: First letter", result);
        Assert.Contains("- **Beta**: Second letter", result);
    }

    /// <summary>
    ///     Test that bullets with section heading includes the heading at default depth (1).
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_BulletsWithSectionHeading_IncludesHeading()
    {
        // Arrange: bullet options with section heading set
        var options = new MarkdownOptions { Format = OutputFormat.Bullets, SectionHeading = "Glossary" };

        // Act: format the entries with a section heading
        var result = MarkdownFormatter.Format(TwoEntries, options);

        // Assert: heading at depth 1 present; depth-2 variant absent; bullet content present
        Assert.Contains("# Glossary", result);
        Assert.DoesNotContain("## Glossary", result);
        Assert.Contains("- **Alpha**: First letter", result);
    }

    /// <summary>
    ///     Test that table format generates a Markdown table.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_TableFormat_GeneratesTable()
    {
        // Arrange: two entries and table format options
        var options = new MarkdownOptions { Format = OutputFormat.Table };

        // Act: format the entries as a table
        var result = MarkdownFormatter.Format(TwoEntries, options);

        // Assert: header row, alignment row, and data rows are present
        Assert.Contains("| Term | Definition |", result);
        Assert.Contains("| :--- | :--- |", result);
        Assert.Contains("| Alpha | First letter |", result);
        Assert.Contains("| Beta | Second letter |", result);
    }

    /// <summary>
    ///     Test that table with section heading includes the heading at default depth (1).
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_TableWithSectionHeading_IncludesHeading()
    {
        // Arrange: table options with section heading set
        var options = new MarkdownOptions { Format = OutputFormat.Table, SectionHeading = "Glossary" };

        // Act: format the entries as a table with a section heading
        var result = MarkdownFormatter.Format(TwoEntries, options);

        // Assert: heading at depth 1 and table header row present
        Assert.Contains("# Glossary", result);
        Assert.Contains("| Term | Definition |", result);
    }

    /// <summary>
    ///     Test that table with custom headers uses those headers.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_TableWithCustomHeaders_UsesCustomHeaders()
    {
        // Arrange: table options with custom column headers
        var options = new MarkdownOptions
        {
            Format = OutputFormat.Table,
            TermHeader = "Word",
            DefinitionHeader = "Meaning"
        };

        // Act: format the entries with custom headers
        var result = MarkdownFormatter.Format(TwoEntries, options);

        // Assert: custom header names appear in the output
        Assert.Contains("| Word | Meaning |", result);
    }

    /// <summary>
    ///     Test that alphabetical sort orders entries alphabetically.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_AlphabeticalSort_SortsEntries()
    {
        // Arrange: entries in reverse alphabetical order with alphabetical sort requested
        var entries = new[]
        {
            new DictionaryEntry("Zeta", "Last"),
            new DictionaryEntry("Alpha", "First")
        };
        var options = new MarkdownOptions { Format = OutputFormat.Bullets, SortOrder = SortOrder.Alphabetical };

        // Act: format with alphabetical sorting
        var result = MarkdownFormatter.Format(entries, options);

        // Assert: Alpha appears before Zeta in the output
        var alphaIndex = result.IndexOf("Alpha", StringComparison.Ordinal);
        var zetaIndex = result.IndexOf("Zeta", StringComparison.Ordinal);
        Assert.True(alphaIndex < zetaIndex, "Alpha should come before Zeta in alphabetical order");
    }

    /// <summary>
    ///     Test that file order preserves the original order.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_FileOrder_PreservesOrder()
    {
        // Arrange: entries with Zeta first and file-order sort requested
        var entries = new[]
        {
            new DictionaryEntry("Zeta", "Last"),
            new DictionaryEntry("Alpha", "First")
        };
        var options = new MarkdownOptions { Format = OutputFormat.Bullets, SortOrder = SortOrder.FileOrder };

        // Act: format with file-order sorting
        var result = MarkdownFormatter.Format(entries, options);

        // Assert: Zeta appears before Alpha (original order preserved)
        var zetaIndex = result.IndexOf("Zeta", StringComparison.Ordinal);
        var alphaIndex = result.IndexOf("Alpha", StringComparison.Ordinal);
        Assert.True(zetaIndex < alphaIndex, "Zeta should come before Alpha in file order");
    }

    /// <summary>
    ///     Test that pipe characters in values are escaped in table format.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_TableWithPipeInValue_EscapesPipe()
    {
        // Arrange: entry with pipe characters in both term and definition
        var entries = new[] { new DictionaryEntry("Term|WithPipe", "Def|WithPipe") };
        var options = new MarkdownOptions { Format = OutputFormat.Table };

        // Act: format the entry as a table row
        var result = MarkdownFormatter.Format(entries, options);

        // Assert: pipe characters are escaped with backslash
        Assert.Contains(@"Term\|WithPipe", result);
        Assert.Contains(@"Def\|WithPipe", result);
    }

    /// <summary>
    ///     Test that duplicate entries are deduplicated, keeping the first occurrence.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_DuplicateEntries_DeduplicatesKeepingFirst()
    {
        // Arrange: two entries with the same term (case-insensitive) but different definitions
        var entries = new[]
        {
            new DictionaryEntry("Term1", "First definition"),
            new DictionaryEntry("term1", "Second definition")  // duplicate (case-insensitive)
        };
        var options = new MarkdownOptions { Format = OutputFormat.Bullets };

        // Act: format with deduplication applied
        var result = MarkdownFormatter.Format(entries, options);

        // Assert: only the first definition appears; second is absent
        Assert.Contains("First definition", result);
        Assert.DoesNotContain("Second definition", result);
    }

    /// <summary>
    ///     Test that empty entries produce empty or whitespace-only output.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_EmptyEntries_ReturnsEmptyOutput()
    {
        // Arrange: empty entry list with bullet format
        var options = new MarkdownOptions { Format = OutputFormat.Bullets };

        // Act: format empty entries
        var result = MarkdownFormatter.Format([], options);

        // Assert: output is empty or whitespace-only
        Assert.True(string.IsNullOrWhiteSpace(result) || result == string.Empty);
    }

    /// <summary>
    ///     Test that empty table entries emit a N/A row.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_EmptyTableEntries_EmitsNaRow()
    {
        // Arrange: empty entry list with table format
        var options = new MarkdownOptions { Format = OutputFormat.Table };

        // Act: format empty entries as table
        var result = MarkdownFormatter.Format([], options);

        // Assert: N/A placeholder row is emitted
        Assert.Contains("| N/A | N/A |", result);
    }

    /// <summary>
    ///     Test that HeadingDepth 1 emits a level-1 heading.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_HeadingDepth1_EmitsLevelOneHeading()
    {
        // Arrange: options with section heading at depth 1
        var options = new MarkdownOptions { Format = OutputFormat.Bullets, SectionHeading = "Glossary", HeadingDepth = 1 };

        // Act: format with heading depth 1
        var result = MarkdownFormatter.Format(TwoEntries, options);

        // Assert: level-1 heading present; level-2 heading absent
        Assert.Contains("# Glossary", result);
        Assert.DoesNotContain("## Glossary", result);
    }

    /// <summary>
    ///     Test that HeadingDepth 2 emits a level-2 heading.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_HeadingDepth2_EmitsLevelTwoHeading()
    {
        // Arrange: options with section heading at depth 2
        var options = new MarkdownOptions { Format = OutputFormat.Bullets, SectionHeading = "Glossary", HeadingDepth = 2 };

        // Act: format with heading depth 2
        var result = MarkdownFormatter.Format(TwoEntries, options);

        // Assert: level-2 heading present; level-3 heading absent
        Assert.Contains("## Glossary", result);
        Assert.DoesNotContain("### Glossary", result);
    }

    /// <summary>
    ///     Test that HeadingDepth 6 emits a level-6 heading.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_HeadingDepth6_EmitsLevelSixHeading()
    {
        // Arrange: options with section heading at depth 6
        var options = new MarkdownOptions { Format = OutputFormat.Bullets, SectionHeading = "Glossary", HeadingDepth = 6 };

        // Act: format with heading depth 6
        var result = MarkdownFormatter.Format(TwoEntries, options);

        // Assert: level-6 heading present
        Assert.Contains("###### Glossary", result);
    }
}
