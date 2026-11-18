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
///     Unit tests for the pattern check of <see cref="FileReferenceResolver"/>: an input pattern gets the resolver's text
///     rules (the never list), with the kind and reason a read gets, without touching the file system.
/// </summary>
public class FileReferenceResolverPatternTests
{
    /// <summary>
    ///     Validates that a null pattern is an argument error.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckPatternText_NullPattern_Throws()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => FileReferenceResolver.CheckPatternText(null!));
    }

    /// <summary>
    ///     Validates that an ordinary local glob passes.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckPatternText_LocalGlob_Passes()
    {
        // Arrange
        var pattern = Path.Combine(Path.GetTempPath(), "terms", "*.yaml");

        // Act
        var error = Record.Exception(() => FileReferenceResolver.CheckPatternText(pattern));

        // Assert
        Assert.Null(error);
    }

    /// <summary>
    ///     Validates that a pattern that starts with two forward slashes is a network path on every platform.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckPatternText_ForwardSlashShare_IsNetworkPath()
    {
        // Act
        var error = Assert.Throws<FileReferenceException>(
            () => FileReferenceResolver.CheckPatternText("//dictionarymark-test.invalid/share/*.yaml"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.NetworkPath, error.Kind);
        Assert.Equal("network paths are not permitted", error.Reason);
    }

    /// <summary>
    ///     Validates that a pattern that starts with two backslashes is a network path on Windows.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckPatternText_BackslashShareOnWindows_IsNetworkPath()
    {
        // A backslash is an ordinary file-name character on Linux and macOS
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        // Act
        var error = Assert.Throws<FileReferenceException>(
            () => FileReferenceResolver.CheckPatternText(@"\\dictionarymark-test.invalid\share\*.yaml"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.NetworkPath, error.Kind);
    }

    /// <summary>
    ///     Validates that a URL-like pattern is refused by the colon rule.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckPatternText_UrlLikePattern_IsInvalidPath()
    {
        // Arrange: a URL rooted at a local folder, as a relative pattern is before it is listed
        var pattern = Path.Combine(Path.GetTempPath(), "https://dictionarymark-test.invalid/terms/*.yaml");

        // Act
        var error = Assert.Throws<FileReferenceException>(() => FileReferenceResolver.CheckPatternText(pattern));

        // Assert
        Assert.Equal(FileReferenceFailureKind.InvalidPath, error.Kind);
        Assert.Equal("the path contains a colon", error.Reason);
    }
}
