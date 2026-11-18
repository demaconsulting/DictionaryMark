// Copyright (c) DEMA Consulting
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32.SafeHandles;

namespace DemaConsulting.DictionaryMark.Utilities;

/// <summary>
///     Decides which files named in a configuration file the tool may read and write, and performs those reads and
///     writes on the verified handle.
/// </summary>
/// <remarks>
///     <para>
///         This unit is the single place where the <c>--file-access</c> mode is applied, so every configuration-file
///         path is judged by the same rules. The rules follow the DEMA Consulting CanvasNet tool:
///     </para>
///     <list type="bullet">
///         <item>
///             In every mode, a URL (<c>scheme://</c>), a <c>file:</c> URI with a host, a path written as a UNC path,
///             a device path, a colon other than the drive of an absolute path on Windows (on Linux and macOS, any
///             colon, also in the name of a file a glob matched), on Linux and macOS an output under <c>/dev</c>
///             (also through a link), and anything that is not a regular file are never accessed.
///         </item>
///         <item>
///             Network locations follow CanvasNet: a path written as a UNC path is never accessed, a path that is
///             itself on a network location (on a mapped network drive, or resolved against a folder on a share) is
///             accessed, and on Windows a local path that leads to a network location through a symbolic link or
///             junction is denied, before anything opens it where the links can be read.
///         </item>
///         <item>
///             Under <see cref="FileAccessMode.Roots"/>, both the full path as written (after <c>${VAR}</c>
///             expansion, rooting and <c>..</c> normalization) and the real path of the opened file must be inside an
///             allowed folder.
///         </item>
///         <item>
///             Under <see cref="FileAccessMode.NoSymlinks"/>, the real path must equal the path as written below the
///             trusted anchor (the deepest folder the path shares with the configuration file's folder), so no
///             symbolic link or junction lies below it; the anchor and the folders above it are trusted.
///         </item>
///         <item>
///             In a mode other than <see cref="FileAccessMode.None"/>, a path whose real path cannot be determined is
///             denied.
///         </item>
///     </list>
///     <para>
///         Every judged path is carried as a <see cref="JudgedPath"/>: the text shown in messages (as written), the
///         text the written-text rules apply to (after <c>${VAR}</c> expansion), and the full path the full-path,
///         network-link, roots and real-path rules apply to. Each is judged before any operation touches it: the
///         text rules, the full-path rules, a walk for links to a network location that reads each link's own target
///         and never follows one, and under roots the full path as written. After that, the file system is touched
///         only by opening a handle whose facts (folder or file, real path) are judged before anything is read,
///         written, listed or created through it; there is no by-name existence or attribute probe. A glob's literal
///         folder, and every link folder a wildcard would descend into, is judged that way before it is listed.
///     </para>
///     <para>
///         Decisions are made on the real path of the open handle, not on the name, so a link swapped in between a
///         check and the open is still caught. A write never truncates before the handle is verified. Hard links
///         cannot be detected. Instances are immutable and thread-safe; reads, writes and pattern resolution perform
///         file-system I/O.
///     </para>
///     <para>Deliberate differences from CanvasNet (see the design):</para>
///     <list type="bullet">
///         <item>
///             <c>file:////server</c> gets the URL reason rather than the <c>file:</c> host reason, because no
///             host follows <c>file://</c>; both deny it.
///         </item>
///         <item>
///             The written-text rules also apply to the expanded <c>root:</c> value (applied by ConfigFile through
///             <see cref="DescribeNeverAccessedFolder(string, string, out bool)"/>), because it is written
///             configuration content; CanvasNet has no such key.
///         </item>
///         <item>
///             <c>${VAR}</c> is expanded in <c>--allow-root</c> (applied by Context), because every folder option
///             is resolved by <see cref="PathResolver"/>, as <c>--root</c> is.
///         </item>
///         <item>
///             An ordinary extended-length <c>--allow-root</c> is rejected (applied by Context), because
///             <see cref="PathResolver"/> refuses <c>?</c> as a wildcard in every folder option.
///         </item>
///         <item>
///             The path is walked for a link to a network location before it is opened, because matching a glob
///             and creating an output folder touch the disk before the file's own handle exists.
///         </item>
///         <item>
///             A glob's literal folder, and every link folder a wildcard would descend into, is judged by the rules
///             of a folder before it is listed, and a file a glob matched is judged by the written-text rules on the
///             pattern's literal part followed by the matched path; CanvasNet has no globs.
///         </item>
///         <item>
///             The write rules have no counterpart, because CanvasNet never writes: on Linux and macOS an output
///             under <c>/dev</c>, whose real path is under <c>/dev</c>, or whose deepest existing folder has a real
///             path under <c>/dev</c>, is denied before the open; a missing output is created exclusively; an
///             existing one is opened without truncation and verified first; missing folders are created one at a
///             time and each verified; and a dangling output link is denied in a restrictive mode.
///         </item>
///         <item>
///             A named pipe with no reader, whose non-blocking open for writing fails with <c>ENXIO</c> on Linux
///             and macOS, is denied as not a regular file, so the write never blocks.
///         </item>
///         <item>
///             The denial is reported as <c>file-access-denied</c> in the per-job error of the tool, because that
///             is its existing error convention; the code name is the same.
///         </item>
///     </list>
/// </remarks>
internal sealed partial class FileAccessPolicy
{
    /// <summary>The usage text that lists the valid modes.</summary>
    private const string ModesHelp = "use any (the default), roots, no-symlinks, or roots,no-symlinks";

    /// <summary>The reason for a URL or URI.</summary>
    private const string UrlReason = "it is a URL or URI, which is never accessed";

    /// <summary>The reason for a <c>file:</c> URI with a host.</summary>
    private const string FileUriHostReason =
        "it is a file: URI with a host, which names a network location and is never accessed";

    /// <summary>The reason for a path written as a UNC or network path.</summary>
    private const string NetworkReason = "it is a UNC or network path, which is never accessed";

    /// <summary>The reason for a local path that leads to a network location through a link.</summary>
    private const string NetworkLinkReason =
        "it leads to a network location through a symbolic link or junction, which is never accessed";

    /// <summary>The reason for a device path.</summary>
    private const string DeviceReason = "it is a device path, which is never accessed";

    /// <summary>The reason for a colon other than the drive of an absolute path on Windows.</summary>
    private const string ColonReason =
        "it contains a colon other than the drive of an absolute path on Windows, which is never accessed";

    /// <summary>The reason for anything that is not a regular file.</summary>
    private const string NotRegularReason =
        "it is not a regular file (a folder, device, pipe or other special file), which is never accessed";

    /// <summary>The directory separators accepted when a path is split into components.</summary>
    private static readonly char[] ComponentSeparators = ['/', '\\'];

    /// <summary>The wildcard characters of a glob pattern.</summary>
    private static readonly char[] Wildcards = ['*', '?'];

    /// <summary>The UTF-8 encoding without a byte-order mark, matching <see cref="File.WriteAllText(string, string)"/>.</summary>
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>The allowed folders, with their real locations captured when the policy was created.</summary>
    private readonly AllowedFolder[] _folders;

