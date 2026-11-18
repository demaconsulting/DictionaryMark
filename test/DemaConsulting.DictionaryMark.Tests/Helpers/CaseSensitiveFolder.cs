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

using DemaConsulting.DictionaryMark.Utilities;

namespace DemaConsulting.DictionaryMark.Tests.Helpers;

/// <summary>
///     An empty temporary folder in which names that differ only in letter case are different entries, for tests of
///     how the file-access policy treats case-variant siblings; skips the test cleanly where none can be made.
/// </summary>
/// <remarks>
///     <para>
///         The folder is made below the user's temporary folder, per platform: on Linux an ordinary folder (Linux
///         file systems distinguish case); on Windows an empty folder with per-folder case sensitivity enabled by
///         <c>fsutil.exe file setCaseSensitiveInfo &lt;folder&gt; enable</c>; on macOS a folder on a small
///         case-sensitive APFS disk image created and attached with <c>hdiutil</c>. Every result is confirmed by
///         <see cref="IsCaseSensitive(string)"/>, a behavioral probe, because <c>fsutil</c> exits with 0 even when it
///         fails (for example on a folder that is not empty, or where the feature is unavailable).
///     </para>
///     <para>
///         Each system tool runs with a time limit and is killed when it exceeds it. On macOS, creating, attaching
///         and detaching disk images is serialized by a lock, so test classes running in parallel do not race the
///         disk arbitration service; the image is detached on disposal, with a forced retry. When a case-sensitive
///         folder cannot be made, the test is skipped with <see cref="Assert.Skip(string)"/> and the reason (for
///         example the tool's output), never failed and never passed without the check it exists for. Disposal
///         removes folder links (junctions and symbolic links) without following them before deleting the
///         temporary folder, because a recursive delete of a folder that holds a Windows junction fails for a
///         non-administrator and leaves the emptied folders behind, and it retries briefly, so nothing is left
///         below the user's temporary folder.
///     </para>
/// </remarks>
internal sealed class CaseSensitiveFolder : IDisposable
{
    /// <summary>The number of attempts made to delete the temporary folder on disposal.</summary>
    private const int DeleteAttempts = 3;

    /// <summary>The time <c>fsutil</c> may run.</summary>
    private static readonly TimeSpan FsutilTimeout = TimeSpan.FromSeconds(60);

    /// <summary>The time each <c>hdiutil</c> step may run (creating an image can be slow on a busy runner).</summary>
    private static readonly TimeSpan HdiutilTimeout = TimeSpan.FromSeconds(120);

    /// <summary>Serializes the macOS disk-image steps across test classes that run in parallel.</summary>
    private static readonly object DiskImageLock = new();

    /// <summary>The delay before each repeated attempt to delete the temporary folder.</summary>
    private static readonly TimeSpan DeleteRetryDelay = TimeSpan.FromMilliseconds(100);

    /// <summary>
    ///     The temporary folder holding the case-sensitive folder (or, on macOS, the image and mount point).
    /// </summary>
    private readonly TemporaryDirectory _temporary;

    /// <summary>The mount point of the attached macOS disk image, or null on other platforms.</summary>
    private readonly string? _mountPoint;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CaseSensitiveFolder"/> class around a folder already made.
    /// </summary>
    /// <param name="temporary">The temporary folder that owns everything created.</param>
    /// <param name="folderPath">The full path of the case-sensitive folder.</param>
    /// <param name="mountPoint">The mount point of the attached disk image, or null.</param>
    private CaseSensitiveFolder(TemporaryDirectory temporary, string folderPath, string? mountPoint)
    {
        _temporary = temporary;
        FolderPath = folderPath;
        _mountPoint = mountPoint;
    }

    /// <summary>
    ///     Gets the full path of the empty case-sensitive folder.
    /// </summary>
    public string FolderPath { get; }

