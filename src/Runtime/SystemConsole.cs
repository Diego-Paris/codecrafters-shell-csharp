using MiniShell.Abstractions;

namespace MiniShell.Runtime;

public sealed class SystemConsole : IConsole
{
    public ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);

    public void Write(char value) => Console.Write(value);

    public void Write(string value) => Console.Write(value);

    public void WriteLine() => Console.WriteLine();
}
