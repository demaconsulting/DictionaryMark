package DictionaryMark {
    private import SoftwareItemMetadata::*;

    import OtsDependencies::*;

    doc
    /*
     * DictionaryMark is a .NET command-line application that reads YAML dictionary
     * files and generates formatted Markdown output as bullet lists or tables.
     */

    part def DictionaryMarkSystem {
        doc /* The DictionaryMark system as a whole. */

        metadata info : SoftwareInfo {
            description = "The DictionaryMark system as a whole.";
            designFiles = ("docs/design/dictionary-mark.md");
            verificationFiles = ("docs/verification/dictionary-mark.md");
            requirementFiles = ("docs/reqstream/dictionary-mark.yaml");
        }

        part program : Program;
        part cli : CliSubsystem;
        part dictionary : DictionarySubsystem;
        part selfTest : SelfTestSubsystem;
        part utilities : UtilitiesSubsystem;
        part yamlDotNet : YamlDotNet;
        part fileSystemGlobbing : FileSystemGlobbing;
        part testResults : TestResults;
    }
}
