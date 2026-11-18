package DictionaryMark {
    private import SoftwareItemMetadata::*;

    part def FileReferenceResolver {
        doc /* Reads policy-approved files a configuration file names, and checks its output paths and input patterns. */

        metadata info : SoftwareInfo {
            sourceFiles = (
                "src/DemaConsulting.DictionaryMark/Files/FileReferenceResolver.cs",
                "src/DemaConsulting.DictionaryMark/Files/FileReferenceResolver.Paths.cs",
                "src/DemaConsulting.DictionaryMark/Files/FileReferenceResolver.Output.cs",
                "src/DemaConsulting.DictionaryMark/Files/FileReferenceResolver.Patterns.cs",
                "src/DemaConsulting.DictionaryMark/Files/FileReferenceOptions.cs",
                "src/DemaConsulting.DictionaryMark/Files/FileReference.cs",
                "src/DemaConsulting.DictionaryMark/Files/FileReferenceException.cs",
                "src/DemaConsulting.DictionaryMark/Files/FileReferenceFailureKind.cs");
            testFiles = (
                "test/DemaConsulting.DictionaryMark.Tests/Files/FileReferenceResolverTests.cs",
                "test/DemaConsulting.DictionaryMark.Tests/Files/FileReferenceResolverLinkTests.cs",
                "test/DemaConsulting.DictionaryMark.Tests/Files/FileReferenceResolverRaceTests.cs",
                "test/DemaConsulting.DictionaryMark.Tests/Files/FileReferenceResolverOutputTests.cs",
                "test/DemaConsulting.DictionaryMark.Tests/Files/FileReferenceResolverPatternTests.cs",
                "test/DemaConsulting.DictionaryMark.Tests/Files/FileReferenceExceptionTests.cs");
            designFiles = ("docs/design/dictionary-mark/files/file-reference-resolver.md");
            verificationFiles = ("docs/verification/dictionary-mark/files/file-reference-resolver.md");
            requirementFiles = ("docs/reqstream/dictionary-mark/files/file-reference-resolver.yaml");
        }
    }

    // The resolver asks the policy about every opened file and output, and opens and inspects files through RealPath
    dependency FileReferenceResolver to FileAccessPolicy;
    dependency FileReferenceResolver to RealPath;
}
