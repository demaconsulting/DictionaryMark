# Context

The `Context` class handles command-line argument parsing and program output for the
Template DotNet Tool. It is the primary interface between the user's command-line invocation
and the tool's internal logic.

## Overview

`Context` is created once per tool invocation via the `Create` factory method. It parses
the argument list, opens any requested log file, and exposes the parsed flags as read-only
properties. It also owns the two output channels — console and log file — through its
`WriteLine` and `WriteError` methods.

## Data Model

| Field          | Type            | Description                                                                       |
|----------------|-----------------|-----------------------------------------------------------------------------------|
| `_logWriter`   | `StreamWriter?` | Log file writer; `null` when logging is disabled.                                 |
| `_hasErrors`   | `bool`          | Set to `true` on the first `WriteError` call.                                     |
| `Version`      | `bool`          | `true` when `-v` or `--version` was passed.                                       |
| `Help`         | `bool`          | `true` when `-?`, `-h`, or `--help` was passed.                                   |
| `Silent`       | `bool`          | `true` when `--silent` was passed.                                                |
| `Validate`     | `bool`          | `true` when `--validate` was passed.                                              |
| `ResultsFile`  | `string?`       | Path supplied after `--results` or `--result`, or `null`.                         |
| `HeadingDepth` | `int`           | Heading depth for markdown output; valid range 1–6 (default 1); via `--depth`.    |
| `ExitCode`     | `int`           | `1` if `_hasErrors`; `0` otherwise.                                               |

## Methods

### Create(string[] args)

Factory method. Delegates to the private `ArgumentParser` helper and opens the log file if
`--log` was supplied.

**Throws:** `ArgumentException` — when an unknown argument or missing value is encountered.
`InvalidOperationException` — when a log file cannot be opened.

### WriteLine(string message)

Writes `message` to `Console.Out` (unless `Silent`) and to `_logWriter` (if open).

### WriteError(string message)

Sets `_hasErrors = true`, writes `message` to `Console.Error` in red (unless `Silent`),
and to `_logWriter` (if open).

### Dispose()

Disposes `_logWriter` and sets it to `null`.

## Interactions

`Context` has no dependencies on other tool units. It uses only .NET base class library types
(`Console`, `StreamWriter`).
