package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def Context {
        doc /* Command-line argument parser and I/O owner. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Cli/Context.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Cli/ContextTests.cs");
            designFiles = ("docs/design/dictionary-mark/cli/context.md");
            verificationFiles = ("docs/verification/dictionary-mark/cli/context.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/cli/context.yaml");
        }
    }

    // Context loads the config file, builds the generation jobs, and resolves --config and --root paths
    dependency Context to ConfigFile;
    dependency Context to TableJob;
    dependency Context to PathResolver;

    // Context builds the file-access policy from --file-access and --allow-root
    dependency Context to FileAccessPolicy;
}
