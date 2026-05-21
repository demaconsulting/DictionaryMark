# Introduction

This document provides the verification design for DictionaryMark, a .NET command-line
application that reads YAML dictionary files and generates formatted Markdown output.

## Purpose

The purpose of this document is to describe how each requirement for DictionaryMark is
verified. For every software item - system, subsystem, and unit - this document names the
verification approach, identifies the test scenarios (including boundary conditions and error
paths), describes what is mocked or stubbed, and maps each requirement to at least one named
test scenario. The document does not restate design; it explains how the design is proven correct.

## Scope

This document covers the verification design for the same software items described in the
*DictionaryMark Software Design Document*:

- **DictionaryMark** - the system as a whole
- **Program** - entry point and execution orchestrator
- **Cli** - command-line interface subsystem
  - **Context** - argument parser and I/O owner
- **Dictionary** - dictionary processing subsystem
  - **DictionaryGenerator** - orchestrates dictionary generation
  - **YamlDictionaryLoader** - loads YAML flat-dictionary files
  - **ConflictDetector** - detects term conflicts across files
  - **MarkdownFormatter** - formats entries as Markdown
- **SelfTest** - self-validation subsystem
  - **Validation** - self-validation test runner
- **Utilities** - shared utility subsystem
  - **GlobMatcher** - file-matching via glob patterns
  - **PathHelpers** - safe path combination utilities

The following topics are out of scope:

- Test infrastructure (xUnit framework, test helpers, Runner utility)
- Build pipeline and CI/CD configuration

The following OTS items are also covered:

- **BuildMark** - build-notes documentation tool
- **FileAssert** - document assertion tool
- **Pandoc** - Markdown-to-HTML conversion tool
- **ReqStream** - requirements traceability tool
- **ReviewMark** - file review enforcement tool
- **SarifMark** - SARIF report conversion tool
- **SonarMark** - SonarCloud quality report tool
- **VersionMark** - tool-version documentation tool
- **WeasyPrint** - HTML-to-PDF conversion tool
- **xUnit** - unit-testing framework

## Software Structure

The following tree shows the software items covered by this document:

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

OTS Items
├── BuildMark
├── FileAssert
├── Pandoc
├── ReqStream
├── ReviewMark
├── SarifMark
├── SonarMark
├── VersionMark
├── WeasyPrint
└── xUnit
```

## Companion Artifact Structure

In-house items have corresponding artifacts in parallel directory trees:

- Requirements: `docs/reqstream/{system-name}.yaml`, `docs/reqstream/{system}/.../{item}.yaml` (kebab-case)
- Design docs: `docs/design/{system-name}.md`, `docs/design/{system-name}/.../{item}.md` (kebab-case)
- Verification design: `docs/verification/{system-name}.md`, `docs/verification/{system-name}/.../{item}.md` (kebab-case)
- Source code: `src/{System}/.../{Item}.cs` (PascalCase for C#)
- Tests: `test/{System}.Tests/.../{Item}Tests.cs` (PascalCase for C#)

OTS items have parallel artifacts in:

- Requirements: `docs/reqstream/ots/{ots-name}.yaml` (kebab-case)
- Verification: `docs/verification/ots/{ots-name}.md` (kebab-case)

Review-sets: defined in `.reviewmark.yaml`

## References

- DictionaryMark releases — <https://github.com/demaconsulting/DictionaryMark/releases> — compiled
  verification design, design, and requirements documents are published as release artifacts on this
  page.
