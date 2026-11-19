### TableJob

The `TableJob` unit and its supporting `FormattingSettings` type are verified with unit tests in
`TableJobTests.cs`. Tests build formatting layers in memory, call `TableJob.Create`, and assert on
the resulting options, label and errors.

#### Verification Approach

`TableJob` is a pure function of its arguments: it performs no I/O, and its only collaborators are
the `FormattingSettings`, `MarkdownOptions` and enum value types, which are used directly. No
mocking is needed. `FormattingSettings.Over` is verified in the same test class because it is
the merge step that `TableJob.Create` relies on.

#### Test Environment

N/A - standard test environment. No files are created.

#### Acceptance Criteria

- All unit tests in `TableJobTests.cs` pass with zero failures.
- All requirements linked to the `TableJob` unit have at least one passing test scenario.
- No tests may be skipped or marked as expected failures.

#### Test Scenarios

**TableJob_Create_CommandLineLayer_OverridesJobAndDefaults**: Three layers all set the format and
section heading; the command-line values must win, and lower layers must fill the members the
command line does not set. This scenario is tested by
`TableJob_Create_CommandLineLayer_OverridesJobAndDefaults`.

**TableJob_Create_JobLayer_OverridesDefaults**: With no command-line layer, the job's definition
header must win over the default, and the default must fill the use header. This scenario is
tested by `TableJob_Create_JobLayer_OverridesDefaults`.

**TableJob_Create_NoLayers_UsesBuiltInDefaults**: With no layers, every option must have its
built-in default and no column widths. This scenario is tested by
`TableJob_Create_NoLayers_UsesBuiltInDefaults`.

**TableJob_Create_DeprecatedWithoutTermHeader_UsesNotUsed**: The deprecated kind must use
`Not used` when no layer sets a term header, and an explicit term header must still win. This
scenario is tested by `TableJob_Create_DeprecatedWithoutTermHeader_UsesNotUsed`.

**TableJob_Create_TermsWithThreeWidths_Throws** and **TableJob_Create_DeprecatedWithTwoWidths_Throws**:
Mismatched width counts for table output must be rejected with a message naming the job (when
labeled) and the required count. These scenarios are tested by the tests of the same names.

**TableJob_Create_BulletsWithMismatchedWidths_IgnoresWidths**: Bullet output with a mismatched
width count must not fail, and no widths must reach the options. This scenario is tested by
`TableJob_Create_BulletsWithMismatchedWidths_IgnoresWidths`.

**TableJob_Create_TableWithMatchingWidths_SetsColumnWidths**: Matching widths must reach the
options unchanged. This scenario is tested by
`TableJob_Create_TableWithMatchingWidths_SetsColumnWidths`.

**TableJob_Create_Label_NamesIndexAndOutput**: A configuration job must be labeled
`table job 3 ('docs/out.md')` and keep its index and inputs; the command-line job must have no
label or index. This scenario is tested by `TableJob_Create_Label_NamesIndexAndOutput`.

**TableJob_IsJudged_ConfigTableJob_JudgesEveryPath**: A configuration-table job must judge a relative and an absolute
path. This scenario is tested by `TableJob_IsJudged_ConfigTableJob_JudgesEveryPath`.

**TableJob_IsJudged_CommandLineJobUnderConfigRoot_JudgesRelativePathsOnly**: A command-line job whose base directory is
the configuration file's `root` key must judge a relative path and must not judge an absolute path. This scenario is
tested by `TableJob_IsJudged_CommandLineJobUnderConfigRoot_JudgesRelativePathsOnly`.

**TableJob_IsJudged_CommandLineJobWithoutConfigRoot_JudgesNothing**: A command-line job whose base directory is not the
configuration file's `root` key must judge no path and report no judged paths. This scenario is tested by
`TableJob_IsJudged_CommandLineJobWithoutConfigRoot_JudgesNothing`.

**TableJob_Create_NoInputs_Throws**: An empty input list must be rejected (defensive contract).
This scenario is tested by `TableJob_Create_NoInputs_Throws`.

**FormattingSettings_Over_NonNullWins**: Merging must take the higher layer's non-null members and
fill the rest from the lower layer, leaving members unset by both as `null`. This scenario is
tested by `FormattingSettings_Over_NonNullWins`.
