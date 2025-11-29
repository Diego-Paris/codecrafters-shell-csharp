using System.Text;
using MiniShell.Abstractions;

namespace MiniShell.Runtime;

public sealed class CustomInputHandler : IInputHandler
{
    private readonly ICompletionProvider _completionProvider;
    private readonly IConsole _console;
    private readonly IShellContext _context;

    public CustomInputHandler(ICompletionProvider completionProvider, IShellContext context)
        : this(completionProvider, context, new SystemConsole())
    {
    }

    public CustomInputHandler(ICompletionProvider completionProvider, IShellContext context, IConsole console)
    {
        _completionProvider = completionProvider;
        _context = context;
        _console = console;
    }

    public string? ReadInput(string prompt)
    {
        _console.Write(prompt);
        var buffer = new StringBuilder();
        var lastPrefix = string.Empty;
        var lastTabWasMultiMatch = false;
        var historyIndex = _context.CommandHistory.Count;
        var currentInput = string.Empty;

        while (true)
        {
            var key = _console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                _console.WriteLine();
                return buffer.ToString();
            }
            else if (key.Key == ConsoleKey.UpArrow)
            {
                if (historyIndex > 0)
                {
                    if (historyIndex == _context.CommandHistory.Count)
                    {
                        currentInput = buffer.ToString();
                    }

                    historyIndex--;
                    ClearCurrentLine(prompt, buffer.Length);
                    buffer.Clear();
                    buffer.Append(_context.CommandHistory[historyIndex]);
                    _console.Write(prompt + buffer.ToString());

                    lastTabWasMultiMatch = false;
                    lastPrefix = string.Empty;
                }
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                if (historyIndex < _context.CommandHistory.Count)
                {
                    historyIndex++;
                    ClearCurrentLine(prompt, buffer.Length);
                    buffer.Clear();

                    if (historyIndex < _context.CommandHistory.Count)
                    {
                        buffer.Append(_context.CommandHistory[historyIndex]);
                    }
                    else
                    {
                        buffer.Append(currentInput);
                    }

                    _console.Write(prompt + buffer.ToString());

                    lastTabWasMultiMatch = false;
                    lastPrefix = string.Empty;
                }
            }
            else if (key.Key == ConsoleKey.Tab)
            {
                var prefix = buffer.ToString();
                var completions = _completionProvider.GetCompletions(prefix).ToList();

                if (completions.Count == 1)
                {
                    var match = completions[0];
                    var remaining = match.Substring(prefix.Length);

                    _console.Write(remaining + " ");
                    buffer.Append(remaining);
                    buffer.Append(' ');

                    lastTabWasMultiMatch = false;
                    lastPrefix = string.Empty;
                }
                else if (completions.Count > 1)
                {
                    var commonPrefix = GetLongestCommonPrefix(completions);
                    var remaining = commonPrefix.Substring(prefix.Length);

                    if (remaining.Length > 0)
                    {
                        _console.Write(remaining);
                        buffer.Append(remaining);

                        var newCompletions = _completionProvider.GetCompletions(buffer.ToString()).ToList();
                        if (newCompletions.Count == 1)
                        {
                            _console.Write(" ");
                            buffer.Append(' ');
                        }

                        lastTabWasMultiMatch = false;
                        lastPrefix = string.Empty;
                    }
                    else if (lastTabWasMultiMatch && lastPrefix == prefix)
                    {
                        _console.WriteLine();
                        _console.Write(string.Join("  ", completions.OrderBy(c => c)));
                        _console.WriteLine();
                        _console.Write(prompt + buffer.ToString());

                        lastTabWasMultiMatch = false;
                        lastPrefix = string.Empty;
                    }
                    else
                    {
                        _console.Write('\x07');
                        lastTabWasMultiMatch = true;
                        lastPrefix = prefix;
                    }
                }
                else
                {
                    _console.Write('\x07');
                    lastTabWasMultiMatch = false;
                    lastPrefix = string.Empty;
                }
            }
            else if (key.Key == ConsoleKey.Backspace && buffer.Length > 0)
            {
                _console.Write("\b \b");
                buffer.Length--;
                lastTabWasMultiMatch = false;
                lastPrefix = string.Empty;
            }
            else if (!char.IsControl(key.KeyChar))
            {
                _console.Write(key.KeyChar);
                buffer.Append(key.KeyChar);
                lastTabWasMultiMatch = false;
                lastPrefix = string.Empty;
            }
        }
    }

    private static string GetLongestCommonPrefix(List<string> strings)
    {
        if (strings.Count == 0) return string.Empty;
        if (strings.Count == 1) return strings[0];

        var first = strings[0];
        var minLength = strings.Min(s => s.Length);

        for (int i = 0; i < minLength; i++)
        {
            var currentChar = first[i];
            if (strings.Any(s => s[i] != currentChar))
            {
                return first.Substring(0, i);
            }
        }

        return first.Substring(0, minLength);
    }

    private void ClearCurrentLine(string prompt, int bufferLength)
    {
        var totalLength = prompt.Length + bufferLength;
        _console.Write('\r');
        _console.Write(new string(' ', totalLength));
        _console.Write('\r');
    }
}
