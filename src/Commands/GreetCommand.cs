using MiniShell.Abstractions;

namespace MiniShell.Commands;

public sealed class GreetCommand : ICommand
{
    public string Name => "greet";
    public string Description => "Prints Hello, World!";
    public int Execute(string[] args, IShellContext ctx)
    {
        ctx.Out.WriteLine("Hello, World!");
        return 0;
    }
}