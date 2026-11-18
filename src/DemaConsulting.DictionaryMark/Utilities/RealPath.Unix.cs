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

using System.Globalization;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace DemaConsulting.DictionaryMark.Utilities;

/// <summary>
///     The Linux and macOS facts of an open descriptor and the non-blocking Unix open.
/// </summary>
internal static partial class RealPath
{
    /// <summary>The <c>errno</c> value <c>EPERM</c> (Linux and macOS).</summary>
    private const int ErrorNotPermitted = 1;

    /// <summary>The <c>errno</c> value <c>ENOENT</c> (Linux and macOS).</summary>
    private const int ErrorNoEntry = 2;

    /// <summary>The <c>errno</c> value <c>EINTR</c> (Linux and macOS).</summary>
    private const int ErrorInterrupted = 4;

    /// <summary>
    ///     The <c>errno</c> value <c>ENXIO</c> (Linux and macOS): a named pipe opened for writing without a reader,
    ///     or a device that is not there.
    /// </summary>
    private const int ErrorNoDevice = 6;

    /// <summary>The <c>errno</c> value <c>EACCES</c> (Linux and macOS).</summary>
    private const int ErrorAccess = 13;

    /// <summary>The <c>errno</c> value <c>ENOTDIR</c> (Linux and macOS).</summary>
    private const int ErrorNotDirectory = 20;

    /// <summary>The Linux <c>open</c> flags <c>O_RDONLY | O_NONBLOCK | O_NOCTTY | O_CLOEXEC</c> (x64 and Arm64).</summary>
    private const int LinuxOpenRead = 0x800 | 0x100 | 0x80000;

    /// <summary>The Linux <c>open</c> flags <c>O_WRONLY | O_NONBLOCK | O_NOCTTY | O_CLOEXEC</c> (no create, no truncate).</summary>
    private const int LinuxOpenWrite = 0x1 | 0x800 | 0x100 | 0x80000;

    /// <summary>The Linux <c>open</c> flags <c>O_PATH | O_CLOEXEC</c>, which open a folder without read permission.</summary>
    private const int LinuxOpenPathOnly = 0x200000 | 0x80000;

    /// <summary>The macOS <c>open</c> flags <c>O_RDONLY | O_NONBLOCK | O_NOCTTY | O_CLOEXEC</c>.</summary>
    private const int MacOpenRead = 0x4 | 0x20000 | 0x01000000;

    /// <summary>The macOS <c>open</c> flags <c>O_WRONLY | O_NONBLOCK | O_NOCTTY | O_CLOEXEC</c> (no create, no truncate).</summary>
    private const int MacOpenWrite = 0x1 | 0x4 | 0x20000 | 0x01000000;

    /// <summary>The size of the buffer handed to <c>fstat</c>; larger than <c>struct stat</c> on every supported architecture.</summary>
    private const int StatBufferSize = 256;

    /// <summary>The macOS <c>proc_pidfdinfo</c> flavor that reports the vnode and path of a descriptor.</summary>
    private const int MacVnodePathInfo = 2;

    /// <summary>
    ///     The size of the vnode-with-path structure: <c>proc_fileinfo</c> (24 bytes) and <c>vnode_info_path</c>
    ///     (152 bytes of <c>vnode_info</c> and a 1024-byte path). The layout has no pointer- or <c>long</c>-sized
    ///     field, so it is the same on x64 and Arm64.
    /// </summary>
    private const int MacVnodePathInfoSize = 1200;

    /// <summary>The offset of <c>vst_mode</c> (16 bits) in the vnode-with-path structure: 24 + 4.</summary>
    private const int MacModeOffset = 28;

    /// <summary>The offset of <c>vip_path</c> (a NUL-terminated UTF-8 path) in the vnode-with-path structure: 24 + 152.</summary>
    private const int MacPathOffset = 176;

    /// <summary>The offset of the 16-bit <c>st_mode</c> in the macOS Arm64 <c>struct stat</c>, after the 32-bit <c>st_dev</c>.</summary>
    private const int MacStatModeOffset = 4;

