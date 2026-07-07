---
name: SysML2 Modeling
description: Follow these standards when creating or modifying the SysML2 architecture
  model, its rendered views, or the software structure it feeds into design documentation.
globs: ["docs/sysml2/**/*.sysml"]
---

# SysML2 Modeling Standard

## Required Standards

Read these standards first before applying this standard:

- **`software-items.md`** - Software categorization (System/Subsystem/Unit/OTS/Shared Package)
- **`design-documentation.md`** - Design document structure that embeds the diagrams this
  standard produces

## Purpose

The repository's software structure is modeled in SysML2 under `docs/sysml2/` rather than
hand-maintained as prose or an ASCII tree. The model is the authoritative, machine-queryable
source of structure; rendered diagrams and `docs/design/introduction.md`'s narrative are
generated/derived artifacts. AI agents should query the model (see `sysml2tools-query`
skill) before deep-diving into source code to understand what a piece of the system is,
why it exists, and what it depends on.

## Repository Structure

```text
docs/sysml2/
├── {system-name}.sysml           # one file per system: subsystems, units, doc comments
├── ots.sysml                     # OTS dependency parts (optional; if OTS items exist)
├── shared.sysml                  # Shared Package parts (optional; if Shared Packages exist)
└── views/
    └── design-views.sysml        # named view usages rendered for the design document
```

There is no separate "stable entry point" file — a repository may contain multiple systems,
so no single alias name would generalize. Agents discover the system(s) present by running
`query list --kind "part def"` or `query find` (see the `sysml2tools-query` skill) over
`docs/sysml2/*.sysml`, not by assuming a fixed name.

Always pass the full set of `.sysml` files (or an equivalent glob) to every `sysml2tools`
invocation — the model spans multiple files and cross-references between them. Note:
`sysml2tools` does not expand `*` glob patterns itself; either let the shell expand an
unquoted wildcard, or list files explicitly in scripts/CI.

## Model Content

- `{system-name}.sysml` defines one `part def` per System, Subsystem, and Unit, with a
  `doc /* ... */` comment on each stating its purpose — mirroring what would otherwise be
  written as prose in `docs/design/introduction.md`'s Software Structure section.
- Subsystems nest as `part` usages inside their parent's `part def`, matching the Software
  Item Hierarchy in `software-items.md`.
- `ots.sysml` / `shared.sysml` define one `part def` per OTS item / Shared Package, referenced
  as `part` usages from the system(s) that depend on them.

## Views (`docs/sysml2/views/design-views.sysml`)

Views control what gets rendered into diagrams for the design document. Use named `view`
**usages** (not `view def` **definitions**) — `expose` is only valid inside a usage:

```sysml
package {SystemName} {
    view SoftwareStructureView {
        expose {SystemName};         // whole package: system + subsystems + units
        render asTreeDiagram;
    }

    view {SystemName}View {
        expose {SystemName}System;   // system def only, not expanded into subsystems
        render asTreeDiagram;
    }

    view {SubsystemName}View {
        expose {SubsystemName};      // one subsystem def only
        render asTreeDiagram;
    }
}
```

**Critical distinction** (do not confuse these — this cost significant trial-and-error to
discover): `expose <name>;` is what scopes a rendered diagram's content (the union of the
named element's containment subtree). `render <kind>;` selects a rendering *style* (e.g.
`asTreeDiagram`) — it has **no effect on scope**. `expose` is only legal inside a named
`view Name { ... }` **usage**; it is a syntax/semantic error inside a `view def Name { ... }`
**definition**. `expose` targets must be `::`-qualified or locally-resolvable names, not
dotted member-access chains (`expose foo.bar;` is invalid — use `expose Bar;`).

**Naming convention** — one `View` suffix per rendered diagram, no `System`/`Subsystem`
suffix duplication:

- `SoftwareStructureView` - full detail: every system, subsystem, and unit in one diagram
- `{SystemName}View` - one per system, direct members only (not expanded into subsystems)
- `{SubsystemName}View` - one per subsystem (at any nesting depth), that subsystem's own
  members only

When a repository has multiple systems, `SoftwareStructureView` may expose each system's
package individually (`expose {SystemNameA}; expose {SystemNameB};`) or the whole workspace,
whichever produces a single coherent overview diagram.

## Diagram Embedding

Render with:

```pwsh
dotnet sysml2tools render `
  docs/sysml2/{system-name}.sysml docs/sysml2/ots.sysml docs/sysml2/shared.sysml `
  docs/sysml2/views/design-views.sysml --output docs/design/generated --format svg
```

With multiple views declared and no `--view` flag, `sysml2tools` renders every declared view
in one invocation, one file per view, using each view's own name as the filename
(`{ViewName}.svg`) — no post-render rename step is needed.

Embed diagrams in `docs/design/` per this rule: every design doc for an item embeds the
diagram for the narrowest view that still shows that item's own immediate structure —

- `docs/design/introduction.md` — `SoftwareStructureView.svg` (the full-detail overview)
- `docs/design/{system-name}.md` and every unit doc for a unit directly under the system
  (no subsystem parent) — `{SystemName}View.svg`
- `docs/design/{system-name}/{subsystem-name}.md` and every unit doc nested under that
  subsystem (at any depth) — `{SubsystemName}View.svg`

Place the image directly under the file's top-level heading, before its first prose
subsection, e.g. `![{Name} Structure]({ViewName}.svg)`.

## Build and Lint Integration

- `sysml2tools lint docs/sysml2/*.sysml` belongs in `lint.ps1`'s compliance-tools section,
  **not** as a separate step in the CI design-document job — `lint.ps1` gates every later
  job (including document generation) transitively, so linting the model there is both
  earlier and non-duplicated.
- The CI design-document job renders the views (see the render command above) before running
  Pandoc, so generated SVGs exist before HTML generation. Rename/stable-filename workarounds
  are unnecessary since view names are stable by definition.
- Add `sysml2tools` to `.config/dotnet-tools.json` (`demaconsulting.sysml2tools.tool`) and to
  `.versionmark.yaml`'s captured tool list.

## Fallback

If the model is stale, doesn't yet cover an item, or a query returns nothing useful, fall
back to `grep`/`glob`/reading source directly. When work adds or changes a Unit/Subsystem,
update the corresponding `.sysml` model file in the same change — this mirrors the existing
requirement to keep `docs/design/*.md` and `docs/reqstream/*.yaml` companion artifacts in
sync.

## Quality Checks

- [ ] Every System/Subsystem/Unit in `docs/design/introduction.md` has a matching `part def`
  in `docs/sysml2/{system-name}.sysml` with a purpose `doc` comment
- [ ] `docs/sysml2/views/design-views.sysml` uses `view Name { expose ...; }` usages, not
  `view def` definitions
- [ ] View names follow the `SoftwareStructureView` / `{SystemName}View` /
  `{SubsystemName}View` convention
- [ ] `sysml2tools lint` passes on all `.sysml` files
- [ ] Rendered diagrams are embedded in every design doc per the Diagram Embedding rule
- [ ] `sysml2tools` is present in `lint.ps1`, `.config/dotnet-tools.json`, and
  `.versionmark.yaml`
