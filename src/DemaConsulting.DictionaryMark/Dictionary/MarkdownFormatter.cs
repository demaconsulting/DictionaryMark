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
    /// <remarks>
    ///     Deduplication is performed first (first occurrence of each term wins, case-insensitive),
    ///     followed by optional alphabetical sorting, optional section heading emission, and
    ///     format dispatch to either bullet-list or table rendering.
    ///     <c>HeadingDepth</c> values outside the range 1–6 are clamped to that range by
    ///     <c>Math.Clamp</c> before use.
    ///     This method is thread-safe; all state is local to each invocation.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entries"/> or <paramref name="options"/> is null.</exception>
    public static string Format(IReadOnlyList<DictionaryEntry> entries, MarkdownOptions options)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(options);

        // Deduplicate (first occurrence wins, case-insensitive)
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduped = entries.Where(e => seen.Add(e.Term)).ToList();

        // Sort if requested
        IEnumerable<DictionaryEntry> ordered = options.SortOrder == SortOrder.Alphabetical
            ? deduped.OrderBy(e => e.Term, StringComparer.OrdinalIgnoreCase)
            : deduped;

        var sb = new System.Text.StringBuilder();

        // Optional section heading at configurable depth (1-6)
        if (!string.IsNullOrEmpty(options.SectionHeading))
        {
            var depth = Math.Clamp(options.HeadingDepth, 1, 6);
            sb.AppendLine($"{new string('#', depth)} {options.SectionHeading}");
            sb.AppendLine();
        }

        if (options.Format == OutputFormat.Table)
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
    /// <param name="sb">The output string builder to append to.</param>
    /// <param name="entries">The deduplicated and sorted entries to format.</param>
    /// <remarks>
    ///     Each entry is emitted as a single line: <c>- **{Term}**: {Definition}</c>.
    ///     No escaping is applied in bullet format.
    /// </remarks>
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
    /// <param name="sb">The output string builder to append to.</param>
    /// <param name="entries">The deduplicated and sorted entries to format.</param>
    /// <param name="options">The formatting options providing column header names.</param>
    /// <remarks>
    ///     Emits a header row, an alignment row (<c>| :--- | :--- |</c>), and one data row per entry.
    ///     When the entry list is empty, a single <c>| N/A | N/A |</c> row is emitted in place of
    ///     data rows to produce a valid non-empty table.
    ///     Pipe characters in term and definition values are escaped via <see cref="EscapePipe"/>.
    /// </remarks>
    private static void FormatTable(System.Text.StringBuilder sb, IEnumerable<DictionaryEntry> entries, MarkdownOptions options)
    {
        sb.AppendLine($"| {EscapePipe(options.TermHeader)} | {EscapePipe(options.DefinitionHeader)} |");
        sb.AppendLine("| :--- | :--- |");
        var entryList = entries.ToList();
        if (entryList.Count == 0)
        {
            sb.AppendLine("| N/A | N/A |");
        }
        else
        {
            foreach (var entry in entryList)
            {
                sb.AppendLine($"| {EscapePipe(entry.Term)} | {EscapePipe(entry.Definition)} |");
            }
        }
    }

    /// <summary>
    ///     Escapes pipe characters in Markdown table cells.
    /// </summary>
    /// <param name="value">The string in which to escape pipe characters.</param>
    /// <returns>The string with all pipe characters replaced by <c>\|</c>.</returns>
    /// <remarks>
    ///     Replaces each literal <c>|</c> with <c>\|</c> so that pipe characters in term or
    ///     definition values do not break the Markdown table structure.
    /// </remarks>
    private static string EscapePipe(string value) => value.Replace("|", @"\|");
}
