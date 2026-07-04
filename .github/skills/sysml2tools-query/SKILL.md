---
name: sysml2tools-query
description: Query this repository's SysML2 architecture model (docs/sysml2/) to understand software structure, purpose, and relationships before deep-diving into source code. Use this when asked to understand the codebase, find which unit implements something, assess the impact of a change, or trace requirements to code.
---

# SysML2Tools Query Skill

This repository models its software structure in SysML2 under `docs/sysml2/`. Prefer
querying this model over grepping/reading source files when you need to understand
*what* a piece of the system is, *why* it exists, or *what it depends on*.

## Prerequisites

The `sysml2tools` CLI is a local .NET tool pinned in `.config/dotnet-tools.json`.
Restore it once per session if not already available:

```pwsh
dotnet tool restore
```

## Model files

- `docs/sysml2/system.sysml` — **stable entry point**, identical across all DEMA
  Consulting repositories. Defines `part def System`, which specializes the
  project's actual top-level system part def.
- `docs/sysml2/{project-slug}.sysml` — the project-specific package (subsystems,
  units, purpose `doc` comments, and dependency usages).
- `docs/sysml2/ots.sysml` — off-the-shelf (OTS) dependency parts.

Always pass all three files (or a glob covering them) to every `sysml2tools`
invocation — the model spans multiple files and cross-references between them.
Note: `sysml2tools` does not expand `*` glob patterns itself; either let the shell
expand an unquoted wildcard, or list the files explicitly:

```pwsh
dotnet sysml2tools query list docs/sysml2/system.sysml docs/sysml2/*.sysml
```

## Recommended workflow

1. **Start at the stable alias** to learn the real qualified name of the system,
   regardless of which DEMA tool repository you are in:

   ```pwsh
   dotnet sysml2tools query describe -e System docs/sysml2/system.sysml docs/sysml2/*.sysml
   ```

   The `Supertypes:` line gives you the project-specific qualified name (e.g.
   `DictionaryMark::DictionaryMarkSystem`) to use in subsequent queries.

2. **Get the full hierarchy** (subsystems and units):

   ```pwsh
   dotnet sysml2tools query hierarchy -e <QualifiedName> --direction down docs/sysml2/system.sysml docs/sysml2/*.sysml
   dotnet sysml2tools query list docs/sysml2/system.sysml docs/sysml2/*.sysml
   ```

3. **Understand a specific unit's purpose** before opening its source file:

   ```pwsh
   dotnet sysml2tools query describe -e <QualifiedName> docs/sysml2/system.sysml docs/sysml2/*.sysml
   ```

4. **Assess impact before editing a unit** — see what depends on it:

   ```pwsh
   dotnet sysml2tools query used-by -e <QualifiedName> docs/sysml2/system.sysml docs/sysml2/*.sysml
   dotnet sysml2tools query impact -e <QualifiedName> docs/sysml2/system.sysml docs/sysml2/*.sysml
   ```

5. **Trace requirements** linked to a unit, if modeled:

   ```pwsh
   dotnet sysml2tools query requirements -e <QualifiedName> docs/sysml2/system.sysml docs/sysml2/*.sysml
   ```

6. **Search by name or kind** when the qualified name is unknown:

   ```pwsh
   dotnet sysml2tools query find --name MarkdownFormatter docs/sysml2/system.sysml docs/sysml2/*.sysml
   dotnet sysml2tools query list --kind "part def" docs/sysml2/system.sysml docs/sysml2/*.sysml
   ```

Use `--format json` on any query verb for machine-parsed output.

## Fallback

If the model is stale, doesn't yet cover a unit, or a query returns nothing useful,
fall back to `grep`/`glob`/reading source directly. When you finish work that adds
or changes a Unit/Subsystem, update the corresponding `.sysml` model file in the
same change (mirrors the existing requirement to keep `docs/design/*.md` and
`docs/reqstream/*.yaml` companion artifacts in sync).

## Validating changes to the model

Before committing changes to any `.sysml` file, lint it:

```pwsh
dotnet sysml2tools lint docs/sysml2/system.sysml docs/sysml2/*.sysml
```
