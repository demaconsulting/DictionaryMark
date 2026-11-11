package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def CliSubsystem {
        doc /* Command-line interface: argument parsing and I/O ownership. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Cli/CliSubsystemTests.cs");
            designFiles = ("docs/design/dictionary-mark/cli.md");
            verificationFiles = ("docs/verification/dictionary-mark/cli.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/cli.yaml");
        }

        part context : Context;
    }
}
