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
internal static class GlobMatcher
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
                    files.Add(pattern);
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
