namespace MiniShell.Abstractions;

/// <summary>
/// Encapsulates the shell's execution environment, providing access to I/O streams, registered commands, and path resolution.
/// </summary>
public interface IShellContext
{
    /// <summary>
    /// Gets the registry of available commands indexed by their names.
    /// </summary>
    IReadOnlyDictionary<string, ICommand> Commands { get; }

    /// <summary>
    /// Gets the standard input stream for reading user input.
    /// </summary>
    TextReader In { get; }

    /// <summary>
    /// Gets the standard output stream for writing command results.
    /// </summary>
    TextWriter Out { get; }

    /// <summary>
    /// Gets the standard error stream for writing error messages and diagnostics.
    /// </summary>
    TextWriter Err { get; }

    /// <summary>
    /// Gets the path resolver for locating executable files in the system PATH.
    /// </summary>
    IPathResolver PathResolver { get; }

    /// <summary>
    /// Gets the command history list.
    /// </summary>
    IReadOnlyList<string> CommandHistory { get; }
}