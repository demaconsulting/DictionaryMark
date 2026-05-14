# Introduction

This document provides the verification design for the Template DotNet Tool, a .NET command-line
application demonstrating best practices for DEMA Consulting DotNet Tools.

## Purpose

The purpose of this document is to describe how each requirement for the Template DotNet Tool is
verified. For every software item — system, subsystem, and unit — this document names the
verification approach, identifies the test scenarios (including boundary conditions and error
paths), describes what is mocked or stubbed, and maps each requirement to at least one named
test scenario. The document does not restate design; it explains how the design is proven correct.

## Scope

This document covers the verification design for the same software items described in the
*Template DotNet Tool Software Design Document*:

- **TemplateDotNetTool** — the system as a whole
- **Program** — entry point and execution orchestrator
- **Cli** — command-line interface subsystem
  - **Context** — argument parser and I/O owner
- **SelfTest** — self-validation subsystem
  - **Validation** — self-validation test runner
- **Utilities** — shared utility subsystem
  - **PathHelpers** — safe path combination utilities

The following topics are out of scope:

- Test infrastructure (xUnit framework, test helpers, Runner utility)
- Build pipeline and CI/CD configuration

The following OTS items are also covered:

- **BuildMark** — build-notes documentation tool
- **FileAssert** — document assertion tool
- **Pandoc** — Markdown-to-HTML conversion tool
- **ReqStream** — requirements traceability tool
- **ReviewMark** — file review enforcement tool
- **SarifMark** — SARIF report conversion tool
- **SonarMark** — SonarCloud quality report tool
- **VersionMark** — tool-version documentation tool
- **WeasyPrint** — HTML-to-PDF conversion tool
- **xUnit** — unit-testing framework

## Software Structure

The following tree shows the software items covered by this document:

```text
TemplateDotNetTool (System)
├── Program (Unit)
├── Cli (Subsystem)
│   └── Context (Unit)
├── SelfTest (Subsystem)
│   └── Validation (Unit)
└── Utilities (Subsystem)
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

- Requirements: `docs/reqstream/{system}/.../{item}.yaml` (kebab-case)
- Design docs: `docs/design/{system}/.../{item}.md` (kebab-case)
- Verification design: `docs/verification/{system}/.../{item}.md` (kebab-case)
- Source code: `src/{System}/.../{Item}.cs` (PascalCase for C#)
- Tests: `test/{System}.Tests/.../{Item}Tests.cs` (PascalCase for C#)

OTS items have parallel artifacts in:

- Requirements: `docs/reqstream/ots/{ots-name}.yaml` (kebab-case)
- Verification: `docs/verification/ots/{ots-name}.md` (kebab-case)

Review-sets: defined in `.reviewmark.yaml`
