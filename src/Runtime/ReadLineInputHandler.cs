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
        private string[]? _currentMatches;
        private int _currentIndex;
        private string? _lastText;

        public TabCompletionHandler(ICompletionProvider completionProvider)
        {
            _completionProvider = completionProvider;
        }

        public char[] Separators { get; set; } = new[] { ' ', '\t' };

        public string[] GetSuggestions(string text, int index)
        {
            var lastWord = GetLastWord(text, out var wordStartIndex);

            if (text != _lastText)
            {
                _currentMatches = null;
                _currentIndex = 0;
                _lastText = text;
            }

            if (_currentMatches == null)
            {
                var completions = _completionProvider.GetCompletions(lastWord).ToArray();
                if (completions.Length == 0)
                {
                    return Array.Empty<string>();
                }

                _currentMatches = completions;
                _currentIndex = 0;
            }

            if (_currentMatches.Length == 0)
            {
                return Array.Empty<string>();
            }

            var completion = _currentMatches[_currentIndex];
            _currentIndex = (_currentIndex + 1) % _currentMatches.Length;

            var prefix = text.Substring(0, wordStartIndex);
            var fullCompletion = prefix + completion;

            return new[] { fullCompletion };
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
