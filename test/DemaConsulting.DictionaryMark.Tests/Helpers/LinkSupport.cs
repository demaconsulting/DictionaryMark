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

namespace DemaConsulting.DictionaryMark.Tests.Helpers;

/// <summary>
///     Creates symbolic links, junctions and named pipes for file-access tests, skipping a test cleanly where the
///     machine cannot create them.
/// </summary>
/// <remarks>
///     Creating a symbolic link on Windows needs a privilege that ordinary accounts lack unless Developer Mode is
///     on. A folder link therefore falls back to a junction (<c>mklink /J</c>), which needs no privilege and is
///     judged the same way by the tool. A file link has no such fallback, so a test that needs one is skipped with
///     <see cref="Assert.Skip(string)"/> and a reason, never failed.
/// </remarks>
internal static class LinkSupport
{
    /// <summary>
    ///     Creates a link to a folder: a symbolic link, or on Windows a junction when symbolic links are not
    ///     permitted; skips the test when neither can be created.
    /// </summary>
    /// <param name="linkPath">The full path of the link to create.</param>
    /// <param name="targetPath">The full path of the existing target folder.</param>
    public static void CreateDirectoryLinkOrSkip(string linkPath, string targetPath)
    {
        // Prefer a symbolic link, which every platform judges by its real path
        string reason;
        try
        {
            Directory.CreateSymbolicLink(linkPath, targetPath);
            return;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            reason = ex.Message;
        }

        // Fall back to a junction on Windows, which needs no privilege
        if (OperatingSystem.IsWindows() && TryCreateJunction(linkPath, targetPath))
        {
            return;
        }

        Assert.Skip($"A folder link cannot be created on this machine: {reason}");
    }

    /// <summary>
    ///     Creates a symbolic link to a file; skips the test when the machine does not permit it.
    /// </summary>
    /// <param name="linkPath">The full path of the link to create.</param>
    /// <param name="targetPath">The full path of the target file (it need not exist).</param>
    public static void CreateFileLinkOrSkip(string linkPath, string targetPath)
    {
        try
        {
            File.CreateSymbolicLink(linkPath, targetPath);
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            Assert.Skip($"A file symbolic link cannot be created on this machine: {ex.Message}");
        }
    }

    /// <summary>
    ///     Creates a named pipe (FIFO) on Linux or macOS; skips the test elsewhere or when it cannot be created.
    /// </summary>
    /// <param name="path">The full path of the pipe to create.</param>
    public static void CreateFifoOrSkip(string path)
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(), "Named pipes are tested on Linux and macOS only.");
        var exitCode = RunProcess("mkfifo", path);
        Assert.SkipUnless(exitCode == 0, $"mkfifo failed with exit code {exitCode}.");
    }

    /// <summary>
    ///     Spells a local Windows folder through the administrative share of its drive on this machine
    ///     (<c>C:\x</c> as <c>\\localhost\C$\x</c>); skips the test elsewhere or when the share is not reachable.
    /// </summary>
    /// <remarks>
    ///     Gives a real network location without a server: administrative shares need an administrator account and
    ///     the Server service, so a test that uses this is supplementary and never requirement evidence.
    /// </remarks>
    /// <param name="localFolder">The full path of an existing local folder.</param>
    /// <returns>The folder's path on the administrative share.</returns>
    public static string AdminShareFolderOrSkip(string localFolder)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network shares are tested on Windows only.");
        var hasDrive = localFolder.Length >= 3 && char.IsAsciiLetter(localFolder[0])
            && localFolder[1] == ':' && localFolder[2] == '\\';
        var shareFolder = hasDrive ? $@"\\localhost\{localFolder[0]}${localFolder[2..]}" : string.Empty;
        Assert.SkipUnless(
            hasDrive && Directory.Exists(shareFolder),
            $"The administrative share of '{localFolder}' is not reachable on this machine.");
        return shareFolder;
    }

    /// <summary>
    ///     Creates a junction with <c>cmd /c mklink /J</c>.
    /// </summary>
    /// <param name="linkPath">The full path of the junction.</param>
    /// <param name="targetPath">The full path of the target folder.</param>
    /// <returns>True when the junction was created.</returns>
    private static bool TryCreateJunction(string linkPath, string targetPath) =>
        RunProcess("cmd.exe", "/c", "mklink", "/J", linkPath, targetPath) == 0 && Directory.Exists(linkPath);

    /// <summary>
    ///     Runs a program with arguments and waits for it to exit, without a time limit.
    /// </summary>
    /// <param name="program">The program.</param>
    /// <param name="arguments">The arguments.</param>
    /// <returns>The exit code, or -1 when the program cannot be started.</returns>
    private static int RunProcess(string program, params string[] arguments) =>
        RunProcess(program, arguments, timeout: null, out _);

    /// <summary>
    ///     Runs a program with arguments and waits for it to exit, killing it (and its child processes) when it does
    ///     not exit in time, and captures what it printed.
    /// </summary>
    /// <remarks>
    ///     Shared by the test helpers that drive system tools (<c>mklink</c>, <c>mkfifo</c>, <c>fsutil</c>,
    ///     <c>hdiutil</c>), so a hung tool can only skip a test, never hang the test run. Standard output and error
    ///     are read asynchronously, so a tool that prints much cannot block on a full pipe. Never throws for a
    ///     program that cannot be started or killed.
    /// </remarks>
    /// <param name="program">The program.</param>
    /// <param name="arguments">The arguments.</param>
    /// <param name="timeout">The time the program may run, or null to wait without a limit.</param>
    /// <param name="output">Set to what the program printed to standard output and error (possibly empty).</param>
    /// <returns>The exit code, or -1 when the program cannot be started or did not exit in time.</returns>
    internal static int RunProcess(
        string program, IReadOnlyList<string> arguments, TimeSpan? timeout, out string output)
    {
        output = string.Empty;
        var startInfo = new ProcessStartInfo(program)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        Process? process;
        try
        {
            process = Process.Start(startInfo);
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            output = ex.Message;
            return -1;
        }

        if (process == null)
        {
            return -1;
        }

        using (process)
        {
            // Read both streams while waiting, so a full pipe cannot block the program
            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            var exited = timeout is { } limit
                ? process.WaitForExit((int)Math.Min(limit.TotalMilliseconds, int.MaxValue))
                : process.WaitForExit(Timeout.Infinite);

            // A program that did not exit in time is killed with its child processes and counts as failed
            if (!exited)
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
                {
                    // The program exited between the wait and the kill, or cannot be killed; it counts as failed
                }
            }

            // Collect what was printed; a killed program closes its pipes, and the wait is bounded regardless
            var streamWait = TimeSpan.FromSeconds(10);
            var printed = Task.WaitAll([standardOutput, standardError], streamWait);
            output = printed ? (standardOutput.Result + standardError.Result).Trim() : string.Empty;
            return exited ? process.ExitCode : -1;
        }
    }
}
