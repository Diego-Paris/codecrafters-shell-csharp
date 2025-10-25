namespace MiniShell.Abstractions;

public interface ICommand
{
    string Name { get; }
    string Description { get; }
    int Execute(string[] args, IShellContext ctx);
}