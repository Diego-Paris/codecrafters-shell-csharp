using MiniShell.Abstractions;

namespace MiniShell.Commands;

public sealed class ExitCommand : ICommand
{
    public string Name => "exit";

    public string Description => "Exit with optional status code";

    public int Execute(string[] args, IShellContext ctx)
    {
        ctx.SaveHistoryToFile();
        var code = args.Length > 0 && int.TryParse(args[0], out var c) ? c : 0;
        Environment.Exit(code);
        return code;
    }
}