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
            var filePath = args[1];
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

        if (args.Length >= 2 && args[0] == "-w")
        {
            var filePath = args[1];
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var commandHistory = ctx.CommandHistory;
            var lines = new List<string>(commandHistory.Count);
            foreach (var cmd in commandHistory)
            {
                lines.Add(cmd);
            }

            File.WriteAllLines(filePath, lines);
            return 0;
        }

        if (args.Length >= 2 && args[0] == "-a")
        {
            var filePath = args[1];
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var newCommands = ctx.GetCommandsSinceLastAppend();
            if (newCommands.Count > 0)
            {
                File.AppendAllLines(filePath, newCommands);
            }

            ctx.MarkLastAppendPosition();
            return 0;
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
}
