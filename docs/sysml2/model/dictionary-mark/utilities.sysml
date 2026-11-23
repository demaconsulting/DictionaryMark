package DictionaryMark_UtilitiesSubsystem {
    public import DictionaryMark_GlobMatcher::*;
    public import DictionaryMark_PathHelpers::*;
    public import DictionaryMark_PathResolver::*;
    public import DictionaryMark_TemporaryDirectory::*;
    private import SoftwareItemMetadata::*;

    part def UtilitiesSubsystem {
        doc /* Shared, cross-cutting helper utilities. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Utilities/UtilitiesSubsystemTests.cs");
            designFiles = ("docs/design/dictionary-mark/utilities.md");
            verificationFiles = ("docs/verification/dictionary-mark/utilities.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/utilities.yaml");
        }

        part globMatcher : GlobMatcher;
        part pathResolver : PathResolver;
        part pathHelpers : PathHelpers;
        part temporaryDirectory : TemporaryDirectory;
    }
}
