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

namespace DemaConsulting.DictionaryMark.Files;

/// <summary>
///     Path handling of <see cref="FileReferenceResolver"/>: the text rules that apply under every policy, and the
///     candidate a reference is looked up at.
/// </summary>
/// <remarks>
///     <para>
///         <b>Text rules.</b> Backslashes are first turned into forward slashes, so a reference means the same on every
///         platform. Refused before anything is touched: an empty or white-space reference, a control character
///         (including NUL, DEL and the C1 range), a device path (<c>\\?\</c>, <c>\\.\</c>), a network path (UNC
///         <c>\\server\share</c> and <c>//host</c>), and a colon anywhere except in the drive of an absolute Windows
///         path (<c>C:/x</c>): an alternate data stream, a drive-relative path and, on Linux and macOS, a drive path.
///         <c>..</c> and absolute paths are allowed; whether the file they reach may be read is the policy's decision.
///     </para>
///     <para>
///         <b>File-system candidates</b> are full paths with <c>.</c> and <c>..</c> collapsed and links not resolved.
///         On Windows a path with a reserved device name in any component (<c>NUL</c>, <c>CON</c>, <c>COM1</c>,
///         <c>NUL.txt</c>, ...) is refused before the open as well.
///     </para>
/// </remarks>
internal sealed partial class FileReferenceResolver
{
    /// <summary>The full path of the base directory, or <see langword="null"/>; computed on first use.</summary>
    private string? _baseDirectory;

    /// <summary>Whether <see cref="_baseDirectory"/> was computed; the configured directory is resolved on first use.</summary>
    private bool _directoriesComputed;

    /// <summary>
    ///     A place a referenced file may be found.
    /// </summary>
    /// <param name="Key">The full path as written.</param>
    /// <param name="Origin">The host-chosen directory the lookup chain started from: the base directory.</param>
    private sealed record Candidate(string Key, string Origin);

    /// <summary>
    ///     Gets the reference as it may be shown in a message: control characters replaced by <c>?</c>.
    /// </summary>
    /// <param name="reference">The reference as written.</param>
    /// <returns>The printable reference.</returns>
    private static string Shown(string reference) => FileReferenceException.Printable(reference);

    /// <summary>
    ///     Validates and normalizes the text of a reference.
    /// </summary>
    /// <param name="raw">The reference as written.</param>
    /// <param name="shown">The printable reference.</param>
    /// <returns>The text with backslashes turned into forward slashes.</returns>
    /// <exception cref="FileReferenceException">Thrown when the text breaks a rule.</exception>
    private static string ValidateText(string raw, string shown)
    {
        var problem = DescribeInvalidText(raw);
        return problem is { } broken
            ? throw new FileReferenceException(broken.Kind, shown, broken.Reason)
            : raw.Replace('\\', '/');
    }

    /// <summary>
    ///     Explains why the text of a reference is not acceptable.
    /// </summary>
    /// <param name="raw">The reference as written.</param>
    /// <returns>The broken rule, or <see langword="null"/> when the text is acceptable.</returns>
    private static (FileReferenceFailureKind Kind, string Reason)? DescribeInvalidText(string raw)
    {
        // The checks run from the most basic to the most specific, so the reason names the first rule that is broken
        if (string.IsNullOrWhiteSpace(raw))
        {
            return (FileReferenceFailureKind.EmptyPath, "the path is empty");
        }

        if (raw.Any(char.IsControl))
        {
            return (FileReferenceFailureKind.ControlCharacter, "the path contains a control character");
        }

        var text = raw.Replace('\\', '/');
        if (text.StartsWith("//?/", StringComparison.Ordinal) || text.StartsWith("//./", StringComparison.Ordinal)
            || (OperatingSystem.IsWindows() && RealPath.IsWindowsDeviceNamespace(text)))
        {
            return (FileReferenceFailureKind.InvalidPath, "device paths are not permitted");
        }

        if (text.StartsWith("//", StringComparison.Ordinal))
        {
            return (FileReferenceFailureKind.NetworkPath, "network paths are not permitted");
        }

        // The only colon a local path may carry is the drive of an absolute Windows path; any other is an alternate data
        // stream, a drive-relative path, or a drive on a system that has none
        var allowedColon = OperatingSystem.IsWindows() && HasDrive(text) ? 1 : -1;
        return text.IndexOf(':', allowedColon + 1) >= 0
            ? (FileReferenceFailureKind.InvalidPath, "the path contains a colon")
            : null;
    }

    /// <summary>
    ///     Determines whether a text starts with a drive and a separator (<c>C:/</c>).
    /// </summary>
    /// <param name="text">The text, with forward slashes.</param>
    /// <returns><see langword="true"/> when it does.</returns>
    private static bool HasDrive(string text) =>
        text.Length >= 3 && char.IsAsciiLetter(text[0]) && text[1] == ':' && text[2] == '/';

