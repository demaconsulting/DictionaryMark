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
- **File system** — tests that exercise log file writing, configuration files and root
  directories create temporary files via `TemporaryDirectory` for automatic cleanup. No
  file-system behavior is stubbed or replaced.
- **ConfigFile, TableJob, PathResolver** — documented dependencies of `Context`; they run their
  real logic, so the configuration and root tests confirm how `Context` combines them.

#### Test Environment

Tests that write log or configuration files use `TemporaryDirectory` for automatic cleanup.
Tests of a relative `--root` or `--config` must not change `Environment.CurrentDirectory`: they
create a uniquely named directory under the working directory (the test output folder) and delete
it in a `finally` block. Tests of `${VAR}` expansion set a uniquely named environment variable and
remove it in a `finally` block. The test class runs in the `Sequential` collection.

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

**Context_Create_Defaults_KindTermsAndHeaders**: Verifies `Kind` defaults to terms and the use/where
headers take their defaults. This scenario is tested by
`Context_Create_Defaults_KindTermsAndHeaders`.

**Context_Create_DeprecatedOptions_AreParsed**: Verifies `--kind deprecated`, `--use-header`,
and `--where-header` are parsed. This scenario is tested by
`Context_Create_DeprecatedOptions_AreParsed`.

**Context_Create_InvalidKind_ThrowsArgumentException**: Verifies an unrecognized `--kind` value is
rejected. This scenario is tested by `Context_Create_InvalidKind_ThrowsArgumentException`.

**Context_Create_ColumnWidthsFlag_SetsColumnWidths**: Verifies that `--column-widths 25,75` and
`20 , 50 , 30` (with spaces) are parsed in order and recorded in the command-line layer. This
scenario is tested by `Context_Create_ColumnWidthsFlag_SetsColumnWidths`.

**Context_Create_ColumnWidthsFlag_InvalidValue_ThrowsArgumentException**: Verifies that `0,1`,
`-1,2`, `1.5,2`, `a,b`, `25,`, an empty value, `1` and `1,2,3,4` are each rejected with the
"2 or 3 comma-separated positive integers" message. This scenario is tested by
`Context_Create_ColumnWidthsFlag_InvalidValue_ThrowsArgumentException`.

**Context_Create_ColumnWidthsFlag_WithoutValue_ThrowsArgumentException**: Verifies that
`--column-widths` without a value is rejected. This scenario is tested by
`Context_Create_ColumnWidthsFlag_WithoutValue_ThrowsArgumentException`.

**Context_Create_ColumnWidthsCountMismatch_ThrowsArgumentException**: Verifies that three widths
for a terms table are rejected when the jobs are built. This scenario is tested by
`Context_Create_ColumnWidthsCountMismatch_ThrowsArgumentException`.

**Context_Create_ConfigFlag_LoadsTableJobs**: Verifies that `--config` with two tables gives two
jobs with merged formats, the sequence input, and the label `table job 2 ('b.md')`. This scenario
is tested by `Context_Create_ConfigFlag_LoadsTableJobs`.

**Context_Create_ConfigFlagTwice_ThrowsArgumentException**: Verifies that a repeated `--config`
is rejected. This scenario is tested by `Context_Create_ConfigFlagTwice_ThrowsArgumentException`.

**Context_Create_ConfigWithTablesAndInput_ThrowsArgumentException** and
**Context_Create_ConfigWithTablesAndOutput_ThrowsArgumentException**: Verify that `--input` and
`--output` are rejected, naming the configuration file, when the file defines tables. These
scenarios are tested by `Context_Create_ConfigWithTablesAndInput_ThrowsArgumentException` and
`Context_Create_ConfigWithTablesAndOutput_ThrowsArgumentException`.

**Context_Create_ConfigWithoutTables_AppliesDefaultsToCommandLineJob**: Verifies that a
configuration file without tables supplies defaults to the single command-line job, while an
explicit `--depth` still wins. This scenario is tested by
`Context_Create_ConfigWithoutTables_AppliesDefaultsToCommandLineJob`.

**Context_Create_FormattingFlags_RecordedAsCommandLineLayer** and
**Context_Create_NoFormattingFlags_CommandLineLayerEmpty**: Verify that exactly the formatting
options given are recorded in `CommandLineFormatting`, and that it is empty when none are given.
These scenarios are tested by `Context_Create_FormattingFlags_RecordedAsCommandLineLayer` and
`Context_Create_NoFormattingFlags_CommandLineLayerEmpty`.

