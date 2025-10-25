using MiniShell.Abstractions;

namespace MiniShell.Commands;

public sealed class TypeCommand : ICommand
{
    public string Name => "type";
    public string Description => "Identify command type or path";
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