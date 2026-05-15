## DictionaryMark Test Helpers Verification

This document describes the shared test helper utilities used across the
DictionaryMark test suite.

### Purpose

The `Helpers/` folder in the test project contains shared infrastructure
used by multiple test classes. These helpers are not software units under
verification themselves; they support the verification of other units by
providing consistent, reliable test scaffolding.

### TemporaryDirectory

`TemporaryDirectory` is a disposable helper that creates a uniquely-named
subdirectory under `Environment.CurrentDirectory` for use within a single
test, then deletes it on disposal.

#### Rationale

Using `Environment.CurrentDirectory` as the base — rather than
`Path.GetTempPath()` — avoids a macOS-specific path resolution issue where
`/tmp` is a symbolic link to `/private/tmp`. When `Environment.CurrentDirectory`
is set to a path under `/tmp`, the OS resolves the symlink and returns the
`/private/tmp/...` form, causing string comparisons against the original
`/tmp/...` path to fail. Creating temporary directories under the test working
directory sidesteps this entirely.

#### Usage

Tests obtain a `TemporaryDirectory` with a `using` declaration. The directory
is deleted automatically when the test completes, whether it passes or fails:

```csharp
using var tmp = new TemporaryDirectory();
var filePath = tmp.GetFilePath("input.yaml");
File.WriteAllText(filePath, "key: value");
// ... use filePath in the test ...
```

#### Members

- **`DirectoryPath`**: Full path to the temporary directory.
- **`GetFilePath(fileName)`**: Returns the full path to a named file within the directory.
- **`Dispose()`**: Deletes the directory and all its contents; ignores cleanup errors.

### Adoption Across the Test Suite

`TemporaryDirectory` is used in multiple test classes that create temporary files
or directories, including:

- **`Utilities/GlobMatcherTests.cs`**: Provides directories for glob pattern matching tests.
- **`Utilities/UtilitiesSubsystemTests.cs`**: Provides directories for path-helper and GlobMatcher subsystem tests.
- **`Cli/CliSubsystemTests.cs`**: Provides directories for results and log file output tests.
- **`SelfTest/SelfTestSubsystemTests.cs`**: Provides directories for validation result file tests.
- **`SelfTest/ValidationTests.cs`**: Provides directories for TRX, XML, and log output tests.
- **`IntegrationTests.cs`**: Provides directories for YAML input, output, and results file tests.
- **`ProgramTests.cs`**: Provides directories for command input-file generation tests.
- **`Dictionary/DictionaryGeneratorTests.cs`**: Provides directories for YAML input and output file tests.
