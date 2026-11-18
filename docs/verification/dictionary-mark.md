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
- **Links and pipes**: file-access tests create folder links with the `LinkSupport` helper, falling
  back to a junction on Windows when symbolic links need a privilege the account does not hold. A test
  that needs a file symbolic link is skipped, with a reason, on such a machine; its requirement takes
  evidence only from Linux and macOS runs.

## Acceptance Criteria

- All integration tests pass with zero failures.
- No tests may be skipped or marked as expected failures, except a file-access test skipped with a
  reason because the machine cannot create a file symbolic link; such a test must pass on the
  platforms named by its requirement's source filter.
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

**FileAccessModes**: Every `--file-access` mode is accepted in any letter case. An invalid value is an
error that lists the modes, and so is a repeated option. Mode any (the default) keeps using
configuration paths outside every folder, as before. These scenarios are tested by
`DictionaryMark_FileAccess_ModeLetterCase_Accepted`, `DictionaryMark_FileAccess_InvalidMode_ReturnsErrorListingModes`,
`DictionaryMark_FileAccess_RepeatedOption_ReturnsError` and
`DictionaryMark_FileAccess_AnyMode_ConfigPathsOutsideFoldersUsed`.

**FileAccessAllowRoot**: `--allow-root` replaces the default folders, so a configuration in the working
directory is denied when only another folder is allowed. A relative `--allow-root` is resolved from the
working directory even with a `--root` that also has a folder of that name. A missing folder is an error,
and so is `--allow-root` with `--file-access any` or `no-symlinks`. These scenarios are tested by
`DictionaryMark_FileAccess_AllowRoot_ReplacesDefaultFolders`,
`DictionaryMark_FileAccess_AllowRootRelative_ResolvedAgainstWorkingDirectory`,
`DictionaryMark_FileAccess_AllowRootMissing_ReturnsError` and
`DictionaryMark_FileAccess_AllowRootWithAnyOrNoSymlinks_ReturnsConflictError`.

**FileAccessDefaultFolders**: Under roots without `--allow-root`, an input in the working directory and
an output in the configuration folder (two separate folders) are allowed, and an input in a third folder
is denied. These scenarios are tested by
`DictionaryMark_FileAccess_RootsDefaultFolders_WorkingAndConfigFoldersAllowed` and
`DictionaryMark_FileAccess_RootsDefaultFolders_OtherFolderDenied`.

**FileAccessJudgedPaths**: Command-line `--input`, `--output`, `--log` and `--results` outside the
allowed folder are used, while a configuration input outside is denied as written. These scenarios are
tested by `DictionaryMark_FileAccess_CommandLinePathsOutsideFolders_NotJudged` and
`DictionaryMark_FileAccess_ConfigInputOutsideFolders_Denied`.

**FileAccessRoots**: Under roots these escapes are denied:

- a `..` escape;
- a `${VAR}` escape of the output;
- a `root` key naming an outside folder;
- a folder link inside the configuration folder that leads out (judged by its real path, which the
  message names).

A glob whose literal folder is outside the allowed folders (`../shared/*.yaml`) is denied as written before
that folder is listed. Inputs and outputs inside the allowed folders still generate. These scenarios are tested
by `DictionaryMark_FileAccess_ParentSegmentEscape_Denied`, `DictionaryMark_FileAccess_EnvironmentVariableEscape_Denied`,
`DictionaryMark_FileAccess_RootKeyDoesNotWiden_Denied`, `DictionaryMark_FileAccess_InputLinkLeadingOut_Denied`,
`DictionaryMark_FileAccess_RootsGlobFolderOutside_Denied` and `DictionaryMark_FileAccess_InsideFolders_Generated`.

**FileAccessNoSymlinks**: Under no-symlinks an input through a folder link below the configuration
folder is denied, even when the link stays inside, and so is a glob whose literal folder is such a link
(before the folder is listed, naming the folder), while a configuration file reached through a link
above its folder generates. These scenarios are tested by
`DictionaryMark_FileAccess_NoSymlinksInputThroughLink_Denied`,
`DictionaryMark_FileAccess_NoSymlinksGlobThroughLink_Denied` and
`DictionaryMark_FileAccess_NoSymlinksLinkAboveConfigFolder_Allowed`.

**FileAccessWrites**: An output outside the allowed folders is denied, and its missing folder is not
created. An output whose folder is a link leading out is denied, and no folder is created behind it. An
existing output that is a file symbolic link leading out is denied, and its target keeps its content.
These scenarios are tested by `DictionaryMark_FileAccess_OutputOutsideFolders_DeniedNothingWritten`,
`DictionaryMark_FileAccess_OutputThroughLinkedFolder_Denied` and
`DictionaryMark_FileAccess_OutputExistingSymlinkLeadingOut_Denied`.

**FileAccessNeverAccessed**: In the default mode the never-accessed list still applies:

- a URL input and an input written as a UNC path are denied;
- a device output (`NUL` on Windows, `/dev/null` elsewhere) is denied;
- on Linux and macOS, an input under `/dev` (`/dev/null`) is denied with the device reason (skipped on
  Windows);
- an output whose name contains a colon (`out.md:hidden`) is denied on every platform, and nothing is created;
- a folder at the output path is denied;
- an input or output whose `${VAR}` expands to `//nonexistent.invalid/share` or `a:b` is denied by its expanded
  text, the message showing it as written, and nothing is created;
- on Linux and macOS, a file `terms:v1.yaml` matched by a glob is denied with the colon reason (skipped on
  Windows, where a colon in a file name is a stream separator).

These scenarios are tested by `DictionaryMark_FileAccess_UrlInput_Denied`,
`DictionaryMark_FileAccess_UncInput_Denied`, `DictionaryMark_FileAccess_DeviceOutput_Denied`,
`DictionaryMark_FileAccess_InputUnderDev_Denied`,
`DictionaryMark_FileAccess_ColonOutput_Denied`, `DictionaryMark_FileAccess_DirectoryAsOutput_Denied`,
`DictionaryMark_FileAccess_VariableExpandingToNeverAccessed_Denied` and
`DictionaryMark_FileAccess_ColonGlobMatch_Denied`.

**FileAccessConfigOnShare** (supplementary, not requirement evidence): On Windows, a configuration file with
relative paths, reached through the administrative share `\\localhost\{drive}$`, runs in the default mode
and writes its output. The test needs an administrator account and the Server service and is skipped with a
reason when the share is not reachable. This scenario is tested by
`DictionaryMark_FileAccess_ConfigOnShare_RelativePathsAllowed`.

**FileAccessErrors**: A denied job is reported as `Error: table job 2 ('../b.md'): file-access-denied:
'../b.md': ...`, the other jobs write their outputs, the summary counts the failure, and the exit code is
non-zero. This scenario is tested by `DictionaryMark_FileAccess_DeniedJob_OtherJobsRunAndExitNonZero`.
