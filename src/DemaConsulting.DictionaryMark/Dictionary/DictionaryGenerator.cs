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

using DemaConsulting.DictionaryMark.Cli;
using DemaConsulting.DictionaryMark.Utilities;

namespace DemaConsulting.DictionaryMark.Dictionary;

/// <summary>
///     Generates Markdown dictionary output from YAML input files.
/// </summary>
internal static class DictionaryGenerator
{
    /// <summary>
    ///     Generates Markdown output from YAML dictionary files.
    /// </summary>
    /// <param name="context">The context containing input patterns, output options, and output channels. Must not be null.</param>
    /// <remarks>
    ///     Applies an early-return-on-error strategy: each error condition writes to
    ///     <c>context.WriteError</c> (which sets the exit code to 1) and returns immediately,
    ///     with no output generated. Error cases include invalid input patterns, I/O errors
    ///     reading YAML files, invalid YAML structure, no files found, and I/O or access errors
    ///     writing the output file.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static void Generate(Context context)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(context);

        // Resolve input files (strict: every pattern must match)
        IReadOnlyList<string> files;
        IReadOnlyList<string> checkFiles;
        try
        {
            files = GlobMatcher.Resolve(context.InputPatterns);
            checkFiles = GlobMatcher.Resolve(context.CheckAgainstPatterns);
        }
        catch (ArgumentException ex)
        {
            context.WriteError($"Error: Invalid input pattern: {ex.Message}");
            return;
        }
        catch (InvalidOperationException ex)
        {
            context.WriteError($"Error: {ex.Message}");
            return;
        }

        if (files.Count == 0)
        {
            context.WriteError("Error: No input files found matching the specified patterns.");
            return;
        }

        var deprecatedKind = context.Kind == InputKind.Deprecated;
        var approved = new List<DictionaryEntry>();
        var deprecated = new List<DeprecatedEntry>();

        // Load the primary files as the selected kind and the check-against files as the other kind
        if (!LoadFiles(context, files, deprecatedKind, approved, deprecated)
            || !LoadFiles(context, checkFiles, !deprecatedKind, approved, deprecated))
        {
            return;
        }

        // Check for conflicts
        var conflicts = ConflictDetector.Detect(approved, deprecated);
        if (conflicts.Count > 0)
        {
            foreach (var conflict in conflicts)
            {
                context.WriteError($"Error: {conflict}");
            }

            return;
        }

        // Format output
        var options = new MarkdownOptions
        {
            Format = context.Format,
            SectionHeading = context.SectionHeading,
            HeadingDepth = context.HeadingDepth,
            TermHeader = context.TermHeaderSpecified || !deprecatedKind ? context.TermHeader : "Not used",
            DefinitionHeader = context.DefinitionHeader,
            SortOrder = context.SortBy,
            Kind = context.Kind,
            UseHeader = context.UseHeader,
            WhereHeader = context.WhereHeader
        };

        var output = deprecatedKind
            ? MarkdownFormatter.FormatDeprecated(DeprecatedForOutput(deprecated, files), options)
            : MarkdownFormatter.Format(approved.Where(e => files.Contains(e.Source, StringComparer.OrdinalIgnoreCase)).ToList(), options);
        // Write output
        if (context.OutputFile != null)
        {
            try
            {
                File.WriteAllText(context.OutputFile, output);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                context.WriteError($"Error: Failed to write output file '{context.OutputFile}': {ex.Message}");
            }
        }
        else
        {
            context.WriteLine(output);
        }
    }

    /// <summary>
    ///     Loads files of one kind into the matching list, reporting the first error.
    /// </summary>
    /// <returns>True when every file loaded; false after writing an error.</returns>
    private static bool LoadFiles(
        Context context,
        IReadOnlyList<string> files,
        bool asDeprecated,
        List<DictionaryEntry> approved,
        List<DeprecatedEntry> deprecated)
    {
        foreach (var file in files)
        {
            try
            {
                if (asDeprecated)
                {
                    deprecated.AddRange(YamlDictionaryLoader.LoadDeprecated(file));
                }
                else
                {
                    approved.AddRange(YamlDictionaryLoader.Load(file));
                }
            }
            catch (IOException ex)
            {
                context.WriteError($"Error: Failed to read file '{file}': {ex.Message}");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                context.WriteError($"Error: Invalid YAML in file '{file}': {ex.Message}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Selects the deprecated entries that came from the primary input files (not check-against files).
    /// </summary>
    private static List<DeprecatedEntry> DeprecatedForOutput(List<DeprecatedEntry> deprecated, IReadOnlyList<string> files) =>
        [.. deprecated.Where(e => files.Contains(e.Source, StringComparer.OrdinalIgnoreCase))];
}
