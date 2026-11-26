---
title: Troubleshooting
summary: Error messages, a silent failure, and fixes for common mistakes.
aliases:
  - errors
  - problems
  - conflicts
tags:
  - troubleshooting
order: 9
see-also:
  - dictionary-files
  - configuration
  - command-line
examples:
  - name: conflict
    kind: command
    language: shell
    description: Two files define one term differently. Exits 1.
    content: '{{tool}} --input "conflict-*.yaml"'
    exit: 1
    expect: "Conflict: term 'API' differs in definition"
  - name: no-match
    kind: command
    language: shell
    description: A pattern that matches no file. Exits 1.
    content: '{{tool}} --input "*.yml"'
    exit: 1
    expect: 'matched no files'
  - name: missing
    kind: command
    language: shell
    description: A file name that does not exist. Exits 1.
    content: '{{tool}} --input absent.yaml'
    exit: 1
    expect: 'was not found'
  - name: bad-entry
    kind: command
    language: shell
    description: A rich entry without a definition. Exits 1.
    content: '{{tool}} --input bad-entry.yaml'
    exit: 1
    expect: "'definition' is required"
  - name: bad-config
    kind: command
    language: shell
    description: A configuration file with an unknown key. Exits 1.
    content: '{{tool}} --config bad-config.yaml'
    exit: 1
    expect: "unknown key 'theme'"
  - name: silent-error
    kind: command
    language: shell
    description: With --silent the error is hidden. Still exits 1.
    content: '{{tool}} --silent --input absent.yaml'
    exit: 1
  - name: help-not-first
    kind: command
    language: shell
    description: help after an option is not the help command. Exits 1.
    content: '{{tool}} --silent help'
    exit: 1
    expect: "Unsupported argument 'help'"
---
**`Conflict: term '...' differs in ...`.** The same term has different
entries in two inputs, as in [[#conflict]]. The message names
the fields that differ and both files. Make the entries identical, or
remove one of them; see [[dictionary-files]].

**`matched no files` or `was not found`.** A pattern matched nothing, or
a file does not exist. Check the current folder, or the folder of the
configuration file for its tables. Quote a glob pattern so that the
shell does not expand it.

**`Invalid YAML in file ...`.** A dictionary file is not valid. The
message names the file and the term, such as a rich entry without a
`definition` ([[#bad-entry]]), an unknown key, or a wrong value.

**`Config file '...': ...`.** The configuration file has a problem,
such as an unknown key ([[#bad-config]]). The message names the
file, the table job, and the key. Nothing is generated.

**Nothing printed, but exit code 1.** `--silent` hides the errors found
while generating, as in [[#silent-error]]. Add `--log <file>`
to keep them.

**The Markdown starts with the banner.** Output printed to the console
follows the banner of the tool. Use `--output <file>` to write only the
Markdown.

**`Unsupported argument 'help'`.** `help` is the help command only as
the first argument ([[#help-not-first]]); anywhere else it is an
unknown argument.

**`file-access-denied`.** The file-access mode refused an input or
output of a table; see [[file-access]].
