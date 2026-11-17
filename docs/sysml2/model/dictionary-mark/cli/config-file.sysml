package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def ConfigFile {
        doc /* YAML configuration file loader and validator: formatting defaults, root directory and table jobs. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Cli/ConfigFile.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Cli/ConfigFileTests.cs");
            designFiles = ("docs/design/dictionary-mark/cli/config-file.md");
            verificationFiles = ("docs/verification/dictionary-mark/cli/config-file.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/cli/config-file.yaml");
        }
    }

    // ConfigFile builds formatting layers, parses YAML, and resolves the root key against the file's directory
    dependency ConfigFile to FormattingSettings;
    dependency ConfigFile to OtsDependencies::YamlDotNet;
    dependency ConfigFile to PathResolver;
}