package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def GlobMatcher {
        doc /* File-matching via glob patterns. */

        metadata info : SoftwareInfo {
            description = "File-matching via glob patterns.";
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Utilities/GlobMatcher.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Utilities/GlobMatcherTests.cs");
            designFiles = ("docs/design/dictionary-mark/utilities/glob-matcher.md");
            verificationFiles = ("docs/verification/dictionary-mark/utilities/glob-matcher.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/utilities/glob-matcher.yaml");
        }
    }
}
