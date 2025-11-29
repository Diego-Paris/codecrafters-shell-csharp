using MiniShell.Abstractions;

namespace MiniShell.Runtime;

/// <summary>
/// Concrete implementation of shell execution context, providing access to registered commands and standard I/O streams.
/// </summary>
public sealed class ShellContext : IShellContext
{
    private readonly List<string> _commandHistory = new();
    private int _lastAppendIndex = 0;

    /// <summary>
    /// Initializes a new shell context, building the command registry and wiring up standard console streams.
    /// </summary>
    /// <param name="commands">Collection of commands to register, indexed by their names.</param>
    /// <param name="resolver">Path resolver for locating external executables.</param>
    public ShellContext(IEnumerable<ICommand> commands, IPathResolver resolver)
        : this(commands, resolver, Console.In, Console.Out, Console.Error)
    {
    }

    /// <summary>
    /// Initializes a new shell context with custom I/O streams for testing.
    /// </summary>
    /// <param name="commands">Collection of commands to register, indexed by their names.</param>
    /// <param name="resolver">Path resolver for locating external executables.</param>
    /// <param name="input">Input stream.</param>
    /// <param name="output">Output stream.</param>
    /// <param name="error">Error stream.</param>
    public ShellContext(
        IEnumerable<ICommand> commands,
        IPathResolver resolver,
        TextReader input,
        TextWriter output,
        TextWriter error)
    {
        Commands = commands.ToDictionary(c => c.Name, StringComparer.Ordinal);
        PathResolver = resolver;
        In = input;
        Out = output;
        Err = error;
    }

    /// <summary>
    /// Gets the registry of available commands indexed by their names.
    /// </summary>
    public IReadOnlyDictionary<string, ICommand> Commands { get; }

    /// <summary>
    /// Gets the standard input stream for reading user input.
    /// </summary>
    public TextReader In { get; }

    /// <summary>
    /// Gets the standard output stream for writing command results.
    /// </summary>
    public TextWriter Out { get; }

    /// <summary>
    /// Gets the standard error stream for writing error messages and diagnostics.
    /// </summary>
    public TextWriter Err { get; }

    /// <summary>
    /// Gets the path resolver for locating executable files in the system PATH.
    /// </summary>
    public IPathResolver PathResolver { get; }

    /// <summary>
    /// Gets the command history list.
    /// </summary>
    public IReadOnlyList<string> CommandHistory => _commandHistory;

    /// <summary>
    /// Adds a command to the history.
    /// </summary>
    public void AddToHistory(string command)
    {
        _commandHistory.Add(command);
    }

    /// <summary>
    /// Gets commands that have been added since the last append operation.
    /// </summary>
    public IReadOnlyList<string> GetCommandsSinceLastAppend()
    {
        if (_lastAppendIndex >= _commandHistory.Count)
        {
            return Array.Empty<string>();
        }

        return _commandHistory.Skip(_lastAppendIndex).ToList();
    }

    /// <summary>
    /// Marks the current position as the last append point.
    /// </summary>
    public void MarkLastAppendPosition()
    {
        _lastAppendIndex = _commandHistory.Count;
    }

    public void SaveHistoryToFile()
    {
        var histFile = Environment.GetEnvironmentVariable("HISTFILE");
        if (string.IsNullOrEmpty(histFile))
        {
            return;
        }

        var directory = Path.GetDirectoryName(histFile);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllLines(histFile, _commandHistory);
    }
}
