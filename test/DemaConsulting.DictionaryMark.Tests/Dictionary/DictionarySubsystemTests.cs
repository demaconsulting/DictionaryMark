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

namespace DemaConsulting.DictionaryMark.Tests.Dictionary;

/// <summary>
///     Subsystem tests for the Dictionary subsystem covering YAML loading, conflict detection,
///     Markdown formatting, and end-to-end generation pipeline integration.
/// </summary>
[Collection("Sequential")]
public class DictionarySubsystemTests
{
    /// <summary>
    ///     Test that the dictionary subsystem generates a bullet list from a valid YAML file.
    /// </summary>
    [Fact]
    public void DictionarySubsystem_BulletGeneration_ValidYaml_GeneratesBulletMarkdown()
    {
        // Arrange: YAML file with two entries; bullet-list output format
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(tmpFile, "API: Application Programming Interface\nCI: Continuous Integration\n");
        var options = new MarkdownOptions
        {
            Format = OutputFormat.Bullets
        };

        // Act: load entries and format as Markdown
        var entries = YamlDictionaryLoader.Load(tmpFile);
        var result = MarkdownFormatter.Format(entries, options);

        // Assert: both entries are rendered as bullet list items
        Assert.Contains("- **API**: Application Programming Interface", result);
        Assert.Contains("- **CI**: Continuous Integration", result);
    }

    /// <summary>
    ///     Test that the dictionary subsystem generates a Markdown table from a valid YAML file.
    /// </summary>
    [Fact]
    public void DictionarySubsystem_TableGeneration_ValidYaml_GeneratesTableMarkdown()
    {
        // Arrange: YAML file with one entry; table output format
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(tmpFile, "API: Application Programming Interface\n");
        var options = new MarkdownOptions
        {
            Format = OutputFormat.Table
        };

        // Act: load entries and format as Markdown table
        var entries = YamlDictionaryLoader.Load(tmpFile);
        var result = MarkdownFormatter.Format(entries, options);

        // Assert: table header and entry row are present
        Assert.Contains("| Term |", result);
        Assert.Contains("| API |", result);
        Assert.Contains("Application Programming Interface", result);
    }

