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
///     Subsystem tests for the Utilities subsystem covering GlobMatcher file resolution,
///     PathHelpers path-combination and traversal-prevention workflows, and TemporaryDirectory
///     directory-creation integration.
/// </summary>
[Collection("Sequential")]
public class UtilitiesSubsystemTests
{
    /// <summary>
    ///     Test that utilities subsystem can handle path combination workflows.
    /// </summary>
    [Fact]
    public void UtilitiesSubsystem_PathResolutionWorkflow_ValidPaths_ResolvesCorrectly()
    {
        // Arrange: setup base path and test paths for path combination testing
        var basePath = Path.GetTempPath();
        var testPaths = new[]
        {
            "test.txt",
            "subdirectory/test.txt",
            "nested/dir/test.txt"
        };

        // Act & Assert: combine each test path with base path and verify results are valid absolute paths
        foreach (var testPath in testPaths)
        {
            var combinedPath = PathHelpers.SafePathCombine(basePath, testPath);
            var fullBasePath = Path.GetFullPath(basePath);
            var fullCombinedPath = Path.GetFullPath(combinedPath);
            var relativePath = Path.GetRelativePath(fullBasePath, fullCombinedPath);

            Assert.True(Path.IsPathFullyQualified(combinedPath),
                $"SafePathCombine should return absolute path for {testPath}");
            Assert.False(
                Path.IsPathRooted(relativePath) ||
                relativePath.Equals("..", StringComparison.Ordinal) ||
                relativePath.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                relativePath.StartsWith(".." + Path.AltDirectorySeparatorChar, StringComparison.Ordinal),
                $"Combined path should be within base directory for {testPath}");
        }
    }

    /// <summary>
    ///     Test that utilities subsystem rejects dangerous path traversal attempts.
    /// </summary>
    [Fact]
    public void UtilitiesSubsystem_PathTraversalValidation_DangerousPaths_ThrowsException()
    {
        // Arrange: setup base path and dangerous path traversal attempts
        var basePath = Path.GetTempPath();
        var dangerousPaths = new[]
        {
            "../escape.txt",
            "../../escape.txt",
            "subdir/../../../escape.txt"
        };

        // Act & Assert: attempt path traversal and verify exceptions are thrown
        foreach (var dangerousPath in dangerousPaths)
        {
            Assert.Throws<ArgumentException>(() =>
                PathHelpers.SafePathCombine(basePath, dangerousPath));
        }
    }

    /// <summary>
    ///     Test that utilities subsystem rejects absolute path injection at the subsystem level.
    /// </summary>
    [Fact]
    public void UtilitiesSubsystem_AbsolutePathRejection_ThrowsException()
    {
        // Arrange: setup base path and absolute path injection attempts
        var basePath = Path.GetTempPath();
        var absolutePaths = new List<string> { "/etc/passwd" };
        if (OperatingSystem.IsWindows())
        {
            absolutePaths.Add(@"C:\Windows\System32\file.txt");
        }

        // Act & Assert: attempt absolute path injection and verify exceptions are thrown
        foreach (var absolutePath in absolutePaths)
        {
            Assert.Throws<ArgumentException>(() =>
                PathHelpers.SafePathCombine(basePath, absolutePath));
        }
    }

    /// <summary>
    ///     Test that utilities subsystem can handle directory creation workflows.
    /// </summary>
    [Fact]
    public void UtilitiesSubsystem_DirectoryCreationWorkflow_ValidPaths_CreatesDirectories()
    {
        // Arrange: setup two temporary root directories for the test
        using var tmpDir1 = new TemporaryDirectory();
        using var tmpDir2 = new TemporaryDirectory();
        var testDirs = new[]
        {
            tmpDir1.DirectoryPath,
            PathHelpers.SafePathCombine(tmpDir2.DirectoryPath, "subdirectory")
        };

        // Act & Assert: create directories and verify they exist
        foreach (var testDir in testDirs)
        {
            Directory.CreateDirectory(testDir);

            Assert.True(Directory.Exists(testDir),
                $"Directory should be created successfully: {testDir}");
        }
    }

    /// <summary>
    ///     Test that GlobMatcher resolves files matching a wildcard pattern at the subsystem level.
    /// </summary>
    [Fact]
    public void UtilitiesSubsystem_GlobMatcher_ResolvesFiles()
    {
        // Arrange: create a temporary directory with a known file
        using var tmpDir = new TemporaryDirectory();
        var testFile = tmpDir.GetFilePath("test.yaml");
        File.WriteAllText(testFile, "key: value\n");

        // Act: resolve files matching an absolute wildcard pattern
        var files = GlobMatcher.GetFiles([Path.Combine(tmpDir.DirectoryPath, "*.yaml")]);

        // Assert: the test file is found in the resolved list
        Assert.Contains(testFile, files);
    }

    /// <summary>
    ///     Test that path resolution and file matching together resolve output paths and input
    ///     globs containing '..' segments against a base directory.
    /// </summary>
    [Fact]
    public void UtilitiesSubsystem_PathResolution_ParentSegments_ResolvesFiles()
    {
        // Arrange: a shared dictionary beside a config directory
        using var tmpDir = new TemporaryDirectory();
        var shared = tmpDir.GetFilePath(Path.Combine("shared", "terms.yaml"));
        File.WriteAllText(shared, "key: value\n");
        var configDirectory = Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "config")).FullName;

        // Act: resolve an input glob and an output path relative to the config directory
        var inputs = GlobMatcher.Resolve(["sub/../../shared/*.yaml"], configDirectory);
        var output = PathResolver.ResolvePath("../out/terms.md", configDirectory, "output path");

        // Assert: both resolve to normalized paths outside the config directory
        Assert.Equal([Path.GetFullPath(shared)], inputs);
        Assert.Equal(Path.Combine(tmpDir.DirectoryPath, "out", "terms.md"), output);
    }

    /// <summary>
    ///     Test that a root directory containing '..' resolves and serves as the base for input globs
    ///     that also contain '..'.
    /// </summary>
    [Fact]
    public void UtilitiesSubsystem_PathResolution_RootDirectoryWithParentSegments_ResolvesFiles()
    {
        // Arrange: directories a, b and c under a temporary directory, with a dictionary in c
        using var tmpDir = new TemporaryDirectory();
        Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "a"));
        Directory.CreateDirectory(Path.Combine(tmpDir.DirectoryPath, "b"));
        var file = tmpDir.GetFilePath(Path.Combine("c", "terms.yaml"));
        File.WriteAllText(file, "key: value\n");

        // Act: resolve the root "a/../b", then a glob climbing from it into c
        var root = PathResolver.ResolveExistingDirectory("a/../b", tmpDir.DirectoryPath, "--root option");
        var inputs = GlobMatcher.Resolve(["../c/*.yaml"], root);

        // Assert: the root is normalized and the glob finds the dictionary
        Assert.Equal(Path.Combine(tmpDir.DirectoryPath, "b"), root);
        Assert.Equal([Path.GetFullPath(file)], inputs);
    }
}
