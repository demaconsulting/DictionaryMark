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

using System.Text;
using DemaConsulting.DictionaryMark.Files;
using Microsoft.Win32.SafeHandles;

namespace DemaConsulting.DictionaryMark.Tests.Files;

/// <summary>
///     Unit tests for <see cref="FileReferenceResolver"/>: the lookup, the text rules that hold under every policy,
///     missing and non-regular files, the open and bounded-read rules, the cache, and how the policy is asked and how
///     its answer is reported. Links have their own class (<see cref="FileReferenceResolverLinkTests"/>), and so does
///     the race-free check (<see cref="FileReferenceResolverRaceTests"/>). Open failures are provoked through the
///     internal open seams, so no test needs a file permission or a lock.
/// </summary>
public class FileReferenceResolverTests
{
    /// <summary>
    ///     Creates a resolver.
    /// </summary>
    /// <param name="options">The options.</param>
    /// <param name="hooks">The open seams, or <see langword="null"/>.</param>
    /// <returns>The resolver.</returns>
    private static FileReferenceResolver Create(FileReferenceOptions options, FileOpenHooks? hooks = null) =>
        new(options, hooks);

    /// <summary>
    ///     Creates seams that record every full path that is about to be opened.
    /// </summary>
    /// <param name="opened">The list that receives the paths.</param>
    /// <returns>The seams.</returns>
    private static FileOpenHooks Recording(List<string> opened) => new() { BeforeOpen = opened.Add };

