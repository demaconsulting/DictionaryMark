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
literal file that does not exist and an undefined variable are all errors. For the tables of a
configuration file, a literal input (one without wildcards) is not looked up in advance: it is read
through the file-access checks (see *File Access*), which report a file that does not exist as
`Input file '...' was not found.` once the file-access mode allows its path.

The same term in several files is fine when the entries are identical. Entries that differ in
`definition`, `pos`, `variants` or `visible` conflict. DictionaryMark does not check approved terms
against deprecated terms; Ste100Mark enforces approved-versus-deprecated conflicts.

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

## Column Widths

When a table is converted to HTML or PDF (for example with Pandoc), the relative column widths follow
the number of dashes in the separator line under the header row. By default every column gets the
same short separator, so the term column takes as much room as the definitions. Use
`--column-widths` to give each column a relative weight:

```bash
dictionarymark --input glossary.yaml --format table --column-widths 25,75
```

```markdown
| Term | Definition |
| :------------------------- | :--------------------------------------------------------------------------- |
| API | Application Programming Interface |
```

Rules:

- The weights are positive integers. Zero, negative, fractional and empty values are errors.
- The dash counts are proportional to the weights, scaled to about 100 in total, with at least 3 dashes
  per column. The `:` left-alignment marker is kept, so the output is always a valid pipe table.
- A terms table takes exactly 2 weights; a deprecated table (`--kind deprecated`) takes exactly 3.
  When no deprecated entry has a `where` value, the Where column is omitted, so the third weight is
  dropped and the first two are rescaled (for example `20,50,30` gives 29 and 71 dashes).
- Bullet output ignores `--column-widths`.
- Without the option, the output is exactly as before.

## Configuration File

A configuration file keeps the formatting options, and optionally several table jobs, in a YAML file
that can be versioned beside the dictionaries. Pass it with `--config <file.yaml>`:

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
  - input:
      - deprecated.yaml
      - ../../shared/deprecated.yaml
    output: ../generated/glossary/deprecated.md
    section: Deprecated Terms
    kind: deprecated
    column-widths: [20, 50, 30]
