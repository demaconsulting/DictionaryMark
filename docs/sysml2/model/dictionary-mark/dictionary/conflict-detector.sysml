package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def ConflictDetector {
        doc /* Detects term conflicts across files. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Dictionary/ConflictDetector.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Dictionary/ConflictDetectorTests.cs");
            designFiles = ("docs/design/dictionary-mark/dictionary/conflict-detector.md");
            verificationFiles = ("docs/verification/dictionary-mark/dictionary/conflict-detector.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/dictionary/conflict-detector.yaml");
        }
    }
}
