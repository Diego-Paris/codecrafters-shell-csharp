namespace MiniShell.Abstractions;

/// <summary>
/// Provides abstraction for locating executable files in the system PATH environment variable.
/// </summary>
public interface IPathResolver
{
    /// <summary>
    /// Searches for an executable in the system PATH.
    /// </summary>
    /// <param name="command">The command name to locate.</param>
    /// <returns>The full path to the executable if found; otherwise, null.</returns>
    string? FindInPath(string command);
}