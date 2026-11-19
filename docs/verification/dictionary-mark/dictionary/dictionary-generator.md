### DictionaryGenerator

#### Verification Approach

`DictionaryGenerator` is a `static` class that orchestrates the full dictionary pipeline by coordinating
`GlobMatcher`, `YamlDictionaryLoader`, `ConflictDetector`, and `MarkdownFormatter`. Tests supply a
`Context` constructed from controlled argument arrays and write temporary YAML files to disk using
`TemporaryDirectory`. Console standard output and standard error are redirected with `StringWriter`
instances to capture generated output and error messages. All collaborating units (`GlobMatcher`,
`PathResolver`, `YamlDictionaryLoader`, `ConflictDetector`, and `MarkdownFormatter`) run with their real
implementations against real files; no mocking or stubbing is used. Multi-job scenarios build their jobs
through `Context` and `TableJob` (documented dependencies) from a real configuration file.

#### Test Environment

Tests write temporary YAML files using `TemporaryDirectory` for automatic cleanup after each test. The
test class is marked `[Collection("Sequential")]` to prevent concurrent Console stream redirection from
interfering between tests. No other environment setup is required beyond the standard test runner.

#### Acceptance Criteria

- All unit tests in `DictionaryGeneratorTests.cs` pass with zero failures.
- No tests are skipped or marked as expected failures.

#### Test Scenarios

**DictionaryGenerator_Generate_SingleYamlFile_WritesToStdout**: Verifies that `Generate` called with a
single YAML input file writes formatted Markdown containing the entry term to standard output and exits
with code 0, confirming the normal end-to-end pipeline execution. This scenario is tested by
`DictionaryGenerator_Generate_SingleYamlFile_WritesToStdout`.

**DictionaryGenerator_Generate_ConflictingEntries_ReportsError**: Verifies that when two input files
define the same term with different definitions, `Generate` writes an error message to standard error,
sets exit code 1, and produces no Markdown output, confirming the conflict-reporting early-exit path.
This scenario is tested by `DictionaryGenerator_Generate_ConflictingEntries_ReportsError`.

**DictionaryGenerator_Generate_NoInputPatterns_ReportsError**: Verifies that when no `--input` flags
are provided, `Generate` reports a "No input files found" error to standard error and sets exit code 1,
confirming the empty-pattern error path. This scenario is tested by
`DictionaryGenerator_Generate_NoInputPatterns_ReportsError`.

**DictionaryGenerator_Generate_OutputFile_WritesToFile**: Verifies that when an `--output` file path is
specified, `Generate` creates the output file containing formatted Markdown and exits with code 0,
confirming the file-output routing path as an alternative to writing to standard output. This scenario
is tested by `DictionaryGenerator_Generate_OutputFile_WritesToFile`.

**DictionaryGenerator_Generate_HiddenEntry_OmittedFromOutput**: A `visible: false` entry is omitted
from output. This scenario is tested by `DictionaryGenerator_Generate_HiddenEntry_OmittedFromOutput`.

**DictionaryGenerator_Generate_HiddenEntryWithoutDefinition_ReportsError**: A hidden entry without a
definition is an error, since a definition is required regardless of visibility. This scenario is
tested by `DictionaryGenerator_Generate_HiddenEntryWithoutDefinition_ReportsError`.

**DictionaryGenerator_Generate_DeprecatedKind_WritesTable**: `--kind deprecated` writes the
deprecated table. This scenario is tested by `DictionaryGenerator_Generate_DeprecatedKind_WritesTable`.

**DictionaryGenerator_Generate_DeprecatedKindCustomHeaders_AreUsed**: Custom deprecated headers are
used. This scenario is tested by `DictionaryGenerator_Generate_DeprecatedKindCustomHeaders_AreUsed`.

**DictionaryGenerator_Generate_MissingInputs_ReportErrors**: Unmatched globs and missing files or
directories are reported with distinct errors. This scenario is tested by
`DictionaryGenerator_Generate_MissingInputs_ReportErrors`.

**DictionaryGenerator_Generate_OneMissingPattern_Fails**: One unmatched pattern among several fails
the run. This scenario is tested by `DictionaryGenerator_Generate_OneMissingPattern_Fails`.

**DictionaryGenerator_Generate_MultipleJobs_WritesEachOutput**: Verifies that a configuration file with
two jobs writes both outputs, each with its own content and format. This scenario is tested by
`DictionaryGenerator_Generate_MultipleJobs_WritesEachOutput`.

