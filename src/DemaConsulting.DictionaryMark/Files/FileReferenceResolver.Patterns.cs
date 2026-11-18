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
///     Pattern checking of <see cref="FileReferenceResolver"/>: the text rules a referenced file gets before it is
///     opened, applied to an input pattern before its folders are listed.
/// </summary>
/// <remarks>
///     This part is a DictionaryMark addition. It applies no new rule: it runs the resolver's own text rules (the never
///     list) and, on Windows, the reserved device-name rule a read applies to its full path, so a pattern that names a
///     network share, a device path, a reserved device name or a path with a colon is refused before any folder is
///     listed and no host or device is contacted.
/// </remarks>
internal sealed partial class FileReferenceResolver
{
    /// <summary>
    ///     Checks the text of an input pattern against the never list, without touching the file system.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         <b>Why it exists.</b> Listing the folders of a pattern such as <c>//host/share/*.yaml</c> contacts the
    ///         host before any match reaches the resolver, which on Windows can expose the user's credentials. The text
    ///         rules of a read are therefore applied to the pattern first.
    ///     </para>
    ///     <para>
    ///         The pattern is checked as given, so the caller passes the exact path that would be listed. Wildcards are
    ///         ordinary characters to these rules. On Windows, a component that is a reserved device name (<c>NUL</c>,
    ///         <c>CON</c>, <c>AUX</c>, <c>COM1</c>, <c>LPT1</c>, ...) is refused with the check a read applies to its
    ///         full path, because listing a folder below such a component would probe the device. The refusal has the
    ///         kind and reason the same rule gives a read.
    ///     </para>
    /// </remarks>
    /// <param name="pattern">The pattern, with variables expanded and rooted, exactly as it would be listed.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern"/> is null.</exception>
    /// <exception cref="FileReferenceException">
    ///     Thrown when the text breaks a never-list rule, or, on Windows, a component is a reserved device name.
    /// </exception>
    internal static void CheckPatternText(string pattern)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        // The same text rules as a read, so the pattern is refused before any folder is listed
        var shown = Shown(pattern);
        _ = ValidateText(pattern, shown);

        // The same reserved device-name check a read applies to its full path, so no device is probed while listing
        if (OperatingSystem.IsWindows() && RealPath.HasWindowsDeviceName(pattern))
        {
            throw new FileReferenceException(FileReferenceFailureKind.InvalidPath, shown, "the path names a device");
        }
    }
}
