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

namespace DemaConsulting.DictionaryMark.Utilities;

/// <summary>
///     Reports that the file-access policy denied a path named in a configuration file.
/// </summary>
/// <remarks>
///     Deliberately not derived from <see cref="IOException"/>, so the input/output error handlers of the
///     generator never mistake a denial for a read or write failure. The message starts with the error code
///     <c>file-access-denied</c>, then names the path as written, its real path when one is known, and the reason,
///     which names the mode when the mode caused the denial.
/// </remarks>
public sealed class FileAccessDeniedException : Exception
{
    /// <summary>The error code that starts every denial message.</summary>
    public const string ErrorCode = "file-access-denied";

    /// <summary>
    ///     Initializes a new instance of the <see cref="FileAccessDeniedException"/> class.
    /// </summary>
    /// <param name="writtenPath">The path as written in the configuration file (or the matched file). Must not be null.</param>
    /// <param name="realPath">The real path of the opened file or folder, or null when it is not known.</param>
    /// <param name="reason">Why the path is denied. Must not be null.</param>
    public FileAccessDeniedException(string writtenPath, string? realPath, string reason)
        : base(FormatMessage(writtenPath, realPath, reason))
    {
        WrittenPath = writtenPath;
        RealPath = realPath;
        Reason = reason;
    }

    /// <summary>Gets the path as written.</summary>
    public string WrittenPath { get; }

    /// <summary>Gets the real path, or null when it is not known.</summary>
    public string? RealPath { get; }

    /// <summary>Gets the reason for the denial.</summary>
    public string Reason { get; }

    /// <summary>
    ///     Formats the denial message: <c>file-access-denied: '{written}' (real path '{real}'): {reason}</c>, without
    ///     the real-path part when the real path is not known.
    /// </summary>
    /// <param name="writtenPath">The path as written.</param>
    /// <param name="realPath">The real path, or null.</param>
    /// <param name="reason">The reason.</param>
    /// <returns>The message.</returns>
    private static string FormatMessage(string writtenPath, string? realPath, string reason)
    {
        ArgumentNullException.ThrowIfNull(writtenPath);
        ArgumentNullException.ThrowIfNull(reason);
        return realPath == null
            ? $"{ErrorCode}: '{writtenPath}': {reason}"
            : $"{ErrorCode}: '{writtenPath}' (real path '{realPath}'): {reason}";
    }
}
