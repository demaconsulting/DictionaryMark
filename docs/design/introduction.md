# Introduction

DictionaryMark is a .NET command-line application that reads YAML dictionary files and generates
formatted Markdown output as bullet lists or tables.

## Purpose

This document defines the software design of DictionaryMark. It covers architectural design for the
system and its subsystems, and detailed design for each software unit, including data models,
algorithms, key methods, and inter-unit interactions. For off-the-shelf (OTS) components it covers
integration and usage design only. The document does not restate requirements; it explains how they
are realized.

The intended audience is code reviewers, compliance auditors, and future maintainers.

## Scope

This document covers the design of the following software items:

- **DictionaryMark** — the system as a whole
- **Program** — entry point and execution orchestrator
- **Cli** subsystem
  - **Context** — command-line argument parser and I/O owner
- **Dictionary** subsystem
  - **DictionaryGenerator** — orchestrates dictionary generation
  - **YamlDictionaryLoader** — loads YAML flat-dictionary files
  - **ConflictDetector** — detects term conflicts across files
  - **MarkdownFormatter** — formats entries as Markdown
  - **DictionaryEntry** — data model: term-definition pair
  - **MarkdownOptions** — data model: Markdown output options
  - **OutputFormat** — data model: output format enum
  - **SortOrder** — data model: sort order enum
- **SelfTest** subsystem
  - **Validation** — self-validation test runner
- **Utilities** subsystem
  - **GlobMatcher** — file-matching via glob patterns
  - **PathHelpers** — safe path combination utilities
  - **TemporaryDirectory** — disposable temporary directory helper

OTS dependencies covered:

- **YamlDotNet** (OTS)
- **Microsoft.Extensions.FileSystemGlobbing** (OTS)
- **DemaConsulting.TestResults** (OTS)

Out of scope: test projects, build pipeline configuration, deployment and packaging, and external
library internals.

## Software Structure

The software structure is modeled in SysML2 under `docs/sysml2/` and rendered to the
diagram below by SysML2Tools as part of the build pipeline. AI agents should query the
SysML2 model directly (see `docs/sysml2/system.sysml` for the stable entry point) rather
than parsing this diagram or the prose below.

![Software Structure](software-structure.svg)

## Folder Layout

```text
src/DemaConsulting.DictionaryMark/
├── Program.cs                          - entry point and execution orchestrator
├── Cli/
│   └── Context.cs                      - command-line argument parser and I/O owner
├── Dictionary/
│   ├── DictionaryGenerator.cs          - orchestrates dictionary generation
│   ├── YamlDictionaryLoader.cs         - loads YAML flat-dictionary files
│   ├── ConflictDetector.cs             - detects term conflicts across files
│   ├── MarkdownFormatter.cs            - formats entries as Markdown
│   ├── DictionaryEntry.cs              - data model: term-definition pair
│   ├── MarkdownOptions.cs              - data model: Markdown output options
│   ├── OutputFormat.cs                 - data model: output format enum
│   └── SortOrder.cs                    - data model: sort order enum
├── SelfTest/
│   └── Validation.cs                   - self-validation test runner
└── Utilities/
    ├── GlobMatcher.cs                  - file-matching via glob patterns
    ├── PathHelpers.cs                  - safe path combination utilities
    └── TemporaryDirectory.cs           - disposable temporary directory helper

test/DemaConsulting.DictionaryMark.Tests/
├── Cli/
│   └── ContextTests.cs
├── Dictionary/
│   ├── DictionaryGeneratorTests.cs
│   ├── YamlDictionaryLoaderTests.cs
│   ├── ConflictDetectorTests.cs
│   ├── MarkdownFormatterTests.cs
│   └── ...
├── SelfTest/
│   └── ValidationTests.cs
└── Utilities/
    ├── GlobMatcherTests.cs
    ├── PathHelpersTests.cs
    └── TemporaryDirectoryTests.cs
```

## Companion Artifact Structure

Each local software item has corresponding artifacts in parallel directory trees:

- Requirements: `docs/reqstream/dictionary-mark.yaml`,
  `docs/reqstream/dictionary-mark/{subsystem}/{item}.yaml`
- Design: `docs/design/dictionary-mark.md`,
  `docs/design/dictionary-mark/{subsystem}/{item}.md`
- Verification design: `docs/verification/dictionary-mark.md`,
  `docs/verification/dictionary-mark/{subsystem}/{item}.md`
- Source code: `src/DemaConsulting.DictionaryMark/{Subsystem}/{Item}.cs`
- Tests: `test/DemaConsulting.DictionaryMark.Tests/{Subsystem}/{Item}Tests.cs`

OTS items have integration and usage design documentation in parallel OTS folders:

- Requirements: `docs/reqstream/ots/{ots-name}.yaml`
- Design: `docs/design/ots/{ots-name}.md`
- Verification design: `docs/verification/ots/{ots-name}.md`

Review-sets are defined in `.reviewmark.yaml`.

## References

- [REF-1] YAML 1.2 Specification — <https://yaml.org/spec/1.2.2/>
- [REF-2] .NET SDK Documentation — <https://learn.microsoft.com/dotnet/>
- [REF-3] CommonMark Specification — <https://spec.commonmark.org/>
