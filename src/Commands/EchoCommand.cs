using MiniShell.Abstractions;

namespace MiniShell.Commands;

/// <summary>
/// Implements the echo command for printing text to stdout, joining arguments with spaces.
/// </summary>
public sealed class EchoCommand : ICommand
{
    /// <summary>
    /// Gets the command name used for shell invocation.
    /// </summary>
    public string Name => "echo";

    /// <summary>
    /// Gets a human-readable description of what the command does.
    /// </summary>
    public string Description => "Echo arguments";

    /// <summary>
    /// Executes the echo command to print arguments to stdout.
    /// </summary>
    /// <param name="args">Arguments to print, joined with spaces.</param>
    /// <param name="ctx">Shell execution context for stdout.</param>
    /// <returns>Always returns 0 (success).</returns>
    public int Execute(string[] args, IShellContext ctx)
    {
        ctx.Out.WriteLine(string.Join(' ', args));
        return 0;
    }
}