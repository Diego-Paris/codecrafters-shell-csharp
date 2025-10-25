namespace MiniShell.Abstractions;

public interface IPathResolver
{
    string? FindInPath(string command);
}