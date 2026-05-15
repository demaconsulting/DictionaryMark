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

using DemaConsulting.DictionaryMark.Tests.Helpers;
using DemaConsulting.DictionaryMark.Utilities;

namespace DemaConsulting.DictionaryMark.Tests;

/// <summary>
///     System-level integration tests that run the DictionaryMark application via dotnet.
/// </summary>
[Collection("Sequential")]
public class IntegrationTests
{
    private readonly string _dllPath;

    /// <summary>
    ///     Initialize test by locating the DictionaryMark DLL.
    /// </summary>
    public IntegrationTests()
    {
        // The DLL should be in the same directory as the test assembly
        // because the test project references the main project
        var baseDir = AppContext.BaseDirectory;
        _dllPath = PathHelpers.SafePathCombine(baseDir, "DemaConsulting.DictionaryMark.dll");

        Assert.True(File.Exists(_dllPath), $"Could not find DictionaryMark DLL at {_dllPath}");
    }

    /// <summary>
    ///     Test that version flag outputs version information.
    /// </summary>
    [Fact]
    public void DictionaryMark_VersionFlag_Provided_OutputsVersion()
    {
        // Arrange: (none - constructor initializes _dllPath)

        // Act: run the tool with version flag
        var exitCode = Runner.Run(
            out var output,
            "dotnet",
            _dllPath,
            "--version");

        // Assert: version string is printed; no banner or errors
        Assert.Equal(0, exitCode);
        Assert.Matches(@"\d+\.\d+\.\d+", output);
        Assert.DoesNotContain("Error", output);
        Assert.DoesNotContain("Copyright", output);
    }

    /// <summary>
    ///     Test that help flag outputs usage information.
    /// </summary>
    [Fact]
    public void DictionaryMark_HelpFlag_Provided_OutputsUsageInformation()
    {
        // Arrange: (none - constructor initializes _dllPath)

        // Act: run the tool with help flag
        var exitCode = Runner.Run(
            out var output,
            "dotnet",
            _dllPath,
            "--help");

        // Assert: usage text contains required sections and key flags
        Assert.Equal(0, exitCode);
        Assert.Contains("Usage:", output);
        Assert.Contains("Options:", output);
        Assert.Contains("--version", output);
        Assert.Contains("--help", output);
    }

    /// <summary>
    ///     Test that no arguments displays the tool banner and runs default logic.
    /// </summary>
    [Fact]
    public void DictionaryMark_NoArguments_Invoked_DisplaysBanner()
    {
        // Arrange: (none - constructor initializes _dllPath)

        // Act: run the tool with no arguments
        var exitCode = Runner.Run(
            out var output,
            "dotnet",
            _dllPath);

        // Assert: banner is displayed with tool name and copyright; exit code is success
        Assert.Equal(0, exitCode);
        Assert.Contains("DictionaryMark version", output);
        Assert.Contains("Copyright", output);
    }

    /// <summary>
    ///     Test that validate flag runs self-validation and outputs summary.
    /// </summary>
    [Fact]
    public void DictionaryMark_ValidateFlag_Provided_RunsValidation()
    {
        // Arrange: (none - constructor initializes _dllPath)

        // Act: run the tool with validate flag
        var exitCode = Runner.Run(
            out var output,
            "dotnet",
            _dllPath,
            "--validate");

        // Assert: validation summary is present; exit code is success
        Assert.Equal(0, exitCode);
        Assert.Contains("Total Tests:", output);
        Assert.Contains("Passed:", output);
    }

    /// <summary>
    ///     Test that validate with --results flag generates a TRX file.
    /// </summary>
    [Fact]
    public void DictionaryMark_ValidateWithTrxResults_Requested_GeneratesTrxFile()
    {
        // Arrange: temporary TRX results file path
        using var tmpDir = new TemporaryDirectory();
        var resultsFile = tmpDir.GetFilePath("results.trx");

        // Act: run validation with TRX results output
        var exitCode = Runner.Run(
            out var _,
            "dotnet",
            _dllPath,
            "--validate",
            "--results",
            resultsFile);

        // Assert: results file is created with valid TRX structure
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(resultsFile), "Results file was not created");

