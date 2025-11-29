namespace MiniShell.Abstractions;

public interface IConsole
{
    ConsoleKeyInfo ReadKey(bool intercept);
    void Write(char value);
    void Write(string value);
    void WriteLine();
}
