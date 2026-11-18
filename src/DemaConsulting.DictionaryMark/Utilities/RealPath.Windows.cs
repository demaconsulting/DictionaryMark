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
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace DemaConsulting.DictionaryMark.Utilities;

/// <summary>
///     The Windows facts of an open handle and the Windows path classification.
/// </summary>
internal static partial class RealPath
{
    /// <summary>The <c>GetFileType</c> result of a file on a disk (as opposed to a character device or a pipe).</summary>
    private const uint WindowsFileTypeDisk = 1;

    /// <summary>The <c>GetFinalPathNameByHandleW</c> flags <c>FILE_NAME_NORMALIZED | VOLUME_NAME_DOS</c> (both zero).</summary>
    private const uint WindowsFinalPathNormalizedDos = 0;

    /// <summary>The <c>CreateFileW</c> share mode <c>FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE</c>.</summary>
    private const uint WindowsShareAll = 7;

    /// <summary>The <c>CreateFileW</c> disposition <c>OPEN_EXISTING</c>.</summary>
    private const uint WindowsOpenExisting = 3;

    /// <summary>The <c>CreateFileW</c> flag <c>FILE_FLAG_BACKUP_SEMANTICS</c>, needed to open a folder.</summary>
    private const uint WindowsBackupSemantics = 0x02000000;

    /// <summary>The <c>CreateFileW</c> access right <c>GENERIC_READ</c>.</summary>
    private const uint WindowsGenericRead = 0x80000000;

    /// <summary>The Windows error <c>ERROR_FILE_NOT_FOUND</c>.</summary>
    private const int WindowsErrorFileNotFound = 2;

    /// <summary>The Windows error <c>ERROR_PATH_NOT_FOUND</c>.</summary>
    private const int WindowsErrorPathNotFound = 3;

    /// <summary>The Windows error <c>ERROR_ACCESS_DENIED</c>.</summary>
    private const int WindowsErrorAccessDenied = 5;

    /// <summary>The Windows error 206: the file name or extension is too long.</summary>
    private const int WindowsErrorFileNameTooLong = 206;

    /// <summary>The bits that turn a Win32 error into the error code an exception carries (0x8007xxxx).</summary>
    private const int WindowsHResultBase = unchecked((int)0x80070000);

    /// <summary>
    ///     The length from which a path gets the extended-length prefix: the classic folder limit (248 characters),
    ///     which is below the file limit (260), as .NET uses for folders.
    /// </summary>
    private const int WindowsLongPathThreshold = 248;

    /// <summary>The first size of a path buffer, in characters; it grows when Windows asks for more.</summary>
    private const int WindowsInitialPathBuffer = 512;

    /// <summary>The largest path Windows can report, in characters (including the terminating NUL).</summary>
    private const int WindowsMaxPathBuffer = 32768;

    /// <summary>The prefix Windows puts in front of a final path (<c>\\?\</c>).</summary>
    private const string WindowsExtendedPrefix = @"\\?\";

    /// <summary>The prefix Windows puts in front of the final path of a network file (<c>\\?\UNC\</c>).</summary>
    private const string WindowsExtendedUncPrefix = @"\\?\UNC\";

    /// <summary>The NT object-namespace prefix a link target may carry (<c>\??\</c>).</summary>
    private const string WindowsNtPrefix = @"\??\";

    /// <summary>The NT object-namespace prefix of a network link target (<c>\??\UNC\</c>).</summary>
    private const string WindowsNtUncPrefix = @"\??\UNC\";

    /// <summary>
    ///     The most reparse points (symbolic links and junctions) Windows follows when it opens one path; an open that
    ///     needs more fails.
    /// </summary>
    private const int WindowsMaxLinkHops = 63;

