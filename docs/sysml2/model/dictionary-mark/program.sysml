package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def Program {
        doc /* Entry point and execution orchestrator. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Program.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/ProgramTests.cs");
            designFiles = ("docs/design/dictionary-mark/program.md");
            verificationFiles = ("docs/verification/dictionary-mark/program.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/program.yaml");
        }
    }
}
