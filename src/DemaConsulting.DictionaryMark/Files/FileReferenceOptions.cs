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
///     Describes where a <see cref="FileReferenceResolver"/> looks for referenced files, which of them it may read, and
///     how large they may be.
/// </summary>
/// <remarks>
///     <para>
///         The file system is used: a relative reference is looked up in <see cref="BaseDirectory"/>; an absolute
///         reference names exactly one file. Every file that is found is checked by <see cref="FileAccess"/> after it
///         was opened.
///     </para>
///     <para>The record is immutable once built and may be shared by any number of resolvers and threads.</para>
/// </remarks>
internal sealed record FileReferenceOptions
{
    /// <summary>
    ///     The default value of <see cref="MaxFileBytes"/>: 16 MiB.
    /// </summary>
    public const long DefaultMaxFileBytes = 16L * 1024 * 1024;

    /// <summary>
    ///     Gets the directory a relative reference is resolved against, or <see langword="null"/> for none. It is a
    ///     starting point, not a boundary: <c>..</c> and absolute references may leave it, subject to
    ///     <see cref="FileAccess"/>.
    /// </summary>
    public string? BaseDirectory { get; init; }

    /// <summary>
    ///     Gets the policy that decides whether an opened file may be read. The default,
    ///     <see cref="FileAccessPolicy.AllowAll"/>, reads every regular local file. Must not be null.
    /// </summary>
    public FileAccessPolicy FileAccess { get; init; } = FileAccessPolicy.AllowAll;

    /// <summary>
    ///     Gets the largest file, in bytes, that may be read, whatever the policy. A longer file is refused with
    ///     <see cref="FileReferenceFailureKind.TooLarge"/>; reading stops one chunk past the limit, so a stream that
    ///     never ends cannot exhaust memory. Must be positive. The default is <see cref="DefaultMaxFileBytes"/>.
    /// </summary>
    public long MaxFileBytes { get; init; } = DefaultMaxFileBytes;
}
