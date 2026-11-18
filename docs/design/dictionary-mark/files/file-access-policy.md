### FileAccessPolicy

![Files Subsystem Structure](FilesView.svg)

The `FileAccessPolicy` unit is the rule through which the person running DictionaryMark decides which files a
configuration file's tables may read and write. It is adapted from CanvasNet's `FileAccessPolicy` and comprises four
source files in the `DemaConsulting.DictionaryMark.Files` namespace:

- `FileAccessPolicy.cs` - `FileAccessPolicy` (internal abstract partial class): the contract, the factories, and the
  internal `RequiresRealPath`, `PreCheck` and `AnchorsOf` members
- `FileAccessPolicy.BuiltIns.cs` - the private built-in policies (`AllowAllPolicy`, `UnderRootsPolicy`,
  `NoSymlinksPolicy`, `CombinedPolicy`) and the internal link match `MatchesWithoutLinks`
- `FileAccessRequest.cs` - `FileAccessRequest` (internal sealed record): the facts a policy decides about, and the
  internal `PreOpenSnapshot` record
- `FileAccessDecision.cs` - `FileAccessDecision` (internal sealed record): the answer

#### Purpose

Which files a configuration file may reach depends on the run: a developer's own configuration file may read anything
its user can, a build server that runs configuration files from pull requests confines them to the repository and may
refuse links. `FileAccessPolicy` expresses that rule once, as an immutable, thread-safe object that
`FileReferenceResolver` asks after it opened a file and before it reads it (or, for an output, before anything is
written). The built-in policies cover the four modes of `--file-access` and compose.

The unit's behavior is specified by the `DictionaryMark-FileAccessPolicy-*` requirements (`AllowAll`, `UnderRoots`,
`NoSymlinks`, `Combine`, and `Decision`) in `docs/reqstream/dictionary-mark/files/file-access-policy.yaml`.

#### Data Model

**`FileAccessRequest`**: `BaseDirectory` (the host-chosen directory the lookup started from: the configured base
directory, so it is trusted), `RequestedPath` (the full path as written, links not resolved), `RealPath` (the final
path of the open handle, or of the deepest existing folder of an output that does not exist yet), and the internal
`Snapshot` (see below).

**`FileAccessDecision`**: `IsAllowed`, `Reason` and `Hint` (sentence fragments without a trailing period). `Allow` is
one shared instance; `Deny(reason, hint)` requires a non-empty reason and drops an empty hint.

**`FileAccessPolicy`**: the abstract `Name` and `Check(request)`; the internal virtual `RequiresRealPath` (`true`
except for `AllowAll`), `PreCheck(requestedPath)` (`null` except for `UnderRoots`), and `AnchorsOf(requestedPath,
baseDirectory)`. The constructor is `protected`; the tests derive a recording policy from it to verify the resolver's
fail-closed handling of a policy that throws or answers nothing.

**Built-in policies** (all immutable and thread-safe):

| Factory | Name | Rule |
| --- | --- | --- |
| `AllowAll` | `any` | allows every request; needs no real path |
| `UnderRoots(roots)` | `roots` | the written path and the real path must each lie inside a root |
| `NoSymlinks` | `no-symlinks` | the real path must equal the written path below the anchor |
| `Combine(policies)` | parts joined with `+` | every part must allow; the first denial wins |

**`UnderRootsPolicy`** keeps each root as its lexical full path and its real location
(`RealPath.TryGetCanonicalDirectory`, then `RealPath.GetExactPathName` so it is in the letter case stored on disk),
both captured when the policy is created and never looked up again. A root that
does not exist then (or whose real location cannot be determined) has no captured location and allows no file, even
once it exists (fail closed; the denial says so and hints to create the folder first). This closes the root
rename-and-replace race: a root renamed and replaced by a link after creation leads an open elsewhere, and the real
path no longer lies under the captured location.

**Pre-open snapshot.** A built-in policy never resolves a folder by name after a file was opened (the only lookup by
name after the open is the stored letter case of the paths it compares, which changes nothing but letter case and
follows no link). The internal
`AnchorsOf(requestedPath, baseDirectory)` names the trusted folders a policy compares against (`NoSymlinks`: its
anchor; `Combine`: the union of its parts; others: none). `FileReferenceResolver` captures their real locations (once
per resolver, before the first file below them is opened) and the long (8.3-expanded) spelling of the written path
before every open, and passes them as the internal `FileAccessRequest.Snapshot`. A request without a snapshot (built by
other code, with no open in progress) is evaluated by resolving now.