    /// <summary>
    ///     Opens a file the way the default opener does, for seams that delegate to the file system.
    /// </summary>
    /// <param name="path">The full path.</param>
    /// <returns>The handle.</returns>
    private static SafeFileHandle OpenShared(string path) =>
        File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);

    /// <summary>
    ///     Decodes the content of a resolved file as UTF-8.
    /// </summary>
    /// <param name="file">The file; must not be null.</param>
    /// <returns>The text.</returns>
    private static string Text(FileReference? file)
    {
        Assert.NotNull(file);
        return Encoding.UTF8.GetString(file.Content.Span);
    }

    /// <summary>
    ///     Validates that a <c>..</c> that leaves the base directory is read under the default policy, like Pandoc.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_DotDotLeavingTheBase_IsReadUnderAllowAll()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        Directory.CreateDirectory(directory.Combine("base"));
        directory.Write("outside/secret.txt", "outside");
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Combine("base") });

        // Act
        var file = resolver.Resolve("../outside/secret.txt");

        // Assert
        Assert.Equal("outside", Text(file));
        Assert.Equal(Path.GetFullPath(directory.Combine("outside/secret.txt")), file!.Key);
    }

    /// <summary>
    ///     Validates that an absolute reference names exactly one candidate: it is read where it points, and a missing
    ///     absolute file is a miss after one attempt only.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_AbsolutePath_IsASingleCandidateAndIsRead()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        Directory.CreateDirectory(directory.Combine("base"));
        directory.Write("elsewhere/abs.txt", "abs");
        var opened = new List<string>();
        var resolver = Create(
            new FileReferenceOptions { BaseDirectory = directory.Combine("base") },
            Recording(opened));
        var absolute = Path.GetFullPath(directory.Combine("elsewhere/abs.txt")).Replace('\\', '/');
        var missing = Path.GetFullPath(directory.Combine("elsewhere/missing.txt"));

        // Act
        var file = resolver.Resolve(absolute);
        var none = resolver.Resolve(missing);

        // Assert: the absolute file is read; the missing one is missing after one attempt only
        Assert.Equal("abs", Text(file));
        Assert.Null(none);
        Assert.Equal([Path.GetFullPath(absolute), missing], opened);
    }

    /// <summary>
    ///     Validates that backslashes and <c>.</c> and <c>..</c> segments are normalized, so every spelling names one file
    ///     (the very same instance).
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_BackslashesAndDotSegments_NameOneFile()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("base/sub/a.txt", "a");
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Combine("base") });

        // Act
        var plain = resolver.Resolve("sub/a.txt");
        var dotted = resolver.Resolve("sub/../sub/./a.txt");
        var backslashes = resolver.Resolve("sub\\a.txt");

        // Assert
        Assert.Same(plain, dotted);
        Assert.Same(plain, backslashes);
    }

    /// <summary>
    ///     Validates that a reference text that is never allowed is refused with the matching kind before any open.
    /// </summary>
    /// <param name="reference">The reference.</param>
    /// <param name="kind">The expected failure kind.</param>
    [Theory]
    [InlineData("", FileReferenceFailureKind.EmptyPath)]
    [InlineData("   ", FileReferenceFailureKind.EmptyPath)]
    [InlineData("a\0b.txt", FileReferenceFailureKind.ControlCharacter)]
    [InlineData("a\nb.txt", FileReferenceFailureKind.ControlCharacter)]
    [InlineData("a\u007Fb.txt", FileReferenceFailureKind.ControlCharacter)]
    [InlineData("a\u0085b.txt", FileReferenceFailureKind.ControlCharacter)]
    [InlineData("a.txt:stream", FileReferenceFailureKind.InvalidPath)]
    [InlineData("C:a.txt", FileReferenceFailureKind.InvalidPath)]
    [InlineData("\\\\server\\share\\a.txt", FileReferenceFailureKind.NetworkPath)]
    [InlineData("//server/share/a.txt", FileReferenceFailureKind.NetworkPath)]
    [InlineData("\\\\?\\C:\\a.txt", FileReferenceFailureKind.InvalidPath)]
    [InlineData("\\\\.\\pipe\\x", FileReferenceFailureKind.InvalidPath)]
    public void FileReferenceResolver_Resolve_HostilePathText_IsRejectedWithoutAnyAccess(string reference, FileReferenceFailureKind kind)
    {
        // Arrange: a file-system resolver that records opens
        using var directory = new FileTestDirectory();
        var opened = new List<string>();
        var onDisk = Create(new FileReferenceOptions { BaseDirectory = directory.Path }, Recording(opened));

        // Act
        var diskError = Assert.Throws<FileReferenceException>(() => onDisk.Resolve(reference));

        // Assert: the rule is named, the reference is printable, and nothing was touched
        Assert.Equal(kind, diskError.Kind);
        Assert.NotNull(diskError.Reason);
        Assert.DoesNotContain(diskError.Reference, char.IsControl);
        Assert.Contains("is not allowed", diskError.Message, StringComparison.Ordinal);
        Assert.Empty(opened);
    }

    /// <summary>
    ///     Validates that a drive path is an absolute path on Windows and an invalid path on Linux and macOS, which have
    ///     no drives.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_DrivePath_IsAbsoluteOnWindowsAndInvalidElsewhere()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var full = directory.Write("drive/a.txt", "drive");
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Path });

        // Act / Assert
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal("drive", Text(resolver.Resolve(full.Replace('\\', '/'))));
        }
        else
        {
            var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve("C:/a.txt"));
            Assert.Equal(FileReferenceFailureKind.InvalidPath, ex.Kind);
        }
    }

    /// <summary>
    ///     Validates that a null reference is an argument error.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_NullPath_ThrowsArgumentNullException()
    {
        // Arrange
        var resolver = Create(new FileReferenceOptions { BaseDirectory = "." });

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => resolver.Resolve(null!));
    }

    /// <summary>
    ///     Validates that a missing file and a dangling link are both a miss: the result is <see langword="null"/>.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_MissingFileAndDanglingLink_ReturnNull()
    {
        // Arrange: a link whose target is removed afterwards
        using var directory = new FileTestDirectory();
        Directory.CreateDirectory(directory.Combine("base"));
        Directory.CreateDirectory(directory.Combine("outside/gone"));
        FileTestSupport.CreateDirectoryLinkOrSkip(directory.Combine("base/dangling"), directory.Combine("outside/gone"));
        Directory.Delete(directory.Combine("outside/gone"));
        var alone = Create(new FileReferenceOptions { BaseDirectory = directory.Combine("base") });

        // Act
        var missing = alone.Resolve("none.txt");
        var danglingAlone = alone.Resolve("dangling/x.txt");

        // Assert
        Assert.Null(missing);
        Assert.Null(danglingAlone);
    }

    /// <summary>
    ///     Validates that a resolver without a base directory finds nothing.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_NoBaseDirectory_ReturnsNull()
    {
        // Arrange
        var resolver = Create(new FileReferenceOptions());

        // Act
        var relative = resolver.Resolve("a.txt");
        var absolute = resolver.Resolve(Path.GetFullPath("a.txt"));

        // Assert
        Assert.Null(relative);
        Assert.Null(absolute);
    }

    /// <summary>
    ///     Validates that a directory where a file is expected is not a regular file, whose message names no host
    ///     directory.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_DirectoryAtFilePath_IsNotARegularFile()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        Directory.CreateDirectory(directory.Combine("folder.txt"));
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Path });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve("folder.txt"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.NotARegularFile, ex.Kind);
        Assert.DoesNotContain(directory.Path, ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Validates on Windows that a device handle (the null device, injected through the open seam) is not a regular
    ///     file and is never read.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_InjectedNulHandle_IsNotARegularFile()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.WindowsOnly);
        }

        using var directory = new FileTestDirectory();
        directory.Write("device.txt", "x");
        var resolver = Create(
            new FileReferenceOptions { BaseDirectory = directory.Path },
            new FileOpenHooks { Open = _ => FileTestSupport.OpenNullDevice() });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve("device.txt"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.NotARegularFile, ex.Kind);
    }

    /// <summary>
    ///     Validates on Linux and macOS that a device reached through an absolute reference is not a regular file.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_DevNull_IsNotARegularFile()
    {
        // Arrange
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.UnixOnly);
        }

        using var directory = new FileTestDirectory();
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Path });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve("/dev/null"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.NotARegularFile, ex.Kind);
    }

    /// <summary>
    ///     Validates on Linux and macOS that a named pipe without a writer is refused as not a regular file instead of
    ///     hanging the open. The resolve runs on the test thread with no time limit: an open that blocked would never
    ///     return, which the time limit of the CI job reports.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_Fifo_IsNotARegularFileWithoutBlocking()
    {
        // Arrange
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.UnixOnly);
        }

        using var directory = new FileTestDirectory();
        var failure = FileTestSupport.CreateFifo(directory.Combine("pipe"));
        Assert.True(failure.Length == 0, failure);
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Path });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve("pipe"));

        // Assert: the resolve returned (it did not block on the pipe) and refused it
        Assert.Equal(FileReferenceFailureKind.NotARegularFile, ex.Kind);
    }

    /// <summary>
    ///     Validates that an existing file whose open fails is a <c>CannotRead</c> failure.
    /// </summary>
    /// <param name="denied">Whether the open fails with an access failure (otherwise an I/O failure).</param>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FileReferenceResolver_Resolve_OpenFailure_IsCannotRead(bool denied)
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("base/a.txt", "from base");
        var opened = new List<string>();
        var resolver = Create(
            new FileReferenceOptions { BaseDirectory = directory.Combine("base") },
            new FileOpenHooks
            {
                Open = path =>
                {
                    opened.Add(path);
                    throw denied ? new UnauthorizedAccessException("denied") : new IOException("locked");
                },
            });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve("a.txt"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.CannotRead, ex.Kind);
        Assert.NotNull(ex.InnerException);
        Assert.Equal([Path.GetFullPath(directory.Combine("base/a.txt"))], opened);
    }

    /// <summary>
    ///     Validates that "not found" from the open itself is a miss.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_FileNotFoundFromOpen_ReturnsNull()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("base/a.txt", "from base");
        var first = Path.GetFullPath(directory.Combine("base/a.txt"));
        var resolver = Create(
            new FileReferenceOptions { BaseDirectory = directory.Combine("base") },
            new FileOpenHooks { Open = path => path == first ? throw new FileNotFoundException("gone") : OpenShared(path) });

        // Act
        var file = resolver.Resolve("a.txt");

        // Assert
        Assert.Null(file);
    }

    /// <summary>
    ///     Validates that a file held open by a writer with permissive sharing is still read.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_FileHeldOpenByWriter_IsStillRead()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var path = directory.Write("a.txt", "held");
        using var writer = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Path });

        // Act
        var file = resolver.Resolve("a.txt");

        // Assert
        Assert.Equal("held", Text(file));
    }

    /// <summary>
    ///     Validates the byte limit at its boundary.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_MaxFileBytesAtLimitAndOneOver()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("ten.txt", "0123456789");
        directory.Write("eleven.txt", "0123456789A");
        var onDisk = Create(new FileReferenceOptions { BaseDirectory = directory.Path, MaxFileBytes = 10 });

        // Act / Assert
        Assert.Equal("0123456789", Text(onDisk.Resolve("ten.txt")));
        Assert.Equal(FileReferenceFailureKind.TooLarge, Assert.Throws<FileReferenceException>(() => onDisk.Resolve("eleven.txt")).Kind);
    }

    /// <summary>
    ///     Validates that a stream that never ends stops one chunk past the byte limit.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_ReadBounded_EndlessStream_StopsAtTheBound()
    {
        // Arrange
        var endless = new EndlessStream();

        // Act
        var bytes = FileReferenceResolver.ReadBounded(endless, 20_000);

        // Assert
        Assert.Null(bytes);
        Assert.InRange(endless.BytesRead, 20_000, 20_000 + 8192);
    }

    /// <summary>
    ///     Validates that one resolver opens a file and a missing file once, returns the same instance for the same file,
    ///     and does not read a file again after it changed.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_SameFileTwice_IsOpenedOnceAndReturnsTheSameInstance()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var path = directory.Write("a.txt", "first");
        var opened = new List<string>();
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Path }, Recording(opened));

        // Act
        var first = resolver.Resolve("a.txt");
        File.WriteAllText(path, "second");
        var again = resolver.Resolve("./a.txt");
        resolver.Resolve("none.txt");
        resolver.Resolve("none.txt");

        // Assert
        Assert.Same(first, again);
        Assert.Equal("first", Text(again));
        Assert.Equal([Path.GetFullPath(path), Path.GetFullPath(directory.Combine("none.txt"))], opened);
    }

    /// <summary>
    ///     Validates that two spellings of a path that differ only in letter case are separate cache entries on every
    ///     platform: each is opened and judged on its own, and the second never reuses the outcome of the first. On a
    ///     case-insensitive folder both name the same file (an extra entry); on a case-sensitive folder the second does
    ///     not exist.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_SpellingsDifferingOnlyInCase_AreSeparateCacheEntries()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("a.txt", "a");
        var opened = new List<string>();
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Path }, Recording(opened));

        // Act
        var lower = resolver.Resolve("a.txt");
        var upper = resolver.Resolve("A.txt");
        var lowerAgain = resolver.Resolve("a.txt");

        // Assert: both spellings reached the opener once each, and neither answered for the other
        Assert.Equal([Path.GetFullPath(directory.Combine("a.txt")), Path.GetFullPath(directory.Combine("A.txt"))], opened);
        Assert.NotSame(lower, upper);
        Assert.Same(lower, lowerAgain);
    }

    /// <summary>
    ///     Validates that two files whose folders differ only in letter case are each read with their own bytes, under a
    ///     policy that captures folder locations. Skips where the temporary folder is observed to be case-insensitive,
    ///     because the two files cannot both exist there; no file-system setting is changed.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_FilesDifferingOnlyInCase_AreReadSeparatelyWhereTheFolderIsCaseSensitive()
    {
        // Arrange: "Allowed/a.txt" exists; whether "allowed" names it too is the file system's own answer
        using var directory = new FileTestDirectory();
        directory.Write("Allowed/a.txt", "upper");
        if (Directory.Exists(directory.Combine("allowed")))
        {
            Assert.Skip("The temporary folder is observed to be case-insensitive.");
        }

        directory.Write("allowed/a.txt", "lower");
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Path, FileAccess = FileAccessPolicy.NoSymlinks });

        // Act
        var upper = resolver.Resolve("Allowed/a.txt");
        var lower = resolver.Resolve("allowed/a.txt");

        // Assert
        Assert.Equal("upper", Text(upper));
        Assert.Equal("lower", Text(lower));
    }

    /// <summary>
    ///     Validates that trusted folders that differ only in letter case are captured separately before the open on
    ///     every platform: each is asked for its own location, and the policy sees each with its own location.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_AnchorsDifferingOnlyInCase_AreCapturedSeparately()
    {
        // Arrange: a policy that names two anchors that differ only in letter case
        using var directory = new FileTestDirectory();
        directory.Write("a.txt", "a");
        var upperAnchor = Path.GetFullPath(directory.Combine("Allowed"));
        var lowerAnchor = Path.GetFullPath(directory.Combine("allowed"));
        var policy = new AnchoredPolicy([upperAnchor, lowerAnchor]);
        var asked = new List<string>();
        var hooks = new FileOpenHooks
        {
            DirectoryLocationOf = anchor =>
            {
                asked.Add(anchor);
                return "location of " + anchor;
            },
        };
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Path, FileAccess = policy }, hooks);

        // Act
        resolver.Resolve("a.txt");

        // Assert
        Assert.Equal([upperAnchor, lowerAnchor], asked);
        var snapshot = Assert.Single(policy.Requests).Snapshot;
        Assert.NotNull(snapshot);
        Assert.Equal(2, snapshot.AnchorLocations.Count);
        Assert.Equal("location of " + upperAnchor, snapshot.AnchorLocations[upperAnchor]);
        Assert.Equal("location of " + lowerAnchor, snapshot.AnchorLocations[lowerAnchor]);
    }

    /// <summary>
    ///     Validates what the policy is asked (base directory - the host-chosen directory the lookup started from -
    ///     requested and real path) and that a denial is reported with the reason, the hint, the policy name and the
    ///     paths, without a host directory in its message.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_PolicyDenial_IsAccessDeniedWithFactsAndNothingRead()
    {
        // Arrange: the policy denies every file
        using var directory = new FileTestDirectory();
        Directory.CreateDirectory(directory.Combine("base"));
        directory.Write("x.txt", "x");
        var policy = new RecordingPolicy(_ => FileAccessDecision.Deny("not this one", "try another"));
        var resolver = Create(new FileReferenceOptions
        {
            BaseDirectory = directory.Combine("base"),
            FileAccess = policy,
        });

        // Act
        FileReference? file = null;
        var ex = Assert.Throws<FileReferenceException>(() => file = resolver.Resolve("../x.txt"));

        // Assert: the request names the base directory, the requested path and the real path
        var request = Assert.Single(policy.Requests);
        Assert.Equal(Path.GetFullPath(directory.Combine("base")), request.BaseDirectory);
        Assert.Equal(Path.GetFullPath(directory.Combine("x.txt")), request.RequestedPath);
        Assert.True(FileTestSupport.SamePath(Path.Join(FileTestSupport.Canonical(directory.Path), "x.txt"), request.RealPath));

        // Assert: the denial carries the facts, and its message names the policy but no host directory
        Assert.Null(file);
        Assert.Equal(FileReferenceFailureKind.AccessDenied, ex.Kind);
        Assert.Equal("../x.txt", ex.Reference);
        Assert.Equal("not this one", ex.Reason);
        Assert.Equal("try another", ex.Hint);
        Assert.Equal("recording", ex.PolicyName);
        Assert.Equal(request.RequestedPath, ex.RequestedPath);
        Assert.Equal(request.RealPath, ex.RealPath);
        Assert.Contains("'recording'", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(directory.Path, ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("not this one", ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Validates that a policy that throws or answers nothing denies the file (fail closed), while a cancellation it
    ///     raises passes through.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_ThrowingOrNullPolicy_IsAccessDeniedAndCancellationPassesThrough()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("a.txt", "a");
        FileReferenceResolver With(FileAccessPolicy policy) =>
            Create(new FileReferenceOptions { BaseDirectory = directory.Path, FileAccess = policy });
        var throwing = With(new RecordingPolicy(_ => throw new InvalidOperationException("boom"), "boom"));
        var silent = With(new RecordingPolicy(_ => null!, "silent"));
        var canceling = With(new RecordingPolicy(_ => throw new OperationCanceledException(), "cancel"));

        // Act
        var thrown = Assert.Throws<FileReferenceException>(() => throwing.Resolve("a.txt"));
        var none = Assert.Throws<FileReferenceException>(() => silent.Resolve("a.txt"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.AccessDenied, thrown.Kind);
        Assert.Equal("the file-access policy failed", thrown.Reason);
        Assert.IsType<InvalidOperationException>(thrown.InnerException);
        Assert.Equal(FileReferenceFailureKind.AccessDenied, none.Kind);
        Assert.Throws<OperationCanceledException>(() => canceling.Resolve("a.txt"));
    }

    /// <summary>
    ///     Validates that a cached file is read once but the policy is asked on every resolve, so a later denial is
    ///     honored.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_CachedFile_IsReCheckedOnEveryResolveButReadOnce()
    {
        // Arrange: a policy that allows the first two requests and denies the third
        using var directory = new FileTestDirectory();
        directory.Write("a.txt", "a");
        var opened = new List<string>();
        var asked = 0;
        var policy = new RecordingPolicy(_ => ++asked <= 2 ? FileAccessDecision.Allow : FileAccessDecision.Deny("changed its mind"));
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Path, FileAccess = policy }, Recording(opened));

        // Act
        var first = resolver.Resolve("a.txt");
        var second = resolver.Resolve("a.txt");
        var third = Assert.Throws<FileReferenceException>(() => resolver.Resolve("a.txt"));

        // Assert
        Assert.Same(first, second);
        Assert.Equal(FileReferenceFailureKind.AccessDenied, third.Kind);
        Assert.Equal(3, policy.Requests.Count);
        Assert.Single(opened);
    }

    /// <summary>
    ///     Validates that a roots policy refuses a path as written outside its roots before the file is opened.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_UnderRootsPreCheck_DeniesBeforeTheOpen()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("base/a.txt", "a");
        directory.Write("outside/x.txt", "x");
        var opened = new List<string>();
        var resolver = Create(
            new FileReferenceOptions
            {
                BaseDirectory = directory.Combine("base"),
                FileAccess = FileAccessPolicy.UnderRoots(directory.Combine("base")),
            },
            Recording(opened));

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve("../outside/x.txt"));
        var inside = resolver.Resolve("a.txt");

        // Assert
        Assert.Equal(FileReferenceFailureKind.AccessDenied, ex.Kind);
        Assert.Equal("roots", ex.PolicyName);
        Assert.Null(ex.RealPath);
        Assert.Equal("a", Text(inside));
        Assert.Equal([Path.GetFullPath(directory.Combine("base/a.txt"))], opened);
    }

    /// <summary>
    ///     Validates that a restrictive policy fails closed when the real path is unavailable, while the default policy
    ///     still reads the file.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_RestrictivePolicyWithoutRealPath_IsRealPathUnavailable()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("a.txt", "a");
        var unavailable = new FileOpenHooks { RealPathOf = _ => null };
        var restrictive = Create(
            new FileReferenceOptions { BaseDirectory = directory.Path, FileAccess = FileAccessPolicy.NoSymlinks },
            unavailable);
        var permissive = Create(new FileReferenceOptions { BaseDirectory = directory.Path }, unavailable);

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => restrictive.Resolve("a.txt"));
        var file = permissive.Resolve("a.txt");

        // Assert
        Assert.Equal(FileReferenceFailureKind.RealPathUnavailable, ex.Kind);
        Assert.Equal("no-symlinks", ex.PolicyName);
        Assert.DoesNotContain(directory.Path, ex.Message, StringComparison.Ordinal);
        Assert.Equal("a", Text(file));
        Assert.Null(file!.RealPath);
    }

    /// <summary>
    ///     Validates that a handle whose type the platform cannot tell is refused as not a regular file under every
    ///     policy, the default included (fail closed), and that nothing is read.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_UnknownFileType_IsNotARegularFileUnderEveryPolicy()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("a.txt", "a");
        var unknown = new FileOpenHooks { IsRegularFileOf = _ => null };
        var permissive = Create(new FileReferenceOptions { BaseDirectory = directory.Path }, unknown);
        var restrictive = Create(
            new FileReferenceOptions { BaseDirectory = directory.Path, FileAccess = FileAccessPolicy.UnderRoots(directory.Path) },
            unknown);

        // Act
        var permissiveError = Assert.Throws<FileReferenceException>(() => permissive.Resolve("a.txt"));
        var restrictiveError = Assert.Throws<FileReferenceException>(() => restrictive.Resolve("a.txt"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.NotARegularFile, permissiveError.Kind);
        Assert.Equal(FileReferenceFailureKind.NotARegularFile, restrictiveError.Kind);
        Assert.DoesNotContain(directory.Path, permissiveError.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Validates on Windows that a local path whose opened file lies on a network share (a link to a UNC target,
    ///     injected through the real-path seam) is refused as a network path under every policy, without a host
    ///     directory in its message, and that the refusal is remembered without opening the file again.
    /// </summary>
    /// <param name="policyName">The policy: <c>any</c>, <c>no-symlinks</c> or <c>roots</c>.</param>
    [Theory]
    [InlineData("any")]
    [InlineData("no-symlinks")]
    [InlineData("roots")]
    public void FileReferenceResolver_Resolve_LocalPathWhoseRealPathIsANetworkShare_IsNetworkPathUnderEveryPolicy(string policyName)
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.WindowsOnly);
        }

        using var directory = new FileTestDirectory();
        directory.Write("a.txt", "a");
        const string share = @"\\server\share\a.txt";
        var policy = policyName switch
        {
            "no-symlinks" => FileAccessPolicy.NoSymlinks,
            "roots" => FileAccessPolicy.UnderRoots(directory.Path),
            _ => FileAccessPolicy.AllowAll,
        };
        var opened = new List<string>();
        var resolver = Create(
            new FileReferenceOptions { BaseDirectory = directory.Path, FileAccess = policy },
            new FileOpenHooks { BeforeOpen = opened.Add, RealPathOf = _ => share });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve("a.txt"));
        var again = Assert.Throws<FileReferenceException>(() => resolver.Resolve("a.txt"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.NetworkPath, ex.Kind);
        Assert.Equal(share, ex.RealPath);
        Assert.Equal("it leads to a network location", ex.Reason);
        Assert.DoesNotContain(directory.Path, ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(FileReferenceFailureKind.NetworkPath, again.Kind);
        Assert.Single(opened);
    }

    /// <summary>
    ///     Validates on Windows that a reference with a reserved device name in any component (in any letter case, with
    ///     an extension, trailing spaces or dots, the superscript-digit forms, and the console names) is refused as an
    ///     invalid path before anything is opened, whether or not this Windows version rewrites the path to a device.
    /// </summary>
    /// <param name="reference">The reference.</param>
    [Theory]
    [InlineData("x/NUL")]
    [InlineData("NUL")]
    [InlineData("x/NUL ")]
    [InlineData("x/nul.txt")]
    [InlineData("x/Nul .txt")]
    [InlineData("x/AUX.tar.gz")]
    [InlineData("x/PRN.")]
    [InlineData("x/CON")]
    [InlineData("x/COM1")]
    [InlineData("x/lpt9")]
    [InlineData("x/COM\u00B9")]
    [InlineData("x/LPT\u00B3.txt")]
    [InlineData("x/CONIN$")]
    [InlineData("x/conout$")]
    [InlineData("NUL/a.txt")]
    public void FileReferenceResolver_Resolve_LegacyDeviceName_IsInvalidPathAndNoDeviceIsOpened(string reference)
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.WindowsOnly);
        }

        using var directory = new FileTestDirectory();
        var opened = new List<string>();
        var resolver = Create(new FileReferenceOptions { BaseDirectory = directory.Path }, Recording(opened));

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve(reference));

        // Assert: refused before the open, so no path at all reached the opener
        Assert.Equal(FileReferenceFailureKind.InvalidPath, ex.Kind);
        Assert.Equal("the path names a device", ex.Reason);
        Assert.Empty(opened);
    }

    /// <summary>
    ///     Validates on Windows that with an extended-length base directory (<c>\\?\</c>) an absolute reference that the
    ///     system maps to a device (<c>C:/x/NUL</c> gives <c>\\.\NUL</c>) is still refused as an invalid path with
    ///     nothing opened, while a file under the extended-length base directory is still found.
    /// </summary>
    /// <param name="reference">The absolute reference.</param>
    [Theory]
    [InlineData("C:/x/NUL")]
    [InlineData("C:/x/nul ")]
    public void FileReferenceResolver_Resolve_LegacyDeviceNameWithExtendedLengthBaseDirectory_IsInvalidPathAndNoDeviceIsOpened(string reference)
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.WindowsOnly);
        }

        using var directory = new FileTestDirectory();
        directory.Write("a.txt", "a");
        var extended = @"\\?\" + directory.Path;
        var opened = new List<string>();
        var resolver = Create(new FileReferenceOptions { BaseDirectory = extended }, Recording(opened));

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve(reference));
        var inside = resolver.Resolve("a.txt");
        var reservedUnderExtended = Record.Exception(() => resolver.Resolve("NUL.txt"));

        // Assert: the device is refused before the open; the extended-length base directory keeps working, and a
        // reserved name under it is an ordinary (here missing) file, because \\?\ paths map no device names
        Assert.Equal(FileReferenceFailureKind.InvalidPath, ex.Kind);
        Assert.Equal("the path names a device", ex.Reason);
        Assert.NotNull(inside);
        Assert.Equal("a"u8.ToArray(), inside.Content.ToArray());
        Assert.Null(reservedUnderExtended);
        Assert.Equal(2, opened.Count);
        Assert.All(opened, path => Assert.StartsWith(@"\\?\", path, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Validates on Windows that a base directory in a device namespace other than an ordinary extended-length drive
    ///     or volume root (<c>\\?\GLOBALROOT\</c>, <c>\\?\UNC\</c>, <c>\\?\pipe\</c>, <c>\\.\</c>) gives no exemption: a
    ///     relative reference under it is refused as a device path before anything is opened.
    /// </summary>
    /// <param name="baseDirectory">The base directory.</param>
    [Theory]
    [InlineData(@"\\?\GLOBALROOT\Device\HarddiskVolume1\docs")]
    [InlineData(@"\\?\UNC\server\share\docs")]
    [InlineData(@"\\?\pipe\docs")]
    [InlineData(@"\\.\C:\docs")]
    public void FileReferenceResolver_Resolve_DeviceNamespaceBaseDirectory_IsInvalidPathAndNothingIsOpened(string baseDirectory)
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.WindowsOnly);
        }

        var opened = new List<string>();
        var resolver = Create(new FileReferenceOptions { BaseDirectory = baseDirectory }, Recording(opened));

        // Act
        var plain = Assert.Throws<FileReferenceException>(() => resolver.Resolve("a.txt"));
        var climbing = Assert.Throws<FileReferenceException>(() => resolver.Resolve("../../HarddiskVolume2/a.txt"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.InvalidPath, plain.Kind);
        Assert.Equal("the path names a device", plain.Reason);
        Assert.Equal(FileReferenceFailureKind.InvalidPath, climbing.Kind);
        Assert.Empty(opened);
    }

    /// <summary>
    ///     Validates on Windows that a base directory given as an ordinary extended-length volume path
    ///     (<c>\\?\Volume{GUID}\...</c>) keeps working: a file under it is read. Skips when the volume GUID path of the
    ///     temporary folder's drive cannot be determined.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_VolumeGuidBaseDirectory_ReadsTheFile()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.WindowsOnly);
        }

        using var directory = new FileTestDirectory();
        directory.Write("a.txt", "a");
        var full = Path.GetFullPath(directory.Path);
        var volume = FileTestSupport.TryGetVolumeGuidPath(full);
        if (volume is null)
        {
            Assert.Skip("The volume GUID path of the temporary folder's drive cannot be determined.");
        }

        var resolver = Create(new FileReferenceOptions { BaseDirectory = volume + full[Path.GetPathRoot(full)!.Length..] });

        // Act
        var file = resolver.Resolve("a.txt");

        // Assert
        Assert.Equal("a", Text(file));
    }

    /// <summary>
    ///     Validates that the message of a denied absolute reference echoes the reference exactly as written, which the
    ///     author supplied, but never adds the real path of the opened file or the base directory; those stay in the
    ///     properties for trusted callers.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_AbsoluteReferenceDenied_MessageEchoesItButNotTheRealPathOrBase()
    {
        // Arrange: the file is reached by an absolute reference, and its real path is reported somewhere else entirely
        using var directory = new FileTestDirectory();
        var file = directory.Write("files/a.txt", "a");
        var baseDirectory = Path.GetFullPath(directory.Combine("base-folder"));
        Directory.CreateDirectory(baseDirectory);
        var reference = Path.GetFullPath(file).Replace('\\', '/');
        var real = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "real-secret-location", "x.txt"));
        var resolver = Create(
            new FileReferenceOptions { BaseDirectory = baseDirectory, FileAccess = FileAccessPolicy.UnderRoots(directory.Path) },
            new FileOpenHooks { RealPathOf = _ => real });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve(reference));

        // Assert
        Assert.Equal(FileReferenceFailureKind.AccessDenied, ex.Kind);
        Assert.Contains(reference, ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("real-secret-location", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(baseDirectory, ex.Message, StringComparison.Ordinal);
        Assert.Equal(real, ex.RealPath);
    }

    /// <summary>
    ///     Validates that a base directory that is not a valid path is reported on first use.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_InvalidBaseDirectory_IsInvalidBaseDirectory()
    {
        // Arrange
        var invalidBase = Create(new FileReferenceOptions { BaseDirectory = "bad\0dir" });

        // Act
        var baseError = Assert.Throws<FileReferenceException>(() => invalidBase.Resolve("a.txt"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.InvalidBaseDirectory, baseError.Kind);
        Assert.Equal("bad?dir", baseError.Reference);
    }

    /// <summary>
    ///     Validates that invalid options are rejected when the resolver is created.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Constructor_InvalidArguments_Throw()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new FileReferenceResolver(null!));
        Assert.Throws<ArgumentNullException>(() => new FileReferenceResolver(new FileReferenceOptions { FileAccess = null! }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileReferenceResolver(new FileReferenceOptions { MaxFileBytes = 0 }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FileReferenceResolver(new FileReferenceOptions { MaxFileBytes = -1 }));
    }
}
