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
    ///     Approximate total number of separator dashes when column widths are given; chosen so a
    ///     weight reads naturally as a percentage of the table width.
    /// </summary>
    private const int TargetDashes = 100;

    /// <summary>
    ///     Minimum number of separator dashes per column; three dashes is the conventional minimum
    ///     that keeps the separator readable and recognized by all Markdown renderers.
    /// </summary>
    private const int MinimumDashes = 3;

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
    ///     <c>Math.Clamp</c> before use. When <see cref="MarkdownOptions.ColumnWidths"/> is set, the
    ///     table separator uses proportional dash counts; bullet output ignores the widths.
    ///     This method is thread-safe; all state is local to each invocation.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entries"/> or <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when table format is used with a column-width count other than 2.</exception>
    public static string Format(IReadOnlyList<DictionaryEntry> entries, MarkdownOptions options)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(options);

        // Omit invisible entries, then deduplicate (first occurrence wins, case-insensitive)
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduped = entries.Where(e => e.Visible).Where(e => seen.Add(e.Term)).ToList();

        // Sort if requested
        IEnumerable<DictionaryEntry> ordered = options.SortOrder == SortOrder.Alphabetical
            ? deduped.OrderBy(e => e.Term, StringComparer.OrdinalIgnoreCase)
            : deduped;

        var sb = new System.Text.StringBuilder();
        AppendHeading(sb, options);

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
    ///     Formats deprecated entries as Markdown text.
    /// </summary>
    /// <param name="entries">The deprecated entries to format.</param>
    /// <param name="options">The formatting options.</param>
    /// <returns>The formatted Markdown string.</returns>
    /// <remarks>
    ///     Deduplicates (first occurrence wins, case-insensitive), optionally sorts, emits the
    ///     optional section heading, then renders a table (<c>Not used | Use instead</c> plus
    ///     <c>Where it was used</c> only when any entry has a non-empty where value) or bullets
    ///     (<c>- **Term**: Use</c> followed by <c> (where: Where)</c> when present). Pipe characters
    ///     are escaped in table cells. With <see cref="MarkdownOptions.ColumnWidths"/> set (3 values),
    ///     the separator uses all 3 weights when the Where column is shown, and otherwise the first 2
    ///     weights rescaled over their own total. This method is thread-safe.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entries"/> or <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when table format is used with a column-width count other than 3.</exception>
    public static string FormatDeprecated(IReadOnlyList<DeprecatedEntry> entries, MarkdownOptions options)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(options);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var deduped = entries.Where(e => seen.Add(e.Term)).ToList();

        IEnumerable<DeprecatedEntry> ordered = options.SortOrder == SortOrder.Alphabetical
            ? deduped.OrderBy(e => e.Term, StringComparer.OrdinalIgnoreCase)
            : deduped;
        var list = ordered.ToList();

        var sb = new System.Text.StringBuilder();
        AppendHeading(sb, options);

        if (options.Format == OutputFormat.Table)
        {
            var showWhere = list.Any(e => !string.IsNullOrEmpty(e.Where));
            var widths = DeprecatedColumnWidths(options.ColumnWidths, showWhere);
            if (showWhere)
            {
                sb.AppendLine($"| {EscapePipe(options.TermHeader)} | {EscapePipe(options.UseHeader)} | {EscapePipe(options.WhereHeader)} |");
                sb.AppendLine(BuildSeparator(widths, 3));
            }
            else
            {
                sb.AppendLine($"| {EscapePipe(options.TermHeader)} | {EscapePipe(options.UseHeader)} |");
                sb.AppendLine(BuildSeparator(widths, 2));
            }

            if (list.Count == 0)
            {
                sb.AppendLine(showWhere ? "| N/A | N/A | N/A |" : "| N/A | N/A |");
            }

            foreach (var entry in list)
            {
                sb.AppendLine(showWhere
                    ? $"| {EscapePipe(entry.Term)} | {EscapePipe(entry.Use)} | {EscapePipe(entry.Where ?? string.Empty)} |"
                    : $"| {EscapePipe(entry.Term)} | {EscapePipe(entry.Use)} |");
            }
        }
        else
        {
            foreach (var entry in list)
            {
                var suffix = string.IsNullOrEmpty(entry.Where) ? string.Empty : $" (where: {entry.Where})";
                sb.AppendLine($"- **{entry.Term}**: {entry.Use}{suffix}");
            }
        }

        return sb.ToString();
    }

    /// <summary>
    ///     Appends the optional section heading (depth clamped to 1-6) followed by a blank line.
    /// </summary>
    private static void AppendHeading(System.Text.StringBuilder sb, MarkdownOptions options)
    {
        if (!string.IsNullOrEmpty(options.SectionHeading))
        {
            var depth = Math.Clamp(options.HeadingDepth, 1, 6);
            sb.AppendLine($"{new string('#', depth)} {options.SectionHeading}");
            sb.AppendLine();
        }
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
    ///     Emits a header row, an alignment row (<c>| :--- | :--- |</c>, or proportional dash counts
    ///     when <see cref="MarkdownOptions.ColumnWidths"/> is set), and one data row per entry.
    ///     When the entry list is empty, a single <c>| N/A | N/A |</c> row is emitted in place of
    ///     data rows to produce a valid non-empty table.
    ///     Pipe characters in term and definition values are escaped via <see cref="EscapePipe"/>.
    /// </remarks>
    private static void FormatTable(System.Text.StringBuilder sb, IEnumerable<DictionaryEntry> entries, MarkdownOptions options)
    {
        sb.AppendLine($"| {EscapePipe(options.TermHeader)} | {EscapePipe(options.DefinitionHeader)} |");
        sb.AppendLine(BuildSeparator(options.ColumnWidths, 2));
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
    ///     Selects the deprecated-table column widths that apply to the columns actually shown.
    /// </summary>
    /// <param name="widths">The configured widths (3 values) or null.</param>
    /// <param name="showWhere">Whether the Where column is shown.</param>
    /// <returns>
    ///     Null when no widths are configured; all 3 widths when the Where column is shown; otherwise
    ///     the first 2 widths, which <see cref="BuildSeparator"/> rescales over their own total.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when widths are configured but their count is not 3.</exception>
    private static IReadOnlyList<int>? DeprecatedColumnWidths(IReadOnlyList<int>? widths, bool showWhere)
    {
        if (widths == null)
        {
            return null;
        }

        // A deprecated table always has 3 configured weights, even when the Where column is dropped
        if (widths.Count != 3)
        {
            throw new ArgumentException(
                $"Column widths for deprecated tables require exactly 3 values, but {widths.Count} were given.",
                nameof(widths));
        }

        return showWhere ? widths : [widths[0], widths[1]];
    }

    /// <summary>
    ///     Builds the table alignment (separator) row.
    /// </summary>
    /// <remarks>
    ///     Without widths the fixed <c>| :--- |</c> row is returned so the default output is
    ///     unchanged byte for byte. With widths, each column gets a dash count proportional to its
    ///     weight, scaled so the counts total about <see cref="TargetDashes"/>:
    ///     <c>dashes = max(MinimumDashes, round_half_up(weight * TargetDashes / total))</c>, computed
    ///     with 64-bit integer arithmetic so the result is deterministic and cannot overflow. The
    ///     <c>:</c> left-alignment marker is kept on every column, so the row is always a valid pipe
    ///     table separator. For example, weights 25 and 75 give 25 and 75 dashes.
    /// </remarks>
    /// <param name="widths">The positive column weights, or null for the default separator.</param>
    /// <param name="columns">The number of table columns.</param>
    /// <returns>The separator row without a trailing newline.</returns>
    /// <exception cref="ArgumentException">Thrown when widths are given but their count differs from <paramref name="columns"/>.</exception>
    private static string BuildSeparator(IReadOnlyList<int>? widths, int columns)
    {
        // Default: the fixed separator used before column widths existed
        if (widths == null)
        {
            return columns == 3 ? "| :--- | :--- | :--- |" : "| :--- | :--- |";
        }

        if (widths.Count != columns)
        {
            throw new ArgumentException(
                $"Expected {columns} column widths, but {widths.Count} were given.", nameof(widths));
        }

        // Scale each weight to its share of the target, rounding half up, with a minimum per column
        var total = widths.Sum(w => (long)w);
        var sb = new System.Text.StringBuilder("|");
        foreach (var weight in widths)
        {
            var dashes = Math.Max(MinimumDashes, (int)((weight * 2L * TargetDashes + total) / (2L * total)));
            sb.Append(" :").Append('-', dashes).Append(" |");
        }

        return sb.ToString();
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
