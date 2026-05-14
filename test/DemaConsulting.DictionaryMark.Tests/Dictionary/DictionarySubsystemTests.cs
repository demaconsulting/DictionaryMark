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
///     Subsystem tests for the Dictionary subsystem covering YAML loading, conflict detection,
///     and Markdown formatting integration.
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
        var tmpFile = Path.GetTempFileName() + ".yaml";
        try
        {
            // Arrange
            File.WriteAllText(tmpFile, "API: Application Programming Interface\nCI: Continuous Integration\n");
            var options = new MarkdownOptions
            {
                Format = OutputFormat.Bullets
            };

            // Act
            var entries = YamlDictionaryLoader.Load(tmpFile);
            var result = MarkdownFormatter.Format(entries, options);

            // Assert
            Assert.Contains("- **API**: Application Programming Interface", result);
            Assert.Contains("- **CI**: Continuous Integration", result);
        }
        finally
        {
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }
        }
    }

    /// <summary>
    ///     Test that the dictionary subsystem generates a Markdown table from a valid YAML file.
    /// </summary>
    [Fact]
    public void DictionarySubsystem_TableGeneration_ValidYaml_GeneratesTableMarkdown()
    {
        var tmpFile = Path.GetTempFileName() + ".yaml";
        try
        {
            // Arrange
            File.WriteAllText(tmpFile, "API: Application Programming Interface\n");
            var options = new MarkdownOptions
            {
                Format = OutputFormat.Table
            };

            // Act
            var entries = YamlDictionaryLoader.Load(tmpFile);
            var result = MarkdownFormatter.Format(entries, options);

            // Assert
            Assert.Contains("| Term |", result);
            Assert.Contains("| API |", result);
            Assert.Contains("Application Programming Interface", result);
        }
        finally
        {
            if (File.Exists(tmpFile))
            {
                File.Delete(tmpFile);
            }
        }
    }

    /// <summary>
    ///     Test that the dictionary subsystem detects conflicting definitions across files.
    /// </summary>
    [Fact]
    public void DictionarySubsystem_ConflictDetection_ConflictingEntries_ReturnsConflicts()
    {
        var tmpFile1 = Path.GetTempFileName() + ".yaml";
        var tmpFile2 = Path.GetTempFileName() + ".yaml";
        try
        {
            // Arrange
            File.WriteAllText(tmpFile1, "API: Application Programming Interface\n");
            File.WriteAllText(tmpFile2, "API: Advanced Peripheral Interface\n");

            // Act
            var entries1 = YamlDictionaryLoader.Load(tmpFile1);
            var entries2 = YamlDictionaryLoader.Load(tmpFile2);
            var allEntries = entries1.Concat(entries2).ToList();
            var conflicts = ConflictDetector.Detect(allEntries);

            // Assert
            Assert.NotEmpty(conflicts);
            Assert.Contains(conflicts, c => c.Contains("API", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            if (File.Exists(tmpFile1))
            {
                File.Delete(tmpFile1);
            }

            if (File.Exists(tmpFile2))
            {
                File.Delete(tmpFile2);
            }
        }
    }
}
