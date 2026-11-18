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

namespace DemaConsulting.DictionaryMark.Files;

/// <summary>
///     The facts of an open handle that a file-access decision needs.
/// </summary>
/// <param name="IsRegularFile">
///     <see langword="true"/> for a regular file, <see langword="false"/> for anything else (a directory, a device, a
///     named pipe), or <see langword="null"/> when the platform cannot tell.
/// </param>
/// <param name="FinalPath">The final path of the open file, or <see langword="null"/> when the platform cannot tell.</param>
internal readonly record struct HandleFacts(bool? IsRegularFile, string? FinalPath);

/// <summary>
///     Opens files and reports the facts of an open handle per operating system: whether it is a regular file and its
///     real (final) path, plus the path comparison rules shared by the resolvers and the policies.
/// </summary>
/// <remarks>
///     <para>
///         <b>Why it exists.</b> A decision about a file is only free of races when it is made about the file that will
///         be read: the real path must come from the <b>open handle</b>, not from the name, which another process can
///         change between a check and the open. .NET offers no API for this (its link APIs work by name), so this unit
///         asks the operating system: Windows <c>GetFinalPathNameByHandleW</c>, Linux <c>/proc/self/fd</c>, macOS
///         <c>proc_pidfdinfo</c>. On any other platform the facts are unknown (<see langword="null"/>), and the
///         resolvers fail closed: a file of unknown type is never read, under any policy.
///     </para>
///     <para>
///         <b>Non-blocking open.</b> On Linux and macOS a file is opened with <c>O_NONBLOCK</c>, so opening a named pipe
///         that has no writer returns at once (the regular-file check then refuses it) instead of hanging, and with
///         <c>O_CLOEXEC</c>, so the descriptor never leaks into a child process. On Windows a file is opened read-only
///         with read, write and delete sharing, so another process can keep writing, renaming or deleting it.
///     </para>
///     <para>
///         All members are thread-safe. The only state is a benign cache in <see cref="NativeMethods"/> of which Linux
///         <c>stat</c> entry point the C library exports; a race merely repeats the probe.
///     </para>
/// </remarks>
internal static partial class RealPath
{
    /// <summary>The <c>S_IFMT</c> mask of the file type bits of a Unix mode.</summary>
    private const int UnixTypeMask = 0xF000;

    /// <summary>The <c>S_IFREG</c> file type of a regular file.</summary>
    private const int UnixRegularFile = 0x8000;

