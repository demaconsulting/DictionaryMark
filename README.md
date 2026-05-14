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
| `--sort <order>` | Sort order: `file` (default) or `alpha` |
| `--log <file>` | Write output to log file |
| `--results <file>` | Write validation results (.trx or .xml) |
| `--depth <n>` | Set heading depth (default: 1) |

## License

MIT License - Copyright (c) DEMA Consulting
