### PathResolver

![Utilities Subsystem Structure](UtilitiesView.svg)

#### Purpose

`PathResolver` turns a path written by a user — on the command line or in a configuration file —
into a path the file system can use. It expands `${VAR}` environment-variable references and roots
relative paths at a base directory. It deliberately allows `..` segments, because users reference
shared files outside the directory that holds their configuration. It is therefore a separate unit
from `PathHelpers`, which is a path-traversal security boundary that rejects `..`. `PathResolver`
is a static, stateless class; it reads environment variables but never modifies them.

#### Data Model

N/A - static class with no instance state.

#### Key Methods

**ExpandEnvironmentVariables**: Replaces each `${NAME}` reference with the variable value.

- *Parameters*: `string path`; `string description` — where the path came from, for example
  `glossary path`, `output path`, `config file path`, `--root option` or `root key`.
- *Returns*: `string` — the expanded path.
- *Preconditions*: Arguments are not null.
- *Postconditions*: No `${NAME}` reference remains. NAME must match `[A-Za-z_][A-Za-z0-9_]*`;
  other text is unchanged. Expansion happens once; a value containing `${...}` is not expanded
  again.

**Combine**: Expands `path` and roots it at `baseDirectory` without normalizing it.

- *Parameters*: `string path`; `string baseDirectory`; `string description`.
- *Returns*: `string` — `Path.Combine(baseDirectory, expanded)`. An absolute expanded path is
  returned unchanged, so absolute paths ignore the base directory.
- *Postconditions*: Only `path` is expanded; `baseDirectory` is used literally, so an already
  resolved root is never expanded a second time. Segments are not normalized, because normalizing
  a glob would change its meaning (for example collapsing `**/..`).

**ResolvePath**: `Path.GetFullPath(Combine(path, baseDirectory, description))`. Used for literal
paths: output files and the configuration file. The result is a full path with `.` and `..`
collapsed. No file-system access.

**IsInvalidPathException**: Returns `true` for `ArgumentException` (other than
`ArgumentNullException`), `NotSupportedException` and `PathTooLongException`. This is the single
definition of a path-validation exception. `ResolvePath` and `ResolveExistingDirectory` use it, and
so do the callers' exception filters around their own `File` and `Directory` calls.

**ResolveExistingDirectory**: Resolves a root directory.

- *Parameters*: `string path`; `string baseDirectory`; `string description`.
- *Returns*: `string` — the normalized full path of the existing directory.
- *Postconditions*: The directory exists.

Algorithm: compute `Combine`; reject the result if it contains `*` or `?`, because a root becomes
the literal prefix of every rooted glob and a wildcard would corrupt the split between the literal
base directory and the wildcard remainder; normalize with `Path.GetFullPath`; require
`Directory.Exists`, which is `false` for files and missing paths.

#### Error Handling

All errors are `InvalidOperationException` (except null arguments, which throw
`ArgumentNullException`):

- Undefined or empty variable:
  `Environment variable '{name}' referenced by {description} '{path}' is not defined.`
- Wildcard in a directory: `{Description} '{path}' must not contain wildcard characters.` (the
  description is capitalized).
- Missing directory: `Directory '{full}' specified by {description} '{path}' does not exist.`
- Malformed or overlong path (a path-validation exception from `Path.GetFullPath` in
  `ResolvePath` or `ResolveExistingDirectory`, for example a null character):
  `Invalid {description} '{path}': {message}`, with the original exception as the inner exception.

Callers either report the message as a per-job error (`DictionaryGenerator`, `GlobMatcher` via
`DictionaryGenerator`) or let it stop the run before any job (`Context`, `ConfigFile`).

#### Dependencies

N/A - uses only .NET base class library path and environment APIs.

#### Callers

- **GlobMatcher.Resolve** — `Combine` for every input pattern (description `glossary path`).
- **DictionaryGenerator.GenerateJob** — `ResolvePath` for the output file (description
  `output path`), and `ExpandEnvironmentVariables` and `Combine` for each input pattern of a
  configuration job before the file-access check (description `glossary path`).
- **Context.Create** — `ResolvePath` for `--config` and `ResolveExistingDirectory` for `--root`
  and each `--allow-root` (description `--allow-root option`).
- **ConfigFile.Load** — `ExpandEnvironmentVariables` and `ResolveExistingDirectory` for the `root`
  key.
- **IsInvalidPathException** — used by `DictionaryGenerator` (input resolution, reading and
  writing) and `ConfigFile` (config path normalization and reading) in their exception filters.