    /// <summary>The directory separators accepted when a path is split into components.</summary>
    private static readonly char[] Separators = [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    /// <summary>
    ///     Gets the comparer of every cache or set keyed by a path: ordinal on every platform.
    /// </summary>
    /// <remarks>
    ///     Whether a folder is case-sensitive cannot be told reliably on any platform, so two paths that differ only in
    ///     letter case may name two different files (<c>.../Allowed/a.txt</c> and <c>.../allowed/a.txt</c> on a
    ///     case-sensitive folder) and must never share a cache entry: one would reuse the other's bytes or the other's
    ///     captured folder location. On a case-insensitive folder the only cost is an extra entry for another spelling
    ///     of the same file, which is judged on its own.
    /// </remarks>
    internal static StringComparer KeyComparer { get; } = StringComparer.Ordinal;

    /// <summary>
    ///     Gets the comparison of the as-written pre-filter (<see cref="IsUnder(string, string)"/>) on this platform:
    ///     case-insensitive on Windows and macOS (their default file systems), ordinal elsewhere.
    /// </summary>
    /// <remarks>
    ///     Private so it cannot spread: it serves only the pre-filter that keeps a file clearly outside every root from
    ///     being opened. It never decides (the real path is compared exactly, see <see cref="IsUnderExactly"/>) and it
    ///     never keys a cache (see <see cref="KeyComparer"/>).
    /// </remarks>
    private static StringComparison PathComparison { get; } =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    ///     Determines whether two real paths are the same, comparing them exactly (ordinal) on every platform.
    /// </summary>
    /// <remarks>
    ///     Both paths must already be in the letter case stored on disk (see <see cref="GetExactPathName"/>); trailing
    ///     separators do not matter, and on macOS both are compared in Unicode composed form. Pure text; thread-safe.
    /// </remarks>
    /// <param name="left">A full path in stored case.</param>
    /// <param name="right">Another full path in stored case.</param>
    /// <returns><see langword="true"/> when they are the same.</returns>
    internal static bool PathsEqualExactly(string left, string right) => AreEqual(left, right, StringComparison.Ordinal);

    /// <summary>
    ///     Determines whether a full path as written is a root folder or lies below it under the as-written pre-filter
    ///     comparison (case-insensitive on Windows and macOS, ordinal elsewhere); the boundary is a directory separator,
    ///     so a sibling whose name merely starts with the root's name is not below it.
    /// </summary>
    /// <remarks>
    ///     A pre-filter only, and the one place the platform's default case rule is used: a path it accepts is still
    ///     judged by its real path with <see cref="IsUnderExactly"/>, so being loose here cannot widen a policy.
    /// </remarks>
    /// <param name="path">The full path.</param>
    /// <param name="root">The full path of the folder.</param>
    /// <returns><see langword="true"/> when <paramref name="path"/> is <paramref name="root"/> or below it.</returns>
    internal static bool IsUnder(string path, string root) => IsUnder(path, root, PathComparison);

    /// <summary>
    ///     Determines whether a real path is a root folder or lies below it, comparing exactly (ordinal) on every
    ///     platform; the boundary is a directory separator, so a sibling whose name merely starts with the root's name
    ///     is not below it.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Why it exists: whether a folder is case-sensitive cannot be told reliably (a Windows folder, a macOS
    ///         volume and a Linux folder can each be either), so a case-insensitive containment check would treat
    ///         <c>C:\work\ALLOWED\x</c> as inside the root <c>C:\work\allowed</c> on a case-sensitive folder, where
    ///         they are different folders. Comparing exactly is correct on both kinds of folder once both paths are in
    ///         the letter case stored on disk.
    ///     </para>
    ///     <para>
    ///         Both paths must already be in stored case (see <see cref="GetExactPathName"/>); trailing separators do
    ///         not matter, and on macOS both are compared in Unicode composed form. Pure text; thread-safe.
    ///     </para>
    /// </remarks>
    /// <param name="path">The full real path, in stored case.</param>
    /// <param name="root">The full real path of the folder, in stored case.</param>
    /// <returns><see langword="true"/> when <paramref name="path"/> is <paramref name="root"/> or below it.</returns>
    internal static bool IsUnderExactly(string path, string root) => IsUnder(path, root, StringComparison.Ordinal);

    /// <summary>
    ///     Gets the spelling of a path in the letter case stored on disk, so that real paths can be compared exactly.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Why it exists.</b> Real paths are compared exactly (<see cref="IsUnderExactly"/>), because a folder may
    ///         be case-sensitive on any platform. On a case-insensitive folder the same entry can be spelled in many
    ///         letter cases (a root given as <c>c:\Work\docs</c> for the folder <c>C:\work\Docs</c>), so each spelling is
    ///         first brought to the one the file system stores.
    ///     </para>
    ///     <para>
    ///         <b>Algorithm.</b> The path is made full, then each component is looked up in its parent, from the root
    ///         down: the parent is listed for the name ignoring case. An entry whose name equals the name exactly wins
    ///         (on a case-sensitive folder several entries can differ only in case, and a name with <c>*</c> or
    ///         <c>?</c> can match other entries as a pattern). Otherwise, a single entry equal to the name ignoring case
    ///         is used when the name as written also exists, which is how a case-insensitive folder resolves it; on a
    ///         case-sensitive folder the name as written does not exist and is kept. A missing component, and everything
    ///         below a missing folder, keeps its name as written, with the existing part of the path still normalized.
    ///         Only a drive letter of a root is changed (upper case); <c>/</c> and UNC roots (server and share names)
    ///         are kept as written.
    ///     </para>
    ///     <para>
    ///         The result differs from the full path only in letter case, so comparing two results exactly is never
    ///         looser than comparing the inputs ignoring case. Links are not resolved (each component is listed in its
    ///         parent by name). A parent that may not be listed (access denied) yields no entries, so the name below it is
    ///         kept as written.
    ///     </para>
    ///     <para>
    ///         <b>Side effects.</b> Reads the file system: lists the entries of each existing parent folder that match
    ///         the name, and asks whether a name exists. Nothing is created or changed. Stateless and thread-safe; the
    ///         answer reflects the file system at the time of the call.
    ///     </para>
    /// </remarks>
    /// <param name="pathName">A path, relative (taken from the current directory) or full. Must not be null or empty.</param>
    /// <returns>The full path, in stored case for every component that exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pathName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="pathName"/> is empty or not a valid path.</exception>
    /// <exception cref="IOException">
    ///     Thrown when a folder cannot be listed for a reason other than denied access (for example it vanished during
    ///     the call, or the path is too long).
    /// </exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access is refused in a way the listing cannot skip.</exception>
    internal static string GetExactPathName(string pathName)
    {
        var full = Path.GetFullPath(pathName);
        var info = new DirectoryInfo(full);

        // Root: normalize a drive letter only ("c:\" -> "C:\"); leave "/" and UNC roots as they are
        if (info.Parent is null)
        {
            return full.Length >= 2 && full[1] == ':' ? char.ToUpperInvariant(full[0]) + full[1..] : full;
        }

        var parent = GetExactPathName(info.Parent.FullName);
        var name = info.Name;

        // A missing entry keeps its name as written, with the existing part of the path still normalized
        if (!Directory.Exists(parent))
        {
            return Path.Combine(parent, name);
        }

        // List the parent ignoring case; more than one match means a case-sensitive folder,
        // or wildcard characters in the name, so the exact-case entry wins
        var options = new EnumerationOptions
        {
            MatchCasing = MatchCasing.CaseInsensitive,
            MatchType = MatchType.Simple,
            IgnoreInaccessible = true,
            AttributesToSkip = 0,
        };
        var matches = new DirectoryInfo(parent).GetFileSystemInfos(name, options)
            .Where(e => string.Equals(e.Name, name, StringComparison.OrdinalIgnoreCase))
            .ToList();
        var exact = matches.Find(e => string.Equals(e.Name, name, StringComparison.Ordinal));

        // A single match in another case is the same entry only when the name as written exists too (a case-insensitive
        // folder); on a case-sensitive folder the name as written is missing and keeps its spelling
        var stored = exact?.Name
            ?? (matches.Count == 1 && Path.Exists(Path.Combine(parent, name)) ? matches[0].Name : name);

        return Path.Combine(parent, stored);
    }

    /// <summary>
    ///     Determines whether any component of a path is a name Windows reserves for a device, so that opening the path
    ///     could reach the device instead of a file.
    /// </summary>
    /// <remarks>
    ///     A pure text check, so it gives the same answer on every platform; the resolvers apply it on Windows only. The
    ///     root of the path is not a component.
    /// </remarks>
    /// <param name="path">The path, with forward or backward slashes.</param>
    /// <returns><see langword="true"/> when a component is a reserved device name.</returns>
    internal static bool HasWindowsDeviceName(string path)
    {
        var root = Path.IsPathRooted(path) ? Path.GetPathRoot(path) ?? string.Empty : string.Empty;
        return path[root.Length..]
            .Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries)
            .Any(IsWindowsDeviceName);
    }

