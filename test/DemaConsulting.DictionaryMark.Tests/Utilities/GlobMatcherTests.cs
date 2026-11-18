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
///     Unit tests for the GlobMatcher class.
/// </summary>
[Collection("Sequential")]
public class GlobMatcherTests
{
    /// <summary>
    ///     Test that a null patterns collection throws ArgumentNullException.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_NullPatterns_ThrowsArgumentNullException()
    {
        // Act + Assert:
        Assert.Throws<ArgumentNullException>(() => GlobMatcher.GetFiles(null!));
    }

    /// <summary>
    ///     Test that an existing absolute path returns the file.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_ExistingAbsolutePath_ReturnsFile()
    {
        // Arrange:
        var tmpFile = Path.GetTempFileName();
        try
        {
            // Act + Assert:
            var result = GlobMatcher.GetFiles([tmpFile]);
            Assert.Contains(result, f => string.Equals(f, tmpFile, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    /// <summary>
    ///     Test that rooted paths with dot segments return fully-qualified file paths.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_RootedPathWithDotSegments_ReturnsFullyQualifiedPath()
    {
        // Arrange:
        using var tmpDir = new TemporaryDirectory();
        var nestedDirectory = Path.Combine(tmpDir.DirectoryPath, "a", "b");
        Directory.CreateDirectory(nestedDirectory);
        var filePath = Path.Combine(nestedDirectory, "test.yaml");
        File.WriteAllText(filePath, "key: value");
        var pathWithDotSegments = Path.Combine(tmpDir.DirectoryPath, "a", ".", "b", "..", "b", "test.yaml");
        var expected = Path.GetFullPath(pathWithDotSegments);

        // Act:
        var result = GlobMatcher.GetFiles([pathWithDotSegments]);

        // Assert:
        Assert.Contains(result, f => string.Equals(f, expected, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Test that a non-existent absolute path returns empty.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_NonExistentAbsolutePath_ReturnsEmpty()
    {
        // Act + Assert:
        var result = GlobMatcher.GetFiles(["/nonexistent/path/file.yaml"]);
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that empty pattern list returns empty.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_EmptyPatternList_ReturnsEmpty()
    {
        // Act + Assert:
        var result = GlobMatcher.GetFiles([]);
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that null or empty pattern throws ArgumentException.
    /// </summary>
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GlobMatcher_GetFiles_NullOrEmptyPattern_ThrowsArgumentException(string? pattern)
    {
        // Act + Assert:
        Assert.Throws<ArgumentException>(() => GlobMatcher.GetFiles([pattern!]));
    }

    /// <summary>
    ///     Test that duplicate absolute paths are deduplicated.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_DuplicateAbsolutePaths_DeduplicatesResults()
    {
        // Arrange:
        var tmpFile = Path.GetTempFileName();
        try
        {
            // Act + Assert:
            var result = GlobMatcher.GetFiles([tmpFile, tmpFile]);
            Assert.Single(result);
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    /// <summary>
    ///     Test that an absolute path glob pattern matches files in the target directory.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_AbsolutePathGlobPattern_ReturnsMatchingFiles()
    {
        // Arrange:
        using var tmpDir = new TemporaryDirectory();
        var tmpFile = tmpDir.GetFilePath("test.yaml");
        File.WriteAllText(tmpFile, "key: value");
        var pattern = Path.Combine(tmpDir.DirectoryPath, "*.yaml");

        // Act:
        var result = GlobMatcher.GetFiles([pattern]);

        // Assert:
        Assert.Contains(result, f => string.Equals(f, tmpFile, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Test that an absolute path glob pattern with no matches returns empty.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_AbsolutePathGlobPattern_NoMatches_ReturnsEmpty()
    {
        // Arrange:
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}");
        var pattern = Path.Combine(nonExistentDir, "*.yaml");

        // Act + Assert:
        var result = GlobMatcher.GetFiles([pattern]);
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that a relative glob pattern resolves matching files in the current directory.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_RelativeGlobPattern_ReturnsMatchingFiles()
    {
        // Arrange: use TemporaryDirectory to avoid OS symlink issues (e.g. /tmp -> /private/tmp on macOS)
        using var tmpDir = new TemporaryDirectory();
        var testFile = tmpDir.GetFilePath("test.yaml");
        File.WriteAllText(testFile, "key: value");
        var savedDir = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = tmpDir.DirectoryPath;

            // Act:
            var result = GlobMatcher.GetFiles(["*.yaml"]);

            // Assert:
            Assert.Contains(result, f => string.Equals(f, testFile, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.CurrentDirectory = savedDir;
        }
    }

    /// <summary>Test that Resolve returns an existing literal and a glob match.</summary>
    [Fact]
    public void GlobMatcher_Resolve_LiteralAndGlob_ReturnsFiles()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("a.yaml");
        File.WriteAllText(file, "A: a\n");

        // Act
        var result = GlobMatcher.Resolve([file, Path.Combine(tmpDir.DirectoryPath, "*.yaml")]);

        // Assert: de-duplicated
        Assert.Equal([Path.GetFullPath(file)], result);
    }

    /// <summary>Test that Resolve fails for a glob matching nothing, naming the pattern.</summary>
    [Fact]
    public void GlobMatcher_Resolve_GlobWithNoMatch_ThrowsNamingPattern()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var pattern = Path.Combine(tmpDir.DirectoryPath, "*.yaml");

        // Act / Assert
        var ex = Assert.Throws<InvalidOperationException>(() => GlobMatcher.Resolve([pattern]));
        Assert.Contains($"Input pattern '{pattern}' matched no files", ex.Message);
    }

    /// <summary>Test that Resolve fails for a missing file or directory with distinct messages.</summary>
    [Fact]
    public void GlobMatcher_Resolve_MissingFileOrDirectory_ThrowsDistinctErrors()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var missingFile = tmpDir.GetFilePath("missing.yaml");
        var missingDir = Path.Combine(tmpDir.DirectoryPath, "missing-dir", "x.yaml");

        // Act
        var fileEx = Assert.Throws<InvalidOperationException>(() => GlobMatcher.Resolve([missingFile]));
        var dirEx = Assert.Throws<InvalidOperationException>(() => GlobMatcher.Resolve([missingDir]));

        // Assert
        Assert.Contains($"Input file '{missingFile}' was not found", fileEx.Message);
        Assert.Contains("Input directory", dirEx.Message);
    }

    /// <summary>Test that relative literal paths resolve against the current directory.</summary>
    [Fact]
    public void GlobMatcher_Resolve_RelativeLiteral_ResolvesFullPath()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("a.yaml");
        File.WriteAllText(file, "A: a\n");
        var savedDir = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = tmpDir.DirectoryPath;

            // Act
            var result = GlobMatcher.Resolve(["a.yaml"]);

            // Assert
            Assert.Equal([Path.GetFullPath(file)], result);
        }
        finally
        {
            Environment.CurrentDirectory = savedDir;
        }
    }

    /// <summary>Test ${VAR} expansion and the undefined-variable error.</summary>
    [Fact]
    public void GlobMatcher_Resolve_EnvironmentVariable_IsExpandedOrFails()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("a.yaml");
        File.WriteAllText(file, "A: a\n");
        const string Name = "DICTIONARYMARK_TEST_DIR";
        Environment.SetEnvironmentVariable(Name, tmpDir.DirectoryPath);
        try
        {
            // Act
            var result = GlobMatcher.Resolve(["${" + Name + "}" + Path.DirectorySeparatorChar + "a.yaml"]);

            // Assert
            Assert.Equal([Path.GetFullPath(file)], result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(Name, null);
        }

        // Act / Assert: the now-undefined variable is an error naming it
        var ex = Assert.Throws<InvalidOperationException>(() => GlobMatcher.Resolve(["${" + Name + "}/a.yaml"]));
        Assert.Contains($"Environment variable '{Name}' referenced by glossary path", ex.Message);
        Assert.Contains("is not defined", ex.Message);
    }

    /// <summary>Test that a UNC path (administrative share of the local machine) resolves on Windows.</summary>
    [Fact]
    public void GlobMatcher_Resolve_UncPath_ResolvesOnWindows()
    {
        // This scenario is only meaningful on Windows where the local administrative share is reachable.
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Arrange: express a temporary file through \\localhost\X$ if that share is reachable
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("a.yaml");
        File.WriteAllText(file, "A: a\n");
        var drive = Path.GetPathRoot(file)![0];
        var unc = $@"\\localhost\{drive}$" + file[2..];
        if (!File.Exists(unc))
        {
            // Administrative shares are unavailable on this machine; nothing to verify
            return;
        }

        // Act
        var literal = GlobMatcher.Resolve([unc]);
        var glob = GlobMatcher.Resolve([Path.Combine(Path.GetDirectoryName(unc)!, "*.yaml")]);

        // Assert
        Assert.Single(literal);
        Assert.Single(glob);
    }

    /// <summary>Test that relative patterns resolve against a supplied base directory.</summary>
    [Fact]
    public void GlobMatcher_Resolve_BaseDirectory_ResolvesRelativePatterns()
    {
        // Arrange: files in a base directory that is not the working directory
        using var tmpDir = new TemporaryDirectory();
        var a = tmpDir.GetFilePath("a.yaml");
        var b = tmpDir.GetFilePath(Path.Combine("sub", "b.yaml"));
        File.WriteAllText(a, "A: a\n");
        File.WriteAllText(b, "B: b\n");

        // Act: resolve a literal and a recursive glob relative to the base directory
        var result = GlobMatcher.Resolve(["a.yaml", "sub/*.yaml"], tmpDir.DirectoryPath);

        // Assert: both files are found as full paths
        Assert.Equal([Path.GetFullPath(a), Path.GetFullPath(b)], result);
    }

    /// <summary>Test that a glob starting with '..' resolves against the base directory.</summary>
    [Fact]
    public void GlobMatcher_Resolve_LeadingParentSegmentGlob_ReturnsFiles()
    {
        // Arrange: a shared directory beside the base directory
        using var tmpDir = new TemporaryDirectory();
        var shared = tmpDir.GetFilePath(Path.Combine("shared", "terms.yaml"));
        File.WriteAllText(shared, "A: a\n");
        var baseDirectory = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "config")).FullName;

        // Act
        var result = GlobMatcher.Resolve(["../shared/*.yaml"], baseDirectory);

        // Assert
        Assert.Equal([Path.GetFullPath(shared)], result);
    }

    /// <summary>Test that globs with '..' after a normal segment and multiple '..' segments resolve.</summary>
    [Fact]
    public void GlobMatcher_Resolve_NestedParentSegmentGlob_ReturnsFiles()
    {
        // Arrange: a nested dictionary tree two levels above the base directory
        using var tmpDir = new TemporaryDirectory();
        var deep = tmpDir.GetFilePath(Path.Combine("a", "x", "deep.yaml"));
        var shared = tmpDir.GetFilePath(Path.Combine("shared", "terms.yaml"));
        File.WriteAllText(deep, "D: d\n");
        File.WriteAllText(shared, "S: s\n");
        var baseDirectory = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "c", "d")).FullName;

        // Act: a recursive glob after two '..' segments, and '..' after a normal segment
        var recursive = GlobMatcher.Resolve(["../../a/**/*.yaml"], baseDirectory);
        var midPattern = GlobMatcher.Resolve(["sub/../../../shared/*.yaml"], baseDirectory);

        // Assert
        Assert.Equal([Path.GetFullPath(deep)], recursive);
        Assert.Equal([Path.GetFullPath(shared)], midPattern);
    }

    /// <summary>Test that a literal path with '..' resolves against the base directory.</summary>
    [Fact]
    public void GlobMatcher_Resolve_ParentSegmentLiteral_ReturnsFile()
    {
        // Arrange: a file beside the base directory
        using var tmpDir = new TemporaryDirectory();
        var shared = tmpDir.GetFilePath(Path.Combine("shared", "terms.yaml"));
        File.WriteAllText(shared, "A: a\n");
        var baseDirectory = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "config")).FullName;

        // Act
        var result = GlobMatcher.Resolve(["../shared/terms.yaml"], baseDirectory);

        // Assert
        Assert.Equal([Path.GetFullPath(shared)], result);
    }

    /// <summary>Test that a glob is split into its literal folder and its wildcard remainder.</summary>
    /// <param name="pattern">The glob pattern.</param>
    /// <param name="folder">The expected literal folder.</param>
    /// <param name="remainder">The expected wildcard remainder.</param>
    [Theory]
    [InlineData("/*.yaml", "/", "*.yaml")]
    [InlineData("C:/*.yaml", "C:/", "*.yaml")]
    [InlineData("*.yaml", "", "*.yaml")]
    [InlineData("../a/**/*.yaml", "../a", "**/*.yaml")]
    [InlineData(@"docs\sub\*.yaml", "docs/sub", "*.yaml")]
    [InlineData("docs/a?/b/*.yaml", "docs", "a?/b/*.yaml")]
    public void GlobMatcher_SplitGlob_FolderAndPattern(string pattern, string folder, string remainder)
    {
        // Act
        var result = GlobMatcher.SplitGlob(pattern);

        // Assert
        Assert.Equal((folder, remainder), result);
    }

    /// <summary>
    ///     Test that every folder below the judged folder, a plain folder and a link folder alike, is checked with its
    ///     full path before a wildcard descends into it, that it is listed when the check allows it, that a pattern
    ///     that never descends into it does not check it, and that a missing folder matches nothing.
    /// </summary>
    [Fact]
    public void GlobMatcher_MatchUnder_LinkFolder_CheckedBeforeListing()
    {
        // Arrange: base/real/a.yaml and base/lnk -> target, which holds b.yaml
        using var tmpDir = new TemporaryDirectory();
        var baseFolder = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "base", "real")).Parent!.FullName;
        File.WriteAllText(Path.Combine(baseFolder, "real", "a.yaml"), "A: a\n");
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        File.WriteAllText(Path.Combine(target, "b.yaml"), "B: b\n");
        var link = Path.Combine(baseFolder, "lnk");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);
        var checkedFolders = new List<string>();
        bool Check(string folder)
        {
            checkedFolders.Add(folder);
            return true;
        }

        // Act
        var recursive = GlobMatcher.MatchUnder(baseFolder, "**/*.yaml", Check);
        var checkedByRecursive = checkedFolders.ToList();
        checkedFolders.Clear();
        var shallow = GlobMatcher.MatchUnder(baseFolder, "*.yaml", Check);
        var missing = GlobMatcher.MatchUnder(Path.Combine(tmpDir.DirectoryPath, "missing"), "**/*.yaml", Check);

        // Assert
        Assert.Equal(
            new[] { Path.Combine(baseFolder, "real"), link }.Order(StringComparer.Ordinal),
            checkedByRecursive.Order(StringComparer.Ordinal));
        Assert.Equal(
            ["lnk/b.yaml", "real/a.yaml"],
            recursive.Select(match => match.RelativePath).Order(StringComparer.Ordinal));
        Assert.Contains(recursive, match => match.FullPath == Path.Combine(link, "b.yaml"));
        Assert.Empty(shallow);
        Assert.Empty(checkedFolders);
        Assert.Empty(missing);
    }

    /// <summary>
    ///     Test that a link folder the check rejects is never listed: a check returning false for it lists nothing in
    ///     it, and a check that throws for it stops the matching with its exception.
    /// </summary>
    [Fact]
    public void GlobMatcher_MatchUnder_RejectedLink_NotListed()
    {
        // Arrange: base/real/a.yaml and base/lnk -> target, which holds b.yaml
        using var tmpDir = new TemporaryDirectory();
        var baseFolder = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "base", "real")).Parent!.FullName;
        File.WriteAllText(Path.Combine(baseFolder, "real", "a.yaml"), "A: a\n");
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        File.WriteAllText(Path.Combine(target, "b.yaml"), "B: b\n");
        var link = Path.Combine(baseFolder, "lnk");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);

        // Act
        var skipped = GlobMatcher.MatchUnder(baseFolder, "**/*.yaml", folder => folder != link);
        var thrown = Record.Exception(
            () => GlobMatcher.MatchUnder(
                baseFolder,
                "**/*.yaml",
                folder => folder == link ? throw new InvalidOperationException(folder) : true));

        // Assert
        Assert.Equal(["real/a.yaml"], skipped.Select(match => match.RelativePath));
        Assert.Equal(link, Assert.IsType<InvalidOperationException>(thrown).Message);
    }

    /// <summary>Test that a folder that does not exist matches nothing, and no folder check is called.</summary>
    [Fact]
    public void GlobMatcher_MatchUnder_MissingFolder_ReturnsEmpty()
    {
        // Arrange: a folder that does not exist
        using var tmpDir = new TemporaryDirectory();
        var missing = Path.Combine(tmpDir.DirectoryPath, "missing");

        // Act
        var result = GlobMatcher.MatchUnder(missing, "**/*.yaml", _ => throw new InvalidOperationException("checked"));

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that a link folder whose target is removed after the check allowed it, and before it is listed,
    ///     matches nothing in it, without an error, while the rest of the folder is still matched.
    /// </summary>
    [Fact]
    public void GlobMatcher_MatchUnder_FolderRemovedAfterCheck_MatchesNothingInIt()
    {
        // Arrange: base/real/a.yaml and base/lnk -> target, which holds b.yaml; the check removes target
        using var tmpDir = new TemporaryDirectory();
        var baseFolder = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "base", "real")).Parent!.FullName;
        File.WriteAllText(Path.Combine(baseFolder, "real", "a.yaml"), "A: a\n");
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        File.WriteAllText(Path.Combine(target, "b.yaml"), "B: b\n");
        var link = Path.Combine(baseFolder, "lnk");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);
        var checkedFolders = new List<string>();
        bool RemoveThenAllow(string folder)
        {
            checkedFolders.Add(folder);
            if (folder == link)
            {
                Directory.Delete(target, recursive: true);
            }

            return true;
        }

        // Act
        var result = GlobMatcher.MatchUnder(baseFolder, "**/*.yaml", RemoveThenAllow);

        // Assert
        Assert.Equal(["real/a.yaml"], result.Select(match => match.RelativePath));
        Assert.Equal(
            new[] { Path.Combine(baseFolder, "real"), link }.Order(StringComparer.Ordinal),
            checkedFolders.Order(StringComparer.Ordinal));
    }

    /// <summary>
    ///     Test that every plain folder below the judged folder is checked, with its full path, before a wildcard
    ///     descends into it: the judged folder itself is never checked, a folder the check rejects is not listed (so
    ///     nothing below it is checked or matched), a pattern that never descends into a folder checks none, and an
    ///     exception from the check stops the matching.
    /// </summary>
    [Fact]
    public void GlobMatcher_MatchUnder_EveryFolder_CheckedBeforeListing()
    {
        // Arrange: base/a.yaml, base/sub/b.yaml, base/sub/deeper/c.yaml and base/other/d.txt, with no link
        using var tmpDir = new TemporaryDirectory();
        var baseFolder = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "base")).FullName;
        var sub = Directory.CreateDirectory(Path.Combine(baseFolder, "sub")).FullName;
        var deeper = Directory.CreateDirectory(Path.Combine(sub, "deeper")).FullName;
        var other = Directory.CreateDirectory(Path.Combine(baseFolder, "other")).FullName;
        File.WriteAllText(Path.Combine(baseFolder, "a.yaml"), "A: a\n");
        File.WriteAllText(Path.Combine(sub, "b.yaml"), "B: b\n");
        File.WriteAllText(Path.Combine(deeper, "c.yaml"), "C: c\n");
        File.WriteAllText(Path.Combine(other, "d.txt"), "d\n");
        var checkedFolders = new List<string>();
        string? rejectedFolder = null;
        bool CheckRecorded(string folder)
        {
            checkedFolders.Add(folder);
            return folder != rejectedFolder;
        }

        // Act
        var all = GlobMatcher.MatchUnder(baseFolder, "**/*.yaml", CheckRecorded);
        var checkedByAll = checkedFolders.ToList();
        checkedFolders.Clear();
        rejectedFolder = sub;
        var rejected = GlobMatcher.MatchUnder(baseFolder, "**/*.yaml", CheckRecorded);
        var checkedByRejected = checkedFolders.ToList();
        checkedFolders.Clear();
        var shallow = GlobMatcher.MatchUnder(baseFolder, "*.yaml", CheckRecorded);
        var thrown = Record.Exception(
            () => GlobMatcher.MatchUnder(
                baseFolder,
                "**/*.yaml",
                folder => folder == sub ? throw new InvalidOperationException(folder) : true));

        // Assert
        Assert.Equal(
            new[] { sub, deeper, other }.Order(StringComparer.Ordinal),
            checkedByAll.Order(StringComparer.Ordinal));
        Assert.Equal(
            ["a.yaml", "sub/b.yaml", "sub/deeper/c.yaml"],
            all.Select(match => match.RelativePath).Order(StringComparer.Ordinal));
        Assert.Equal(["a.yaml"], rejected.Select(match => match.RelativePath));
        Assert.Equal(
            new[] { sub, other }.Order(StringComparer.Ordinal),
            checkedByRejected.Order(StringComparer.Ordinal));
        Assert.Equal(["a.yaml"], shallow.Select(match => match.RelativePath));
        Assert.Empty(checkedFolders);
        Assert.Equal(sub, Assert.IsType<InvalidOperationException>(thrown).Message);
    }
}
