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
    /// <summary>
    ///     Full path to the DictionaryMark DLL under test, resolved once in the constructor
    ///     and reused by every test method.
    /// </summary>
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

    /// <summary>Test that a config root naming a missing directory is an error naming the file.</summary>
    [Fact]
    public void DictionaryMark_Config_MissingRoot_ReturnsErrorNamingFile()
    {
        // Arrange: a config whose root does not exist
        using var tmpDir = new TemporaryDirectory();
        var config = WriteFile(tmpDir, "dictionarymark.yaml", "root: missing\n");

        // Act
        var exitCode = Runner.Run(out var output, "dotnet", _dllPath, "--config", config);

        // Assert
        Assert.Equal(1, exitCode);
        Assert.Contains($"Config file '{config}': Directory '{PathIn(tmpDir, "missing")}' specified by root key", output);
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
}
