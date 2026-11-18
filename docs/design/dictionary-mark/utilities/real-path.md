### RealPath

![Utilities Subsystem Structure](UtilitiesView.svg)

#### Purpose

`RealPath` holds all platform-specific (native) code of the tool. It opens files and reports, per operating
system, whether an open handle is a regular file and what its real (final) path is. It also provides the
path-comparison and path-classification rules that the file-access policy relies on.

A decision about a file is only free of races when it is made about the file that will be read or written.
The real path must therefore come from the open handle, not from the name, which another process can change
between a check and the open. .NET offers no API for this, because its link APIs work by name. The approach
and the native contracts follow the DEMA Consulting CanvasNet tool, which proves them on the same platforms.

#### Data Model

- **HandleFacts** (`readonly record struct`): `IsRegularFile` (`bool?`) and `FinalPath` (`string?`). Each is
  null when the platform cannot tell.
- **PathState** (enumeration): `Missing` (the open found no entry, or a folder of its path is missing),
  `Exists` (the entry was opened) and `Unknown` (the open failed for another reason).
- **PathFacts** (`readonly record struct`): `State` (`PathState`), `IsDirectory` (`bool?`, null when the entry
  is missing or the platform cannot tell; always null on macOS) and `FinalPath` (`string?`).
- `PathComparison` (`StringComparison`): `OrdinalIgnoreCase` on Windows and macOS, `Ordinal` elsewhere.
- A cached flag records whether the Linux C library lacks `fstat` (glibc before 2.33), so that `__fxstat` is
  used instead. This is the only state, and a race on it only repeats the probe.

#### Key Methods

**Inspect(SafeFileHandle)**: Never throws. Unknown facts are null, and the caller fails closed. A native library
or entry point that is missing, or a native call that fails, leaves both facts unknown; it never reports a handle
as not a regular file. FileAccessPolicy then denies the file with its type-unknown reason.