    /// <summary>
    ///     Finds, without following any link, whether a path passes through a symbolic link or junction whose target
    ///     is a network location on Windows.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The components of the path are examined from its root. At each component the immediate link target is
    ///         read, which examines the entry itself and never its target. When a component is a link, its target is
    ///         normalized (<c>\??\UNC\x</c> becomes <c>\\x</c> and any other <c>\??\</c> becomes <c>\\?\</c>) and a
    ///         relative or rooted target is resolved against the link's folder. A target on a network location ends
    ///         the walk; any other target replaces the components up to the link, and the walk starts again on the
    ///         new path. A component is therefore never read through a link that has not been checked, and a chain
    ///         of links is followed by its targets.
    ///     </para>
    ///     <para>
    ///         The walk stops after <see cref="WindowsMaxLinkHops"/> links, which also ends a link loop: Windows will
    ///         not open a path that needs more, so every link the open could follow has been checked. A missing
    ///         component is not a link. Always null on other systems. Never throws: a target that is not a valid
    ///         path ends the walk with null, and the real path of the opened file still decides.
    ///     </para>
    /// </remarks>
    /// <param name="fullPath">The full path. Must not be null.</param>
    /// <param name="readLinkTarget">
    ///     The function that reads the immediate link target of an entry, or returns null when it is not a link. Must
    ///     not be null and must not throw.
    /// </param>
    /// <returns>
    ///     The path the links lead to (the network target followed by the components after the link), or null when
    ///     no link on the path leads to a network location.
    /// </returns>
    internal static string? FindNetworkLinkTarget(string fullPath, Func<string, string?> readLinkTarget)
    {
        if (!OperatingSystem.IsWindows())
        {
            return null;
        }

        try
        {
            var path = fullPath;
            var hops = 0;
            while (true)
            {
                // Find the first component of the path that is a link
                var root = Path.GetPathRoot(path);
                if (string.IsNullOrEmpty(root))
                {
                    return null;
                }

                var parts = path[root.Length..].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
                var prefix = root;
                string? target = null;
                var index = 0;
                for (; index < parts.Length; index++)
                {
                    prefix = Path.Join(prefix, parts[index]);
                    target = readLinkTarget(prefix);
                    if (target != null)
                    {
                        break;
                    }
                }

                // No link on the path, or more links than Windows follows in one open
                if (target == null || ++hops > WindowsMaxLinkHops)
                {
                    return null;
                }

                // Resolve the target against the link's folder and keep the components after the link
                var parent = Path.GetDirectoryName(prefix) ?? root;
                var resolved = Path.GetFullPath(NormalizeLinkTarget(target), parent);
                var rest = string.Join(Path.DirectorySeparatorChar, parts, index + 1, parts.Length - index - 1);
                path = rest.Length == 0 ? resolved : Path.Join(resolved, rest);
                if (IsNetworkLocation(resolved))
                {
                    return path;
                }
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // A target that is not a valid path ends the walk; the real path of the opened file still decides
            return null;
        }
    }

    /// <summary>
    ///     Brings a link target in NT form into its Win32 spelling.
    /// </summary>
    /// <param name="target">The link target as stored.</param>
    /// <returns><c>\\x</c> for <c>\??\UNC\x</c>, <c>\\?\x</c> for any other <c>\??\x</c>, or the target unchanged.</returns>
    private static string NormalizeLinkTarget(string target)
    {
        if (target.StartsWith(WindowsNtUncPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return @"\\" + target[WindowsNtUncPrefix.Length..];
        }

        return target.StartsWith(WindowsNtPrefix, StringComparison.Ordinal)
            ? WindowsExtendedPrefix + target[WindowsNtPrefix.Length..]
            : target;
    }

    /// <summary>
    ///     Determines whether a full path is a network location on Windows.
    /// </summary>
    /// <remarks>
    ///     A network location is a UNC path (<c>\\server\share</c> or <c>\\?\UNC\server\share</c>) or a path on a
    ///     mapped network drive. A device-namespace path (<c>\\.\</c>, <c>\\?\</c> other than UNC) and a local drive
    ///     are not. A drive that cannot be asked for its type is treated as local; the real path of the opened file
    ///     then decides, because Windows reports the real path of a file on a mapped drive as a UNC path. Always
    ///     false on other systems. Never throws.
    /// </remarks>
    /// <param name="fullPath">The full path. Must not be null.</param>
    /// <returns>True when the path is a network location.</returns>
    internal static bool IsNetworkLocation(string fullPath)
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        // An extended-length UNC path is a share; any other extended-length path is classified by what follows it
        if (fullPath.StartsWith(WindowsExtendedUncPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var path = fullPath.StartsWith(WindowsExtendedPrefix, StringComparison.Ordinal) ? fullPath[4..] : fullPath;
        if (path.StartsWith(@"\\", StringComparison.Ordinal) || path.StartsWith("//", StringComparison.Ordinal))
        {
            return path.Length > 2 && path[2] != '.' && path[2] != '?';
        }

        // A drive is asked for its type
        var root = Path.GetPathRoot(path);
        if (root is not { Length: 3 } || root[1] != ':')
        {
            return false;
        }

        try
        {
            return new DriveInfo(root).DriveType == DriveType.Network;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>
    ///     Reports the facts of an open Windows handle.
    /// </summary>
    /// <remarks>
    ///     A regular file is a disk file that is not a folder. A character device (<c>NUL</c>, a console) or a pipe
    ///     is not a disk file, and a folder handle is a disk file with the folder attribute; neither is read, so its
    ///     real path is not needed.
    /// </remarks>
    /// <param name="handle">The open handle.</param>
    /// <returns>The facts.</returns>
    [SupportedOSPlatform("windows")]
    private static HandleFacts InspectWindows(SafeFileHandle handle)
    {
        if (handle.IsInvalid || handle.IsClosed || GetFileTypeNative(handle) != WindowsFileTypeDisk
            || File.GetAttributes(handle).HasFlag(FileAttributes.Directory))
        {
            return new HandleFacts(false, null);
        }

        return new HandleFacts(true, TryGetFinalPathName(handle));
    }

    /// <summary>
    ///     Gets the final path of an open file or folder, with links, junctions, substituted and mapped drives and
    ///     short names resolved.
    /// </summary>
    /// <param name="handle">The open handle.</param>
    /// <returns>
    ///     The path without the <c>\\?\</c> prefix (a network path starts with <c>\\</c>), or null when Windows
    ///     cannot tell.
    /// </returns>
    [SupportedOSPlatform("windows")]
    private static string? TryGetFinalPathName(SafeFileHandle handle)
    {
        // The call returns the length without the NUL when the buffer is large enough, and the size it needs otherwise
        var buffer = new char[WindowsInitialPathBuffer];
        while (true)
        {
            var length = GetFinalPathNameByHandleNative(handle, buffer, (uint)buffer.Length, WindowsFinalPathNormalizedDos);
            if (length == 0 || length > WindowsMaxPathBuffer)
            {
                return null;
            }

            if (length < buffer.Length)
            {
                return StripExtendedPrefix(new string(buffer, 0, (int)length));
            }

            buffer = new char[length + 1];
        }
    }

    /// <summary>
    ///     Opens a path by name without access rights and reports its facts from the handle.
    /// </summary>
    /// <remarks>
    ///     Backup semantics let a folder open; no access rights are needed to read the attributes and the final path.
    ///     Error 2 (<c>ERROR_FILE_NOT_FOUND</c>) and 3 (<c>ERROR_PATH_NOT_FOUND</c>) mean the entry is missing,
    ///     including a link whose target is missing; any other failure leaves the facts unknown.
    /// </remarks>
    /// <param name="path">The full path.</param>
    /// <returns>The facts.</returns>
    [SupportedOSPlatform("windows")]
    private static PathFacts InspectPathWindows(string path)
    {
        using var handle = CreateFileNative(ToNativePath(path), 0, WindowsShareAll, 0, WindowsOpenExisting, WindowsBackupSemantics, 0);
        if (handle.IsInvalid)
        {
            var error = Marshal.GetLastPInvokeError();
            return new PathFacts(
                error is WindowsErrorFileNotFound or WindowsErrorPathNotFound ? PathState.Missing : PathState.Unknown,
                null,
                null);
        }

        var isDirectory = File.GetAttributes(handle).HasFlag(FileAttributes.Directory);
        return new PathFacts(PathState.Exists, isDirectory, TryGetFinalPathName(handle));
    }

    /// <summary>
    ///     Opens an existing file (or folder) for reading with full sharing, translating a failure into the exception
    ///     .NET itself raises for it.
    /// </summary>
    /// <remarks>
    ///     <c>File.OpenHandle</c> refuses a folder before any handle exists, so the open is native, with backup
    ///     semantics: a folder opens and its handle is then judged (and refused) like any other.
    /// </remarks>
    /// <param name="path">The full path.</param>
    /// <returns>The open handle; the caller disposes it.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when a folder of the path does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access is denied.</exception>
    /// <exception cref="PathTooLongException">Thrown when the path is too long.</exception>
    /// <exception cref="IOException">Thrown for any other failure.</exception>
    [SupportedOSPlatform("windows")]
    private static SafeFileHandle OpenForReadWindows(string path)
    {
        var handle = CreateFileNative(
            ToNativePath(path), WindowsGenericRead, WindowsShareAll, 0, WindowsOpenExisting, WindowsBackupSemantics, 0);
        if (!handle.IsInvalid)
        {
            return handle;
        }

        var error = Marshal.GetLastPInvokeError();
        handle.Dispose();
        var message = $"Could not open '{path}': {Marshal.GetPInvokeErrorMessage(error)}";
        throw error switch
        {
            WindowsErrorFileNotFound => new FileNotFoundException(message, path),
            WindowsErrorPathNotFound => new DirectoryNotFoundException(message),
            WindowsErrorAccessDenied => new UnauthorizedAccessException(message),
            WindowsErrorFileNameTooLong => new PathTooLongException(message),
            _ => new IOException(message, WindowsHResultBase | error),
        };
    }

    /// <summary>
    ///     Gives a full path the extended-length prefix when it is too long for the classic Win32 limit, as .NET does
    ///     internally before its own calls.
    /// </summary>
    /// <param name="path">The full path.</param>
    /// <returns>The path, with <c>\\?\</c> (or <c>\\?\UNC\</c> for a share) in front when it is long.</returns>
    private static string ToNativePath(string path)
    {
        if (path.Length < WindowsLongPathThreshold
            || path.StartsWith(WindowsExtendedPrefix, StringComparison.Ordinal)
            || path.StartsWith(@"\\.\", StringComparison.Ordinal))
        {
            return path;
        }

        // The prefix turns off path normalization, so the separators must already be backslashes
        var normalized = path.Replace('/', '\\');
        return normalized.StartsWith(@"\\", StringComparison.Ordinal)
            ? WindowsExtendedUncPrefix + normalized[2..]
            : WindowsExtendedPrefix + normalized;
    }

    /// <summary>
    ///     Gets the long spelling of a path whose components may be short (8.3) names.
    /// </summary>
    /// <param name="path">The full path of an existing file or folder.</param>
    /// <returns>The long spelling, or null when it cannot be determined (for example the path does not exist).</returns>
    [SupportedOSPlatform("windows")]
    private static string? TryGetLongPathName(string path)
    {
        // Same protocol as the final path: grow the buffer to the size the call asks for
        var buffer = new char[WindowsInitialPathBuffer];
        while (true)
        {
            var length = GetLongPathNameNative(path, buffer, (uint)buffer.Length);
            if (length == 0 || length > WindowsMaxPathBuffer)
            {
                return null;
            }

            if (length < buffer.Length)
            {
                return new string(buffer, 0, (int)length);
            }

            buffer = new char[length + 1];
        }
    }

    /// <summary>
    ///     Removes the <c>\\?\</c> prefix of a final path.
    /// </summary>
    /// <remarks>
    ///     <c>\\?\UNC\server\share</c> becomes <c>\\server\share</c> and <c>\\?\C:\x</c> becomes <c>C:\x</c>. Any
    ///     other spelling (a volume GUID or a device-namespace root, which the requested flags should never yield) is
    ///     no usable path, so it is reported as unknown and the caller fails closed.
    /// </remarks>
    /// <param name="path">The path as Windows reported it.</param>
    /// <returns>The path in its usual spelling, or null.</returns>
    private static string? StripExtendedPrefix(string path)
    {
        if (path.StartsWith(WindowsExtendedUncPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return @"\\" + path[WindowsExtendedUncPrefix.Length..];
        }

        var stripped = path.StartsWith(WindowsExtendedPrefix, StringComparison.Ordinal)
            ? path[WindowsExtendedPrefix.Length..]
            : path;
        return stripped.Length >= 3 && char.IsAsciiLetter(stripped[0]) && stripped[1] == ':' && stripped[2] == '\\'
            ? stripped
            : null;
    }
}
