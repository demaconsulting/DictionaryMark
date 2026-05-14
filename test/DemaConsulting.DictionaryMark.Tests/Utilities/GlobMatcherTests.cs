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
    ///     Test that an existing absolute path returns the file.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_ExistingAbsolutePath_ReturnsFile()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
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
        var result = GlobMatcher.GetFiles(["/nonexistent/path/file.yaml"]);
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that empty pattern list returns empty.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_EmptyPatternList_ReturnsEmpty()
    {
        var result = GlobMatcher.GetFiles([]);
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that null or empty pattern throws ArgumentException.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_NullOrEmptyPattern_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => GlobMatcher.GetFiles([string.Empty]));
    }

    /// <summary>
    ///     Test that duplicate absolute paths are deduplicated.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_DuplicateAbsolutePaths_DeduplicatesResults()
    {
        var tmpFile = Path.GetTempFileName();
        try
        {
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
        var tmpDir = Path.GetTempPath();
        var tmpFile = Path.Combine(tmpDir, $"glob-test-{Guid.NewGuid()}.yaml");
        File.WriteAllText(tmpFile, "key: value");
        try
        {
            // Use an absolute path with a wildcard pattern
            var pattern = Path.Combine(tmpDir, "*.yaml");
            var result = GlobMatcher.GetFiles([pattern]);
            Assert.Contains(result, f => string.Equals(f, tmpFile, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(tmpFile);
        }
    }

    /// <summary>
    ///     Test that an absolute path glob pattern with no matches returns empty.
    /// </summary>
    [Fact]
    public void GlobMatcher_GetFiles_AbsolutePathGlobPattern_NoMatches_ReturnsEmpty()
    {
        var nonExistentDir = Path.Combine(Path.GetTempPath(), $"nonexistent-{Guid.NewGuid()}");
        var pattern = Path.Combine(nonExistentDir, "*.yaml");
        var result = GlobMatcher.GetFiles([pattern]);
        Assert.Empty(result);
    }
}
