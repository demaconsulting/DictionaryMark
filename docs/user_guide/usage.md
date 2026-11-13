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

## Rich Entries and a Good Glossary

Every approved term needs a non-empty `definition`, either as a plain string or inside an object
entry. An object entry may also carry `pos` (a part of speech or list of them: `noun`, `verb`,
`adjective`, `adverb`, `proper-noun`; default `noun`), `variants` (a list of alternative spellings)
and `visible` (default `true`). A missing or empty definition, a null value or an unknown key is an
error naming the term and file. Entries with `visible: false` are left out of the output but are
still checked for conflicts. Here is a complete small glossary, `terms.yaml`:

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

```bash
dictionarymark --input terms.yaml --format table --section Glossary
```

Output (the hidden `bleed valve` entry is omitted):

```markdown
# Glossary

| Term | Definition |
| :--- | :--- |
| Test Mode | A mode in which Product A runs self-checks and does not control the process. |
| Product A | The main control unit made by Acme. |
| Zorbix | A sealed connector that joins the bleed valve to the main line. |
```

With `--format bullets` the output is:

```markdown
# Glossary

- **Test Mode**: A mode in which Product A runs self-checks and does not control the process.
- **Product A**: The main control unit made by Acme.
- **Zorbix**: A sealed connector that joins the bleed valve to the main line.
```

## Deprecated Terms

Use `--kind deprecated` for a file of terms that must not be used. Each entry needs `use` and may
have `where`; there is no definition. Given `deprecated.yaml`:

```yaml
bench mode: { use: Test Mode, where: Product A }
Zorbix unit: { use: Zorbix }
```

```bash
dictionarymark --input deprecated.yaml --kind deprecated --format table \
  --section "Deprecated Terms" --depth 2
```

Output:

```markdown
## Deprecated Terms

| Not used | Use instead | Where it was used |
| :--- | :--- | :--- |
| bench mode | Test Mode | Product A |
| Zorbix unit | Zorbix |  |
```

The `Where it was used` column appears only when at least one entry has a `where` value. The
headers can be changed with `--term-header`, `--use-header` and `--where-header`. With
`--format bullets` the output is `- **bench mode**: Test Mode (where: Product A)` and
`- **Zorbix unit**: Zorbix`.

## Multiple Input Files

Merge entries from multiple YAML files:

```bash
dictionarymark --input terms/*.yaml --input extra/more-terms.yaml --output docs/glossary.md
```

Input patterns may contain `${VAR}` environment variables. A pattern that matches no file, a
literal file that does not exist and an undefined variable are all errors.

The same term in several files is fine when the entries are identical. Entries that differ in
`definition`, `pos`, `variants` or `visible` conflict, as does a term (or approved variant) that is
both approved and deprecated. Use `--check-against` to load files of the other kind for conflict
checking only; they are never rendered:

```bash
dictionarymark --input terms.yaml --check-against deprecated.yaml --output docs/glossary.md
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

Unrecognized arguments cause the tool to print an error message to standard error and exit with a
non-zero exit code. For example:

```text
Error: Unsupported argument '--unknown'
```

Conflicting entries for the same term across input files also produce a clear error naming the
differing fields and both files:

```text
Error: Conflict: term 'API' differs in definition between C:\a\one.yaml and C:\a\two.yaml.
```

This behavior enables automated scripts and CI/CD pipelines to detect and surface misconfiguration
failures automatically.

## Self-Validation

DictionaryMark includes a built-in self-validation mode for use in regulated environments where tool
qualification evidence is required.

### Running Validation

To perform self-validation:

```bash
dictionarymark --validate
```

To save validation results to a file:

```bash
dictionarymark --validate --results results.trx
```

The results file format is determined by the file extension: `.trx` for TRX (MSTest) format, or `.xml`
for JUnit XML format.

### Validation Tests

The self-validation suite runs the following tests:

| Test                               | What is verified                                                  |
| :--------------------------------- | :---------------------------------------------------------------- |
| `DictionaryMark_VersionDisplay`    | `--version` flag outputs a valid version string                   |
| `DictionaryMark_HelpDisplay`       | `--help` flag outputs usage and options text                      |
| `DictionaryMark_BulletGeneration`  | Bullet list is generated correctly from a YAML input file         |
| `DictionaryMark_TableGeneration`   | Markdown table is generated correctly from a YAML input file      |
| `DictionaryMark_CustomHeaders`     | `--term-header` and `--def-header` override the table column text |
| `DictionaryMark_ConflictDetection` | Conflicting definitions across files are detected and reported    |
| `DictionaryMark_HiddenEntry`       | Entries with `visible: false` are omitted from the output         |
| `DictionaryMark_DeprecatedTable`   | A deprecated-terms table is generated with `--kind deprecated`    |

### Heading Depth

Use `--depth <n>` to control the heading level of the output (default: `1`). This is useful when
embedding the output into a larger Markdown document:

```bash
# Embed output at heading level 2
dictionarymark --validate --depth 2
```

### Validation Report

The validation report contains the tool version, machine name, operating system version, .NET runtime
version, timestamp, and test results. On validation failure the tool exits with a non-zero exit code.

# Command-Line Options

The following command-line options are supported:

| Option                                                  | Description                                                       |
| :------------------------------------------------------ | :---------------------------------------------------------------- |
| `-v`, `--version`                                       | Display version information                                       |
| `-?`, `-h`, `--help`                                    | Display help message                                              |
| `--silent`                                              | Suppress console output                                           |
| `--validate`                                            | Run self-validation                                               |
| `--results <file>`, `--result <file>`                   | Write validation results to file (TRX or JUnit format)            |
| `--depth <n>`                                           | Set heading depth for Markdown output (default: 1, range: 1–6)    |
| `--log <file>`                                          | Write output to log file                                          |
| `-i`, `--input <pattern>`                               | Input YAML file or glob pattern (repeatable)                      |
| `-o`, `--output <file>`                                 | Output Markdown file (default: stdout)                            |
| `-f`, `--format <format>`                               | Output format: `bullets` (default) or `table`                     |
| `-s`, `--section <heading>`                             | Section heading text                                              |
| `--term-header <header>`                                | Term column header for table format (default: `Term`)             |
| `--def-header <header>`, `--definition-header <header>` | Definition column header for table format (default: `Definition`) |
| `--sort <order>`                                        | Sort order: `file` (default) or `alpha`                           |
| `--kind <kind>`                                         | Input kind: `terms` (default) or `deprecated`                     |
| `--use-header <header>`                                 | Deprecated "use" column header (default: `Use instead`)           |
| `--where-header <header>`                               | Deprecated "where" column header (default: `Where it was used`)   |
| `--check-against <pattern>`                             | Extra files used only for conflict checking (repeatable)          |

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

# Continuous Compliance

DictionaryMark follows the
[Continuous Compliance](https://github.com/demaconsulting/ContinuousCompliance) methodology, which
ensures compliance evidence is generated automatically on every CI run.

## Key Practices

- **Requirements Traceability**: Every requirement is linked to passing tests, and a trace matrix is
  auto-generated on each release
- **Linting Enforcement**: markdownlint, TextLint, and yamllint are enforced before any build proceeds
- **Automated Audit Documentation**: Each release ships with generated requirements, justifications,
  trace matrix, and quality reports
- **CodeQL and SonarCloud**: Security and quality analysis runs on every build
