using MiniShell.Abstractions;

namespace MiniShell.Runtime;

/// <summary>
/// Manages persistent command history with support for loading from and saving to HISTFILE.
/// </summary>
public sealed class HistoryService : IHistoryService
{
    private readonly IShellContext _context;

    public HistoryService(IShellContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Loads command history from the file specified by the HISTFILE environment variable.
    /// </summary>
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

    /// <summary>
    /// Saves the entire command history to the file specified by the HISTFILE environment variable.
    /// </summary>
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

    /// <summary>
    /// Appends only new commands (since the last append) to the HISTFILE, implementing incremental saves.
    /// </summary>
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
