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
using DemaConsulting.DictionaryMark.Tests.Files;
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

    /// <summary>Writes a config file into a "config" sub-directory and returns its path.</summary>
    /// <param name="tmpDir">The temporary directory.</param>
    /// <param name="content">The YAML content.</param>
    /// <returns>The full path of the config file.</returns>
    private static string WriteConfig(TemporaryDirectory tmpDir, string content)
    {
        var path = tmpDir.GetFilePath(Path.Combine("config", "dictionarymark.yaml"));
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Test that every config job writes its own output.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_MultipleJobs_WritesEachOutput()
    {
        // Arrange: two dictionaries and two jobs with different formats
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "a.yaml")), "Alpha: First\n");
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "Beta: Second\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: a.yaml\n    output: a.md\n    format: table\n  - input: b.yaml\n    output: b.md\n");

        // Act
        var (_, _, exitCode) = Run("--config", config);

        // Assert: both outputs exist with their own content and format
        Assert.Equal(0, exitCode);
        Assert.Contains("| Alpha | First |", File.ReadAllText(tmpDir.GetFilePath(Path.Combine("config", "a.md"))));
        Assert.Contains("- **Beta**: Second", File.ReadAllText(tmpDir.GetFilePath(Path.Combine("config", "b.md"))));
    }

    /// <summary>Test that a failing job does not stop later jobs and is reported with its label.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_OneJobFails_ContinuesAndReportsJobLabel()
    {
        // Arrange: the first job matches nothing; the second is valid
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "Beta: Second\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: none*.yaml\n    output: a.md\n  - input: b.yaml\n    output: b.md\n");

        // Act
        var (_, err, _) = Run("--config", config);

        // Assert: the first job's error names its index and output; the second job still ran
        Assert.Contains("Error: table job 1 ('a.md'): Input pattern 'none*.yaml' matched no files.", err);
        Assert.True(File.Exists(tmpDir.GetFilePath(Path.Combine("config", "b.md"))));
        Assert.False(File.Exists(Path.Combine(tmpDir.DirectoryPath, "config", "a.md")));
    }

    /// <summary>
    ///     Test that an output path with an invalid character fails only its own job.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_InvalidOutputPathCharacter_ContinuesWithLaterJob()
    {
        // Arrange: the first job's output contains a null character (invalid on every platform); the second is valid
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "a.yaml")), "Alpha: First\n");
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "Beta: Second\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: a.yaml\n    output: \"bad\\0name.md\"\n  - input: b.yaml\n    output: b.md\n");

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert: the first job is reported as an invalid output path; the second job still wrote its output
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: table job 1 ('bad\0name.md'): Invalid output path 'bad\0name.md':", err);
        Assert.Contains("Error: 1 of 2 table jobs failed.", err);
        Assert.Contains("- **Beta**: Second", File.ReadAllText(tmpDir.GetFilePath(Path.Combine("config", "b.md"))));
    }

    /// <summary>
    ///     Test that an input pattern with an invalid character fails only its own job.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_InvalidInputPathCharacter_ContinuesWithLaterJob()
    {
        // Arrange: the first job's input contains a null character (invalid on every platform); the second is valid
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "Beta: Second\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: \"bad\\0name.yaml\"\n    output: a.md\n  - input: b.yaml\n    output: b.md\n");

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert: the never list refuses the control character before anything is listed; the second job still ran
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: table job 1 ('a.md'): Failed to resolve input pattern 'bad?name.yaml': The path '", err);
        Assert.Contains("is not allowed: the path contains a control character.", err);
        Assert.Contains("Error: 1 of 2 table jobs failed.", err);
        Assert.Contains("- **Beta**: Second", File.ReadAllText(tmpDir.GetFilePath(Path.Combine("config", "b.md"))));
    }

    /// <summary>
    ///     Test that an output whose parent is an existing file fails only its own job.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_OutputParentIsFile_ContinuesWithLaterJob()
    {
        // Arrange: the first job writes beneath a file, which cannot become a directory; the second is valid
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "a.yaml")), "Alpha: First\n");
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "Beta: Second\n");
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "blocker")), "x");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: a.yaml\n    output: blocker/a.md\n  - input: b.yaml\n    output: b.md\n");

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert: the write failure is reported against the first job; the second job still wrote its output
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: table job 1 ('blocker/a.md'): Failed to write output file 'blocker/a.md':", err);
        Assert.Contains("Error: 1 of 2 table jobs failed.", err);
        Assert.Contains("- **Beta**: Second", File.ReadAllText(tmpDir.GetFilePath(Path.Combine("config", "b.md"))));
    }

    /// <summary>
    ///     Test that an output file name the platform rejects on write (a wildcard on Windows) fails only its own job.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_WildcardOutputFileNameOnWindows_ContinuesWithLaterJob()
    {
        // A wildcard is a valid file-name character on Linux and macOS, so only Windows rejects it on write
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange: the first job's output name contains a wildcard; the second is valid
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "a.yaml")), "Alpha: First\n");
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "Beta: Second\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: a.yaml\n    output: \"out/*.md\"\n  - input: b.yaml\n    output: b.md\n");

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert: the write failure is reported against the first job; the second job still wrote its output
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: table job 1 ('out/*.md'): Failed to write output file 'out/*.md':", err);
        Assert.Contains("Error: 1 of 2 table jobs failed.", err);
        Assert.Contains("- **Beta**: Second", File.ReadAllText(tmpDir.GetFilePath(Path.Combine("config", "b.md"))));
    }

    /// <summary>
    ///     Test that a syntactically malformed YAML input fails only its own job.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_MalformedYaml_ContinuesWithLaterJob()
    {
        // Arrange: the first job reads a file with an unclosed flow sequence; the second is valid
        using var tmpDir = new TemporaryDirectory();
        var malformed = tmpDir.GetFilePath(Path.Combine("config", "a.yaml"));
        File.WriteAllText(malformed, "Alpha: [unclosed\n");
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "Beta: Second\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: a.yaml\n    output: a.md\n  - input: b.yaml\n    output: b.md\n");

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert: the malformed file is reported against the first job, which wrote nothing; the second job still ran
        Assert.Equal(1, exitCode);
        Assert.Contains($"Error: table job 1 ('a.md'): Invalid YAML in file '{malformed}':", err);
        Assert.Contains("Error: 1 of 2 table jobs failed.", err);
        Assert.False(File.Exists(tmpDir.GetFilePath(Path.Combine("config", "a.md"))));
        Assert.Contains("- **Beta**: Second", File.ReadAllText(tmpDir.GetFilePath(Path.Combine("config", "b.md"))));
    }

    /// <summary>
    ///     Test that a malformed deprecated-terms YAML input fails only its own job.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_MalformedDeprecatedYaml_ContinuesWithLaterJob()
    {
        // Arrange: the first job reads a malformed deprecated-terms file; the second is valid
        using var tmpDir = new TemporaryDirectory();
        var malformed = tmpDir.GetFilePath(Path.Combine("config", "old.yaml"));
        File.WriteAllText(malformed, "bench mode: { use: Test Mode\n");
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "Beta: Second\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: old.yaml\n    output: old.md\n    kind: deprecated\n  - input: b.yaml\n    output: b.md\n");

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert: the malformed file is reported against the first job; the second job still wrote its output
        Assert.Equal(1, exitCode);
        Assert.Contains($"Error: table job 1 ('old.md'): Invalid YAML in file '{malformed}':", err);
        Assert.Contains("- **Beta**: Second", File.ReadAllText(tmpDir.GetFilePath(Path.Combine("config", "b.md"))));
    }

    /// <summary>Test that failed jobs produce a summary and exit code 1.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_JobsFailed_WritesSummaryAndExitCodeOne()
    {
        // Arrange: two jobs that both fail
        using var tmpDir = new TemporaryDirectory();
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: none1.yaml\n    output: a.md\n  - input: none2.yaml\n    output: b.md\n");

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert: both failures are reported, followed by the summary
        Assert.Equal(1, exitCode);
        Assert.Contains("table job 1 ('a.md')", err);
        Assert.Contains("table job 2 ('b.md')", err);
        Assert.Contains("Error: 2 of 2 table jobs failed.", err);
    }

    /// <summary>Test that missing parent directories of the output file are created.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_OutputInMissingDirectory_CreatesDirectory()
    {
        // Arrange: an output path inside directories that do not exist
        using var tmpDir = new TemporaryDirectory();
        var input = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(input, "Alpha: First\n");
        var output = Path.Combine(tmpDir.DirectoryPath, "new", "nested", "out.md");

        // Act
        var (_, _, exitCode) = Run("--input", input, "--output", output);

        // Assert: the directories and the file were created
        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha", File.ReadAllText(output));
    }

    /// <summary>Test that relative inputs and output resolve against the job's base directory.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_JobBaseDirectory_ResolvesRelativeInputsAndOutput()
    {
        // Arrange: a shared dictionary beside the config directory, referenced through '..'
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("shared", "terms.yaml")), "Alpha: First\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: sub/../../shared/*.yaml\n    output: ../out/terms.md\n");

        // Act
        var (_, _, exitCode) = Run("--config", config);

        // Assert: the output was written relative to the config directory
        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha", File.ReadAllText(Path.Combine(tmpDir.DirectoryPath, "out", "terms.md")));
    }

    /// <summary>Test that conflicts are detected within a job but not across jobs.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConflictsDetectedPerJob()
    {
        // Arrange: two dictionaries that conflict, used by separate jobs and then by one job
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "a.yaml")), "API: Application Programming Interface\n");
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "API: Advanced Peripheral Interface\n");
        var separate = WriteConfig(tmpDir,
            "tables:\n  - input: a.yaml\n    output: a.md\n  - input: b.yaml\n    output: b.md\n");

        // Act: separate jobs
        var (_, _, separateExit) = Run("--config", separate);

        // Arrange: one job reading both dictionaries
        var combined = WriteConfig(tmpDir, "tables:\n  - input: [a.yaml, b.yaml]\n    output: c.md\n");

        // Act: combined job
        var (_, combinedErr, combinedExit) = Run("--config", combined);

        // Assert: only the combined job reports the conflict, labeled with the job
        Assert.Equal(0, separateExit);
        Assert.Equal(1, combinedExit);
        Assert.Contains("Error: table job 1 ('c.md'): Conflict: term 'API'", combinedErr);
    }

    /// <summary>Test that column widths reach the generated table.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_ColumnWidths_AppliedToOutput()
    {
        // Arrange: a dictionary file
        using var tmpDir = new TemporaryDirectory();
        var input = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(input, "Alpha: First\n");

        // Act: generate a table with column widths
        var (output, _, exitCode) = Run("--input", input, "--format", "table", "--column-widths", "25,75");

        // Assert: the separator has 25 and 75 dashes
        Assert.Equal(0, exitCode);
        Assert.Contains($"| :{new string('-', 25)} | :{new string('-', 75)} |", output);
    }

    /// <summary>Test that a config input outside the allowed roots fails its job with file-access-denied.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigInputOutsideRoots_FileAccessDenied()
    {
        // Arrange: the input lies next to the config folder, which is the only allowed root
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("shared", "terms.yaml")), "Alpha: First\n");
        var config = WriteConfig(tmpDir, "tables:\n  - input: ../shared/terms.yaml\n    output: out.md\n");
        var root = Path.GetDirectoryName(config)!;

        // Act
        var (_, err, exitCode) = Run("--config", config, "--allow-root", root);

        // Assert: the job is denied and writes nothing
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: table job 1 ('out.md'): file-access-denied: The file '", err);
        Assert.Contains("was denied by the file-access policy 'roots'.", err);
        Assert.Contains("Allow the file in the file-access policy, or reference a file it allows.", err);
        Assert.False(File.Exists(Path.Combine(root, "out.md")));
    }

    /// <summary>
    ///     Test that a literal config input outside the allowed roots is judged before it is looked up: a file that does
    ///     not exist there is reported as file-access-denied, not as not found, so its existence is never probed.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigLiteralInputOutsideRoots_DeniedWithoutLookup()
    {
        // Arrange: the input names a missing file next to the config folder, which is the only allowed root
        using var tmpDir = new TemporaryDirectory();
        var config = WriteConfig(tmpDir, "tables:\n  - input: ../shared/missing.yaml\n    output: out.md\n");
        var root = Path.GetDirectoryName(config)!;
        var missing = Path.Combine(tmpDir.DirectoryPath, "shared", "missing.yaml");

        // Act
        var (_, err, exitCode) = Run("--config", config, "--allow-root", root);

        // Assert: the policy refused the path as written; nothing reports whether the file exists
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: table job 1 ('out.md'): file-access-denied: The file '", err);
        Assert.Contains("was denied by the file-access policy 'roots'.", err);
        Assert.DoesNotContain("was not found", err);
        Assert.DoesNotContain("Input directory", err);
        Assert.False(File.Exists(missing));
    }

    /// <summary>
    ///     Test that a literal config input inside the allowed roots that does not exist is reported clearly as not
    ///     found, through the resolver's outcome.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigLiteralMissingInputInsideRoots_ReportsNotFound()
    {
        // Arrange: the input names a missing file in a missing folder below the config folder (the allowed root)
        using var tmpDir = new TemporaryDirectory();
        var config = WriteConfig(tmpDir, "tables:\n  - input: sub/missing.yaml\n    output: out.md\n");
        var root = Path.GetDirectoryName(config)!;
        var missing = Path.Combine(root, "sub", "missing.yaml");

        // Act
        var (_, err, exitCode) = Run("--config", config, "--allow-root", root);

        // Assert: a not-found error naming the full path; no output was written
        Assert.Equal(1, exitCode);
        Assert.Contains($"Error: table job 1 ('out.md'): Input file '{missing}' was not found.", err);
        Assert.DoesNotContain("file-access-denied", err);
        Assert.False(File.Exists(Path.Combine(root, "out.md")));
    }

    /// <summary>
    ///     Test that a glob whose matches lie both inside and outside the allowed roots fails its job: the match outside
    ///     is denied rather than skipped, and the match inside is not used alone.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigGlobMatchOutsideRoots_IsDeniedNotSkipped()
    {
        // Arrange: one match inside the config folder (the root) and one in a sibling folder
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "terms-a.yaml")), "Alpha: First\n");
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("other", "terms-b.yaml")), "Beta: Second\n");
        var config = WriteConfig(tmpDir, "tables:\n  - input: ../*/terms-*.yaml\n    output: out.md\n");
        var root = Path.GetDirectoryName(config)!;

        // Act
        var (_, err, exitCode) = Run("--config", config, "--allow-root", root);

        // Assert: the outside match fails the job, so no output is written from the inside match alone
        Assert.Equal(1, exitCode);
        Assert.Contains("file-access-denied:", err);
        Assert.Contains("terms-b.yaml", err);
        Assert.False(File.Exists(Path.Combine(root, "out.md")));
    }

    /// <summary>Test that a glob match reached through a directory link is denied under no-symlinks.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigGlobMatchThroughLinkUnderNoSymlinks_FileAccessDenied()
    {
        // Arrange: config/alias is a link to config/real, which holds the dictionary
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "real", "terms.yaml")), "Alpha: First\n");
        var config = WriteConfig(tmpDir, "tables:\n  - input: alias/*.yaml\n    output: out.md\n");
        var root = Path.GetDirectoryName(config)!;
        FileTestSupport.CreateDirectoryLinkOrSkip(Path.Combine(root, "alias"), Path.Combine(root, "real"));

        // Act
        var (_, err, exitCode) = Run("--config", config, "--file-access", "no-symlinks");

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains("file-access-denied:", err);
        Assert.Contains("'no-symlinks'", err);
        Assert.False(File.Exists(Path.Combine(root, "out.md")));
    }

    /// <summary>Test that a config output outside the allowed roots is denied and nothing is created.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigOutputOutsideRoots_DeniedAndNothingCreated()
    {
        // Arrange: the output folder lies outside the config folder, which is the only allowed root
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "terms.yaml")), "Alpha: First\n");
        var config = WriteConfig(tmpDir, "tables:\n  - input: terms.yaml\n    output: ../out/table.md\n");
        var root = Path.GetDirectoryName(config)!;

        // Act
        var (_, err, exitCode) = Run("--config", config, "--allow-root", root);

        // Assert: denied, and not even the output folder was created
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: table job 1 ('../out/table.md'): file-access-denied:", err);
        Assert.False(Directory.Exists(Path.Combine(tmpDir.DirectoryPath, "out")));
    }

    /// <summary>
    ///     Test that under the default mode a config output that breaks a never-list rule (a colon outside a drive, and
    ///     on Windows a network path) is refused before anything is accessed or created.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigNeverListOutputUnderAny_RefusedWithoutAccess()
    {
        // Arrange: one output names an alternate data stream; on Windows another names a network share
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "terms.yaml")), "Alpha: First\n");
        var network = OperatingSystem.IsWindows() ? "  - input: terms.yaml\n    output: //server/share/out.md\n" : string.Empty;
        var config = WriteConfig(tmpDir, "tables:\n  - input: terms.yaml\n    output: \"out.md:stream\"\n" + network);

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert: ordinary write errors naming the broken rule; nothing was written
        Assert.Equal(1, exitCode);
        Assert.Contains("Failed to write output file 'out.md:stream': The path '", err);
        Assert.Contains("is not allowed: the path contains a colon.", err);
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(config)!, "out.md")));
        if (OperatingSystem.IsWindows())
        {
            Assert.Contains("is not allowed: network paths are not permitted.", err);
        }
    }

    /// <summary>
    ///     Test that a config output naming a network share is refused on every platform before anything is accessed or
    ///     created: on Linux and macOS a leading "//" would collapse to a local "/" if normalized before the check, and
    ///     on Windows the backslash spelling is refused as well.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigNetworkOutput_RefusedBeforeNormalization()
    {
        // Arrange: a forward-slash share on every platform; on Windows also a backslash share
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "terms.yaml")), "Alpha: First\n");
        var backslash = OperatingSystem.IsWindows()
            ? "  - input: terms.yaml\n    output: '\\\\dictionarymark-test.invalid\\share\\out.md'\n"
            : string.Empty;
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: terms.yaml\n    output: \"//dictionarymark-test.invalid/share/out.md\"\n" + backslash);
        var jobs = OperatingSystem.IsWindows() ? 2 : 1;

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert: every job is refused by the network-path rule, so nothing was written
        Assert.Equal(1, exitCode);
        Assert.Contains(
            "Error: table job 1 ('//dictionarymark-test.invalid/share/out.md'): Failed to write output file " +
            "'//dictionarymark-test.invalid/share/out.md': The path '//dictionarymark-test.invalid/share/out.md' is not " +
            "allowed: network paths are not permitted.",
            err);
        Assert.Contains($"Error: {jobs} of {jobs} table jobs failed.", err);
        Assert.DoesNotContain("file-access-denied", err);
    }

    /// <summary>
    ///     Test that a relative config output under a root key naming a network share is refused on every platform
    ///     before anything is accessed or created: the root is joined with the output as text, so a leading "//" is not
    ///     collapsed to a local "/" on Linux and macOS, and on Windows the backslash spelling is refused as well.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigNetworkRootRelativeOutput_RefusedBeforeNormalization()
    {
        // Arrange: one config per spelling of the share; the input is an absolute local file, so only the output is
        // rooted at the share
        using var tmpDir = new TemporaryDirectory();
        var input = tmpDir.GetFilePath(Path.Combine("local", "terms.yaml"));
        File.WriteAllText(input, "Alpha: First\n");
        var roots = OperatingSystem.IsWindows()
            ? new[] { "\"//dictionarymark-test.invalid/share\"", "'\\\\dictionarymark-test.invalid\\share'" }
            : ["\"//dictionarymark-test.invalid/share\""];

        foreach (var root in roots)
        {
            var config = WriteConfig(tmpDir, $"root: {root}\ntables:\n  - input: '{input}'\n    output: out.md\n");

            // Act
            var (_, err, exitCode) = Run("--config", config);

            // Assert: the output is refused by the network-path rule; the input was not the problem
            Assert.Equal(1, exitCode);
            Assert.Contains("Error: table job 1 ('out.md'): Failed to write output file 'out.md': The path '", err);
            Assert.Contains("is not allowed: network paths are not permitted.", err);
            Assert.DoesNotContain("file-access-denied", err);
        }
    }

    /// <summary>
    ///     Test that a relative config input under a root key naming a network share is refused on every platform before
    ///     it is looked up: the root is joined with the pattern as text, so a leading "//" is not collapsed to a local "/"
    ///     on Linux and macOS.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigNetworkRootRelativeInput_RefusedBeforeNormalization()
    {
        // Arrange: the root names a share; the literal input and the output are relative to it
        using var tmpDir = new TemporaryDirectory();
        var config = WriteConfig(tmpDir,
            "root: \"//dictionarymark-test.invalid/share\"\ntables:\n  - input: terms.yaml\n    output: out.md\n");

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert: the input pattern is refused by the network-path rule before any lookup
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: table job 1 ('out.md'): Failed to resolve input pattern 'terms.yaml': The path '", err);
        Assert.Contains("is not allowed: network paths are not permitted.", err);
        Assert.DoesNotContain("was not found", err);
    }

    /// <summary>
    ///     Test that on Windows a config pattern with a reserved device-name component is refused before any folder is
    ///     listed, so the device is never probed, and the other job still runs.
    /// </summary>
    /// <param name="pattern">The input pattern.</param>
    [Theory]
    [InlineData("NUL/*.yaml")]
    [InlineData("C:/NUL/*.yaml")]
    [InlineData("CON/*.yaml")]
    [InlineData("AUX/*.yaml")]
    [InlineData("COM1/*.yaml")]
    [InlineData("LPT1/*.yaml")]
    public void DictionaryGenerator_Generate_ConfigDeviceNamePatternOnWindows_RefusedBeforeListing(string pattern)
    {
        // Reserved device names exist only on Windows
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.WindowsOnly);
        }

        // Arrange
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "Beta: Second\n");
        var config = WriteConfig(tmpDir,
            $"tables:\n  - input: \"{pattern}\"\n    output: a.md\n  - input: b.yaml\n    output: b.md\n");
        var root = Path.GetDirectoryName(config)!;

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert: refused by the device-name rule, so nothing was listed; the other job wrote its output
        Assert.Equal(1, exitCode);
        Assert.Contains($"Error: table job 1 ('a.md'): Failed to resolve input pattern '{pattern}': The path '", err);
        Assert.Contains("is not allowed: the path names a device.", err);
        Assert.DoesNotContain("matched no files", err);
        Assert.Contains("Error: 1 of 2 table jobs failed.", err);
        Assert.False(File.Exists(Path.Combine(root, "a.md")));
        Assert.Contains("- **Beta**: Second", File.ReadAllText(Path.Combine(root, "b.md")));
    }

    /// <summary>Test that a config input longer than the 16 MiB size limit is an ordinary read error.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigInputOverSizeLimit_ReadError()
    {
        // Arrange: a YAML comment one byte longer than the limit
        using var tmpDir = new TemporaryDirectory();
        var input = tmpDir.GetFilePath(Path.Combine("config", "big.yaml"));
        var bytes = new byte[(16 * 1024 * 1024) + 1];
        Array.Fill(bytes, (byte)' ');
        bytes[0] = (byte)'#';
        File.WriteAllBytes(input, bytes);
        var config = WriteConfig(tmpDir, "tables:\n  - input: big.yaml\n    output: out.md\n");

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains($"Failed to read file '{input}': The file '", err);
        Assert.Contains("is larger than the allowed size.", err);
        Assert.DoesNotContain("file-access-denied", err);
    }

    /// <summary>Test that a denied job fails only itself: the other jobs run and the exit code is 1.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_DeniedJob_OtherJobsRunAndExitCodeIsOne()
    {
        // Arrange: the first job writes outside the allowed root; the second writes inside it
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "terms.yaml")), "Alpha: First\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: terms.yaml\n    output: ../out/a.md\n  - input: terms.yaml\n    output: b.md\n");
        var root = Path.GetDirectoryName(config)!;

        // Act
        var (_, err, exitCode) = Run("--config", config, "--allow-root", root);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: table job 1 ('../out/a.md'): file-access-denied:", err);
        Assert.Contains("Error: 1 of 2 table jobs failed.", err);
        Assert.Contains("- **Alpha**: First", File.ReadAllText(Path.Combine(root, "b.md")));
    }

    /// <summary>Test that the command-line job is never judged by the file-access policy.</summary>
    [Fact]
    public void DictionaryGenerator_Generate_CommandLineJob_IsNeverJudged()
    {
        // Arrange: the input and output lie outside the only allowed root
        using var tmpDir = new TemporaryDirectory();
        var root = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "root")).FullName;
        var input = tmpDir.GetFilePath(Path.Combine("outside", "terms.yaml"));
        File.WriteAllText(input, "Alpha: First\n");
        var output = Path.Combine(tmpDir.DirectoryPath, "outside", "out", "terms.md");

        // Act
        var (_, err, exitCode) = Run("--allow-root", root, "--input", input, "--output", output);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, err);
        Assert.Contains("- **Alpha**: First", File.ReadAllText(output));
    }

    /// <summary>
    ///     Test that a config pattern naming a network share with forward slashes is refused before any folder is listed:
    ///     the job fails with the network-path rule (not "matched no files"), and the other job still runs.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigNetworkPatternForwardSlashes_RefusedBeforeListing()
    {
        // Arrange: on every platform the resolver's text rules treat a leading "//" as a network path
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "Beta: Second\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: \"//dictionarymark-test.invalid/share/*.yaml\"\n    output: a.md\n" +
            "  - input: b.yaml\n    output: b.md\n");
        var root = Path.GetDirectoryName(config)!;

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert: refused by the never list, so the share was never listed; the other job wrote its output
        Assert.Equal(1, exitCode);
        Assert.Contains(
            "Error: table job 1 ('a.md'): Failed to resolve input pattern '//dictionarymark-test.invalid/share/*.yaml': " +
            "The path '//dictionarymark-test.invalid/share/*.yaml' is not allowed: network paths are not permitted.",
            err);
        Assert.DoesNotContain("matched no files", err);
        Assert.Contains("Error: 1 of 2 table jobs failed.", err);
        Assert.False(File.Exists(Path.Combine(root, "a.md")));
        Assert.Contains("- **Beta**: Second", File.ReadAllText(Path.Combine(root, "b.md")));
    }

    /// <summary>
    ///     Test that on Windows a config pattern naming a network share with backslashes is refused before any folder is
    ///     listed, and the other job still runs.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigNetworkPatternBackslashesOnWindows_RefusedBeforeListing()
    {
        // A backslash is an ordinary file-name character on Linux and macOS, so only Windows reads this as a share
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "Beta: Second\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: '\\\\dictionarymark-test.invalid\\share\\*.yaml'\n    output: a.md\n" +
            "  - input: b.yaml\n    output: b.md\n");
        var root = Path.GetDirectoryName(config)!;

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains(
            "Error: table job 1 ('a.md'): Failed to resolve input pattern '\\\\dictionarymark-test.invalid\\share\\*.yaml': ",
            err);
        Assert.Contains("is not allowed: network paths are not permitted.", err);
        Assert.DoesNotContain("matched no files", err);
        Assert.Contains("Error: 1 of 2 table jobs failed.", err);
        Assert.False(File.Exists(Path.Combine(root, "a.md")));
        Assert.Contains("- **Beta**: Second", File.ReadAllText(Path.Combine(root, "b.md")));
    }

    /// <summary>
    ///     Test that a URL-like config pattern is refused by the colon rule before any folder is listed, and the other job
    ///     still runs.
    /// </summary>
    [Fact]
    public void DictionaryGenerator_Generate_ConfigUrlPattern_RefusedBeforeListing()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "b.yaml")), "Beta: Second\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: \"https://dictionarymark-test.invalid/terms/*.yaml\"\n    output: a.md\n" +
            "  - input: b.yaml\n    output: b.md\n");
        var root = Path.GetDirectoryName(config)!;

        // Act
        var (_, err, exitCode) = Run("--config", config);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains(
            "Error: table job 1 ('a.md'): Failed to resolve input pattern 'https://dictionarymark-test.invalid/terms/*.yaml': ",
            err);
        Assert.Contains("is not allowed: the path contains a colon.", err);
        Assert.DoesNotContain("matched no files", err);
        Assert.DoesNotContain("file-access-denied", err);
        Assert.Contains("Error: 1 of 2 table jobs failed.", err);
        Assert.False(File.Exists(Path.Combine(root, "a.md")));
        Assert.Contains("- **Beta**: Second", File.ReadAllText(Path.Combine(root, "b.md")));
    }
}
