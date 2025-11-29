namespace MiniShell.Abstractions;

/// <summary>
/// Provides command name completions based on a prefix, supporting both built-in commands and PATH executables.
/// </summary>
public interface ICompletionProvider
{
    /// <summary>
    /// Retrieves all commands that start with the given prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match against command names.</param>
    /// <returns>Command names matching the prefix.</returns>
    IEnumerable<string> GetCompletions(string prefix);
}
