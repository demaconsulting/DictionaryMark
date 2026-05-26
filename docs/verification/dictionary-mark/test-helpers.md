## TestHelpers

### Verification Approach

N/A - `TestHelpers` is test-only infrastructure, not a production software unit under
verification. The `Runner` class in `test/DemaConsulting.DictionaryMark.Tests/Helpers/Runner.cs`
provides a shared helper that launches DictionaryMark as an external operating-system process and
returns the exit code. It exposes two overloads: one that captures stdout and stderr as separate
strings, and a convenience overload that concatenates them (stdout before stderr) into a single
string. Its correctness is demonstrated transitively: every integration test that calls `Runner.Run`
and correctly asserts on process output or exit code implicitly proves that `Runner` launched the
process, drained both output streams without deadlock, and returned the exit code faithfully. No
dedicated tests exist for `Runner` itself.

### Test Environment

N/A - `TestHelpers` is test-only infrastructure.

### Acceptance Criteria

N/A - `TestHelpers` is test-only infrastructure and carries no independent verification
requirements. Its reliability is demonstrated by the passing integration tests in
`IntegrationTests.cs` that depend on it.

### Test Scenarios

N/A - `TestHelpers` is test-only infrastructure. All scenarios that exercise the tool via
`Runner` are defined in `IntegrationTests.cs` under the system-level verification.
