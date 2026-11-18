### Context

![Cli Subsystem Structure](CliView.svg)

#### Purpose

`Context` is created once per tool invocation via the `Create` factory method. It parses the
argument list using the inner `ArgumentParser` helper, resolves the root directory, loads the
configuration file, builds the generation jobs, opens any requested log file, and exposes the
parsed flags as read-only properties. It owns the two output channels — console and log file —
through `WriteLine` and `WriteError`. It signals errors both at construction time (exceptions) and
at runtime (via `_hasErrors` flag).

#### Data Model

**\_logWriter**: `StreamWriter?` — Log file writer; `null` when logging is disabled. Disposed by
`Dispose()`.

**\_hasErrors**: `bool` — Set to `true` on the first `WriteError` call; never reset. Invariant:
once `true`, remains `true` for the lifetime of the instance.

**Version**: `bool` — `true` when `-v` or `--version` was passed; default `false`.

**Help**: `bool` — `true` when `-?`, `-h`, or `--help` was passed; default `false`.

**Silent**: `bool` — `true` when `--silent` was passed; default `false`. When `true`, neither
`WriteLine` nor `WriteError` writes to the console.

**Validate**: `bool` — `true` when `--validate` was passed; default `false`.

**ResultsFile**: `string?` — Path supplied after `--results` or `--result`; `null` when not
specified.

**HeadingDepth**: `int` — Heading depth for output; valid range 1–6; default 1; set via
`--depth` or the configuration file's top-level `depth` key.

**InputPatterns**: `List<string>` — Input file patterns specified via `-i` or `--input`
(repeatable); default empty list.

**OutputFile**: `string?` — Output file path via `-o` or `--output`; `null` means stdout.

**Format**: `OutputFormat` — Output format (`Bullets` or `Table`); set via `-f` or `--format`;
default `Bullets`.

**SectionHeading**: `string?` — Optional section heading text; set via `-s` or `--section`;
`null` when not specified.

**TermHeader**: `string` — Term column header; set via `--term-header`; default `"Term"`.

**DefinitionHeader**: `string` — Definition column header; set via `--def-header` or
`--definition-header`; default `"Definition"`.

**SortBy**: `SortOrder` — Sort order; set via `--sort`; default `FileOrder`.

**Kind**: `InputKind` — Input kind; set via `--kind <terms|deprecated>` (case-insensitive); default
`Terms`.

**TermHeaderSpecified**: `bool` — `true` when a term header was given by `--term-header` or the
configuration file's top-level `term-header` key.

**UseHeader**: `string` — Deprecated "use" column header; `--use-header`; default `"Use instead"`.

**WhereHeader**: `string` — Deprecated "where" column header; `--where-header`; default
`"Where it was used"`.

The formatting properties above (`HeadingDepth`, `Format`, `SectionHeading`, `TermHeader`,
`DefinitionHeader`, `SortBy`, `Kind`, `UseHeader`, `WhereHeader`) hold the effective top-level
value: the command line, else the configuration file's top-level default, else the built-in
default. Per-job values are in `Jobs`.

**ColumnWidths**: `IReadOnlyList<int>?` — Effective top-level relative column widths from
`--column-widths` or the configuration file's `column-widths` key; `null` when not given.

**ConfigFilePath**: `string?` — Full path of the configuration file given by `--config`, after
`${VAR}` expansion and resolution against the working directory; `null` when not given.

**RootDirectory**: `string?` — Effective root directory: the resolved `--root`, else the
configuration file's resolved `root` key, else `null`. Always an existing full path when set.

**CommandLineFormatting**: `FormattingSettings` — The formatting options given explicitly on the
command line; options not given are `null`. Highest-precedence formatting layer.

**Jobs**: `IReadOnlyList<TableJob>` — The generation jobs: one per configuration table, or one
command-line job when `--input` was given, or none.

**FileAccess**: `FileAccessPolicy` — The file-access policy built from `--file-access` and
`--allow-root`; `FileAccessPolicy.Any` (mode any, no folders) when neither was given. Judges every
path of the configuration jobs.

**UsesConfigTables**: `bool` — `true` when `Jobs` come from the configuration file's `tables`.

