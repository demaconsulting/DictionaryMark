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
///     Subsystem tests for the <c>Files</c> subsystem: referenced files are read anywhere under the default policy and
///     confined by the built-in policies, the check follows the opened file rather than its name, unsafe references are
///     refused under every policy, reading is bounded, a denial is reported with its facts, and an output path gets
///     the same checks before anything is written.
/// </summary>
public class FilesSubsystemTests
{
    /// <summary>
    ///     Validates that relative, dot-dot and absolute references are read under the default policy, and that a
    ///     missing reference is no file rather than a failure.
    /// </summary>
    [Fact]
    public void FilesSubsystem_ReferenceResolution_RelativeDotDotAndAbsoluteReferences_AreReadUnderTheDefaultPolicy()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("docs/images/a.txt", "relative");
        directory.Write("assets/b.txt", "dot-dot");
        var absolute = directory.Write("elsewhere/c.txt", "absolute");
        var resolver = new FileReferenceResolver(new FileReferenceOptions { BaseDirectory = directory.Combine("docs") });

        // Act
        var relative = resolver.Resolve("images/a.txt");
        var dotDot = resolver.Resolve("../assets/b.txt");
        var rooted = resolver.Resolve(absolute);
        var missing = resolver.Resolve("images/none.txt");

        // Assert
        Assert.Equal("relative", Encoding.UTF8.GetString(relative!.Content.Span));
        Assert.Equal("dot-dot", Encoding.UTF8.GetString(dotDot!.Content.Span));
        Assert.Equal("absolute", Encoding.UTF8.GetString(rooted!.Content.Span));
        Assert.Null(missing);
    }

    /// <summary>
    ///     Validates that roots and no-symlinks combined read a plain file inside the root and deny an escape with
    ///     dot-dot and a file reached through a link inside the root.
    /// </summary>
    [Fact]
    public void FilesSubsystem_AccessPolicy_RootsAndNoSymlinksCombined_ReadsInsideAndDeniesEscapeAndLink()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("root/real/a.txt", "inside");
        directory.Write("outside.txt", "outside");
        FileTestSupport.CreateDirectoryLinkOrSkip(directory.Combine("root/alias"), directory.Combine("root/real"));
        var root = directory.Combine("root");
        var policy = FileAccessPolicy.Combine(FileAccessPolicy.UnderRoots(root), FileAccessPolicy.NoSymlinks);
        var resolver = new FileReferenceResolver(new FileReferenceOptions { BaseDirectory = root, FileAccess = policy });

        // Act
        var inside = resolver.Resolve("real/a.txt");
        var escape = Assert.Throws<FileReferenceException>(() => resolver.Resolve("../outside.txt"));
        var link = Assert.Throws<FileReferenceException>(() => resolver.Resolve("alias/a.txt"));

        // Assert
        Assert.Equal("inside", Encoding.UTF8.GetString(inside!.Content.Span));
        Assert.Equal(FileReferenceFailureKind.AccessDenied, escape.Kind);
        Assert.Equal(FileReferenceFailureKind.AccessDenied, link.Kind);
        Assert.Equal("roots+no-symlinks", link.PolicyName);
    }

    /// <summary>
    ///     Validates that a change of the tree around the open does not change the decision: it follows the file that
    ///     was opened.
    /// </summary>
    [Fact]
    public void FilesSubsystem_RaceFreeCheck_SwapAfterOpen_DecisionFollowsTheOpenedFile()
    {
        // Arrange: root/sub is a link to the outside; after the open it becomes a real folder with a benign file
        using var directory = new FileTestDirectory();
        directory.Write("outside/a.txt", "outside");
        Directory.CreateDirectory(directory.Combine("root"));
        FileTestSupport.CreateDirectoryLinkOrSkip(directory.Combine("root/sub"), directory.Combine("outside"));
        var root = directory.Combine("root");
        var engine = new FileReferenceResolver(
            new FileReferenceOptions { BaseDirectory = root, FileAccess = FileAccessPolicy.UnderRoots(root) },
            new FileOpenHooks
            {
                AfterOpen = _ =>
                {
                    FileTestSupport.RemoveDirectoryLink(directory.Combine("root/sub"));
                    directory.Write("root/sub/a.txt", "benign");
                },
            });

        // Act
        var engineError = Assert.Throws<FileReferenceException>(() => engine.Resolve("sub/a.txt"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.AccessDenied, engineError.Kind);
    }

    /// <summary>
    ///     Validates that network and device references, a directory, (on Linux and macOS) a device reached by an
    ///     absolute path, and (on Windows) a legacy device name and a local path whose opened file lies on a network
    ///     share are refused even under the default policy.
    /// </summary>
    [Fact]
    public void FilesSubsystem_AlwaysRefused_NetworkDeviceAndNonRegularReferences_AreRefusedUnderAllowAll()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        Directory.CreateDirectory(directory.Combine("folder"));
        var resolver = new FileReferenceResolver(new FileReferenceOptions { BaseDirectory = directory.Path });

        // Act
        var network = Assert.Throws<FileReferenceException>(() => resolver.Resolve("\\\\server\\share\\a.png"));
        var host = Assert.Throws<FileReferenceException>(() => resolver.Resolve("//server/share/a.png"));
        var device = Assert.Throws<FileReferenceException>(() => resolver.Resolve("\\\\.\\PhysicalDrive0"));
        var stream = Assert.Throws<FileReferenceException>(() => resolver.Resolve("a.txt:hidden"));
        var folder = Assert.Throws<FileReferenceException>(() => resolver.Resolve("folder"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.NetworkPath, network.Kind);
        Assert.Equal(FileReferenceFailureKind.NetworkPath, host.Kind);
        Assert.Equal(FileReferenceFailureKind.InvalidPath, device.Kind);
        Assert.Equal(FileReferenceFailureKind.InvalidPath, stream.Kind);
        Assert.Equal(FileReferenceFailureKind.NotARegularFile, folder.Kind);
        if (!OperatingSystem.IsWindows())
        {
            Assert.Equal(FileReferenceFailureKind.NotARegularFile, Assert.Throws<FileReferenceException>(() => resolver.Resolve("/dev/zero")).Kind);
        }
        else
        {
            // A reserved device name (refused before anything is opened), and a local path whose opened file lies on a
            // network share (a link to a UNC target)
            directory.Write("a.txt", "a");
            var opened = new List<string>();
            var recording = new FileReferenceResolver(
                new FileReferenceOptions { BaseDirectory = directory.Path },
                new FileOpenHooks { BeforeOpen = opened.Add });
            var linkedToShare = new FileReferenceResolver(
                new FileReferenceOptions { BaseDirectory = directory.Path },
                new FileOpenHooks { RealPathOf = _ => @"\\server\share\a.txt" });
            foreach (var deviceName in new[] { "folder/NUL", "folder/con.txt", "folder/COM1", "NUL/a.txt" })
            {
                Assert.Equal(FileReferenceFailureKind.InvalidPath, Assert.Throws<FileReferenceException>(() => recording.Resolve(deviceName)).Kind);
            }

            Assert.Empty(opened);
            Assert.Equal(FileReferenceFailureKind.NetworkPath, Assert.Throws<FileReferenceException>(() => linkedToShare.Resolve("a.txt")).Kind);
        }
    }

    /// <summary>
    ///     Validates that a file at the size limit is read and a file one byte longer is refused as too large, while a
    ///     file held open by a writer is still read.
    /// </summary>
    [Fact]
    public void FilesSubsystem_BoundedReading_SizeLimit_ReadsAtTheLimitAndRefusesALongerFile()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("ten.txt", "0123456789");
        directory.Write("eleven.txt", "0123456789A");
        var held = directory.Write("held.txt", "held");
        using var writer = new FileStream(held, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        var resolver = new FileReferenceResolver(new FileReferenceOptions { BaseDirectory = directory.Path, MaxFileBytes = 10 });

        // Act
        var atLimit = resolver.Resolve("ten.txt");
        var tooLong = Assert.Throws<FileReferenceException>(() => resolver.Resolve("eleven.txt"));
        var shared = resolver.Resolve("held.txt");

        // Assert
        Assert.Equal("0123456789", Encoding.UTF8.GetString(atLimit!.Content.Span));
        Assert.Equal(FileReferenceFailureKind.TooLarge, tooLong.Kind);
        Assert.Equal("held", Encoding.UTF8.GetString(shared!.Content.Span));
        Assert.Equal(16L * 1024 * 1024, FileReferenceOptions.DefaultMaxFileBytes);
    }

    /// <summary>
    ///     Validates that a denial carries the reference, the policy, the reason and the paths, while its message names
    ///     the reference and the policy but no host directory.
    /// </summary>
    [Fact]
    public void FilesSubsystem_ReferenceFailureReporting_DenialCarriesFactsWithoutHostPathInMessage()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        directory.Write("secret/key.txt", "key");
        Directory.CreateDirectory(directory.Combine("docs"));
        var resolver = new FileReferenceResolver(
            new FileReferenceOptions
            {
                BaseDirectory = directory.Combine("docs"),
                FileAccess = FileAccessPolicy.UnderRoots(directory.Combine("docs")),
            });

        // Act
        var ex = Assert.Throws<FileReferenceException>(() => resolver.Resolve("../secret/key.txt"));

        // Assert
        Assert.Equal(FileReferenceFailureKind.AccessDenied, ex.Kind);
        Assert.Equal("../secret/key.txt", ex.Reference);
        Assert.Equal("roots", ex.PolicyName);
        Assert.NotNull(ex.Reason);
        Assert.NotNull(ex.Hint);
        Assert.Equal(Path.GetFullPath(directory.Combine("secret/key.txt")), ex.RequestedPath);
        Assert.Contains("'../secret/key.txt'", ex.Message, StringComparison.Ordinal);
        Assert.Contains("'roots'", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(directory.Path, ex.Message, StringComparison.Ordinal);
    }

    /// <summary>
    ///     Validates that an output path gets the never-list rules under the default policy and the policy under a
    ///     restrictive one, before anything is created: an output inside the roots (in a folder that does not exist
    ///     yet) is allowed, and an output outside them, a network output and a directory are refused, with nothing
    ///     created.
    /// </summary>
    [Fact]
    public void FilesSubsystem_OutputPaths_NeverListAndPolicyApplyBeforeAnyWrite()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var root = Directory.CreateDirectory(directory.Combine("root")).FullName;
        Directory.CreateDirectory(directory.Combine("root/folder"));
        var permissive = new FileReferenceResolver(new FileReferenceOptions { BaseDirectory = root });
        var confined = new FileReferenceResolver(
            new FileReferenceOptions { BaseDirectory = root, FileAccess = FileAccessPolicy.UnderRoots(root) });

        // Act
        var insideError = Record.Exception(() => confined.CheckOutputPath(Path.Combine(root, "new/sub/out.md")));
        var outside = Assert.Throws<FileReferenceException>(() => confined.CheckOutputPath(directory.Combine("out/out.md")));
        var network = Assert.Throws<FileReferenceException>(() => permissive.CheckOutputPath("//server/share/out.md"));
        var folder = Assert.Throws<FileReferenceException>(() => permissive.CheckOutputPath(Path.Combine(root, "folder")));

        // Assert: the allowed output created nothing; the refused ones were refused with their kinds
        Assert.Null(insideError);
        Assert.False(Directory.Exists(Path.Combine(root, "new")));
        Assert.Equal(FileReferenceFailureKind.AccessDenied, outside.Kind);
        Assert.False(Directory.Exists(directory.Combine("out")));
        Assert.Equal(FileReferenceFailureKind.NetworkPath, network.Kind);
        Assert.Equal(FileReferenceFailureKind.NotARegularFile, folder.Kind);
    }

    /// <summary>
    ///     Validates that the subsystem refuses an input pattern by the never list without touching the file system: a
    ///     network pattern and a URL-like pattern are refused with the kinds a read gets, and a local glob passes.
    /// </summary>
    [Fact]
    public void FilesSubsystem_PatternText_NeverListRefusesPatternsBeforeListing()
    {
        // Arrange
        using var directory = new FileTestDirectory();
        var local = Path.Combine(directory.Combine("root"), "*.yaml");
        var url = Path.Combine(directory.Combine("root"), "https://dictionarymark-test.invalid/*.yaml");

        // Act
        var localError = Record.Exception(() => FileReferenceResolver.CheckPatternText(local));
        var network = Assert.Throws<FileReferenceException>(
            () => FileReferenceResolver.CheckPatternText("//dictionarymark-test.invalid/share/*.yaml"));
        var colon = Assert.Throws<FileReferenceException>(() => FileReferenceResolver.CheckPatternText(url));

        // Assert
        Assert.Null(localError);
        Assert.Equal(FileReferenceFailureKind.NetworkPath, network.Kind);
        Assert.Equal(FileReferenceFailureKind.InvalidPath, colon.Kind);
    }
}
