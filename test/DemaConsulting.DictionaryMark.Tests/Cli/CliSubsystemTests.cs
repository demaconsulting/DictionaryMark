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
using DemaConsulting.DictionaryMark.Utilities;

namespace DemaConsulting.DictionaryMark.Tests.Cli;

/// <summary>
///     Subsystem tests for the CLI subsystem covering Context and Program integration.
/// </summary>
[Collection("Sequential")]
public class CliSubsystemTests
{
    /// <summary>
    ///     Test that Context and Program work together to handle version flag workflow.
    /// </summary>
    [Fact]
    public void CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits()
    {
        // Arrange: command line arguments with version flag; capture console output
        var args = new[] { "--version" };
        var originalOut = Console.Out;
        using var capturedOut = new StringWriter();

        try
        {
            Console.SetOut(capturedOut);

            // Act: create context and run program logic
            using var context = Context.Create(args);
            Program.Run(context);

            // Assert: version flag is parsed, version text is displayed, and exit code is success
            Assert.True(context.Version, "Context should parse version flag");
            Assert.Equal(0, context.ExitCode);
            Assert.Contains(Program.Version, capturedOut.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Context and Program work together to handle the -v short version flag.
    /// </summary>
    [Fact]
    public void CliSubsystem_VersionFlow_ContextAndProgram_DisplaysVersionAndExits_WithShortVFlag()
    {
        // Arrange: command line arguments with -v short version flag; capture console output
        var args = new[] { "-v" };
        var originalOut = Console.Out;
        using var capturedOut = new StringWriter();

        try
        {
            Console.SetOut(capturedOut);

            // Act: create context and run program logic
            using var context = Context.Create(args);
            Program.Run(context);

            // Assert: version flag is parsed, version text is displayed, and exit code is success
            Assert.True(context.Version, "Context should parse -v flag as version");
            Assert.Equal(0, context.ExitCode);
            Assert.Contains(Program.Version, capturedOut.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Context and Program work together to handle help flag workflow.
    /// </summary>
    [Fact]
    public void CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits()
    {
        // Arrange: command line arguments with help flag; capture console output
        var args = new[] { "--help" };
        var originalOut = Console.Out;
        using var capturedOut = new StringWriter();

        try
        {
            Console.SetOut(capturedOut);

            // Act: create context and run program logic
            using var context = Context.Create(args);
            Program.Run(context);

            // Assert: help flag is parsed, usage text is displayed, and exit code is success
            Assert.True(context.Help, "Context should parse help flag");
            Assert.Equal(0, context.ExitCode);
            var output = capturedOut.ToString();
            Assert.Contains("Usage:", output);
            Assert.Contains("Options:", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Context and Program work together to handle the -? short help flag.
    /// </summary>
    [Fact]
    public void CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortQuestionFlag()
    {
        // Arrange: command line arguments with -? short help flag; capture console output
        var args = new[] { "-?" };
        var originalOut = Console.Out;
        using var capturedOut = new StringWriter();

        try
        {
            Console.SetOut(capturedOut);

            // Act: create context and run program logic
            using var context = Context.Create(args);
            Program.Run(context);

            // Assert: help flag is parsed, usage text is displayed, and exit code is success
            Assert.True(context.Help, "Context should parse -? flag as help");
            Assert.Equal(0, context.ExitCode);
            var output = capturedOut.ToString();
            Assert.Contains("Usage:", output);
            Assert.Contains("Options:", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Context and Program work together to handle the -h short help flag.
    /// </summary>
    [Fact]
    public void CliSubsystem_HelpFlow_ContextAndProgram_DisplaysHelpAndExits_WithShortHFlag()
    {
        // Arrange: command line arguments with -h short help flag; capture console output
        var args = new[] { "-h" };
        var originalOut = Console.Out;
        using var capturedOut = new StringWriter();

        try
        {
            Console.SetOut(capturedOut);

            // Act: create context and run program logic
            using var context = Context.Create(args);
            Program.Run(context);

            // Assert: help flag is parsed, usage text is displayed, and exit code is success
            Assert.True(context.Help, "Context should parse -h flag as help");
            Assert.Equal(0, context.ExitCode);
            var output = capturedOut.ToString();
            Assert.Contains("Usage:", output);
            Assert.Contains("Options:", output);
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Context and Program work together to handle validation flag workflow.
    /// </summary>
    [Fact]
    public void CliSubsystem_ValidateFlow_ContextAndProgram_RunsValidationAndExits()
    {
        // Arrange: command line arguments with validate flag; capture console output
        var args = new[] { "--validate" };
        var originalOut = Console.Out;
        using var capturedOut = new StringWriter();

        try
        {
            Console.SetOut(capturedOut);

            // Act: create context and run program logic
            using var context = Context.Create(args);
            Program.Run(context);

            // Assert: validate flag is parsed, summary is output, and exit code is success
            Assert.True(context.Validate, "Context should parse validate flag");
            Assert.Equal(0, context.ExitCode);
            Assert.Contains("Total Tests:", capturedOut.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
        }
    }

    /// <summary>
    ///     Test that Context and Program work together to handle silent flag workflow.
    /// </summary>
    [Fact]
    public void CliSubsystem_SilentFlow_ContextAndProgram_SuppressesOutput()
    {
        // Arrange: command line arguments with version and silent flags; capture console streams
        var args = new[] { "--version", "--silent" };
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var capturedOut = new StringWriter();
        using var capturedError = new StringWriter();

        try
        {
            Console.SetOut(capturedOut);
            Console.SetError(capturedError);

            // Act: create context and run program logic
            using var context = Context.Create(args);
            Program.Run(context);

            // Assert: silent flag is parsed, exit code is success, and no console output is produced
            Assert.True(context.Silent, "Context should parse silent flag");
            Assert.Equal(0, context.ExitCode);
            Assert.Equal(string.Empty, capturedOut.ToString());
            Assert.Equal(string.Empty, capturedError.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test that Context and Program work together to handle results flag workflow.
    /// </summary>
    [Fact]
    public void CliSubsystem_ResultsFlow_ContextAndProgram_WritesResultsFile()
    {
        // Arrange: temporary results file path and validation command with results output
        using var tmpDir = new TemporaryDirectory();
        var resultsFile = tmpDir.GetFilePath("results.trx");
        var args = new[] { "--validate", "--silent", "--results", resultsFile };

        // Act: create context and run program logic
        using var context = Context.Create(args);
        Program.Run(context);

        // Assert: results flag is parsed, validation runs, and results file is written
        Assert.Equal(resultsFile, context.ResultsFile);
        Assert.Equal(0, context.ExitCode);
        Assert.True(File.Exists(resultsFile), "Results file should be written to specified path");
    }

    /// <summary>
    ///     Test that Context and Program work together to handle log flag workflow.
    /// </summary>
    [Fact]
    public void CliSubsystem_LogFlow_ContextAndProgram_WritesLogFile()
    {
        // Arrange: temporary log file path and version command with log output
        using var tmpDir = new TemporaryDirectory();
        var logFile = tmpDir.GetFilePath("cli_test.log");
        var args = new[] { "--version", "--log", logFile };

        // Act: create context and run program logic
        using (var context = Context.Create(args))
        {
            Program.Run(context);

            // Assert: version flag is parsed and exit code is success
            Assert.True(context.Version, "Context should parse version flag");
            Assert.Equal(0, context.ExitCode);
        }

        // Assert: log file is written with version output
        Assert.True(File.Exists(logFile), "Log file should be created at specified path");
        var logContent = File.ReadAllText(logFile);
        Assert.Contains(Program.Version, logContent);
    }

    /// <summary>
    ///     Test that Program rejects unknown arguments, writes an error to stderr, and exits non-zero.
    /// </summary>
    [Fact]
    public void CliSubsystem_InvalidArgs_ContextAndProgram_RejectsUnknownArgumentsAndExitsNonZero()
    {
        // Arrange: unknown command-line argument
        var args = new[] { "--unknown-flag" };

        var originalError = Console.Error;
        try
        {
            using var errWriter = new StringWriter();
            Console.SetError(errWriter);

            // Act: invoke the actual CLI entry point with an unknown flag
            var result = Program.Main(args);

            // Assert: invalid arguments produce a non-zero exit code and an error on stderr
            Assert.Equal(1, result);
            var errorOutput = errWriter.ToString();
            Assert.False(string.IsNullOrWhiteSpace(errorOutput), "Program should write an error to stderr for unknown arguments");
            Assert.Contains("--unknown-flag", errorOutput);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test that Context writes error messages to stderr.
    /// </summary>
    [Fact]
    public void CliSubsystem_ErrorOutput_ContextAndProgram_WritesErrorToStderr()
    {
        // Arrange: redirect stderr to capture error output
        var originalError = Console.Error;
        try
        {
            using var errWriter = new StringWriter();
            Console.SetError(errWriter);
            using var context = Context.Create([]);

            // Act: write an error message through the context
            context.WriteError("Test error message");

            // Assert: error is written to stderr and exit code reflects failure
            var errorOutput = errWriter.ToString();
            Assert.Contains("Test error message", errorOutput);
            Assert.Equal(1, context.ExitCode);
        }
        finally
        {
            Console.SetError(originalError);
        }
    }

    /// <summary>
    ///     Test that Context and Program work together to handle the --result legacy alias for results.
    /// </summary>
    [Fact]
    public void CliSubsystem_ResultAliasFlow_ContextAndProgram_WritesResultsFile()
    {
        // Arrange: temporary results file path and validation command with legacy --result alias
        using var tmpDir = new TemporaryDirectory();
        var resultsFile = tmpDir.GetFilePath("results.trx");
        var args = new[] { "--validate", "--silent", "--result", resultsFile };

        // Act: create context and run program logic
        using var context = Context.Create(args);
        Program.Run(context);

        // Assert: legacy --result alias is parsed, validation runs, and results file is written
        Assert.Equal(resultsFile, context.ResultsFile);
        Assert.Equal(0, context.ExitCode);
        Assert.True(File.Exists(resultsFile), "Results file should be written to specified path");
    }

    /// <summary>
    ///     Test that Context and Program work together to handle depth flag with self-validation.
    /// </summary>
    [Fact]
    public void CliSubsystem_DepthFlow_ContextAndProgram_AdjustsHeadingDepth()
    {
        // Arrange: dictionary-generation command with --depth 2, --section, and a log file to capture output
        using var tmpDir = new TemporaryDirectory();
        var inputFile = tmpDir.GetFilePath("input.yaml");
        var logFile = tmpDir.GetFilePath("cli_test.log");
        File.WriteAllText(inputFile, "API: Application Programming Interface\n");
        var args = new[] { "--input", inputFile, "--section", "CLI Depth", "--depth", "2", "--silent", "--log", logFile };

        // Act: create context and run program logic
        using (var context = Context.Create(args))
        {
            Program.Run(context);

            // Assert: depth is parsed correctly
            Assert.Equal(2, context.HeadingDepth);
            Assert.Equal(0, context.ExitCode);
        }

        // Assert: generated output in log contains level-2 heading
        var logContent = File.ReadAllText(logFile);
        Assert.Contains("## CLI Depth", logContent);
    }

    /// <summary>Runs Program.Main with captured console output and returns the exit code and stderr.</summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>The exit code and the captured standard error.</returns>
    private static (int ExitCode, string Error) RunMain(params string[] args)
    {
        var originalOut = Console.Out;
        var originalError = Console.Error;
        try
        {
            using var outWriter = new StringWriter();
            using var errWriter = new StringWriter();
            Console.SetOut(outWriter);
            Console.SetError(errWriter);
            var exitCode = Program.Main(args);
            return (exitCode, errWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
        }
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

    /// <summary>Expected separator for column widths 25,75 (or 1,3).</summary>
    private static readonly string Separator2575 = $"| :{new string('-', 25)} | :{new string('-', 75)} |";

    /// <summary>
    ///     Test that Context and Program work together to apply --column-widths to a generated table.
    /// </summary>
    [Fact]
    public void CliSubsystem_ColumnWidthsFlow_ContextAndProgram_WritesProportionalTable()
    {
        // Arrange: a dictionary and a table command with column widths
        using var tmpDir = new TemporaryDirectory();
        var inputFile = tmpDir.GetFilePath("input.yaml");
        var outputFile = tmpDir.GetFilePath("output.md");
        File.WriteAllText(inputFile, "API: Application Programming Interface\n");
        var args = new[] { "--silent", "--input", inputFile, "--format", "table", "--column-widths", "25,75", "--output", outputFile };

        // Act: create context and run program logic
        using var context = Context.Create(args);
        Program.Run(context);

        // Assert: the widths are parsed and the table separator is proportional
        Assert.Equal(0, context.ExitCode);
        Assert.Equal([25, 75], context.ColumnWidths!);
        Assert.Contains(Separator2575, File.ReadAllText(outputFile));
    }

    /// <summary>
    ///     Test that an invalid --column-widths value exits non-zero with an error on stderr.
    /// </summary>
    [Fact]
    public void CliSubsystem_ColumnWidthsFlow_InvalidValue_ExitsNonZero()
    {
        // Act: run the entry point with a zero width
        var (exitCode, error) = RunMain("--column-widths", "0,1");

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains("--column-widths requires 2 or 3 comma-separated positive integers", error);
    }

    /// <summary>
    ///     Test that Context and Program work together to generate every table job of a config file.
    /// </summary>
    [Fact]
    public void CliSubsystem_ConfigFlow_TableJobs_GeneratesEachOutput()
    {
        // Arrange: a dictionary and a config with two jobs
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("shared", "terms.yaml")), "API: Application Programming Interface\n");
        var config = WriteConfig(tmpDir,
            "format: table\ntables:\n  - input: ../shared/*.yaml\n    output: ../out/a.md\n" +
            "  - input: ../shared/terms.yaml\n    output: ../out/b.md\n    format: bullets\n");

        // Act: create context and run program logic
        using var context = Context.Create(["--silent", "--config", config]);
        Program.Run(context);

        // Assert: both outputs exist with their own format
        Assert.Equal(0, context.ExitCode);
        Assert.Contains("| API |", File.ReadAllText(Path.Combine(tmpDir.DirectoryPath, "out", "a.md")));
        Assert.Contains("- **API**:", File.ReadAllText(Path.Combine(tmpDir.DirectoryPath, "out", "b.md")));
    }

    /// <summary>
    ///     Test that command-line formatting options override the config file for every job.
    /// </summary>
    [Fact]
    public void CliSubsystem_ConfigFlow_CommandLineOverride_AppliesToEveryJob()
    {
        // Arrange: a config whose jobs set bullets, overridden on the command line
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "terms.yaml")), "API: Application Programming Interface\n");
        var config = WriteConfig(tmpDir,
            "format: bullets\ntables:\n  - input: terms.yaml\n    output: a.md\n" +
            "  - input: terms.yaml\n    output: b.md\n    format: bullets\n");

        // Act: override the format and widths on the command line
        using var context = Context.Create(["--silent", "--config", config, "--format", "table", "--column-widths", "1,3"]);
        Program.Run(context);

        // Assert: both outputs are proportional tables
        Assert.Equal(0, context.ExitCode);
        Assert.Contains(Separator2575, File.ReadAllText(tmpDir.GetFilePath(Path.Combine("config", "a.md"))));
        Assert.Contains(Separator2575, File.ReadAllText(tmpDir.GetFilePath(Path.Combine("config", "b.md"))));
    }

    /// <summary>
    ///     Test that an invalid config file exits non-zero with an error naming the file.
    /// </summary>
    [Fact]
    public void CliSubsystem_ConfigFlow_InvalidConfig_ExitsNonZero()
    {
        // Arrange: a config with an unknown key
        using var tmpDir = new TemporaryDirectory();
        var config = WriteConfig(tmpDir, "theme: dark\n");

        // Act
        var (exitCode, error) = RunMain("--config", config);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains($"Config file '{config}': unknown key 'theme'", error);
    }

    /// <summary>
    ///     Test that --input with a config that defines tables exits non-zero.
    /// </summary>
    [Fact]
    public void CliSubsystem_ConfigFlow_TablesWithInput_ExitsNonZero()
    {
        // Arrange: a config with tables
        using var tmpDir = new TemporaryDirectory();
        var config = WriteConfig(tmpDir, "tables:\n  - input: a.yaml\n    output: a.md\n");

        // Act
        var (exitCode, error) = RunMain("--config", config, "--input", "b.yaml");

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains("--input cannot be used with config file", error);
    }

    /// <summary>
    ///     Test that --root is the base for command-line inputs and output.
    /// </summary>
    [Fact]
    public void CliSubsystem_RootFlow_CommandLineRoot_ResolvesInputsAndOutput()
    {
        // Arrange: a dictionary under a root directory
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("root", "dict", "terms.yaml")), "API: Application Programming Interface\n");
        var root = Path.Combine(tmpDir.DirectoryPath, "root");

        // Act: use relative paths that only exist under the root
        using var context = Context.Create(["--silent", "--root", root, "--input", "dict/*.yaml", "--output", "out/terms.md"]);
        Program.Run(context);

        // Assert
        Assert.Equal(0, context.ExitCode);
        Assert.Contains("API", File.ReadAllText(Path.Combine(root, "out", "terms.md")));
    }

    /// <summary>
    ///     Test that the config root key is the base for table jobs.
    /// </summary>
    [Fact]
    public void CliSubsystem_RootFlow_ConfigRoot_ResolvesTableJobs()
    {
        // Arrange: a config two levels below the root it names
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("dict", "terms.yaml")), "API: Application Programming Interface\n");
        var config = tmpDir.GetFilePath(Path.Combine("a", "b", "dictionarymark.yaml"));
        File.WriteAllText(config, "root: ../..\ntables:\n  - input: dict/*.yaml\n    output: out/terms.md\n");

        // Act
        using var context = Context.Create(["--silent", "--config", config]);
        Program.Run(context);

        // Assert
        Assert.Equal(0, context.ExitCode);
        Assert.Contains("API", File.ReadAllText(Path.Combine(tmpDir.DirectoryPath, "out", "terms.md")));
    }

    /// <summary>
    ///     Test that --root overrides the config root key for table jobs.
    /// </summary>
    [Fact]
    public void CliSubsystem_RootFlow_CommandLineRootOverridesConfigRoot_ResolvesTableJobs()
    {
        // Arrange: the config root names an empty directory; --root names the one with the dictionary
        using var tmpDir = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "empty"));
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("real", "terms.yaml")), "API: Application Programming Interface\n");
        var config = WriteConfig(tmpDir, "root: ../empty\ntables:\n  - input: terms.yaml\n    output: out.md\n");
        var root = Path.Combine(tmpDir.DirectoryPath, "real");

        // Act
        using var context = Context.Create(["--silent", "--config", config, "--root", root]);
        Program.Run(context);

        // Assert: the job resolved against --root
        Assert.Equal(0, context.ExitCode);
        Assert.Contains("API", File.ReadAllText(Path.Combine(root, "out.md")));
    }

    /// <summary>
    ///     Test that a missing root directory exits non-zero with an error naming it.
    /// </summary>
    [Fact]
    public void CliSubsystem_RootFlow_MissingRoot_ExitsNonZero()
    {
        // Arrange: a root directory that does not exist
        using var tmpDir = new TemporaryDirectory();
        var missing = Path.Combine(tmpDir.DirectoryPath, "missing");

        // Act
        var (exitCode, error) = RunMain("--root", missing, "--input", "a.yaml");

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains($"Directory '{missing}' specified by --root option", error);
    }

    /// <summary>
    ///     Test that --allow-root confines the config-file table jobs: a table inside the folder is generated and a
    ///     table writing outside it fails with file-access-denied and a non-zero exit code.
    /// </summary>
    [Fact]
    public void CliSubsystem_FileAccessFlow_AllowRoot_ConfinesConfigTableJobs()
    {
        // Arrange: two tables, one writing inside the config folder and one outside it
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "terms.yaml")), "API: Application Programming Interface\n");
        var config = WriteConfig(tmpDir,
            "tables:\n  - input: terms.yaml\n    output: inside.md\n  - input: terms.yaml\n    output: ../out/terms.md\n");
        var root = Path.GetDirectoryName(config)!;

        // Act
        var (exitCode, error) = RunMain("--config", config, "--allow-root", root);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.True(File.Exists(Path.Combine(root, "inside.md")));
        Assert.Contains("file-access-denied:", error);
        Assert.False(Directory.Exists(Path.Combine(tmpDir.DirectoryPath, "out")));
    }

    /// <summary>
    ///     Test that a mistake in the file-access options exits non-zero with its usage error before any job runs.
    /// </summary>
    [Fact]
    public void CliSubsystem_FileAccessFlow_InvalidOptions_ExitsNonZero()
    {
        // Arrange: a config whose table would write a file if it ran
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath(Path.Combine("config", "terms.yaml")), "API: Application Programming Interface\n");
        var config = WriteConfig(tmpDir, "tables:\n  - input: terms.yaml\n    output: out.md\n");

        // Act
        var (modeExit, modeError) = RunMain("--config", config, "--file-access", "everything");
        var (conflictExit, conflictError) = RunMain("--config", config, "--file-access", "any", "--allow-root", tmpDir.DirectoryPath);

        // Assert: both are usage errors and no job ran
        Assert.Equal(1, modeExit);
        Assert.Contains("'everything' is not a value of --file-access.", modeError);
        Assert.Equal(1, conflictExit);
        Assert.Contains("The options --file-access any and --allow-root cannot be used together.", conflictError);
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(config)!, "out.md")));
    }
}
