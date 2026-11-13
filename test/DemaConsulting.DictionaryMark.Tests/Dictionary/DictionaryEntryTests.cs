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
///     Unit tests for the DictionaryEntry class.
/// </summary>
public class DictionaryEntryTests
{
    /// <summary>
    ///     Test that constructor sets Term and Definition properties correctly.
    /// </summary>
    [Fact]
    public void DictionaryEntry_Constructor_ValidArgs_SetsProperties()
    {
        // Arrange: term and definition strings for the new entry

        // Act: construct a DictionaryEntry with known values
        var entry = new DictionaryEntry("Term", "Definition");

        // Assert: Term and Definition properties match the constructor arguments
        Assert.Equal("Term", entry.Term);
        Assert.Equal("Definition", entry.Definition);
    }

    /// <summary>
    ///     Test that constructor accepts empty strings.
    /// </summary>
    [Fact]
    public void DictionaryEntry_Constructor_EmptyStrings_SetsProperties()
    {
        // Arrange: empty strings as term and definition

        // Act: construct a DictionaryEntry with empty values
        var entry = new DictionaryEntry("", "");

        // Assert: Term and Definition are both empty strings
        Assert.Equal("", entry.Term);
        Assert.Equal("", entry.Definition);
    }

    /// <summary>Test that the short constructor applies default metadata.</summary>
    [Fact]
    public void DictionaryEntry_Constructor_TermAndDefinition_HasDefaultMetadata()
    {
        // Act
        var entry = new DictionaryEntry("Term", "Def");

        // Assert
        Assert.Equal(["noun"], entry.Pos);
        Assert.Empty(entry.Variants);
        Assert.True(entry.Visible);
        Assert.Equal("", entry.Source);
    }

    /// <summary>Test that the full constructor stores the metadata.</summary>
    [Fact]
    public void DictionaryEntry_Constructor_FullMetadata_SetsProperties()
    {
        // Act
        var entry = new DictionaryEntry("Term", "Def", ["verb"], ["alt"], false, "a.yaml");

        // Assert
        Assert.Equal(["verb"], entry.Pos);
        Assert.Equal(["alt"], entry.Variants);
        Assert.False(entry.Visible);
        Assert.Equal("a.yaml", entry.Source);
    }

    /// <summary>Test that Pos and Variants cannot be mutated through a cast or via the source collections.</summary>
    [Fact]
    public void DictionaryEntry_Constructor_Collections_AreReadOnlyCopies()
    {
        // Arrange
        var pos = new List<string> { "verb" };
        var variants = new List<string> { "alt" };

        // Act
        var entry = new DictionaryEntry("Term", "Def", pos, variants, true, "a.yaml");
        pos.Add("noun");
        variants.Add("other");

        // Assert
        Assert.Equal(["verb"], entry.Pos);
        Assert.Equal(["alt"], entry.Variants);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)entry.Pos).Add("x"));
        Assert.Throws<NotSupportedException>(() => ((IList<string>)entry.Variants).Add("x"));
    }

    /// <summary>Test that a null definition is rejected.</summary>
    [Fact]
    public void DictionaryEntry_Constructor_NullDefinition_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new DictionaryEntry("Term", null!));
    }
}
