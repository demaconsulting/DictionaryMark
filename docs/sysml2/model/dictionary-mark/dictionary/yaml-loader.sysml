package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def YamlDictionaryLoader {
        doc /* Loads YAML flat-dictionary files. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Dictionary/YamlDictionaryLoader.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Dictionary/YamlDictionaryLoaderTests.cs");
            designFiles = ("docs/design/dictionary-mark/dictionary/yaml-loader.md");
            verificationFiles = ("docs/verification/dictionary-mark/dictionary/yaml-loader.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/dictionary/yaml-loader.yaml");
        }
    }
}
