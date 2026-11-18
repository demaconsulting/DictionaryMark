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

using System.Globalization;
using DemaConsulting.DictionaryMark.Dictionary;
using DemaConsulting.DictionaryMark.Utilities;

namespace DemaConsulting.DictionaryMark.Cli;

/// <summary>
///     Context class that handles command-line arguments and program output.
/// </summary>
internal sealed class Context : IDisposable
{
    /// <summary>
    ///     Log file stream writer (if logging is enabled).
    /// </summary>
    private StreamWriter? _logWriter;

    /// <summary>
    ///     Indicates whether errors have been reported.
    /// </summary>
    private bool _hasErrors;

    /// <summary>
    ///     Gets a value indicating whether the version flag was specified.
    /// </summary>
    public bool Version { get; private init; }

    /// <summary>
    ///     Gets a value indicating whether the help flag was specified.
    /// </summary>
    public bool Help { get; private init; }

    /// <summary>
    ///     Gets a value indicating whether the silent flag was specified.
    /// </summary>
    public bool Silent { get; private init; }

    /// <summary>
    ///     Gets a value indicating whether the validate flag was specified.
    /// </summary>
    public bool Validate { get; private init; }

    /// <summary>
    ///     Gets the validation results file path.
    /// </summary>
    public string? ResultsFile { get; private init; }

    /// <summary>
    ///     Gets the heading depth for markdown output (default is 1).
    /// </summary>
    public int HeadingDepth { get; private init; } = 1;

    /// <summary>Gets the input file patterns (can be specified multiple times with --input or -i).</summary>
    public List<string> InputPatterns { get; } = [];

    /// <summary>Gets the output file path (--output or -o). Null means write to stdout.</summary>
    public string? OutputFile { get; private init; }

    /// <summary>Gets the output format (--format or -f). Default is Bullets.</summary>
    public OutputFormat Format { get; private init; } = OutputFormat.Bullets;

    /// <summary>Gets the optional section heading text (--section or -s).</summary>
    public string? SectionHeading { get; private init; }

    /// <summary>Gets the term column header for table format (--term-header). Default is "Term".</summary>
    public string TermHeader { get; private init; } = "Term";

    /// <summary>Gets the definition column header for table format (--def-header or --definition-header). Default is "Definition".</summary>
    public string DefinitionHeader { get; private init; } = "Definition";

    /// <summary>Gets the sort order (--sort). Default is FileOrder.</summary>
    public SortOrder SortBy { get; private init; } = SortOrder.FileOrder;

    /// <summary>Gets the kind of input (--kind): Terms (default) or Deprecated.</summary>
    public InputKind Kind { get; private init; } = InputKind.Terms;

    /// <summary>Gets a value indicating whether a term header was given on the command line or in the config file defaults.</summary>
    public bool TermHeaderSpecified { get; private init; }

    /// <summary>Gets the "use instead" column header for deprecated table format (--use-header). Default is "Use instead".</summary>
    public string UseHeader { get; private init; } = "Use instead";

    /// <summary>Gets the "where used" column header for deprecated table format (--where-header). Default is "Where it was used".</summary>
    public string WhereHeader { get; private init; } = "Where it was used";

    /// <summary>
    ///     Gets the effective top-level relative table column widths (--column-widths or the config
    ///     file's <c>column-widths</c>), or null when none were given.
    /// </summary>
    public IReadOnlyList<int>? ColumnWidths { get; private init; }

    /// <summary>
    ///     Gets the resolved full path of the configuration file (--config), or null when none was given.
    /// </summary>
    public string? ConfigFilePath { get; private init; }

    /// <summary>
    ///     Gets the effective root directory for relative input and output paths: the resolved
    ///     --root value, else the config file's resolved <c>root</c> key, else null.
    /// </summary>
    public string? RootDirectory { get; private init; }

    /// <summary>
    ///     Gets the formatting options given explicitly on the command line; members that were not
    ///     given are null. This is the highest-precedence formatting layer.
    /// </summary>
    public FormattingSettings CommandLineFormatting { get; private init; } = FormattingSettings.Empty;

    /// <summary>
    ///     Gets the generation jobs to run: one per config-file table, or one command-line job when
    ///     --input was given, or none.
    /// </summary>
    public IReadOnlyList<TableJob> Jobs { get; private init; } = [];