    /// <summary>The function that reports the facts of an open handle (the platform, or a test double).</summary>
    private readonly Func<SafeFileHandle, HandleFacts> _inspector;

    /// <summary>
    ///     The function that reads the immediate target of a symbolic link or junction without following it (the
    ///     platform, or a test double).
    /// </summary>
    private readonly Func<string, string?> _linkReader;

    /// <summary>
    ///     Initializes a new instance of the <see cref="FileAccessPolicy"/> class.
    /// </summary>
    /// <param name="mode">The mode.</param>
    /// <param name="folders">The allowed folders.</param>
    /// <param name="trustedBase">The trusted base folder, or null.</param>
    /// <param name="inspector">The handle inspector.</param>
    /// <param name="linkReader">The link reader.</param>
    private FileAccessPolicy(
        FileAccessMode mode,
        AllowedFolder[] folders,
        string? trustedBase,
        Func<SafeFileHandle, HandleFacts> inspector,
        Func<string, string?> linkReader)
    {
        Mode = mode;
        _folders = folders;
        TrustedBase = trustedBase;
        _inspector = inspector;
        _linkReader = linkReader;
        AllowedFolders = [.. folders.Select(folder => folder.Lexical)];
    }

    /// <summary>Gets the default policy: mode <c>any</c> with no allowed folders.</summary>
    public static FileAccessPolicy Any { get; } = new(FileAccessMode.None, [], null, RealPath.Inspect, RealPath.ReadLinkTarget);

    /// <summary>Gets the mode.</summary>
    public FileAccessMode Mode { get; }

    /// <summary>Gets the mode as written on the command line: any, roots, no-symlinks or roots,no-symlinks.</summary>
    public string ModeName => NameOf(Mode);

    /// <summary>Gets the full paths of the allowed folders, as resolved (used under <see cref="FileAccessMode.Roots"/>).</summary>
    public IReadOnlyList<string> AllowedFolders { get; }

    /// <summary>
    ///     Gets the trusted base folder (the configuration file's folder), or null; it and every folder above it are
    ///     trusted under <see cref="FileAccessMode.NoSymlinks"/>.
    /// </summary>
    public string? TrustedBase { get; }

    /// <summary>
    ///     Gets the name of a mode as written on the command line.
    /// </summary>
    /// <param name="mode">The mode.</param>
    /// <returns><c>any</c>, <c>roots</c>, <c>no-symlinks</c> or <c>roots,no-symlinks</c>.</returns>
    public static string NameOf(FileAccessMode mode) => mode switch
    {
        FileAccessMode.None => "any",
        FileAccessMode.Roots => "roots",
        FileAccessMode.NoSymlinks => "no-symlinks",
        _ => "roots,no-symlinks",
    };

    /// <summary>
    ///     Parses the value of the <c>--file-access</c> option.
    /// </summary>
    /// <remarks>
    ///     The value is split at commas and each item is trimmed and compared in any letter case. <c>any</c> is
    ///     valid only on its own; otherwise the items must be distinct and drawn from <c>roots</c> and
    ///     <c>no-symlinks</c>, in either order.
    /// </remarks>
    /// <param name="text">The option value. Must not be null.</param>
    /// <returns>The mode.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the value is not a valid mode; the message lists the modes.</exception>
    public static FileAccessMode ParseMode(string text)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(text);

        // Normalize the items: trimmed, lower case
        var items = text.Split(',').Select(item => item.Trim().ToLowerInvariant()).ToArray();
        if (items is ["any"])
        {
            return FileAccessMode.None;
        }

        // Combine distinct roots and no-symlinks items; anything else (including an empty item) is invalid
        var mode = FileAccessMode.None;
        foreach (var item in items)
        {
            var flag = item switch
            {
                "roots" => FileAccessMode.Roots,
                "no-symlinks" => FileAccessMode.NoSymlinks,
                _ => FileAccessMode.None,
            };
            if (flag == FileAccessMode.None || mode.HasFlag(flag))
            {
                throw new ArgumentException($"'{text}' is not a value of --file-access: {ModesHelp}");
            }

            mode |= flag;
        }

