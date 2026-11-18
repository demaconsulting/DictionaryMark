### FileReferenceResolver

![Files Subsystem Structure](FilesView.svg)

The `FileReferenceResolver` unit is the engine that reads the files a configuration file names and checks the outputs
it names. It is adapted from CanvasNet's `FileReferenceResolver` and comprises seven source files in the
`DemaConsulting.DictionaryMark.Files` namespace:

- `FileReferenceResolver.cs` - `FileReferenceResolver` (internal sealed partial class): the entry point, the
  per-candidate pipeline (pre-check, cache, open, inspect, policy, read), and the bounded read
- `FileReferenceResolver.Paths.cs` - the text rules that hold under every policy, the candidate, and the configured
  base directory
- `FileReferenceResolver.Output.cs` - the output-path check (`CheckOutputPath`), added by DictionaryMark
- `FileReferenceResolver.Patterns.cs` - the input-pattern check (`CheckPatternText`), added by DictionaryMark
- `FileReferenceOptions.cs` - `FileReferenceOptions` (internal sealed record): base directory, file-access policy and
  byte limit
- `FileReference.cs` - `FileReference` (internal sealed class): the result
- `FileReferenceException.cs` - `FileReferenceException` (public sealed class): the failure
- `FileReferenceFailureKind.cs` - `FileReferenceFailureKind` (public enum): the reasons for a failure

#### Purpose

`FileReferenceResolver` turns a path that a configuration file names into the bytes of a regular file after a
host-chosen `FileAccessPolicy` approved the file that was actually opened, and checks an output path the
configuration file names against the same rules before anything is written. It follows paths the way Pandoc does -
relative paths, dot-dot segments, absolute paths, and symbolic links - and leaves every restriction to the policy,
except for the paths that are never safe (network, device and alternate-data-stream paths, control characters, and
files that are not regular files; on Windows also reserved device names in any path component and a link from a local
path to a network share) and the byte limit, which hold under every policy. The decision about a read is made about
the open handle, so it cannot be raced by another process that changes the directory tree between a check and the
open.

The unit's behavior is specified by the `DictionaryMark-FileReferenceResolver-*` requirements (`Candidates`,
`PathText`, `MissingFiles`, `PolicyEvaluation`, `NetworkTarget`, `RaceFree`, `RealPathUnavailable`, `RegularFile`,
`Reading`, `Limits`, `Cache`, `Failures`, and `OutputPath`) in
`docs/reqstream/dictionary-mark/files/file-reference-resolver.yaml`.

#### Data Model

**`FileReferenceOptions`** (immutable record): `BaseDirectory` (where a relative path starts and the trusted origin
of every request; a starting point, not a boundary), `FileAccess` (the policy; default `FileAccessPolicy.AllowAll`),
and `MaxFileBytes` (default `DefaultMaxFileBytes`, 16 MiB; positive).

**`FileReference`** (immutable): `Key` (the full path as written, `.` and `..` collapsed, links not resolved),
`RealPath` (the real path of the open handle, or `null` where the platform cannot tell), and `Content`
(`ReadOnlyMemory<byte>`).

