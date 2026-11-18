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
///     One fully resolved unit of generation work: which inputs to read, where to write, and how
///     to format the output.
/// </summary>
/// <remarks>
///     A run without a configuration file has a single command-line job. A configuration file
///     with <c>tables</c> produces one job per table. This class is the single place where the
///     formatting precedence (command line, then job, then configuration defaults, then built-in
///     defaults) is applied and where the column-width count is checked against the effective
///     table shape, so every job is validated the same way regardless of where it came from.
///     Instances are immutable and therefore thread-safe.
/// </remarks>
internal sealed class TableJob
{
    /// <summary>Term column header used for deprecated tables when no layer gives one.</summary>
    private const string DeprecatedTermHeader = "Not used";

    /// <summary>
    ///     Gets the 1-based position of the job in the configuration file, or null for the
    ///     command-line job.
    /// </summary>
    public int? Index { get; }

    /// <summary>Gets the input patterns, exactly as written by the user.</summary>
    public IReadOnlyList<string> InputPatterns { get; }

    /// <summary>Gets the output path as written by the user, or null to write to stdout.</summary>
    public string? OutputFile { get; }

    /// <summary>
    ///     Gets the directory that relative input patterns and the relative output path resolve
    ///     against (a root directory, the configuration file's directory, or the working directory).
    /// </summary>
    public string BaseDirectory { get; }

    /// <summary>
    ///     Gets the label used to prefix error messages (<c>table job {n} ('{output}')</c>), or null
    ///     for the command-line job so its messages are unchanged.
    /// </summary>
    public string? Label { get; }

    /// <summary>Gets the effective Markdown formatting options for this job.</summary>
    public MarkdownOptions Options { get; }

    /// <summary>
    ///     Gets a value indicating whether the job's input and output paths are judged by the file-access policy.
    /// </summary>
    /// <remarks>
    ///     Paths written in a configuration file (jobs with an <see cref="Index"/>) are judged by the
    ///     <c>--file-access</c> mode and the never-accessed list, because a configuration file may come from
    ///     someone the user does not trust. Paths given on the command line (the command-line job) are chosen by
    ///     the user running the tool and are never judged.
    /// </remarks>
    public bool ArePathsJudged => Index.HasValue;

    /// <summary>
    ///     Private constructor - use <see cref="Create"/> instead.
    /// </summary>
    /// <param name="index">The 1-based job index, or null for the command-line job.</param>
    /// <param name="inputPatterns">The input patterns.</param>
    /// <param name="outputFile">The output path, or null for stdout.</param>
    /// <param name="baseDirectory">The base directory for relative paths.</param>
    /// <param name="label">The error-message label, or null.</param>
    /// <param name="options">The effective formatting options.</param>
    private TableJob(
        int? index,
        IReadOnlyList<string> inputPatterns,
        string? outputFile,
        string baseDirectory,
        string? label,
        MarkdownOptions options)
    {
        Index = index;
        InputPatterns = inputPatterns;
        OutputFile = outputFile;
        BaseDirectory = baseDirectory;
        Label = label;
        Options = options;
    }

    /// <summary>
    ///     Creates a job by merging formatting layers and validating the result.
    /// </summary>
    /// <remarks>
    ///     The layers are merged in the order given, so the first non-null layer value wins. Built-in
    ///     defaults fill any member that no layer specifies: bullets, no section heading, depth 1,
    ///     file order, terms, <c>Term</c> (or <c>Not used</c> when the effective kind is deprecated),
    ///     <c>Definition</c>, <c>Use instead</c>, <c>Where it was used</c> and no column widths. When
    ///     the effective format is table, the column-width count must match the table shape (2 for
    ///     terms, 3 for deprecated). When the effective format is bullets, the widths are ignored.
    /// </remarks>
    /// <param name="index">
    ///     The 1-based position of the job in the configuration file, or null for the command-line
    ///     job. A non-null index gives the job the label <c>table job {index} ('{output}')</c>.
    /// </param>
    /// <param name="inputPatterns">The input patterns. Must not be null or empty.</param>
    /// <param name="outputFile">The output path as written, or null to write to stdout.</param>
    /// <param name="baseDirectory">The base directory for relative inputs and output. Must not be null.</param>
    /// <param name="layers">
    ///     The formatting layers, highest precedence first. Must not be null; null elements are
    ///     skipped.
    /// </param>
    /// <returns>The validated job.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="inputPatterns"/> is empty, or the column-width count does not
    ///     match the effective table shape; the message names the job when it has a label.
    /// </exception>
    public static TableJob Create(
        int? index,
        IReadOnlyList<string> inputPatterns,
        string? outputFile,
        string baseDirectory,
        IReadOnlyList<FormattingSettings?> layers)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(inputPatterns);
        ArgumentNullException.ThrowIfNull(baseDirectory);
        ArgumentNullException.ThrowIfNull(layers);
        if (inputPatterns.Count == 0)
        {
            throw new ArgumentException("A table job requires at least one input pattern.", nameof(inputPatterns));
        }

        // Merge the layers so the highest-precedence non-null value wins
        var merged = FormattingSettings.Empty;
        for (var i = layers.Count - 1; i >= 0; i--)
        {
            var layer = layers[i];
            if (layer != null)
            {
                merged = layer.Over(merged);
            }
        }

        // Fill unspecified members with the built-in defaults
        var label = index.HasValue ? $"table job {index.Value} ('{outputFile}')" : null;
        var format = merged.Format ?? OutputFormat.Bullets;
        var kind = merged.Kind ?? InputKind.Terms;
        var termHeader = merged.TermHeader ?? (kind == InputKind.Deprecated ? DeprecatedTermHeader : "Term");

        // Check the column-width count against the table shape; bullets ignore the widths
        var widths = format == OutputFormat.Table ? merged.ColumnWidths : null;
        ValidateColumnWidthCount(widths, kind, label);

        var options = new MarkdownOptions
        {
            Format = format,
            SectionHeading = merged.SectionHeading,
            HeadingDepth = merged.HeadingDepth ?? 1,
            TermHeader = termHeader,
            DefinitionHeader = merged.DefinitionHeader ?? "Definition",
            SortOrder = merged.SortBy ?? SortOrder.FileOrder,
            Kind = kind,
            UseHeader = merged.UseHeader ?? "Use instead",
            WhereHeader = merged.WhereHeader ?? "Where it was used",
            ColumnWidths = widths
        };

        return new TableJob(index, [.. inputPatterns], outputFile, baseDirectory, label, options);
    }

    /// <summary>
    ///     Requires the column-width count to equal the number of table columns for the kind.
    /// </summary>
    /// <param name="widths">The widths to check, or null when none apply.</param>
    /// <param name="kind">The effective input kind.</param>
    /// <param name="label">The job label for the error message, or null.</param>
    /// <exception cref="ArgumentException">Thrown when the count does not match.</exception>
    private static void ValidateColumnWidthCount(IReadOnlyList<int>? widths, InputKind kind, string? label)
    {
        if (widths == null)
        {
            return;
        }

        // Terms tables have 2 columns; deprecated tables have 3 (the Where column may be dropped later)
        var expected = kind == InputKind.Deprecated ? 3 : 2;
        if (widths.Count != expected)
        {
            var kindName = kind == InputKind.Deprecated ? "deprecated" : "terms";
            var prefix = label != null ? $"{label}: " : string.Empty;
            throw new ArgumentException(
                $"{prefix}Column widths for {kindName} tables require exactly {expected} values, but {widths.Count} were given.");
        }
    }
}
