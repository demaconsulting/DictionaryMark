# Program

The `Program` class is the main entry point for the Template DotNet Tool. It creates a `Context`
from command-line arguments, dispatches to the appropriate logic based on the flags, and returns
the exit code.

## Overview

`Program` owns the top-level execution flow. It delegates all argument interpretation to `Context`
and all validation logic to `Validation`. Its own responsibility is limited to reading the flags
that `Context` exposes and calling the correct handler.

## Data Model

`Program` holds no instance state. Its single static property is:

| Field     | Type     | Description                                                    |
|-----------|----------|----------------------------------------------------------------|
| `Version` | `string` | The tool version from `AssemblyInformationalVersionAttribute`. |

## Methods

### Main(string[] args)

Entry point. Creates a `Context`, calls `Run`, and returns `context.ExitCode`.

**Error handling:** catches `ArgumentException` and `InvalidOperationException` — writes
`"Error: {message}"` to stderr and returns exit code 1. Catches unexpected `Exception` —
writes `"Unexpected error: {message}"` to stderr and re-throws.

**Returns:** `int` — 0 for success, non-zero for failure.

### Run(Context context)

Inspects the flags on `context` and dispatches:

- `Version` flag → prints `Version` string only, then returns (no banner).
- Otherwise: calls `PrintBanner`, then:
  - `Help` flag → calls `PrintHelp`, then returns.
  - `Validate` flag → calls `Validation.Run(context)`.
  - No flags → calls `RunToolLogic`.

### PrintBanner(Context context)

Writes the tool name, version, and copyright line to `context`.

### PrintHelp(Context context)

Writes the usage/options table to `context`.

### RunToolLogic(Context context)

Placeholder for main tool logic. Currently writes a demo message:

```text
Template DotNet Tool - Demo Functionality
This is a template project demonstrating best practices.

Replace this with your actual tool implementation.
```

Downstream projects replace this method body with actual tool behavior.

### Version (property)

Reads `AssemblyInformationalVersionAttribute` from the executing assembly, falling back to
`AssemblyVersion`, then `"0.0.0"`.

## Interactions

The `Program` unit uses the following dependencies:

- **`Context`**: Reads flags; calls `WriteLine`/`WriteError`.
- **`Validation`**: Calls `Validation.Run` when validate flag is set.
