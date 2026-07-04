package DictionaryMark {
    import OtsDependencies::*;

    doc
    /*
     * DictionaryMark is a .NET command-line application that reads YAML dictionary
     * files and generates formatted Markdown output as bullet lists or tables.
     */

    part def DictionaryMarkSystem {
        doc /* The DictionaryMark system as a whole. */

        part program : Program;
        part cli : CliSubsystem;
        part dictionary : DictionarySubsystem;
        part selfTest : SelfTestSubsystem;
        part utilities : UtilitiesSubsystem;
        part yamlDotNet : YamlDotNet;
        part fileSystemGlobbing : FileSystemGlobbing;
        part testResults : TestResults;
    }

    part def Program {
        doc /* Entry point and execution orchestrator. */
    }

    part def CliSubsystem {
        doc /* Command-line interface: argument parsing and I/O ownership. */

        part context : Context;
    }

    part def Context {
        doc /* Command-line argument parser and I/O owner. */
    }

    part def DictionarySubsystem {
        doc /* Loads, validates, and formats dictionary data. */

        part dictionaryGenerator : DictionaryGenerator;
        part yamlDictionaryLoader : YamlDictionaryLoader;
        part conflictDetector : ConflictDetector;
        part markdownFormatter : MarkdownFormatter;
        part dictionaryEntry : DictionaryEntry;
        part markdownOptions : MarkdownOptions;
        part outputFormat : OutputFormat;
        part sortOrder : SortOrder;
    }

    part def DictionaryGenerator {
        doc /* Orchestrates dictionary generation. */
    }

    part def YamlDictionaryLoader {
        doc /* Loads YAML flat-dictionary files. */
    }

    part def ConflictDetector {
        doc /* Detects term conflicts across files. */
    }

    part def MarkdownFormatter {
        doc /* Formats entries as Markdown. */
    }

    part def DictionaryEntry {
        doc /* Data model: term-definition pair. */
    }

    part def MarkdownOptions {
        doc /* Data model: Markdown output options. */
    }

    part def OutputFormat {
        doc /* Data model: output format enum. */
    }

    part def SortOrder {
        doc /* Data model: sort order enum. */
    }

    part def SelfTestSubsystem {
        doc /* Self-validation test runner subsystem. */

        part validation : Validation;
    }

    part def Validation {
        doc /* Self-validation test runner. */
    }

    part def UtilitiesSubsystem {
        doc /* Shared, cross-cutting helper utilities. */

        part globMatcher : GlobMatcher;
        part pathHelpers : PathHelpers;
        part temporaryDirectory : TemporaryDirectory;
    }

    part def GlobMatcher {
        doc /* File-matching via glob patterns. */
    }

    part def PathHelpers {
        doc /* Safe path combination utilities. */
    }

    part def TemporaryDirectory {
        doc /* Disposable temporary directory helper. */
    }
}
