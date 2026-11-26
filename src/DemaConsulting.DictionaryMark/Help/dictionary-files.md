---
title: Dictionary files
summary: 'The YAML format of approved terms: definitions, rich entries, and conflicts.'
aliases:
  - dictionary
  - dictionaries
  - glossary
  - terms
  - entries
  - yaml
tags:
  - reference
order: 3
see-also:
  - deprecated-terms
  - output-formats
  - troubleshooting
examples:
  - name: sample
    kind: source
    language: yaml
    save-as: glossary.yaml
    description: A dictionary with simple and rich entries.
    content: |
      API: Application Programming Interface
      Test Mode: A mode in which Product A runs self-checks.
      Product A:
        definition: The main control unit.
        pos: proper-noun
      bleed valve:
        definition: A valve that releases trapped air.
        pos: [noun]
        variants: [bleed-valve, air bleed valve]
        visible: false
  - name: bullets
    kind: command
    language: shell
    description: Print the visible terms as a bullet list.
    content: '{{tool}} --input glossary.yaml'
    expect: '- **API**: Application Programming Interface'
  - name: table
    kind: command
    language: shell
    description: Print the visible terms as a table.
    content: '{{tool}} --input glossary.yaml --format table --section Glossary'
    expect: '| Product A | The main control unit. |'
---
A dictionary file holds approved terms; [[#sample]] prints a complete
one. The root of the file is a YAML mapping of terms to entries. An empty
file has no terms, and any other root is an error.

**Terms.** A term is trimmed, and runs of white space become one space.
An empty term is an error, and so is a term that appears twice in one
file, even in another letter case.

**Entries.** An entry is a definition string, or an object with these
keys and no others:

| Key | Meaning |
| --- | --- |
| `definition` | Required, a non-empty string, even when hidden. |
| `pos` | Part of speech, one or a list; default `noun`. |
| `variants` | Other spellings, one or a list. |
| `visible` | `true` (default) or `false`. |

`pos` takes `noun`, `verb`, `adjective`, `adverb`, and `proper-noun`, in
any letter case. An unknown key, a repeated key, or a wrong value is an
error that names the term and the file.

**Output.** Only the term and the definition are printed; `pos` and
`variants` are never printed. An entry with `visible: false` is left out
of the output, but it is still checked for conflicts.

**Several files.** The same term may appear in several files when the
entries are identical. A different definition (compared exactly), a
different `pos` or `variants` (compared as sets, ignoring letter case),
or a different `visible` is a conflict, which is an error; see
[[troubleshooting]]. The tool does not check approved terms against
deprecated terms; for deprecated terms see [[deprecated-terms]].
