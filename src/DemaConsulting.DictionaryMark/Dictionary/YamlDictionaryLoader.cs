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

using System.Text.RegularExpressions;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace DemaConsulting.DictionaryMark.Dictionary;

/// <summary>
///     Loads dictionary entries from YAML files.
/// </summary>
/// <remarks>
///     Static utility class with no instance state. <see cref="Load"/> reads approved-term files
///     (a term maps to a definition string or to an object with <c>definition</c>, <c>pos</c>,
///     <c>variants</c> and <c>visible</c>); <see cref="LoadDeprecated"/> reads deprecated-term files
///     (a term maps to an object with <c>use</c> and optional <c>where</c>). Validation happens at
///     load time so downstream consumers always receive validated, unambiguous data.
/// </remarks>
internal static partial class YamlDictionaryLoader
{
    /// <summary>Parts of speech accepted in the <c>pos</c> field.</summary>
    private static readonly string[] AllowedPos = ["noun", "verb", "adjective", "adverb", "proper-noun"];

    /// <summary>Keys allowed in an approved-term object.</summary>
    private static readonly string[] TermKeys = ["definition", "pos", "variants", "visible"];

    /// <summary>Keys allowed in a deprecated-term object.</summary>
    private static readonly string[] DeprecatedKeys = ["use", "where"];

    /// <summary>
    ///     Loads approved-term entries from a YAML file.
    /// </summary>
    /// <remarks>
    ///     Reads the entire file into memory before parsing so that I/O errors are reported
    ///     immediately. The root must be a mapping of scalar term to either a non-empty definition
    ///     string or an object. A definition is required for every entry, regardless of
    ///     <c>visible</c>; null and empty values are errors. Terms are whitespace-normalized and
    ///     duplicate detection is case-insensitive. Stateless and thread-safe.
    /// </remarks>
    /// <param name="filePath">Absolute or relative path to the YAML file. Must not be null.</param>
    /// <returns>
    ///     Ordered list of <see cref="DictionaryEntry"/> objects in document order, with
    ///     <see cref="DictionaryEntry.Source"/> set to the full path; empty when the file has no entries.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the YAML structure or any entry is invalid; the message names the term and file.
    /// </exception>
    public static IReadOnlyList<DictionaryEntry> Load(string filePath)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(filePath);

        var source = Path.GetFullPath(filePath);
        var mapping = ReadRootMapping(filePath, source);
        if (mapping == null)
        {
            return [];
        }

        var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<DictionaryEntry>();

        foreach (var child in mapping.Children)
        {
            var term = ReadTerm(child.Key, source, "Glossary");
            if (!seenTerms.Add(term))
            {
                throw new InvalidOperationException($"File '{source}' contains duplicate key '{term}'.");
            }

            entries.Add(ParseEntry(term, child.Value, source));
        }

