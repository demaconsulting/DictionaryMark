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

namespace DemaConsulting.DictionaryMark.Tests.Files;

/// <summary>
///     Unit tests for the real-path unit (<see cref="RealPath"/> and its native calls): the final path and the type of
///     an open handle on each operating system, the non-blocking open, the real location of a folder, and the path
///     comparison rules. Each operating-system scenario runs on its own system and skips on the others; the CI matrix
///     runs Windows, Linux and macOS.
/// </summary>
public class RealPathTests
{
    /// <summary>
    ///     Validates that a plain file opened for reading is a regular file whose final path is its full path in the
    ///     real location of its folder.
    /// </summary>
    [Fact]
    public void RealPath_Inspect_PlainFile_ReportsItsCanonicalPathAndARegularFile()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var path = directory.Write("a.txt", "a");

        // Act
        using var handle = RealPath.OpenForRead(path);
        var facts = RealPath.Inspect(handle);

        // Assert
        Assert.True(facts.IsRegularFile);
        Assert.NotNull(facts.FinalPath);
        Assert.True(Path.IsPathFullyQualified(facts.FinalPath));
        Assert.True(FileTestSupport.SamePath(Path.Join(FileTestSupport.Canonical(directory.Path), "a.txt"), facts.FinalPath));
    }

    /// <summary>
    ///     Validates that a file opened through a directory link reports the path of the link's target.
    /// </summary>
    [Fact]
    public void RealPath_Inspect_FileThroughADirectoryLink_ReportsTheTarget()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("target/a.txt", "a");
        FileTestSupport.CreateDirectoryLinkOrSkip(directory.Combine("link"), directory.Combine("target"));

        // Act
        using var handle = RealPath.OpenForRead(Path.GetFullPath(directory.Combine("link/a.txt")));
        var facts = RealPath.Inspect(handle);

        // Assert
        Assert.True(FileTestSupport.SamePath(Path.Join(FileTestSupport.Canonical(directory.Combine("target")), "a.txt"), facts.FinalPath!));
    }

    /// <summary>
    ///     Validates on Windows that a file opened by its short (8.3) spelling reports its long spelling, and that short
    ///     names are expanded. Skips where the volume does not create short names.
    /// </summary>
    [Fact]
    public void RealPath_Inspect_ShortNameSpelling_IsExpanded()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.WindowsOnly);
        }

        using var directory = new FileTestDirectory();
        var longFile = directory.Write("a-long-folder-name/a.txt", "a");
        var shortFolder = FileTestSupport.TryGetShortPath(Path.GetDirectoryName(longFile)!);
        if (shortFolder is null || !shortFolder.Contains('~', StringComparison.Ordinal))
        {
            Assert.Skip("The volume does not create short (8.3) names.");
        }

        // Act
        using var handle = RealPath.OpenForRead(Path.Combine(shortFolder, "a.txt"));
        var facts = RealPath.Inspect(handle);
        var expanded = RealPath.ExpandShortNames(Path.Combine(shortFolder, "a.txt"));

        // Assert
        Assert.Contains("a-long-folder-name", facts.FinalPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("a-long-folder-name", expanded, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Validates on macOS that the final path comes back through <c>proc_pidfdinfo</c> in the real location of the
    ///     temporary folder (<c>/var</c> and <c>/tmp</c> are links to <c>/private</c>), which pins the offset of the path
    ///     in the kernel's structure.
    /// </summary>
    [Fact]
    public void RealPath_Inspect_TempFileOnMacOS_ReportsThePrivateLocation()
    {
        // Arrange
        if (!OperatingSystem.IsMacOS())
        {
            Assert.Skip("This scenario exists only on macOS.");
        }

        using var directory = new FileTestDirectory();
        var path = Path.GetFullPath(directory.Write("a.txt", "a"));
        var linked = path.StartsWith("/var/", StringComparison.Ordinal) || path.StartsWith("/tmp/", StringComparison.Ordinal);
        var expected = linked ? "/private" + path : path;

        // Act
        using var handle = RealPath.OpenForRead(path);
        var facts = RealPath.Inspect(handle);

        // Assert
        Assert.True(facts.IsRegularFile);
        Assert.True(FileTestSupport.SamePath(expected, facts.FinalPath!), $"expected '{expected}', got '{facts.FinalPath}'");
    }

    /// <summary>
    ///     Validates on Linux that a file deleted after it was opened has no real path any more (fail closed for a
    ///     restrictive policy), while it is still a regular file.
    /// </summary>
    [Fact]
    public void RealPath_Inspect_DeletedFile_IsUnavailable()
    {
        // Arrange
        if (!OperatingSystem.IsLinux())
        {
            Assert.Skip("This scenario exists only on Linux.");
        }

        using var directory = new FileTestDirectory();
        var path = directory.Write("a.txt", "a");
        using var handle = RealPath.OpenForRead(path);
        File.Delete(path);

        // Act
        var facts = RealPath.Inspect(handle);

        // Assert
        Assert.True(facts.IsRegularFile);
        Assert.Null(facts.FinalPath);
    }

    /// <summary>
    ///     Validates that a live file whose name ends with the text Linux appends to a deleted file's link still reports
    ///     its real path, suffix included: deletion is told from the open file, not from its name.
    /// </summary>
    [Fact]
    public void RealPath_Inspect_LiveFileNamedLikeADeletedOne_ReportsItsRealPath()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var path = directory.Write("x (deleted)", "a");

        // Act
        using var handle = RealPath.OpenForRead(Path.GetFullPath(path));
        var facts = RealPath.Inspect(handle);

        // Assert
        Assert.True(facts.IsRegularFile);
        Assert.NotNull(facts.FinalPath);
        Assert.True(
            FileTestSupport.SamePath(Path.Join(FileTestSupport.Canonical(directory.Path), "x (deleted)"), facts.FinalPath),
            $"got '{facts.FinalPath}'");
    }

    /// <summary>
    ///     Validates that a device and a directory are not regular files: on Linux and macOS both open without blocking
    ///     and are reported as such; on Windows the null device is a character device and a directory cannot be opened as
    ///     a file at all.
    /// </summary>
    [Fact]
    public void RealPath_Inspect_DirectoryAndDevice_AreNotRegularFiles()
    {
        // Arrange
        using var directory = new FileTestDirectory();

        // Act / Assert
        if (OperatingSystem.IsWindows())
        {
            using var device = FileTestSupport.OpenNullDevice();
            Assert.False(RealPath.Inspect(device).IsRegularFile);
            Assert.Throws<UnauthorizedAccessException>(() => RealPath.OpenForRead(directory.Path));
        }
        else
        {
            using var device = RealPath.OpenForRead("/dev/null");
            using var folder = RealPath.OpenForRead(directory.Path);
            Assert.False(RealPath.Inspect(device).IsRegularFile);
            Assert.False(RealPath.Inspect(folder).IsRegularFile);
        }
    }

    /// <summary>
    ///     Validates on Linux and macOS that opening a named pipe without a writer returns at once and the handle is not
    ///     a regular file. The open runs on the test thread with no time limit: an open that blocked would never return,
    ///     which the time limit of the CI job reports.
    /// </summary>
    [Fact]
    public void RealPath_OpenForRead_Fifo_DoesNotBlockAndIsNotRegular()
    {
        // Arrange
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.UnixOnly);
        }

        using var directory = new FileTestDirectory();
        var pipe = directory.Combine("pipe");
        var failure = FileTestSupport.CreateFifo(pipe);
        Assert.True(failure.Length == 0, failure);

        // Act
        using var handle = RealPath.OpenForRead(pipe);
        var info = RealPath.Inspect(handle);

        // Assert
        Assert.False(info.IsRegularFile);
    }

    /// <summary>
    ///     Validates that a missing file and a missing folder fail with the "not found" exceptions a caller treats as a
    ///     missing candidate.
    /// </summary>
    [Fact]
    public void RealPath_OpenForRead_MissingFileAndMissingFolder_MapToNotFoundExceptions()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("file.txt", "f");

        // Act
        var missingFile = Record.Exception(() => RealPath.OpenForRead(Path.GetFullPath(directory.Combine("none.txt"))));
        var missingFolder = Record.Exception(() => RealPath.OpenForRead(Path.GetFullPath(directory.Combine("none/a.txt"))));
        var fileAsFolder = Record.Exception(() => RealPath.OpenForRead(Path.GetFullPath(directory.Combine("file.txt/a.txt"))));

        // Assert
        Assert.IsType<FileNotFoundException>(missingFile);
        Assert.True(missingFolder is FileNotFoundException or DirectoryNotFoundException, missingFolder?.GetType().Name);
        Assert.True(fileAsFolder is FileNotFoundException or DirectoryNotFoundException, fileAsFolder?.GetType().Name);
    }

    /// <summary>
    ///     Validates that the real location of a linked folder is its target, and that a missing folder has none.
    /// </summary>
    [Fact]
    public void RealPath_TryGetCanonicalDirectory_LinkedDirectory_ReturnsTheTarget()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        Directory.CreateDirectory(directory.Combine("target"));
        FileTestSupport.CreateDirectoryLinkOrSkip(directory.Combine("link"), directory.Combine("target"));

        // Act
        var viaLink = RealPath.TryGetCanonicalDirectory(Path.GetFullPath(directory.Combine("link")));
        var direct = RealPath.TryGetCanonicalDirectory(Path.GetFullPath(directory.Combine("target")));
        var missing = RealPath.TryGetCanonicalDirectory(Path.GetFullPath(directory.Combine("missing")));

        // Assert
        Assert.NotNull(viaLink);
        Assert.True(FileTestSupport.SamePath(direct!, viaLink));
        Assert.EndsWith("target", viaLink, StringComparison.OrdinalIgnoreCase);
        Assert.Null(missing);
    }

    /// <summary>
    ///     Validates on Linux and macOS that a folder the process may search but not read (execute-only) still has a
    ///     real location, the same one it has while it is readable. Skips when permissions are not enforced (root).
    /// </summary>
    [Fact]
    public void RealPath_TryGetCanonicalDirectory_ExecuteOnlyDirectory_ReturnsItsLocation()
    {
        // Arrange
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.UnixOnly);
        }

        using var directory = new FileTestDirectory();
        var folder = Path.GetFullPath(directory.Combine("search-only"));
        Directory.CreateDirectory(folder);
        var readable = FileTestSupport.Canonical(folder);
        try
        {
            if (!FileTestSupport.TryMakeExecuteOnly(folder))
            {
                Assert.Skip(FileTestSupport.PermissionsNotEnforced);
            }

            // Act
            var canonical = RealPath.TryGetCanonicalDirectory(folder);

            // Assert
            Assert.NotNull(canonical);
            Assert.True(FileTestSupport.SamePath(readable, canonical), $"expected '{readable}', got '{canonical}'");
        }
        finally
        {
            FileTestSupport.RestoreOwnerAccess(folder);
        }
    }

    /// <summary>
    ///     Validates on Windows that a UNC path is a network location, while a local path and the device namespaces are
    ///     not. A mapped network drive cannot be created on CI and is not covered here.
    /// </summary>
    [Fact]
    public void RealPath_IsNetworkLocation_UncAndLocalPaths_AreClassified()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.WindowsOnly);
        }

        using var directory = new FileTestDirectory();
        var local = Path.GetFullPath(directory.Write("a.txt", "a"));

        // Act / Assert
        Assert.True(RealPath.IsNetworkLocation(@"\\server\share\x"));
        Assert.False(RealPath.IsNetworkLocation(local));
        Assert.False(RealPath.IsNetworkLocation(@"\\?\C:\x"));
        Assert.True(RealPath.IsNetworkLocation(@"\\?\UNC\server\share\x"));
        Assert.False(RealPath.IsNetworkLocation(@"\\?\GLOBALROOT\Device\HarddiskVolume1\x"));
        Assert.False(RealPath.IsNetworkLocation(@"\\.\NUL"));
    }

    /// <summary>
    ///     Validates the comparison rules: a folder contains itself and what lies below its separator but not a sibling
    ///     that shares its name as a prefix, trailing separators do not matter, and the deepest shared folder is found by
    ///     whole components.
    /// </summary>
    [Fact]
    public void RealPath_IsUnder_BoundaryIsTheSeparator()
    {
        // Arrange
        var root = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "real-path-root"));
        var separator = Path.DirectorySeparatorChar;
        var systemRoot = Path.GetPathRoot(root)!;

        // Act / Assert
        Assert.True(RealPath.IsUnder(root, root));
        Assert.True(RealPath.IsUnder(root + separator + "a.txt", root));
        Assert.True(RealPath.IsUnder(root + separator + "a.txt", root + separator));
        Assert.False(RealPath.IsUnder(root + "-sibling" + separator + "a.txt", root));
        Assert.True(RealPath.IsUnder(root, systemRoot));
        Assert.True(RealPath.PathsEqualExactly(root + separator, root));
        Assert.Equal(root, RealPath.CommonAncestor(root + separator + "a" + separator + "x.txt", root + separator + "b"));
        Assert.Equal(Path.GetDirectoryName(root), RealPath.CommonAncestor(root + separator + "a.txt", root + "-sibling" + separator + "b"));
    }

    /// <summary>
    ///     Validates that two paths that differ only in letter case are separate keys of every cache keyed by a path, on
    ///     every platform, so on a case-sensitive folder one file can never reuse the bytes or the captured folder
    ///     location of the other (pure text, no disk).
    /// </summary>
    [Fact]
    public void RealPath_KeyComparer_PathsDifferingOnlyInCase_AreSeparateKeys()
    {
        // Arrange
        var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "real-path-keys"));
        var upper = Path.Combine(parent, "Allowed", "a.txt");
        var lower = Path.Combine(parent, "allowed", "a.txt");
        var cache = new Dictionary<string, string>(RealPath.KeyComparer);

        // Act
        cache[upper] = "upper";
        cache[lower] = "lower";

        // Assert
        Assert.False(RealPath.KeyComparer.Equals(upper, lower));
        Assert.Equal(2, cache.Count);
        Assert.Equal("upper", cache[upper]);
        Assert.Equal("lower", cache[lower]);
    }

    /// <summary>
    ///     Validates that the deepest shared folder is found by components compared exactly on every platform: a folder
    ///     that differs from the other path's folder only in letter case is not shared, so <c>no-symlinks</c> never
    ///     trusts a folder the base directory does not name (pure text, no disk).
    /// </summary>
    [Fact]
    public void RealPath_CommonAncestor_ComponentsDifferingOnlyInCase_AreNotShared()
    {
        // Arrange
        var parent = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "real-path-ancestor"));
        var baseDirectory = Path.Combine(parent, "Allowed");

        // Act
        var otherCase = RealPath.CommonAncestor(Path.Combine(parent, "allowed", "a.txt"), baseDirectory);
        var sameCase = RealPath.CommonAncestor(Path.Combine(parent, "Allowed", "a.txt"), baseDirectory);

        // Assert
        Assert.Equal(parent, otherCase);
        Assert.Equal(baseDirectory, sameCase);
    }

    /// <summary>
    ///     Validates that the exact containment of real paths compares stored-case paths exactly on every platform: a
    ///     folder that differs from the root only in letter case is outside it, the root and what lies below it are
    ///     inside, and a sibling sharing the root's name as a prefix is outside (pure text, no disk).
    /// </summary>
    [Fact]
    public void RealPath_IsUnderExactly_StoredCasePaths_AreComparedExactly()
    {
        // Arrange: stored-case spellings in the platform's own path syntax
        var windows = OperatingSystem.IsWindows();
        var root = windows ? @"C:\work\allowed" : "/work/allowed";
        var inside = windows ? @"C:\work\allowed\x" : "/work/allowed/x";
        var otherCase = windows ? @"C:\work\ALLOWED\x" : "/work/ALLOWED/x";
        var sibling = windows ? @"C:\work\allowed-sibling\x" : "/work/allowed-sibling/x";

        // Act / Assert
        Assert.True(RealPath.IsUnderExactly(inside, root));
        Assert.True(RealPath.IsUnderExactly(root, root));
        Assert.True(RealPath.IsUnderExactly(inside, root + Path.DirectorySeparatorChar));
        Assert.False(RealPath.IsUnderExactly(otherCase, root));
        Assert.False(RealPath.IsUnderExactly(sibling, root));
        Assert.True(RealPath.PathsEqualExactly(root + Path.DirectorySeparatorChar, root));
        Assert.False(RealPath.PathsEqualExactly(root.ToUpperInvariant(), root));
    }

    /// <summary>
    ///     Validates that an existing folder named in another letter case is returned in the case stored on disk where
    ///     the temporary folder is observed to be case-insensitive, and that the name is returned unchanged where it is
    ///     observed to be case-sensitive, because the name as written does not exist there.
    /// </summary>
    [Fact]
    public void RealPath_GetExactPathName_FolderInOtherCase_ReturnsTheStoredCaseWhereItExists()
    {
        // Arrange: a folder stored as "Docs"; whether "docs" names it too is the file system's own answer
        using var directory = new FileTestDirectory();
        Directory.CreateDirectory(directory.Combine("Docs"));
        var caseInsensitive = Directory.Exists(directory.Combine("docs"));
        var expected = Path.Combine(RealPath.GetExactPathName(directory.Path), caseInsensitive ? "Docs" : "docs");

        // Act
        var exact = RealPath.GetExactPathName(directory.Combine("docs"));

        // Assert
        Assert.Equal(expected, exact);
    }

    /// <summary>
    ///     Validates that a missing tail below an existing folder keeps its names as written, while the existing part of
    ///     the path is still brought to the stored case.
    /// </summary>
    [Fact]
    public void RealPath_GetExactPathName_MissingTail_KeepsItsNamesAndNormalizesTheExistingPart()
    {
        // Arrange: "Docs" exists, "Missing/File.TXT" below it does not
        using var directory = new FileTestDirectory();
        Directory.CreateDirectory(directory.Combine("Docs"));
        var existingSpelling = Directory.Exists(directory.Combine("docs")) ? "Docs" : "docs";
        var expected = Path.Combine(RealPath.GetExactPathName(directory.Path), existingSpelling, "Missing", "File.TXT");

        // Act
        var exact = RealPath.GetExactPathName(Path.Combine(directory.Path, "docs", "Missing", "File.TXT"));

        // Assert
        Assert.Equal(expected, exact);
        Assert.EndsWith(Path.Combine("Missing", "File.TXT"), exact, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Validates on Windows that a drive letter written in lower case is returned in upper case, for a drive root
    ///     and for a path on the drive.
    /// </summary>
    [Fact]
    public void RealPath_GetExactPathName_LowerCaseDriveLetter_IsUpperCased()
    {
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.WindowsOnly);
        }

        // Arrange: the temporary folder and its drive root, with the drive letter in lower case
        using var directory = new FileTestDirectory();
        var full = Path.GetFullPath(directory.Path);
        var lower = char.ToLowerInvariant(full[0]) + full[1..];
        var driveRoot = lower[..3];

        // Act
        var exactRoot = RealPath.GetExactPathName(driveRoot);
        var exactPath = RealPath.GetExactPathName(lower);

        // Assert
        Assert.Equal(char.ToUpperInvariant(full[0]) + @":\", exactRoot);
        Assert.Equal(char.ToUpperInvariant(full[0]), exactPath[0]);
        Assert.Equal(RealPath.GetExactPathName(full), exactPath);
    }

    /// <summary>
    ///     Validates on Linux and macOS that a name containing <c>*</c> or <c>?</c> returns the entry with exactly that
    ///     name rather than another entry the name matches as a pattern, and that a missing name with a wildcard is kept
    ///     as written.
    /// </summary>
    [Fact]
    public void RealPath_GetExactPathName_NameWithWildcards_ReturnsTheExactEntry()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.UnixOnly);
        }

        // Arrange: entries a wildcard name also matches as a pattern
        using var directory = new FileTestDirectory();
        directory.Write("a*c", "star");
        directory.Write("abc", "plain");
        directory.Write("a?c", "question");
        directory.Write("xyz", "plain");
        var folder = RealPath.GetExactPathName(directory.Path);

        // Act
        var star = RealPath.GetExactPathName(directory.Combine("a*c"));
        var question = RealPath.GetExactPathName(directory.Combine("a?c"));
        var missing = RealPath.GetExactPathName(directory.Combine("x*"));

        // Assert
        Assert.Equal(Path.Combine(folder, "a*c"), star);
        Assert.Equal(Path.Combine(folder, "a?c"), question);
        Assert.Equal(Path.Combine(folder, "x*"), missing);
    }

    /// <summary>
    ///     Validates on a case-sensitive folder (the Linux default) that, of several entries that differ only in letter
    ///     case, the one named exactly is returned, and that a spelling none of them has is kept as written. Skips where
    ///     the temporary folder is case-insensitive.
    /// </summary>
    [Fact]
    public void RealPath_GetExactPathName_CaseSensitiveFolder_PrefersTheExactEntry()
    {
        // Arrange: "Docs" exists; a second folder "docs" can only be created on a case-sensitive folder
        using var directory = new FileTestDirectory();
        Directory.CreateDirectory(directory.Combine("Docs"));
        if (Directory.Exists(directory.Combine("docs")))
        {
            Assert.Skip("The temporary folder is case-insensitive, so entries cannot differ only in letter case.");
        }

        Directory.CreateDirectory(directory.Combine("docs"));
        var folder = RealPath.GetExactPathName(directory.Path);

        // Act
        var upper = RealPath.GetExactPathName(directory.Combine("Docs"));
        var lower = RealPath.GetExactPathName(directory.Combine("docs"));
        var missing = RealPath.GetExactPathName(directory.Combine("DOCS"));

        // Assert
        Assert.Equal(Path.Combine(folder, "Docs"), upper);
        Assert.Equal(Path.Combine(folder, "docs"), lower);
        Assert.Equal(Path.Combine(folder, "DOCS"), missing);
    }

    /// <summary>
    ///     Validates the classification of extended-length and device-namespace paths (pure text, so it runs on every
    ///     system): only <c>\\?\</c> followed by a drive or a volume GUID is an ordinary extended-length path; every
    ///     device-namespace spelling is recognized as one.
    /// </summary>
    /// <param name="path">The path.</param>
    /// <param name="ordinary">Whether it is an ordinary extended-length path.</param>
    /// <param name="deviceNamespace">Whether it is in a device namespace.</param>
    [Theory]
    [InlineData(@"\\?\C:\docs", true, true)]
    [InlineData(@"\\?\c:\", true, true)]
    [InlineData(@"\\?\Z:", true, true)]
    [InlineData(@"\\?\Volume{67589fd6-ee90-4e7d-9ad7-a17168a66d75}\docs", true, true)]
    [InlineData(@"\\?\volume{67589FD6-EE90-4E7D-9AD7-A17168A66D75}", true, true)]
    [InlineData(@"\\?\GLOBALROOT\Device\HarddiskVolume1\docs", false, true)]
    [InlineData(@"\\?\UNC\server\share", false, true)]
    [InlineData(@"\\?\pipe\x", false, true)]
    [InlineData(@"\\?\Volume{not-a-guid}\docs", false, true)]
    [InlineData(@"\\?\Volume{67589fd6-ee90-4e7d-9ad7-a17168a66d75}x", false, true)]
    [InlineData(@"\\?\C:docs", false, true)]
    [InlineData(@"\\.\C:\docs", false, true)]
    [InlineData(@"\??\C:\docs", false, true)]
    [InlineData("//?/C:/docs", false, true)]
    [InlineData(@"C:\docs", false, false)]
    [InlineData(@"\\server\share", false, false)]
    [InlineData("/home/docs", false, false)]
    public void RealPath_IsOrdinaryExtendedPath_AcceptsOnlyDriveAndVolumeRoots(string path, bool ordinary, bool deviceNamespace)
    {
        // Act
        var isOrdinary = RealPath.IsOrdinaryExtendedPath(path);
        var isDevice = RealPath.IsWindowsDeviceNamespace(path);

        // Assert
        Assert.Equal(ordinary, isOrdinary);
        Assert.Equal(deviceNamespace, isDevice);
    }

    /// <summary>
    ///     Validates that the reserved Windows device names are recognized in every spelling Windows accepts, in a single
    ///     component and anywhere in a path, and that names that merely resemble them are not. The check is pure text, so
    ///     it runs on every system.
    /// </summary>
    /// <param name="component">The path component.</param>
    /// <param name="reserved">Whether it is a reserved device name.</param>
    [Theory]
    [InlineData("CON", true)]
    [InlineData("prn", true)]
    [InlineData("Aux", true)]
    [InlineData("NUL", true)]
    [InlineData("NUL.txt", true)]
    [InlineData("NUL ", true)]
    [InlineData("NUL...", true)]
    [InlineData("NUL .txt", true)]
    [InlineData("NUL:", true)]
    [InlineData("NUL:stream", true)]
    [InlineData("COM0", true)]
    [InlineData("com9", true)]
    [InlineData("LPT1.log", true)]
    [InlineData("COM\u00B9", true)]
    [InlineData("LPT\u00B2", true)]
    [InlineData("lpt\u00B3", true)]
    [InlineData("CONIN$", true)]
    [InlineData("conout$.x", true)]
    [InlineData("NULL", false)]
    [InlineData("CONSOLE", false)]
    [InlineData("COM10", false)]
    [InlineData("COM", false)]
    [InlineData("LPT", false)]
    [InlineData("a.NUL", false)]
    [InlineData(" NUL", false)]
    [InlineData("nul_x", false)]
    [InlineData("CONIN", false)]
    public void RealPath_IsWindowsDeviceName_RecognizesEveryReservedSpelling(string component, bool reserved)
    {
        // Act
        var single = RealPath.IsWindowsDeviceName(component);
        var inPath = RealPath.HasWindowsDeviceName("C:/docs/" + component + "/a.txt");

        // Assert
        Assert.Equal(reserved, single);
        Assert.Equal(reserved, inPath);
    }
}
