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
///     Unit tests for <see cref="FileReferenceException"/>: it keeps its facts and its message never names a host
///     directory nor the reason of a policy.
/// </summary>
public class FileReferenceExceptionTests
{
    /// <summary>
    ///     Validates, for every failure kind, that the exception keeps its facts and that the message is a sentence that
    ///     never contains a host path, and contains the reason only for a rule of the path text.
    /// </summary>
    /// <param name="kind">The failure kind.</param>
    [Theory]
    [MemberData(nameof(Kinds))]
    public void FileReferenceException_Constructor_EveryKind_KeepsFactsAndHidesHostPath(FileReferenceFailureKind kind)
    {
        // Arrange: a path-text failure carries a rule; every other failure carries a reason that names a host path
        var host = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "secret-host-folder"));
        var textKind = kind is FileReferenceFailureKind.EmptyPath or FileReferenceFailureKind.ControlCharacter
            or FileReferenceFailureKind.InvalidPath or FileReferenceFailureKind.NetworkPath;
        var reason = textKind ? "network paths are not permitted" : "its real path " + host + " is outside";
        var cause = new IOException("cause");

        // Act
        var ex = new FileReferenceException(kind, "../images/logo.png", reason, cause)
        {
            RequestedPath = Path.Combine(host, "logo.png"),
            RealPath = Path.Combine(host, "real.png"),
            PolicyName = "roots",
            Hint = "a hint",
        };

        // Assert: the facts are kept
        Assert.Equal(kind, ex.Kind);
        Assert.Equal("../images/logo.png", ex.Reference);
        Assert.Equal(reason, ex.Reason);
        Assert.Equal(Path.Combine(host, "logo.png"), ex.RequestedPath);
        Assert.Equal(Path.Combine(host, "real.png"), ex.RealPath);
        Assert.Equal("roots", ex.PolicyName);
        Assert.Equal("a hint", ex.Hint);
        Assert.Same(cause, ex.InnerException);

        // Assert: the message is a sentence without a host path; only a path-text failure states its rule
        Assert.EndsWith(".", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-host-folder", ex.Message, StringComparison.Ordinal);
        Assert.Equal(textKind, ex.Message.Contains(reason, StringComparison.Ordinal));
    }

    /// <summary>
    ///     Validates that a null reference is an argument error.
    /// </summary>
    [Fact]
    public void FileReferenceException_Constructor_NullReference_ThrowsArgumentNullException()
    {
        // Act / Assert
        Assert.Throws<ArgumentNullException>(() => new FileReferenceException(FileReferenceFailureKind.CannotRead, null!));
    }

    /// <summary>
    ///     Validates that a reference with control characters, passed straight to the public constructor, is stored and
    ///     shown with each control character replaced by <c>?</c>.
    /// </summary>
    [Fact]
    public void FileReferenceException_Constructor_ControlCharacters_AreReplacedInReferenceAndMessage()
    {
        // Act
        var ex = new FileReferenceException(FileReferenceFailureKind.CannotRead, "a\0b\n");

        // Assert
        Assert.Equal("a?b?", ex.Reference);
        Assert.Contains("'a?b?'", ex.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(ex.Message, char.IsControl);
    }

    /// <summary>
    ///     Gets every failure kind.
    /// </summary>
    /// <returns>The kinds.</returns>
    public static TheoryData<FileReferenceFailureKind> Kinds() => [.. Enum.GetValues<FileReferenceFailureKind>()];
}
