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
- **Execution method**: `dotnet <DLL> <args>` via `Runner.Run` helper. Tests of working-directory
  relative paths (relative `..` globs, a relative `--root`, command-line paths without a root) use
  `Runner.RunInDirectory` to start the tool in a temporary working directory without changing the
  working directory of the test process.
- **Environment variables**: tests of `${VAR}` expansion set a uniquely named variable in the test
  process (inherited by the started tool) and remove it in a `finally` block.
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

**DeprecatedKind**: When `--kind deprecated` is provided with a YAML file of deprecated entries,
the tool must produce a table with the "Use instead" and "Where it was used" columns and exit
with code zero. This ensures the deprecated-terms output requirement is satisfied.
This scenario is tested by `DictionaryMark_Generate_DeprecatedKind_OutputsDeprecatedTable`.

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

**ColumnWidths**: When `--column-widths 25,75` is given with table output, the separator row must
contain exactly 25 and 75 dashes after the `:` markers. When three weights are given for a
deprecated table whose entries have no Where value, the third weight must be dropped and the first
two rescaled (20,50 becomes 29,71). Bullet output must be byte-for-byte identical with and without
widths.
This scenario is tested by `DictionaryMark_Generate_ColumnWidths_OutputsProportionalSeparator`,
`DictionaryMark_Generate_ColumnWidthsDeprecatedWithoutWhere_RescalesTwoColumns` and
`DictionaryMark_Generate_ColumnWidthsWithBullets_OutputUnchanged`.

**ColumnWidthsValidation**: Zero, negative and non-integer weights, and a weight count that does
not match the table (2 weights for a deprecated table), must be rejected with an error and exit
code 1.
This scenario is tested by `DictionaryMark_Generate_ColumnWidthsInvalidValue_ReturnsError` and
`DictionaryMark_Generate_ColumnWidthsWrongCount_ReturnsError`.

**ConfigDefaults**: A configuration file with only formatting defaults must apply them to a
command-line `--input` run (section heading, table format and column widths).
This scenario is tested by `DictionaryMark_Config_FormattingDefaults_AppliedToCommandLineRun`.

**ConfigPrecedence**: A command-line option must override both the job key and the top-level
default of the configuration file, for every job.
This scenario is tested by `DictionaryMark_Config_CommandLineOption_OverridesFileValues`.

**ConfigTableJobs**: A configuration file with two table jobs (approved terms and deprecated terms,
each with its own widths) must produce both output files.
This scenario is tested by `DictionaryMark_Config_MultipleTableJobs_WritesEachOutput`.

**ConfigRelativePaths**: Relative inputs (a leading `..` recursive glob and a mid-pattern `..`
literal) and a relative output in a missing directory, all in a configuration file, must resolve
against the configuration file's directory regardless of the working directory.
This scenario is tested by
`DictionaryMark_Config_RelativePathsWithParentSegments_ResolvedFromConfigDirectory`.

**ConfigEnvironmentVariables**: `${VAR}` references in the `--config` path and in the input and
output paths of a job must be expanded.
This scenario is tested by `DictionaryMark_Config_EnvironmentVariablesInPaths_AreExpanded`.

**ConfigJobFailures**: When the second of three jobs matches no files, jobs 1 and 3 must still be
written, the failure must be reported as `table job 2 ('b.md')`, a summary `1 of 3 table jobs
failed` must be reported, and the exit code must be 1.
This scenario is tested by `DictionaryMark_Config_OneJobFails_OtherJobsWrittenAndExitNonZero`.

**ConfigValidation**: A missing file, malformed YAML, an unknown job key and a disallowed top-level
key must each be rejected with an error naming the file and the key (and job), and exit code 1.
This scenario is tested by `DictionaryMark_Config_InvalidFile_ReturnsErrorNamingFile` and
`DictionaryMark_Config_UnknownKey_ReturnsErrorNamingKey`.

**ConfigCommandLineConflict**: `--input` and `--output` with a configuration file that defines
tables, and a repeated `--config`, must be rejected with exit code 1.
This scenario is tested by `DictionaryMark_Config_TablesWithCommandLineInput_ReturnsError`.

