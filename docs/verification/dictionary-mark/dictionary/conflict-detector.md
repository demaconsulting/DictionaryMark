# ConflictDetector Verification

This document describes the unit-level verification design for the `ConflictDetector` unit.
It defines test scenarios, dependency usage, and requirement coverage for
`ConflictDetectorTests.cs`.

## Verification Approach

`ConflictDetector` is verified with unit tests in `ConflictDetectorTests.cs`. Tests construct
`DictionaryEntry` arrays with controlled term and definition values, invoke
`ConflictDetector.Detect`, and assert on the number and content of returned conflict messages.

## Dependencies

| Dependency        | Usage in Tests                                       |
| ----------------- | ---------------------------------------------------- |
| `DictionaryEntry` | Constructed inline with test term/definition values. |

## Test Scenarios

### ConflictDetector_Detect_NoEntries_ReturnsEmpty

**Scenario**: Empty entry collection passed to `Detect`.

**Expected**: Empty list returned.

### ConflictDetector_Detect_UniqueTerms_ReturnsEmpty

**Scenario**: Two entries with distinct terms.

**Expected**: Empty list returned.

### ConflictDetector_Detect_ExactDuplicate_ReturnsEmpty

**Scenario**: Two entries with identical term and identical definition.

**Expected**: Empty list (same term + same definition is deduplication, not a conflict).

### ConflictDetector_Detect_SameTermDifferentDefinition_ReturnsConflict

**Scenario**: Two entries with same term but different definitions.

**Expected**: One conflict message containing the term name.

### ConflictDetector_Detect_CaseInsensitiveConflict_Detected

**Scenario**: Two entries with `"term1"` and `"TERM1"` but different definitions.

**Expected**: One conflict reported (case-insensitive comparison).

### ConflictDetector_Detect_MultipleConflicts_ReturnsAll

**Scenario**: Four entries: two pairs of conflicting terms.

**Expected**: Two conflict messages returned.

## Requirements Coverage

- **`DictionaryMark-ConflictDetector-Detect`**: All `ConflictDetector_Detect_*` tests.
