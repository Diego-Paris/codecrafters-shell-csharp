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

        Console.Error.WriteLine($"[DEBUG] ReadInput called, IsInputRedirected={Console.IsInputRedirected}");

        var buffer = new StringBuilder();
        var isInteractive = !Console.IsInputRedirected;
        var hasEchoed = false;

        while (true)
        {
            Console.Error.WriteLine($"[DEBUG] Waiting for input, buffer='{buffer}'");
            int charCode;
            if (isInteractive && Console.KeyAvailable)
            {
                var key = Console.ReadKey(intercept: true);
                charCode = key.KeyChar;
                Console.Error.WriteLine($"[DEBUG] Read char via ReadKey: '{(char)charCode}' (0x{charCode:X2})");

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
                Console.Error.WriteLine($"[DEBUG] Read char via Console.Read: '{(char)charCode}' (0x{charCode:X2})");
                if (charCode == -1)
                {
                    return null;
                }
            }

            char ch = (char)charCode;
            Console.Error.WriteLine($"[DEBUG] Processing char: '{ch}' (0x{(int)ch:X2})");

            if (ch == '\n')
            {
                if (!hasEchoed && !isInteractive)
                {
                    Console.Write(buffer.ToString());
                }
                Console.WriteLine();
                return buffer.ToString();
            }
            else if (ch == '\t')
            {
                var currentText = buffer.ToString();
                Console.Error.WriteLine($"[DEBUG] TAB pressed, buffer='{currentText}', hasEchoed={hasEchoed}, isInteractive={isInteractive}");
                var completions = _completionProvider.GetCompletions(currentText).ToList();
                Console.Error.WriteLine($"[DEBUG] Found {completions.Count} completions: [{string.Join(", ", completions)}]");

                if (completions.Count > 0)
                {
                    var completion = completions[0];
                    Console.Error.WriteLine($"[DEBUG] Using completion: '{completion}'");

                    if (isInteractive && hasEchoed)
                    {
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            Console.Write("\b \b");
                        }
                    }

                    buffer.Clear();
                    buffer.Append(completion);
                    buffer.Append(' ');

                    Console.Error.WriteLine($"[DEBUG] Writing to stdout: '{completion} '");
                    Console.Write($"{completion} ");
                    Console.Out.Flush();
                    hasEchoed = true;
                }
                else if (!hasEchoed)
                {
                    Console.Error.WriteLine($"[DEBUG] No completions, writing current text + space");
                    Console.Write(currentText);
                    Console.Write(" ");
                    Console.Out.Flush();
                    buffer.Append(' ');
                    hasEchoed = true;
                }
            }
            else if (!char.IsControl(ch))
            {
                buffer.Append(ch);
                if (isInteractive)
                {
                    Console.Write(ch);
                    hasEchoed = true;
                }
            }
        }
    }
}