    /// <summary>The macOS <c>PATH_MAX</c>: the size of the buffer <c>realpath</c> fills, including the terminating NUL.</summary>
    private const int MacPathMax = 1024;

    /// <summary>Whether <c>fstat</c> is missing from the C library (so <c>__fxstat</c> is used); null until first needed.</summary>
    private static bool? _linuxUsesFxStat;

    /// <summary>
    ///     The purpose of a non-blocking Unix open.
    /// </summary>
    private enum UnixOpenKind
    {
        /// <summary>Open an existing file for reading.</summary>
        Read,

        /// <summary>Open an existing file for writing, without creating or truncating it.</summary>
        Write,

        /// <summary>Open only a reference to a path (Linux <c>O_PATH</c>), used for folders.</summary>
        PathOnly,
    }

    /// <summary>
    ///     Opens a path without blocking, so a named pipe or a device cannot hang the open, and so the descriptor is
    ///     not inherited by child processes.
    /// </summary>
    /// <remarks>
    ///     The variadic mode argument of <c>open</c> is only read with <c>O_CREAT</c>, which is never passed, so the
    ///     two fixed arguments are all the call needs on every ABI (Arm64 macOS included). macOS has no
    ///     <c>O_PATH</c>; macOS folders are resolved with <c>realpath</c> instead, so a path-only open there is a
    ///     read open.
    /// </remarks>
    /// <param name="path">The full path.</param>
    /// <param name="kind">The purpose of the open.</param>
    /// <returns>
    ///     The handle, which owns the descriptor; or null when the system reports <c>ENXIO</c> (a named pipe with no
    ///     reader, or a device that is not there), which only a special file can cause.
    /// </returns>
    /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when a folder of the path is not a folder.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when access is denied.</exception>
    /// <exception cref="IOException">Thrown for any other failure.</exception>
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    private static SafeFileHandle? OpenNonBlocking(string path, UnixOpenKind kind)
    {
        // Select the flags of the platform (macOS has no O_PATH, so a path-only open there is a read open)
        int flags;
        if (OperatingSystem.IsMacOS())
        {
            flags = kind == UnixOpenKind.Write ? MacOpenWrite : MacOpenRead;
        }
        else
        {
            flags = kind switch
            {
                UnixOpenKind.Write => LinuxOpenWrite,
                UnixOpenKind.PathOnly => LinuxOpenPathOnly,
                _ => LinuxOpenRead,
            };
        }

        // Open, retrying when a signal interrupts the call
        var bytes = Encoding.UTF8.GetBytes(path + "\0");
        int descriptor;
        int error;
        do
        {
            descriptor = OpenNative(bytes, flags);
            error = descriptor < 0 ? Marshal.GetLastPInvokeError() : 0;
        }
        while (descriptor < 0 && error == ErrorInterrupted);

        if (descriptor >= 0)
        {
            return new SafeFileHandle(descriptor, ownsHandle: true);
        }

        // Translate errno into the exceptions .NET itself raises for it, so callers need not know errno
        var message = $"The file '{path}' cannot be opened (errno {error}).";
        return error switch
        {
            ErrorNoDevice => null,
            ErrorNoEntry => throw new FileNotFoundException(message, path),
            ErrorNotDirectory => throw new DirectoryNotFoundException(message),
            ErrorAccess or ErrorNotPermitted => throw new UnauthorizedAccessException(message),
            _ => throw new IOException(message, error),
        };
    }

