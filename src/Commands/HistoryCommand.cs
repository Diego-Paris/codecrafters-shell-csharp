using MiniShell.Abstractions;

namespace MiniShell.Commands;

public sealed class HistoryCommand : ICommand
{
    public string Name => "history";

    public string Description => "Show command history";

    public int Execute(string[] args, IShellContext ctx)
    {
        if (args.Length >= 2 && args[0] == "-r")
        {
            return LoadFromFile(args[1], ctx);
        }

        if (args.Length >= 2 && args[0] == "-w")
        {
            return WriteToFile(args[1], ctx);
        }

        if (args.Length >= 2 && args[0] == "-a")
        {
            return AppendToFile(args[1], ctx);
        }

        var history = ctx.CommandHistory;
        int startIndex = 0;

        if (args.Length > 0 && int.TryParse(args[0], out int limit) && limit > 0)
        {
            startIndex = Math.Max(0, history.Count - limit);
        }

        for (int i = startIndex; i < history.Count; i++)
        {
            ctx.Out.WriteLine($"{i + 1,5}  {history[i]}");
        }
        return 0;
    }

    private static int LoadFromFile(string filePath, IShellContext ctx)
    {
        if (!File.Exists(filePath))
        {
            ctx.Err.WriteLine($"history: {filePath}: No such file or directory");
            return 1;
        }

        var lines = File.ReadAllLines(filePath);
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                ctx.AddToHistory(line);
            }
        }
        return 0;
    }

    private static int WriteToFile(string filePath, IShellContext ctx)
    {
        EnsureDirectoryExists(filePath);
        File.WriteAllLines(filePath, ctx.CommandHistory);
        return 0;
    }

    private static int AppendToFile(string filePath, IShellContext ctx)
    {
        var newCommands = ctx.GetCommandsSinceLastAppend();
        if (newCommands.Count > 0)
        {
            EnsureDirectoryExists(filePath);
            File.AppendAllLines(filePath, newCommands);
        }

        ctx.MarkLastAppendPosition();
        return 0;
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }
}
