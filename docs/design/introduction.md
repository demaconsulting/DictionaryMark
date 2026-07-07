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

Local items:

- **DictionaryMark**: system, subsystem, and unit design.

OTS items:

- **YamlDotNet**: integration and usage design.
- **Microsoft.Extensions.FileSystemGlobbing**: integration and usage design.
- **DemaConsulting.TestResults**: integration and usage design.

Out of scope: test projects, build pipeline configuration, deployment and packaging, and external
library internals.

## Software Structure

The software structure is modeled in SysML2 under `docs/sysml2/` and rendered to the
diagram below by SysML2Tools as part of the build pipeline. AI agents should query the
SysML2 model directly (see the `sysml2tools-query` skill) rather than parsing this
diagram or the prose below.

![Software Structure](SoftwareStructureView.svg)

## Folder Layout

- **src/** - source files and projects
  - **DemaConsulting.DictionaryMark/** - DictionaryMark system source
    - **Cli/** - Cli subsystem
    - **Dictionary/** - Dictionary subsystem
    - **SelfTest/** - SelfTest subsystem
    - **Utilities/** - Utilities subsystem
- **test/** - test projects
  - **DemaConsulting.DictionaryMark.Tests/** - DictionaryMark test source
    - **Cli/** - Cli subsystem tests
    - **Dictionary/** - Dictionary subsystem tests
    - **SelfTest/** - SelfTest subsystem tests
    - **Utilities/** - Utilities subsystem tests

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
