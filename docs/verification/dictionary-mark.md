# DictionaryMark

## Verification Approach

System requirements are verified through integration tests in `IntegrationTests.cs` (located in
`test/DemaConsulting.DictionaryMark.Tests/`) that invoke the compiled DLL as an out-of-process
`dotnet` command. This approach exercises the full system stack — argument parsing, subsystem
dispatch, and output generation — without mocking any internal components. Each system
requirement maps to one or more named integration test scenarios. Integration tests that depend
on external files use the `TemporaryDirectory` test helper to create and automatically clean up
temporary files.

The built-in `help` command is verified by running the compiled tool with `help` arguments. The
command examples of the help cards are verified with the example runner of
`DemaConsulting.HelpCards`: it gives each card a fresh scratch folder that holds the card's saved
`source` examples, and calls a test delegate for each command example. The delegate copies the
input files of `test/DemaConsulting.DictionaryMark.Tests/TestData/HelpExamples/` into that folder
before every command, runs the compiled tool there with `Runner.RunInDirectory`, and the runner
compares the exit code and the output with the `exit` and `expect` values of the example.

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
- The `help` command lists the topics with exit code 0, and exits with code 2 for an unknown topic.
- Every command example in the help cards gives the exit code and output that its card states.

## Test Scenarios

**VersionDisplay**: When the `--version` flag is provided, the tool must print a valid semantic
version string and exit with code zero. This ensures the version requirement is satisfied and the
tool self-identifies correctly for audit trails.
This scenario is tested by `DictionaryMark_VersionFlag_Provided_OutputsVersion`.

**HelpDisplay**: When the `--help` flag is provided, the tool must print usage information
listing all key command-line options and exit with code zero. This ensures users and automated
pipelines can discover the tool's interface.
This scenario is tested by `DictionaryMark_HelpFlag_Provided_OutputsUsageInformation`.

**HelpTopics**: Invoke the tool with `help` as the sole argument; it must exit with code zero,
list every topic with its command, and print no banner. This ensures users and AI agents that have
only the installed tool can find the help topics.
This scenario is tested by `DictionaryMark_HelpTopics_HelpVerb_ListsTopics`.

**HelpUnknownTopic**: Invoke the tool with `help no-such-topic`; it must exit with code 2 and the
output must name the topic, so that scripts can tell a mistyped topic from a failed tool run.
This scenario is tested by `DictionaryMark_HelpTopics_UnknownTopic_ReturnsUsageError`.

**HelpExamples**: Build the catalog that the tool ships and run every command example of every card
against the compiled tool in a scratch folder that holds the card's saved source examples and the
files of `TestData/HelpExamples`; at least one command example must exist, and each must give the
exit code and output fragments that its card states.
This scenario is tested by `DictionaryMark_HelpExamples_AllCommandExamples_GiveStatedResults`.

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

**ConfigEnvironmentVariables**: `${VAR}` references in the `--config` path and, in the default file-access mode, in
the input and output paths of a job must be expanded.
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

**RootValidation**: A missing `--root` directory, a repeated or empty `--root`, and a `root` key inside a table job
must each be rejected with an error and exit code 1.
This scenario is tested by `DictionaryMark_Root_MissingDirectory_ReturnsError`,
`DictionaryMark_Root_SpecifiedTwice_ReturnsError` and `DictionaryMark_Config_RootInTableJob_ReturnsErrorNamingJob`.

**ConfigRootTextOnly**: A configuration `root` key is resolved as text and never probed. A `root` naming a folder that
does not exist must not be rejected when the file is loaded; its table must fail with `Input file '...' was not found.`
reported by the file-access resolver, and nothing may be created. A `root` naming a network share
(`//dictionarymark-test.invalid/share`, and on Windows also `\\dictionarymark-test.invalid\share`) must fail its table
through the resolver - refused as a network path on every platform, because the root is joined with the table's paths
as text before normalization - with no "does not exist" or "not found" error.
This scenario is tested by `DictionaryMark_Config_NonexistentRoot_JobFailsThroughResolver` and
`DictionaryMark_Config_NetworkRoot_JobFailsThroughResolverWithoutProbing`.

