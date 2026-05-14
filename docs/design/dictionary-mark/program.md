# Program Design

The `Program` class is the entry point for the DictionaryMark CLI tool. It parses command-line
arguments via `Context`, dispatches to the appropriate subsystem, and returns an exit code.

## Overview

`Program` provides two public members: the `Version` property (read from assembly attributes)
and the `Main` entry point. `Main` delegates to `Run`, which performs priority-ordered dispatch.
Expected exceptions (`ArgumentException`, `InvalidOperationException`) are caught and written to
`stderr`; unexpected exceptions are re-thrown after logging.

## Data Model

| Field/Property | Type     | Description                                                                     |
| -------------- | -------- | ------------------------------------------------------------------------------- |
| `Version`      | `string` | Assembly informational version, falling back to `AssemblyVersion` or `"0.0.0"`. |

## Methods

### Main(string[] args) → int

Entry point. Creates a `Context` from `args`, calls `Run`, and returns `context.ExitCode`.
Catches `ArgumentException` and `InvalidOperationException`, writes to `stderr`, returns 1.
Re-throws any other exception after writing to `stderr`.

### Run(Context context)

Priority-ordered dispatch:

1. `context.Version` → print version string only.
2. Print banner (tool name + copyright).
3. `context.Help` → print usage information.
4. `context.Validate` → call `Validation.Run(context)`.
5. Default → call `RunToolLogic(context)`.

### PrintBanner(Context context) *(private)*

Writes the `DictionaryMark version {Version}` banner and copyright notice.

### PrintHelp(Context context) *(private)*

Writes the full usage/options text to `context`.

### RunToolLogic(Context context) *(private)*

If `context.InputPatterns` is non-empty, creates a `DictionaryGenerator` and calls `Generate`.
Otherwise, writes a hint directing the user to `--input` and `--help`.

## Interactions

| Dependency            | Role                                                        |
| --------------------- | ----------------------------------------------------------- |
| `Context`             | Provides parsed flags, output methods, and exit code state. |
| `Validation`          | Invoked when `--validate` is specified.                     |
| `DictionaryGenerator` | Invoked when `--input` patterns are specified.              |
