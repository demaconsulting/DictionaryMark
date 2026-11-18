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
///     Unit tests for the FileAccessPolicy unit (FileAccessPolicy, FileAccessMode and FileAccessDeniedException).
/// </summary>
public class FileAccessPolicyTests
{
    /// <summary>Content written to the dictionary files read by these tests.</summary>
    private const string Content = "Alpha: First\n";

    /// <summary>Test that every valid --file-access value is parsed, in any letter case and order.</summary>
    /// <param name="text">The option value.</param>
    /// <param name="expected">The expected mode, as written on the command line.</param>
    [Theory]
    [InlineData("any", "any")]
    [InlineData("ANY", "any")]
    [InlineData("roots", "roots")]
    [InlineData("Roots", "roots")]
    [InlineData("no-symlinks", "no-symlinks")]
    [InlineData("NO-SYMLINKS", "no-symlinks")]
    [InlineData("roots,no-symlinks", "roots,no-symlinks")]
    [InlineData("no-symlinks,roots", "roots,no-symlinks")]
    [InlineData(" Roots , No-Symlinks ", "roots,no-symlinks")]
    public void FileAccessPolicy_ParseMode_ValidValues_Parsed(string text, string expected)
    {
        // Act
        var mode = FileAccessPolicy.ParseMode(text);

        // Assert
        Assert.Equal(expected, FileAccessPolicy.NameOf(mode));
    }

    /// <summary>Test that an invalid --file-access value is rejected with a message listing the modes.</summary>
    /// <param name="text">The option value.</param>
    [Theory]
    [InlineData("all")]
    [InlineData("any,roots")]
    [InlineData("")]
    [InlineData("roots,,no-symlinks")]
    [InlineData("roots;no-symlinks")]
    [InlineData("roots,roots")]
    public void FileAccessPolicy_ParseMode_InvalidValues_ThrowsListingModes(string text)
    {
        // Act / Assert: the message names the value and lists every mode
        var ex = Assert.Throws<ArgumentException>(() => FileAccessPolicy.ParseMode(text));
        Assert.Equal(
            $"'{text}' is not a value of --file-access: use any (the default), roots, no-symlinks, or roots,no-symlinks",
            ex.Message);
    }

    /// <summary>Test that mode any reads a file anywhere.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_AnyModeOutsideFolders_ReturnsContent()
    {
        // Arrange: a file in a temporary folder
        using var tmpDir = new TemporaryDirectory();
        var file = WriteFile(tmpDir, "outside/a.yaml");

        // Act
        var text = FileAccessPolicy.Any.ReadAllText(file, file);

        // Assert
        Assert.Equal(Content, text);
    }

    /// <summary>Test that mode any creates missing folders and writes the output.</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_AnyMode_CreatesFoldersAndWrites()
    {
        // Arrange: an output inside folders that do not exist
        using var tmpDir = new TemporaryDirectory();
        var output = PathIn(tmpDir, "new/nested/out.md");

        // Act
        FileAccessPolicy.Any.WriteAllText("new/nested/out.md", output, "# Out\n");

        // Assert
        Assert.Equal("# Out\n", File.ReadAllText(output));
    }

    /// <summary>Test that hostile path texts are on the never-accessed list with the right reason.</summary>
    /// <param name="path">The path text.</param>
    /// <param name="reason">A fragment of the expected reason.</param>
    [Theory]
    [InlineData("http://example.com/a.yaml", "it is a URL or URI")]
    [InlineData("https://example.com/a.yaml", "it is a URL or URI")]
    [InlineData("ftp://host/a.yaml", "it is a URL or URI")]
    [InlineData("file:///tmp/a.yaml", "it is a URL or URI")]
    [InlineData("file://server/share/a.yaml", "it is a file: URI with a host")]
    [InlineData(@"\\server\share\a.yaml", "it is a UNC or network path")]
    [InlineData("//server/share/a.yaml", "it is a UNC or network path")]
    [InlineData(@"\\.\pipe\x", "it is a device path")]
    [InlineData(@"\\?\C:\a.yaml", "it is a device path")]
    public void FileAccessPolicy_DescribeNeverAccessed_HostileText_ReturnsReason(string path, string reason)
    {
        // Act
        var result = FileAccessPolicy.DescribeNeverAccessed(path);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith(reason, result);
        Assert.EndsWith("is never accessed", result);
    }

    /// <summary>
    ///     Test that a colon other than the drive of an absolute path on Windows is on the never-accessed list, on
    ///     every platform.
    /// </summary>
    /// <param name="path">The path text.</param>
    [Theory]
    [InlineData("a.yaml:stream")]
    [InlineData("C:a.yaml")]
    [InlineData("docs/terms.yaml:hidden")]
    [InlineData("terms:v1.yaml")]
    [InlineData("urn:x")]
    [InlineData("file:/tmp/a.yaml")]
    public void FileAccessPolicy_DescribeNeverAccessed_Colon_ReturnsReason(string path)
    {
        // Act
        var result = FileAccessPolicy.DescribeNeverAccessed(path);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("it contains a colon", result);
        Assert.EndsWith("is never accessed", result);
    }

