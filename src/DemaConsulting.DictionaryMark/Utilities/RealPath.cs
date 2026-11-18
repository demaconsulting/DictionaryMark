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

using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace DemaConsulting.DictionaryMark.Utilities;

/// <summary>
///     The facts about an open handle that a file-access decision needs.
/// </summary>
/// <param name="IsRegularFile">
///     True for a regular file, false for anything else (a folder, a device, a named pipe), or null when the
///     platform cannot tell.
/// </param>
/// <param name="FinalPath">The real (final) path of the open file, or null when the platform cannot tell.</param>
internal readonly record struct HandleFacts(bool? IsRegularFile, string? FinalPath);

/// <summary>
///     Whether a path names an existing entry, as told by an attempt to open it.
/// </summary>
internal enum PathState
{
    /// <summary>The entry does not exist (or a folder of its path does not), so the open found nothing.</summary>
    Missing,

    /// <summary>The entry exists: it was opened.</summary>
    Exists,

    /// <summary>The open failed for another reason, so it is not known whether the entry exists.</summary>
    Unknown,
}

/// <summary>
///     The facts about a path that a file-access decision needs before anything is read, written, listed or
///     created through it, taken from a handle (Windows, Linux) or <c>realpath(3)</c> (macOS) whose result is judged.
/// </summary>
/// <param name="State">Whether the entry exists.</param>
/// <param name="IsDirectory">
///     True for a folder, false for anything else, or null when the entry does not exist or the platform cannot
///     tell (always null on macOS, except from a folder check).
/// </param>
/// <param name="FinalPath">The real (final) path of the entry, or null when it is missing or cannot be told.</param>
internal readonly record struct PathFacts(PathState State, bool? IsDirectory, string? FinalPath);

/// <summary>
///     Opens files and reports, per operating system, whether an open handle is a regular file and what its real
///     path is, plus the path-comparison and path-classification rules the file-access policy relies on.
/// </summary>
/// <remarks>
///     <para>
///         This unit exists because a decision about a file is only free of races when it is made about the file
///         that will actually be read or written: the real path must come from the open handle, not from the name,
///         which another process can change between a check and the open. .NET offers no API for this (its link
///         APIs work by name), so this unit asks the operating system: Windows <c>GetFinalPathNameByHandleW</c>,
///         Linux <c>/proc/self/fd</c>, macOS <c>proc_pidfdinfo</c>. All native code of the tool lives in this unit
///         so that it can be reviewed and tested in isolation. The approach follows the DEMA Consulting CanvasNet
///         tool, whose native contracts are proven on the same platforms.
///     </para>
///     <para>
///         On Linux and macOS files are opened with <c>O_NONBLOCK</c>, so opening a named pipe (FIFO) without a
///         peer returns at once (the regular-file check then refuses it) instead of hanging, and with
///         <c>O_CLOEXEC</c>, so the descriptor never leaks into a child process. Where the platform cannot tell a
///         fact it is reported as unknown (null) and the caller fails closed.
///     </para>
///     <para>
///         All members are thread-safe. The only state is a benign cache of which Linux <c>stat</c> entry point the
///         C library exports; a race merely repeats the probe.
///     </para>
/// </remarks>
internal static partial class RealPath
{
    /// <summary>The <c>S_IFMT</c> mask of the file-type bits of a Unix mode.</summary>
    private const int UnixTypeMask = 0xF000;

    /// <summary>The <c>S_IFREG</c> file type of a regular file.</summary>
    private const int UnixRegularFile = 0x8000;

    /// <summary>The <c>S_IFDIR</c> file type of a folder.</summary>
    private const int UnixDirectory = 0x4000;

    /// <summary>The directory separators accepted when a path is split into components.</summary>
    private static readonly char[] Separators = ['/', '\\'];

