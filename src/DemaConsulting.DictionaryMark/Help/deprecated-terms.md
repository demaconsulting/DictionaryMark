---
title: Deprecated terms
summary: The YAML format of deprecated terms and the table that lists them.
aliases:
  - deprecated
  - retired
  - kind
tags:
  - reference
order: 4
see-also:
  - dictionary-files
  - output-formats
examples:
  - name: sample
    kind: source
    language: yaml
    save-as: deprecated.yaml
    description: A file of two deprecated terms.
    content: |
      bench mode:
        use: Test Mode
        where: Product A
      setup screen:
        use: Settings page
  - name: table
    kind: command
    language: shell
    description: Print the deprecated terms as a table.
    content: '{{tool}} --input deprecated.yaml --kind deprecated --format table'
    expect: '| Not used | Use instead | Where it was used |'
  - name: bullets
    kind: command
    language: shell
    description: Print the deprecated terms as a bullet list.
    content: '{{tool}} --input deprecated.yaml --kind deprecated'
    expect: '- **bench mode**: Test Mode (where: Product A)'
  - name: headers
    kind: command
    language: shell
    description: Print the table with other column headers.
    content: '{{tool}} --input deprecated.yaml --kind deprecated --format table --term-header Avoid --use-header Prefer'
    expect: '| Avoid | Prefer |'
---
A deprecated-terms file lists terms that must not be used, each with the
term to use instead; [[#sample]] prints a complete one. Read it
with `--kind deprecated`, which applies to every input of the run or of
the table job.

**Entries.** The root is a YAML mapping of terms to objects. Each object
has a required, non-empty `use` and an optional `where`, and no
definition. Any other key is an error that names the term and the file.

**Table.** The default headers are `Not used`, `Use instead`, and
`Where it was used`; change them with `--term-header`, `--use-header`,
and `--where-header`. `--def-header` does not apply. The where column is
printed only when at least one entry has `where`.

**Bullets.** Each term prints as `- **term**: use`, followed by
`(where: ...)` when the entry has `where`.

**Column widths.** `--column-widths` takes exactly 3 values for a
deprecated table. When the where column is left out, the third value is
dropped and the other two are scaled to the full width.

**Several files.** The same deprecated term may appear in several files
when `use` and `where` are the same; otherwise it is a conflict, which
is an error.