**ExitCode**: `int` — Computed property: returns `1` if `_hasErrors` is `true`, otherwise `0`.

#### Key Methods

**Create**: Factory method. The steps are, in order:

1. Delegate to the private `ArgumentParser` helper to parse `args`. Formatting options are
   recorded only when given, as the command-line `FormattingSettings` layer. A second `--config`,
   `--root` or `--file-access` throws `ArgumentException`; so does an empty or whitespace `--root`
   or `--allow-root` value. After the last argument the parser settles the file-access mode
   (private `ResolveFileAccessMode`): the `--file-access` value parsed with
   `FileAccessPolicy.ParseMode` (which lists the modes when the value is invalid), or `roots` when
   only `--allow-root` was given, or `any`. `--allow-root` with `--file-access any` or
   `no-symlinks` throws `ArgumentException` naming the conflict and the spelling to use instead.
2. If `--root` was given, resolve it with `PathResolver.ResolveExistingDirectory` against the
   working directory (description `--root option`).
3. If `--config` was given, resolve it with `PathResolver.ResolvePath` against the working
   directory (never the root) and load it with `ConfigFile.Load`.
4. Select the effective root: `--root`, else the configuration file's `RootDirectory`.
5. Build the file-access policy (private `BuildFileAccessPolicy`). Mode any needs no folders and
   returns `FileAccessPolicy.Any` without touching the file system. Otherwise the allowed folders
   are the `--allow-root` values, each resolved with `PathResolver.ResolveExistingDirectory`
   against the working directory, never the root (description `--allow-root option`), or else the
   working directory and the configuration file's folder. On Windows each `--allow-root` folder is
   first resolved with `PathResolver.ResolvePath` and refused when
   `FileAccessPolicy.IsDeviceNamespaceFolder` is true (a device namespace other than an ordinary
   extended-length path), before its existence is checked. An ordinary extended-length path
   (`\\?\C:\`, `\\?\Volume{GUID}\`) passes that check. It is then rejected by the wildcard rule of
   `PathResolver.ResolveExistingDirectory`, as for `--root`, because it contains `?`. So no `\\?\`
   spelling is accepted. The policy is created with
   `FileAccessPolicy.Create(mode, folders, configFolder)`; the configuration file's folder is the
   trusted base of `no-symlinks`.
6. Build the jobs (private `BuildJobs`). When the configuration file has tables, `--input` or
   `--output` throws `ArgumentException` naming the file; otherwise each table becomes
   `TableJob.Create(index, inputs, output, base, [command line, job, config defaults])`. A
   column-width error from `TableJob.Create` is re-thrown with the `Config file '{path}': `
   prefix. Without tables, when `--input` was given, one command-line job
   `TableJob.Create(null, inputs, output, base, [command line, config defaults])` is created.
   The base directory comes from the private `SelectBaseDirectory(root, originDefault)`, which
   returns the root when set and otherwise the origin default (the configuration file's
   directory for table jobs, the working directory for the command-line job).
7. Open the log file if `--log` was supplied.

- *Parameters*: `string[] args` — command-line arguments to parse.
- *Returns*: `Context` — a fully initialized instance.
- *Preconditions*: None.
- *Postconditions*: Returns a fully initialized `Context`; all parsed properties are set; every
  job has been created and validated; log file is open if `--log` was specified.

Throws `ArgumentException` when an argument is unknown, invalid, repeated or missing its value,
when the `--file-access` value is not a mode, when `--allow-root` is combined with
`--file-access any` or `no-symlinks`, when `--input`/`--output` is combined with configuration
tables, or when a column-width count does not match the table. Throws `InvalidOperationException`
when the root directory or an `--allow-root` folder is invalid or missing (or, on Windows, an
`--allow-root` folder is in a device namespace), the `--root` or `--config`
path is malformed (reported by `PathResolver` as
`Invalid --root option '...': ...` or `Invalid config file path '...': ...`), the configuration
file is invalid, or the log file cannot be opened.

**ParseColumnWidths** (private, in `ArgumentParser`): splits the `--column-widths` value on `,`,
trims each item, and parses it with `NumberStyles.None` and the invariant culture, which rejects
signs, decimal points and blanks. Each value must be greater than 0, and there must be 2 or 3
values; otherwise it throws `ArgumentException("--column-widths requires 2 or 3 comma-separated
positive integers (e.g. 25,75)")`. The count is checked against the table shape later, in
`TableJob.Create`.

**WriteLine**: Writes `message` to `Console.Out` (unless `Silent` is `true`) and to `_logWriter`
(if open).

- *Parameters*: `string message` — text line to write.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: Message written to console output and/or log file.

**WriteError**: Sets `_hasErrors = true`, writes `message` to `Console.Error` in red (unless
`Silent`), and to `_logWriter` (if open).

- *Parameters*: `string message` — error message to write.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: `_hasErrors` is `true`; `ExitCode` will return `1`; message written to stderr
  and log.

**Dispose**: Disposes `_logWriter` and sets it to `null`.

- *Parameters*: N/A — parameterless.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: `_logWriter` is disposed and set to `null`.

The following flags are accepted by `Create`: `--version` (`-v`) → `Version`; `--help` (`-h`,
`-?`) → `Help`; `--silent` → `Silent`; `--validate` → `Validate`; `--log <file>` → log channel
(default disabled); `--results <file>` / `--result <file>` → `ResultsFile`; `--depth <n>` →
`HeadingDepth` (default 1); `--input <pattern>` (`-i`) → `InputPatterns` (repeatable);
`--output <file>` (`-o`) → `OutputFile`; `--format <table|bullets>` (`-f`) → `Format` (default
`Bullets`); `--section <heading>` (`-s`) → `SectionHeading`; `--term-header <text>` →
`TermHeader` (default `"Term"`); `--def-header <text>` / `--definition-header <text>` →
`DefinitionHeader` (default `"Definition"`); `--sort <file|alpha>` → `SortBy` (default
`FileOrder`); `--kind <terms|deprecated>` → `Kind` (invalid value throws `ArgumentException`);
`--use-header <text>` → `UseHeader`; `--where-header <text>` → `WhereHeader`;
`--column-widths <w1,w2[,w3]>` → `ColumnWidths`; `--config <file>` → `ConfigFilePath` (once
only); `--root <dir>` → `RootDirectory` (once only, non-empty); `--file-access <mode>` →
`FileAccess.Mode` (once only; `any`, `roots`, `no-symlinks` or `roots,no-symlinks` in any letter
case); `--allow-root <dir>` → `FileAccess.AllowedFolders` (repeatable, non-empty; implies `roots`).

- Construction errors: `Create` throws `ArgumentException` when an argument is unrecognized,
  invalid, repeated or missing its required value, when `--input`/`--output` is combined with
  configuration tables, or when a column-width count does not match the table; throws
  `InvalidOperationException` when the root directory is invalid or missing, the configuration
  file is invalid, or the log file cannot be opened. `Program.Main` is expected to catch both
  types. All of these stop the run before any job runs.
- Runtime errors: `WriteError` does not throw; it records the error message and sets
  `_hasErrors = true` so that `ExitCode` returns 1. No exception is raised at the point of the
  error.

#### Dependencies

- **OutputFormat** — Dictionary subsystem enum; type of the parsed `--format` flag value.
- **SortOrder** — Dictionary subsystem enum; type of the parsed `--sort` flag value.
- **InputKind** — Dictionary subsystem enum; type of the parsed `--kind` flag value.
- **ConfigFile** — Cli unit; loads the configuration file given by `--config`.
- **TableJob** and **FormattingSettings** — Cli unit and supporting type; the generation jobs and
  the formatting precedence layers.
- **PathResolver** — Utilities unit; resolves `--config`, `--root` and `--allow-root`.
- **FileAccessPolicy** — Utilities unit; parses the `--file-access` mode, refuses a device-namespace
  `--allow-root` folder and holds the allowed folders.

#### Callers

- **Program.Main** — creates `Context` via `Context.Create(args)` once per invocation and passes
  it to `Program.Run`.
- **Validation** — creates additional short-lived `Context` instances (with `--silent`) during
  each self-test.
- **DictionaryGenerator.Generate** and **Validation.Run** — receive `Context` as a parameter and
  call its methods throughout execution.
