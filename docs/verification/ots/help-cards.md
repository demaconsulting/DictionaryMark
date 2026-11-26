## HelpCards

### Verification Approach

DemaConsulting.HelpCards serves the built-in `dictionarymark help` command from the help cards
embedded in the tool assembly, and supplies the test helpers that validate the cards and run their
examples. It is a runtime library with no self-validation mode, so it is verified by tests in the
DictionaryMark test suite, which use the real package and the cards embedded in the tool assembly.
No mocking is applied. When this OTS dependency is updated, the full CI pipeline is re-executed and
all test scenarios must continue to pass before the update is accepted.

### Test Environment

Tests require no network access, because the cards are read from the tool assembly, and a writable
temporary directory, in which the example runner creates a scratch folder per card.

### Acceptance Criteria

The OTS integration is accepted when all linked tests pass with zero failures.

### Test Scenarios

**HelpCommand**: The help verb is answered from the embedded cards, in-process and by the built
tool. This scenario is tested by `HelpCommand_TryRun_HelpVerb_PrintsTopicIndexAndReturnsZero` and
`DictionaryMark_HelpTopics_HelpVerb_ListsTopics`.

**TestHelpers**: The validator accepts the shipped cards, and the example runner runs every command
example against the built tool. This scenario is tested by
`HelpCommand_CreateOptions_ShippedCatalog_PassesValidation` and
`DictionaryMark_HelpExamples_AllCommandExamples_GiveStatedResults`.
