---
title: Command-line options
summary: Every option of the tool, the order of a run, errors, and exit codes.
aliases:
  - options
  - flags
  - usage
  - exit-codes
tags:
  - reference
order: 2
see-also:
  - output-formats
  - configuration
  - troubleshooting
examples:
  - name: usage
    kind: command
    language: shell
    description: Print the short usage text. It ends with a pointer to this help.
    content: '{{tool}} --help'
    expect: "Run '{{tool}} help' for topics and examples."
  - name: no-input
    kind: command
    language: shell
    description: Without options the tool prints a hint and exits 0.
    content: '{{tool}}'
    expect: 'No input files specified.'
  - name: silent-log
    kind: command
    language: shell
    description: Hide the console output and keep it in a log file.
    content: '{{tool}} --silent --log output.log --input terms.yaml'
  - name: unknown-option
    kind: command
    language: shell
    description: An unknown option is printed even with --silent. Exits 1.
    content: '{{tool}} --silent --unknown'
    exit: 1
    expect: "Error: Unsupported argument '--unknown'"
  - name: missing-value
    kind: command
    language: shell
    description: An option without its value is an error. Exits 1.
    content: '{{tool}} --input'
    exit: 1
    expect: '--input requires an input file pattern'
---
Options may be given in any order. The general options:

| Option | Meaning |
| --- | --- |
| `-v`, `--version` | Print only the version. |
| `-?`, `-h`, `--help` | Print the short usage text. |
| `--silent` | Hide console output. Errors still set exit code 1. |
| `--log <file>` | Also write the output to a file, replacing it. |
| `--validate` | Run the self-tests; see [[self-validation]]. |
| `--results <file>` | Write the self-test results as `.trx` or `.xml`. |
| `--depth <n>` | Heading level, 1 to 6, of the output; default 1. |

`--result` is another name of `--results`.

The options that generate Markdown:

| Option | Meaning |
| --- | --- |
| `-i`, `--input <pattern>` | A dictionary file or glob pattern; repeatable. |
| `-o`, `--output <file>` | The Markdown file; default the console. |
| `-f`, `--format <format>` | `bullets` (default) or `table`. |
| `-s`, `--section <text>` | The text of a heading above the output. |
| `--sort <order>` | `file` (default) or `alpha`. |
| `--kind <kind>` | `terms` (default) or `deprecated`. |
| `--term-header <text>` | The header of the term column. |
| `--def-header <text>` | The header of the definition column. |
| `--use-header <text>` | The header of the use column. |
| `--where-header <text>` | The header of the where column. |
| `--column-widths <list>` | Relative column widths, such as `25,75`. |
| `--config <file>` | A configuration file of defaults and tables. |
| `--root <dir>` | The base folder of relative paths. |
| `--file-access <mode>` | The files that a configuration may use. |
| `--allow-root <dir>` | A folder those files may be in; repeatable. |

`--definition-header` is another name of `--def-header`. For the
formats and headers see [[output-formats]], for deprecated terms
[[deprecated-terms]], for `--config` and `--root`
[[configuration]], and for the last two options [[file-access]].

**Names and values.** Option names are exact. The values of `--format`,
`--sort`, `--kind`, and `--file-access` ignore letter case. `--input`
and `--allow-root` may be repeated, `--config` and `--root` may be given
only once, and for any other option the last value wins.

**Order.** `--version`, when all arguments are valid, prints only the
version and skips everything else. It still needs the `--config` file,
the `--root` and `--allow-root` folders, and the `--log` file to be
usable, because the tool checks them before the run starts. Otherwise
the tool prints a banner, then handles the first of `--help`,
`--validate`, and the generation of Markdown. Without an input or a
configuration with tables, it prints a hint, as the command that
[[#no-input]] prints shows, and exits 0.

**Errors that are always printed.** An unknown argument, an option
without its value or with a wrong value, a repeated `--config` or
`--root`, a wrong number of column widths, a mistake in the file-access
options, a missing `--root` folder, any problem in the configuration
file, and a log file that cannot be opened stop the run before
`--silent` and `--log` take effect. They print `Error: ...`, are never
logged, and exit 1, as the command that [[#unknown-option]]
prints shows.

**Errors that `--silent` hides.** A missing input, a conflict, invalid
YAML in a dictionary, a failed write, and a failed self-test are found
during the run. `--silent` hides them on the console, the `--log` file
still gets them, and the exit code is still 1. `--silent` also hides
Markdown written to the console, so the command that
[[#silent-log]] prints writes the Markdown to the log file only.

**Exit codes.** 0 means success, and 1 means any error. The `help`
command exits 0, or 2 for an unknown topic or a wrong help argument.

**Help.** `help` is a command, not an option, and must be the first
argument. `--silent` and `--log` do not apply to it.
