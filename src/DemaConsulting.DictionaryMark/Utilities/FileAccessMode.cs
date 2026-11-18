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

namespace DemaConsulting.DictionaryMark.Utilities;

/// <summary>
///     The file-access mode chosen with <c>--file-access</c>: which files named in a configuration file the tool may
///     read and write.
/// </summary>
/// <remarks>
///     The flags combine: <see cref="Roots"/> and <see cref="NoSymlinks"/> together give the mode written
///     <c>roots,no-symlinks</c>. <see cref="None"/> (no flag, written <c>any</c>) is the default.
/// </remarks>
[Flags]
internal enum FileAccessMode
{
    /// <summary>
    ///     No restriction flag: the mode written <c>any</c> (the default), under which every regular local file may
    ///     be used and only the never-accessed list applies.
    /// </summary>
    None = 0,

    /// <summary>Only files whose written and real paths are inside the allowed folders may be used.</summary>
    Roots = 1,

    /// <summary>A path may not pass through a symbolic link or junction below the configuration file's folder.</summary>
    NoSymlinks = 2,
}
