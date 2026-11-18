### FileAccessPolicy

The `FileAccessPolicy` unit (the policy, `FileAccessMode` and `FileAccessDeniedException`) is verified
with unit tests in `FileAccessPolicyTests.cs`. Tests create real files, folders and links in a
`TemporaryDirectory`, build a policy with `FileAccessPolicy.Create`, and assert on the returned content,
the written file, or the `FileAccessDeniedException` reason and message.

#### Verification Approach

The policy's one collaborator, `RealPath`, is used for real, so the decisions are verified against the
real paths the operating system reports. To cover the branches a supported platform never produces (a
real path the platform cannot tell, or a handle that is not a regular file), the internal
`Create(mode, folders, trustedBase, inspector)` overload injects a handle inspector that returns chosen
`HandleFacts`. A recording inspector also proves that a path written outside the allowed folders is
denied before any handle is opened. The same overload takes an optional link reader, so a folder link to a
network location is simulated without a link privilege or a share; with the recording inspector it proves
that such a path is denied before any handle is opened. A link reader that reports no links at all, used
with a real folder link, simulates a link swapped in after the walk, so the denial must come from the handle.
A recording inspector also proves that a folder is told by its handle, not by a by-name probe.

#### Test Environment

- Links are created with the `LinkSupport` test helper. A folder link is a symbolic link, or on Windows
  a junction (`mklink /J`) when symbolic links need a privilege the account does not hold, so folder-link
  tests run on every platform.
- A file symbolic link has no fallback: on a Windows machine without the privilege (for example without
  Developer Mode), the file-link tests are skipped with `Assert.Skip` and a reason. Their requirements
  take evidence only from Linux and macOS runs, where they always run.
- Named-pipe and `/dev` tests (including a folder link to `/dev`, which needs no privilege there) run on Linux
  and macOS only, and Windows device-name tests on Windows only; elsewhere they are skipped with a reason. A
  named-pipe test fails when the read does not finish within 10 seconds, so a blocking open cannot hang the
  run.
- The link-to-share tests run on Windows only, because network locations exist only there; elsewhere they
  are skipped with a reason. The injected link reader needs no privilege and no share.
- The glob-match colon test and the file-link-to-device test run on Linux and macOS only (a colon in a file
  name is a stream separator on Windows, and `/dev` exists only there); elsewhere they are skipped with a
  reason. The glob-listing tests with folder links run on every platform (junction fallback on Windows).
- The colon tests run on every platform, except the drive-path test, which runs on Linux and macOS only (on
  Windows a drive path is an ordinary absolute path, verified by
  `FileAccessPolicy_DescribeNeverAccessed_OrdinaryPaths_ReturnsNull`), and the NT-namespace test, which runs
  on Windows only. Elsewhere they are skipped with a reason.
- The network-decision tests (`LeadsToNetwork`, the full-path rule and a pattern on a share) are pure
  string checks or use an injected link reader or inspector, so they need no share. They run on Windows
  only, because network locations and device namespaces exist only there.
- **FileAccessPolicy_ReadAllText_RealSymlinkToShare_DeniedBeforeOpen** creates a real symbolic link to an
  unreachable share. It needs the symbolic-link privilege and is skipped with a reason without it. It is
  supplementary and is not requirement evidence.
- **FileAccessPolicy_ReadAllText_RelativePathOnAdminShare_Allowed** reads through the administrative share
  `\\localhost\{drive}$`. It needs an administrator account and the Server service and is skipped with a
  reason when the share is not reachable. It is supplementary and is not requirement evidence.
- The case-variant sibling tests need a folder that distinguishes letter case, made by the
  `CaseSensitiveFolder` test helper (see the Test Helpers verification design): an ordinary folder on Linux,
  per-folder case sensitivity on Windows, a case-sensitive APFS disk image on macOS. They are skipped with a
  reason where none can be made. Their requirement evidence is taken from Linux runs only; the Windows and
  macOS runs are supplementary until a CI run shows that the hosted runners can make such a folder. The
  injected-real-path tests (`..._RealPathInCaseVariantSibling_...`) need no such folder and run on every
  platform.
- The differently-cased written path tests need a temporary folder that ignores letter case (Windows and macOS
  by default). They are skipped with a reason where the temporary folder distinguishes case (Linux).

#### Acceptance Criteria

- All unit tests in `FileAccessPolicyTests.cs` pass with zero failures on Windows, Linux and macOS.
- A test may be skipped only for the platform, link-privilege, share or letter-case-folder reasons above, and
  every requirement has at least one passing test on each platform its source filter names.

#### Test Scenarios

