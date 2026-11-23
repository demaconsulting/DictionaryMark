package DictionaryMark_PathResolver {
    private import SoftwareItemMetadata::*;

    part def PathResolver {
        doc /* Resolves user paths against a base directory with ${VAR} expansion and parent-directory support. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Utilities/PathResolver.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Utilities/PathResolverTests.cs");
            designFiles = ("docs/design/dictionary-mark/utilities/path-resolver.md");
            verificationFiles = ("docs/verification/dictionary-mark/utilities/path-resolver.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/utilities/path-resolver.yaml");
        }
    }
}
