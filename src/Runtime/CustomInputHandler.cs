using System.Text;
using MiniShell.Abstractions;

namespace MiniShell.Runtime;

public sealed class CustomInputHandler : IInputHandler
{
    private readonly ICompletionProvider _completionProvider;

    public CustomInputHandler(ICompletionProvider completionProvider)
    {
        _completionProvider = completionProvider;
    }

    public string? ReadInput(string prompt)
    {
        Console.Write(prompt);
        var buffer = new StringBuilder();

        while (true)
        {
            var key = Console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
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

                    Console.Write(remaining + " ");
                    buffer.Append(remaining);
                    buffer.Append(' ');
                }
            }
            else if (key.Key == ConsoleKey.Backspace && buffer.Length > 0)
            {
                Console.Write("\b \b");
                buffer.Length--;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                Console.Write(key.KeyChar);
                buffer.Append(key.KeyChar);
            }
        }
    }
}
