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

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace DemaConsulting.DictionaryMark.Files;

/// <summary>
///     Output-path checking of <see cref="FileReferenceResolver"/>: the checks a referenced file gets before it is
///     read, applied to a file a document names as an output before it is written.
/// </summary>
/// <remarks>
///     This part is a DictionaryMark addition. Every rule it applies is one the resolver applies to a read, in the same
///     order: the text rules, the Windows device names, the pre-check of the path as written, the snapshot of the
///     trusted folders, the open and inspection of an existing file, the network-target rule, the fail-closed real-path
///     rule and the policy. The only new logic is the real path of an output that does not exist yet: the real
///     location of its deepest existing folder followed by the missing part of the path.
/// </remarks>
internal sealed partial class FileReferenceResolver
{
    /// <summary>
    ///     Checks that a document may write a file at a path, without creating, writing or reading anything.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Why it exists.</b> A configuration file names outputs as well as inputs. An output must be held to
    ///         the same rules as an input, so a document cannot write outside the folders the host allowed, through a
    ///         link the policy refuses, to a device or to a network location. The check runs before any folder is
    ///         created, so nothing is written when it fails.
    ///     </para>
    ///     <para>
    ///         <b>Existing output.</b> When a directory entry exists at the path (a file, a directory, a device, or a
    ///         link, including a dangling link), it is opened for reading and inspected on the handle, exactly like a
    ///         read: something that is not a regular file is refused, and an entry that cannot be opened is refused.
    ///         Its real path is the real path of the open handle.
    ///     </para>
    ///     <para>
    ///         <b>Output that does not exist yet.</b> When no directory entry exists at the path, its real path is the
    ///         real location of the deepest existing folder above it, followed by the rest of the path as written.
    ///     </para>
    ///     <para>
    ///         The output is never cached and never read, and the size limit does not apply. The check is not free of
    ///         races: the write that follows goes by name.
    ///     </para>
    /// </remarks>
    /// <param name="path">The full path of the output as resolved by the caller.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the resolver has no base directory.</exception>
    /// <exception cref="FileReferenceException">
    ///     Thrown when the path is never allowed, the base directory is invalid, the policy denies the path (or
    ///     cannot be applied), an existing entry at the path is not a regular file or cannot be opened, or the path
    ///     leads to a network location.
    /// </exception>
    internal void CheckOutputPath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        // The text rules apply before anything is touched, exactly as for a read
        var shown = Shown(path);
        var text = ValidateText(path, shown);
        if (!HasSource)
        {
            throw new InvalidOperationException("An output path is checked against the base directory, which is not configured.");
        }

        // The full path as written, with Windows device names refused, and the pre-check of a restrictive policy
        var candidate = Candidates(text, shown).First();
        var key = candidate.Key;
        if (_policy.PreCheck(key) is { IsAllowed: false } early)
        {
            throw Denied(shown, key, null, early, null);
        }

        // The trusted folders are captured before the open, as for a read
        var snapshot = CaptureBeforeOpen(candidate);
        _hooks?.BeforeOpen?.Invoke(key);
        SafeFileHandle? handle;
        try
        {
            handle = _hooks?.Open?.Invoke(key) ?? RealPath.OpenForRead(key);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException && !IsLinkEntry(key))
        {
            // No directory entry exists at the path: the output does not exist yet
            handle = null;
        }
        catch (UnauthorizedAccessException ex) when (Directory.Exists(key))
        {
            // Windows refuses to open a directory as a file; it exists, but it is not a file
            throw RefuseOutput(FileReferenceFailureKind.NotARegularFile, null, ex, shown, key);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Any other failure to open an existing entry (a dangling link included) refuses the output
            throw RefuseOutput(FileReferenceFailureKind.CannotRead, null, ex, shown, key);
        }

        // The real path comes from the handle of an existing output, or from the deepest existing folder otherwise
        string? realPath;
        if (handle is not null)
        {
            realPath = InspectExistingOutput(handle, shown, key);
        }
        else
        {
            realPath = RealPathOfMissing(key);

            // A local path that leads to a network share is refused under every policy, as for a read
            if (OperatingSystem.IsWindows() && realPath is not null
                && RealPath.IsNetworkLocation(realPath) && !RealPath.IsNetworkLocation(key))
            {
                throw RefuseOutput(FileReferenceFailureKind.NetworkPath, realPath, null, shown, key);
            }
        }

