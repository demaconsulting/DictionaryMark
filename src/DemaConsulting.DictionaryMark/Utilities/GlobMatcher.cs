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

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace DemaConsulting.DictionaryMark.Utilities;

/// <summary>
///     Matches files using glob patterns.
/// </summary>
internal static partial class GlobMatcher
{
    /// <summary>
    ///     Gets files matching the specified glob patterns.
    /// </summary>
    /// <remarks>
    ///     Resolves absolute paths without wildcards directly via <see cref="File.Exists"/>. For absolute
    ///     paths with wildcards, extracts the non-wildcard base directory and applies the remaining glob
    ///     pattern using <c>Microsoft.Extensions.FileSystemGlobbing</c>. Relative patterns are resolved
    ///     against <see cref="Environment.CurrentDirectory"/> at the time of the call; this method is not
    ///     thread-safe if <see cref="Environment.CurrentDirectory"/> is changed concurrently from another
    ///     thread. File-system I/O is performed; exceptions from the underlying globbing library propagate
    ///     to the caller.
    /// </remarks>
    /// <param name="patterns">
    ///     Collection of file path patterns to resolve. Patterns may be absolute or relative, with or
    ///     without wildcard characters (<c>*</c>, <c>?</c>). Must not be null; each element must be
    ///     non-null and non-empty.
    /// </param>
    /// <returns>
    ///     Sorted (ordinal, case-insensitive), deduplicated list of absolute file paths matching the
    ///     supplied patterns; an empty list when no patterns produce a match.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="patterns"/> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when any element of <paramref name="patterns"/> is null or empty.
    /// </exception>
    public static IReadOnlyList<string> GetFiles(IEnumerable<string> patterns)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(patterns);

        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in patterns)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException("Pattern cannot be null or empty.", nameof(patterns));
            }

            // Check if absolute path with no wildcards
            if (Path.IsPathRooted(pattern) && !pattern.Contains('*') && !pattern.Contains('?'))
            {
                if (File.Exists(pattern))
                {
                    files.Add(Path.GetFullPath(pattern));
                }

                continue;
            }

            // For absolute path with wildcards, find the non-wildcard base directory
            if (Path.IsPathRooted(pattern))
            {
                MatchAbsoluteGlob(pattern, files);
                continue;
            }

            // Use glob matching relative to current directory
            MatchRelativeGlob(pattern, files);
        }

        return [.. files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    ///     Resolves input patterns strictly against the current working directory.
    /// </summary>
    /// <remarks>
    ///     Equivalent to <see cref="Resolve(IEnumerable{string}, string)"/> with
    ///     <see cref="Environment.CurrentDirectory"/> as the base directory, which keeps the original
    ///     command-line behavior when no root directory is given.
    /// </remarks>
    /// <param name="patterns">Patterns to resolve. Must not be null; elements must be non-empty.</param>
    /// <returns>Sorted (ordinal, case-insensitive), deduplicated list of absolute file paths.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="patterns"/> is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when an element is null or empty, or a glob is rejected by the globbing library
    ///     (for example a <c>..</c> segment after a wildcard).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when an environment variable is undefined or empty, a glob matches no files, or a
    ///     literal directory or file does not exist; the message names the pattern or full path.
    /// </exception>
    public static IReadOnlyList<string> Resolve(IEnumerable<string> patterns) =>
        Resolve(patterns, Environment.CurrentDirectory);

    /// <summary>
    ///     Resolves input patterns strictly against a base directory: every pattern must produce at
    ///     least one file.
    /// </summary>
    /// <remarks>
    ///     Each pattern first has <c>${VAR}</c> environment-variable references expanded and, when
    ///     relative, is rooted at <paramref name="baseDirectory"/> by <see cref="PathResolver"/>. A
    ///     pattern containing <c>*</c> or <c>?</c> is then treated as an absolute glob and must match
    ///     at least one file; <c>..</c> segments anywhere before the first wildcard are supported
    ///     because the literal part of the pattern becomes the matcher's base directory. Any other
    ///     pattern is a literal path resolved with <see cref="Path.GetFullPath(string)"/> (absolute,
    ///     relative and UNC paths are supported) and must exist. Absolute patterns ignore the base
    ///     directory. This method never silently produces an empty result for a pattern that matches
    ///     nothing. File-system I/O is performed; the method is thread-safe.
    /// </remarks>
    /// <param name="patterns">Patterns to resolve. Must not be null; elements must be non-empty.</param>
    /// <param name="baseDirectory">
    ///     Directory that relative patterns are resolved against (the working directory, the
    ///     configuration file's directory, or a root directory). Must not be null.
    /// </param>
    /// <returns>Sorted (ordinal, case-insensitive), deduplicated list of absolute file paths.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown when an element is null or empty, or a glob is rejected by the globbing library
    ///     (for example a <c>..</c> segment after a wildcard).
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when an environment variable is undefined or empty, a glob matches no files, or a
    ///     literal directory or file does not exist; the message names the pattern or full path.
    /// </exception>
    public static IReadOnlyList<string> Resolve(IEnumerable<string> patterns, string baseDirectory)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(patterns);
        ArgumentNullException.ThrowIfNull(baseDirectory);

        var files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var pattern in patterns)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException("Pattern cannot be null or empty.", nameof(patterns));
            }

            // Expand variables and root the pattern so every glob is matched as an absolute glob
            var expanded = PathResolver.Combine(pattern, baseDirectory, "glossary path");

            if (expanded.Contains('*') || expanded.Contains('?'))
            {
                var matches = GetFiles([expanded]);
                if (matches.Count == 0)
                {
                    throw new InvalidOperationException($"Input pattern '{pattern}' matched no files.");
                }

                foreach (var match in matches)
                {
                    files.Add(match);
                }

                continue;
            }

            var fullPath = Path.GetFullPath(expanded);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                throw new InvalidOperationException($"Input directory '{directory}' was not found (pattern '{pattern}').");
            }

            if (!File.Exists(fullPath))
            {
                throw new InvalidOperationException($"Input file '{fullPath}' was not found.");
            }

            files.Add(fullPath);
        }

        return [.. files.OrderBy(f => f, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    ///     Matches an absolute path glob pattern against the file system.
    /// </summary>
    /// <param name="pattern">Absolute path pattern containing wildcards.</param>
    /// <param name="files">Set to add matching file paths to.</param>
    private static void MatchAbsoluteGlob(string pattern, HashSet<string> files)
    {
        // Normalize to forward slashes so all subsequent index arithmetic is consistent
        var normalizedPattern = pattern.Replace('\\', '/');

        // Find the last separator before the first wildcard to identify the base directory
        var firstWildcard = normalizedPattern.IndexOfAny(['*', '?']);
        var lastSepBeforeWildcard = normalizedPattern.LastIndexOf('/', firstWildcard);

        string baseDir;
        string relPattern;
        if (lastSepBeforeWildcard > 0)
        {
            // Split at the separator: everything before it is the base directory
            baseDir = normalizedPattern[..lastSepBeforeWildcard];
            relPattern = normalizedPattern[(lastSepBeforeWildcard + 1)..];

            // On Windows, a bare drive letter "C:" resolves to the current directory of
            // that drive, not its root. Append a slash to get the drive root "C:/".
            if (baseDir.Length == 2 && baseDir[1] == ':')
            {
                baseDir += '/';
            }
        }
        else if (lastSepBeforeWildcard == 0)
        {
            // Wildcard immediately after the leading slash (e.g. "/*.yaml" on Unix):
            // base is the filesystem root.
            baseDir = "/";
            relPattern = normalizedPattern[1..];
        }
        else
        {
            // No separator before the wildcard - derive the root from the normalized path
            var root = Path.GetPathRoot(normalizedPattern) ?? string.Empty;
            baseDir = root.TrimEnd('/');
            relPattern = normalizedPattern[root.Length..];
        }

        if (Directory.Exists(baseDir))
        {
            // Normalize the literal base so ".." segments before the first wildcard are collapsed
            baseDir = Path.GetFullPath(baseDir);
            var matcher = new Matcher();
            matcher.AddInclude(relPattern);
            var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(baseDir)));
            foreach (var match in result.Files)
            {
                var fullPath = Path.GetFullPath(Path.Combine(baseDir, match.Path));
                files.Add(fullPath);
            }
        }
    }

    /// <summary>
    ///     Matches a relative glob pattern against the current directory.
    /// </summary>
    /// <param name="pattern">Relative glob pattern.</param>
    /// <param name="files">Set to add matching file paths to.</param>
    private static void MatchRelativeGlob(string pattern, HashSet<string> files)
    {
        var matcher = new Matcher();
        matcher.AddInclude(pattern);
        var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(Environment.CurrentDirectory)));
        foreach (var match in result.Files)
        {
            var fullPath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, match.Path));
            files.Add(fullPath);
        }
    }
}
