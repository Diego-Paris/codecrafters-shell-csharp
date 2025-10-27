using MiniShell.Abstractions;

namespace MiniShell.Runtime;

public sealed class ShellContext : IShellContext
{
    public ShellContext(IEnumerable<ICommand> commands, IPathResolver resolver)
    {
        Commands = commands.ToDictionary(c => c.Name, StringComparer.Ordinal);
        PathResolver = resolver;
        In = Console.In;
        Out = Console.Out;
        Err = Console.Error;
    }

    public IReadOnlyDictionary<string, ICommand> Commands { get; }
    public TextReader In { get; }
    public TextWriter Out { get; }

    public TextWriter Err { get; }
    public IPathResolver PathResolver { get; }
}
