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

using System.Globalization;
using DemaConsulting.DictionaryMark.Dictionary;
using DemaConsulting.DictionaryMark.Utilities;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace DemaConsulting.DictionaryMark.Cli;

/// <summary>
///     A validated YAML configuration file: formatting defaults, an optional root directory, and
///     optional table jobs.
/// </summary>
/// <remarks>
///     This is the only code that knows the configuration file schema. Loading is fail-fast: the
///     first problem stops loading with an <see cref="InvalidOperationException"/> whose message
///     starts with <c>Config file '{full path}': </c> and, for problems inside a job,
///     <c>table job {n}: </c>, and names the offending key. Input and output strings are kept as
///     written, because they are resolved when the job runs so that a failure in one job does not
///     stop the others. Instances are immutable and therefore thread-safe.
/// </remarks>
internal sealed class ConfigFile
{
    /// <summary>Description of the root key used in path-resolution error messages.</summary>
    private const string RootKeyDescription = "root key";

    /// <summary>Command-line-only options that may not appear as configuration keys.</summary>
    private static readonly string[] CommandLineOnlyKeys = ["log", "results", "validate", "silent", "version", "help"];

    /// <summary>
    ///     One table job as written in the configuration file.
    /// </summary>
    /// <param name="Index">The 1-based position of the job in the <c>tables</c> sequence.</param>
    /// <param name="Inputs">The input patterns as written (at least one, each non-empty).</param>
    /// <param name="Output">The output path as written (non-empty).</param>
    /// <param name="Settings">The formatting keys given in the job.</param>
    public sealed record ConfigTable(int Index, IReadOnlyList<string> Inputs, string Output, FormattingSettings Settings);

    /// <summary>Gets the full path of the configuration file.</summary>
    public string FilePath { get; }

    /// <summary>Gets the directory that contains the configuration file.</summary>
    public string BaseDirectory { get; }

    /// <summary>
    ///     Gets the resolved, existing full path of the top-level <c>root</c> key, or null when the
    ///     key is absent.
    /// </summary>
    public string? RootDirectory { get; }

    /// <summary>Gets the top-level formatting defaults.</summary>
    public FormattingSettings Defaults { get; }

    /// <summary>Gets the table jobs in file order; empty when the file has no <c>tables</c> key.</summary>
    public IReadOnlyList<ConfigTable> Tables { get; }

    /// <summary>
    ///     Private constructor - use <see cref="Load"/> instead.
    /// </summary>
    /// <param name="filePath">The full path of the file.</param>
    /// <param name="rootDirectory">The resolved root directory, or null.</param>
    /// <param name="defaults">The top-level formatting defaults.</param>
    /// <param name="tables">The table jobs.</param>
    private ConfigFile(string filePath, string? rootDirectory, FormattingSettings defaults, IReadOnlyList<ConfigTable> tables)
    {
        FilePath = filePath;
        BaseDirectory = Path.GetDirectoryName(filePath) ?? filePath;
        RootDirectory = rootDirectory;
        Defaults = defaults;
        Tables = tables;
    }

    /// <summary>
    ///     Loads and validates a configuration file.
    /// </summary>
    /// <remarks>
    ///     Reads the first YAML document of the file. Its root must be a mapping. Allowed top-level
    ///     keys are the formatting keys (<c>format</c>, <c>section</c>, <c>depth</c>, <c>sort</c>,
    ///     <c>term-header</c>, <c>def-header</c> or <c>definition-header</c>, <c>kind</c>,
    ///     <c>use-header</c>, <c>where-header</c>, <c>column-widths</c>), <c>root</c> and
    ///     <c>tables</c>. The <c>root</c> value is resolved relative to the file's directory, with
    ///     <c>${VAR}</c> expansion and <c>..</c> normalization, must not be on the file-access
    ///     never-accessed list, and must name an existing directory.
    ///     Performs file-system I/O; stateless and thread-safe.
    /// </remarks>
    /// <param name="path">Path of the configuration file; normally already a full path. Must not be null.</param>
    /// <returns>The validated configuration.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="path"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the path is malformed, the file is missing or unreadable, is not valid YAML, is empty, does not have a
    ///     mapping root, or contains an unknown, disallowed, missing or invalid key (including a malformed or missing
    ///     root directory); the message names the file, the key, and the job when the problem is inside one.
    /// </exception>
    public static ConfigFile Load(string path)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(path);

