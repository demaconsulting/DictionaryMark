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
///     Unit tests for the Context class.
/// </summary>
[Collection("Sequential")]
public class ContextTests
{
    /// <summary>
    ///     Test creating a context with no arguments.
    /// </summary>
    [Fact]
    public void Context_Create_NoArguments_ReturnsDefaultContext()
    {
        // Act: execute the operation being tested
        using var context = Context.Create([]);

        // Assert: verify expected behavior
        Assert.False(context.Version);
        Assert.False(context.Help);
        Assert.False(context.Silent);
        Assert.False(context.Validate);
        Assert.Null(context.ResultsFile);
        Assert.Equal(1, context.HeadingDepth);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the version flag.
    /// </summary>
    [Fact]
    public void Context_Create_VersionFlag_SetsVersionTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--version"]);

        // Assert: verify expected behavior
        Assert.True(context.Version);
        Assert.False(context.Help);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the short version flag.
    /// </summary>
    [Fact]
    public void Context_Create_ShortVersionFlag_SetsVersionTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["-v"]);

        // Assert: verify expected behavior
        Assert.True(context.Version);
        Assert.False(context.Help);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the help flag.
    /// </summary>
    [Fact]
    public void Context_Create_HelpFlag_SetsHelpTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--help"]);

        // Assert: verify expected behavior
        Assert.False(context.Version);
        Assert.True(context.Help);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the short help flag -h.
    /// </summary>
    [Fact]
    public void Context_Create_ShortHelpFlag_H_SetsHelpTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["-h"]);

        // Assert: verify expected behavior
        Assert.False(context.Version);
        Assert.True(context.Help);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the short help flag -?.
    /// </summary>
    [Fact]
    public void Context_Create_ShortHelpFlag_Question_SetsHelpTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["-?"]);

        // Assert: verify expected behavior
        Assert.False(context.Version);
        Assert.True(context.Help);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the silent flag.
    /// </summary>
    [Fact]
    public void Context_Create_SilentFlag_SetsSilentTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--silent"]);

        // Assert: verify expected behavior
        Assert.True(context.Silent);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the validate flag.
    /// </summary>
    [Fact]
    public void Context_Create_ValidateFlag_SetsValidateTrue()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--validate"]);

        // Assert: verify expected behavior
        Assert.True(context.Validate);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the results flag.
    /// </summary>
    [Fact]
    public void Context_Create_ResultsFlag_SetsResultsFile()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--results", "test.trx"]);

        // Assert: verify expected behavior
        Assert.Equal("test.trx", context.ResultsFile);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with the log flag.
    /// </summary>
    [Fact]
    public void Context_Create_LogFlag_OpensLogFile()
    {
        // Arrange: setup test conditions
        var logFile = Path.GetTempFileName();
        try
        {
            // Act: execute the operation being tested
            using (var context = Context.Create(["--log", logFile]))
            {
                context.WriteLine("Test message");
                Assert.Equal(0, context.ExitCode);
            }

            // Assert: verify expected behavior
            // Verify log file was written
            Assert.True(File.Exists(logFile));
            var logContent = File.ReadAllText(logFile);
            Assert.Contains("Test message", logContent);
        }
        finally
        {
            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }
        }
    }

    /// <summary>
    ///     Test creating a context with an unknown argument throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_UnknownArgument_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--unknown"]));
        Assert.Contains("Unsupported argument", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with --log flag but no value throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_LogFlag_WithoutValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--log"]));
        Assert.Contains("--log", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with --results flag but no value throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_ResultsFlag_WithoutValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--results"]));
        Assert.Contains("--results", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with the --result alias flag (legacy alias for --results).
    /// </summary>
    [Fact]
    public void Context_Create_ResultAliasFlag_SetsResultsFile()
    {
        // Act: execute the operation using the legacy --result alias
        using var context = Context.Create(["--result", "test.trx"]);

        // Assert: verify --result sets ResultsFile identically to --results
        Assert.Equal("test.trx", context.ResultsFile);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with --result flag but no value throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_ResultAliasFlag_WithoutValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--result"]));
        Assert.Contains("--result", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with the depth flag.
    /// </summary>
    [Fact]
    public void Context_Create_DepthFlag_SetsHeadingDepth()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--depth", "3"]);

        // Assert: verify expected behavior
        Assert.Equal(3, context.HeadingDepth);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with no depth flag returns default heading depth of 1.
    /// </summary>
    [Fact]
    public void Context_Create_NoDepthFlag_ReturnsDefaultHeadingDepth()
    {
        // Act: execute the operation being tested
        using var context = Context.Create([]);

        // Assert: verify default depth is 1
        Assert.Equal(1, context.HeadingDepth);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test creating a context with --depth flag but no value throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_DepthFlag_WithoutValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--depth"]));
        Assert.Contains("--depth", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with --depth flag and non-integer value throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_DepthFlag_NonIntegerValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--depth", "abc"]));
        Assert.Contains("--depth", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with --depth flag and zero value throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_DepthFlag_ZeroValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--depth", "0"]));
        Assert.Contains("--depth", exception.Message);
    }

    /// <summary>
    ///     Test creating a context with --depth flag and value exceeding maximum throws exception.
    /// </summary>
    [Fact]
    public void Context_Create_DepthFlag_ExceedsMaxValue_ThrowsArgumentException()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--depth", "7"]));
        Assert.Contains("--depth", exception.Message);
    }

    /// <summary>
    ///     Test WriteLine writes to console output when not silent.
    /// </summary>
    [Fact]
    public void Context_WriteLine_NotSilent_WritesToConsole()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create([]);

            // Act: execute the operation being tested
            context.WriteLine("Test message");

            // Assert: verify expected behavior
            var output = outWriter.ToString();
            Assert.Contains("Test message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test WriteLine does not write to console when silent.
    /// </summary>
    [Fact]
    public void Context_WriteLine_Silent_DoesNotWriteToConsole()
    {
        // Arrange: setup test conditions
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(["--silent"]);

            // Act: execute the operation being tested
            context.WriteLine("Test message");

            // Assert: verify expected behavior
            var output = outWriter.ToString();
            Assert.DoesNotContain("Test message", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test WriteError does not write to console when silent.
    /// </summary>
    [Fact]
    public void Context_WriteError_Silent_DoesNotWriteToConsole()
    {
        // Arrange: setup test conditions
        var originalError = Console.Error;
        try
        {
            using var errWriter = new StringWriter();
            Console.SetError(errWriter);
            using var context = Context.Create(["--silent"]);

            // Act: execute the operation being tested
            context.WriteError("Test error message");

            // Assert - error output should be suppressed in silent mode
            var output = errWriter.ToString();
            Assert.DoesNotContain("Test error message", output);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test WriteError sets exit code to 1.
    /// </summary>
    [Fact]
    public void Context_WriteError_SetsErrorExitCode()
    {
        // Arrange: setup test conditions
        var originalError = Console.Error;
        try
        {
            using var errWriter = new StringWriter();
            Console.SetError(errWriter);
            using var context = Context.Create([]);

            // Act: execute the operation being tested
            context.WriteError("Test error message");

            // Assert: verify expected behavior
            Assert.Equal(1, context.ExitCode);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test WriteError writes message to console when not silent.
    /// </summary>
    [Fact]
    public void Context_WriteError_NotSilent_WritesToConsole()
    {
        // Arrange: setup test conditions
        var originalError = Console.Error;
        try
        {
            using var errWriter = new StringWriter();
            Console.SetError(errWriter);
            using var context = Context.Create([]);

            // Act: execute the operation being tested
            context.WriteError("Test error message");

            // Assert: verify expected behavior
            var output = errWriter.ToString();
            Assert.Contains("Test error message", output);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test WriteError writes message to log file when logging is enabled.
    /// </summary>
    [Fact]
    public void Context_WriteError_WritesToLogFile()
    {
        // Arrange: setup test conditions
        var logFile = Path.GetTempFileName();
        try
        {
            // Act - use silent to avoid console output; verify the error still goes to the log
            using (var context = Context.Create(["--silent", "--log", logFile]))
            {
                context.WriteError("Test error in log");
                Assert.Equal(1, context.ExitCode);
            }

            // Assert - log file should contain the error message
            Assert.True(File.Exists(logFile));
            var logContent = File.ReadAllText(logFile);
            Assert.Contains("Test error in log", logContent);
        }
        finally
        {
            if (File.Exists(logFile))
            {
                File.Delete(logFile);
            }
        }
    }

    /// <summary>
    ///     Test creating a context with --log flag pointing to an invalid path throws InvalidOperationException.
    /// </summary>
    [Fact]
    public void Context_Create_LogFlag_InvalidPath_ThrowsInvalidOperationException()
    {
        // Arrange: a path that cannot be opened as a file (directory or invalid characters)
        // Use a directory path so it cannot be opened as a file
        var invalidLogPath = Path.GetTempPath(); // temp directory itself, not a file

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => Context.Create(["--log", invalidLogPath]));
    }

    /// <summary>Test creating a context with the --input flag adds the pattern.</summary>
    [Fact]
    public void Context_Create_InputFlag_AddsPattern()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--input", "*.yaml"]);

        // Assert: verify expected behavior
        Assert.Single(context.InputPatterns);
        Assert.Equal("*.yaml", context.InputPatterns[0]);
    }

    /// <summary>Test creating a context with the short -i flag adds the pattern.</summary>
    [Fact]
    public void Context_Create_ShortInputFlag_AddsPattern()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["-i", "*.yaml"]);

        // Assert: verify expected behavior
        Assert.Single(context.InputPatterns);
        Assert.Equal("*.yaml", context.InputPatterns[0]);
    }

    /// <summary>Test creating a context with multiple --input flags adds all patterns.</summary>
    [Fact]
    public void Context_Create_MultipleInputFlags_AddsAllPatterns()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--input", "file1.yaml", "--input", "file2.yaml"]);

        // Assert: verify expected behavior
        Assert.Equal(2, context.InputPatterns.Count);
    }

    /// <summary>Test creating a context with the --output flag sets output file.</summary>
    [Fact]
    public void Context_Create_OutputFlag_SetsOutputFile()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--output", "output.md"]);

        // Assert: verify expected behavior
        Assert.Equal("output.md", context.OutputFile);
    }

    /// <summary>Test creating a context with --format table sets table format.</summary>
    [Fact]
    public void Context_Create_FormatFlag_Table_SetsTableFormat()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--format", "table"]);

        // Assert: verify expected behavior
        Assert.Equal(OutputFormat.Table, context.Format);
    }

    /// <summary>Test creating a context with --format bullets sets bullets format.</summary>
    [Fact]
    public void Context_Create_FormatFlag_Bullets_SetsBulletsFormat()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--format", "bullets"]);

        // Assert: verify expected behavior
        Assert.Equal(OutputFormat.Bullets, context.Format);
    }

    /// <summary>Test creating a context with --section sets section heading.</summary>
    [Fact]
    public void Context_Create_SectionFlag_SetsSectionHeading()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--section", "Glossary"]);

        // Assert: verify expected behavior
        Assert.Equal("Glossary", context.SectionHeading);
    }

    /// <summary>Test creating a context with --term-header sets term header.</summary>
    [Fact]
    public void Context_Create_TermHeaderFlag_SetsTermHeader()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--term-header", "Word"]);

        // Assert: verify expected behavior
        Assert.Equal("Word", context.TermHeader);
    }

    /// <summary>Test creating a context with --def-header sets definition header.</summary>
    [Fact]
    public void Context_Create_DefinitionHeaderFlag_SetsDefinitionHeader()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--def-header", "Meaning"]);

        // Assert: verify expected behavior
        Assert.Equal("Meaning", context.DefinitionHeader);
    }

    /// <summary>Test creating a context with --sort alpha sets alphabetical sort.</summary>
    [Fact]
    public void Context_Create_SortFlag_Alpha_SetsAlphabetical()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--sort", "alpha"]);

        // Assert: verify expected behavior
        Assert.Equal(SortOrder.Alphabetical, context.SortBy);
    }

    /// <summary>Test creating a context with --sort file sets file order sort.</summary>
    [Fact]
    public void Context_Create_SortFlag_File_SetsFileOrder()
    {
        // Act: execute the operation being tested
        using var context = Context.Create(["--sort", "file"]);

        // Assert: verify expected behavior
        Assert.Equal(SortOrder.FileOrder, context.SortBy);
    }

    /// <summary>Test creating a context with --input without value throws exception.</summary>
    [Fact]
    public void Context_Create_InputFlag_WithoutValue_ThrowsArgumentException()
    {
        // Act & Assert: missing value causes ArgumentException mentioning the flag name
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--input"]));
        Assert.Contains("--input", exception.Message);
    }

    /// <summary>Test creating a context with --output without value throws exception.</summary>
    [Fact]
    public void Context_Create_OutputFlag_WithoutValue_ThrowsArgumentException()
    {
        // Act & Assert: missing value causes ArgumentException mentioning the flag name
        var exception = Assert.Throws<ArgumentException>(() => Context.Create(["--output"]));
        Assert.Contains("--output", exception.Message);
    }

    /// <summary>Test creating a context with --format with invalid value throws exception.</summary>
    [Fact]
    public void Context_Create_FormatFlag_InvalidValue_ThrowsArgumentException()
    {
        // Act & Assert: unrecognized format value causes ArgumentException
        Assert.Throws<ArgumentException>(() => Context.Create(["--format", "invalid"]));
    }

    /// <summary>Test creating a context with --sort with invalid value throws exception.</summary>
    [Fact]
    public void Context_Create_SortFlag_InvalidValue_ThrowsArgumentException()
    {
        // Act & Assert: unrecognized sort value causes ArgumentException
        Assert.Throws<ArgumentException>(() => Context.Create(["--sort", "invalid"]));
    }

    /// <summary>Test creating a context with no --input flag leaves InputPatterns empty.</summary>
    [Fact]
    public void Context_Create_NoInputFlag_InputPatternsEmpty()
    {
        // Act: execute the operation being tested
        using var context = Context.Create([]);

        // Assert: verify expected behavior
        Assert.Empty(context.InputPatterns);
    }

    /// <summary>Test creating a context with no --output flag leaves OutputFile null.</summary>
    [Fact]
    public void Context_Create_NoOutputFlag_OutputFileNull()
    {
        // Act: execute the operation being tested
        using var context = Context.Create([]);

        // Assert: verify expected behavior
        Assert.Null(context.OutputFile);
    }

    /// <summary>Test that the default format is Bullets.</summary>
    [Fact]
    public void Context_Create_DefaultFormat_IsBullets()
    {
        // Act: execute the operation being tested
        using var context = Context.Create([]);

        // Assert: verify expected behavior
        Assert.Equal(OutputFormat.Bullets, context.Format);
    }

    /// <summary>Test that the default sort order is FileOrder.</summary>
    [Fact]
    public void Context_Create_DefaultSort_IsFileOrder()
    {
        // Act: execute the operation being tested
        using var context = Context.Create([]);

        // Assert: verify expected behavior
        Assert.Equal(SortOrder.FileOrder, context.SortBy);
    }

    /// <summary>Test that the new option defaults are as documented.</summary>
    [Fact]
    public void Context_Create_Defaults_KindTermsAndHeaders()
    {
        // Act
        using var context = Context.Create([]);

        // Assert
        Assert.Equal(InputKind.Terms, context.Kind);
        Assert.Equal("Use instead", context.UseHeader);
        Assert.Equal("Where it was used", context.WhereHeader);
        Assert.False(context.TermHeaderSpecified);
    }

    /// <summary>Test that --kind, header options are parsed.</summary>
    [Fact]
    public void Context_Create_DeprecatedOptions_AreParsed()
    {
        // Act
        using var context = Context.Create(
        [
            "--kind", "Deprecated", "--term-header", "Avoid", "--use-header", "Prefer", "--where-header", "Seen in"
        ]);

        // Assert
        Assert.Equal(InputKind.Deprecated, context.Kind);
        Assert.True(context.TermHeaderSpecified);
        Assert.Equal("Avoid", context.TermHeader);
        Assert.Equal("Prefer", context.UseHeader);
        Assert.Equal("Seen in", context.WhereHeader);
    }

    /// <summary>Test that an invalid --kind value is rejected.</summary>
    [Fact]
    public void Context_Create_InvalidKind_ThrowsArgumentException()
    {
        // Act / Assert
        var ex = Assert.Throws<ArgumentException>(() => Context.Create(["--kind", "other"]));
        Assert.Contains("--kind requires 'terms' or 'deprecated'", ex.Message);
    }

    /// <summary>Name of the environment variable used by the config and root tests.</summary>
    private const string VariableName = "DICTIONARYMARK_CONTEXT_TEST";

    /// <summary>A config file with one table job.</summary>
    private const string OneTableConfig = "format: table\ntables:\n  - input: a.yaml\n    output: a.md\n";

    /// <summary>Writes a config file into a sub-directory of the temporary directory.</summary>
    /// <param name="tmpDir">The temporary directory.</param>
    /// <param name="content">The YAML content.</param>
    /// <returns>The full path of the config file.</returns>
    private static string WriteConfig(TemporaryDirectory tmpDir, string content)
    {
        var path = tmpDir.GetFilePath(Path.Combine("config", "dictionarymark.yaml"));
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Creates a sub-directory of the temporary directory and returns its full path.</summary>
    /// <param name="tmpDir">The temporary directory.</param>
    /// <param name="name">The sub-directory name.</param>
    /// <returns>The full path of the new directory.</returns>
    private static string CreateDirectory(TemporaryDirectory tmpDir, string name) =>
        Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, name)).FullName;

    /// <summary>Test that --column-widths is parsed into the column widths.</summary>
    [Fact]
    public void Context_Create_ColumnWidthsFlag_SetsColumnWidths()
    {
        // Act: parse two and three widths, with surrounding spaces
        using var two = Context.Create(["--column-widths", "25,75"]);
        using var three = Context.Create(["--column-widths", " 20 , 50 , 30 "]);

        // Assert: the widths are parsed in order
        Assert.Equal([25, 75], two.ColumnWidths!);
        Assert.Equal([20, 50, 30], three.ColumnWidths!);
        Assert.Equal([25, 75], two.CommandLineFormatting.ColumnWidths!);
    }

    /// <summary>Test that invalid --column-widths values are rejected.</summary>
    /// <param name="value">The invalid value.</param>
    [Theory]
    [InlineData("0,1")]
    [InlineData("-1,2")]
    [InlineData("1.5,2")]
    [InlineData("a,b")]
    [InlineData("25,")]
    [InlineData("")]
    [InlineData("1")]
    [InlineData("1,2,3,4")]
    public void Context_Create_ColumnWidthsFlag_InvalidValue_ThrowsArgumentException(string value)
    {
        // Act / Assert: the value is rejected with the expected message
        var ex = Assert.Throws<ArgumentException>(() => Context.Create(["--column-widths", value]));
        Assert.Contains("--column-widths requires 2 or 3 comma-separated positive integers", ex.Message);
    }

    /// <summary>Test that --column-widths without a value is rejected.</summary>
    [Fact]
    public void Context_Create_ColumnWidthsFlag_WithoutValue_ThrowsArgumentException()
    {
        // Act / Assert
        var ex = Assert.Throws<ArgumentException>(() => Context.Create(["--column-widths"]));
        Assert.Contains("--column-widths requires", ex.Message);
    }

    /// <summary>Test that a widths count that does not match the table is rejected.</summary>
    [Fact]
    public void Context_Create_ColumnWidthsCountMismatch_ThrowsArgumentException()
    {
        // Act / Assert: a terms table with three widths is rejected
        var ex = Assert.Throws<ArgumentException>(
            () => Context.Create(["--input", "a.yaml", "--format", "table", "--column-widths", "1,2,3"]));
        Assert.Contains("terms tables require exactly 2 values", ex.Message);
    }

    /// <summary>Test that --config loads the table jobs.</summary>
    [Fact]
    public void Context_Create_ConfigFlag_LoadsTableJobs()
    {
        // Arrange: a config with two jobs
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, OneTableConfig + "  - input: [b.yaml, c.yaml]\n    output: b.md\n    format: bullets\n");

        // Act: create the context
        using var context = Context.Create(["--config", path]);

        // Assert: one job per table, with the merged formatting
        Assert.Equal(path, context.ConfigFilePath);
        Assert.True(context.UsesConfigTables);
        Assert.Equal(2, context.Jobs.Count);
        Assert.Equal(OutputFormat.Table, context.Jobs[0].Options.Format);
        Assert.Equal(OutputFormat.Bullets, context.Jobs[1].Options.Format);
        Assert.Equal(["b.yaml", "c.yaml"], context.Jobs[1].InputPatterns);
        Assert.Equal("table job 2 ('b.md')", context.Jobs[1].Label);
    }

    /// <summary>Test that --config given twice is rejected.</summary>
    [Fact]
    public void Context_Create_ConfigFlagTwice_ThrowsArgumentException()
    {
        // Act / Assert
        var ex = Assert.Throws<ArgumentException>(() => Context.Create(["--config", "a.yaml", "--config", "b.yaml"]));
        Assert.Contains("--config can only be specified once", ex.Message);
    }

    /// <summary>Test that --input with a config that defines tables is rejected.</summary>
    [Fact]
    public void Context_Create_ConfigWithTablesAndInput_ThrowsArgumentException()
    {
        // Arrange: a config with tables
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, OneTableConfig);

        // Act / Assert: the error names the option and the config file
        var ex = Assert.Throws<ArgumentException>(() => Context.Create(["--config", path, "--input", "x.yaml"]));
        Assert.Contains($"--input cannot be used with config file '{path}'", ex.Message);
    }

    /// <summary>Test that --output with a config that defines tables is rejected.</summary>
    [Fact]
    public void Context_Create_ConfigWithTablesAndOutput_ThrowsArgumentException()
    {
        // Arrange: a config with tables
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, OneTableConfig);

        // Act / Assert: the error names the option and the config file
        var ex = Assert.Throws<ArgumentException>(() => Context.Create(["--config", path, "--output", "x.md"]));
        Assert.Contains($"--output cannot be used with config file '{path}'", ex.Message);
    }

    /// <summary>Test that a config without tables supplies defaults to the command-line job.</summary>
    [Fact]
    public void Context_Create_ConfigWithoutTables_AppliesDefaultsToCommandLineJob()
    {
        // Arrange: a config with formatting defaults only
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, "format: table\nsection: Glossary\ndepth: 3\n");

        // Act: create a context with a command-line input and an overriding depth
        using var context = Context.Create(["--config", path, "--input", "a.yaml", "--depth", "2"]);

        // Assert: one command-line job; config defaults apply where the command line is silent
        var job = Assert.Single(context.Jobs);
        Assert.False(context.UsesConfigTables);
        Assert.Null(job.Label);
        Assert.Equal(OutputFormat.Table, job.Options.Format);
        Assert.Equal("Glossary", job.Options.SectionHeading);
        Assert.Equal(2, job.Options.HeadingDepth);
        Assert.Equal(OutputFormat.Table, context.Format);
        Assert.Equal(2, context.HeadingDepth);
    }

    /// <summary>Test that explicitly given formatting options form the command-line layer.</summary>
    [Fact]
    public void Context_Create_FormattingFlags_RecordedAsCommandLineLayer()
    {
        // Act: give several formatting options
        using var context = Context.Create(
            ["--format", "table", "--sort", "alpha", "--def-header", "Meaning", "--depth", "4", "--section", "S"]);

        // Assert: exactly the given options are recorded
        var layer = context.CommandLineFormatting;
        Assert.Equal(OutputFormat.Table, layer.Format);
        Assert.Equal(SortOrder.Alphabetical, layer.SortBy);
        Assert.Equal("Meaning", layer.DefinitionHeader);
        Assert.Equal(4, layer.HeadingDepth);
        Assert.Equal("S", layer.SectionHeading);
        Assert.Null(layer.Kind);
        Assert.Null(layer.TermHeader);
        Assert.Null(layer.ColumnWidths);
    }

    /// <summary>Test that the command-line layer is empty when no formatting option is given.</summary>
    [Fact]
    public void Context_Create_NoFormattingFlags_CommandLineLayerEmpty()
    {
        // Act
        using var context = Context.Create(["--input", "a.yaml"]);

        // Assert: nothing was recorded, and no config or root applies
        Assert.Equal(FormattingSettings.Empty, context.CommandLineFormatting);
        Assert.Null(context.ConfigFilePath);
        Assert.Null(context.RootDirectory);
        Assert.Null(context.ColumnWidths);
    }

    /// <summary>Test that ${VAR} references in the --config path are expanded.</summary>
    [Fact]
    public void Context_Create_ConfigPathEnvironmentVariable_Expanded()
    {
        // Arrange: a variable naming the config directory
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, OneTableConfig);
        Environment.SetEnvironmentVariable(VariableName, Path.GetDirectoryName(path));
        try
        {
            // Act: reference the config through the variable
            using var context = Context.Create(["--config", "${" + VariableName + "}/dictionarymark.yaml"]);

            // Assert: the expanded config was loaded
            Assert.Equal(path, context.ConfigFilePath);
            Assert.Single(context.Jobs);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>Test that --root sets the root directory.</summary>
    [Fact]
    public void Context_Create_RootFlag_SetsRootDirectory()
    {
        // Arrange: an existing directory
        using var tmpDir = new TemporaryDirectory();
        var root = CreateDirectory(tmpDir, "root");

        // Act
        using var context = Context.Create(["--root", root]);

        // Assert
        Assert.Equal(root, context.RootDirectory);
    }

    /// <summary>Test that a relative --root with '..' resolves against the working directory.</summary>
    [Fact]
    public void Context_Create_RootFlagRelativeWithParentSegments_ResolvedAgainstWorkingDirectory()
    {
        // Arrange: a uniquely named directory under the working directory
        var name = "root-" + Guid.NewGuid().ToString("N");
        var full = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, name)).FullName;
        try
        {
            // Act: reference it through a path that climbs out of a sub-directory
            using var context = Context.Create(["--root", $"sub/../{name}"]);

            // Assert: the normalized full path is the root
            Assert.Equal(full, context.RootDirectory);
        }
        finally
        {
            Directory.Delete(full, recursive: true);
        }
    }

    /// <summary>Test that ${VAR} references in --root are expanded.</summary>
    [Fact]
    public void Context_Create_RootFlagEnvironmentVariable_Expanded()
    {
        // Arrange: a variable naming an existing directory
        using var tmpDir = new TemporaryDirectory();
        Environment.SetEnvironmentVariable(VariableName, tmpDir.DirectoryPath);
        try
        {
            // Act
            using var context = Context.Create(["--root", "${" + VariableName + "}"]);

            // Assert
            Assert.Equal(Path.GetFullPath(tmpDir.DirectoryPath), context.RootDirectory);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>Test that a missing --root directory is rejected.</summary>
    [Fact]
    public void Context_Create_RootFlagMissingDirectory_ThrowsInvalidOperationException()
    {
        // Arrange: a directory that does not exist
        using var tmpDir = new TemporaryDirectory();
        var missing = Path.Combine(tmpDir.DirectoryPath, "missing");

        // Act / Assert: the error names the directory and the option
        var ex = Assert.Throws<InvalidOperationException>(() => Context.Create(["--root", missing]));
        Assert.Equal($"Directory '{missing}' specified by --root option '{missing}' does not exist.", ex.Message);
    }

    /// <summary>Test that a --root value with a character invalid on every platform is reported cleanly.</summary>
    [Fact]
    public void Context_Create_RootFlagInvalidCharacter_ThrowsInvalidOperationException()
    {
        // Arrange: a root containing a null character
        const string root = "bad\0dir";

        // Act / Assert: the error names the option and the value instead of escaping as a raw path exception
        var ex = Assert.Throws<InvalidOperationException>(() => Context.Create(["--root", root]));
        Assert.StartsWith($"Invalid --root option '{root}': ", ex.Message);
    }

    /// <summary>Test that a --config value with a character invalid on every platform is reported cleanly.</summary>
    [Fact]
    public void Context_Create_ConfigFlagInvalidCharacter_ThrowsInvalidOperationException()
    {
        // Arrange: a config file path containing a null character
        const string config = "bad\0config.yaml";

        // Act / Assert: the error names the config file path and the value
        var ex = Assert.Throws<InvalidOperationException>(() => Context.Create(["--config", config]));
        Assert.StartsWith($"Invalid config file path '{config}': ", ex.Message);
    }

    /// <summary>Test that --root given twice is rejected.</summary>
    [Fact]
    public void Context_Create_RootFlagTwice_ThrowsArgumentException()
    {
        // Act / Assert
        var ex = Assert.Throws<ArgumentException>(() => Context.Create(["--root", ".", "--root", "."]));
        Assert.Contains("--root can only be specified once", ex.Message);
    }

    /// <summary>Test that --root without a value is rejected.</summary>
    [Fact]
    public void Context_Create_RootFlagWithoutValue_ThrowsArgumentException()
    {
        // Act / Assert
        var ex = Assert.Throws<ArgumentException>(() => Context.Create(["--root"]));
        Assert.Contains("--root requires a directory path", ex.Message);
    }

    /// <summary>Test that an empty --root value is rejected.</summary>
    [Fact]
    public void Context_Create_RootFlagEmpty_ThrowsArgumentException()
    {
        // Act / Assert
        var ex = Assert.Throws<ArgumentException>(() => Context.Create(["--root", " "]));
        Assert.Contains("--root requires a non-empty directory path", ex.Message);
    }

    /// <summary>Test that --root is the base directory of the command-line job.</summary>
    [Fact]
    public void Context_Create_RootFlag_AppliedToCommandLineJob()
    {
        // Arrange: an existing root directory
        using var tmpDir = new TemporaryDirectory();
        var root = CreateDirectory(tmpDir, "root");

        // Act
        using var context = Context.Create(["--root", root, "--input", "a.yaml"]);

        // Assert
        Assert.Equal(root, Assert.Single(context.Jobs).BaseDirectory);
    }

    /// <summary>Test that --root is the base directory of config table jobs.</summary>
    [Fact]
    public void Context_Create_RootFlag_AppliedToConfigTables()
    {
        // Arrange: a config with tables and a separate root
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, OneTableConfig);
        var root = CreateDirectory(tmpDir, "root");

        // Act
        using var context = Context.Create(["--config", path, "--root", root]);

        // Assert
        Assert.Equal(root, Assert.Single(context.Jobs).BaseDirectory);
    }

    /// <summary>Test that --root overrides the config file's root key.</summary>
    [Fact]
    public void Context_Create_RootFlag_OverridesConfigRoot()
    {
        // Arrange: a config whose root key names one directory, and --root naming another
        using var tmpDir = new TemporaryDirectory();
        CreateDirectory(tmpDir, "config-root");
        var path = WriteConfig(tmpDir, "root: ../config-root\n" + OneTableConfig);
        var root = CreateDirectory(tmpDir, "cli-root");

        // Act
        using var context = Context.Create(["--config", path, "--root", root]);

        // Assert: --root wins for the effective root and the job
        Assert.Equal(root, context.RootDirectory);
        Assert.Equal(root, Assert.Single(context.Jobs).BaseDirectory);
    }

    /// <summary>Test that the config root key is the base directory of config table jobs.</summary>
    [Fact]
    public void Context_Create_ConfigRoot_AppliedToConfigTables()
    {
        // Arrange: a config whose root key names a sibling directory
        using var tmpDir = new TemporaryDirectory();
        var root = CreateDirectory(tmpDir, "config-root");
        var path = WriteConfig(tmpDir, "root: ../config-root\n" + OneTableConfig);

        // Act
        using var context = Context.Create(["--config", path]);

        // Assert
        Assert.Equal(root, context.RootDirectory);
        Assert.Equal(root, Assert.Single(context.Jobs).BaseDirectory);
    }

    /// <summary>Test that the config root key is the base directory of the command-line job.</summary>
    [Fact]
    public void Context_Create_ConfigRoot_AppliedToCommandLineJob()
    {
        // Arrange: a config with a root key and no tables
        using var tmpDir = new TemporaryDirectory();
        var root = CreateDirectory(tmpDir, "config-root");
        var path = WriteConfig(tmpDir, "root: ../config-root\n");

        // Act
        using var context = Context.Create(["--config", path, "--input", "a.yaml"]);

        // Assert
        Assert.Equal(root, Assert.Single(context.Jobs).BaseDirectory);
    }

    /// <summary>Test that, without a root, config table jobs use the config file's directory.</summary>
    [Fact]
    public void Context_Create_NoRoot_ConfigTablesUseConfigDirectory()
    {
        // Arrange: a config with tables and no root
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, OneTableConfig);

        // Act
        using var context = Context.Create(["--config", path]);

        // Assert
        Assert.Null(context.RootDirectory);
        Assert.Equal(Path.GetDirectoryName(path), Assert.Single(context.Jobs).BaseDirectory);
    }

    /// <summary>Test that, without a root, the command-line job uses the working directory.</summary>
    [Fact]
    public void Context_Create_NoRoot_CommandLineJobUsesWorkingDirectory()
    {
        // Act
        using var context = Context.Create(["--input", "a.yaml"]);

        // Assert
        Assert.Equal(Environment.CurrentDirectory, Assert.Single(context.Jobs).BaseDirectory);
    }

    /// <summary>Test that --config is resolved against the working directory, not the root.</summary>
    [Fact]
    public void Context_Create_RootFlag_ConfigPathNotResolvedAgainstRoot()
    {
        // Arrange: a config under a unique working-directory folder, and a root that does not contain it
        using var tmpDir = new TemporaryDirectory();
        var root = CreateDirectory(tmpDir, "root");
        var name = "config-" + Guid.NewGuid().ToString("N");
        var configDirectory = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, name)).FullName;
        try
        {
            var configPath = Path.Combine(configDirectory, "dictionarymark.yaml");
            File.WriteAllText(configPath, OneTableConfig);

            // Act: give the config as a working-directory-relative path together with --root
            using var context = Context.Create(["--root", root, "--config", $"{name}/dictionarymark.yaml"]);

            // Assert: the config was found relative to the working directory
            Assert.Equal(configPath, context.ConfigFilePath);
            Assert.Equal(root, Assert.Single(context.Jobs).BaseDirectory);
        }
        finally
        {
            Directory.Delete(configDirectory, recursive: true);
        }
    }

    /// <summary>Test that the file-access mode defaults to any, with no allowed folders.</summary>
    [Fact]
    public void Context_Create_NoFileAccess_DefaultsToAny()
    {
        // Act
        using var context = Context.Create([]);

        // Assert
        Assert.Equal(FileAccessMode.None, context.FileAccess.Mode);
        Assert.Equal("any", context.FileAccess.ModeName);
        Assert.Empty(context.FileAccess.AllowedFolders);
    }

    /// <summary>Test that every --file-access mode is accepted in any letter case.</summary>
    /// <param name="text">The option value.</param>
    /// <param name="expected">The expected mode name.</param>
    [Theory]
    [InlineData("ANY", "any")]
    [InlineData("Roots", "roots")]
    [InlineData("No-Symlinks", "no-symlinks")]
    [InlineData("ROOTS,no-symlinks", "roots,no-symlinks")]
    public void Context_Create_FileAccessEachMode_Parsed(string text, string expected)
    {
        // Act
        using var context = Context.Create(["--file-access", text]);

        // Assert
        Assert.Equal(expected, context.FileAccess.ModeName);
    }

    /// <summary>Test that an invalid --file-access value is a usage error listing the modes.</summary>
    [Fact]
    public void Context_Create_FileAccessInvalid_ThrowsListingModes()
    {
        // Act / Assert
        var ex = Assert.Throws<ArgumentException>(() => Context.Create(["--file-access", "everything"]));
        Assert.Equal(
            "'everything' is not a value of --file-access: use any (the default), roots, no-symlinks, or roots,no-symlinks",
            ex.Message);
    }

    /// <summary>Test that --file-access given twice is a usage error.</summary>
    [Fact]
    public void Context_Create_FileAccessTwice_Throws()
    {
        // Act / Assert
        var ex = Assert.Throws<ArgumentException>(() => Context.Create(["--file-access", "roots", "--file-access", "roots"]));
        Assert.Contains("--file-access can only be specified once", ex.Message);
    }

    /// <summary>Test that --allow-root implies roots and replaces the default folders.</summary>
    [Fact]
    public void Context_Create_AllowRoot_ImpliesRootsAndReplacesDefaults()
    {
        // Arrange: two folders, neither of them the working directory
        using var tmpDir = new TemporaryDirectory();
        var first = CreateDirectory(tmpDir, "first");
        var second = CreateDirectory(tmpDir, "second");

        // Act
        using var context = Context.Create(["--allow-root", first, "--allow-root", second]);

        // Assert: exactly the named folders, in roots mode
        Assert.Equal(FileAccessMode.Roots, context.FileAccess.Mode);
        Assert.Equal([first, second], context.FileAccess.AllowedFolders);
    }

    /// <summary>Test that a relative --allow-root resolves against the working directory, never against --root.</summary>
    [Fact]
    public void Context_Create_AllowRootRelative_ResolvedAgainstWorkingDirectoryNotRoot()
    {
        // Arrange: a uniquely named folder under the working directory, and a root that also contains one
        using var tmpDir = new TemporaryDirectory();
        var name = "allow-" + Guid.NewGuid().ToString("N");
        var root = CreateDirectory(tmpDir, "root");
        Directory.CreateDirectory(Path.Combine(root, name));
        var full = Directory.CreateDirectory(Path.Combine(Environment.CurrentDirectory, name)).FullName;
        try
        {
            // Act: name the folder through a path that climbs out of a sub-folder
            using var context = Context.Create(["--root", root, "--allow-root", $"sub/../{name}"]);

            // Assert: the working-directory folder is allowed
            Assert.Equal([full], context.FileAccess.AllowedFolders);
        }
        finally
        {
            Directory.Delete(full, recursive: true);
        }
    }

    /// <summary>Test that ${VAR} references in --allow-root are expanded.</summary>
    [Fact]
    public void Context_Create_AllowRootEnvironmentVariable_Expanded()
    {
        // Arrange: a variable naming an existing folder
        using var tmpDir = new TemporaryDirectory();
        var docs = CreateDirectory(tmpDir, "docs");
        Environment.SetEnvironmentVariable(VariableName, tmpDir.DirectoryPath);
        try
        {
            // Act
            using var context = Context.Create(["--allow-root", "${" + VariableName + "}/docs"]);

            // Assert
            Assert.Equal([docs], context.FileAccess.AllowedFolders);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>Test that an --allow-root folder that does not exist is a usage error.</summary>
    [Fact]
    public void Context_Create_AllowRootMissing_Throws()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var missing = Path.Combine(tmpDir.DirectoryPath, "missing");

        // Act / Assert: the error names the folder and the option
        var ex = Assert.Throws<InvalidOperationException>(() => Context.Create(["--allow-root", missing]));
        Assert.Equal($"Directory '{missing}' specified by --allow-root option '{missing}' does not exist.", ex.Message);
    }

    /// <summary>Test that on Windows an --allow-root folder in a device namespace is a usage error.</summary>
    [Fact]
    public void Context_Create_AllowRootInDeviceNamespace_Throws()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Device namespaces exist on Windows only.");

        // Arrange: an existing folder spelled in the Win32 device namespace
        using var tmpDir = new TemporaryDirectory();
        var device = @"\\.\" + tmpDir.DirectoryPath;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => Context.Create(["--allow-root", device]));

        // Assert: the error names the folder and the option
        Assert.Equal(
            $"Directory '{device}' specified by --allow-root option '{device}' must not be in a device namespace.",
            ex.Message);
    }

    /// <summary>
    ///     Test that on Windows an extended-length --allow-root is rejected by the wildcard rule, as for --root.
    /// </summary>
    [Fact]
    public void Context_Create_AllowRootExtendedLength_ThrowsWildcard()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Extended-length paths exist on Windows only.");

        // Arrange: an existing folder spelled as an ordinary extended-length drive path
        using var tmpDir = new TemporaryDirectory();
        var extended = @"\\?\" + tmpDir.DirectoryPath;

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => Context.Create(["--allow-root", extended]));

        // Assert: the device-namespace check passes it, and the wildcard rule refuses it
        Assert.Equal($"--allow-root option '{extended}' must not contain wildcard characters.", ex.Message);
    }

    /// <summary>Test that --allow-root with --file-access any is a usage error naming the conflict.</summary>
    [Fact]
    public void Context_Create_AllowRootWithAny_ThrowsNamingConflict()
    {
        // Act / Assert
        var ex = Assert.Throws<ArgumentException>(
            () => Context.Create(["--file-access", "any", "--allow-root", Environment.CurrentDirectory]));
        Assert.StartsWith("The options --file-access any and --allow-root cannot be used together", ex.Message);
    }

    /// <summary>Test that --allow-root with --file-access no-symlinks is a usage error naming the conflict.</summary>
    [Fact]
    public void Context_Create_AllowRootWithNoSymlinks_ThrowsNamingConflict()
    {
        // Act / Assert
        var ex = Assert.Throws<ArgumentException>(
            () => Context.Create(["--allow-root", Environment.CurrentDirectory, "--file-access", "No-Symlinks"]));
        Assert.StartsWith("The options --file-access no-symlinks and --allow-root cannot be used together", ex.Message);
        Assert.Contains("--file-access roots,no-symlinks", ex.Message);
    }

    /// <summary>Test that roots without --allow-root allows the working directory and the config file's folder.</summary>
    [Fact]
    public void Context_Create_RootsWithConfig_AllowsWorkingAndConfigFolders()
    {
        // Arrange: a config file outside the working directory
        using var tmpDir = new TemporaryDirectory();
        var path = WriteConfig(tmpDir, OneTableConfig);

        // Act
        using var context = Context.Create(["--file-access", "roots", "--config", path]);

        // Assert: the default folders, with the config folder as the trusted base
        Assert.Equal([Environment.CurrentDirectory, Path.GetDirectoryName(path)!], context.FileAccess.AllowedFolders);
        Assert.Equal(Path.GetDirectoryName(path), context.FileAccess.TrustedBase);
    }

    /// <summary>Test that roots without --allow-root and without --config allows only the working directory.</summary>
    [Fact]
    public void Context_Create_RootsWithoutConfig_AllowsWorkingDirectory()
    {
        // Act
        using var context = Context.Create(["--file-access", "roots"]);

        // Assert
        Assert.Equal([Environment.CurrentDirectory], context.FileAccess.AllowedFolders);
        Assert.Null(context.FileAccess.TrustedBase);
    }
}
