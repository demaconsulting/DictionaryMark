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
///     Unit tests for the output-path check of <see cref="FileReferenceResolver"/>: an output gets the read checks in
///     the read order (text rules, pre-check, open and inspection of an existing entry, network target, real path,
///     policy), the real path of an output that does not exist yet comes from its deepest existing folder, and nothing
///     is ever created, written or read.
/// </summary>
public class FileReferenceResolverOutputTests
{
    /// <summary>
    ///     Creates a resolver.
    /// </summary>
    /// <param name="baseDirectory">The base directory.</param>
    /// <param name="policy">The policy, or <see langword="null"/> for the default.</param>
    /// <param name="hooks">The open seams, or <see langword="null"/>.</param>
    /// <returns>The resolver.</returns>
    private static FileReferenceResolver Create(string baseDirectory, FileAccessPolicy? policy = null, FileOpenHooks? hooks = null) =>
        new(new FileReferenceOptions { BaseDirectory = baseDirectory, FileAccess = policy ?? FileAccessPolicy.AllowAll }, hooks);

    /// <summary>
    ///     Validates that a null output path is an argument error.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_NullPath_Throws()
    {
        // Arrange
        var resolver = Create(".");

        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => resolver.CheckOutputPath(null!));
    }

    /// <summary>
    ///     Validates that an output inside the roots, in folders that do not exist yet, is allowed under roots and
    ///     no-symlinks combined, and that the check creates nothing.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_InsideRootsWithMissingParents_IsAllowedAndCreatesNothing()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var root = Directory.CreateDirectory(directory.Combine("root")).FullName;
        var policy = FileAccessPolicy.Combine(FileAccessPolicy.UnderRoots(root), FileAccessPolicy.NoSymlinks);
        var resolver = Create(root, policy);

        // Act
        var error = Record.Exception(() => resolver.CheckOutputPath(Path.Combine(root, "new", "sub", "out.md")));

        // Assert
        Assert.Null(error);
        Assert.False(Directory.Exists(Path.Combine(root, "new")));
    }

    /// <summary>
    ///     Validates that an existing regular output file inside the roots is allowed and left unchanged.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_ExistingFileInsideRoots_IsAllowedAndUnchanged()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var output = directory.Write("root/out.md", "old");
        var root = directory.Combine("root");
        var resolver = Create(root, FileAccessPolicy.Combine(FileAccessPolicy.UnderRoots(root), FileAccessPolicy.NoSymlinks));

        // Act
        var error = Record.Exception(() => resolver.CheckOutputPath(output));

        // Assert
        Assert.Null(error);
        Assert.Equal("old", File.ReadAllText(output));
    }

    /// <summary>
    ///     Validates that an output outside the roots is denied by the pre-check, before anything is opened.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_OutsideRoots_IsAccessDeniedBeforeAnyOpen()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var root = Directory.CreateDirectory(directory.Combine("root")).FullName;
        var opened = new List<string>();
        var resolver = Create(root, FileAccessPolicy.UnderRoots(root), new FileOpenHooks { BeforeOpen = opened.Add });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.CheckOutputPath(directory.Combine("out/out.md")));

        // Assert
        Assert.Equal(FileReferenceFailureKind.AccessDenied, ex.Kind);
        Assert.Equal("roots", ex.PolicyName);
        Assert.Null(ex.RealPath);
        Assert.Empty(opened);
        Assert.False(Directory.Exists(directory.Combine("out")));
    }

    /// <summary>
    ///     Validates that an output path that breaks a never-list text rule is refused under the default policy before
    ///     anything is opened.
    /// </summary>
    /// <param name="path">The output path.</param>
    /// <param name="kind">The expected failure kind.</param>
    [Theory]
    [InlineData("\\\\server\\share\\out.md", FileReferenceFailureKind.NetworkPath)]
    [InlineData("//server/share/out.md", FileReferenceFailureKind.NetworkPath)]
    [InlineData("\\\\.\\pipe\\out", FileReferenceFailureKind.InvalidPath)]
    [InlineData("out.md:stream", FileReferenceFailureKind.InvalidPath)]
    [InlineData("out\u0001.md", FileReferenceFailureKind.ControlCharacter)]
    public void FileReferenceResolver_CheckOutputPath_NeverListText_IsRefusedUnderAllowAll(string path, FileReferenceFailureKind kind)
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var opened = new List<string>();
        var resolver = Create(directory.Path, null, new FileOpenHooks { BeforeOpen = opened.Add });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.CheckOutputPath(path));

        // Assert
        Assert.Equal(kind, ex.Kind);
        Assert.Empty(opened);
    }

    /// <summary>
    ///     Validates on Windows that an output with a reserved device name is refused under the default policy before
    ///     anything is opened.
    /// </summary>
    /// <param name="name">The output name.</param>
    [Theory]
    [InlineData("NUL")]
    [InlineData("COM1.md")]
    public void FileReferenceResolver_CheckOutputPath_DeviceName_IsInvalidPathOnWindows(string name)
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.WindowsOnly);
        }

        using var directory = new FileTestDirectory();
        var opened = new List<string>();
        var resolver = Create(directory.Path, null, new FileOpenHooks { BeforeOpen = opened.Add });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.CheckOutputPath(Path.Combine(directory.Path, name)));

        // Assert
        Assert.Equal(FileReferenceFailureKind.InvalidPath, ex.Kind);
        Assert.Empty(opened);
    }

    /// <summary>
    ///     Validates that an existing directory at the output path is refused as not a regular file.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_ExistingDirectory_IsNotARegularFile()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var folder = Directory.CreateDirectory(directory.Combine("out.md")).FullName;
        var resolver = Create(directory.Path);

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.CheckOutputPath(folder));

        // Assert
        Assert.Equal(FileReferenceFailureKind.NotARegularFile, ex.Kind);
    }

    /// <summary>
    ///     Validates that an existing output reached through a directory link is denied under no-symlinks.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_ExistingFileThroughDirectoryLinkUnderNoSymlinks_IsAccessDenied()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("root/real/out.md", "old");
        FileTestSupport.CreateDirectoryLinkOrSkip(directory.Combine("root/alias"), directory.Combine("root/real"));
        var resolver = Create(directory.Combine("root"), FileAccessPolicy.NoSymlinks);

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.CheckOutputPath(directory.Combine("root/alias/out.md")));

        // Assert
        Assert.Equal(FileReferenceFailureKind.AccessDenied, ex.Kind);
        Assert.Equal("no-symlinks", ex.PolicyName);
        Assert.Equal("old", File.ReadAllText(directory.Combine("root/real/out.md")));
    }

    /// <summary>
    ///     Validates that an output that does not exist yet, below a directory link inside the roots that leads outside
    ///     them, is denied: its real path is the real location of the linked folder.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_MissingFileUnderParentLinkLeavingTheRoots_IsAccessDenied()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var root = Directory.CreateDirectory(directory.Combine("root")).FullName;
        Directory.CreateDirectory(directory.Combine("outside"));
        FileTestSupport.CreateDirectoryLinkOrSkip(directory.Combine("root/link"), directory.Combine("outside"));
        var resolver = Create(root, FileAccessPolicy.UnderRoots(root));

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.CheckOutputPath(directory.Combine("root/link/new/out.md")));

        // Assert
        Assert.Equal(FileReferenceFailureKind.AccessDenied, ex.Kind);
        Assert.Equal("roots", ex.PolicyName);
        Assert.False(Directory.Exists(directory.Combine("outside/new")));
    }

    /// <summary>
    ///     Validates that an output that does not exist yet, below a directory link inside the base directory, is denied
    ///     under no-symlinks.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_MissingFileUnderDirectoryLinkUnderNoSymlinks_IsAccessDenied()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        Directory.CreateDirectory(directory.Combine("root/real"));
        FileTestSupport.CreateDirectoryLinkOrSkip(directory.Combine("root/alias"), directory.Combine("root/real"));
        var resolver = Create(directory.Combine("root"), FileAccessPolicy.NoSymlinks);

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.CheckOutputPath(directory.Combine("root/alias/out.md")));

        // Assert
        Assert.Equal(FileReferenceFailureKind.AccessDenied, ex.Kind);
        Assert.Equal("no-symlinks", ex.PolicyName);
    }

    /// <summary>
    ///     Validates that a dangling link at the output path is an existing output that cannot be opened, so it is
    ///     refused under every policy (the default included) and its target is never created. The link is a file
    ///     symbolic link, or, where the host cannot create one, a directory link (a junction on Windows) whose target
    ///     was removed; the test skips when the host can create neither.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_DanglingLinkAtOutput_IsRefusedUnderEveryPolicy()
    {
        // Arrange: root/out.md is a link to a location outside that does not exist
        using var directory = new FileTestDirectory();
        var root = Directory.CreateDirectory(directory.Combine("root")).FullName;
        Directory.CreateDirectory(directory.Combine("outside"));
        var target = directory.Combine("outside/target.md");
        var link = Path.Combine(root, "out.md");
        if (!FileLinkSupport.TryCreateFileLink(link, target, out _))
        {
            Directory.CreateDirectory(target);
            FileTestSupport.CreateDirectoryLinkOrSkip(link, target);
            Directory.Delete(target);
        }

        var permissive = Create(root);
        var confined = Create(root, FileAccessPolicy.Combine(FileAccessPolicy.UnderRoots(root), FileAccessPolicy.NoSymlinks));

        // Act
        var permissiveError = Assert.Throws<FileReferenceException>(() => permissive.CheckOutputPath(link));
        var confinedError = Assert.Throws<FileReferenceException>(() => confined.CheckOutputPath(link));

        // Assert: refused as an existing entry that cannot be opened (or, for a directory link on Windows, as an
        // entry that is not a regular file)
        FileReferenceFailureKind[] refused = [FileReferenceFailureKind.CannotRead, FileReferenceFailureKind.NotARegularFile];
        Assert.Contains(permissiveError.Kind, refused);
        Assert.Contains(confinedError.Kind, refused);
        Assert.False(File.Exists(target));
        Assert.False(Directory.Exists(target));
    }

    /// <summary>
    ///     Validates that a restrictive policy fails closed when the real path of an output cannot be determined, for
    ///     an output that does not exist yet and for an existing one, while the default policy allows both.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_RealPathUnavailable_IsRealPathUnavailable()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var root = Directory.CreateDirectory(directory.Combine("root")).FullName;
        var existing = directory.Write("root/old.md", "old");
        var unknown = new FileOpenHooks { RealPathOf = _ => null, DirectoryLocationOf = _ => null };
        var restrictive = Create(root, FileAccessPolicy.UnderRoots(root), unknown);
        var permissive = Create(root, null, unknown);

        // Act
        var missingError = Assert.Throws<FileReferenceException>(() => restrictive.CheckOutputPath(Path.Combine(root, "new.md")));
        var existingError = Assert.Throws<FileReferenceException>(() => restrictive.CheckOutputPath(existing));
        var permissiveError = Record.Exception(() => permissive.CheckOutputPath(Path.Combine(root, "new.md")));

        // Assert
        Assert.Equal(FileReferenceFailureKind.RealPathUnavailable, missingError.Kind);
        Assert.Equal("roots", missingError.PolicyName);
        Assert.Equal(FileReferenceFailureKind.RealPathUnavailable, existingError.Kind);
        Assert.Null(permissiveError);
    }

    /// <summary>
    ///     Validates on Windows that an output that does not exist yet, whose deepest existing folder lies on a network
    ///     share while the path as written is local, is refused as a network path under the default policy.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_ParentOnNetworkShare_IsNetworkPath()
    {
        // Arrange
        if (!OperatingSystem.IsWindows())
        {
            Assert.Skip(FileTestSupport.WindowsOnly);
        }

        using var directory = new FileTestDirectory();
        var resolver = Create(directory.Path, null, new FileOpenHooks { DirectoryLocationOf = _ => @"\\server\share\docs" });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.CheckOutputPath(directory.Combine("out.md")));

        // Assert
        Assert.Equal(FileReferenceFailureKind.NetworkPath, ex.Kind);
        Assert.Equal(@"\\server\share\docs\out.md", ex.RealPath);
    }

    /// <summary>
    ///     Validates that an existing output whose open fails is refused as <c>CannotRead</c>, with the cause kept.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_ExistingFileOpenFailure_IsCannotRead()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var output = directory.Write("out.md", "old");
        var resolver = Create(directory.Path, null, new FileOpenHooks { Open = _ => throw new IOException("locked") });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.CheckOutputPath(output));

        // Assert
        Assert.Equal(FileReferenceFailureKind.CannotRead, ex.Kind);
        Assert.IsType<IOException>(ex.InnerException);
    }

    /// <summary>
    ///     Validates that a resolver without a base directory cannot check an output path.
    /// </summary>
    [Fact]
    public void FileReferenceResolver_CheckOutputPath_NoBaseDirectory_ThrowsInvalidOperationException()
    {
        // Arrange
        var resolver = new FileReferenceResolver(new FileReferenceOptions());

        // Act / Assert
        Assert.Throws<InvalidOperationException>(() => resolver.CheckOutputPath(Path.GetFullPath("out.md")));
    }
}