        // A restrictive policy is never consulted without the real path: unknown means refused (fail closed)
        if (_policy.RequiresRealPath)
        {
            if (realPath is null)
            {
                throw RefuseOutput(FileReferenceFailureKind.RealPathUnavailable, null, null, shown, key);
            }

            CheckPolicy(candidate, shown, realPath, snapshot);
        }
    }

    /// <summary>
    ///     Inspects the open handle of an existing output: its real path, and whether it is a regular file.
    /// </summary>
    /// <param name="handle">The open handle; disposed here.</param>
    /// <param name="shown">The path as shown in messages.</param>
    /// <param name="key">The full path as written.</param>
    /// <returns>The real path of the handle, or <see langword="null"/> when the platform cannot tell.</returns>
    /// <exception cref="FileReferenceException">
    ///     Thrown when the handle is not known to be a regular file, or, on Windows, a local path leads to a network
    ///     location.
    /// </exception>
    private string? InspectExistingOutput(SafeFileHandle handle, string shown, string key)
    {
        using (handle)
        {
            // The facts come from the handle, never from the name, as for a read
            _hooks?.AfterOpen?.Invoke(key);
            var facts = RealPath.Inspect(handle);
            var realPath = _hooks?.RealPathOf is { } realPathOf ? realPathOf(handle) : facts.FinalPath;

            // The network-target rule runs before the regular-file rule, as for a read
            if (OperatingSystem.IsWindows() && realPath is not null
                && RealPath.IsNetworkLocation(realPath) && !RealPath.IsNetworkLocation(key))
            {
                throw RefuseOutput(FileReferenceFailureKind.NetworkPath, realPath, null, shown, key);
            }

            // Only a handle known to be a regular file may be overwritten (fail closed)
            var isRegularFile = _hooks?.IsRegularFileOf is { } isRegularFileOf ? isRegularFileOf(handle) : facts.IsRegularFile;
            if (isRegularFile != true)
            {
                throw RefuseOutput(FileReferenceFailureKind.NotARegularFile, realPath, null, shown, key);
            }

            return realPath;
        }
    }

    /// <summary>
    ///     Computes the real path of an output that does not exist yet: the real location of its deepest existing
    ///     folder, followed by the rest of the path as written.
    /// </summary>
    /// <param name="key">The full path as written.</param>
    /// <returns>The real path, or <see langword="null"/> when no folder above it exists or its location is unknown.</returns>
    private string? RealPathOfMissing(string key)
    {
        // Walk up to the deepest folder that exists; the folders below it are created by the write
        var directory = Path.GetDirectoryName(key);
        while (directory is not null && !Directory.Exists(directory))
        {
            directory = Path.GetDirectoryName(directory);
        }

        if (directory is null)
        {
            return null;
        }

        // The real location of that folder, followed by the part of the path that does not exist yet
        var location = _hooks?.DirectoryLocationOf is { } locationOf
            ? locationOf(directory)
            : RealPath.TryGetCanonicalDirectory(directory);
        return location is null ? null : Path.Join(location, Path.GetRelativePath(directory, key));
    }

    /// <summary>
    ///     Determines whether the directory entry at a path is a link, including a dangling link, so that an output
    ///     whose open reports "not found" is only treated as missing when no entry exists at all.
    /// </summary>
    /// <param name="key">The full path as written.</param>
    /// <returns>
    ///     <see langword="true"/> when the entry is a link, or when that cannot be determined (fail closed);
    ///     <see langword="false"/> when no link exists at the path.
    /// </returns>
    private static bool IsLinkEntry(string key)
    {
        try
        {
            return new FileInfo(key).LinkTarget is not null;
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // Nothing exists at the path, so it is no link
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            // The entry cannot be examined: it is treated as existing, so the output is refused
            return true;
        }
    }

    /// <summary>
    ///     Builds the exception that refuses an output, with the reason and facts the resolver gives the same failure
    ///     of a read.
    /// </summary>
    /// <param name="kind">The failure kind.</param>
    /// <param name="realPath">The real path, when known.</param>
    /// <param name="cause">The underlying exception, if any.</param>
    /// <param name="shown">The path as shown in messages.</param>
    /// <param name="key">The full path as written.</param>
    /// <returns>The exception to throw.</returns>
    private FileReferenceException RefuseOutput(
        FileReferenceFailureKind kind, string? realPath, Exception? cause, string shown, string key)
    {
        // A failed outcome is turned into its exception exactly as for a read; Deliver throws it
        try
        {
            _ = Deliver(Outcome.Failed(kind, realPath, cause), shown, key);
        }
        catch (FileReferenceException ex)
        {
            return ex;
        }

        throw new UnreachableException("A failed outcome always produces an exception.");
    }
}
