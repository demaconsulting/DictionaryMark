### DictionaryGenerator

![Dictionary Subsystem Structure](DictionaryView.svg)

#### Purpose

`DictionaryGenerator` is a static class that orchestrates the full dictionary generation pipeline.
The `Generate` static method runs every job of the context. For each job, `GenerateJob` resolves
input file paths using `GlobMatcher`, loads entries from each file via `YamlDictionaryLoader`,
detects conflicts with `ConflictDetector`, formats output with `MarkdownFormatter`, and writes the
result to the output file or stdout.

#### Data Model

N/A - static class with no instance state.

#### Key Methods

**Generate**: Runs every job.

- *Parameters*: `Context context` — provides the jobs and the output channels.
- *Returns*: `void`.
- *Preconditions*: `context` is non-null.
- *Postconditions*: Every job has run. When there are no jobs,
  `Error: No input files found matching the specified patterns.` is reported. When
  `context.UsesConfigTables` is true and at least one job failed,
  `Error: {failed} of {total} table jobs failed.` is reported.

**GenerateJob** *(internal)*: Runs one job; returns `true` when the job produced its output.

- *Parameters*: `Context context`; `TableJob job`.
- *Returns*: `bool` — `false` after reporting an error.
- *Preconditions*: Arguments are non-null.
- *Postconditions*: Output written to file or stdout, or an error reported and no output written.

Processing steps: (1) Resolve input file paths — for a configuration job (`job.ArePathsJudged`), the private
`ResolveJudgedPatterns` expands and roots every pattern with `PathResolver` and calls
`context.FileAccess.ResolvePattern(pattern, expanded, rooted)`, which judges the pattern and every folder it lists
before matching and returns `JudgedPath` values; a glob that matches nothing throws `InvalidOperationException`
(`Input pattern '{pattern}' matched no files.`); files named by several patterns are kept once, with the judged path
of the first literal pattern that names them, and sorted by full path. The command-line job calls
`GlobMatcher.Resolve(job.InputPatterns, job.BaseDirectory)` and wraps each file as a `JudgedPath`. Return early on
`FileAccessDeniedException`, `ArgumentException`, `InvalidOperationException`, `IOException`,
`UnauthorizedAccessException` or empty result. (2) Load entries — for a configuration job, read each file with
`context.FileAccess.ReadAllText(judgedPath)`, where the judged path shows a literal pattern as written and a file a
glob matched by its full path, and judges a matched file's name by the written-text rules too; a missing file
(`FileNotFoundException`) reports `Input file '{full}' was not found.` and a missing folder
(`DirectoryNotFoundException`) reports `Input directory '{folder}' was not found (pattern '{shown}').`. The read open
is not preceded by a by-name probe of the folder, so the message follows what the open reports: on Windows a missing
folder of the path (`ERROR_PATH_NOT_FOUND`) reports `Input directory …`, while on Linux and macOS the open reports
`ENOENT` for a missing folder too, so the message is `Input file '{full}' was not found.`, and `Input directory …`
appears there only when a component of the path is a file (`ENOTDIR`); parse the text
with `YamlDictionaryLoader.LoadText` (or `LoadDeprecatedText`); for the command-line job, call
`YamlDictionaryLoader.Load` (or `LoadDeprecated` when `job.Options.Kind` is deprecated) for each input file; return
early on `FileAccessDeniedException`, `IOException`, `UnauthorizedAccessException`, `YamlException` or
`InvalidOperationException`. A denied file fails the job; it is never skipped. (3) Detect
conflicts — call
`ConflictDetector.Detect(approved, deprecated)` for this job's files; if any conflicts found, report each and return.
(4) Format output — call `MarkdownFormatter.Format` or `FormatDeprecated` with `job.Options` (already merged by
`TableJob`, including the `Not used` deprecated term header and the column widths) using only entries from the primary
input files. (5) Write output — if `job.OutputFile` is set, resolve it with
`PathResolver.ResolvePath(job.OutputFile, job.BaseDirectory, "output path")` and expand it with
`PathResolver.ExpandEnvironmentVariables`. A configuration job writes with
`context.FileAccess.WriteAllText(new JudgedPath(job.OutputFile, expanded, fullPath), output)`, so the text rules see
the expanded output while the message shows it as written, and the policy creates missing parent folders only when it
is allowed; the command-line job creates missing parent directories with
`Directory.CreateDirectory` and writes via `File.WriteAllText`. Without an output file the result is written via
`context.WriteLine`. The base
directory may be a root directory, the configuration file's directory, or the working directory; the generator does
not distinguish them.

**Report** *(private)*: Writes `Error: {text}` for the command-line job, or `Error: {label}: {text}` for a
configuration job, through `context.WriteError`. Every job error goes through this method, so command-line messages are
unchanged.

#### Error Handling

