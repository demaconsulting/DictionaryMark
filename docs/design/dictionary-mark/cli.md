## CLI Subsystem Design

The CLI subsystem handles command-line argument parsing and program output for DictionaryMark.
It is the primary interface between the user's command-line invocation and the tool's internal
logic.

### Overview

The CLI subsystem contains one unit, `Context`, which encapsulates all argument-parsing logic
and both output channels (console and log file). Its boundaries span: parsing `string[]` args
into typed properties, opening an optional log file, routing normal output through `WriteLine`,
routing errors through `WriteError`, and exposing the derived `ExitCode`. All other subsystems
communicate with the rest of the system exclusively through a `Context` instance; the CLI
subsystem has no dependency on DictionaryMark subsystems other than referencing the
`OutputFormat` and `SortOrder` enums from the Dictionary subsystem.

### Interfaces

**Exposed to the rest of the system:**

- `Context.Create(string[] args)` — factory method; returns a fully initialized `Context`.
- `context.Version`, `context.Help`, `context.Silent`, `context.Validate` — parsed boolean flags.
- `context.InputPatterns`, `context.OutputFile`, `context.Format`, `context.SortBy`,
  `context.SectionHeading`, `context.TermHeader`, `context.DefinitionHeader`,
  `context.HeadingDepth`, `context.ResultsFile` — parsed option values.
- `context.WriteLine(string)` — writes a line to stdout and the log file.
- `context.WriteError(string)` — writes an error to stderr in red, sets the error flag, and
  writes to the log file.
- `context.ExitCode` — derived from the internal error flag: 0 (no errors) or 1.

**Consumed from other items:**

- `OutputFormat` and `SortOrder` enums from the Dictionary subsystem — used as property types
  for `context.Format` and `context.SortBy`.

### Design

`Context` is created once per invocation by `Program.Main` calling `Context.Create(args)`. The
factory delegates to the private nested `ArgumentParser`, which processes `args` sequentially
in a `while` loop: each recognized flag sets a property; unrecognized flags throw
`ArgumentException`. After parsing, `Context.Create` copies the results into a new `Context`
instance (init-only properties) and opens the log file if `--log` was specified.

Output routing uses two public methods: `WriteLine` writes to `Console.Out` (unless `Silent`)
and to `_logWriter` (if open); `WriteError` additionally sets `_hasErrors = true` and writes
to `Console.Error` in red (unless `Silent`). `ExitCode` derives its value purely from `_hasErrors`, so the exit
code cannot be reset once an error has been recorded. `Context` implements `IDisposable` to
close the log `StreamWriter` on disposal.

### Context

Created once per tool invocation via the `Context.Create` factory method. It parses the
argument list, opens any requested log file, and exposes the parsed flags as read-only
properties. It also owns the two output channels - console and log file - through its
`WriteLine` and `WriteError` methods.

### Accepted Flags

The following list describes every flag accepted by `Context.Create`, its short alias (if any),
the property it sets, and its default value.

- `--version` (`-v`) → `Version` (default: `false`)
- `--help` (`-h`, `-?`) → `Help` (default: `false`)
- `--silent` → `Silent` (default: `false`)
- `--validate` → `Validate` (default: `false`)
- `--log <file>` → *(log channel)* (default: disabled)
- `--results <file>` / `--result <file>` → `ResultsFile` (default: `null`)
- `--depth <n>` → `HeadingDepth` (default: `1`)
- `--input <pattern>` (`-i`) → `InputPatterns` (default: `[]`)
- `--output <file>` (`-o`) → `OutputFile` (default: `null`)
- `--format <table|bullets>` (`-f`) → `Format` (default: `Bullets`)
- `--section <heading>` (`-s`) → `SectionHeading` (default: `null`)
- `--term-header <text>` → `TermHeader` (default: `"Term"`)
- `--def-header <text>` / `--definition-header <text>` → `DefinitionHeader` (default: `"Definition"`)
- `--sort <file|alpha>` → `SortBy` (default: `FileOrder`)

### Error Handling

- **Invalid arguments**: When an unknown flag or a flag with a missing required value is
  encountered, `Context.Create` throws `ArgumentException`. `Program.Main` catches this,
  writes the error message to stderr via `Console.Error`, and returns exit code 1.
- **`WriteError` behavior**: Calling `context.WriteError(message)` sets `_hasErrors` to
  `true` (causing `ExitCode` to return 1), writes the message to `Console.Error` in red
  (unless `Silent`), and also writes it to the log file when one is open.
- **Log file failure**: If `--log` is specified and the file cannot be opened,
  `Context.Create` throws `InvalidOperationException` wrapping the original file-system
  exception.

### Interactions

The CLI subsystem is consumed by `Program`, which creates a `Context` instance and passes it
to all downstream subsystems (`Validation`, `DictionaryGenerator`). The CLI subsystem has no
behavioral dependency on other DictionaryMark subsystems, but `Context` references
Dictionary types (`OutputFormat`, `SortOrder`) for shared option values.
