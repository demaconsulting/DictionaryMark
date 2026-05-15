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
///     Unit tests for the YamlDictionaryLoader class.
/// </summary>
public class YamlDictionaryLoaderTests
{
    /// <summary>
    ///     Test that a valid flat YAML file is loaded correctly.
    /// </summary>
    [Fact]
    public void YamlDictionaryLoader_Load_ValidFlatYaml_ReturnsEntries()
    {
        // Arrange: create a temporary YAML file with two entries
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, "Term1: Definition1\nTerm2: Definition2\n");

            // Act: load the YAML file
            var entries = YamlDictionaryLoader.Load(tmpFile);

            // Assert: two entries returned in file order with correct values
            Assert.Equal(2, entries.Count);
            Assert.Equal("Term1", entries[0].Term);
            Assert.Equal("Definition1", entries[0].Definition);
            Assert.Equal("Term2", entries[1].Term);
            Assert.Equal("Definition2", entries[1].Definition);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    /// <summary>
    ///     Test that an empty file returns an empty list.
    /// </summary>
    [Fact]
    public void YamlDictionaryLoader_Load_EmptyFile_ReturnsEmptyList()
    {
        // Arrange: create an empty temporary file
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, "");

            // Act: load the empty YAML file
            var entries = YamlDictionaryLoader.Load(tmpFile);

            // Assert: empty list returned
            Assert.Empty(entries);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    /// <summary>
    ///     Test that a non-existent file throws FileNotFoundException.
    /// </summary>
    [Fact]
    public void YamlDictionaryLoader_Load_NonExistentFile_ThrowsFileNotFoundException()
    {
        // Arrange: a path that does not exist on disk
        var path = Path.Combine(Path.GetTempPath(), "nonexistent_dictionarymark_test_file.yaml");

        // Act / Assert: loading a non-existent file throws FileNotFoundException
        Assert.Throws<FileNotFoundException>(() => YamlDictionaryLoader.Load(path));
    }

    /// <summary>
    ///     Test that nested YAML throws InvalidOperationException.
    /// </summary>
    [Fact]
    public void YamlDictionaryLoader_Load_NestedYaml_ThrowsInvalidOperationException()
    {
        // Arrange: create a temporary YAML file with a nested structure
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, "Term1:\n  nested: value\n");

            // Act / Assert: loading nested YAML throws InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => YamlDictionaryLoader.Load(tmpFile));
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    /// <summary>
    ///     Test that a YAML list (sequence) throws InvalidOperationException.
    /// </summary>
    [Fact]
    public void YamlDictionaryLoader_Load_ListYaml_ThrowsInvalidOperationException()
    {
        // Arrange: create a temporary YAML file with a sequence (list) root node
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, "- item1\n- item2\n");

            // Act / Assert: loading a YAML list throws InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => YamlDictionaryLoader.Load(tmpFile));
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    /// <summary>
    ///     Test that a single entry YAML file is loaded correctly.
    /// </summary>
    [Fact]
    public void YamlDictionaryLoader_Load_SingleEntry_ReturnsOneEntry()
    {
        // Arrange: create a temporary YAML file with a single entry
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, "API: Application Programming Interface\n");

            // Act: load the YAML file
            var entries = YamlDictionaryLoader.Load(tmpFile);

            // Assert: single entry with correct term and definition
            Assert.Single(entries);
            Assert.Equal("API", entries[0].Term);
            Assert.Equal("Application Programming Interface", entries[0].Definition);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    /// <summary>
    ///     Test that null filePath throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void YamlDictionaryLoader_Load_NullFilePath_ThrowsArgumentNullException()
    {
        // Arrange: null is the file path argument

        // Act / Assert: passing null throws ArgumentNullException
        Assert.Throws<ArgumentNullException>(() => YamlDictionaryLoader.Load(null!));
    }

    /// <summary>
    ///     Test that a YAML file with a duplicate key throws InvalidOperationException.
    /// </summary>
    [Fact]
    public void YamlDictionaryLoader_Load_DuplicateKey_ThrowsInvalidOperationException()
    {
        // Arrange: create a temporary YAML file with a duplicate key (case-insensitive)
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, "Term1: Definition1\nterm1: AnotherDefinition\n");

            // Act / Assert: loading YAML with duplicate keys throws InvalidOperationException
            Assert.Throws<InvalidOperationException>(() => YamlDictionaryLoader.Load(tmpFile));
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }
}
