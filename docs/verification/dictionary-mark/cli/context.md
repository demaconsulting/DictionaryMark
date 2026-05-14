# Context Verification

This document describes the unit-level verification design for the `Context` unit. It defines
the test scenarios, dependency usage, and requirement coverage for `ContextTests.cs`.

## Verification Approach

`Context` is verified with unit tests in `ContextTests.cs`. Tests create `Context` instances
from controlled argument arrays and assert on property values, console output, log file content,
and exception types. Console streams are temporarily redirected for output assertions.

## Dependencies

| Dependency  | Usage in Tests                                                 |
| ----------- | -------------------------------------------------------------- |
| `Console`   | Redirected to `StringWriter` to assert written output content. |
| File system | Temporary files used to verify log file writing behavior.      |

## Test Scenarios

### Context_Create_NoArguments_ReturnsDefaultContext

**Expected**: All flags false; `ResultsFile` null; `HeadingDepth` 1; exit code 0.

### Context_Create_VersionFlag_SetsVersionTrue

**Expected**: `Version` true; `Help` false; exit code 0.

### Context_Create_ShortVersionFlag_SetsVersionTrue

**Expected**: `Version` true with `-v`; exit code 0.

### Context_Create_HelpFlag_SetsHelpTrue

**Expected**: `Help` true; `Version` false; exit code 0.

### Context_Create_ShortHelpFlag_H_SetsHelpTrue

**Expected**: `Help` true with `-h`; exit code 0.

### Context_Create_ShortHelpFlag_Question_SetsHelpTrue

**Expected**: `Help` true with `-?`; exit code 0.

### Context_Create_SilentFlag_SetsSilentTrue

**Expected**: `Silent` true; exit code 0.

### Context_Create_ValidateFlag_SetsValidateTrue

**Expected**: `Validate` true; exit code 0.

### Context_Create_ResultsFlag_SetsResultsFile

**Expected**: `ResultsFile` is `"test.trx"`; exit code 0.

### Context_Create_LogFlag_OpensLogFile

**Expected**: Log file is created and contains the written message after `Dispose`.

### Context_Create_UnknownArgument_ThrowsArgumentException

**Expected**: `ArgumentException` with "Unsupported argument" in message.

### Context_Create_LogFlag_WithoutValue_ThrowsArgumentException

**Expected**: `ArgumentException` mentioning "--log".

### Context_Create_ResultsFlag_WithoutValue_ThrowsArgumentException

**Expected**: `ArgumentException` mentioning "--results".

### Context_Create_ResultAliasFlag_SetsResultsFile

**Expected**: `--result` alias sets `ResultsFile` to `"test.trx"`; exit code 0.

### Context_Create_ResultAliasFlag_WithoutValue_ThrowsArgumentException

**Expected**: `ArgumentException` mentioning "--result".

### Context_Create_DepthFlag_SetsHeadingDepth

**Expected**: `HeadingDepth` is 3 when `--depth 3` is passed.

### Context_Create_NoDepthFlag_ReturnsDefaultHeadingDepth

**Expected**: `HeadingDepth` is 1 when no `--depth` is passed.

### Context_Create_DepthFlag_WithoutValue_ThrowsArgumentException

**Expected**: `ArgumentException` mentioning "--depth".

### Context_Create_DepthFlag_NonIntegerValue_ThrowsArgumentException

**Expected**: `ArgumentException` for non-integer value.

### Context_Create_DepthFlag_ZeroValue_ThrowsArgumentException

**Expected**: `ArgumentException` for out-of-range value 0.

### Context_Create_DepthFlag_ExceedsMaxValue_ThrowsArgumentException

**Expected**: `ArgumentException` for out-of-range value 7.

### Context_WriteLine_NotSilent_WritesToConsole

**Expected**: Message appears on captured stdout.

### Context_WriteLine_Silent_DoesNotWriteToConsole

**Expected**: Message suppressed in silent mode.

### Context_WriteError_Silent_DoesNotWriteToConsole

**Expected**: Error suppressed in silent mode.

### Context_WriteError_SetsErrorExitCode

**Expected**: `ExitCode` becomes 1 after `WriteError`.

### Context_WriteError_NotSilent_WritesToConsole

**Expected**: Error message appears on captured stderr.

### Context_WriteError_WritesToLogFile

**Expected**: Error message appears in log file even in silent mode.

### Context_Create_LogFlag_InvalidPath_ThrowsInvalidOperationException

**Expected**: `InvalidOperationException` when log path is a directory.

### Context_Create_InputFlag_AddsPattern

**Expected**: `InputPatterns` contains `"*.yaml"`.

### Context_Create_ShortInputFlag_AddsPattern

**Expected**: `-i` adds pattern to `InputPatterns`.

### Context_Create_MultipleInputFlags_AddsAllPatterns

**Expected**: Two `--input` flags produce two entries in `InputPatterns`.

### Context_Create_OutputFlag_SetsOutputFile

**Expected**: `OutputFile` is `"output.md"`.

### Context_Create_FormatFlag_Table_SetsTableFormat

**Expected**: `Format` is `OutputFormat.Table`.

### Context_Create_FormatFlag_Bullets_SetsBulletsFormat

**Expected**: `Format` is `OutputFormat.Bullets`.

### Context_Create_SectionFlag_SetsSectionHeading

**Expected**: `SectionHeading` is `"Glossary"`.

### Context_Create_TermHeaderFlag_SetsTermHeader

**Expected**: `TermHeader` is `"Word"`.

### Context_Create_DefinitionHeaderFlag_SetsDefinitionHeader

**Expected**: `DefinitionHeader` is `"Meaning"`.

### Context_Create_SortFlag_Alpha_SetsAlphabetical

**Expected**: `SortBy` is `SortOrder.Alphabetical`.

### Context_Create_SortFlag_File_SetsFileOrder

**Expected**: `SortBy` is `SortOrder.FileOrder`.

### Context_Create_InputFlag_WithoutValue_ThrowsArgumentException

**Expected**: `ArgumentException` mentioning "--input".

### Context_Create_OutputFlag_WithoutValue_ThrowsArgumentException

**Expected**: `ArgumentException` mentioning "--output".

### Context_Create_FormatFlag_InvalidValue_ThrowsArgumentException

**Expected**: `ArgumentException` for invalid format value.

### Context_Create_SortFlag_InvalidValue_ThrowsArgumentException

**Expected**: `ArgumentException` for invalid sort value.

### Context_Create_NoInputFlag_InputPatternsEmpty

**Expected**: `InputPatterns` is empty by default.

### Context_Create_NoOutputFlag_OutputFileNull

**Expected**: `OutputFile` is null by default.

### Context_Create_DefaultFormat_IsBullets

**Expected**: `Format` defaults to `OutputFormat.Bullets`.

### Context_Create_DefaultSort_IsFileOrder

**Expected**: `SortBy` defaults to `SortOrder.FileOrder`.

## Requirements Coverage

- **`DictionaryMark-Cli-Context`**: All `Context_Create_*` and `Context_Write*` tests.
