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
    /// <param name="context">The context containing command line arguments.</param>
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

        // Resolve input files
        IReadOnlyList<string> files;
        try
        {
            files = GlobMatcher.GetFiles(context.InputPatterns);
        }
        catch (ArgumentException ex)
        {
            context.WriteError($"Error: Invalid input pattern: {ex.Message}");
            return;
        }

        if (files.Count == 0)
        {
            context.WriteError("Error: No input files found matching the specified patterns.");
            return;
        }

        // Load all entries from all files
        var allEntries = new List<DictionaryEntry>();
        foreach (var file in files)
        {
            try
            {
                var entries = YamlDictionaryLoader.Load(file);
                allEntries.AddRange(entries);
            }
            catch (IOException ex)
            {
                context.WriteError($"Error: Failed to read file '{file}': {ex.Message}");
                return;
            }
            catch (InvalidOperationException ex)
            {
                context.WriteError($"Error: Invalid YAML in file '{file}': {ex.Message}");
                return;
            }
        }

        // Check for conflicts
        var conflicts = ConflictDetector.Detect(allEntries);
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
            TermHeader = context.TermHeader,
            DefinitionHeader = context.DefinitionHeader,
            SortOrder = context.SortBy
        };

        var output = MarkdownFormatter.Format(allEntries, options);

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
}
