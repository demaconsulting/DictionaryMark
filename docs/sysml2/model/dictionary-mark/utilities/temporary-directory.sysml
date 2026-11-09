package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def TemporaryDirectory {
        doc /* Disposable temporary directory helper. */

        metadata info : SoftwareInfo {
            description = "Disposable temporary directory helper.";
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Utilities/TemporaryDirectory.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Utilities/TemporaryDirectoryTests.cs");
            designFiles = ("docs/design/dictionary-mark/utilities/temporary-directory.md");
            verificationFiles = ("docs/verification/dictionary-mark/utilities/temporary-directory.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/utilities/temporary-directory.yaml");
        }
    }
}
