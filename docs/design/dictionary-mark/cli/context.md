### Context

![Cli Subsystem Structure](CliView.svg)

#### Purpose

`Context` is created once per tool invocation via the `Create` factory method. It parses the
argument list using the inner `ArgumentParser` helper, opens any requested log file, and exposes
the parsed flags as read-only properties. It owns the two output channels — console and log file —
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
`--depth`.

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

**ExitCode**: `int` — Computed property: returns `1` if `_hasErrors` is `true`, otherwise `0`.

#### Key Methods

**Create**: Factory method. Delegates to the private `ArgumentParser` helper to parse `args`, then
opens the log file if `--log` was supplied.

- *Parameters*: `string[] args` — command-line arguments to parse.
- *Returns*: `Context` — a fully initialized instance.
- *Preconditions*: None.
- *Postconditions*: Returns a fully initialized `Context`; all parsed properties are set; log file
  is open if `--log` was specified.

Throws `ArgumentException` when an unknown argument or a flag missing its required value is
encountered. Throws `InvalidOperationException` when the specified log file cannot be opened.

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
`FileOrder`).

#### Error Handling

- Construction errors: `Create` throws `ArgumentException` when an argument is unrecognized or
  missing its required value; throws `InvalidOperationException` when the specified log file
  cannot be opened. `Program.Main` is expected to catch both types.
- Runtime errors: `WriteError` does not throw; it records the error message and sets
  `_hasErrors = true` so that `ExitCode` returns 1. No exception is raised at the point of the
  error.

#### Dependencies

- **OutputFormat** — Dictionary subsystem enum; type of the parsed `--format` flag value.
- **SortOrder** — Dictionary subsystem enum; type of the parsed `--sort` flag value.

#### Callers

- **Program.Main** — creates `Context` via `Context.Create(args)` once per invocation and passes
  it to `Program.Run`.
- **Validation** — creates additional short-lived `Context` instances (with `--silent`) during
  each self-test.
- **DictionaryGenerator.Generate** and **Validation.Run** — receive `Context` as a parameter and
  call its methods throughout execution.
