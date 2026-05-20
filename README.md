# DictionaryMark

[![GitHub forks](https://img.shields.io/github/forks/demaconsulting/DictionaryMark?style=plastic)](https://github.com/demaconsulting/DictionaryMark/network/members)
[![GitHub stars](https://img.shields.io/github/stars/demaconsulting/DictionaryMark?style=plastic)](https://github.com/demaconsulting/DictionaryMark/stargazers)
[![GitHub contributors](https://img.shields.io/github/contributors/demaconsulting/DictionaryMark?style=plastic)](https://github.com/demaconsulting/DictionaryMark/graphs/contributors)
[![License](https://img.shields.io/github/license/demaconsulting/DictionaryMark?style=plastic)](https://github.com/demaconsulting/DictionaryMark/blob/main/LICENSE)
[![Build](https://img.shields.io/github/actions/workflow/status/demaconsulting/DictionaryMark/build_on_push.yaml)](https://github.com/demaconsulting/DictionaryMark/actions/workflows/build_on_push.yaml)
[![Quality Gate](https://sonarcloud.io/api/project_badges/measure?project=demaconsulting_DictionaryMark&metric=alert_status)](https://sonarcloud.io/dashboard?id=demaconsulting_DictionaryMark)
[![Security](https://sonarcloud.io/api/project_badges/measure?project=demaconsulting_DictionaryMark&metric=security_rating)](https://sonarcloud.io/dashboard?id=demaconsulting_DictionaryMark)
[![NuGet](https://img.shields.io/nuget/v/DemaConsulting.DictionaryMark?style=plastic)](https://www.nuget.org/packages/DemaConsulting.DictionaryMark)

Markdown Dictionary Generation Tool

## Overview

DictionaryMark is a .NET command-line tool that reads YAML dictionary files and generates
formatted Markdown output as either bullet lists or tables. It supports glob patterns for
processing multiple files, detects conflicting definitions, and is designed for easy
integration into documentation pipelines.

## Features

- 📄 **YAML Dictionary Input** - Read term/definition pairs from YAML files
- 📝 **Markdown Output** - Generate bullet lists or formatted tables
- 🔍 **Glob Patterns** - Process multiple input files with wildcards
- ⚔️ **Conflict Detection** - Detect conflicting definitions across files
- 🎯 **Customizable Output** - Control headings, column headers, sort order, and depth
- 🌐 **Multi-Platform** - Windows, Linux, and macOS on .NET 8, 9, and 10
- ✅ **Self-Validation** - Built-in qualification tests for regulated environments

## Installation

### Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) 8.0, 9.0, or 10.0

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
| .NET SDK  | 8.0     | ✅ Supported |
| .NET SDK  | 9.0     | ✅ Supported |
| .NET SDK  | 10.0    | ✅ Supported |
| OS        | Windows | ✅ Supported |
| OS        | Linux   | ✅ Supported |
| OS        | macOS   | ✅ Supported |

## Usage

### Options

| Option                    | Description                                              |
| :------------------------ | :------------------------------------------------------- |
| `-v, --version`           | Display version information                              |
| `-?, -h, --help`          | Display help                                             |
| `--silent`                | Suppress console output                                  |
| `--validate`              | Run self-validation                                      |
| `-i, --input <pattern>`   | Input YAML file or glob pattern (repeatable)             |
| `-o, --output <file>`     | Output Markdown file (default: stdout)                   |
| `-f, --format <format>`   | Output format: `bullets` (default) or `table`            |
| `-s, --section <heading>` | Section heading text                                     |
| `--term-header <header>`  | Term column header for table format (default: Term)      |
| `--def-header <header>`   | Definition column header (default: Definition)           |
| `--definition-header <header>` | Alias for `--def-header`                          |
| `--sort <order>`          | Sort order: `file` (default) or `alpha`                  |
| `--log <file>`            | Write output to log file                                 |
| `--results <file>`        | Write validation results (.trx or .xml)                  |
| `--result <file>`         | Alias for `--results`                                    |
| `--depth <n>`             | Set heading depth (default: 1)                           |

### Quick Start Examples

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

### Example

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

Results can be saved to a file:

```bash
dictionarymark --validate --results results.trx   # TRX (MSTest) format
dictionarymark --validate --results results.xml   # JUnit XML format
```

On validation failure the tool will exit with a non-zero exit code.

## Project Practices

The DictionaryMark repository itself follows these development practices:

- 🔍 **Linting Enforcement** - markdownlint, cspell, and yamllint enforced on every CI run
- 📋 **Continuous Compliance** - Compliance evidence generated automatically on every CI run,
  following the [Continuous Compliance](https://github.com/demaconsulting/ContinuousCompliance) methodology
- ☁️ **SonarCloud Integration** - Quality gate and security analysis on every build
- 🔗 **Requirements Traceability** - Requirements linked to passing tests with auto-generated trace matrix

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

This project is licensed under the MIT License - see the
[LICENSE](https://github.com/demaconsulting/DictionaryMark/blob/main/LICENSE) file for details.

By contributing to this project, you agree that your contributions will be licensed under the MIT License.

## Support

- 🐛 **Report Bugs**: [GitHub Issues](https://github.com/demaconsulting/DictionaryMark/issues)
- 💡 **Request Features**: [GitHub Issues](https://github.com/demaconsulting/DictionaryMark/issues)
- ❓ **Ask Questions**: [GitHub Discussions](https://github.com/demaconsulting/DictionaryMark/discussions)
- 🤝 **Contributing**: [Contributing Guide](https://github.com/demaconsulting/DictionaryMark/blob/main/CONTRIBUTING.md)

## Security

For security concerns and vulnerability reporting, please see our
[Security Policy](https://github.com/demaconsulting/DictionaryMark/blob/main/SECURITY.md).

## Acknowledgements

DictionaryMark is built with the following open-source projects:

- [.NET](https://dotnet.microsoft.com/) - Cross-platform framework for building applications
- [YamlDotNet](https://github.com/aaubry/YamlDotNet) - YAML parsing library for .NET
- [Microsoft.Extensions.FileSystemGlobbing](https://www.nuget.org/packages/Microsoft.Extensions.FileSystemGlobbing)
  \- Glob pattern matching for file system paths
- [DemaConsulting.TestResults](https://github.com/demaconsulting/TestResults) - TRX and JUnit XML test results library
