using MiniShell.Abstractions;

namespace MiniShell.Runtime;

/// <summary>
/// Adapter that wraps System.Console to implement the IConsole abstraction for production use.
/// </summary>
public sealed class SystemConsole : IConsole
{
    public ConsoleKeyInfo ReadKey(bool intercept) => Console.ReadKey(intercept);

    public void Write(char value) => Console.Write(value);

    public void Write(string value) => Console.Write(value);

    public void WriteLine() => Console.WriteLine();
}
