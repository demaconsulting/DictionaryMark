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
    ///     <see cref="TableJob.Index"/>) is judged by the file-access policy of the context, and so is every relative
    ///     path of a command-line job that the config file's <c>root</c> key relocates (see
    ///     <see cref="TableJob.IsJudged"/>): every
    ///     judged input file is read through one <see cref="FileReferenceResolver"/> per job, whose base
    ///     directory is the config file's folder, and a judged output path is checked by the same
    ///     resolver before anything is created. Before any folder is listed, each of its judged input patterns is checked
    ///     against the resolver's never list, so a network or device pattern is refused without contacting a host.
    ///     A judged literal input (one without wildcard characters) is not looked up first: its path is handed
    ///     straight to the resolver, which applies the policy before it checks that the file exists.
    ///     A denial fails the job with <c>file-access-denied</c>. Every message about a judged path (denied, not
    ///     found, unreadable, invalid YAML, conflicting, or an output write failure) names the path as written - a
    ///     glob match relative to the base directory - and never the absolute host path, because the path comes from
    ///     untrusted content and its message may be shown to the author of that content.
    ///     The other paths of a command-line job (absolute paths, and every path when no config root key relocates
    ///     them) are never judged.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    internal static bool GenerateJob(Context context, TableJob job)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(job);

        // The judged patterns of the job are checked against the never list before any folder is listed
        if (job.HasJudgedPaths && !CheckPatterns(context, job))
        {
            return false;
        }

        // Resolve input files (strict: every pattern must match)
        IReadOnlyList<string> files;
        IReadOnlyDictionary<string, string> judged;
        try
        {
            (files, judged) = ResolveInputs(job);
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

        // A job with judged paths (a config-file table job, or a command-line job whose relative paths the config
        // root key relocates) is judged by the file-access policy through one resolver per job, whose trusted base
        // directory is the config file's folder (never --root or the config root key); a fully trusted
        // command-line job reads by name
        var resolver = job.HasJudgedPaths
            ? new FileReferenceResolver(new FileReferenceOptions
            {
                BaseDirectory = Path.GetDirectoryName(context.ConfigFilePath)!,
                FileAccess = context.FileAccess
            })
            : null;

        // Load the input files as the selected kind
        if (!LoadFiles(context, job, resolver, judged, files, deprecatedKind, approved, deprecated))
        {
            return false;
        }

        // Check for conflicts between this job's files
        var conflicts = ConflictDetector.Detect(approved, deprecated);
        if (conflicts.Count > 0)
        {
            foreach (var conflict in conflicts)
            {
                // A conflict names the two files by full path; a judged file is named as it was written
                Report(context, job, Redact(conflict, judged));
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
    /// <remarks>
    ///     The output path is expanded and rooted at <see cref="TableJob.BaseDirectoryText"/> as text; a judged output
    ///     is checked by the resolver on that text, before normalization, and the normalized path is used only for the
    ///     write.
    /// </remarks>
    /// <param name="context">The context for error reporting.</param>
    /// <param name="job">The job whose output file is written.</param>
    /// <param name="resolver">The resolver that judges the judged paths of the job, or null for a trusted job.</param>
    /// <param name="output">The formatted Markdown.</param>
    /// <returns>True when the file was written; false after reporting an error.</returns>
    private static bool WriteOutputFile(Context context, TableJob job, FileReferenceResolver? resolver, string output)
    {
        var outputFile = job.OutputFile!;

        // Expand ${VAR} in the output path and root it at the job's base directory as text, then normalize it;
        // malformed or overlong paths are reported by the path resolver as InvalidOperationException
        string pathText;
        string fullPath;
        bool judged;
        try
        {
            pathText = PathResolver.Combine(outputFile, job.BaseDirectoryText, "output path");
            fullPath = PathResolver.ResolvePath(outputFile, job.BaseDirectoryText, "output path");

            // Only a path the job judges (every table output; a command-line output the config root key relocates)
            // is checked; a rooted command-line output ignores the root and stays trusted
            judged = resolver != null && job.IsJudged(PathResolver.ExpandEnvironmentVariables(outputFile, "output path"));
        }
        catch (InvalidOperationException ex)
        {
            Report(context, job, ex.Message);
            return false;
        }

        // A judged output is checked before anything is created, so a refused output writes nothing; the resolver gets
        // the text before normalization, because normalization can hide a never-list path (on Linux and macOS a
        // leading "//" collapses to "/"), and the normalized path is used only for the write
        if (judged)
        {
            try
            {
                resolver!.CheckOutputPath(pathText);
            }
            catch (FileReferenceException ex)
            {
                // The refusal is shown with the output as written, never the absolute path it was checked as
                Report(context, job, DescribeRefusal(ex, outputFile, $"Failed to write output file '{outputFile}'"));
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
            // character in a Windows file name), so path-validation exceptions fail only this job; the text of a
            // judged output names the path as written, not the absolute path
            var text = judged
                ? Redact(ex.Message, new Dictionary<string, string> { [fullPath] = FileReferenceException.Printable(outputFile) })
                : ex.Message;
            Report(context, job, $"Failed to write output file '{outputFile}': {text}");
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
    ///     Resolves the input patterns of a job to the files to read.
    /// </summary>
    /// <remarks>
    ///     A pattern the job does not judge (every pattern of a fully trusted command-line job, and an absolute
    ///     pattern of a command-line job whose relative paths the config <c>root</c> key relocates) goes through the
    ///     strict <see cref="GlobMatcher.Resolve(IEnumerable{string}, string)"/>, which checks that each literal file
    ///     exists. A judged pattern (see <see cref="TableJob.IsJudged"/>) is untrusted content: a pattern without
    ///     wildcard characters
    ///     (after <c>${VAR}</c> expansion) is only expanded, rooted at the job's base directory and normalized as
    ///     text, never probed, because its file is read through the resolver, which applies the policy before it
    ///     looks for the file and reports a missing file as not found. Only a real wildcard pattern, which has already
    ///     passed the never-list check, is listed by the glob matcher; listing is the one access made for judged
    ///     content outside the resolver. Every file a judged pattern names is also returned in the judged map, which
    ///     tells the loader to read it through the resolver and gives the text to show for it in messages; a file
    ///     reached by both a trusted and a judged pattern is judged. The text shown for a judged file is never its
    ///     absolute path: a literal input is shown as written, a glob match relative to the job's base directory
    ///     (with <c>/</c> separators), or in full when the pattern itself is absolute. When two judged patterns reach
    ///     the same file, the first one's text is kept. Each judged glob is resolved on its own, with the same strict
    ///     semantics as the batch, so that its matches can be attributed to it.
    /// </remarks>
    /// <param name="job">The job whose patterns are resolved.</param>
    /// <returns>
    ///     The sorted (ordinal, case-insensitive), deduplicated list of absolute file paths, and the map
    ///     (case-insensitive) from each file that a judged pattern named to the text to show for it.
    /// </returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when a pattern is empty or malformed, or a glob is rejected by the globbing library.
    /// </exception>
    /// <exception cref="PathTooLongException">Thrown when a literal path exceeds the platform's maximum length.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when an environment variable is undefined or empty, a glob matches no files, or (trusted pattern
    ///     only) a literal directory or file does not exist.
    /// </exception>
    private static (IReadOnlyList<string> Files, IReadOnlyDictionary<string, string> Judged) ResolveInputs(TableJob job)
    {
        // Split the patterns: trusted ones go to the strict glob matcher; judged literal paths are resolved as text
        // and judged wildcard patterns are listed
        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var judged = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var trusted = new List<string>();
        var globs = new List<(string Pattern, bool Absolute)>();
        foreach (var pattern in job.InputPatterns)
        {
            var expandedPattern = PathResolver.ExpandEnvironmentVariables(pattern, "glossary path");
            if (!job.IsJudged(expandedPattern))
            {
                trusted.Add(pattern);
                continue;
            }

            var expanded = PathResolver.Combine(pattern, job.BaseDirectory, "glossary path");
            if (expanded.Contains('*') || expanded.Contains('?'))
            {
                globs.Add((pattern, Path.IsPathRooted(expandedPattern)));
            }
            else
            {
                // A literal is shown as written
                var full = Path.GetFullPath(expanded);
                files.Add(full);
                judged.TryAdd(full, FileReferenceException.Printable(pattern));
            }
        }

        // The trusted patterns are resolved strictly, exactly as a fully trusted command-line job always was
        if (trusted.Count > 0)
        {
            files.UnionWith(GlobMatcher.Resolve(trusted, job.BaseDirectory));
        }

        // List the judged wildcard patterns (they passed the never-list check before this call), one at a time so
        // that each match is shown relative to the base directory, or in full when the pattern was absolute
        foreach (var (pattern, absolute) in globs)
        {
            foreach (var match in GlobMatcher.Resolve([pattern], job.BaseDirectory))
            {
                files.Add(match);
                judged.TryAdd(match, ShownMatch(match, job.BaseDirectory, absolute));
            }
        }

        return ([.. files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)], judged);
    }

    /// <summary>
    ///     Gets the text to show for a file that a judged glob pattern matched.
    /// </summary>
    /// <remarks>
    ///     Showing the match relative to the job's base directory keeps the pattern's own prefix and the matched part
    ///     visible while revealing nothing about where the base directory sits on the host. When the author wrote an
    ///     absolute pattern, the full path starts with text the author already wrote, so it is shown as is.
    /// </remarks>
    /// <param name="match">The full path of the matched file.</param>
    /// <param name="baseDirectory">The job's base directory.</param>
    /// <param name="absolute">True when the pattern was absolute after expansion.</param>
    /// <returns>The printable text to show.</returns>
    private static string ShownMatch(string match, string baseDirectory, bool absolute)
    {
        // An absolute pattern already revealed its own prefix
        if (absolute)
        {
            return FileReferenceException.Printable(match);
        }

        // Otherwise show the match relative to the base directory, with portable separators
        var relative = Path.GetRelativePath(baseDirectory, match);
        return FileReferenceException.Printable(
            Path.DirectorySeparatorChar == '/' ? relative : relative.Replace(Path.DirectorySeparatorChar, '/'));
    }

    /// <summary>
    ///     Replaces the full path of each judged file in a text with the text shown for it.
    /// </summary>
    /// <remarks>
    ///     Messages written by the YAML loader, the conflict detector and the operating system quote the full path of
    ///     the file they concern; this keeps that text from reaching the user for a judged file. Longer paths are
    ///     replaced first so that a path that is a prefix of another cannot garble it. Text that names other paths
    ///     (for example a missing parent directory in an operating system message) is left as is.
    /// </remarks>
    /// <param name="text">The message text.</param>
    /// <param name="shown">The map from full path to the text to show; may be empty.</param>
    /// <returns>The text with each judged full path replaced.</returns>
    private static string Redact(string text, IReadOnlyDictionary<string, string> shown) =>
        shown.OrderByDescending(pair => pair.Key.Length)
            .Aggregate(text, (current, pair) => current.Replace(pair.Key, pair.Value, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    ///     Checks the judged input patterns of a job against the resolver's never list before any folder is
    ///     listed.
    /// </summary>
    /// <remarks>
    ///     Each judged pattern (see <see cref="TableJob.IsJudged"/>) is checked as it would be listed: with
    ///     <c>${VAR}</c> references expanded and rooted at the
    ///     job's base directory as text (<see cref="TableJob.BaseDirectoryText"/>, before normalization, so a root key
    ///     such as <c>//host/share</c> is not collapsed to a local path on Linux and macOS), and again with <c>.</c> and
    ///     <c>..</c> segments collapsed. A pattern that names a network share, a device path, a reserved Windows device
    ///     name or a path with a colon therefore fails the job without contacting any host or device.
    ///     Problems other than a never-list rule (an undefined variable, a malformed path) are left to the pattern
    ///     resolution that follows, which reports them as before. A trusted pattern is not checked.
    /// </remarks>
    /// <param name="context">The context used for error reporting.</param>
    /// <param name="job">The job with judged paths.</param>
    /// <returns><see langword="true"/> when every judged pattern passes; <see langword="false"/> when one is refused.</returns>
    private static bool CheckPatterns(Context context, TableJob job)
    {
        foreach (var pattern in job.InputPatterns)
        {
            try
            {
                // A trusted pattern (an absolute command-line pattern) is not judged
                if (!job.IsJudged(PathResolver.ExpandEnvironmentVariables(pattern, "glossary path")))
                {
                    continue;
                }

                // The pattern rooted at the base directory's text as written (never-list paths survive, as a leading
                // "//" collapses on Linux and macOS when normalized), then with its relative segments collapsed
                var expanded = PathResolver.Combine(pattern, job.BaseDirectoryText, "glossary path");
                FileReferenceResolver.CheckPatternText(expanded);
                FileReferenceResolver.CheckPatternText(Path.GetFullPath(expanded));
            }
            catch (FileReferenceException ex)
            {
                // The refusal is shown with the pattern as written, not the expanded, rooted text it was checked as
                var shown = FileReferenceException.Printable(pattern);
                Report(context, job, DescribeRefusal(ex, shown, $"Failed to resolve input pattern '{shown}'"));
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
    /// <remarks>
    ///     The resolver reports the path it was given, which for a judged path is an absolute host path. The refusal
    ///     is therefore rebuilt around the path as the user wrote it, so a message never reveals where the base
    ///     directory or the root sits on the host; the policy name and the rule text are kept (a policy's own reason is
    ///     never part of the message).
    /// </remarks>
    /// <param name="ex">The refusal.</param>
    /// <param name="written">The path as the user wrote it (or the text to show for it).</param>
    /// <param name="prefix">The text that introduces an ordinary read or write error.</param>
    /// <returns>The error text without the <c>Error:</c> prefix.</returns>
    private static string DescribeRefusal(FileReferenceException ex, string written, string prefix)
    {
        // Same failure, but naming the path as written
        var message = new FileReferenceException(ex.Kind, written, ex.Reason) { PolicyName = ex.PolicyName }.Message;
        return ex.Kind is FileReferenceFailureKind.AccessDenied or FileReferenceFailureKind.RealPathUnavailable
            ? $"file-access-denied: {message} {FileAccessDeniedHint}"
            : $"{prefix}: {message}";
    }

    /// <summary>
    ///     Loads files of one kind into the matching list, reporting the first error.
    /// </summary>
    /// <remarks>
    ///     With a resolver, every judged literal input and every judged file a glob matched is read
    ///     through it, so the file-access policy judges each one and the loader parses exactly the bytes that were
    ///     approved; the first refused file fails the job, so a denied match is never skipped. A file that is not
    ///     judged (or every file when there is no resolver) is read by name.
    /// </remarks>
    /// <returns>True when every file loaded; false after writing an error.</returns>
    private static bool LoadFiles(
        Context context,
        TableJob job,
        FileReferenceResolver? resolver,
        IReadOnlyDictionary<string, string> judged,
        IReadOnlyList<string> files,
        bool asDeprecated,
        List<DictionaryEntry> approved,
        List<DeprecatedEntry> deprecated)
    {
        foreach (var file in files)
        {
            // A judged file is named in messages as it was written; any other file by its full path
            var isJudged = resolver != null && judged.ContainsKey(file);
            var shown = isJudged ? judged[file] : file;
            try
            {
                if (isJudged)
                {
                    // Read the file through the resolver; a literal input that does not exist, or a match that vanished
                    // since the glob ran, is not found
                    var read = resolver!.Resolve(file);
                    if (read == null)
                    {
                        Report(context, job, $"Input file '{shown}' was not found.");
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
                Report(context, job, DescribeRefusal(ex, shown, $"Failed to read file '{shown}'"));
                return false;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException || PathResolver.IsInvalidPathException(ex))
            {
                Report(context, job, $"Failed to read file '{shown}': {Redact(ex.Message, judged)}");
                return false;
            }
            catch (YamlException ex)
            {
                // Syntactically malformed YAML is rejected by the parser before structural checks run
                Report(context, job, $"Invalid YAML in file '{shown}': {Redact(ex.Message, judged)}");
                return false;
            }
            catch (InvalidOperationException ex)
            {
                Report(context, job, $"Invalid YAML in file '{shown}': {Redact(ex.Message, judged)}");
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
