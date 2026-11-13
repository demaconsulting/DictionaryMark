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
///     Detects conflicts among approved and deprecated dictionary entries.
/// </summary>
internal static class ConflictDetector
{
    /// <summary>
    ///     Detects conflicts among dictionary entries and, optionally, deprecated entries.
    /// </summary>
    /// <remarks>
    ///     Terms are compared case-insensitively. The same term in several places is not a conflict
    ///     when the entries are identical; it is a conflict when any of definition, pos, variants or
    ///     visible differs (definition by ordinal comparison; pos and variants as case-insensitive sets).
    ///     Deprecated entries conflict when use or where differ. A term that is both approved and
    ///     deprecated (or an approved variant that is deprecated) is a conflict. Entries are ordered
    ///     by source before comparison so the result does not depend on input order, and the returned
    ///     messages are sorted by ordinal comparison. Thread-safe; all state is method-local.
    /// </remarks>
    /// <param name="entries">The approved entries to check. Must not be null.</param>
    /// <param name="deprecated">Optional deprecated entries to check.</param>
    /// <returns>Sorted list of conflict messages (empty if no conflicts).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="entries"/> is null.</exception>
    public static IReadOnlyList<string> Detect(
        IEnumerable<DictionaryEntry> entries,
        IEnumerable<DeprecatedEntry>? deprecated = null)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(entries);

        var messages = new HashSet<string>(StringComparer.Ordinal);

        var approvedGroups = entries
            .GroupBy(e => e.Term, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(e => e.Source, StringComparer.Ordinal).ToList(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var group in approvedGroups.Values)
        {
            DetectApprovedConflicts(group, messages);
        }

        var deprecatedGroups = (deprecated ?? [])
            .GroupBy(e => e.Term, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(e => e.Source, StringComparer.Ordinal).ToList(),
                StringComparer.OrdinalIgnoreCase);

        foreach (var group in deprecatedGroups.Values)
        {
            DetectDeprecatedConflicts(group, messages);
        }

        DetectCrossConflicts(approvedGroups, deprecatedGroups, messages);

        return [.. messages.Order(StringComparer.Ordinal)];
    }

    /// <summary>
    ///     Compares every entry of a same-term group with the first one.
    /// </summary>
    private static void DetectApprovedConflicts(List<DictionaryEntry> group, HashSet<string> messages)
    {
        var first = group[0];
        foreach (var other in group.Skip(1))
        {
            var fields = new List<string>();
            if (!string.Equals(first.Definition, other.Definition, StringComparison.Ordinal))
            {
                fields.Add("definition");
            }

            if (!first.Pos.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(other.Pos))
            {
                fields.Add("pos");
            }

            if (!first.Variants.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(other.Variants))
            {
                fields.Add("variants");
            }

            if (first.Visible != other.Visible)
            {
                fields.Add("visible");
            }

            if (fields.Count > 0)
            {
                messages.Add(
                    $"Conflict: term '{first.Term}' differs in {string.Join(", ", fields)} between {Name(first.Source)} and {Name(other.Source)}.");
            }
        }
    }

    /// <summary>
    ///     Compares every deprecated entry of a same-term group with the first one.
    /// </summary>
    private static void DetectDeprecatedConflicts(List<DeprecatedEntry> group, HashSet<string> messages)
    {
        var first = group[0];
        foreach (var other in group.Skip(1))
        {
            var fields = new List<string>();
            if (!string.Equals(first.Use, other.Use, StringComparison.Ordinal))
            {
                fields.Add("use");
            }

            if (!string.Equals(first.Where ?? string.Empty, other.Where ?? string.Empty, StringComparison.Ordinal))
            {
                fields.Add("where");
            }

            if (fields.Count > 0)
            {
                messages.Add(
                    $"Conflict: deprecated term '{first.Term}' differs in {string.Join(", ", fields)} between {Name(first.Source)} and {Name(other.Source)}.");
            }
        }
    }

    /// <summary>
    ///     Detects terms or variants that are both approved and deprecated.
    /// </summary>
    private static void DetectCrossConflicts(
        Dictionary<string, List<DictionaryEntry>> approved,
        Dictionary<string, List<DeprecatedEntry>> deprecated,
        HashSet<string> messages)
    {
        if (deprecated.Count == 0)
        {
            return;
        }

        foreach (var group in approved.Values)
        {
            var first = group[0];
            if (deprecated.TryGetValue(first.Term, out var deprecatedGroup))
            {
                messages.Add(
                    $"Conflict: term '{first.Term}' is both approved ({Name(first.Source)}) and deprecated ({Name(deprecatedGroup[0].Source)}).");
            }

            foreach (var entry in group)
            {
                foreach (var variant in entry.Variants)
                {
                    if (deprecated.TryGetValue(variant, out var variantGroup))
                    {
                        messages.Add(
                            $"Conflict: variant '{variant}' of term '{entry.Term}' is approved ({Name(entry.Source)}) but also deprecated ({Name(variantGroup[0].Source)}).");
                    }
                }
            }
        }
    }

    /// <summary>
    ///     Renders a source path for a message.
    /// </summary>
    private static string Name(string source) => string.IsNullOrEmpty(source) ? "<unknown>" : source;
}
