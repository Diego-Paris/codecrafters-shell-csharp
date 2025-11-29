using MiniShell.Abstractions;

namespace MiniShell.Commands;

public sealed class HistoryCommand : ICommand
{
    public string Name => "history";

    public string Description => "Show command history";

    public int Execute(string[] args, IShellContext ctx)
    {
        var history = ctx.CommandHistory;
        for (int i = 0; i < history.Count; i++)
        {
            ctx.Out.WriteLine($"{i + 1,5}  {history[i]}");
        }
        return 0;
    }
}
