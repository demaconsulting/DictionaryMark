package DictionaryMark_HelpCommand {
    private import SoftwareItemMetadata::*;
    private import OtsDependencies::*;

    part def HelpCommand {
        doc /* Built-in help command: answers the help verb from embedded help cards before option parsing. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Cli/HelpCommand.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Cli/HelpCommandTests.cs");
            designFiles = ("docs/design/dictionary-mark/cli/help-command.md");
            verificationFiles = ("docs/verification/dictionary-mark/cli/help-command.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/cli/help-command.yaml");
        }
    }

    // Serves the help command from the cards embedded in the assembly
    dependency HelpCommand to HelpCards;
}
