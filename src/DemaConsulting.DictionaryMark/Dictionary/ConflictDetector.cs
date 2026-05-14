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
///     Detects conflicts in dictionary entries where the same term has different definitions.
/// </summary>
internal static class ConflictDetector
{
    /// <summary>
    ///     Detects conflicts among dictionary entries.
    /// </summary>
    /// <param name="entries">The entries to check.</param>
    /// <returns>List of conflict error messages (empty if no conflicts).</returns>
    /// <remarks>
    ///     Same term + same definition is NOT a conflict (it is a duplicate, which is allowed).
    ///     Same term + different definition IS a conflict.
    ///     Term comparison is case-insensitive.
    /// </remarks>
    public static IReadOnlyList<string> Detect(IEnumerable<DictionaryEntry> entries)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(entries);

        var termDefinitions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var conflicts = new List<string>();
        var reported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in entries)
        {
            if (termDefinitions.TryGetValue(entry.Term, out var existingDefinition))
            {
                // Same definition - no conflict (deduplication)
                if (string.Equals(existingDefinition, entry.Definition, StringComparison.Ordinal))
                {
                    continue;
                }

                // Different definition - conflict
                if (reported.Add(entry.Term))
                {
                    conflicts.Add($"Conflict: term '{entry.Term}' has multiple definitions");
                }
            }
            else
            {
                termDefinitions[entry.Term] = entry.Definition;
            }
        }

        return conflicts;
    }
}
