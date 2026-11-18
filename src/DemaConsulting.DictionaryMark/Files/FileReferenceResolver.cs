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

using Microsoft.Win32.SafeHandles;

namespace DemaConsulting.DictionaryMark.Files;

/// <summary>
///     Resolves a file that a document references (a table input of a configuration file) to the bytes of that file,
///     after a host-chosen <see cref="FileAccessPolicy"/> has approved the file that was actually opened.
/// </summary>
/// <remarks>
///     <para>
///         <b>Why it exists.</b> Every caller that reads files named by a document needs the same mechanics: find the
///         file relative to the base directory, refuse references that are never safe (network and device paths,
///         alternate data streams, control characters, anything that is not a regular file), let the host decide which
///         files may be read, bound what is read, and turn every failure into one exception type. This type is that
///         single, tested implementation. The default policy, <see cref="FileAccessPolicy.AllowAll"/>, reads any
///         regular local file, as Pandoc does: <c>..</c>, absolute paths and symbolic links are followed.
///     </para>
///     <para>
///         <b>Race-free check.</b> A file is opened first; the policy is then asked about the real path of the
///         <b>open handle</b> (see <see cref="FileAccessRequest.RealPath"/>), and the bytes are read from that same
///         handle. A link swapped in or out by another process after a name-based check therefore cannot redirect the
///         read: the decision is always about the file that is read. A policy that restricts paths
///         (<see cref="FileAccessPolicy.UnderRoots(string[])"/>) also refuses a path as written that lies outside its
///         roots before anything is opened.
///     </para>
///     <para>
///         <b>Lookup.</b> A relative reference is resolved against <see cref="FileReferenceOptions.BaseDirectory"/>;
///         an absolute reference (<c>/x</c>, or <c>C:/x</c> on Windows) names exactly one candidate. A file that does
///         not exist, including a dangling link, is a miss.
///     </para>
///     <para>
///         <b>Caching and limits.</b> Each distinct full path is opened at most once per instance (a miss and a failure
///         are remembered too); the policy is asked again on every <see cref="Resolve(string)"/>. A file is never longer
///         than <see cref="FileReferenceOptions.MaxFileBytes"/>.
///     </para>
///     <para>
///         <b>Thread safety.</b> An instance is <b>not</b> thread-safe: it keeps unsynchronized caches and invokes the
///         policy on the calling thread. Use one instance per operation (one table job). The options, policies and
///         returned <see cref="FileReference"/> values are immutable and may be shared.
///     </para>
/// </remarks>
internal sealed partial class FileReferenceResolver
{
    /// <summary>The number of bytes read from a stream per call.</summary>
    private const int ReadChunkSize = 8192;

    /// <summary>The effective options.</summary>
    private readonly FileReferenceOptions _options;

    /// <summary>The policy that decides whether an opened file may be read.</summary>
    private readonly FileAccessPolicy _policy;

    /// <summary>The test seams around the open, or <see langword="null"/>.</summary>
    private readonly FileOpenHooks? _hooks;

    /// <summary>The outcome of each key that was looked up; <see langword="null"/> remembers a key that does not exist.</summary>
    private readonly Dictionary<string, Outcome?> _outcomes;

    /// <summary>
    ///     The real location of each trusted folder a policy compares against, captured the first time it is needed and
    ///     before any file below it is opened; <see langword="null"/> where it could not be determined.
    /// </summary>
    private readonly Dictionary<string, string?> _folderLocations = new(RealPath.PathComparer);

    /// <summary>
    ///     Initializes a resolver for one operation.
    /// </summary>
    /// <param name="options">Where files may be found, which may be read, and how large. Must not be null.</param>
    /// <exception cref="ArgumentNullException">
    ///     Thrown when <paramref name="options"/> or its <see cref="FileReferenceOptions.FileAccess"/> is null.
    /// </exception>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <see cref="FileReferenceOptions.MaxFileBytes"/> is not positive.
    /// </exception>
    public FileReferenceResolver(FileReferenceOptions options)
        : this(options, null)
    {
    }

