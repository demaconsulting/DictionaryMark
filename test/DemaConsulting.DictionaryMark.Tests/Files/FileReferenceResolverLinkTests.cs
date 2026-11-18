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

namespace DemaConsulting.DictionaryMark.Tests.Files;

/// <summary>
///     Unit tests for <see cref="FileReferenceResolver"/> with real links: a directory link is followed by the default
///     policy, kept inside by a roots policy and refused by the no-symlinks policy. Directory links fall back to a
///     junction on Windows, which needs no privilege, so these tests run on every system; only the file-link scenario
///     skips where the host cannot create file links.
/// </summary>
public class FileReferenceResolverLinkTests
{

    /// <summary>
    ///     Builds a tree with a root, a real folder inside it, a folder outside it, and two directory links in the root:
    ///     <c>alias</c> (to the inside folder) and <c>escape</c> (to the outside folder).
    /// </summary>
    /// <param name="directory">The temporary directory.</param>
    /// <returns>The full path of the root.</returns>
    private static string BuildLinkedTree(FileTestDirectory directory)
    {
        directory.Write("root/real/x.txt", "real");
        directory.Write("outside/x.txt", "outside");
        FileTestSupport.CreateDirectoryLinkOrSkip(directory.Combine("root/alias"), directory.Combine("root/real"));
        FileTestSupport.CreateDirectoryLinkOrSkip(directory.Combine("root/escape"), directory.Combine("outside"));
        return directory.Combine("root");
    }

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
    ///     Validates that the default policy follows a directory link and reports the real path of the target.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_DirectoryLinkUnderAllowAll_IsFollowed()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var root = BuildLinkedTree(directory);
        var resolver = new FileReferenceResolver(new FileReferenceOptions { BaseDirectory = root });

        // Act
        var file = resolver.Resolve("escape/x.txt");

        // Assert: the key is the path as written, the real path is the target
        Assert.Equal("outside", Text(file));
        Assert.Equal(Path.GetFullPath(Path.Combine(root, "escape", "x.txt")), file!.Key);
        Assert.True(FileTestSupport.SamePath(Path.Join(FileTestSupport.Canonical(directory.Combine("outside")), "x.txt"), file.RealPath!));
    }

    /// <summary>
    ///     Validates that the no-symlinks policy refuses a file reached through a directory link, whatever its target.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_DirectoryLinkUnderNoSymlinks_IsAccessDenied()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var root = BuildLinkedTree(directory);
        var resolver = new FileReferenceResolver(
            new FileReferenceOptions { BaseDirectory = root, FileAccess = FileAccessPolicy.NoSymlinks });

        // Act
        var inside = Assert.Throws<FileReferenceException>(() => resolver.Resolve("alias/x.txt"));
        var outside = Assert.Throws<FileReferenceException>(() => resolver.Resolve("escape/x.txt"));
        var real = resolver.Resolve("real/x.txt");

        // Assert
        Assert.Equal(FileReferenceFailureKind.AccessDenied, inside.Kind);
        Assert.Equal(FileReferenceFailureKind.AccessDenied, outside.Kind);
        Assert.Equal("no-symlinks", outside.PolicyName);
        Assert.Equal("real", Text(real));
    }

    /// <summary>
    ///     Validates that a roots policy follows a link that stays inside the roots.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_DirectoryLinkStayingInsideTheRoots_IsAllowed()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var root = BuildLinkedTree(directory);
        var resolver = new FileReferenceResolver(
            new FileReferenceOptions { BaseDirectory = root, FileAccess = FileAccessPolicy.UnderRoots(root) });

        // Act
        var file = resolver.Resolve("alias/x.txt");

        // Assert
        Assert.Equal("real", Text(file));
    }

    /// <summary>
    ///     Validates that a roots policy refuses a link that leads out of the roots, after the open and before the read.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_DirectoryLinkLeavingTheRoots_IsAccessDenied()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var root = BuildLinkedTree(directory);
        var resolver = new FileReferenceResolver(
            new FileReferenceOptions { BaseDirectory = root, FileAccess = FileAccessPolicy.UnderRoots(root) });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve("escape/x.txt"));

        // Assert: the written path was inside, so the denial is about the real path of the opened file
        Assert.Equal(FileReferenceFailureKind.AccessDenied, ex.Kind);
        Assert.Equal("its real path is outside the allowed roots", ex.Reason);
        Assert.NotNull(ex.RealPath);
    }

    /// <summary>
    ///     Validates that the no-symlinks policy trusts a base directory that is itself reached through a link: only links
    ///     below the folder a reference starts from count.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_LinkedBaseDirectoryUnderNoSymlinks_IsTrusted()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var root = BuildLinkedTree(directory);
        var resolver = new FileReferenceResolver(
            new FileReferenceOptions { BaseDirectory = Path.Combine(root, "alias"), FileAccess = FileAccessPolicy.NoSymlinks });

        // Act
        var file = resolver.Resolve("x.txt");

        // Assert
        Assert.Equal("real", Text(file));
    }

    /// <summary>
    ///     Validates that the no-symlinks policy refuses a file that is itself a symbolic link. File links need a
    ///     privilege on Windows, so the test skips there when the host cannot create one; the same decision is covered
    ///     on every system by the injected real-path tests.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_Resolve_FileSymbolicLinkUnderNoSymlinks_IsAccessDeniedOrCapabilitySkipped()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("root/real.txt", "real");
        if (!FileLinkSupport.TryCreateFileLink(directory.Combine("root/link.txt"), directory.Combine("root/real.txt"), out var reason))
        {
            Assert.Skip("File symbolic links need Developer Mode or SeCreateSymbolicLinkPrivilege on Windows: " + reason);
        }

        var strict = new FileReferenceResolver(
            new FileReferenceOptions { BaseDirectory = directory.Combine("root"), FileAccess = FileAccessPolicy.NoSymlinks });
        var permissive = new FileReferenceResolver(new FileReferenceOptions { BaseDirectory = directory.Combine("root") });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => strict.Resolve("link.txt"));
        var followed = permissive.Resolve("link.txt");

        // Assert
        Assert.Equal(FileReferenceFailureKind.AccessDenied, ex.Kind);
        Assert.Equal("real", Text(followed));
    }
}
