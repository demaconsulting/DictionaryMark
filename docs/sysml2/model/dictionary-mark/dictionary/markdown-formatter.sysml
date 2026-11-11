package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def MarkdownFormatter {
        doc /* Formats entries as Markdown. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Dictionary/MarkdownFormatter.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Dictionary/MarkdownFormatterTests.cs");
            designFiles = ("docs/design/dictionary-mark/dictionary/markdown-formatter.md");
            verificationFiles = ("docs/verification/dictionary-mark/dictionary/markdown-formatter.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/dictionary/markdown-formatter.yaml");
        }
    }
}
