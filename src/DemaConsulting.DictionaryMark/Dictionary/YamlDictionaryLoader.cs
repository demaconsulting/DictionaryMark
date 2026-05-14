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

using YamlDotNet.RepresentationModel;

namespace DemaConsulting.DictionaryMark.Dictionary;

/// <summary>
///     Loads dictionary entries from YAML files.
/// </summary>
internal static class YamlDictionaryLoader
{
    /// <summary>
    ///     Loads dictionary entries from a YAML file.
    /// </summary>
    /// <param name="filePath">Path to the YAML file.</param>
    /// <returns>List of dictionary entries in file order.</returns>
    /// <exception cref="IOException">Thrown when the file cannot be read.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the YAML structure is not a flat key-value mapping or contains duplicate keys.</exception>
    public static IReadOnlyList<DictionaryEntry> Load(string filePath)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(filePath);

        // Read file content (let IOException propagate)
        var content = File.ReadAllText(filePath);

        // Parse YAML
        var yaml = new YamlStream();
        using var reader = new StringReader(content);
        yaml.Load(reader);

        // Handle empty files
        if (yaml.Documents.Count == 0)
        {
            return [];
        }

        // Get root node
        var root = yaml.Documents[0].RootNode;

        // Must be a mapping node
        if (root is not YamlMappingNode mappingNode)
        {
            throw new InvalidOperationException($"File '{filePath}' must contain a flat YAML mapping, but found {root.NodeType}.");
        }

        // Track seen terms to detect duplicates
        var seenTerms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entries = new List<DictionaryEntry>();

        foreach (var child in mappingNode.Children)
        {
            // Key must be scalar
            if (child.Key is not YamlScalarNode keyNode)
            {
                throw new InvalidOperationException($"File '{filePath}' contains a non-scalar key.");
            }

            // Value must be scalar
            if (child.Value is not YamlScalarNode valueNode)
            {
                throw new InvalidOperationException($"File '{filePath}' must contain a flat YAML mapping (no nested structures). Key '{keyNode.Value}' has a non-scalar value.");
            }

            var term = keyNode.Value ?? string.Empty;
            var definition = valueNode.Value ?? string.Empty;

            // Check for duplicate key
            if (!seenTerms.Add(term))
            {
                throw new InvalidOperationException($"File '{filePath}' contains duplicate key '{term}'.");
            }

            entries.Add(new DictionaryEntry(term, definition));
        }

        return entries;
    }
}
