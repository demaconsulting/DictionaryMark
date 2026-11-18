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

using DemaConsulting.DictionaryMark.Tests.Helpers;
using DemaConsulting.DictionaryMark.Utilities;

namespace DemaConsulting.DictionaryMark.Tests.Utilities;

/// <summary>
///     Unit tests for the RealPath unit, which holds all platform-specific (native) code of the tool.
/// </summary>
public class RealPathTests
{
    /// <summary>Test that a plain file opened for reading is a regular file whose real path is its own path.</summary>
    [Fact]
    public void RealPath_Inspect_PlainFile_ReportsRealPathAndRegular()
    {
        // Arrange: a file and the real location of its folder
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("a.yaml");
        File.WriteAllText(file, "x");
        var expected = Path.Join(RealPath.TryGetCanonicalDirectory(tmpDir.DirectoryPath), "a.yaml");

        // Act
        HandleFacts facts;
        using (var handle = RealPath.OpenForRead(file))
        {
            facts = RealPath.Inspect(handle);
        }

        // Assert
        Assert.True(facts.IsRegularFile);
        Assert.NotNull(facts.FinalPath);
        Assert.True(RealPath.RealPathsEqual(expected, facts.FinalPath), $"Expected '{expected}', got '{facts.FinalPath}'.");
    }

    /// <summary>Test that the real path of a file opened through a folder link names the link's target.</summary>
    [Fact]
    public void RealPath_Inspect_FileThroughDirectoryLink_ReportsTarget()
    {
        // Arrange: link -> target, with a file in the target
        using var tmpDir = new TemporaryDirectory();
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        File.WriteAllText(Path.Combine(target, "a.yaml"), "x");
        var link = Path.Combine(tmpDir.DirectoryPath, "link");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);
        var expected = Path.Join(RealPath.TryGetCanonicalDirectory(target), "a.yaml");

        // Act
        HandleFacts facts;
        using (var handle = RealPath.OpenForRead(Path.Combine(link, "a.yaml")))
        {
            facts = RealPath.Inspect(handle);
        }

