package DictionaryMark {
    part def SelfTestSubsystem {
        doc /* Self-validation test runner subsystem. */

        comment testRef /* Test: test/DemaConsulting.DictionaryMark.Tests/SelfTest/SelfTestSubsystemTests.cs */
        comment designRef /* Design: docs/design/dictionary-mark/self-test.md */
        comment verificationRef /* Verification: docs/verification/dictionary-mark/self-test.md */
        comment reqRef /* Requirements: docs/reqstream/dictionary-mark/self-test.yaml */

        part validation : Validation;
    }
}