**DictionaryGenerator_Generate_OneJobFails_ContinuesAndReportsJobLabel**: Verifies that when the first
job matches no files, its error is prefixed with `table job 1 ('a.md')`, it writes no output, and the
second job still writes its output. This scenario is tested by
`DictionaryGenerator_Generate_OneJobFails_ContinuesAndReportsJobLabel`.

**DictionaryGenerator_Generate_InvalidOutputPathCharacter_ContinuesWithLaterJob**: Verifies that when
the first job's output path contains a null character, the job is reported as an invalid output path
with its label, the summary `1 of 2 table jobs failed` is written, the exit code is 1, and the second
job still writes its output. This scenario is tested by
`DictionaryGenerator_Generate_InvalidOutputPathCharacter_ContinuesWithLaterJob`.

**DictionaryGenerator_Generate_InvalidInputPathCharacter_ContinuesWithLaterJob**: Verifies that when
the first job's input pattern contains a null character, the job is refused by the never-list control-character rule
before anything is listed (the character shown as `?`), with its label, the exit code is 1, and the second job still
writes its output. This
scenario is tested by `DictionaryGenerator_Generate_InvalidInputPathCharacter_ContinuesWithLaterJob`.

**DictionaryGenerator_Generate_OutputParentIsFile_ContinuesWithLaterJob**: Verifies that when the
first job's output lies beneath an existing file, the write failure is reported with the job label
and the output path as written, the exit code is 1, and the second job still writes its output.
This scenario is tested by `DictionaryGenerator_Generate_OutputParentIsFile_ContinuesWithLaterJob`.

**DictionaryGenerator_Generate_WildcardOutputFileNameOnWindows_ContinuesWithLaterJob**: Verifies on
Windows (the test returns early on other platforms, where `*` is a valid file-name character) that
an output path such as `out/*.md`, which normalizes cleanly but is rejected on write, fails only its
own job and the second job still writes its output. This scenario is tested by
`DictionaryGenerator_Generate_WildcardOutputFileNameOnWindows_ContinuesWithLaterJob`.

**DictionaryGenerator_Generate_MalformedYaml_ContinuesWithLaterJob**: Verifies that when the first job
reads a syntactically malformed YAML file, the job is reported as invalid YAML naming the file as written
(`Invalid YAML in file 'a.yaml':`, never its full path), writes
no output, the exit code is 1, and the second job still writes its output. This scenario is tested by
`DictionaryGenerator_Generate_MalformedYaml_ContinuesWithLaterJob`.

**DictionaryGenerator_Generate_MalformedDeprecatedYaml_ContinuesWithLaterJob**: Verifies the same
behavior for a malformed deprecated-terms file loaded with `kind: deprecated`. This scenario is tested
by `DictionaryGenerator_Generate_MalformedDeprecatedYaml_ContinuesWithLaterJob`.

An unreadable input file (`UnauthorizedAccessException`) is not tested, because file permissions cannot
be denied reliably on every platform (for example when tests run as root); it is handled by the same
code path as other read errors.

**DictionaryGenerator_Generate_JobsFailed_WritesSummaryAndExitCodeOne**: Verifies that when both jobs
fail, both are reported, the summary `2 of 2 table jobs failed` is written, and the exit code is 1.
This scenario is tested by `DictionaryGenerator_Generate_JobsFailed_WritesSummaryAndExitCodeOne`.

**DictionaryGenerator_Generate_OutputInMissingDirectory_CreatesDirectory**: Verifies that an `--output`
path inside two missing directories is written after the directories are created. This scenario is
tested by `DictionaryGenerator_Generate_OutputInMissingDirectory_CreatesDirectory`.

