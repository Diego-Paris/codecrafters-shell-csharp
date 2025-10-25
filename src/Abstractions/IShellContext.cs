namespace MiniShell.Abstractions;

public interface IShellContext
{
    IReadOnlyDictionary<string, ICommand> Commands { get; }
    TextReader In { get; }
    TextWriter Out { get; }
    IPathResolver PathResolver { get; }
}