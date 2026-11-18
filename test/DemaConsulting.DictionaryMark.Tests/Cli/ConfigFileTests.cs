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
using DemaConsulting.DictionaryMark.Utilities;

namespace DemaConsulting.DictionaryMark.Tests.Cli;

/// <summary>
///     Unit tests for the ConfigFile class.
/// </summary>
[Collection("Sequential")]
public class ConfigFileTests
{
    /// <summary>Name of the environment variable used by these tests.</summary>
    private const string VariableName = "DICTIONARYMARK_CONFIG_TEST";

    /// <summary>A minimal valid table job, appended to configs that only test top-level keys.</summary>
    private const string MinimalTables = "tables:\n  - input: a.yaml\n    output: a.md\n";

    /// <summary>Writes a config file in a sub-directory of the temporary directory and returns its path.</summary>
    /// <param name="tmpDir">The temporary directory.</param>
    /// <param name="content">The YAML content.</param>
    /// <returns>The full path of the config file.</returns>
    private static string WriteConfig(TemporaryDirectory tmpDir, string content)
    {
        var path = tmpDir.GetFilePath(Path.Combine("config", "dictionarymark.yaml"));
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Test that every formatting key and the tables are parsed.</summary>
    [Fact]
    public void ConfigFile_Load_DefaultsAndTables_ParsesAllKeys()
    {
        // Arrange: a config using every top-level formatting key and one job overriding some
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir,
            "format: TABLE\nsection: Glossary\ndepth: 2\nsort: alpha\nterm-header: Word\ndef-header: Meaning\n" +
            "kind: deprecated\nuse-header: Prefer\nwhere-header: Seen in\ncolumn-widths: [20, 50, 30]\n" +
            "tables:\n  - input: a.yaml\n    output: out/a.md\n    format: bullets\n    kind: terms\n    column-widths: [1, 3]\n");

        // Act: load the config
        var config = ConfigFile.Load(path);

        // Assert: defaults, job and paths are all read
        Assert.Equal(path, config.FilePath);
        Assert.Equal(Path.GetDirectoryName(path), config.BaseDirectory);
        Assert.Null(config.RootDirectory);
        Assert.Equal(OutputFormat.Table, config.Defaults.Format);
        Assert.Equal("Glossary", config.Defaults.SectionHeading);
        Assert.Equal(2, config.Defaults.HeadingDepth);
        Assert.Equal(SortOrder.Alphabetical, config.Defaults.SortBy);
        Assert.Equal("Word", config.Defaults.TermHeader);
        Assert.Equal("Meaning", config.Defaults.DefinitionHeader);
        Assert.Equal(InputKind.Deprecated, config.Defaults.Kind);
        Assert.Equal("Prefer", config.Defaults.UseHeader);
        Assert.Equal("Seen in", config.Defaults.WhereHeader);
        Assert.Equal([20, 50, 30], config.Defaults.ColumnWidths!);
        var table = Assert.Single(config.Tables);
        Assert.Equal(1, table.Index);
        Assert.Equal(["a.yaml"], table.Inputs);
        Assert.Equal("out/a.md", table.Output);
        Assert.Equal(OutputFormat.Bullets, table.Settings.Format);
        Assert.Equal(InputKind.Terms, table.Settings.Kind);
        Assert.Equal([1, 3], table.Settings.ColumnWidths!);
    }

    /// <summary>Test that a string input gives a single pattern.</summary>
    [Fact]
    public void ConfigFile_Load_InputAsString_ReturnsSinglePattern()
    {
        // Arrange: a job with a string input
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "tables:\n  - input: ../shared/*.yaml\n    output: a.md\n");

        // Act: load the config
        var config = ConfigFile.Load(path);

