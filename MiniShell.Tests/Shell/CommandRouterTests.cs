using MiniShell;

namespace MiniShell.Tests.Shell;

public class CommandRouterTests
{
    [Theory]
    [InlineData("echo hello world", new[] { "echo", "hello", "world" })]
    [InlineData("echo hello", new[] { "echo", "hello" })]
    [InlineData("echo", new[] { "echo" })]
    [InlineData("", new string[] { })]
    [InlineData("   ", new string[] { })]
    public void Tokenize_BasicCommands_ShouldSplitByWhitespace(string input, string[] expected)
    {
        var result = CommandRouter.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo  hello    world", new[] { "echo", "hello", "world" })]
    [InlineData("echo\thello\tworld", new[] { "echo", "hello", "world" })]
    [InlineData("  echo  hello  ", new[] { "echo", "hello" })]
    public void Tokenize_MultipleSpaces_ShouldCollapseWhitespace(string input, string[] expected)
    {
        var result = CommandRouter.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo \"hello world\"", new[] { "echo", "hello world" })]
    [InlineData("echo \"hello    world\"", new[] { "echo", "hello    world" })]
    [InlineData("echo \"\"", new[] { "echo", "" })]
    [InlineData("echo \"hello\"\"world\"", new[] { "echo", "helloworld" })]
    [InlineData("echo hello\"\"world", new[] { "echo", "helloworld" })]
    public void Tokenize_DoubleQuotes_ShouldPreserveSpacesAndConcatenate(string input, string[] expected)
    {
        var result = CommandRouter.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo 'hello world'", new[] { "echo", "hello world" })]
    [InlineData("echo 'hello    world'", new[] { "echo", "hello    world" })]
    [InlineData("echo ''", new[] { "echo", "" })]
    [InlineData("echo 'hello''world'", new[] { "echo", "helloworld" })]
    [InlineData("echo hello''world", new[] { "echo", "helloworld" })]
    public void Tokenize_SingleQuotes_ShouldPreserveSpacesAndConcatenate(string input, string[] expected)
    {
        var result = CommandRouter.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo 'it\"s fine'", new[] { "echo", "it\"s fine" })]
    [InlineData("echo \"it's fine\"", new[] { "echo", "it's fine" })]
    public void Tokenize_NestedQuotes_ShouldTreatOppositeQuoteAsLiteral(string input, string[] expected)
    {
        var result = CommandRouter.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("cat '/tmp/file name'", new[] { "cat", "/tmp/file name" })]
    [InlineData("cat '/tmp/file name' '/tmp/another file'", new[] { "cat", "/tmp/file name", "/tmp/another file" })]
    public void Tokenize_SingleQuotedFilePaths_ShouldPreserveSpaces(string input, string[] expected)
    {
        var result = CommandRouter.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo world\\ \\ \\ \\ \\ \\ script", new[] { "echo", "world      script" })]
    [InlineData("echo hello\\\\world", new[] { "echo", "hello\\world" })]
    [InlineData("echo \\'\\\"hello world\\\"\\'", new[] { "echo", "'\"hello", "world\"'" })]
    [InlineData("cat \"/tmp/file\\\\name\"", new[] { "cat", "/tmp/file\\name" })]
    [InlineData("cat \"/tmp/file\\ name\"", new[] { "cat", "/tmp/file name" })]
    [InlineData("cat \"/tmp/file\\\\name\" \"/tmp/file\\ name\"", new[] { "cat", "/tmp/file\\name", "/tmp/file name" })]
    [InlineData("echo before\\ after", new[] { "echo", "before after" })]
    public void Tokenize_BackslashEscaping_ShouldEscapeSpecialCharacters(string input, string[] expected)
    {
        var result = CommandRouter.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo test\\nvalue", new[] { "echo", "testnvalue" })]
    [InlineData("echo \\$HOME", new[] { "echo", "$HOME" })]
    [InlineData("echo hello\\ \\ \\ \\ \\ \\ shell", new[] { "echo", "hello      shell" })]
    [InlineData("echo test\\nworld", new[] { "echo", "testnworld" })]
    [InlineData("cat \"/tmp/pig/f\\n24\" \"/tmp/pig/f\\56\" \"/tmp/pig/f'\\'4\"", new[] { "cat", "/tmp/pig/f\\n24", "/tmp/pig/f\\56", "/tmp/pig/f'\\4" })]
    [InlineData("cat \"/tmp/rat/f\\\\n58\" \"/tmp/rat/f\\\\60\" \"/tmp/rat/f'\\\\20\"", new[] { "cat", "/tmp/rat/f\\n58", "/tmp/rat/f\\60", "/tmp/rat/f'\\20" })]
    public void Tokenize_BackslashEscaping_MatchesCodeCraftersRequirements(string input, string[] expected)
    {
        var result = CommandRouter.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo 'no\\escape'", new[] { "echo", "no\\escape" })]
    [InlineData("echo 'test\\nvalue'", new[] { "echo", "test\\nvalue" })]
    public void Tokenize_BackslashInsideSingleQuotes_ShouldNotEscape(string input, string[] expected)
    {
        var result = CommandRouter.Tokenize(input);
        Assert.Equal(expected, result);
    }
}