    /// <summary>
    ///     Initializes a resolver with test seams around the open, so the race-free check can be proven without timing.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="hooks">The seams, or <see langword="null"/>.</param>
    internal FileReferenceResolver(FileReferenceOptions options, FileOpenHooks? hooks)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(options.FileAccess);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(options.MaxFileBytes, 0L);
        _options = options;
        _policy = options.FileAccess;
        _hooks = hooks;
        _outcomes = new Dictionary<string, Outcome?>(RealPath.PathComparer);
    }

    /// <summary>
    ///     Gets a value indicating whether a way of finding files is configured: a base directory.
    ///     <see cref="Resolve(string)"/> returns <see langword="null"/> for every reference when it is
    ///     <see langword="false"/>.
    /// </summary>
    private bool HasSource => _options.BaseDirectory is not null;

    /// <summary>
    ///     Finds, checks and reads the file a reference names.
    /// </summary>
    /// <param name="reference">
    ///     The path as written in the document: relative (resolved against the base directory) or absolute.
    ///     Backslashes are treated as forward slashes, so a reference means the same on every platform.
    /// </param>
    /// <returns>The file, or <see langword="null"/> when no candidate exists (or no file source is configured).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reference"/> is null.</exception>
    /// <exception cref="FileReferenceException">
    ///     Thrown when the reference is never allowed, a configured directory is invalid, the policy denies the file
    ///     (or cannot be applied), the path is not a regular file, or the file cannot be read or is too large.
    /// </exception>
    public FileReference? Resolve(string reference)
    {
        ArgumentNullException.ThrowIfNull(reference);

        // The text is validated before anything is touched, so a hostile reference never causes an access or a request
        var shown = Shown(reference);
        var text = ValidateText(reference, shown);
        if (!HasSource)
        {
            return null;
        }

        // Candidates are produced lazily: the first one that exists wins, so later ones are only examined on a miss
        foreach (var candidate in Candidates(text, shown))
        {
            var file = LoadFile(candidate, shown);
            if (file is not null)
            {
                return file;
            }
        }

        return null;
    }

    /// <summary>
    ///     Reads a whole stream as bytes under a size limit, stopping once the limit is certainly exceeded.
    /// </summary>
    /// <remarks>
    ///     A size the stream reports is checked first, and the running length is checked after every chunk, so a stream
    ///     that never ends stops one chunk past the limit. Failures of the stream itself are not caught here: the
    ///     caller decides what a failing source means.
    /// </remarks>
    /// <param name="stream">The stream; it is left open.</param>
    /// <param name="maxBytes">The largest number of bytes accepted.</param>
    /// <returns>The bytes, or <see langword="null"/> when the stream is longer than the limit.</returns>
    internal static byte[]? ReadBounded(Stream stream, long maxBytes)
    {
        if (stream.CanSeek && stream.Length > maxBytes)
        {
            return null;
        }

        using var bytes = new MemoryStream();
        var buffer = new byte[ReadChunkSize];
        int count;
        while ((count = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (bytes.Length + count > maxBytes)
            {
                return null;
            }

            bytes.Write(buffer, 0, count);
        }

        return bytes.ToArray();
    }

    /// <summary>
    ///     Loads a file-system candidate: lexical pre-check, cache, then open, inspect, check and read.
    /// </summary>
    /// <param name="candidate">The candidate.</param>
    /// <param name="shown">The reference as shown in messages.</param>
    /// <returns>The file, or <see langword="null"/> when the candidate does not exist.</returns>
    private FileReference? LoadFile(Candidate candidate, string shown)
    {
        // A restrictive policy refuses a path as written outside its area before the open, so such a file is never
        // opened: no existence oracle, and no device outside the area is touched
        if (_policy.PreCheck(candidate.Key) is { IsAllowed: false } early)
        {
            throw Denied(shown, candidate.Key, null, early, null);
        }

        // A known key answers from the cache; the policy is asked again on every request
        if (_outcomes.TryGetValue(candidate.Key, out var known))
        {
            if (known?.File is { } cachedFile && _policy.RequiresRealPath)
            {
                CheckPolicy(candidate, shown, cachedFile.RealPath!, CaptureBeforeOpen(candidate));
            }

            return known is null ? null : Deliver(known, shown, candidate.Key);
        }

        var outcome = OpenCheckAndRead(candidate, shown);
        _outcomes[candidate.Key] = outcome;
        return outcome is null ? null : Deliver(outcome, shown, candidate.Key);
    }

    /// <summary>
    ///     Opens a candidate, inspects the open handle, asks the policy about it and reads it from the same handle.
    /// </summary>
    /// <param name="candidate">The candidate.</param>
    /// <param name="shown">The reference as shown in messages.</param>
    /// <returns>The outcome to remember, or <see langword="null"/> when the candidate does not exist.</returns>
    /// <exception cref="FileReferenceException">Thrown when the policy denies the file (not remembered: it is asked again next time).</exception>
    private Outcome? OpenCheckAndRead(Candidate candidate, string shown)
    {
        // The trusted folders the policy compares against are captured before the open; after the open nothing is
        // resolved by name, so a folder renamed and replaced by a link in between cannot change the decision
        var snapshot = CaptureBeforeOpen(candidate);
        var key = candidate.Key;
        _hooks?.BeforeOpen?.Invoke(key);
        SafeFileHandle handle;
        try
        {
            handle = _hooks?.Open?.Invoke(key) ?? RealPath.OpenForRead(key);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            // The file is missing (a dangling link included)
            return null;
        }
        catch (UnauthorizedAccessException ex) when (Directory.Exists(key))
        {
            // Windows refuses to open a directory as a file; it exists, but it is not a file
            return Outcome.Failed(FileReferenceFailureKind.NotARegularFile, null, ex);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Any other failure to open an existing file is a located read failure
            return Outcome.Failed(FileReferenceFailureKind.CannotRead, null, ex);
        }

        using (handle)
        {
            // The facts come from the handle, never from the name: whatever happens to the name after the open does
            // not change what the policy sees or what is read
            _hooks?.AfterOpen?.Invoke(key);
            var facts = RealPath.Inspect(handle);
            var realPath = _hooks?.RealPathOf is { } realPathOf ? realPathOf(handle) : facts.FinalPath;

            // A local path that leads to a network share (a link to a UNC target) is refused under every policy; a path
            // the host placed on a network drive or share itself is not. Where the real path is unknown, a restrictive
            // policy fails closed below, and the default policy reads the file
            if (OperatingSystem.IsWindows() && realPath is not null
                && RealPath.IsNetworkLocation(realPath) && !RealPath.IsNetworkLocation(key))
            {
                return Outcome.Failed(FileReferenceFailureKind.NetworkPath, realPath, null);
            }

            // Only a handle known to be a regular file is read: a directory, a device, a pipe, or a handle whose type the
            // platform cannot tell is refused under every policy (fail closed)
            var isRegularFile = _hooks?.IsRegularFileOf is { } isRegularFileOf ? isRegularFileOf(handle) : facts.IsRegularFile;
            if (isRegularFile != true)
            {
                return Outcome.Failed(FileReferenceFailureKind.NotARegularFile, realPath, null);
            }

            // A restrictive policy is never consulted without the real path: unknown means refused (fail closed)
            if (_policy.RequiresRealPath)
            {
                if (realPath is null)
                {
                    return Outcome.Failed(FileReferenceFailureKind.RealPathUnavailable, null, null);
                }

                CheckPolicy(candidate, shown, realPath, snapshot);
            }

            return Read(handle, candidate, realPath);
        }
    }

    /// <summary>
    ///     Captures, before a file is opened, what a restrictive policy compares the opened file against: the real
    ///     location of each trusted folder it names (captured once per resolver, on first use) and the long spelling of
    ///     the written path.
    /// </summary>
    /// <param name="candidate">The candidate about to be opened.</param>
    /// <returns>The snapshot, or <see langword="null"/> for a policy that needs no real path.</returns>
    private PreOpenSnapshot? CaptureBeforeOpen(Candidate candidate)
    {
        if (!_policy.RequiresRealPath)
        {
            return null;
        }

        var anchors = new Dictionary<string, string?>(RealPath.PathComparer);
        foreach (var anchor in _policy.AnchorsOf(candidate.Key, candidate.Origin))
        {
            if (!_folderLocations.TryGetValue(anchor, out var location))
            {
                location = _hooks?.DirectoryLocationOf is { } locationOf
                    ? locationOf(anchor)
                    : RealPath.TryGetCanonicalDirectory(anchor);
                _folderLocations[anchor] = location;
            }

            anchors[anchor] = location;
        }

        return new PreOpenSnapshot(FileAccessPolicy.LongSpelling(candidate.Key), anchors);
    }

    /// <summary>
    ///     Reads an approved file from its open handle.
    /// </summary>
    /// <param name="handle">The open handle; disposed by the caller.</param>
    /// <param name="candidate">The candidate: its key and the origin of its chain.</param>
    /// <param name="realPath">The real path, or <see langword="null"/>.</param>
    /// <returns>The outcome.</returns>
    private Outcome Read(SafeFileHandle handle, Candidate candidate, string? realPath)
    {
        // Once the file is open it exists: a failure while reading it - even one spelled "not found" - is a read failure
        try
        {
            using var stream = new FileStream(handle, FileAccess.Read, bufferSize: 1);
            var bytes = ReadBounded(stream, _options.MaxFileBytes);
            var key = candidate.Key;
            return bytes is null
                ? Outcome.Failed(FileReferenceFailureKind.TooLarge, realPath, null)
                : new Outcome(new FileReference(key, realPath, bytes), null, realPath, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
        {
            return Outcome.Failed(FileReferenceFailureKind.CannotRead, realPath, ex);
        }
    }

    /// <summary>
    ///     Asks the policy about an opened file and throws its denial.
    /// </summary>
    /// <param name="candidate">The candidate.</param>
    /// <param name="shown">The reference as shown in messages.</param>
    /// <param name="realPath">The real path of the opened file.</param>
    /// <param name="snapshot">What was captured before the open, or <see langword="null"/>.</param>
    /// <exception cref="FileReferenceException">Thrown when the policy denies the file, fails, or answers nothing.</exception>
    private void CheckPolicy(Candidate candidate, string shown, string realPath, PreOpenSnapshot? snapshot)
    {
        var request = new FileAccessRequest
        {
            BaseDirectory = candidate.Origin,
            RequestedPath = candidate.Key,
            RealPath = realPath,
            Snapshot = snapshot,
        };

        // A policy is host code that may fail; a failure, or no answer, is a denial (fail closed). Only a cancellation
        // passes through
        FileAccessDecision? decision;
        Exception? failure = null;
        try
        {
            decision = _policy.Check(request);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            decision = null;
            failure = ex;
        }

        if (decision is not { IsAllowed: true })
        {
            throw Denied(shown, candidate.Key, realPath, decision, failure);
        }
    }

    /// <summary>
    ///     Builds the exception of a policy denial.
    /// </summary>
    /// <param name="shown">The reference as shown in messages.</param>
    /// <param name="key">The full path as written.</param>
    /// <param name="realPath">The real path, or <see langword="null"/> before the open.</param>
    /// <param name="decision">The denial, or <see langword="null"/> when the policy failed or answered nothing.</param>
    /// <param name="failure">The exception of a failed policy, or <see langword="null"/>.</param>
    /// <returns>The exception.</returns>
    private FileReferenceException Denied(
        string shown, string key, string? realPath, FileAccessDecision? decision, Exception? failure) =>
        new(FileReferenceFailureKind.AccessDenied, shown, decision?.Reason ?? "the file-access policy failed", failure)
        {
            RequestedPath = key,
            RealPath = realPath,
            PolicyName = _policy.Name,
            Hint = decision?.Hint,
        };

    /// <summary>
    ///     Turns a remembered outcome into the file or the failure it stands for.
    /// </summary>
    /// <param name="outcome">The outcome.</param>
    /// <param name="shown">The reference as shown in messages.</param>
    /// <param name="key">The key.</param>
    /// <returns>The file.</returns>
    /// <exception cref="FileReferenceException">Thrown when the outcome is a failure.</exception>
    private FileReference Deliver(Outcome outcome, string shown, string key)
    {
        if (outcome.File is { } file)
        {
            return file;
        }

        var kind = outcome.Failure!.Value;
        var reason = kind switch
        {
            FileReferenceFailureKind.RealPathUnavailable => "the real path of the opened file cannot be determined on this system",
            FileReferenceFailureKind.NotARegularFile => "it is not a regular file",
            FileReferenceFailureKind.NetworkPath => "it leads to a network location",
            _ => null,
        };
        throw new FileReferenceException(kind, shown, reason, outcome.Cause)
        {
            RequestedPath = key,
            RealPath = outcome.RealPath,
            PolicyName = kind == FileReferenceFailureKind.RealPathUnavailable ? _policy.Name : null,
        };
    }

    /// <summary>
    ///     What was learned about one key.
    /// </summary>
    /// <param name="File">The file, when it was read.</param>
    /// <param name="Failure">The failure, when it could not be delivered.</param>
    /// <param name="RealPath">The real path of the opened file, when known.</param>
    /// <param name="Cause">The underlying exception of a failure, if any.</param>
    private sealed record Outcome(FileReference? File, FileReferenceFailureKind? Failure, string? RealPath, Exception? Cause)
    {
        /// <summary>
        ///     Creates the outcome of a failure.
        /// </summary>
        /// <param name="kind">The failure kind.</param>
        /// <param name="realPath">The real path, when known.</param>
        /// <param name="cause">The underlying exception, if any.</param>
        /// <returns>The outcome.</returns>
        public static Outcome Failed(FileReferenceFailureKind kind, string? realPath, Exception? cause) =>
            new(null, kind, realPath, cause);
    }
}
