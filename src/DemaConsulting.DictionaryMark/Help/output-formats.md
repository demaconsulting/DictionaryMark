---
title: Output formats
summary: Lists and tables, headings, headers, sorting, column widths, and output files.
aliases:
  - output
  - format
  - markdown
  - table
  - bullets
  - headers
  - sort
  - column-widths
tags:
  - reference
order: 5
see-also:
  - dictionary-files
  - deprecated-terms
  - configuration
examples:
  - name: bullets
    kind: command
    language: shell
    description: Print a bullet list, the default format.
    content: '{{tool}} --input terms.yaml'
    expect: '- **API**: Application Programming Interface'
  - name: table
    kind: command
    language: shell
    description: Print a table under a level-2 heading.
    content: '{{tool}} --input terms.yaml --format table --section Glossary --depth 2'
    expect: '## Glossary'
  - name: headers
    kind: command
    language: shell
    description: Print a table with other column headers.
    content: '{{tool}} --input terms.yaml --format table --term-header Abbreviation --def-header Meaning'
    expect: '| Abbreviation | Meaning |'
  - name: merge
    kind: command
    language: shell
    description: Merge every matching file and sort the terms.
    content: '{{tool}} --input "*terms.yaml" --sort alpha'
    expect: '- **CI**: Continuous Integration'
  - name: widths
    kind: command
    language: shell
    description: Give the term column a quarter of the width.
    content: '{{tool}} --input terms.yaml --format table --column-widths 25,75'
    expect: '| Term | Definition |'
  - name: wrong-count
    kind: command
    language: shell
    description: A terms table takes 2 widths, not 3. Exits 1.
    content: '{{tool}} --input terms.yaml --format table --column-widths 20,50,30'
    exit: 1
    expect: 'require exactly 2 values'
  - name: to-file
    kind: command
    language: shell
    description: Write the Markdown to a file in a new folder.
    content: '{{tool}} --input terms.yaml --output docs/glossary.md'
---
**Bullets** are the default: each term prints as `- **term**: definition`,
as the command that [[#bullets]] prints shows.

**Tables.** `--format table` prints a `Term` and a `Definition` column,
left-aligned. A `|` in a cell is escaped, and a table without terms gets
one `N/A` row. Change the headers with `--term-header` and
`--def-header`, as in [[#headers]].

**Heading.** A heading is printed only with `--section`. `--depth` sets
its level, 1 to 6, and also the level of the `--validate` report.

**Inputs.** Repeat `--input`, or use a glob pattern, to merge several
files into one list or table; quote the pattern so that the shell does
not expand it. A pattern that matches no file, a missing file, and an
undefined `${VAR}` in a path are errors. `..` is allowed before the
first wildcard.

**Order.** `--sort file` (the default) keeps the order of the files and
of the terms in them, and `--sort alpha` sorts the terms ignoring letter
case. A term that appears in several inputs is printed once, as it
appears first.

**Column widths.** When a table is converted to HTML or PDF, the width
of each column follows the number of dashes under its header.
`--column-widths` takes positive whole numbers: exactly 2 for a terms
table and 3 for a deprecated table. The dashes are scaled to about 100
in total, with at least 3 per column, so `25,75` gives 25 and 75
dashes. A bullet list ignores the widths.

**Output.** Without `--output`, the Markdown is printed after the banner
of the tool. `--output` writes only the Markdown to the file, replacing
it, and creates missing folders, as in [[#to-file]].

**Paths.** Relative paths are taken from the current folder, or from the
root folder; see [[configuration]].