    /// <summary>
    ///     Makes an empty case-sensitive folder, or skips the test when this machine cannot make one.
    /// </summary>
    /// <returns>The folder; the caller disposes it.</returns>
    public static CaseSensitiveFolder CreateOrSkip()
    {
        // The user's temporary folder, where the user has full control: Windows refuses to set the case-sensitive
        // flag (access denied) in a folder whose permissions are inherited from a drive root, as a build folder may be
        TemporaryDirectory temporary;
        try
        {
            temporary = new TemporaryDirectory(Path.GetTempPath());
        }
        catch (InvalidOperationException ex)
        {
            Assert.Skip(
                "A case-sensitive folder cannot be made on this machine: the temporary folder cannot be written: " +
                ex.Message);
            throw;
        }

        string? mountPoint = null;
        string folder;
        string? skipReason;
        if (OperatingSystem.IsWindows())
        {
            (folder, skipReason) = MakeWindowsFolder(temporary.DirectoryPath);
        }
        else if (OperatingSystem.IsMacOS())
        {
            (folder, mountPoint, skipReason) = MakeMacFolder(temporary.DirectoryPath);
        }
        else
        {
            // Linux (and other Unix) file systems distinguish case, unless the folder is case-folding
            folder = Path.Combine(temporary.DirectoryPath, "cs");
            skipReason = ProbeSkipReason(folder, "The temporary folder ignores letter case (case folding).");
        }

        // Clean up and skip when no case-sensitive folder could be made
        var result = new CaseSensitiveFolder(temporary, folder, mountPoint);
        if (skipReason != null)
        {
            result.Dispose();
            Assert.Skip($"A case-sensitive folder cannot be made on this machine: {skipReason}");
        }

        return result;
    }

    /// <summary>
    ///     Determines by behavior whether a folder distinguishes letter case: a probe folder is created and looked
    ///     up by its name in upper case, then removed.
    /// </summary>
    /// <param name="folder">The full path of an existing, writable folder.</param>
    /// <returns>True when the upper-case name does not find the probe, so the folder distinguishes case.</returns>
    public static bool IsCaseSensitive(string folder)
    {
        var probe = Path.Combine(folder, "case-probe");
        Directory.CreateDirectory(probe);
        try
        {
            return !Directory.Exists(Path.Combine(folder, "CASE-PROBE"));
        }
        finally
        {
            Directory.Delete(probe);
        }
    }

    /// <summary>
    ///     Detaches the macOS disk image (if any), removes folder links, then deletes everything created, retrying
    ///     briefly. Never throws.
    /// </summary>
    public void Dispose()
    {
        if (_mountPoint != null)
        {
            Detach(_mountPoint);
        }

        // Remove folder links first: a recursive delete of a folder holding a Windows junction fails with access
        // denied for a non-administrator and leaves the emptied folders behind
        RemoveFolderLinks(_temporary.DirectoryPath);
        _temporary.Dispose();

        // Retry anything still left (for example a file briefly held by an antivirus scanner); never throw
        for (var attempt = 1; attempt < DeleteAttempts && Directory.Exists(_temporary.DirectoryPath); attempt++)
        {
            Thread.Sleep(DeleteRetryDelay);
            try
            {
                Directory.Delete(_temporary.DirectoryPath, recursive: true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Left for the next attempt; after the last one the folder is abandoned rather than failing the test
            }
        }
    }

    /// <summary>
    ///     Removes every folder link (junction or symbolic link to a folder) below a folder, without following it.
    ///     Never throws.
    /// </summary>
    /// <param name="root">The folder to search.</param>
    private static void RemoveFolderLinks(string root)
    {
        // Find every folder below the root, including hidden and system ones; links are listed but not followed
        List<string> folders;
        try
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                AttributesToSkip = 0,
                IgnoreInaccessible = true
            };
            folders = Directory.EnumerateDirectories(root, "*", options).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // The root is missing or unreadable; the recursive delete deals with what remains
            return;
        }

        foreach (var folder in folders)
        {
            try
            {
                // A non-recursive delete of a link removes only the link, never the contents of its target
                if (File.GetAttributes(folder).HasFlag(FileAttributes.ReparsePoint))
                {
                    Directory.Delete(folder);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Leave this link for the recursive delete and its retries
            }
        }
    }

