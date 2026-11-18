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

namespace DemaConsulting.DictionaryMark.Tests.Files;

/// <summary>
///     A readable stream of spaces that never ends and counts the bytes read from it.
/// </summary>
internal sealed class EndlessStream : Stream
{
    /// <summary>Gets the number of bytes that were read.</summary>
    public long BytesRead { get; private set; }

    /// <inheritdoc/>
    public override bool CanRead => true;

    /// <inheritdoc/>
    public override bool CanSeek => false;

    /// <inheritdoc/>
    public override bool CanWrite => false;

    /// <inheritdoc/>
    public override long Length => throw new NotSupportedException();

    /// <inheritdoc/>
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    /// <inheritdoc/>
    public override void Flush()
    {
    }

    /// <inheritdoc/>
    public override int Read(byte[] buffer, int offset, int count)
    {
        Array.Fill(buffer, (byte)' ', offset, count);
        BytesRead += count;
        return count;
    }

    /// <inheritdoc/>
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void SetLength(long value) => throw new NotSupportedException();

    /// <inheritdoc/>
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}

/// <summary>
///     A temporary directory that is deleted when disposed.
/// </summary>
internal sealed class FileTestDirectory : IDisposable
{
    /// <summary>
    ///     Initializes a new, empty temporary directory.
    /// </summary>
    public FileTestDirectory()
    {
        Path = Directory.CreateTempSubdirectory("files-test").FullName;
    }

    /// <summary>
    ///     Gets the full path of the directory.
    /// </summary>
    public string Path { get; }

    /// <summary>
    ///     Writes a text file as UTF-8 without a byte-order mark, creating its folders.
    /// </summary>
    /// <param name="relativePath">The path relative to the directory (forward slashes).</param>
    /// <param name="text">The file text.</param>
    /// <returns>The full path of the file.</returns>
    public string Write(string relativePath, string text) => Write(relativePath, new UTF8Encoding(false).GetBytes(text));

    /// <summary>
    ///     Writes a file of bytes, creating its folders.
    /// </summary>
    /// <param name="relativePath">The path relative to the directory (forward slashes).</param>
    /// <param name="bytes">The file bytes.</param>
    /// <returns>The full path of the file.</returns>
    public string Write(string relativePath, byte[] bytes)
    {
        var full = System.IO.Path.Combine(Path, relativePath);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        File.WriteAllBytes(full, bytes);
        return full;
    }

    /// <summary>
    ///     Gets the full path of a location inside the directory.
    /// </summary>
    /// <param name="relativePath">The relative path.</param>
    /// <returns>The full path.</returns>
    public string Combine(string relativePath) => System.IO.Path.Combine(Path, relativePath);

    /// <inheritdoc/>
    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best effort: a leftover temporary folder must not fail a test
        }
        catch (UnauthorizedAccessException)
        {
            // Best effort, as above
        }
    }
}

/// <summary>
///     Helpers that create links for the tests that need real ones.
/// </summary>
internal static class FileLinkSupport
{
    /// <summary>
    ///     Tries to create a directory link (a symbolic link, or on Windows a junction when links need a privilege).
    /// </summary>
    /// <param name="link">The full path of the link to create.</param>
    /// <param name="target">The full path of the existing target directory.</param>
    /// <param name="reason">The reason the link could not be created, or an empty string.</param>
    /// <returns><see langword="true"/> when the link exists afterwards.</returns>
    public static bool TryCreateDirectoryLink(string link, string target, out string reason)
    {
        try
        {
            Directory.CreateSymbolicLink(link, target);
            reason = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            reason = ex.Message;
        }

        // Without the privilege for symbolic links a junction can still be made on Windows
        return OperatingSystem.IsWindows() && TryCreateJunction(link, target, out reason);
    }

    /// <summary>
    ///     Tries to create a file symbolic link.
    /// </summary>
    /// <param name="link">The full path of the link to create.</param>
    /// <param name="target">The full path of the target file (which need not exist).</param>
    /// <param name="reason">The reason the link could not be created, or an empty string.</param>
    /// <returns><see langword="true"/> when the link was created.</returns>
    public static bool TryCreateFileLink(string link, string target, out string reason)
    {
        try
        {
            File.CreateSymbolicLink(link, target);
            reason = string.Empty;
            return true;
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or PlatformNotSupportedException)
        {
            reason = ex.Message;
            return false;
        }
    }

    /// <summary>
    ///     Creates a Windows junction through the command interpreter (no privilege needed).
    /// </summary>
    /// <param name="link">The full path of the junction.</param>
    /// <param name="target">The full path of the target directory.</param>
    /// <param name="reason">The reason the junction could not be created, or an empty string.</param>
    /// <returns><see langword="true"/> when the junction exists afterwards.</returns>
    private static bool TryCreateJunction(string link, string target, out string reason)
    {
        var info = new System.Diagnostics.ProcessStartInfo("cmd.exe")
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        info.ArgumentList.Add("/c");
        info.ArgumentList.Add("mklink");
        info.ArgumentList.Add("/J");
        info.ArgumentList.Add(Path.GetFullPath(link));
        info.ArgumentList.Add(Path.GetFullPath(target));
        try
        {
            using var process = System.Diagnostics.Process.Start(info);
            if (process is null)
            {
                reason = "cmd.exe could not be started";
                return false;
            }

            process.StandardOutput.ReadToEnd();
            process.StandardError.ReadToEnd();
            process.WaitForExit();
            reason = process.ExitCode == 0 ? string.Empty : "mklink /J failed";
            return process.ExitCode == 0 && Directory.Exists(link);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            reason = ex.Message;
            return false;
        }
    }
}
