### Context

The `Context` unit is verified with unit tests in `ContextTests.cs`. Tests create `Context`
instances from controlled argument arrays and assert on property values, console output, log
file content, and exception types.

#### Verification Approach

`Context` has no unit-level dependencies that require mocking. All dependencies are exercised
directly:

- **`Console`** — stdout and stderr streams are temporarily redirected to `StringWriter`
  instances so that tests can assert on the exact content written without producing console
  noise. This is a test infrastructure technique, not mocking.
- **File system** — tests that exercise log file writing create temporary files via
  `TemporaryDirectory` for automatic cleanup. No file-system behavior is stubbed or replaced.

#### Test Environment

N/A - standard test environment. Tests that write log files use `TemporaryDirectory` for
automatic cleanup.

#### Acceptance Criteria

- All unit tests in `ContextTests.cs` pass with zero failures.
- All requirements linked to the `Context` unit have at least one passing test scenario.
- No tests may be skipped or marked as expected failures.

#### Test Scenarios

**Context_Create_NoArguments_ReturnsDefaultContext**: Verifies that a `Context` created with an
empty argument array has all flags false, `ResultsFile` null, `HeadingDepth` 1, and `ExitCode`
0, confirming the default state is well-defined. This scenario is tested by
`Context_Create_NoArguments_ReturnsDefaultContext`.

**Context_Create_VersionFlag_SetsVersionTrue**: Verifies that `--version` sets `Version` to
true while all other flags remain false and `ExitCode` is 0. This scenario is tested by
`Context_Create_VersionFlag_SetsVersionTrue`.

**Context_Create_ShortVersionFlag_SetsVersionTrue**: Verifies that the short alias `-v` also
sets `Version` to true with `ExitCode` 0. This scenario is tested by
`Context_Create_ShortVersionFlag_SetsVersionTrue`.

**Context_Create_HelpFlag_SetsHelpTrue**: Verifies that `--help` sets `Help` to true while
`Version` remains false and `ExitCode` is 0. This scenario is tested by
`Context_Create_HelpFlag_SetsHelpTrue`.

**Context_Create_ShortHelpFlag_H_SetsHelpTrue**: Verifies that `-h` sets `Help` to true with
`ExitCode` 0. This scenario is tested by `Context_Create_ShortHelpFlag_H_SetsHelpTrue`.

**Context_Create_ShortHelpFlag_Question_SetsHelpTrue**: Verifies that `-?` sets `Help` to true
with `ExitCode` 0. This scenario is tested by
`Context_Create_ShortHelpFlag_Question_SetsHelpTrue`.

**Context_Create_SilentFlag_SetsSilentTrue**: Verifies that `--silent` sets `Silent` to true
with `ExitCode` 0. This scenario is tested by `Context_Create_SilentFlag_SetsSilentTrue`.

**Context_Create_ValidateFlag_SetsValidateTrue**: Verifies that `--validate` sets `Validate` to
true with `ExitCode` 0. This scenario is tested by `Context_Create_ValidateFlag_SetsValidateTrue`.

**Context_Create_ResultsFlag_SetsResultsFile**: Verifies that `--results test.trx` sets
`ResultsFile` to `"test.trx"` with `ExitCode` 0. This scenario is tested by
`Context_Create_ResultsFlag_SetsResultsFile`.

**Context_Create_LogFlag_OpensLogFile**: Verifies that `--log <path>` creates the log file on
disk and that a subsequent `WriteLine` call produces content in that file after `Dispose`. This
scenario is tested by `Context_Create_LogFlag_OpensLogFile`.

**Context_Create_UnknownArgument_ThrowsArgumentException**: Verifies that an unrecognized flag
causes `Create` to throw `ArgumentException` with "Unsupported argument" in the message. This
scenario is tested by `Context_Create_UnknownArgument_ThrowsArgumentException`.

**Context_Create_LogFlag_WithoutValue_ThrowsArgumentException**: Verifies that `--log` supplied
without a following value causes `Create` to throw `ArgumentException` mentioning `"--log"`.
This scenario is tested by `Context_Create_LogFlag_WithoutValue_ThrowsArgumentException`.

**Context_Create_ResultsFlag_WithoutValue_ThrowsArgumentException**: Verifies that `--results`
supplied without a following value causes `Create` to throw `ArgumentException` mentioning
`"--results"`. This scenario is tested by
`Context_Create_ResultsFlag_WithoutValue_ThrowsArgumentException`.

