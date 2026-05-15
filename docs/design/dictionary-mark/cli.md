## CLI Subsystem Design

The CLI subsystem handles command-line argument parsing and program output for DictionaryMark.
It is the primary interface between the user's command-line invocation and the tool's internal
logic.

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