    /// <summary>
    ///     Generates the candidates of a reference in search order.
    /// </summary>
    /// <param name="text">The validated text.</param>
    /// <param name="shown">The printable reference.</param>
    /// <returns>The candidates.</returns>
    private IEnumerable<Candidate> Candidates(string text, string shown)
    {
        EnsureDirectories();

        // An absolute reference names one file; it is resolved against the starting directory only for its drive. Its
        // chain starts at the base directory
        if (Path.IsPathRooted(text))
        {
            yield return FullCandidate(_baseDirectory!, _baseDirectory!, text, shown);
            yield break;
        }

        if (_baseDirectory is not null)
        {
            yield return FullCandidate(_baseDirectory, _baseDirectory, text, shown);
        }
    }

    /// <summary>
    ///     Combines a directory and a reference into a file-system candidate.
    /// </summary>
    /// <param name="directory">The full path of the directory.</param>
    /// <param name="origin">The host-chosen directory the lookup chain started from.</param>
    /// <param name="text">The validated text.</param>
    /// <param name="shown">The printable reference.</param>
    /// <returns>The candidate.</returns>
    /// <exception cref="FileReferenceException">Thrown when the result is not a valid path or names a device.</exception>
    private static Candidate FullCandidate(string directory, string origin, string text, string shown)
    {
        string full;
        try
        {
            full = Path.GetFullPath(text, directory);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new FileReferenceException(FileReferenceFailureKind.InvalidPath, shown, "the path is not a valid path", ex);
        }

        // Windows maps a reserved device name in any component (NUL, CON, COM1, LPT1, NUL.txt, ...) to a device, either
        // by rewriting the path (\\.\NUL) or when it is opened; such a path is refused before the open, under every
        // policy, as is any path in a device namespace. A host that configured an ordinary extended-length directory
        // (\\?\C:\docs or \\?\Volume{GUID}\docs) keeps working: a path that stays under the same such root has no
        // device names mapped. Any other extended-length root (\\?\GLOBALROOT\...) gets no exemption, and an absolute
        // reference (C:/x/NUL) leaves the root and is judged like any other
        if (OperatingSystem.IsWindows() && !IsUnderOrdinaryExtendedRoot(full, directory)
            && (RealPath.IsWindowsDeviceNamespace(full) || RealPath.HasWindowsDeviceName(full)))
        {
            throw new FileReferenceException(FileReferenceFailureKind.InvalidPath, shown, "the path names a device");
        }

        return new Candidate(full, origin);
    }

    /// <summary>
    ///     Determines whether a full path is an ordinary extended-length path (a drive or a volume GUID, see
    ///     <see cref="RealPath.IsOrdinaryExtendedPath"/>) with the same root as the ordinary extended-length directory it
    ///     was resolved against, so the system mapped no device name in it. A <c>\\.\</c> path, or any other
    ///     extended-length root such as <c>\\?\GLOBALROOT\</c>, is never under such a root.
    /// </summary>
    /// <param name="full">The full path of the candidate.</param>
    /// <param name="directory">The full path of the directory it was resolved against.</param>
    /// <returns><see langword="true"/> when it is.</returns>
    private static bool IsUnderOrdinaryExtendedRoot(string full, string directory) =>
        RealPath.IsOrdinaryExtendedPath(full)
        && RealPath.IsOrdinaryExtendedPath(directory)
        && string.Equals(Path.GetPathRoot(full), Path.GetPathRoot(directory), RealPath.PathComparison);

    /// <summary>
    ///     Computes the configured directory on first use, so a resolver that is never asked for a file never touches
    ///     the path options.
    /// </summary>
    /// <exception cref="FileReferenceException">Thrown when the configured directory is not a valid path.</exception>
    private void EnsureDirectories()
    {
        if (_directoriesComputed)
        {
            return;
        }

        _baseDirectory = _options.BaseDirectory is null ? null : FullDirectory(_options.BaseDirectory);
        _directoriesComputed = true;
    }

    /// <summary>
    ///     Computes the full path of a configured directory.
    /// </summary>
    /// <param name="directory">The configured directory.</param>
    /// <returns>The full path.</returns>
    /// <exception cref="FileReferenceException">Thrown when the directory is not a valid path.</exception>
    private static string FullDirectory(string directory)
    {
        try
        {
            return Path.GetFullPath(directory);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new FileReferenceException(FileReferenceFailureKind.InvalidBaseDirectory, directory ?? string.Empty, null, ex);
        }
    }
}