**`FileReferenceException`** (immutable): `Kind`, `Reference` (as written, control characters replaced by `?`),
`Reason` (the broken text rule or the policy's reason), `RequestedPath`, `RealPath`, `PolicyName`, `Hint`, and the
cause as the inner exception. `Message` is computed from the kind, the reference and the policy name only; it never
contains a host directory nor a policy reason, which may name host paths. The exception and its kind are public
because the code analysis requires exceptions to be public; every other type of the subsystem is internal.

**`FileReferenceFailureKind`**: `EmptyPath`, `ControlCharacter`, `InvalidPath`, `NetworkPath` (the path itself;
`NetworkPath` also for a Windows link from a local path to a share), `AccessDenied`, `RealPathUnavailable`,
`NotARegularFile`, `CannotRead`, `TooLarge`, and `InvalidBaseDirectory`.

**`FileReferenceResolver`** is created for one operation (one table job) with the options and (internally) the
`FileOpenHooks` test seams. It holds the outcome of every key that was looked up (a file, a remembered failure, or a
miss) and the real location of every trusted folder a policy compares against, captured on first use. **It is not
thread-safe**: one instance per operation. A candidate (private record `Candidate`) carries its `Key` and its `Origin`
(the base directory).

**Invariants**: no byte of a file is read before the policy (when it needs the real path) allowed the open handle; the
bytes come from the handle the policy was asked about; a key is opened at most once per instance except after a
policy denial (a denial is not cached, so the policy is asked again); no more than `MaxFileBytes` bytes are read of any
file; `CheckOutputPath` creates, writes and reads nothing and caches nothing.

#### Key Methods

- `Resolve(reference)` - validates the text (`ValidateText`), returns `null` when no base directory is configured,
  and loads the candidate, returning the file or `null` when it does not exist.
- `DescribeInvalidText` - backslashes become forward slashes; refuses an empty text, a control character, a device
  path (`//?/`, `//./`, and on Windows any device namespace), a network path (a leading `//`), and a colon anywhere
  except the drive of an absolute Windows path (`C:/`).
- `Candidates` - an absolute path (`Path.IsPathRooted`) is one candidate, resolved with `Path.GetFullPath(text, base)`
  so a rooted path without a drive takes the drive of the base directory; a relative path is resolved against the base
  directory. `FullCandidate` refuses on Windows a full path in a device namespace (`\\.\`, `\\?\`, `\??\`) or with a
  reserved device name in any component, with `InvalidPath` before the open. The only exemption is an ordinary
  extended-length path (`\\?\X:\` or `\\?\Volume{GUID}\`) under an ordinary extended-length base directory with the
  same root.
- `LoadFile` - the per-candidate pipeline: (1) `policy.PreCheck(key)` may deny the path as written before anything is
  opened; (2) a cached outcome answers, after the policy is asked again for a cached file; (3) `OpenCheckAndRead`.
- `OpenCheckAndRead` - captures the real location of the trusted folders the policy names (`CaptureBeforeOpen`),
  then opens the key through the hooks or `RealPath.OpenForRead`. "Not found" is a miss; an access failure on a
  directory is `NotARegularFile`; any other open failure is `CannotRead`. The handle's facts come from
  `RealPath.Inspect` (or the `RealPathOf` and `IsRegularFileOf` hooks). On Windows, a real path that is a network
  location while the key is not is `NetworkPath` under every policy, before the regular-file check and the policy; a
  handle that is not known to be a regular file is `NotARegularFile` under every policy (fail closed); when the policy
  needs the real path and it is unknown, `RealPathUnavailable` (fail closed); otherwise `CheckPolicy` runs and the
  file is read from the same handle (`Read`).
- `CheckPolicy` - builds the `FileAccessRequest` (base directory = the candidate's origin, requested path, real path,
  pre-open snapshot) and asks the policy. An exception other than `OperationCanceledException`, or a `null` decision,
  is a denial with the reason `the file-access policy failed`; a denial throws `AccessDenied` with the policy's name,
  reason and hint and both paths.
- `ReadBounded` (internal static) - checks a reported length first, then reads in 8 KiB chunks and stops as soon as
  the running length exceeds the limit.
- `CheckOutputPath(path)` (DictionaryMark addition) - applies the read checks in the read order without reading:
  (1) the text rules; (2) the candidate (full path and Windows device names); (3) `policy.PreCheck`; (4) the pre-open
  snapshot; (5) the open: "not found" means the output does not exist yet **only when no directory entry exists at
  the path** (a link, including a dangling link, is an existing entry, detected with `FileInfo.LinkTarget`); an
  access failure on a directory is `NotARegularFile`; any other failure to open an existing entry is `CannotRead`;
  (6) for an existing entry, the handle's real path, the network-target rule and the regular-file rule, as for a read;
  for an output that does not exist yet, the real path is the real location (`RealPath.TryGetCanonicalDirectory`, or
  the `DirectoryLocationOf` hook) of its deepest existing folder joined with the rest of the path
  (`RealPathOfMissing`), followed by the network-target rule; (7) `RealPathUnavailable` when the policy needs a real
  path that is unknown; (8) `CheckPolicy`. A failure is turned into its exception exactly as for a read (`Deliver`).
  Without a base directory it throws `InvalidOperationException`.
- `CheckPatternText(pattern)` (internal static, DictionaryMark addition) - runs the text rules of a read
  (`ValidateText`) on an input pattern exactly as it would be listed, without touching the file system; wildcards are
  ordinary characters to these rules. A broken rule throws the `FileReferenceException` a read of that path gets (for
  example `NetworkPath` for `//host/share/*.yaml`, `InvalidPath` for a colon). It adds no rule.

**Preconditions and postconditions**: the path is untrusted text; on return from `Resolve` the file is a regular file
that the policy allowed (or the policy needs no real path) and its bytes are within the limit, or `null` was returned
because no file exists, or an exception was thrown and nothing was read. On return from `CheckOutputPath` the output
passed every check; nothing on disk was changed.

#### Error Handling

Every refusal and read failure is a `FileReferenceException`; a missing input file is `null`. Text failures are thrown
before any access. `NotARegularFile`, `RealPathUnavailable`, `CannotRead`, `TooLarge`, and a `NetworkPath` found after
the open are remembered per key for reads; a denial is not, so the policy decides again on the next resolve. A
cancellation raised by the policy passes through unwrapped. Invalid options are `ArgumentNullException` /
`ArgumentOutOfRangeException`; an invalid base directory is `InvalidBaseDirectory` on first use.

#### Design Decisions

- **The configuration file's folder, never a folder the configuration file names, is the trusted origin.** The
  `no-symlinks` policy trusts the folders above its anchor, which is computed from the base directory; if the
  configuration file's `root` key could become the base directory, the file could move its own boundary.
- **A link to a share is refused under every policy.** A configuration file reaching the network through a local path
  is never expected; the open itself follows the link and may already contact the share before the check refuses the
  file. A key that is itself a network location (a UNC base directory or a mapped network drive) is exempt.
- **An input pattern gets the text rules before it is listed.** Listing a pattern such as `//host/share/*.yaml`
  contacts the host before any match reaches `Resolve`, which on Windows can expose the user's credentials; running
  the read's own text rules on the pattern first refuses it with no connection. No other check applies to a pattern:
  each match is still judged by `Resolve`.
- **An output that does not exist yet is judged by its deepest existing folder.** There is no open file to inspect,
  and the folders below the deepest existing one are created by the write, so the real location of that folder
  decides where the write lands.

**Known limits**: where Windows cannot tell the real path, the default policy reads the file without the network
check (a restrictive policy fails closed with `RealPathUnavailable`); a SUBST or DFS path whose drive is local but
whose target is remote is refused (fail closed); paths are compared case-insensitively on Windows and macOS (see
*FileAccessPolicy Design*); a hard link cannot be told apart from the file itself; a file deleted after it is opened
has no real path on Linux, so a restrictive policy refuses it; the output check is not free of races, because the
write goes by name (a link swapped in between the check and the write is not caught), and a missing parent folder that
is a dangling link is judged by the folder above it; an existing output that cannot be opened for reading is refused
(`CannotRead`); and an output that does not exist yet, spelled with short (8.3) names below the anchor, is refused
under `no-symlinks` (fail closed).

#### Dependencies

- **FileAccessPolicy** (same subsystem) - the policy that decides, including its lexical pre-check and the folders it
  compares against.
- **RealPath** (same subsystem) - the open, the handle facts, the real location of a folder, and the platform path
  comparer.
- **.NET base class library** - `System.IO` (`Path`, `FileStream`, `Directory`, `FileInfo`) and
  `System.Collections.Generic`.

#### Callers

`DictionaryGenerator` (Dictionary subsystem) creates one resolver per configuration-file table job, with the
configuration file's folder as the base directory and the run's policy from `Context`, reads every input file the
job's patterns match with `Resolve`, and checks the job's output with `CheckOutputPath`; before listing a job's
patterns it checks each with `CheckPatternText`. The tests in
`test/DemaConsulting.DictionaryMark.Tests/Files/` call it directly and reach the internal hooks constructor through
the assembly's `InternalsVisibleTo` grant to the test project.