**FileAccess**: The file-access options must confine the inputs and outputs of configuration-file tables: with
`--allow-root` a table inside the folder is generated while a table whose input or output lies outside it is denied with
`file-access-denied` and writes nothing; `--file-access no-symlinks` must deny an input reached through a directory
link; `--file-access roots` alone must allow the working directory and the configuration file's folder; every glob match
must be judged (a match outside the folder fails its table); a literal input must be judged before it is looked up (a
missing file outside the folder is denied, while a missing file inside it is not found); an `--allow-root` folder named
in another letter case than the folder on disk must still allow the tables inside it where the temporary folder is
observed to be case-insensitive (skipped where it is observed to be case-sensitive); the default mode must still
refuse never-list paths, and
refuse a network input pattern before anything is listed; a denied table must fail alone with exit code 1; command-line
paths must not be judged when no configuration `root` key relocates them (see **ConfigRootRelocated**); the
configuration `root` key must never widen the allowed folders; and every mistake in the
options must be a usage error reported before any input is read. This scenario is tested by
`DictionaryMark_FileAccess_AllowRootAsProcess_ConfinesConfigJobs`,
`DictionaryMark_FileAccess_MistakesAsProcess_AreUsageErrors`,
`DictionaryMark_FileAccess_NoSymlinksAsProcess_DeniesLinkedConfigInput`,
`DictionaryMark_FileAccess_DefaultMode_NeverListRefusesConfigPaths`,
`DictionaryMark_FileAccess_NetworkInputPattern_RefusedBeforeListing`,
`DictionaryMark_FileAccess_CommandLinePaths_AreNotJudged`,
`DictionaryMark_FileAccess_DeniedJob_OtherJobsRunAndExitCodeIsOne`,
`DictionaryMark_FileAccess_RootsWithoutAllowRoot_AllowsWorkingAndConfigFolders`,
`DictionaryMark_FileAccess_GlobMatchOutsideRoots_IsDeniedNotSkipped`,
`DictionaryMark_FileAccess_LiteralConfigInputs_JudgedBeforeLookup`,
`DictionaryMark_FileAccess_AllowRootInOtherCase_AllowsConfigJobsInside` and
`DictionaryMark_FileAccess_ConfigRootKey_DoesNotWidenAllowedFolders`.

**ConfigRootRelocated**: A relative `--input` pattern or `--output` path that a configuration `root` key moves (no
`--root`, a configuration file with a `root` key and no `tables`) must be judged by the file-access mode exactly like a
table path. With `--allow-root` naming the configuration file's folder and a `root` naming a folder outside it, a
relative `--input` must be denied with `Error: file-access-denied:` and a non-zero exit code without its content being
read, and a relative `--output` (with an absolute, trusted input) must be denied before anything is written, not even
the folder; with a `root` inside the allowed folder both must be generated. Absolute `--input` and `--output` paths
outside the allowed folders, and relative paths when `--root` is given, must stay trusted and be generated. This
scenario is tested by `DictionaryMark_FileAccess_ConfigRootRelocatedInput_IsDenied`,
`DictionaryMark_FileAccess_ConfigRootRelocatedOutput_IsDenied`,
`DictionaryMark_FileAccess_ConfigRootRelocatedPathsInsideRoots_AreGenerated` and
`DictionaryMark_FileAccess_CommandLineRootOrAbsolutePaths_StayTrustedUnderConfigRoot`.

**ConfigEnvironmentRefused**: Under a restrictive mode (`roots`, `no-symlinks`, and `--allow-root`, which implies
`roots`), a `${VAR}` reference in a configuration table `output`, a table `input` or the `root` key must be refused
as a process before anything is read or created: exit code 1; an error naming the configuration file, the job
(`table job 1`) and the key (`key 'output'`, `key 'input'`, `key 'root'`); a variable that holds a distinctive secret
value must not appear anywhere in the output (nor in the `--log` file when it exists), nor may a file or folder
named by it be created; the error for a defined and for an undefined variable must be identical (and must not say
that the variable is not defined); a `root` reference must be refused for a configuration with tables and for a
command-line `--input` and `--output` that the key would relocate. With no file-access option and with
`--file-access any`, the same references must still be expanded and the output written. This scenario is tested by
`DictionaryMark_FileAccess_ConfigOutputEnvironmentVariableAsProcess_RefusedWithoutLeak`,
`DictionaryMark_FileAccess_ConfigInputEnvironmentVariableAsProcess_RefusedWithoutLeak`,
`DictionaryMark_FileAccess_ConfigRootEnvironmentVariableAsProcess_RefusedForTablesAndRelocatedCommandLine` and
`DictionaryMark_FileAccess_ConfigEnvironmentVariablesAsProcess_AnyModeStillExpands`.

**CommandLineEnvironmentExpanded**: Under `--file-access roots`, `${VAR}` in `--root` and `--output` on the command
line must still be expanded and the output written with exit code 0. This scenario is tested by
`DictionaryMark_FileAccess_CommandLineEnvironmentVariablesAsProcess_StillExpandedUnderRoots`.

**WrittenPathMessages**: With `--allow-root` naming the configuration file's folder, a table whose input is
`../other/more.yaml`, a table whose glob `../other/*.yaml` matches a file outside, a table whose literal input does
not exist, and a table whose output is `../out/d.md` must report their failures with the path as written
(`The file '../other/more.yaml' was denied`, `Input file 'missing.yaml' was not found.`, `The file '../out/d.md' was
denied`), keeping `file-access-denied` and the job label; neither the console output nor the `--log` file may contain
the temporary folder or the working directory. This scenario is tested by
`DictionaryMark_FileAccess_DeniedAndMissingConfigPathsAsProcess_ShowNoHostPath`.