    /// <summary>Test that on Windows a path in the NT object namespace is on the never-accessed list.</summary>
    /// <param name="path">The path text.</param>
    [Theory]
    [InlineData(@"\??\C:\a.yaml")]
    public void FileAccessPolicy_DescribeNeverAccessed_NtNamespaceOnWindows_ReturnsReason(string path)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The NT object namespace rule applies on Windows only.");

        // Act
        var result = FileAccessPolicy.DescribeNeverAccessed(path);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("it is a device path", result);
        Assert.EndsWith("is never accessed", result);
    }

    /// <summary>
    ///     Test that on Linux and macOS a drive path is on the never-accessed list for its colon, because those
    ///     systems have no drives.
    /// </summary>
    /// <param name="path">The path text.</param>
    [Theory]
    [InlineData("C:/a.yaml")]
    [InlineData(@"C:\a.yaml")]
    public void FileAccessPolicy_DescribeNeverAccessed_DrivePathOnLinuxAndMac_ReturnsColonReason(string path)
    {
        Assert.SkipUnless(
            OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(),
            "A drive path is an ordinary absolute path on Windows; it is refused on Linux and macOS only.");

        // Act
        var result = FileAccessPolicy.DescribeNeverAccessed(path);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("it contains a colon", result);
        Assert.EndsWith("is never accessed", result);
    }

    /// <summary>
    ///     Test that on Linux and macOS the Windows <c>\??\</c> namespace and a path under /dev are ordinary text (a
    ///     device is refused by its handle type).
    /// </summary>
    /// <param name="path">The path text.</param>
    [Theory]
    [InlineData("/??/a.yaml")]
    [InlineData("/dev/null")]
    public void FileAccessPolicy_DescribeNeverAccessed_DeviceTextOnLinuxAndMac_ReturnsNull(string path)
    {
        Assert.SkipUnless(
            OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(),
            "The Windows namespace and /dev text rules are tested on Linux and macOS only.");

        // Act
        var result = FileAccessPolicy.DescribeNeverAccessed(path);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    ///     Test that a written path containing a colon is denied before it is opened, on every platform, and that
    ///     nothing is created.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ColonInWrittenPath_DeniedBeforeOpenNothingWritten()
    {
        // Arrange: an empty allowed folder (no colon-named file is created, which on Windows would be a stream)
        using var tmpDir = new TemporaryDirectory();
        var policy = Roots(tmpDir, "allowed");
        var allowed = PathIn(tmpDir, "allowed");

        // Act: a missing input would fail with FileNotFoundException if it were opened
        var read = Assert.Throws<FileAccessDeniedException>(
            () => policy.ReadAllText("terms:v1.yaml", PathIn(tmpDir, "allowed/terms:v1.yaml")));
        var write = Assert.Throws<FileAccessDeniedException>(
            () => policy.WriteAllText("sub/out:1.md", PathIn(tmpDir, "allowed/sub/out:1.md"), "# Out\n"));

        // Assert: both are denied for the colon and the allowed folder is still empty
        Assert.StartsWith("it contains a colon", read.Reason);
        Assert.StartsWith("it contains a colon", write.Reason);
        Assert.False(Directory.Exists(PathIn(tmpDir, "allowed/sub")));
        Assert.Empty(Directory.EnumerateFileSystemEntries(allowed));
    }

    /// <summary>Test that ordinary relative, parent-segment, glob and absolute paths are not on the list.</summary>
    [Fact]
    public void FileAccessPolicy_DescribeNeverAccessed_OrdinaryPaths_ReturnsNull()
    {
        // Arrange: an absolute path in the platform's own form (a drive path on Windows)
        var absolute = Path.GetFullPath("terms.yaml");

        // Act / Assert
        Assert.Null(FileAccessPolicy.DescribeNeverAccessed("terms.yaml"));
        Assert.Null(FileAccessPolicy.DescribeNeverAccessed("../shared/*.yaml"));
        Assert.Null(FileAccessPolicy.DescribeNeverAccessed("sub/dir/a.yaml"));
        Assert.Null(FileAccessPolicy.DescribeNeverAccessed("${HOME}/a.yaml"));
        Assert.Null(FileAccessPolicy.DescribeNeverAccessed(absolute));
    }

    /// <summary>Test that a Windows reserved device name in any component is denied before it is opened.</summary>
    /// <param name="name">The device name component.</param>
    [Theory]
    [InlineData("NUL")]
    [InlineData("con.yaml")]
    [InlineData("COM1")]
    public void FileAccessPolicy_ReadAllText_WindowsDeviceName_Denied(string name)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Windows reserved device names are tested on Windows only.");

        // Arrange: a path whose last component is a device name
        using var tmpDir = new TemporaryDirectory();
        var path = Path.Combine(tmpDir.DirectoryPath, name);

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => FileAccessPolicy.Any.ReadAllText(name, path));
        Assert.Equal("it is a device path, which is never accessed", ex.Reason);
    }

    /// <summary>Test that a device node is read only to be denied as not a regular file on Linux and macOS.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_DevNull_DeniedAsNotRegular()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(), "/dev is tested on Linux and macOS only.");

        // Act / Assert: the non-blocking open succeeds and the handle type refuses the device
        var ex = Assert.Throws<FileAccessDeniedException>(() => FileAccessPolicy.Any.ReadAllText("/dev/null", "/dev/null"));
        Assert.StartsWith("it is not a regular file", ex.Reason);
    }

    /// <summary>Test that an output under /dev is denied on Linux and macOS before anything is opened.</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_DevPath_DeniedBeforeOpen()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(), "/dev is tested on Linux and macOS only.");

        // Arrange: an inspector that records any open
        var opened = false;
        var policy = FileAccessPolicy.Create(
            FileAccessMode.None,
            [],
            null,
            handle =>
            {
                opened = true;
                return RealPath.Inspect(handle);
            });

        // Act
        var direct = Record.Exception(() => policy.WriteAllText("/dev/null", "/dev/null", "x"));
        var rooted = Record.Exception(() => policy.WriteAllText("/tmp/../dev/x.md", Path.GetFullPath("/tmp/../dev/x.md"), "x"));

        // Assert
        Assert.Equal("it is a device path, which is never accessed", Assert.IsType<FileAccessDeniedException>(direct).Reason);
        Assert.Equal("it is a device path, which is never accessed", Assert.IsType<FileAccessDeniedException>(rooted).Reason);
        Assert.False(opened);
        Assert.False(File.Exists("/dev/x.md"));
    }

    /// <summary>
    ///     Test that on Linux and macOS an output in a folder whose real path is /dev is denied before the open.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_FolderLinkToDev_DeniedBeforeOpen()
    {
        Assert.SkipUnless(
            OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(),
            "/dev is tested on Linux and macOS only.");

        // Arrange: a folder link to /dev (no privilege needed on Linux and macOS) and an inspector recording any open
        using var tmpDir = new TemporaryDirectory();
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "devices"), "/dev");
        var opened = false;
        var policy = FileAccessPolicy.Create(
            FileAccessMode.None,
            [],
            null,
            handle =>
            {
                opened = true;
                return RealPath.Inspect(handle);
            });

        // Act: an existing device through the link, and a missing folder below the link
        var direct = Record.Exception(
            () => policy.WriteAllText("devices/null", PathIn(tmpDir, "devices/null"), "x"));
        var nested = Record.Exception(
            () => policy.WriteAllText("devices/new/x.md", PathIn(tmpDir, "devices/new/x.md"), "x"));

        // Assert: both are denied with the device reason in mode any, and nothing is opened or created
        var ex = Assert.IsType<FileAccessDeniedException>(direct);
        Assert.Equal("it is a device path, which is never accessed", ex.Reason);
        Assert.Equal("/dev/null", ex.RealPath);
        Assert.Equal(
            "it is a device path, which is never accessed",
            Assert.IsType<FileAccessDeniedException>(nested).Reason);
        Assert.False(opened);
        Assert.False(Directory.Exists("/dev/new"));
    }

    /// <summary>Test that a named pipe is denied as not a regular file without blocking the read.</summary>
    [Fact]
    public async Task FileAccessPolicy_ReadAllText_Fifo_DeniedWithoutBlocking()
    {
        // Arrange: a named pipe with no writer (skipped where pipes cannot be created)
        using var tmpDir = new TemporaryDirectory();
        var fifo = PathIn(tmpDir, "pipe.yaml");
        LinkSupport.CreateFifoOrSkip(fifo);

        // Act: the read must finish promptly (a blocked open times out)
        var result = await Task.Run(() => Record.Exception(() => FileAccessPolicy.Any.ReadAllText(fifo, fifo)), TestContext.Current.CancellationToken)
            .WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // Assert
        var ex = Assert.IsType<FileAccessDeniedException>(result);
        Assert.StartsWith("it is not a regular file", ex.Reason);
    }

    /// <summary>Test that a folder is never read.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_Directory_Denied()
    {
        // Arrange: a folder
        using var tmpDir = new TemporaryDirectory();
        var folder = Directory.CreateDirectory(PathIn(tmpDir, "folder.yaml")).FullName;

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => FileAccessPolicy.Any.ReadAllText(folder, folder));
        Assert.StartsWith("it is not a regular file", ex.Reason);
    }

    /// <summary>Test that an existing folder is never written as an output.</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_ExistingDirectory_Denied()
    {
        // Arrange: a folder at the output path
        using var tmpDir = new TemporaryDirectory();
        var folder = Directory.CreateDirectory(PathIn(tmpDir, "out.md")).FullName;

        // Act / Assert: denied, and the folder is left as it was
        var ex = Assert.Throws<FileAccessDeniedException>(() => FileAccessPolicy.Any.WriteAllText("out.md", folder, "x"));
        Assert.StartsWith("it is not a regular file", ex.Reason);
        Assert.True(Directory.Exists(folder));
    }

    /// <summary>Test that a handle the platform reports as not a regular file is denied.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_NotRegularHandle_Denied()
    {
        // Arrange: a policy whose inspector reports a special file
        using var tmpDir = new TemporaryDirectory();
        var file = WriteFile(tmpDir, "a.yaml");
        var policy = FileAccessPolicy.Create(FileAccessMode.None, [], null, _ => new HandleFacts(false, file));

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(file, file));
        Assert.StartsWith("it is not a regular file", ex.Reason);
    }

    /// <summary>Test that a file inside an allowed folder is read under roots.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_InsideFolder_ReturnsContent()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = WriteFile(tmpDir, "allowed/sub/a.yaml");
        var policy = Roots(tmpDir, "allowed");

        // Act
        var text = policy.ReadAllText(file, file);

        // Assert
        Assert.Equal(Content, text);
    }

    /// <summary>Test that a file written outside the allowed folders is denied before it is opened.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_WrittenPathOutside_DeniedBeforeOpen()
    {
        // Arrange: an inspector that records whether any handle was opened
        using var tmpDir = new TemporaryDirectory();
        var file = WriteFile(tmpDir, "outside/a.yaml");
        Directory.CreateDirectory(PathIn(tmpDir, "allowed"));
        var opened = false;
        var policy = FileAccessPolicy.Create(
            FileAccessMode.Roots,
            [PathIn(tmpDir, "allowed")],
            null,
            handle =>
            {
                opened = true;
                return RealPath.Inspect(handle);
            });

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText("../outside/a.yaml", file));
        Assert.Equal("the path as written is outside the allowed folders (--file-access roots)", ex.Reason);
        Assert.Equal(
            "file-access-denied: '../outside/a.yaml': the path as written is outside the allowed folders (--file-access roots)",
            ex.Message);
        Assert.False(opened);
    }

    /// <summary>Test that a sibling folder whose name starts with an allowed folder's name is not inside it.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_SiblingWithSharedPrefix_Denied()
    {
        // Arrange: allowed "docs", file in "docs2"
        using var tmpDir = new TemporaryDirectory();
        var file = WriteFile(tmpDir, "docs2/a.yaml");
        var policy = Roots(tmpDir, "docs");

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(file, file));
        Assert.StartsWith("the path as written is outside the allowed folders", ex.Reason);
    }

    /// <summary>Test that a literal pattern outside the allowed folders is denied before matching.</summary>
    [Fact]
    public void FileAccessPolicy_CheckPattern_LiteralOutside_Denied()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var policy = Roots(tmpDir, "allowed");
        var rooted = Path.Combine(PathIn(tmpDir, "allowed"), "../secret.yaml");

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.CheckPattern("../secret.yaml", "../secret.yaml", rooted));
        Assert.Equal("../secret.yaml", ex.WrittenPath);
        Assert.StartsWith("the path as written is outside the allowed folders", ex.Reason);
    }

    /// <summary>Test that a glob pattern is left to the individual judgment of its matches, but a URL pattern is denied.</summary>
    [Fact]
    public void FileAccessPolicy_CheckPattern_GlobAndUrlPatterns_JudgedByText()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var policy = Roots(tmpDir, "allowed");
        var rooted = Path.Combine(PathIn(tmpDir, "allowed"), "../shared/*.yaml");

        // Act
        var globException = Record.Exception(() => policy.CheckPattern("../shared/*.yaml", "../shared/*.yaml", rooted));
        var urlException = Record.Exception(() => policy.CheckPattern("https://x/a.yaml", "https://x/a.yaml", "https://x/a.yaml"));

        // Assert
        Assert.Null(globException);
        Assert.StartsWith("it is a URL or URI", Assert.IsType<FileAccessDeniedException>(urlException).Reason);
    }

    /// <summary>Test that a folder link inside the allowed folder that leads out is denied by the real path.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_DirectoryLinkLeadingOut_Denied()
    {
        // Arrange: allowed/link -> outside
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "outside/a.yaml");
        Directory.CreateDirectory(PathIn(tmpDir, "allowed"));
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "allowed/link"), PathIn(tmpDir, "outside"));
        var policy = Roots(tmpDir, "allowed");
        var path = PathIn(tmpDir, "allowed/link/a.yaml");

        // Act / Assert: the real path, which names the outside folder, is reported
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText("link/a.yaml", path));
        Assert.StartsWith("its real path is outside the allowed folders", ex.Reason);
        Assert.NotNull(ex.RealPath);
        Assert.True(RealPath.IsUnder(ex.RealPath, RealPath.TryGetCanonicalDirectory(PathIn(tmpDir, "outside"))!));
    }

    /// <summary>Test that a path written outside the allowed folders is denied even when it links back in.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_OutsidePathLinkingBackIn_Denied()
    {
        // Arrange: outside/link -> allowed
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "allowed/a.yaml");
        Directory.CreateDirectory(PathIn(tmpDir, "outside"));
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "outside/link"), PathIn(tmpDir, "allowed"));
        var policy = Roots(tmpDir, "allowed");
        var path = PathIn(tmpDir, "outside/link/a.yaml");

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(path, path));
        Assert.StartsWith("the path as written is outside the allowed folders", ex.Reason);
    }

    /// <summary>Test that a folder link that stays inside the allowed folder is allowed under roots.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_DirectoryLinkStayingInside_Allowed()
    {
        // Arrange: allowed/link -> allowed/real
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "allowed/real/a.yaml");
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "allowed/link"), PathIn(tmpDir, "allowed/real"));
        var policy = Roots(tmpDir, "allowed");

        // Act
        var text = policy.ReadAllText("link/a.yaml", PathIn(tmpDir, "allowed/link/a.yaml"));

        // Assert
        Assert.Equal(Content, text);
    }

    /// <summary>Test that no-symlinks denies a path through a link below the trusted base folder.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_NoSymlinksLinkBelowTrustedBase_Denied()
    {
        // Arrange: config/link -> shared, with the config folder as trusted base
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "shared/a.yaml");
        Directory.CreateDirectory(PathIn(tmpDir, "config"));
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "config/link"), PathIn(tmpDir, "shared"));
        var policy = FileAccessPolicy.Create(FileAccessMode.NoSymlinks, [], PathIn(tmpDir, "config"));

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.ReadAllText("link/a.yaml", PathIn(tmpDir, "config/link/a.yaml")));
        Assert.Equal(
            "its real path differs from the path as written, so the path passes through a symbolic link or junction (--file-access no-symlinks)",
            ex.Reason);
    }

    /// <summary>Test that no-symlinks trusts a link above the trusted base folder.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_NoSymlinksLinkAboveTrustedBase_Allowed()
    {
        // Arrange: linked-top -> real-top, with linked-top/config as trusted base
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "real-top/config/a.yaml");
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "linked-top"), PathIn(tmpDir, "real-top"));
        var policy = FileAccessPolicy.Create(FileAccessMode.NoSymlinks, [], PathIn(tmpDir, "linked-top/config"));

        // Act
        var text = policy.ReadAllText("a.yaml", PathIn(tmpDir, "linked-top/config/a.yaml"));

        // Assert
        Assert.Equal(Content, text);
    }

    /// <summary>Test that no-symlinks denies a file that is itself a symbolic link.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_NoSymlinksFileSymlink_Denied()
    {
        // Arrange: config/a.yaml -> config/real.yaml (skipped where file links cannot be created)
        using var tmpDir = new TemporaryDirectory();
        var real = WriteFile(tmpDir, "config/real.yaml");
        var link = PathIn(tmpDir, "config/a.yaml");
        LinkSupport.CreateFileLinkOrSkip(link, real);
        var policy = FileAccessPolicy.Create(FileAccessMode.NoSymlinks, [], PathIn(tmpDir, "config"));

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText("a.yaml", link));
        Assert.StartsWith("its real path differs from the path as written", ex.Reason);
    }

    /// <summary>Test that a restrictive mode denies a file whose real path the platform cannot tell.</summary>
    /// <param name="modeText">The mode as written.</param>
    [Theory]
    [InlineData("roots")]
    [InlineData("no-symlinks")]
    [InlineData("roots,no-symlinks")]
    public void FileAccessPolicy_ReadAllText_RealPathUnknown_DeniedUnderRestrictiveModes(string modeText)
    {
        // Arrange: an inspector that cannot tell the real path
        using var tmpDir = new TemporaryDirectory();
        var file = WriteFile(tmpDir, "allowed/a.yaml");
        var policy = FileAccessPolicy.Create(
            FileAccessPolicy.ParseMode(modeText),
            [PathIn(tmpDir, "allowed")],
            PathIn(tmpDir, "allowed"),
            _ => new HandleFacts(true, null));

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText("a.yaml", file));
        Assert.Equal(
            $"the real path of the opened file cannot be determined on this system, so --file-access {modeText} cannot be applied",
            ex.Reason);
        Assert.Null(ex.RealPath);
    }

    /// <summary>Test that mode any reads a file whose real path the platform cannot tell.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_RealPathUnknown_AllowedUnderAny()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = WriteFile(tmpDir, "a.yaml");
        var policy = FileAccessPolicy.Create(FileAccessMode.None, [], null, _ => new HandleFacts(true, null));

        // Act
        var text = policy.ReadAllText(file, file);

        // Assert
        Assert.Equal(Content, text);
    }

    /// <summary>Test that an output outside the allowed folders is denied and nothing, not even a folder, is written.</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_OutputOutside_DeniedNothingWritten()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var policy = Roots(tmpDir, "allowed");
        var output = PathIn(tmpDir, "out/new/x.md");

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.WriteAllText("../out/new/x.md", output, "x"));
        Assert.StartsWith("the path as written is outside the allowed folders", ex.Reason);
        Assert.False(Directory.Exists(PathIn(tmpDir, "out")));
    }

    /// <summary>Test that missing folders inside the allowed folder are created under roots.</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_MissingFoldersInside_Created()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        Directory.CreateDirectory(PathIn(tmpDir, "allowed"));
        var policy = FileAccessPolicy.Create(
            FileAccessMode.Roots | FileAccessMode.NoSymlinks,
            [PathIn(tmpDir, "allowed")],
            PathIn(tmpDir, "allowed"));
        var output = PathIn(tmpDir, "allowed/a/b/x.md");

        // Act
        policy.WriteAllText("a/b/x.md", output, "# X\n");

        // Assert
        Assert.Equal("# X\n", File.ReadAllText(output));
    }

    /// <summary>Test that a parent folder link leading out is denied and no folder is created behind it.</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_ParentFolderLinkLeadingOut_DeniedNoFolderCreated()
    {
        // Arrange: allowed/link -> outside
        using var tmpDir = new TemporaryDirectory();
        Directory.CreateDirectory(PathIn(tmpDir, "allowed"));
        Directory.CreateDirectory(PathIn(tmpDir, "outside"));
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "allowed/link"), PathIn(tmpDir, "outside"));
        var policy = Roots(tmpDir, "allowed");

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.WriteAllText("link/new/x.md", PathIn(tmpDir, "allowed/link/new/x.md"), "x"));
        Assert.Contains("leads out of the allowed folders through a symbolic link or junction", ex.Reason);
        Assert.False(Directory.Exists(PathIn(tmpDir, "outside/new")));
    }

    /// <summary>Test that an existing output that is a link leading out is denied and its target is unchanged.</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_ExistingOutputSymlinkLeadingOut_DeniedTargetUnchanged()
    {
        // Arrange: allowed/out.md -> outside/target.md (skipped where file links cannot be created)
        using var tmpDir = new TemporaryDirectory();
        var target = WriteFile(tmpDir, "outside/target.md", "original");
        Directory.CreateDirectory(PathIn(tmpDir, "allowed"));
        var link = PathIn(tmpDir, "allowed/out.md");
        LinkSupport.CreateFileLinkOrSkip(link, target);
        var policy = Roots(tmpDir, "allowed");

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.WriteAllText("out.md", link, "replaced"));
        Assert.StartsWith("its real path is outside the allowed folders", ex.Reason);
        Assert.Equal("original", File.ReadAllText(target));
    }

    /// <summary>Test that an existing output is replaced completely (truncated after verification).</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_ExistingOutput_ReplacedContent()
    {
        // Arrange: an existing output longer than the new content
        using var tmpDir = new TemporaryDirectory();
        var output = WriteFile(tmpDir, "allowed/out.md", "a much longer original content\n");
        var policy = Roots(tmpDir, "allowed");

        // Act
        policy.WriteAllText("out.md", output, "short\n");

        // Assert
        Assert.Equal("short\n", File.ReadAllText(output));
    }

    /// <summary>Test that an existing output is not truncated when its handle is denied.</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_NotRegularHandle_ContentUnchanged()
    {
        // Arrange: an inspector that reports a special file
        using var tmpDir = new TemporaryDirectory();
        var output = WriteFile(tmpDir, "out.md", "original");
        var policy = FileAccessPolicy.Create(FileAccessMode.None, [], null, _ => new HandleFacts(false, output));

        // Act / Assert
        Assert.Throws<FileAccessDeniedException>(() => policy.WriteAllText("out.md", output, "replaced"));
        Assert.Equal("original", File.ReadAllText(output));
    }

    /// <summary>Test that no-symlinks denies an output whose folder is reached through a link.</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_NoSymlinksLinkedFolder_Denied()
    {
        // Arrange: config/link -> elsewhere
        using var tmpDir = new TemporaryDirectory();
        Directory.CreateDirectory(PathIn(tmpDir, "config"));
        Directory.CreateDirectory(PathIn(tmpDir, "elsewhere"));
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "config/link"), PathIn(tmpDir, "elsewhere"));
        var policy = FileAccessPolicy.Create(FileAccessMode.NoSymlinks, [], PathIn(tmpDir, "config"));

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.WriteAllText("link/x.md", PathIn(tmpDir, "config/link/x.md"), "x"));
        Assert.Contains("passes through a symbolic link or junction (--file-access no-symlinks)", ex.Reason);
        Assert.False(File.Exists(PathIn(tmpDir, "elsewhere/x.md")));
    }

    /// <summary>
    ///     Test that a path through a folder link to a network location is denied before anything is opened, with the
    ///     network path the link leads to.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_IntermediateLinkToShare_DeniedBeforeOpen()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: a fake link reader where lnk leads to a share, and an inspector that records any open
        using var tmpDir = new TemporaryDirectory();
        var opened = false;
        var policy = FileAccessPolicy.Create(
            FileAccessMode.None,
            [],
            null,
            handle =>
            {
                opened = true;
                return RealPath.Inspect(handle);
            },
            ShareLinkReader(tmpDir));

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.ReadAllText("lnk/sub/terms.yaml", PathIn(tmpDir, "lnk/sub/terms.yaml")));
        Assert.Equal("it leads to a network location through a symbolic link or junction, which is never accessed", ex.Reason);
        Assert.StartsWith(@"\\server\share", ex.RealPath);
        Assert.False(opened);
    }

    /// <summary>Test that an output through a folder link to a network location is denied and nothing is created.</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_IntermediateLinkToShare_DeniedNothingCreated()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: roots allowing the temporary folder, with a fake link reader where lnk leads to a share
        using var tmpDir = new TemporaryDirectory();
        var policy = FileAccessPolicy.Create(
            FileAccessMode.Roots,
            [tmpDir.DirectoryPath],
            null,
            RealPath.Inspect,
            ShareLinkReader(tmpDir));

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.WriteAllText("lnk/out/g.md", PathIn(tmpDir, "lnk/out/g.md"), "x"));
        Assert.Equal("it leads to a network location through a symbolic link or junction, which is never accessed", ex.Reason);
        Assert.False(Directory.Exists(PathIn(tmpDir, "lnk")));
    }

    /// <summary>Test that a pattern whose literal folders pass through a link to a network location is denied before matching.</summary>
    [Fact]
    public void FileAccessPolicy_CheckPattern_PrefixLinkToShare_DeniedBeforeMatching()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: a fake link reader where lnk leads to a share
        using var tmpDir = new TemporaryDirectory();
        var policy = FileAccessPolicy.Create(FileAccessMode.None, [], null, RealPath.Inspect, ShareLinkReader(tmpDir));

        // Act
        var globException = Record.Exception(
            () => policy.CheckPattern("lnk/*.yaml", "lnk/*.yaml", PathIn(tmpDir, "lnk/*.yaml")));
        var literalException = Record.Exception(
            () => policy.CheckPattern("lnk/a.yaml", "lnk/a.yaml", PathIn(tmpDir, "lnk/a.yaml")));
        var localException = Record.Exception(
            () => policy.CheckPattern("other/*.yaml", "other/*.yaml", PathIn(tmpDir, "other/*.yaml")));

        // Assert
        var glob = Assert.IsType<FileAccessDeniedException>(globException);
        Assert.Equal("it leads to a network location through a symbolic link or junction, which is never accessed", glob.Reason);
        Assert.StartsWith(@"\\server\share", glob.RealPath);
        Assert.Equal(
            "it leads to a network location through a symbolic link or junction, which is never accessed",
            Assert.IsType<FileAccessDeniedException>(literalException).Reason);
        Assert.Null(localException);
    }

    /// <summary>
    ///     Test, end to end, that a real symbolic link to an unreachable share is denied before the open contacts it
    ///     (skipped where symbolic links cannot be created; supplementary, not requirement evidence).
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_RealSymlinkToShare_DeniedBeforeOpen()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: lnk -> an unreachable share (a junction cannot target a share, so there is no fallback)
        using var tmpDir = new TemporaryDirectory();
        var link = PathIn(tmpDir, "lnk");
        try
        {
            Directory.CreateSymbolicLink(link, @"\\nonexistent.invalid\share");
        }
        catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
        {
            Assert.Skip($"A symbolic link cannot be created on this machine: {ex.Message}");
        }

        // Act / Assert: without the walk the open would fail with an I/O error after contacting the network
        var denied = Assert.Throws<FileAccessDeniedException>(
            () => FileAccessPolicy.Any.ReadAllText("lnk/a.yaml", Path.Combine(link, "a.yaml")));
        Assert.Equal("it leads to a network location through a symbolic link or junction, which is never accessed", denied.Reason);
    }

    /// <summary>
    ///     Test that on Windows the full-path rule accepts a share and an ordinary extended-length path, and denies
    ///     every other device-namespace path and a reserved device name.
    /// </summary>
    /// <param name="fullPath">The full path.</param>
    /// <param name="denied">True when the path must be denied as a device path.</param>
    [Theory]
    [InlineData(@"\\server\share\cfg\a.yaml", false)]
    [InlineData(@"\\?\C:\cfg\a.yaml", false)]
    [InlineData(@"\\?\C:\cfg\NUL.txt", false)]
    [InlineData(@"C:\x\NUL", true)]
    [InlineData(@"\\.\C:\x\a.yaml", true)]
    [InlineData(@"\\?\pipe\x\a.yaml", true)]
    [InlineData(@"\\?\UNC\s\x\a.yaml", true)]
    public void FileAccessPolicy_DescribeNeverAccessedFullPath_SharesAndExtendedPaths(string fullPath, bool denied)
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The full-path device rules apply on Windows only.");

        // Act
        var result = FileAccessPolicy.DescribeNeverAccessedFullPath(fullPath);

        // Assert
        Assert.Equal(denied ? "it is a device path, which is never accessed" : null, result);
    }

    /// <summary>Test that on Windows only a local path whose real path is on a network location leads to the network.</summary>
    [Fact]
    public void FileAccessPolicy_LeadsToNetwork_LocalToShareOnly()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network locations are classified on Windows only.");

        // Act
        var localToShare = FileAccessPolicy.LeadsToNetwork(@"C:\x\a.yaml", @"\\server\share\a.yaml");
        var shareToSameShare = FileAccessPolicy.LeadsToNetwork(@"\\server\share\a.yaml", @"\\server\share\a.yaml");
        var shareToOtherShare = FileAccessPolicy.LeadsToNetwork(@"\\server\share\a.yaml", @"\\other\x\a.yaml");
        var localToLocal = FileAccessPolicy.LeadsToNetwork(@"C:\x", @"C:\y");

        // Assert
        Assert.True(localToShare);
        Assert.False(shareToSameShare);
        Assert.False(shareToOtherShare);
        Assert.False(localToLocal);
    }

    /// <summary>Test that on Windows a pattern rooted on a share is allowed and its folders are not walked for links.</summary>
    [Fact]
    public void FileAccessPolicy_CheckPattern_PatternOnShare_AllowedWithoutWalk()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network locations are classified on Windows only.");

        // Arrange: mode any with a link reader that records every call
        var reads = 0;
        var policy = FileAccessPolicy.Create(
            FileAccessMode.None,
            [],
            null,
            RealPath.Inspect,
            _ =>
            {
                reads++;
                return null;
            });

        // Act
        var glob = Record.Exception(() => policy.CheckPattern("*.yaml", "*.yaml", @"\\server\share\cfg\*.yaml"));
        var literal = Record.Exception(() => policy.CheckPattern("a.yaml", "a.yaml", @"\\server\share\cfg\a.yaml"));

        // Assert
        Assert.Null(glob);
        Assert.Null(literal);
        Assert.Equal(0, reads);
    }

    /// <summary>Test that on Windows a local path whose opened file is on a network location is denied.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_LocalPathRealPathOnShare_Denied()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network locations are classified on Windows only.");

        // Arrange: a local file whose handle reports a real path on a share
        using var tmpDir = new TemporaryDirectory();
        var file = WriteFile(tmpDir, "a.yaml");
        var policy = FileAccessPolicy.Create(FileAccessMode.None, [], null, _ => new HandleFacts(true, @"\\server\share\a.yaml"));

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText("a.yaml", file));
        Assert.Equal("it leads to a network location through a symbolic link or junction, which is never accessed", ex.Reason);
        Assert.Equal(@"\\server\share\a.yaml", ex.RealPath);
    }

    /// <summary>Test that on Windows a relative path resolved against an ordinary extended-length base folder is read.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_ExtendedLengthBase_ReturnsContent()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Extended-length paths exist on Windows only.");

        // Arrange: the full path of a file under the extended-length spelling of the temporary folder
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "a.yaml");
        var fullPath = @"\\?\" + PathIn(tmpDir, "a.yaml");

        // Act
        var text = FileAccessPolicy.Any.ReadAllText("a.yaml", fullPath);

        // Assert
        Assert.Equal(Content, text);
    }

    /// <summary>
    ///     Test, end to end, that a relative path resolved against a folder on a share is read (skipped unless the
    ///     administrative share of the temporary folder's drive is reachable; supplementary, not requirement
    ///     evidence).
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_RelativePathOnAdminShare_Allowed()
    {
        // Arrange: the temporary folder reached through \\localhost\<drive>$ (skipped when not reachable)
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "a.yaml");
        var shareFolder = LinkSupport.AdminShareFolderOrSkip(tmpDir.DirectoryPath);

        // Act
        var text = FileAccessPolicy.Any.ReadAllText("a.yaml", Path.Combine(shareFolder, "a.yaml"));

        // Assert
        Assert.Equal(Content, text);
    }

    /// <summary>Test that the denial message holds the code, the written path, the real path and the reason.</summary>
    [Fact]
    public void FileAccessDeniedException_Message_ContainsCodePathsAndReason()
    {
        // Act
        var ex = new FileAccessDeniedException("link/a.yaml", "/real/a.yaml", "a reason");

        // Assert
        Assert.Equal("file-access-denied: 'link/a.yaml' (real path '/real/a.yaml'): a reason", ex.Message);
        Assert.Equal("file-access-denied", FileAccessDeniedException.ErrorCode);
        Assert.Equal("link/a.yaml", ex.WrittenPath);
        Assert.Equal("/real/a.yaml", ex.RealPath);
        Assert.Equal("a reason", ex.Reason);
    }

    /// <summary>Test that the denial message omits the real-path part when the real path is not known.</summary>
    [Fact]
    public void FileAccessDeniedException_Message_UnknownRealPath_OmitsRealPart()
    {
        // Act
        var ex = new FileAccessDeniedException("a.yaml", null, "a reason");

        // Assert
        Assert.Equal("file-access-denied: 'a.yaml': a reason", ex.Message);
    }

    /// <summary>Creates a roots policy whose single allowed folder is a sub-folder of the temporary folder.</summary>
    /// <param name="tmpDir">The temporary folder.</param>
    /// <param name="folder">The allowed sub-folder, created when missing.</param>
    /// <returns>The policy.</returns>
    private static FileAccessPolicy Roots(TemporaryDirectory tmpDir, string folder)
    {
        var full = Directory.CreateDirectory(PathIn(tmpDir, folder)).FullName;
        return FileAccessPolicy.Create(FileAccessMode.Roots, [full], null);
    }

    /// <summary>Writes a file under the temporary folder and returns its full path.</summary>
    /// <param name="tmpDir">The temporary folder.</param>
    /// <param name="relativePath">The path below the folder, with '/' separators.</param>
    /// <param name="content">The content.</param>
    /// <returns>The full path.</returns>
    private static string WriteFile(TemporaryDirectory tmpDir, string relativePath, string content = Content)
    {
        var path = tmpDir.GetFilePath(relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.WriteAllText(path, content);
        return path;
    }

    /// <summary>Returns the full path of an entry under the temporary folder without creating anything.</summary>
    /// <param name="tmpDir">The temporary folder.</param>
    /// <param name="relativePath">The path below the folder, with '/' separators.</param>
    /// <returns>The full path.</returns>
    private static string PathIn(TemporaryDirectory tmpDir, string relativePath) =>
        Path.Combine(tmpDir.DirectoryPath, relativePath.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>Creates a fake link reader in which the entry lnk under the temporary folder leads to a share.</summary>
    /// <param name="tmpDir">The temporary folder.</param>
    /// <returns>The link reader; every other entry is not a link.</returns>
    private static Func<string, string?> ShareLinkReader(TemporaryDirectory tmpDir)
    {
        var link = PathIn(tmpDir, "lnk");
        return path => string.Equals(path, link, StringComparison.OrdinalIgnoreCase) ? @"\\server\share" : null;
    }
}
