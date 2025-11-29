namespace MiniShell.DataStructures;

/// <summary>
/// Provides abstraction for a Trie data structure optimized for command name prefix matching.
/// </summary>
public interface ICompletionTrie
{
    /// <summary>
    /// Adds a word to the trie for future prefix matching.
    /// </summary>
    /// <param name="word">The word to add.</param>
    void Add(string word);

    /// <summary>
    /// Retrieves all words in the trie that start with the given prefix.
    /// </summary>
    /// <param name="prefix">The prefix to match.</param>
    /// <returns>All words matching the prefix.</returns>
    IEnumerable<string> GetPrefixMatches(string prefix);

    /// <summary>
    /// Removes all words from the trie.
    /// </summary>
    void Clear();
}

/// <summary>
/// Implements a Trie (prefix tree) for efficient O(m) prefix matching where m is the prefix length.
/// </summary>
public sealed class CompletionTrie : ICompletionTrie
{
    private sealed class TrieNode
    {
        public Dictionary<char, TrieNode> Children { get; } = new();
        public bool IsEndOfWord { get; set; }
        public string? Word { get; set; }
    }

    private TrieNode _root = new();

    public void Add(string word)
    {
        if (string.IsNullOrEmpty(word)) return;

        var current = _root;
        foreach (var ch in word)
        {
            if (!current.Children.ContainsKey(ch))
            {
                current.Children[ch] = new TrieNode();
            }
            current = current.Children[ch];
        }

        current.IsEndOfWord = true;
        current.Word = word;
    }

    public IEnumerable<string> GetPrefixMatches(string prefix)
    {
        if (string.IsNullOrEmpty(prefix))
        {
            return CollectAllWords(_root);
        }

        var current = _root;
        foreach (var ch in prefix)
        {
            if (!current.Children.TryGetValue(ch, out var next))
            {
                return Enumerable.Empty<string>();
            }
            current = next;
        }

        return CollectAllWords(current);
    }

    public void Clear()
    {
        _root = new TrieNode();
    }

    private static IEnumerable<string> CollectAllWords(TrieNode node)
    {
        var results = new List<string>();
        CollectWordsRecursive(node, results);
        return results;
    }

    private static void CollectWordsRecursive(TrieNode node, List<string> results)
    {
        if (node.IsEndOfWord && node.Word is not null)
        {
            results.Add(node.Word);
        }

        foreach (var child in node.Children.Values)
        {
            CollectWordsRecursive(child, results);
        }
    }
}
