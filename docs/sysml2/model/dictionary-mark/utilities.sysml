package DictionaryMark {
    part def UtilitiesSubsystem {
        doc /* Shared, cross-cutting helper utilities. */

        comment testRef /* Test: test/DemaConsulting.DictionaryMark.Tests/Utilities/UtilitiesSubsystemTests.cs */
        comment designRef /* Design: docs/design/dictionary-mark/utilities.md */
        comment verificationRef /* Verification: docs/verification/dictionary-mark/utilities.md */
        comment reqRef /* Requirements: docs/reqstream/dictionary-mark/utilities.yaml */

        part globMatcher : GlobMatcher;
        part pathHelpers : PathHelpers;
        part temporaryDirectory : TemporaryDirectory;
    }
}
