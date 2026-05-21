### ConflictDetector Verification

This document describes the unit-level verification design for the `ConflictDetector` unit.
It defines test scenarios, dependency usage, and requirement coverage for
`ConflictDetectorTests.cs`.

#### Verification Strategy

`ConflictDetector` is verified with unit tests in `ConflictDetectorTests.cs`. Tests construct
`DictionaryEntry` arrays with controlled term and definition values, invoke
`ConflictDetector.Detect`, and assert on the number and content of returned conflict messages.

#### Dependencies

| Dependency        | Usage in Tests                                       |
| ----------------- | ---------------------------------------------------- |
| `DictionaryEntry` | Constructed inline with test term/definition values. |

#### Test Environment

N/A - standard test environment.

#### Acceptance Criteria

All unit tests in `ConflictDetectorTests.cs` pass; all requirements listed in the Requirements
Coverage section have at least one passing test scenario; no tests may be skipped or marked as
expected failures.

#### Test Scenarios

##### ConflictDetector_Detect_NoEntries_ReturnsEmpty

**Scenario**: Empty entry collection passed to `Detect`.

**Expected**: Empty list returned.

##### ConflictDetector_Detect_UniqueTerms_ReturnsEmpty

**Scenario**: Two entries with distinct terms.

**Expected**: Empty list returned.

##### ConflictDetector_Detect_ExactDuplicate_ReturnsEmpty

**Scenario**: Two entries with identical term and identical definition.

**Expected**: Empty list (same term + same definition is deduplication, not a conflict).

##### ConflictDetector_Detect_SameTermDifferentDefinition_ReturnsConflict

**Scenario**: Two entries with same term but different definitions.

**Expected**: One conflict message containing the term name.

##### ConflictDetector_Detect_CaseInsensitiveConflict_Detected

**Scenario**: Two entries with `"term1"` and `"TERM1"` but different definitions.

**Expected**: One conflict reported (case-insensitive comparison).

##### ConflictDetector_Detect_MultipleConflicts_ReturnsAll

**Scenario**: Four entries: two pairs of conflicting terms.

**Expected**: Two conflict messages returned.

##### ConflictDetector_Detect_NullEntries_ThrowsArgumentNullException

**Scenario**: `null` is passed as the `entries` argument.

**Expected**: `ArgumentNullException` is thrown.

#### Requirements Coverage

- **`DictionaryMark-ConflictDetector-Detect`**: ConflictDetector_Detect_NoEntries_ReturnsEmpty,
  ConflictDetector_Detect_UniqueTerms_ReturnsEmpty,
  ConflictDetector_Detect_ExactDuplicate_ReturnsEmpty,
  ConflictDetector_Detect_SameTermDifferentDefinition_ReturnsConflict,
  ConflictDetector_Detect_CaseInsensitiveConflict_Detected,
  ConflictDetector_Detect_MultipleConflicts_ReturnsAll.
- **`DictionaryMark-ConflictDetector-CaseInsensitive`**: ConflictDetector_Detect_CaseInsensitiveConflict_Detected.
- **`DictionaryMark-ConflictDetector-Deduplication`**: ConflictDetector_Detect_ExactDuplicate_ReturnsEmpty.
- **`DictionaryMark-ConflictDetector-NullRejection`**: ConflictDetector_Detect_NullEntries_ThrowsArgumentNullException.
