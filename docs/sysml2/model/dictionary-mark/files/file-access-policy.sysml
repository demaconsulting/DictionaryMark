package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def FileAccessPolicy {
        doc /* The file-access policy and its built-in modes: any, roots, no-symlinks and their combination. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.DictionaryMark/Files/FileAccessPolicy.cs",
                "src/DemaConsulting.DictionaryMark/Files/FileAccessPolicy.BuiltIns.cs",
                "src/DemaConsulting.DictionaryMark/Files/FileAccessRequest.cs",
                "src/DemaConsulting.DictionaryMark/Files/FileAccessDecision.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Files/FileAccessPolicyTests.cs");
            designFiles = ("docs/design/dictionary-mark/files/file-access-policy.md");
            verificationFiles = ("docs/verification/dictionary-mark/files/file-access-policy.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/files/file-access-policy.yaml");
        }
    }

    // The built-in policies compare paths and capture the real location of their folders through RealPath
    dependency FileAccessPolicy to RealPath;
}
