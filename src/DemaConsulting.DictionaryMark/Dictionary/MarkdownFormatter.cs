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
///     Formats dictionary entries as Markdown.
/// </summary>
internal static class MarkdownFormatter
{
    /// <summary>
    ///     Formats dictionary entries as Markdown text.
    /// </summary>
    /// <param name="entries">The dictionary entries to format.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>The formatted Markdown string.</returns>
    public static string Format(IReadOnlyList<DictionaryEntry> entries, MarkdownOptions options)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(options);

        // Deduplicate (first occurrence wins, case-insensitive)
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduped = entries.Where(e => seen.Add(e.Term)).ToList();

        // Sort if requested
        IEnumerable<DictionaryEntry> ordered = options.SortOrder == Cli.SortOrder.Alphabetical
            ? deduped.OrderBy(e => e.Term, StringComparer.OrdinalIgnoreCase)
            : deduped;

        var sb = new System.Text.StringBuilder();

        // Optional section heading (level 2)
        if (!string.IsNullOrEmpty(options.SectionHeading))
        {
            sb.AppendLine($"## {options.SectionHeading}");
            sb.AppendLine();
        }

        if (options.Format == Cli.OutputFormat.Table)
        {
            FormatTable(sb, ordered, options);
        }
        else
        {
            FormatBullets(sb, ordered);
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Formats entries as a bullet list.
    /// </summary>
    private static void FormatBullets(System.Text.StringBuilder sb, IEnumerable<DictionaryEntry> entries)
    {
        foreach (var entry in entries)
        {
            sb.AppendLine($"- **{entry.Term}**: {entry.Definition}");
        }
    }

    /// <summary>
    ///     Formats entries as a Markdown table.
    /// </summary>
    private static void FormatTable(System.Text.StringBuilder sb, IEnumerable<DictionaryEntry> entries, MarkdownOptions options)
    {
        sb.AppendLine($"| {EscapePipe(options.TermHeader)} | {EscapePipe(options.DefinitionHeader)} |");
        sb.AppendLine("| :--- | :--- |");
        foreach (var entry in entries)
        {
            sb.AppendLine($"| {EscapePipe(entry.Term)} | {EscapePipe(entry.Definition)} |");
        }
    }

    /// <summary>
    ///     Escapes pipe characters in Markdown table cells.
    /// </summary>
    private static string EscapePipe(string value) => value.Replace("|", @"\|");
}
