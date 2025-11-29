using MiniShell.Abstractions;

namespace MiniShell.Commands;

/// <summary>
/// Implements a simple greeting command for testing shell functionality.
/// </summary>
public sealed class GreetCommand : ICommand
{
    /// <summary>
    /// Gets the command name used for shell invocation.
    /// </summary>
    public string Name => "greet";

    /// <summary>
    /// Gets a human-readable description of what the command does.
    /// </summary>
    public string Description => "Prints Hello, World!";

    /// <summary>
    /// Executes the greet command to print a greeting message.
    /// </summary>
    /// <param name="args">Arguments (unused).</param>
    /// <param name="ctx">Shell execution context for stdout.</param>
    /// <returns>Always returns 0 (success).</returns>
    public int Execute(string[] args, IShellContext ctx)
    {
        ctx.Out.WriteLine("Hello, World!");
        return 0;
    }
}