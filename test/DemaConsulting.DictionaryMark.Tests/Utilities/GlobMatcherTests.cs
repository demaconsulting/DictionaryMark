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
}
