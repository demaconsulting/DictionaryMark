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
///     The operating-system calls the real-path unit needs and .NET does not offer.
/// </summary>
/// <remarks>
///     Every import is private and called only from the platform-specific members of this unit, which add the logic
///     the raw call needs (buffer growth, prefix stripping, error translation, structure offsets) after an
///     <see cref="OperatingSystem"/> check. No unsafe code is used: buffers are managed arrays. Windows imports load
///     <c>kernel32.dll</c> from System32 only. Linux and macOS imports use <c>libc</c> with NUL-terminated UTF-8
///     paths. On macOS <c>libc</c> is <c>libSystem</c>, which re-exports the kernel system-call library of xnu,
///     where <c>proc_pidfdinfo</c> is implemented. This is the CanvasNet binding, and the macOS
///     tests that read a real path from a handle (such as <c>RealPath_Inspect_TempFile_ReportsPrivateLocation</c>)
///     prove that it resolves. A missing library or entry point is caught by the callers and leaves the facts
///     unknown, never classified.
/// </remarks>
internal static partial class RealPath
{
    /// <summary>The Windows <c>GetFinalPathNameByHandleW</c> function.</summary>
    /// <param name="file">The open handle.</param>
    /// <param name="buffer">The buffer for the path.</param>
    /// <param name="bufferLength">The size of the buffer, in characters.</param>
    /// <param name="flags">The flags.</param>
    /// <returns>The length of the path, the size needed, or zero on failure.</returns>
    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", EntryPoint = "GetFinalPathNameByHandleW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint GetFinalPathNameByHandleNative(SafeFileHandle file, [Out] char[] buffer, uint bufferLength, uint flags);

    /// <summary>The Windows <c>GetFileType</c> function.</summary>
    /// <param name="file">The open handle.</param>
    /// <returns>The type of the file.</returns>
    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", EntryPoint = "GetFileType", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint GetFileTypeNative(SafeFileHandle file);

    /// <summary>The Windows <c>CreateFileW</c> function.</summary>
    /// <param name="fileName">The path.</param>
    /// <param name="desiredAccess">The access rights.</param>
    /// <param name="shareMode">The share mode.</param>
    /// <param name="securityAttributes">The security attributes (zero).</param>
    /// <param name="creationDisposition">The disposition.</param>
    /// <param name="flagsAndAttributes">The flags.</param>
    /// <param name="templateFile">The template file (zero).</param>
    /// <returns>The handle; invalid on failure.</returns>
    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", EntryPoint = "CreateFileW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern SafeFileHandle CreateFileNative(
        string fileName,
        uint desiredAccess,
        uint shareMode,
        nint securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        nint templateFile);

    /// <summary>The Windows <c>GetLongPathNameW</c> function.</summary>
    /// <param name="shortPath">The path.</param>
    /// <param name="buffer">The buffer for the long path.</param>
    /// <param name="bufferLength">The size of the buffer, in characters.</param>
    /// <returns>The length of the long path, the size needed, or zero on failure.</returns>
    [SupportedOSPlatform("windows")]
    [DllImport("kernel32.dll", EntryPoint = "GetLongPathNameW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern uint GetLongPathNameNative(string shortPath, [Out] char[] buffer, uint bufferLength);

    /// <summary>The C library <c>open</c> function, without the optional mode argument.</summary>
    /// <param name="path">The path as NUL-terminated UTF-8.</param>
    /// <param name="flags">The flags.</param>
    /// <returns>The descriptor, or -1 with <c>errno</c> set.</returns>
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [DllImport("libc", EntryPoint = "open", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int OpenNative(byte[] path, int flags);

    /// <summary>The C library <c>fstat</c> function (Linux, and macOS as a fallback).</summary>
    /// <param name="descriptor">The descriptor.</param>
    /// <param name="statBuffer">The buffer for <c>struct stat</c>.</param>
    /// <returns>Zero on success.</returns>
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("macos")]
    [DllImport("libc", EntryPoint = "fstat", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int FStatNative(int descriptor, [Out] byte[] statBuffer);

    /// <summary>The glibc <c>__fxstat</c> function, for glibc before 2.33.</summary>
    /// <param name="version">The <c>_STAT_VER</c> value.</param>
    /// <param name="descriptor">The descriptor.</param>
    /// <param name="statBuffer">The buffer for <c>struct stat</c>.</param>
    /// <returns>Zero on success.</returns>
    [SupportedOSPlatform("linux")]
    [DllImport("libc", EntryPoint = "__fxstat", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int FXStatNative(int version, int descriptor, [Out] byte[] statBuffer);

    /// <summary>
    ///     The macOS <c>proc_pidfdinfo</c> function (implemented in the kernel system-call library of xnu,
    ///     re-exported through <c>libc</c>, which is <c>libSystem</c> on macOS).
    /// </summary>
    /// <param name="pid">The process.</param>
    /// <param name="descriptor">The descriptor.</param>
    /// <param name="flavor">The kind of information.</param>
    /// <param name="buffer">The buffer.</param>
    /// <param name="bufferSize">The size of the buffer.</param>
    /// <returns>The number of bytes filled in, or zero on failure.</returns>
    [SupportedOSPlatform("macos")]
    [DllImport("libc", EntryPoint = "proc_pidfdinfo", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern int ProcPidFdInfoNative(int pid, int descriptor, int flavor, [Out] byte[] buffer, int bufferSize);

    /// <summary>The C library <c>realpath</c> function, with a caller-owned buffer of <c>PATH_MAX</c> bytes.</summary>
    /// <param name="path">The path as NUL-terminated UTF-8.</param>
    /// <param name="resolvedPath">The buffer that receives the NUL-terminated resolved path.</param>
    /// <returns>The buffer's address on success, or zero (<c>NULL</c>) with <c>errno</c> set.</returns>
    [SupportedOSPlatform("macos")]
    [DllImport("libc", EntryPoint = "realpath", ExactSpelling = true, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    private static extern IntPtr RealPathNative(byte[] path, [Out] byte[] resolvedPath);
}
