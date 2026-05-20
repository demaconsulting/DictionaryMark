## Program Design

The `Program` class is the entry point for the DictionaryMark CLI tool. It parses command-line
arguments via `Context`, dispatches to the appropriate subsystem, and returns an exit code.

### Purpose

`Program` provides two public members: the `Version` property (read from assembly attributes)
and the `Main` entry point. `Main` delegates to `Run`, which performs priority-ordered dispatch.
Expected exceptions (`ArgumentException`, `InvalidOperationException`) are caught and written to
`stderr`; unexpected exceptions are re-thrown after logging.

### Interfaces

**Exposed to the rest of the system:**

- `Program.Main(string[] args) → int` — application entry point; called by the .NET runtime.
- `Program.Run(Context context)` — internal execution entry point; called by `Main` and also by
  `Validation` during self-tests to exercise the full tool execution path.
- `Program.Version` — `string` property exposing the assembly informational version; used by
  `Run` when printing the banner and by self-tests to verify version output.

**Consumed from other items:**

- `Context.Create(string[] args)` (CLI subsystem) — factory used by `Main` to produce the
  context that drives the rest of execution.
- `Validation.Run(Context context)` (SelfTest subsystem) — invoked when `context.Validate`
  is `true`.
- `DictionaryGenerator.Generate(Context context)` (Dictionary subsystem) — invoked when
  `context.InputPatterns` is non-empty.

### Data Model

| Field/Property | Type     | Description                                                                     |
| -------------- | -------- | ------------------------------------------------------------------------------- |
| `Version`      | `string` | Assembly informational version, falling back to `AssemblyVersion` or `"0.0.0"`. |

### Key Methods

#### Main(string[] args) → int

Entry point. Creates a `Context` from `args`, calls `Run`, and returns `context.ExitCode`.
Catches `ArgumentException` and `InvalidOperationException`, writes to `stderr`, returns 1.
Re-throws any other exception after writing to `stderr`.

#### Run(Context context)

Priority-ordered dispatch:

1. `context.Version` → print version string only.
2. Print banner (tool name + copyright).
3. `context.Help` → print usage information.
4. `context.Validate` → call `Validation.Run(context)`.
5. Default → call `RunToolLogic(context)`.

#### PrintBanner(Context context) *(private)*

Writes the `DictionaryMark version {Version}` banner and copyright notice.

#### PrintHelp(Context context) *(private)*

Writes the full usage/options text to `context`.

#### RunToolLogic(Context context) *(private)*

If `context.InputPatterns` is non-empty, creates a `DictionaryGenerator` and calls `Generate`
(`DictionaryMark-Program-GenerateDictionary`).
Otherwise, writes a hint directing the user to `--input` and `--help` (`DictionaryMark-Program-NoInputHint`).

### Error Handling

`Program` applies two layers of error handling:

- **Expected errors** — `ArgumentException` and `InvalidOperationException` propagating from
  `Context.Create` (invalid arguments or unopenable log file) are caught in `Main`, written to
  `stderr`, and mapped to exit code 1. No stack trace is emitted.
- **Unexpected errors** — Any other exception is written to `stderr` and then re-thrown so the
  .NET runtime can record it in event logs.

`Run` itself does not throw; error conditions in the subsystems it dispatches to
(`Validation`, `DictionaryGenerator`) are handled internally by those subsystems via
`context.WriteError`, which sets `context.ExitCode` to 1 without throwing.

### Interactions

| Dependency            | Role                                                        |
| --------------------- | ----------------------------------------------------------- |
| `Context`             | Provides parsed flags, output methods, and exit code state. |
| `Validation`          | Invoked when `--validate` is specified.                     |
| `DictionaryGenerator` | Invoked when `--input` patterns are specified.              |

### Dependencies

| Dependency            | Role                                                        |
| --------------------- | ----------------------------------------------------------- |
| `Context`             | CLI subsystem — provides parsed flags, output channels, and exit code state. |
| `Validation`          | SelfTest subsystem — invoked when `--validate` is specified. |
| `DictionaryGenerator` | Dictionary subsystem — invoked when `--input` patterns are specified. |

### Callers

`Program.Main` is the application entry point invoked by the .NET runtime. `Program.Run` is
additionally called by `Validation` during each self-test to exercise the full tool execution
path with a crafted argument array and in-process `Context`.