        return entries;
    }

    /// <summary>
    ///     Loads deprecated-term entries from a YAML file.
    /// </summary>
    /// <remarks>
    ///     The root must be a mapping of scalar term to an object with a required non-empty
    ///     <c>use</c> string and an optional <c>where</c> string. Deprecated entries never carry a
    ///     definition. Stateless and thread-safe.
    /// </remarks>
    /// <param name="filePath">Absolute or relative path to the YAML file. Must not be null.</param>
    /// <returns>Ordered list of <see cref="DeprecatedEntry"/> objects in document order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the YAML structure or any entry is invalid; the message names the term and file.
    /// </exception>
    public static IReadOnlyList<DeprecatedEntry> LoadDeprecated(string filePath)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(filePath);

        var source = Path.GetFullPath(filePath);
        var mapping = ReadRootMapping(filePath, source);
        if (mapping == null)
        {
            return [];
        }

        var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<DeprecatedEntry>();

        foreach (var child in mapping.Children)
        {
            var term = ReadTerm(child.Key, source, "Deprecated-terms file");
            if (!seenTerms.Add(term))
            {
                throw new InvalidOperationException($"File '{source}' contains duplicate key '{term}'.");
            }

            entries.Add(ParseDeprecated(term, child.Value, source));
        }

        return entries;
    }

    /// <summary>
    ///     Reads a file and returns its root mapping node, or null for an empty file.
    /// </summary>
    private static YamlMappingNode? ReadRootMapping(string filePath, string source)
    {
        // Read file content (let IOException propagate)
        var content = File.ReadAllText(filePath);

        var yaml = new YamlStream();
        using var reader = new StringReader(content);
        yaml.Load(reader);

        if (yaml.Documents.Count == 0)
        {
            return null;
        }

        var root = yaml.Documents[0].RootNode;
        if (root is YamlMappingNode mappingNode)
        {
            return mappingNode;
        }

        // An empty document parses as a null scalar; treat as no entries
        if (root is YamlScalarNode { Value: null or "" } scalar && scalar.Style == ScalarStyle.Plain)
        {
            return null;
        }

        throw new InvalidOperationException($"File '{source}' must contain a YAML mapping of terms, but found {root.NodeType}.");
    }

    /// <summary>
    ///     Reads and whitespace-normalizes a term key.
    /// </summary>
    private static string ReadTerm(YamlNode key, string source, string fileDescription)
    {
        if (key is not YamlScalarNode keyNode)
        {
            throw new InvalidOperationException($"File '{source}' contains a non-scalar key.");
        }

        var term = Normalize(keyNode.Value);
        if (term.Length == 0)
        {
            throw new InvalidOperationException($"{fileDescription} in '{source}' contains an empty term.");
        }

        return term;
    }

    /// <summary>
    ///     Parses an approved-term value (definition string or object).
    /// </summary>
    private static DictionaryEntry ParseEntry(string term, YamlNode value, string source)
    {
        switch (value)
        {
            case YamlScalarNode scalar:
                {
                    var definition = RequireDefinition(term, source, IsNull(scalar) ? null : scalar.Value);
                    return new DictionaryEntry(term, definition, null, null, true, source);
                }

            case YamlMappingNode map:
                return ParseObjectEntry(term, map, source);

            default:
                throw Invalid(term, source, "an entry must be a definition string or an object");
        }
    }

    /// <summary>
    ///     Parses the object form of an approved-term entry.
    /// </summary>
    private static DictionaryEntry ParseObjectEntry(string term, YamlMappingNode map, string source)
    {
        string? definition = null;
        var pos = new List<string>();
        var variants = new List<string>();
        var visible = true;
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in map.Children)
        {
            var key = (field.Key as YamlScalarNode)?.Value ?? string.Empty;
            if (!TermKeys.Contains(key) || !seenKeys.Add(key))
            {
                throw new InvalidOperationException(
                    $"Term '{term}' in '{source}' has unknown key '{key}'; allowed keys are {string.Join(", ", TermKeys)}.");
            }

            switch (key)
            {
                case "definition":
                    if (field.Value is not YamlScalarNode defNode || IsNull(defNode))
                    {
                        throw Invalid(term, source, "'definition' must be a string");
                    }

                    definition = defNode.Value;
                    break;

                case "pos":
                    pos = ParsePos(term, source, field.Value);
                    break;

                case "variants":
                    variants = ParseVariants(term, source, field.Value);
                    break;

                default:
                    visible = ParseVisible(term, source, field.Value);
                    break;
            }
        }

        // A definition is required for every approved term, regardless of visibility
        definition = RequireDefinition(term, source, definition);
        return new DictionaryEntry(term, definition, pos, variants, visible, source);
    }

    /// <summary>
    ///     Validates that a definition is present and non-empty.
    /// </summary>
    private static string RequireDefinition(string term, string source, string? definition)
    {
        if (string.IsNullOrWhiteSpace(definition))
        {
            throw Invalid(term, source, "'definition' is required and must be a non-empty string");
        }

        return definition;
    }

    /// <summary>
    ///     Parses the <c>pos</c> field: one string or a list of strings.
    /// </summary>
    private static List<string> ParsePos(string term, string source, YamlNode node)
    {
        var raw = ReadStringOrList(term, source, "pos", node, "a string or a list of strings");
        var result = new List<string>();
        foreach (var item in raw)
        {
            var canonical = item.Trim().ToLowerInvariant();
            if (!AllowedPos.Contains(canonical))
            {
                throw Invalid(term, source, $"'pos' has unsupported value '{item}'; use noun, verb, adjective, adverb, or proper-noun");
            }

            if (!result.Contains(canonical))
            {
                result.Add(canonical);
            }
        }

        if (result.Count == 0)
        {
            throw Invalid(term, source, "'pos' must not be empty");
        }

        return result;
    }

    /// <summary>
    ///     Parses the <c>variants</c> field: whitespace-normalized, de-duplicated case-insensitively.
    /// </summary>
    private static List<string> ParseVariants(string term, string source, YamlNode node)
    {
        var raw = ReadStringOrList(term, source, "variants", node, "a list of strings");
        var result = new List<string>();
        foreach (var item in raw)
        {
            var normalized = Normalize(item);
            if (normalized.Length > 0 && !result.Contains(normalized, StringComparer.OrdinalIgnoreCase))
            {
                result.Add(normalized);
            }
        }

        return result;
    }

    /// <summary>
    ///     Parses the <c>visible</c> field: a boolean or the strings true/false.
    /// </summary>
    private static bool ParseVisible(string term, string source, YamlNode node)
    {
        if (node is YamlScalarNode scalar && !IsNull(scalar))
        {
            var text = (scalar.Value ?? string.Empty).Trim();
            if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        throw Invalid(term, source, "'visible' must be true or false");
    }

    /// <summary>
    ///     Reads a node that must be a single string or a list of strings.
    /// </summary>
    private static List<string> ReadStringOrList(string term, string source, string field, YamlNode node, string expected)
    {
        switch (node)
        {
            case YamlScalarNode scalar when !IsNull(scalar):
                return [scalar.Value ?? string.Empty];

            case YamlSequenceNode sequence:
                var items = new List<string>();
                foreach (var child in sequence.Children)
                {
                    if (child is not YamlScalarNode item || IsNull(item))
                    {
                        throw Invalid(term, source, $"'{field}' must be {expected}");
                    }

                    items.Add(item.Value ?? string.Empty);
                }

                return items;

            default:
                throw Invalid(term, source, $"'{field}' must be {expected}");
        }
    }

    /// <summary>
    ///     Parses a deprecated-term value.
    /// </summary>
    private static DeprecatedEntry ParseDeprecated(string term, YamlNode value, string source)
    {
        if (value is not YamlMappingNode map)
        {
            throw new InvalidOperationException(
                $"Deprecated term '{term}' in '{source}' is invalid: a deprecated entry must be an object with 'use' and optional 'where'.");
        }

        string? use = null;
        string? where = null;
        var seenKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in map.Children)
        {
            var key = (field.Key as YamlScalarNode)?.Value ?? string.Empty;
            if (!DeprecatedKeys.Contains(key) || !seenKeys.Add(key))
            {
                throw new InvalidOperationException(
                    $"Deprecated term '{term}' in '{source}' has unknown key '{key}'; allowed keys are {string.Join(", ", DeprecatedKeys)}.");
            }

            if (field.Value is not YamlScalarNode scalar || IsNull(scalar))
            {
                if (key == "where")
                {
                    continue;
                }

                throw new InvalidOperationException($"Deprecated term '{term}' in '{source}' must have a non-empty 'use' value.");
            }

            if (key == "use")
            {
                use = scalar.Value?.Trim();
            }
            else
            {
                where = scalar.Value?.Trim();
            }
        }

        if (string.IsNullOrWhiteSpace(use))
        {
            throw new InvalidOperationException($"Deprecated term '{term}' in '{source}' must have a non-empty 'use' value.");
        }

        return new DeprecatedEntry(term, use, string.IsNullOrWhiteSpace(where) ? null : where, source);
    }

    /// <summary>
    ///     Determines whether a scalar is a YAML null (empty or an unquoted null token).
    /// </summary>
    private static bool IsNull(YamlScalarNode scalar)
    {
        if (scalar.Style != ScalarStyle.Plain)
        {
            return false;
        }

        return scalar.Value is null or "" or "~"
            || string.Equals(scalar.Value, "null", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Trims a string and collapses internal whitespace runs to a single space.
    /// </summary>
    private static string Normalize(string? value) =>
        string.IsNullOrEmpty(value) ? string.Empty : WhitespaceRegex().Replace(value.Trim(), " ");

    /// <summary>
    ///     Creates the standard field-error exception.
    /// </summary>
    private static InvalidOperationException Invalid(string term, string source, string problem) =>
        new($"Term '{term}' in '{source}' is invalid: {problem}.");

    /// <summary>Matches runs of whitespace.</summary>
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