**FileAccessPolicy_ParseMode_ValidValues_Parsed** and
**FileAccessPolicy_ParseMode_InvalidValues_ThrowsListingModes**: Every mode in any letter case and
item order is parsed. `all`, `any,roots`, an empty value, an empty item, a `;` separator and a
repeated item are rejected with the message that lists the modes.

**FileAccessPolicy_ReadAllText_AnyModeOutsideFolders_ReturnsContent** and
**FileAccessPolicy_WriteAllText_AnyMode_CreatesFoldersAndWrites**: Mode any reads a file anywhere and
creates missing output folders.

**FileAccessPolicy_DescribeNeverAccessed_HostileText_ReturnsReason** and
**FileAccessPolicy_DescribeNeverAccessed_OrdinaryPaths_ReturnsNull**:

- `http:`, `https:`, `ftp:` and host-less `file:` URLs give the URL reason.
- `file://server/...` gives the file-host reason, and `\\server\share` and `//server/share` give the network
  reason.
- `\\.\pipe\x` and `\\?\C:\a.yaml` give the device reason.
- Relative, `..`, glob, `${VAR}` and absolute local paths are not on the list.

**FileAccessPolicy_DescribeNeverAccessed_Colon_ReturnsReason**: On every platform, `a.yaml:stream`,
`C:a.yaml`, `docs/terms.yaml:hidden`, `terms:v1.yaml`, `urn:x` and `file:/tmp/a.yaml` give the colon reason.

**FileAccessPolicy_DescribeNeverAccessed_NtNamespaceOnWindows_ReturnsReason** (Windows): `\??\C:\a.yaml`
gives the device reason.

**FileAccessPolicy_DescribeNeverAccessed_DrivePathOnLinuxAndMac_ReturnsColonReason** (Linux and macOS):
`C:/a.yaml` and `C:\a.yaml` give the colon reason, because those systems have no drives.

**FileAccessPolicy_ColonInWrittenPath_DeniedBeforeOpenNothingWritten**: On every platform, under roots, a
read of `terms:v1.yaml` and a write of `sub/out:1.md` are denied with the colon reason before anything is
opened, and nothing is created in the allowed folder.