    /// <summary>
    ///     Determines whether one path component is a name Windows reserves for a device: <c>CON</c>, <c>PRN</c>,
    ///     <c>AUX</c>, <c>NUL</c>, <c>COM0</c>-<c>COM9</c>, <c>LPT0</c>-<c>LPT9</c>, <c>COM</c> or <c>LPT</c> with a
    ///     superscript 1, 2 or 3, <c>CONIN$</c> and <c>CONOUT$</c>, in any letter case. Windows ignores what follows the
    ///     name, so an extension (<c>NUL.txt</c>), trailing spaces or dots (<c>NUL .</c>) and a stream suffix
    ///     (<c>NUL:</c>) still name the device.
    /// </summary>
    /// <param name="component">The path component.</param>
    /// <returns><see langword="true"/> when it is a reserved device name.</returns>
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
    ///     Determines whether a full path is in a Windows device namespace: the Win32 device namespace (<c>\\.\</c>), an
    ///     extended-length path (<c>\\?\</c>), or the NT object namespace (<c>\??\</c>), with either slash.
    /// </summary>
    /// <remarks>A pure text check; the resolvers apply it on Windows only.</remarks>
    /// <param name="path">The full path.</param>
    /// <returns><see langword="true"/> when it is.</returns>
    internal static bool IsWindowsDeviceNamespace(string path) =>
        path.Length >= 4
        && path[0] is '\\' or '/'
        && path[1] is '\\' or '/' or '?'
        && path[2] is '.' or '?'
        && path[3] is '\\' or '/';

