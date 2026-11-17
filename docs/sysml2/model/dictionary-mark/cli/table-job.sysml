package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def TableJob {
        doc /* One resolved generation job: inputs, output, base directory and merged formatting options. */

        metadata info : SoftwareInfo {
            sourceFiles = ("src/DemaConsulting.DictionaryMark/Cli/TableJob.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Cli/TableJobTests.cs");
            designFiles = ("docs/design/dictionary-mark/cli/table-job.md");
            verificationFiles = ("docs/verification/dictionary-mark/cli/table-job.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/cli/table-job.yaml");
        }
    }

    // TableJob merges formatting layers and produces the Markdown options used by the formatter
    dependency TableJob to FormattingSettings;
    dependency TableJob to MarkdownOptions;
}
