## Cli

### Overview

The Cli subsystem handles command-line argument parsing and program output for DictionaryMark. It
contains one unit, `Context`, which encapsulates all argument-parsing logic and both output
channels (console and log file). Boundaries: parsing `string[]` args into typed properties,
opening an optional log file, routing normal output through `WriteLine`, routing errors through
`WriteError`, and exposing the derived `ExitCode`. All other subsystems communicate with the rest
of the system through a `Context` instance.

### Interfaces

**Context**: The command-line context object that all other subsystems consume.

- *Type*: In-process .NET public API
- *Role*: Provider — other subsystems consume this unit
- *Contract*: `Context.Create(string[] args)` factory method; parsed boolean flags `Version`,
  `Help`, `Silent`, `Validate`; option properties `InputPatterns`, `OutputFile`, `Format`,
  `SortBy`, `SectionHeading`, `TermHeader`, `DefinitionHeader`, `HeadingDepth`, `ResultsFile`;
  output methods `WriteLine(string)` and `WriteError(string)`; derived property `ExitCode`
  (0 or 1).
- *Constraints*: `Context` implements `IDisposable`; callers must dispose to release the log file
  `StreamWriter`. Throws `ArgumentException` for unknown flags and `InvalidOperationException` for
  an unopenable log file.

**OutputFormat**: Enum from the Dictionary subsystem used as the type of `context.Format`.

- *Type*: In-process .NET public API
- *Role*: Consumer — `Context` references this type defined in the Dictionary subsystem
- *Contract*: `Bullets` (default) or `Table`
- *Constraints*: N/A

**SortOrder**: Enum from the Dictionary subsystem used as the type of `context.SortBy`.

- *Type*: In-process .NET public API
- *Role*: Consumer — `Context` references this type defined in the Dictionary subsystem
- *Contract*: `FileOrder` (default) or `Alphabetical`
- *Constraints*: N/A

### Design

`Context` is created once per invocation by `Program.Main` calling `Context.Create(args)`. The
factory delegates to the private nested `ArgumentParser`, which processes `args` sequentially:
each recognized flag sets a property; unrecognized flags throw `ArgumentException`. After parsing,
`Context.Create` copies results into a new `Context` instance (init-only properties) and opens the
log file if `--log` was specified.

Output routing: `WriteLine` writes to `Console.Out` (unless `Silent`) and to `_logWriter` (if
open). `WriteError` additionally sets `_hasErrors = true`, writes to `Console.Error` in red
(unless `Silent`), and writes to `_logWriter`. `ExitCode` derives from `_hasErrors` only; it
cannot be reset once an error is recorded. `Context` implements `IDisposable` to close the log
`StreamWriter`.
