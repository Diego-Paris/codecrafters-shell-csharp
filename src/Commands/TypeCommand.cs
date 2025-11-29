using MiniShell.Abstractions;

namespace MiniShell.Commands;

/// <summary>
/// Implements the type command for identifying whether a command is a shell builtin or an external executable.
/// </summary>
public sealed class TypeCommand : ICommand
{
    /// <summary>
    /// Gets the command name used for shell invocation.
    /// </summary>
    public string Name => "type";

    /// <summary>
    /// Gets a human-readable description of what the command does.
    /// </summary>
    public string Description => "Identify command type or path";

    /// <summary>
    /// Executes the type command to determine if a command is builtin or external.
    /// </summary>
    /// <param name="args">Command name to identify.</param>
    /// <param name="ctx">Shell execution context for command lookup and output.</param>
    /// <returns>Exit code (0 for found, 2 for missing operand, 127 for not found).</returns>
    public int Execute(string[] args, IShellContext ctx)
    {
        if (args.Length == 0)
        {
            ctx.Out.WriteLine("type: missing operand");
            return 2;
        }

        var name = args[0];

        if (ctx.Commands.ContainsKey(name) && args.Length == 1)
        {
            ctx.Out.WriteLine($"{name} is a shell builtin");
            return 0;
        }

        var full = ctx.PathResolver.FindInPath(name);
        if (full is not null)
        {
            ctx.Out.WriteLine($"{name} is {full}");
            return 0;
        }

        ctx.Out.WriteLine($"{name}: not found");
        return 127;
    }
}