**FileAccessPolicy_DescribeNeverAccessed_DeviceTextOnLinuxAndMac_ReturnsNull** (Linux and macOS):
`/??/a.yaml` and `/dev/null` are not on the text list. `\??\` is a Windows namespace, and a device is refused
by its handle type.

**FileAccessPolicy_ReadAllText_WindowsDeviceName_Denied** (Windows): A reserved device name in any component
is denied before the open.

**FileAccessPolicy_ReadAllText_DevNull_DeniedAsNotRegular** and
**FileAccessPolicy_ReadAllText_Fifo_DeniedWithoutBlocking** (Linux and macOS): No text rule applies.
`/dev/null` and a named pipe are denied as not a regular file after a non-blocking open, the pipe without
blocking.

**FileAccessPolicy_WriteAllText_DevPath_DeniedBeforeOpen** (Linux and macOS): The outputs `/dev/null` and
`/tmp/../dev/x.md` are denied with the device reason, and the recording inspector shows that nothing was
opened.

**FileAccessPolicy_WriteAllText_FolderLinkToDev_DeniedBeforeOpen** (Linux and macOS): In mode any, the
outputs `devices/null` and `devices/new/x.md` below a folder link to `/dev` are denied with the device reason,
and the direct output reports the real path `/dev/null`. The recording inspector shows that nothing was opened,
and no folder is created under `/dev`.

**FileAccessPolicy_DescribeNeverAccessedFullPath_SharesAndExtendedPaths** (Windows): `\\server\share\cfg\a.yaml`,
`\\?\C:\cfg\a.yaml` and `\\?\C:\cfg\NUL.txt` are accepted. `C:\x\NUL`, `\\.\C:\x\a.yaml`,
`\\?\pipe\x\a.yaml` and `\\?\UNC\s\x\a.yaml` give the device reason.

**FileAccessPolicy_ReadAllText_ExtendedLengthBase_ReturnsContent** (Windows): A relative path whose full path
is under the extended-length spelling `\\?\` of the temporary folder is read.

**FileAccessPolicy_LeadsToNetwork_LocalToShareOnly** (Windows): A local path whose real path is on a share
leads to the network. A share path whose real path is on the same or another share, and a local path whose
real path is local, do not.

**FileAccessPolicy_CheckPath_PathOnShare_AllowedWithoutWalk** (Windows): In mode any, a glob's literal folder
`\\server\share\cfg` and a literal file in it are allowed by `CheckPath`, and the recording link reader shows
that they were not walked (nothing is listed or opened).

**FileAccessPolicy_DescribeNeverAccessedFolder_LinkToShare_ReturnsReason** (Windows): With a fake link reader
that makes `lnk` a link to `\\server\share`, the base-folder check gives the network-link reason for `lnk/sub`,
none for an ordinary folder, and the network reason for a folder written as a UNC path. The ordinary folder is
reported as existing, and each denied folder is reported as not existing.

**FileAccessPolicy_DescribeNeverAccessedFolder_ExistenceFromFolderFacts**: With a recording folder inspector
that returns given facts, the base-folder check reports a folder as existing only from those facts: a path that is
not on disk but whose open reports a folder exists; a real folder whose open reports it missing, not a folder or
unknown does not; an existing entry of unknown type (Linux where `fstat` cannot be read) exists. The inspector is
called exactly once per check, with the full path, and a folder denied by its text (a UNC path or a URL) is never
inspected and never exists. This proves that no by-name probe decides existence and that nothing is inspected
before the text verdict.

**FileAccessPolicy_ReadAllText_LocalPathRealPathOnShare_Denied** (Windows): A local file whose handle reports a
real path on a share is denied with the network-link reason, naming the share path as its real path.

**FileAccessPolicy_ReadAllText_RelativePathOnAdminShare_Allowed** (Windows, supplementary): A relative path
resolved against the temporary folder on `\\localhost\{drive}$` is read.

**FileAccessPolicy_ReadAllText_IntermediateLinkToShare_DeniedBeforeOpen**,
**FileAccessPolicy_WriteAllText_IntermediateLinkToShare_DeniedNothingCreated** and
**FileAccessPolicy_ResolvePattern_PrefixLinkToShare_DeniedBeforeMatching** (Windows): A fake link reader makes
the folder `lnk` a link to `\\server\share`.

- A read through `lnk` is denied with the network-link reason and the share path as its real path, and the
  recording inspector shows that no handle was opened.
- A write through `lnk` under roots is denied with the network-link reason, and no folder is created.
- A glob pattern and a literal pattern whose folders pass through `lnk` are denied before matching, while a
  glob pattern in another (missing) folder is not.

**FileAccessPolicy_ResolvePattern_LinkUnderWildcardToShare_DeniedBeforeListing** (Windows): A real folder link
`docs/lnk`, which a fake link reader reports as leading to `\\server\share`, is reached by `docs/**/*.yaml`. The
pattern is denied with the network-link reason, naming the share as the real path, before the link is listed.

**FileAccessPolicy_ReadAllText_RealSymlinkToShare_DeniedBeforeOpen** (Windows, supplementary): A real
symbolic link to an unreachable share is denied with the network-link reason before the open, which would
otherwise fail with an I/O error after contacting the network.

**FileAccessPolicy_ReadAllText_Directory_Denied**, **FileAccessPolicy_WriteAllText_ExistingDirectory_Denied**
and **FileAccessPolicy_ReadAllText_NotRegularHandle_Denied**: A folder is never read or written, and a
handle reported as not a regular file is denied.

**FileAccessPolicy_JudgedPath_ExpandedTextJudged_ShownAsWritten**: For a read and a write of a judged path
shown as `${V}/x.md` whose full path is ordinary, an expanded text `//nonexistent.invalid/share/x.md`,
`sub:x/out.md` or `https://x/a.md` gives the network, colon or URL reason; the denial shows `${V}/x.md`, the
recording inspector shows nothing was opened, and nothing is created.

**FileAccessPolicy_ResolvePattern_UrlPattern_Denied**: A URL glob is denied by its text.

**FileAccessPolicy_ResolvePattern_GlobMatch_ExpandedIsPrefixPlusMatch**: The file `docs/sub/a.yaml` matched by
`docs/**/*.yaml` is returned with the full path shown and `docs/sub/a.yaml` as its expanded text.

**FileAccessPolicy_ReadAllText_GlobMatchWithColon_Denied** (Linux and macOS): A file `terms:v1.yaml` matched by
`*.yaml` is denied with the colon reason when it is read, shown by its full path.

**FileAccessPolicy_ReadAllText_Directory_ClassifiedFromHandle**: A folder named as an input is opened, the
recording inspector shows its handle was judged, and it is denied as not a regular file.

**FileAccessPolicy_ReadAllText_LinkAppearingAfterWalk_DeniedByHandle** and
**FileAccessPolicy_WriteAllText_LinkAppearingAfterWalk_DeniedByHandle**: Under roots, with a link reader that
reports no links, a read and a write through a real folder link that leads out are denied by the real path of
the file's or the folder's handle, and nothing is created outside.

