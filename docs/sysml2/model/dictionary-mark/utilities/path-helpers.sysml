package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def PathHelpers {
        doc /* Safe path combination utilities. */

        metadata info : SoftwareInfo {
            description = "Safe path combination utilities.";
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Utilities/PathHelpers.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Utilities/PathHelpersTests.cs");
            designFiles = ("docs/design/dictionary-mark/utilities/path-helpers.md");
            verificationFiles = ("docs/verification/dictionary-mark/utilities/path-helpers.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/utilities/path-helpers.yaml");
        }
    }
}
