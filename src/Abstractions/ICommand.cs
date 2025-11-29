namespace MiniShell.Abstractions;

/// <summary>
/// Defines the contract for shell commands, enabling the shell to discover and execute both built-in and external commands uniformly.
/// </summary>
public interface ICommand
{
    /// <summary>
    /// Gets the command name used for shell invocation.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a human-readable description of what the command does.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Executes the command with the provided arguments.
    /// </summary>
    /// <param name="args">Command arguments (excluding the command name itself).</param>
    /// <param name="ctx">Shell execution context providing I/O streams and command registry.</param>
    /// <returns>Exit code (0 for success, non-zero for errors).</returns>
    int Execute(string[] args, IShellContext ctx);
}