    /// <summary>
    ///     Test that the dictionary subsystem detects conflicting definitions across files.
    /// </summary>
    [Fact]
    public void DictionarySubsystem_ConflictDetection_ConflictingEntries_ReturnsConflicts()
    {
        // Arrange: two YAML files each defining the same term with different definitions
        using var tmpDir = new TemporaryDirectory();
        var tmpFile1 = tmpDir.GetFilePath("first.yaml");
        var tmpFile2 = tmpDir.GetFilePath("second.yaml");
        File.WriteAllText(tmpFile1, "API: Application Programming Interface\n");
        File.WriteAllText(tmpFile2, "API: Advanced Peripheral Interface\n");

        // Act: load both files and run conflict detection
        var entries1 = YamlDictionaryLoader.Load(tmpFile1);
        var entries2 = YamlDictionaryLoader.Load(tmpFile2);
        var allEntries = entries1.Concat(entries2).ToList();
        var conflicts = ConflictDetector.Detect(allEntries);

        // Assert: conflict list is non-empty and references the conflicting term
        Assert.NotEmpty(conflicts);
        Assert.Contains(conflicts, c => c.Contains("API", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Test that the dictionary subsystem orchestrates the end-to-end generation pipeline,
    ///     loading YAML entries and producing Markdown output.
    /// </summary>
    [Fact]
    public void DictionarySubsystem_Generation_ValidYaml_GeneratesMarkdown()
    {
        // Arrange: YAML file with two entries; redirect stdout to capture generated output
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(tmpFile, "API: Application Programming Interface\nCI: Continuous Integration\n");
        var originalOut = Console.Out;
        try
        {
            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(["--input", tmpFile]);

            // Act: invoke the full generation pipeline end-to-end
            DictionaryGenerator.Generate(context);

            // Assert: generated output contains both entries; exit code indicates success
            var output = outWriter.ToString();
            Assert.Contains("API", output);
            Assert.Contains("CI", output);
            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            // Restore original stdout regardless of test outcome
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that the dictionary subsystem sizes table columns in proportion to the column widths.
    /// </summary>
    [Fact]
    public void DictionarySubsystem_TableGeneration_ColumnWidths_GeneratesProportionalSeparator()
    {
        // Arrange: YAML file with one entry; table output with column widths 1:3
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(tmpFile, "API: Application Programming Interface\n");
        var options = new MarkdownOptions { Format = OutputFormat.Table, ColumnWidths = [1, 3] };

        // Act: load entries and format as a Markdown table
        var entries = YamlDictionaryLoader.Load(tmpFile);
        var result = MarkdownFormatter.Format(entries, options);

        // Assert: the separator has 25 and 75 dashes and the entry row follows
        Assert.Contains($"| :{new string('-', 25)} | :{new string('-', 75)} |", result);
        Assert.Contains("| API | Application Programming Interface |", result);
    }

    /// <summary>
    ///     Test that the dictionary subsystem generates one output per configured table job.
    /// </summary>
    [Fact]
    public void DictionarySubsystem_Generation_MultipleJobs_GeneratesEachOutput()
    {
        // Arrange: a dictionary and a config with two jobs writing into a missing directory
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath("terms.yaml"), "API: Application Programming Interface\n");
        var config = tmpDir.GetFilePath("dictionarymark.yaml");
        File.WriteAllText(config,
            "tables:\n  - input: terms.yaml\n    output: out/table.md\n    format: table\n" +
            "  - input: terms.yaml\n    output: out/bullets.md\n");
        using var context = Context.Create(["--silent", "--config", config]);

        // Act: invoke the full generation pipeline for every job
        DictionaryGenerator.Generate(context);

        // Assert: both outputs exist with their own format; exit code indicates success
        Assert.Equal(0, context.ExitCode);
        Assert.Contains("| API |", File.ReadAllText(Path.Combine(tmpDir.DirectoryPath, "out", "table.md")));
        Assert.Contains("- **API**:", File.ReadAllText(Path.Combine(tmpDir.DirectoryPath, "out", "bullets.md")));
    }

    /// <summary>
    ///     Test that the dictionary subsystem judges config-file table inputs and outputs by the file-access policy:
    ///     a table whose input lies outside the allowed folder fails with file-access-denied, while the other tables
    ///     still generate their output.
    /// </summary>
    [Fact]
    public void DictionarySubsystem_FileAccess_ConfigJobOutsideRoots_DeniedAndOtherJobsRun()
    {
        // Arrange: the config folder is the only allowed root; one table reads a dictionary outside it
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "terms.yaml")), "API: Application Programming Interface\n");
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("other", "secret.yaml")), "Secret: Hidden\n");
        var config = tmpDir.GetFilePath(Path.Combine("config", "dictionarymark.yaml"));
        File.WriteAllText(config,
            "tables:\n  - input: ../other/secret.yaml\n    output: secret.md\n  - input: terms.yaml\n    output: terms.md\n");
        var root = Path.GetDirectoryName(config)!;
        using var context = Context.Create(["--silent", "--config", config, "--allow-root", root]);

        // Act
        DictionaryGenerator.Generate(context);

        // Assert: the denied table wrote nothing; the other table ran; the exit code reports the failure
        Assert.Equal(1, context.ExitCode);
        Assert.False(File.Exists(Path.Combine(root, "secret.md")));
        Assert.Contains("- **API**:", File.ReadAllText(Path.Combine(root, "terms.md")));
    }

    /// <summary>
    ///     Test that a configuration table whose input pattern names a network share is refused before anything is
    ///     listed, and the other table still runs.
    /// </summary>
    [Fact]
    public void DictionarySubsystem_FileAccess_NetworkPattern_RefusedAndOtherJobsRun()
    {
        // Arrange: one table names a share; the other reads a local dictionary
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "terms.yaml")), "API: Application Programming Interface\n");
        var config = tmpDir.GetFilePath(Path.Combine("config", "dictionarymark.yaml"));
        File.WriteAllText(config,
            "tables:\n  - input: \"//dictionarymark-test.invalid/share/*.yaml\"\n    output: share.md\n" +
            "  - input: terms.yaml\n    output: terms.md\n");
        var root = Path.GetDirectoryName(config)!;
        using var context = Context.Create(["--silent", "--config", config]);

        // Act
        DictionaryGenerator.Generate(context);

        // Assert: the refused table wrote nothing; the other table ran; the exit code reports the failure
        Assert.Equal(1, context.ExitCode);
        Assert.False(File.Exists(Path.Combine(root, "share.md")));
        Assert.Contains("- **API**:", File.ReadAllText(Path.Combine(root, "terms.md")));
    }
}