    /// <summary>
    ///     Reports the facts of an open Linux descriptor: the file type and link count from <c>fstat</c>, and the
    ///     real path from the <c>/proc/self/fd</c> link of the descriptor.
    /// </summary>
    /// <param name="handle">The open handle.</param>
    /// <returns>
    ///     The facts; the path is null when <c>/proc</c> is not mounted, when the file was deleted (its link count is
    ///     zero), or when the type and link count cannot be read.
    /// </returns>
    [SupportedOSPlatform("linux")]
    private static HandleFacts InspectLinux(SafeFileHandle handle)
    {
        // The link of the descriptor names the file the descriptor refers to now, whatever happened to the name used
        // to open it; it is read before fstat, so a file deleted between the two calls is seen as deleted
        var target = WithDescriptor(
            handle,
            descriptor => new FileInfo("/proc/self/fd/" + descriptor.ToString(CultureInfo.InvariantCulture)).LinkTarget);
        var stat = TryGetLinuxStat(handle);

        // Deletion is decided from the link count, never from the " (deleted)" text Linux appends to the link
        var path = stat is { LinkCount: > 0 } && target is not null && target.StartsWith('/') ? target : null;
        return new HandleFacts(IsRegularMode(stat?.Mode), path);
    }

    /// <summary>
    ///     Reports the facts of an open macOS descriptor from one <c>proc_pidfdinfo</c> call, falling back to
    ///     <c>fstat</c> for the type when the vnode information is not available.
    /// </summary>
    /// <param name="handle">The open handle.</param>
    /// <returns>The facts.</returns>
    [SupportedOSPlatform("macos")]
    private static HandleFacts InspectMac(SafeFileHandle handle)
    {
        // The kernel refuses a buffer smaller than the structure, and answers with the number of bytes it filled in
        var buffer = new byte[MacVnodePathInfoSize];
        var filled = WithDescriptor(
            handle,
            descriptor => ProcPidFdInfoNative(Environment.ProcessId, descriptor, MacVnodePathInfo, buffer, buffer.Length));
        if (filled != MacVnodePathInfoSize)
        {
            // Without the vnode information there is no path, but the type can still come from fstat, so a device or
            // a named pipe is never mistaken for a file of unknown type
            return new HandleFacts(IsRegularMode(TryGetMacStatMode(handle)), null);
        }

        var mode = BitConverter.ToUInt16(buffer, MacModeOffset);
        var end = Array.IndexOf(buffer, (byte)0, MacPathOffset);
        var length = (end < 0 ? buffer.Length : end) - MacPathOffset;
        var path = length > 0 ? Encoding.UTF8.GetString(buffer, MacPathOffset, length) : null;
        return new HandleFacts(IsRegularMode(mode), path);
    }

    /// <summary>
    ///     Gets the mode (type and permission bits) and the hard-link count of an open descriptor on Linux.
    /// </summary>
    /// <remarks>
    ///     The two known layouts of <c>struct stat</c> (glibc and musl agree on both) are x64, with <c>st_nlink</c>
    ///     as a 64-bit value at offset 16 and <c>st_mode</c> at offset 24, and Arm64, with <c>st_mode</c> at offset
    ///     16 and a 32-bit <c>st_nlink</c> at offset 20. Other architectures are unknown, so their facts are unknown.
    /// </remarks>
    /// <param name="handle">The open handle.</param>
    /// <returns>The mode and the link count, or null when they cannot be read.</returns>
    [SupportedOSPlatform("linux")]
    private static (int Mode, long LinkCount)? TryGetLinuxStat(SafeFileHandle handle)
    {
        int modeOffset;
        int linkOffset;
        bool wideLinkCount;
        int statVersion;
        var architecture = RuntimeInformation.ProcessArchitecture;
        if (architecture == Architecture.X64)
        {
            modeOffset = 24;
            linkOffset = 16;
            wideLinkCount = true;
            statVersion = 1;
        }
        else if (architecture == Architecture.Arm64)
        {
            modeOffset = 16;
            linkOffset = 20;
            wideLinkCount = false;
            statVersion = 0;
        }
        else
        {
            return null;
        }

        var buffer = new byte[StatBufferSize];
        if (WithDescriptor(handle, descriptor => CallFStat(descriptor, buffer, statVersion)) != 0)
        {
            return null;
        }

        var linkCount = wideLinkCount ? BitConverter.ToInt64(buffer, linkOffset) : BitConverter.ToUInt32(buffer, linkOffset);
        return (BitConverter.ToInt32(buffer, modeOffset), linkCount);
    }

