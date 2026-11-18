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

namespace DemaConsulting.DictionaryMark.Files;

/// <summary>
///     The exception thrown by <see cref="FileReferenceResolver"/> when it refuses a reference or cannot deliver a file.
/// </summary>
/// <remarks>
///     <para>
///         The exception exists so a caller can turn a failure into its own diagnostic, with its own wording and source
///         location, without parsing text: <see cref="Kind"/> says what went wrong and the other properties carry the
///         facts. A reference that merely names no file is not a failure; the resolver returns <see langword="null"/>.
///         A cancellation is never wrapped: it surfaces as <see cref="OperationCanceledException"/>.
///     </para>
///     <para>
///         <see cref="Message"/> is a complete English sentence that echoes the reference as written (control
///         characters replaced) and names the policy. It never adds the resolved path, the real path or a configured
///         directory, nor the reason of a policy (which may name host paths), so it reveals nothing beyond what the
///         author of the document wrote and is safe to show to that author: an absolute reference appears as the
///         author wrote it. <see cref="RequestedPath"/>, <see cref="RealPath"/> and <see cref="Reason"/> may reveal
///         host directories; show them only to trusted callers.
///     </para>
///     <para>Instances are immutable and safe to use from any thread.</para>
/// </remarks>
public sealed class FileReferenceException : Exception
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FileReferenceException"/> class.
    /// </summary>
    /// <param name="kind">What went wrong.</param>
    /// <param name="reference">
    ///     The reference as written, or for <see cref="FileReferenceFailureKind.InvalidBaseDirectory"/> the configured
    ///     directory; control characters in it are replaced by <c>?</c> when it is stored. Must not be null.
    /// </param>
    /// <param name="reason">
    ///     For the path-text kinds, the rule that was broken (for example <c>network paths are not permitted</c>); for
    ///     <see cref="FileReferenceFailureKind.AccessDenied"/>, the reason of the policy; otherwise
    ///     <see langword="null"/> or an explanation of the platform limitation.
    /// </param>
    /// <param name="innerException">The underlying failure, or <see langword="null"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reference"/> is null.</exception>
    public FileReferenceException(
        FileReferenceFailureKind kind,
        string reference,
        string? reason = null,
        Exception? innerException = null)
        : base(null, innerException)
    {
        ArgumentNullException.ThrowIfNull(reference);
        Kind = kind;
        Reference = Printable(reference);
        Reason = reason;
    }

    /// <summary>
    ///     Gets what went wrong.
    /// </summary>
    public FileReferenceFailureKind Kind { get; }

    /// <summary>
    ///     Gets the reference as written (control characters replaced), or the configured directory for
    ///     <see cref="FileReferenceFailureKind.InvalidBaseDirectory"/>.
    /// </summary>
    public string Reference { get; }

    /// <summary>
    ///     Gets the rule a path text broke, the reason of a policy denial, or <see langword="null"/>. A policy reason
    ///     may name host paths.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    ///     Gets the full path that was opened (or would have been), as written; <see langword="null"/> when the failure
    ///     happened before a path was formed. Reveals host directories.
    /// </summary>
    public string? RequestedPath { get; init; }

    /// <summary>
    ///     Gets the real path of the opened file, when it was determined. Reveals host directories.
    /// </summary>
    public string? RealPath { get; init; }

    /// <summary>
    ///     Gets the name of the file-access policy, for <see cref="FileReferenceFailureKind.AccessDenied"/> and
    ///     <see cref="FileReferenceFailureKind.RealPathUnavailable"/>; otherwise <see langword="null"/>.
    /// </summary>
    public string? PolicyName { get; init; }

    /// <summary>
    ///     Gets how the failure can be resolved (the hint of a policy denial), or <see langword="null"/>.
    /// </summary>
    public string? Hint { get; init; }

    /// <summary>
    ///     Gets the message: a complete English sentence that echoes the reference as written and, for a denial, names
    ///     the policy; it never adds the resolved path, the real path, a configured directory, nor the reason of a policy.
    /// </summary>
    public override string Message => Kind switch
    {
        FileReferenceFailureKind.EmptyPath or FileReferenceFailureKind.ControlCharacter
            or FileReferenceFailureKind.InvalidPath or FileReferenceFailureKind.NetworkPath =>
            $"The path '{Reference}' is not allowed: {Reason}.",
        FileReferenceFailureKind.AccessDenied =>
            $"The file '{Reference}' was denied by the file-access policy '{PolicyName}'.",
        FileReferenceFailureKind.RealPathUnavailable =>
            $"The real path of the file '{Reference}' cannot be determined on this system, so the file-access policy "
            + $"'{PolicyName}' cannot be applied and the file is not read.",
        FileReferenceFailureKind.NotARegularFile => $"The file '{Reference}' is not a regular file.",
        FileReferenceFailureKind.CannotRead => $"The file '{Reference}' cannot be read.",
        FileReferenceFailureKind.TooLarge => $"The file '{Reference}' is larger than the allowed size.",
        FileReferenceFailureKind.InvalidBaseDirectory =>
            "A configured directory (BaseDirectory or SearchPaths) is not a valid path.",
        _ => $"The file '{Reference}' was refused.",
    };

    /// <summary>
    ///     Gets a text as it may be shown in a message: control characters replaced by <c>?</c>.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <returns>The printable text.</returns>
    internal static string Printable(string text) =>
        text.Any(char.IsControl) ? new string(text.Select(c => char.IsControl(c) ? '?' : c).ToArray()) : text;
}
