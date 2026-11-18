### DictionaryGenerator

![Dictionary Subsystem Structure](DictionaryView.svg)

#### Purpose

`DictionaryGenerator` is a static class that orchestrates the full dictionary generation pipeline.
The `Generate` static method runs every job of the context. For each job, `GenerateJob` resolves
input file paths (using `GlobMatcher` for wildcard patterns and every command-line pattern), loads entries from each
file via `YamlDictionaryLoader`,
detects conflicts with `ConflictDetector`, formats output with `MarkdownFormatter`, and writes the
result to the output file or stdout. A configuration-table job is judged by the run's file-access
policy: its input files are read, and its output path is checked, through one `FileReferenceResolver`
per job.

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

Processing steps: (0) For a configuration-table job (`job.Index` is set), create one
`FileReferenceResolver` with `BaseDirectory` = the configuration file's folder (never `--root` or the
configuration `root` key, so a configuration file cannot move its own trusted origin) and `FileAccess` =
`context.FileAccess`; the command-line job gets none and is never judged. (0a) For a configuration-table job, check
each input pattern before anything is listed (`CheckPatterns`). (1) Resolve input file paths — call
`ResolveInputs(job)`;
return early on `ArgumentException`, `InvalidOperationException`, `IOException`, `UnauthorizedAccessException` or empty
result. (2) Load entries — with a resolver, read every
literal input and every matched file with `resolver.Resolve(file)` (every file is judged; nothing is skipped) and call
`YamlDictionaryLoader.Load(file, content)` (or `LoadDeprecated(file, content)`) on the bytes it returned; without a
resolver, call `YamlDictionaryLoader.Load(file)` (or `LoadDeprecated(file)`). Return early at the first failure:
`FileReferenceException`, a literal input or match that is not found, `IOException`, `UnauthorizedAccessException`,
`YamlException` or `InvalidOperationException`. (3) Detect
conflicts — call
`ConflictDetector.Detect(approved, deprecated)` for this job's files; if any conflicts found, report each and return.
(4) Format output — call `MarkdownFormatter.Format` or `FormatDeprecated` with `job.Options` (already merged by
`TableJob`, including the `Not used` deprecated term header and the column widths) using only entries from the primary
input files. (5) Write output — if `job.OutputFile` is set, resolve it with
`PathResolver.ResolvePath(job.OutputFile, job.BaseDirectory, "output path")`, check it with
`resolver.CheckOutputPath(fullPath)` when the job has a resolver (before anything is created), create missing parent
directories with
`Directory.CreateDirectory`, and write via `File.WriteAllText`; otherwise write via `context.WriteLine`. The base
directory may be a root directory, the configuration file's directory, or the working directory; the generator does
not distinguish them.

**ResolveInputs** *(private)*: Resolves a job's input patterns to the sorted (ordinal, case-insensitive),
deduplicated list of full file paths to read. The command-line job is trusted, so every pattern goes to
`GlobMatcher.Resolve(job.InputPatterns, job.BaseDirectory)`, which requires each literal file to exist. The patterns
of a configuration-table job are untrusted content: each is expanded and rooted with
`PathResolver.Combine(pattern, job.BaseDirectory, "glossary path")`; one without `*` or `?` is a literal input, which
is only normalized with `Path.GetFullPath` (text arithmetic) and never looked up, because the resolver applies the
policy before it looks for the file and reports a missing file as not found; the others are real wildcard patterns,
which have already passed `CheckPatterns` and go to `GlobMatcher.Resolve`. Listing the folders of a wildcard pattern is
the one access made for configuration content outside the resolver.

**CheckPatterns** *(private)*: For a configuration-table job, roots each input pattern with
`PathResolver.Combine(pattern, job.BaseDirectory, "glossary path")` (the path that would be listed) and passes it, and
its `Path.GetFullPath` form with `.` and `..` collapsed, to `FileReferenceResolver.CheckPatternText`. A
`FileReferenceException` fails the job with `DescribeRefusal(ex, "Failed to resolve input pattern '{pattern}'")`
before any folder is listed, so a network, device or URL-like pattern never contacts a host. An undefined `${VAR}` or a
malformed path is left to `ResolveInputs`, which reports it as before. Command-line patterns are not checked.

