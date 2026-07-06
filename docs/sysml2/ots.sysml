package OtsDependencies {
    doc /* Off-the-shelf (OTS) dependencies used by DictionaryMark. */

    part def YamlDotNet {
        doc /* OTS: YAML parsing library used to load dictionary files. */
    }

    part def FileSystemGlobbing {
        doc /* OTS: Microsoft.Extensions.FileSystemGlobbing, used for glob-based file matching. */
    }

    part def TestResults {
        doc /* OTS: DemaConsulting.TestResults, used for self-test result reporting. */
    }
}
