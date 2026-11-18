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
| `root` | top level only | Base directory for relative paths, relative to the file; must exist |
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
- A pattern that matches no file is an error for that job.
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
| `--config`, `--log`, `--results`, `--allow-root` | Not affected | Not affected | Working directory |

- A relative `--root` is resolved against the working directory; a relative `root` key is resolved
  against the configuration file's directory. Both support `${VAR}` and `..`.
- Absolute input and output paths ignore the root.
- The root must be an existing directory and must not contain wildcards. `--root` may be given only
  once and must not be empty. The `root` key is checked even when `--root` overrides it.
- A `root` key that names a symbolic link or junction whose target is missing is not an existing
  directory, and on macOS the folder a `root` key names must be readable.
- A root only sets the base directory: it never grants file access (see *File Access*).

## File Access

A configuration file may come from someone you do not trust, for example a pull request that a CI
pipeline builds. The `--file-access <mode>` option decides which files the paths written in a
configuration file may read and write.

### Modes

- **`any`** (the default): every regular local file may be used, as in earlier versions: a relative
  path, a path with `..`, an absolute path, and a symbolic link or junction.
- **`roots`**: only a file inside an allowed folder. Both the path as written (after `${VAR}`
  expansion, the base directory and `..`) and the real path of the opened file must be inside an
  allowed folder. A symbolic link or junction that leads out of the folders is therefore denied, and so
  is a path outside the folders that links back in.
- **`no-symlinks`**: a path may not pass through a symbolic link or junction below the configuration
  file's folder. That folder and the folders above it are trusted, so a linked home or temporary folder
  does not count.
- **`roots,no-symlinks`**: both rules.

The mode may be written in any letter case (`Roots`, `ROOTS,no-symlinks`). Any other value, or the
option given more than once, is an error that lists the modes:

```text
Error: 'strict' is not a value of --file-access: use any (the default), roots, no-symlinks, or roots,no-symlinks
```

### Allowed Folders

`--allow-root <dir>` names a folder that configuration-file paths may use.

- It may be repeated, and it implies `--file-access roots`.
- It replaces the default folders.
- A relative folder is taken from the working directory, never from `--root`. `${VAR}` and `..` are
  supported.
- A folder that does not exist is an error.
- With `--file-access any` or `--file-access no-symlinks` it is an error that names the conflict. To
  confine paths and refuse links, write `--file-access roots,no-symlinks --allow-root <dir>`.

Without `--allow-root`, `roots` allows the working directory and the configuration file's folder.

### Which Paths Are Judged

- **Judged**: every input pattern, every folder a pattern lists, every file a pattern matches, and every
  output path of a configuration file. Each is judged before the tool touches it, including before it
  lists a folder or checks whether a file exists. Each matched file is judged on its own, and a denied
  match fails the job; it is never silently skipped.
- **Glob folders**: the folder before the first wildcard (`docs` in `docs/**/*.yaml`) is judged before it is
  listed. Under `roots` it must be inside an allowed folder, so `../shared/*.yaml` is denied as a whole when
  `shared` is not allowed (name the files one by one, or allow the folder). Under `no-symlinks` it may not
  pass through a link. Every symbolic link or junction a wildcard reaches is judged the same way before it is
  listed, and a denied link fails the job.
- **Trusted**: the paths given on the command line are never judged: `--config`, `--input` and the
  files it matches, `--output`, `--log` and `--results`. You chose them when you ran the tool.
- **Root**: `--root` and the configuration `root` key only set the base directory of relative paths.
  They never grant access: a path under a root outside the allowed folders is denied.

### Never Accessed

In every mode, including `any`, a configuration-file path is never read or written when it is:

