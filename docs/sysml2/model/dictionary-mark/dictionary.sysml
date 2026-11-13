package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def DictionarySubsystem {
        doc /* Loads, validates, and formats dictionary data. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Dictionary/DictionarySubsystemTests.cs");
            designFiles = ("docs/design/dictionary-mark/dictionary.md");
            verificationFiles = ("docs/verification/dictionary-mark/dictionary.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/dictionary.yaml");
        }

        part dictionaryGenerator : DictionaryGenerator;
        part yamlDictionaryLoader : YamlDictionaryLoader;
        part conflictDetector : ConflictDetector;
        part markdownFormatter : MarkdownFormatter;
        part dictionaryEntry : DictionaryEntry;
        part deprecatedEntry : DeprecatedEntry;
        part inputKind : InputKind;
        part markdownOptions : MarkdownOptions;
        part outputFormat : OutputFormat;
        part sortOrder : SortOrder;
    }

    part def DictionaryEntry {
        doc /* Data model: term-definition pair. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Dictionary/DictionaryEntry.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Dictionary/DictionaryEntryTests.cs");
            designFiles = ("docs/design/dictionary-mark/dictionary.md");
            verificationFiles = ("docs/verification/dictionary-mark/dictionary.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/dictionary.yaml");
        }
    }

    part def DeprecatedEntry {
        doc /* Data model: deprecated term with the replacement to use instead. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Dictionary/DeprecatedEntry.cs");
            designFiles = ("docs/design/dictionary-mark/dictionary.md");
            verificationFiles = ("docs/verification/dictionary-mark/dictionary.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/dictionary.yaml");
        }
    }

    part def InputKind {
        doc /* Data model: input kind enum (terms or deprecated). */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Dictionary/InputKind.cs");
            designFiles = ("docs/design/dictionary-mark/dictionary.md");
            verificationFiles = ("docs/verification/dictionary-mark/dictionary.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/dictionary.yaml");
        }
    }

    part def MarkdownOptions {
        doc /* Data model: Markdown output options. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Dictionary/MarkdownOptions.cs");
            designFiles = ("docs/design/dictionary-mark/dictionary.md");
            verificationFiles = ("docs/verification/dictionary-mark/dictionary.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/dictionary.yaml");
        }
    }

    part def OutputFormat {
        doc /* Data model: output format enum. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Dictionary/OutputFormat.cs");
            designFiles = ("docs/design/dictionary-mark/dictionary.md");
            verificationFiles = ("docs/verification/dictionary-mark/dictionary.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/dictionary.yaml");
        }
    }

    part def SortOrder {
        doc /* Data model: sort order enum. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Dictionary/SortOrder.cs");
            designFiles = ("docs/design/dictionary-mark/dictionary.md");
            verificationFiles = ("docs/verification/dictionary-mark/dictionary.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/dictionary.yaml");
        }
    }
}
