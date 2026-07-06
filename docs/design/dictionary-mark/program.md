## Program

![System Structure](DictionaryMarkView.svg)

### Purpose

`Program` provides the application entry point (`Main`) and the internal execution entry point
(`Run`). `Main` creates a `Context` from the CLI arguments, calls `Run`, and returns
`context.ExitCode`. Expected exceptions (`ArgumentException`, `InvalidOperationException`) are
caught in `Main` and mapped to exit code 1 without a stack trace. `Program.Version` reads the
assembly informational version attribute, falling back to `AssemblyVersion` then `"0.0.0"`.

### Data Model

**Version**: `string` — Assembly informational version string; read from the
`AssemblyInformationalVersionAttribute`, falling back to `AssemblyVersionAttribute` then
`"0.0.0"`. Not cached; the assembly attributes are re-read on every access. Callers that need
the value more than once should store the result locally.

### Key Methods

**Main**: Application entry point. Creates a `Context` from `args`, calls `Run(context)`, returns
`context.ExitCode`. Catches `ArgumentException` and `InvalidOperationException` from
`Context.Create`, writes to stderr, returns 1. Re-throws any other exception after writing to
stderr.

- *Parameters*: `string[] args` — command-line arguments passed by the .NET runtime.
- *Returns*: `int` — exit code (0 = success, 1 = error).
- *Preconditions*: None.
- *Postconditions*: Returns 0 when no errors were recorded; returns 1 when any error was recorded
  or an expected exception was caught.

**Run**: Internal execution entry point. Performs priority-ordered dispatch based on context flags.

- *Parameters*: `Context context` — fully initialized context providing parsed flags and output
  channels.
- *Returns*: `void`.
- *Preconditions*: `context` is non-null and fully initialized.
- *Postconditions*: Appropriate subsystem has been invoked; any errors are recorded in `context`.

Processing steps: (1) If `context.Version` → print version string only and return. (2) Print
banner (tool name + copyright). (3) If `context.Help` → print usage information and return.
(4) If `context.Validate` → call `Validation.Run(context)` and return. (5) Default → call
`RunToolLogic(context)`.

**PrintBanner** *(private)*: Writes the `DictionaryMark version {Version}` banner and copyright
notice to `context`.

- *Parameters*: `Context context` — output channel.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: Banner written to context output.

**PrintHelp** *(private)*: Writes the full usage and options text to `context`.

- *Parameters*: `Context context` — output channel.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: Usage text written to context output.

**RunToolLogic** *(private)*: If `context.InputPatterns` is non-empty, calls
`DictionaryGenerator.Generate(context)` directly (static method; no instance is created).
Otherwise writes a hint directing the user to `--input` and `--help`.

- *Parameters*: `Context context` — provides input patterns and output channels.
- *Returns*: `void`.
- *Preconditions*: None.
- *Postconditions*: Dictionary generation attempted or hint written to output.

### Error Handling

Two layers:

1. Expected errors — `ArgumentException` and `InvalidOperationException` from `Context.Create`
   caught in `Main`, written to stderr, mapped to exit code 1 (no stack trace).
2. Unexpected errors — Any other exception written to stderr then re-thrown so the .NET runtime
   can record it.

`Run` itself does not throw; subsystem errors (`Validation`, `DictionaryGenerator`) are handled
internally via `context.WriteError`.

### Dependencies

- **Context** — CLI subsystem; provides parsed flags, output channels, and exit code state.
- **Validation** — SelfTest subsystem; invoked when `--validate` is specified.
- **DictionaryGenerator** — Dictionary subsystem; invoked when `--input` patterns are specified.

### Callers

`Program.Main` is called by the .NET runtime. `Program.Run` is also called by `Validation` during
each self-test to exercise the full tool execution path with a crafted argument array.