    /// <summary>
    ///     Gets the comparison under which two full paths name the same file on this platform: case-insensitive on
    ///     Windows and macOS (their default file systems), ordinal elsewhere.
    /// </summary>
    internal static StringComparison PathComparison { get; } =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    /// <summary>
    ///     Determines whether two full paths name the same location under the platform comparison.
    /// </summary>
    /// <remarks>Trailing separators are ignored and macOS paths are compared in composed Unicode form.</remarks>
    /// <param name="left">A full path. Must not be null.</param>
    /// <param name="right">Another full path. Must not be null.</param>
    /// <returns>True when both paths name the same location.</returns>
    internal static bool PathsEqual(string left, string right) =>
        string.Equals(Normalize(TrimEnd(left)), Normalize(TrimEnd(right)), PathComparison);

    /// <summary>
    ///     Determines whether a full path is a folder or lies below it.
    /// </summary>
    /// <remarks>
    ///     The boundary is a directory separator, so a sibling whose name merely starts with the folder's name
    ///     (<c>/a/bc</c> against <c>/a/b</c>) is not below it.
    /// </remarks>
    /// <param name="path">The full path to test. Must not be null.</param>
    /// <param name="folder">The full path of the folder. Must not be null.</param>
    /// <returns>True when <paramref name="path"/> is <paramref name="folder"/> or lies below it.</returns>
    internal static bool IsUnder(string path, string folder)
    {
        var normalizedPath = Normalize(TrimEnd(path));
        var trimmedFolder = Normalize(TrimEnd(folder));
        return string.Equals(normalizedPath, trimmedFolder, PathComparison)
            || normalizedPath.StartsWith(trimmedFolder.TrimEnd(Separators) + Path.DirectorySeparatorChar, PathComparison)
            || normalizedPath.StartsWith(trimmedFolder.TrimEnd(Separators) + Path.AltDirectorySeparatorChar, PathComparison);
    }

    /// <summary>
    ///     Gets the deepest folder that two full paths share, comparing whole components.
    /// </summary>
    /// <param name="first">A full path. Must not be null.</param>
    /// <param name="second">Another full path. Must not be null.</param>
    /// <returns>The shared folder, or null when the paths are on different roots (for example Windows drives).</returns>
    internal static string? CommonAncestor(string first, string second)
    {
        var root = Path.GetPathRoot(first);
        if (string.IsNullOrEmpty(root) || !string.Equals(root, Path.GetPathRoot(second), PathComparison))
        {
            return null;
        }

        // Count the leading components the two paths share below the common root
        var firstParts = first[root.Length..].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        var secondParts = second[root.Length..].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        var count = 0;
        while (count < firstParts.Length && count < secondParts.Length
            && string.Equals(firstParts[count], secondParts[count], PathComparison))
        {
            count++;
        }

        return count == 0 ? root : Path.Join(root, string.Join(Path.DirectorySeparatorChar, firstParts, 0, count));
    }

    /// <summary>
    ///     Determines whether any component of a path is a name Windows reserves for a device.
    /// </summary>
    /// <remarks>A pure text check that gives the same answer on every platform; the root is not a component.</remarks>
    /// <param name="path">The path, with forward or backward slashes. Must not be null.</param>
    /// <returns>True when a component is a reserved device name.</returns>
    internal static bool HasWindowsDeviceName(string path)
    {
        var root = Path.IsPathRooted(path) ? Path.GetPathRoot(path) ?? string.Empty : string.Empty;
        return path[root.Length..]
            .Split(Separators, StringSplitOptions.RemoveEmptyEntries)
            .Any(IsWindowsDeviceName);
    }