```

```bash
dictionarymark --config docs/glossary/dictionarymark.yaml
```

### Keys

| Key | Where | Value |
| :--- | :--- | :--- |
| `format` | top level or job | `bullets` or `table` |
| `section` | top level or job | Section heading text |
| `depth` | top level or job | Heading depth, 1 to 6 |
| `sort` | top level or job | `file` or `alpha` |
| `term-header` | top level or job | Term column header |
| `def-header` (or `definition-header`) | top level or job | Definition column header |
| `kind` | top level or job | `terms` or `deprecated` |
| `use-header` | top level or job | Deprecated "use" column header |
| `where-header` | top level or job | Deprecated "where" column header |
| `column-widths` | top level or job | YAML sequence of 2 or 3 positive integers, for example `[25, 75]` |
| `root` | top level only | Base directory for relative paths, relative to the file; never checked on disk |
| `tables` | top level only | Non-empty list of table jobs |
| `input` | job only, required | A pattern, or a list of patterns |
| `output` | job only, required | Output Markdown file |

The values follow the same rules as the matching command-line options. `def-header` and
`definition-header` may not both appear in one mapping. The keys `input`, `output`, `log`, `results`,
`validate`, `silent`, `version` and `help` are not allowed at the top level, `root` is not allowed in
a job, and any unknown key is an error.

### Precedence

Each formatting setting is taken from, in order:

1. The command-line option, which applies to every job.
2. The job's key.
3. The file's top-level key.
4. The built-in default.

For example, `dictionarymark --config dictionarymark.yaml --format bullets` writes every table job as
a bullet list.

### Without and With Tables

- **Without `tables`**, the file only supplies formatting defaults (and optionally `root`). The inputs
  and output come from `--input` and `--output` on the command line.
- **With `tables`**, the file decides the inputs and outputs. `--input` and `--output` on the command
  line are errors. `--silent` and `--log` are allowed. `--config` may be given only once.

### Paths

- Relative `input` patterns and `output` paths in the file are resolved against the configuration
  file's directory, so the file works from any working directory. A root directory changes this; see
  *Root Directory*.
- `..` segments are allowed anywhere before the first wildcard, for example `../shared/*.yaml` or
  `sub/../../shared/*.yaml`.
- `${VAR}` environment variables are expanded in the `--config` path and in every input and output
  path. An undefined variable is an error.
- A pattern that matches no file is an error for that job, and so is a literal input that does not
  exist (reported by the file-access checks; see *File Access*).
- Missing parent directories of every output file are created (also for `--output` on the command
  line).

### Job Failures

Every job runs, even after an earlier job fails. Each failure is reported with the job's 1-based
index and output, followed by a summary, and the exit code is non-zero:

```text
Error: table job 2 ('deprecated.md'): Input pattern 'missing/*.yaml' matched no files.
Error: 1 of 2 table jobs failed.
```

Problems with the file itself (missing file, invalid YAML, unknown or misplaced keys, invalid values)
stop the run before any job, with an error naming the file, the job and the key:

```text
Error: Config file 'C:\repo\docs\glossary\dictionarymark.yaml': table job 1: unknown key 'theme'
```

## Root Directory

A root directory is the base for every relative input pattern and output path, so the tool can run from
any working directory while the paths stay relative to, for example, the repository root. Set it with
`--root <dir>` on the command line or with the top-level `root` key in the configuration file:

```yaml
# tools/dictionarymark/dictionarymark.yaml
root: ../..
tables:
  - input: docs/glossary/*.yaml
    output: docs/generated/glossary.md
```

The base directory for a relative path is chosen as follows:

| Path from | 1st | 2nd | Otherwise |
| :--- | :--- | :--- | :--- |
| Configuration table job (`input`, `output`) | `--root` | `root` key | Configuration file directory |
| Command line (`--input`, `--output`) | `--root` | `root` key | Working directory |
| `--config`, `--log`, `--results` | Not affected | Not affected | Working directory |

- A relative `--root` is resolved against the working directory; a relative `root` key is resolved
  against the configuration file's directory. Both support `${VAR}` and `..`.
- Absolute input and output paths ignore the root.
- Neither root may contain wildcards. `--root` must be an existing directory, may be given only once
  and must not be empty. The `root` key is configuration content, so it is only the base that relative
  job paths are combined with: it is resolved as text and never checked on disk, and it never grants
  access to anything. A `root` key that does not exist, or that names a network share, makes each table
  that uses it fail through the file-access checks (its inputs are not found or are refused). The
  `root` key is checked for wildcards and undefined variables even when `--root` overrides it.

## File Access

The trust boundary is the command line. Everything you type there is trusted, including `--file-access`,
`--allow-root`, `--root` and the `--config` path itself, and reading the configuration file is never judged. The
content of the configuration file is not trusted, because it may come from a source you do not fully trust, for example
a pull request or a shared repository. Everything that content names reaches the disk only through the file-access
checks: DictionaryMark judges every input and output its tables name by a file-access mode, and its `root:` key only
moves where relative table paths point (it never grants access, and the allowed folders never come from it). The one
accepted exception is listing the folders of a wildcard pattern, which happens only after the pattern has passed the
rules that refuse network paths. The paths you name on the command line yourself (`--config`, `--input`, `--output`,
`--log` and `--results`) are trusted and never judged.

| Mode | What configuration tables may read and write |
| :--- | :--- |
| `any` (default) | Every regular local file, as Pandoc does |
| `roots` | Only files inside the allowed folders |
| `no-symlinks` | Only files not reached through a symbolic link or junction |
| `roots,no-symlinks` | Both rules |

Choose the mode with `--file-access <mode>` (case-insensitive; the two rules may be given in either order; if the
option is repeated, the last value wins). Name the allowed folders with `--allow-root <dir>` (repeatable; a relative
folder is taken from the working directory). `--allow-root` alone implies `roots`. With `roots` and no `--allow-root`,
the allowed folders are the working directory and the configuration file's folder.

```bash
# Confine the tables of a configuration file to the repository folder
dictionarymark --config docs/glossary/dictionarymark.yaml --allow-root .

# The same, and refuse every symbolic link or junction
dictionarymark --config docs/glossary/dictionarymark.yaml --allow-root . --file-access roots,no-symlinks
```

How the files are judged:

- Every input is judged, including every match of a glob pattern. A match outside the allowed folders fails the
  table; it is never skipped. A literal input (one without wildcards) is judged before it is looked up, so a file
  outside the allowed folders is refused whether or not it exists.
- The decision is about the file that was actually opened (its real path), and the bytes that were judged are the
  bytes that are parsed, so a link swapped in by another process cannot slip past the check.
- Every output is judged before anything is written, so a refused output creates neither the file nor any folder.
  An output that does not exist yet is judged by the real location of its deepest existing folder. An existing entry
  at the output path that cannot be opened (for example a dangling link) is refused, and so is an output below a
  dangling folder link.
- The `no-symlinks` rule trusts the configuration file's folder and the folders above it; a link below it is refused.
- `--root` and the configuration `root:` key move the base of relative paths, but never widen the allowed folders.

Under every mode, including the default `any`, these are refused for configuration tables: network paths
(`\\server\share` and `//host`), device paths (and on Windows reserved names such as `NUL`, `CON` or `COM1` in any part
of the path), a colon anywhere except in a Windows drive (an alternate data stream; on Linux and macOS a folder name
with `:` too), control characters, anything that is not a regular file (a folder, a device, a named pipe), and on
Windows a local path that leads to a network share through a link. Each input is read up to 16 MiB; a larger file is an
error.

A table whose input or output the file-access mode denies fails with `file-access-denied`:

```text
Error: table job 2 ('../out/table.md'): file-access-denied: The file 'C:\repo\out\table.md' was denied by the
file-access policy 'roots'. Allow the file in the file-access policy, or reference a file it allows.
```

A table that breaks one of the rules above, or names an input larger than 16 MiB, fails with the ordinary
`Failed to read file` or `Failed to write output file` error, which names the rule:

```text
Error: table job 3 ('//host/share/t.md'): Failed to write output file '//host/share/t.md': The path
'\\host\share\t.md' is not allowed: network paths are not permitted.
```

Either way the table writes nothing, the other tables still run, and the exit code is non-zero.

A mistake in the options (an unknown mode, `--allow-root` with `any` or `no-symlinks`, or an `--allow-root` folder
that does not exist) is reported before any input is read.

Known limits:

- A hard link cannot be told apart from the file it links to, so `no-symlinks` does not refuse it.
- The output check is not race-proof: the write that follows goes by name, so a link swapped in between the check and
  the write is not caught.
- A configuration input pattern is checked against the rules that apply under every mode before anything is listed,
  so a network or URL-like pattern (such as `//host/share/*.yaml`) is refused without contacting the host. Under local
  folders, glob matching lists the folders of a wildcard pattern before each match is judged, so file names outside the
  allowed folders can still be learned; no content of such a file is read. Literal inputs are not listed or looked up
  in advance.
- The real location of each allowed folder is captured when the run starts; a folder that does not exist then allows
  nothing, even if a table creates it.
- An existing output that cannot be opened for reading (for example a write-only file) is refused, and an output that
  does not exist yet, written with short (8.3) names, is refused under `no-symlinks`.

Letter case: whether a file is inside an allowed folder is decided by the letter case stored on disk. An
`--allow-root` folder (or a path in the configuration file) written in another letter case works where the file system
ignores case, as it does by default on Windows and macOS; on a case-sensitive folder, a folder whose name differs from
an allowed folder only in letter case is a different folder and stays outside.

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

| Test | What is verified |
| :--- | :--- |
| `DictionaryMark_VersionDisplay` | `--version` flag outputs a valid version string |
| `DictionaryMark_HelpDisplay` | `--help` flag outputs usage and options text |
| `DictionaryMark_BulletGeneration` | Bullet list is generated correctly from a YAML input file |
| `DictionaryMark_TableGeneration` | Markdown table is generated correctly from a YAML input file |
| `DictionaryMark_CustomHeaders` | `--term-header` and `--def-header` override the table column text |
| `DictionaryMark_ConflictDetection` | Conflicting definitions across files are detected and reported |
| `DictionaryMark_HiddenEntry` | Entries with `visible: false` are omitted from the output |
| `DictionaryMark_DeprecatedTable` | A deprecated-terms table is generated with `--kind deprecated` |
| `DictionaryMark_ColumnWidths` | `--column-widths` sizes the table separator in proportion to the weights |
| `DictionaryMark_ConfigTables` | A `--config` file generates every table job, with config-relative `..` paths |
| `DictionaryMark_FileAccessRoots` | With `--allow-root`, an output outside the folder is denied, nothing written |

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

| Option | Description |
| :--- | :--- |
| `-v`, `--version` | Display version information |
| `-?`, `-h`, `--help` | Display help message |
| `--silent` | Suppress console output |
| `--validate` | Run self-validation |
| `--results <file>`, `--result <file>` | Write validation results to file (TRX or JUnit format) |
| `--depth <n>` | Set heading depth for Markdown output (default: 1, range: 1–6) |
| `--log <file>` | Write output to log file |
| `-i`, `--input <pattern>` | Input YAML file or glob pattern (repeatable) |
| `-o`, `--output <file>` | Output Markdown file (default: stdout) |
| `-f`, `--format <format>` | Output format: `bullets` (default) or `table` |
| `-s`, `--section <heading>` | Section heading text |
| `--term-header <header>` | Term column header for table format (default: `Term`) |
| `--def-header <header>`, `--definition-header <header>` | Table definition column header (default: `Definition`) |
| `--sort <order>` | Sort order: `file` (default) or `alpha` |
| `--kind <kind>` | Input kind: `terms` (default) or `deprecated` |
| `--use-header <header>` | Deprecated "use" column header (default: `Use instead`) |
| `--where-header <header>` | Deprecated "where" column header (default: `Where it was used`) |
| `--column-widths <w1,w2[,w3]>` | Relative table column widths, for example `25,75` |
| `--config <file>` | Read formatting defaults and table jobs from a YAML file (once only) |
| `--root <dir>` | Base directory for relative input and output paths (once only) |
| `--file-access <mode>` | Files configuration tables may use: `any` (default), `roots`, `no-symlinks` or both |
| `--allow-root <dir>` | A folder configuration tables may use (repeatable; implies `roots`) |

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

## Example 5: Generate Several Tables from One Configuration File

```bash
dictionarymark --silent --config docs/glossary/dictionarymark.yaml --log glossary.log
```

## Example 6: Run from Anywhere with `--root`

```bash
dictionarymark --root /path/to/repo --input "docs/glossary/*.yaml" --format table \
  --column-widths 25,75 --output docs/generated/glossary.md
```

# Continuous Compliance

DictionaryMark follows the
[Continuous Compliance](https://github.com/demaconsulting/ContinuousCompliance) methodology, which
ensures compliance evidence is generated automatically on every CI run.

## Key Practices

- **Requirements Traceability**: Every requirement is linked to passing tests, and a trace matrix is
  auto-generated on each release
- **Linting Enforcement**: LintMark, TextLint, and yamllint are enforced before any build proceeds
- **Automated Audit Documentation**: Each release ships with generated requirements, justifications,
  trace matrix, and quality reports
- **CodeQL and SonarCloud**: Security and quality analysis runs on every build
