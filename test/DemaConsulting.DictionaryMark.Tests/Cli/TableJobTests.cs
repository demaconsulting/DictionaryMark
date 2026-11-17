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
using DemaConsulting.DictionaryMark.Dictionary;

namespace DemaConsulting.DictionaryMark.Tests.Cli;

/// <summary>
///     Unit tests for the TableJob class and its FormattingSettings layers.
/// </summary>
public class TableJobTests
{
    /// <summary>Base directory used by the tests; never accessed on disk.</summary>
    private static readonly string BaseDirectory = Path.GetTempPath();

    /// <summary>Test that the command-line layer overrides the job and default layers.</summary>
    [Fact]
    public void TableJob_Create_CommandLineLayer_OverridesJobAndDefaults()
    {
        // Arrange: three layers that all set the format and section heading
        var commandLine = new FormattingSettings { Format = OutputFormat.Table, SectionHeading = "CLI" };
        var job = new FormattingSettings { Format = OutputFormat.Bullets, SectionHeading = "Job", SortBy = SortOrder.Alphabetical };
        var defaults = new FormattingSettings { SectionHeading = "Defaults", HeadingDepth = 3 };

        // Act: create the job with the layers in precedence order
        var result = TableJob.Create(1, ["a.yaml"], "out.md", BaseDirectory, [commandLine, job, defaults]);

        // Assert: the command line wins where it is set; lower layers fill the rest
        Assert.Equal(OutputFormat.Table, result.Options.Format);
        Assert.Equal("CLI", result.Options.SectionHeading);
        Assert.Equal(SortOrder.Alphabetical, result.Options.SortOrder);
        Assert.Equal(3, result.Options.HeadingDepth);
    }

    /// <summary>Test that the job layer overrides the default layer.</summary>
    [Fact]
    public void TableJob_Create_JobLayer_OverridesDefaults()
    {
        // Arrange: a job layer and a default layer that both set the definition header
        var job = new FormattingSettings { DefinitionHeader = "Meaning" };
        var defaults = new FormattingSettings { DefinitionHeader = "Description", UseHeader = "Prefer" };

        // Act: create the job without a command-line layer
        var result = TableJob.Create(1, ["a.yaml"], "out.md", BaseDirectory, [null, job, defaults]);

        // Assert: the job wins; the defaults fill the rest
        Assert.Equal("Meaning", result.Options.DefinitionHeader);
        Assert.Equal("Prefer", result.Options.UseHeader);
    }

    /// <summary>Test that the built-in defaults apply when no layer sets a value.</summary>
    [Fact]
    public void TableJob_Create_NoLayers_UsesBuiltInDefaults()
    {
        // Act: create a command-line job without any layer
        var result = TableJob.Create(null, ["a.yaml"], null, BaseDirectory, []);

        // Assert: every option has its built-in default
        Assert.Equal(OutputFormat.Bullets, result.Options.Format);
        Assert.Null(result.Options.SectionHeading);
        Assert.Equal(1, result.Options.HeadingDepth);
        Assert.Equal(SortOrder.FileOrder, result.Options.SortOrder);
        Assert.Equal(InputKind.Terms, result.Options.Kind);
        Assert.Equal("Term", result.Options.TermHeader);
        Assert.Equal("Definition", result.Options.DefinitionHeader);
        Assert.Equal("Use instead", result.Options.UseHeader);
        Assert.Equal("Where it was used", result.Options.WhereHeader);
        Assert.Null(result.Options.ColumnWidths);
        Assert.Null(result.OutputFile);
        Assert.Equal(BaseDirectory, result.BaseDirectory);
    }

    /// <summary>Test that the deprecated kind uses "Not used" when no layer sets the term header.</summary>
    [Fact]
    public void TableJob_Create_DeprecatedWithoutTermHeader_UsesNotUsed()
    {
        // Arrange: a default layer selecting the deprecated kind
        var defaults = new FormattingSettings { Kind = InputKind.Deprecated };

        // Act: create jobs with and without an explicit term header
        var implicitHeader = TableJob.Create(1, ["a.yaml"], "out.md", BaseDirectory, [null, null, defaults]);
        var explicitHeader = TableJob.Create(
            1, ["a.yaml"], "out.md", BaseDirectory, [null, new FormattingSettings { TermHeader = "Avoid" }, defaults]);

        // Assert: "Not used" applies only when no term header was given
        Assert.Equal("Not used", implicitHeader.Options.TermHeader);
        Assert.Equal("Avoid", explicitHeader.Options.TermHeader);
    }