        // Assert: the input is a single pattern, kept as written
        Assert.Equal(["../shared/*.yaml"], config.Tables[0].Inputs);
    }

    /// <summary>Test that a sequence input gives every pattern.</summary>
    [Fact]
    public void ConfigFile_Load_InputAsSequence_ReturnsAllPatterns()
    {
        // Arrange: a job with a sequence input, and a second job
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir,
            "tables:\n  - input: [a.yaml, b/*.yaml]\n    output: a.md\n  - input: c.yaml\n    output: c.md\n");

        // Act: load the config
        var config = ConfigFile.Load(path);

        // Assert: both patterns are kept in order, and jobs are numbered from 1
        Assert.Equal(["a.yaml", "b/*.yaml"], config.Tables[0].Inputs);
        Assert.Equal(2, config.Tables[1].Index);
    }

    /// <summary>Test that the definition-header alias is accepted.</summary>
    [Fact]
    public void ConfigFile_Load_DefinitionHeaderAlias_IsAccepted()
    {
        // Arrange: the long alias of def-header
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "definition-header: Meaning\n");

        // Act: load the config
        var config = ConfigFile.Load(path);

        // Assert: the alias sets the definition header, and no tables are defined
        Assert.Equal("Meaning", config.Defaults.DefinitionHeader);
        Assert.Empty(config.Tables);
    }

    /// <summary>Test that giving both definition-header aliases is an error.</summary>
    [Fact]
    public void ConfigFile_Load_BothDefHeaderAliases_Throws()
    {
        // Arrange: both aliases in one mapping
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "def-header: A\ndefinition-header: B\n");

        // Act / Assert: the error names both keys
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Contains("'def-header' and 'definition-header'", ex.Message);
    }

    /// <summary>Test that a missing file is an error naming the file.</summary>
    [Fact]
    public void ConfigFile_Load_MissingFile_ThrowsNamingFile()
    {
        // Arrange: a path that does not exist
        using var tmpDir = new TemporaryDirectory();
        var path = Path.Combine(tmpDir.DirectoryPath, "missing.yaml");

        // Act / Assert: the error names the file
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Equal($"Config file '{path}': file not found", ex.Message);
    }

    /// <summary>Test that invalid YAML is an error naming the file.</summary>
    [Fact]
    public void ConfigFile_Load_InvalidYaml_ThrowsNamingFile()
    {
        // Arrange: malformed YAML
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "format: [table\n");

        // Act / Assert: the error names the file and says the YAML is invalid
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.StartsWith($"Config file '{path}': invalid YAML", ex.Message);
    }

    /// <summary>Test that a non-mapping root is an error.</summary>
    [Fact]
    public void ConfigFile_Load_NonMappingRoot_Throws()
    {
        // Arrange: a sequence at the root
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "- a\n- b\n");

        // Act / Assert: the error says the root must be a mapping
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Contains("root must be a YAML mapping", ex.Message);
    }

    /// <summary>Test that an empty file is an error.</summary>
    [Fact]
    public void ConfigFile_Load_EmptyFile_Throws()
    {
        // Arrange: an empty file
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, string.Empty);

        // Act / Assert: the error says the file is empty
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Equal($"Config file '{path}': file is empty", ex.Message);
    }

    /// <summary>Test that tables that are not a sequence are an error.</summary>
    [Fact]
    public void ConfigFile_Load_TablesNotSequence_Throws()
    {
        // Arrange: tables as a mapping
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "tables:\n  input: a.yaml\n");

        // Act / Assert: the error names the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Contains("key 'tables' must be a non-empty sequence", ex.Message);
    }

    /// <summary>Test that empty tables are an error.</summary>
    [Fact]
    public void ConfigFile_Load_TablesEmpty_Throws()
    {
        // Arrange: an empty tables sequence
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "tables: []\n");

        // Act / Assert: the error names the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Contains("key 'tables' must be a non-empty sequence", ex.Message);
    }

    /// <summary>Test that a job that is not a mapping is an error naming the job.</summary>
    [Fact]
    public void ConfigFile_Load_JobNotMapping_ThrowsNamingJob()
    {
        // Arrange: a scalar job
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "tables:\n  - a.yaml\n");

        // Act / Assert: the error names the file and the job
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.StartsWith($"Config file '{path}': table job 1: must be a mapping", ex.Message);
    }

    /// <summary>Test that a job without input is an error naming the job.</summary>
    [Fact]
    public void ConfigFile_Load_JobMissingInput_ThrowsNamingJob()
    {
        // Arrange: the second job lacks input
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, MinimalTables + "  - output: b.md\n");

        // Act / Assert: the error names the job and the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Equal($"Config file '{path}': table job 2: missing required key 'input'", ex.Message);
    }

    /// <summary>Test that a job without output is an error naming the job.</summary>
    [Fact]
    public void ConfigFile_Load_JobMissingOutput_ThrowsNamingJob()
    {
        // Arrange: a job without output
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "tables:\n  - input: a.yaml\n");

        // Act / Assert: the error names the job and the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Equal($"Config file '{path}': table job 1: missing required key 'output'", ex.Message);
    }

    /// <summary>Test that keys reserved for jobs or the command line are rejected at the top level.</summary>
    /// <param name="key">The disallowed key.</param>
    [Theory]
    [InlineData("input")]
    [InlineData("output")]
    [InlineData("log")]
    [InlineData("results")]
    [InlineData("validate")]
    [InlineData("silent")]
    [InlineData("version")]
    [InlineData("help")]
    public void ConfigFile_Load_DisallowedTopLevelKey_ThrowsNamingKey(string key)
    {
        // Arrange: the disallowed key at the top level
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, $"{key}: x\n");

        // Act / Assert: the error names the file and the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.StartsWith($"Config file '{path}': key '{key}'", ex.Message);
    }

    /// <summary>Test that an unknown top-level key is rejected.</summary>
    [Fact]
    public void ConfigFile_Load_UnknownTopLevelKey_ThrowsNamingKey()
    {
        // Arrange: a key that is not part of the schema
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "style: table\n");

        // Act / Assert: the error names the file and the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Equal($"Config file '{path}': unknown key 'style'", ex.Message);
    }

    /// <summary>Test that an unknown job key is rejected.</summary>
    [Fact]
    public void ConfigFile_Load_UnknownJobKey_ThrowsNamingKeyAndJob()
    {
        // Arrange: a job with an unknown key
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "tables:\n  - input: a.yaml\n    output: a.md\n    log: x.log\n");

        // Act / Assert: the error names the file, the job and the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Equal($"Config file '{path}': table job 1: unknown key 'log'", ex.Message);
    }

    /// <summary>Test that an invalid depth is rejected.</summary>
    /// <param name="value">The invalid depth value.</param>
    [Theory]
    [InlineData("0")]
    [InlineData("7")]
    [InlineData("x")]
    public void ConfigFile_Load_InvalidDepth_Throws(string value)
    {
        // Arrange: an out-of-range or non-integer depth
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, $"depth: {value}\n");

        // Act / Assert: the error names the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Contains("key 'depth' must be an integer between 1 and 6", ex.Message);
    }

    /// <summary>Test that invalid choice values are rejected.</summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The invalid value.</param>
    [Theory]
    [InlineData("format", "list")]
    [InlineData("sort", "random")]
    [InlineData("kind", "approved")]
    public void ConfigFile_Load_InvalidFormatSortKind_Throws(string key, string value)
    {
        // Arrange: an unsupported choice
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, $"{key}: {value}\n");

        // Act / Assert: the error names the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Contains($"key '{key}' must be", ex.Message);
    }

    /// <summary>Test that column widths that are not a sequence are rejected.</summary>
    [Fact]
    public void ConfigFile_Load_ColumnWidthsNotSequence_Throws()
    {
        // Arrange: column widths written as a comma-separated string
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "column-widths: 25,75\n");

        // Act / Assert: the error names the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Contains("key 'column-widths' must be a sequence of 2 or 3 positive integers", ex.Message);
    }

    /// <summary>Test that invalid column-width values or counts are rejected.</summary>
    /// <param name="value">The invalid sequence.</param>
    [Theory]
    [InlineData("[0, 1]")]
    [InlineData("[-1, 2]")]
    [InlineData("[1.5, 2]")]
    [InlineData("[a, b]")]
    [InlineData("[1]")]
    [InlineData("[1, 2, 3, 4]")]
    public void ConfigFile_Load_ColumnWidthsInvalidValues_Throws(string value)
    {
        // Arrange: an invalid column-widths sequence
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, $"column-widths: {value}\n");

        // Act / Assert: the error names the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Contains("key 'column-widths'", ex.Message);
    }

    /// <summary>Test that the root key resolves relative to the config file's directory.</summary>
    [Fact]
    public void ConfigFile_Load_Root_ResolvedRelativeToConfigDirectory()
    {
        // Arrange: a root sub-directory next to the config file
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "root: docs\n");
        var docs = Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(path)!, "docs")).FullName;

        // Act: load the config
        var config = ConfigFile.Load(path);

        // Assert: the root is the full path of the sub-directory
        Assert.Equal(docs, config.RootDirectory);
    }

    /// <summary>Test that '..' segments in the root key are normalized.</summary>
    [Fact]
    public void ConfigFile_Load_RootWithParentSegments_Normalized()
    {
        // Arrange: a config two levels below the intended root
        using var tmpDir = new TemporaryDirectory();
        var path = tmpDir.GetFilePath(Path.Combine("a", "b", "dictionarymark.yaml"));
        File.WriteAllText(path, "root: ../..\n");

        // Act: load the config
        var config = ConfigFile.Load(path);

        // Assert: the root is the normalized ancestor directory
        Assert.Equal(Path.GetFullPath(tmpDir.DirectoryPath).TrimEnd(Path.DirectorySeparatorChar), config.RootDirectory);
    }

    /// <summary>Test that ${VAR} references in the root key are expanded.</summary>
    [Fact]
    public void ConfigFile_Load_RootEnvironmentVariable_Expanded()
    {
        // Arrange: a variable naming the root directory
        using var tmpDir = new TemporaryDirectory();
        using var rootDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "root: ${" + VariableName + "}\n");
        Environment.SetEnvironmentVariable(VariableName, rootDir.DirectoryPath);
        try
        {
            // Act: load the config
            var config = ConfigFile.Load(path);

            // Assert: the variable's directory is the root
            Assert.Equal(Path.GetFullPath(rootDir.DirectoryPath), config.RootDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>Test that an undefined variable in the root key is an error naming the file.</summary>
    [Fact]
    public void ConfigFile_Load_RootUndefinedVariable_ThrowsNamingFile()
    {
        // Arrange: a root referencing an undefined variable
        using var tmpDir = new TemporaryDirectory();
        Environment.SetEnvironmentVariable(VariableName, null);
        var path = WriteConfig(tmpDir, "root: ${" + VariableName + "}/docs\n");

        // Act / Assert: the error names the file and the variable
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.StartsWith($"Config file '{path}': Environment variable '{VariableName}' referenced by root key", ex.Message);
    }

    /// <summary>
    ///     Test that a root naming a folder that does not exist is accepted as text: the key is never probed, so its
    ///     full path is returned and the jobs that use it are judged when they run.
    /// </summary>
    [Fact]
    public void ConfigFile_Load_RootMissingDirectory_ResolvedAsTextWithoutError()
    {
        // Arrange: a root that does not exist
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "root: missing\n");
        var full = Path.Combine(Path.GetDirectoryName(path)!, "missing");

        // Act: load the config
        var config = ConfigFile.Load(path);

        // Assert: the root is the full path of the missing folder, which was not created
        Assert.Equal(full, config.RootDirectory);
        Assert.False(Directory.Exists(full));
    }

    /// <summary>
    ///     Test that a root naming a network share is accepted as text without contacting the host: loading returns
    ///     at once and leaves the share to the file-access resolver when the jobs run.
    /// </summary>
    [Fact]
    public void ConfigFile_Load_RootNetworkShare_ResolvedAsTextWithoutError()
    {
        // Arrange: a root naming a share on a host that cannot exist (the .invalid top-level domain)
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "root: //dictionarymark-test.invalid/share\n");

        // Act: load the config
        var config = ConfigFile.Load(path);

        // Assert: the root is the normalized text of the share; on Windows it is the UNC path. The root text keeps
        // the leading "//" that normalization collapses on Linux and macOS
        Assert.NotNull(config.RootDirectory);
        Assert.Equal("//dictionarymark-test.invalid/share", config.RootText);
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(@"\\dictionarymark-test.invalid\share", config.RootDirectory);
        }
    }

    /// <summary>Test that a root with a character invalid on every platform is an error naming the file.</summary>
    [Fact]
    public void ConfigFile_Load_RootInvalidCharacter_ThrowsNamingFile()
    {
        // Arrange: a root containing a null character (YAML "\0" escape in a double-quoted scalar)
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "root: \"bad\\0dir\"\n");

        // Act / Assert: the malformed path is reported with the file's context rather than escaping
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.StartsWith($"Config file '{path}': Invalid root key 'bad\0dir': ", ex.Message);
    }

    /// <summary>Test that a config file path with an invalid character is an error naming the path.</summary>
    [Fact]
    public void ConfigFile_Load_PathInvalidCharacter_ThrowsNamingFile()
    {
        // Arrange: a config file path containing a null character, which is invalid on every platform
        const string path = "bad\0config.yaml";

        // Act / Assert: the malformed path is reported with the config-file context
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.StartsWith($"Config file '{path}': invalid path: ", ex.Message);
    }

    /// <summary>Test that a root value that is not a non-empty string is rejected.</summary>
    /// <param name="value">The invalid root value.</param>
    [Theory]
    [InlineData("[a, b]")]
    [InlineData("{ a: b }")]
    [InlineData("\"\"")]
    public void ConfigFile_Load_RootInvalidValue_Throws(string value)
    {
        // Arrange: a sequence, mapping or empty root
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, $"root: {value}\n");

        // Act / Assert: the error names the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Equal($"Config file '{path}': key 'root' must be a non-empty string", ex.Message);
    }

    /// <summary>Test that the root key inside a job is rejected.</summary>
    [Fact]
    public void ConfigFile_Load_RootInTableJob_ThrowsNamingJob()
    {
        // Arrange: a job with a root key
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "tables:\n  - input: a.yaml\n    output: a.md\n    root: ..\n");

        // Act / Assert: the error names the file, the job and the key
        var ex = Assert.Throws<InvalidOperationException>(() => ConfigFile.Load(path));
        Assert.Equal($"Config file '{path}': table job 1: key 'root' is only allowed at the top level", ex.Message);
    }

    /// <summary>Test that the root directory is null when the key is absent.</summary>
    [Fact]
    public void ConfigFile_Load_NoRoot_RootDirectoryIsNull()
    {
        // Arrange: a config without a root key
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, MinimalTables);

        // Act: load the config
        var config = ConfigFile.Load(path);

        // Assert: no root directory
        Assert.Null(config.RootDirectory);
    }
}
