using MiniShell.Abstractions;
using MiniShell.DataStructures;
using MiniShell.Runtime;

namespace MiniShell.Tests.Runtime;

public class CommandCompletionProviderTests
{
    [Theory]
    [InlineData("ech", "echo")]
    [InlineData("exi", "exit")]
    [InlineData("c", "cd")]
    [InlineData("t", "type")]
    [InlineData("p", "pwd")]
    public void GetCompletions_BuiltinCommandPrefix_ShouldReturnMatchingCommands(
        string prefix, string expectedCommand)
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var completions = provider.GetCompletions(prefix).ToList();

        Assert.NotEmpty(completions);
        Assert.Contains(expectedCommand, completions);
    }

    [Fact]
    public void GetCompletions_EmptyPrefix_ShouldReturnAllCommands()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var completions = provider.GetCompletions("").ToList();

        Assert.NotEmpty(completions);
        Assert.Contains("echo", completions);
        Assert.Contains("exit", completions);
        Assert.Contains("cd", completions);
        Assert.Contains("pwd", completions);
        Assert.Contains("type", completions);
    }

    [Fact]
    public void GetCompletions_NoMatch_ShouldReturnEmpty()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var completions = provider.GetCompletions("xyz").ToList();

        Assert.Empty(completions);
    }

    [Fact]
    public void GetCompletions_ExactMatch_ShouldReturnCommand()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var completions = provider.GetCompletions("echo").ToList();

        Assert.Contains("echo", completions);
    }

    [Fact]
    public void GetCompletions_CalledMultipleTimes_ShouldInitializeOnce()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var firstCall = provider.GetCompletions("e").ToList();
        var secondCall = provider.GetCompletions("e").ToList();

        Assert.Equal(firstCall, secondCall);
    }

    [Fact]
    public void GetCompletions_IncludesBuiltinCommands_ShouldContainAllBuiltins()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var allCompletions = provider.GetCompletions("").ToList();

        var expectedBuiltins = new[] { "echo", "exit", "cd", "pwd", "type" };
        foreach (var builtin in expectedBuiltins)
        {
            Assert.Contains(builtin, allCompletions);
        }
    }

    [Theory]
    [InlineData("echo")]
    [InlineData("exit")]
    [InlineData("cd")]
    [InlineData("pwd")]
    [InlineData("type")]
    public void GetCompletions_SingleBuiltin_ShouldBeAvailable(string command)
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var completions = provider.GetCompletions(command).ToList();

        Assert.Contains(command, completions);
    }

    [Fact]
    public void GetCompletions_PartialPrefixWithMultipleMatches_ShouldReturnAll()
    {
        var ctx = CreateShellContext();
        var trie = new CompletionTrie();
        var provider = new CommandCompletionProvider(ctx, trie);

        var completions = provider.GetCompletions("e").OrderBy(x => x).ToList();

        Assert.Contains("echo", completions);
        Assert.Contains("exit", completions);
    }

    private IShellContext CreateShellContext()
    {
        var commands = new ICommand[]
        {
            new MiniShell.Commands.EchoCommand(),
            new MiniShell.Commands.ExitCommand(),
            new MiniShell.Commands.CdCommand(),
            new MiniShell.Commands.PwdCommand(),
            new MiniShell.Commands.TypeCommand(),
            new MiniShell.Commands.ExternalCommand()
        };

        var pathResolver = new PathResolver();
        return new ShellContext(commands, pathResolver);
    }
}
