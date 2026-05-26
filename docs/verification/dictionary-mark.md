# DictionaryMark

## Verification Approach

System requirements are verified through integration tests in `IntegrationTests.cs` (located in
`test/DemaConsulting.DictionaryMark.Tests/`) that invoke the compiled DLL as an out-of-process
`dotnet` command. This approach exercises the full system stack — argument parsing, subsystem
dispatch, and output generation — without mocking any internal components. Each system
requirement maps to one or more named integration test scenarios. Integration tests that depend
on external files use the `TemporaryDirectory` test helper to create and automatically clean up
temporary files.

## Test Environment

- **Test framework**: xUnit v3 with `[Collection("Sequential")]` to prevent parallel execution.
- **Execution method**: `dotnet <DLL> <args>` via `Runner.Run` helper.
- **DLL location**: `AppContext.BaseDirectory` (resolved in the test class constructor).
- **Temporary files**: `TemporaryDirectory` helper creates a unique subdirectory under
  `Environment.CurrentDirectory` and deletes it on disposal (see
  *DictionaryMark Test Helpers Verification Design*).
- **External dependencies**: None — the tool is self-contained.

## Acceptance Criteria

- All integration tests pass with zero failures.
- No tests may be skipped or marked as expected failures.
- The `DictionaryMark_ValidateFlag_Provided_RunsValidation` test must report zero failures in
  the embedded self-validation output.
- File-based tests must use `TemporaryDirectory` to ensure all temporary files are cleaned up
  on completion.

## Test Scenarios

**VersionDisplay**: When the `--version` flag is provided, the tool must print a valid semantic
version string and exit with code zero. This ensures the version requirement is satisfied and the
tool self-identifies correctly for audit trails.
This scenario is tested by `DictionaryMark_VersionFlag_Provided_OutputsVersion`.

**HelpDisplay**: When the `--help` flag is provided, the tool must print usage information
listing all key command-line options and exit with code zero. This ensures users and automated
pipelines can discover the tool's interface.
This scenario is tested by `DictionaryMark_HelpFlag_Provided_OutputsUsageInformation`.

**SelfValidation**: When the `--validate` flag is provided, the tool must run all built-in
self-validation tests, print a summary, and exit with code zero when all tests pass. This
ensures the tool is fit for use in regulated environments that require qualification evidence.
This scenario is tested by `DictionaryMark_ValidateFlag_Provided_RunsValidation`.

**SilentMode**: When the `--silent` flag is provided, the tool must suppress all console output
while still generating any requested output files. This ensures the tool integrates quietly into
automated pipelines.
This scenario is tested by `DictionaryMark_SilentFlag_Provided_SuppressesOutput`.

**LogFile**: When the `--log` flag is provided with a file path, the tool must write its console
output to the specified log file in addition to (or instead of) the terminal. This ensures
pipeline runs can capture tool output for audit evidence.
This scenario is tested by `DictionaryMark_LogFlag_Provided_WritesOutputToFile`.

**BulletGeneration**: When a valid YAML input file is supplied and the output format is `bullets`
(the default), the tool must produce a Markdown bullet list of term–definition pairs in the
correct format. This is the primary output mode of DictionaryMark.
This scenario is tested by `DictionaryMark_Generate_BulletsFormat_OutputsBulletList`.

**TableGeneration**: When a valid YAML input file is supplied and `--format table` is specified,
the tool must produce a Markdown table of term–definition pairs with the correct column
separators and alignment. This ensures the table output mode meets formatting requirements.
This scenario is tested by `DictionaryMark_Generate_TableFormat_OutputsTable`.

**OutputFile**: When the `--output` flag is provided with a file path, the tool must write the
generated Markdown to that file rather than to stdout. This ensures the tool can be integrated
into file-based documentation pipelines.
This scenario is tested by `DictionaryMark_Generate_WithOutputFile_WritesFile`.

**GlobPatternInput**: When the `--input` flag specifies a glob pattern, the tool must expand
the pattern and process all matching YAML files. This ensures the glob-expansion requirement
is satisfied for batch-processing workflows.
This scenario is tested by `DictionaryMark_Generate_GlobInput_ProcessesMatches`.

