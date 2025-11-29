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
                var currentText = buffer.ToString();
                var completions = _completionProvider.GetCompletions(currentText).ToList();

                if (completions.Count > 0)
                {
                    var completion = completions[0];

                    for (int i = 0; i < buffer.Length; i++)
                    {
                        Console.Write("\b \b");
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
            }
        }
    }
}
