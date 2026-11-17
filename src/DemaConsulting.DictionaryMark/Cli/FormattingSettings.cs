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

using DemaConsulting.DictionaryMark.Dictionary;

namespace DemaConsulting.DictionaryMark.Cli;

/// <summary>
///     One precedence layer of formatting settings, in which every member is optional.
/// </summary>
/// <remarks>
///     Formatting settings can come from the command line, from a table job in a configuration
///     file, from the configuration file's top-level defaults, or from the built-in defaults. Each
///     of the first three sources is represented by one instance of this record, where a
///     <c>null</c> member means "not specified at this layer". <see cref="Over"/> merges two layers
///     so that the higher-precedence layer wins wherever it specifies a value. Instances are
///     immutable and therefore thread-safe.
/// </remarks>
internal sealed record FormattingSettings
{
    /// <summary>Gets an empty layer that specifies no settings.</summary>
    public static FormattingSettings Empty { get; } = new();

    /// <summary>Gets the output format, or null when not specified.</summary>
    public OutputFormat? Format { get; init; }

    /// <summary>Gets the section heading text, or null when not specified.</summary>
    public string? SectionHeading { get; init; }

    /// <summary>Gets the section heading depth (1-6), or null when not specified.</summary>
    public int? HeadingDepth { get; init; }

    /// <summary>Gets the sort order, or null when not specified.</summary>
    public SortOrder? SortBy { get; init; }

    /// <summary>Gets the term column header, or null when not specified.</summary>
    public string? TermHeader { get; init; }

    /// <summary>Gets the definition column header, or null when not specified.</summary>
    public string? DefinitionHeader { get; init; }

    /// <summary>Gets the input kind, or null when not specified.</summary>
    public InputKind? Kind { get; init; }

    /// <summary>Gets the "use instead" column header, or null when not specified.</summary>
    public string? UseHeader { get; init; }

    /// <summary>Gets the "where used" column header, or null when not specified.</summary>
    public string? WhereHeader { get; init; }

    /// <summary>
    ///     Gets the relative table column widths (2 or 3 positive integers), or null when not specified.
    /// </summary>
    public IReadOnlyList<int>? ColumnWidths { get; init; }

    /// <summary>
    ///     Merges this layer over a lower-precedence layer.
    /// </summary>
    /// <remarks>
    ///     Each member of the result is this layer's value when it is not null, and otherwise the
    ///     lower layer's value. Neither input is modified.
    /// </remarks>
    /// <param name="lower">The lower-precedence layer. Must not be null.</param>
    /// <returns>A new layer in which this layer's non-null values win.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="lower"/> is null.</exception>
    public FormattingSettings Over(FormattingSettings lower)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(lower);

        // Take each member from this layer when specified, otherwise from the lower layer
        return new FormattingSettings
        {
            Format = Format ?? lower.Format,
            SectionHeading = SectionHeading ?? lower.SectionHeading,
            HeadingDepth = HeadingDepth ?? lower.HeadingDepth,
            SortBy = SortBy ?? lower.SortBy,
            TermHeader = TermHeader ?? lower.TermHeader,
            DefinitionHeader = DefinitionHeader ?? lower.DefinitionHeader,
            Kind = Kind ?? lower.Kind,
            UseHeader = UseHeader ?? lower.UseHeader,
            WhereHeader = WhereHeader ?? lower.WhereHeader,
            ColumnWidths = ColumnWidths ?? lower.ColumnWidths
        };
    }
}
