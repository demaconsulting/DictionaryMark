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
///     The answer of a <see cref="FileAccessPolicy"/> about one opened file: allowed, or denied with a reason.
/// </summary>
/// <remarks>
///     <para>
///         A decision exists so a policy can explain a denial in words that end up in the caller's diagnostic, without
///         throwing: <see cref="Reason"/> says why (a sentence fragment such as <c>its real path is outside the allowed
///         roots</c>, without a trailing period), and <see cref="Hint"/> optionally says how the author or the host can
///         fix it. <see cref="FileReferenceResolver"/> turns a denial into a <see cref="FileReferenceException"/> of kind
///         <see cref="FileReferenceFailureKind.AccessDenied"/> that carries both.
///     </para>
///     <para>Instances are immutable and safe to use from any thread.</para>
/// </remarks>
internal sealed record FileAccessDecision
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="FileAccessDecision"/> class.
    /// </summary>
    /// <param name="isAllowed">Whether the file may be read.</param>
    /// <param name="reason">The reason of a denial, or <see langword="null"/>.</param>
    /// <param name="hint">The hint of a denial, or <see langword="null"/>.</param>
    private FileAccessDecision(bool isAllowed, string? reason, string? hint)
    {
        IsAllowed = isAllowed;
        Reason = reason;
        Hint = hint;
    }

    /// <summary>
    ///     Gets the decision that allows the file to be read. The same instance is returned every time.
    /// </summary>
    public static FileAccessDecision Allow { get; } = new(true, null, null);

    /// <summary>
    ///     Gets a value indicating whether the file may be read.
    /// </summary>
    public bool IsAllowed { get; }

    /// <summary>
    ///     Gets why the file was denied (a sentence fragment without a trailing period), or <see langword="null"/> when it
    ///     is allowed. A reason may name host paths; show it only to trusted callers.
    /// </summary>
    public string? Reason { get; }

    /// <summary>
    ///     Gets how a denial can be resolved (a sentence fragment without a trailing period), or <see langword="null"/>.
    /// </summary>
    public string? Hint { get; }

    /// <summary>
    ///     Creates a decision that denies the file.
    /// </summary>
    /// <param name="reason">
    ///     Why the file is denied: a non-empty sentence fragment without a trailing period, for example
    ///     <c>its real path is outside the allowed roots</c>.
    /// </param>
    /// <param name="hint">How the denial can be resolved, or <see langword="null"/> for no hint.</param>
    /// <returns>The denial.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="reason"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="reason"/> is empty or white space.</exception>
    public static FileAccessDecision Deny(string reason, string? hint = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        return new FileAccessDecision(false, reason, string.IsNullOrWhiteSpace(hint) ? null : hint);
    }
}
