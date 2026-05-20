### Context Design

The `Context` class handles command-line argument parsing and program output for DictionaryMark.
It is the primary interface between the user's command-line invocation and the tool's internal
logic.

#### Purpose

`Context` is created once per tool invocation via the `Create` factory method. It parses the
argument list using the inner `ArgumentParser` helper, opens any requested log file, and exposes
the parsed flags as read-only properties. It also owns the two output channels - console and log
file - through its `WriteLine` and `WriteError` methods.

#### Data Model

- `_logWriter` (`StreamWriter?`) — Log file writer; `null` when logging is disabled.
- `_hasErrors` (`bool`) — Set to `true` on the first `WriteError` call.
- `Version` (`bool`) — `true` when `-v` or `--version` was passed.
- `Help` (`bool`) — `true` when `-?`, `-h`, or `--help` was passed.
- `Silent` (`bool`) — `true` when `--silent` was passed.
- `Validate` (`bool`) — `true` when `--validate` was passed.
- `ResultsFile` (`string?`) — Path supplied after `--results` or `--result`, or `null`.
- `HeadingDepth` (`int`) — Heading depth for output; valid range 1–6 (default 1); via `--depth`.
- `InputPatterns` (`List<string>`) — Input file patterns specified via `-i` or `--input` (repeatable).
- `OutputFile` (`string?`) — Output file path via `-o` or `--output`; `null` means stdout.
- `Format` (`OutputFormat`) — Output format (Bullets or Table); via `-f` or `--format`. Default is Bullets.
- `SectionHeading` (`string?`) — Optional section heading text; via `-s` or `--section`.
- `TermHeader` (`string`) — Term column header; via `--term-header`. Default is `"Term"`.
- `DefinitionHeader` (`string`) — Definition column header; via `--def-header` or `--definition-header`. Default is `"Definition"`.
- `SortBy` (`SortOrder`) — Sort order (FileOrder or Alphabetical); via `--sort`. Default is FileOrder.
- `ExitCode` (`int`) — `1` if `_hasErrors`; `0` otherwise.

#### Key Methods

##### Create(string[] args)

Factory method. Delegates to the private `ArgumentParser` helper and opens the log file if
`--log` was supplied.

**Throws:** `ArgumentException` - when an unknown argument or missing value is encountered.
`InvalidOperationException` - when a log file cannot be opened.

##### WriteLine(string message)

Writes `message` to `Console.Out` (unless `Silent`) and to `_logWriter` (if open).

##### WriteError(string message)

Sets `_hasErrors = true`, writes `message` to `Console.Error` in red (unless `Silent`),
and to `_logWriter` (if open).

##### Dispose()

Disposes `_logWriter` and sets it to `null`.

#### Error Handling

`Context` signals errors in two ways:

- **Construction errors** — `Create` throws `ArgumentException` when an argument is unrecognised
  or is missing its required value, and `InvalidOperationException` when the specified log file
  cannot be opened. The caller (`Program.Main`) is expected to catch both exception types.
- **Runtime errors** — `WriteError` does not throw; it records the error message to stderr and
  the log file, and sets `_hasErrors = true` so that `ExitCode` returns 1. No exception is raised
  at the point of the error.

#### Interactions

`Context` uses `OutputFormat` and `SortOrder` types from the Dictionary subsystem to represent
the parsed `--format` and `--sort` flag values respectively. Apart from these type references,
`Context` has no behavioral dependency on other DictionaryMark subsystems. It uses only .NET
base class library types (`Console`, `StreamWriter`) for I/O.

#### Dependencies

| Dependency     | Role                                                                   |
| -------------- | ---------------------------------------------------------------------- |
| `OutputFormat` | Dictionary subsystem enum — type of the parsed `--format` flag value.  |
| `SortOrder`    | Dictionary subsystem enum — type of the parsed `--sort` flag value.    |
| `Console`      | .NET BCL — stdout and stderr output channels.                          |
| `StreamWriter` | .NET BCL — optional log file channel opened when `--log` is specified. |

#### Callers

`Program.Main` creates a `Context` instance via `Context.Create(args)` once per invocation and
passes it to `Program.Run`. `Validation` creates additional short-lived `Context` instances
(with `--silent`) during each self-test to capture tool output without writing to the console.
`DictionaryGenerator.Generate` and `Validation.Run` each receive the `Context` instance as a
parameter and call its `WriteLine`, `WriteError`, and property accessors throughout their
execution.