**MultipleInputs**: When multiple `--input` flags are provided with non-conflicting YAML files,
the tool must merge all entries into a single output. This ensures the multi-file merge
requirement is satisfied.
This scenario is tested by `DictionaryMark_Generate_MultipleInputs_MergesEntries`.

**ConflictDetection**: When two or more input files define the same term differently, the tool
must report the conflict, write nothing to the output, and exit with a non-zero code. This
ensures conflicting definitions are never silently accepted.
This scenario is tested by `DictionaryMark_Generate_ConflictingEntries_ReportsError`.

**SectionHeading**: When the `--section` flag is provided with heading text, the tool must
prefix the output with a Markdown heading at the configured depth. This ensures the optional
heading requirement is satisfied.
This scenario is tested by `DictionaryMark_Generate_WithSectionHeading_OutputsHeading`.

**CustomTermHeader**: When the `--term-header` flag is provided, the tool must use the
specified string as the term column header in table output instead of the default "Term". This
ensures the customizable column header requirement is satisfied.
This scenario is tested by `DictionaryMark_Generate_WithTermHeader_OutputsCustomHeader`.

**CustomDefinitionHeader**: When the `--def-header` flag is provided, the tool must use the
specified string as the definition column header in table output instead of the default
"Definition". This ensures the customizable column header requirement is satisfied.
This scenario is tested by `DictionaryMark_Generate_WithDefHeader_OutputsCustomHeader`.

**AlphabeticalSort**: When `--sort alpha` is specified, the tool must output entries in
case-insensitive alphabetical order regardless of their order in the source files. This ensures
the alphabetical sort requirement is satisfied.
This scenario is tested by `DictionaryMark_Generate_WithSortAlpha_OutputsAlphabeticOrder`.

**FileOrderSort**: When no `--sort` flag is specified (or `--sort file` is used), the tool
must preserve the order in which entries appear across the input files. This ensures the
default file-order preservation requirement is satisfied.
This scenario is tested by `DictionaryMark_Generate_DefaultSort_PreservesFileOrder`.

**HeadingDepth**: When the `--depth` flag is provided, the tool must generate section
headings at the specified Markdown level (e.g., depth 2 produces `##`). This ensures the
configurable heading depth requirement is satisfied.
This scenario is tested by `DictionaryMark_Generate_SectionWithDepth_OutputsHeading`.

**ValidationResultsTRX**: When `--validate --results <file.trx>` is provided, the tool must
write self-validation results in TRX (MSTest) format to the specified file. This ensures the
tool can supply qualification evidence compatible with .NET test tooling.
This scenario is tested by `DictionaryMark_ValidateWithTrxResults_Requested_GeneratesTrxFile`.

**ValidationResultsJUnit**: When `--validate --results <file.xml>` is provided, the tool must
write self-validation results in JUnit XML format to the specified file. This ensures the tool
can supply qualification evidence compatible with JUnit-compatible CI systems.
This scenario is tested by
`DictionaryMark_ValidateWithXmlResults_Requested_GeneratesJUnitFile`.

**ErrorExitCode**: When an unrecognized or invalid argument is provided, the tool must exit
with a non-zero exit code. This ensures integration scripts can detect and handle tool
failures reliably.
This scenario is tested by `DictionaryMark_UnknownArgument_Provided_ReturnsError`.

**ErrorMessageToStderr**: When an unrecognized argument is provided, the tool must write a
descriptive error message identifying the unrecognized argument to stderr. This ensures the
error is visible to users and pipeline logs.
This scenario is tested by `DictionaryMark_UnrecognizedArgument_WritesErrorToStderr`.

**NoMarkdownOutputOnError**: When an unrecognized argument is provided, the tool must not
write any Markdown output — neither to stdout nor to any output file. This ensures partial
or corrupt output is never produced when the tool is misconfigured.
This scenario is tested by `DictionaryMark_UnrecognizedArgument_WritesNoMarkdownOutput`.