#### Key Methods

- `UnderRoots(...)` - validates the roots (null list or entry: `ArgumentNullException`; no root, an empty, white-space
  or invalid root, or on Windows a root in a device namespace other than an ordinary extended-length drive or volume
  path: `ArgumentException`) and captures their real locations. `PreCheck` denies a written path outside every root,
  in the root's given or captured real spelling (`the path as written is outside the allowed roots`); a written path
  with `~` is also compared in its long spelling (Windows). `Check` repeats the written-path test (with the long
  spelling from the snapshot) and then requires the real path, brought to stored case with
  `RealPath.GetExactPathName`, inside the captured real location of a root (`its real path is outside the allowed
  roots`). The written-path tests use `RealPath.IsUnder` (platform case rule: case-insensitive on Windows and macOS);
  they are only a pre-filter that keeps a file clearly outside every root from being opened. The real-path test, which
  decides, uses `RealPath.IsUnderExactly` (ordinal on every platform, the separator as the boundary), so on a
  case-sensitive folder `C:\work\ALLOWED\x` is outside the root `C:\work\allowed`, while a root given in another
  letter case on a case-insensitive folder still contains its files.
- `NoSymlinks.Check` - the anchor is `RealPath.CommonAncestor(RequestedPath, BaseDirectory)` (the drive root when they
  share none); the anchor and everything above it are trusted. The expected real path is the anchor's real location
  from the snapshot joined with the written path below the anchor; the expected and the real path are both brought to
  stored case with `RealPath.GetExactPathName`, and the file is allowed when they are equal exactly
  (`RealPath.PathsEqualExactly`), so a path written in another letter case on a case-insensitive folder is not
  mistaken for a link. When they differ and the snapshot's long spelling differs from the written path, the
  comparison is repeated with the same number of trailing components of the long spelling; this only rewrites the
  expected text, so a link still shows up as a different real path. An anchor whose real location is unknown, or was
  not captured, is a denial. The comparison itself is the internal static `MatchesWithoutLinks`, which resolves no
  folder by name (it only looks up letter case) and answers `true` (no link below the anchor), `false` (a link), or
  `null` (the anchor's real location is unknown).
- `Combine(...)` - validates the list, flattens nested combinations, returns a single policy unchanged.
  `RequiresRealPath` is true when any part needs it; `PreCheck` returns the first part's denial; `Check` returns the
  first denial and treats a part that returns `null` as a failure (`InvalidOperationException`), which the resolver
  turns into a denial.

#### Error Handling

Factories throw `ArgumentNullException` / `ArgumentException` for invalid arguments. `Check` never throws for a valid
request of the built-ins, except when the file system fails while the letter case is looked up (`IOException`,
`UnauthorizedAccessException`); a request that is `null` is an `ArgumentNullException`. An exception or a `null`
decision of a policy is handled by the caller (`FileReferenceResolver` denies the file). A root whose stored-case
spelling cannot be looked up when the policy is created has no captured location and allows no file (fail closed).

**Letter case**: a path as written is compared case-insensitively on Windows and macOS only as a pre-filter; the
real path decides and is compared exactly in the letter case stored on disk, so no case sensitivity has to be
detected. The server and share names of a UNC path are compared as spelled. **Known limit (roots fixed at
creation)**: a root that does not exist when the policy is created (at startup, before any job runs) allows nothing
for the whole run, even if a job creates it. **Known limit (hard links)**: a hard link cannot be told apart from the
file itself and is not denied by `NoSymlinks`.

#### Dependencies

- **RealPath** (same subsystem) - `IsUnder`, `IsUnderExactly`, `PathsEqualExactly`, `GetExactPathName`,
  `CommonAncestor`, `TryGetCanonicalDirectory`, and `ExpandShortNames`.
- **.NET base class library** - `System.IO.Path` and LINQ.

#### Callers

`FileReferenceResolver` asks the policy for every opened file and every output. `Context` (Cli subsystem) creates the
run's policy through the factories from `--file-access` and `--allow-root`.
