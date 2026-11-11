package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def DictionaryGenerator {
        doc /* Orchestrates dictionary generation. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Dictionary/DictionaryGenerator.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Dictionary/DictionaryGeneratorTests.cs");
            designFiles = ("docs/design/dictionary-mark/dictionary/dictionary-generator.md");
            verificationFiles = ("docs/verification/dictionary-mark/dictionary/dictionary-generator.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/dictionary/dictionary-generator.yaml");
        }
    }
}
