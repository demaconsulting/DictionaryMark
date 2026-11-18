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

using System.Text.RegularExpressions;

namespace DemaConsulting.DictionaryMark.Utilities;

/// <summary>
///     Resolves user-supplied paths against a base directory, expanding <c>${VAR}</c> references.
/// </summary>
/// <remarks>
///     This unit is the single place where the tool turns a path written by a user (on the command
///     line or in a configuration file) into a path the file system can use. It deliberately
///     allows <c>..</c> segments, because users reference shared files outside the directory that
///     holds their configuration. It is therefore kept separate from <see cref="PathHelpers"/>,
///     which is a path-traversal security boundary that rejects <c>..</c>. All members are static,
///     stateless and thread-safe; they read environment variables but never modify them.
/// </remarks>
internal static partial class PathResolver
{
    /// <summary>
    ///     Expands <c>${VAR}</c> environment-variable references in a path.
    /// </summary>
    /// <remarks>
    ///     Only the <c>${NAME}</c> form is recognized, where NAME starts with a letter or underscore
    ///     followed by letters, digits or underscores. Any other text, including a bare <c>$</c>, is
    ///     left unchanged. Expansion happens exactly once; a variable value that itself contains
    ///     <c>${...}</c> is not expanded again.
    /// </remarks>
    /// <param name="path">The path to expand. Must not be null.</param>
    /// <param name="description">
    ///     Short description of where the path came from (for example <c>glossary path</c>), used in
    ///     the error message. Must not be null.
    /// </param>
    /// <returns>The path with every reference replaced by the variable value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a referenced variable is undefined or empty; the message names the variable,
    ///     the description and the original path.
    /// </exception>
    public static string ExpandEnvironmentVariables(string path, string description)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(description);