    /// <summary>
    ///     Determines whether one path component is a name Windows reserves for a device.
    /// </summary>
    /// <remarks>
    ///     The reserved names are <c>CON</c>, <c>PRN</c>, <c>AUX</c>, <c>NUL</c>, <c>COM0</c>-<c>COM9</c>,
    ///     <c>LPT0</c>-<c>LPT9</c>, <c>COM</c> or <c>LPT</c> with a superscript 1, 2 or 3, <c>CONIN$</c> and
    ///     <c>CONOUT$</c>, in any letter case. Windows ignores what follows the name, so an extension
    ///     (<c>NUL.txt</c>), trailing spaces and a stream suffix (<c>NUL:</c>) still name the device.
    /// </remarks>
    /// <param name="component">The path component. Must not be null.</param>
    /// <returns>True when the component is a reserved device name.</returns>
    internal static bool IsWindowsDeviceName(string component)
    {
        // The name is what precedes a stream suffix and an extension, without trailing spaces
        var name = component;
        var colon = name.IndexOf(':', StringComparison.Ordinal);
        name = colon >= 0 ? name[..colon] : name;
        var dot = name.IndexOf('.', StringComparison.Ordinal);
        name = (dot >= 0 ? name[..dot] : name).TrimEnd(' ');
        return name.Length switch
        {
            3 => name.Equals("CON", StringComparison.OrdinalIgnoreCase)
                || name.Equals("PRN", StringComparison.OrdinalIgnoreCase)
                || name.Equals("AUX", StringComparison.OrdinalIgnoreCase)
                || name.Equals("NUL", StringComparison.OrdinalIgnoreCase),
            4 => (name.StartsWith("COM", StringComparison.OrdinalIgnoreCase)
                    || name.StartsWith("LPT", StringComparison.OrdinalIgnoreCase))
                && (char.IsAsciiDigit(name[3]) || name[3] is '\u00B9' or '\u00B2' or '\u00B3'),
            _ => name.Equals("CONIN$", StringComparison.OrdinalIgnoreCase)
                || name.Equals("CONOUT$", StringComparison.OrdinalIgnoreCase),
        };
    }

    /// <summary>
    ///     Determines whether a path is in a Windows device namespace: the Win32 device namespace (<c>\\.\</c>), an
    ///     extended-length path (<c>\\?\</c>), or the NT object namespace (<c>\??\</c>), with either slash.
    /// </summary>
    /// <remarks>A pure text check that gives the same answer on every platform.</remarks>
    /// <param name="path">The path. Must not be null.</param>
    /// <returns>True when the path is in a device namespace.</returns>
    internal static bool IsWindowsDeviceNamespace(string path) =>
        path.Length >= 4
        && path[0] is '\\' or '/'
        && path[1] is '\\' or '/' or '?'
        && path[2] is '.' or '?'
        && path[3] is '\\' or '/';

    /// <summary>
    ///     Determines whether a full path is an ordinary extended-length path: <c>\\?\</c> followed by a drive
    ///     (<c>\\?\C:\docs</c>) or a volume GUID (<c>\\?\Volume{GUID}\docs</c>).
    /// </summary>
    /// <remarks>
    ///     These name a file on a local volume with no device names mapped, so a base folder written that way is
    ///     trusted like an ordinary drive path; every other extended-length or device-namespace root
    ///     (<c>\\?\UNC\</c>, <c>\\?\pipe\</c>, another <c>\\?\</c> object path, <c>\\.\</c>, <c>\??\</c>) is not
    ///     ordinary. The same rule as CanvasNet. A pure text check that gives the same answer on every platform;
    ///     the callers apply it on Windows only.
    /// </remarks>
    /// <param name="path">The full path. Must not be null.</param>
    /// <returns>True when it is an ordinary extended-length path.</returns>
    internal static bool IsOrdinaryExtendedPath(string path)
    {
        if (!path.StartsWith(WindowsExtendedPrefix, StringComparison.Ordinal))
        {
            return false;
        }

        // A drive: \\?\X: alone or followed by a backslash
        var rest = path.AsSpan(WindowsExtendedPrefix.Length);
        if (rest.Length >= 2 && char.IsAsciiLetter(rest[0]) && rest[1] == ':')
        {
            return rest.Length == 2 || rest[2] == '\\';
        }

        // A volume: \\?\Volume{GUID} alone or followed by a backslash
        const string VolumePrefix = "Volume{";
        const int GuidLength = 36;
        var close = VolumePrefix.Length + GuidLength;
        return rest.StartsWith(VolumePrefix, StringComparison.OrdinalIgnoreCase)
            && rest.Length > close
            && rest[close] == '}'
            && Guid.TryParseExact(rest.Slice(VolumePrefix.Length, GuidLength), "D", out _)
            && (rest.Length == close + 1 || rest[close + 1] == '\\');
    }

