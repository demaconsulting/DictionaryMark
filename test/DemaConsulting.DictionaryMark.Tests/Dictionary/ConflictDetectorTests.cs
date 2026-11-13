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
///     Unit tests for the ConflictDetector class.
/// </summary>
public class ConflictDetectorTests
{
    /// <summary>
    ///     Test that no entries returns empty list.
    /// </summary>
    [Fact]
    public void ConflictDetector_Detect_NoEntries_ReturnsEmpty()
    {
        // Arrange: empty entry collection

        // Act: detect conflicts in empty collection
        var result = ConflictDetector.Detect([]);

        // Assert: no conflicts reported
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that unique terms return empty conflict list.
    /// </summary>
    [Fact]
    public void ConflictDetector_Detect_UniqueTerms_ReturnsEmpty()
    {
        // Arrange: two entries with distinct terms
        var entries = new[]
        {
            new DictionaryEntry("Term1", "Def1"),
            new DictionaryEntry("Term2", "Def2")
        };

        // Act: detect conflicts among unique terms
        var result = ConflictDetector.Detect(entries);

        // Assert: no conflicts reported
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that exact duplicates (same term + same definition) are not conflicts.
    /// </summary>
    [Fact]
    public void ConflictDetector_Detect_ExactDuplicate_ReturnsEmpty()
    {
        // Arrange: two entries with identical term and identical definition
        var entries = new[]
        {
            new DictionaryEntry("Term1", "Def1"),
            new DictionaryEntry("Term1", "Def1")
        };

        // Act: detect conflicts among exact duplicates
        var result = ConflictDetector.Detect(entries);

        // Assert: same term + same definition is deduplication, not a conflict
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that same term with different definitions is a conflict.
    /// </summary>
    [Fact]
    public void ConflictDetector_Detect_SameTermDifferentDefinition_ReturnsConflict()
    {
        // Arrange: two entries with same term but different definitions
        var entries = new[]
        {
            new DictionaryEntry("Term1", "Def1"),
            new DictionaryEntry("Term1", "DifferentDef")
        };

        // Act: detect conflicts
        var result = ConflictDetector.Detect(entries);

        // Assert: one conflict message referencing the term
        Assert.Single(result);
        Assert.Contains("Term1", result[0]);
    }

    /// <summary>
    ///     Test that case-insensitive term comparison detects conflicts.
    /// </summary>
    [Fact]
    public void ConflictDetector_Detect_CaseInsensitiveConflict_Detected()
    {
        // Arrange: entries with same term in different cases but different definitions
        var entries = new[]
        {
            new DictionaryEntry("term1", "Def1"),
            new DictionaryEntry("TERM1", "DifferentDef")
        };

        // Act: detect conflicts with case-insensitive term comparison
        var result = ConflictDetector.Detect(entries);

        // Assert: one conflict reported
        Assert.Single(result);
    }

    /// <summary>
    ///     Test that multiple conflicts are all reported.
    /// </summary>
    [Fact]
    public void ConflictDetector_Detect_MultipleConflicts_ReturnsAll()
    {
        // Arrange: two pairs of conflicting entries
        var entries = new[]
        {
            new DictionaryEntry("Term1", "Def1"),
            new DictionaryEntry("Term1", "ConflictDef1"),
            new DictionaryEntry("Term2", "Def2"),
            new DictionaryEntry("Term2", "ConflictDef2")
        };

        // Act: detect conflicts among all entries
        var result = ConflictDetector.Detect(entries);

        // Assert: two conflict messages returned (one per conflicting term)
        Assert.Equal(2, result.Count);
    }

    /// <summary>
    ///     Test that null entries throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void ConflictDetector_Detect_NullEntries_ThrowsArgumentNullException()
    {
        // Arrange: null as the entries argument

        // Act / Assert: passing null throws ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => ConflictDetector.Detect(null!));
    }

    /// <summary>Creates an entry with metadata and a source.</summary>
    private static DictionaryEntry Entry(
        string term, string source, string definition = "Def", string[]? pos = null, string[]? variants = null, bool visible = true) =>
        new(term, definition, pos, variants, visible, source);

    /// <summary>Test that identical entries from different files are not a conflict.</summary>
    [Fact]
    public void ConflictDetector_Detect_IdenticalRichEntries_ReturnsEmpty()
    {
        // Act: same term, equal metadata (case-insensitive term, set-equal pos and variants)
        var result = ConflictDetector.Detect(
        [
            Entry("Prime", "a.yaml", pos: ["noun", "verb"], variants: ["Pri"]),
            Entry("prime", "b.yaml", pos: ["verb", "noun"], variants: ["pri"])
        ]);

        // Assert
        Assert.Empty(result);
    }

    /// <summary>Test that each differing field is reported by name.</summary>
    [Theory]
    [InlineData("definition")]
    [InlineData("pos")]
    [InlineData("variants")]
    [InlineData("visible")]
    public void ConflictDetector_Detect_DifferingField_ReportsFieldAndBothSources(string field)
    {
        // Arrange: the second entry differs only in the named field
        var second = field switch
        {
            "definition" => Entry("prime", "b.yaml", definition: "Other"),
            "pos" => Entry("prime", "b.yaml", pos: ["verb"]),
            "variants" => Entry("prime", "b.yaml", variants: ["x"]),
            _ => Entry("prime", "b.yaml", visible: false)
        };

        // Act
        var result = ConflictDetector.Detect([Entry("prime", "a.yaml"), second]);

        // Assert
        Assert.Equal([$"Conflict: term 'prime' differs in {field} between a.yaml and b.yaml."], result);
    }

    /// <summary>Test that several differing fields are listed in a fixed order.</summary>
    [Fact]
    public void ConflictDetector_Detect_SeveralFields_ListsInFixedOrder()
    {
        // Act
        var result = ConflictDetector.Detect(
        [
            Entry("prime", "a.yaml"),
            Entry("prime", "b.yaml", definition: "Other", pos: ["verb"], visible: false)
        ]);

        // Assert
        Assert.Equal(["Conflict: term 'prime' differs in definition, pos, visible between a.yaml and b.yaml."], result);
    }

    /// <summary>Test that the result does not depend on input order.</summary>
    [Fact]
    public void ConflictDetector_Detect_InputOrder_DoesNotChangeResult()
    {
        // Arrange
        var a = Entry("prime", "a.yaml");
        var b = Entry("prime", "b.yaml", pos: ["verb"]);

        // Act / Assert: both orders give the same message
        Assert.Equal(ConflictDetector.Detect([a, b]), ConflictDetector.Detect([b, a]));
    }

    /// <summary>Test that a hidden entry still takes part in conflict detection.</summary>
    [Fact]
    public void ConflictDetector_Detect_HiddenEntry_StillConflicts()
    {
        // Act: hidden definition differs from the visible one
        var result = ConflictDetector.Detect(
        [
            Entry("prime", "a.yaml", visible: false),
            Entry("prime", "b.yaml", visible: false, definition: "Other")
        ]);

        // Assert
        Assert.Single(result);
    }

    /// <summary>Test deprecated-versus-deprecated conflicts.</summary>
    [Fact]
    public void ConflictDetector_Detect_DeprecatedDiffer_ReportsUseAndWhere()
    {
        // Act
        var result = ConflictDetector.Detect(
            [],
            [
                new DeprecatedEntry("bench mode", "Test Mode", "Product A", "a.yaml"),
                new DeprecatedEntry("Bench Mode", "Other", null, "b.yaml")
            ]);

        // Assert
        Assert.Equal(["Conflict: deprecated term 'bench mode' differs in use, where between a.yaml and b.yaml."], result);
    }

    /// <summary>Test identical deprecated duplicates collapse.</summary>
    [Fact]
    public void ConflictDetector_Detect_DeprecatedIdentical_ReturnsEmpty()
    {
        // Act / Assert
        Assert.Empty(ConflictDetector.Detect(
            [],
            [new DeprecatedEntry("a", "b", null, "x.yaml"), new DeprecatedEntry("A", "b", null, "y.yaml")]));
    }

    /// <summary>Test a term that is both approved and deprecated.</summary>
    [Fact]
    public void ConflictDetector_Detect_ApprovedAndDeprecated_ReportsConflict()
    {
        // Act
        var result = ConflictDetector.Detect(
            [Entry("Test Mode", "a.yaml")],
            [new DeprecatedEntry("test mode", "Other", null, "d.yaml")]);

        // Assert
        Assert.Equal(["Conflict: term 'Test Mode' is both approved (a.yaml) and deprecated (d.yaml)."], result);
    }

    /// <summary>Test an approved variant that is deprecated.</summary>
    [Fact]
    public void ConflictDetector_Detect_VariantDeprecated_ReportsConflict()
    {
        // Act
        var result = ConflictDetector.Detect(
            [Entry("bleed valve", "a.yaml", variants: ["bleed-valve"])],
            [new DeprecatedEntry("Bleed-Valve", "Other", null, "d.yaml")]);

        // Assert
        Assert.Equal(["Conflict: variant 'bleed-valve' of term 'bleed valve' is approved (a.yaml) but also deprecated (d.yaml)."], result);
    }

    /// <summary>Test that an entry without a source renders as unknown.</summary>
    [Fact]
    public void ConflictDetector_Detect_NoSource_RendersUnknown()
    {
        // Act
        var result = ConflictDetector.Detect([new DictionaryEntry("a", "x"), new DictionaryEntry("a", "y")]);

        // Assert
        Assert.Equal(["Conflict: term 'a' differs in definition between <unknown> and <unknown>."], result);
    }
}
