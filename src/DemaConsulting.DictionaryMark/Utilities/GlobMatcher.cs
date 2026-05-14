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
    /// <param name="patterns">Glob patterns to match against.</param>
    /// <returns>Sorted, deduplicated list of matching file paths.</returns>
    /// <exception cref="ArgumentException">Thrown when a pattern is null or empty.</exception>
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
        var normalizedPattern = pattern.Replace('\\', '/');
        var firstWildcard = normalizedPattern.IndexOfAny(['*', '?']);
        var lastSepBeforeWildcard = normalizedPattern.LastIndexOf('/', firstWildcard);
        string baseDir;
        string relPattern;
        if (lastSepBeforeWildcard >= 0)
        {
            baseDir = normalizedPattern[..lastSepBeforeWildcard];
            relPattern = normalizedPattern[(lastSepBeforeWildcard + 1)..];
        }
        else
        {
            baseDir = Path.GetPathRoot(pattern) ?? Environment.CurrentDirectory;
            relPattern = normalizedPattern[baseDir.Length..];
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
