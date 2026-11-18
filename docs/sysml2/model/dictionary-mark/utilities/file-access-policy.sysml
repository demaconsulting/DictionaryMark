package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def FileAccessPolicy {
        doc /* Judges configuration-file paths by the never-accessed list and the --file-access mode, using real paths. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.DictionaryMark/Utilities/FileAccessPolicy.cs",
                "src/DemaConsulting.DictionaryMark/Utilities/FileAccessMode.cs",
                "src/DemaConsulting.DictionaryMark/Utilities/FileAccessDeniedException.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Utilities/FileAccessPolicyTests.cs");
            designFiles = ("docs/design/dictionary-mark/utilities/file-access-policy.md");
            verificationFiles = ("docs/verification/dictionary-mark/utilities/file-access-policy.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/utilities/file-access-policy.yaml");
        }
    }

    // FileAccessPolicy opens files and asks for the real path and type of every handle it judges
    dependency FileAccessPolicy to RealPath;
}