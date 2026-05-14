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
        var result = ConflictDetector.Detect([]);
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that unique terms return empty conflict list.
    /// </summary>
    [Fact]
    public void ConflictDetector_Detect_UniqueTerms_ReturnsEmpty()
    {
        var entries = new[]
        {
            new DictionaryEntry("Term1", "Def1"),
            new DictionaryEntry("Term2", "Def2")
        };
        var result = ConflictDetector.Detect(entries);
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that exact duplicates (same term + same definition) are not conflicts.
    /// </summary>
    [Fact]
    public void ConflictDetector_Detect_ExactDuplicate_ReturnsEmpty()
    {
        // Same term + same definition = no conflict
        var entries = new[]
        {
            new DictionaryEntry("Term1", "Def1"),
            new DictionaryEntry("Term1", "Def1")
        };
        var result = ConflictDetector.Detect(entries);
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that same term with different definitions is a conflict.
    /// </summary>
    [Fact]
    public void ConflictDetector_Detect_SameTermDifferentDefinition_ReturnsConflict()
    {
        var entries = new[]
        {
            new DictionaryEntry("Term1", "Def1"),
            new DictionaryEntry("Term1", "DifferentDef")
        };
        var result = ConflictDetector.Detect(entries);
        Assert.Single(result);
        Assert.Contains("Term1", result[0]);
    }

    /// <summary>
    ///     Test that case-insensitive term comparison detects conflicts.
    /// </summary>
    [Fact]
    public void ConflictDetector_Detect_CaseInsensitiveConflict_Detected()
    {
        var entries = new[]
        {
            new DictionaryEntry("term1", "Def1"),
            new DictionaryEntry("TERM1", "DifferentDef")
        };
        var result = ConflictDetector.Detect(entries);
        Assert.Single(result);
    }

    /// <summary>
    ///     Test that multiple conflicts are all reported.
    /// </summary>
    [Fact]
    public void ConflictDetector_Detect_MultipleConflicts_ReturnsAll()
    {
        var entries = new[]
        {
            new DictionaryEntry("Term1", "Def1"),
            new DictionaryEntry("Term1", "ConflictDef1"),
            new DictionaryEntry("Term2", "Def2"),
            new DictionaryEntry("Term2", "ConflictDef2")
        };
        var result = ConflictDetector.Detect(entries);
        Assert.Equal(2, result.Count);
    }
}
