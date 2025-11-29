using System.Text;
using MiniShell.Abstractions;

namespace MiniShell.Runtime;

public sealed class CustomInputHandler : IInputHandler
{
    private readonly ICompletionProvider _completionProvider;
    private readonly IConsole _console;

    public CustomInputHandler(ICompletionProvider completionProvider)
        : this(completionProvider, new SystemConsole())
    {
    }

    public CustomInputHandler(ICompletionProvider completionProvider, IConsole console)
    {
        _completionProvider = completionProvider;
        _console = console;
    }

    public string? ReadInput(string prompt)
    {
        _console.Write(prompt);
        var buffer = new StringBuilder();

        while (true)
        {
            var key = _console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                _console.WriteLine();
                return buffer.ToString();
            }
            else if (key.Key == ConsoleKey.Tab)
            {
                var prefix = buffer.ToString();
                var completions = _completionProvider.GetCompletions(prefix).ToList();

                if (completions.Count > 0)
                {
                    var match = completions[0];
                    var remaining = match.Substring(prefix.Length);

                    _console.Write(remaining + " ");
                    buffer.Append(remaining);
                    buffer.Append(' ');
                }
                else
                {
                    _console.Write('\x07');
                }
            }
            else if (key.Key == ConsoleKey.Backspace && buffer.Length > 0)
            {
                _console.Write("\b \b");
                buffer.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                _console.Write(key.KeyChar);
                buffer.Append(key.KeyChar);
            }
        }
    }
}