    /// <summary>
    ///     Gets the mode of an open descriptor on macOS through <c>fstat</c>, the fallback when
    ///     <c>proc_pidfdinfo</c> cannot describe the descriptor.
    /// </summary>
    /// <remarks>
    ///     Only the Arm64 layout (64-bit inode numbers, 16-bit <c>st_mode</c> at offset 4) is read; the mode is
    ///     unknown elsewhere.
    /// </remarks>
    /// <param name="handle">The open handle.</param>
    /// <returns>The mode, or null when it cannot be read.</returns>
    [SupportedOSPlatform("macos")]
    private static int? TryGetMacStatMode(SafeFileHandle handle)
    {
        if (RuntimeInformation.ProcessArchitecture != Architecture.Arm64)
        {
            return null;
        }

        var buffer = new byte[StatBufferSize];
        var result = WithDescriptor(handle, descriptor => FStatNative(descriptor, buffer));
        return result == 0 ? BitConverter.ToUInt16(buffer, MacStatModeOffset) : null;
    }

    /// <summary>
    ///     Gets the real location of a path on macOS through <c>realpath(3)</c>, which resolves links by name and
    ///     needs only search permission on the components of the path.
    /// </summary>
    /// <param name="path">The full path.</param>
    /// <returns>The resolved path, or null when it does not exist or cannot be resolved.</returns>
    [SupportedOSPlatform("macos")]
    private static string? TryRealPath(string path)
    {
        // The buffer is owned here (PATH_MAX bytes), so nothing is allocated by the C library or freed here
        var resolved = new byte[MacPathMax];
        if (RealPathNative(Encoding.UTF8.GetBytes(path + "\0"), resolved) == IntPtr.Zero)
        {
            return null;
        }

        var length = Array.IndexOf(resolved, (byte)0);
        return length > 0 ? Encoding.UTF8.GetString(resolved, 0, length) : null;
    }

    /// <summary>
    ///     Runs a call with the descriptor number of a handle, keeping the handle from being closed (and its number
    ///     reused by another open) for the duration of the call.
    /// </summary>
    /// <remarks>
    ///     This is the one place that reads the raw descriptor of a handle: the C functions take an <c>int</c>
    ///     descriptor, and the <c>/proc/self/fd</c> link is named by it. The reference count taken around the call
    ///     is what makes reading the raw value safe.
    /// </remarks>
    /// <typeparam name="T">The result type of the call.</typeparam>
    /// <param name="handle">The open handle.</param>
    /// <param name="call">The call that uses the descriptor.</param>
    /// <returns>What the call returned.</returns>
    private static T WithDescriptor<T>(SafeFileHandle handle, Func<int, T> call)
    {
        var added = false;
        try
        {
            handle.DangerousAddRef(ref added);
#pragma warning disable S3869 // The raw descriptor is needed by the C call; the reference count above keeps it valid
            return call((int)handle.DangerousGetHandle());
#pragma warning restore S3869
        }
        finally
        {
            if (added)
            {
                handle.DangerousRelease();
            }
        }
    }

    /// <summary>
    ///     Calls <c>fstat</c>, or <c>__fxstat</c> where the C library does not export <c>fstat</c> (glibc before 2.33).
    /// </summary>
    /// <param name="descriptor">The descriptor.</param>
    /// <param name="buffer">The buffer for <c>struct stat</c>.</param>
    /// <param name="statVersion">The <c>_STAT_VER</c> value of the architecture, for <c>__fxstat</c>.</param>
    /// <returns>Zero on success.</returns>
    [SupportedOSPlatform("linux")]
    private static int CallFStat(int descriptor, byte[] buffer, int statVersion)
    {
        if (_linuxUsesFxStat != true)
        {
            try
            {
                var result = FStatNative(descriptor, buffer);
                _linuxUsesFxStat = false;
                return result;
            }
            catch (EntryPointNotFoundException)
            {
                _linuxUsesFxStat = true;
            }
        }

        return FXStatNative(statVersion, descriptor, buffer);
    }
}