        // Read and parse the file into its root mapping (a malformed path is reported with the file's context)
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(path);
        }
        catch (Exception ex) when (PathResolver.IsInvalidPathException(ex))
        {
            throw new InvalidOperationException($"Config file '{path}': invalid path: {ex.Message}", ex);
        }

        var prefix = $"Config file '{fullPath}': ";
        var root = ReadRootMapping(fullPath, prefix);

        // Interpret each top-level key
        var defaults = FormattingSettings.Empty;
        IReadOnlyList<ConfigTable> tables = [];
        string? rawRoot = null;
        foreach (var (keyNode, value) in root.Children)
        {
            var key = ReadKey(keyNode, prefix);
            switch (key)
            {
                case "tables":
                    tables = ReadTables(value, prefix);
                    break;

                case "root":
                    rawRoot = ReadRequiredString(value, key, prefix);
                    break;

                case "input":
                case "output":
                    throw new InvalidOperationException($"{prefix}key '{key}' is only allowed inside a table job");

                default:
                    if (CommandLineOnlyKeys.Contains(key, StringComparer.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"{prefix}key '{key}' is a command-line option and is not allowed in a config file");
                    }

                    defaults = ApplyFormattingKey(defaults, key, value, prefix)
                        ?? throw new InvalidOperationException($"{prefix}unknown key '{key}'");
                    break;
            }
        }

        // Resolve and validate the root directory relative to the configuration file
        var rootDirectory = rawRoot != null ? ResolveRoot(rawRoot, fullPath, prefix) : null;

        return new ConfigFile(fullPath, rootDirectory, defaults, tables);
    }

    /// <summary>
    ///     Reads the file and returns the root mapping of its first YAML document.
    /// </summary>
    /// <param name="fullPath">The full path of the file.</param>
    /// <param name="prefix">The error-message prefix naming the file.</param>
    /// <returns>The root mapping node.</returns>
    /// <exception cref="InvalidOperationException">Thrown for read, parse, empty or non-mapping errors.</exception>
    private static YamlMappingNode ReadRootMapping(string fullPath, string prefix)
    {
        // Read the file, reporting a missing file separately from other read failures
        string content;
        try
        {
            content = File.ReadAllText(fullPath);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new InvalidOperationException($"{prefix}file not found", ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException || PathResolver.IsInvalidPathException(ex))
        {
            throw new InvalidOperationException($"{prefix}cannot be read: {ex.Message}", ex);
        }

        // Parse the YAML text
        var yaml = new YamlStream();
        try
        {
            using var reader = new StringReader(content);
            yaml.Load(reader);
        }
        catch (YamlException ex)
        {
            throw new InvalidOperationException($"{prefix}invalid YAML: {ex.Message}", ex);
        }

        // Require a non-empty document whose root is a mapping
        if (yaml.Documents.Count == 0 || IsEmptyScalar(yaml.Documents[0].RootNode))
        {
            throw new InvalidOperationException($"{prefix}file is empty");
        }

        var root = yaml.Documents[0].RootNode;
        return root as YamlMappingNode
            ?? throw new InvalidOperationException($"{prefix}root must be a YAML mapping, but found {root.NodeType}");
    }

    /// <summary>
    ///     Reads the table jobs from the value of the <c>tables</c> key.
    /// </summary>
    /// <param name="value">The value node.</param>
    /// <param name="prefix">The error-message prefix naming the file.</param>
    /// <returns>The table jobs in file order.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value or any job is invalid.</exception>
    private static List<ConfigTable> ReadTables(YamlNode value, string prefix)
    {
        if (value is not YamlSequenceNode sequence || sequence.Children.Count == 0)
        {
            throw new InvalidOperationException($"{prefix}key 'tables' must be a non-empty sequence of table jobs");
        }

        var tables = new List<ConfigTable>();
        for (var i = 0; i < sequence.Children.Count; i++)
        {
            tables.Add(ReadTable(sequence.Children[i], i + 1, prefix));
        }

        return tables;
    }

    /// <summary>
    ///     Reads one table job.
    /// </summary>
    /// <param name="node">The job node.</param>
    /// <param name="index">The 1-based job index.</param>
    /// <param name="filePrefix">The error-message prefix naming the file.</param>
    /// <returns>The table job.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the job is invalid.</exception>
    private static ConfigTable ReadTable(YamlNode node, int index, string filePrefix)
    {
        var prefix = $"{filePrefix}table job {index}: ";
        if (node is not YamlMappingNode mapping)
        {
            throw new InvalidOperationException($"{prefix}must be a mapping, but found {node.NodeType}");
        }

        // Interpret each job key
        IReadOnlyList<string>? inputs = null;
        string? output = null;
        var settings = FormattingSettings.Empty;
        foreach (var (keyNode, value) in mapping.Children)
        {
            var key = ReadKey(keyNode, prefix);
            switch (key)
            {
                case "input":
                    inputs = ReadInputs(value, prefix);
                    break;

                case "output":
                    output = ReadRequiredString(value, key, prefix);
                    break;

                case "root":
                    throw new InvalidOperationException($"{prefix}key 'root' is only allowed at the top level");

                default:
                    settings = ApplyFormattingKey(settings, key, value, prefix)
                        ?? throw new InvalidOperationException($"{prefix}unknown key '{key}'");
                    break;
            }
        }

        // Require the input and output keys
        if (inputs == null)
        {
            throw new InvalidOperationException($"{prefix}missing required key 'input'");
        }

        if (output == null)
        {
            throw new InvalidOperationException($"{prefix}missing required key 'output'");
        }

        return new ConfigTable(index, inputs, output, settings);
    }

    /// <summary>
    ///     Reads the <c>input</c> value: a non-empty string or a non-empty sequence of non-empty strings.
    /// </summary>
    /// <param name="value">The value node.</param>
    /// <param name="prefix">The error-message prefix naming the file and job.</param>
    /// <returns>The input patterns.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value has the wrong shape.</exception>
    private static List<string> ReadInputs(YamlNode value, string prefix)
    {
        var error = $"{prefix}key 'input' must be a non-empty string or a non-empty sequence of non-empty strings";
        if (value is YamlScalarNode scalar)
        {
            return string.IsNullOrWhiteSpace(scalar.Value) ? throw new InvalidOperationException(error) : [scalar.Value];
        }

        if (value is not YamlSequenceNode sequence || sequence.Children.Count == 0)
        {
            throw new InvalidOperationException(error);
        }

        var inputs = new List<string>();
        foreach (var item in sequence.Children)
        {
            if (item is not YamlScalarNode itemScalar || string.IsNullOrWhiteSpace(itemScalar.Value))
            {
                throw new InvalidOperationException(error);
            }

            inputs.Add(itemScalar.Value);
        }

        return inputs;
    }

    /// <summary>
    ///     Applies one formatting key to a settings layer.
    /// </summary>
    /// <param name="settings">The layer to extend.</param>
    /// <param name="key">The key name.</param>
    /// <param name="value">The value node.</param>
    /// <param name="prefix">The error-message prefix naming the file and job.</param>
    /// <returns>The extended layer, or null when <paramref name="key"/> is not a formatting key.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is invalid for the key.</exception>
    private static FormattingSettings? ApplyFormattingKey(FormattingSettings settings, string key, YamlNode value, string prefix) =>
        key switch
        {
            "format" => settings with
            {
                Format = ReadChoice(value, key, prefix, ("table", OutputFormat.Table), ("bullets", OutputFormat.Bullets))
            },
            "section" => settings with { SectionHeading = ReadRequiredScalar(value, key, prefix) },
            "depth" => settings with { HeadingDepth = ReadDepth(value, prefix) },
            "sort" => settings with
            {
                SortBy = ReadChoice(value, key, prefix, ("file", SortOrder.FileOrder), ("alpha", SortOrder.Alphabetical))
            },
            "term-header" => settings with { TermHeader = ReadRequiredScalar(value, key, prefix) },
            "def-header" or "definition-header" => settings with
            {
                DefinitionHeader = settings.DefinitionHeader == null
                    ? ReadRequiredScalar(value, key, prefix)
                    : throw new InvalidOperationException(
                        $"{prefix}keys 'def-header' and 'definition-header' are aliases and cannot both be given")
            },
            "kind" => settings with
            {
                Kind = ReadChoice(value, key, prefix, ("terms", InputKind.Terms), ("deprecated", InputKind.Deprecated))
            },
            "use-header" => settings with { UseHeader = ReadRequiredScalar(value, key, prefix) },
            "where-header" => settings with { WhereHeader = ReadRequiredScalar(value, key, prefix) },
            "column-widths" => settings with { ColumnWidths = ReadColumnWidths(value, prefix) },
            _ => null
        };

    /// <summary>
    ///     Reads a case-insensitive choice value.
    /// </summary>
    /// <typeparam name="T">The value type of the choices.</typeparam>
    /// <param name="value">The value node.</param>
    /// <param name="key">The key name.</param>
    /// <param name="prefix">The error-message prefix.</param>
    /// <param name="choices">The allowed names and their values.</param>
    /// <returns>The value of the matching choice.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value matches no choice.</exception>
    private static T ReadChoice<T>(YamlNode value, string key, string prefix, params (string Name, T Value)[] choices)
    {
        var text = value is YamlScalarNode scalar ? scalar.Value : null;
        foreach (var (name, choice) in choices)
        {
            if (string.Equals(text, name, StringComparison.OrdinalIgnoreCase))
            {
                return choice;
            }
        }

        var allowed = string.Join(" or ", choices.Select(c => $"'{c.Name}'"));
        throw new InvalidOperationException($"{prefix}key '{key}' must be {allowed}");
    }

    /// <summary>
    ///     Reads the <c>depth</c> value: an integer from 1 to 6.
    /// </summary>
    /// <param name="value">The value node.</param>
    /// <param name="prefix">The error-message prefix.</param>
    /// <returns>The heading depth.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is not an integer from 1 to 6.</exception>
    private static int ReadDepth(YamlNode value, string prefix)
    {
        if (value is YamlScalarNode scalar &&
            int.TryParse(scalar.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var depth) &&
            depth is >= 1 and <= 6)
        {
            return depth;
        }

        throw new InvalidOperationException($"{prefix}key 'depth' must be an integer between 1 and 6");
    }

    /// <summary>
    ///     Reads the <c>column-widths</c> value: a sequence of 2 or 3 positive integers.
    /// </summary>
    /// <param name="value">The value node.</param>
    /// <param name="prefix">The error-message prefix.</param>
    /// <returns>The column widths.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value has the wrong shape or values.</exception>
    private static List<int> ReadColumnWidths(YamlNode value, string prefix)
    {
        var error = $"{prefix}key 'column-widths' must be a sequence of 2 or 3 positive integers (e.g. [25, 75])";
        if (value is not YamlSequenceNode sequence || sequence.Children.Count is < 2 or > 3)
        {
            throw new InvalidOperationException(error);
        }

        var widths = new List<int>();
        foreach (var item in sequence.Children)
        {
            if (item is not YamlScalarNode scalar ||
                !int.TryParse(scalar.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var width) ||
                width <= 0)
            {
                throw new InvalidOperationException(error);
            }

            widths.Add(width);
        }

        return widths;
    }

    /// <summary>
    ///     Reads a scalar value that must be present (an explicitly quoted empty string is allowed).
    /// </summary>
    /// <param name="value">The value node.</param>
    /// <param name="key">The key name.</param>
    /// <param name="prefix">The error-message prefix.</param>
    /// <returns>The scalar text.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is missing or not a scalar.</exception>
    private static string ReadRequiredScalar(YamlNode value, string key, string prefix)
    {
        if (value is not YamlScalarNode scalar || scalar.Value == null || IsEmptyScalar(scalar))
        {
            throw new InvalidOperationException($"{prefix}key '{key}' must be a string value");
        }

        return scalar.Value;
    }

    /// <summary>
    ///     Reads a scalar value that must be a non-empty, non-whitespace string.
    /// </summary>
    /// <param name="value">The value node.</param>
    /// <param name="key">The key name.</param>
    /// <param name="prefix">The error-message prefix.</param>
    /// <returns>The scalar text.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the value is missing, empty or not a scalar.</exception>
    private static string ReadRequiredString(YamlNode value, string key, string prefix)
    {
        if (value is not YamlScalarNode scalar || string.IsNullOrWhiteSpace(scalar.Value))
        {
            throw new InvalidOperationException($"{prefix}key '{key}' must be a non-empty string");
        }

        return scalar.Value;
    }

    /// <summary>
    ///     Reads a mapping key, which must be a scalar.
    /// </summary>
    /// <param name="keyNode">The key node.</param>
    /// <param name="prefix">The error-message prefix.</param>
    /// <returns>The key text.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the key is not a scalar.</exception>
    private static string ReadKey(YamlNode keyNode, string prefix) =>
        keyNode is YamlScalarNode { Value: not null } scalar
            ? scalar.Value
            : throw new InvalidOperationException($"{prefix}contains a non-scalar key");

    /// <summary>
    ///     Resolves the <c>root</c> value relative to the configuration file's directory.
    /// </summary>
    /// <remarks>
    ///     The <c>root</c> value only sets the base directory of relative paths; it never grants file access. It is
    ///     still configuration content, so after <c>${VAR}</c> expansion it is checked against the file-access
    ///     never-accessed list (URLs, UNC paths written as such, device paths, and on Windows alternate data
    ///     streams) before its existence is checked, so that a configuration file cannot make the tool probe a
    ///     network share or a device. Only the text is checked, so a relative root on a share is resolved.
    /// </remarks>
    /// <param name="rawRoot">The root value as written.</param>
    /// <param name="fullPath">The full path of the configuration file.</param>
    /// <param name="prefix">The error-message prefix naming the file.</param>
    /// <returns>The full path of the existing root directory.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the root is on the never-accessed list (the message contains <c>file-access-denied</c>), or
    ///     is malformed, cannot be resolved or does not exist; the message starts with <paramref name="prefix"/>.
    /// </exception>
    private static string ResolveRoot(string rawRoot, string fullPath, string prefix)
    {
        // PathResolver reports every root problem (undefined variable, wildcard, malformed or
        // overlong path, missing directory) as InvalidOperationException; add the file's context
        try
        {
            // The never-accessed list is checked on the expanded text, before the file system is touched
            var expanded = PathResolver.ExpandEnvironmentVariables(rawRoot, RootKeyDescription);
            var reason = FileAccessPolicy.DescribeNeverAccessed(expanded);
            if (reason != null)
            {
                throw new InvalidOperationException($"{FileAccessDeniedException.ErrorCode}: root '{rawRoot}': {reason}");
            }

            var configDirectory = Path.GetDirectoryName(fullPath) ?? fullPath;
            return PathResolver.ResolveExistingDirectory(rawRoot, configDirectory, RootKeyDescription);
        }
        catch (InvalidOperationException ex)
        {
            throw new InvalidOperationException($"{prefix}{ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Determines whether a node is a plain scalar with no value (for example <c>key:</c> or <c>~</c>).
    /// </summary>
    /// <param name="node">The node to test.</param>
    /// <returns>True when the node is an unquoted empty or null scalar.</returns>
    private static bool IsEmptyScalar(YamlNode node) =>
        node is YamlScalarNode { Style: ScalarStyle.Plain } scalar &&
        (string.IsNullOrEmpty(scalar.Value) || scalar.Value == "~" || scalar.Value == "null");
}
