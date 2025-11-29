using MiniShell.Abstractions;
using MiniShell.DataStructures;
using MiniShell.Runtime;

namespace MiniShell.Tests.Runtime;

public class TabCompletionIntegrationTests
{
    [Fact]
    public void CompletionProvider_WithBuiltinCommands_ReturnsEchoForEchPrefix()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var completions = provider.GetCompletions("ech").ToList();

        Assert.Single(completions);
        Assert.Equal("echo", completions[0]);
    }

    [Fact]
    public void CompletionProvider_WithBuiltinCommands_ReturnsExitForExiPrefix()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var completions = provider.GetCompletions("exi").ToList();

        Assert.Single(completions);
        Assert.Equal("exit", completions[0]);
    }

    [Fact]
    public void CompletionProvider_WithEPrefix_ReturnsBothEchoAndExit()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var completions = provider.GetCompletions("e").OrderBy(x => x).ToList();

        Assert.Contains("echo", completions);
        Assert.Contains("exit", completions);
        Assert.True(completions.Count >= 2);
    }

    [Fact]
    public void CompletionProvider_AfterTypingEch_FirstCompletionIsEcho()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var completions = provider.GetCompletions("ech").ToList();
        var firstCompletion = completions.FirstOrDefault();

        Assert.NotNull(firstCompletion);
        Assert.Equal("echo", firstCompletion);
    }

    [Fact]
    public void CompletionProvider_SupportsCodeCraftersTestCase_EchToEcho()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var input = "ech";
        var completions = provider.GetCompletions(input).ToList();

        Assert.NotEmpty(completions);
        var firstCompletion = completions[0];
        Assert.Equal("echo", firstCompletion);

        var remaining = firstCompletion.Substring(input.Length);
        Assert.Equal("o", remaining);
    }

    [Fact]
    public void CompletionProvider_SupportsCodeCraftersTestCase_ExiToExit()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var input = "exi";
        var completions = provider.GetCompletions(input).ToList();

        Assert.NotEmpty(completions);
        var firstCompletion = completions[0];
        Assert.Equal("exit", firstCompletion);

        var remaining = firstCompletion.Substring(input.Length);
        Assert.Equal("t", remaining);
    }

    [Fact]
    public void TriePlusProvider_EndToEnd_CompletesEchoCorrectly()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();

        trie.Add("echo");
        trie.Add("exit");
        trie.Add("cd");

        var matches = trie.GetPrefixMatches("ech").ToList();

        Assert.Single(matches);
        Assert.Equal("echo", matches[0]);
    }

    [Fact]
    public void TriePlusProvider_EndToEnd_CompletesExitCorrectly()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();

        trie.Add("echo");
        trie.Add("exit");
        trie.Add("cd");

        var matches = trie.GetPrefixMatches("exi").ToList();

        Assert.Single(matches);
        Assert.Equal("exit", matches[0]);
    }

    [Theory]
    [InlineData("ech", "echo", "o")]
    [InlineData("exi", "exit", "t")]
    [InlineData("c", "cd", "d")]
    [InlineData("pw", "pwd", "d")]
    [InlineData("ty", "type", "pe")]
    public void CompletionProvider_CalculatesRemainingCorrectly(
        string input, string expectedCompletion, string expectedRemaining)
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var completions = provider.GetCompletions(input).ToList();
        var completion = completions.FirstOrDefault(c => c == expectedCompletion);

        Assert.NotNull(completion);
        var remaining = completion.Substring(input.Length);
        Assert.Equal(expectedRemaining, remaining);
    }

    [Fact]
    public void CompletionProvider_ConsistentResults_AcrossMultipleCalls()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var firstCall = provider.GetCompletions("ech").ToList();
        var secondCall = provider.GetCompletions("ech").ToList();
        var thirdCall = provider.GetCompletions("ech").ToList();

        Assert.Equal(firstCall, secondCall);
        Assert.Equal(secondCall, thirdCall);
    }

    [Fact]
    public void CompletionProvider_DifferentPrefixes_ReturnDifferentResults()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var echoResults = provider.GetCompletions("ech").ToList();
        var exitResults = provider.GetCompletions("exi").ToList();

        Assert.NotEqual(echoResults, exitResults);
        Assert.Contains("echo", echoResults);
        Assert.Contains("exit", exitResults);
    }

    private IShellContext CreateShellContext()
    {
        var mockHistoryService = new MockHistoryService();
        var commands = new ICommand[]
        {
            new MiniShell.Commands.EchoCommand(),
            new MiniShell.Commands.ExitCommand(mockHistoryService),
            new MiniShell.Commands.CdCommand(),
            new MiniShell.Commands.PwdCommand(),
            new MiniShell.Commands.TypeCommand(),
            new MiniShell.Commands.ExternalCommand()
        };

        var pathResolver = new PathResolver();
        return new ShellContext(commands, pathResolver);
    }

    private sealed class MockHistoryService : IHistoryService
    {
        public void LoadFromFile() { }
        public void SaveToFile() { }
        public void AppendNewCommandsToFile() { }
    }
}
