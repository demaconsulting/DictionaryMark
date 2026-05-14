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

using DemaConsulting.DictionaryMark.Cli;
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
        var options = new MarkdownOptions { Format = OutputFormat.Bullets };
        var result = MarkdownFormatter.Format(TwoEntries, options);
        Assert.Contains("- **Alpha**: First letter", result);
        Assert.Contains("- **Beta**: Second letter", result);
    }

    /// <summary>
    ///     Test that bullets with section heading includes the heading.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_BulletsWithSectionHeading_IncludesHeading()
    {
        var options = new MarkdownOptions { Format = OutputFormat.Bullets, SectionHeading = "Glossary" };
        var result = MarkdownFormatter.Format(TwoEntries, options);
        Assert.Contains("## Glossary", result);
        Assert.Contains("- **Alpha**: First letter", result);
    }

    /// <summary>
    ///     Test that table format generates a Markdown table.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_TableFormat_GeneratesTable()
    {
        var options = new MarkdownOptions { Format = OutputFormat.Table };
        var result = MarkdownFormatter.Format(TwoEntries, options);
        Assert.Contains("| Term | Definition |", result);
        Assert.Contains("| :--- | :--- |", result);
        Assert.Contains("| Alpha | First letter |", result);
        Assert.Contains("| Beta | Second letter |", result);
    }

    /// <summary>
    ///     Test that table with section heading includes the heading.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_TableWithSectionHeading_IncludesHeading()
    {
        var options = new MarkdownOptions { Format = OutputFormat.Table, SectionHeading = "Glossary" };
        var result = MarkdownFormatter.Format(TwoEntries, options);
        Assert.Contains("## Glossary", result);
        Assert.Contains("| Term | Definition |", result);
    }

    /// <summary>
    ///     Test that table with custom headers uses those headers.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_TableWithCustomHeaders_UsesCustomHeaders()
    {
        var options = new MarkdownOptions
        {
            Format = OutputFormat.Table,
            TermHeader = "Word",
            DefinitionHeader = "Meaning"
        };
        var result = MarkdownFormatter.Format(TwoEntries, options);
        Assert.Contains("| Word | Meaning |", result);
    }

    /// <summary>
    ///     Test that alphabetical sort orders entries alphabetically.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_AlphabeticalSort_SortsEntries()
    {
        var entries = new[]
        {
            new DictionaryEntry("Zeta", "Last"),
            new DictionaryEntry("Alpha", "First")
        };
        var options = new MarkdownOptions { Format = OutputFormat.Bullets, SortOrder = SortOrder.Alphabetical };
        var result = MarkdownFormatter.Format(entries, options);
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
        var entries = new[]
        {
            new DictionaryEntry("Zeta", "Last"),
            new DictionaryEntry("Alpha", "First")
        };
        var options = new MarkdownOptions { Format = OutputFormat.Bullets, SortOrder = SortOrder.FileOrder };
        var result = MarkdownFormatter.Format(entries, options);
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
        var entries = new[] { new DictionaryEntry("Term|WithPipe", "Def|WithPipe") };
        var options = new MarkdownOptions { Format = OutputFormat.Table };
        var result = MarkdownFormatter.Format(entries, options);
        Assert.Contains(@"Term\|WithPipe", result);
        Assert.Contains(@"Def\|WithPipe", result);
    }

    /// <summary>
    ///     Test that duplicate entries are deduplicated, keeping the first occurrence.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_DuplicateEntries_DeduplicatesKeepingFirst()
    {
        var entries = new[]
        {
            new DictionaryEntry("Term1", "First definition"),
            new DictionaryEntry("term1", "Second definition")  // duplicate (case-insensitive)
        };
        var options = new MarkdownOptions { Format = OutputFormat.Bullets };
        var result = MarkdownFormatter.Format(entries, options);
        Assert.Contains("First definition", result);
        Assert.DoesNotContain("Second definition", result);
    }

    /// <summary>
    ///     Test that empty entries produce empty or whitespace-only output.
    /// </summary>
    [Fact]
    public void MarkdownFormatter_Format_EmptyEntries_ReturnsEmptyOutput()
    {
        var options = new MarkdownOptions { Format = OutputFormat.Bullets };
        var result = MarkdownFormatter.Format([], options);
        Assert.True(string.IsNullOrWhiteSpace(result) || result == string.Empty);
    }
}
