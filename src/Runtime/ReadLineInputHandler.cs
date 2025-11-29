using MiniShell.Abstractions;

namespace MiniShell.Runtime;

/// <summary>
/// Provides line editing and tab completion for user input using the ReadLine library.
/// </summary>
public sealed class ReadLineInputHandler : IInputHandler
{
    private readonly ICompletionProvider _completionProvider;

    public ReadLineInputHandler(ICompletionProvider completionProvider)
    {
        _completionProvider = completionProvider;
        ReadLine.AutoCompletionHandler = new TabCompletionHandler(_completionProvider);
    }

    public string? ReadInput(string prompt)
    {
        return ReadLine.Read(prompt);
    }

    private sealed class TabCompletionHandler : IAutoCompleteHandler
    {
        private readonly ICompletionProvider _completionProvider;

        public TabCompletionHandler(ICompletionProvider completionProvider)
        {
            _completionProvider = completionProvider;
        }

        public char[] Separators { get; set; } = new[] { ' ', '\t' };

        public string[]? GetSuggestions(string text, int index)
        {
            var lastWord = GetLastWord(text, out var _);
            var completions = _completionProvider.GetCompletions(lastWord).ToArray();

            return completions.Length == 0 ? null : completions;
        }

        private string GetLastWord(string text, out int wordStartIndex)
        {
            if (string.IsNullOrEmpty(text))
            {
                wordStartIndex = 0;
                return string.Empty;
            }

            var lastSeparatorIndex = text.LastIndexOfAny(Separators);

            if (lastSeparatorIndex == -1)
            {
                wordStartIndex = 0;
                return text;
            }

            wordStartIndex = lastSeparatorIndex + 1;
            return text.Substring(lastSeparatorIndex + 1);
        }
    }
}
