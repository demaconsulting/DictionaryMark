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

- `RunProcess` runs a system tool and waits for it. An internal overload takes a time limit, kills the
  tool and its child processes when the limit is exceeded (reporting failure), reads standard output and
  error asynchronously so a full pipe cannot block the tool, and returns what the tool printed.

Its correctness is demonstrated transitively by the link, junction and pipe tests in
`FileAccessPolicyTests.cs`, `RealPathTests.cs` and `IntegrationTests.cs`. A link that was not created
makes a test skip, never pass, because the helper either creates the link or calls `Assert.Skip`.

The `CaseSensitiveFolder` class in `test/DemaConsulting.DictionaryMark.Tests/Helpers/CaseSensitiveFolder.cs`
makes an empty folder, below the user's temporary folder, in which names that differ only in letter case are
different entries, for the letter-case tests in `FileAccessPolicyTests.cs` and `RealPathTests.cs`:

- On Linux it is an ordinary folder, because Linux file systems distinguish case.
- On Windows it is an empty folder with per-folder case sensitivity set by
  `fsutil.exe file setCaseSensitiveInfo <folder> enable` (time limit 60 seconds). `fsutil` exits with 0 even
  when it fails (for example for a folder that is not empty, or one whose permissions do not allow it, as in a
  build folder inheriting the permissions of a drive root), so its exit code is not trusted.
- On macOS it is a folder on a 32 MB case-sensitive APFS disk image, created with `hdiutil create` and attached
  with `hdiutil attach -nobrowse -noverify -noautoopen -mountpoint` (time limit 120 seconds each). Creating,
  attaching and detaching are serialized by a lock, so test classes running in parallel do not race the disk
  arbitration service. Disposal detaches the image, retrying with `-force`.

On every platform, disposal (after detaching any macOS image) first removes folder links (junctions and symbolic
links) without following them, because a .NET recursive delete of a folder that holds a Windows junction fails
with access denied for a non-administrator and leaves the emptied folders behind. It then deletes the temporary
folder, retrying up to three times with a short delay, and never throws, so no folder is left below the user's
temporary folder.

Every result is confirmed by `IsCaseSensitive`, a behavioral probe: a folder `case-probe` is created and must
not be found as `CASE-PROBE`. The tests that need the opposite (a temporary folder that ignores case) use the
same probe to skip where it distinguishes case. When a case-sensitive folder cannot be made (a temporary folder
that cannot be written, a tool that is missing, fails or exceeds its time limit, or a probe that finds case
ignored), the helper cleans up and skips
the test with `Assert.Skip` and the reason, never failing it and never letting it pass without the check it
exists for. Its correctness is demonstrated transitively by the case-variant tests, which fail when the
real-path comparison ignores letter case.

### Test Environment

N/A - `TestHelpers` is test-only infrastructure.

### Acceptance Criteria

N/A - `TestHelpers` is test-only infrastructure and carries no independent verification
requirements. Its reliability is demonstrated by the passing integration tests in
`IntegrationTests.cs` that depend on it.

### Test Scenarios

N/A - `TestHelpers` is test-only infrastructure. All scenarios that exercise the tool via
`Runner` are defined in `IntegrationTests.cs` under the system-level verification.