        // Assert
        Assert.True(facts.IsRegularFile);
        Assert.True(RealPath.RealPathsEqual(expected, facts.FinalPath!), $"Expected '{expected}', got '{facts.FinalPath}'.");
    }

    /// <summary>Test that on macOS the real path of a temporary file names /private, where /var really lives.</summary>
    [Fact]
    public void RealPath_Inspect_TempFile_ReportsPrivateLocation()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "The /var link of macOS is tested on macOS only.");

        // Arrange: a file in the system temporary folder (below /var/folders)
        using var tmpDir = new TemporaryDirectory(Path.GetTempPath());
        var file = tmpDir.GetFilePath("a.yaml");
        File.WriteAllText(file, "x");

        // Act
        using var handle = RealPath.OpenForRead(file);
        var facts = RealPath.Inspect(handle);

        // Assert
        Assert.True(facts.IsRegularFile);
        Assert.StartsWith("/private/", facts.FinalPath);
    }

    /// <summary>Test that /dev/null is reported as not a regular file on Linux and macOS.</summary>
    [Fact]
    public void RealPath_Inspect_DevNull_IsNotRegular()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(), "/dev/null is tested on Linux and macOS only.");

        // Act
        using var handle = RealPath.OpenForRead("/dev/null");
        var facts = RealPath.Inspect(handle);

        // Assert
        Assert.False(facts.IsRegularFile);
    }

    /// <summary>Test that opening a named pipe with no writer does not block and reports a special file.</summary>
    [Fact]
    public async Task RealPath_OpenForRead_Fifo_DoesNotBlockAndIsNotRegular()
    {
        // Arrange: a named pipe (skipped where pipes cannot be created)
        using var tmpDir = new TemporaryDirectory();
        var fifo = Path.Combine(tmpDir.DirectoryPath, "pipe");
        LinkSupport.CreateFifoOrSkip(fifo);

        // Act: open and inspect on a worker, which must finish promptly (a blocked open times out)
        var facts = await Task.Run(
            () =>
            {
                using var handle = RealPath.OpenForRead(fifo);
                return RealPath.Inspect(handle);
            },
            TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(facts.IsRegularFile);
    }

    /// <summary>Test that the Windows NUL device is reported as not a regular file.</summary>
    [Fact]
    public void RealPath_Inspect_NulDevice_IsNotRegular()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The NUL device is tested on Windows only.");

        // Act
        using var handle = File.OpenHandle(@"\\.\NUL", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var facts = RealPath.Inspect(handle);

        // Assert
        Assert.False(facts.IsRegularFile);
        Assert.Null(facts.FinalPath);
    }

    /// <summary>
    ///     Test that a handle the platform cannot ask (here a closed one) leaves both facts unknown rather than
    ///     reporting it as not a regular file.
    /// </summary>
    [Fact]
    public void RealPath_Inspect_ClosedHandle_ReportsUnknownFacts()
    {
        // Arrange: a handle to a regular file, closed before it is inspected
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("a.yaml");
        File.WriteAllText(file, "A: a\n");
        var handle = File.OpenHandle(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        handle.Dispose();

        // Act
        var facts = RealPath.Inspect(handle);

        // Assert: unknown, never classified
        Assert.Null(facts.IsRegularFile);
        Assert.Null(facts.FinalPath);
    }

    /// <summary>Test that the real location of a linked folder is its target.</summary>
    [Fact]
    public void RealPath_TryGetCanonicalDirectory_LinkedDirectory_ReturnsTarget()
    {
        // Arrange: link -> target
        using var tmpDir = new TemporaryDirectory();
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        var link = Path.Combine(tmpDir.DirectoryPath, "link");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);

        // Act
        var canonicalLink = RealPath.TryGetCanonicalDirectory(link);
        var canonicalTarget = RealPath.TryGetCanonicalDirectory(target);

        // Assert
        Assert.NotNull(canonicalLink);
        Assert.NotNull(canonicalTarget);
        Assert.True(RealPath.RealPathsEqual(canonicalTarget, canonicalLink));
        Assert.EndsWith("target", canonicalLink.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Test that a folder that does not exist has no real location.</summary>
    [Fact]
    public void RealPath_TryGetCanonicalDirectory_MissingDirectory_ReturnsNull()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();

        // Act
        var canonical = RealPath.TryGetCanonicalDirectory(Path.Combine(tmpDir.DirectoryPath, "missing"));

        // Assert
        Assert.Null(canonical);
    }

    /// <summary>
    ///     Test that a regular file has no real location as a folder, on every platform, so it can never be the real
    ///     location of an allowed folder or anchor.
    /// </summary>
    [Fact]
    public void RealPath_TryGetCanonicalDirectory_File_ReturnsNull()
    {
        // Arrange: a regular file
        using var tmpDir = new TemporaryDirectory();
        var file = Path.Combine(tmpDir.DirectoryPath, "a.yaml");
        File.WriteAllText(file, "Alpha: First\n");

        // Act
        var canonical = RealPath.TryGetCanonicalDirectory(file);

        // Assert
        Assert.Null(canonical);
    }

    /// <summary>
    ///     Test that a file opened by a path in another letter case than stored reports its real path in the stored
    ///     letter case, on a file system that ignores case (Windows and macOS by default).
    /// </summary>
    [Fact]
    public void RealPath_Inspect_DifferentlyCasedPath_ReportsStoredCase()
    {
        // Arrange: Glossary/Terms.yaml, opened as glossary/terms.yaml where letter case is ignored
        using var tmpDir = new TemporaryDirectory();
        Assert.SkipWhen(
            CaseSensitiveFolder.IsCaseSensitive(tmpDir.DirectoryPath),
            "The temporary folder distinguishes letter case, so a differently cased path names nothing.");
        var folder = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "Glossary")).FullName;
        File.WriteAllText(Path.Combine(folder, "Terms.yaml"), "x");

        // Act
        HandleFacts facts;
        using (var handle = RealPath.OpenForRead(Path.Combine(tmpDir.DirectoryPath, "glossary", "terms.yaml")))
        {
            facts = RealPath.Inspect(handle);
        }

        // Assert: the stored names, compared exactly
        Assert.NotNull(facts.FinalPath);
        Assert.EndsWith(Path.Combine("Glossary", "Terms.yaml"), facts.FinalPath, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Test that the real location of a folder named in another letter case than stored is reported in the
    ///     stored letter case, on a file system that ignores case (Windows and macOS by default).
    /// </summary>
    [Fact]
    public void RealPath_TryGetCanonicalDirectory_DifferentlyCasedPath_ReportsStoredCase()
    {
        // Arrange: Glossary, named as glossary where letter case is ignored
        using var tmpDir = new TemporaryDirectory();
        Assert.SkipWhen(
            CaseSensitiveFolder.IsCaseSensitive(tmpDir.DirectoryPath),
            "The temporary folder distinguishes letter case, so a differently cased path names nothing.");
        var folder = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "Glossary")).FullName;

        // Act
        var lower = RealPath.TryGetCanonicalDirectory(Path.Combine(tmpDir.DirectoryPath, "glossary"));
        var stored = RealPath.TryGetCanonicalDirectory(folder);

        // Assert: both spellings report the stored name, compared exactly
        Assert.NotNull(lower);
        Assert.NotNull(stored);
        Assert.EndsWith("Glossary", lower.TrimEnd(Path.DirectorySeparatorChar), StringComparison.Ordinal);
        Assert.True(RealPath.RealPathsEqual(stored, lower), $"Expected '{stored}', got '{lower}'.");
    }

    /// <summary>
    ///     Test that in a folder that distinguishes letter case, files in sibling folders whose names differ only in
    ///     letter case report distinct real paths, each in its exact letter case.
    /// </summary>
    [Fact]
    public void RealPath_Inspect_CaseVariantSiblings_ReportsEachExactly()
    {
        // Arrange: allowed/a.yaml and ALLOWED/a.yaml in a case-sensitive folder
        using var caseSensitive = CaseSensitiveFolder.CreateOrSkip();
        var lowerFile = Path.Combine(caseSensitive.FolderPath, "allowed", "a.yaml");
        var upperFile = Path.Combine(caseSensitive.FolderPath, "ALLOWED", "a.yaml");
        Directory.CreateDirectory(Path.GetDirectoryName(lowerFile)!);
        Directory.CreateDirectory(Path.GetDirectoryName(upperFile)!);
        File.WriteAllText(lowerFile, "lower");
        File.WriteAllText(upperFile, "upper");

        // Act
        string? lowerReal;
        string? upperReal;
        using (var handle = RealPath.OpenForRead(lowerFile))
        {
            lowerReal = RealPath.Inspect(handle).FinalPath;
        }

        using (var handle = RealPath.OpenForRead(upperFile))
        {
            upperReal = RealPath.Inspect(handle).FinalPath;
        }

        // Assert: each real path keeps its own letter case, and they differ
        Assert.NotNull(lowerReal);
        Assert.NotNull(upperReal);
        Assert.EndsWith(Path.Combine("allowed", "a.yaml"), lowerReal, StringComparison.Ordinal);
        Assert.EndsWith(Path.Combine("ALLOWED", "a.yaml"), upperReal, StringComparison.Ordinal);
        Assert.False(RealPath.RealPathsEqual(lowerReal, upperReal));
    }

    /// <summary>
    ///     Test that on Linux and macOS the real location of a folder that may be searched but not read is still
    ///     reported (on macOS through the <c>realpath(3)</c> fallback, as the folder cannot be opened for reading).
    /// </summary>
    [Fact]
    public void RealPath_TryGetCanonicalDirectory_SearchOnlyFolder_ReturnsRealPath()
    {
        Assert.SkipUnless(
            OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(),
            "Folder permissions are tested on Linux and macOS only.");

        // Arrange: a folder and its real location while it is still readable
        using var tmpDir = new TemporaryDirectory();
        var folder = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "search-only")).FullName;
        var expected = RealPath.TryGetCanonicalDirectory(folder);

        // Act: only search (execute) permission for the owner
        var canonical = WithUnixMode(folder, UnixFileMode.UserExecute, () => RealPath.TryGetCanonicalDirectory(folder));

        // Assert
        Assert.NotNull(expected);
        Assert.NotNull(canonical);
        Assert.True(RealPath.RealPathsEqual(expected, canonical), $"Expected '{expected}', got '{canonical}'.");
    }

    /// <summary>Test that opening an existing file for writing keeps its content.</summary>
    [Fact]
    public void RealPath_TryOpenExistingForWrite_ExistingFile_KeepsContent()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("out.md");
        File.WriteAllText(file, "original");

        // Act
        using (var handle = RealPath.TryOpenExistingForWrite(file))
        {
            Assert.NotNull(handle);
        }

        // Assert
        Assert.Equal("original", File.ReadAllText(file));
    }

    /// <summary>Test that exclusive creation fails when an entry already has the name.</summary>
    [Fact]
    public void RealPath_CreateNewForWrite_ExistingEntry_Throws()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("out.md");
        File.WriteAllText(file, "original");

        // Act / Assert
        Assert.Throws<IOException>(() => RealPath.CreateNewForWrite(file).Dispose());
        Assert.Equal("original", File.ReadAllText(file));
    }

    /// <summary>
    ///     Test that the facts of a path come from a handle: a missing file, a missing folder and a link whose target
    ///     is missing are missing; a folder and a file exist, with their type (unknown on macOS) and real path.
    /// </summary>
    [Fact]
    public void RealPath_InspectPath_FileFolderMissing_Classified()
    {
        // Arrange: a folder, a file in it, and a folder link whose target was removed
        using var tmpDir = new TemporaryDirectory();
        var folder = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "folder")).FullName;
        var file = Path.Combine(folder, "a.yaml");
        File.WriteAllText(file, "x");
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        var link = Path.Combine(tmpDir.DirectoryPath, "link");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);
        Directory.Delete(target);
        var canonicalFolder = RealPath.TryGetCanonicalDirectory(folder)!;

        // Act
        var missingFile = RealPath.InspectPath(Path.Combine(folder, "missing.yaml"));
        var missingFolder = RealPath.InspectPath(Path.Combine(tmpDir.DirectoryPath, "missing", "a.yaml"));
        var dangling = RealPath.InspectPath(link);
        var folderFacts = RealPath.InspectPath(folder);
        var fileFacts = RealPath.InspectPath(file);

        // Assert
        Assert.Equal(new PathFacts(PathState.Missing, null, null), missingFile);
        Assert.Equal(new PathFacts(PathState.Missing, null, null), missingFolder);
        Assert.Equal(new PathFacts(PathState.Missing, null, null), dangling);
        Assert.Equal(PathState.Exists, folderFacts.State);
        Assert.Equal(OperatingSystem.IsMacOS() ? null : true, folderFacts.IsDirectory);
        Assert.Equal(PathState.Exists, fileFacts.State);
        Assert.Equal(OperatingSystem.IsMacOS() ? null : false, fileFacts.IsDirectory);
        Assert.NotNull(fileFacts.FinalPath);
        Assert.True(RealPath.RealPathsEqual(Path.Join(canonicalFolder, "a.yaml"), fileFacts.FinalPath));
    }

    /// <summary>
    ///     Test that a folder check tells an existing folder on every platform (on macOS from an open with
    ///     <c>O_DIRECTORY</c>), with its real path, and that a file, a missing path and a folder link whose target is
    ///     missing are not existing folders.
    /// </summary>
    [Fact]
    public void RealPath_InspectFolder_FolderFileMissing_Classified()
    {
        // Arrange: a folder, a file in it, and a folder link whose target was removed
        using var tmpDir = new TemporaryDirectory();
        var folder = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "folder")).FullName;
        var file = Path.Combine(folder, "a.yaml");
        File.WriteAllText(file, "x");
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        var link = Path.Combine(tmpDir.DirectoryPath, "link");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);
        Directory.Delete(target);
        var canonicalFolder = RealPath.TryGetCanonicalDirectory(folder)!;

        // Act
        var folderFacts = RealPath.InspectFolder(folder);
        var fileFacts = RealPath.InspectFolder(file);
        var missing = RealPath.InspectFolder(Path.Combine(tmpDir.DirectoryPath, "missing"));
        var dangling = RealPath.InspectFolder(link);

        // Assert: the folder exists as a folder, with its real path, on every platform
        Assert.Equal(PathState.Exists, folderFacts.State);
        Assert.True(folderFacts.IsDirectory);
        Assert.NotNull(folderFacts.FinalPath);
        Assert.True(RealPath.RealPathsEqual(canonicalFolder, folderFacts.FinalPath));

        // Assert: a file is not an existing folder (missing on macOS, where the open requires a folder)
        Assert.False(fileFacts.State == PathState.Exists && fileFacts.IsDirectory != false);
        if (OperatingSystem.IsMacOS())
        {
            Assert.Equal(new PathFacts(PathState.Missing, null, null), fileFacts);
        }
        else
        {
            Assert.Equal(PathState.Exists, fileFacts.State);
            Assert.False(fileFacts.IsDirectory);
        }

        // Assert: a missing path and a dangling folder link are missing
        Assert.Equal(new PathFacts(PathState.Missing, null, null), missing);
        Assert.Equal(new PathFacts(PathState.Missing, null, null), dangling);
    }

    /// <summary>Test that a folder opens for reading and its handle is reported as not a regular file.</summary>
    [Fact]
    public void RealPath_OpenForRead_Directory_HandleIsNotRegular()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var folder = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "folder")).FullName;

        // Act
        HandleFacts facts;
        using (var handle = RealPath.OpenForRead(folder))
        {
            facts = RealPath.Inspect(handle);
        }

        // Assert
        Assert.False(facts.IsRegularFile);
    }

    /// <summary>Test that opening a missing file for reading reports it as not found.</summary>
    [Fact]
    public void RealPath_OpenForRead_MissingFile_ThrowsFileNotFound()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();

        // Act / Assert
        Assert.Throws<FileNotFoundException>(() => RealPath.OpenForRead(Path.Combine(tmpDir.DirectoryPath, "missing.yaml")).Dispose());
    }

    /// <summary>Test that on Linux and macOS opening a folder for writing is reported as not openable (null).</summary>
    [Fact]
    public void RealPath_TryOpenExistingForWrite_Directory_ReturnsNull()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(), "EISDIR is tested on Linux and macOS only.");

        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var folder = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "folder")).FullName;

        // Act
        using var handle = RealPath.TryOpenExistingForWrite(folder);

        // Assert
        Assert.Null(handle);
    }

    /// <summary>Test that on Windows a path longer than the classic limit is inspected and opened.</summary>
    [Fact]
    public void RealPath_InspectPath_LongPath_ReportsRealPath()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The extended-length prefix is tested on Windows only.");

        // Arrange: a file whose full path is longer than 260 characters
        using var tmpDir = new TemporaryDirectory();
        var folder = tmpDir.DirectoryPath;
        while (folder.Length < 280)
        {
            folder = Path.Combine(folder, new string('d', 40));
        }

        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, "a.yaml");
        File.WriteAllText(file, "x");

        // Act
        var facts = RealPath.InspectPath(file);
        string content;
        using (var handle = RealPath.OpenForRead(file))
        using (var stream = new FileStream(handle, FileAccess.Read, bufferSize: 1))
        using (var reader = new StreamReader(stream))
        {
            content = reader.ReadToEnd();
        }

        // Assert
        Assert.Equal(PathState.Exists, facts.State);
        Assert.False(facts.IsDirectory);
        Assert.NotNull(facts.FinalPath);
        Assert.EndsWith(Path.Combine(new string('d', 40), "a.yaml"), facts.FinalPath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("x", content);
    }

    /// <summary>
    ///     Test that the folder boundary is a directory separator, not a name prefix, for real paths and for paths
    ///     as written.
    /// </summary>
    [Fact]
    public void RealPath_IsUnder_BoundaryIsSeparator()
    {
        // Arrange
        var folder = Path.GetFullPath("docs");

        // Act / Assert: both containment checks share the boundary rule
        foreach (var isUnder in new Func<string, string, bool>[] { RealPath.IsUnderRealPath, RealPath.IsWrittenUnder })
        {
            Assert.True(isUnder(folder, folder));
            Assert.True(isUnder(Path.Combine(folder, "a", "b.yaml"), folder));
            Assert.True(isUnder(Path.Combine(folder, "a"), folder + Path.DirectorySeparatorChar));
            Assert.False(isUnder(folder + "2", folder));
            Assert.False(isUnder(Path.GetDirectoryName(folder)!, folder));
        }
    }

    /// <summary>Test that real paths are equal only with the same letter case, on every platform.</summary>
    [Fact]
    public void RealPath_RealPathsEqual_LetterCaseSignificant()
    {
        // Arrange
        var path = Path.GetFullPath("Docs");
        var upper = Path.Combine(Path.GetDirectoryName(path)!, "DOCS");

        // Act
        var equal = RealPath.RealPathsEqual(path, upper);

        // Assert: letter case is significant everywhere; a trailing separator is ignored
        Assert.False(equal);
        Assert.True(RealPath.RealPathsEqual(path, path + Path.DirectorySeparatorChar));
    }

    /// <summary>Test that equality of paths as written follows the platform's default letter-case rule.</summary>
    [Fact]
    public void RealPath_WrittenPathsEqual_PlatformCaseRule()
    {
        // Arrange
        var path = Path.GetFullPath("Docs");
        var upper = Path.Combine(Path.GetDirectoryName(path)!, "DOCS");

        // Act
        var equal = RealPath.WrittenPathsEqual(path, upper);

        // Assert: case-insensitive on Windows and macOS, case-sensitive elsewhere; a trailing separator is ignored
        Assert.Equal(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS(), equal);
        Assert.True(RealPath.WrittenPathsEqual(path, path + Path.DirectorySeparatorChar));
    }

    /// <summary>
    ///     Test that a real path in a folder whose name differs from a folder's only in letter case is not under that
    ///     folder on any platform, while as written it is under it on Windows and macOS (the pre-filter).
    /// </summary>
    [Fact]
    public void RealPath_IsUnderRealPath_CaseVariantFolder_NotUnder()
    {
        // Arrange: allowed and its case-variant sibling ALLOWED
        var parent = Path.GetFullPath("base");
        var allowed = Path.Combine(parent, "allowed");
        var variant = Path.Combine(parent, "ALLOWED", "a.yaml");

        // Act
        var realUnder = RealPath.IsUnderRealPath(variant, allowed);
        var writtenUnder = RealPath.IsWrittenUnder(variant, allowed);

        // Assert
        Assert.False(realUnder);
        Assert.True(RealPath.IsUnderRealPath(Path.Combine(allowed, "a.yaml"), allowed));
        Assert.Equal(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS(), writtenUnder);
    }

    /// <summary>Test that the common ancestor is the deepest folder two paths share by whole components.</summary>
    [Fact]
    public void RealPath_CommonAncestor_ReturnsDeepestSharedFolder()
    {
        // Arrange
        var root = Path.GetFullPath("base");
        var first = Path.Combine(root, "config", "a.yaml");
        var second = Path.Combine(root, "configs", "b.yaml");

        // Act
        var ancestor = RealPath.CommonAncestor(first, Path.Combine(root, "config"));
        var sibling = RealPath.CommonAncestor(first, second);

        // Assert
        Assert.Equal(Path.Combine(root, "config"), ancestor);
        Assert.Equal(root, sibling);
    }

    /// <summary>
    ///     Test that folder names differing only in letter case are not shared by the common ancestor on any platform,
    ///     while on Windows a drive letter differing in case is the same root.
    /// </summary>
    [Fact]
    public void RealPath_CommonAncestor_ComponentsDifferingInCase_NotShared()
    {
        // Arrange: a path through Proj and a trusted base below proj
        var root = Path.GetFullPath("base");
        var first = Path.Combine(root, "Proj", "docs", "a.yaml");
        var second = Path.Combine(root, "proj", "cfg");

        // Act
        var ancestor = RealPath.CommonAncestor(first, second);

        // Assert: the case variant ends the shared folders
        Assert.Equal(root, ancestor);
        if (OperatingSystem.IsWindows())
        {
            Assert.Equal(@"c:\base", RealPath.CommonAncestor(@"c:\base\x", @"C:\base\y"));
            Assert.Equal(@"c:\", RealPath.CommonAncestor(@"c:\base\x", @"C:\BASE\y"));
        }
    }

    /// <summary>Test that UNC paths are network locations and local paths are not.</summary>
    [Fact]
    public void RealPath_IsNetworkLocation_UncAndLocal_Classified()
    {
        // Act / Assert: network classification applies on Windows only
        var windows = OperatingSystem.IsWindows();
        Assert.Equal(windows, RealPath.IsNetworkLocation(@"\\server\share\a.yaml"));
        Assert.Equal(windows, RealPath.IsNetworkLocation(@"\\?\UNC\server\share\a.yaml"));
        Assert.False(RealPath.IsNetworkLocation(@"\\.\pipe\x"));
        Assert.False(RealPath.IsNetworkLocation(Path.GetFullPath("a.yaml")));
    }

    /// <summary>
    ///     Test that only an extended-length drive or volume path is an ordinary extended-length path, on every
    ///     platform (a pure text check).
    /// </summary>
    /// <param name="path">The path text.</param>
    /// <param name="expected">Whether it is an ordinary extended-length path.</param>
    [Theory]
    [InlineData(@"\\?\C:\docs", true)]
    [InlineData(@"\\?\C:", true)]
    [InlineData(@"\\?\Volume{12345678-1234-1234-1234-123456789abc}\x", true)]
    [InlineData(@"\\?\UNC\s\x", false)]
    [InlineData(@"\\?\pipe\x", false)]
    [InlineData(@"\\.\C:\x", false)]
    [InlineData(@"C:\x", false)]
    [InlineData(@"\\?\Volume{bad}\x", false)]
    public void RealPath_IsOrdinaryExtendedPath_Classified(string path, bool expected)
    {
        // Act
        var result = RealPath.IsOrdinaryExtendedPath(path);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>Test that every reserved Windows device spelling is recognized, and ordinary names are not.</summary>
    /// <param name="component">The path component.</param>
    /// <param name="expected">Whether it is a device name.</param>
    [Theory]
    [InlineData("NUL", true)]
    [InlineData("nul.txt", true)]
    [InlineData("CON", true)]
    [InlineData("prn", true)]
    [InlineData("AUX", true)]
    [InlineData("COM1", true)]
    [InlineData("LPT9", true)]
    [InlineData("COM\u00B9", true)]
    [InlineData("CONIN$", true)]
    [InlineData("conout$", true)]
    [InlineData("NUL:stream", true)]
    [InlineData("NUL .yaml", true)]
    [InlineData("null", false)]
    [InlineData("console", false)]
    [InlineData("COM10", false)]
    [InlineData("terms.yaml", false)]
    public void RealPath_IsWindowsDeviceName_ReservedSpellings(string component, bool expected)
    {
        // Act
        var result = RealPath.IsWindowsDeviceName(component);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>Test that a folder link (symbolic link or junction) is reported as a link and a folder is not.</summary>
    [Fact]
    public void RealPath_IsLink_SymlinkOrJunction_True()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        var link = Path.Combine(tmpDir.DirectoryPath, "link");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);

        // Act / Assert
        Assert.True(RealPath.IsLink(link));
        Assert.False(RealPath.IsLink(target));
        Assert.False(RealPath.IsLink(Path.Combine(tmpDir.DirectoryPath, "missing")));
    }

    /// <summary>Test that a path without a short (8.3) name is returned unchanged.</summary>
    [Fact]
    public void RealPath_ExpandShortNames_PathWithoutShortName_Unchanged()
    {
        // Arrange
        var path = Path.GetFullPath("plain-name.yaml");

        // Act
        var expanded = RealPath.ExpandShortNames(path);

        // Assert
        Assert.Equal(path, expanded);
    }

    /// <summary>
    ///     Test that a link to a network location is found at any folder of a path, in either target spelling,
    ///     through a chain of links (up to 63) and through a relative target, without following any link.
    /// </summary>
    [Fact]
    public void RealPath_FindNetworkLinkTarget_IntermediateLinkToShare_ReturnsNetworkPath()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: a fake link reader over a local base folder, so no link or share is needed
        var root = Path.GetFullPath("walk");
        var links = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(root, "lnk")] = @"\\server\share\x",
            [Path.Combine(root, "nt")] = @"\??\UNC\server\share",
            [Path.Combine(root, "a")] = Path.Combine(root, "b"),
            [Path.Combine(root, "b", "c")] = @"\\server\s",
            [Path.Combine(root, "sub", "r")] = @"..\b",
        };
        for (var i = 0; i < 62; i++)
        {
            links[Path.Combine(root, $"h{i}")] = Path.Combine(root, $"h{i + 1}");
        }

        links[Path.Combine(root, "h62")] = @"\\server\deep";
        string? Reader(string path) => links.GetValueOrDefault(path);

        // Act
        var intermediate = RealPath.FindNetworkLinkTarget(Path.Combine(root, "lnk", "sub", "terms.yaml"), Reader);
        var ntForm = RealPath.FindNetworkLinkTarget(Path.Combine(root, "nt", "f.yaml"), Reader);
        var chain = RealPath.FindNetworkLinkTarget(Path.Combine(root, "a", "c", "f.yaml"), Reader);
        var relative = RealPath.FindNetworkLinkTarget(Path.Combine(root, "sub", "r", "c", "f.yaml"), Reader);
        var longChain = RealPath.FindNetworkLinkTarget(Path.Combine(root, "h0", "f.yaml"), Reader);

        // Assert: the path the links lead to, with the components after the link
        Assert.Equal(@"\\server\share\x\sub\terms.yaml", intermediate);
        Assert.Equal(@"\\server\share\f.yaml", ntForm);
        Assert.Equal(@"\\server\s\f.yaml", chain);
        Assert.Equal(@"\\server\s\f.yaml", relative);
        Assert.Equal(@"\\server\deep\f.yaml", longChain);
    }

    /// <summary>
    ///     Test that a path through local links only, a link loop, a chain longer than 63 links and a path without
    ///     links report no network target, and that the walk ends.
    /// </summary>
    [Fact]
    public void RealPath_FindNetworkLinkTarget_LocalLinksAndLoop_ReturnsNull()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: local links (one in NT form), a loop, and a chain of 64 links that ends at a share
        var root = Path.GetFullPath("walk");
        var links = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(root, "a")] = Path.Combine(root, "b"),
            [Path.Combine(root, "b")] = @"\??\" + Path.Combine(root, "local"),
            [Path.Combine(root, "x")] = Path.Combine(root, "y"),
            [Path.Combine(root, "y")] = Path.Combine(root, "x"),
        };
        for (var i = 0; i < 63; i++)
        {
            links[Path.Combine(root, $"h{i}")] = Path.Combine(root, $"h{i + 1}");
        }

        links[Path.Combine(root, "h63")] = @"\\server\deep";
        string? Reader(string path) => links.GetValueOrDefault(path);

        // Act
        var local = RealPath.FindNetworkLinkTarget(Path.Combine(root, "a", "f.yaml"), Reader);
        var loop = RealPath.FindNetworkLinkTarget(Path.Combine(root, "x", "f.yaml"), Reader);
        var tooLong = RealPath.FindNetworkLinkTarget(Path.Combine(root, "h0", "f.yaml"), Reader);
        var plain = RealPath.FindNetworkLinkTarget(Path.Combine(root, "plain", "f.yaml"), _ => null);

        // Assert
        Assert.Null(local);
        Assert.Null(loop);
        Assert.Null(tooLong);
        Assert.Null(plain);
    }

    /// <summary>Test that a real local folder link is read and followed locally, reporting no network target.</summary>
    [Fact]
    public void RealPath_FindNetworkLinkTarget_RealLocalLink_ReturnsNull()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: link -> target (a junction where symbolic links are not permitted)
        using var tmpDir = new TemporaryDirectory();
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        var link = Path.Combine(tmpDir.DirectoryPath, "link");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);

        // Act
        var linkTarget = RealPath.ReadLinkTarget(link);
        var networkTarget = RealPath.FindNetworkLinkTarget(Path.Combine(link, "f.yaml"));

        // Assert
        Assert.NotNull(linkTarget);
        Assert.Null(networkTarget);
    }

    /// <summary>
    ///     Runs an action while a folder has the given Unix permissions, restoring its permissions afterwards.
    /// </summary>
    /// <typeparam name="T">The result type of the action.</typeparam>
    /// <param name="folder">The full path of the folder.</param>
    /// <param name="mode">The permissions to apply during the action.</param>
    /// <param name="action">The action.</param>
    /// <returns>What the action returned.</returns>
    /// <exception cref="PlatformNotSupportedException">Thrown on Windows, which has no Unix permissions.</exception>
    private static T WithUnixMode<T>(string folder, UnixFileMode mode, Func<T> action)
    {
        if (OperatingSystem.IsWindows())
        {
            throw new PlatformNotSupportedException("Unix permissions are not available on Windows.");
        }

        var original = File.GetUnixFileMode(folder);
        File.SetUnixFileMode(folder, mode);
        try
        {
            return action();
        }
        finally
        {
            File.SetUnixFileMode(folder, original);
        }
    }
}
