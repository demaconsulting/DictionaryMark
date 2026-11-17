package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def CliSubsystem {
        doc /* Command-line interface: argument parsing, configuration files, generation jobs and I/O ownership. */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Cli/CliSubsystemTests.cs");
            designFiles = ("docs/design/dictionary-mark/cli.md");
            verificationFiles = ("docs/verification/dictionary-mark/cli.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/cli.yaml");
        }

        part context : Context;
        part configFile : ConfigFile;
        part tableJob : TableJob;
        part formattingSettings : FormattingSettings;
    }

    part def FormattingSettings {
        doc /* Data model: one optional-valued precedence layer of formatting settings (documented as a supporting value type in the subsystem doc). */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Cli/FormattingSettings.cs");
            designFiles = ("docs/design/dictionary-mark/cli.md");
            verificationFiles = ("docs/verification/dictionary-mark/cli.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/cli/table-job.yaml");
        }
    }
}
