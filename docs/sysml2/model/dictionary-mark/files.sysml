package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def FilesSubsystem {
        doc /* File-access confinement of configuration-file table inputs and outputs (adapted from CanvasNet). */

        metadata info : SoftwareInfo {
            testFiles = ("test/DemaConsulting.DictionaryMark.Tests/Files/FilesSubsystemTests.cs");
            designFiles = ("docs/design/dictionary-mark/files.md");
            verificationFiles = ("docs/verification/dictionary-mark/files.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/files.yaml");
        }

        part fileReferenceResolver : FileReferenceResolver;
        part fileAccessPolicy : FileAccessPolicy;
        part realPath : RealPath;
    }
}
