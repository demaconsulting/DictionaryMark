# DictionaryMark

<!-- IMPORTANT: All links in this file must be absolute URLs.
     This file is distributed in packages and relative links will not resolve. -->

[![GitHub forks](https://img.shields.io/github/forks/demaconsulting/DictionaryMark?style=plastic)](https://github.com/demaconsulting/DictionaryMark/network/members)
[![GitHub stars](https://img.shields.io/github/stars/demaconsulting/DictionaryMark?style=plastic)](https://github.com/demaconsulting/DictionaryMark/stargazers)
[![GitHub contributors](https://img.shields.io/github/contributors/demaconsulting/DictionaryMark?style=plastic)](https://github.com/demaconsulting/DictionaryMark/graphs/contributors)
[![License](https://img.shields.io/github/license/demaconsulting/DictionaryMark?style=plastic)](https://github.com/demaconsulting/DictionaryMark/blob/main/LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/demaconsulting/DictionaryMark/build_on_push.yaml)](https://github.com/demaconsulting/DictionaryMark/actions/workflows/build_on_push.yaml)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=demaconsulting_DictionaryMark&metric=alert_status)](https://sonarcloud.io/dashboard?id=demaconsulting_DictionaryMark)
[![Security](https://sonarcloud.io/api/project_badges/measure?project=demaconsulting_DictionaryMark&metric=security_rating)](https://sonarcloud.io/dashboard?id=demaconsulting_DictionaryMark)
[![NuGet](https://img.shields.io/nuget/v/DemaConsulting.DictionaryMark?style=plastic)](https://www.nuget.org/packages/DemaConsulting.DictionaryMark)

## Overview

DictionaryMark is a .NET command-line tool that reads YAML dictionary files and generates
formatted Markdown output as either bullet lists or tables. It supports glob patterns for
processing multiple files, detects conflicting definitions, and is designed for easy
integration into documentation pipelines. A YAML configuration file can hold the formatting defaults,
including relative table column widths, and several table jobs, so one run produces every table.

The workflow below shows how to use it: author YAML dictionary files, run `dictionarymark` from a
shell or CI pipeline with `--input` and `--format` options or a `--config` file of table jobs, and
use the one or more generated Markdown bullet lists or tables in your documents and documentation
pipeline.

![DictionaryMark workflow: author YAML dictionary files with simple or rich entries or deprecated terms, run
dictionarymark with --input and --format options or a --config file of table jobs, get one or more Markdown
bullet lists or tables, then embed them in documents, a docs pipeline or a CI
gate](https://raw.githubusercontent.com/demaconsulting/DictionaryMark/main/docs/images/workflow.png)

## Features

- 📄 **YAML Dictionary Input** - Read terms from YAML files, as simple definitions or rich entries
  (`definition`, `pos`, `variants`, `visible`)
- 📝 **Markdown Output** - Generate bullet lists or formatted tables
- 🚫 **Deprecated Terms** - Generate a "Not used / Use instead" table from deprecated-term files
- 🔍 **Glob Patterns** - Process multiple input files with wildcards
- ⚔️ **Conflict Detection** - Detect conflicting entries across files
- 🎯 **Customizable Output** - Control headings, column headers, sort order, and depth
- 📏 **Column Widths** - Size table columns in proportion to relative weights (for example `25,75`)
- ⚙️ **Configuration File** - Keep formatting options and several table jobs in a YAML file beside the
  dictionaries
- 📂 **Root Directory** - Resolve relative input and output paths against a chosen base directory
- 🔒 **File Access Confinement** - Confine the files a configuration file may read and write, judged by their
  real paths (`--file-access`, `--allow-root`)
- 🌐 **Multi-Platform** - Windows, Linux, and macOS on .NET 10 and 11
- ✅ **Self-Validation** - Built-in qualification tests for regulated environments

## Installation

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) 10.0 or 11.0

### Global Installation

Install DictionaryMark as a global .NET tool for system-wide use:

```bash
dotnet tool install --global DemaConsulting.DictionaryMark
```

Verify the installation:

```bash
dictionarymark --version
```

### Local Installation

Install DictionaryMark as a local tool in your project (recommended for team projects):

```bash
dotnet new tool-manifest  # if you don't have a tool manifest already
dotnet tool install DemaConsulting.DictionaryMark
```

Run the tool:

```bash
dotnet dictionarymark --version
```

### Update

To update to the latest version:

```bash
# Global installation
dotnet tool update --global DemaConsulting.DictionaryMark

# Local installation
dotnet tool update DemaConsulting.DictionaryMark
```

### Compatibility

| Component | Version | Status |
| --- | --- | --- |
| .NET SDK | 10.0 | ✅ Supported |
| .NET SDK | 11.0 | ✅ Supported |
| OS | Windows | ✅ Supported |
| OS | Linux | ✅ Supported |
| OS | macOS | ✅ Supported |

## Usage

```bash
dictionarymark --input glossary.yaml --format table --section "Glossary" --output docs/glossary.md
```

### Options

| Option | Description |
| :--- | :--- |
| `-v, --version` | Display version information |
| `-?, -h, --help` | Display help |
| `--silent` | Suppress console output |
| `--validate` | Run self-validation |
| `-i, --input <pattern>` | Input YAML file or glob pattern (repeatable) |
| `-o, --output <file>` | Output Markdown file (default: stdout) |
| `-f, --format <format>` | Output format: `bullets` (default) or `table` |
| `-s, --section <heading>` | Section heading text |
| `--term-header <header>` | Term column header for table format (default: Term) |
| `--def-header <header>` | Definition column header (default: Definition) |
| `--definition-header <header>` | Alias for `--def-header` |
| `--sort <order>` | Sort order: `file` (default) or `alpha` |
| `--log <file>` | Write output to log file |
| `--results <file>` | Write validation results (.trx or .xml) |
| `--result <file>` | Alias for `--results` |
| `--depth <n>` | Set heading depth (default: 1, range: 1–6) |
| `--kind <kind>` | Input kind: `terms` (default) or `deprecated` |
| `--use-header <header>` | Deprecated "use" column header |
| `--where-header <header>` | Deprecated "where" column header |
| `--column-widths <w1,w2[,w3]>` | Relative table column widths (e.g. `25,75`) |
| `--config <file>` | Read formatting defaults and table jobs from a YAML file |
| `--root <dir>` | Base directory for relative input and output paths |
| `--file-access <mode>` | Config path access: `any` (default), `roots`, `no-symlinks`, `roots,no-symlinks` |
| `--allow-root <dir>` | Folder that config file paths may use (repeatable; implies `roots`) |

Missing parent directories of output files are created. Input patterns may use `..` segments before
the first wildcard (for example `../shared/*.yaml`), and input and output paths may contain `${VAR}`
environment variables.

### Examples

**Generate a bullet list from a YAML dictionary:**

```bash
dictionarymark --input glossary.yaml
```

**Generate a table with a section heading:**

```bash
dictionarymark --input glossary.yaml --format table --section "Glossary" --output docs/glossary.md
```

**Merge multiple files and sort alphabetically:**

```bash
dictionarymark --input file1.yaml --input file2.yaml --sort alpha
```

**Process all YAML files in a directory:**

```bash
dictionarymark --input "terms/*.yaml" --format table --output docs/glossary.md
```

Given an input file `glossary.yaml`:

```yaml
API: Application Programming Interface
CLI: Command-Line Interface
SDK: Software Development Kit
```

Running:

```bash
dictionarymark --input glossary.yaml --format table --section "Glossary" --depth 1
```

Produces:

```markdown
# Glossary

| Term | Definition |
| :--- | :--- |
| API | Application Programming Interface |
| CLI | Command-Line Interface |
| SDK | Software Development Kit |
```

Or with `--format bullets`:

```markdown
# Glossary

- **API**: Application Programming Interface
- **CLI**: Command-Line Interface
- **SDK**: Software Development Kit
```

### A Good Glossary

Every approved term needs a non-empty `definition`, either as a plain string or inside an object
entry. An object entry may also carry `pos`, `variants` and `visible`. Entries with
`visible: false` are still checked for conflicts but are left out of the output. Here is a complete
small glossary, `terms.yaml`:

```yaml
Test Mode: A mode in which Product A runs self-checks and does not control the process.
Product A: The main control unit made by Acme.
Zorbix: A sealed connector that joins the bleed valve to the main line.
bleed valve:
  definition: A valve that releases trapped air.
  pos: noun
  variants: [bleed-valve]
  visible: false
```

Running `dictionarymark --input terms.yaml --format table --section Glossary` produces:

```markdown
# Glossary

| Term | Definition |
| :--- | :--- |
| Test Mode | A mode in which Product A runs self-checks and does not control the process. |
| Product A | The main control unit made by Acme. |
| Zorbix | A sealed connector that joins the bleed valve to the main line. |
```

With `--format bullets` the same file produces:

```markdown
# Glossary

- **Test Mode**: A mode in which Product A runs self-checks and does not control the process.
- **Product A**: The main control unit made by Acme.
- **Zorbix**: A sealed connector that joins the bleed valve to the main line.
```

The hidden `bleed valve` entry does not appear in either output.

### Deprecated Terms

A deprecated-terms file lists terms that must not be used. Each entry needs `use` and may have
`where`; there is no definition. Given `deprecated.yaml`:

```yaml
bench mode: { use: Test Mode, where: Product A }
Zorbix unit: { use: Zorbix }
```

Running
`dictionarymark --input deprecated.yaml --kind deprecated --format table --section "Deprecated Terms" --depth 2`
produces:

```markdown
## Deprecated Terms

| Not used | Use instead | Where it was used |
| :--- | :--- | :--- |
| bench mode | Test Mode | Product A |
| Zorbix unit | Zorbix |  |
```

The `Where it was used` column appears only when at least one entry has a `where` value. With
`--format bullets` the output is `- **bench mode**: Test Mode (where: Product A)` and
`- **Zorbix unit**: Zorbix`.

### Column Widths

When a table is converted to HTML or PDF (for example with Pandoc), the column widths follow the
number of dashes in the separator line. `--column-widths` sets those dash counts in proportion to
relative weights, scaled to about 100 dashes in total with at least 3 per column:

```bash
dictionarymark --input glossary.yaml --format table --column-widths 25,75
```

```markdown
| Term | Definition |
| :------------------------- | :--------------------------------------------------------------------------- |
| API | Application Programming Interface |
```

The weights must be positive integers. A terms table takes exactly 2 values and a deprecated table
exactly 3; when no deprecated entry has a `where` value, the third weight is dropped and the first two
are rescaled. Bullet output ignores the option. Without the option the output is unchanged.

### Configuration File

`--config <file.yaml>` reads formatting defaults, an optional root directory, and optional table jobs
from a YAML file, so the options can be versioned beside the dictionaries they describe:

```yaml
# docs/glossary/dictionarymark.yaml
format: table
depth: 2
sort: alpha
column-widths: [25, 75]
tables:
  - input: "*glossary.yaml"
    output: ../generated/glossary/terms.md
    section: Terms
  - input: [deprecated.yaml, ../../shared/deprecated.yaml]
    output: ../generated/glossary/deprecated.md
    section: Deprecated Terms
    kind: deprecated
    column-widths: [20, 50, 30]
```

```bash
dictionarymark --config docs/glossary/dictionarymark.yaml
```

- **Top-level keys** are formatting defaults: `format`, `section`, `depth`, `sort`, `term-header`,
  `def-header` (or `definition-header`), `kind`, `use-header`, `where-header` and `column-widths`
  (a YAML sequence), plus `root` and `tables`.
- **`tables`** is a list of jobs. Each job needs `input` (a pattern or a list of patterns) and
  `output`, and may set any formatting key.
- **Precedence**: a command-line option wins over a job key, which wins over a top-level default,
  which wins over the built-in default.
- **Without `tables`**, the file only supplies defaults, and `--input`/`--output` come from the
  command line. **With `tables`**, `--input` and `--output` are errors; `--silent` and `--log` are
  allowed.
- **Paths** in the file are relative to the file's directory (unless a root directory is set), may use
  `..` and `${VAR}`, and missing output directories are created.
- **Errors**: unknown or misplaced keys (for example `log` or `input` at the top level) are reported
  with the key, the job and the file. Every job runs; each failing job is reported as
  `table job {n} ('{output}')`, and the exit code is non-zero if any job failed.

### Root Directory

`--root <dir>` (relative to the working directory) or a top-level `root:` key in the configuration
file (relative to the file) sets the base directory for every relative input pattern and output path,
so the tool can run from any working directory:

```yaml
# tools/dictionarymark/dictionarymark.yaml
root: ../..
tables:
  - input: docs/glossary/*.yaml
    output: docs/generated/glossary.md
```

The base directory is `--root` if given, else the configuration `root`, else the configuration file's
directory (for paths from the file) or the working directory (for command-line paths). Absolute paths
ignore the root, and `--config`, `--log` and `--results` are never resolved against it. The root must
exist; `${VAR}` and `..` are supported. A root only sets the base directory: it never grants file access
(see *File Access* below).

### File Access

A configuration file may come from someone you do not trust, for example a pull request built by CI.
`--file-access <mode>` decides which files the paths written in a configuration file may read and write:

- **`any`** (the default): every regular local file, as in earlier versions.
- **`roots`**: only files inside an allowed folder. Both the path as written (after `${VAR}`, the base
  directory and `..`) and the real path of the opened file must be inside, so a symbolic link or
  junction that leads out is denied, and so is a path outside that links back in.
- **`no-symlinks`**: a path may not pass through a symbolic link or junction below the configuration
  file's folder; that folder and the folders above it are trusted.
- **`roots,no-symlinks`**: both rules.

The mode may be written in any letter case; anything else, or the option given twice, is an error that
lists the modes.

`--allow-root <dir>` names an allowed folder; it is repeatable, implies `roots`, and replaces the default
folders. A relative folder is taken from the working directory (never from `--root`), `${VAR}` and `..`
are supported, and a folder that does not exist is an error. With `--file-access any` or `no-symlinks` it
is an error that names the conflict. Without `--allow-root`, `roots` allows the working directory and the
configuration file's folder.

- **Judged paths**: every input pattern, every folder a pattern lists, every file a pattern matches, and
  every output of a configuration file, each before the tool touches it. The folder before a glob's first
  wildcard, and every link a wildcard reaches, is judged before it is listed: under `roots` it must be inside
  an allowed folder (so `../shared/*.yaml` is denied when `shared` is not allowed), and under `no-symlinks` a
  link there fails the job.
- **Trusted paths**: the paths on the command line (`--config`, `--input` and its matches, `--output`,
  `--log` and `--results`) are never judged.
- **Never accessed**, in every mode: a URL (`http://`, `https://` and other `scheme://` forms, and a `file:`
  URI with a host), a path written as a UNC path (`\\server\share`, `//server/share`), a device path (`\\.\`,
  `\\?\`, `NUL`), a path with a `:` other than the drive of an absolute Windows path (on Linux and macOS, any
  `:`), and anything that is not a regular file (a folder, pipe or device). The rules apply after `${VAR}`
  expansion and to the name of every file a glob matches (on Linux and macOS, `terms:v1.yaml` matched by
  `*.yaml` fails the job). Outputs under `/dev`, or linked there, are never written. A `root:` key naming
  such a path is an error.
- **Network shares**: as in CanvasNet, a configuration file on a share or mapped network drive works with
  relative paths. On Windows, a local path that leads to a network share through a symbolic link or junction
  is denied, including a link a glob wildcard reaches.
- **Real path unavailable**: a mode other than `any` needs the real path of every opened file; where the
  system cannot tell it, the file is denied, never used unchecked.
- **Writes**: under `roots` an output, every folder it creates, and the real path of the opened output
  must be inside an allowed folder. An existing output is never truncated before its real path is
  checked, and a denied job writes nothing, not even a folder.
- **Errors**: a denied path fails only its job with an error of the form
  `Error: <job>: file-access-denied: '<path as written>' (real path '<real path>'): <reason>`, where the
  real-path part appears only when it is known and the reason names the mode. The other jobs still run, and
  the exit code is non-zero; the user guide shows complete examples.

For example, a CI job that builds a pull request can confine its configuration file to `docs`:

```bash
dictionarymark --config docs/glossary/dictionarymark.yaml --file-access roots --allow-root docs
```

Limits: `roots` confines a configuration file to its folders, but it can still overwrite any file inside
them. A hard link cannot be detected, because it is a second name of the same file. A link swapped in by
another process between the check of a path and its open is followed by that open, but the real path of what
was opened is then checked and the job is denied.

### Conflicts

The same term in several files is fine when the entries are identical. Entries that differ in `definition`, `pos`,
`variants` or `visible` are an error naming the term, the differing fields and both files, for example
`Error: Conflict: term 'API' differs in definition between <file A> and <file B>.` Approved-versus-deprecated conflicts
are not checked by DictionaryMark; Ste100Mark enforces them. Input paths may contain `${VAR}` environment variables, and
a pattern that matches no file is an error.

## Building

```pwsh
pwsh ./build.ps1
```

## User Guide

The DictionaryMark User Guide is available on the
[DictionaryMark releases page](https://github.com/demaconsulting/DictionaryMark/releases).

## Contributing

Contributions are welcome! We appreciate your interest in improving DictionaryMark.

Please see our [Contributing Guide](https://github.com/demaconsulting/DictionaryMark/blob/main/CONTRIBUTING.md) for
development setup, coding standards, and submission guidelines. Also review our
[Code of Conduct](https://github.com/demaconsulting/DictionaryMark/blob/main/CODE_OF_CONDUCT.md) for community
guidelines.

For bug reports, feature requests, and questions, please use
[GitHub Issues](https://github.com/demaconsulting/DictionaryMark/issues).

## License

This project is licensed under the MIT License — see the
[LICENSE](https://github.com/demaconsulting/DictionaryMark/blob/main/LICENSE) file for details.

By contributing to this project, you agree that your contributions will be licensed under the MIT License.

## Support

- 🐛 **Report Bugs**: [GitHub Issues](https://github.com/demaconsulting/DictionaryMark/issues)
- 💡 **Request Features**: [GitHub Issues](https://github.com/demaconsulting/DictionaryMark/issues)
- ❓ **Ask Questions**: [GitHub Discussions](https://github.com/demaconsulting/DictionaryMark/discussions)
- 🤝 **Contributing**: [Contributing Guide](https://github.com/demaconsulting/DictionaryMark/blob/main/CONTRIBUTING.md)

## Self-Validation

DictionaryMark includes a built-in self-validation mode for use in regulated environments
where tool qualification evidence is required.

```bash
dictionarymark --validate
```

The validation report includes system information (tool version, OS, .NET runtime) and
the results of each self-test. Each test proves a specific capability:

- **`DictionaryMark_VersionDisplay`** - `--version` flag outputs a valid version string
- **`DictionaryMark_HelpDisplay`** - `--help` flag outputs usage and options text
- **`DictionaryMark_BulletGeneration`** - Bullet list is generated correctly from YAML input
- **`DictionaryMark_TableGeneration`** - Markdown table is generated correctly from YAML input
- **`DictionaryMark_CustomHeaders`** - `--term-header` and `--def-header` override column text
- **`DictionaryMark_ConflictDetection`** - Conflicting definitions across files are detected
- **`DictionaryMark_HiddenEntry`** - Entries with `visible: false` are omitted from the output
- **`DictionaryMark_DeprecatedTable`** - A deprecated-terms table is generated with `--kind deprecated`
- **`DictionaryMark_ColumnWidths`** - `--column-widths` sizes the table separator in proportion to the weights
- **`DictionaryMark_ConfigTables`** - A `--config` file generates every table job, with config-relative
  `..` paths and missing output directories created
- **`DictionaryMark_FileAccessRoots`** - `--file-access roots` denies a config file output outside the
  allowed folder and writes nothing for that job, while a job inside the folder still runs

Results can be saved to a file:

```bash
dictionarymark --validate --results results.trx   # TRX (MSTest) format
dictionarymark --validate --results results.xml   # JUnit XML format
```

On validation failure the tool will exit with a non-zero exit code.

## Project Practices

The DictionaryMark repository follows these development practices:

- 🔍 **Linting Enforcement** - LintMark, TextLint, and yamllint enforced on every CI run
- 📋 **Continuous Compliance** - Compliance evidence generated automatically on every CI run,
  following the [Continuous Compliance](https://github.com/demaconsulting/ContinuousCompliance) methodology
- ☁️ **SonarCloud Integration** - Quality gate and security analysis on every build
- 🔗 **Requirements Traceability** - Requirements linked to passing tests with auto-generated trace matrix

## Security

For security concerns and vulnerability reporting, please see our
[Security Policy](https://github.com/demaconsulting/DictionaryMark/blob/main/SECURITY.md).

## Acknowledgments

DictionaryMark is built with the following open-source projects:

- [.NET](https://dotnet.microsoft.com/) - Cross-platform framework for building applications
- [YamlDotNet](https://github.com/aaubry/YamlDotNet) - YAML parsing library for .NET
- [Microsoft.Extensions.FileSystemGlobbing](https://www.nuget.org/packages/Microsoft.Extensions.FileSystemGlobbing)
  \- Glob pattern matching for file system paths
- [DemaConsulting.TestResults](https://github.com/demaconsulting/TestResults) - TRX and JUnit XML test results library
