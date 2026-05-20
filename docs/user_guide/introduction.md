# Introduction

## Purpose

DictionaryMark is a .NET CLI tool that reads YAML flat-dictionary files and
generates formatted Markdown output as either bullet lists or tables.

## Scope

This user guide covers:

- Installation instructions
- Usage examples for common tasks
- Command-line options reference
- Practical examples for various scenarios

## References

- [DictionaryMark releases](https://github.com/demaconsulting/DictionaryMark/releases)
- [.NET SDK](https://dotnet.microsoft.com/download)

# Continuous Compliance

This project follows the
[Continuous Compliance](https://github.com/demaconsulting/ContinuousCompliance) methodology, which ensures
compliance evidence is generated automatically on every CI run.

## Key Practices

- **Requirements Traceability**: Every requirement is linked to passing tests, and a trace matrix is
  auto-generated on each release
- **Linting Enforcement**: markdownlint, cspell, and yamllint are enforced before any build proceeds
- **Automated Audit Documentation**: Each release ships with generated requirements, justifications,
  trace matrix, and quality reports
- **CodeQL and SonarCloud**: Security and quality analysis runs on every build

# Installation

Install the tool globally using the .NET CLI:

```bash
dotnet tool install -g DemaConsulting.DictionaryMark
```

# Usage

## Display Version

Display the tool version:

```bash
dictionarymark --version
```

## Display Help

Display usage information:

```bash
dictionarymark --help
```

## Generate a Bullet List

Given a YAML file `glossary.yaml`:

```yaml
API: Application Programming Interface
CLI: Command-Line Interface
SDK: Software Development Kit
```

Generate a Markdown bullet list:

```bash
dictionarymark --input glossary.yaml --format bullets --section "Glossary" --depth 1
```

Output:

```markdown
# Glossary

- **API**: Application Programming Interface
- **CLI**: Command-Line Interface
- **SDK**: Software Development Kit
```

## Generate a Table

Generate a Markdown table from the same input, with a level-2 heading:

```bash
dictionarymark --input glossary.yaml --format table --section "Glossary" --depth 2
```

Output:

```markdown
## Glossary

| Term | Definition |
| :--- | :--- |
| API | Application Programming Interface |
| CLI | Command-Line Interface |
| SDK | Software Development Kit |
```

## Multiple Input Files

Merge entries from multiple YAML files (conflicting entries produce a clear error):

```bash
dictionarymark --input terms/*.yaml --input extra/more-terms.yaml --output docs/glossary.md
```

## Sorted Output

Sort entries alphabetically:

```bash
dictionarymark --input glossary.yaml --sort alpha --output docs/glossary.md
```

## Custom Column Headers

Override the default column headers for table output:

```bash
dictionarymark --input glossary.yaml --format table \
  --term-header "Abbreviation" --def-header "Meaning"
```

## Self-Validation

Self-validation produces a report demonstrating that DictionaryMark is functioning
correctly. This is useful in regulated industries where tool validation evidence is required.

### Running Validation

To perform self-validation:

```bash
dictionarymark --validate
```

To save validation results to a file:

```bash
dictionarymark --validate --results results.trx
```

The results file format is determined by the file extension: `.trx` for TRX (MSTest) format,
or `.xml` for JUnit format.

### Validation Tests

The self-validation suite runs the following tests:

| Test                               | What is verified                                                   |
| :--------------------------------- | :----------------------------------------------------------------- |
| `DictionaryMark_VersionDisplay`    | `--version` flag outputs a valid version string                    |
| `DictionaryMark_HelpDisplay`       | `--help` flag outputs usage and options text                       |
| `DictionaryMark_BulletGeneration`  | Bullet list is generated correctly from a YAML input file          |
| `DictionaryMark_TableGeneration`   | Markdown table is generated correctly from a YAML input file       |
| `DictionaryMark_CustomHeaders`     | `--term-header` and `--def-header` override the table column text  |
| `DictionaryMark_ConflictDetection` | Conflicting definitions across files are detected and reported     |

### Heading Depth

Use `--depth <#>` to control the heading level of the validation output (default: `1`).
This is useful when embedding the validation report into a larger markdown document:

```bash
# Embed validation at heading level 2
dictionarymark --validate --depth 2
```

### Validation Report

The validation report contains the tool version, machine name, operating system version,
.NET runtime version, timestamp, and test results.

## Silent Mode

Suppress console output:

```bash
dictionarymark --silent --input glossary.yaml --output docs/glossary.md
```

## Logging

Write output to a log file:

```bash
dictionarymark --input glossary.yaml --log dictionarymark.log
```

## Error Handling

Unrecognized arguments cause the tool to print an error message to standard error and exit
with a non-zero exit code. For example:

```text
Error: Unsupported argument '--unknown'
```

Conflicting definitions for the same term across input files also produce a clear error:

```text
Error: Conflict: term 'API' has multiple definitions
```

This behavior enables automated scripts and CI/CD pipelines to detect and surface
misconfiguration failures automatically.

# Command-Line Options

The following command-line options are supported:

| Option                                | Description                                                        |
| :------------------------------------ | :----------------------------------------------------------------- |
| `-v`, `--version`                     | Display version information                                        |
| `-?`, `-h`, `--help`                  | Display help message                                               |
| `--silent`                            | Suppress console output                                            |
| `--validate`                          | Run self-validation                                                |
| `--results <file>`, `--result <file>` | Write validation results to file (TRX or JUnit format)             |
| `--depth <n>`                         | Set heading depth for markdown output (default: 1)                 |
| `--log <file>`                        | Write output to log file                                           |
| `-i`, `--input <pattern>`             | Input YAML file or glob pattern (repeatable)                       |
| `-o`, `--output <file>`               | Output Markdown file (default: stdout)                             |
| `-f`, `--format <format>`             | Output format: `bullets` (default) or `table`                      |
| `-s`, `--section <heading>`           | Section heading text                                               |
| `--term-header <header>`              | Term column header for table format (default: `Term`)              |
| `--def-header <header>`, `--definition-header <header>` | Definition column header for table format (default: `Definition`)  |
| `--sort <order>`                      | Sort order: `file` (default) or `alpha`                            |

# Examples

## Example 1: Single File, Table Output

```bash
dictionarymark --input glossary.yaml --format table --section "Glossary" --output docs/glossary.md
```

## Example 2: Multiple Files, Alphabetically Sorted

```bash
dictionarymark --input "docs/**/*.yaml" --sort alpha --output docs/full-glossary.md
```

## Example 3: Self-Validation with Results

```bash
dictionarymark --validate --results validation-results.trx
```

## Example 4: Silent Mode with Logging

```bash
dictionarymark --silent --input glossary.yaml --output docs/glossary.md --log tool-output.log
```
