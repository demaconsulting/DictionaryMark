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
  `TemporaryDirectory`. Nothing is mocked, except that one test passes a recording folder judgment to
  the internal `Load(path, judgeRootFolder)` overload to show when the file-access folder check runs.

#### Test Environment

Tests use `TemporaryDirectory` for automatic cleanup. Tests of `${VAR}` expansion set a uniquely
named environment variable and remove it in a `finally` block; the test class runs in the
`Sequential` collection.

#### Acceptance Criteria

- All unit tests in `ConfigFileTests.cs` pass with zero failures.
- All requirements linked to the `ConfigFile` unit have at least one passing test scenario.
- No tests may be skipped or marked as expected failures, except the platform-specific tests named below
  (the `/dev` root on Linux and macOS, the symbolic link to a share on Windows), which are skipped with a
  reason on other platforms.

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

**ConfigFile_Load_RootLinkToMissingFolder_ThrowsNamingFile** (all platforms; skipped with a reason only where
neither a folder link nor a junction can be created): A `root` key that names a folder link (on Windows a junction
when symbolic links are not permitted) whose target was removed is rejected as
`Config file '...': Directory '...' specified by root key 'lnk' does not exist.`. Existence comes from the
folder's open that judged it, not from a by-name probe, which would accept a dangling junction on Windows. This
scenario is tested by the test of the same name.

**ConfigFile_Load_RootNamingFile_ThrowsNamingFile**: A `root` key that names an existing file is rejected with the
same missing-directory message on every platform; on macOS this covers the open with `O_DIRECTORY`, which fails
with `ENOTDIR` for a file. This scenario is tested by the test of the same name.

**ConfigFile_Load_RootInvalidCharacter_ThrowsNamingFile** and
**ConfigFile_Load_PathInvalidCharacter_ThrowsNamingFile**: A `root` value and a configuration file
path containing a null character (invalid on every platform) are rejected as
`InvalidOperationException` with the `Config file '...': ` prefix instead of escaping as a raw
path-validation exception. These scenarios are tested by the tests of the same names.

**ConfigFile_Load_RootInvalidValue_Throws**: A sequence, a mapping and an empty string `root` are
rejected with `key 'root' must be a non-empty string`. This scenario is tested by
`ConfigFile_Load_RootInvalidValue_Throws`.

**ConfigFile_Load_RootInTableJob_ThrowsNamingJob**: A `root` key inside a job is rejected with
`table job 1: key 'root' is only allowed at the top level`. This scenario is tested by
`ConfigFile_Load_RootInTableJob_ThrowsNamingJob`.

**ConfigFile_Load_NoRoot_RootDirectoryIsNull**: Without a `root` key, `RootDirectory` is `null`.
This scenario is tested by `ConfigFile_Load_NoRoot_RootDirectoryIsNull`.

**ConfigFile_Load_RootNamingNeverAccessedPath_ThrowsFileAccessDenied**: A `root` key naming
`//server/share`, `\\server\share`, an `http:` URL, `\\.\pipe\x` or `docs:stream` (a colon) is rejected with
`Config file '{path}': file-access-denied: root '{value}': {reason}` before the directory is probed.
This scenario is tested by `ConfigFile_Load_RootNamingNeverAccessedPath_ThrowsFileAccessDenied`.

**ConfigFile_Load_RootWithWildcard_RejectedBeforeFolderCheck**: A `root` key `sub*`, `sub?` or
`//server/share*` is rejected with exactly `Config file '{path}': Root key '{value}' must not contain wildcard
characters.`, and a recording folder judgment passed to the internal `Load` overload is never called, so the
wildcard check comes before any file-access folder check, even for a value also on the never-accessed list.
This scenario is tested by `ConfigFile_Load_RootWithWildcard_RejectedBeforeFolderCheck`.

**ConfigFile_Load_RootUnderDev_ThrowsFileAccessDenied** (Linux and macOS): A `root` key `/dev` is rejected with
`Config file '{path}': file-access-denied: root '/dev': it is a device path, which is never accessed`. This
scenario is tested by `ConfigFile_Load_RootUnderDev_ThrowsFileAccessDenied`.

**ConfigFile_Load_RootThroughSymlinkToShare_ThrowsFileAccessDenied** (Windows, supplementary, not requirement
evidence): A `root` key that passes through a real symbolic link to an unreachable share is rejected with the
network-link reason before its existence is checked, without contacting the share. It needs the symbolic-link
privilege and is skipped with a reason without it. The walk itself is requirement evidence through
`FileAccessPolicy_DescribeNeverAccessedFolder_LinkToShare_ReturnsReason`.