**Context_Create_ResultAliasFlag_SetsResultsFile**: Verifies that the `--result` alias (without
the trailing `s`) sets `ResultsFile` to `"test.trx"` with `ExitCode` 0. This scenario is
tested by `Context_Create_ResultAliasFlag_SetsResultsFile`.

**Context_Create_ResultAliasFlag_WithoutValue_ThrowsArgumentException**: Verifies that `--result`
supplied without a following value causes `Create` to throw `ArgumentException` mentioning
`"--result"`. This scenario is tested by
`Context_Create_ResultAliasFlag_WithoutValue_ThrowsArgumentException`.

**Context_Create_DepthFlag_SetsHeadingDepth**: Verifies that `--depth 3` sets `HeadingDepth` to
3. This scenario is tested by `Context_Create_DepthFlag_SetsHeadingDepth`.

**Context_Create_NoDepthFlag_ReturnsDefaultHeadingDepth**: Verifies that when `--depth` is not
supplied, `HeadingDepth` defaults to 1. This scenario is tested by
`Context_Create_NoDepthFlag_ReturnsDefaultHeadingDepth`.

**Context_Create_DepthFlag_WithoutValue_ThrowsArgumentException**: Verifies that `--depth`
supplied without a following value causes `Create` to throw `ArgumentException` mentioning
`"--depth"`. This scenario is tested by
`Context_Create_DepthFlag_WithoutValue_ThrowsArgumentException`.

**Context_Create_DepthFlag_NonIntegerValue_ThrowsArgumentException**: Verifies that `--depth`
supplied with a non-integer value causes `Create` to throw `ArgumentException`. This scenario
is tested by `Context_Create_DepthFlag_NonIntegerValue_ThrowsArgumentException`.

**Context_Create_DepthFlag_ZeroValue_ThrowsArgumentException**: Verifies that `--depth 0` is
rejected as out-of-range with `ArgumentException`. This scenario is tested by
`Context_Create_DepthFlag_ZeroValue_ThrowsArgumentException`.

**Context_Create_DepthFlag_ExceedsMaxValue_ThrowsArgumentException**: Verifies that `--depth 7`
is rejected as out-of-range with `ArgumentException` (valid range is 1–6). This scenario is
tested by `Context_Create_DepthFlag_ExceedsMaxValue_ThrowsArgumentException`.

**Context_WriteLine_NotSilent_WritesToConsole**: Verifies that `WriteLine` writes the message to
captured stdout when `Silent` is false. This scenario is tested by
`Context_WriteLine_NotSilent_WritesToConsole`.

**Context_WriteLine_Silent_DoesNotWriteToConsole**: Verifies that `WriteLine` suppresses console
output when `Silent` is true. This scenario is tested by
`Context_WriteLine_Silent_DoesNotWriteToConsole`.

**Context_WriteError_Silent_DoesNotWriteToConsole**: Verifies that `WriteError` suppresses
console output when `Silent` is true. This scenario is tested by
`Context_WriteError_Silent_DoesNotWriteToConsole`.

**Context_WriteError_SetsErrorExitCode**: Verifies that calling `WriteError` sets `ExitCode` to
1 regardless of other flags. This scenario is tested by `Context_WriteError_SetsErrorExitCode`.

**Context_WriteError_NotSilent_WritesToConsole**: Verifies that `WriteError` writes the error
message to captured stderr when `Silent` is false. This scenario is tested by
`Context_WriteError_NotSilent_WritesToConsole`.

**Context_WriteError_WritesToLogFile**: Verifies that `WriteError` writes the error message to
the open log file even in silent mode. This scenario is tested by
`Context_WriteError_WritesToLogFile`.

**Context_Create_LogFlag_InvalidPath_ThrowsInvalidOperationException**: Verifies that specifying
a directory path as the log file path causes `Create` to throw `InvalidOperationException`. This
scenario is tested by `Context_Create_LogFlag_InvalidPath_ThrowsInvalidOperationException`.

**Context_Create_InputFlag_AddsPattern**: Verifies that `--input *.yaml` adds `"*.yaml"` to
`InputPatterns`. This scenario is tested by `Context_Create_InputFlag_AddsPattern`.

**Context_Create_ShortInputFlag_AddsPattern**: Verifies that the short alias `-i *.yaml` also
adds `"*.yaml"` to `InputPatterns`. This scenario is tested by
`Context_Create_ShortInputFlag_AddsPattern`.

