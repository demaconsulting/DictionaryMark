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
}