**FileAccessPolicy_WriteAllText_FileLinkToDevice_DeniedBeforeOpen** (Linux and macOS): In mode any, an existing
output `out.md` that links to `/dev/null` is denied with the device reason and real path `/dev/null`, and the
recording inspector shows nothing was opened for writing.

**FileAccessPolicy_ResolvePattern_Literal_ReturnedWithoutProbe**: A literal pattern naming a missing file is
returned as one judged path; the read, not the resolution, decides whether it exists.

**FileAccessPolicy_ResolvePattern_MissingFolder_ReturnsEmpty**: A glob whose literal folder does not exist
matches nothing.

**FileAccessPolicy_ResolvePattern_FolderIsFile_ReturnsEmpty**: In any, roots, no-symlinks and roots,no-symlinks,
the glob `a.yaml/*.yaml`, whose literal folder `a.yaml` is a regular file, matches nothing, without an error or a
denial, on every platform.

**FileAccessPolicy_ResolvePattern_RootsFolderIsFileLinkLeadingOut_ReturnsEmpty** (where file links can be
created): Under roots, the glob `lnk.yaml/*.yaml`, whose literal folder `allowed/lnk.yaml` is a file link to
`outside/real.yaml`, matches nothing rather than being denied as a folder that leads out. On macOS this proves
that the literal folder is told by a folder open, not by `realpath(3)`.

**FileAccessPolicy_ResolvePattern_RootsFolderOutside_DeniedBeforeMatching**,
**FileAccessPolicy_ResolvePattern_RootsFolderLinkLeadingOut_DeniedBeforeMatching** and
**FileAccessPolicy_ResolvePattern_NoSymlinksPrefixThroughLink_DeniedBeforeMatching**: The literal folder of a
glob is judged before it is listed. Under roots, `../shared/*.yaml` is denied as written with the exact message,
and `lnk/*.yaml` through a folder link leading out is denied by the folder's real path. Under no-symlinks,
`lnk/*.yaml` through a link below the trusted base is denied with the folder reason, naming the pattern.

**FileAccessPolicy_ResolvePattern_LinkUnderWildcard_DeniedBeforeListing**,
**FileAccessPolicy_ResolvePattern_RootsLinkUnderWildcardInside_Matched** and
**FileAccessPolicy_ResolvePattern_RootsLinkUnderWildcardLeadingOut_DeniedBeforeListing**: A folder link a
wildcard descends into is judged before it is listed. Under no-symlinks, `**/*.yaml` is denied for a link below
the trusted base, while `real/*.yaml`, which never descends into it, matches. Under roots, a link that stays
inside is listed (its file is matched through the link and directly), and a link that leads out is denied.

**FileAccessPolicy_ReadAllText_InsideFolder_ReturnsContent**,
**FileAccessPolicy_ReadAllText_WrittenPathOutside_DeniedBeforeOpen**,
**FileAccessPolicy_ReadAllText_SiblingWithSharedPrefix_Denied**,
**FileAccessPolicy_ResolvePattern_LiteralOutside_Denied**: Under roots a file inside is read; a path written
outside is denied with the exact message and no handle opened; `docs2` is outside `docs`; a literal pattern
outside is denied before anything is touched.

**FileAccessPolicy_ReadAllText_RealPathInCaseVariantSibling_Denied** and
**FileAccessPolicy_WriteAllText_RealPathInCaseVariantSibling_ContentUnchanged**: On every platform, an injected
inspector reports the real path of the opened file in `ALLOWED`, the case variant of the allowed folder
`allowed`. Under roots the read is denied with the real-path reason and that real path, and the existing
output is denied before it is truncated, so its content is unchanged.

**FileAccessPolicy_ReadAllText_CaseVariantSiblingOfAllowedFolder_Denied**,
**FileAccessPolicy_WriteAllText_CaseVariantSiblingOfAllowedFolder_DeniedNothingWritten** and
**FileAccessPolicy_ResolvePattern_CaseVariantSiblingFolder_DeniedBeforeListing**: In a folder that distinguishes
case, with sibling folders `allowed` and `ALLOWED` and only `allowed` allowed, under roots a file in `allowed`
is read while one in `ALLOWED` is denied as outside the allowed folders. An output in a missing folder of
`ALLOWED` is denied and nothing, not even the folder, is created there. A glob whose literal folder is
`ALLOWED` fails, naming the pattern as written, instead of returning the match. On Linux the denial comes from
the path as written, on Windows and macOS from the real path.