    /// <summary>
    ///     Determines whether a path names a symbolic link or junction, including one whose target does not exist.
    /// </summary>
    /// <remarks>
    ///     A link-safe check: only the entry's own link data is read, so its target is never followed or
    ///     contacted. Used only to decide how to handle an entry before an open; the decisions that matter are
    ///     always made on the real path of a handle. Never throws: an entry that cannot be examined is reported as
    ///     not a link.
    /// </remarks>
    /// <param name="fullPath">The full path. Must not be null.</param>
    /// <returns>True when the entry is a symbolic link or junction.</returns>
    internal static bool IsLink(string fullPath) => ReadLinkTarget(fullPath) != null;

    /// <summary>
    ///     Reads the immediate target of a symbolic link or junction, without following it.
    /// </summary>
    /// <remarks>
    ///     Only the entry itself is examined (its reparse data on Windows, <c>readlink</c> elsewhere), so the target
    ///     is never contacted. The target is returned as the system stores it: it may be relative to the link's
    ///     folder, and on Windows it may carry the NT prefix <c>\??\</c>. Never throws: an entry that cannot be
    ///     examined, does not exist or is not a link is reported as null.
    /// </remarks>
    /// <param name="fullPath">The full path of the entry. Must not be null.</param>
    /// <returns>The immediate link target, or null when the entry is not a link.</returns>
    internal static string? ReadLinkTarget(string fullPath)
    {
        try
        {
            return new FileInfo(fullPath).LinkTarget;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Finds, without following any link, whether a path passes through a symbolic link or junction whose target
    ///     is a network location on Windows, reading the links from the file system.
    /// </summary>
    /// <param name="fullPath">The full path. Must not be null.</param>
    /// <returns>The path the links lead to on the network location, or null.</returns>
    internal static string? FindNetworkLinkTarget(string fullPath) => FindNetworkLinkTarget(fullPath, ReadLinkTarget);

    /// <summary>
    ///     Opens an existing file for reading: read-only with full sharing on Windows, non-blocking and not inherited
    ///     on Linux and macOS.
    /// </summary>
    /// <remarks>
    ///     A folder opens too (on Windows with backup semantics), so the caller learns that the entry is a folder
    ///     from the handle it judges, not from a by-name probe; <see cref="Inspect(SafeFileHandle)"/> then reports it
    ///     as not a regular file.
    /// </remarks>
    /// <param name="fullPath">The full path of the file. Must not be null.</param>
    /// <returns>The open handle; the caller disposes it.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when a folder of the path does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access is denied.</exception>
    /// <exception cref="IOException">Thrown for any other failure.</exception>
    internal static SafeFileHandle OpenForRead(string fullPath)
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return OpenNonBlocking(fullPath, UnixOpenKind.Read) ?? throw new IOException(
                $"The file '{fullPath}' is not a regular file and cannot be opened without blocking.");
        }

        if (OperatingSystem.IsWindows())
        {
            return OpenForReadWindows(fullPath);
        }

        // Any other platform: the framework's open, read-only with the most permissive sharing
        return File.OpenHandle(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    }

    /// <summary>
    ///     Opens an existing file for writing without truncating it and without creating it, so its real path can be
    ///     checked before any content changes.
    /// </summary>
    /// <remarks>
    ///     On Linux and macOS the open is non-blocking, so a named pipe with no reader is reported (as null) instead
    ///     of hanging the open, and a folder (which cannot be opened for writing) is reported as null too. On Windows
    ///     the file is opened for writing with read sharing.
    /// </remarks>
    /// <param name="fullPath">The full path of the file. Must not be null.</param>
    /// <returns>
    ///     The open handle, which the caller disposes; or null when the operating system reports that the entry is a
    ///     special file (a named pipe or device) that cannot be opened for writing without blocking, or a folder.
    /// </returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when a folder of the path does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access is denied.</exception>
    /// <exception cref="IOException">Thrown for any other failure.</exception>
    internal static SafeFileHandle? TryOpenExistingForWrite(string fullPath)
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return OpenNonBlocking(fullPath, UnixOpenKind.Write);
        }

