using MiniShell.DataStructures;

namespace MiniShell.Tests.DataStructures;

public class CompletionTrieTests
{
    [Fact]
    public void Add_SingleWord_ShouldBeRetrievable()
    {
        var trie = new CompletionTrie();

        trie.Add("echo");

        var matches = trie.GetPrefixMatches("echo").ToList();
        Assert.Single(matches);
        Assert.Equal("echo", matches[0]);
    }

    [Fact]
    public void Add_MultipleWords_ShouldAllBeRetrievable()
    {
        var trie = new CompletionTrie();

        trie.Add("echo");
        trie.Add("exit");
        trie.Add("export");

        var matches = trie.GetPrefixMatches("e").OrderBy(x => x).ToList();
        Assert.Equal(3, matches.Count);
        Assert.Contains("echo", matches);
        Assert.Contains("exit", matches);
        Assert.Contains("export", matches);
    }

    [Fact]
    public void GetPrefixMatches_EmptyPrefix_ShouldReturnAllWords()
    {
        var trie = new CompletionTrie();
        trie.Add("echo");
        trie.Add("exit");
        trie.Add("cd");

        var matches = trie.GetPrefixMatches("").ToList();

        Assert.Equal(3, matches.Count);
    }

    [Fact]
    public void GetPrefixMatches_PartialPrefix_ShouldReturnMatchingWords()
    {
        var trie = new CompletionTrie();
        trie.Add("echo");
        trie.Add("exit");
        trie.Add("export");
        trie.Add("cd");
        trie.Add("cat");

        var matches = trie.GetPrefixMatches("ex").OrderBy(x => x).ToList();

        Assert.Equal(2, matches.Count);
        Assert.Contains("exit", matches);
        Assert.Contains("export", matches);
    }

    [Fact]
    public void GetPrefixMatches_ExactMatch_ShouldReturnWord()
    {
        var trie = new CompletionTrie();
        trie.Add("echo");
        trie.Add("echos");

        var matches = trie.GetPrefixMatches("echo").OrderBy(x => x).ToList();

        Assert.Equal(2, matches.Count);
        Assert.Contains("echo", matches);
        Assert.Contains("echos", matches);
    }

    [Fact]
    public void GetPrefixMatches_NoMatches_ShouldReturnEmpty()
    {
        var trie = new CompletionTrie();
        trie.Add("echo");
        trie.Add("exit");

        var matches = trie.GetPrefixMatches("xyz").ToList();

        Assert.Empty(matches);
    }

    [Theory]
    [InlineData("ech", new[] { "echo" })]
    [InlineData("exi", new[] { "exit" })]
    [InlineData("e", new[] { "echo", "exit" })]
    [InlineData("c", new[] { "cd", "cat" })]
    public void GetPrefixMatches_BuiltinCommands_ShouldMatchCodeCraftersRequirements(
        string prefix, string[] expectedMatches)
    {
        var trie = new CompletionTrie();
        trie.Add("echo");
        trie.Add("exit");
        trie.Add("cd");
        trie.Add("cat");
        trie.Add("type");
        trie.Add("pwd");

        var matches = trie.GetPrefixMatches(prefix).OrderBy(x => x).ToList();

        Assert.Equal(expectedMatches.OrderBy(x => x), matches);
    }

    [Fact]
    public void Clear_AfterAddingWords_ShouldRemoveAllWords()
    {
        var trie = new CompletionTrie();
        trie.Add("echo");
        trie.Add("exit");

        trie.Clear();

        var matches = trie.GetPrefixMatches("").ToList();
        Assert.Empty(matches);
    }

    [Fact]
    public void Add_DuplicateWord_ShouldNotCreateDuplicateMatches()
    {
        var trie = new CompletionTrie();

        trie.Add("echo");
        trie.Add("echo");
        trie.Add("echo");

        var matches = trie.GetPrefixMatches("echo").ToList();
        Assert.Single(matches);
    }

    [Fact]
    public void GetPrefixMatches_CaseSensitive_ShouldDistinguishCase()
    {
        var trie = new CompletionTrie();
        trie.Add("Echo");
        trie.Add("echo");

        var lowerMatches = trie.GetPrefixMatches("echo").ToList();
        var upperMatches = trie.GetPrefixMatches("Echo").ToList();

        Assert.Single(lowerMatches);
        Assert.Equal("echo", lowerMatches[0]);
        Assert.Single(upperMatches);
        Assert.Equal("Echo", upperMatches[0]);
    }

    [Fact]
    public void GetPrefixMatches_LongPrefix_ShouldMatchCorrectly()
    {
        var trie = new CompletionTrie();
        trie.Add("environmental");
        trie.Add("environment");
        trie.Add("env");

        var matches = trie.GetPrefixMatches("environ").OrderBy(x => x).ToList();

        Assert.Equal(2, matches.Count);
        Assert.Contains("environment", matches);
        Assert.Contains("environmental", matches);
    }
}
