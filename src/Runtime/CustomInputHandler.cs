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

        while (true)
        {
            int charCode = Console.Read();
            if (charCode == -1)
            {
                return null;
            }

            char ch = (char)charCode;

            if (ch == '\n')
            {
                Console.WriteLine();
                return buffer.ToString();
            }
            else if (ch == '\t')
            {
                Console.Error.WriteLine($"[DEBUG] TAB received! buffer='{buffer}'");
                var currentText = buffer.ToString();
                var completions = _completionProvider.GetCompletions(currentText).ToList();
                Console.Error.WriteLine($"[DEBUG] Completions: {string.Join(", ", completions)}");

                if (completions.Count > 0)
                {
                    var completion = completions[0];
                    var remaining = completion.Substring(currentText.Length);
                    Console.Error.WriteLine($"[DEBUG] Appending: '{remaining} '");

                    buffer.Clear();
                    buffer.Append(completion);
                    buffer.Append(' ');

                    Console.Write($"{remaining} ");
                    Console.Out.Flush();
                }
            }
            else if (!char.IsControl(ch))
            {
                buffer.Append(ch);
            }
        }
    }
}
