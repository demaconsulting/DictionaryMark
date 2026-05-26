### YamlDictionaryLoader

#### Verification Approach

`YamlDictionaryLoader` is a `static` class whose behavior depends on the file system and the
`YamlDotNet` library. Tests create temporary YAML files with controlled content using
`Path.GetTempFileName`, invoke `YamlDictionaryLoader.Load`, and assert on the returned entry list or the
expected exception type. The file system and `YamlDotNet` run with their real implementations; no
mocking or stubbing is used.

#### Test Environment

Tests create temporary files using `Path.GetTempFileName` and delete them in `finally` blocks after each
test. No other environment setup beyond the standard test runner is required.

#### Acceptance Criteria

- All unit tests in `YamlDictionaryLoaderTests.cs` pass with zero failures.
- No tests are skipped or marked as expected failures.

#### Test Scenarios

**YamlDictionaryLoader_Load_ValidFlatYaml_ReturnsEntries**: Verifies that a YAML file containing two
flat key-value pairs is loaded into two `DictionaryEntry` objects in file order with correct term and
definition values, confirming the primary success path. This scenario is tested by
`YamlDictionaryLoader_Load_ValidFlatYaml_ReturnsEntries`.

**YamlDictionaryLoader_Load_EmptyFile_ReturnsEmptyList**: Verifies that loading an empty YAML file
returns an empty list rather than throwing, confirming that the absence of entries is a valid input.
This scenario is tested by `YamlDictionaryLoader_Load_EmptyFile_ReturnsEmptyList`.

**YamlDictionaryLoader_Load_NonExistentFile_ThrowsFileNotFoundException**: Verifies that loading a
path that does not exist on disk causes `Load` to throw `FileNotFoundException`, confirming that I/O
errors from the file system are surfaced to the caller as documented. This scenario is tested by
`YamlDictionaryLoader_Load_NonExistentFile_ThrowsFileNotFoundException`.

**YamlDictionaryLoader_Load_NestedYaml_ThrowsInvalidOperationException**: Verifies that a YAML file
containing a nested mapping structure (a value that is itself a mapping) causes `Load` to throw
`InvalidOperationException`, confirming that non-scalar values are rejected as invalid structure.
This scenario is tested by `YamlDictionaryLoader_Load_NestedYaml_ThrowsInvalidOperationException`.

**YamlDictionaryLoader_Load_ListYaml_ThrowsInvalidOperationException**: Verifies that a YAML file
whose root node is a sequence rather than a mapping causes `Load` to throw `InvalidOperationException`,
confirming that non-mapping root nodes are rejected. This scenario is tested by
`YamlDictionaryLoader_Load_ListYaml_ThrowsInvalidOperationException`.

**YamlDictionaryLoader_Load_SingleEntry_ReturnsOneEntry**: Verifies that a YAML file containing a
single key-value pair returns a list with exactly one `DictionaryEntry` bearing the correct term and
definition, confirming correct parsing at the minimum non-empty input size. This scenario is tested by
`YamlDictionaryLoader_Load_SingleEntry_ReturnsOneEntry`.

**YamlDictionaryLoader_Load_NullFilePath_ThrowsArgumentNullException**: Verifies that passing `null`
as the `filePath` argument causes `Load` to throw `ArgumentNullException`, confirming that the method
enforces a non-null precondition. This scenario is tested by
`YamlDictionaryLoader_Load_NullFilePath_ThrowsArgumentNullException`.

**YamlDictionaryLoader_Load_DuplicateKey_ThrowsInvalidOperationException**: Verifies that a YAML file
containing two entries with the same key (case-insensitive, e.g., `"Term1"` and `"term1"`) causes `Load`
to throw `InvalidOperationException`, confirming that duplicate keys within the same file are rejected.
This scenario is tested by `YamlDictionaryLoader_Load_DuplicateKey_ThrowsInvalidOperationException`.
