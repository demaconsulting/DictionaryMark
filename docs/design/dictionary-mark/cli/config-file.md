### ConfigFile

![Cli Subsystem Structure](CliView.svg)

#### Purpose

`ConfigFile` loads and validates the YAML configuration file named by `--config`. It is the only
code that knows the configuration file schema. It turns the file into three parts: the top-level
formatting defaults, the optional root directory, and the optional table jobs. It keeps the
input and output strings of each job exactly as written, because they are resolved when the job
runs, so that a bad path in one job does not stop the others. Instances are immutable.

The schema is:

| Key | Where | Value |
| :--- | :--- | :--- |
| `format` | top level or job | `table` or `bullets` (case-insensitive) |
| `section` | top level or job | section heading text |
| `depth` | top level or job | integer from 1 to 6 |
| `sort` | top level or job | `file` or `alpha` (case-insensitive) |
| `term-header` | top level or job | term column header |
| `def-header` / `definition-header` | top level or job | definition column header (one alias per mapping) |
| `kind` | top level or job | `terms` or `deprecated` (case-insensitive) |
| `use-header` | top level or job | "use instead" column header |
| `where-header` | top level or job | "where used" column header |
| `column-widths` | top level or job | sequence of 2 or 3 positive integers |
| `root` | top level only | non-empty directory path |
| `tables` | top level only | non-empty sequence of job mappings |
| `input` | job only (required) | non-empty string or non-empty sequence of non-empty strings |
| `output` | job only (required) | non-empty string |

The keys `input` and `output` are rejected at the top level, and the command-line-only keys `log`,
`results`, `validate`, `silent`, `version` and `help` are rejected anywhere. Any other unknown key
is rejected at the top level and in a job. `root` inside a job is rejected with its own message.

#### Data Model

**FilePath**: `string` — Full path of the configuration file.

**BaseDirectory**: `string` — Directory that contains the configuration file. Default base
directory for the paths of table jobs.

**RootDirectory**: `string?` — Full path of the top-level `root` key, resolved as text only; `null`
when the key is absent. The folder is never probed, so it may not exist.

**Defaults**: `FormattingSettings` — Top-level formatting defaults; members not given are `null`.

**Tables**: `IReadOnlyList<ConfigTable>` — Table jobs in file order; empty when there is no
`tables` key.

**ConfigTable** (nested record): `Index` (`int`, 1-based position in `tables`), `Inputs`
(`IReadOnlyList<string>`, at least one, as written), `Output` (`string`, as written), `Settings`
(`FormattingSettings`, the job's formatting keys).

#### Key Methods

**Load**: Loads and validates a configuration file.

- *Parameters*: `string path` — path of the configuration file (normally already a full path).
- *Returns*: `ConfigFile` — the validated configuration.
- *Preconditions*: `path` is not null.
- *Postconditions*: Every key has been validated; `RootDirectory`, when set, is a normalized full
  path that has not been checked on disk.

Algorithm:

1. Read the file with `File.ReadAllText` and parse it with `YamlStream`.
2. Take the first document. It must not be empty and its root must be a `YamlMappingNode`.
3. For each top-level key: `tables` is read by `ReadTables`; `root` is read as a non-empty
   string; `input` and `output` throw; command-line-only keys throw; formatting keys are applied
   to the defaults layer by `ApplyFormattingKey` (a `switch` over the key name that returns
   `null` for an unknown key, which then throws).
4. `ReadTables` requires a non-empty `YamlSequenceNode`; `ReadTable` requires each item to be a
   mapping, reads `input` (string or sequence), `output` and formatting keys, rejects `root` and
   unknown keys, and requires `input` and `output`.
5. When `root` was given, resolve it with
   `PathResolver.ResolveDirectory(root, BaseDirectory, "root key")`. This expands `${VAR}`,
   roots the value at the file's directory, normalizes `..` and rejects wildcards, as text only.
   The key is untrusted content whose only role is the base that relative job paths are combined
   with, so it is never probed: a folder that does not exist, or a network share, is left to the
   file-access resolver, which judges each job's inputs and outputs when the job runs (denied or not
   found). The root is validated even when `--root` will override it, so a file's validity does not
   depend on the command line.

Value parsing uses the same rules as the command line: choices are case-insensitive; `depth` and
the `column-widths` items are parsed with `NumberStyles.None` and the invariant culture, which
rejects signs, decimal points and blanks.

#### Error Handling

Loading is fail-fast: the first problem throws `InvalidOperationException`. Every message starts
with `Config file '{full path}': `, then `table job {n}: ` for a problem inside a job, and names
the key. Examples:

- `file not found`, `cannot be read: {reason}`, `invalid YAML: {reason}`, `file is empty`,
  `root must be a YAML mapping, but found {node type}`.
- `key 'tables' must be a non-empty sequence of table jobs`, `table job 2: must be a mapping`,
  `table job 1: missing required key 'output'`.
- `unknown key 'style'`, `key 'input' is only allowed inside a table job`,
  `key 'log' is a command-line option and is not allowed in a config file`,
  `table job 1: key 'root' is only allowed at the top level`.
- `key 'depth' must be an integer between 1 and 6`,
  `key 'column-widths' must be a sequence of 2 or 3 positive integers (e.g. [25, 75])`,
  `key 'root' must be a non-empty string`.
- Root resolution errors from `PathResolver` are re-thrown with the file prefix and the original
  as the inner exception, for example
  `Config file '...': Root key 'docs/*' must not contain wildcard characters.` This includes a
  malformed or overlong root (for example a null character), which `PathResolver` reports as
  `Config file '...': Invalid root key '{root}': {reason}`. A root that does not exist is not an
  error of the file.
- A configuration file path that `Path.GetFullPath` rejects is reported as
  `Config file '{path}': invalid path: {reason}`, and a path-validation exception while reading
  the file is reported as `cannot be read: {reason}`.

File-system and YAML parser exceptions are wrapped, so callers only handle
`InvalidOperationException`. `Program.Main` reports it as `Error: {message}` with exit code 1.

#### Dependencies

- **FormattingSettings** — supporting Cli type; the defaults and job layers.
- **PathResolver** — Utilities unit; resolves the `root` key.
- **YamlDotNet** — OTS library; `YamlStream`, `YamlMappingNode`, `YamlSequenceNode`,
  `YamlScalarNode` and `YamlException`.

#### Callers

- **Context.Create** — loads the file named by `--config`.
