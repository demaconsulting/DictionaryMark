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

    /// <summary>Loads YAML text through a temporary file.</summary>
    private static IReadOnlyList<DictionaryEntry> LoadText(string yaml)
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, yaml);
            return YamlDictionaryLoader.Load(tmpFile);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    /// <summary>Loads deprecated-term YAML text through a temporary file.</summary>
    private static IReadOnlyList<DeprecatedEntry> LoadDeprecatedText(string yaml)
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tmpFile, yaml);
            return YamlDictionaryLoader.LoadDeprecated(tmpFile);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    /// <summary>Test that a plain string value loads with default metadata.</summary>
    [Fact]
    public void YamlDictionaryLoader_Load_PlainString_HasDefaultMetadata()
    {
        // Act: load a plain-string entry
        var entry = Assert.Single(LoadText("EMI: Electromagnetic Interference.\n"));

        // Assert: noun, no variants, visible, source is the full path
        Assert.Equal(["noun"], entry.Pos);
        Assert.Empty(entry.Variants);
        Assert.True(entry.Visible);
        Assert.True(Path.IsPathRooted(entry.Source));
    }

    /// <summary>Test that an object entry with every field loads.</summary>
    [Fact]
    public void YamlDictionaryLoader_Load_ObjectEntry_ReadsAllFields()
    {
        // Act: load an object entry using a list of pos values and variants
        var entry = Assert.Single(LoadText(
            "bleed valve:\n  definition: A valve that releases trapped air.\n  pos: [Noun, verb]\n  variants: [bleed-valve, Bleed-Valve, ' bleed  valve ']\n  visible: false\n"));

        // Assert: all fields are read, normalized and de-duplicated
        Assert.Equal("A valve that releases trapped air.", entry.Definition);
        Assert.Equal(["noun", "verb"], entry.Pos);
        Assert.Equal(["bleed-valve", "bleed valve"], entry.Variants);
        Assert.False(entry.Visible);
    }

    /// <summary>Test that pos may be a single string and visible may be a string.</summary>
    [Fact]
    public void YamlDictionaryLoader_Load_PosString_VisibleString_Accepted()
    {
        // Act: load an entry with a single pos string and a quoted visible value
        var entry = Assert.Single(LoadText("Acme:\n  definition: A company.\n  pos: Proper-Noun\n  variants: Acme Corp\n  visible: \"false\"\n"));

        // Assert: values are normalized
        Assert.Equal(["proper-noun"], entry.Pos);
        Assert.Equal(["Acme Corp"], entry.Variants);
        Assert.False(entry.Visible);
    }

    /// <summary>Test that terms are whitespace-normalized.</summary>
    [Fact]
    public void YamlDictionaryLoader_Load_Term_IsWhitespaceNormalized()
    {
        // Act: load a term with extra internal whitespace
        var entry = Assert.Single(LoadText("\"  Test   Mode \": A mode.\n"));

        // Assert: collapsed and trimmed
        Assert.Equal("Test Mode", entry.Term);
    }

    /// <summary>Test that an empty definition string is an error.</summary>
    [Fact]
    public void YamlDictionaryLoader_Load_EmptyDefinitionString_ThrowsNamingTermAndFile()
    {
        // Act / Assert: an empty plain-string definition is rejected naming the term
        var ex = Assert.Throws<InvalidOperationException>(() => LoadText("Zorbix: \"\"\n"));
        Assert.Contains("Term 'Zorbix'", ex.Message);
        Assert.Contains("'definition' is required", ex.Message);
    }

    /// <summary>Test that a null value is an error.</summary>
    [Theory]
    [InlineData("Zorbix:\n")]
    [InlineData("Zorbix: ~\n")]
    [InlineData("Zorbix: null\n")]
    public void YamlDictionaryLoader_Load_NullValue_Throws(string yaml)
    {
        // Act / Assert: a null entry value is an error
        var ex = Assert.Throws<InvalidOperationException>(() => LoadText(yaml));
        Assert.Contains("Term 'Zorbix'", ex.Message);
    }

    /// <summary>Test that an object entry without a definition is an error even when hidden.</summary>
    [Theory]
    [InlineData("prime:\n  pos: verb\n")]
    [InlineData("prime:\n  pos: verb\n  visible: false\n")]
    [InlineData("prime:\n  definition: \"\"\n  visible: false\n")]
    [InlineData("prime:\n  definition:\n  visible: false\n")]
    public void YamlDictionaryLoader_Load_ObjectWithoutDefinition_Throws(string yaml)
    {
        // Act / Assert: definition is required regardless of visibility
        var ex = Assert.Throws<InvalidOperationException>(() => LoadText(yaml));
        Assert.Contains("Term 'prime'", ex.Message);
        Assert.Contains("definition", ex.Message);
    }

    /// <summary>Test that an unknown key is an error naming the key.</summary>
    [Fact]
    public void YamlDictionaryLoader_Load_UnknownKey_Throws()
    {
        // Act / Assert: a typo'd key is rejected
        var ex = Assert.Throws<InvalidOperationException>(() => LoadText("prime:\n  definition: x\n  visibility: false\n"));
        Assert.Contains("unknown key 'visibility'", ex.Message);
        Assert.Contains("allowed keys are definition, pos, variants, visible", ex.Message);
    }

    /// <summary>Test that invalid pos, visible and variants values are errors.</summary>
    [Theory]
    [InlineData("a:\n  definition: x\n  pos: gerund\n", "'pos' has unsupported value 'gerund'")]
    [InlineData("a:\n  definition: x\n  pos: []\n", "'pos' must not be empty")]
    [InlineData("a:\n  definition: x\n  pos:\n", "'pos' must be")]
    [InlineData("a:\n  definition: x\n  visible: maybe\n", "'visible' must be true or false")]
    [InlineData("a:\n  definition: x\n  variants: {a: b}\n", "'variants' must be")]
    [InlineData("a:\n  definition: [x]\n", "'definition' must be a string")]
    public void YamlDictionaryLoader_Load_InvalidFieldValue_Throws(string yaml, string expected)
    {
        // Act / Assert: the error names the field problem
        var ex = Assert.Throws<InvalidOperationException>(() => LoadText(yaml));
        Assert.Contains(expected, ex.Message);
    }

    /// <summary>Test that an empty term is an error.</summary>
    [Fact]
    public void YamlDictionaryLoader_Load_EmptyTerm_Throws()
    {
        // Act / Assert
        var ex = Assert.Throws<InvalidOperationException>(() => LoadText("\" \": x\n"));
        Assert.Contains("contains an empty term", ex.Message);
    }

    /// <summary>Test that a sequence value is an error.</summary>
    [Fact]
    public void YamlDictionaryLoader_Load_SequenceValue_Throws()
    {
        // Act / Assert
        var ex = Assert.Throws<InvalidOperationException>(() => LoadText("a: [x, y]\n"));
        Assert.Contains("must be a definition string or an object", ex.Message);
    }

    /// <summary>Test that deprecated entries load with use and optional where.</summary>
    [Fact]
    public void YamlDictionaryLoader_LoadDeprecated_ValidEntries_ReturnsEntries()
    {
        // Act
        var entries = LoadDeprecatedText("bench mode: { use: Test Mode, where: Product A }\nE-stop: { use: emergency stop }\n");

        // Assert
        Assert.Equal(2, entries.Count);
        Assert.Equal("Test Mode", entries[0].Use);
        Assert.Equal("Product A", entries[0].Where);
        Assert.Null(entries[1].Where);
    }

    /// <summary>Test that deprecated entries without use, with unknown keys or non-objects are errors.</summary>
    [Theory]
    [InlineData("a: { where: Product A }\n", "must have a non-empty 'use' value")]
    [InlineData("a: { use: \"\" }\n", "must have a non-empty 'use' value")]
    [InlineData("a: { use: b, definition: c }\n", "unknown key 'definition'; allowed keys are use, where")]
    [InlineData("a: b\n", "must be an object with 'use' and optional 'where'")]
    public void YamlDictionaryLoader_LoadDeprecated_Invalid_Throws(string yaml, string expected)
    {
        // Act / Assert
        var ex = Assert.Throws<InvalidOperationException>(() => LoadDeprecatedText(yaml));
        Assert.Contains("Deprecated term 'a'", ex.Message);
        Assert.Contains(expected, ex.Message);
    }
}
