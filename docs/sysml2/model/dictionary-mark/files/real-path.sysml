package DictionaryMark_RealPath {
    private import SoftwareItemMetadata::*;

    part def RealPath {
        doc /* Operating-system facts of an open file, folder locations and path comparison. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.DictionaryMark/Files/RealPath.cs",
                "src/DemaConsulting.DictionaryMark/Files/RealPath.Windows.cs",
                "src/DemaConsulting.DictionaryMark/Files/RealPath.Unix.cs",
                "src/DemaConsulting.DictionaryMark/Files/NativeMethods.cs",
                "src/DemaConsulting.DictionaryMark/Files/FileOpenHooks.cs");
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Files/RealPathTests.cs");
            designFiles = ("docs/design/dictionary-mark/files/real-path.md");
            verificationFiles = ("docs/verification/dictionary-mark/files/real-path.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/files/real-path.yaml");
        }
    }
}
