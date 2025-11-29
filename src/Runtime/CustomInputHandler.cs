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

        TerminalMode.EnableRawMode();
        try
        {
            var buffer = new StringBuilder();

            while (true)
            {
                int charCode = Console.Read();
                if (charCode == -1)
                {
                    return null;
                }

                char ch = (char)charCode;

                if (ch == '\n' || ch == '\r')
                {
                    Console.WriteLine();
                    return buffer.ToString();
                }
                else if (ch == '\t')
                {
                    var currentText = buffer.ToString();
                    var completions = _completionProvider.GetCompletions(currentText).ToList();

                    if (completions.Count > 0)
                    {
                        var completion = completions[0];
                        var remaining = completion.Substring(currentText.Length);

                        // Move cursor back to start of word
                        for (int i = 0; i < currentText.Length; i++)
                        {
                            Console.Write("\b");
                        }

                        // Write completion and space
                        Console.Write($"{completion} ");
                        Console.Out.Flush();

                        buffer.Clear();
                        buffer.Append(completion);
                        buffer.Append(' ');
                    }
                }
                else if (ch == '\b' || ch == (char)127) // Backspace or DEL
                {
                    if (buffer.Length > 0)
                    {
                        buffer.Length--;
                        Console.Write("\b \b");
                        Console.Out.Flush();
                    }
                }
                else if (!char.IsControl(ch))
                {
                    buffer.Append(ch);
                    Console.Write(ch);
                    Console.Out.Flush();
                }
            }
        }
        finally
        {
            TerminalMode.DisableRawMode();
        }
    }
}
