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
///     A referenced file that <see cref="FileReferenceResolver"/> found, checked and read: its identity, its real
///     location and its bytes.
/// </summary>
/// <remarks>
///     The same instance is returned every time the same file is resolved again by one resolver, so reference
///     equality (or <see cref="Key"/> equality under the platform's path comparison) identifies a file reached through
///     different spellings of one path. Two different paths that reach one file through a link have different keys but
///     the same <see cref="RealPath"/>. The type is immutable; <see cref="Content"/> is shared and must not be modified
///     by unsafe means. It is safe to use from any thread.
/// </remarks>
internal sealed class FileReference
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FileReference"/> class.
    /// </summary>
    /// <param name="key">The identity of the file.</param>
    /// <param name="realPath">The real path, or <see langword="null"/>.</param>
    /// <param name="content">The bytes of the file.</param>
    internal FileReference(string key, string? realPath, ReadOnlyMemory<byte> content)
    {
        Key = key;
        RealPath = realPath;
        Content = content;
    }

    /// <summary>
    ///     Gets the identity of the file: its full path as written (<c>.</c> and <c>..</c> collapsed, links not
    ///     resolved). It may reveal the host's directories.
    /// </summary>
    public string Key { get; }

    /// <summary>
    ///     Gets the real path of the file as reported for the open handle (links, junctions and short names resolved),
    ///     or <see langword="null"/> where the platform cannot tell. It may reveal the host's directories.
    /// </summary>
    public string? RealPath { get; }

    /// <summary>
    ///     Gets the bytes of the file, at most <see cref="FileReferenceOptions.MaxFileBytes"/> of them. Decoding them to
    ///     text is left to the caller, which knows the expected encoding.
    /// </summary>
    public ReadOnlyMemory<byte> Content { get; }
}
