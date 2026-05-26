# Introduction

This document describes how each software item in DictionaryMark is verified.

## Purpose

This document describes how each software item in DictionaryMark is verified — local items
(system, subsystems, and units) and OTS software items. For each item, it names the
verification approach, identifies the test scenarios (including boundary conditions and error
paths), describes what is mocked or stubbed, and maps each requirement to at least one named
test scenario. A reviewer should be able to confirm coverage completeness without reading test
code. The document does not restate design; it explains how the design is proven correct.

## Scope

This document covers the verification design for the following software items.

Local items:

- **DictionaryMark**: system, subsystem, and unit verification.
  - **Program**: entry point and execution orchestrator.
  - **Cli**: command-line interface subsystem.
    - **Context**: argument parser and I/O owner.
  - **Dictionary**: dictionary processing subsystem.
    - **DictionaryGenerator**: orchestrates dictionary generation.
    - **YamlDictionaryLoader**: loads YAML flat-dictionary files.
    - **ConflictDetector**: detects term conflicts across files.
    - **MarkdownFormatter**: formats entries as Markdown.
  - **SelfTest**: self-validation subsystem.
    - **Validation**: self-validation test runner.
  - **Utilities**: shared utility subsystem.
    - **GlobMatcher**: file-matching via glob patterns.
    - **PathHelpers**: safe path combination utilities.
    - **TemporaryDirectory**: disposable temporary directory helper.

OTS items:

- **BuildMark**: build-notes documentation tool.
- **DemaConsulting.TestResults**: TRX and JUnit XML test results library.
- **FileAssert**: document assertion tool.
- **Microsoft.Extensions.FileSystemGlobbing**: glob pattern matching for file system paths.
- **Pandoc**: Markdown-to-HTML conversion tool.
- **ReqStream**: requirements traceability tool.
- **ReviewMark**: file review enforcement tool.
- **SarifMark**: SARIF report conversion tool.
- **SonarMark**: SonarCloud quality report tool.
- **VersionMark**: tool-version documentation tool.
- **WeasyPrint**: HTML-to-PDF conversion tool.
- **xUnit**: unit-testing framework.
- **YamlDotNet**: YAML parsing library.

The following topics are out of scope:

- Test infrastructure (xUnit framework internals, test helpers, Runner utility).
- Build pipeline and CI/CD configuration.
- Deployment and packaging.

## Companion Artifact Structure

Local items have parallel artifacts in:

- Requirements: `docs/reqstream/{system-name}.yaml`,
  `docs/reqstream/{system-name}[/{subsystem-name}...]/{item}.yaml`
- Design: `docs/design/{system-name}.md`,
  `docs/design/{system-name}[/{subsystem-name}...]/{item}.md`
- Verification: `docs/verification/{system-name}.md`,
  `docs/verification/{system-name}[/{subsystem-name}...]/{item}.md`
- Source: `src/{SystemName}[/{SubsystemName}...]/{Item}.cs`
- Tests: `test/{SystemName}.Tests[/{SubsystemName}...]/{Item}Tests.cs`

OTS items have integration/usage design documentation parallel to system folders:

- Requirements: `docs/reqstream/ots/{ots-name}.yaml`
- Design: `docs/design/ots/{ots-name}.md`
- Verification: `docs/verification/ots/{ots-name}.md`

Shared Packages have integration/usage design documentation parallel to system and OTS folders:

- Requirements: `docs/reqstream/shared/{package-name}.yaml`
- Design: `docs/design/shared/{package-name}.md`
- Verification: `docs/verification/shared/{package-name}.md`

Review-sets: defined in `.reviewmark.yaml`

## References

- [DictionaryMark releases](https://github.com/demaconsulting/DictionaryMark/releases) —
  compiled verification design, design, and requirements documents are published as release
  artifacts on this page.
