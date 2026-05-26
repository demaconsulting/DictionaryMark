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

using System.Diagnostics;

namespace DemaConsulting.DictionaryMark.Tests.Helpers;

/// <summary>
///     Program runner class for integration testing.
/// </summary>
internal static class Runner
{
    /// <summary>
    ///     Runs the specified program and captures its output.
    /// </summary>
    /// <remarks>
    ///     Delegates to the overload that captures stdout and stderr separately, then concatenates
    ///     them in that order (stdout first, stderr second). See that overload for details on
    ///     deadlock-prevention and timeout behavior.
    /// </remarks>
    /// <param name="output">Program output (stdout concatenated before stderr).</param>
    /// <param name="program">Program name or path.</param>
    /// <param name="arguments">Program arguments.</param>
    /// <returns>Program exit code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when process fails to start.</exception>
    /// <exception cref="TimeoutException">Thrown when the process does not exit within 30 seconds.</exception>
    public static int Run(out string output, string program, params string[] arguments)
    {
        // Delegate to the overload that separates stdout and stderr, then merge them
        var exitCode = Run(out var stdout, out var stderr, program, arguments);
        output = stdout + stderr;
        return exitCode;
    }

    /// <summary>
    ///     Runs the specified program and captures its standard output and standard error separately.
    /// </summary>
    /// <remarks>
    ///     Stdout and stderr are read concurrently on background tasks to prevent the OS pipe buffers
    ///     from filling and deadlocking the process. The process is given 30 seconds to exit; if it
    ///     does not exit in time it is forcibly terminated and a <see cref="TimeoutException"/> is
    ///     thrown.
    /// </remarks>
    /// <param name="stdout">Program standard output.</param>
    /// <param name="stderr">Program standard error.</param>
    /// <param name="program">Program name or path.</param>
    /// <param name="arguments">Program arguments.</param>
    /// <returns>Program exit code.</returns>
    /// <exception cref="InvalidOperationException">Thrown when process fails to start.</exception>
    /// <exception cref="TimeoutException">Thrown when the process does not exit within 30 seconds.</exception>
    public static int Run(out string stdout, out string stderr, string program, params string[] arguments)
    {
        // Construct the start information
        var startInfo = new ProcessStartInfo(program)
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        // Add the arguments
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // Start the process
        using var process = Process.Start(startInfo) ??
                            throw new InvalidOperationException("Failed to start process");

        // Read stdout and stderr asynchronously to avoid buffer overflow
        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        // Wait for the process to exit with a 30-second timeout
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        try
        {
            process.WaitForExitAsync(cts.Token).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            try { process.Kill(entireProcessTree: true); } catch { /* best effort */ }
            stdout = string.Empty;
            stderr = string.Empty;
            throw new TimeoutException($"Process '{program}' did not exit within 30 seconds.");
        }

        // Save each stream separately and return the exit code
        stdout = stdoutTask.GetAwaiter().GetResult();
        stderr = stderrTask.GetAwaiter().GetResult();
        return process.ExitCode;
    }
}
