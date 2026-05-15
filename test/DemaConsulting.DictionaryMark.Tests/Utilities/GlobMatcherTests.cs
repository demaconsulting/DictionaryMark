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
        var tmpDir = Path.Combine(Path.GetTempPath(), $"glob-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        var tmpFile = Path.Combine(tmpDir, "test.yaml");
        File.WriteAllText(tmpFile, "key: value");
        try
        {
            var pattern = Path.Combine(tmpDir, "*.yaml");

            // Act:
            var result = GlobMatcher.GetFiles([pattern]);

            // Assert:
            Assert.Contains(result, f => string.Equals(f, tmpFile, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tmpDir, recursive: true);
        }
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
        // Arrange:
        var tmpDir = Path.Combine(Path.GetTempPath(), $"glob-rel-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tmpDir);
        var tmpFile = Path.Combine(tmpDir, "test.yaml");
        File.WriteAllText(tmpFile, "key: value");
        var savedDir = Environment.CurrentDirectory;
        try
        {
            Environment.CurrentDirectory = tmpDir;

            // Act:
            var result = GlobMatcher.GetFiles(["*.yaml"]);

            // Assert:
            Assert.Contains(result, f => string.Equals(f, tmpFile, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Environment.CurrentDirectory = savedDir;
            Directory.Delete(tmpDir, recursive: true);
        }
    }
}
