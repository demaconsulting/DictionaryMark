### ConfigFile

The `ConfigFile` unit is verified with unit tests in `ConfigFileTests.cs`. Tests write
configuration files into temporary directories, call `ConfigFile.Load`, and assert on the
loaded defaults, root directory and table jobs, or on the exception message.

#### Verification Approach

`ConfigFile` reads real files and uses its documented dependencies directly:

- **YamlDotNet** — the real parser is used, so malformed YAML produces the real `YamlException`
  that `ConfigFile` must wrap.
- **PathResolver** — the real resolver is used for the `root` key, so the root tests confirm
  `${VAR}` expansion, `..` normalization and the existence check through `ConfigFile`.
- **File system** — configuration files and root directories are created in a
  `TemporaryDirectory`. Nothing is mocked.

#### Test Environment

Tests use `TemporaryDirectory` for automatic cleanup. Tests of `${VAR}` expansion set a uniquely
named environment variable and remove it in a `finally` block; the test class runs in the
`Sequential` collection.

#### Acceptance Criteria

- All unit tests in `ConfigFileTests.cs` pass with zero failures.
- All requirements linked to the `ConfigFile` unit have at least one passing test scenario.
- No tests may be skipped or marked as expected failures.

#### Test Scenarios

**ConfigFile_Load_DefaultsAndTables_ParsesAllKeys**: A file using every top-level formatting key
(with `format: TABLE` to check case-insensitivity) and one job overriding `format`, `kind` and
`column-widths` is loaded; every value, the file path, the base directory and the job index must
be read. This scenario is tested by `ConfigFile_Load_DefaultsAndTables_ParsesAllKeys`.

**ConfigFile_Load_InputAsString_ReturnsSinglePattern** and
**ConfigFile_Load_InputAsSequence_ReturnsAllPatterns**: A string input gives one pattern kept as
written (`../shared/*.yaml`); a sequence input gives every pattern in order; jobs are numbered from

1. These scenarios are tested by the tests of the same names.

**ConfigFile_Load_DefinitionHeaderAlias_IsAccepted** and
**ConfigFile_Load_BothDefHeaderAliases_Throws**: `definition-header` sets the definition header,
and giving both aliases in one mapping is an error naming both keys. These scenarios are tested by
the tests of the same names.

**ConfigFile_Load_MissingFile_ThrowsNamingFile**, **ConfigFile_Load_InvalidYaml_ThrowsNamingFile**,
**ConfigFile_Load_NonMappingRoot_Throws** and **ConfigFile_Load_EmptyFile_Throws**: A missing
file, malformed YAML, a sequence root and an empty file are each rejected with a message that
starts with `Config file '{path}': `. These scenarios are tested by the tests of the same names.

**ConfigFile_Load_TablesNotSequence_Throws** and **ConfigFile_Load_TablesEmpty_Throws**: `tables`
as a mapping and as an empty sequence are rejected naming the key. These scenarios are tested by
the tests of the same names.

**ConfigFile_Load_JobNotMapping_ThrowsNamingJob**, **ConfigFile_Load_JobMissingInput_ThrowsNamingJob**
and **ConfigFile_Load_JobMissingOutput_ThrowsNamingJob**: A scalar job, a second job without
`input` and a job without `output` are rejected with `table job {n}: ` naming the job. These
scenarios are tested by the tests of the same names.

**ConfigFile_Load_DisallowedTopLevelKey_ThrowsNamingKey**: Each of `input`, `output`, `log`,
`results`, `validate`, `silent`, `version` and `help` at the top level is rejected naming the key.
This scenario is tested by `ConfigFile_Load_DisallowedTopLevelKey_ThrowsNamingKey`.

**ConfigFile_Load_UnknownTopLevelKey_ThrowsNamingKey** and
**ConfigFile_Load_UnknownJobKey_ThrowsNamingKeyAndJob**: An unknown top-level key and an unknown
job key (`log`) are rejected with exact messages naming the file, the job and the key. These
scenarios are tested by the tests of the same names.

**ConfigFile_Load_InvalidDepth_Throws** and **ConfigFile_Load_InvalidFormatSortKind_Throws**:
Depth values `0`, `7` and `x`, and unsupported `format`, `sort` and `kind` values, are rejected
naming the key (boundary values of the 1–6 range). These scenarios are tested by the tests of the
same names.

**ConfigFile_Load_ColumnWidthsNotSequence_Throws** and
**ConfigFile_Load_ColumnWidthsInvalidValues_Throws**: A comma-separated string, and the sequences
`[0, 1]`, `[-1, 2]`, `[1.5, 2]`, `[a, b]`, `[1]` and `[1, 2, 3, 4]`, are rejected naming the key.
These scenarios are tested by the tests of the same names.

**ConfigFile_Load_Root_ResolvedRelativeToConfigDirectory**,
**ConfigFile_Load_RootWithParentSegments_Normalized** and
**ConfigFile_Load_RootEnvironmentVariable_Expanded**: `root: docs`, `root: ../..` and
`root: ${VAR}` resolve to the full path of the existing directory, relative to the configuration
file. These scenarios are tested by the tests of the same names.

**ConfigFile_Load_RootUndefinedVariable_ThrowsNamingFile** and
**ConfigFile_Load_RootMissingDirectory_ThrowsNamingFile**: An undefined variable and a missing
directory are rejected with the file prefix and the resolver's message. These scenarios are tested
by the tests of the same names.

**ConfigFile_Load_RootInvalidValue_Throws**: A sequence, a mapping and an empty string `root` are
rejected with `key 'root' must be a non-empty string`. This scenario is tested by
`ConfigFile_Load_RootInvalidValue_Throws`.

**ConfigFile_Load_RootInTableJob_ThrowsNamingJob**: A `root` key inside a job is rejected with
`table job 1: key 'root' is only allowed at the top level`. This scenario is tested by
`ConfigFile_Load_RootInTableJob_ThrowsNamingJob`.

**ConfigFile_Load_NoRoot_RootDirectoryIsNull**: Without a `root` key, `RootDirectory` is `null`.
This scenario is tested by `ConfigFile_Load_NoRoot_RootDirectoryIsNull`.