**FileAccessPolicy_ReadAllText_DifferentlyCasedWrittenPath_Allowed** and
**FileAccessPolicy_WriteAllText_DifferentlyCasedWrittenPath_CreatesAndWrites** (Windows and macOS): Where the
temporary folder ignores case, a file stored as `Docs/a.yaml` is read as `DOCS/A.yaml` with the allowed folder
and the trusted base written as `docs`, under roots and under roots,no-symlinks. An output written as
`OUT/new/x.md` with the allowed folder written as `out` (stored as `Out`) is written into `Out/new`, and the
created folder, verified exactly, has the name `new` as written.

**FileAccessPolicy_ReadAllText_DirectoryLinkLeadingOut_Denied**,
**FileAccessPolicy_ReadAllText_OutsidePathLinkingBackIn_Denied** and
**FileAccessPolicy_ReadAllText_DirectoryLinkStayingInside_Allowed**: A folder link that leads out is
denied by its real path, which the message names. An outside path that links back in is denied as
written. A link that stays inside is allowed.

**FileAccessPolicy_ReadAllText_NoSymlinksLinkBelowTrustedBase_Denied**,
**FileAccessPolicy_ReadAllText_NoSymlinksLinkAboveTrustedBase_Allowed** and
**FileAccessPolicy_ReadAllText_NoSymlinksFileSymlink_Denied**: Under no-symlinks a folder link below
the configuration folder is denied, a link above it is trusted, and a file that is itself a link is
denied.

**FileAccessPolicy_ReadAllText_NoSymlinksCaseVariantAnchorLink_Denied**: In a folder that distinguishes case,
with the trusted base `proj/cfg` and a folder link `Proj` to `elsewhere`, under no-symlinks a file read through
`Proj/docs/a.yaml` is denied because its real path differs from the path as written: the link `Proj` is not
taken for the folder `proj` above the trusted base and trusted as the anchor.

**FileAccessPolicy_ReadAllText_RealPathUnknown_DeniedUnderRestrictiveModes** and
**FileAccessPolicy_ReadAllText_RealPathUnknown_AllowedUnderAny**: With an inspector that cannot tell
the real path, roots, no-symlinks and roots,no-symlinks deny the file with a reason naming the mode,
while any reads it.

**FileAccessPolicy_ReadAllText_TypeUnknown_DeniedWithTypeReason** and
**FileAccessPolicy_WriteAllText_TypeUnknown_ContentUnchanged**: With an inspector that reports unknown facts
(exactly what the handle inspection returns when a native library or entry point is missing), a read is denied in
any, roots, no-symlinks and roots,no-symlinks with the exact type-unknown reason, which does not say "not a
regular file", and with no real path. A write in mode any is denied with the same reason, and the existing output
keeps its content.

**FileAccessPolicy_WriteAllText_OutputOutside_DeniedNothingWritten**,
**FileAccessPolicy_WriteAllText_MissingFoldersInside_Created**,
**FileAccessPolicy_WriteAllText_ParentFolderLinkLeadingOut_DeniedNoFolderCreated**,
**FileAccessPolicy_WriteAllText_ExistingOutputSymlinkLeadingOut_DeniedTargetUnchanged**,
**FileAccessPolicy_WriteAllText_ExistingOutput_ReplacedContent**,
**FileAccessPolicy_WriteAllText_NotRegularHandle_ContentUnchanged**,
**FileAccessPolicy_WriteAllText_ParentIsFile_FailsNothingWritten** and
**FileAccessPolicy_WriteAllText_NoSymlinksLinkedFolder_Denied**: These cover the write rules.

- An output outside the allowed folders is denied, and its missing folder is not created.
- Missing folders inside are created one level at a time (in mode any too).
- A parent folder link that leads out is denied, and no folder is created behind it.
- An existing output that links out is denied, and its target keeps its content.
- A shorter content fully replaces a longer one, which proves the truncation after verification.
- A denied handle leaves the existing content unchanged.
- An output `a.yaml/out.md` whose parent `a.yaml` is a regular file fails in every mode with a plain I/O error
  (not a denial, and not a missing folder): the file is never taken for an existing folder, on macOS too. The
  file keeps its content, and nothing else is created in its folder.
- No-symlinks denies an output whose folder is a link.

**FileAccessDeniedException_Message_ContainsCodePathsAndReason** and
**FileAccessDeniedException_Message_UnknownRealPath_OmitsRealPart**: The message is
`file-access-denied: '{written}' (real path '{real}'): {reason}`, without the real-path part when it is
unknown.
