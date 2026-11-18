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
using YamlDotNet.Core;

namespace DemaConsulting.DictionaryMark.Dictionary;

/// <summary>
///     Generates Markdown dictionary output from YAML input files.
/// </summary>
internal static class DictionaryGenerator
{
    /// <summary>
    ///     Generates Markdown output for every job in the context.
    /// </summary>
    /// <param name="context">The context containing the jobs and output channels. Must not be null.</param>
    /// <remarks>
    ///     Every job runs, even after an earlier job fails, so a single run reports every problem.
    ///     Each failure is written to <c>context.WriteError</c> (which sets the exit code to 1);
    ///     messages for config-file jobs start with the job label, which names the job index and
    ///     output. When the jobs come from config-file tables and any job failed, a summary line
    ///     reports how many failed. When there are no jobs, an error is reported.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context"/> is null.</exception>
    public static void Generate(Context context)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(context);

        // Nothing to generate is an error
        if (context.Jobs.Count == 0)
        {
            context.WriteError("Error: No input files found matching the specified patterns.");
            return;
        }

        // Run every job (Select is fully enumerated by Count), counting failures rather than stopping at the first one
        var failed = context.Jobs.Select(job => GenerateJob(context, job)).Count(succeeded => !succeeded);

        // Summarize failures of config-file table jobs
        if (context.UsesConfigTables && failed > 0)
        {
            context.WriteError($"Error: {failed} of {context.Jobs.Count} table jobs failed.");
        }
    }

    /// <summary>
    ///     Generates the Markdown output of one job.
    /// </summary>
    /// <param name="context">The context providing the output channels. Must not be null.</param>
    /// <param name="job">The job to run. Must not be null.</param>
    /// <returns>True when the job produced its output; false after reporting an error.</returns>
    /// <remarks>
    ///     Applies an early-return-on-error strategy within the job: each error condition is
    ///     reported (prefixed with the job label when the job has one) and the job stops with no
    ///     output. Error cases include invalid input patterns, undefined environment variables,
    ///     I/O or access errors resolving input patterns or reading YAML files, malformed or
    ///     structurally invalid YAML, conflicts between the job's input files, invalid output paths,
    ///     I/O or access errors writing the output file, and paths the file-access policy denies
    ///     (reported with the code <c>file-access-denied</c>). Every expected exception is caught here
    ///     so that a failing job never stops the jobs that follow it. Relative inputs and the
    ///     relative output resolve against <see cref="TableJob.BaseDirectory"/>. Missing parent
    ///     directories of the output file are created. When <see cref="TableJob.ArePathsJudged"/> is
    ///     true (a configuration-file job), every input pattern is judged before matching, every
    ///     matched file is read through <see cref="Context.FileAccess"/>, and the output is written
    ///     through it; a denial fails the job before anything is written. The command-line job keeps
    ///     the direct file-system path, because its paths are trusted.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static bool GenerateJob(Context context, TableJob job)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(job);

        // Resolve input files (strict: every pattern must match), judging configuration-file patterns first
        IReadOnlyList<string> files;
        Dictionary<string, string> writtenPaths;
        try
        {
            writtenPaths = job.ArePathsJudged ? CheckPatterns(context, job) : [];
            files = GlobMatcher.Resolve(job.InputPatterns, job.BaseDirectory);
        }
        catch (FileAccessDeniedException ex)
        {
            Report(context, job, ex.Message);
            return false;
        }
        catch (Exception ex) when (PathResolver.IsInvalidPathException(ex))
        {
            // Literal patterns are normalized, which rejects malformed (for example a null character)
            // or overlong paths; the globbing library rejects unsupported glob syntax
            Report(context, job, $"Invalid input pattern: {ex.Message}");
            return false;
        }
        catch (InvalidOperationException ex)
        {
            Report(context, job, ex.Message);
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Glob matching enumerates directories, which can fail on unreadable or vanished directories
            Report(context, job, $"Failed to resolve input patterns: {ex.Message}");
            return false;
        }

        if (files.Count == 0)
        {
            Report(context, job, "No input files found matching the specified patterns.");
            return false;
        }

        var options = job.Options;
        var deprecatedKind = options.Kind == InputKind.Deprecated;
        var approved = new List<DictionaryEntry>();
        var deprecated = new List<DeprecatedEntry>();

        // Load the input files as the selected kind
        if (!LoadFiles(context, job, files, writtenPaths, deprecatedKind, approved, deprecated))
        {
            return false;
        }

        // Check for conflicts between this job's files
        var conflicts = ConflictDetector.Detect(approved, deprecated);
        if (conflicts.Count > 0)
        {
            foreach (var conflict in conflicts)
            {
                Report(context, job, conflict);
            }

            return false;
        }

        // Format output
        var output = deprecatedKind
            ? MarkdownFormatter.FormatDeprecated(DeprecatedForOutput(deprecated, files), options)
            : MarkdownFormatter.Format(approved.Where(e => files.Contains(e.Source, StringComparer.OrdinalIgnoreCase)).ToList(), options);

        // Write output
        if (job.OutputFile == null)
        {
            context.WriteLine(output);
            return true;
        }

        return WriteOutputFile(context, job, output);
    }

    /// <summary>
    ///     Writes a job's output to its output file, creating missing parent directories.
    /// </summary>
    /// <param name="context">The context for error reporting.</param>
    /// <param name="job">The job whose output file is written.</param>
    /// <param name="output">The formatted Markdown.</param>
    /// <returns>True when the file was written; false after reporting an error.</returns>
    private static bool WriteOutputFile(Context context, TableJob job, string output)
    {
        var outputFile = job.OutputFile!;

        // Resolve the output path (expanding ${VAR}) against the job's base directory; malformed or
        // overlong paths are reported by the resolver as InvalidOperationException
        string fullPath;
        try
        {
            fullPath = PathResolver.ResolvePath(outputFile, job.BaseDirectory, "output path");
        }
        catch (InvalidOperationException ex)
        {
            Report(context, job, ex.Message);
            return false;
        }

        // Create missing parent directories and write the file; configuration-file outputs are
        // judged and written by the file-access policy, which writes nothing when it denies the path
        try
        {
            if (job.ArePathsJudged)
            {
                context.FileAccess.WriteAllText(outputFile, fullPath, output);
                return true;
            }

            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, output);
            return true;
        }
        catch (FileAccessDeniedException ex)
        {
            Report(context, job, ex.Message);
            return false;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException || PathResolver.IsInvalidPathException(ex))
        {
            // The platform can still reject a normalized path when writing (for example a wildcard
            // character in a Windows file name), so path-validation exceptions fail only this job
            Report(context, job, $"Failed to write output file '{outputFile}': {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     Reports a job error, prefixed with the job label when the job has one.
    /// </summary>
    /// <param name="context">The context for error reporting.</param>
    /// <param name="job">The job that failed.</param>
    /// <param name="text">The error text without the <c>Error:</c> prefix.</param>
    private static void Report(Context context, TableJob job, string text) =>
        context.WriteError(job.Label == null ? $"Error: {text}" : $"Error: {job.Label}: {text}");

    /// <summary>
    ///     Judges every input pattern of a configuration-file job before it is matched.
    /// </summary>
    /// <remarks>
    ///     Matching touches the file system, so the never-accessed list (and, under roots, the written-path rule
    ///     for literal patterns) is applied first. The returned map lets a denial of a literal pattern's file name
    ///     the pattern as written.
    /// </remarks>
    /// <param name="context">The context that holds the file-access policy.</param>
    /// <param name="job">The configuration-file job.</param>
    /// <returns>The pattern as written for each literal pattern's full path.</returns>
    /// <exception cref="FileAccessDeniedException">Thrown when a pattern is denied.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a referenced environment variable is undefined.</exception>
    /// <exception cref="ArgumentException">Thrown when a literal pattern is not a valid path.</exception>
    private static Dictionary<string, string> CheckPatterns(Context context, TableJob job)
    {
        var writtenPaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var pattern in job.InputPatterns)
        {
            var expanded = PathResolver.ExpandEnvironmentVariables(pattern, "glossary path");
            var rooted = PathResolver.Combine(pattern, job.BaseDirectory, "glossary path");
            context.FileAccess.CheckPattern(pattern, expanded, rooted);
            if (!rooted.Contains('*') && !rooted.Contains('?'))
            {
                writtenPaths.TryAdd(Path.GetFullPath(rooted), pattern);
            }
        }

        return writtenPaths;
    }

    /// <summary>
    ///     Loads files of one kind into the matching list, reporting the first error.
    /// </summary>
    /// <remarks>
    ///     Files of a configuration-file job are read through the file-access policy, which judges each file by
    ///     its real path; a denied file fails the job and is never silently skipped. A literal pattern's file is
    ///     passed with the pattern as written; a file a glob matched is passed with no written path, so the policy
    ///     applies only its full-path rules (the pattern text was judged before matching) and names it by its full
    ///     path.
    /// </remarks>
    /// <param name="context">The context for the policy and error reporting.</param>
    /// <param name="job">The job whose files are loaded.</param>
    /// <param name="files">The matched files.</param>
    /// <param name="writtenPaths">The pattern as written for each literal pattern's full path.</param>
    /// <param name="asDeprecated">True to load deprecated-term files.</param>
    /// <param name="approved">The list that receives approved-term entries.</param>
    /// <param name="deprecated">The list that receives deprecated-term entries.</param>
    /// <returns>True when every file loaded; false after writing an error.</returns>
    private static bool LoadFiles(
        Context context,
        TableJob job,
        IReadOnlyList<string> files,
        Dictionary<string, string> writtenPaths,
        bool asDeprecated,
        List<DictionaryEntry> approved,
        List<DeprecatedEntry> deprecated)
    {
        foreach (var file in files)
        {
            try
            {
                if (job.ArePathsJudged)
                {
                    // Judge the file and parse the text read from the verified handle; a file a glob matched has no
                    // text as written (its pattern was judged), so it is judged and named by its full path
                    var text = context.FileAccess.ReadAllText(writtenPaths.GetValueOrDefault(file), file);
                    if (asDeprecated)
                    {
                        deprecated.AddRange(YamlDictionaryLoader.LoadDeprecatedText(text, file));
                    }
                    else
                    {
                        approved.AddRange(YamlDictionaryLoader.LoadText(text, file));
                    }
                }
                else if (asDeprecated)
                {
                    deprecated.AddRange(YamlDictionaryLoader.LoadDeprecated(file));
                }
                else
                {
                    approved.AddRange(YamlDictionaryLoader.Load(file));
                }
            }
            catch (FileAccessDeniedException ex)
            {
                Report(context, job, ex.Message);
                return false;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException || PathResolver.IsInvalidPathException(ex))
            {
                Report(context, job, $"Failed to read file '{file}': {ex.Message}");
                return false;
            }
            catch (YamlException ex)
            {
                // Syntactically malformed YAML is rejected by the parser before structural checks run
                Report(context, job, $"Invalid YAML in file '{file}': {ex.Message}");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Report(context, job, $"Invalid YAML in file '{file}': {ex.Message}");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Selects the deprecated entries that came from the primary input files .
    /// </summary>
    private static List<DeprecatedEntry> DeprecatedForOutput(List<DeprecatedEntry> deprecated, IReadOnlyList<string> files) =>
        [.. deprecated.Where(e => files.Contains(e.Source, StringComparer.OrdinalIgnoreCase))];
}
