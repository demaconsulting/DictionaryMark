package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def Validation {
        doc /* Self-validation test runner. */

        metadata info : SoftwareInfo {
            description = "Self-validation test runner.";
            sourceFiles = ("src/DemaConsulting.DictionaryMark/SelfTest/Validation.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/SelfTest/ValidationTests.cs");
            designFiles = ("docs/design/dictionary-mark/self-test/validation.md");
            verificationFiles = ("docs/verification/dictionary-mark/self-test/validation.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/self-test/validation.yaml");
        }
    }
}
