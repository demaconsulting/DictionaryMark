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
    ///     Thrown when an element is null or empty, a literal path is malformed (for example it contains a
    ///     null character), or a glob is rejected by the globbing library (for example a <c>..</c> segment
    ///     after a wildcard).
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
    ///     Thrown when an element is null or empty, a literal path is malformed (for example it contains a
    ///     null character), or a glob is rejected by the globbing library (for example a <c>..</c> segment
    ///     after a wildcard).
    /// </exception>
    /// <exception cref="PathTooLongException">Thrown when a literal path exceeds the platform's maximum length.</exception>
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
        var (baseDir, relPattern) = SplitGlob(pattern);
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
    ///     Splits a glob pattern into its literal folder (the folders before the first wildcard) and the wildcard
    ///     remainder that is matched below that folder.
    /// </summary>
    /// <remarks>
    ///     A pure text operation. Backslashes become forward slashes. A bare drive (<c>C:</c>) becomes its root
    ///     (<c>C:/</c>), and a wildcard right after a leading slash gives the root <c>/</c>. A relative pattern whose
    ///     first component holds a wildcard has an empty folder. The folder is not normalized, so <c>..</c>
    ///     segments before the first wildcard are kept.
    /// </remarks>
    /// <param name="pattern">The glob pattern, containing at least one wildcard. Must not be null.</param>
    /// <returns>The literal folder (possibly empty) and the remainder.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pattern"/> is null.</exception>
    internal static (string Folder, string Pattern) SplitGlob(string pattern)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(pattern);

        // Normalize to forward slashes so all subsequent index arithmetic is consistent
        var normalizedPattern = pattern.Replace('\\', '/');

        // Find the last separator before the first wildcard to identify the base directory
        var firstWildcard = normalizedPattern.IndexOfAny(['*', '?']);
        var lastSepBeforeWildcard = firstWildcard < 0 ? -1 : normalizedPattern.LastIndexOf('/', firstWildcard);
        if (lastSepBeforeWildcard > 0)
        {
            // Split at the separator: everything before it is the base directory
            var baseDir = normalizedPattern[..lastSepBeforeWildcard];

            // On Windows, a bare drive letter "C:" resolves to the current directory of
            // that drive, not its root. Append a slash to get the drive root "C:/".
            if (baseDir.Length == 2 && baseDir[1] == ':')
            {
                baseDir += '/';
            }

            return (baseDir, normalizedPattern[(lastSepBeforeWildcard + 1)..]);
        }

        if (lastSepBeforeWildcard == 0)
        {
            // Wildcard immediately after the leading slash (e.g. "/*.yaml" on Unix): base is the filesystem root
            return ("/", normalizedPattern[1..]);
        }

        // No separator before the wildcard - derive the root from the normalized path
        var root = Path.GetPathRoot(normalizedPattern) ?? string.Empty;
        return (root.TrimEnd('/'), normalizedPattern[root.Length..]);
    }

    /// <summary>
    ///     Matches a glob remainder below a folder that the caller has already judged, without probing the folder by
    ///     name, and asks the caller before it lists any folder that is a symbolic link or junction.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Used for configuration-file patterns, whose folders must be judged by the file-access policy before
    ///         they are listed. The folder is listed directly (a folder that does not exist gives no matches), with
    ///         no existence probe. Every child folder the matcher may descend into is checked for being a link when
    ///         it is listed (on Windows by its reparse-point attribute and then its link data, on Linux and macOS by
    ///         its link data); before such a link folder is itself listed, <paramref name="checkLinkFolder"/> is
    ///         called with its full path. It returns true to list the link folder, false to list nothing in it (for
    ///         example a link whose target is missing), or throws to stop the matching; the exception propagates to
    ///         the caller. A link folder the matcher never descends into is never checked or listed.
    ///     </para>
    ///     <para>
    ///         A <c>..</c> segment cannot climb above a listed folder: a request for a parent folder throws
    ///         <see cref="ArgumentException"/>. File-system I/O is performed; the method is thread-safe.
    ///     </para>
    /// </remarks>
    /// <param name="folder">The full, normalized path of the folder to match below. Must not be null.</param>
    /// <param name="pattern">The wildcard remainder (see <see cref="SplitGlob(string)"/>). Must not be null.</param>
    /// <param name="checkLinkFolder">The check called before a link folder is listed. Must not be null.</param>
    /// <returns>The matched files, with their full paths and their paths relative to the folder (with '/').</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the globbing library rejects the pattern.</exception>
    /// <exception cref="IOException">Thrown when a folder cannot be listed.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown when the system denies listing a folder.</exception>
    internal static IReadOnlyList<GlobMatch> MatchUnder(string folder, string pattern, Func<string, bool> checkLinkFolder)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(folder);
        ArgumentNullException.ThrowIfNull(pattern);
        ArgumentNullException.ThrowIfNull(checkLinkFolder);

        // Match through the guarded wrapper, which never probes by name and asks before listing a link folder
        var matcher = new Matcher();
        matcher.AddInclude(pattern);
        var result = matcher.Execute(new GuardedDirectory(new DirectoryInfo(folder), isLink: false, checkLinkFolder, parent: null));
        return [.. result.Files.Select(match => new GlobMatch(Path.GetFullPath(Path.Combine(folder, match.Path)), match.Path))];
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

    /// <summary>
    ///     Determines whether a listed child folder is a symbolic link or junction.
    /// </summary>
    /// <remarks>
    ///     On Windows the attributes the listing already holds tell a reparse point without another call, and only
    ///     those are asked for their link data; on Linux and macOS every child folder's link data is read, which never
    ///     follows the link.
    /// </remarks>
    /// <param name="directory">The listed child folder.</param>
    /// <returns>True when it is a link.</returns>
    private static bool IsLinkFolder(DirectoryInfo directory) =>
        (!OperatingSystem.IsWindows() || directory.Attributes.HasFlag(FileAttributes.ReparsePoint))
        && RealPath.IsLink(directory.FullName);

    /// <summary>
    ///     A folder offered to the globbing library that lists itself without an existence probe and asks before a
    ///     link folder is listed.
    /// </summary>
    private sealed class GuardedDirectory : DirectoryInfoBase
    {
        /// <summary>The folder.</summary>
        private readonly DirectoryInfo _directory;

        /// <summary>Whether the folder is a symbolic link or junction.</summary>
        private readonly bool _isLink;

        /// <summary>The check called before a link folder is listed.</summary>
        private readonly Func<string, bool> _checkLinkFolder;

        /// <summary>The folder this one was listed in, or null for the folder matched below.</summary>
        private readonly GuardedDirectory? _parent;

        /// <summary>
        ///     Initializes a new instance of the <see cref="GuardedDirectory"/> class.
        /// </summary>
        /// <param name="directory">The folder.</param>
        /// <param name="isLink">Whether the folder is a symbolic link or junction.</param>
        /// <param name="checkLinkFolder">The check called before a link folder is listed.</param>
        /// <param name="parent">The folder this one was listed in, or null.</param>
        public GuardedDirectory(DirectoryInfo directory, bool isLink, Func<string, bool> checkLinkFolder, GuardedDirectory? parent)
        {
            _directory = directory;
            _isLink = isLink;
            _checkLinkFolder = checkLinkFolder;
            _parent = parent;
        }

        /// <inheritdoc/>
        public override string Name => _directory.Name;

        /// <inheritdoc/>
        public override string FullName => _directory.FullName;

        /// <inheritdoc/>
        public override DirectoryInfoBase? ParentDirectory => _parent;

        /// <inheritdoc/>
        public override IEnumerable<FileSystemInfoBase> EnumerateFileSystemInfos()
        {
            // A link folder is judged before it is listed; a rejected one throws, one with nothing to list is empty
            if (_isLink && !_checkLinkFolder(_directory.FullName))
            {
                return [];
            }

            // The listing itself reports a missing folder, so no existence probe is needed
            IEnumerable<FileSystemInfo> entries;
            try
            {
                entries = _directory.EnumerateFileSystemInfos("*", SearchOption.TopDirectoryOnly);
            }
            catch (DirectoryNotFoundException)
            {
                return [];
            }

            return Wrap(entries);
        }

        /// <inheritdoc/>
        public override DirectoryInfoBase? GetDirectory(string path)
        {
            // A parent folder would leave the folder that was judged before it was listed
            if (string.Equals(path, "..", StringComparison.Ordinal))
            {
                throw new ArgumentException("A '..' segment after a wildcard is not supported.", nameof(path));
            }

            var matches = _directory.GetDirectories(path);
            return matches.Length == 1 ? new GuardedDirectory(matches[0], IsLinkFolder(matches[0]), _checkLinkFolder, this) : null;
        }

        /// <inheritdoc/>
        public override FileInfoBase? GetFile(string path) =>
            new FileInfoWrapper(new FileInfo(Path.Combine(_directory.FullName, path)));

        /// <summary>
        ///     Wraps the listed entries, marking each child folder that is a link.
        /// </summary>
        /// <param name="entries">The listed entries.</param>
        /// <returns>The wrapped entries.</returns>
        private IEnumerable<FileSystemInfoBase> Wrap(IEnumerable<FileSystemInfo> entries)
        {
            foreach (var entry in entries)
            {
                yield return entry is DirectoryInfo directory
                    ? new GuardedDirectory(directory, IsLinkFolder(directory), _checkLinkFolder, this)
                    : new FileInfoWrapper((FileInfo)entry);
            }
        }
    }
}

/// <summary>
///     A file matched by <see cref="GlobMatcher.MatchUnder(string, string, Func{string, bool})"/>.
/// </summary>
/// <param name="FullPath">The full path of the file.</param>
/// <param name="RelativePath">The path of the file relative to the folder matched below, with '/' separators.</param>
internal readonly record struct GlobMatch(string FullPath, string RelativePath);
