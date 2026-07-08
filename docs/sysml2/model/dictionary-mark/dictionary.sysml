package DictionaryMark {
    part def DictionarySubsystem {
        doc /* Loads, validates, and formats dictionary data. */

        comment testRef /* Test: test/DemaConsulting.DictionaryMark.Tests/Dictionary/DictionarySubsystemTests.cs */
        comment designRef /* Design: docs/design/dictionary-mark/dictionary.md */
        comment verificationRef /* Verification: docs/verification/dictionary-mark/dictionary.md */
        comment reqRef /* Requirements: docs/reqstream/dictionary-mark/dictionary.yaml */

        part dictionaryGenerator : DictionaryGenerator;
        part yamlDictionaryLoader : YamlDictionaryLoader;
        part conflictDetector : ConflictDetector;
        part markdownFormatter : MarkdownFormatter;
        part dictionaryEntry : DictionaryEntry;
        part markdownOptions : MarkdownOptions;
        part outputFormat : OutputFormat;
        part sortOrder : SortOrder;
    }

    part def DictionaryEntry {
        doc /* Data model: term-definition pair. */

        comment sourceRef /* Source: src/DemaConsulting.DictionaryMark/Dictionary/DictionaryEntry.cs */
        comment testRef /* Test: test/DemaConsulting.DictionaryMark.Tests/Dictionary/DictionaryEntryTests.cs */
        comment designRef /* Design: docs/design/dictionary-mark/dictionary.md (documented as a supporting value type) */
        comment verificationRef /* Verification: docs/verification/dictionary-mark/dictionary.md */
        comment reqRef /* Requirements: docs/reqstream/dictionary-mark/dictionary.yaml */
    }

    part def MarkdownOptions {
        doc /* Data model: Markdown output options. */

        comment sourceRef /* Source: src/DemaConsulting.DictionaryMark/Dictionary/MarkdownOptions.cs */
        comment designRef /* Design: docs/design/dictionary-mark/dictionary.md (documented as a supporting value type) */
        comment verificationRef /* Verification: docs/verification/dictionary-mark/dictionary.md */
        comment reqRef /* Requirements: docs/reqstream/dictionary-mark/dictionary.yaml */
    }

    part def OutputFormat {
        doc /* Data model: output format enum. */

        comment sourceRef /* Source: src/DemaConsulting.DictionaryMark/Dictionary/OutputFormat.cs */
        comment designRef /* Design: docs/design/dictionary-mark/dictionary.md (documented as a supporting value type) */
        comment verificationRef /* Verification: docs/verification/dictionary-mark/dictionary.md */
        comment reqRef /* Requirements: docs/reqstream/dictionary-mark/dictionary.yaml */
    }

    part def SortOrder {
        doc /* Data model: sort order enum. */

        comment sourceRef /* Source: src/DemaConsulting.DictionaryMark/Dictionary/SortOrder.cs */
        comment designRef /* Design: docs/design/dictionary-mark/dictionary.md (documented as a supporting value type) */
        comment verificationRef /* Verification: docs/verification/dictionary-mark/dictionary.md */
        comment reqRef /* Requirements: docs/reqstream/dictionary-mark/dictionary.yaml */
    }
}
