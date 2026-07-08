package OtsDependencies {
    doc /* Off-the-shelf (OTS) dependencies used by DictionaryMark. */

    part def YamlDotNet {
        doc /* OTS: YAML parsing library used to load dictionary files. */

        comment designRef /* Design: docs/design/ots/yaml-dot-net.md */
        comment verificationRef /* Verification: docs/verification/ots/yaml-dot-net.md */
        comment reqRef /* Requirements: docs/reqstream/ots/yaml-dot-net.yaml */
    }

    part def FileSystemGlobbing {
        doc /* OTS: Microsoft.Extensions.FileSystemGlobbing, used for glob-based file matching. */

        comment designRef /* Design: docs/design/ots/file-system-globbing.md */
        comment verificationRef /* Verification: docs/verification/ots/file-system-globbing.md */
        comment reqRef /* Requirements: docs/reqstream/ots/file-system-globbing.yaml */
    }

    part def TestResults {
        doc /* OTS: DemaConsulting.TestResults, used for self-test result reporting. */

        comment designRef /* Design: docs/design/ots/test-results.md */
        comment verificationRef /* Verification: docs/verification/ots/test-results.md */
        comment reqRef /* Requirements: docs/reqstream/ots/test-results.yaml */
    }
}