`GenerateJob` applies an early-return-on-error strategy within a job: each error condition calls
`Report` (which calls `context.WriteError`, setting exit code to 1) and returns `false`. `Generate`
continues with the next job, so one run reports the failures of every job. Every expected exception
is caught inside `GenerateJob`, so no job failure escapes to stop the jobs that follow it; there is no
catch-all for unexpected exceptions.

Error cases:

- **Invalid or unresolved input** — `InvalidOperationException` or a path-validation exception
  (`PathResolver.IsInvalidPathException`: `ArgumentException`, `NotSupportedException` or
  `PathTooLongException`) from `GlobMatcher.Resolve` or `FileAccessPolicy.ResolvePattern`. An
  `InvalidOperationException` reports `"Error: {message}"` (pattern matched no files, file or directory not
  found, undefined `${VAR}`).
  A path-validation exception reports `"Error: Invalid input pattern: {message}"` (for example a
  null character or a glob rejected by the globbing library).
- **I/O or access error resolving input** — `IOException` or `UnauthorizedAccessException` from
  `GlobMatcher.Resolve` or `FileAccessPolicy.ResolvePattern` (directory enumeration); reports
  `"Error: Failed to resolve input patterns: {message}"`.
- **No files found** — no jobs, or an empty list from `GlobMatcher.Resolve`; reports
  `"Error: No input files found matching the specified patterns."`.
- **Missing input of a configuration job** — `FileNotFoundException` or `DirectoryNotFoundException` from
  `FileAccessPolicy.ReadAllText`; reports `"Error: Input file '{file}' was not found."` or
  `"Error: Input directory '{folder}' was not found (pattern '{pattern}')."`. A missing folder of the path gives
  the directory message on Windows and the file message on Linux and macOS (`ENOENT`), where the directory
  message means a component of the path is a file (`ENOTDIR`).
- **I/O or access error reading YAML** — `IOException`, `UnauthorizedAccessException` or a
  path-validation exception from `YamlDictionaryLoader.Load` or `FileAccessPolicy.ReadAllText`; reports
  `"Error: Failed to read file '{file}': {message}"`.
- **Malformed YAML** — `YamlException` from `YamlDictionaryLoader.Load` (syntax error); reports
  `"Error: Invalid YAML in file '{file}': {message}"`.
- **Invalid YAML structure** — `InvalidOperationException` from `YamlDictionaryLoader.Load`;
  reports `"Error: Invalid YAML in file '{file}': {message}"`.
- **Unresolvable output path** — `InvalidOperationException` from `PathResolver.ResolvePath`, for
  an undefined variable or a malformed or overlong path (for example a null character); reports
  `"Error: {message}"`, which for a malformed path reads
  `"Error: Invalid output path '{file}': {message}"` with the output path as written.
- **I/O or access error writing output** — `IOException`, `UnauthorizedAccessException` or a
  path-validation exception from `Directory.CreateDirectory` or `File.WriteAllText`. A path that
  normalizes cleanly can still be rejected on write, for example a wildcard in a Windows file name.
  Reports `"Error: Failed to write output file '{file}': {message}"` with the output path as
  written.
- **File access denied** — `FileAccessDeniedException` from `FileAccessPolicy` while judging a
  pattern, reading a file or writing the output of a configuration job; reports
  `"Error: {message}"`, where the message is
  `file-access-denied: '{written}' (real path '{real}'): {reason}` (the real-path part is left out when
  no file was opened). Nothing is written for the job.

For configuration jobs, every message above is prefixed with the job label, for example
`"Error: table job 2 ('docs/deprecated.md'): Input pattern 'x/*.yaml' matched no files."`.

#### Dependencies

- **Context** — CLI subsystem; provides the jobs, output channel, and exit code.
- **TableJob** — CLI subsystem; one job's inputs, output, base directory, label and options.
- **GlobMatcher** — Utilities subsystem; resolves glob patterns to concrete file paths.
- **PathResolver** — Utilities subsystem; resolves the output path against the job's base
  directory, and expands and roots each input pattern of a configuration job for the file-access check.
- **FileAccessPolicy** — Utilities subsystem; judges, reads and writes every path of a configuration job
  (through `context.FileAccess`).
- **YamlDictionaryLoader** — Dictionary subsystem unit; loads approved-term and deprecated-term entries
  from each YAML file, or from the text the file-access policy read.
- **ConflictDetector** — Dictionary subsystem unit; detects term conflicts across loaded entries.
- **MarkdownFormatter** — Dictionary subsystem unit; formats deduplicated entries as a Markdown
  string.

#### Callers

- **Program.RunToolLogic** — calls `DictionaryGenerator.Generate(context)` when `context.Jobs` is
  non-empty. `Generate` is the sole public entry point of the Dictionary subsystem pipeline.