        // Replace each ${NAME} reference with the variable value, failing on undefined variables
        return EnvironmentVariableRegex().Replace(path, match =>
        {
            var name = match.Groups[1].Value;
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
            {
                throw new InvalidOperationException(
                    $"Environment variable '{name}' referenced by {description} '{path}' is not defined.");
            }

            return value;
        });
    }

    /// <summary>
    ///     Expands variables in a path and roots it against a base directory without normalizing it.
    /// </summary>
    /// <remarks>
    ///     Used for glob patterns: normalizing a glob with <see cref="Path.GetFullPath(string)"/> would
    ///     collapse sequences such as <c>**/..</c> and change the pattern's meaning. Only
    ///     <paramref name="path"/> is expanded; <paramref name="baseDirectory"/> is used literally so
    ///     that an already-resolved directory is never expanded a second time. An absolute path
    ///     (after expansion) is returned unchanged, so absolute paths ignore the base directory.
    /// </remarks>
    /// <param name="path">The path or glob pattern to root. Must not be null.</param>
    /// <param name="baseDirectory">The directory that relative paths are rooted at. Must not be null.</param>
    /// <param name="description">Description of the path used in error messages. Must not be null.</param>
    /// <returns>The expanded path, rooted at <paramref name="baseDirectory"/> when it was relative.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when a referenced variable is undefined or empty.</exception>
    public static string Combine(string path, string baseDirectory, string description)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(baseDirectory);
        ArgumentNullException.ThrowIfNull(description);

        // Expand the path only, then root it (Path.Combine keeps a rooted second argument as-is)
        var expanded = ExpandEnvironmentVariables(path, description);
        return Path.Combine(baseDirectory, expanded);
    }

    /// <summary>
    ///     Expands variables in a literal path, roots it against a base directory and normalizes it.
    /// </summary>
    /// <remarks>
    ///     Used for literal (non-glob) paths such as output files and the configuration file. The
    ///     result is a full path with <c>.</c> and <c>..</c> segments collapsed. No file-system access
    ///     is performed. A path that the platform rejects as malformed (for example one containing a
    ///     null character) or too long is reported as an <see cref="InvalidOperationException"/>, so
    ///     callers handle every user-path problem through a single exception type.
    /// </remarks>
    /// <param name="path">The literal path to resolve. Must not be null.</param>
    /// <param name="baseDirectory">The directory that relative paths are rooted at. Must not be null.</param>
    /// <param name="description">Description of the path used in error messages. Must not be null.</param>
    /// <returns>The normalized full path.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a referenced variable is undefined or empty, or the path is malformed or too
    ///     long; the message names the description and the original path.
    /// </exception>
    public static string ResolvePath(string path, string baseDirectory, string description) =>
        GetFullPath(Combine(path, baseDirectory, description), path, description);

    /// <summary>
    ///     Determines whether an exception reports a malformed, unsupported or overlong path.
    /// </summary>
    /// <remarks>
    ///     Gives every caller the same definition of "invalid path" for use in exception filters, so
    ///     the list of path-validation exceptions raised by <see cref="Path"/>, <see cref="File"/> and
    ///     <see cref="Directory"/> members is kept in one place. <see cref="ArgumentNullException"/> is
    ///     excluded because a null argument is a programming error, not a user-supplied path problem.
    /// </remarks>
    /// <param name="exception">The exception to classify. Must not be null.</param>
    /// <returns>
    ///     True for <see cref="ArgumentException"/> (other than <see cref="ArgumentNullException"/>),
    ///     <see cref="NotSupportedException"/> and <see cref="PathTooLongException"/>; otherwise false.
    /// </returns>
    public static bool IsInvalidPathException(Exception exception) =>
        exception is (ArgumentException and not ArgumentNullException) or NotSupportedException or PathTooLongException;

    /// <summary>
    ///     Resolves a directory path against a base directory as text only, without accessing the file system.
    /// </summary>
    /// <remarks>
    ///     Used for the <c>root</c> key of a configuration file, which is untrusted content: its only role is
    ///     the base that relative job paths are combined with, so it is never probed. Whether the files a job
    ///     reaches through it exist, and whether they may be read or written, is decided later by the
    ///     file-access resolver. A root becomes the literal prefix of every rooted glob pattern, so a wildcard
    ///     in it would corrupt the split between the literal base directory and the wildcard remainder;
    ///     wildcards are therefore rejected. Stateless and thread-safe; reads environment variables only.
    /// </remarks>
    /// <param name="path">The directory path to resolve. Must not be null.</param>
    /// <param name="baseDirectory">The directory that a relative path is rooted at. Must not be null.</param>
    /// <param name="description">
    ///     Description of where the path came from (for example <c>root key</c>), used in error messages.
    ///     Must not be null.
    /// </param>
    /// <returns>The normalized full path; the directory may or may not exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a referenced variable is undefined, the expanded path contains <c>*</c> or <c>?</c>,
    ///     or the path is malformed or too long; the message names the description and path.
    /// </exception>
    public static string ResolveDirectory(string path, string baseDirectory, string description)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(baseDirectory);
        ArgumentNullException.ThrowIfNull(description);

        // Expand and root the path, rejecting wildcards before any normalization
        var combined = Combine(path, baseDirectory, description);
        if (combined.Contains('*') || combined.Contains('?'))
        {
            throw new InvalidOperationException(
                $"{Capitalize(description)} '{path}' must not contain wildcard characters.");
        }

        // Collapse "." and ".." segments; this is text arithmetic and touches nothing on disk
        return GetFullPath(combined, path, description);
    }

    /// <summary>
    ///     Resolves a directory path against a base directory and requires the directory to exist.
    /// </summary>
    /// <remarks>
    ///     Used for the trusted <c>--root</c> command-line option. The path is first resolved as text by
    ///     <see cref="ResolveDirectory"/> (which rejects wildcards); the check for existence then performs
    ///     file-system access; an existing file is not accepted as a directory.
    /// </remarks>
    /// <param name="path">The directory path to resolve. Must not be null.</param>
    /// <param name="baseDirectory">The directory that a relative path is rooted at. Must not be null.</param>
    /// <param name="description">
    ///     Description of where the path came from (for example <c>--root option</c>), used in error
    ///     messages. Must not be null.
    /// </param>
    /// <returns>The normalized full path of the existing directory.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when a referenced variable is undefined, the expanded path contains <c>*</c> or
    ///     <c>?</c>, the path is malformed or too long, or the directory does not exist; the message
    ///     names the description and path.
    /// </exception>
    public static string ResolveExistingDirectory(string path, string baseDirectory, string description)
    {
        // Resolve the path as text (validating the arguments and rejecting wildcards), then require the
        // directory to exist
        var full = ResolveDirectory(path, baseDirectory, description);
        if (!Directory.Exists(full))
        {
            throw new InvalidOperationException(
                $"Directory '{full}' specified by {description} '{path}' does not exist.");
        }

        return full;
    }

    /// <summary>
    ///     Normalizes a rooted path, reporting a malformed or overlong path with the user's context.
    /// </summary>
    /// <param name="combined">The expanded and rooted path to normalize.</param>
    /// <param name="path">The path as written by the user, quoted in the error message.</param>
    /// <param name="description">Description of the path used in the error message.</param>
    /// <returns>The normalized full path.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <see cref="Path.GetFullPath(string)"/> rejects the path as malformed, unsupported
    ///     or too long.
    /// </exception>
    private static string GetFullPath(string combined, string path, string description)
    {
        try
        {
            return Path.GetFullPath(combined);
        }
        catch (Exception ex) when (IsInvalidPathException(ex))
        {
            throw new InvalidOperationException($"Invalid {description} '{path}': {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Capitalizes the first character of a description so it can start a sentence.
    /// </summary>
    /// <param name="text">The text to capitalize.</param>
    /// <returns>The text with its first character in upper case.</returns>
    private static string Capitalize(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    /// <summary>Matches <c>${VAR}</c> references.</summary>
    [GeneratedRegex(@"\$\{([A-Za-z_][A-Za-z0-9_]*)\}")]
    private static partial Regex EnvironmentVariableRegex();
}