**Context_Create_ConfigPathEnvironmentVariable_Expanded**: Verifies that `${VAR}` in the
`--config` path is expanded. This scenario is tested by
`Context_Create_ConfigPathEnvironmentVariable_Expanded`.

**Context_Create_RootFlag_SetsRootDirectory**,
**Context_Create_RootFlagRelativeWithParentSegments_ResolvedAgainstWorkingDirectory** and
**Context_Create_RootFlagEnvironmentVariable_Expanded**: Verify that `--root` resolves an absolute
path, a relative `sub/../{name}` path against the working directory, and a `${VAR}` reference to
the full path of the directory. These scenarios are tested by
`Context_Create_RootFlag_SetsRootDirectory`,
`Context_Create_RootFlagRelativeWithParentSegments_ResolvedAgainstWorkingDirectory` and
`Context_Create_RootFlagEnvironmentVariable_Expanded`.

**Context_Create_RootFlagMissingDirectory_ThrowsInvalidOperationException**,
**Context_Create_RootFlagTwice_ThrowsArgumentException**,
**Context_Create_RootFlagWithoutValue_ThrowsArgumentException** and
**Context_Create_RootFlagEmpty_ThrowsArgumentException**: Verify the error paths of `--root`: a
missing directory (message naming the directory and option), a repeated option, a missing value
and a whitespace value. These scenarios are tested by the tests of the same names.

**Context_Create_RootFlagInvalidCharacter_ThrowsInvalidOperationException** and
**Context_Create_ConfigFlagInvalidCharacter_ThrowsInvalidOperationException**: Verify that a
`--root` or `--config` value containing a null character (invalid on every platform) is reported
as `InvalidOperationException` naming the option and the value (`Invalid --root option '...'`,
`Invalid config file path '...'`), so `Program.Main` prints a clean error. These scenarios are
tested by the tests of the same names.

**Context_Create_RootFlag_AppliedToCommandLineJob**,
**Context_Create_RootFlag_AppliedToConfigTables**, **Context_Create_RootFlag_OverridesConfigRoot**,
**Context_Create_ConfigRoot_AppliedToConfigTables**, **Context_Create_ConfigRoot_AppliedToCommandLineJob**,
**Context_Create_NoRoot_ConfigTablesUseConfigDirectory** and
**Context_Create_NoRoot_CommandLineJobUsesWorkingDirectory**: Verify every cell of the
base-directory precedence table by checking `TableJob.BaseDirectory` of the created jobs. These
scenarios are tested by the tests of the same names.

**Context_Create_RootFlag_ConfigPathNotResolvedAgainstRoot**: Verifies that a relative `--config`
is resolved against the working directory even when `--root` names a different directory. This
scenario is tested by `Context_Create_RootFlag_ConfigPathNotResolvedAgainstRoot`.

**Context_Create_NoFileAccess_DefaultsToAny**, **Context_Create_FileAccessEachMode_Parsed**,
**Context_Create_FileAccessInvalid_ThrowsListingModes** and **Context_Create_FileAccessTwice_Throws**:
Verify that the file-access mode defaults to any with no folders, that every mode is accepted in any
letter case, that an invalid value is rejected with the message listing the modes, and that a repeated
`--file-access` is rejected. These scenarios are tested by the tests of the same names.

**Context_Create_AllowRoot_ImpliesRootsAndReplacesDefaults**,
**Context_Create_AllowRootRelative_ResolvedAgainstWorkingDirectoryNotRoot**,
**Context_Create_AllowRootEnvironmentVariable_Expanded** and **Context_Create_AllowRootMissing_Throws**:
Verify that two `--allow-root` options give roots with exactly those folders. A relative value climbing
through `..` resolves under the working directory even with a `--root` that has a folder of the same
name, and a `${VAR}` value is expanded. A missing folder gives the exact message naming the folder and
the option. These scenarios are tested by the tests of the same names.

**Context_Create_AllowRootWithAny_ThrowsNamingConflict** and
**Context_Create_AllowRootWithNoSymlinks_ThrowsNamingConflict**: Verify that `--allow-root` with
`--file-access any` or `No-Symlinks` (in either order) is rejected with a message naming the conflict
and, for no-symlinks, the `roots,no-symlinks` spelling to use. These scenarios are tested by the tests of
the same names.

**Context_Create_RootsWithConfig_AllowsWorkingAndConfigFolders** and
**Context_Create_RootsWithoutConfig_AllowsWorkingDirectory**: Verify the default folders of roots (the
working directory, plus the configuration file's folder when `--config` is given) and that the
configuration file's folder is the trusted base. These scenarios are tested by the tests of the same
names.
