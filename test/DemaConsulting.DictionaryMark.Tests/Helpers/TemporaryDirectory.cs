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

namespace DemaConsulting.DictionaryMark.Tests.Helpers;

/// <summary>
///     A disposable temporary directory created under the current working directory.
/// </summary>
/// <remarks>
///     Using <see cref="Environment.CurrentDirectory"/> as the base avoids OS symlink
///     issues such as <c>/tmp</c> resolving to <c>/private/tmp</c> on macOS, which can
///     cause path-comparison failures when the OS returns the real (resolved) path.
/// </remarks>
internal sealed class TemporaryDirectory : IDisposable
{
    /// <summary>
    ///     Gets the full path to the temporary directory.
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    ///     Initializes a new instance of the <see cref="TemporaryDirectory"/> class,
    ///     creating a uniquely-named subdirectory under the current working directory.
    /// </summary>
    public TemporaryDirectory()
    {
        DirectoryPath = Path.Combine(Environment.CurrentDirectory, $"test-tmp-{Guid.NewGuid():N}");
        Directory.CreateDirectory(DirectoryPath);
    }

    /// <summary>
    ///     Returns the full path to a file within the temporary directory.
    /// </summary>
    /// <param name="fileName">The file name (no path separators).</param>
    /// <returns>The combined full path.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when <paramref name="fileName"/> contains a directory separator character.
    /// </exception>
    public string GetFilePath(string fileName)
    {
        if (fileName.Contains(Path.DirectorySeparatorChar) ||
            fileName.Contains(Path.AltDirectorySeparatorChar))
        {
            throw new ArgumentException(
                $"The file name must not contain path separator characters, but was: '{fileName}'",
                nameof(fileName));
        }

        return Path.Combine(DirectoryPath, fileName);
    }

    /// <summary>
    ///     Deletes the temporary directory and all its contents.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(DirectoryPath))
            {
                Directory.Delete(DirectoryPath, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Ignore cleanup errors during disposal
        }
    }
}
