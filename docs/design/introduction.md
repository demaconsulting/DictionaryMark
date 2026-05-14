# Introduction

This document provides the detailed design for the Template DotNet Tool, a .NET command-line
application demonstrating best practices for DEMA Consulting DotNet Tools.

## Purpose

The purpose of this document is to describe the internal design of each software unit that
comprises the Template DotNet Tool. It captures data models, algorithms, key methods, and
inter-unit interactions at a level of detail sufficient for formal code review, compliance
verification, and future maintenance. The document does not restate requirements; it explains
how they are realized.

## Scope

This document covers the detailed design of the following subsystems and software units:

- **TemplateDotNetTool** — the system as a whole (`template-dot-net-tool.md`)
- **Program** — entry point and execution orchestrator (`Program.cs`)
- **Cli** subsystem
  - **Context** — command-line argument parser and I/O owner (`Cli/Context.cs`)
- **SelfTest** subsystem
  - **Validation** — self-validation test runner (`SelfTest/Validation.cs`)
- **Utilities** subsystem
  - **PathHelpers** — safe path combination utilities (`Utilities/PathHelpers.cs`)

The following topics are out of scope:

- External library internals
- Build pipeline configuration
- Deployment and packaging

## Software Structure

The following tree shows how the Template DotNet Tool software items are organized across the
system, subsystem, and unit levels:

```text
TemplateDotNetTool (System)
├── Program (Unit)
├── Cli (Subsystem)
│   └── Context (Unit)
├── SelfTest (Subsystem)
│   └── Validation (Unit)
└── Utilities (Subsystem)
    └── PathHelpers (Unit)
```

Each unit is described in detail in its own chapter within this document.

## Folder Layout

The source code folder structure mirrors the top-level subsystem breakdown above, giving
reviewers an explicit navigation aid from design to code:

```text
src/DemaConsulting.TemplateDotNetTool/
├── Program.cs                  — entry point and execution orchestrator
├── Cli/
│   └── Context.cs              — command-line argument parser and I/O owner
├── SelfTest/
│   └── Validation.cs           — self-validation test runner
└── Utilities/
    └── PathHelpers.cs          — safe path combination utilities
```

The test project mirrors the same layout under `test/DemaConsulting.TemplateDotNetTool.Tests/`.

## Document Conventions

Throughout this document:

- Class names, method names, property names, and file names appear in `monospace` font.
- The word **shall** denotes a design constraint that the implementation must satisfy.
- Section headings within each unit chapter follow a consistent structure: overview, data model,
  methods/algorithms, and interactions with other units.
- Text tables are used in preference to diagrams, which may not render in all PDF viewers.

## Companion Artifact Structure

Each software item in the structure above has corresponding artifacts in parallel directory trees:

- Requirements: `docs/reqstream/{system}/.../{item}.yaml` (kebab-case)
- Design docs: `docs/design/{system}/.../{item}.md` (kebab-case)
- Verification design: `docs/verification/{system}/.../{item}.md` (kebab-case)
- Source code: `src/{System}/.../{Item}.cs` (PascalCase for C#)
- Tests: `test/{System}.Tests/.../{Item}Tests.cs` (PascalCase for C#)
- Review-sets: defined in `.reviewmark.yaml`

## References

- [REF-1] Template DotNet Tool User Guide — `docs/user_guide/introduction.md`
- [REF-2] Template DotNet Tool Repository — `https://github.com/demaconsulting/TemplateDotNetTool`
