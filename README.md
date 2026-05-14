# DictionaryMark

[![Build Status](https://github.com/demaconsulting/DictionaryMark/actions/workflows/build_on_push.yaml/badge.svg)](https://github.com/demaconsulting/DictionaryMark/actions/workflows/build_on_push.yaml)

DictionaryMark is a .NET tool that reads YAML dictionary files and generates formatted
Markdown output as either bullet lists or tables.

## Installation

```sh
dotnet tool install --global DemaConsulting.DictionaryMark
```

## Usage

```sh
dictionarymark --input glossary.yaml
dictionarymark --input *.yaml --format table --section "Glossary" --output docs/glossary.md
dictionarymark --input file1.yaml --input file2.yaml --sort alpha
```

## Options

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
| `--sort <order>`          | Sort order: `file` (default) or `alpha`                  |
| `--log <file>`            | Write output to log file                                 |
| `--results <file>`        | Write validation results (.trx or .xml)                  |
| `--depth <n>`             | Set heading depth (default: 1)                           |

## Example

Given an input file `glossary.yaml`:

```yaml
API: Application Programming Interface
CLI: Command-Line Interface
SDK: Software Development Kit
```

Running:

```sh
dictionarymark --input glossary.yaml --format table --section "Glossary"
```

Produces:

```markdown
# Glossary

| Term | Definition                        |
| :--- | :-------------------------------- |
| API  | Application Programming Interface |
| CLI  | Command-Line Interface            |
| SDK  | Software Development Kit          |
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

```sh
dictionarymark --validate
```

The validation report includes system information (tool version, OS, .NET runtime) and
the following self-tests:

| Test                              | What is verified                                                  |
| :-------------------------------- | :---------------------------------------------------------------- |
| `DictionaryMark_VersionDisplay`   | `--version` flag outputs a valid version string                   |
| `DictionaryMark_HelpDisplay`      | `--help` flag outputs usage and options text                      |
| `DictionaryMark_BulletGeneration` | Bullet list is generated correctly from a YAML input file         |
| `DictionaryMark_TableGeneration`  | Markdown table is generated correctly from a YAML input file      |
| `DictionaryMark_CustomHeaders`    | `--term-header` and `--def-header` override the table column text |
| `DictionaryMark_ConflictDetection`| Conflicting definitions across files are detected and reported    |

Results can be saved to a file:

```sh
dictionarymark --validate --results results.trx   # TRX (MSTest) format
dictionarymark --validate --results results.xml   # JUnit XML format
```

## License

MIT License - Copyright (c) DEMA Consulting
