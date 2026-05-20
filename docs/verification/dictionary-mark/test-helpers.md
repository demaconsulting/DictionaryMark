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
  Throws `ArgumentException` if `fileName` contains path separator characters.
- **`Dispose()`**: Deletes the directory and all its contents; ignores cleanup errors.

### Runner

`Runner` is a static helper that launches an external process, captures its
combined stdout and stderr, and returns the exit code.

#### Runner Rationale

Integration tests need to run the DictionaryMark tool as a real operating-system
process to verify end-to-end behaviour. Without a shared helper, every test
class would duplicate the same `ProcessStartInfo` setup, redirection wiring, and
output-reading logic. `Runner` centralises this boilerplate and enforces three
important correctness properties:

- **Consistent output capture**: stdout and stderr are both redirected and
  concatenated, so assertions can inspect the full program output in one string.
- **Buffer-deadlock prevention**: both streams are drained asynchronously (via
  `ReadToEndAsync`) *before* `WaitForExit` is called. Reading from a redirected
  stream synchronously after the process has tried to write more than the OS
  pipe buffer can hold would cause the child to block on a write and the parent
  to block on `WaitForExit`, resulting in a deadlock. Starting both reads
  concurrently avoids this.
- **Shell-injection prevention**: arguments are added one at a time through
  `ProcessStartInfo.ArgumentList` rather than being concatenated into a command
  string, so no shell parsing occurs and argument values cannot inject extra
  commands or flags.

#### Runner Usage

Tests call `Runner.Run` as a static method and check the returned exit code:

```csharp
var exitCode = Runner.Run(
    out var output,
    "dotnet",
    _dllPath,
    "--version");

Assert.Equal(0, exitCode);
Assert.Matches(@"\d+\.\d+\.\d+", output);
```

A non-zero exit code indicates that the process reported a failure; the
`output` string contains whatever the program wrote to stdout or stderr.

#### Runner Members

- **`Run(out string output, string program, params string[] arguments)`**:
  Launches `program` with the supplied `arguments`, waits for the process to
  exit, and returns the integer exit code.
  - `output`: receives the concatenation of stdout and stderr once the process
    exits.
  - `program`: the executable name or full path (e.g. `"dotnet"`).
  - `arguments`: zero or more argument strings added individually to
    `ProcessStartInfo.ArgumentList`; no shell quoting or escaping is applied.
  - **Return value**: the process exit code — `0` conventionally indicates
    success; any other value indicates failure.
  - Throws `InvalidOperationException` if `Process.Start` returns `null`.
  - Throws `TimeoutException` if the process does not exit within 30 seconds.

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

`Runner` is used by the following test classes:

- **`IntegrationTests.cs`**: Launches `dotnet` with the DictionaryMark DLL path
  and various arguments to exercise the tool at the system boundary.
