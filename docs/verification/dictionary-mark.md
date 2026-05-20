# DictionaryMark System Verification

This document describes system-level verification for DictionaryMark.

## Verification Strategy

System requirements are verified through integration tests in `IntegrationTests.cs` that
invoke the compiled DLL as an out-of-process `dotnet` command. This approach exercises the
full system stack — argument parsing, subsystem dispatch, and output generation — without
mocking any internal components.

Each system requirement maps to one or more named integration test scenarios. Integration
tests that depend on external files use the `TemporaryDirectory` test helper (see
`docs/verification/dictionary-mark/test-helpers.md`) to create and automatically clean up
temporary files.

## Test Environment

- **Test framework**: xUnit v3 with `[Collection("Sequential")]` to prevent parallelism.
- **Execution method**: `dotnet <DLL> <args>` via `Runner.Run` helper.
- **DLL location**: `AppContext.BaseDirectory` (resolved in constructor).
- **Temporary files**: `TemporaryDirectory` helper — creates a unique subdirectory under
  `Environment.CurrentDirectory` and deletes it on disposal (see
  `docs/verification/dictionary-mark/test-helpers.md`).
- **External dependencies**: None — tool is self-contained.

## External Interface Simulation

The integration tests drive DictionaryMark through its real command-line interface. There is
no interface simulation or mocking at the system level. Temporary YAML input files are
created inline within each test to control input data precisely.

## System-Level Test Scenarios

The following scenarios cover the complete system behavior from argument parsing through
output generation:

- Version flag: outputs version string, suppresses banner.
- Help flag: outputs usage information with all key flags listed.
- Validate flag: runs self-validation tests and outputs summary.
- Silent flag: suppresses all console output.
- Log flag: writes output to specified log file.
- Bullet generation: reads YAML input, produces Markdown bullet list.
- Table generation: reads YAML input, produces Markdown table.
- Output file: writes generated Markdown to specified file.
- Glob pattern input: expands wildcard `--input` patterns and processes all matching YAML files.
- Multiple input files: merges non-conflicting entries from multiple `--input` values.
- Conflict detection: reports error and exits non-zero on duplicate terms.
- Section heading: prefixes output with configured heading text.
- Custom term header: uses specified column header in table output.
- Custom definition header: uses specified column header in table output.
- Alphabetical sort: outputs entries in alphabetical order.
- Default sort order: preserves source file encounter order when alphabetical sorting is not requested.
- Heading depth: generates headings at the specified Markdown level.
- Validation results (TRX): writes TRX-format test results file.
- Validation results (JUnit XML): writes JUnit XML-format test results file.
- Error handling: returns non-zero exit code for unrecognized arguments.
- Error handling message: writes the unrecognized argument to stderr.
- Error handling no output: writes no Markdown output on unrecognized argument errors.

## Requirements Coverage

| Requirement | Test |
| --- | --- |
| DictionaryMark-System-Version | DictionaryMark_VersionFlag_Provided_OutputsVersion |
| DictionaryMark-System-Help | DictionaryMark_HelpFlag_Provided_OutputsUsageInformation |
| DictionaryMark-System-Validate | DictionaryMark_ValidateFlag_Provided_RunsValidation |
| DictionaryMark-System-Silent | DictionaryMark_SilentFlag_Provided_SuppressesOutput |
| DictionaryMark-System-Log | DictionaryMark_LogFlag_Provided_WritesOutputToFile |
| DictionaryMark-System-ErrorHandling | DictionaryMark_UnknownArgument_Provided_ReturnsError |
| DictionaryMark-System-GenerateBullets | DictionaryMark_Generate_BulletsFormat_OutputsBulletList |
| DictionaryMark-System-GenerateTable | DictionaryMark_Generate_TableFormat_OutputsTable |
| DictionaryMark-System-OutputFile | DictionaryMark_Generate_WithOutputFile_WritesFile |
| DictionaryMark-System-GlobPatterns | DictionaryMark_Generate_GlobInput_ProcessesMatches |
| DictionaryMark-System-MultipleInputs | DictionaryMark_Generate_MultipleInputs_MergesEntries |
| DictionaryMark-System-ConflictDetection-Report | DictionaryMark_Generate_ConflictingEntries_ReportsError |
| DictionaryMark-System-ConflictDetection-ExitCode | DictionaryMark_Generate_ConflictingEntries_ReportsError |
| DictionaryMark-System-SectionHeading | DictionaryMark_Generate_WithSectionHeading_OutputsHeading |
| DictionaryMark-System-TermHeader | DictionaryMark_Generate_WithTermHeader_OutputsCustomHeader |
| DictionaryMark-System-DefHeader | DictionaryMark_Generate_WithDefHeader_OutputsCustomHeader |
| DictionaryMark-System-SortAlpha | DictionaryMark_Generate_WithSortAlpha_OutputsAlphabeticOrder |
| DictionaryMark-System-SortFileOrder | DictionaryMark_Generate_DefaultSort_PreservesFileOrder |
| DictionaryMark-System-HeadingDepth | DictionaryMark_Generate_SectionWithDepth_OutputsHeading |
| DictionaryMark-System-ValidateResults-TRX | DictionaryMark_ValidateWithTrxResults_Requested_GeneratesTrxFile |
| DictionaryMark-System-ValidateResults-XML | DictionaryMark_ValidateWithXmlResults_Requested_GeneratesJUnitFile |
| DictionaryMark-System-ErrorHandling-Message | DictionaryMark_UnrecognizedArgument_WritesErrorToStderr |
| DictionaryMark-System-ErrorHandling-NoOutput | DictionaryMark_UnrecognizedArgument_WritesNoMarkdownOutput |

## Acceptance Criteria

All integration tests must pass. Individual tool invocations may return non-zero exit codes
when exercising expected error scenarios. No tests may be skipped or marked as expected
failures. The `DictionaryMark_ValidateFlag_Provided_RunsValidation` test must
report zero failures in the embedded self-validation output. File-based tests must use
`TemporaryDirectory` to ensure all temporary files are cleaned up on completion.