    /// <summary>
    ///     Gets the file-access policy (--file-access and --allow-root) that judges every input and output
    ///     path written in a config file; the default is mode any.
    /// </summary>
    public FileAccessPolicy FileAccess { get; private init; } = FileAccessPolicy.Any;

    /// <summary>
    ///     Gets a value indicating whether the jobs come from the <c>tables</c> of a config file.
    /// </summary>
    public bool UsesConfigTables { get; private init; }

    /// <summary>
    ///     Gets the proposed exit code for the application (0 for success, 1 for errors).
    /// </summary>
    public int ExitCode => _hasErrors ? 1 : 0;

    /// <summary>
    ///     Private constructor - use Create factory method instead.
    /// </summary>
    private Context()
    {
    }

    /// <summary>
    ///     Creates a Context instance from command-line arguments.
    /// </summary>
    /// <remarks>
    ///     Uses the factory pattern to keep the constructor private, preventing partial initialization. Parsing is
    ///     delegated to the inner <c>ArgumentParser</c> helper so that validation and default-setting logic are
    ///     isolated from the immutable <c>Context</c> properties. This separation means <c>Context</c> is always
    ///     fully initialized or never created. The steps are, in order: parse the arguments (including the
    ///     --file-access mode and its conflicts with --allow-root); resolve --root
    ///     against the working directory; resolve --config against the working directory (never the root) and
    ///     load it, which also resolves its <c>root</c> key; build the file-access policy, whose allowed folders
    ///     are the --allow-root folders (resolved against the working directory, never the root) or else the
    ///     working directory and the config file's folder; reject --input/--output when the config defines
    ///     tables; build the jobs with the base directory --root, else config <c>root</c>, else the path's origin
    ///     default; and finally open the log file. Every validation failure stops the run before any job runs.
    ///     Performs file-system I/O (root existence, config file, allowed folders, log file). Stateless after
    ///     construction; thread-safe for concurrent reads.
    /// </remarks>
    /// <param name="args">Command-line arguments. Must not be null.</param>
    /// <returns>A fully initialized <see cref="Context"/> with all parsed properties set.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when arguments are invalid, an option is repeated, --input or --output is combined with a
    ///     config file that defines tables, the column-width count does not match the table shape, the
    ///     --file-access value is not a mode, or --allow-root is combined with --file-access any or no-symlinks.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the root directory or config file path is malformed, invalid or missing, the config file is
    ///     missing or invalid, an --allow-root folder is malformed or missing, or the specified log file cannot be
    ///     opened.
    /// </exception>
    public static Context Create(string[] args)
    {
        // Validate input
        ArgumentNullException.ThrowIfNull(args);

        var parser = new ArgumentParser();
        parser.ParseArguments(args);

        // Resolve the command-line root against the working directory
        var commandLineRoot = parser.RootDirectory != null
            ? PathResolver.ResolveExistingDirectory(parser.RootDirectory, Environment.CurrentDirectory, "--root option")
            : null;

        // Locate and load the config file (relative to the working directory, never the root)
        string? configPath = null;
        ConfigFile? config = null;
        if (parser.ConfigPath != null)
        {
            configPath = PathResolver.ResolvePath(parser.ConfigPath, Environment.CurrentDirectory, "config file path");
            config = ConfigFile.Load(configPath);
        }

        // Select the effective root and the effective top-level formatting settings
        var rootDirectory = commandLineRoot ?? config?.RootDirectory;
        var commandLine = parser.Formatting;
        var effective = commandLine.Over(config?.Defaults ?? FormattingSettings.Empty);

        // Build the file-access policy that judges the config file's paths
        var fileAccess = BuildFileAccessPolicy(parser, configPath);

        // Build the generation jobs
        var jobs = BuildJobs(parser, config, rootDirectory);

        var result = new Context
        {
            Version = parser.Version,
            Help = parser.Help,
            Silent = parser.Silent,
            Validate = parser.Validate,
            ResultsFile = parser.ResultsFile,
            HeadingDepth = effective.HeadingDepth ?? 1,
            OutputFile = parser.OutputFile,
            Format = effective.Format ?? OutputFormat.Bullets,
            SectionHeading = effective.SectionHeading,
            TermHeader = effective.TermHeader ?? "Term",
            DefinitionHeader = effective.DefinitionHeader ?? "Definition",
            SortBy = effective.SortBy ?? SortOrder.FileOrder,
            Kind = effective.Kind ?? InputKind.Terms,
            TermHeaderSpecified = effective.TermHeader != null,
            UseHeader = effective.UseHeader ?? "Use instead",
            WhereHeader = effective.WhereHeader ?? "Where it was used",
            ColumnWidths = effective.ColumnWidths,
            ConfigFilePath = configPath,
            RootDirectory = rootDirectory,
            CommandLineFormatting = commandLine,
            Jobs = jobs,
            FileAccess = fileAccess,
            UsesConfigTables = config is { Tables.Count: > 0 }
        };

        // Copy input patterns
        result.InputPatterns.AddRange(parser.InputPatterns);

        // Open log file if specified
        if (parser.LogFile != null)
        {
            result.OpenLogFile(parser.LogFile);
        }

        return result;
    }