**DictionaryGenerator_Generate_JobBaseDirectory_ResolvesRelativeInputsAndOutput**: Verifies that a job's
`sub/../../shared/*.yaml` input and `../out/terms.md` output resolve against the job's base directory
(the configuration file's directory). This scenario is tested by
`DictionaryGenerator_Generate_JobBaseDirectory_ResolvesRelativeInputsAndOutput`.

**DictionaryGenerator_Generate_ConflictsDetectedPerJob**: Verifies that two conflicting dictionaries
used by separate jobs produce no error, while one job reading both reports the conflict prefixed with
its label. This scenario is tested by `DictionaryGenerator_Generate_ConflictsDetectedPerJob`.

**DictionaryGenerator_Generate_ColumnWidths_AppliedToOutput**: Verifies that `--column-widths 25,75`
reaches the generated table separator. This scenario is tested by
`DictionaryGenerator_Generate_ColumnWidths_AppliedToOutput`.

**DictionaryGenerator_Generate_ConfigInputOutsideRoots_FileAccessDenied**: Test that a config input outside the allowed
roots fails its job with file-access-denied. This scenario is tested by
`DictionaryGenerator_Generate_ConfigInputOutsideRoots_FileAccessDenied`.

**DictionaryGenerator_Generate_ConfigLiteralInputOutsideRoots_DeniedWithoutLookup**: Test that a literal config input
that does not exist, outside the allowed roots, fails its job with file-access-denied rather than a not-found error, so
its existence is never probed. This scenario is tested by
`DictionaryGenerator_Generate_ConfigLiteralInputOutsideRoots_DeniedWithoutLookup`.

**DictionaryGenerator_Generate_ConfigLiteralMissingInputInsideRoots_ReportsNotFound**: Test that a literal config input
inside the allowed roots that does not exist (in a folder that does not exist either) fails its job with
`Input file 'sub/missing.yaml' was not found.` (the path as written, not the full path), the resolver's outcome, and
writes no output. This scenario is tested by
`DictionaryGenerator_Generate_ConfigLiteralMissingInputInsideRoots_ReportsNotFound`.

**DictionaryGenerator_Generate_ConfigGlobMatchOutsideRoots_IsDeniedNotSkipped**: Test that a glob whose matches lie both
inside and outside the allowed roots fails its job: the match outside is denied rather than skipped, and the match
inside is not used alone. This scenario is tested by
`DictionaryGenerator_Generate_ConfigGlobMatchOutsideRoots_IsDeniedNotSkipped`.

**DictionaryGenerator_Generate_ConfigGlobMatchThroughLinkUnderNoSymlinks_FileAccessDenied**: Test that a glob match
reached through a directory link is denied under no-symlinks. This scenario is tested by
`DictionaryGenerator_Generate_ConfigGlobMatchThroughLinkUnderNoSymlinks_FileAccessDenied`.

**DictionaryGenerator_Generate_ConfigOutputOutsideRoots_DeniedAndNothingCreated**: Test that a config output outside the
allowed roots is denied and nothing is created. This scenario is tested by
`DictionaryGenerator_Generate_ConfigOutputOutsideRoots_DeniedAndNothingCreated`.

**DictionaryGenerator_Generate_ConfigNeverListOutputUnderAny_RefusedWithoutAccess**: Test that under the default mode a
config output that breaks a never-list rule (a colon outside a drive, and on Windows a network path) is refused before
anything is accessed or created. This scenario is tested by
`DictionaryGenerator_Generate_ConfigNeverListOutputUnderAny_RefusedWithoutAccess`.

**DictionaryGenerator_Generate_ConfigNetworkOutput_RefusedBeforeNormalization**: Test that a config output naming a
network share (`//dictionarymark-test.invalid/share/out.md` on every platform, and on Windows also the backslash
spelling) is refused by the network-path rule before anything is accessed or created, because the resolver sees the
path before a leading `//` can collapse to `/` on Linux and macOS. This scenario is tested by
`DictionaryGenerator_Generate_ConfigNetworkOutput_RefusedBeforeNormalization`.

**DictionaryGenerator_Generate_ConfigNetworkRootRelativeOutput_RefusedBeforeNormalization**: Test that a relative config
output under a `root` key naming a network share (`//dictionarymark-test.invalid/share`, and on Windows also
`\\dictionarymark-test.invalid\share`) is refused by the network-path rule on every platform, because the root is
joined with the output as text before normalization. This scenario is tested by
`DictionaryGenerator_Generate_ConfigNetworkRootRelativeOutput_RefusedBeforeNormalization`.

**DictionaryGenerator_Generate_ConfigInputOverSizeLimit_ReadError**: Test that a config input longer than the 16 MiB
size limit is an ordinary read error, `Failed to read file 'big.yaml': The file 'big.yaml' is larger than the allowed
size.`, naming the file as written and not by its full path. This scenario is tested by
`DictionaryGenerator_Generate_ConfigInputOverSizeLimit_ReadError`.

**DictionaryGenerator_Generate_ConfigInputOutsideRoots_MessageShowsWrittenPath**: Test that the denial of
`../shared/terms.yaml` reads exactly `file-access-denied: The file '../shared/terms.yaml' was denied by the file-access
policy 'roots'.` after the job label, and that neither the temporary folder nor `Path.GetTempPath()` appears. This
scenario is tested by `DictionaryGenerator_Generate_ConfigInputOutsideRoots_MessageShowsWrittenPath`.

**DictionaryGenerator_Generate_ConfigGlobMatchOutsideRoots_MessageShowsMatchRelativeToBase**: Test that the denied
match of `../*/terms-*.yaml` is shown as `../other/terms-b.yaml`, relative to the base directory, with no host path.
This scenario is tested by `DictionaryGenerator_Generate_ConfigGlobMatchOutsideRoots_MessageShowsMatchRelativeToBase`.

**DictionaryGenerator_Generate_ConfigOutputOutsideRoots_MessageShowsWrittenPath**: Test that the denial of the output
`../out/terms.md` shows it as written, with no host path, and that its folder is not created. This scenario is tested
by `DictionaryGenerator_Generate_ConfigOutputOutsideRoots_MessageShowsWrittenPath`.

**DictionaryGenerator_Generate_ConfigNetworkRootPattern_MessageShowsWrittenPattern**: Test that a relative input under
a `root` naming `//dictionarymark-test.invalid/share` is refused as
`Failed to resolve input pattern 'terms.yaml': The path 'terms.yaml' is not allowed: network paths are not permitted.`,
without the host name (the pattern as written, not the root joined with it). This scenario is tested by
`DictionaryGenerator_Generate_ConfigNetworkRootPattern_MessageShowsWrittenPattern`.

**DictionaryGenerator_Generate_RootRelocatedCommandLineInputDenied_MessageShowsWrittenPath**: Test that a command-line
`--input terms.yaml` that the configuration `root` key moves outside the allowed roots is denied as
`Error: file-access-denied: The file 'terms.yaml' was denied by the file-access policy 'roots'.` (no job label) with
no host path. This scenario is tested by
`DictionaryGenerator_Generate_RootRelocatedCommandLineInputDenied_MessageShowsWrittenPath`.

**DictionaryGenerator_Generate_ConfigInputsConflict_MessageShowsWrittenPaths**: Test that a conflict between two config
inputs names them as `a.yaml and b.yaml`, not by full path. This scenario is tested by
`DictionaryGenerator_Generate_ConfigInputsConflict_MessageShowsWrittenPaths`.

**DictionaryGenerator_Generate_ConfigOutputWriteFailure_MessageShowsWrittenPath**: Test that when the write of a config
output fails because the output path names an existing folder, the message starts
`Failed to write output file 'out.md':` and contains no host path (the operating system's quotation of the full path is
redacted). This scenario is tested by `DictionaryGenerator_Generate_ConfigOutputWriteFailure_MessageShowsWrittenPath`.

**DictionaryGenerator_Generate_TrustedAbsoluteInputMissing_MessageKeepsFullPath**: Test that a missing absolute
command-line input is still reported with its full path. This scenario is tested by
`DictionaryGenerator_Generate_TrustedAbsoluteInputMissing_MessageKeepsFullPath`.

**DictionaryGenerator_Generate_DeniedJob_OtherJobsRunAndExitCodeIsOne**: Test that a denied job fails only itself: the
other jobs run and the exit code is 1. This scenario is tested by
`DictionaryGenerator_Generate_DeniedJob_OtherJobsRunAndExitCodeIsOne`.

**DictionaryGenerator_Generate_CommandLineJob_IsNeverJudged**: Test that a command-line job whose paths no configuration
`root` key relocates (here absolute paths, with `--allow-root` naming another folder) is not judged by the file-access
policy. This scenario is tested by `DictionaryGenerator_Generate_CommandLineJob_IsNeverJudged`.

**DictionaryGenerator_Generate_ConfigRootRelocatedInputOutsideRoots_FileAccessDenied**: Test that a relative
`--input` that the configuration `root` key moves outside the allowed folder is denied with `Error: file-access-denied:`
and its content is not read. This scenario is tested by
`DictionaryGenerator_Generate_ConfigRootRelocatedInputOutsideRoots_FileAccessDenied`.

**DictionaryGenerator_Generate_ConfigRootRelocatedLiteralInputOutsideRoots_DeniedWithoutLookup**: Test that a relocated
literal input outside the allowed folder that does not exist is denied, not reported as not found. This scenario is
tested by `DictionaryGenerator_Generate_ConfigRootRelocatedLiteralInputOutsideRoots_DeniedWithoutLookup`.

**DictionaryGenerator_Generate_ConfigRootRelocatedGlobMatchOutsideRoots_IsDeniedNotSkipped**: Test that a relocated glob
with one match inside and one outside the allowed folder is denied and produces no output. This scenario is tested by
`DictionaryGenerator_Generate_ConfigRootRelocatedGlobMatchOutsideRoots_IsDeniedNotSkipped`.

**DictionaryGenerator_Generate_ConfigRootRelocatedOutputOutsideRoots_DeniedAndNothingCreated**: Test that a relative
`--output` that the `root` key moves outside the allowed folder is denied and no file or folder is created. This
scenario is tested by `DictionaryGenerator_Generate_ConfigRootRelocatedOutputOutsideRoots_DeniedAndNothingCreated`.

**DictionaryGenerator_Generate_ConfigRootRelocatedPathsInsideRoots_AreGenerated**: Test that relocated input and output
inside the allowed folder are generated. This scenario is tested by
`DictionaryGenerator_Generate_ConfigRootRelocatedPathsInsideRoots_AreGenerated`.

**DictionaryGenerator_Generate_ConfigRootRelocatedAbsolutePaths_StayTrusted**: Test that absolute inputs and output
outside the allowed folder stay trusted under a `root` key, also in one job with an allowed relocated relative input.
This scenario is tested by `DictionaryGenerator_Generate_ConfigRootRelocatedAbsolutePaths_StayTrusted`.

**DictionaryGenerator_Generate_RootFlagWithConfigRoot_PathsStayTrusted**: Test that `--root` keeps relative command-line
paths trusted even when the configuration file has a `root` key. This scenario is tested by
`DictionaryGenerator_Generate_RootFlagWithConfigRoot_PathsStayTrusted`.

**DictionaryGenerator_Generate_ConfigRootRelocatedNetworkRoot_RefusedBeforeNormalization**: Test that a relative
`--input` under a `root` key naming a network share is refused by the network-path rule before any lookup, in the
default mode. This scenario is tested by
`DictionaryGenerator_Generate_ConfigRootRelocatedNetworkRoot_RefusedBeforeNormalization`.

**DictionaryGenerator_Generate_ConfigRootRelocatedInputThroughLinkUnderNoSymlinks_FileAccessDenied**: Test that a
relocated input reached through a directory link is denied under `no-symlinks` (skipped where links cannot be created).
This scenario is tested by
`DictionaryGenerator_Generate_ConfigRootRelocatedInputThroughLinkUnderNoSymlinks_FileAccessDenied`.

**DictionaryGenerator_Generate_ConfigRootRelocatedInputUnderAny_IsRead**: Test that in the default mode a relocated
input outside the working directory is read, so the `root` key keeps working. This scenario is tested by
`DictionaryGenerator_Generate_ConfigRootRelocatedInputUnderAny_IsRead`.

**DictionaryGenerator_Generate_ConfigNetworkPatternForwardSlashes_RefusedBeforeListing**: Test that a config pattern
naming a network share with forward slashes is refused by the network-path rule before any folder is listed (not with
"matched no files"), writes nothing, and the other job still runs. This scenario is tested by
`DictionaryGenerator_Generate_ConfigNetworkPatternForwardSlashes_RefusedBeforeListing`.

**DictionaryGenerator_Generate_ConfigNetworkPatternBackslashesOnWindows_RefusedBeforeListing**: Test on Windows that a
config pattern naming a network share with backslashes is refused before any folder is listed, and the other job still
runs. This scenario is tested by
`DictionaryGenerator_Generate_ConfigNetworkPatternBackslashesOnWindows_RefusedBeforeListing`.

**DictionaryGenerator_Generate_ConfigUrlPattern_RefusedBeforeListing**: Test that a URL-like config pattern is refused
by the colon rule before any folder is listed, with the ordinary error rather than `file-access-denied`, and the other
job still runs. This scenario is tested by `DictionaryGenerator_Generate_ConfigUrlPattern_RefusedBeforeListing`.

**DictionaryGenerator_Generate_ConfigNetworkRootRelativeInput_RefusedBeforeNormalization**: Test that a relative config
input under a `root` key naming a network share is refused by the network-path rule on every platform before it is
looked up, because the root is joined with the pattern as text before normalization. This scenario is tested by
`DictionaryGenerator_Generate_ConfigNetworkRootRelativeInput_RefusedBeforeNormalization`.

**DictionaryGenerator_Generate_ConfigDeviceNamePatternOnWindows_RefusedBeforeListing**: Test on Windows that a config
pattern with a reserved device-name component (`NUL`, `C:/NUL`, `CON`, `AUX`, `COM1`, `LPT1`) is refused by the
device-name rule before any folder is listed, and the other job still runs. This scenario is tested by
`DictionaryGenerator_Generate_ConfigDeviceNamePatternOnWindows_RefusedBeforeListing`.