- *Windows*: a regular file is a disk file (`GetFileType` is `FILE_TYPE_DISK`) without the folder
  attribute; a character device, a pipe or a folder is not a regular file. An invalid or closed handle, or a
  `GetFileType` result of `FILE_TYPE_UNKNOWN` (also its failure result), leaves both facts unknown. Its real
  path comes from `GetFinalPathNameByHandleW` with `FILE_NAME_NORMALIZED |
  VOLUME_NAME_DOS`. The buffer starts at 512 characters and grows to at most 32768. The `\\?\` prefix is
  removed, and `\\?\UNC\` becomes `\\`. Any other spelling, such as a volume GUID, is reported as unknown.
- *Linux*: the real path is the target of `/proc/self/fd/{descriptor}`. The type and link count come from
  `fstat`, read at known offsets: on x64, `st_nlink` is 64 bits at offset 16 and `st_mode` is at 24; on Arm64,
  `st_mode` is at 16 and `st_nlink` is 32 bits at 20. A link count of zero (a deleted file) gives an unknown
  path. Other architectures give unknown facts.
- *macOS*: one `proc_pidfdinfo` call with the vnode-path flavor fills a 1200-byte structure. The mode is a
  16-bit value at offset 28, and the path is a NUL-terminated UTF-8 string at offset 176. When the call
  fails, the type comes from `fstat` (Arm64 layout). The path query of `fcntl` is not used, because `fcntl`
  is variadic and Arm64 macOS passes variadic arguments on the stack.

Only `IOException`, `UnauthorizedAccessException`, `ExternalException`, `DllNotFoundException`,
`EntryPointNotFoundException` and `ObjectDisposedException` are caught.

**OpenForRead(fullPath)**: On Linux and macOS it calls native `open` with
`O_RDONLY | O_NONBLOCK | O_NOCTTY | O_CLOEXEC`. A named pipe without a writer therefore returns at once
instead of hanging, and the descriptor is not inherited by child processes. On Windows it calls `CreateFileW`
with `GENERIC_READ`, read, write and delete sharing, `OPEN_EXISTING` and `FILE_FLAG_BACKUP_SEMANTICS`, so a
folder opens too and the caller learns it is a folder from the handle (`File.OpenHandle` refuses a folder
before any handle exists). A long path gets the extended-length prefix. `errno` and the Windows error are
translated into `FileNotFoundException` (`ENOENT`, error 2), `DirectoryNotFoundException` (`ENOTDIR`, error
3), `UnauthorizedAccessException` (`EACCES`, `EPERM`, error 5), `PathTooLongException` (error 206) or
`IOException`.

**TryOpenExistingForWrite(fullPath)**: Opens an existing file for writing without truncating or creating it.
On Linux and macOS it uses `O_WRONLY | O_NONBLOCK | O_NOCTTY | O_CLOEXEC`. A named pipe without a reader
(`ENXIO`) and a folder (`EISDIR`, 21 on both systems) return null, which the caller treats as not a regular
file. On Windows it uses `File.OpenHandle` with `FileMode.Open` and read sharing.

**CreateNewForWrite(fullPath)**: Calls `File.OpenHandle` with `FileMode.CreateNew` (`CREATE_NEW` on Windows,
`O_CREAT | O_EXCL` on Unix). This never follows a link in the last component, so it fails when any entry,
including a dangling link, has the name.

**InspectPath(fullPath)**: Reports whether a path exists, whether it is a folder and its real path, from a
handle (Windows, Linux) or `realpath(3)` (macOS), never from a by-name probe. The open follows every link on the
path, as any later open would, so a link swapped in before the open is seen by its real path.

- *Windows*: `CreateFileW` with no access rights, all sharing, `OPEN_EXISTING` and
  `FILE_FLAG_BACKUP_SEMANTICS` (a folder opens too); a path of 248 characters or more gets the `\\?\`
  (`\\?\UNC\`) prefix, as .NET does internally. The folder attribute comes from `File.GetAttributes(handle)`
  and the real path from the final path of the handle. Error 2 (`ERROR_FILE_NOT_FOUND`) and 3
  (`ERROR_PATH_NOT_FOUND`) give `Missing`.
- *Linux*: `open` with `O_PATH | O_CLOEXEC` (`0x200000 | 0x80000` on x64 and Arm64), which needs no read
  permission and opens a folder too; `fstat` on the descriptor (Linux 3.6 and later) gives the type
  (`S_IFDIR`, `0x4000`) and `/proc/self/fd` the real path. `ENOENT` and `ENOTDIR` give `Missing`.
- *macOS*: `realpath(3)` with a 1024-byte buffer owned by the caller; `errno` `ENOENT` or `ENOTDIR` gives
  `Missing`. Whether the entry is a folder is not told (the read handle reports its type, and the write open
  reports a folder with `EISDIR`).

A missing entry includes a link whose target is missing. Any other failure gives `Unknown`. Never throws.

**InspectFolder(fullPath)**: Reports whether a path is an existing folder and its real path from one open of
the folder, never from a by-name probe. The caller decides existence from these facts alone: the state is
`Exists` and `IsDirectory` is not false.

- *Windows and Linux*: `InspectPath`, whose handle already tells a folder.
- *macOS*: `open` with `O_RDONLY | O_DIRECTORY | O_NONBLOCK | O_NOCTTY | O_CLOEXEC` (`O_DIRECTORY` is
  `0x00100000` in the xnu `sys/fcntl.h`). Success gives `Exists`, a folder, and the real path of the descriptor
  from `proc_pidfdinfo`. `ENOENT` or `ENOTDIR` gives `Missing`, which includes a file, a named pipe or a device
  (refused with `ENOTDIR` before anything is opened, so nothing blocks), a link whose target is missing and a
  missing parent folder. Any other failure gives `Unknown`; `O_RDONLY` needs read permission on the folder, so a
  folder that may be searched but not read is `Unknown`.

Rationale: on macOS, `realpath(3)` does not report `ENOTDIR` for a file even with a trailing slash (Apple Libc
has no folder check at the end of its walk), so it cannot tell a folder. `O_SEARCH` would avoid the read
permission, but the oldest macOS release that honors it is unverified. Never throws.

**TryGetCanonicalDirectory(fullPath)**: The final path of `InspectPath`, used for the allowed folders.
Returns null when the entry does not exist or cannot be resolved. Never throws.

**ExpandShortNames(fullPath)**: On Windows, for a path that contains `~`, `GetLongPathNameW` expands short
(8.3) names. Every other path is returned unchanged.

**PathsEqual, IsUnder, CommonAncestor**: Comparisons under `PathComparison`, ignoring trailing separators.
macOS paths are compared in composed Unicode form. `IsUnder` uses a separator boundary, so `/a/bc` is not
under `/a/b`. `CommonAncestor` compares whole components and returns null for paths on different Windows
drives.

**IsWindowsDeviceName, HasWindowsDeviceName, IsWindowsDeviceNamespace**: Pure text checks.

- The reserved names are `CON`, `PRN`, `AUX`, `NUL`, `COM0`-`COM9`, `LPT0`-`LPT9`, the superscript digits,
  `CONIN$` and `CONOUT$`, in any letter case.
- What follows a name (an extension, trailing spaces, or a stream suffix) is ignored.
- The device namespaces are `\\.\`, `\\?\` and `\??\`, with either slash.

**IsNetworkLocation(fullPath)**: On Windows, true for `\\server\share`, `\\?\UNC\...` and a mapped network drive
(`DriveType.Network`). Always false elsewhere. The file-access policy asks it about both the path as written and the
real path: a path that is itself a network location is accessed, and only a local path whose real path is a network
location (one that leads there through a link) is denied, as in CanvasNet.

**IsOrdinaryExtendedPath(path)**: A pure text check with the same rule as CanvasNet. True for an
extended-length path whose root is a drive (`\\?\C:`, alone or followed by `\`) or a volume GUID
(`\\?\Volume{GUID}`, alone or followed by `\`). Such a path names a file on a local volume with no device
names mapped, so the policy trusts it like a drive path. Every other extended-length or device-namespace
root (`\\?\UNC\`, `\\?\pipe\`, another `\\?\` object path, `\\.\`, `\??\`) is not ordinary.

**IsLink(fullPath)**: True when the entry is a symbolic link or junction, even a dangling one
(`ReadLinkTarget` is not null). A link-safe check: only the entry's own link data is read.

**ReadLinkTarget(fullPath)**: The immediate target of a symbolic link or junction, read from the entry itself
with `FileInfo.LinkTarget` (which opens the reparse point without following it), or null when the entry is
not a link, does not exist or cannot be examined. The target is returned as stored: it may be relative to the
link's folder, and on Windows it may carry the NT prefix `\??\`.

**FindNetworkLinkTarget(fullPath[, readLinkTarget])**: On Windows, finds whether a path passes through a
symbolic link or junction whose target is a network location, without following any link. The link reader
defaults to `ReadLinkTarget`; tests inject a fake reader, so no link privilege or share is needed.

1. Walk the components of the path from its root. At each component prefix, read the immediate link target.
2. At the first link, normalize its target (`\??\UNC\x` becomes `\\x`, any other `\??\` becomes `\\?\`) and
   resolve a relative or rooted target against the link's folder with `Path.GetFullPath(target, parent)`.
3. Append the components after the link. When the resolved target is a network location
   (`IsNetworkLocation`), return that path.
4. Otherwise substitute the new path and start again from step 1. A component is therefore never read
   through a link that has not been checked, and a chain of links is followed by its targets.
5. Stop with null after 63 links. Windows does not open a path that needs more than 63 reparse points, so
   every link the open could follow has been checked; the limit also ends a link loop.

A missing component is not a link. The result is null when no link leads to a network location, on any
other system, and when a target is not a valid path (only `ArgumentException`, `NotSupportedException` and
`PathTooLongException` are caught). The real path of the opened file still decides in that case.

The native imports are private `DllImport` declarations:

- `kernel32.dll` with `CharSet.Unicode`, `ExactSpelling` and System32-only search: `GetFinalPathNameByHandleW`,
  `GetFileType`, `CreateFileW` and `GetLongPathNameW`.
- `libc` with NUL-terminated UTF-8 byte paths: `open` with its two fixed arguments only (the variadic mode is
  read only with `O_CREAT`, which is never passed), `fstat`, `__fxstat`, `proc_pidfdinfo` and `realpath`.
  On macOS `libc` is `libSystem`, which re-exports the kernel system-call library of xnu where
  `proc_pidfdinfo` is implemented. This is the CanvasNet binding. The macOS tests that read a real path from a handle,
  such as `RealPath_Inspect_TempFile_ReportsPrivateLocation`, prove that it resolves: only a successful
  `proc_pidfdinfo` call gives a real path on macOS.

Each import is called only from a member marked with `[SupportedOSPlatform]` after an `OperatingSystem`
check. No unsafe code is used: buffers are managed arrays. The raw descriptor of a handle is read only inside
`WithDescriptor`, which holds a reference count so that the number cannot be reused during the call.

#### Error Handling

`Inspect`, `InspectPath`, `InspectFolder`, `TryGetCanonicalDirectory`, `ExpandShortNames`, `IsLink`, `ReadLinkTarget`,
`FindNetworkLinkTarget` and `IsNetworkLocation` never throw. They report unknown facts as null or false, and the policy
fails closed. The open and create methods propagate the exceptions .NET raises for the corresponding failures.

#### Dependencies

N/A - uses only the .NET base class library and the operating-system calls listed above.

#### Callers

- **FileAccessPolicy** — every member.
- **GlobMatcher** — `IsLink`, to tell a listed folder that is a symbolic link or junction.
