## TestHelpers

### Verification Approach

N/A - `TestHelpers` is test-only infrastructure, not a production software unit under
verification. The `Runner` class in `test/DemaConsulting.DictionaryMark.Tests/Helpers/Runner.cs`
provides a shared helper that launches DictionaryMark as an external operating-system process and
returns the exit code. It exposes two overloads: one that captures stdout and stderr as separate
strings, and a convenience overload that concatenates them (stdout before stderr) into a single
string. A third method, `RunInDirectory`, starts the process in a given working directory and
returns the concatenated output; it has a distinct name so string arguments cannot bind to the
wrong overload, and it lets tests of working-directory-relative paths run without changing the
working directory of the test process. Its correctness is demonstrated transitively: every
integration test that calls `Runner.Run` or `Runner.RunInDirectory`
and correctly asserts on process output or exit code implicitly proves that `Runner` launched the
process, drained both output streams without deadlock, and returned the exit code faithfully. No
dedicated tests exist for `Runner` itself.

The `LinkSupport` class in `test/DemaConsulting.DictionaryMark.Tests/Helpers/LinkSupport.cs` creates
the symbolic links, junctions and named pipes that the file-access tests need:

- `CreateDirectoryLinkOrSkip` creates a folder symbolic link. On Windows it falls back to a junction
  (`cmd /c mklink /J`), which needs no privilege. When neither can be created, it skips the test with
  `Assert.Skip` and the reason.
- `CreateFileLinkOrSkip` creates a file symbolic link, or skips the test with the reason. A file link
  needs a privilege on Windows (for example Developer Mode or an administrator account) and has no
  fallback.
- `CreateFifoOrSkip` creates a named pipe with `mkfifo` on Linux and macOS, and skips the test
  elsewhere.

Its correctness is demonstrated transitively by the link, junction and pipe tests in
`FileAccessPolicyTests.cs`, `RealPathTests.cs` and `IntegrationTests.cs`. A link that was not created
makes a test skip, never pass, because the helper either creates the link or calls `Assert.Skip`.

### Test Environment

N/A - `TestHelpers` is test-only infrastructure.

### Acceptance Criteria

N/A - `TestHelpers` is test-only infrastructure and carries no independent verification
requirements. Its reliability is demonstrated by the passing integration tests in
`IntegrationTests.cs` that depend on it.

### Test Scenarios

N/A - `TestHelpers` is test-only infrastructure. All scenarios that exercise the tool via
`Runner` are defined in `IntegrationTests.cs` under the system-level verification.
