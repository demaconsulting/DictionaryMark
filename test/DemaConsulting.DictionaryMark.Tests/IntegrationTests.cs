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
using DemaConsulting.DictionaryMark.Tests.Helpers;
using DemaConsulting.DictionaryMark.Utilities;
using DemaConsulting.HelpCards.Catalog;
using DemaConsulting.HelpCards.Testing;

namespace DemaConsulting.DictionaryMark.Tests;

/// <summary>
///     System-level integration tests that run the DictionaryMark application via dotnet.
/// </summary>
[Collection("Sequential")]
public class IntegrationTests
{
    /// <summary>
    ///     Full path to the DictionaryMark DLL under test, resolved once in the constructor
    ///     and reused by every test method.
    /// </summary>
    private readonly string _dllPath;

    /// <summary>
    ///     Full path to the test data folder in the test output, which holds the input files that the help
    ///     examples read.
    /// </summary>
    private readonly string _testDataPath;

    /// <summary>
    ///     Initialize test by locating the DictionaryMark DLL and the test data.
    /// </summary>
    public IntegrationTests()
    {
        // The DLL should be in the same directory as the test assembly
        // because the test project references the main project
        var baseDir = AppContext.BaseDirectory;
        _dllPath = PathHelpers.SafePathCombine(baseDir, "DemaConsulting.DictionaryMark.dll");
        _testDataPath = PathHelpers.SafePathCombine(baseDir, "TestData");

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
        using var tmpDir = new TemporaryDirectory();
        var logFile = tmpDir.GetFilePath("output.log");

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

    /// <summary>Test that DictionaryMark accepts wildcard input patterns and expands matching YAML files.</summary>
    [Fact]
    public void DictionaryMark_Generate_GlobInput_ProcessesMatches()
    {
        // Arrange: create two YAML files and one non-YAML file in a temporary directory
        using var tmpDir = new TemporaryDirectory();
        var firstYamlFile = tmpDir.GetFilePath("first.yaml");
        var secondYamlFile = tmpDir.GetFilePath("second.yaml");
        var ignoredFile = tmpDir.GetFilePath("ignored.txt");
        File.WriteAllText(firstYamlFile, "API: Application Programming Interface\n");
        File.WriteAllText(secondYamlFile, "UI: User Interface\n");
        File.WriteAllText(ignoredFile, "This file should not be loaded\n");
        var wildcardPattern = tmpDir.GetFilePath("*.yaml");

        // Act: run the tool with a wildcard input pattern
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--input", wildcardPattern);

        // Assert: entries from matching YAML files are present
        Assert.Equal(0, exitCode);
        Assert.Contains("API", output);
        Assert.Contains("UI", output);
    }

    /// <summary>Test that DictionaryMark merges entries from multiple non-conflicting input files.</summary>
    [Fact]
    public void DictionaryMark_Generate_MultipleInputs_MergesEntries()
    {
        // Arrange: create two input files with non-conflicting entries
        using var tmpDir = new TemporaryDirectory();
        var firstInputFile = tmpDir.GetFilePath("input1.yaml");
        var secondInputFile = tmpDir.GetFilePath("input2.yaml");
        File.WriteAllText(firstInputFile, "API: Application Programming Interface\n");
        File.WriteAllText(secondInputFile, "UI: User Interface\n");

        // Act: run the tool with both input files
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--input", firstInputFile, "--input", secondInputFile);

        // Assert: output contains merged entries from both files
        Assert.Equal(0, exitCode);
        Assert.Contains("API", output);
        Assert.Contains("UI", output);
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

    /// <summary>Test that DictionaryMark generates a deprecated-terms table when --kind deprecated is specified.</summary>
    [Fact]
    public void DictionaryMark_Generate_DeprecatedKind_OutputsDeprecatedTable()
    {
        // Arrange: YAML input file with one deprecated entry
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("deprecated.yaml");
        File.WriteAllText(tmpFile, "Zorbix unit: { use: Zorbix, where: Product A }\n");

        // Act: run the tool with deprecated kind in table format
        var exitCode = Runner.Run(
            out var output, "dotnet", _dllPath, "--input", tmpFile, "--kind", "deprecated", "--format", "table");

        // Assert: deprecated table headers and the entry are present
        Assert.Equal(0, exitCode);
        Assert.Contains("Use instead", output);
        Assert.Contains("Where it was used", output);
        Assert.Contains("Zorbix unit", output);
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

    /// <summary>Test that DictionaryMark returns a non-zero exit code when conflicting entries are detected.</summary>
    [Fact]
    public void DictionaryMark_Generate_ConflictingEntries_ExitCodeIsNonZero()
    {
        // Arrange: two input files with conflicting definitions for the same term
        using var tmpDir = new TemporaryDirectory();
        var tmpFile1 = tmpDir.GetFilePath("conflict1.yaml");
        var tmpFile2 = tmpDir.GetFilePath("conflict2.yaml");
        File.WriteAllText(tmpFile1, "SDK: Software Development Kit\n");
        File.WriteAllText(tmpFile2, "SDK: System Development Kit\n");

        // Act: run the tool with both conflicting input files
        var exitCode = Runner.Run(out var _, "dotnet", _dllPath, "--input", tmpFile1, "--input", tmpFile2);

        // Assert: exit code is explicitly non-zero to confirm conflict detection triggers failure
        Assert.NotEqual(0, exitCode);
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

        // Assert: section heading is emitted as a Markdown heading before generated entries
        Assert.Equal(0, exitCode);
        var headingIndex = output.IndexOf("# My Section", StringComparison.Ordinal);
        var entryIndex = output.IndexOf("\n- ", StringComparison.Ordinal);
        Assert.True(headingIndex >= 0, "Expected '# My Section' heading in output");
        Assert.True(entryIndex > headingIndex, "Expected section heading to appear before generated entries");
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

    /// <summary>Test that DictionaryMark preserves file-encounter order by default when sort order is not specified.</summary>
    [Fact]
    public void DictionaryMark_Generate_DefaultSort_PreservesFileOrder()
    {
        // Arrange: YAML input file with entries intentionally ordered Zebra then Alpha
        using var tmpDir = new TemporaryDirectory();
        var inputFile = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(inputFile, "Zebra: Last in alphabet\nAlpha: First in alphabet\n");

        // Act: run the tool without --sort so file-order behavior is used
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--input", inputFile);

        // Assert: default output preserves file encounter order - Zebra before Alpha
        Assert.Equal(0, exitCode);
        var zebraIndex = output.IndexOf("Zebra", StringComparison.Ordinal);
        var alphaIndex = output.IndexOf("Alpha", StringComparison.Ordinal);
        Assert.True(zebraIndex < alphaIndex, "Zebra should appear before Alpha in default file-order sort");
    }

    /// <summary>Test that DictionaryMark applies heading depth to generated section headings.</summary>
    [Fact]
    public void DictionaryMark_Generate_SectionWithDepth_OutputsHeading()
    {
        // Arrange: YAML input file and section heading with explicit heading depth
        using var tmpDir = new TemporaryDirectory();
        var inputFile = tmpDir.GetFilePath("input.yaml");
        File.WriteAllText(inputFile, "API: Application Programming Interface\n");

        // Act: run the tool with section heading and depth 3
        var exitCode = Runner.Run(
            out var output,
            "dotnet",
            _dllPath,
            "--input",
            inputFile,
            "--section",
            "My Section",
            "--depth",
            "3");

        // Assert: output contains level-3 heading for the section before generated entries
        Assert.Equal(0, exitCode);
        var headingIndex = output.IndexOf("### My Section", StringComparison.Ordinal);
        var entryIndex = output.IndexOf("\n- ", StringComparison.Ordinal);
        Assert.True(headingIndex >= 0, "Expected '### My Section' heading in output");
        Assert.True(entryIndex > headingIndex, "Expected section heading to appear before generated entries");
    }

    /// <summary>
    ///     Test that an unrecognized argument causes an error message written to stderr.
    /// </summary>
    [Fact]
    public void DictionaryMark_UnrecognizedArgument_WritesErrorToStderr()
    {
        // Arrange: (none - constructor initializes _dllPath)

        // Act: run the tool with an unrecognized argument; capture stderr separately
        var exitCode = Runner.Run(out var _, out var stderr, "dotnet", _dllPath, "--invalid-flag");

        // Assert: non-zero exit code and error message present on stderr
        Assert.NotEqual(0, exitCode);
        Assert.False(string.IsNullOrWhiteSpace(stderr), "Expected an error message on stderr but got none");
    }

    /// <summary>
    ///     Test that an unrecognized argument produces no markdown output on stdout.
    /// </summary>
    [Fact]
    public void DictionaryMark_UnrecognizedArgument_WritesNoMarkdownOutput()
    {
        // Arrange: (none - constructor initializes _dllPath)

        // Act: run the tool with an unrecognized argument; capture stdout separately
        var exitCode = Runner.Run(out var stdout, out var _, "dotnet", _dllPath, "--invalid-flag");

        // Assert: exit is non-zero and stdout is empty - no markdown output should be emitted on the error path
        Assert.NotEqual(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(stdout),
            $"Expected no markdown output on stdout, but got: {stdout}");
    }

    /// <summary>Name of the environment variable used by the path tests.</summary>
    private const string VariableName = "DICTIONARYMARK_INTEGRATION_TEST";

    /// <summary>Builds the expected separator row for the given dash counts.</summary>
    /// <param name="dashes">The dash count of each column.</param>
    /// <returns>The separator row.</returns>
    private static string Separator(params int[] dashes) =>
        "|" + string.Concat(dashes.Select(d => $" :{new string('-', d)} |"));

    /// <summary>Writes a text file, creating its directory, and returns its full path.</summary>
    /// <param name="tmpDir">The temporary directory.</param>
    /// <param name="relativePath">The path relative to the temporary directory, using '/' separators.</param>
    /// <param name="content">The file content.</param>
    /// <returns>The full path of the file.</returns>
    private static string WriteFile(TemporaryDirectory tmpDir, string relativePath, string content)
    {
        var path = tmpDir.GetFilePath(relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Returns the full path of a file under the temporary directory without creating anything.</summary>
    /// <param name="tmpDir">The temporary directory.</param>
    /// <param name="relativePath">The path relative to the temporary directory, using '/' separators.</param>
    /// <returns>The full path.</returns>
    private static string PathIn(TemporaryDirectory tmpDir, string relativePath) =>
        Path.Combine(tmpDir.DirectoryPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Test that --column-widths sizes the table separator in proportion to the weights.</summary>
    [Fact]
    public void DictionaryMark_Generate_ColumnWidths_OutputsProportionalSeparator()
    {
        // Arrange: a dictionary file
        using var tmpDir = new TemporaryDirectory();
        var input = WriteFile(tmpDir, "input.yaml", "API: Application Programming Interface\n");

        // Act: generate a table with weights 25,75
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--input", input, "--format", "table", "--column-widths", "25,75");

        // Assert: exactly 25 and 75 dashes follow the header row
        Assert.Equal(0, exitCode);
        Assert.Contains($"| Term | Definition |{Environment.NewLine}{Separator(25, 75)}", output);
    }

    /// <summary>Test that a deprecated table without Where drops the third weight and rescales the first two.</summary>
    [Fact]
    public void DictionaryMark_Generate_ColumnWidthsDeprecatedWithoutWhere_RescalesTwoColumns()
    {
        // Arrange: deprecated entries without a where value
        using var tmpDir = new TemporaryDirectory();
        var input = WriteFile(tmpDir, "deprecated.yaml", "E-stop: { use: emergency stop }\n");

        // Act: three weights for the deprecated table
        var exitCode = Runner.Run(
            out var output, "dotnet", _dllPath,
            "--input", input, "--kind", "deprecated", "--format", "table", "--column-widths", "20,50,30");

        // Assert: two columns rescaled from 20:50
        Assert.Equal(0, exitCode);
        Assert.Contains(Separator(29, 71) + Environment.NewLine, output);
        Assert.DoesNotContain("Where it was used", output);
    }

    /// <summary>Test that bullet output ignores --column-widths.</summary>
    [Fact]
    public void DictionaryMark_Generate_ColumnWidthsWithBullets_OutputUnchanged()
    {
        // Arrange: a dictionary file
        using var tmpDir = new TemporaryDirectory();
        var input = WriteFile(tmpDir, "input.yaml", "API: Application Programming Interface\n");

        // Act: generate bullets with and without (mismatched) widths
        var withExit = Runner.Run(out var withWidths, "dotnet", _dllPath, "--input", input, "--column-widths", "1,2,3");
        var withoutExit = Runner.Run(out var withoutWidths, "dotnet", _dllPath, "--input", input);

        // Assert: identical output
        Assert.Equal(0, withExit);
        Assert.Equal(0, withoutExit);
        Assert.Equal(withoutWidths, withWidths);
    }

    /// <summary>Test that invalid --column-widths values are rejected.</summary>
    [Fact]
    public void DictionaryMark_Generate_ColumnWidthsInvalidValue_ReturnsError()
    {
        // Act: zero, negative and non-integer weights
        var zero = Runner.Run(out var zeroOutput, "dotnet", _dllPath, "--column-widths", "0,100");
        var negative = Runner.Run(out _, "dotnet", _dllPath, "--column-widths", "-1,2");
        var fraction = Runner.Run(out _, "dotnet", _dllPath, "--column-widths", "1.5,2");

        // Assert: every value is rejected with the expected message
        Assert.Equal(1, zero);
        Assert.Equal(1, negative);
        Assert.Equal(1, fraction);
        Assert.Contains("--column-widths requires 2 or 3 comma-separated positive integers", zeroOutput);
    }

    /// <summary>Test that a weight count not matching the table columns is rejected.</summary>
    [Fact]
    public void DictionaryMark_Generate_ColumnWidthsWrongCount_ReturnsError()
    {
        // Arrange: a deprecated dictionary
        using var tmpDir = new TemporaryDirectory();
        var input = WriteFile(tmpDir, "deprecated.yaml", "E-stop: { use: emergency stop }\n");

        // Act: two weights for a deprecated table
        var exitCode = Runner.Run(
            out var output, "dotnet", _dllPath,
            "--input", input, "--kind", "deprecated", "--format", "table", "--column-widths", "1,2");

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains("deprecated tables require exactly 3 values", output);
    }

    /// <summary>Test that config file defaults apply to a command-line run.</summary>
    [Fact]
    public void DictionaryMark_Config_FormattingDefaults_AppliedToCommandLineRun()
    {
        // Arrange: a config with formatting defaults only
        using var tmpDir = new TemporaryDirectory();
        var input = WriteFile(tmpDir, "input.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "dictionarymark.yaml", "format: table\nsection: Glossary\ncolumn-widths: [25, 75]\n");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config, "--input", input);

        // Assert: section, table and widths come from the config
        Assert.Equal(0, exitCode);
        Assert.Contains("# Glossary", output);
        Assert.Contains(Separator(25, 75), output);
    }

    /// <summary>Test that command-line options override job and default values from the config file.</summary>
    [Fact]
    public void DictionaryMark_Config_CommandLineOption_OverridesFileValues()
    {
        // Arrange: a config whose default and job both set a different term header
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "input.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "dictionarymark.yaml",
            "format: table\nterm-header: FromDefaults\ntables:\n  - input: input.yaml\n    output: a.md\n" +
            "  - input: input.yaml\n    output: b.md\n    term-header: FromJob\n");

        // Act
        var exitCode = Runner.Run(out _, "dotnet", _dllPath, "--config", config, "--term-header", "FromCli");

        // Assert: the command line wins for every job
        Assert.Equal(0, exitCode);
        Assert.Contains("| FromCli |", File.ReadAllText(PathIn(tmpDir, "a.md")));
        Assert.Contains("| FromCli |", File.ReadAllText(PathIn(tmpDir, "b.md")));
    }

    /// <summary>Test that each config table job writes its own output.</summary>
    [Fact]
    public void DictionaryMark_Config_MultipleTableJobs_WritesEachOutput()
    {
        // Arrange: approved and deprecated dictionaries with one job each
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "terms.yaml", "API: Application Programming Interface\n");
        WriteFile(tmpDir, "deprecated.yaml", "E-stop: { use: emergency stop, where: Product A }\n");
        var config = WriteFile(tmpDir, "dictionarymark.yaml",
            "format: table\ntables:\n  - input: terms.yaml\n    output: terms.md\n    column-widths: [25, 75]\n" +
            "  - input: deprecated.yaml\n    output: deprecated.md\n    kind: deprecated\n    column-widths: [20, 50, 30]\n");

        // Act
        var exitCode = Runner.Run(out _, "dotnet", _dllPath, "--silent", "--config", config);

        // Assert: both outputs exist with their own widths
        Assert.Equal(0, exitCode);
        Assert.Contains(Separator(25, 75), File.ReadAllText(PathIn(tmpDir, "terms.md")));
        Assert.Contains(Separator(20, 50, 30), File.ReadAllText(PathIn(tmpDir, "deprecated.md")));
    }

    /// <summary>Test that config paths, including '..' segments, resolve from the config file's directory.</summary>
    [Fact]
    public void DictionaryMark_Config_RelativePathsWithParentSegments_ResolvedFromConfigDirectory()
    {
        // Arrange: shared dictionaries beside the config directory; output into a missing sibling directory
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "shared/a/terms.yaml", "API: Application Programming Interface\n");
        WriteFile(tmpDir, "shared/b.yaml", "UI: User Interface\n");
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml",
            "tables:\n  - input: [../shared/**/*.yaml, sub/../../shared/b.yaml]\n    output: ../docs/generated/terms.md\n");

        // Act: run from a working directory unrelated to the config file
        var exitCode = Runner.Run(out _, "dotnet", _dllPath, "--silent", "--config", config);

        // Assert
        Assert.Equal(0, exitCode);
        var content = File.ReadAllText(PathIn(tmpDir, "docs/generated/terms.md"));
        Assert.Contains("API", content);
        Assert.Contains("UI", content);
    }

    /// <summary>Test that ${VAR} references are expanded in the config path and in config file paths.</summary>
    [Fact]
    public void DictionaryMark_Config_EnvironmentVariablesInPaths_AreExpanded()
    {
        // Arrange: a variable naming the temporary directory
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "shared/terms.yaml", "API: Application Programming Interface\n");
        WriteFile(tmpDir, "config/dictionarymark.yaml",
            "tables:\n  - input: ${" + VariableName + "}/shared/*.yaml\n    output: ${" + VariableName + "}/out/terms.md\n");
        Environment.SetEnvironmentVariable(VariableName, tmpDir.DirectoryPath);
        try
        {
            // Act: the config path itself also uses the variable
            var exitCode = Runner.Run(out _, "dotnet", _dllPath, "--silent", "--config", "${" + VariableName + "}/config/dictionarymark.yaml");

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Contains("API", File.ReadAllText(PathIn(tmpDir, "out/terms.md")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>Test that a failing job does not stop the others and the run exits non-zero.</summary>
    [Fact]
    public void DictionaryMark_Config_OneJobFails_OtherJobsWrittenAndExitNonZero()
    {
        // Arrange: the second of three jobs matches nothing
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "dictionarymark.yaml",
            "tables:\n  - input: terms.yaml\n    output: a.md\n  - input: missing/*.yaml\n    output: b.md\n" +
            "  - input: terms.yaml\n    output: c.md\n");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config);

        // Assert: jobs 1 and 3 are written; job 2 is reported by index and output
        Assert.Equal(1, exitCode);
        Assert.True(File.Exists(PathIn(tmpDir, "a.md")));
        Assert.True(File.Exists(PathIn(tmpDir, "c.md")));
        Assert.False(File.Exists(PathIn(tmpDir, "b.md")));
        Assert.Contains("Error: table job 2 ('b.md'):", output);
        Assert.Contains("Error: 1 of 3 table jobs failed.", output);
    }

    /// <summary>Test that a missing or malformed config file is reported with its name.</summary>
    [Fact]
    public void DictionaryMark_Config_InvalidFile_ReturnsErrorNamingFile()
    {
        // Arrange: a missing file and a malformed file
        using var tmpDir = new TemporaryDirectory();
        var missing = PathIn(tmpDir, "missing.yaml");
        var malformed = WriteFile(tmpDir, "bad.yaml", "format: [table\n");

        // Act
        var missingExit = Runner.Run(out var missingOutput, "dotnet", _dllPath, "--config", missing);
        var malformedExit = Runner.Run(out var malformedOutput, "dotnet", _dllPath, "--config", malformed);

        // Assert
        Assert.Equal(1, missingExit);
        Assert.Contains($"Config file '{missing}': file not found", missingOutput);
        Assert.Equal(1, malformedExit);
        Assert.Contains($"Config file '{malformed}': invalid YAML", malformedOutput);
    }

    /// <summary>Test that unknown and disallowed keys are reported with the key, job and file.</summary>
    [Fact]
    public void DictionaryMark_Config_UnknownKey_ReturnsErrorNamingKey()
    {
        // Arrange: an unknown job key and a disallowed top-level key
        using var tmpDir = new TemporaryDirectory();
        var jobKey = WriteFile(tmpDir, "job.yaml", "tables:\n  - input: a.yaml\n    output: a.md\n    theme: dark\n");
        var topKey = WriteFile(tmpDir, "top.yaml", "log: run.log\n");

        // Act
        var jobExit = Runner.Run(out var jobOutput, "dotnet", _dllPath, "--config", jobKey);
        var topExit = Runner.Run(out var topOutput, "dotnet", _dllPath, "--config", topKey);

        // Assert
        Assert.Equal(1, jobExit);
        Assert.Contains($"Config file '{jobKey}': table job 1: unknown key 'theme'", jobOutput);
        Assert.Equal(1, topExit);
        Assert.Contains($"Config file '{topKey}': key 'log'", topOutput);
    }

    /// <summary>Test that --input, --output and a repeated --config are rejected with a tables config.</summary>
    [Fact]
    public void DictionaryMark_Config_TablesWithCommandLineInput_ReturnsError()
    {
        // Arrange: a config with tables
        using var tmpDir = new TemporaryDirectory();
        var config = WriteFile(tmpDir, "dictionarymark.yaml", "tables:\n  - input: a.yaml\n    output: a.md\n");

        // Act
        var inputExit = Runner.Run(out var inputOutput, "dotnet", _dllPath, "--config", config, "--input", "b.yaml");
        var outputExit = Runner.Run(out var outputOutput, "dotnet", _dllPath, "--config", config, "--output", "b.md");
        var twiceExit = Runner.Run(out var twiceOutput, "dotnet", _dllPath, "--config", config, "--config", config);

        // Assert
        Assert.Equal(1, inputExit);
        Assert.Contains("--input cannot be used with config file", inputOutput);
        Assert.Equal(1, outputExit);
        Assert.Contains("--output cannot be used with config file", outputOutput);
        Assert.Equal(1, twiceExit);
        Assert.Contains("--config can only be specified once", twiceOutput);
    }

    /// <summary>Test that relative command-line globs with '..' segments are processed.</summary>
    [Fact]
    public void DictionaryMark_Generate_ParentDirectoryGlob_ProcessesMatches()
    {
        // Arrange: a working directory beside a shared dictionary directory
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "shared/terms.yaml", "API: Application Programming Interface\n");
        var work = Directory.CreateDirectory(PathIn(tmpDir, "work")).FullName;

        // Act: leading and mid-pattern '..' segments
        var leadingExit = Runner.RunInDirectory(out var leading, work, "dotnet", _dllPath, "--input", "../shared/*.yaml");
        var midExit = Runner.RunInDirectory(out var mid, work, "dotnet", _dllPath, "--input", "sub/../../shared/*.yaml");

        // Assert
        Assert.Equal(0, leadingExit);
        Assert.Contains("API", leading);
        Assert.Equal(0, midExit);
        Assert.Contains("API", mid);
    }

    /// <summary>Test that missing output directories are created.</summary>
    [Fact]
    public void DictionaryMark_Generate_OutputInMissingDirectory_CreatesDirectory()
    {
        // Arrange: an output path in directories that do not exist
        using var tmpDir = new TemporaryDirectory();
        var input = WriteFile(tmpDir, "input.yaml", "API: Application Programming Interface\n");
        var output = PathIn(tmpDir, "new/nested/out.md");

        // Act
        var exitCode = Runner.Run(out _, "dotnet", _dllPath, "--input", input, "--output", output);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("API", File.ReadAllText(output));
    }

    /// <summary>Test that relative command-line inputs and output resolve against --root.</summary>
    [Fact]
    public void DictionaryMark_Root_CommandLineInputsAndOutput_ResolvedAgainstRoot()
    {
        // Arrange: a dictionary under a root directory
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "root/dict/terms.yaml", "API: Application Programming Interface\n");
        var root = PathIn(tmpDir, "root");

        // Act
        var exitCode = Runner.Run(out _, "dotnet", _dllPath, "--root", root, "--input", "dict/*.yaml", "--output", "out/terms.md");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("API", File.ReadAllText(PathIn(tmpDir, "root/out/terms.md")));
    }

    /// <summary>Test that a relative --root with '..' resolves against the working directory.</summary>
    [Fact]
    public void DictionaryMark_Root_RelativeRootWithParentSegments_ResolvedAgainstWorkingDirectory()
    {
        // Arrange: a working directory and a sibling root
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "root/terms.yaml", "API: Application Programming Interface\n");
        var work = Directory.CreateDirectory(PathIn(tmpDir, "work")).FullName;

        // Act
        var exitCode = Runner.RunInDirectory(out var output, work, "dotnet", _dllPath, "--root", "../root", "--input", "*.yaml");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("API", output);
    }

    /// <summary>Test that config table paths resolve against --root.</summary>
    [Fact]
    public void DictionaryMark_Root_ConfigTables_ResolvedAgainstRoot()
    {
        // Arrange: a config in one directory and a dictionary under a separate root
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "root/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml", "tables:\n  - input: terms.yaml\n    output: out.md\n");

        // Act
        var exitCode = Runner.Run(out _, "dotnet", _dllPath, "--silent", "--config", config, "--root", PathIn(tmpDir, "root"));

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("API", File.ReadAllText(PathIn(tmpDir, "root/out.md")));
    }

    /// <summary>Test that ${VAR} references in --root are expanded.</summary>
    [Fact]
    public void DictionaryMark_Root_EnvironmentVariable_Expanded()
    {
        // Arrange: a variable naming the root directory
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "terms.yaml", "API: Application Programming Interface\n");
        Environment.SetEnvironmentVariable(VariableName, tmpDir.DirectoryPath);
        try
        {
            // Act
            var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--root", "${" + VariableName + "}", "--input", "terms.yaml");

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Contains("API", output);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>Test that absolute inputs and outputs ignore --root.</summary>
    [Fact]
    public void DictionaryMark_Root_AbsoluteInputAndOutput_IgnoreRoot()
    {
        // Arrange: an absolute input and output outside the root
        using var tmpDir = new TemporaryDirectory();
        var input = WriteFile(tmpDir, "terms.yaml", "API: Application Programming Interface\n");
        var output = PathIn(tmpDir, "out.md");
        var root = Directory.CreateDirectory(PathIn(tmpDir, "root")).FullName;

        // Act
        var exitCode = Runner.Run(out _, "dotnet", _dllPath, "--root", root, "--input", input, "--output", output);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("API", File.ReadAllText(output));
        Assert.Empty(Directory.GetFileSystemEntries(root));
    }

    /// <summary>Test that the config root key is the base for table jobs.</summary>
    [Fact]
    public void DictionaryMark_Config_Root_TablesResolvedAgainstRoot()
    {
        // Arrange: a config two levels below the repository root it names
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "dict/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "tools/config/dictionarymark.yaml",
            "root: ../..\ntables:\n  - input: dict/*.yaml\n    output: docs/terms.md\n");

        // Act
        var exitCode = Runner.Run(out _, "dotnet", _dllPath, "--silent", "--config", config);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("API", File.ReadAllText(PathIn(tmpDir, "docs/terms.md")));
    }

    /// <summary>Test that the config root key is the base for command-line inputs when the file has no tables.</summary>
    [Fact]
    public void DictionaryMark_Config_Root_CommandLineInputsResolvedAgainstRoot()
    {
        // Arrange: a config without tables naming a root directory
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "dict/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml", "root: ../dict\n");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config, "--input", "terms.yaml");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("API", output);
    }

    /// <summary>Test that --root overrides the config root key.</summary>
    [Fact]
    public void DictionaryMark_Root_CommandLineRoot_OverridesConfigRoot()
    {
        // Arrange: the config root names an empty directory; --root names the one with the dictionary
        using var tmpDir = new TemporaryDirectory();
        Directory.CreateDirectory(PathIn(tmpDir, "empty"));
        WriteFile(tmpDir, "real/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml", "root: ../empty\ntables:\n  - input: terms.yaml\n    output: out.md\n");

        // Act
        var exitCode = Runner.Run(out _, "dotnet", _dllPath, "--silent", "--config", config, "--root", PathIn(tmpDir, "real"));

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(PathIn(tmpDir, "real/out.md")));
    }

    /// <summary>Test that, without a root, command-line paths resolve against the working directory.</summary>
    [Fact]
    public void DictionaryMark_Config_NoRoot_CommandLinePathsUseWorkingDirectory()
    {
        // Arrange: a config without tables or root in one directory; the dictionary in the working directory
        using var tmpDir = new TemporaryDirectory();
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml", "format: table\n");
        WriteFile(tmpDir, "work/terms.yaml", "API: Application Programming Interface\n");
        var work = PathIn(tmpDir, "work");

        // Act
        var exitCode = Runner.RunInDirectory(
            out _, work, "dotnet", _dllPath, "--config", config, "--input", "terms.yaml", "--output", "terms.md");

        // Assert: the output was written in the working directory
        Assert.Equal(0, exitCode);
        Assert.Contains("| API |", File.ReadAllText(PathIn(tmpDir, "work/terms.md")));
    }

    /// <summary>Test that a missing --root directory is an error.</summary>
    [Fact]
    public void DictionaryMark_Root_MissingDirectory_ReturnsError()
    {
        // Arrange: a root that does not exist
        using var tmpDir = new TemporaryDirectory();
        var missing = PathIn(tmpDir, "missing");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--root", missing, "--input", "a.yaml");

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains($"Directory '{missing}' specified by --root option", output);
    }

    /// <summary>Test that a repeated or empty --root is an error.</summary>
    [Fact]
    public void DictionaryMark_Root_SpecifiedTwice_ReturnsError()
    {
        // Act
        var twiceExit = Runner.Run(out var twice, "dotnet", _dllPath, "--root", ".", "--root", ".");
        var emptyExit = Runner.Run(out var empty, "dotnet", _dllPath, "--root", "");

        // Assert
        Assert.Equal(1, twiceExit);
        Assert.Contains("--root can only be specified once", twice);
        Assert.Equal(1, emptyExit);
        Assert.Contains("--root requires a non-empty directory path", empty);
    }

    /// <summary>
    ///     Test that a config root naming a folder that does not exist is not rejected when the file is loaded: the
    ///     root is never probed, and the table job that uses it fails through the file-access resolver because its
    ///     input is not found.
    /// </summary>
    [Fact]
    public void DictionaryMark_Config_NonexistentRoot_JobFailsThroughResolver()
    {
        // Arrange: a config whose root does not exist, with one table relative to it
        using var tmpDir = new TemporaryDirectory();
        var config = WriteFile(tmpDir, "dictionarymark.yaml", "root: missing\ntables:\n  - input: terms.yaml\n    output: out.md\n");
        var input = PathIn(tmpDir, "missing/terms.yaml");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config);

        // Assert: the job reports its input as not found; the load did not reject the root; nothing was created
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: table job 1 ('out.md'): Input file 'terms.yaml' was not found.", output);
        Assert.DoesNotContain(input, output);
        Assert.DoesNotContain("does not exist", output);
        Assert.False(Directory.Exists(PathIn(tmpDir, "missing")));
    }

    /// <summary>
    ///     Test that a config root naming a network share (with forward slashes, and on Windows also with backslashes)
    ///     is never probed: loading succeeds and the table job fails through the file-access resolver, which refuses the
    ///     network path on every platform, because the root is joined with the job's paths as text before normalization.
    /// </summary>
    [Fact]
    public void DictionaryMark_Config_NetworkRoot_JobFailsThroughResolverWithoutProbing()
    {
        // Arrange: one config per spelling of the share; the host is under the .invalid top-level domain
        using var tmpDir = new TemporaryDirectory();
        var roots = OperatingSystem.IsWindows()
            ? new[] { "\"//dictionarymark-test.invalid/share\"", "'\\\\dictionarymark-test.invalid\\share'" }
            : ["\"//dictionarymark-test.invalid/share\""];

        for (var index = 0; index < roots.Length; index++)
        {
            var config = WriteFile(
                tmpDir,
                $"config{index}/dictionarymark.yaml",
                $"root: {roots[index]}\ntables:\n  - input: terms.yaml\n    output: out.md\n");

            // Act
            var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config, "--file-access", "roots");

            // Assert: the job (not the load) failed, refused by the resolver before anything was looked up
            Assert.Equal(1, exitCode);
            Assert.Contains("Error: table job 1 ('out.md'): ", output);
            Assert.Contains("is not allowed: network paths are not permitted.", output);
            Assert.DoesNotContain("does not exist", output);
            Assert.DoesNotContain("was not found", output);
        }
    }

    /// <summary>Test that a root key inside a table job is an error naming the job.</summary>
    [Fact]
    public void DictionaryMark_Config_RootInTableJob_ReturnsErrorNamingJob()
    {
        // Arrange: a job with a root key
        using var tmpDir = new TemporaryDirectory();
        var config = WriteFile(tmpDir, "dictionarymark.yaml", "tables:\n  - input: a.yaml\n    output: a.md\n    root: ..\n");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains($"Config file '{config}': table job 1: key 'root' is only allowed at the top level", output);
    }

    /// <summary>
    ///     Test that --allow-root confines the inputs and outputs of config-file tables when run as a process: a table
    ///     inside the named folder is generated, a table whose input or output lies outside it is denied with
    ///     file-access-denied, and nothing is written outside.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_AllowRootAsProcess_ConfinesConfigJobs()
    {
        // Arrange: three tables - inside, an input outside, and an output outside
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "site/terms.yaml", "API: Application Programming Interface\n");
        WriteFile(tmpDir, "other/secret.yaml", "Secret: Hidden\n");
        var config = WriteFile(
            tmpDir,
            "site/dictionarymark.yaml",
            "tables:\n"
            + "  - input: terms.yaml\n    output: inside.md\n"
            + "  - input: ../other/secret.yaml\n    output: secret.md\n"
            + "  - input: terms.yaml\n    output: ../out/terms.md\n");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config, "--allow-root", PathIn(tmpDir, "site"));

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains("API", File.ReadAllText(PathIn(tmpDir, "site/inside.md")));
        Assert.Contains("Error: table job 2 ('secret.md'): file-access-denied:", output);
        Assert.Contains("Error: table job 3 ('../out/terms.md'): file-access-denied:", output);
        Assert.False(File.Exists(PathIn(tmpDir, "site/secret.md")));
        Assert.False(Directory.Exists(PathIn(tmpDir, "out")));
    }

    /// <summary>
    ///     Test that an --allow-root folder spelled in another letter case than the folder on disk still allows the
    ///     config-file tables inside it when run as a process. Skips where the temporary folder is observed to be
    ///     case-sensitive, because "site" then names no folder.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_AllowRootInOtherCase_AllowsConfigJobsInside()
    {
        // Arrange: the folder is stored as "Site" and named as "site" in --allow-root
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "Site/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "Site/dictionarymark.yaml", "tables:\n  - input: terms.yaml\n    output: inside.md\n");
        if (!Directory.Exists(PathIn(tmpDir, "site")))
        {
            Assert.Skip("The temporary folder is case-sensitive, so \"site\" does not name the \"Site\" folder.");
        }

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config, "--allow-root", PathIn(tmpDir, "site"));

        // Assert
        Assert.True(exitCode == 0, output);
        Assert.Contains("API", File.ReadAllText(PathIn(tmpDir, "Site/inside.md")));
        Assert.DoesNotContain("file-access-denied", output, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Test that the mistakes of the file-access options are usage errors with their documented messages, reported
    ///     before any input is read.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_MistakesAsProcess_AreUsageErrors()
    {
        // Arrange: a config that does not exist, so a mistake must be reported before the config is opened
        using var tmpDir = new TemporaryDirectory();
        var config = PathIn(tmpDir, "missing.yaml");
        var nowhere = PathIn(tmpDir, "nowhere");
        var cases = new (string[] Args, string Message)[]
        {
            (["--file-access", "everything"], "Error: 'everything' is not a value of --file-access."),
            (["--file-access", "any", "--allow-root", tmpDir.DirectoryPath], "Error: The options --file-access any and --allow-root cannot be used together."),
            (["--allow-root", nowhere], $"Error: The folder '{nowhere}' of --allow-root does not exist.")
        };

        foreach (var (args, message) in cases)
        {
            // Act
            var exitCode = Runner.Run(out var text, "dotnet", [_dllPath, "--config", config, .. args]);

            // Assert
            Assert.Equal(1, exitCode);
            Assert.Contains(message, text, StringComparison.Ordinal);
        }
    }

    /// <summary>
    ///     Test that --file-access no-symlinks denies a config-file table input reached through a directory link, while
    ///     the same input by its real path is read.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_NoSymlinksAsProcess_DeniesLinkedConfigInput()
    {
        // Arrange: config/alias is a link to config/real, which holds the dictionary
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "config/real/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(
            tmpDir,
            "config/dictionarymark.yaml",
            "tables:\n  - input: real/terms.yaml\n    output: real.md\n  - input: alias/terms.yaml\n    output: alias.md\n");
        Files.FileTestSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "config/alias"), PathIn(tmpDir, "config/real"));

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config, "--file-access", "no-symlinks");

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains("API", File.ReadAllText(PathIn(tmpDir, "config/real.md")));
        Assert.Contains("Error: table job 2 ('alias.md'): file-access-denied:", output);
        Assert.False(File.Exists(PathIn(tmpDir, "config/alias.md")));
    }

    /// <summary>
    ///     Test that under the default mode a config-file table output that breaks a never-list rule (a colon outside a
    ///     drive, and on Windows a network path) is refused before anything is written.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_DefaultMode_NeverListRefusesConfigPaths()
    {
        // Arrange: an output naming an alternate data stream, and on Windows one naming a network share
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "config/terms.yaml", "API: Application Programming Interface\n");
        var network = OperatingSystem.IsWindows() ? "  - input: terms.yaml\n    output: //server/share/out.md\n" : string.Empty;
        var config = WriteFile(
            tmpDir,
            "config/dictionarymark.yaml",
            "tables:\n  - input: terms.yaml\n    output: \"out.md:stream\"\n" + network);

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains("is not allowed: the path contains a colon.", output);
        Assert.False(File.Exists(PathIn(tmpDir, "config/out.md")));
        if (OperatingSystem.IsWindows())
        {
            Assert.Contains("is not allowed: network paths are not permitted.", output);
        }
    }

    /// <summary>
    ///     Test that a configuration input pattern naming a network share is refused before anything is listed under the
    ///     default mode, and the other table still runs.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_NetworkInputPattern_RefusedBeforeListing()
    {
        // Arrange: one table names a share; the other reads a local dictionary
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "config/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(
            tmpDir,
            "config/dictionarymark.yaml",
            "tables:\n  - input: \"//dictionarymark-test.invalid/share/*.yaml\"\n    output: share.md\n" +
            "  - input: terms.yaml\n    output: terms.md\n");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config);

        // Assert: refused by the network-path rule, not by an empty listing; the other table ran
        Assert.Equal(1, exitCode);
        Assert.Contains("Failed to resolve input pattern '//dictionarymark-test.invalid/share/*.yaml'", output);
        Assert.Contains("is not allowed: network paths are not permitted.", output);
        Assert.DoesNotContain("matched no files", output);
        Assert.False(File.Exists(PathIn(tmpDir, "config/share.md")));
        Assert.True(File.Exists(PathIn(tmpDir, "config/terms.md")));
    }

    /// <summary>
    ///     Test that inputs and outputs named on the command line are not judged by the file-access policy when no
    ///     config root key relocates them.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_CommandLinePaths_AreNotJudged()
    {
        // Arrange: the input, output and log lie outside the only allowed folder
        using var tmpDir = new TemporaryDirectory();
        Directory.CreateDirectory(PathIn(tmpDir, "root"));
        var input = WriteFile(tmpDir, "outside/terms.yaml", "API: Application Programming Interface\n");
        var output = PathIn(tmpDir, "outside/out/terms.md");
        var log = PathIn(tmpDir, "outside/run.log");

        // Act
        var exitCode = Runner.Run(
            out _, "dotnet", _dllPath, "--allow-root", PathIn(tmpDir, "root"), "--input", input, "--output", output, "--log", log);

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("API", File.ReadAllText(output));
        Assert.True(File.Exists(log));
    }

    /// <summary>Test that a denied table job fails only itself: the other jobs run and the exit code is 1.</summary>
    [Fact]
    public void DictionaryMark_FileAccess_DeniedJob_OtherJobsRunAndExitCodeIsOne()
    {
        // Arrange: the first job writes outside the allowed folder; the second writes inside it
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "config/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(
            tmpDir,
            "config/dictionarymark.yaml",
            "tables:\n  - input: terms.yaml\n    output: ../out/a.md\n  - input: terms.yaml\n    output: b.md\n");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config, "--allow-root", PathIn(tmpDir, "config"));

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: table job 1 ('../out/a.md'): file-access-denied:", output);
        Assert.Contains("Error: 1 of 2 table jobs failed.", output);
        Assert.Contains("API", File.ReadAllText(PathIn(tmpDir, "config/b.md")));
    }

    /// <summary>
    ///     Test that --file-access roots without --allow-root allows the working directory and the config file's
    ///     folder, and denies a config-file table input outside both.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_RootsWithoutAllowRoot_AllowsWorkingAndConfigFolders()
    {
        // Arrange: the run starts in "work"; the config lies in "config"; one table reads from each, one from "other"
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "work/work.yaml", "Work: In the working directory\n");
        WriteFile(tmpDir, "config/terms.yaml", "API: Application Programming Interface\n");
        WriteFile(tmpDir, "other/secret.yaml", "Secret: Hidden\n");
        var config = WriteFile(
            tmpDir,
            "config/dictionarymark.yaml",
            "tables:\n"
            + "  - input: [terms.yaml, ../work/work.yaml]\n    output: allowed.md\n"
            + "  - input: ../other/secret.yaml\n    output: denied.md\n");

        // Act
        var exitCode = Runner.RunInDirectory(
            out var output, PathIn(tmpDir, "work"), "dotnet", _dllPath, "--config", config, "--file-access", "roots");

        // Assert
        Assert.Equal(1, exitCode);
        var allowed = File.ReadAllText(PathIn(tmpDir, "config/allowed.md"));
        Assert.Contains("API", allowed);
        Assert.Contains("Work", allowed);
        Assert.Contains("Error: table job 2 ('denied.md'): file-access-denied:", output);
        Assert.False(File.Exists(PathIn(tmpDir, "config/denied.md")));
    }

    /// <summary>
    ///     Test that every match of a config-file table glob is judged: a glob with one match inside the allowed folder
    ///     and one outside it fails its table with file-access-denied instead of skipping the outside match.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_GlobMatchOutsideRoots_IsDeniedNotSkipped()
    {
        // Arrange: matches in the config folder (allowed) and in a sibling folder (denied)
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "config/terms-a.yaml", "API: Application Programming Interface\n");
        WriteFile(tmpDir, "other/terms-b.yaml", "Secret: Hidden\n");
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml", "tables:\n  - input: ../*/terms-*.yaml\n    output: out.md\n");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config, "--allow-root", PathIn(tmpDir, "config"));

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains("file-access-denied:", output);
        Assert.Contains("terms-b.yaml", output);
        Assert.False(File.Exists(PathIn(tmpDir, "config/out.md")));
    }

    /// <summary>
    ///     Test that a literal config-file table input outside the allowed folder is judged before it is looked up: a
    ///     file that does not exist there is reported as file-access-denied, not as not found, while a missing literal
    ///     input inside the folder is reported as not found.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_LiteralConfigInputs_JudgedBeforeLookup()
    {
        // Arrange: neither input exists; one lies outside the allowed folder and one inside it
        using var tmpDir = new TemporaryDirectory();
        var config = WriteFile(
            tmpDir,
            "config/dictionarymark.yaml",
            "tables:\n  - input: ../other/missing.yaml\n    output: outside.md\n  - input: missing.yaml\n    output: inside.md\n");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config, "--allow-root", PathIn(tmpDir, "config"));

        // Assert: the outside input is denied without revealing whether it exists; the inside one is not found
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: table job 1 ('outside.md'): file-access-denied:", output);
        Assert.DoesNotContain("Input file '../other/missing.yaml' was not found", output);
        Assert.Contains("Error: table job 2 ('inside.md'): Input file 'missing.yaml' was not found.", output);
        Assert.DoesNotContain(PathIn(tmpDir, "config/missing.yaml"), output);
        Assert.Contains("Error: 2 of 2 table jobs failed.", output);
    }

    /// <summary>
    ///     Test that, under a restrictive file-access mode, a ${VAR} reference in a config table output is refused as
    ///     a process: exit code 1, an error naming the file, job and key, no variable value in the output, and
    ///     nothing created.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_ConfigOutputEnvironmentVariableAsProcess_RefusedWithoutLeak()
    {
        // Arrange: a config whose output is named by a variable holding a secret-looking value
        using var tmpDir = new TemporaryDirectory();
        var secret = "DM_SECRET_" + Guid.NewGuid().ToString("N");
        WriteFile(tmpDir, "config/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml",
            "tables:\n  - input: terms.yaml\n    output: ${" + VariableName + "}.md\n");
        var log = PathIn(tmpDir, "run.log");
        Environment.SetEnvironmentVariable(VariableName, secret);
        try
        {
            // Act
            var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config, "--file-access", "roots", "--log", log);

            // Assert: refused naming file, job and key; the secret appears nowhere and no file was created for it
            Assert.Equal(1, exitCode);
            Assert.Contains($"Config file '{config}': table job 1: key 'output' must not contain environment variable references", output);
            Assert.DoesNotContain(secret, output);
            Assert.DoesNotContain(VariableName, output);
            Assert.Empty(Directory.GetFiles(tmpDir.DirectoryPath, secret + "*", SearchOption.AllDirectories));
            Assert.True(!File.Exists(log) || !File.ReadAllText(log).Contains(secret));
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>
    ///     Test that, under a restrictive file-access mode, a ${VAR} reference in a config table input is refused as a
    ///     process with the same message whether the variable is defined or not, so the refusal reveals neither the
    ///     value nor whether the variable is set.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_ConfigInputEnvironmentVariableAsProcess_RefusedWithoutLeak()
    {
        // Arrange: a config whose input references a variable
        using var tmpDir = new TemporaryDirectory();
        var secret = "DM_SECRET_" + Guid.NewGuid().ToString("N");
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml",
            "tables:\n  - input: ${" + VariableName + "}/*.yaml\n    output: out.md\n");

        // Act: run once with the variable defined, once without
        Environment.SetEnvironmentVariable(VariableName, secret);
        int definedExit;
        string defined;
        try
        {
            definedExit = Runner.Run(out defined, "dotnet", _dllPath, "--config", config, "--file-access", "no-symlinks");
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }

        var undefinedExit = Runner.Run(out var undefined, "dotnet", _dllPath, "--config", config, "--file-access", "no-symlinks");

        // Assert: the same refusal both times, naming file, job and key, with no value and no "not defined" oracle
        Assert.Equal(1, definedExit);
        Assert.Equal(1, undefinedExit);
        Assert.Contains($"Config file '{config}': table job 1: key 'input' must not contain environment variable references", defined);
        Assert.Equal(defined, undefined);
        Assert.DoesNotContain(secret, defined);
        Assert.DoesNotContain("is not defined", undefined);
    }

    /// <summary>
    ///     Test that --allow-root (which implies the roots mode) refuses a ${VAR} reference in the config root key, for
    ///     config tables and for a command-line --input and --output that the root key would relocate, as a process.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_ConfigRootEnvironmentVariableAsProcess_RefusedForTablesAndRelocatedCommandLine()
    {
        // Arrange: one config with tables and one without, both with a root that references a variable
        using var tmpDir = new TemporaryDirectory();
        var secret = "DM_SECRET_" + Guid.NewGuid().ToString("N");
        var withTables = WriteFile(tmpDir, "config/tables.yaml",
            "root: ${" + VariableName + "}\ntables:\n  - input: terms.yaml\n    output: out.md\n");
        var noTables = WriteFile(tmpDir, "config/plain.yaml", "root: ${" + VariableName + "}\n");
        var allowed = PathIn(tmpDir, "config");
        Environment.SetEnvironmentVariable(VariableName, secret);
        try
        {
            // Act
            var tablesExit = Runner.Run(out var tablesOutput, "dotnet", _dllPath, "--config", withTables, "--allow-root", allowed);
            var relocatedExit = Runner.Run(
                out var relocatedOutput, "dotnet", _dllPath, "--config", noTables, "--allow-root", allowed, "--input", "terms.yaml", "--output", "out.md");

            // Assert: both are refused naming the root key; nothing leaked or created
            Assert.Equal(1, tablesExit);
            Assert.Equal(1, relocatedExit);
            Assert.Contains($"Config file '{withTables}': key 'root' must not contain environment variable references", tablesOutput);
            Assert.Contains($"Config file '{noTables}': key 'root' must not contain environment variable references", relocatedOutput);
            Assert.DoesNotContain(secret, tablesOutput);
            Assert.DoesNotContain(secret, relocatedOutput);
            Assert.False(File.Exists(PathIn(tmpDir, "config/out.md")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>
    ///     Test that with no file-access option, and with --file-access any, ${VAR} references in a config file's
    ///     paths are expanded as a process exactly as before.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void DictionaryMark_FileAccess_ConfigEnvironmentVariablesAsProcess_AnyModeStillExpands(bool explicitAny)
    {
        // Arrange: a config whose input and output folders come from a variable
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "shared/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml",
            "tables:\n  - input: ${" + VariableName + "}/shared/*.yaml\n    output: ${" + VariableName + "}/out/terms.md\n");
        Environment.SetEnvironmentVariable(VariableName, tmpDir.DirectoryPath);
        try
        {
            // Act
            var exitCode = explicitAny
                ? Runner.Run(out _, "dotnet", _dllPath, "--silent", "--config", config, "--file-access", "any")
                : Runner.Run(out _, "dotnet", _dllPath, "--silent", "--config", config);

            // Assert: the variable was expanded for both paths
            Assert.Equal(0, exitCode);
            Assert.Contains("- **API**:", File.ReadAllText(PathIn(tmpDir, "out/terms.md")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>
    ///     Test that command-line paths still expand ${VAR} under the roots mode as a process: only the untrusted
    ///     config file content is refused.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_CommandLineEnvironmentVariablesAsProcess_StillExpandedUnderRoots()
    {
        // Arrange: a variable naming the folder used by --root and --output
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "terms.yaml", "API: Application Programming Interface\n");
        Environment.SetEnvironmentVariable(VariableName, tmpDir.DirectoryPath);
        try
        {
            // Act
            var exitCode = Runner.Run(
                out _, "dotnet", _dllPath, "--silent", "--file-access", "roots", "--root", "${" + VariableName + "}",
                "--input", "terms.yaml", "--output", "${" + VariableName + "}/out.md");

            // Assert: the command-line variables were expanded and the output written
            Assert.Equal(0, exitCode);
            Assert.Contains("- **API**:", File.ReadAllText(PathIn(tmpDir, "out.md")));
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>
    ///     Test that the messages for denied and missing config paths show the paths as written and reveal no host
    ///     directory (neither the temporary folder nor the working directory) on stderr or in the log file.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_DeniedAndMissingConfigPathsAsProcess_ShowNoHostPath()
    {
        // Arrange: tables whose input, glob match, missing input and output are denied or missing
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "other/more.yaml", "CLI: Command Line Interface\n");
        WriteFile(tmpDir, "config/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(
            tmpDir,
            "config/dictionarymark.yaml",
            "tables:\n  - input: ../other/more.yaml\n    output: a.md\n"
            + "  - input: ../other/*.yaml\n    output: b.md\n"
            + "  - input: missing.yaml\n    output: c.md\n"
            + "  - input: terms.yaml\n    output: ../out/d.md\n");
        var log = PathIn(tmpDir, "run.log");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config, "--allow-root", PathIn(tmpDir, "config"), "--log", log);

        // Assert: each message names the path as written
        Assert.Equal(1, exitCode);
        Assert.Contains("table job 1 ('a.md'): file-access-denied: The file '../other/more.yaml' was denied", output);
        Assert.Contains("table job 2 ('b.md'): file-access-denied: The file '../other/more.yaml' was denied", output);
        Assert.Contains("table job 3 ('c.md'): Input file 'missing.yaml' was not found.", output);
        Assert.Contains("table job 4 ('../out/d.md'): file-access-denied: The file '../out/d.md' was denied", output);

        // Assert: no host directory is revealed on the console or in the log
        var logText = File.ReadAllText(log);
        foreach (var text in new[] { output, logText })
        {
            Assert.DoesNotContain(tmpDir.DirectoryPath, text);
            Assert.DoesNotContain(Environment.CurrentDirectory, text);
        }
    }

    /// <summary>
    ///     Test that the config file's root key moves the base of its relative table paths but never widens the
    ///     folders the roots mode allows.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_ConfigRootKey_DoesNotWidenAllowedFolders()
    {
        // Arrange: the root key points at a folder outside the only allowed folder
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "other/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml", "root: ../other\ntables:\n  - input: terms.yaml\n    output: out.md\n");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config, "--allow-root", PathIn(tmpDir, "config"));

        // Assert: the table under the root key is denied
        Assert.Equal(1, exitCode);
        Assert.Contains("file-access-denied:", output);
        Assert.False(File.Exists(PathIn(tmpDir, "other/out.md")));
    }

    /// <summary>
    ///     Test that a relative command-line input that the config root key moves outside the allowed folder is denied,
    ///     so the root key cannot be used to read a file the file-access policy keeps out.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_ConfigRootRelocatedInput_IsDenied()
    {
        // Arrange: the config (no tables) has a root key naming a folder outside the only allowed folder
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "other/secret.yaml", "Secret: Hidden\n");
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml", "root: ../other\n");

        // Act
        var exitCode = Runner.Run(
            out var output, "dotnet", _dllPath, "--config", config, "--allow-root", PathIn(tmpDir, "config"), "--input", "secret.yaml");

        // Assert: denied, and the content was not read
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: file-access-denied:", output);
        Assert.DoesNotContain("Hidden", output);
    }

    /// <summary>
    ///     Test that a relative command-line output that the config root key moves outside the allowed folder is denied
    ///     and nothing is written.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_ConfigRootRelocatedOutput_IsDenied()
    {
        // Arrange: the input is absolute and trusted; the root key moves the output outside the allowed folder
        using var tmpDir = new TemporaryDirectory();
        var input = WriteFile(tmpDir, "config/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml", "root: ../out\n");

        // Act
        var exitCode = Runner.Run(
            out var output,
            "dotnet", _dllPath, "--config", config, "--allow-root", PathIn(tmpDir, "config"),
            "--input", input, "--output", "result.md");

        // Assert: denied before anything is written, not even the folder
        Assert.Equal(1, exitCode);
        Assert.Contains("Error: file-access-denied:", output);
        Assert.False(File.Exists(PathIn(tmpDir, "out/result.md")));
        Assert.False(Directory.Exists(PathIn(tmpDir, "out")));
    }

    /// <summary>
    ///     Test that relocated command-line paths inside the allowed folders are generated.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_ConfigRootRelocatedPathsInsideRoots_AreGenerated()
    {
        // Arrange: the root key names a folder inside the allowed folder
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "work/other/terms.yaml", "API: Application Programming Interface\n");
        var config = WriteFile(tmpDir, "work/config/dictionarymark.yaml", "root: ../other\n");

        // Act
        var exitCode = Runner.Run(
            out _,
            "dotnet", _dllPath, "--config", config, "--allow-root", PathIn(tmpDir, "work"),
            "--input", "terms.yaml", "--output", "out.md");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.Contains("API", File.ReadAllText(PathIn(tmpDir, "work/other/out.md")));
    }

    /// <summary>
    ///     Test that --root and absolute paths keep the command-line paths trusted when the config has a root key.
    /// </summary>
    [Fact]
    public void DictionaryMark_FileAccess_CommandLineRootOrAbsolutePaths_StayTrustedUnderConfigRoot()
    {
        // Arrange: every dictionary and output lies outside the only allowed folder
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "cli-root/terms.yaml", "API: Application Programming Interface\n");
        var absoluteInput = WriteFile(tmpDir, "outside/terms.yaml", "CI: Continuous Integration\n");
        var absoluteOutput = PathIn(tmpDir, "outside/absolute.md");
        var config = WriteFile(tmpDir, "config/dictionarymark.yaml", "root: ../other\n");
        var allowed = PathIn(tmpDir, "config");

        // Act
        var rootExit = Runner.Run(
            out _,
            "dotnet", _dllPath, "--config", config, "--allow-root", allowed, "--root", PathIn(tmpDir, "cli-root"),
            "--input", "terms.yaml", "--output", "relative.md");
        var absoluteExit = Runner.Run(
            out _,
            "dotnet", _dllPath, "--config", config, "--allow-root", allowed, "--input", absoluteInput, "--output", absoluteOutput);

        // Assert
        Assert.Equal(0, rootExit);
        Assert.Contains("API", File.ReadAllText(PathIn(tmpDir, "cli-root/relative.md")));
        Assert.Equal(0, absoluteExit);
        Assert.Contains("CI", File.ReadAllText(absoluteOutput));
    }

    /// <summary>
    ///     Test that the help verb lists the help topics.
    /// </summary>
    [Fact]
    public void DictionaryMark_HelpTopics_HelpVerb_ListsTopics()
    {
        // Arrange: (none - constructor initializes _dllPath)

        // Act: run the tool with the help verb
        var exitCode = Runner.Run(
            out var output,
            "dotnet",
            _dllPath,
            "help");

        // Assert: every topic is listed with its command, without the banner
        Assert.Equal(0, exitCode);
        foreach (var topic in HelpCommand.RootCardIds)
        {
            Assert.Contains($"{HelpCommand.ToolName} help {topic}", output);
        }

        Assert.DoesNotContain("Copyright", output);
    }

    /// <summary>
    ///     Test that an unknown help topic returns the usage-error exit code.
    /// </summary>
    [Fact]
    public void DictionaryMark_HelpTopics_UnknownTopic_ReturnsUsageError()
    {
        // Arrange: (none - constructor initializes _dllPath)

        // Act: ask for a topic that does not exist
        var exitCode = Runner.Run(
            out var output,
            "dotnet",
            _dllPath,
            "help",
            "no-such-topic");

        // Assert: exit code 2 and a message that names the topic
        Assert.Equal(2, exitCode);
        Assert.Contains("no-such-topic", output);
    }

    /// <summary>
    ///     Test that every command example in the help cards gives the result that its card states.
    /// </summary>
    [Fact]
    public void DictionaryMark_HelpExamples_AllCommandExamples_GiveStatedResults()
    {
        // Arrange: the catalog the tool ships, and the input files that the examples read but users do not
        // author (inputs of other topics and deliberately broken files)
        var catalog = HelpCatalog.Create(HelpCommand.CreateOptions().GetCatalogConfiguration());
        var exampleFiles = Directory.GetFiles(PathHelpers.SafePathCombine(_testDataPath, "HelpExamples"));

        // Act: run every command example against the built tool in the runner's scratch folder; the runner
        // compares the exit codes and the 'expect' fragments. The input files are copied in before every
        // command, so that a command that overwrites a file cannot affect the next one.
        var report = HelpExampleRunner.Run(catalog, invocation =>
        {
            foreach (var file in exampleFiles)
            {
                File.Copy(file, PathHelpers.SafePathCombine(invocation.WorkingDirectory, Path.GetFileName(file)), true);
            }

            var exitCode = Runner.RunInDirectory(
                out var output,
                invocation.WorkingDirectory,
                "dotnet",
                [_dllPath, .. invocation.Arguments]);
            return new HelpExampleOutcome(exitCode, output);
        });

        // Assert: the cards hold command examples, and every one gave the result its card states
        Assert.True(report.CommandsRun > 0, "The help cards contain no command examples.");
        report.AssertSucceeded();
    }
}
