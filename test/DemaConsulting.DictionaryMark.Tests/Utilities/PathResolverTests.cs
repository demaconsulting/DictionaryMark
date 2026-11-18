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
///     Unit tests for the PathResolver class.
/// </summary>
[Collection("Sequential")]
public class PathResolverTests
{
    /// <summary>Name of the environment variable used by these tests.</summary>
    private const string VariableName = "DICTIONARYMARK_RESOLVER_TEST";

    /// <summary>Test that a relative path is rooted at the base directory.</summary>
    [Fact]
    public void PathResolver_ResolvePath_RelativePath_RootedAtBaseDirectory()
    {
        // Arrange: a base directory and a relative path
        using var tmpDir = new TemporaryDirectory();

        // Act: resolve the relative path
        var result = PathResolver.ResolvePath(Path.Combine("sub", "file.md"), tmpDir.DirectoryPath, "output path");

        // Assert: the result is the full path under the base directory
        Assert.Equal(Path.Combine(tmpDir.DirectoryPath, "sub", "file.md"), result);
    }

    /// <summary>Test that parent-directory segments are normalized.</summary>
    [Fact]
    public void PathResolver_ResolvePath_ParentSegments_Normalized()
    {
        // Arrange: a base directory two levels deep
        using var tmpDir = new TemporaryDirectory();
        var baseDirectory = Path.Combine(tmpDir.DirectoryPath, "a", "b");

        // Act: resolve a path that climbs two levels and descends again
        var result = PathResolver.ResolvePath("../../c/file.md", baseDirectory, "output path");

        // Assert: the '..' segments are collapsed
        Assert.Equal(Path.Combine(tmpDir.DirectoryPath, "c", "file.md"), result);
    }

    /// <summary>Test that an absolute path ignores the base directory.</summary>
    [Fact]
    public void PathResolver_ResolvePath_AbsolutePath_IgnoresBaseDirectory()
    {
        // Arrange: an absolute path in one directory and a different base directory
        using var tmpDir = new TemporaryDirectory();
        using var otherDir = new TemporaryDirectory();
        var absolute = Path.Combine(tmpDir.DirectoryPath, "file.md");

        // Act: resolve the absolute path against the other directory
        var result = PathResolver.ResolvePath(absolute, otherDir.DirectoryPath, "output path");

        // Assert: the absolute path is unchanged
        Assert.Equal(absolute, result);
    }

    /// <summary>Test that ${VAR} references in the path are expanded.</summary>
    [Fact]
    public void PathResolver_ResolvePath_EnvironmentVariable_Expanded()
    {
        // Arrange: a variable naming a directory
        using var tmpDir = new TemporaryDirectory();
        using var otherDir = new TemporaryDirectory();
        Environment.SetEnvironmentVariable(VariableName, tmpDir.DirectoryPath);
        try
        {
            // Act: resolve a path that references the variable
            var result = PathResolver.ResolvePath("${" + VariableName + "}/file.md", otherDir.DirectoryPath, "output path");

            // Assert: the variable value replaces the reference
            Assert.Equal(Path.Combine(tmpDir.DirectoryPath, "file.md"), result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>Test that an undefined variable is an error naming the variable and the description.</summary>
    [Fact]
    public void PathResolver_ExpandEnvironmentVariables_Undefined_ThrowsNamingVariableAndDescription()
    {
        // Arrange: make sure the variable is undefined
        Environment.SetEnvironmentVariable(VariableName, null);
        var path = "${" + VariableName + "}/file.md";

        // Act: expand the undefined reference
        var ex = Assert.Throws<InvalidOperationException>(() => PathResolver.ExpandEnvironmentVariables(path, "output path"));

        // Assert: the message names the variable, the description and the path
        Assert.Equal($"Environment variable '{VariableName}' referenced by output path '{path}' is not defined.", ex.Message);
    }

    /// <summary>Test that Combine roots a glob pattern without normalizing it.</summary>
    [Fact]
    public void PathResolver_Combine_GlobPattern_NotNormalized()
    {
        // Arrange: a base directory and a glob containing '..'
        using var tmpDir = new TemporaryDirectory();

        // Act: combine the glob with the base directory
        var result = PathResolver.Combine("../shared/**/*.yaml", tmpDir.DirectoryPath, "glossary path");

        // Assert: the pattern is rooted but its segments are left as written
        Assert.Equal(Path.Combine(tmpDir.DirectoryPath, "../shared/**/*.yaml"), result);
        Assert.Contains("..", result);
    }

    /// <summary>Test that only the path argument is expanded, never the base directory.</summary>
    [Fact]
    public void PathResolver_Combine_BaseDirectory_NotExpanded()
    {
        // Arrange: a base directory text that contains a variable reference, with the variable defined
        Environment.SetEnvironmentVariable(VariableName, "expanded");
        try
        {
            var baseDirectory = Path.Combine(Path.GetTempPath(), "${" + VariableName + "}");

            // Act: combine a relative path with that base directory
            var result = PathResolver.Combine("file.md", baseDirectory, "output path");

            // Assert: the base directory is used literally
            Assert.Equal(Path.Combine(baseDirectory, "file.md"), result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>Test that null arguments are rejected.</summary>
    [Fact]
    public void PathResolver_ResolvePath_NullArguments_Throw()
    {
        // Act / Assert: each null argument is rejected
        Assert.Throws<ArgumentNullException>(() => PathResolver.ResolvePath(null!, "base", "d"));
        Assert.Throws<ArgumentNullException>(() => PathResolver.ResolvePath("p", null!, "d"));
        Assert.Throws<ArgumentNullException>(() => PathResolver.ResolvePath("p", "base", null!));
        Assert.Throws<ArgumentNullException>(() => PathResolver.ExpandEnvironmentVariables(null!, "d"));
        Assert.Throws<ArgumentNullException>(() => PathResolver.ResolveExistingDirectory(null!, "base", "d"));
        Assert.Throws<ArgumentNullException>(() => PathResolver.ResolveDirectory("p", null!, "d"));
    }

    /// <summary>Test that an existing relative directory resolves to its full path.</summary>
    [Fact]
    public void PathResolver_ResolveExistingDirectory_RelativeDirectory_ReturnsFullPath()
    {
        // Arrange: a sub-directory of a base directory
        using var tmpDir = new TemporaryDirectory();
        var sub = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "root")).FullName;

        // Act: resolve the relative directory
        var result = PathResolver.ResolveExistingDirectory("root", tmpDir.DirectoryPath, "--root option");

        // Assert: the full path of the directory is returned
        Assert.Equal(sub, result);
    }

    /// <summary>Test that '..' segments in a directory are normalized.</summary>
    [Fact]
    public void PathResolver_ResolveExistingDirectory_ParentSegments_Normalized()
    {
        // Arrange: two sibling directories
        using var tmpDir = new TemporaryDirectory();
        var a = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "a")).FullName;
        var b = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "b")).FullName;

        // Act: resolve the sibling through '..'
        var result = PathResolver.ResolveExistingDirectory("../b", a, "root key");

        // Assert: the normalized sibling path is returned
        Assert.Equal(b, result);
    }

