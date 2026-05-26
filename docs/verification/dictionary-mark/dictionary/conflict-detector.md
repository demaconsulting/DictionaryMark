### ConflictDetector

#### Verification Approach

`ConflictDetector` is a `static` class with no external dependencies beyond `DictionaryEntry`, which is a
simple immutable data model with no side effects. Tests construct `DictionaryEntry` arrays inline with
controlled term and definition values and pass them directly to `ConflictDetector.Detect`. No mocking or
stubbing is required because all behavior is self-contained within the method.

#### Test Environment

N/A - standard test environment.

#### Acceptance Criteria

- All unit tests in `ConflictDetectorTests.cs` pass with zero failures.
- No tests are skipped or marked as expected failures.

#### Test Scenarios

**ConflictDetector_Detect_NoEntries_ReturnsEmpty**: Verifies that passing an empty entry collection to
`Detect` produces an empty result list, confirming the base-case behavior when there is nothing to scan.
This scenario is tested by `ConflictDetector_Detect_NoEntries_ReturnsEmpty`.

**ConflictDetector_Detect_UniqueTerms_ReturnsEmpty**: Verifies that two entries with distinct terms
produce no conflict messages, confirming that the detector does not generate false positives when all
terms are unique. This scenario is tested by `ConflictDetector_Detect_UniqueTerms_ReturnsEmpty`.

**ConflictDetector_Detect_ExactDuplicate_ReturnsEmpty**: Verifies that two entries with the same term
and the same definition produce no conflict messages, confirming that identical deduplication is
explicitly allowed and is not treated as a conflict. This scenario is tested by
`ConflictDetector_Detect_ExactDuplicate_ReturnsEmpty`.

**ConflictDetector_Detect_SameTermDifferentDefinition_ReturnsConflict**: Verifies that two entries
sharing the same term but with different definitions produce exactly one conflict message containing the
term name, confirming the primary conflict-detection path. This scenario is tested by
`ConflictDetector_Detect_SameTermDifferentDefinition_ReturnsConflict`.

**ConflictDetector_Detect_CaseInsensitiveConflict_Detected**: Verifies that entries with the same term
in different letter cases (e.g., `"term1"` and `"TERM1"`) but with different definitions are reported as
conflicting, confirming that term comparison is case-insensitive. This scenario is tested by
`ConflictDetector_Detect_CaseInsensitiveConflict_Detected`.

**ConflictDetector_Detect_MultipleConflicts_ReturnsAll**: Verifies that when two independent conflicting
term pairs are present, both conflicts are reported, confirming that conflict detection is not limited to
the first conflict found. This scenario is tested by
`ConflictDetector_Detect_MultipleConflicts_ReturnsAll`.

**ConflictDetector_Detect_NullEntries_ThrowsArgumentNullException**: Verifies that passing `null` as
the `entries` argument causes `Detect` to throw `ArgumentNullException`, confirming that the method
enforces a non-null precondition. This scenario is tested by
`ConflictDetector_Detect_NullEntries_ThrowsArgumentNullException`.
