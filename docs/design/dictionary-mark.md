# DictionaryMark System Design

This document describes the system-level design of DictionaryMark, a .NET tool
that reads YAML dictionary files and generates Markdown output in bullet or table format.

## System Architecture

DictionaryMark is a command-line application that processes YAML files containing
term-definition pairs and produces formatted Markdown output. The system consists
of four primary subsystems: Cli, SelfTest, Dictionary, and Utilities.

## Major Components

- **CLI Subsystem** - Command-line argument parsing and user interface management
- **Dictionary Subsystem** - YAML loading, conflict detection, and Markdown formatting
- **Utilities Subsystem** - Shared file-matching utilities via glob patterns
- **SelfTest Subsystem** - Automated validation framework

## Component Interactions

The Program unit acts as the system orchestrator:

1. **Initialization Phase** - Program creates Context from CLI subsystem to parse arguments
2. **Execution Phase** - Program delegates to Dictionary subsystem for generation
3. **Output Phase** - All subsystems use Context for consistent output and logging

## External Interfaces

DictionaryMark exposes a single external interface — its command-line argument list.

### Command-Line Interface

- **Version Query**: `-v`, `--version`
- **Help Display**: `-?`, `-h`, `--help`
- **Silent Mode**: `--silent`
- **Self-Validation**: `--validate`
- **Results Output**: `--results <file>`
- **Logging**: `--log <file>`
- **Input Files**: `-i`, `--input <pattern>` (repeatable)
- **Output File**: `-o`, `--output <file>`
- **Format**: `-f`, `--format <bullets|table>`
- **Section Heading**: `-s`, `--section <text>`
- **Term Header**: `--term-header <text>`
- **Definition Header**: `--def-header <text>`
- **Sort Order**: `--sort <file|alpha>`
- **Heading Depth**: `--depth <n>`
- **Validation Results**: `--result <file>` (alias for `--results`)

## Error Handling

DictionaryMark reports errors via stderr and signals failure through a non-zero exit code.

### Unrecognized Argument Handling

When an unrecognized argument is supplied on the command line, the tool:

1. Writes an error message that names the unrecognized argument to **stderr**.
2. Exits with a **non-zero exit code** (1).

No Markdown output is generated when an argument error occurs.

### Conflict Detection Behavior

When conflicting definitions are detected across input files, the tool:

1. Writes an error message identifying the conflicting term to **stderr** via
   `context.WriteError`, setting the internal error flag.
2. Returns a **non-zero exit code** (1) derived from the error flag.

Conflict errors are reported before any Markdown output is written.

## Data Flow

Data moves through the system in two phases: input processing and output generation.

### Input Processing

1. **YAML files** → YamlDictionaryLoader reads flat key-value mappings
2. **Entries** → ConflictDetector checks for term conflicts across files
3. **Validated entries** → MarkdownFormatter generates output

### Output Processing

1. **Formatted Markdown** → Written to output file or stdout