    /// <summary>Test that ${VAR} references in a directory are expanded.</summary>
    [Fact]
    public void PathResolver_ResolveExistingDirectory_EnvironmentVariable_Expanded()
    {
        // Arrange: a variable naming an existing directory
        using var tmpDir = new TemporaryDirectory();
        using var otherDir = new TemporaryDirectory();
        Environment.SetEnvironmentVariable(VariableName, tmpDir.DirectoryPath);
        try
        {
            // Act: resolve the variable reference
            var result = PathResolver.ResolveExistingDirectory("${" + VariableName + "}", otherDir.DirectoryPath, "--root option");

            // Assert: the variable's directory is returned
            Assert.Equal(Path.GetFullPath(tmpDir.DirectoryPath), result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>Test that a missing directory is an error naming the directory and the description.</summary>
    [Fact]
    public void PathResolver_ResolveExistingDirectory_MissingDirectory_ThrowsNamingDirectory()
    {
        // Arrange: a base directory without the requested sub-directory
        using var tmpDir = new TemporaryDirectory();
        var full = Path.Combine(tmpDir.DirectoryPath, "missing");

        // Act: resolve the missing directory
        var ex = Assert.Throws<InvalidOperationException>(
            () => PathResolver.ResolveExistingDirectory("missing", tmpDir.DirectoryPath, "--root option"));

        // Assert: the message names the full path, the description and the value
        Assert.Equal($"Directory '{full}' specified by --root option 'missing' does not exist.", ex.Message);
    }

    /// <summary>Test that a path naming a file is not accepted as a directory.</summary>
    [Fact]
    public void PathResolver_ResolveExistingDirectory_PathIsFile_Throws()
    {
        // Arrange: a file rather than a directory
        using var tmpDir = new TemporaryDirectory();
        File.WriteAllText(tmpDir.GetFilePath("file.txt"), "x");

        // Act / Assert: the file is rejected
        Assert.Throws<InvalidOperationException>(
            () => PathResolver.ResolveExistingDirectory("file.txt", tmpDir.DirectoryPath, "root key"));
    }

    /// <summary>Test that wildcard characters are rejected in a directory.</summary>
    [Fact]
    public void PathResolver_ResolveExistingDirectory_Wildcard_Throws()
    {
        // Arrange: a base directory
        using var tmpDir = new TemporaryDirectory();

        // Act: resolve a wildcard directory
        var ex = Assert.Throws<InvalidOperationException>(
            () => PathResolver.ResolveExistingDirectory("sub*", tmpDir.DirectoryPath, "root key"));

        // Assert: the message names the description (capitalized) and the value
        Assert.Equal("Root key 'sub*' must not contain wildcard characters.", ex.Message);
    }

    /// <summary>Test that a literal path with a null character is reported with the description and value.</summary>
    [Fact]
    public void PathResolver_ResolvePath_NullCharacter_ThrowsInvalidOperationExceptionNamingPath()
    {
        // Arrange: a path containing a null character, which is invalid on every platform
        using var tmpDir = new TemporaryDirectory();
        const string path = "bad\0name.md";

        // Act: resolve the malformed path
        var ex = Assert.Throws<InvalidOperationException>(
            () => PathResolver.ResolvePath(path, tmpDir.DirectoryPath, "output path"));

        // Assert: the message names the description and the value, and keeps the original exception
        Assert.StartsWith($"Invalid output path '{path}': ", ex.Message);
        Assert.IsAssignableFrom<ArgumentException>(ex.InnerException);
    }

    /// <summary>Test that a directory path with a null character is reported with the description and value.</summary>
    [Fact]
    public void PathResolver_ResolveExistingDirectory_NullCharacter_ThrowsInvalidOperationExceptionNamingPath()
    {
        // Arrange: a directory path containing a null character, which is invalid on every platform
        using var tmpDir = new TemporaryDirectory();
        const string path = "bad\0dir";

        // Act: resolve the malformed directory
        var ex = Assert.Throws<InvalidOperationException>(
            () => PathResolver.ResolveExistingDirectory(path, tmpDir.DirectoryPath, "--root option"));

        // Assert: the message names the description and the value
        Assert.StartsWith($"Invalid --root option '{path}': ", ex.Message);
    }

    /// <summary>
    ///     Test that a directory resolved as text is returned as its normalized full path even when it does not exist.
    /// </summary>
    [Fact]
    public void PathResolver_ResolveDirectory_MissingDirectory_ReturnsNormalizedFullPath()
    {
        // Arrange: a base directory without the requested sub-directory
        using var tmpDir = new TemporaryDirectory();
        var expected = Path.Combine(tmpDir.DirectoryPath, "missing");

        // Act: resolve a missing directory reached through '..' segments
        var result = PathResolver.ResolveDirectory("a/../missing", tmpDir.DirectoryPath, "root key");

        // Assert: the normalized full path is returned and nothing was created
        Assert.Equal(expected, result);
        Assert.False(Directory.Exists(expected));
    }

    /// <summary>Test that ${VAR} references in a directory resolved as text are expanded.</summary>
    [Fact]
    public void PathResolver_ResolveDirectory_EnvironmentVariable_Expanded()
    {
        // Arrange: a variable naming a folder below the base directory
        using var tmpDir = new TemporaryDirectory();
        Environment.SetEnvironmentVariable(VariableName, "shared");
        try
        {
            // Act: resolve the variable reference
            var result = PathResolver.ResolveDirectory("${" + VariableName + "}/docs", tmpDir.DirectoryPath, "root key");

            // Assert: the variable is expanded and the path rooted at the base directory
            Assert.Equal(Path.Combine(tmpDir.DirectoryPath, "shared", "docs"), result);
        }
        finally
        {
            Environment.SetEnvironmentVariable(VariableName, null);
        }
    }

    /// <summary>Test that wildcard characters are rejected in a directory resolved as text.</summary>
    [Fact]
    public void PathResolver_ResolveDirectory_Wildcard_Throws()
    {
        // Arrange: a base directory
        using var tmpDir = new TemporaryDirectory();

        // Act: resolve a wildcard directory
        var ex = Assert.Throws<InvalidOperationException>(
            () => PathResolver.ResolveDirectory("sub?", tmpDir.DirectoryPath, "root key"));

        // Assert: the message names the description (capitalized) and the value
        Assert.Equal("Root key 'sub?' must not contain wildcard characters.", ex.Message);
    }

    /// <summary>Test that path-validation exceptions are classified as invalid-path exceptions.</summary>
    [Fact]
    public void PathResolver_IsInvalidPathException_PathValidationExceptions_ReturnsTrue()
    {
        // Arrange / Act / Assert: the path-validation exceptions are recognized
        Assert.True(PathResolver.IsInvalidPathException(new ArgumentException("bad")));
        Assert.True(PathResolver.IsInvalidPathException(new NotSupportedException("bad")));
        Assert.True(PathResolver.IsInvalidPathException(new PathTooLongException("bad")));
    }

    /// <summary>Test that programming errors and other I/O errors are not classified as invalid-path exceptions.</summary>
    [Fact]
    public void PathResolver_IsInvalidPathException_OtherExceptions_ReturnsFalse()
    {
        // Arrange: a genuine null-argument exception raised by the resolver itself
        var nullArgument = Assert.Throws<ArgumentNullException>(() => PathResolver.ResolvePath(null!, "base", "d"));

        // Act / Assert: null arguments and general I/O errors are not invalid paths
        Assert.False(PathResolver.IsInvalidPathException(nullArgument));
        Assert.False(PathResolver.IsInvalidPathException(new IOException("io")));
        Assert.False(PathResolver.IsInvalidPathException(new InvalidOperationException("op")));
    }
}
