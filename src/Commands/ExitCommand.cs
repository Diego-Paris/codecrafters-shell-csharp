using MiniShell.Abstractions;

namespace MiniShell.Commands;

/// <summary>
/// Implements the exit command for terminating the shell process with an optional exit code.
/// </summary>
public sealed class ExitCommand : ICommand
{
    /// <summary>
    /// Gets the command name used for shell invocation.
    /// </summary>
    public string Name => "exit";

    /// <summary>
    /// Gets a human-readable description of what the command does.
    /// </summary>
    public string Description => "Exit with optional status code";

    /// <summary>
    /// Executes the exit command to terminate the shell process.
    /// </summary>
    /// <param name="args">Optional exit code as first argument (defaults to 0).</param>
    /// <param name="ctx">Shell execution context used to save history before exit.</param>
    /// <returns>The exit code (though Environment.Exit prevents return).</returns>
    public int Execute(string[] args, IShellContext ctx)
    {
        ctx.SaveHistoryToFile();
        var code = args.Length > 0 && int.TryParse(args[0], out var c) ? c : 0;
        Environment.Exit(code);
        return code;
    }
}