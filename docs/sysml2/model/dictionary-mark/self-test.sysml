package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def SelfTestSubsystem {
        doc /* Self-validation test runner subsystem. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/SelfTest/SelfTestSubsystemTests.cs");
            designFiles = ("docs/design/dictionary-mark/self-test.md");
            verificationFiles = ("docs/verification/dictionary-mark/self-test.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/self-test.yaml");
        }

        part validation : Validation;
    }
}
