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
        Console.Out.Flush();

        var buffer = new StringBuilder();
        var isInteractive = !Console.IsInputRedirected;

        while (true)
        {
            int charCode;
            if (isInteractive && Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                charCode = key.KeyChar;

                if (key.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    return buffer.ToString();
                }
                else if (key.Key == ConsoleKey.Backspace)
                {
                    if (buffer.Length > 0)
                    {
                        buffer.Length--;
                        Console.Write("\b \b");
                    }
                    continue;
                }
                else if (key.Key == ConsoleKey.Tab)
                {
                    charCode = '\t';
                }
            }
            else
            {
                charCode = Console.Read();
                if (charCode == -1)
                {
                    return null;
                }
            }

            char ch = (char)charCode;

            if (ch == '\n')
            {
                if (!isInteractive) Console.WriteLine();
                return buffer.ToString();
            }
            else if (ch == '\t')
            {
                var currentText = buffer.ToString();
                var completions = _completionProvider.GetCompletions(currentText).ToList();

                if (completions.Count > 0)
                {
                    var completion = completions[0];

                    if (isInteractive)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            Console.Write("\b \b");
                        }
                    }

                    buffer.Clear();
                    buffer.Append(completion);
                    buffer.Append(' ');

                    Console.Write($"{completion} ");
                    Console.Out.Flush();
                }
            }
            else if (!char.IsControl(ch))
            {
                buffer.Append(ch);
                if (isInteractive)
                {
                    Console.Write(ch);
                }
            }
        }
    }
}
