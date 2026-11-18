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
that such a path is denied before any handle is opened.

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

#### Acceptance Criteria

- All unit tests in `FileAccessPolicyTests.cs` pass with zero failures on Windows, Linux and macOS.
- A test may be skipped only for the platform, link-privilege or share reasons above, and every requirement
  has at least one passing test on each platform its source filter names.

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

**FileAccessPolicy_CheckPattern_PatternOnShare_AllowedWithoutWalk** (Windows): In mode any, a glob and a
literal pattern rooted at `\\server\share\cfg` are allowed, and the recording link reader shows that their
folders were not walked.

**FileAccessPolicy_ReadAllText_LocalPathRealPathOnShare_Denied** (Windows): A local file whose handle reports a
real path on a share is denied with the network-link reason, naming the share path as its real path.

**FileAccessPolicy_ReadAllText_RelativePathOnAdminShare_Allowed** (Windows, supplementary): A relative path
resolved against the temporary folder on `\\localhost\{drive}$` is read.

**FileAccessPolicy_ReadAllText_IntermediateLinkToShare_DeniedBeforeOpen**,
**FileAccessPolicy_WriteAllText_IntermediateLinkToShare_DeniedNothingCreated** and
**FileAccessPolicy_CheckPattern_PrefixLinkToShare_DeniedBeforeMatching** (Windows): A fake link reader makes
the folder `lnk` a link to `\\server\share`.

- A read through `lnk` is denied with the network-link reason and the share path as its real path, and the
  recording inspector shows that no handle was opened.
- A write through `lnk` under roots is denied with the network-link reason, and no folder is created.
- A glob pattern and a literal pattern whose folders pass through `lnk` are denied before matching, while a
  glob pattern in another folder is not.

**FileAccessPolicy_ReadAllText_RealSymlinkToShare_DeniedBeforeOpen** (Windows, supplementary): A real
symbolic link to an unreachable share is denied with the network-link reason before the open, which would
otherwise fail with an I/O error after contacting the network.

**FileAccessPolicy_ReadAllText_Directory_Denied**, **FileAccessPolicy_WriteAllText_ExistingDirectory_Denied**
and **FileAccessPolicy_ReadAllText_NotRegularHandle_Denied**: A folder is never read or written, and a
handle reported as not a regular file is denied.

**FileAccessPolicy_ReadAllText_InsideFolder_ReturnsContent**,
**FileAccessPolicy_ReadAllText_WrittenPathOutside_DeniedBeforeOpen**,
**FileAccessPolicy_ReadAllText_SiblingWithSharedPrefix_Denied**,
**FileAccessPolicy_CheckPattern_LiteralOutside_Denied** and
**FileAccessPolicy_CheckPattern_GlobAndUrlPatterns_JudgedByText**: Under roots a file inside is read; a
path written outside is denied with the exact message and no handle opened; `docs2` is outside `docs`;
a literal pattern outside is denied before matching; a glob pattern is left to its matches, and a URL
pattern is denied by text.

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

**FileAccessPolicy_ReadAllText_RealPathUnknown_DeniedUnderRestrictiveModes** and
**FileAccessPolicy_ReadAllText_RealPathUnknown_AllowedUnderAny**: With an inspector that cannot tell
the real path, roots, no-symlinks and roots,no-symlinks deny the file with a reason naming the mode,
while any reads it.

**FileAccessPolicy_WriteAllText_OutputOutside_DeniedNothingWritten**,
**FileAccessPolicy_WriteAllText_MissingFoldersInside_Created**,
**FileAccessPolicy_WriteAllText_ParentFolderLinkLeadingOut_DeniedNoFolderCreated**,
**FileAccessPolicy_WriteAllText_ExistingOutputSymlinkLeadingOut_DeniedTargetUnchanged**,
**FileAccessPolicy_WriteAllText_ExistingOutput_ReplacedContent**,
**FileAccessPolicy_WriteAllText_NotRegularHandle_ContentUnchanged** and
**FileAccessPolicy_WriteAllText_NoSymlinksLinkedFolder_Denied**: These cover the write rules.

- An output outside the allowed folders is denied, and its missing folder is not created.
- Missing folders inside are created one level at a time.
- A parent folder link that leads out is denied, and no folder is created behind it.
- An existing output that links out is denied, and its target keeps its content.
- A shorter content fully replaces a longer one, which proves the truncation after verification.
- A denied handle leaves the existing content unchanged.
- No-symlinks denies an output whose folder is a link.

**FileAccessDeniedException_Message_ContainsCodePathsAndReason** and
**FileAccessDeniedException_Message_UnknownRealPath_OmitsRealPart**: The message is
`file-access-denied: '{written}' (real path '{real}'): {reason}`, without the real-path part when it is
unknown.
