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
using DemaConsulting.DictionaryMark.Tests.Helpers;

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
}
