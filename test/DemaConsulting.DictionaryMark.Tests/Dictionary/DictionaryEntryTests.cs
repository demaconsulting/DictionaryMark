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
        var entry = new DictionaryEntry("Term", "Definition");
        Assert.Equal("Term", entry.Term);
        Assert.Equal("Definition", entry.Definition);
    }

    /// <summary>
    ///     Test that constructor accepts empty strings.
    /// </summary>
    [Fact]
    public void DictionaryEntry_Constructor_EmptyStrings_SetsProperties()
    {
        var entry = new DictionaryEntry("", "");
        Assert.Equal("", entry.Term);
        Assert.Equal("", entry.Definition);
    }
}
