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

using DemaConsulting.DictionaryMark.Cli;
using DemaConsulting.DictionaryMark.SelfTest;
using DemaConsulting.DictionaryMark.Tests.Helpers;

namespace DemaConsulting.DictionaryMark.Tests.SelfTest;

/// <summary>
///     Subsystem tests for the SelfTest subsystem covering Validation workflows.
/// </summary>
[Collection("Sequential")]
public class SelfTestSubsystemTests
{
    /// <summary>
    ///     Test that self-test subsystem can run validation workflow without result files.
    /// </summary>
    [Fact]
    public void SelfTestSubsystem_ValidationWorkflow_NoResultFiles_CompletesSuccessfully()
    {
        // Arrange: command line arguments for validation in silent mode
        var args = new[] { "--validate", "--silent" };

        // Act: create context and run validation
        using var context = Context.Create(args);
        Validation.Run(context);

        // Assert: validation completes successfully with correct flags set
        Assert.True(context.Validate, "Context should have validate flag set");
        Assert.Equal(0, context.ExitCode);
    }

    /// <summary>
    ///     Test that self-test subsystem can run validation workflow with TRX result file.
    /// </summary>
    [Fact]
    public void SelfTestSubsystem_ValidationWorkflow_WithTrxFile_GeneratesResults()
    {
        // Arrange: temporary TRX file path and validation command with results output
        using var tmpDir = new TemporaryDirectory();
        var trxFile = tmpDir.GetFilePath("results.trx");
        var args = new[] { "--validate", "--silent", "--results", trxFile };

        // Act: create context and run validation with TRX output
        using var context = Context.Create(args);
        Validation.Run(context);

        // Assert: validation completes and generates TRX file with standard format
        Assert.True(context.Validate, "Context should have validate flag set");
        Assert.Equal(0, context.ExitCode);
        Assert.True(File.Exists(trxFile), "TRX file should be generated");
        var trxContent = File.ReadAllText(trxFile);
        Assert.Contains("<TestRun", trxContent);
    }

    /// <summary>
    ///     Test that self-test subsystem can run validation workflow with JUnit result file.
    /// </summary>
    [Fact]
    public void SelfTestSubsystem_ValidationWorkflow_WithJUnitFile_GeneratesResults()
    {
        // Arrange: temporary JUnit XML file path and validation command with results output
        using var tmpDir = new TemporaryDirectory();
        var junitFile = tmpDir.GetFilePath("results.xml");
        var args = new[] { "--validate", "--silent", "--results", junitFile };

        // Act: create context and run validation with JUnit XML output
        using var context = Context.Create(args);
        Validation.Run(context);

        // Assert: validation completes and generates JUnit XML file with standard format
        Assert.True(context.Validate, "Context should have validate flag set");
        Assert.Equal(0, context.ExitCode);
        Assert.True(File.Exists(junitFile), "JUnit file should be generated");
        var junitContent = File.ReadAllText(junitFile);
        Assert.Contains("<testsuites", junitContent);
    }

    /// <summary>
    ///     Test that self-test subsystem can run validation workflow with both result file formats.
    /// </summary>
    [Fact]
    public void SelfTestSubsystem_ValidationWorkflow_WithBothResultFiles_GeneratesBothResults()
    {
        // Arrange: setup validation arguments for both TRX and JUnit result file outputs
        using var tmpDir = new TemporaryDirectory();
        var trxFile = tmpDir.GetFilePath("results.trx");
        var junitFile = tmpDir.GetFilePath("results.xml");
        var trxArgs = new[] { "--validate", "--silent", "--results", trxFile };
        var junitArgs = new[] { "--validate", "--silent", "--results", junitFile };

        // Act: run validation with TRX output
        using var trxContext = Context.Create(trxArgs);
        Validation.Run(trxContext);

        // Assert: verify validation completed and TRX result file was generated with standard format
        Assert.True(trxContext.Validate, "Context should have validate flag set for TRX run");
        Assert.Equal(0, trxContext.ExitCode);
        Assert.True(File.Exists(trxFile), "TRX file should be generated");
        var trxContent = File.ReadAllText(trxFile);
        Assert.Contains("<TestRun", trxContent);

        // Act: run validation with JUnit XML output
        using var junitContext = Context.Create(junitArgs);
        Validation.Run(junitContext);

        // Assert: verify validation completed and JUnit XML result file was generated with standard format
        Assert.True(junitContext.Validate, "Context should have validate flag set for JUnit run");
        Assert.Equal(0, junitContext.ExitCode);
        Assert.True(File.Exists(junitFile), "JUnit file should be generated");
        var junitContent = File.ReadAllText(junitFile);
        Assert.Contains("<testsuites", junitContent);
    }

    /// <summary>
    ///     Test that self-test subsystem with unsupported results extension emits an error and does not create the file.
    /// </summary>
    [Fact]
    public void SelfTestSubsystem_ValidationWorkflow_WithUnsupportedExtension_EmitsErrorAndNoFile()
    {
        // Arrange: unsupported results file extension; capture stderr for error message assertion
        using var tmpDir = new TemporaryDirectory();
        var badFile = tmpDir.GetFilePath("results.bad");
        var args = new[] { "--validate", "--results", badFile };
        var originalError = Console.Error;

        try
        {
            using var errWriter = new StringWriter();
            Console.SetError(errWriter);

            // Act: create context and run validation with unsupported extension
            using var context = Context.Create(args);
            Validation.Run(context);

            // Assert: error is reported, message identifies the format, and no file is created
            Assert.Equal(1, context.ExitCode);
            Assert.False(File.Exists(badFile), "No file should be created for unsupported extension");
            Assert.Contains(".bad", errWriter.ToString());
        }
        finally
        {
            Console.SetError(originalError);
        }
    }
}
