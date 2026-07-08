package DictionaryMark {
    part def CliSubsystem {
        doc /* Command-line interface: argument parsing and I/O ownership. */

        comment testRef /* Test: test/DemaConsulting.DictionaryMark.Tests/Cli/CliSubsystemTests.cs */
        comment designRef /* Design: docs/design/dictionary-mark/cli.md */
        comment verificationRef /* Verification: docs/verification/dictionary-mark/cli.md */
        comment reqRef /* Requirements: docs/reqstream/dictionary-mark/cli.yaml */

        part context : Context;
    }
}