        return mode;
    }

    /// <summary>
    ///     Creates a policy, capturing the real location of every allowed folder now.
    /// </summary>
    /// <remarks>
    ///     The real locations are never looked up again, so a folder renamed and replaced by a link later cannot
    ///     widen the policy. A folder whose real location cannot be determined allows no file under
    ///     <see cref="FileAccessMode.Roots"/> (fail closed). Performs file-system I/O when the mode is not
    ///     <see cref="FileAccessMode.None"/>.
    /// </remarks>
    /// <param name="mode">The mode.</param>
    /// <param name="allowedFolders">The full paths of the allowed folders. Must not be null.</param>
    /// <param name="trustedBase">The configuration file's folder, or null when there is none.</param>
    /// <returns>The policy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="allowedFolders"/> is null.</exception>
    public static FileAccessPolicy Create(FileAccessMode mode, IReadOnlyList<string> allowedFolders, string? trustedBase) =>
        Create(mode, allowedFolders, trustedBase, RealPath.Inspect);

    /// <summary>
    ///     Creates a policy with a given handle inspector and link reader, so tests can simulate a platform that
    ///     cannot tell a fact, or links that lead to a network location.
    /// </summary>
    /// <param name="mode">The mode.</param>
    /// <param name="allowedFolders">The full paths of the allowed folders. Must not be null.</param>
    /// <param name="trustedBase">The configuration file's folder, or null when there is none.</param>
    /// <param name="inspector">The function that reports the facts of an open handle. Must not be null.</param>
    /// <param name="linkReader">
    ///     The function that reads the immediate target of a link without following it, or null for
    ///     <see cref="RealPath.ReadLinkTarget(string)"/>. It must not throw.
    /// </param>
    /// <returns>The policy.</returns>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="allowedFolders"/> or <paramref name="inspector"/> is null.
    /// </exception>
    internal static FileAccessPolicy Create(
        FileAccessMode mode,
        IReadOnlyList<string> allowedFolders,
        string? trustedBase,
        Func<SafeFileHandle, HandleFacts> inspector,
        Func<string, string?>? linkReader = null)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(allowedFolders);
        ArgumentNullException.ThrowIfNull(inspector);

        // Capture each folder's full path and, in a restrictive mode, its real location
        var folders = allowedFolders
            .Select(Path.GetFullPath)
            .Distinct(StringComparer.Ordinal)
            .Select(full => new AllowedFolder(full, mode == FileAccessMode.None ? null : RealPath.TryGetCanonicalDirectory(full)))
            .ToArray();
        var baseFolder = trustedBase == null ? null : Path.GetFullPath(trustedBase);
        return new FileAccessPolicy(mode, folders, baseFolder, inspector, linkReader ?? RealPath.ReadLinkTarget);
    }

    /// <summary>
    ///     Explains why a path text is on the never-accessed list, or returns null when it is not.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         A pure text check of a path as written (after <c>${VAR}</c> expansion), run before anything is
    ///         touched, in every mode. Backslashes count as slashes. The rules, in order: a device path
    ///         (<c>\\?\</c> and <c>\\.\</c> on every platform, <c>\??\</c> on Windows); a path written as a network
    ///         path (<c>\\server</c>, <c>//server</c>); a URL (a scheme of two or more characters followed by
    ///         <c>://</c>), where a <c>file:</c> URL with a host is a network location; any other colon, on every
    ///         platform, except the drive of an absolute path on Windows (an alternate data stream, a drive-relative
    ///         path, another URI such as <c>urn:x</c>, and on Linux and macOS a drive path such as <c>C:/x</c>), as
    ///         in CanvasNet; and on Windows a reserved device name in any component.
    ///     </para>
    ///     <para>
    ///         These rules apply to the text as written after expansion, as in CanvasNet, so a relative path resolved
    ///         against a folder on a share is not a network path. A URL is recognized by <c>://</c>, so a colon in a
    ///         file name is never mistaken for a scheme. A device file on Linux and macOS is refused by its handle
    ///         type after a non-blocking open, and an output under <c>/dev</c> by <see cref="WriteAllText"/>.
    ///     </para>
    /// </remarks>
    /// <param name="path">The path text. Must not be null.</param>
    /// <returns>The reason, or null when the text may be accessed.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    public static string? DescribeNeverAccessed(string path)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(path);

        // Device and network prefixes come first, so their reason names them rather than a colon
        var text = path.Replace('\\', '/');
        if (text.StartsWith("//?/", StringComparison.Ordinal) || text.StartsWith("//./", StringComparison.Ordinal)
            || (OperatingSystem.IsWindows() && RealPath.IsWindowsDeviceNamespace(text)))
        {
            return DeviceReason;
        }

        if (text.StartsWith("//", StringComparison.Ordinal))
        {
            return NetworkReason;
        }

        // A scheme followed by // names a URL; a file: URL with a host names a network location
        var scheme = SchemeRegex().Match(text);
        if (scheme.Success)
        {
            var hasHost = text.Length > scheme.Length && text[scheme.Length] != '/';
            return scheme.Groups[1].Value.Equals("file", StringComparison.OrdinalIgnoreCase) && hasHost
                ? FileUriHostReason
                : UrlReason;
        }

        // The only colon a path may carry is the drive of an absolute path on Windows; any other is an alternate
        // data stream, a drive-relative path, another URI, or a drive on a system that has none (as in CanvasNet)
        var allowedColon = OperatingSystem.IsWindows() && HasDrive(text) ? 1 : -1;
        if (text.IndexOf(':', allowedColon + 1) >= 0)
        {
            return ColonReason;
        }

        // On Windows, a reserved device name in any component
        return OperatingSystem.IsWindows() && RealPath.HasWindowsDeviceName(text) ? DeviceReason : null;
    }

    /// <summary>
    ///     Explains why a full candidate path is never accessed, or returns null when it is not.
    /// </summary>
    /// <remarks>
    ///     A pure text check of the full path (the written path combined with its base folder), as CanvasNet applies
    ///     to a full candidate: on Windows only, a path in a device namespace or with a reserved device name in any
    ///     component is a device path, except under an ordinary extended-length drive or volume root
    ///     (<c>\\?\C:\</c>, <c>\\?\Volume{GUID}\</c>), where device names are not mapped. The written text cannot
    ///     itself be extended-length (<see cref="DescribeNeverAccessed(string)"/> denies it), so such a full path
    ///     only comes from a trusted base folder. The URL, network and colon rules apply only to the text as
    ///     written, so a path on a share or on a mapped network drive is not denied here. Always null on Linux and
    ///     macOS.
    /// </remarks>
    /// <param name="fullPath">The full path. Must not be null.</param>
    /// <returns>The reason, or null.</returns>
    internal static string? DescribeNeverAccessedFullPath(string fullPath) =>
        OperatingSystem.IsWindows()
            && !RealPath.IsOrdinaryExtendedPath(fullPath)
            && (RealPath.IsWindowsDeviceNamespace(fullPath) || RealPath.HasWindowsDeviceName(fullPath))
            ? DeviceReason
            : null;

    /// <summary>
    ///     Determines whether a path that is not itself a network location leads to one.
    /// </summary>
    /// <remarks>
    ///     CanvasNet's network-target rule: a path the host placed on a network drive or share is accessed, but a
    ///     local path whose real path (or link target) is a network location reached it through a symbolic link or
    ///     junction, and is never accessed. A mapped network drive counts as a network location, so a path on
    ///     <c>Z:\</c> whose real path names the share is not denied. Always false on Linux and macOS, where
    ///     <see cref="RealPath.IsNetworkLocation(string)"/> is false.
    /// </remarks>
    /// <param name="fullPath">The full path as written. Must not be null.</param>
    /// <param name="realPath">The real path, or the path its links lead to. Must not be null.</param>
    /// <returns>True when the local path leads to a network location.</returns>
    internal static bool LeadsToNetwork(string fullPath, string realPath) =>
        RealPath.IsNetworkLocation(realPath) && !RealPath.IsNetworkLocation(fullPath);

    /// <summary>
    ///     Determines whether a folder offered as an allowed folder is in a Windows device namespace.
    /// </summary>
    /// <remarks>
    ///     An allowed folder confines every configuration-file path, so it must never be a device. The rule is
    ///     CanvasNet's for its allowed roots: on Windows, a full path in a device namespace (<c>\\.\</c>,
    ///     <c>\??\</c>, or <c>\\?\</c>) is refused unless it is an ordinary extended-length drive or volume path
    ///     (<c>\\?\C:\</c>, <c>\\?\Volume{GUID}\</c>). A pure text check, so the caller can apply it before the
    ///     folder is probed. Always false on Linux and macOS.
    /// </remarks>
    /// <param name="fullPath">The full path of the folder. Must not be null.</param>
    /// <returns>True when the folder must be refused as an allowed folder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fullPath"/> is null.</exception>
    public static bool IsDeviceNamespaceFolder(string fullPath)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(fullPath);

        return OperatingSystem.IsWindows()
            && RealPath.IsWindowsDeviceNamespace(fullPath)
            && !RealPath.IsOrdinaryExtendedPath(fullPath);
    }

    /// <summary>
    ///     Explains why a folder named in a configuration file as a base folder (the <c>root:</c> key) is never
    ///     accessed, or returns null when it may be used, and reports whether it exists.
    /// </summary>
    /// <remarks>
    ///     The <c>root:</c> key only sets the base folder of relative paths and never grants access, so the mode does
    ///     not apply to it, but its existence check must not reach a network location or a device. The written-text
    ///     rules apply to the expanded text and the full-path rules to the full path; then, on Windows, unless the
    ///     full path is itself on a network location, the path is walked for a symbolic link or junction whose
    ///     target is a network location (reading each link's own target). Finally the folder is opened once
    ///     (<see cref="RealPath.InspectFolder(string)"/>; on macOS with <c>O_DIRECTORY</c>), also when the full path
    ///     is itself on a network location: unless it is, its real path must not lead to a network location, and
    ///     whether the folder exists is decided from the facts of that open alone, never from a separate by-name
    ///     probe. A link or junction whose target is missing, or a file, is not an existing folder. Performs
    ///     file-system I/O.
    /// </remarks>
    /// <param name="expandedText">The folder text after <c>${VAR}</c> expansion. Must not be null.</param>
    /// <param name="fullPath">The full, normalized path of the folder. Must not be null.</param>
    /// <param name="exists">
    ///     Set to true when the folder was opened and is a folder; false when it is missing, is not a folder, cannot
    ///     be told, or a reason is returned.
    /// </param>
    /// <returns>The reason, or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static string? DescribeNeverAccessedFolder(string expandedText, string fullPath, out bool exists) =>
        DescribeNeverAccessedFolder(
            expandedText, fullPath, RealPath.ReadLinkTarget, RealPath.InspectFolder, out exists);

    /// <summary>
    ///     Explains why a base folder is never accessed, and reports whether it exists, with a given link reader and
    ///     folder inspector, so tests can simulate links that lead to a network location and the facts of the open.
    /// </summary>
    /// <param name="expandedText">The folder text after <c>${VAR}</c> expansion. Must not be null.</param>
    /// <param name="fullPath">The full, normalized path of the folder. Must not be null.</param>
    /// <param name="linkReader">The function that reads the immediate target of a link. Must not be null.</param>
    /// <param name="folderInspector">
    ///     The function that opens the folder once and reports its facts. Must not be null.
    /// </param>
    /// <param name="exists">Set to true when the facts show an existing folder and no reason is returned.</param>
    /// <returns>The reason, or null.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static string? DescribeNeverAccessedFolder(
        string expandedText,
        string fullPath,
        Func<string, string?> linkReader,
        Func<string, PathFacts> folderInspector,
        out bool exists)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(expandedText);
        ArgumentNullException.ThrowIfNull(fullPath);
        ArgumentNullException.ThrowIfNull(linkReader);
        ArgumentNullException.ThrowIfNull(folderInspector);

        // The text and full-path rules come first, before anything touches the file system
        exists = false;
        var reason = DescribeNeverAccessed(expandedText) ?? DescribeNeverAccessedFullPath(fullPath);
        if (reason != null)
        {
            return reason;
        }

        // Then the links on the path, unless the path is itself on a network location (a relative root on a share)
        var onNetwork = RealPath.IsNetworkLocation(fullPath);
        if (!onNetwork && RealPath.FindNetworkLinkTarget(fullPath, linkReader) != null)
        {
            return NetworkLinkReason;
        }

        // Then one open of the folder: its real path is judged, and its facts alone decide whether it exists
        var facts = folderInspector(fullPath);
        if (!onNetwork && facts.FinalPath != null && LeadsToNetwork(fullPath, facts.FinalPath))
        {
            return NetworkLinkReason;
        }

        // A null folder flag (Linux where fstat cannot be read) is accepted, as an existing entry of unknown type
        exists = facts.State == PathState.Exists && facts.IsDirectory != false;
        return null;
    }

    /// <summary>
    ///     Judges an input pattern of a configuration-file job and resolves it to the files it names, judging every
    ///     folder before it is listed.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The written-text rules apply to the expanded pattern and the full-path rules to the rooted pattern,
    ///         in every mode. A literal (wildcard-free) pattern names one file: it is judged by
    ///         <see cref="CheckPath(JudgedPath)"/> and returned without touching the file system (the read decides
    ///         whether it exists).
    ///     </para>
    ///     <para>
    ///         A glob is split into its literal folder and its wildcard remainder. Before the folder is listed it
    ///         is judged by <see cref="CheckPath(JudgedPath)"/> (so under <see cref="FileAccessMode.Roots"/> it must
    ///         be inside an allowed folder as written) and then by its handle: on Windows a local folder whose real
    ///         path is on a network location is denied in every mode; under roots its real path must be inside an
    ///         allowed folder; under <see cref="FileAccessMode.NoSymlinks"/> no link may lie below the trusted
    ///         anchor. A folder that does not exist, or is not a folder, gives no matches. While matching, every
    ///         folder that is a symbolic link or junction is judged the same way before a wildcard descends into it,
    ///         and a denial fails the pattern; a link folder whose target is missing is not listed. Each match is
    ///         returned with the pattern's literal part followed by its matched path as the text the written-text
    ///         rules apply to, and it is judged again, in full, when it is read.
    ///     </para>
    /// </remarks>
    /// <param name="writtenPattern">The pattern as written, used in the message. Must not be null.</param>
    /// <param name="expandedPattern">The pattern after <c>${VAR}</c> expansion. Must not be null.</param>
    /// <param name="rootedPattern">The expanded pattern rooted at the job's base folder. Must not be null.</param>
    /// <returns>The judged path of the literal file, or of every matched file (empty when nothing matched).</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when the literal part of the pattern is not a valid path, or the globbing library rejects it.
    /// </exception>
    /// <exception cref="FileAccessDeniedException">Thrown when the pattern or a folder it lists is denied.</exception>
    /// <exception cref="IOException">Thrown when a folder cannot be listed.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the system denies listing a folder.</exception>
    public IReadOnlyList<JudgedPath> ResolvePattern(string writtenPattern, string expandedPattern, string rootedPattern)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(writtenPattern);
        ArgumentNullException.ThrowIfNull(expandedPattern);
        ArgumentNullException.ThrowIfNull(rootedPattern);

        // The never-accessed list applies to the expanded text and to the rooted path, in every mode
        var reason = DescribeNeverAccessed(expandedPattern) ?? DescribeNeverAccessedFullPath(rootedPattern);
        if (reason != null)
        {
            throw new FileAccessDeniedException(writtenPattern, null, reason);
        }

        // A literal pattern names exactly one file, judged now and in full when it is read
        if (rootedPattern.IndexOfAny(Wildcards) < 0)
        {
            var literal = new JudgedPath(writtenPattern, expandedPattern, Path.GetFullPath(rootedPattern));
            CheckPath(literal);
            return [literal];
        }

        // A glob: judge the literal folder by its text and by its handle before it is listed
        var (folderText, remainder) = GlobMatcher.SplitGlob(rootedPattern);
        var folder = Path.GetFullPath(folderText);
        var prefixText = GlobMatcher.SplitGlob(expandedPattern).Folder;
        if (!CheckListedFolder(new JudgedPath(writtenPattern, expandedPattern, folder)))
        {
            return [];
        }

        // Match below the folder, judging every link folder before a wildcard descends into it
        var matches = GlobMatcher.MatchUnder(
            folder,
            remainder,
            link => CheckListedFolder(
                new JudgedPath(writtenPattern, JoinExpanded(prefixText, Path.GetRelativePath(folder, link)), link)));
        return [.. matches.Select(match => new JudgedPath(match.FullPath, JoinExpanded(prefixText, match.RelativePath), match.FullPath))];
    }

    /// <summary>
    ///     Reads a text file named in a configuration file, after judging it by the never-accessed list and the mode.
    /// </summary>
    /// <remarks>
    ///     The checks run in order: <see cref="CheckPath(JudgedPath)"/> (the text rules on the expanded text, the
    ///     device rules on the full path, on Windows a symbolic link or junction at any folder of a local path whose
    ///     target is a network location, and under <see cref="FileAccessMode.Roots"/> the full path as written), all
    ///     before anything is opened; then the open (non-blocking on Linux and macOS; a folder opens too, so it is
    ///     told by its handle, never by a by-name probe); then the facts of the open handle: a regular file, not a
    ///     local path whose real path is on a network location, and in a restrictive mode a known real path that
    ///     satisfies the mode. The content is read from the same handle that was judged, decoded as UTF-8 with
    ///     byte-order-mark detection like <see cref="File.ReadAllText(string)"/>.
    /// </remarks>
    /// <param name="path">The judged path of the file.</param>
    /// <returns>The file content.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a form of <paramref name="path"/> is null.</exception>
    /// <exception cref="FileAccessDeniedException">Thrown when the file is denied.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when a folder of the path does not exist.</exception>
    /// <exception cref="IOException">Thrown when the file cannot be opened or read.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the system denies access to the file.</exception>
    public string ReadAllText(JudgedPath path)
    {
        // Judge every form of the path before anything is opened
        CheckPath(path);
        var anchor = CaptureAnchor(path.Shown, path.FullPath);

        // Open the file, judge the handle, and read from the same handle
        using var handle = RealPath.OpenForRead(path.FullPath);
        CheckHandle(path.Shown, path.FullPath, handle, anchor);
        using var stream = new FileStream(handle, FileAccess.Read, bufferSize: 1);
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return reader.ReadToEnd();
    }

    /// <summary>
    ///     Writes a text file named in a configuration file, after judging it by the never-accessed list and the
    ///     mode, creating missing parent folders.
    /// </summary>
    /// <remarks>
    ///     Nothing is created or written until the pre-checks pass: <see cref="CheckPath(JudgedPath)"/>, on Linux and
    ///     macOS an output under <c>/dev</c>, then the deepest existing parent folder, found and judged by handles
    ///     (on Linux and macOS a real path under <c>/dev</c>, because opening a device node for writing can act on
    ///     the device before its handle can be judged; on Windows a local folder that leads to a network location;
    ///     in a restrictive mode its real path), and an existing output, judged by its handle (a folder, on Linux and
    ///     macOS a real path under <c>/dev</c>, and its real path). The missing folders are then created one at a
    ///     time, each verified by its real path when its parent's real path is known. An existing output is opened
    ///     without truncation and a missing one is created exclusively (which never follows a link at the output
    ///     name); the handle is judged, and only then is the file truncated and written as UTF-8 without a
    ///     byte-order mark, like <see cref="File.WriteAllText(string, string)"/>. A folder swapped by another process
    ///     between its check and the exclusive create can leave an empty file at the swapped location, but no
    ///     content is written.
    /// </remarks>
    /// <param name="path">The judged path of the output.</param>
    /// <param name="content">The text to write. Must not be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="FileAccessDeniedException">Thrown when the output, or a folder it needs, is denied.</exception>
    /// <exception cref="IOException">Thrown when a folder or the file cannot be created, opened or written.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the system denies access.</exception>
    public void WriteAllText(JudgedPath path, string content)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(content);

        // Judge every form of the path before anything is touched
        CheckPath(path);
        var shown = path.Shown;
        var fullPath = path.FullPath;
        if (IsUnixDevicePath(fullPath))
        {
            throw new FileAccessDeniedException(shown, null, DeviceReason);
        }

        // Find the deepest existing parent folder by handles, and judge it before anything is created
        var anchor = CaptureAnchor(shown, fullPath);
        var parent = Path.GetDirectoryName(fullPath);
        var (existing, canonicalParent, missing) = FindExistingFolder(parent);
        CheckFolder(shown, fullPath, existing ?? parent ?? fullPath, canonicalParent, anchor, forWrite: true);

        // Judge an existing output entry, which may be a link, before it is opened
        var exists = missing.Count == 0 && CheckExistingOutput(shown, fullPath, anchor);

        // Create the missing folders one at a time, verifying each
        CreateFolders(shown, missing, canonicalParent);

        // Open without truncating (or create exclusively), judge the handle, then truncate and write
        using var handle = exists
            ? RealPath.TryOpenExistingForWrite(fullPath) ?? throw new FileAccessDeniedException(shown, null, NotRegularReason)
            : RealPath.CreateNewForWrite(fullPath);
        CheckHandle(shown, fullPath, handle, anchor);
        RandomAccess.SetLength(handle, 0);
        RandomAccess.Write(handle, Utf8NoBom.GetBytes(content), 0);
    }

    /// <summary>
    ///     Judges every form of a path before anything touches it: the never-accessed list (the text rules on the
    ///     expanded text and the device rules on the full path), a link on a local path whose target is a network
    ///     location, and, under <see cref="FileAccessMode.Roots"/>, the full path as written inside an allowed folder.
    /// </summary>
    /// <remarks>
    ///     Link-safe: only the targets of links on the path are read (see
    ///     <see cref="RealPath.FindNetworkLinkTarget(string, Func{string, string?})"/>); nothing is opened, probed or
    ///     listed. The denial names <see cref="JudgedPath.Shown"/>.
    /// </remarks>
    /// <param name="path">The judged path.</param>
    /// <exception cref="ArgumentNullException">Thrown when a form of <paramref name="path"/> is null.</exception>
    /// <exception cref="FileAccessDeniedException">Thrown when the path is denied.</exception>
    internal void CheckPath(JudgedPath path)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(path.Shown);
        ArgumentNullException.ThrowIfNull(path.Expanded);
        ArgumentNullException.ThrowIfNull(path.FullPath);

        // The never-accessed list, in every mode: text rules on the expanded text, device rules on the full path
        var reason = DescribeNeverAccessed(path.Expanded) ?? DescribeNeverAccessedFullPath(path.FullPath);
        if (reason != null)
        {
            throw new FileAccessDeniedException(path.Shown, null, reason);
        }

        // A link anywhere on a local path that leads to a network location, before anything contacts it
        CheckNetworkLink(path.Shown, path.FullPath);

        // The full path as written must be inside an allowed folder
        if (Mode.HasFlag(FileAccessMode.Roots) && !IsWrittenInside(path.FullPath))
        {
            throw new FileAccessDeniedException(path.Shown, null, RootsWrittenReason);
        }
    }

    /// <summary>Gets the reason for a path written outside the allowed folders.</summary>
    private string RootsWrittenReason =>
        $"the path as written is outside the allowed folders (--file-access {ModeName})";

    /// <summary>Gets the reason for a real path outside the allowed folders.</summary>
    private string RootsRealReason =>
        $"its real path is outside the allowed folders, so it leads out through a symbolic link or junction (--file-access {ModeName})";

    /// <summary>Gets the reason for a real path that differs from the path as written.</summary>
    private string NoSymlinksReason =>
        $"its real path differs from the path as written, so the path passes through a symbolic link or junction (--file-access {ModeName})";

    /// <summary>Gets the reason for a real path that cannot be determined.</summary>
    private string RealPathUnknownReason =>
        $"the real path of the opened file cannot be determined on this system, so --file-access {ModeName} cannot be applied";

    /// <summary>
    ///     Captures, before the open, the trusted anchor of a path and its real location for the
    ///     <see cref="FileAccessMode.NoSymlinks"/> rule.
    /// </summary>
    /// <remarks>
    ///     The anchor is the deepest folder the path shares with the trusted base folder (or the root of the path
    ///     when there is no trusted base). It and everything above it are trusted, so links there (for example
    ///     macOS <c>/var</c>) do not count. Capturing before the open means an anchor replaced by a link afterwards
    ///     leads the open elsewhere, and the real path then no longer matches.
    /// </remarks>
    /// <param name="writtenPath">The path as written.</param>
    /// <param name="fullPath">The full path.</param>
    /// <returns>The anchor snapshot, or null when the mode does not include no-symlinks.</returns>
    /// <exception cref="FileAccessDeniedException">Thrown when the anchor's real location cannot be determined.</exception>
    private AnchorSnapshot? CaptureAnchor(string writtenPath, string fullPath)
    {
        if (!Mode.HasFlag(FileAccessMode.NoSymlinks))
        {
            return null;
        }

        var anchor = (TrustedBase == null ? null : RealPath.CommonAncestor(fullPath, TrustedBase))
            ?? Path.GetPathRoot(fullPath)
            ?? fullPath;
        var canonical = RealPath.TryGetCanonicalDirectory(anchor)
            ?? throw new FileAccessDeniedException(writtenPath, null, FolderUnknownReason(anchor));
        return new AnchorSnapshot(anchor, canonical);
    }

    /// <summary>
    ///     Judges a folder by its real location: the deepest existing parent folder of an output, or a folder about to
    ///     be listed.
    /// </summary>
    /// <param name="shown">The path or pattern shown in the message.</param>
    /// <param name="fullPath">The full output path, or the folder itself when it is listed.</param>
    /// <param name="folder">The folder (for an output, the output path when no folder exists).</param>
    /// <param name="canonicalFolder">The real location of the folder, or null when it is unknown.</param>
    /// <param name="anchor">The no-symlinks anchor, or null.</param>
    /// <param name="forWrite">True for an output's folder, to which the Linux and macOS <c>/dev</c> rule applies.</param>
    /// <exception cref="FileAccessDeniedException">Thrown when the folder is denied.</exception>
    private void CheckFolder(string shown, string fullPath, string folder, string? canonicalFolder, AnchorSnapshot? anchor, bool forWrite)
    {
        // The real path the output (or the listed folder) would have, for the message
        string? realTarget = null;
        if (canonicalFolder != null)
        {
            realTarget = string.Equals(folder, fullPath, StringComparison.Ordinal)
                ? canonicalFolder
                : Path.Join(canonicalFolder, Path.GetRelativePath(folder, fullPath));
        }

        // On Linux and macOS a folder whose real location is in /dev holds devices, in every mode, for writes
        if (forWrite && canonicalFolder != null && IsUnixDevicePath(canonicalFolder))
        {
            throw new FileAccessDeniedException(shown, realTarget, DeviceReason);
        }

        // A local folder that leads to a network location is never accessed, in every mode
        if (canonicalFolder != null && LeadsToNetwork(folder, canonicalFolder))
        {
            throw new FileAccessDeniedException(shown, realTarget, NetworkLinkReason);
        }

        if (Mode == FileAccessMode.None)
        {
            return;
        }

        // A restrictive mode needs the real location of the folder
        if (canonicalFolder == null)
        {
            throw new FileAccessDeniedException(shown, null, FolderUnknownReason(folder));
        }

        if (Mode.HasFlag(FileAccessMode.Roots)
            && (!IsWrittenInside(folder) || !Array.Exists(_folders, allowed => allowed.ContainsRealPath(canonicalFolder))))
        {
            throw new FileAccessDeniedException(
                shown,
                realTarget,
                $"its folder '{folder}' leads out of the allowed folders through a symbolic link or junction (--file-access {ModeName})");
        }

        if (anchor is { } snapshot && !MatchesWithoutLinks(snapshot, folder, canonicalFolder))
        {
            throw new FileAccessDeniedException(
                shown,
                realTarget,
                $"its folder '{folder}' passes through a symbolic link or junction (--file-access {ModeName})");
        }
    }

    /// <summary>
    ///     Judges an existing output entry by its handle before it is opened for writing.
    /// </summary>
    /// <remarks>
    ///     The entry is opened without access rights, which follows any link, so its facts are those of the file a
    ///     write would reach. A folder is never written. On Linux and macOS a real path under <c>/dev</c> (a link to
    ///     a device) is denied in every mode. The real path is judged (a local path that leads to a network location
    ///     in every mode, the mode's rules in a restrictive mode), so a link leading out is denied before the file is
    ///     opened; the open handle is judged again afterwards. A link whose target does not exist is denied in a
    ///     restrictive mode (its real path cannot be checked); in mode any the exclusive create then fails because
    ///     the name is taken.
    /// </remarks>
    /// <param name="shown">The output path as written.</param>
    /// <param name="fullPath">The full output path.</param>
    /// <param name="anchor">The no-symlinks anchor, or null.</param>
    /// <returns>True when an entry may exist at the output path, so it is opened rather than created.</returns>
    /// <exception cref="FileAccessDeniedException">Thrown when the existing entry is denied.</exception>
    private bool CheckExistingOutput(string shown, string fullPath, AnchorSnapshot? anchor)
    {
        var entry = RealPath.InspectPath(fullPath);
        if (entry.State == PathState.Missing)
        {
            if (Mode != FileAccessMode.None && RealPath.IsLink(fullPath))
            {
                throw new FileAccessDeniedException(
                    shown,
                    null,
                    $"it is a symbolic link whose target does not exist, so its real path cannot be checked (--file-access {ModeName})");
            }

            return false;
        }

        if (entry.IsDirectory == true)
        {
            throw new FileAccessDeniedException(shown, null, NotRegularReason);
        }

        if (entry.FinalPath != null && IsUnixDevicePath(entry.FinalPath))
        {
            throw new FileAccessDeniedException(shown, entry.FinalPath, DeviceReason);
        }

        CheckRealPath(shown, fullPath, entry.FinalPath, anchor);
        return true;
    }

    /// <summary>
    ///     Creates the missing parent folders of an output, one level at a time.
    /// </summary>
    /// <remarks>
    ///     Each folder is created in a parent that was already judged or verified. When the parent's real location
    ///     is known, the new folder's real location (from a handle) must be the parent's real location followed by
    ///     its name, which catches a folder swapped for a link while it was created. When it is unknown (mode any on
    ///     a platform that cannot tell), the folders are created without verification.
    /// </remarks>
    /// <param name="shown">The output path as written.</param>
    /// <param name="missing">The missing folders, outermost first.</param>
    /// <param name="canonicalParent">The real location of the deepest existing parent folder, or null.</param>
    /// <exception cref="FileAccessDeniedException">Thrown when a created folder is not where it should be.</exception>
    /// <exception cref="IOException">Thrown when a folder cannot be created.</exception>
    private void CreateFolders(string shown, List<string> missing, string? canonicalParent)
    {
        var canonical = canonicalParent;
        foreach (var folder in missing)
        {
            Directory.CreateDirectory(folder);
            if (canonical == null)
            {
                continue;
            }

            var created = RealPath.InspectPath(folder).FinalPath;
            if (created == null || !RealPath.PathsEqual(created, Path.Join(canonical, Path.GetFileName(folder))))
            {
                throw new FileAccessDeniedException(
                    shown,
                    created,
                    $"the folder '{folder}' changed while it was created, so --file-access {ModeName} cannot be applied");
            }

            canonical = created;
        }
    }

    /// <summary>
    ///     Judges a folder before it is listed: by every form of its path, then by its handle.
    /// </summary>
    /// <remarks>
    ///     Used for the literal folder of a glob and for every link folder a wildcard would descend into. The handle
    ///     check is that of an output's folder, except the write-only <c>/dev</c> rule: a local folder whose real
    ///     path is on a network location is denied in every mode, a restrictive mode needs its real path, under
    ///     roots the real path must be inside an allowed folder, and under no-symlinks no link may lie below the
    ///     trusted anchor of the folder.
    /// </remarks>
    /// <param name="path">The judged path of the folder, shown as the pattern as written.</param>
    /// <returns>True when the folder may be listed; false when it does not exist or is not a folder.</returns>
    /// <exception cref="FileAccessDeniedException">Thrown when the folder is denied.</exception>
    private bool CheckListedFolder(JudgedPath path)
    {
        CheckPath(path);
        var anchor = CaptureAnchor(path.Shown, path.FullPath);
        var facts = RealPath.InspectPath(path.FullPath);
        if (facts.State == PathState.Missing || facts.IsDirectory == false)
        {
            return false;
        }

        CheckFolder(path.Shown, path.FullPath, path.FullPath, facts.FinalPath, anchor, forWrite: false);
        return true;
    }

    /// <summary>
    ///     Finds the deepest existing folder of a path by handles, never by a by-name probe.
    /// </summary>
    /// <remarks>
    ///     Each level is opened without access rights from the deepest up. A level that exists but is not a folder
    ///     counts as missing (creating it then fails). A link whose target is missing counts as existing with an
    ///     unknown real path, as does a level whose facts cannot be told, so a restrictive mode denies it.
    /// </remarks>
    /// <param name="parent">The parent folder of the output, or null.</param>
    /// <returns>The deepest existing folder and its real path (each possibly null), and the missing folders.</returns>
    private static (string? Existing, string? Canonical, List<string> Missing) FindExistingFolder(string? parent)
    {
        var missing = new List<string>();
        var folder = parent;
        while (folder != null)
        {
            var facts = RealPath.InspectPath(folder);
            if (facts.State == PathState.Exists && facts.IsDirectory != false)
            {
                return (folder, facts.FinalPath, missing);
            }

            if (facts.State == PathState.Unknown || (facts.State == PathState.Missing && RealPath.IsLink(folder)))
            {
                return (folder, null, missing);
            }

            missing.Insert(0, folder);
            folder = Path.GetDirectoryName(folder);
        }

        return (null, null, missing);
    }

    /// <summary>
    ///     Joins the literal folder text of an expanded glob and a path below it into the text the written-text
    ///     rules apply to.
    /// </summary>
    /// <param name="prefix">The literal folder text (possibly empty).</param>
    /// <param name="relative">The path below the folder.</param>
    /// <returns>The joined text, with '/' separators below the folder.</returns>
    private static string JoinExpanded(string prefix, string relative)
    {
        var below = relative.Replace('\\', '/');
        if (prefix.Length == 0)
        {
            return below;
        }

        return prefix.EndsWith('/') || prefix.EndsWith('\\') ? prefix + below : prefix + "/" + below;
    }

    /// <summary>
    ///     Judges the facts of an open handle.
    /// </summary>
    /// <param name="writtenPath">The path as written.</param>
    /// <param name="fullPath">The full path that was opened.</param>
    /// <param name="handle">The open handle.</param>
    /// <param name="anchor">The no-symlinks anchor, or null.</param>
    /// <exception cref="FileAccessDeniedException">Thrown when the handle is denied.</exception>
    private void CheckHandle(string writtenPath, string fullPath, SafeFileHandle handle, AnchorSnapshot? anchor)
    {
        var facts = _inspector(handle);
        if (facts.IsRegularFile != true)
        {
            throw new FileAccessDeniedException(writtenPath, facts.FinalPath, NotRegularReason);
        }

        CheckRealPath(writtenPath, fullPath, facts.FinalPath, anchor);
    }

    /// <summary>
    ///     Judges a real path: never a local path that leads to a network location, and in a restrictive mode known
    ///     and satisfying the mode.
    /// </summary>
    /// <param name="writtenPath">The path as written.</param>
    /// <param name="fullPath">The full path as written.</param>
    /// <param name="realPath">The real path, or null when it is unknown.</param>
    /// <param name="anchor">The no-symlinks anchor, or null.</param>
    /// <exception cref="FileAccessDeniedException">Thrown when the real path is denied.</exception>
    private void CheckRealPath(string writtenPath, string fullPath, string? realPath, AnchorSnapshot? anchor)
    {
        if (realPath != null && LeadsToNetwork(fullPath, realPath))
        {
            throw new FileAccessDeniedException(writtenPath, realPath, NetworkLinkReason);
        }

        if (Mode == FileAccessMode.None)
        {
            return;
        }

        var real = realPath ?? throw new FileAccessDeniedException(writtenPath, null, RealPathUnknownReason);
        if (Mode.HasFlag(FileAccessMode.Roots) && !Array.Exists(_folders, folder => folder.ContainsRealPath(real)))
        {
            throw new FileAccessDeniedException(writtenPath, real, RootsRealReason);
        }

        if (anchor is { } snapshot && !MatchesWithoutLinks(snapshot, fullPath, real))
        {
            throw new FileAccessDeniedException(writtenPath, real, NoSymlinksReason);
        }
    }

    /// <summary>
    ///     Determines whether a full path as written lies inside an allowed folder, in the folder's given or real
    ///     spelling, also comparing the long spelling of a path written with short (8.3) Windows names.
    /// </summary>
    /// <param name="fullPath">The full path.</param>
    /// <returns>True when the path is inside an allowed folder.</returns>
    private bool IsWrittenInside(string fullPath)
    {
        if (Array.Exists(_folders, folder => folder.ContainsWrittenPath(fullPath)))
        {
            return true;
        }

        var expanded = RealPath.ExpandShortNames(fullPath);
        return !string.Equals(expanded, fullPath, StringComparison.Ordinal)
            && Array.Exists(_folders, folder => folder.ContainsWrittenPath(expanded));
    }

    /// <summary>
    ///     Gets the reason for a folder whose real location cannot be determined.
    /// </summary>
    /// <param name="folder">The folder.</param>
    /// <returns>The reason.</returns>
    private string FolderUnknownReason(string folder) =>
        $"the real path of the folder '{folder}' cannot be determined, so --file-access {ModeName} cannot be applied";

    /// <summary>
    ///     Determines whether the real path of a file is the real location of the anchor followed by the path as
    ///     written below it, so no link lies below the anchor.
    /// </summary>
    /// <remarks>
    ///     A path written with short (8.3) Windows names is compared in its long spelling too. The expansion keeps
    ///     one component per component, so the part below the anchor is the same number of trailing components of
    ///     the long spelling; a link still shows up as a different real path.
    /// </remarks>
    /// <param name="anchor">The anchor snapshot.</param>
    /// <param name="fullPath">The full path as written.</param>
    /// <param name="realPath">The real path.</param>
    /// <returns>True when no link lies below the anchor.</returns>
    private static bool MatchesWithoutLinks(AnchorSnapshot anchor, string fullPath, string realPath)
    {
        var relative = Path.GetRelativePath(anchor.Anchor, fullPath);
        if (RealPath.PathsEqual(Expected(anchor.Canonical, relative), realPath))
        {
            return true;
        }

        var longPath = RealPath.ExpandShortNames(fullPath);
        if (relative == "." || string.Equals(longPath, fullPath, StringComparison.Ordinal))
        {
            return false;
        }

        var count = relative.Split(ComponentSeparators, StringSplitOptions.RemoveEmptyEntries).Length;
        var longParts = longPath.Split(ComponentSeparators, StringSplitOptions.RemoveEmptyEntries);
        return longParts.Length >= count
            && RealPath.PathsEqual(
                Expected(anchor.Canonical, string.Join(Path.DirectorySeparatorChar, longParts[^count..])),
                realPath);
    }

    /// <summary>
    ///     Computes the real path a file must have when no link lies below the anchor.
    /// </summary>
    /// <param name="canonicalAnchor">The real location of the anchor.</param>
    /// <param name="relative">The path below the anchor (<c>.</c> for the anchor itself).</param>
    /// <returns>The expected real path.</returns>
    private static string Expected(string canonicalAnchor, string relative) =>
        relative == "." ? canonicalAnchor : Path.Join(canonicalAnchor, relative);

    /// <summary>
    ///     Determines whether a full output path, or a real path (of an existing output or of its deepest existing
    ///     folder), is the <c>/dev</c> tree on Linux and macOS.
    /// </summary>
    /// <remarks>
    ///     A write-only rule with no counterpart in CanvasNet, which never writes: a read of a device node is refused
    ///     by its handle type after a non-blocking open, but opening a device node for writing can already act on
    ///     the device, so an output there is denied before anything is opened. Always false on Windows, where
    ///     device names are judged by <see cref="DescribeNeverAccessedFullPath(string)"/>.
    /// </remarks>
    /// <param name="fullPath">The full path or real folder path.</param>
    /// <returns>True when the path is <c>/dev</c> or under it.</returns>
    private static bool IsUnixDevicePath(string fullPath) =>
        !OperatingSystem.IsWindows()
        && (fullPath == "/dev" || fullPath.StartsWith("/dev/", StringComparison.Ordinal));

    /// <summary>
    ///     Denies, on Windows, a local path that passes through a symbolic link or junction whose target is a
    ///     network location, before anything opens, tests or enumerates the path.
    /// </summary>
    /// <remarks>
    ///     A path that is itself on a network location (a share, or a mapped network drive) is accessed, as in
    ///     CanvasNet, so it is not walked. Otherwise every component of the path is checked, not only the last, and
    ///     a chain of links is followed by reading each link's target without opening through it (see
    ///     <see cref="RealPath.FindNetworkLinkTarget(string, Func{string, string?})"/>), so the share is never
    ///     contacted. Anything denied here would also be denied by the real path after the open (CanvasNet's only
    ///     check); the walk only moves the denial before any network contact, which matters because matching a glob
    ///     and creating an output folder touch the disk before any handle exists. Does nothing on other systems.
    /// </remarks>
    /// <param name="writtenPath">The path or pattern as written, used in the message.</param>
    /// <param name="fullPath">The full path to walk.</param>
    /// <exception cref="FileAccessDeniedException">Thrown when a link on the path leads to a network location.</exception>
    private void CheckNetworkLink(string writtenPath, string fullPath)
    {
        if (RealPath.IsNetworkLocation(fullPath))
        {
            return;
        }

        if (RealPath.FindNetworkLinkTarget(fullPath, _linkReader) is { } networkPath)
        {
            throw new FileAccessDeniedException(writtenPath, networkPath, NetworkLinkReason);
        }
    }

    /// <summary>
    ///     Determines whether a text starts with a drive and a separator (<c>C:/</c>).
    /// </summary>
    /// <param name="text">The text, with forward slashes.</param>
    /// <returns>True when it does.</returns>
    private static bool HasDrive(string text) =>
        text.Length >= 3 && char.IsAsciiLetter(text[0]) && text[1] == ':' && text[2] == '/';

    /// <summary>Matches a URL scheme and its authority marker: a letter, at least one more scheme character, then <c>://</c>.</summary>
    [GeneratedRegex("^([A-Za-z][A-Za-z0-9+.-]+)://")]
    private static partial Regex SchemeRegex();

    /// <summary>
    ///     The trusted anchor of a path and its real location, captured before the open.
    /// </summary>
    /// <param name="Anchor">The anchor folder as written.</param>
    /// <param name="Canonical">The real location of the anchor.</param>
    private readonly record struct AnchorSnapshot(string Anchor, string Canonical);

    /// <summary>
    ///     One allowed folder: its full path as given, and its real location captured when the policy was created.
    /// </summary>
    /// <param name="Lexical">The full path as given.</param>
    /// <param name="Canonical">The real location, or null when it could not be determined (it then allows nothing).</param>
    private sealed record AllowedFolder(string Lexical, string? Canonical)
    {
        /// <summary>
        ///     Determines whether a path as written lies inside the folder, in its given or its real spelling.
        /// </summary>
        /// <param name="path">The full path.</param>
        /// <returns>True when it is inside.</returns>
        public bool ContainsWrittenPath(string path) =>
            RealPath.IsUnder(path, Lexical) || (Canonical is { } canonical && RealPath.IsUnder(path, canonical));

        /// <summary>
        ///     Determines whether a real path lies inside the real location captured when the policy was created.
        /// </summary>
        /// <param name="path">The real path.</param>
        /// <returns>True when it is inside.</returns>
        public bool ContainsRealPath(string path) => Canonical is { } canonical && RealPath.IsUnder(path, canonical);
    }
}

/// <summary>
///     The three forms of a path judged by the file-access policy.
/// </summary>
/// <remarks>
///     One value carries every form, so each rule is applied to the form it is defined on and the message names the
///     path as the user wrote it.
/// </remarks>
/// <param name="Shown">
///     The text shown in a denial: the path as written (with <c>${VAR}</c>); for a file a glob matched, its full path.
/// </param>
/// <param name="Expanded">
///     The text the written-text rules apply to: the path after <c>${VAR}</c> expansion; for a file a glob matched, the
///     pattern's literal part followed by the matched path.
/// </param>
/// <param name="FullPath">
///     The full, normalized path the full-path, network-link, roots and real-path rules apply to, and that is opened.
/// </param>
internal readonly record struct JudgedPath(string Shown, string Expanded, string FullPath);