    /// <summary>
    ///     Determines whether a full path is an ordinary extended-length path: <c>\\?\</c> followed by a drive
    ///     (<c>\\?\C:\docs</c>) or a volume GUID (<c>\\?\Volume{GUID}\docs</c>). These name a file on a local volume with
    ///     no device names mapped; every other extended-length or device-namespace root (<c>\\?\GLOBALROOT\</c>,
    ///     <c>\\?\UNC\</c>, <c>\\?\pipe\</c>, <c>\\.\</c>, <c>\??\</c>) is not ordinary.
    /// </summary>
    /// <remarks>A pure text check, the one rule both resolvers use to exempt an extended-length directory.</remarks>
    /// <param name="path">The full path.</param>
    /// <returns><see langword="true"/> when it is an ordinary extended-length path.</returns>
    internal static bool IsOrdinaryExtendedPath(string path)
    {
        if (!path.StartsWith(@"\\?\", StringComparison.Ordinal))
        {
            return false;
        }

        // A drive: \\?\X: alone or followed by a backslash
        var rest = path.AsSpan(4);
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
    ///     Gets the deepest folder that two full paths share, comparing whole components exactly (ordinal) on every
    ///     platform.
    /// </summary>
    /// <remarks>
    ///     The result is trusted by <c>no-symlinks</c> (a link in it is not looked for), so it must never be a folder
    ///     the second path does not name: on a case-sensitive folder <c>/work/allowed</c> is not <c>/work/Allowed</c>.
    ///     On a case-insensitive folder a component spelled in another case only makes the shared folder shallower, so
    ///     more of the path is checked, never less.
    /// </remarks>
    /// <param name="first">A full path.</param>
    /// <param name="second">Another full path.</param>
    /// <returns>The shared folder, or <see langword="null"/> when the paths are on different roots (Windows drives).</returns>
    internal static string? CommonAncestor(string first, string second)
    {
        var root = Path.GetPathRoot(first);
        if (string.IsNullOrEmpty(root) || !string.Equals(root, Path.GetPathRoot(second), StringComparison.Ordinal))
        {
            return null;
        }

        var firstParts = first[root.Length..].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        var secondParts = second[root.Length..].Split(Separators, StringSplitOptions.RemoveEmptyEntries);
        var count = 0;
        while (count < firstParts.Length && count < secondParts.Length
            && string.Equals(firstParts[count], secondParts[count], StringComparison.Ordinal))
        {
            count++;
        }

        return count == 0 ? root : Path.Join(root, string.Join(Path.DirectorySeparatorChar, firstParts, 0, count));
    }

    /// <summary>
    ///     Opens a file for reading: read-only and shared on Windows, non-blocking and not inherited on Linux and macOS.
    /// </summary>
    /// <param name="fullPath">The full path of the file.</param>
    /// <returns>The open handle; the caller disposes it.</returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when a directory of the path does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access is denied (on Windows also for a directory).</exception>
    /// <exception cref="IOException">Thrown for any other failure.</exception>
    internal static SafeFileHandle OpenForRead(string fullPath)
    {
        if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
        {
            return NativeMethods.OpenNonBlocking(fullPath, pathOnly: false);
        }

        // Windows (and any other platform): the framework's open, read-only with the most permissive sharing
        return File.OpenHandle(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
    }

    /// <summary>
    ///     Reports whether an open handle is a regular file and what its final path is. Never throws.
    /// </summary>
    /// <param name="handle">The open handle.</param>
    /// <returns>The facts; each is <see langword="null"/> where the platform cannot tell.</returns>
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
                var (mode, path) = NativeMethods.TryGetMacVnodeInfo(handle);
                return new HandleFacts(IsRegularMode(mode), path);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException
            or DllNotFoundException or EntryPointNotFoundException or ObjectDisposedException)
        {
            // A platform call that is missing or fails leaves the facts unknown, and the caller decides (fail closed)
        }

        return default;
    }

    /// <summary>
    ///     Gets the real location of a folder (or file) by name: its final path once links, junctions and short names
    ///     are resolved. Used for folders the host chose (roots and anchors), which are trusted. A folder that may be
    ///     searched but not read (execute-only) has a real location too: Linux opens it with <c>O_PATH</c>, and macOS
    ///     asks <c>realpath(3)</c>, which resolves by name and needs only search permission.
    /// </summary>
    /// <param name="fullPath">The full path.</param>
    /// <returns>The final path, or <see langword="null"/> when it does not exist, cannot be opened, or the platform cannot tell.</returns>
    internal static string? TryGetCanonicalDirectory(string fullPath)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                return NativeMethods.TryGetFinalPathOfPath(fullPath);
            }

            if (OperatingSystem.IsLinux())
            {
                using var handle = NativeMethods.OpenNonBlocking(fullPath, pathOnly: true);
                return Inspect(handle).FinalPath;
            }

            if (OperatingSystem.IsMacOS())
            {
                return NativeMethods.TryRealPath(fullPath);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ExternalException
            or DllNotFoundException or EntryPointNotFoundException or ArgumentException)
        {
            // A folder that cannot be opened has no known real location
        }

        return null;
    }

