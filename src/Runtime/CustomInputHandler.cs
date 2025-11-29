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

    private sealed class InputState
    {
        public StringBuilder Buffer { get; } = new();
        public string LastPrefix { get; set; } = string.Empty;
        public bool LastTabWasMultiMatch { get; set; }
        public int HistoryIndex { get; set; }
        public string CurrentInput { get; set; } = string.Empty;

        public InputState()
        {
            HistoryIndex = int.MaxValue;
        }
    }

    public string? ReadInput(string prompt)
    {
        _console.Write(prompt);
        var state = new InputState { HistoryIndex = _context.CommandHistory.Count };

        while (true)
        {
            var key = _console.ReadKey(intercept: true);

            if (key.Key == ConsoleKey.Enter)
            {
                _console.WriteLine();
                return state.Buffer.ToString();
            }

            if (key.Key == ConsoleKey.UpArrow)
            {
                HandleUpArrow(prompt, state);
            }
            else if (key.Key == ConsoleKey.DownArrow)
            {
                HandleDownArrow(prompt, state);
            }
            else if (key.Key == ConsoleKey.Tab)
            {
                HandleTab(prompt, state);
            }
            else if (key.Key == ConsoleKey.Backspace && state.Buffer.Length > 0)
            {
                HandleBackspace(state);
            }
            else if (!char.IsControl(key.KeyChar))
            {
                HandleCharacter(key.KeyChar, state);
            }
        }
    }

    private void HandleUpArrow(string prompt, InputState state)
    {
        if (state.HistoryIndex > 0)
        {
            if (state.HistoryIndex == _context.CommandHistory.Count)
            {
                state.CurrentInput = state.Buffer.ToString();
            }

            state.HistoryIndex--;
            ClearCurrentLine(prompt, state.Buffer.Length);
            state.Buffer.Clear();
            state.Buffer.Append(_context.CommandHistory[state.HistoryIndex]);
            _console.Write(prompt + state.Buffer.ToString());

            state.LastTabWasMultiMatch = false;
            state.LastPrefix = string.Empty;
        }
    }

    private void HandleDownArrow(string prompt, InputState state)
    {
        if (state.HistoryIndex < _context.CommandHistory.Count)
        {
            state.HistoryIndex++;
            ClearCurrentLine(prompt, state.Buffer.Length);
            state.Buffer.Clear();

            if (state.HistoryIndex < _context.CommandHistory.Count)
            {
                state.Buffer.Append(_context.CommandHistory[state.HistoryIndex]);
            }
            else
            {
                state.Buffer.Append(state.CurrentInput);
            }

            _console.Write(prompt + state.Buffer.ToString());

            state.LastTabWasMultiMatch = false;
            state.LastPrefix = string.Empty;
        }
    }

    private void HandleTab(string prompt, InputState state)
    {
        var prefix = state.Buffer.ToString();
        var completions = _completionProvider.GetCompletions(prefix).ToList();

        if (completions.Count == 1)
        {
            HandleSingleCompletion(completions[0], prefix, state);
        }
        else if (completions.Count > 1)
        {
            HandleMultipleCompletions(prompt, prefix, completions, state);
        }
        else
        {
            _console.Write('\x07');
            state.LastTabWasMultiMatch = false;
            state.LastPrefix = string.Empty;
        }
    }

    private void HandleSingleCompletion(string match, string prefix, InputState state)
    {
        var remaining = match.Substring(prefix.Length);
        _console.Write(remaining + " ");
        state.Buffer.Append(remaining);
        state.Buffer.Append(' ');

        state.LastTabWasMultiMatch = false;
        state.LastPrefix = string.Empty;
    }

    private void HandleMultipleCompletions(string prompt, string prefix, List<string> completions, InputState state)
    {
        var commonPrefix = GetLongestCommonPrefix(completions);
        var remaining = commonPrefix.Substring(prefix.Length);

        if (remaining.Length > 0)
        {
            _console.Write(remaining);
            state.Buffer.Append(remaining);

            var newCompletions = _completionProvider.GetCompletions(state.Buffer.ToString()).ToList();
            if (newCompletions.Count == 1)
            {
                _console.Write(" ");
                state.Buffer.Append(' ');
            }

            state.LastTabWasMultiMatch = false;
            state.LastPrefix = string.Empty;
        }
        else if (state.LastTabWasMultiMatch && state.LastPrefix == prefix)
        {
            _console.WriteLine();
            _console.Write(string.Join("  ", completions.OrderBy(c => c)));
            _console.WriteLine();
            _console.Write(prompt + state.Buffer.ToString());

            state.LastTabWasMultiMatch = false;
            state.LastPrefix = string.Empty;
        }
        else
        {
            _console.Write('\x07');
            state.LastTabWasMultiMatch = true;
            state.LastPrefix = prefix;
        }
    }

    private void HandleBackspace(InputState state)
    {
        _console.Write("\b \b");
        state.Buffer.Length--;
        state.LastTabWasMultiMatch = false;
        state.LastPrefix = string.Empty;
    }

    private void HandleCharacter(char ch, InputState state)
    {
        _console.Write(ch);
        state.Buffer.Append(ch);
        state.LastTabWasMultiMatch = false;
        state.LastPrefix = string.Empty;
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