**ParentDirectoryPatterns**: Relative command-line globs with a leading `..` and with `..` after a
normal segment must match files outside the working directory.
This scenario is tested by `DictionaryMark_Generate_ParentDirectoryGlob_ProcessesMatches`.

**OutputDirectoryCreation**: An `--output` path inside directories that do not exist must be
written after the directories are created.
This scenario is tested by `DictionaryMark_Generate_OutputInMissingDirectory_CreatesDirectory`.

**RootDirectory**: With `--root`, relative command-line inputs and outputs, and relative
configuration table paths, must resolve against the root; a relative root with `..` must resolve
against the working directory; `${VAR}` in the root must be expanded; and absolute inputs and
outputs must ignore the root.
This scenario is tested by `DictionaryMark_Root_CommandLineInputsAndOutput_ResolvedAgainstRoot`,
`DictionaryMark_Root_RelativeRootWithParentSegments_ResolvedAgainstWorkingDirectory`,
`DictionaryMark_Root_ConfigTables_ResolvedAgainstRoot`, `DictionaryMark_Root_EnvironmentVariable_Expanded`
and `DictionaryMark_Root_AbsoluteInputAndOutput_IgnoreRoot`.

**ConfigRoot**: A configuration file's `root: ../..` key must be the base for its table jobs, and a
`root` key in a file without tables must be the base for command-line inputs.
This scenario is tested by `DictionaryMark_Config_Root_TablesResolvedAgainstRoot` and
`DictionaryMark_Config_Root_CommandLineInputsResolvedAgainstRoot`.

**RootPrecedence**: `--root` must override the configuration `root` key; without any root,
configuration paths must resolve against the configuration file's directory and command-line
paths against the working directory.
This scenario is tested by `DictionaryMark_Root_CommandLineRoot_OverridesConfigRoot`,
`DictionaryMark_Config_RelativePathsWithParentSegments_ResolvedFromConfigDirectory` and
`DictionaryMark_Config_NoRoot_CommandLinePathsUseWorkingDirectory`.

**RootValidation**: A missing `--root` directory, a repeated or empty `--root`, a configuration
`root` naming a missing directory, and a `root` key inside a table job must each be rejected with
an error and exit code 1.
This scenario is tested by `DictionaryMark_Root_MissingDirectory_ReturnsError`,
`DictionaryMark_Root_SpecifiedTwice_ReturnsError`, `DictionaryMark_Config_MissingRoot_ReturnsErrorNamingFile`
and `DictionaryMark_Config_RootInTableJob_ReturnsErrorNamingJob`.

**FileAccess**: The file-access options must confine the inputs and outputs of configuration-file tables: with
`--allow-root` a table inside the folder is generated while a table whose input or output lies outside it is denied with
`file-access-denied` and writes nothing; `--file-access no-symlinks` must deny an input reached through a directory
link; `--file-access roots` alone must allow the working directory and the configuration file's folder; every glob match
must be judged (a match outside the folder fails its table); the default mode must still refuse never-list paths, and
refuse a network input pattern before anything is listed; a denied table must fail alone with exit code 1; command-line
paths must never be judged; the configuration `root` key must never widen the allowed folders; and every mistake in the
options must be a usage error reported before any input is read. This scenario is tested by
`DictionaryMark_FileAccess_AllowRootAsProcess_ConfinesConfigJobs`,
`DictionaryMark_FileAccess_MistakesAsProcess_AreUsageErrors`,
`DictionaryMark_FileAccess_NoSymlinksAsProcess_DeniesLinkedConfigInput`,
`DictionaryMark_FileAccess_DefaultMode_NeverListRefusesConfigPaths`,
`DictionaryMark_FileAccess_NetworkInputPattern_RefusedBeforeListing`,
`DictionaryMark_FileAccess_CommandLinePaths_AreNotJudged`,
`DictionaryMark_FileAccess_DeniedJob_OtherJobsRunAndExitCodeIsOne`,
`DictionaryMark_FileAccess_RootsWithoutAllowRoot_AllowsWorkingAndConfigFolders`,
`DictionaryMark_FileAccess_GlobMatchOutsideRoots_IsDeniedNotSkipped` and
`DictionaryMark_FileAccess_ConfigRootKey_DoesNotWidenAllowedFolders`.
