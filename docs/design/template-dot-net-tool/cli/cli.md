# Cli Subsystem

The `Cli` subsystem provides the command-line interface for the Template DotNet Tool.
It is responsible for accepting user input from the command line and routing output to
the console and an optional log file.

## Overview

The `Cli` subsystem acts as the primary boundary between the user's shell invocation and
the tool's internal logic. It owns argument parsing, output formatting, and error tracking.
All other subsystems receive a `Context` object from the `Cli` subsystem to read parsed
flags and write output.

## Units

The `Cli` subsystem contains the following software unit:

| Unit      | File             | Responsibility                                    |
|-----------|------------------|---------------------------------------------------|
| `Context` | `Cli/Context.cs` | Argument parsing, output channels, and exit code. |

## Interfaces

The `Cli` subsystem exposes the following outbound interfaces to the rest of the tool:

- **`Context.Create`**: Factory method constructing a `Context` from `string[] args`.
  `--result` is accepted as a legacy alias for `--results`.
- **`Context.WriteLine`**: Writes a message to stdout and to the log file (if one is open).
  Stdout output is suppressed when `--silent` is active; the log file always receives the
  message regardless of `--silent`.
- **`Context.WriteError`**: Writes an error message to stderr and to the log file (if one is
  open), and unconditionally sets the error exit code. Stderr output is suppressed when
  `--silent` is active; the log file always receives the message and `ExitCode` is set to 1
  regardless of the `Silent` flag.
- **`Context.ExitCode`**: Returns 0 for success or 1 when errors have been reported.
- **`Context.HeadingDepth`**: Heading depth for markdown output (default 1); supplied via `--depth`.
- **`Context.Version`**: `true` when `-v` or `--version` was passed.
- **`Context.Help`**: `true` when `-?`, `-h`, or `--help` was passed.
- **`Context.Silent`**: `true` when `--silent` was passed.
- **`Context.Validate`**: `true` when `--validate` was passed.
- **`Context.ResultsFile`**: Path supplied after `--results` or `--result`, or `null`.
- **`Context.Dispose`**: Releases the log file `StreamWriter` if `--log` was specified.
  Callers are responsible for disposal (use `using` statement).

## Interactions

The `Cli` subsystem has no dependencies on other tool subsystems. It uses only .NET base
class library types. The `Program` unit at system level creates the `Context` and passes it
to all subsystems that need to produce output.

Subsystem verification uses `Program.Run` as an observable entry point alongside `Context.Create`.
This is intentional: the subsystem boundary is defined as the pair `(Context.Create, Program.Run)`,
and subsystem tests exercise both together to confirm end-to-end argument-to-behavior flow.

## Error Handling

`Context.Create` throws `ArgumentException` for unknown or malformed arguments.
`Program.Main` catches this exception, writes `"Error: {message}"` to stderr, and returns exit code 1.

`Context.Create` also throws `InvalidOperationException` when `--log` specifies a file that cannot
be opened (e.g., access denied, invalid path). `Program.Main` catches this exception, writes
`"Error: {message}"` to stderr, and returns exit code 1.

## Resource Lifecycle

When `--log` is active, `Context` holds an open `StreamWriter` for the duration of the invocation.
Callers must ensure `Context` is disposed (via a `using` statement or explicit `Dispose` call) to
release the file handle and flush any buffered log content.
