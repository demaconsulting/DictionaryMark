package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def RealPath {
        doc /* Isolates the native code: non-blocking opens and the real path and type of an open handle per platform. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.DictionaryMark/Utilities/RealPath.cs",
                "src/DemaConsulting.DictionaryMark/Utilities/RealPath.Windows.cs",
                "src/DemaConsulting.DictionaryMark/Utilities/RealPath.Unix.cs",
                "src/DemaConsulting.DictionaryMark/Utilities/RealPath.NativeMethods.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Utilities/RealPathTests.cs");
            designFiles = ("docs/design/dictionary-mark/utilities/real-path.md");
            verificationFiles = ("docs/verification/dictionary-mark/utilities/real-path.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/utilities/real-path.yaml");
        }
    }
}