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
///     Unit tests for the RealPath unit, which holds all platform-specific (native) code of the tool.
/// </summary>
public class RealPathTests
{
    /// <summary>Test that a plain file opened for reading is a regular file whose real path is its own path.</summary>
    [Fact]
    public void RealPath_Inspect_PlainFile_ReportsRealPathAndRegular()
    {
        // Arrange: a file and the real location of its folder
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("a.yaml");
        File.WriteAllText(file, "x");
        var expected = Path.Join(RealPath.TryGetCanonicalDirectory(tmpDir.DirectoryPath), "a.yaml");

        // Act
        HandleFacts facts;
        using (var handle = RealPath.OpenForRead(file))
        {
            facts = RealPath.Inspect(handle);
        }

        // Assert
        Assert.True(facts.IsRegularFile);
        Assert.NotNull(facts.FinalPath);
        Assert.True(RealPath.PathsEqual(expected, facts.FinalPath), $"Expected '{expected}', got '{facts.FinalPath}'.");
    }

    /// <summary>Test that the real path of a file opened through a folder link names the link's target.</summary>
    [Fact]
    public void RealPath_Inspect_FileThroughDirectoryLink_ReportsTarget()
    {
        // Arrange: link -> target, with a file in the target
        using var tmpDir = new TemporaryDirectory();
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        File.WriteAllText(Path.Combine(target, "a.yaml"), "x");
        var link = Path.Combine(tmpDir.DirectoryPath, "link");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);
        var expected = Path.Join(RealPath.TryGetCanonicalDirectory(target), "a.yaml");

        // Act
        HandleFacts facts;
        using (var handle = RealPath.OpenForRead(Path.Combine(link, "a.yaml")))
        {
            facts = RealPath.Inspect(handle);
        }

