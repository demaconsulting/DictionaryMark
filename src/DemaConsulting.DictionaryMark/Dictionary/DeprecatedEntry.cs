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

namespace DemaConsulting.DictionaryMark.Dictionary;

/// <summary>
///     Represents a deprecated term and the replacement to use instead.
/// </summary>
/// <remarks>
///     Immutable; once constructed no property can change. Thread-safe for concurrent reads.
/// </remarks>
public sealed class DeprecatedEntry
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DeprecatedEntry"/> class.
    /// </summary>
    /// <param name="term">The deprecated term. Must not be null.</param>
    /// <param name="use">The replacement to use instead. Must not be null.</param>
    /// <param name="where">Optional note on where the deprecated term was used.</param>
    /// <param name="source">Optional resolved path of the file the entry came from.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="term"/> or <paramref name="use"/> is null.
    /// </exception>
    public DeprecatedEntry(string term, string use, string? where = null, string source = "")
    {
        ArgumentNullException.ThrowIfNull(term);
        ArgumentNullException.ThrowIfNull(use);
        Term = term;
        Use = use;
        Where = where;
        Source = source ?? string.Empty;
    }

    /// <summary>Gets the deprecated term; never null.</summary>
    public string Term { get; }

    /// <summary>Gets the replacement to use instead; never null.</summary>
    public string Use { get; }

    /// <summary>Gets the optional note on where the term was used; null when absent.</summary>
    public string? Where { get; }

    /// <summary>Gets the resolved path of the source file; empty when unknown.</summary>
    public string Source { get; }
}
