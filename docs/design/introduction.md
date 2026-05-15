# Introduction

This document provides the detailed design for DictionaryMark, a .NET command-line
application that reads YAML dictionary files and generates formatted Markdown output.

## Purpose

The purpose of this document is to describe the internal design of each software unit that
comprises DictionaryMark. It captures data models, algorithms, key methods, and
inter-unit interactions at a level of detail sufficient for formal code review, compliance
verification, and future maintenance. The document does not restate requirements; it explains
how they are realized.

## Scope

This document covers the detailed design of the following subsystems and software units:

- **DictionaryMark** - the system as a whole (`dictionary-mark.md`)
- **Program** - entry point and execution orchestrator (`Program.cs`)
- **Cli** subsystem
  - **Context** - command-line argument parser and I/O owner (`Cli/Context.cs`)
- **Dictionary** subsystem
  - **DictionaryGenerator** - orchestrates dictionary generation (`Dictionary/DictionaryGenerator.cs`)
  - **YamlDictionaryLoader** - loads YAML flat-dictionary files (`Dictionary/YamlDictionaryLoader.cs`)
  - **ConflictDetector** - detects term conflicts across files (`Dictionary/ConflictDetector.cs`)
  - **MarkdownFormatter** - formats entries as Markdown (`Dictionary/MarkdownFormatter.cs`)
- **SelfTest** subsystem
  - **Validation** - self-validation test runner (`SelfTest/Validation.cs`)
- **Utilities** subsystem
  - **GlobMatcher** - file-matching via glob patterns (`Utilities/GlobMatcher.cs`)
  - **PathHelpers** - safe path combination utilities (`Utilities/PathHelpers.cs`)

The following topics are out of scope:

- External library internals
- Build pipeline configuration
- Deployment and packaging

## Software Structure

The following tree shows how the DictionaryMark software items are organized across the
system, subsystem, and unit levels:

```text
DictionaryMark (System)
├── Program (Unit)
├── Cli (Subsystem)
│   └── Context (Unit)
├── Dictionary (Subsystem)
│   ├── DictionaryGenerator (Unit)
│   ├── YamlDictionaryLoader (Unit)
│   ├── ConflictDetector (Unit)
│   └── MarkdownFormatter (Unit)
├── SelfTest (Subsystem)
│   └── Validation (Unit)
└── Utilities (Subsystem)
    ├── GlobMatcher (Unit)
    └── PathHelpers (Unit)
```

Each unit is described in detail in its own chapter within this document.

## Folder Layout

The source code folder structure mirrors the top-level subsystem breakdown above, giving
reviewers an explicit navigation aid from design to code:

```text
src/DemaConsulting.DictionaryMark/
├── Program.cs                          - entry point and execution orchestrator
├── Cli/
│   └── Context.cs                      - command-line argument parser and I/O owner
├── Dictionary/
│   ├── DictionaryGenerator.cs          - orchestrates dictionary generation
│   ├── YamlDictionaryLoader.cs         - loads YAML flat-dictionary files
│   ├── ConflictDetector.cs             - detects term conflicts across files
│   └── MarkdownFormatter.cs            - formats entries as Markdown
├── SelfTest/
│   └── Validation.cs                   - self-validation test runner
└── Utilities/
    ├── GlobMatcher.cs                  - file-matching via glob patterns
    └── PathHelpers.cs                  - safe path combination utilities
```

The test project mirrors the same layout under `test/DemaConsulting.DictionaryMark.Tests/`.

## Document Conventions

Throughout this document:

- Class names, method names, property names, and file names appear in `monospace` font.
- The word **shall** denotes a design constraint that the implementation must satisfy.
- Section headings within each unit chapter follow a consistent structure: overview, data model,
  methods/algorithms, and interactions with other units.
- Text tables are used in preference to diagrams, which may not render in all PDF viewers.

## Companion Artifact Structure

Each software item in the structure above has corresponding artifacts in parallel directory trees:

- Requirements: `docs/reqstream/{system-name}.yaml`, `docs/reqstream/{system-name}/.../{item}.yaml` (kebab-case)
- Design docs: `docs/design/{system-name}.md`, `docs/design/{system-name}/.../{item}.md` (kebab-case)
- Verification design: `docs/verification/{system-name}.md`, `docs/verification/{system-name}/.../{item}.md` (kebab-case)
- Source code: `src/{System}/.../{Item}.cs` (PascalCase for C#)
- Tests: `test/{System}.Tests/.../{Item}Tests.cs` (PascalCase for C#)
- Review-sets: defined in `.reviewmark.yaml`
- User guide: `docs/user_guide/{system-name}.md`

## References

- [REF-1] YAML 1.2 Specification - <https://yaml.org/spec/1.2.2/>
- [REF-2] .NET SDK Documentation - <https://learn.microsoft.com/dotnet/>
- [REF-3] CommonMark Specification - <https://spec.commonmark.org/>
