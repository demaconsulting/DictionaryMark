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
using DemaConsulting.DictionaryMark.Files;
using DemaConsulting.DictionaryMark.Utilities;
using YamlDotNet.Core;

namespace DemaConsulting.DictionaryMark.Dictionary;

/// <summary>
///     Generates Markdown dictionary output from YAML input files.
/// </summary>
internal static class DictionaryGenerator
{
    /// <summary>The hint added to every <c>file-access-denied</c> error.</summary>
    private const string FileAccessDeniedHint = "Allow the file in the file-access policy, or reference a file it allows.";

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
    ///     and I/O or access errors writing the output file. Every expected exception is caught here
    ///     so that a failing job never stops the jobs that follow it. Relative inputs and the
    ///     relative output resolve against <see cref="TableJob.BaseDirectory"/>. Missing parent
    ///     directories of the output file are created. A config-file table job (one with an
    ///     <see cref="TableJob.Index"/>) is judged by the file-access policy of the context: every
    ///     input file is read through one <see cref="FileReferenceResolver"/> per job, whose base
    ///     directory is the config file's folder, and the output path is checked by the same
    ///     resolver before anything is created. Before any folder is listed, each of its input patterns is checked
    ///     against the resolver's never list, so a network or device pattern is refused without contacting a host.
    ///     A denial fails the job with <c>file-access-denied</c>.
    ///     The command-line job is never judged.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static bool GenerateJob(Context context, TableJob job)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(job);

        // A config-file table job's patterns are checked against the never list before any folder is listed
        if (job.Index != null && !CheckPatterns(context, job))
        {
            return false;
        }

        // Resolve input files (strict: every pattern must match)
        IReadOnlyList<string> files;
        try
        {
            files = GlobMatcher.Resolve(job.InputPatterns, job.BaseDirectory);
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

        // A config-file table job is judged by the file-access policy through one resolver per job, whose
        // trusted base directory is the config file's folder (never --root or the config root key); the
        // command-line job is trusted and reads by name
        var resolver = job.Index != null
            ? new FileReferenceResolver(new FileReferenceOptions
            {
                BaseDirectory = Path.GetDirectoryName(context.ConfigFilePath)!,
                FileAccess = context.FileAccess
            })
            : null;

        // Load the input files as the selected kind
        if (!LoadFiles(context, job, resolver, files, deprecatedKind, approved, deprecated))
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

        return WriteOutputFile(context, job, resolver, output);
    }

    /// <summary>
    ///     Writes a job's output to its output file, creating missing parent directories.
    /// </summary>
    /// <param name="context">The context for error reporting.</param>
    /// <param name="job">The job whose output file is written.</param>
    /// <param name="resolver">The resolver that judges the output path of a config-file table job, or null for a trusted job.</param>
    /// <param name="output">The formatted Markdown.</param>
    /// <returns>True when the file was written; false after reporting an error.</returns>
    private static bool WriteOutputFile(Context context, TableJob job, FileReferenceResolver? resolver, string output)
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

        // A judged output is checked before anything is created, so a refused output writes nothing
        if (resolver != null)
        {
            try
            {
                resolver.CheckOutputPath(fullPath);
            }
            catch (FileReferenceException ex)
            {
                Report(context, job, DescribeRefusal(ex, $"Failed to write output file '{outputFile}'"));
                return false;
            }
        }

        // Create missing parent directories and write the file
        try
        {
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, output);
            return true;
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
    ///     Checks the input patterns of a config-file table job against the resolver's never list before any folder is
    ///     listed.
    /// </summary>
    /// <remarks>
    ///     Each pattern is checked as it would be listed: with <c>${VAR}</c> references expanded and rooted at the
    ///     job's base directory, and again with <c>.</c> and <c>..</c> segments collapsed. A pattern that names a
    ///     network share, a device path or a path with a colon therefore fails the job without contacting any host.
    ///     Problems other than a never-list rule (an undefined variable, a malformed path) are left to the pattern
    ///     resolution that follows, which reports them as before.
    /// </remarks>
    /// <param name="context">The context used for error reporting.</param>
    /// <param name="job">The config-file table job.</param>
    /// <returns><see langword="true"/> when every pattern passes; <see langword="false"/> when one is refused.</returns>
    private static bool CheckPatterns(Context context, TableJob job)
    {
        foreach (var pattern in job.InputPatterns)
        {
            try
            {
                // The rooted pattern as written, then with its relative segments collapsed
                var expanded = PathResolver.Combine(pattern, job.BaseDirectory, "glossary path");
                FileReferenceResolver.CheckPatternText(expanded);
                FileReferenceResolver.CheckPatternText(Path.GetFullPath(expanded));
            }
            catch (FileReferenceException ex)
            {
                var shown = FileReferenceException.Printable(pattern);
                Report(context, job, DescribeRefusal(ex, $"Failed to resolve input pattern '{shown}'"));
                return false;
            }
            catch (Exception ex) when (ex is InvalidOperationException || PathResolver.IsInvalidPathException(ex))
            {
                // Not a never-list problem: the pattern resolution that follows reports it
            }
        }

        return true;
    }

    /// <summary>
    ///     Describes a refusal of the file-access resolver: a policy denial (or a policy that cannot be
    ///     applied) is a <c>file-access-denied</c> error with its hint; any other refusal is an ordinary
    ///     read or write error.
    /// </summary>
    /// <param name="ex">The refusal.</param>
    /// <param name="prefix">The text that introduces an ordinary read or write error.</param>
    /// <returns>The error text without the <c>Error:</c> prefix.</returns>
    private static string DescribeRefusal(FileReferenceException ex, string prefix) =>
        ex.Kind is FileReferenceFailureKind.AccessDenied or FileReferenceFailureKind.RealPathUnavailable
            ? $"file-access-denied: {ex.Message} {FileAccessDeniedHint}"
            : $"{prefix}: {ex.Message}";

    /// <summary>
    ///     Loads files of one kind into the matching list, reporting the first error.
    /// </summary>
    /// <remarks>
    ///     With a resolver (a config-file table job), every file the glob matched is read through it, so the
    ///     file-access policy judges each match and the loader parses exactly the bytes that were approved; the
    ///     first refused match fails the job, so a denied match is never skipped. Without a resolver the files
    ///     are read by name.
    /// </remarks>
    /// <returns>True when every file loaded; false after writing an error.</returns>
    private static bool LoadFiles(
        Context context,
        TableJob job,
        FileReferenceResolver? resolver,
        IReadOnlyList<string> files,
        bool asDeprecated,
        List<DictionaryEntry> approved,
        List<DeprecatedEntry> deprecated)
    {
        foreach (var file in files)
        {
            try
            {
                if (resolver != null)
                {
                    // Read the match through the resolver; a file that vanished since the glob ran is not found
                    var read = resolver.Resolve(file);
                    if (read == null)
                    {
                        Report(context, job, $"Input file '{file}' was not found.");
                        return false;
                    }

                    if (asDeprecated)
                    {
                        deprecated.AddRange(YamlDictionaryLoader.LoadDeprecated(file, read.Content));
                    }
                    else
                    {
                        approved.AddRange(YamlDictionaryLoader.Load(file, read.Content));
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
            catch (FileReferenceException ex)
            {
                Report(context, job, DescribeRefusal(ex, $"Failed to read file '{file}'"));
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