    /// <summary>
    ///     Expands the short (8.3) names of an existing path into their long spelling (Windows); elsewhere, or when the
    ///     path cannot be expanded, returns it unchanged. Links are not resolved.
    /// </summary>
    /// <param name="fullPath">The full path.</param>
    /// <returns>The long spelling, or <paramref name="fullPath"/>.</returns>
    internal static string ExpandShortNames(string fullPath)
    {
        try
        {
            return OperatingSystem.IsWindows() ? NativeMethods.TryGetLongPathName(fullPath) ?? fullPath : fullPath;
        }
        catch (Exception ex) when (ex is ExternalException or DllNotFoundException or EntryPointNotFoundException)
        {
            return fullPath;
        }
    }

    /// <summary>
    ///     Determines from a Unix mode whether it describes a regular file.
    /// </summary>
    /// <param name="mode">The mode, or <see langword="null"/> when unknown.</param>
    /// <returns>The answer, or <see langword="null"/> when the mode is unknown.</returns>
    private static bool? IsRegularMode(int? mode) => mode is { } value ? (value & UnixTypeMask) == UnixRegularFile : null;

    /// <summary>
    ///     Determines whether two full paths are the same under a comparison, ignoring trailing separators and (on
    ///     macOS) the Unicode form.
    /// </summary>
    /// <param name="left">A full path.</param>
    /// <param name="right">Another full path.</param>
    /// <param name="comparison">The comparison of the text.</param>
    /// <returns><see langword="true"/> when they are the same.</returns>
    private static bool AreEqual(string left, string right, StringComparison comparison) =>
        string.Equals(Normalize(TrimEnd(left)), Normalize(TrimEnd(right)), comparison);

    /// <summary>
    ///     Determines whether a full path is a root folder or lies below it under a comparison; the boundary is a
    ///     directory separator.
    /// </summary>
    /// <param name="path">The full path.</param>
    /// <param name="root">The full path of the folder.</param>
    /// <param name="comparison">The comparison of the text.</param>
    /// <returns><see langword="true"/> when <paramref name="path"/> is <paramref name="root"/> or below it.</returns>
    private static bool IsUnder(string path, string root, StringComparison comparison)
    {
        var normalizedPath = Normalize(path);
        var trimmedRoot = Normalize(TrimEnd(root));
        return string.Equals(TrimEnd(normalizedPath), trimmedRoot, comparison)
            || normalizedPath.StartsWith(trimmedRoot.TrimEnd(Separators) + Path.DirectorySeparatorChar, comparison);
    }

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
    ///     Brings a path into the composed Unicode form on macOS, whose file systems may hand back decomposed names;
    ///     elsewhere returns it unchanged.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <returns>The normalized path.</returns>
    private static string Normalize(string path) =>
        OperatingSystem.IsMacOS() && !path.IsNormalized(NormalizationForm.FormC) ? path.Normalize(NormalizationForm.FormC) : path;
}
