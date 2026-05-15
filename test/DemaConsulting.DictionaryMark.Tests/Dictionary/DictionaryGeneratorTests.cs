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
///     Unit tests for the DictionaryGenerator class.
/// </summary>
[Collection("Sequential")]
public class DictionaryGeneratorTests
{
    /// <summary>
    ///     Test that generating with no input patterns reports an error.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_NoInputPatterns_ReportsError()
    {
        // Arrange: context with no --input flags; redirect stderr to capture error output
        var originalError = Console.Error;
        try
        {
            using var errWriter = new StringWriter();
            Console.SetError(errWriter);
            using var context = Context.Create([]);

            // Act
            var generator = new DictionaryGenerator();
            generator.Generate(context);

            // Assert
            Assert.Equal(1, context.ExitCode);
            Assert.Contains("No input files found", errWriter.ToString());
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test that generating from a single YAML file writes to stdout.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_SingleYamlFile_WritesToStdout()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("input.yaml");
        var originalOut = Console.Out;
        try
        {
            File.WriteAllText(tmpFile, "API: Application Programming Interface\n");

            using var outWriter = new StringWriter();
            Console.SetOut(outWriter);
            using var context = Context.Create(["--input", tmpFile]);

            // Act
            var generator = new DictionaryGenerator();
            generator.Generate(context);

            // Assert
            var output = outWriter.ToString();
            Assert.Contains("API", output);
            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that conflicting entries reports an error.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConflictingEntries_ReportsError()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var tmpFile1 = tmpDir.GetFilePath("first.yaml");
        var tmpFile2 = tmpDir.GetFilePath("second.yaml");
        var originalError = Console.Error;
        try
        {
            File.WriteAllText(tmpFile1, "API: Application Programming Interface\n");
            File.WriteAllText(tmpFile2, "API: Advanced Peripheral Interface\n");

            using var errWriter = new StringWriter();
            Console.SetError(errWriter);
            using var context = Context.Create(["--input", tmpFile1, "--input", tmpFile2]);

            // Act
            var generator = new DictionaryGenerator();
            generator.Generate(context);

            // Assert
            Assert.Equal(1, context.ExitCode);
            Assert.Contains("Conflict: term 'API' has multiple definitions", errWriter.ToString());
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test that generating with an output file writes the formatted output to that file.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_OutputFile_WritesToFile()
    {
        // Arrange: create a temporary YAML file and specify a temporary output file path
        using var tmpDir = new TemporaryDirectory();
        var tmpInputFile = tmpDir.GetFilePath("input.yaml");
        var tmpOutputFile = tmpDir.GetFilePath("output.md");
        try
        {
            File.WriteAllText(tmpInputFile, "API: Application Programming Interface\n");
            if (File.Exists(tmpOutputFile))
            {
                File.Delete(tmpOutputFile);
            }

            using var context = Context.Create(["--input", tmpInputFile, "--output", tmpOutputFile]);

            // Act: generate with an output file configured
            var generator = new DictionaryGenerator();
            generator.Generate(context);

            // Assert: output file exists and contains the expected content; exit code is 0
            Assert.True(File.Exists(tmpOutputFile), "Output file should be created by Generate");
            var fileContent = File.ReadAllText(tmpOutputFile);
            Assert.Contains("API", fileContent);
            Assert.Equal(0, context.ExitCode);
        }
        finally
        {
            if (File.Exists(tmpOutputFile))
            {
                File.Delete(tmpOutputFile);
            }
        }
    }
}
