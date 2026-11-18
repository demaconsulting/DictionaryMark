### GlobMatcher

![Utilities Subsystem Structure](UtilitiesView.svg)

#### Purpose

`GlobMatcher.GetFiles` resolves a list of glob patterns to a sorted, deduplicated list of
absolute file paths. It handles three path categories: absolute paths without wildcards (checked
via `File.Exists`), absolute paths with wildcards (base directory extracted before the first
wildcard character), and relative patterns (matched against `Environment.CurrentDirectory`).
`GlobMatcher` is a static class. For configuration-file patterns it also provides `SplitGlob` and
`MatchUnder`, which the file-access policy uses so that every folder is judged before it is listed.

#### Data Model

- **GlobMatch** (`readonly record struct`): `FullPath` (`string`) and `RelativePath` (`string`, relative to the
  folder matched below, with `/` separators) of a file matched by `MatchUnder`.
- **GuardedDirectory** (private nested class, derives from the library's `DirectoryInfoBase`): a folder offered
  to the globbing library. It holds the `DirectoryInfo`, the folder check and its parent (none for the folder
  matched below). It has no existence probe.

#### Key Methods

**GetFiles**: Returns a sorted, deduplicated list of absolute file paths matching the supplied
patterns.

- *Parameters*: `IEnumerable<string> patterns` — collection of file path patterns (absolute or
  relative, with or without wildcards).
- *Returns*: `IReadOnlyList<string>` — sorted, deduplicated list of absolute file paths; empty
  list when no patterns match.
- *Preconditions*: `patterns` is non-null; each individual pattern is non-null and non-empty.
- *Postconditions*: Returned list is sorted with `StringComparer.OrdinalIgnoreCase`; each path
  is absolute; no duplicates (case-insensitive).

For each pattern: (1) reject null or empty with `ArgumentException`; (2) absolute path without
wildcards — check `File.Exists`, add if present; (3) absolute path with wildcards — locate the
non-wildcard base (segment before first `*` or `?`), create `Matcher` with remaining relative
pattern, execute against that base; (4) relative pattern — create `Matcher`, execute against
`Environment.CurrentDirectory`. Results accumulated in a `HashSet<string>` (case-insensitive)
then sorted.

**Resolve**: Strict variant of `GetFiles` used by the generator. Two overloads exist:
`Resolve(patterns)` delegates to `Resolve(patterns, Environment.CurrentDirectory)`.

- *Parameters*: `IEnumerable<string> patterns`; `string baseDirectory` — the directory relative
  patterns resolve against (the working directory, the configuration file's directory, or a root
  directory).
- *Returns*: `IReadOnlyList<string>` — sorted, deduplicated absolute paths.
- *Postconditions*: Each pattern is first passed to `PathResolver.Combine(pattern, baseDirectory,
  "glossary path")`, which expands `${VAR}` references and roots a relative pattern at the base
  directory without normalizing it. A rooted pattern with a wildcard is then matched as an absolute
  glob by `GetFiles`: the literal part before the first wildcard becomes the matcher's base
  directory (normalized with `Path.GetFullPath`), so `..` segments anywhere before the first
  wildcard work, whether they are leading (`../shared/*.yaml`) or follow a normal segment
  (`sub/../../shared/*.yaml`). Rooting first also avoids the matcher enumerating parent
  directories for a leading `..`. A `..` after a wildcard is not supported; the globbing library
  rejects it with `ArgumentException`. A literal pattern is resolved with `Path.GetFullPath`. A
  pattern that matches nothing, a literal file that does not exist, a missing directory, or an
  undefined variable throws `InvalidOperationException` with a message naming the pattern or
  path. Windows UNC paths are supported as literals and as glob bases.

**SplitGlob(pattern)**: A pure text operation, also used by `GetFiles` for an absolute glob. Backslashes become
forward slashes. The literal folder is the text before the last separator before the first `*` or `?`, and the
remainder is the rest. A bare drive (`C:`) becomes its root (`C:/`), a wildcard right after a leading slash
gives `/`, and a relative pattern whose first component holds a wildcard gives an empty folder. The folder is
not normalized, so `..` segments are kept.

**MatchUnder(folder, pattern, checkFolder)**: Matches a remainder below a folder that the caller has
already judged, for configuration-file patterns.

- *Parameters*: `string folder` — the full, normalized folder; `string pattern` — the wildcard remainder;
  `Func<string, bool> checkFolder` — the check called with the full path of every folder below `folder`, a
  plain folder or a symbolic link or junction, before it is listed.
- *Returns*: `IReadOnlyList<GlobMatch>`.
- *Behavior*: a `Matcher` runs against a `GuardedDirectory`. Listing a folder calls
  `DirectoryInfo.GetFileSystemInfos` directly, reading all its entries at once inside the handler for a missing
  folder, so a folder that is missing when it is listed (including one removed after it was checked) gives no
  entries whatever the framework's enumeration timing. There is no `Exists` probe like that of the library's
  `DirectoryInfoWrapper`. When the matcher asks to list any folder other than the one matched below, the check
  is called first with its full path: true lists it, false lists nothing in it (for example one that is
  missing or not a folder), and an exception stops the matching and propagates. The folder matched below is
  judged by the caller and never passed to the check, and a folder the matcher never descends into is never
  checked or listed. The matcher itself does not tell links from plain folders. A request for a parent folder
  (`..`) throws `ArgumentException`, so the matching cannot climb above the judged folder.

#### Error Handling

- `InvalidOperationException` — thrown by `Resolve` as described above; `DictionaryGenerator`
  reports it as `Error: {message}`.
- `ArgumentNullException` — thrown when `patterns` is null.
- `ArgumentException` — thrown when any individual pattern is null or empty; caller must validate
  pattern values before passing them. `Path.GetFullPath` also throws it (or `PathTooLongException`)
  for a malformed or overlong literal pattern, for example one containing a null character.
- Non-matching patterns return no error — empty result is returned silently; `DictionaryGenerator`
  is responsible for detecting and reporting the "no files found" condition.
- File-system errors from `Matcher.Execute` propagate as-is. `DictionaryGenerator` catches
  path-validation exceptions (`PathResolver.IsInvalidPathException`), `IOException` and
  `UnauthorizedAccessException` around `GlobMatcher.Resolve` and reports them as a failure of that
  job only.

#### Dependencies

- **Microsoft.Extensions.FileSystemGlobbing** — OTS package; `Matcher` and `DirectoryInfoWrapper`
  APIs used to evaluate glob patterns against the file system.
- **PathResolver** — Utilities unit; expands `${VAR}` references and roots patterns at the base
  directory.

#### Callers

- **DictionaryGenerator.GenerateJob** — calls `GlobMatcher.Resolve(job.InputPatterns,
  job.BaseDirectory)` as the first step of a command-line job to resolve user-supplied glob patterns
  to a sorted, deduplicated list of concrete file paths.
- **FileAccessPolicy.ResolvePattern** — calls `SplitGlob` and `MatchUnder` for configuration-file
  patterns, judging every folder in the callback.
