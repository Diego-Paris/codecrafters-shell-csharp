using MiniShell.Abstractions;

namespace MiniShell.Runtime;

public sealed class HistoryService : IHistoryService
{
    private readonly IShellContext _context;

    public HistoryService(IShellContext context)
    {
        _context = context;
    }

    public void LoadFromFile()
    {
        var histFile = Environment.GetEnvironmentVariable("HISTFILE");
        if (string.IsNullOrEmpty(histFile) || !File.Exists(histFile))
        {
            return;
        }

        var lines = File.ReadAllLines(histFile);
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                _context.AddToHistory(line);
            }
        }
    }

    public void SaveToFile()
    {
        var histFile = Environment.GetEnvironmentVariable("HISTFILE");
        if (string.IsNullOrEmpty(histFile))
        {
            return;
        }

        EnsureDirectoryExists(histFile);
        File.WriteAllLines(histFile, _context.CommandHistory);
    }

    public void AppendNewCommandsToFile()
    {
        var histFile = Environment.GetEnvironmentVariable("HISTFILE");
        if (string.IsNullOrEmpty(histFile))
        {
            return;
        }

        var newCommands = _context.GetCommandsSinceLastAppend();
        if (newCommands.Count > 0)
        {
            EnsureDirectoryExists(histFile);
            File.AppendAllLines(histFile, newCommands);
        }

        _context.MarkLastAppendPosition();
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
