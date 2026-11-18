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
using DemaConsulting.DictionaryMark.Files;
using Microsoft.Win32.SafeHandles;

namespace DemaConsulting.DictionaryMark.Tests.Files;

/// <summary>
///     Helpers for the tests of <see cref="FileReferenceResolver"/>, <see cref="FileAccessPolicy"/> and the real-path
///     unit: removing a directory link, creating a named pipe, finding a short (8.3) path, and a policy that records the
///     requests it is asked about.
/// </summary>
internal static class FileTestSupport
{
    /// <summary>The reason given when a test needs a Windows-only capability on another system.</summary>
    public const string WindowsOnly = "This scenario exists only on Windows.";

    /// <summary>The reason given when a test needs a Linux or macOS capability on another system.</summary>
    public const string UnixOnly = "This scenario exists only on Linux and macOS.";

    /// <summary>The reason given when a permission test runs where permissions are not enforced (as root).</summary>
    public const string PermissionsNotEnforced = "Folder permissions are not enforced for this user (running as root).";

    /// <summary>
    ///     Makes a folder execute-only for its owner (mode 0100: it may be searched but not listed) on Linux and macOS.
    /// </summary>
    /// <param name="path">The full path of the folder.</param>
    /// <returns>
    ///     <see langword="true"/> when listing the folder is now refused; <see langword="false"/> on Windows or where
    ///     permissions are not enforced (a process running as root).
    /// </returns>
    public static bool TryMakeExecuteOnly(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return false;
        }

        File.SetUnixFileMode(path, UnixFileMode.UserExecute);
        try
        {
            _ = Directory.EnumerateFileSystemEntries(path).Any();
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
    }

    /// <summary>
    ///     Gives the owner full access to a folder again (mode 0700) on Linux and macOS, so it can be deleted.
    /// </summary>
    /// <param name="path">The full path of the folder.</param>
    public static void RestoreOwnerAccess(string path)
    {
        if (!OperatingSystem.IsWindows() && Directory.Exists(path))
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    /// <summary>
    ///     Removes a directory link (a symbolic link or a junction) without touching its target.
    /// </summary>
    /// <param name="link">The full path of the link.</param>
    public static void RemoveDirectoryLink(string link)
    {
        // On Windows a junction or directory link is removed like an empty directory; on Unix a symbolic link is a file
        if (OperatingSystem.IsWindows())
        {
            Directory.Delete(link);
        }
        else
        {
            File.Delete(link);
        }
    }

    /// <summary>
    ///     Creates a directory link, or skips the test when the host cannot create one (not expected on CI: Windows falls
    ///     back to a junction, which needs no privilege).
    /// </summary>
    /// <param name="link">The full path of the link to create.</param>
    /// <param name="target">The full path of the existing target directory.</param>
    public static void CreateDirectoryLinkOrSkip(string link, string target)
    {
        if (!FileLinkSupport.TryCreateDirectoryLink(link, target, out var reason))
        {
            Assert.Skip("Directory links cannot be created in this environment: " + reason);
        }
    }

    /// <summary>
    ///     Creates a named pipe (FIFO) with <c>mkfifo</c> (Linux and macOS).
    /// </summary>
    /// <param name="path">The full path of the pipe.</param>
    /// <returns>An empty string on success, otherwise the reason it failed.</returns>
    public static string CreateFifo(string path)
    {
        var info = new ProcessStartInfo("mkfifo")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add(path);
        using var process = Process.Start(info);
        if (process is null)
        {
            return "mkfifo could not be started";
        }

        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        return process.ExitCode == 0 ? string.Empty : "mkfifo failed: " + error;
    }

    /// <summary>
    ///     Gets the short (8.3) spelling of an existing Windows path through the command interpreter.
    /// </summary>
    /// <param name="path">The full path.</param>
    /// <returns>The short spelling, or <see langword="null"/> when it cannot be determined.</returns>
    public static string? TryGetShortPath(string path)
    {
        // The command line is passed as written: cmd.exe does not understand the escaped quotes of an argument list
        var info = new ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            Arguments = $"/c for %I in (\"{path}\") do @echo %~sI",
        };
        using var process = Process.Start(info);
        if (process is null)
        {
            return null;
        }

        var output = process.StandardOutput.ReadToEnd().Trim();
        process.WaitForExit();
        return process.ExitCode == 0 && output.Length > 0 ? output : null;
    }

    /// <summary>
    ///     Gets the volume GUID path (<c>\\?\Volume{GUID}\</c>) of the drive of an existing Windows path through
    ///     <c>mountvol</c>.
    /// </summary>
    /// <param name="path">The full path on a drive.</param>
    /// <returns>The volume GUID path with its trailing backslash, or <see langword="null"/> when it cannot be determined.</returns>
    public static string? TryGetVolumeGuidPath(string path)
    {
        var info = new ProcessStartInfo("mountvol.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            Arguments = $"{Path.GetPathRoot(path)} /L",
        };
        try
        {
            using var process = Process.Start(info);
            if (process is null)
            {
                return null;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            return process.ExitCode == 0 && RealPath.IsOrdinaryExtendedPath(output) && output.EndsWith('\\') ? output : null;
        }
        catch (System.ComponentModel.Win32Exception)
        {
            return null;
        }
    }

    /// <summary>
    ///     Opens the Windows null device as a handle, to stand in for a device reached through a reference.
    /// </summary>
    /// <returns>The handle.</returns>
    public static SafeFileHandle OpenNullDevice() =>
        File.OpenHandle("NUL", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

    /// <summary>
    ///     Gets the real location of a directory, as the policies compare it.
    /// </summary>
    /// <param name="path">The full path.</param>
    /// <returns>The real location.</returns>
    public static string Canonical(string path)
    {
        var canonical = RealPath.TryGetCanonicalDirectory(Path.GetFullPath(path));
        Assert.NotNull(canonical);
        return canonical;
    }
}

/// <summary>
///     A policy that records every request and answers with a function, so a test can assert exactly what the
///     resolver asked.
/// </summary>
/// <param name="answer">The function that decides.</param>
/// <param name="name">The name of the policy; <c>recording</c> by default.</param>
internal sealed class RecordingPolicy(Func<FileAccessRequest, FileAccessDecision> answer, string name = "recording") : FileAccessPolicy
{
    /// <summary>The requests, in order.</summary>
    private readonly List<FileAccessRequest> _requests = [];

    /// <summary>
    ///     Gets the requests the policy was asked about, in order.
    /// </summary>
    public IReadOnlyList<FileAccessRequest> Requests => _requests;

    /// <inheritdoc/>
    public override string Name => name;

    /// <summary>
    ///     Creates a policy that allows everything and records the requests.
    /// </summary>
    /// <returns>The policy.</returns>
    public static RecordingPolicy AllowingAll() => new(_ => FileAccessDecision.Allow);

    /// <inheritdoc/>
    public override FileAccessDecision Check(FileAccessRequest request)
    {
        _requests.Add(request);
        return answer(request);
    }
}