        // Assert
        Assert.True(facts.IsRegularFile);
        Assert.True(RealPath.PathsEqual(expected, facts.FinalPath!), $"Expected '{expected}', got '{facts.FinalPath}'.");
    }

    /// <summary>Test that on macOS the real path of a temporary file names /private, where /var really lives.</summary>
    [Fact]
    public void RealPath_Inspect_TempFile_ReportsPrivateLocation()
    {
        Assert.SkipUnless(OperatingSystem.IsMacOS(), "The /var link of macOS is tested on macOS only.");

        // Arrange: a file in the system temporary folder (below /var/folders)
        using var tmpDir = new TemporaryDirectory(Path.GetTempPath());
        var file = tmpDir.GetFilePath("a.yaml");
        File.WriteAllText(file, "x");

        // Act
        using var handle = RealPath.OpenForRead(file);
        var facts = RealPath.Inspect(handle);

        // Assert
        Assert.True(facts.IsRegularFile);
        Assert.StartsWith("/private/", facts.FinalPath);
    }

    /// <summary>Test that /dev/null is reported as not a regular file on Linux and macOS.</summary>
    [Fact]
    public void RealPath_Inspect_DevNull_IsNotRegular()
    {
        Assert.SkipUnless(OperatingSystem.IsLinux() || OperatingSystem.IsMacOS(), "/dev/null is tested on Linux and macOS only.");

        // Act
        using var handle = RealPath.OpenForRead("/dev/null");
        var facts = RealPath.Inspect(handle);

        // Assert
        Assert.False(facts.IsRegularFile);
    }

    /// <summary>Test that opening a named pipe with no writer does not block and reports a special file.</summary>
    [Fact]
    public async Task RealPath_OpenForRead_Fifo_DoesNotBlockAndIsNotRegular()
    {
        // Arrange: a named pipe (skipped where pipes cannot be created)
        using var tmpDir = new TemporaryDirectory();
        var fifo = Path.Combine(tmpDir.DirectoryPath, "pipe");
        LinkSupport.CreateFifoOrSkip(fifo);

        // Act: open and inspect on a worker, which must finish promptly (a blocked open times out)
        var facts = await Task.Run(
            () =>
            {
                using var handle = RealPath.OpenForRead(fifo);
                return RealPath.Inspect(handle);
            },
            TestContext.Current.CancellationToken).WaitAsync(TimeSpan.FromSeconds(10), TestContext.Current.CancellationToken);

        // Assert
        Assert.False(facts.IsRegularFile);
    }

    /// <summary>Test that the Windows NUL device is reported as not a regular file.</summary>
    [Fact]
    public void RealPath_Inspect_NulDevice_IsNotRegular()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "The NUL device is tested on Windows only.");

        // Act
        using var handle = File.OpenHandle(@"\\.\NUL", FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var facts = RealPath.Inspect(handle);

        // Assert
        Assert.False(facts.IsRegularFile);
        Assert.Null(facts.FinalPath);
    }

    /// <summary>Test that the real location of a linked folder is its target.</summary>
    [Fact]
    public void RealPath_TryGetCanonicalDirectory_LinkedDirectory_ReturnsTarget()
    {
        // Arrange: link -> target
        using var tmpDir = new TemporaryDirectory();
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        var link = Path.Combine(tmpDir.DirectoryPath, "link");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);

        // Act
        var canonicalLink = RealPath.TryGetCanonicalDirectory(link);
        var canonicalTarget = RealPath.TryGetCanonicalDirectory(target);

        // Assert
        Assert.NotNull(canonicalLink);
        Assert.NotNull(canonicalTarget);
        Assert.True(RealPath.PathsEqual(canonicalTarget, canonicalLink));
        Assert.EndsWith("target", canonicalLink.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Test that a folder that does not exist has no real location.</summary>
    [Fact]
    public void RealPath_TryGetCanonicalDirectory_MissingDirectory_ReturnsNull()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();

        // Act
        var canonical = RealPath.TryGetCanonicalDirectory(Path.Combine(tmpDir.DirectoryPath, "missing"));

        // Assert
        Assert.Null(canonical);
    }

    /// <summary>Test that opening an existing file for writing keeps its content.</summary>
    [Fact]
    public void RealPath_TryOpenExistingForWrite_ExistingFile_KeepsContent()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("out.md");
        File.WriteAllText(file, "original");

        // Act
        using (var handle = RealPath.TryOpenExistingForWrite(file))
        {
            Assert.NotNull(handle);
        }

        // Assert
        Assert.Equal("original", File.ReadAllText(file));
    }

    /// <summary>Test that exclusive creation fails when an entry already has the name.</summary>
    [Fact]
    public void RealPath_CreateNewForWrite_ExistingEntry_Throws()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var file = tmpDir.GetFilePath("out.md");
        File.WriteAllText(file, "original");

        // Act / Assert
        Assert.Throws<IOException>(() => RealPath.CreateNewForWrite(file).Dispose());
        Assert.Equal("original", File.ReadAllText(file));
    }

    /// <summary>Test that the folder boundary is a directory separator, not a name prefix.</summary>
    [Fact]
    public void RealPath_IsUnder_BoundaryIsSeparator()
    {
        // Arrange
        var folder = Path.GetFullPath("docs");

        // Act / Assert
        Assert.True(RealPath.IsUnder(folder, folder));
        Assert.True(RealPath.IsUnder(Path.Combine(folder, "a", "b.yaml"), folder));
        Assert.True(RealPath.IsUnder(Path.Combine(folder, "a"), folder + Path.DirectorySeparatorChar));
        Assert.False(RealPath.IsUnder(folder + "2", folder));
        Assert.False(RealPath.IsUnder(Path.GetDirectoryName(folder)!, folder));
    }

    /// <summary>Test that path equality follows the platform's case rule.</summary>
    [Fact]
    public void RealPath_PathsEqual_PlatformCaseRule()
    {
        // Arrange
        var path = Path.GetFullPath("Docs");
        var upper = Path.Combine(Path.GetDirectoryName(path)!, "DOCS");

        // Act
        var equal = RealPath.PathsEqual(path, upper);

        // Assert: case-insensitive on Windows and macOS, case-sensitive elsewhere
        Assert.Equal(OperatingSystem.IsWindows() || OperatingSystem.IsMacOS(), equal);
        Assert.True(RealPath.PathsEqual(path, path + Path.DirectorySeparatorChar));
    }

    /// <summary>Test that the common ancestor is the deepest folder two paths share by whole components.</summary>
    [Fact]
    public void RealPath_CommonAncestor_ReturnsDeepestSharedFolder()
    {
        // Arrange
        var root = Path.GetFullPath("base");
        var first = Path.Combine(root, "config", "a.yaml");
        var second = Path.Combine(root, "configs", "b.yaml");

        // Act
        var ancestor = RealPath.CommonAncestor(first, Path.Combine(root, "config"));
        var sibling = RealPath.CommonAncestor(first, second);

        // Assert
        Assert.Equal(Path.Combine(root, "config"), ancestor);
        Assert.Equal(root, sibling);
    }

    /// <summary>Test that UNC paths are network locations and local paths are not.</summary>
    [Fact]
    public void RealPath_IsNetworkLocation_UncAndLocal_Classified()
    {
        // Act / Assert: network classification applies on Windows only
        var windows = OperatingSystem.IsWindows();
        Assert.Equal(windows, RealPath.IsNetworkLocation(@"\\server\share\a.yaml"));
        Assert.Equal(windows, RealPath.IsNetworkLocation(@"\\?\UNC\server\share\a.yaml"));
        Assert.False(RealPath.IsNetworkLocation(@"\\.\pipe\x"));
        Assert.False(RealPath.IsNetworkLocation(Path.GetFullPath("a.yaml")));
    }

    /// <summary>Test that every reserved Windows device spelling is recognized, and ordinary names are not.</summary>
    /// <param name="component">The path component.</param>
    /// <param name="expected">Whether it is a device name.</param>
    [Theory]
    [InlineData("NUL", true)]
    [InlineData("nul.txt", true)]
    [InlineData("CON", true)]
    [InlineData("prn", true)]
    [InlineData("AUX", true)]
    [InlineData("COM1", true)]
    [InlineData("LPT9", true)]
    [InlineData("COM\u00B9", true)]
    [InlineData("CONIN$", true)]
    [InlineData("conout$", true)]
    [InlineData("NUL:stream", true)]
    [InlineData("NUL .yaml", true)]
    [InlineData("null", false)]
    [InlineData("console", false)]
    [InlineData("COM10", false)]
    [InlineData("terms.yaml", false)]
    public void RealPath_IsWindowsDeviceName_ReservedSpellings(string component, bool expected)
    {
        // Act
        var result = RealPath.IsWindowsDeviceName(component);

        // Assert
        Assert.Equal(expected, result);
    }

    /// <summary>Test that a folder link (symbolic link or junction) is reported as a link and a folder is not.</summary>
    [Fact]
    public void RealPath_IsLink_SymlinkOrJunction_True()
    {
        // Arrange
        using var tmpDir = new TemporaryDirectory();
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        var link = Path.Combine(tmpDir.DirectoryPath, "link");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);

        // Act / Assert
        Assert.True(RealPath.IsLink(link));
        Assert.False(RealPath.IsLink(target));
        Assert.False(RealPath.IsLink(Path.Combine(tmpDir.DirectoryPath, "missing")));
    }

    /// <summary>Test that a path without a short (8.3) name is returned unchanged.</summary>
    [Fact]
    public void RealPath_ExpandShortNames_PathWithoutShortName_Unchanged()
    {
        // Arrange
        var path = Path.GetFullPath("plain-name.yaml");

        // Act
        var expanded = RealPath.ExpandShortNames(path);

        // Assert
        Assert.Equal(path, expanded);
    }

    /// <summary>
    ///     Test that a link to a network location is found at any folder of a path, in either target spelling,
    ///     through a chain of links (up to 63) and through a relative target, without following any link.
    /// </summary>
    [Fact]
    public void RealPath_FindNetworkLinkTarget_IntermediateLinkToShare_ReturnsNetworkPath()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: a fake link reader over a local base folder, so no link or share is needed
        var root = Path.GetFullPath("walk");
        var links = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(root, "lnk")] = @"\\server\share\x",
            [Path.Combine(root, "nt")] = @"\??\UNC\server\share",
            [Path.Combine(root, "a")] = Path.Combine(root, "b"),
            [Path.Combine(root, "b", "c")] = @"\\server\s",
            [Path.Combine(root, "sub", "r")] = @"..\b",
        };
        for (var i = 0; i < 62; i++)
        {
            links[Path.Combine(root, $"h{i}")] = Path.Combine(root, $"h{i + 1}");
        }

        links[Path.Combine(root, "h62")] = @"\\server\deep";
        string? Reader(string path) => links.GetValueOrDefault(path);

        // Act
        var intermediate = RealPath.FindNetworkLinkTarget(Path.Combine(root, "lnk", "sub", "terms.yaml"), Reader);
        var ntForm = RealPath.FindNetworkLinkTarget(Path.Combine(root, "nt", "f.yaml"), Reader);
        var chain = RealPath.FindNetworkLinkTarget(Path.Combine(root, "a", "c", "f.yaml"), Reader);
        var relative = RealPath.FindNetworkLinkTarget(Path.Combine(root, "sub", "r", "c", "f.yaml"), Reader);
        var longChain = RealPath.FindNetworkLinkTarget(Path.Combine(root, "h0", "f.yaml"), Reader);

        // Assert: the path the links lead to, with the components after the link
        Assert.Equal(@"\\server\share\x\sub\terms.yaml", intermediate);
        Assert.Equal(@"\\server\share\f.yaml", ntForm);
        Assert.Equal(@"\\server\s\f.yaml", chain);
        Assert.Equal(@"\\server\s\f.yaml", relative);
        Assert.Equal(@"\\server\deep\f.yaml", longChain);
    }

    /// <summary>
    ///     Test that a path through local links only, a link loop, a chain longer than 63 links and a path without
    ///     links report no network target, and that the walk ends.
    /// </summary>
    [Fact]
    public void RealPath_FindNetworkLinkTarget_LocalLinksAndLoop_ReturnsNull()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: local links (one in NT form), a loop, and a chain of 64 links that ends at a share
        var root = Path.GetFullPath("walk");
        var links = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [Path.Combine(root, "a")] = Path.Combine(root, "b"),
            [Path.Combine(root, "b")] = @"\??\" + Path.Combine(root, "local"),
            [Path.Combine(root, "x")] = Path.Combine(root, "y"),
            [Path.Combine(root, "y")] = Path.Combine(root, "x"),
        };
        for (var i = 0; i < 63; i++)
        {
            links[Path.Combine(root, $"h{i}")] = Path.Combine(root, $"h{i + 1}");
        }

        links[Path.Combine(root, "h63")] = @"\\server\deep";
        string? Reader(string path) => links.GetValueOrDefault(path);

        // Act
        var local = RealPath.FindNetworkLinkTarget(Path.Combine(root, "a", "f.yaml"), Reader);
        var loop = RealPath.FindNetworkLinkTarget(Path.Combine(root, "x", "f.yaml"), Reader);
        var tooLong = RealPath.FindNetworkLinkTarget(Path.Combine(root, "h0", "f.yaml"), Reader);
        var plain = RealPath.FindNetworkLinkTarget(Path.Combine(root, "plain", "f.yaml"), _ => null);

        // Assert
        Assert.Null(local);
        Assert.Null(loop);
        Assert.Null(tooLong);
        Assert.Null(plain);
    }

    /// <summary>Test that a real local folder link is read and followed locally, reporting no network target.</summary>
    [Fact]
    public void RealPath_FindNetworkLinkTarget_RealLocalLink_ReturnsNull()
    {
        Assert.SkipUnless(OperatingSystem.IsWindows(), "Network link targets are checked on Windows only.");

        // Arrange: link -> target (a junction where symbolic links are not permitted)
        using var tmpDir = new TemporaryDirectory();
        var target = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "target")).FullName;
        var link = Path.Combine(tmpDir.DirectoryPath, "link");
        LinkSupport.CreateDirectoryLinkOrSkip(link, target);

        // Act
        var linkTarget = RealPath.ReadLinkTarget(link);
        var networkTarget = RealPath.FindNetworkLinkTarget(Path.Combine(link, "f.yaml"));

        // Assert
        Assert.NotNull(linkTarget);
        Assert.Null(networkTarget);
    }
}
