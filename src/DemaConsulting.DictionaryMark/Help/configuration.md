---
title: Configuration file
summary: 'The configuration file: formatting defaults, table jobs, root, and precedence.'
aliases:
  - config
  - config-file
  - tables
  - jobs
  - root
  - precedence
tags:
  - reference
order: 6
see-also:
  - output-formats
  - file-access
  - troubleshooting
examples:
  - name: sample
    kind: source
    language: yaml
    save-as: dictionarymark.yaml
    description: Defaults and two table jobs, one of deprecated terms.
    content: |
      format: table
      depth: 2
      sort: alpha
      column-widths: [25, 75]
      tables:
        - input: "*terms.yaml"
          output: generated/glossary.md
          section: Glossary
        - input: retired.yaml
          output: generated/retired.md
          section: Retired Terms
          kind: deprecated
          column-widths: [20, 50, 30]
  - name: run
    kind: command
    language: shell
    description: Write every table of the configuration file.
    content: '{{tool}} --config dictionarymark.yaml'
  - name: override
    kind: command
    language: shell
    description: Write every table as a bullet list instead.
    content: '{{tool}} --config dictionarymark.yaml --format bullets'
  - name: with-input
    kind: command
    language: shell
    description: A file with tables does not take --input. Exits 1.
    content: '{{tool}} --config dictionarymark.yaml --input terms.yaml'
    exit: 1
    expect: 'because it defines table jobs'
---
A configuration file keeps the formatting options, and optionally the
table jobs of a run, in a YAML file beside the dictionaries. Pass it
with `--config <file>`; [[#sample]] prints a complete one.

**Formatting keys** may be given at the top level, as defaults, or in a
job: `format`, `section`, `depth`, `sort`, `kind`, `term-header`,
`def-header` (or `definition-header`, not both), `use-header`,
`where-header`, and `column-widths` as a YAML list. Their values follow
the rules of the options of the same names.

**Other keys.** `root` and `tables` are allowed only at the top level,
and `tables` must be a non-empty list. Each job needs `input`, a pattern
or a list of patterns, and `output`. Any other key is an error, and so
are `input` and `output` at the top level. `log`, `results`, `validate`,
`silent`, `version`, and `help` are command-line options only.

**Precedence.** A command-line option applies to every job and wins over
the key of the job, which wins over the top-level key, which wins over
the default. The command that [[#override]] prints writes
every table as a bullet list.

**Without `tables`** the file supplies only defaults and `root`, and the
inputs and output come from `--input` and `--output`. **With `tables`**
the file decides the inputs and outputs, so `--input` and `--output` are
errors; `--silent` and `--log` are allowed.

**Paths.** Relative paths in the file are taken from the folder of the
file, or from `root` when it is set; a relative `root` is taken from the
folder of the file. `--root <dir>` on the command line wins over the
`root` key. Neither moves `--config`, `--log`, or `--results`, which are
taken from the current folder. An output file replaces an existing one,
and missing folders are created.

**Jobs.** Every job runs, even after another fails. A failure names the
job, as in `table job 2 ('generated/retired.md')`, and the run ends
with `Error: 1 of 2 table jobs failed.` and exit code 1. A problem in
the file itself, such as an unknown key or a wrong value, stops the run
before any job, names the file, the job, and the key, and is always
printed, even with `--silent`.

**Environment variables.** `${VAR}` in the `root`, `input`, and `output`
of the file is expanded only in the default file-access mode, `any`;
every other mode refuses it; see [[file-access]].