**Context_Create_MultipleInputFlags_AddsAllPatterns**: Verifies that supplying `--input` twice
produces two entries in `InputPatterns`. This scenario is tested by
`Context_Create_MultipleInputFlags_AddsAllPatterns`.

**Context_Create_OutputFlag_SetsOutputFile**: Verifies that `--output output.md` sets
`OutputFile` to `"output.md"`. This scenario is tested by
`Context_Create_OutputFlag_SetsOutputFile`.

**Context_Create_FormatFlag_Table_SetsTableFormat**: Verifies that `--format table` sets
`Format` to `OutputFormat.Table`. This scenario is tested by
`Context_Create_FormatFlag_Table_SetsTableFormat`.

**Context_Create_FormatFlag_Bullets_SetsBulletsFormat**: Verifies that `--format bullets` sets
`Format` to `OutputFormat.Bullets`. This scenario is tested by
`Context_Create_FormatFlag_Bullets_SetsBulletsFormat`.

**Context_Create_SectionFlag_SetsSectionHeading**: Verifies that `--section Glossary` sets
`SectionHeading` to `"Glossary"`. This scenario is tested by
`Context_Create_SectionFlag_SetsSectionHeading`.

**Context_Create_TermHeaderFlag_SetsTermHeader**: Verifies that `--term-header Word` sets
`TermHeader` to `"Word"`. This scenario is tested by `Context_Create_TermHeaderFlag_SetsTermHeader`.

**Context_Create_DefinitionHeaderFlag_SetsDefinitionHeader**: Verifies that `--def-header
Meaning` sets `DefinitionHeader` to `"Meaning"`. This scenario is tested by
`Context_Create_DefinitionHeaderFlag_SetsDefinitionHeader`.

**Context_Create_SortFlag_Alpha_SetsAlphabetical**: Verifies that `--sort alpha` sets `SortBy`
to `SortOrder.Alphabetical`. This scenario is tested by `Context_Create_SortFlag_Alpha_SetsAlphabetical`.

**Context_Create_SortFlag_File_SetsFileOrder**: Verifies that `--sort file` sets `SortBy` to
`SortOrder.FileOrder`. This scenario is tested by `Context_Create_SortFlag_File_SetsFileOrder`.

**Context_Create_InputFlag_WithoutValue_ThrowsArgumentException**: Verifies that `--input`
supplied without a following value causes `Create` to throw `ArgumentException` mentioning
`"--input"`. This scenario is tested by
`Context_Create_InputFlag_WithoutValue_ThrowsArgumentException`.

**Context_Create_OutputFlag_WithoutValue_ThrowsArgumentException**: Verifies that `--output`
supplied without a following value causes `Create` to throw `ArgumentException` mentioning
`"--output"`. This scenario is tested by
`Context_Create_OutputFlag_WithoutValue_ThrowsArgumentException`.

**Context_Create_FormatFlag_InvalidValue_ThrowsArgumentException**: Verifies that `--format`
supplied with an unrecognized value causes `Create` to throw `ArgumentException`. This scenario
is tested by `Context_Create_FormatFlag_InvalidValue_ThrowsArgumentException`.

**Context_Create_SortFlag_InvalidValue_ThrowsArgumentException**: Verifies that `--sort`
supplied with an unrecognized value causes `Create` to throw `ArgumentException`. This scenario
is tested by `Context_Create_SortFlag_InvalidValue_ThrowsArgumentException`.

**Context_Create_NoInputFlag_InputPatternsEmpty**: Verifies that `InputPatterns` is an empty
list when no `--input` flag is supplied. This scenario is tested by
`Context_Create_NoInputFlag_InputPatternsEmpty`.

**Context_Create_NoOutputFlag_OutputFileNull**: Verifies that `OutputFile` is null when no
`--output` flag is supplied. This scenario is tested by `Context_Create_NoOutputFlag_OutputFileNull`.

**Context_Create_DefaultFormat_IsBullets**: Verifies that `Format` defaults to
`OutputFormat.Bullets` when `--format` is not supplied. This scenario is tested by
`Context_Create_DefaultFormat_IsBullets`.

**Context_Create_DefaultSort_IsFileOrder**: Verifies that `SortBy` defaults to
`SortOrder.FileOrder` when `--sort` is not supplied. This scenario is tested by
`Context_Create_DefaultSort_IsFileOrder`.