    /// <summary>
    ///     Makes an empty folder with per-folder case sensitivity on Windows.
    /// </summary>
    /// <remarks>
    ///     The folder must be empty when the flag is set; folders created in it afterwards inherit it. The exit code
    ///     of <c>fsutil</c> is not trusted (it is 0 even on failure); the probe decides.
    /// </remarks>
    /// <param name="temporaryPath">The temporary folder to make it in.</param>
    /// <returns>The folder, and the reason to skip (null when it is case-sensitive).</returns>
    private static (string Folder, string? SkipReason) MakeWindowsFolder(string temporaryPath)
    {
        var folder = Path.Combine(temporaryPath, "cs");
        try
        {
            folder = Directory.CreateDirectory(folder).FullName;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (folder, $"The folder '{folder}' cannot be created: {ex.Message}");
        }

        var exitCode = LinkSupport.RunProcess(
            "fsutil.exe", ["file", "setCaseSensitiveInfo", folder, "enable"], FsutilTimeout, out var output);
        return (folder, ProbeSkipReason(folder, $"fsutil exited with {exitCode}: {output}"));
    }

    /// <summary>
    ///     Makes a folder on a small case-sensitive APFS disk image on macOS, created and attached under a lock.
    /// </summary>
    /// <param name="temporaryPath">The temporary folder to hold the image and its mount point.</param>
    /// <returns>
    ///     The folder, the mount point (null when nothing was attached), and the reason to skip (null when it is
    ///     case-sensitive).
    /// </returns>
    private static (string Folder, string? MountPoint, string? SkipReason) MakeMacFolder(string temporaryPath)
    {
        var image = Path.Combine(temporaryPath, "cs.dmg");
        var mountPoint = Path.Combine(temporaryPath, "mnt");
        var folder = Path.Combine(mountPoint, "cs");
        try
        {
            mountPoint = Directory.CreateDirectory(mountPoint).FullName;
            folder = Path.Combine(mountPoint, "cs");
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (folder, null, $"The folder '{mountPoint}' cannot be created: {ex.Message}");
        }

        lock (DiskImageLock)
        {
            // Create the image with a case-sensitive file system
            var created = LinkSupport.RunProcess(
                "hdiutil",
                ["create", "-size", "32m", "-fs", "Case-sensitive APFS", "-volname", "DMCase", "-quiet", image],
                HdiutilTimeout,
                out var createOutput);
            if (created != 0)
            {
                return (folder, null, $"hdiutil create exited with {created}: {createOutput}");
            }

            // Attach it at the mount point, without showing it or checking it
            var attached = LinkSupport.RunProcess(
                "hdiutil",
                ["attach", "-nobrowse", "-noverify", "-noautoopen", "-mountpoint", mountPoint, image],
                HdiutilTimeout,
                out var attachOutput);
            if (attached != 0)
            {
                Detach(mountPoint);
                return (folder, null, $"hdiutil attach exited with {attached}: {attachOutput}");
            }
        }

        // Confirm by behavior that the mounted volume distinguishes case
        return (folder, mountPoint, ProbeSkipReason(folder, "The attached disk image ignores letter case."));
    }

    /// <summary>
    ///     Creates a folder (when missing) and confirms by behavior that it distinguishes letter case.
    /// </summary>
    /// <remarks>
    ///     A folder that cannot be created or probed gives a reason to skip rather than an exception, so the helper
    ///     never fails a test.
    /// </remarks>
    /// <param name="folder">The full path of the folder.</param>
    /// <param name="ignoresCaseReason">The reason to skip when the folder ignores letter case.</param>
    /// <returns>Null when the folder distinguishes case; otherwise the reason to skip.</returns>
    private static string? ProbeSkipReason(string folder, string ignoresCaseReason)
    {
        try
        {
            Directory.CreateDirectory(folder);
            return IsCaseSensitive(folder) ? null : ignoresCaseReason;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return $"The folder '{folder}' cannot be probed: {ex.Message}";
        }
    }

    /// <summary>
    ///     Detaches a macOS disk image, retrying with <c>-force</c> when the first attempt fails. Never throws.
    /// </summary>
    /// <param name="mountPoint">The mount point.</param>
    private static void Detach(string mountPoint)
    {
        lock (DiskImageLock)
        {
            if (LinkSupport.RunProcess("hdiutil", ["detach", mountPoint, "-quiet"], HdiutilTimeout, out _) != 0)
            {
                LinkSupport.RunProcess("hdiutil", ["detach", mountPoint, "-force", "-quiet"], HdiutilTimeout, out _);
            }
        }
    }
}