    /// <summary>
    ///     Builds the file-access policy from --file-access and --allow-root.
    /// </summary>
    /// <remarks>
    ///     The mode is the --file-access value, or roots when only --allow-root is given, or any. Under roots the
    ///     allowed folders are the --allow-root folders, each resolved against the working directory (never the
    ///     root directory) with <c>${VAR}</c> expansion and <c>..</c> normalization and required to exist; without
    ///     --allow-root they are the working directory and the config file's folder. The config file's folder is
    ///     the trusted base of the no-symlinks rule. Neither --root nor the config <c>root</c> key adds an allowed
    ///     folder: they only set the base directory of relative paths.
    /// </remarks>
    /// <param name="parser">The parsed arguments.</param>
    /// <param name="configPath">The full path of the config file, or null.</param>
    /// <returns>The policy.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an --allow-root folder is malformed or missing.</exception>
    private static FileAccessPolicy BuildFileAccessPolicy(ArgumentParser parser, string? configPath)
    {
        // The default needs no folders and no file-system access
        var mode = parser.FileAccessMode;
        if (mode == FileAccessMode.None)
        {
            return FileAccessPolicy.Any;
        }

        // The allowed folders: --allow-root replaces the defaults
        var configFolder = configPath == null ? null : Path.GetDirectoryName(configPath);
        var folders = new List<string>();
        if (parser.AllowRoots.Count > 0)
        {
            folders.AddRange(parser.AllowRoots.Select(folder =>
                PathResolver.ResolveExistingDirectory(folder, Environment.CurrentDirectory, "--allow-root option")));
        }
        else
        {
            folders.Add(Environment.CurrentDirectory);
            if (configFolder != null)
            {
                folders.Add(configFolder);
            }
        }

        return FileAccessPolicy.Create(mode, folders, configFolder);
    }

    /// <summary>
    ///     Builds the generation jobs from the config file tables or the command-line inputs.
    /// </summary>
    /// <remarks>
    ///     When the config file defines tables, each table becomes a job whose formatting layers are, in
    ///     precedence order, the command line, the job, and the config defaults, and whose base directory is
    ///     the root or else the config file's directory. Otherwise, when --input was given, a single
    ///     command-line job is created with the command line and config defaults as layers and the root or
    ///     else the working directory as base directory.
    /// </remarks>
    /// <param name="parser">The parsed arguments.</param>
    /// <param name="config">The loaded config file, or null.</param>
    /// <param name="rootDirectory">The effective root directory, or null.</param>
    /// <returns>The jobs; empty when there is nothing to generate.</returns>
    /// <exception cref="ArgumentException">
    ///     Thrown when --input or --output is combined with config tables, or a job's column-width count does
    ///     not match its table shape.
    /// </exception>
    private static List<TableJob> BuildJobs(ArgumentParser parser, ConfigFile? config, string? rootDirectory)
    {
        var commandLine = parser.Formatting;

        // Config tables: the file decides the inputs and outputs of every job
        if (config is { Tables.Count: > 0 })
        {
            if (parser.InputPatterns.Count > 0)
            {
                throw new ArgumentException(
                    $"--input cannot be used with config file '{config.FilePath}' because it defines table jobs");
            }

            if (parser.OutputFile != null)
            {
                throw new ArgumentException(
                    $"--output cannot be used with config file '{config.FilePath}' because it defines table jobs");
            }

            var baseDirectory = SelectBaseDirectory(rootDirectory, config.BaseDirectory);
            var jobs = new List<TableJob>();
            foreach (var table in config.Tables)
            {
                try
                {
                    jobs.Add(TableJob.Create(table.Index, table.Inputs, table.Output, baseDirectory, [commandLine, table.Settings, config.Defaults]));
                }
                catch (ArgumentException ex)
                {
                    throw new ArgumentException($"Config file '{config.FilePath}': {ex.Message}", ex);
                }
            }

            return jobs;
        }

        // Command-line job: inputs and output come from --input and --output
        if (parser.InputPatterns.Count > 0)
        {
            var baseDirectory = SelectBaseDirectory(rootDirectory, Environment.CurrentDirectory);
            return [TableJob.Create(null, parser.InputPatterns, parser.OutputFile, baseDirectory, [commandLine, config?.Defaults])];
        }

        return [];
    }

