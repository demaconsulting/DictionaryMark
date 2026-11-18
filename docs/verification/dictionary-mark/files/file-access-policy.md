### FileAccessPolicy

The `FileAccessPolicy` unit (`FileAccessPolicy` with its built-in policies, `FileAccessRequest` and
`FileAccessDecision`) is verified with unit tests in `FileAccessPolicyTests.cs`, adapted from CanvasNet's.

#### Verification Approach

The policies are asked directly (`Check`, and the internal `PreCheck` and `RequiresRealPath` through the assembly's
`InternalsVisibleTo` grant) with `FileAccessRequest`s that the test builds over real temporary folders, because roots
and anchors are compared in their real location: the real path in a request is the real location of a folder (from the
`RealPath` unit, a documented dependency) joined with the file name. A link that exists only in the request (a real
path that differs from the written one) needs no link on disk; the scenarios that need a real link above the anchor use
a directory link or, on Windows, a junction. The combination scenario uses named recording test policies
(`RecordingPolicy`) as parts. No mocking is needed.

#### Test Environment

- **Files**: per-test temporary directories, deleted by the test.
- **Operating-system scenarios**: the short-name scenario runs on Windows (it skips where the volume does not create
  short names) and is linked with a `windows@` filter; the case-rule scenario asserts the expectation of each system.

#### Acceptance Criteria

- All tests in `FileAccessPolicyTests.cs` pass with zero failures.
- Every `DictionaryMark-FileAccessPolicy-*` requirement has at least one passing test scenario.
- An allowed file outside a root, a denied file whose real path equals its written path below the anchor, a wrong
  name, or an accepted invalid argument constitutes a failure.

#### Test Scenarios

**FileAccessPolicy_AllowAll_AnyRequest_IsAllowedAndNamedAny**: Validates that the default policy allows every request,
is named `any`, and needs no real path. This scenario is tested by
`FileAccessPolicy_AllowAll_AnyRequest_IsAllowedAndNamedAny`.

**FileAccessPolicy_UnderRoots_RealPathInsideOrEqualToARoot_IsAllowed**: Validates that the roots policy allows a file
whose written and real paths lie inside a root, or equal it. This scenario is tested by
`FileAccessPolicy_UnderRoots_RealPathInsideOrEqualToARoot_IsAllowed`.

**FileAccessPolicy_UnderRoots_SiblingWithSharedPrefix_IsDenied**: Validates that a sibling folder whose name starts with
the root's name is outside the root. This scenario is tested by
`FileAccessPolicy_UnderRoots_SiblingWithSharedPrefix_IsDenied`.

**FileAccessPolicy_UnderRoots_RootCreatedAfterThePolicy_AllowsNothing**: Validates that a root that did not exist when
the policy was created allows nothing, even once it exists: its real location was not captured, and the policy never
resolves it by name later (fail closed). This scenario is tested by
`FileAccessPolicy_UnderRoots_RootCreatedAfterThePolicy_AllowsNothing`.

**FileAccessPolicy_UnderRoots_RealPathOutsideEveryRoot_IsDeniedWithReasonAndHint**: Validates that a file written inside
a root whose real path lies outside every root is denied with a reason and a hint. This scenario is tested by
`FileAccessPolicy_UnderRoots_RealPathOutsideEveryRoot_IsDeniedWithReasonAndHint`.

**FileAccessPolicy_UnderRoots_WrittenPathOutsideEveryRoot_IsDeniedByThePreCheck**: Validates that a path as written
outside every root is denied before the open, and also after it even when its real path lies inside a root (an outside
link that points in is refused). This scenario is tested by
`FileAccessPolicy_UnderRoots_WrittenPathOutsideEveryRoot_IsDeniedByThePreCheck`.

**FileAccessPolicy_UnderRoots_InvalidRoots_Throw**: Validates that the roots policy rejects missing, empty and invalid
roots when it is created, and on Windows a root in a device namespace other than an ordinary extended-length drive path.
This scenario is tested by `FileAccessPolicy_UnderRoots_InvalidRoots_Throw`.

**FileAccessPolicy_UnderRoots_RealPathInOtherCase_IsMatchedByItsStoredCase**: Validates that a real path spelled in
another letter case is brought to the case stored on disk before it is compared exactly with the root: where the
temporary folder is observed to be case-insensitive it names the root and is allowed; where it is observed to be
case-sensitive it names a folder that is not the root and is denied by the real-path check, although the path as
written is inside.
This scenario is tested by `FileAccessPolicy_UnderRoots_RealPathInOtherCase_IsMatchedByItsStoredCase`.

**FileAccessPolicy_NoSymlinks_RealPathMatchesTheWrittenPath_IsAllowed**: Validates that the no-symlinks policy allows a
file whose real path is the written path below the folder the reference starts from. This scenario is tested by
`FileAccessPolicy_NoSymlinks_RealPathMatchesTheWrittenPath_IsAllowed`.

**FileAccessPolicy_NoSymlinks_RealPathDiffersBelowTheAnchor_IsDenied**: Validates that the no-symlinks policy denies a
file whose real path differs from the written path below the folder the reference starts from. This scenario is tested
by `FileAccessPolicy_NoSymlinks_RealPathDiffersBelowTheAnchor_IsDenied`.

**FileAccessPolicy_NoSymlinks_LinkAboveTheAnchor_IsTrusted**: Validates that a link at or above the folder a reference
starts from is trusted by the no-symlinks policy. This scenario is tested by
`FileAccessPolicy_NoSymlinks_LinkAboveTheAnchor_IsTrusted`.

**FileAccessPolicy_NoSymlinks_WrittenPathInOtherCase_IsMatchedByItsStoredCase**: Validates that the no-symlinks policy
brings the written path below the anchor to the letter case stored on disk before it compares it exactly with the real
path: a file written in another letter case is allowed where the temporary folder is observed to be case-insensitive,
and denied where it is observed to be case-sensitive, because that spelling names no file there. This scenario is tested
by `FileAccessPolicy_NoSymlinks_WrittenPathInOtherCase_IsMatchedByItsStoredCase`.

**FileAccessPolicy_NoSymlinks_ShortNameSpelling_IsAllowed**: Validates on Windows that a path written with short (8.3)
names is compared in its long spelling, so it is not mistaken for a link. Skips where the volume does not create short
names. This scenario is tested by `FileAccessPolicy_NoSymlinks_ShortNameSpelling_IsAllowed`.

**FileAccessPolicy_Combine_FirstDenialWinsAndNamesAreJoined**: Validates that a combination asks its parts in order, the
first denial wins, names are joined with `+`, nested combinations are flattened, a single policy is returned as it is,
and the lexical pre-check and the need for a real path come from the parts. This scenario is tested by
`FileAccessPolicy_Combine_FirstDenialWinsAndNamesAreJoined`.

**FileAccessPolicy_Combine_InvalidArguments_Throw**: Validates that a combination rejects a missing, empty or
null-holding list. This scenario is tested by `FileAccessPolicy_Combine_InvalidArguments_Throw`.

**FileAccessDecision_Deny_KeepsReasonAndHintAndRejectsAnEmptyReason**: Validates that a decision keeps its reason and
hint, that the allow decision is one instance, and that a denial needs a reason. This scenario is tested by
`FileAccessDecision_Deny_KeepsReasonAndHintAndRejectsAnEmptyReason`.