- a URL (`http://`, `https://` and other `scheme://` forms);
- a `file:` URI with a host;
- a path written as a UNC path (`\\server\share`, `//server/share`);
- a device path (`\\.\`, `\\?\`, a Windows reserved name such as `NUL` or `COM1`);
- a path with a `:` other than the drive of an absolute path on Windows (an alternate data stream, a
  drive-relative path); on Linux and macOS any `:`, so `terms:v1.yaml` and `C:/x` are refused there too;
- anything that is not a regular file (a folder, a named pipe, a device).

These rules apply to the path after `${VAR}` expansion, so `output: ${OUT}/terms.md` is denied when `OUT`
is `//server/share` or `a:b`; the error still shows the path as written. They also apply to the name of every
file a glob matches, so on Linux and macOS a file `terms:v1.yaml` matched by `*.yaml` fails the job.

This matches CanvasNet, so a configuration file is judged the same on every platform. A file whose name
contains a colon can still be named on the command line, which is trusted. A device such as `/dev/null` is
refused when it is read, as not a regular file. An output under `/dev`, one that is a link to a device, or
one in a folder whose real path is under `/dev` (for example through a link), is never written.

A `root` key that names such a path, or on Windows one that leads to a network share through a link, is an
error when the configuration file is loaded.

On Windows, an `--allow-root` folder in a device namespace (`\\.\`, `\??\`, or a `\\?\` path other than a
drive or volume) is an error. A `\\?\` drive or volume path such as `\\?\C:\docs` is an error too, because
`?` is a wildcard character, as for `--root`. No `\\?\` spelling of `--allow-root` is accepted.

### Network Shares

The network rule is the same as in the DEMA Consulting CanvasNet tool:

- A path written as a UNC path is never accessed.
- A path that is itself on a share or a mapped network drive is used. A configuration file on a share or a
  mapped network drive therefore works: its relative inputs and outputs, and a `--root` or `root` key on
  that share, resolve to the share.
- On Windows, a local path that leads to a network share through a symbolic link or junction is denied,
  at any folder of the path and through a chain of links, including a link a glob wildcard reaches. The
  links are read without being followed, so the share is never contacted through a link that is present when
  the path is judged. A link swapped in afterwards is reached only by an open whose real path is then checked
  and denied.

### Real Paths

The modes other than `any` judge the real path of every opened file. Where the system cannot tell it
(for example Linux without `/proc`), the file is denied, never used unchecked. In every mode, including `any`,
a file whose type the system cannot tell is refused too, with a reason that says its type cannot be
determined (not that it is a folder, pipe or device).

### Writes

Under `roots` the following must all be inside an allowed folder:

- the output;
- the deepest existing folder above it, and every folder the tool creates for it (one at a time, each
  checked by its real path);
- the real path of the opened output.

An existing output is opened without truncation, and its real path is checked before its content is
replaced. `no-symlinks` applies to outputs too. A denied job writes nothing, not even a folder.

### Denials

A denied path fails only its job. The error has the code `file-access-denied`, the path as written (for a
file a glob pattern matched, its full path), the real path when one is known, and a reason that names the
mode. The other jobs still run, and the exit code is non-zero:

```text
Error: table job 1 ('link/terms.yaml'): file-access-denied: 'link/terms.yaml' (real path '/home/ci/secrets/terms.yaml'): its real path is outside the allowed folders, so it leads out through a symbolic link or junction (--file-access roots)
Error: 1 of 2 table jobs failed.
```

### Limits

- `roots` confines a configuration file to its folders, but the file can still overwrite any file
  inside them.
- A hard link cannot be detected: it is a second name of the same file.
- A folder that another process swaps for a link while the tool creates it can leave an empty file at
  the swapped location, but no content is written there.
- Another process can swap a folder for a link in the short window between the check of a path and its
  open. The real path of what was opened is then checked and the job is denied, but the open itself has
  already followed the link. Listing a folder and creating a folder have no handle of their own; each is
  checked just before and again just after.

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

A configuration-file path that the file-access mode denies fails its job with the code
`file-access-denied` (see *File Access*):

```text
Error: table job 2 ('../out.md'): file-access-denied: '../out.md': the path as written is outside the allowed folders (--file-access roots)
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
| `DictionaryMark_FileAccessRoots` | `--file-access roots` denies a config output outside the allowed folder |

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
| `--file-access <mode>` | Config file path access mode (default: `any`) |
| `--allow-root <dir>` | Allowed folder for config file paths (repeatable) |

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

## Example 7: Confine a Configuration File in CI

A CI job that builds a pull request confines the configuration file's paths to the `docs` folder:

```bash
dictionarymark --config docs/glossary/dictionarymark.yaml --file-access roots --allow-root docs
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
