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
            DictionaryGenerator.Generate(context);

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
        // Arrange: create a temporary YAML file with one entry; redirect stdout to capture generated output
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
            DictionaryGenerator.Generate(context);

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
        // Arrange: two YAML files defining the same term with different definitions; redirect stderr to capture error
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
            DictionaryGenerator.Generate(context);

            // Assert
            Assert.Equal(1, context.ExitCode);
            Assert.Contains("Conflict: term 'API' differs in definition between", errWriter.ToString());
            Assert.Contains(tmpFile1, errWriter.ToString());
            Assert.Contains(tmpFile2, errWriter.ToString());
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
        File.WriteAllText(tmpInputFile, "API: Application Programming Interface\n");
        using var context = Context.Create(["--input", tmpInputFile, "--output", tmpOutputFile]);

        // Act: generate with an output file configured
        DictionaryGenerator.Generate(context);

        // Assert: output file exists and contains the expected content; exit code is 0
        Assert.True(File.Exists(tmpOutputFile), "Output file should be created by Generate");
        var fileContent = File.ReadAllText(tmpOutputFile);
        Assert.Contains("API", fileContent);
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>Runs the generator with the arguments and returns stdout, stderr and the exit code.</summary>
    private static (string Out, string Err, int ExitCode) Run(params string[] args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            using var outWriter = new StringWriter();
            using var errWriter = new StringWriter();
            Console.SetOut(outWriter);
            Console.SetError(errWriter);
            using var context = Context.Create(args);
            DictionaryGenerator.Generate(context);
            return (outWriter.ToString(), errWriter.ToString(), context.ExitCode);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    /// <summary>Test that hidden entries are omitted from the output but still conflict.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_HiddenEntry_OmittedFromOutput()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("terms.yaml");
        File.WriteAllText(file, "Alpha: First\nprime:\n  definition: Hidden\n  pos: verb\n  visible: false\n");

        // Act
        var (output, _, exitCode) = Run("--input", file);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha", output);
        Assert.DoesNotContain("prime", output);
    }

    /// <summary>Test that a hidden entry without a definition is an error.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_HiddenEntryWithoutDefinition_ReportsError()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("terms.yaml");
        File.WriteAllText(file, "prime:\n  pos: verb\n  visible: false\n");

        // Act
        var (_, err, exitCode) = Run("--input", file);

        // Assert: the error names the term and the file
        Assert.Equal(1, exitCode);
        Assert.Contains("Term 'prime'", err);
        Assert.Contains(file, err);
    }

    /// <summary>Test the deprecated table output.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_DeprecatedKind_WritesTable()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("deprecated.yaml");
        File.WriteAllText(file, "bench mode: { use: Test Mode, where: Product A }\nE-stop: { use: emergency stop }\n");

        // Act
        var (output, _, exitCode) = Run("--input", file, "--kind", "deprecated", "--format", "table");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("| Not used | Use instead | Where it was used |", output);
        Assert.Contains("| bench mode | Test Mode | Product A |", output);
    }

    /// <summary>Test that an explicit term header applies to the deprecated kind.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_DeprecatedKindCustomHeaders_AreUsed()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("deprecated.yaml");
        File.WriteAllText(file, "bench mode: { use: Test Mode, where: Product A }\n");

        // Act
        var (output, _, _) = Run(
            "--input", file, "--kind", "deprecated", "--format", "table",
            "--term-header", "Avoid", "--use-header", "Prefer", "--where-header", "Seen in");

        // Assert
        Assert.Contains("| Avoid | Prefer | Seen in |", output);
    }

    /// <summary>Test that a missing file and an unmatched glob are errors naming the pattern.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_MissingInputs_ReportErrors()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var missing = tmpDir.GetFilePath("missing.yaml");
        var glob = Path.Combine(tmpDir.DirectoryPath, "*.yaml");

        // Act
        var (_, fileErr, fileExit) = Run("--input", missing);
        var (_, globErr, globExit) = Run("--input", glob);

        // Assert
        Assert.Equal(1, fileExit);
        Assert.Contains(missing, fileErr);
        Assert.Equal(1, globExit);
        Assert.Contains($"Input pattern '{glob}' matched no files", globErr);
    }

    /// <summary>Test that one missing pattern fails even when another matches.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_OneMissingPattern_Fails()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var good = tmpDir.GetFilePath("good.yaml");
        File.WriteAllText(good, "Alpha: First\n");

        // Act
        var (output, _, exitCode) = Run("--input", good, "--input", tmpDir.GetFilePath("none*.yaml"));

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Equal(string.Empty, output);
    }
}