    /// <summary>Test that a terms table with three widths is rejected.</summary>
    [Fact]
    public void TableJob_Create_TermsWithThreeWidths_Throws()
    {
        // Arrange: a terms table with three widths
        var layer = new FormattingSettings { Format = OutputFormat.Table, ColumnWidths = [1, 2, 3] };

        // Act: create the job
        var ex = Assert.Throws<ArgumentException>(() => TableJob.Create(2, ["a.yaml"], "out.md", BaseDirectory, [layer]));

        // Assert: the message names the job and the required count
        Assert.Contains("table job 2 ('out.md')", ex.Message);
        Assert.Contains("terms tables require exactly 2 values", ex.Message);
    }

    /// <summary>Test that a deprecated table with two widths is rejected.</summary>
    [Fact]
    public void TableJob_Create_DeprecatedWithTwoWidths_Throws()
    {
        // Arrange: a deprecated table with two widths
        var layer = new FormattingSettings { Format = OutputFormat.Table, Kind = InputKind.Deprecated, ColumnWidths = [1, 2] };

        // Act: create the command-line job
        var ex = Assert.Throws<ArgumentException>(() => TableJob.Create(null, ["a.yaml"], null, BaseDirectory, [layer]));

        // Assert: the message names the required count
        Assert.Contains("deprecated tables require exactly 3 values", ex.Message);
    }

    /// <summary>Test that bullet output silently ignores widths of any count.</summary>
    [Fact]
    public void TableJob_Create_BulletsWithMismatchedWidths_IgnoresWidths()
    {
        // Arrange: bullet output with three widths for a terms dictionary
        var layer = new FormattingSettings { Format = OutputFormat.Bullets, ColumnWidths = [1, 2, 3] };

        // Act: create the job
        var result = TableJob.Create(1, ["a.yaml"], "out.md", BaseDirectory, [layer]);

        // Assert: no error and no widths reach the formatter
        Assert.Null(result.Options.ColumnWidths);
    }

    /// <summary>Test that a matching width count reaches the formatting options.</summary>
    [Fact]
    public void TableJob_Create_TableWithMatchingWidths_SetsColumnWidths()
    {
        // Arrange: a terms table with two widths
        var layer = new FormattingSettings { Format = OutputFormat.Table, ColumnWidths = [25, 75] };

        // Act: create the job
        var result = TableJob.Create(1, ["a.yaml"], "out.md", BaseDirectory, [layer]);

        // Assert: the widths are passed on
        Assert.Equal([25, 75], result.Options.ColumnWidths!);
    }

    /// <summary>Test that a config job is labeled with its index and output, and a command-line job is not.</summary>
    [Fact]
    public void TableJob_Create_Label_NamesIndexAndOutput()
    {
        // Act: create a config job and a command-line job
        var configJob = TableJob.Create(3, ["a.yaml", "b.yaml"], "docs/out.md", BaseDirectory, []);
        var commandLineJob = TableJob.Create(null, ["a.yaml"], "out.md", BaseDirectory, []);

        // Assert: only the config job has a label, naming its index and output as written
        Assert.Equal("table job 3 ('docs/out.md')", configJob.Label);
        Assert.Equal(3, configJob.Index);
        Assert.Equal(["a.yaml", "b.yaml"], configJob.InputPatterns);
        Assert.Null(commandLineJob.Label);
        Assert.Null(commandLineJob.Index);
    }

    /// <summary>Test that a job without inputs is rejected.</summary>
    [Fact]
    public void TableJob_Create_NoInputs_Throws()
    {
        // Act / Assert: an empty input list is rejected
        Assert.Throws<ArgumentException>(() => TableJob.Create(1, [], "out.md", BaseDirectory, []));
    }

    /// <summary>Test that the non-null members of the higher layer win in a merge.</summary>
    [Fact]
    public void FormattingSettings_Over_NonNullWins()
    {
        // Arrange: a higher and a lower layer with overlapping members
        var higher = new FormattingSettings { Format = OutputFormat.Table, ColumnWidths = [1, 2] };
        var lower = new FormattingSettings { Format = OutputFormat.Bullets, Kind = InputKind.Deprecated, WhereHeader = "Seen" };

        // Act: merge the higher layer over the lower one
        var merged = higher.Over(lower);

        // Assert: higher values win; lower values fill the gaps
        Assert.Equal(OutputFormat.Table, merged.Format);
        Assert.Equal(InputKind.Deprecated, merged.Kind);
        Assert.Equal("Seen", merged.WhereHeader);
        Assert.Equal([1, 2], merged.ColumnWidths!);
        Assert.Null(merged.TermHeader);
    }
}