        var trxContent = File.ReadAllText(resultsFile);
        Assert.Contains("<TestRun", trxContent);
        Assert.Contains("</TestRun>", trxContent);
    }

    /// <summary>
    ///     Test that validate with --result (legacy alias) flag generates a results file.
    /// </summary>
    [Fact]
    public void DictionaryMark_ResultAlias_LegacyFlag_WritesResultsFile()
    {
        // Arrange: temporary TRX results file path; use legacy --result alias
        using var tmpDir = new TemporaryDirectory();
        var resultsFile = tmpDir.GetFilePath("results.trx");

        // Act: run validation with legacy --result alias
        var exitCode = Runner.Run(
            out var _,
            "dotnet",
            _dllPath,
            "--validate",
            "--result",
            resultsFile);

        // Assert: results file is created; exit code is success
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(resultsFile), "Results file was not created for legacy --result alias");
    }

    /// <summary>
    ///     Test that validate with an unsupported results extension returns a non-zero exit code.
    /// </summary>
    [Fact]
    public void DictionaryMark_ValidateWithBadExtension_ExtensionInvalid_ReturnsNonZero()
    {
        // Arrange: unsupported results file extension triggers WriteError → ExitCode 1
        using var tmpDir = new TemporaryDirectory();
        var resultsFile = tmpDir.GetFilePath("results.bad");

        // Act: run validation with unsupported extension
        var exitCode = Runner.Run(
            out var _,
            "dotnet",
            _dllPath,
            "--validate",
            "--results",
            resultsFile);

        // Assert: non-zero exit code indicates the error path was triggered
        Assert.NotEqual(0, exitCode);
        Assert.False(File.Exists(resultsFile), "No file should be created for unsupported extension");
    }

    /// <summary>
    ///     Test that silent flag suppresses output.
    /// </summary>
    [Fact]
    public void DictionaryMark_SilentFlag_Provided_SuppressesOutput()
    {
        // Arrange: (none - constructor initializes _dllPath)

        // Act: run the tool with --version and --silent to produce deterministic silent output
        var exitCode = Runner.Run(
            out var output,
            "dotnet",
            _dllPath,
            "--version",
            "--silent");

        // Assert: no console output in silent mode
        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected no output in silent mode but got: {output}");
    }

    /// <summary>
    ///     Test that log flag writes output to a file.
    /// </summary>
    [Fact]
    public void DictionaryMark_LogFlag_Provided_WritesOutputToFile()
    {
        // Arrange: temporary log file path
        var logFile = Path.GetTempFileName();

        try
        {
            // Act: run the tool with log flag
            var exitCode = Runner.Run(
                out var output,
                "dotnet",
                _dllPath,
                "--log",
                logFile);

            // Assert: log file is created and contains tool output; console output matches
            Assert.Equal(0, exitCode);
            Assert.True(File.Exists(logFile), "Log file was not created");

            var logContent = File.ReadAllText(logFile);
            Assert.Contains("DictionaryMark version", logContent);
            Assert.Contains("DictionaryMark version", output);
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
    ///     Test that validate with --results flag generates a JUnit XML file.
    /// </summary>
    [Fact]
    public void DictionaryMark_ValidateWithXmlResults_Requested_GeneratesJUnitFile()
    {
        // Arrange: temporary XML results file path
        using var tmpDir = new TemporaryDirectory();
        var resultsFile = tmpDir.GetFilePath("results.xml");

        // Act: run validation with JUnit XML results output
        var exitCode = Runner.Run(
            out var _,
            "dotnet",
            _dllPath,
            "--validate",
            "--results",
            resultsFile);

        // Assert: results file is created with valid JUnit structure
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(resultsFile), "Results file was not created");

        var xmlContent = File.ReadAllText(resultsFile);
        Assert.Contains("<testsuites", xmlContent);
    }

    /// <summary>
    ///     Test that an unknown argument causes an error message and non-zero exit code.
    /// </summary>
    [Fact]
    public void DictionaryMark_UnknownArgument_Provided_ReturnsError()
    {
        // Arrange: (none - constructor initializes _dllPath)

        // Act: run the tool with an unknown argument
        var exitCode = Runner.Run(
            out var output,
            "dotnet",
            _dllPath,
            "--unknown");

        // Assert: non-zero exit code and error message naming the unrecognized flag
        Assert.NotEqual(0, exitCode);
        Assert.Contains("Error", output);
        Assert.Contains("--unknown", output);
    }

    /// <summary>
    ///     Test that validate with depth flag outputs headings at the specified depth.
    /// </summary>
    [Fact]
    public void DictionaryMark_ValidateWithDepth_DepthThree_OutputsCorrectHeadingLevel()
    {
        // Arrange: (none - constructor initializes _dllPath)

        // Act: run validation with heading depth 3
        var exitCode = Runner.Run(
            out var output,
            "dotnet",
            _dllPath,
            "--validate",
            "--depth",
            "3");

        // Assert: output contains level-3 markdown headings
        Assert.Equal(0, exitCode);
        Assert.Contains("###", output);
    }

    /// <summary>Test that DictionaryMark generates bullet list output from YAML files.</summary>
    [Fact]
    public void DictionaryMark_Generate_BulletsFormat_OutputsBulletList()
    {
        // Arrange: YAML input file with two entries
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(tmpFile, "API: Application Programming Interface\nUI: User Interface\n");

        // Act: run the tool with default bullets format
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--input", tmpFile);

        // Assert: bullet list structure and content present in output
        Assert.Equal(0, exitCode);
        Assert.Contains("API", output);
        Assert.Contains("UI", output);
        Assert.Contains("- ", output);
    }

    /// <summary>Test that DictionaryMark generates table output when table format is specified.</summary>
    [Fact]
    public void DictionaryMark_Generate_TableFormat_OutputsTable()
    {
        // Arrange: YAML input file with one entry
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(tmpFile, "API: Application Programming Interface\n");

        // Act: run the tool with table format
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--input", tmpFile, "--format", "table");

        // Assert: table structure with header and data rows present
        Assert.Equal(0, exitCode);
        Assert.Contains("| Term |", output);
        Assert.Contains("| API |", output);
    }

    /// <summary>Test that DictionaryMark writes output to a file when --output is specified.</summary>
    [Fact]
    public void DictionaryMark_Generate_WithOutputFile_WritesFile()
    {
        // Arrange: input YAML file and target output file path
        using var tmpDir = new TemporaryDirectory();
        var tmpInput = tmpDir.GetFilePath("input.yaml");
        var tmpOutput = tmpDir.GetFilePath("output.md");
        File.WriteAllText(tmpInput, "API: Application Programming Interface\n");

        // Act: run the tool with --output flag
        var exitCode = Runner.Run(out var _, "dotnet", _dllPath, "--input", tmpInput, "--output", tmpOutput);

        // Assert: output file is created and contains generated content
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(tmpOutput));
        var content = File.ReadAllText(tmpOutput);
        Assert.Contains("API", content);
    }

    /// <summary>Test that DictionaryMark reports error for conflicting definitions.</summary>
    [Fact]
    public void DictionaryMark_Generate_ConflictingEntries_ReportsError()
    {
        // Arrange: two input files with conflicting definitions for the same term
        using var tmpDir = new TemporaryDirectory();
        var tmpFile1 = tmpDir.GetFilePath("input1.yaml");
        var tmpFile2 = tmpDir.GetFilePath("input2.yaml");
        File.WriteAllText(tmpFile1, "API: Application Programming Interface\n");
        File.WriteAllText(tmpFile2, "API: Advanced Peripheral Interface\n");

        // Act: run the tool with both conflicting input files
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--input", tmpFile1, "--input", tmpFile2);

        // Assert: non-zero exit code and conflict error message in output
        Assert.NotEqual(0, exitCode);
        Assert.Contains("Conflict", output);
    }

    /// <summary>Test that DictionaryMark outputs a section heading when --section is specified.</summary>
    [Fact]
    public void DictionaryMark_Generate_WithSectionHeading_OutputsHeading()
    {
        // Arrange: YAML input file and section heading argument
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(tmpFile, "API: Application Programming Interface\n");

        // Act: run the tool with --section flag
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--input", tmpFile, "--section", "My Section");

        // Assert: section heading appears in output
        Assert.Equal(0, exitCode);
        Assert.Contains("My Section", output);
    }

    /// <summary>Test that DictionaryMark uses custom term header when --term-header is specified.</summary>
    [Fact]
    public void DictionaryMark_Generate_WithTermHeader_OutputsCustomHeader()
    {
        // Arrange: YAML input file and term header argument
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(tmpFile, "API: Application Programming Interface\n");

        // Act: run the tool with --term-header and table format
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--input", tmpFile, "--format", "table", "--term-header", "Word");

        // Assert: custom header appears in table output
        Assert.Equal(0, exitCode);
        Assert.Contains("Word", output);
    }

    /// <summary>Test that DictionaryMark uses custom definition header when --def-header is specified.</summary>
    [Fact]
    public void DictionaryMark_Generate_WithDefHeader_OutputsCustomHeader()
    {
        // Arrange: YAML input file and definition header argument
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(tmpFile, "API: Application Programming Interface\n");

        // Act: run the tool with --def-header and table format
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--input", tmpFile, "--format", "table", "--def-header", "Meaning");

        // Assert: custom definition header appears in table output
        Assert.Equal(0, exitCode);
        Assert.Contains("Meaning", output);
    }

    /// <summary>Test that DictionaryMark outputs entries in alphabetical order when --sort alpha is specified.</summary>
    [Fact]
    public void DictionaryMark_Generate_WithSortAlpha_OutputsAlphabeticOrder()
    {
        // Arrange: YAML input file with entries in reverse alphabetical order
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(tmpFile, "Zebra: Last in alphabet\nAlpha: First in alphabet\n");

        // Act: run the tool with --sort alpha
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--input", tmpFile, "--sort", "alpha");

        // Assert: alphabetical ordering - Alpha before Zebra
        Assert.Equal(0, exitCode);
        var alphaIndex = output.IndexOf("Alpha", StringComparison.Ordinal);
        var zebraIndex = output.IndexOf("Zebra", StringComparison.Ordinal);
        Assert.True(alphaIndex < zebraIndex, "Alpha should appear before Zebra in alphabetical sort");
    }
}
