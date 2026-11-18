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

using DemaConsulting.DictionaryMark.Files;

namespace DemaConsulting.DictionaryMark.Cli;

/// <summary>
///     The file-access options of <see cref="Context"/>: <c>--file-access</c> and <c>--allow-root</c>.
/// </summary>
/// <remarks>
///     <para>
///         The options decide which local files the tables of a configuration file may read as inputs and write as
///         outputs; the paths named on the command line are never judged. The mode <c>any</c> (the default) reads
///         every regular local file, as Pandoc does; <c>roots</c> keeps the table inputs and outputs inside the folders
///         of <c>--allow-root</c>, or, without it, inside the working directory and the folder of the configuration
///         file when it is outside it; <c>no-symlinks</c> refuses a symbolic link or junction; and
///         <c>roots,no-symlinks</c> applies both. <c>--allow-root</c> alone implies <c>roots</c>.
///     </para>
///     <para>
///         Every mistake (an unknown mode, <c>--allow-root</c> with <c>any</c> or <c>no-symlinks</c>, a folder that does
///         not exist) is a usage error raised while the options are read, before any input is opened. One policy is
///         built per run and used by every table job.
///     </para>
/// </remarks>
internal sealed partial class Context
{
    /// <summary>
    ///     The name of the option that chooses the file-access mode.
    /// </summary>
    internal const string FileAccessOption = "--file-access";

    /// <summary>
    ///     The name of the option that names an allowed folder.
    /// </summary>
    internal const string AllowRootOption = "--allow-root";

    /// <summary>
    ///     The token of the mode that confines references to folders.
    /// </summary>
    private const string RootsToken = "roots";

    /// <summary>
    ///     The token of the mode that refuses symbolic links and junctions.
    /// </summary>
    private const string NoSymlinksToken = "no-symlinks";

    /// <summary>
    ///     The comparison of two full paths on this platform: case-insensitive on Windows and macOS.
    /// </summary>
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>
    ///     Reads the file-access options and builds the policy of one run.
    /// </summary>
    /// <param name="modeText">The value of the last <c>--file-access</c>, or <see langword="null"/> when it was not given.</param>
    /// <param name="allowRoots">The values of every <c>--allow-root</c>, in order.</param>
    /// <param name="inputs">The inputs whose folders the default roots include: the configuration file, when given.</param>
    /// <param name="workingDirectory">The folder a relative input or <c>--allow-root</c> folder is taken from.</param>
    /// <returns>The policy: <see cref="FileAccessPolicy.AllowAll"/> when neither option is given.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">
    ///     Thrown (a usage error) for a mode that is not one of the four, for <c>--allow-root</c> with
    ///     <c>--file-access any</c> or <c>no-symlinks</c>, and for an <c>--allow-root</c> folder that does not exist.
    /// </exception>
    internal static FileAccessPolicy ReadFileAccess(
        string? modeText, IReadOnlyList<string> allowRoots, IReadOnlyList<string> inputs, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(allowRoots);
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(workingDirectory);

        // --allow-root alone implies roots; an explicit mode is parsed first so that a bad value is reported as such
        var (roots, noSymlinks) = modeText == null ? (allowRoots.Count > 0, false) : ParseFileAccessMode(modeText);
        if (allowRoots.Count > 0 && !roots)
        {
            throw noSymlinks
                ? new ArgumentException(
                    $"The options {FileAccessOption} {NoSymlinksToken} and {AllowRootOption} cannot be used together. "
                    + $"To confine references to the folders and refuse links, write {FileAccessOption} {RootsToken},{NoSymlinksToken} with {AllowRootOption}.")
                : new ArgumentException(
                    $"The options {FileAccessOption} any and {AllowRootOption} cannot be used together. "
                    + $"{AllowRootOption} confines references to the folders it names: drop {FileAccessOption} any, or write {FileAccessOption} {RootsToken}.");
        }

        if (!roots)
        {
            return noSymlinks ? FileAccessPolicy.NoSymlinks : FileAccessPolicy.AllowAll;
        }

        // The folders named replace the default ones; every folder is checked now, before any input is opened
        var folders = allowRoots.Count > 0
            ? allowRoots.Select(text => ResolveRoot(text, workingDirectory)).ToList()
            : DefaultRoots(inputs, workingDirectory);
        var underRoots = FileAccessPolicy.UnderRoots(folders);
        return noSymlinks ? FileAccessPolicy.Combine(underRoots, FileAccessPolicy.NoSymlinks) : underRoots;
    }

