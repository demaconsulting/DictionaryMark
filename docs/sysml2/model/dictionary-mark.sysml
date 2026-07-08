package DictionaryMark {
    import OtsDependencies::*;

    doc
    /*
     * DictionaryMark is a .NET command-line application that reads YAML dictionary
     * files and generates formatted Markdown output as bullet lists or tables.
     */

    part def DictionaryMarkSystem {
        doc /* The DictionaryMark system as a whole. */

        comment designRef /* Design: docs/design/dictionary-mark.md */
        comment verificationRef /* Verification: docs/verification/dictionary-mark.md */
        comment reqRef /* Requirements: docs/reqstream/dictionary-mark.yaml */

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
