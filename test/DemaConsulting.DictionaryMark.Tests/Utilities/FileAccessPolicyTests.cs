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
        var text = FileAccessPolicy.Any.ReadAllText(Judged(file, file));

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
        FileAccessPolicy.Any.WriteAllText(Judged("new/nested/out.md", output), "# Out\n");

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
            () => policy.ReadAllText(Judged("terms:v1.yaml", PathIn(tmpDir, "allowed/terms:v1.yaml"))));
        var write = Assert.Throws<FileAccessDeniedException>(
            () => policy.WriteAllText(Judged("sub/out:1.md", PathIn(tmpDir, "allowed/sub/out:1.md")), "# Out\n"));

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
        var ex = Assert.Throws<FileAccessDeniedException>(() => FileAccessPolicy.Any.ReadAllText(Judged(name, path)));
        Assert.Equal("it is a device path, which is never accessed", ex.Reason);
    }

    /// <summary>Test that a device node is read only to be denied as not a regular file on Linux and macOS.</summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_DevNull_DeniedAsNotRegular()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(), "/dev is tested on Linux and macOS only.");

        // Act / Assert: the non-blocking open succeeds and the handle type refuses the device
        var ex = Assert.Throws<FileAccessDeniedException>(() => FileAccessPolicy.Any.ReadAllText(Judged("/dev/null", "/dev/null")));
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
        var direct = Record.Exception(() => policy.WriteAllText(Judged("/dev/null", "/dev/null"), "x"));
        var rooted = Record.Exception(() => policy.WriteAllText(Judged("/tmp/../dev/x.md", Path.GetFullPath("/tmp/../dev/x.md")), "x"));

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
            () => policy.WriteAllText(Judged("devices/null", PathIn(tmpDir, "devices/null")), "x"));
        var nested = Record.Exception(
            () => policy.WriteAllText(Judged("devices/new/x.md", PathIn(tmpDir, "devices/new/x.md")), "x"));

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
        var result = await Task.Run(() => Record.Exception(() => FileAccessPolicy.Any.ReadAllText(Judged(fifo, fifo))), TestContext.Current.CancellationToken)
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
        var ex = Assert.Throws<FileAccessDeniedException>(() => FileAccessPolicy.Any.ReadAllText(Judged(folder, folder)));
        Assert.StartsWith("it is not a regular file", ex.Reason);
    }

    /// <summary>
    ///     Test that a folder named as an input is opened and told by its handle, never by a by-name probe, and then
    ///     denied as not a regular file.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_Directory_ClassifiedFromHandle()
    {
        // Arrange: a folder and an inspector that records whether a handle was judged
        using var tmpDir = new TemporaryDirectory();
        var folder = Directory.CreateDirectory(PathIn(tmpDir, "folder.yaml")).FullName;
        var inspected = false;
        var policy = FileAccessPolicy.Create(
            FileAccessMode.None,
            [],
            null,
            handle =>
            {
                inspected = true;
                return RealPath.Inspect(handle);
            });

        // Act / Assert: the denial comes from the facts of the opened handle
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(Judged("folder.yaml", folder)));
        Assert.StartsWith("it is not a regular file", ex.Reason);
        Assert.True(inspected);
    }

    /// <summary>
    ///     Test that an input reached through a folder link that the walk did not see (as when a link is swapped in
    ///     after the walk) is denied by the real path of its handle under roots.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_LinkAppearingAfterWalk_DeniedByHandle()
    {
        // Arrange: allowed/lnk -> outside, with a link reader that reports no links at all
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "outside/a.yaml");
        Directory.CreateDirectory(PathIn(tmpDir, "allowed"));
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "allowed/lnk"), PathIn(tmpDir, "outside"));
        var policy = FileAccessPolicy.Create(FileAccessMode.Roots, [PathIn(tmpDir, "allowed")], null, RealPath.Inspect, _ => null);

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.ReadAllText(Judged("lnk/a.yaml", PathIn(tmpDir, "allowed/lnk/a.yaml"))));
        Assert.StartsWith("its real path is outside the allowed folders", ex.Reason);
    }

    /// <summary>
    ///     Test that an output reached through a folder link that the walk did not see (as when a link is swapped in
    ///     after the walk) is denied by the real path of the folder's handle under roots, and nothing is created.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_LinkAppearingAfterWalk_DeniedByHandle()
    {
        // Arrange: allowed/lnk -> outside, with a link reader that reports no links at all
        using var tmpDir = new TemporaryDirectory();
        Directory.CreateDirectory(PathIn(tmpDir, "allowed"));
        Directory.CreateDirectory(PathIn(tmpDir, "outside"));
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "allowed/lnk"), PathIn(tmpDir, "outside"));
        var policy = FileAccessPolicy.Create(FileAccessMode.Roots, [PathIn(tmpDir, "allowed")], null, RealPath.Inspect, _ => null);

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.WriteAllText(Judged("lnk/new/x.md", PathIn(tmpDir, "allowed/lnk/new/x.md")), "x"));
        Assert.Contains("leads out of the allowed folders through a symbolic link or junction", ex.Reason);
        Assert.Empty(Directory.EnumerateFileSystemEntries(PathIn(tmpDir, "outside")));
    }

    /// <summary>
    ///     Test that on Linux and macOS an existing output that is a link to a device is denied by its real path before
    ///     it is opened, in mode any.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_FileLinkToDevice_DeniedBeforeOpen()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(), "/dev is tested on Linux and macOS only.");

        // Arrange: out.md -> /dev/null and an inspector that records any open
        using var tmpDir = new TemporaryDirectory();
        var link = PathIn(tmpDir, "out.md");
        LinkSupport.CreateFileLinkOrSkip(link, "/dev/null");
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

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.WriteAllText(Judged("out.md", link), "x"));
        Assert.Equal("it is a device path, which is never accessed", ex.Reason);
        Assert.Equal("/dev/null", ex.RealPath);
        Assert.False(opened);
    }

    /// <summary>Test that an existing folder is never written as an output.</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_ExistingDirectory_Denied()
    {
        // Arrange: a folder at the output path
        using var tmpDir = new TemporaryDirectory();
        var folder = Directory.CreateDirectory(PathIn(tmpDir, "out.md")).FullName;

        // Act / Assert: denied, and the folder is left as it was
        var ex = Assert.Throws<FileAccessDeniedException>(() => FileAccessPolicy.Any.WriteAllText(Judged("out.md", folder), "x"));
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
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(Judged(file, file)));
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
        var text = policy.ReadAllText(Judged(file, file));

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
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(Judged("../outside/a.yaml", file)));
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
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(Judged(file, file)));
        Assert.StartsWith("the path as written is outside the allowed folders", ex.Reason);
    }

    /// <summary>Test that a literal pattern outside the allowed folders is denied before anything is touched.</summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_LiteralOutside_Denied()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var policy = Roots(tmpDir, "allowed");
        var rooted = Path.Combine(PathIn(tmpDir, "allowed"), "../secret.yaml");

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ResolvePattern("../secret.yaml", "../secret.yaml", rooted));
        Assert.Equal("../secret.yaml", ex.WrittenPath);
        Assert.StartsWith("the path as written is outside the allowed folders", ex.Reason);
    }

    /// <summary>Test that a literal pattern is returned as one judged path, without probing its existence.</summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_Literal_ReturnedWithoutProbe()
    {
        // Arrange: a literal pattern naming a file that does not exist
        using var tmpDir = new TemporaryDirectory();
        var rooted = PathIn(tmpDir, "missing.yaml");

        // Act
        var result = FileAccessPolicy.Any.ResolvePattern("missing.yaml", "missing.yaml", rooted);

        // Assert: the read, not the resolution, decides whether the file exists
        var judged = Assert.Single(result);
        Assert.Equal(new JudgedPath("missing.yaml", "missing.yaml", rooted), judged);
    }

    /// <summary>Test that a URL pattern is denied by its text, in every mode.</summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_UrlPattern_Denied()
    {
        // Act
        var urlException = Record.Exception(
            () => FileAccessPolicy.Any.ResolvePattern("https://x/*.yaml", "https://x/*.yaml", "https://x/*.yaml"));

        // Assert
        Assert.StartsWith("it is a URL or URI", Assert.IsType<FileAccessDeniedException>(urlException).Reason);
    }

    /// <summary>
    ///     Test that under roots a glob whose literal folder is written outside the allowed folders is denied before
    ///     that folder is listed, naming the pattern as written.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_RootsFolderOutside_DeniedBeforeMatching()
    {
        // Arrange: a file beside the allowed folder that the glob would match
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "shared/b.yaml");
        var policy = Roots(tmpDir, "allowed");
        var rooted = Path.Combine(PathIn(tmpDir, "allowed"), "../shared/*.yaml");

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.ResolvePattern("../shared/*.yaml", "../shared/*.yaml", rooted));
        Assert.Equal(
            "file-access-denied: '../shared/*.yaml': the path as written is outside the allowed folders (--file-access roots)",
            ex.Message);
    }

    /// <summary>
    ///     Test that under roots a glob whose literal folder is a link leading out of the allowed folders is denied by
    ///     the folder's real path before it is listed.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_RootsFolderLinkLeadingOut_DeniedBeforeMatching()
    {
        // Arrange: allowed/lnk -> outside, which holds a file the glob would match
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "outside/b.yaml");
        var policy = Roots(tmpDir, "allowed");
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "allowed/lnk"), PathIn(tmpDir, "outside"));

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.ResolvePattern("lnk/*.yaml", "lnk/*.yaml", PathIn(tmpDir, "allowed/lnk/*.yaml")));
        Assert.Equal("lnk/*.yaml", ex.WrittenPath);
        Assert.Equal(
            $"its folder '{PathIn(tmpDir, "allowed/lnk")}' leads out of the allowed folders through a symbolic link or junction (--file-access roots)",
            ex.Reason);
        Assert.NotNull(ex.RealPath);
        Assert.True(RealPath.PathsEqual(RealPath.TryGetCanonicalDirectory(PathIn(tmpDir, "outside"))!, ex.RealPath));
    }

    /// <summary>
    ///     Test that under no-symlinks a glob whose literal folder passes through a link below the trusted base folder
    ///     is denied before the folder is listed.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_NoSymlinksPrefixThroughLink_DeniedBeforeMatching()
    {
        // Arrange: config/lnk -> shared, with the config folder as trusted base
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "shared/a.yaml");
        Directory.CreateDirectory(PathIn(tmpDir, "config"));
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "config/lnk"), PathIn(tmpDir, "shared"));
        var policy = FileAccessPolicy.Create(FileAccessMode.NoSymlinks, [], PathIn(tmpDir, "config"));

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.ResolvePattern("lnk/*.yaml", "lnk/*.yaml", PathIn(tmpDir, "config/lnk/*.yaml")));
        Assert.Equal("lnk/*.yaml", ex.WrittenPath);
        Assert.Equal(
            $"its folder '{PathIn(tmpDir, "config/lnk")}' passes through a symbolic link or junction (--file-access no-symlinks)",
            ex.Reason);
    }

    /// <summary>
    ///     Test that under no-symlinks a link folder a wildcard would descend into is denied before it is listed, so
    ///     the pattern fails instead of the link being silently skipped.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_LinkUnderWildcard_DeniedBeforeListing()
    {
        // Arrange: config/real/a.yaml and config/sub/lnk -> shared, with the config folder as trusted base
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "config/real/a.yaml");
        WriteFile(tmpDir, "shared/b.yaml");
        Directory.CreateDirectory(PathIn(tmpDir, "config/sub"));
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "config/sub/lnk"), PathIn(tmpDir, "shared"));
        var policy = FileAccessPolicy.Create(FileAccessMode.NoSymlinks, [], PathIn(tmpDir, "config"));

        // Act
        var recursive = Record.Exception(
            () => policy.ResolvePattern("**/*.yaml", "**/*.yaml", PathIn(tmpDir, "config/**/*.yaml")));
        var shallow = policy.ResolvePattern("real/*.yaml", "real/*.yaml", PathIn(tmpDir, "config/real/*.yaml"));

        // Assert: the descent into the link is denied; a pattern that never descends into it is not
        var ex = Assert.IsType<FileAccessDeniedException>(recursive);
        Assert.Equal("**/*.yaml", ex.WrittenPath);
        Assert.Equal(
            $"its folder '{PathIn(tmpDir, "config/sub/lnk")}' passes through a symbolic link or junction (--file-access no-symlinks)",
            ex.Reason);
        Assert.Equal(PathIn(tmpDir, "config/real/a.yaml"), Assert.Single(shallow).FullPath);
    }

    /// <summary>
    ///     Test that under roots a link folder a wildcard descends into is listed when its real path stays inside the
    ///     allowed folders.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_RootsLinkUnderWildcardInside_Matched()
    {
        // Arrange: allowed/lnk -> allowed/real
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "allowed/real/a.yaml");
        var policy = Roots(tmpDir, "allowed");
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "allowed/lnk"), PathIn(tmpDir, "allowed/real"));

        // Act
        var result = policy.ResolvePattern("**/*.yaml", "**/*.yaml", PathIn(tmpDir, "allowed/**/*.yaml"));

        // Assert: the file is matched through the link and directly
        Assert.Equal(
            [PathIn(tmpDir, "allowed/lnk/a.yaml"), PathIn(tmpDir, "allowed/real/a.yaml")],
            result.Select(path => path.FullPath).Order(StringComparer.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     Test that under roots a link folder a wildcard would descend into is denied before it is listed when its
    ///     real path leads out of the allowed folders.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_RootsLinkUnderWildcardLeadingOut_DeniedBeforeListing()
    {
        // Arrange: allowed/lnk -> outside
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "allowed/a.yaml");
        WriteFile(tmpDir, "outside/b.yaml");
        var policy = Roots(tmpDir, "allowed");
        LinkSupport.CreateDirectoryLinkOrSkip(PathIn(tmpDir, "allowed/lnk"), PathIn(tmpDir, "outside"));

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.ResolvePattern("**/*.yaml", "**/*.yaml", PathIn(tmpDir, "allowed/**/*.yaml")));
        Assert.Contains("leads out of the allowed folders through a symbolic link or junction", ex.Reason);
    }

    /// <summary>
    ///     Test that a file a glob matched is judged by the pattern's literal part followed by its matched path, and
    ///     shown by its full path.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_GlobMatch_ExpandedIsPrefixPlusMatch()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = WriteFile(tmpDir, "docs/sub/a.yaml");

        // Act
        var result = FileAccessPolicy.Any.ResolvePattern("docs/**/*.yaml", "docs/**/*.yaml", PathIn(tmpDir, "docs/**/*.yaml"));

        // Assert
        Assert.Equal(new JudgedPath(file, "docs/sub/a.yaml", file), Assert.Single(result));
    }

    /// <summary>Test that a glob whose literal folder does not exist matches nothing.</summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_MissingFolder_ReturnsEmpty()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();

        // Act
        var result = FileAccessPolicy.Any.ResolvePattern("none/*.yaml", "none/*.yaml", PathIn(tmpDir, "none/*.yaml"));

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that a glob whose literal folder is a regular file matches nothing in every mode, on every platform,
    ///     without an error or a denial.
    /// </summary>
    /// <param name="modeText">The mode as written.</param>
    [Theory]
    [InlineData("any")]
    [InlineData("roots")]
    [InlineData("no-symlinks")]
    [InlineData("roots,no-symlinks")]
    public void FileAccessPolicy_ResolvePattern_FolderIsFile_ReturnsEmpty(string modeText)
    {
        // Arrange: a.yaml is a regular file, used as the literal folder of the glob
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "a.yaml");
        var policy = FileAccessPolicy.Create(
            FileAccessPolicy.ParseMode(modeText),
            [tmpDir.DirectoryPath],
            tmpDir.DirectoryPath);

        // Act
        var result = policy.ResolvePattern("a.yaml/*.yaml", "a.yaml/*.yaml", PathIn(tmpDir, "a.yaml/*.yaml"));

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that under roots a glob whose literal folder is a file link leading out of the allowed folders matches
    ///     nothing, as a file is not a folder, rather than being denied as a folder that leads out.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_RootsFolderIsFileLinkLeadingOut_ReturnsEmpty()
    {
        // Arrange: allowed/lnk.yaml -> outside/real.yaml (skipped where file links cannot be created)
        using var tmpDir = new TemporaryDirectory();
        var real = WriteFile(tmpDir, "outside/real.yaml");
        var policy = Roots(tmpDir, "allowed");
        LinkSupport.CreateFileLinkOrSkip(PathIn(tmpDir, "allowed/lnk.yaml"), real);

        // Act
        var result = policy.ResolvePattern(
            "lnk.yaml/*.yaml", "lnk.yaml/*.yaml", PathIn(tmpDir, "allowed/lnk.yaml/*.yaml"));

        // Assert
        Assert.Empty(result);
    }

    /// <summary>
    ///     Test that on Linux and macOS a file a glob matched whose name contains a colon is denied by the
    ///     written-text rules when it is read, shown by its full path.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ReadAllText_GlobMatchWithColon_Denied()
    {
        Assert.SkipUnless(
            OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(),
            "A colon is a stream separator on Windows, so a file name with a colon is tested on Linux and macOS only.");

        // Arrange: a file whose name contains a colon, matched by a glob
        using var tmpDir = new TemporaryDirectory();
        var file = WriteFile(tmpDir, "terms:v1.yaml");
        var match = Assert.Single(FileAccessPolicy.Any.ResolvePattern("*.yaml", "*.yaml", PathIn(tmpDir, "*.yaml")));

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => FileAccessPolicy.Any.ReadAllText(match));
        Assert.StartsWith("it contains a colon", ex.Reason);
        Assert.Equal(file, ex.WrittenPath);
    }

    /// <summary>
    ///     Test that the written-text rules apply to the expanded text of a path while the denial shows the path as
    ///     written, for reads and writes, and that nothing is opened or created.
    /// </summary>
    /// <param name="write">True to write, false to read.</param>
    /// <param name="expanded">The text after <c>${VAR}</c> expansion.</param>
    /// <param name="reason">The start of the expected reason.</param>
    [Theory]
    [InlineData(false, "//nonexistent.invalid/share/x.md", "it is a UNC or network path")]
    [InlineData(false, "sub:x/out.md", "it contains a colon")]
    [InlineData(false, "https://x/a.md", "it is a URL or URI")]
    [InlineData(true, "//nonexistent.invalid/share/x.md", "it is a UNC or network path")]
    [InlineData(true, "sub:x/out.md", "it contains a colon")]
    [InlineData(true, "https://x/a.md", "it is a URL or URI")]
    public void FileAccessPolicy_JudgedPath_ExpandedTextJudged_ShownAsWritten(bool write, string expanded, string reason)
    {
        // Arrange: an ordinary full path, so only the expanded text can cause the denial; an inspector records opens
        using var tmpDir = new TemporaryDirectory();
        var path = new JudgedPath("${V}/x.md", expanded, PathIn(tmpDir, "x.md"));
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
        var result = write
            ? Record.Exception(() => policy.WriteAllText(path, "x"))
            : Record.Exception(() => policy.ReadAllText(path));

        // Assert
        var ex = Assert.IsType<FileAccessDeniedException>(result);
        Assert.StartsWith(reason, ex.Reason);
        Assert.Equal("${V}/x.md", ex.WrittenPath);
        Assert.False(opened);
        Assert.Empty(Directory.EnumerateFileSystemEntries(tmpDir.DirectoryPath));
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
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(Judged("link/a.yaml", path)));
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
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(Judged(path, path)));
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
        var text = policy.ReadAllText(Judged("link/a.yaml", PathIn(tmpDir, "allowed/link/a.yaml")));

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
            () => policy.ReadAllText(Judged("link/a.yaml", PathIn(tmpDir, "config/link/a.yaml"))));
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
        var text = policy.ReadAllText(Judged("a.yaml", PathIn(tmpDir, "linked-top/config/a.yaml")));

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
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(Judged("a.yaml", link)));
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
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(Judged("a.yaml", file)));
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
        var text = policy.ReadAllText(Judged(file, file));

        // Assert
        Assert.Equal(Content, text);
    }

    /// <summary>
    ///     Test that a file whose type the platform cannot tell (the facts a missing native library or entry point
    ///     leaves) is denied in every mode with a reason saying its type is unknown, never that it is a special file.
    /// </summary>
    /// <param name="modeText">The mode as written.</param>
    [Theory]
    [InlineData("any")]
    [InlineData("roots")]
    [InlineData("no-symlinks")]
    [InlineData("roots,no-symlinks")]
    public void FileAccessPolicy_ReadAllText_TypeUnknown_DeniedWithTypeReason(string modeText)
    {
        // Arrange: an inspector that reports unknown facts, as RealPath.Inspect does when a native call is missing
        using var tmpDir = new TemporaryDirectory();
        var file = WriteFile(tmpDir, "allowed/a.yaml");
        var policy = FileAccessPolicy.Create(
            FileAccessPolicy.ParseMode(modeText),
            [PathIn(tmpDir, "allowed")],
            PathIn(tmpDir, "allowed"),
            _ => default);

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(Judged("a.yaml", file)));
        Assert.Equal(
            "the type of the opened file cannot be determined on this system, "
            + "so it cannot be confirmed to be a regular file and is never accessed",
            ex.Reason);
        Assert.DoesNotContain("not a regular file", ex.Reason);
        Assert.Null(ex.RealPath);
    }

    /// <summary>Test that an existing output whose type the platform cannot tell is denied and not truncated.</summary>
    [Fact]
    public void FileAccessPolicy_WriteAllText_TypeUnknown_ContentUnchanged()
    {
        // Arrange: an existing output and an inspector that reports unknown facts
        using var tmpDir = new TemporaryDirectory();
        var output = WriteFile(tmpDir, "out.md", "original");
        var policy = FileAccessPolicy.Create(FileAccessMode.None, [], null, _ => default);

        // Act / Assert: denied with the unknown-type reason, before the file is truncated
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.WriteAllText(Judged("out.md", output), "replaced"));
        Assert.StartsWith("the type of the opened file cannot be determined on this system", ex.Reason);
        Assert.Equal("original", File.ReadAllText(output));
    }

    /// <summary>
    ///     Test that an output whose parent folder is an existing regular file fails with an I/O error in every mode,
    ///     on every platform, and that nothing is written: the file is not taken for an existing folder.
    /// </summary>
    /// <param name="modeText">The mode as written.</param>
    [Theory]
    [InlineData("any")]
    [InlineData("roots")]
    [InlineData("no-symlinks")]
    [InlineData("roots,no-symlinks")]
    public void FileAccessPolicy_WriteAllText_ParentIsFile_FailsNothingWritten(string modeText)
    {
        // Arrange: allowed/a.yaml is a regular file, used as the folder of the output
        using var tmpDir = new TemporaryDirectory();
        var file = WriteFile(tmpDir, "allowed/a.yaml", "original");
        var policy = FileAccessPolicy.Create(
            FileAccessPolicy.ParseMode(modeText),
            [PathIn(tmpDir, "allowed")],
            PathIn(tmpDir, "allowed"));

        // Act: the file counts as a missing folder, so creating that folder fails
        var ex = Record.Exception(
            () => policy.WriteAllText(Judged("a.yaml/out.md", PathIn(tmpDir, "allowed/a.yaml/out.md")), "x"));

        // Assert: a plain I/O error (not a denial or a missing folder), and the file and its folder are unchanged
        Assert.IsType<IOException>(ex);
        Assert.Equal("original", File.ReadAllText(file));
        Assert.Equal([file], Directory.GetFileSystemEntries(PathIn(tmpDir, "allowed")));
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
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.WriteAllText(Judged("../out/new/x.md", output), "x"));
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
        policy.WriteAllText(Judged("a/b/x.md", output), "# X\n");

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
            () => policy.WriteAllText(Judged("link/new/x.md", PathIn(tmpDir, "allowed/link/new/x.md")), "x"));
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
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.WriteAllText(Judged("out.md", link), "replaced"));
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
        policy.WriteAllText(Judged("out.md", output), "short\n");

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
        Assert.Throws<FileAccessDeniedException>(() => policy.WriteAllText(Judged("out.md", output), "replaced"));
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
            () => policy.WriteAllText(Judged("link/x.md", PathIn(tmpDir, "config/link/x.md")), "x"));
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
            () => policy.ReadAllText(Judged("lnk/sub/terms.yaml", PathIn(tmpDir, "lnk/sub/terms.yaml"))));
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
            () => policy.WriteAllText(Judged("lnk/out/g.md", PathIn(tmpDir, "lnk/out/g.md")), "x"));
        Assert.Equal("it leads to a network location through a symbolic link or junction, which is never accessed", ex.Reason);
        Assert.False(Directory.Exists(PathIn(tmpDir, "lnk")));
    }

    /// <summary>Test that a pattern whose literal folders pass through a link to a network location is denied before matching.</summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_PrefixLinkToShare_DeniedBeforeMatching()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: a fake link reader where lnk leads to a share
        using var tmpDir = new TemporaryDirectory();
        var policy = FileAccessPolicy.Create(FileAccessMode.None, [], null, RealPath.Inspect, ShareLinkReader(tmpDir));

        // Act
        var globException = Record.Exception(
            () => policy.ResolvePattern("lnk/*.yaml", "lnk/*.yaml", PathIn(tmpDir, "lnk/*.yaml")));
        var literalException = Record.Exception(
            () => policy.ResolvePattern("lnk/a.yaml", "lnk/a.yaml", PathIn(tmpDir, "lnk/a.yaml")));
        var localException = Record.Exception(
            () => policy.ResolvePattern("other/*.yaml", "other/*.yaml", PathIn(tmpDir, "other/*.yaml")));

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
    ///     Test that a link folder a wildcard would descend into is denied before it is listed when it leads to a
    ///     network location, in mode any.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_ResolvePattern_LinkUnderWildcardToShare_DeniedBeforeListing()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: a real folder link docs/lnk (a junction where symbolic links are not permitted), which a fake link
        // reader reports as leading to a share
        using var tmpDir = new TemporaryDirectory();
        WriteFile(tmpDir, "target/a.yaml");
        Directory.CreateDirectory(PathIn(tmpDir, "docs"));
        var link = PathIn(tmpDir, "docs/lnk");
        LinkSupport.CreateDirectoryLinkOrSkip(link, PathIn(tmpDir, "target"));
        string? Reader(string path) => string.Equals(path, link, StringComparison.OrdinalIgnoreCase) ? @"\\server\share" : null;
        var policy = FileAccessPolicy.Create(FileAccessMode.None, [], null, RealPath.Inspect, Reader);

        // Act / Assert
        var ex = Assert.Throws<FileAccessDeniedException>(
            () => policy.ResolvePattern("docs/**/*.yaml", "docs/**/*.yaml", PathIn(tmpDir, "docs/**/*.yaml")));
        Assert.Equal("docs/**/*.yaml", ex.WrittenPath);
        Assert.Equal("it leads to a network location through a symbolic link or junction, which is never accessed", ex.Reason);
        Assert.Equal(@"\\server\share", ex.RealPath);
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
            () => FileAccessPolicy.Any.ReadAllText(Judged("lnk/a.yaml", Path.Combine(link, "a.yaml"))));
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

    /// <summary>Test that on Windows a path on a share is allowed and its folders are not walked for links.</summary>
    [Fact]
    public void FileAccessPolicy_CheckPath_PathOnShare_AllowedWithoutWalk()
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

        // Act: a glob's literal folder and a literal file, both on a share (judged without being listed or opened)
        var folder = Record.Exception(() => policy.CheckPath(new JudgedPath("*.yaml", "*.yaml", @"\\server\share\cfg")));
        var literal = Record.Exception(() => policy.CheckPath(new JudgedPath("a.yaml", "a.yaml", @"\\server\share\cfg\a.yaml")));

        // Assert
        Assert.Null(folder);
        Assert.Null(literal);
        Assert.Equal(0, reads);
    }

    /// <summary>
    ///     Test that on Windows a base folder (the root key) that leads to a network location through a link is
    ///     described as never accessed, before it is probed, while an ordinary folder is not, and that a denied
    ///     folder is never reported as existing.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_DescribeNeverAccessedFolder_LinkToShare_ReturnsReason()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: a fake link reader where lnk leads to a share, and a real docs folder
        using var tmpDir = new TemporaryDirectory();
        var reader = ShareLinkReader(tmpDir);
        Directory.CreateDirectory(PathIn(tmpDir, "docs"));

        // Act
        var throughLink = FileAccessPolicy.DescribeNeverAccessedFolder(
            "lnk/sub", PathIn(tmpDir, "lnk/sub"), reader, RealPath.InspectFolder, out var throughLinkExists);
        var ordinary = FileAccessPolicy.DescribeNeverAccessedFolder(
            "docs", PathIn(tmpDir, "docs"), reader, RealPath.InspectFolder, out var ordinaryExists);
        var written = FileAccessPolicy.DescribeNeverAccessedFolder(
            "//server/share", @"\\server\share", reader, RealPath.InspectFolder, out var writtenExists);

        // Assert
        Assert.Equal("it leads to a network location through a symbolic link or junction, which is never accessed", throughLink);
        Assert.False(throughLinkExists);
        Assert.Null(ordinary);
        Assert.True(ordinaryExists);
        Assert.Equal("it is a UNC or network path, which is never accessed", written);
        Assert.False(writtenExists);
    }

    /// <summary>
    ///     Test that whether a base folder exists is decided only from the facts of the one open that judged it (the
    ///     folder inspector is called exactly once, with the full path), never from a by-name probe, and that a
    ///     folder denied by its text is never inspected.
    /// </summary>
    [Fact]
    public void FileAccessPolicy_DescribeNeverAccessedFolder_ExistenceFromFolderFacts()
    {
        // Arrange: a path that does not exist on disk, a real folder, and a recording inspector with given facts
        using var tmpDir = new TemporaryDirectory();
        var missing = PathIn(tmpDir, "missing");
        var real = Directory.CreateDirectory(PathIn(tmpDir, "real")).FullName;
        var calls = new List<string>();
        Func<string, string?> reader = _ => null;
        (string? Reason, bool Exists) Describe(string text, string fullPath, PathFacts facts)
        {
            var reason = FileAccessPolicy.DescribeNeverAccessedFolder(
                text,
                fullPath,
                reader,
                path =>
                {
                    calls.Add(path);
                    return facts;
                },
                out var exists);
            return (reason, exists);
        }

        // Act
        var openedFolder = Describe("missing", missing, new PathFacts(PathState.Exists, true, missing));
        var openFoundNothing = Describe("real", real, new PathFacts(PathState.Missing, null, null));
        var notFolder = Describe("real", real, new PathFacts(PathState.Exists, false, real));
        var unknown = Describe("real", real, new PathFacts(PathState.Unknown, null, null));
        var typeUnknown = Describe("missing", missing, new PathFacts(PathState.Exists, null, missing));
        var callsBeforeDenied = calls.Count;
        var unc = Describe("//server/share", missing, new PathFacts(PathState.Exists, true, missing));
        var url = Describe("https://example.com/x", missing, new PathFacts(PathState.Exists, true, missing));

        // Assert: each judged folder was inspected once, with its full path, and existence follows the facts alone
        Assert.Equal((null, true), openedFolder);
        Assert.Equal((null, false), openFoundNothing);
        Assert.Equal((null, false), notFolder);
        Assert.Equal((null, false), unknown);
        Assert.Equal((null, true), typeUnknown);
        Assert.Equal(new[] { missing, real, real, real, missing }, calls.Take(callsBeforeDenied));
        Assert.Equal(5, callsBeforeDenied);

        // Assert: a folder denied by its text is never inspected and never exists
        Assert.Equal(("it is a UNC or network path, which is never accessed", false), unc);
        Assert.Equal(("it is a URL or URI, which is never accessed", false), url);
        Assert.Equal(callsBeforeDenied, calls.Count);
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
        var ex = Assert.Throws<FileAccessDeniedException>(() => policy.ReadAllText(Judged("a.yaml", file)));
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
        var text = FileAccessPolicy.Any.ReadAllText(Judged("a.yaml", fullPath));

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
        var text = FileAccessPolicy.Any.ReadAllText(Judged("a.yaml", Path.Combine(shareFolder, "a.yaml")));

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

    /// <summary>Creates the judged path of a path written without <c>${VAR}</c> references.</summary>
    /// <param name="written">The path as written, which is also its expanded text.</param>
    /// <param name="full">The full path.</param>
    /// <returns>The judged path.</returns>
    private static JudgedPath Judged(string written, string full) => new(written, written, full);

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