        return File.OpenHandle(fullPath, FileMode.Open, FileAccess.Write, FileShare.Read);
    }

    /// <summary>
    ///     Creates a new file for writing, failing when any entry (including a symbolic link) already has the name.
    /// </summary>
    /// <remarks>
    ///     Exclusive creation (<c>CREATE_NEW</c> on Windows, <c>O_CREAT | O_EXCL</c> on Unix) never follows a link in
    ///     the last component, so a link planted at the output name cannot make the tool create a file elsewhere.
    /// </remarks>
    /// <param name="fullPath">The full path of the file. Must not be null.</param>
    /// <returns>The open handle; the caller disposes it.</returns>
    /// <exception cref="IOException">Thrown when an entry already exists or the file cannot be created.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access is denied.</exception>
    internal static SafeFileHandle CreateNewForWrite(string fullPath) =>
        File.OpenHandle(fullPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read);

    /// <summary>
    ///     Reports whether an open handle is a regular file and what its real path is.
    /// </summary>
    /// <remarks>
    ///     Never throws: a platform call that is missing or fails leaves the facts unknown (null), so the caller
    ///     can fail closed. Only the specific failures a native call can raise are caught.
    /// </remarks>
    /// <param name="handle">The open handle. Must not be null.</param>
    /// <returns>The facts; each is null where the platform cannot tell.</returns>
    internal static HandleFacts Inspect(SafeFileHandle handle)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return InspectWindows(handle);
            }

            if (OperatingSystem.IsLinux())
            {
                return InspectLinux(handle);
            }

            if (OperatingSystem.IsMacOS())
            {
                return InspectMac(handle);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException
            or DllNotFoundException or EntryPointNotFoundException or ObjectDisposedException)
        {
            // A platform call that is missing or fails leaves the facts unknown, and the caller fails closed
        }

        return default;
    }

    /// <summary>
    ///     Gets the real location of an existing folder (or file) by name: its final path once links, junctions and
    ///     short names are resolved.
    /// </summary>
    /// <remarks>
    ///     The final path of <see cref="InspectPath(string)"/>; used for the allowed folders. Never throws.
    /// </remarks>
    /// <param name="fullPath">The full path. Must not be null.</param>
    /// <returns>The real path, or null when it does not exist, cannot be opened, or the platform cannot tell.</returns>
    internal static string? TryGetCanonicalDirectory(string fullPath) => InspectPath(fullPath).FinalPath;

    /// <summary>
    ///     Reports whether a path exists, whether it is a folder and what its real path is, from a handle opened
    ///     without access rights (Windows, Linux) or <c>realpath(3)</c> (macOS), never from a by-name probe.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The open follows every link on the path, as any later open would, and the facts are those of the
    ///         entry the handle reaches, so a link swapped in before the open is seen by its real path. Windows opens
    ///         with no access rights and backup semantics (a folder opens too), adding the extended-length prefix to
    ///         a long path; Linux opens with <c>O_PATH</c> (a folder that may be searched but not read opens too)
    ///         and reads the type with <c>fstat</c>; macOS asks <c>realpath(3)</c>, which cannot tell a folder.
    ///     </para>
    ///     <para>
    ///         A missing entry or folder (Windows error 2 or 3, <c>ENOENT</c>, <c>ENOTDIR</c>) is
    ///         <see cref="PathState.Missing"/>, which includes a link whose target does not exist; any other
    ///         failure is <see cref="PathState.Unknown"/>, and the caller fails closed in a restrictive mode. Never
    ///         throws.
    ///     </para>
    /// </remarks>
    /// <param name="fullPath">The full path. Must not be null.</param>
    /// <returns>The facts.</returns>
    internal static PathFacts InspectPath(string fullPath)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return InspectPathWindows(fullPath);
            }

            if (OperatingSystem.IsLinux())
            {
                return InspectPathLinux(fullPath);
            }

            if (OperatingSystem.IsMacOS())
            {
                return InspectPathMac(fullPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException
            or DllNotFoundException or EntryPointNotFoundException or ArgumentException)
        {
            // A path that cannot be opened for a reason other than a missing entry has unknown facts
        }

        return new PathFacts(PathState.Unknown, null, null);
    }

    /// <summary>
    ///     Reports whether a path is an existing folder and what its real path is, from one open of the folder,
    ///     never from a by-name probe.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Windows and Linux are <see cref="InspectPath(string)"/>, whose handle already tells a folder. macOS
    ///         opens the path with <c>O_DIRECTORY</c>, because <c>realpath(3)</c> cannot tell a folder there: a
    ///         folder is <see cref="PathState.Exists"/> with <see cref="PathFacts.IsDirectory"/> true and the real
    ///         path of the descriptor; a file, a link whose target is missing or a missing folder is
    ///         <see cref="PathState.Missing"/>; a folder that cannot be read is <see cref="PathState.Unknown"/>.
    ///     </para>
    ///     <para>
    ///         The caller decides existence from these facts alone: the path exists as a folder when the state is
    ///         <see cref="PathState.Exists"/> and <see cref="PathFacts.IsDirectory"/> is not false. Never throws.
    ///     </para>
    /// </remarks>
    /// <param name="fullPath">The full path. Must not be null.</param>
    /// <returns>The facts.</returns>
    internal static PathFacts InspectFolder(string fullPath)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return InspectPath(fullPath);
        }

        try
        {
            return InspectFolderMac(fullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException
            or DllNotFoundException or EntryPointNotFoundException or ArgumentException)
        {
            // A folder that cannot be opened for a reason other than a missing entry has unknown facts
            return new PathFacts(PathState.Unknown, null, null);
        }
    }

    /// <summary>
    ///     Gets the spelling of a full path with short (8.3) Windows names expanded.
    /// </summary>
    /// <remarks>
    ///     Only a Windows path containing <c>~</c> can hold a short name, so every other path is returned unchanged
    ///     without a system call. A path that cannot be expanded (for example because it does not exist) is also
    ///     returned unchanged. Links are not resolved. Never throws.
    /// </remarks>
    /// <param name="fullPath">The full path. Must not be null.</param>
    /// <returns>The long spelling, or <paramref name="fullPath"/>.</returns>
    internal static string ExpandShortNames(string fullPath)
    {
        if (!OperatingSystem.IsWindows() || !fullPath.Contains('~', StringComparison.Ordinal))
        {
            return fullPath;
        }

        try
        {
            return TryGetLongPathName(fullPath) ?? fullPath;
        }
        catch (Exception ex) when (ex is ExternalException or DllNotFoundException or EntryPointNotFoundException)
        {
            return fullPath;
        }
    }

    /// <summary>
    ///     Determines from a Unix mode whether it describes a regular file.
    /// </summary>
    /// <param name="mode">The mode, or null when unknown.</param>
    /// <returns>The answer, or null when the mode is unknown.</returns>
    private static bool? IsRegularMode(int? mode) =>
        mode is { } value ? (value & UnixTypeMask) == UnixRegularFile : null;

    /// <summary>
    ///     Removes trailing directory separators, keeping a root such as <c>/</c> or <c>C:\</c> intact.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The trimmed path.</returns>
    private static string TrimEnd(string path)
    {
        var root = Path.GetPathRoot(path);
        var trimmed = path.TrimEnd(Separators);
        return root is not null && trimmed.Length < root.Length ? root : trimmed;
    }

    /// <summary>
    ///     Brings a path into composed Unicode form on macOS, whose file systems may report decomposed names;
    ///     elsewhere returns it unchanged.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The normalized path.</returns>
    private static string Normalize(string path) =>
        OperatingSystem.IsMacOS() && !path.IsNormalized(NormalizationForm.FormC)
            ? path.Normalize(NormalizationForm.FormC)
            : path;
}
