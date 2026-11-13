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
integration into documentation pipelines.

The workflow below shows how to use it: author YAML dictionary files, run `dictionarymark` from a
shell or CI pipeline, and use the generated Markdown in your documents and documentation pipeline.

![DictionaryMark workflow: author YAML dictionary files, run dictionarymark with --input, --format and header
options, get a Markdown bullet list or table, then embed it in documents, a docs pipeline or a CI
gate](https://raw.githubusercontent.com/demaconsulting/DictionaryMark/main/docs/images/workflow.png)

## Features

- 📄 **YAML Dictionary Input** - Read terms from YAML files, as simple definitions or rich entries
  (`definition`, `pos`, `variants`, `visible`)
- 📝 **Markdown Output** - Generate bullet lists or formatted tables
- 🚫 **Deprecated Terms** - Generate a "Not used / Use instead" table from deprecated-term files
- 🔍 **Glob Patterns** - Process multiple input files with wildcards
- ⚔️ **Conflict Detection** - Detect conflicting entries across files
- 🎯 **Customizable Output** - Control headings, column headers, sort order, and depth
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

| Component | Version | Status       |
|-----------|---------|--------------|
| .NET SDK  | 10.0    | ✅ Supported |
| .NET SDK  | 11.0    | ✅ Supported |
| OS        | Windows | ✅ Supported |
| OS        | Linux   | ✅ Supported |
| OS        | macOS   | ✅ Supported |

## Usage

```bash
dictionarymark --input glossary.yaml --format table --section "Glossary" --output docs/glossary.md
```

### Options

| Option                         | Description                                         |
| :----------------------------- | :-------------------------------------------------- |
| `-v, --version`                | Display version information                         |
| `-?, -h, --help`               | Display help                                        |
| `--silent`                     | Suppress console output                             |
| `--validate`                   | Run self-validation                                 |
| `-i, --input <pattern>`        | Input YAML file or glob pattern (repeatable)        |
| `-o, --output <file>`          | Output Markdown file (default: stdout)              |
| `-f, --format <format>`        | Output format: `bullets` (default) or `table`       |
| `-s, --section <heading>`      | Section heading text                                |
| `--term-header <header>`       | Term column header for table format (default: Term) |
| `--def-header <header>`        | Definition column header (default: Definition)      |
| `--definition-header <header>` | Alias for `--def-header`                            |
| `--sort <order>`               | Sort order: `file` (default) or `alpha`             |
| `--log <file>`                 | Write output to log file                            |
| `--results <file>`             | Write validation results (.trx or .xml)             |
| `--result <file>`              | Alias for `--results`                               |
| `--depth <n>`                  | Set heading depth (default: 1, range: 1–6)          |
| `--kind <kind>`                | Input kind: `terms` (default) or `deprecated`       |
| `--use-header <header>`        | Deprecated "use" column header                      |
| `--where-header <header>`      | Deprecated "where" column header                    |

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

### Conflicts

The same term in several files is fine when the entries are identical. Entries that differ in
`definition`, `pos`, `variants` or `visible` are an error naming the term, the differing fields and
both files, for example
`Error: Conflict: term 'API' differs in definition between <file A> and <file B>.` Approved-versus-deprecated conflicts are
not checked by DictionaryMark; Ste100Mark enforces them. Input paths may contain `${VAR}`
environment variables, and a pattern that matches no file is an error.

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

Results can be saved to a file:

```bash
dictionarymark --validate --results results.trx   # TRX (MSTest) format
dictionarymark --validate --results results.xml   # JUnit XML format
```

On validation failure the tool will exit with a non-zero exit code.

## Project Practices

The DictionaryMark repository follows these development practices:

- 🔍 **Linting Enforcement** - markdownlint, TextLint, and yamllint enforced on every CI run
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
