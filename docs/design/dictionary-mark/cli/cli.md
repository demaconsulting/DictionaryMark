# CLI Subsystem Design

The CLI subsystem handles command-line argument parsing and program output for DictionaryMark.
It is the primary interface between the user's command-line invocation and the tool's internal
logic.

## Components

### Context

Created once per tool invocation via the `Context.Create` factory method. It parses the
argument list, opens any requested log file, and exposes the parsed flags as read-only
properties. It also owns the two output channels - console and log file - through its
`WriteLine` and `WriteError` methods.

## Interactions

The CLI subsystem is consumed by `Program`, which creates a `Context` instance and passes it
to all downstream subsystems (`Validation`, `DictionaryGenerator`). The CLI subsystem has no
dependency on other DictionaryMark subsystems.