    /// <summary>
    ///     Selects the base directory for relative input and output paths.
    /// </summary>
    /// <remarks>
    ///     States the base-directory precedence in one place: a root directory (from --root or the config
    ///     <c>root</c> key, already selected by the caller) wins over the default for the path's origin, which
    ///     is the config file's directory for paths from the file and the working directory for command-line
    ///     paths.
    /// </remarks>
    /// <param name="rootDirectory">The effective root directory, or null.</param>
    /// <param name="originDefault">The default directory for the path's origin.</param>
    /// <returns>The base directory.</returns>
    private static string SelectBaseDirectory(string? rootDirectory, string originDefault) =>
        rootDirectory ?? originDefault;

    /// <summary>
    ///     Opens the log file for writing
    /// </summary>
    /// <param name="logFile">Log file path</param>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when the file at <paramref name="logFile"/> cannot be opened for writing, wrapping
    ///     any underlying file-system exception (e.g. <see cref="IOException"/>,
    ///     <see cref="UnauthorizedAccessException"/>).
    /// </exception>
    private void OpenLogFile(string logFile)
    {
        try
        {
            // Open with AutoFlush enabled so log entries are immediately written to disk
            // even if the application terminates unexpectedly before Dispose is called
            _logWriter = new StreamWriter(logFile, append: false) { AutoFlush = true };
        }
        // Generic catch is justified here to wrap any file system exception with context.
        // Expected exceptions include IOException, UnauthorizedAccessException, ArgumentException,
        // NotSupportedException, and other file system-related exceptions.
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to open log file '{logFile}': {ex.Message}", ex);
        }
    }

    /// <summary>
    ///     Helper class for parsing command-line arguments
    /// </summary>
    private sealed class ArgumentParser
    {
        /// <summary>Description of a valid --column-widths value, used in error messages.</summary>
        private const string ColumnWidthsDescription = "2 or 3 comma-separated positive integers (e.g. 25,75)";

        /// <summary>
        ///     Gets a value indicating whether the version flag was specified.
        /// </summary>
        public bool Version { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the help flag was specified.
        /// </summary>
        public bool Help { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the silent flag was specified.
        /// </summary>
        public bool Silent { get; private set; }

        /// <summary>
        ///     Gets a value indicating whether the validate flag was specified.
        /// </summary>
        public bool Validate { get; private set; }

        /// <summary>
        ///     Gets the log file path.
        /// </summary>
        public string? LogFile { get; private set; }

        /// <summary>
        ///     Gets the validation results file path.
        /// </summary>
        public string? ResultsFile { get; private set; }

        /// <summary>Gets the input file patterns.</summary>
        public List<string> InputPatterns { get; } = [];

        /// <summary>Gets the output file path.</summary>
        public string? OutputFile { get; private set; }

        /// <summary>Gets the config file path as written, or null when --config was not given.</summary>
        public string? ConfigPath { get; private set; }

        /// <summary>Gets the root directory as written, or null when --root was not given.</summary>
        public string? RootDirectory { get; private set; }

        /// <summary>Gets the --file-access value as written, or null when it was not given.</summary>
        public string? FileAccessText { get; private set; }

        /// <summary>Gets the --allow-root folders as written, in the order given.</summary>
        public List<string> AllowRoots { get; } = [];

        /// <summary>
        ///     Gets the effective file-access mode: the --file-access value, or roots when only --allow-root was
        ///     given, or any.
        /// </summary>
        public FileAccessMode FileAccessMode { get; private set; } = FileAccessMode.None;

        /// <summary>
        ///     Gets the formatting options that were given explicitly; members not given remain null so that
        ///     lower-precedence layers (config file, built-in defaults) can supply them.
        /// </summary>
        public FormattingSettings Formatting { get; private set; } = FormattingSettings.Empty;

        /// <summary>
        ///     Iterates over the argument list, dispatching each token to <see cref="ParseArgument"/> so that
        ///     recognized flags set their corresponding properties and unknown flags throw immediately, then
        ///     settles the file-access mode.
        /// </summary>
        /// <param name="args">Command-line arguments. Must not be null.</param>
        /// <exception cref="ArgumentException">
        ///     Thrown when an argument is invalid, or --allow-root is combined with --file-access any or
        ///     no-symlinks.
        /// </exception>
        public void ParseArguments(string[] args)
        {
            // Validate input
            ArgumentNullException.ThrowIfNull(args);

            int i = 0;
            while (i < args.Length)
            {
                var arg = args[i++];
                i = ParseArgument(arg, args, i);
            }

            ResolveFileAccessMode();
        }

        /// <summary>
        ///     Settles the file-access mode from --file-access and --allow-root, rejecting a combination that
        ///     contradicts itself.
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     Thrown when --allow-root is combined with a mode that does not confine paths to folders; the message
        ///     names the conflict.
        /// </exception>
        private void ResolveFileAccessMode()
        {
            // An explicit mode wins; --allow-root alone implies roots
            var mode = FileAccessText != null ? FileAccessPolicy.ParseMode(FileAccessText) : FileAccessMode.None;
            if (AllowRoots.Count == 0)
            {
                FileAccessMode = mode;
                return;
            }

            if (FileAccessText == null)
            {
                FileAccessMode = FileAccessMode.Roots;
                return;
            }

            // --allow-root names the folders of roots, so a mode without roots contradicts it
            FileAccessMode = mode switch
            {
                FileAccessMode.None => throw new ArgumentException(
                    "The options --file-access any and --allow-root cannot be used together: --allow-root confines " +
                    "config file paths to the folders it names; drop --file-access any, or write --file-access roots"),
                FileAccessMode.NoSymlinks => throw new ArgumentException(
                    "The options --file-access no-symlinks and --allow-root cannot be used together: to confine " +
                    "config file paths to the folders and refuse links, write --file-access roots,no-symlinks with --allow-root"),
                _ => mode,
            };
        }

        /// <summary>
        ///     Handles a single recognized argument token, updating the appropriate property and returning the
        ///     updated index so the caller can advance past any consumed value tokens.
        /// </summary>
        /// <param name="arg">Argument to parse</param>
        /// <param name="args">All arguments</param>
        /// <param name="index">Current index</param>
        /// <returns>Updated index after consuming this argument and any associated value.</returns>
        private int ParseArgument(string arg, string[] args, int index)
        {
            switch (arg)
            {
                case "-v":
                case "--version":
                    Version = true;
                    return index;

                case "-?":
                case "-h":
                case "--help":
                    Help = true;
                    return index;

                case "--silent":
                    Silent = true;
                    return index;

                case "--validate":
                    Validate = true;
                    return index;

                case "--log":
                    LogFile = GetRequiredStringArgument(arg, args, index, "a filename argument");
                    return index + 1;

                case "--results":
                case "--result":
                    ResultsFile = GetRequiredStringArgument(arg, args, index, "a results filename argument");
                    return index + 1;

                case "-i":
                case "--input":
                    InputPatterns.Add(GetRequiredStringArgument(arg, args, index, "an input file pattern"));
                    return index + 1;

                case "-o":
                case "--output":
                    OutputFile = GetRequiredStringArgument(arg, args, index, "an output file path");
                    return index + 1;

                case "--config":
                    if (ConfigPath != null)
                    {
                        throw new ArgumentException("--config can only be specified once", nameof(args));
                    }

                    ConfigPath = GetRequiredStringArgument(arg, args, index, "a config file path");
                    return index + 1;

                case "--root":
                    RootDirectory = ParseRootArgument(arg, args, index);
                    return index + 1;

                case "--file-access":
                    if (FileAccessText != null)
                    {
                        throw new ArgumentException("--file-access can only be specified once", nameof(args));
                    }

                    FileAccessText = GetRequiredStringArgument(
                        arg, args, index, "a mode (any, roots, no-symlinks, or roots,no-symlinks)");
                    return index + 1;

                case "--allow-root":
                    AllowRoots.Add(ParseAllowRootArgument(arg, args, index));
                    return index + 1;

                default:
                    return ParseFormattingArgument(arg, args, index);
            }
        }

        /// <summary>
        ///     Parses the value of the --root option, which may be given once with a non-empty value.
        /// </summary>
        /// <param name="arg">Argument name</param>
        /// <param name="args">All arguments</param>
        /// <param name="index">Index of the value</param>
        /// <returns>The root directory as written.</returns>
        /// <exception cref="ArgumentException">Thrown when --root is repeated, has no value, or has an empty value.</exception>
        private string ParseRootArgument(string arg, string[] args, int index)
        {
            if (RootDirectory != null)
            {
                throw new ArgumentException("--root can only be specified once", nameof(args));
            }

            var value = GetRequiredStringArgument(arg, args, index, "a directory path");
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("--root requires a non-empty directory path", nameof(args));
            }

            return value;
        }

        /// <summary>
        ///     Parses the value of one --allow-root option, which must be a non-empty folder path.
        /// </summary>
        /// <param name="arg">Argument name</param>
        /// <param name="args">All arguments</param>
        /// <param name="index">Index of the value</param>
        /// <returns>The folder as written.</returns>
        /// <exception cref="ArgumentException">Thrown when the value is missing or empty.</exception>
        private static string ParseAllowRootArgument(string arg, string[] args, int index)
        {
            var value = GetRequiredStringArgument(arg, args, index, "a directory path");
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("--allow-root requires a non-empty directory path", nameof(args));
            }

            return value;
        }

        /// <summary>
        ///     Handles a formatting option, recording it in <see cref="Formatting"/> so that only options the
        ///     user actually gave take part in the precedence merge.
        /// </summary>
        /// <param name="arg">Argument to parse</param>
        /// <param name="args">All arguments</param>
        /// <param name="index">Current index</param>
        /// <returns>Updated index after consuming this argument and its value.</returns>
        /// <exception cref="ArgumentException">Thrown when the argument is unknown or its value is invalid.</exception>
        private int ParseFormattingArgument(string arg, string[] args, int index)
        {
            Formatting = arg switch
            {
                "--depth" => Formatting with
                {
                    HeadingDepth = GetRequiredIntArgument(arg, args, index, "a heading depth argument", 1, 6)
                },
                "-f" or "--format" => Formatting with
                {
                    Format = GetRequiredStringArgument(arg, args, index, "a format value (table or bullets)").ToLowerInvariant() switch
                    {
                        "table" => OutputFormat.Table,
                        "bullets" => OutputFormat.Bullets,
                        _ => throw new ArgumentException($"{arg} requires 'table' or 'bullets'", nameof(args))
                    }
                },
                "-s" or "--section" => Formatting with
                {
                    SectionHeading = GetRequiredStringArgument(arg, args, index, "a section heading")
                },
                "--term-header" => Formatting with
                {
                    TermHeader = GetRequiredStringArgument(arg, args, index, "a term header")
                },
                "--use-header" => Formatting with
                {
                    UseHeader = GetRequiredStringArgument(arg, args, index, "a use header")
                },
                "--where-header" => Formatting with
                {
                    WhereHeader = GetRequiredStringArgument(arg, args, index, "a where header")
                },
                "--kind" => Formatting with
                {
                    Kind = GetRequiredStringArgument(arg, args, index, "a kind value (terms or deprecated)").ToLowerInvariant() switch
                    {
                        "terms" => InputKind.Terms,
                        "deprecated" => InputKind.Deprecated,
                        _ => throw new ArgumentException($"{arg} requires 'terms' or 'deprecated'", nameof(args))
                    }
                },
                "--def-header" or "--definition-header" => Formatting with
                {
                    DefinitionHeader = GetRequiredStringArgument(arg, args, index, "a definition header")
                },
                "--sort" => Formatting with
                {
                    SortBy = GetRequiredStringArgument(arg, args, index, "a sort value (file or alpha)").ToLowerInvariant() switch
                    {
                        "file" => SortOrder.FileOrder,
                        "alpha" => SortOrder.Alphabetical,
                        _ => throw new ArgumentException($"{arg} requires 'file' or 'alpha'", nameof(args))
                    }
                },
                "--column-widths" => Formatting with
                {
                    ColumnWidths = ParseColumnWidths(GetRequiredStringArgument(arg, args, index, ColumnWidthsDescription))
                },
                _ => throw new ArgumentException($"Unsupported argument '{arg}'", nameof(args))
            };

            return index + 1;
        }

        /// <summary>
        ///     Parses a --column-widths value: 2 or 3 comma-separated positive integers.
        /// </summary>
        /// <remarks>
        ///     Items are trimmed and parsed with <see cref="NumberStyles.None"/>, which rejects signs, decimal
        ///     points and blanks, so zero, negative, non-integer and empty values (including a trailing comma)
        ///     are all rejected. The count is checked against the table shape later, once the effective kind
        ///     and format are known.
        /// </remarks>
        /// <param name="value">The option value.</param>
        /// <returns>The parsed widths.</returns>
        /// <exception cref="ArgumentException">Thrown when the value is not 2 or 3 positive integers.</exception>
        private static List<int> ParseColumnWidths(string value)
        {
            var items = value.Split(',');
            var widths = new List<int>();
            foreach (var item in items)
            {
                if (!int.TryParse(item.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var width) || width <= 0)
                {
                    throw new ArgumentException($"--column-widths requires {ColumnWidthsDescription}", nameof(value));
                }

                widths.Add(width);
            }

            if (widths.Count is < 2 or > 3)
            {
                throw new ArgumentException($"--column-widths requires {ColumnWidthsDescription}", nameof(value));
            }

            return widths;
        }

        /// <summary>
        ///     Gets a required string argument value
        /// </summary>
        /// <param name="arg">Argument name</param>
        /// <param name="args">All arguments</param>
        /// <param name="index">Current index</param>
        /// <param name="description">Description of what's required</param>
        /// <returns>Argument value</returns>
        private static string GetRequiredStringArgument(string arg, string[] args, int index, string description)
        {
            if (index >= args.Length)
            {
                throw new ArgumentException($"{arg} requires {description}", nameof(args));
            }

            return args[index];
        }

        /// <summary>
        ///     Gets a required integer argument value
        /// </summary>
        /// <param name="arg">Argument name</param>
        /// <param name="args">All arguments</param>
        /// <param name="index">Current index</param>
        /// <param name="description">Description of what's required</param>
        /// <param name="min">Minimum valid value (inclusive)</param>
        /// <param name="max">Maximum valid value (inclusive)</param>
        /// <returns>Argument value as an integer in [min, max]</returns>
        private static int GetRequiredIntArgument(string arg, string[] args, int index, string description, int min = 1, int max = int.MaxValue)
        {
            var value = GetRequiredStringArgument(arg, args, index, description);
            if (!int.TryParse(value, out var result) || result < min || result > max)
            {
                throw new ArgumentException($"{arg} requires an integer between {min} and {max} for {description}", nameof(args));
            }

            return result;
        }
    }

    /// <summary>
    ///     Writes a line of output to the console and log file (if logging is enabled).
    /// </summary>
    /// <param name="message">The message to write.</param>
    /// <remarks>
    ///     Output is written to stdout. When <see cref="Silent"/> is <c>true</c>, stdout output is
    ///     suppressed, but the message is still written to the log file when one is open.
    /// </remarks>
    public void WriteLine(string message)
    {
        // Write to console unless silent mode is enabled
        if (!Silent)
        {
            Console.WriteLine(message);
        }

        // Write to log file if logging is enabled
        _logWriter?.WriteLine(message);
    }

    /// <summary>
    ///     Writes an error message to the error console and log file (if logging is enabled).
    /// </summary>
    /// <param name="message">The error message to write.</param>
    /// <remarks>
    ///     <c>_hasErrors</c> is set to <c>true</c> unconditionally, so <see cref="ExitCode"/> will
    ///     return 1 regardless of whether <see cref="Silent"/> suppresses the console output.
    ///     Stderr output is suppressed when <see cref="Silent"/> is <c>true</c>, but the message
    ///     is still written to the log file when one is open.
    /// </remarks>
    public void WriteError(string message)
    {
        // Mark that we have encountered errors
        _hasErrors = true;

        // Write to error console unless silent mode is enabled
        if (!Silent)
        {
            var previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(message);
            Console.ForegroundColor = previousColor;
        }

        // Write to log file if logging is enabled
        _logWriter?.WriteLine(message);
    }

    /// <summary>
    ///     Disposes resources used by the Context.
    /// </summary>
    public void Dispose()
    {
        // Close and dispose the log file writer if it exists
        _logWriter?.Dispose();
        _logWriter = null;
    }
}