    /// <summary>
    ///     Parses the value of <c>--file-access</c>.
    /// </summary>
    /// <param name="text">
    ///     The value: <c>any</c>, <c>roots</c>, <c>no-symlinks</c>, or <c>roots,no-symlinks</c> (in either order), in any
    ///     letter case, each token trimmed.
    /// </param>
    /// <returns>Whether the mode confines references to folders, and whether it refuses links; <c>any</c> is neither.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="text"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown (a usage error) when the value is none of these.</exception>
    internal static (bool Roots, bool NoSymlinks) ParseFileAccessMode(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var tokens = text.Split(',').Select(token => token.Trim().ToLowerInvariant()).ToList();
        if (tokens.Count == 1 && tokens[0] == "any")
        {
            return (false, false);
        }

        // Every token must be one of the two rules, each at most once; "any" does not combine, and an empty or
        // repeated token is a typo
        if (tokens.TrueForAll(token => token is RootsToken or NoSymlinksToken) && tokens.Distinct().Count() == tokens.Count)
        {
            return (tokens.Contains(RootsToken), tokens.Contains(NoSymlinksToken));
        }

        throw new ArgumentException(
            $"'{text}' is not a value of {FileAccessOption}. "
            + $"Use any (the default), {RootsToken}, {NoSymlinksToken}, or {RootsToken},{NoSymlinksToken}.");
    }

    /// <summary>
    ///     Gets the default folders of <c>roots</c>: the working directory, and the folder of each input that is not the
    ///     working directory or inside it.
    /// </summary>
    /// <param name="inputs">The inputs as written on the command line.</param>
    /// <param name="workingDirectory">The working directory, which a relative input is taken from.</param>
    /// <returns>The full paths of the folders, without duplicates, the working directory first.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is <see langword="null"/>.</exception>
    /// <remarks>
    ///     The comparison is lexical (case-insensitive on Windows and macOS). An input whose path cannot be read is
    ///     skipped here: its own loading reports the mistake.
    /// </remarks>
    internal static IReadOnlyList<string> DefaultRoots(IEnumerable<string> inputs, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(inputs);
        ArgumentNullException.ThrowIfNull(workingDirectory);

        var working = Path.GetFullPath(workingDirectory);
        var roots = new List<string> { working };
        foreach (var input in inputs)
        {
            var folder = InputFolder(input, working);
            if (folder != null && !roots.Exists(root => IsSameOrUnder(folder, root)))
            {
                roots.Add(folder);
            }
        }

        return roots;
    }

    /// <summary>
    ///     Gets the full path of the folder an input is in.
    /// </summary>
    /// <param name="input">The input as written.</param>
    /// <param name="workingDirectory">The full path of the working directory.</param>
    /// <returns>The folder, or <see langword="null"/> when the path cannot be read or has no folder.</returns>
    private static string? InputFolder(string input, string workingDirectory)
    {
        ArgumentNullException.ThrowIfNull(input);

        try
        {
            var path = Path.GetFullPath(input, workingDirectory);
            return Path.GetDirectoryName(path);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return null;
        }
    }

    /// <summary>
    ///     Resolves the value of one <c>--allow-root</c> to the full path of an existing folder.
    /// </summary>
    /// <param name="text">The value as written.</param>
    /// <param name="workingDirectory">The folder a relative value is taken from.</param>
    /// <returns>The full path.</returns>
    /// <exception cref="ArgumentException">Thrown (a usage error) when the value is not an existing folder.</exception>
    private static string ResolveRoot(string text, string workingDirectory)
    {
        string? full = null;
        try
        {
            full = string.IsNullOrWhiteSpace(text) ? null : Path.GetFullPath(text, workingDirectory);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            // Reported below as a folder that does not exist
        }

        return full != null && Directory.Exists(full)
            ? full
            : throw new ArgumentException(
                $"The folder '{text}' of {AllowRootOption} does not exist. "
                + "Name an existing folder; a relative folder is taken from the current folder.");
    }

    /// <summary>
    ///     Determines whether a full path is a folder or lies inside it, comparing whole path segments.
    /// </summary>
    /// <param name="path">The full path.</param>
    /// <param name="folder">The full path of the folder.</param>
    /// <returns><see langword="true"/> when <paramref name="path"/> is <paramref name="folder"/> or inside it.</returns>
    private static bool IsSameOrUnder(string path, string folder)
    {
        var trimmedPath = Path.TrimEndingDirectorySeparator(path);
        var trimmedFolder = Path.TrimEndingDirectorySeparator(folder);
        return string.Equals(trimmedPath, trimmedFolder, PathComparison)
               || trimmedPath.StartsWith(trimmedFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, PathComparison);
    }
}
