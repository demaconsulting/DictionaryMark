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

using DemaConsulting.DictionaryMark.Cli;

namespace DemaConsulting.DictionaryMark.Dictionary;

/// <summary>
///     Options for Markdown output generation.
/// </summary>
public sealed class MarkdownOptions
{
    /// <summary>Gets or initializes the output format.</summary>
    public OutputFormat Format { get; init; } = OutputFormat.Bullets;

    /// <summary>Gets or initializes the optional section heading text.</summary>
    public string? SectionHeading { get; init; }

    /// <summary>Gets or initializes the term column header (table format only).</summary>
    public string TermHeader { get; init; } = "Term";

    /// <summary>Gets or initializes the definition column header (table format only).</summary>
    public string DefinitionHeader { get; init; } = "Definition";

    /// <summary>Gets or initializes the sort order.</summary>
    public SortOrder SortOrder { get; init; } = SortOrder.FileOrder;
}