**DescribeRefusal** *(private)*: Turns a `FileReferenceException` into the error text. A policy denial or a policy
that cannot be applied (`AccessDenied` or `RealPathUnavailable`) reads
`file-access-denied: {message} Allow the file in the file-access policy, or reference a file it allows.` (CanvasNet's
mapping and hint); any other refusal reads `Failed to read file '{file}': {message}` or
`Failed to write output file '{file}': {message}`.

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
  `PathTooLongException`) from `ResolveInputs` (`PathResolver.Combine`, `Path.GetFullPath` or
  `GlobMatcher.Resolve`). An `InvalidOperationException` reports
  `"Error: {message}"` (pattern matched no files, a command-line file or directory not found, undefined `${VAR}`).
  A path-validation exception reports `"Error: Invalid input pattern: {message}"` (for example a
  glob rejected by the globbing library, or a null character in a command-line pattern).
- **Refused input pattern** — `FileReferenceException` from `CheckPatterns` (a configuration-table pattern that breaks
  a never-list rule, including a control character); reports
  `"Error: Failed to resolve input pattern '{pattern}': {message}"` (control characters shown as `?`) before any
  folder is listed.
- **I/O or access error resolving input** — `IOException` or `UnauthorizedAccessException` from
  `GlobMatcher.Resolve` (directory enumeration); reports
  `"Error: Failed to resolve input patterns: {message}"`.
- **No files found** — no jobs, or an empty list from `GlobMatcher.Resolve`; reports
  `"Error: No input files found matching the specified patterns."`.
- **File-access denial** — `FileReferenceException` of kind `AccessDenied` or `RealPathUnavailable` from
  `Resolve` or `CheckOutputPath`; reports `"Error: file-access-denied: {message} Allow the file in the file-access
  policy, or reference a file it allows."`. Nothing is read, and for an output nothing is created.
- **Other refusal of a judged path** — any other `FileReferenceException` (a never-list path, a file that is not a
  regular file, a file larger than 16 MiB, a file that cannot be opened); reports
  `"Error: Failed to read file '{file}': {message}"` or `"Error: Failed to write output file '{file}': {message}"`.
- **Input not found** — `Resolve` returned no file for a configuration-table input: a literal input that does not
  exist (in a folder the policy allows), or a glob match that vanished after the glob ran; reports
  `"Error: Input file '{file}' was not found."` with the full path, the same message the command-line job gets for a
  missing literal file.
- **I/O or access error reading YAML** — `IOException`, `UnauthorizedAccessException` or a
  path-validation exception from `YamlDictionaryLoader.Load`; reports
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

For configuration jobs, every message above is prefixed with the job label, for example
`"Error: table job 2 ('docs/deprecated.md'): Input pattern 'x/*.yaml' matched no files."`.

#### Dependencies

- **Context** — CLI subsystem; provides the jobs, output channel, and exit code.
- **TableJob** — CLI subsystem; one job's inputs, output, base directory, label and options.
- **GlobMatcher** — Utilities subsystem; resolves the command-line job's patterns and the wildcard patterns of a
  configuration-table job to concrete file paths.
- **FileReferenceResolver**, **FileReferenceOptions**, **FileReference**, **FileReferenceException** — Files
  subsystem; read and judge the input files of a configuration-table job and check its output path.
- **PathResolver** — Utilities subsystem; expands and roots the input patterns of a configuration-table job and
  resolves the output path against the job's base directory.
- **YamlDictionaryLoader** — Dictionary subsystem unit; loads approved-term and deprecated-term entries
  from each YAML file.
- **ConflictDetector** — Dictionary subsystem unit; detects term conflicts across loaded entries.
- **MarkdownFormatter** — Dictionary subsystem unit; formats deduplicated entries as a Markdown
  string.

#### Callers

- **Program.RunToolLogic** — calls `DictionaryGenerator.Generate(context)` when `context.Jobs` is
  non-empty. `Generate` is the sole public entry point of the Dictionary subsystem pipeline.
