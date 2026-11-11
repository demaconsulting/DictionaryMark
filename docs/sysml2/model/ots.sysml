package OtsDependencies {
    private import SoftwareItemMetadata::*;

    doc /* Off-the-shelf (OTS) dependencies used by DictionaryMark. */

    part def YamlDotNet {
        doc /* OTS: YAML parsing library used to load dictionary files. */

        metadata info : SoftwareInfo {
            designFiles = ("docs/design/ots/yaml-dot-net.md");
            verificationFiles = ("docs/verification/ots/yaml-dot-net.md");
            requirementFiles = ("docs/reqstream/ots/yaml-dot-net.yaml");
        }
    }

    part def FileSystemGlobbing {
        doc /* OTS: Microsoft.Extensions.FileSystemGlobbing, used for glob-based file matching. */

        metadata info : SoftwareInfo {
            designFiles = ("docs/design/ots/file-system-globbing.md");
            verificationFiles = ("docs/verification/ots/file-system-globbing.md");
            requirementFiles = ("docs/reqstream/ots/file-system-globbing.yaml");
        }
    }

    part def TestResults {
        doc /* OTS: DemaConsulting.TestResults, used for self-test result reporting. */

        metadata info : SoftwareInfo {
            designFiles = ("docs/design/ots/test-results.md");
            verificationFiles = ("docs/verification/ots/test-results.md");
            requirementFiles = ("docs/reqstream/ots/test-results.yaml");
        }
    }
}
