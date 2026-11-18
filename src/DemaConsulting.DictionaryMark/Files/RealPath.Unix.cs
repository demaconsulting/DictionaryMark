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
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace DemaConsulting.DictionaryMark.Files;

/// <summary>
///     The Linux facts of an open handle (the macOS facts come from one native call, see
///     <see cref="NativeMethods.TryGetMacVnodeInfo"/>).
/// </summary>
internal static partial class RealPath
{
    /// <summary>
    ///     Reports the facts of an open Linux descriptor: the file type and link count from <c>fstat</c>, and the final
    ///     path from the <c>/proc/self/fd</c> link of the descriptor.
    /// </summary>
    /// <param name="handle">The open handle.</param>
    /// <returns>
    ///     The facts; the path is <see langword="null"/> when <c>/proc</c> is not mounted, when the file was deleted
    ///     (its link count is zero), or when the type and link count cannot be read.
    /// </returns>
    [SupportedOSPlatform("linux")]
    private static HandleFacts InspectLinux(SafeFileHandle handle)
    {
        // The link of the descriptor names the file the descriptor refers to now, whatever happened to the name used to
        // open it. It is read before fstat, so a file deleted between the two calls is seen as deleted
        var target = NativeMethods.WithDescriptor(
            handle,
            descriptor => new FileInfo("/proc/self/fd/" + descriptor.ToString(CultureInfo.InvariantCulture)).LinkTarget);
        var stat = NativeMethods.TryGetLinuxStat(handle);
        var isRegular = IsRegularMode(stat?.Mode);

        // Deletion is decided from the open file (no name links to it any more), never from the " (deleted)" text Linux
        // appends to the link, which a live file may carry in its own name; the link text is kept verbatim
        var path = stat is { LinkCount: > 0 } && target is not null && target.StartsWith('/') ? target : null;
        return new HandleFacts(isRegular, path);
    }
}
