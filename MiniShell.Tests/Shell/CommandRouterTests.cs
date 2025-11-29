using MiniShell;
using MiniShell.Abstractions;
using MiniShell.Parsing;
using MiniShell.Runtime;

namespace MiniShell.Tests.Shell;

public class CommandRouterTests : IDisposable
{
    private readonly string _testDirectory;

    public CommandRouterTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"shell_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }
    [Theory]
    [InlineData("echo hello world", new[] { "echo", "hello", "world" })]
    [InlineData("echo hello", new[] { "echo", "hello" })]
    [InlineData("echo", new[] { "echo" })]
    [InlineData("", new string[] { })]
    [InlineData("   ", new string[] { })]
    public void Tokenize_BasicCommands_ShouldSplitByWhitespace(string input, string[] expected)
    {
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo  hello    world", new[] { "echo", "hello", "world" })]
    [InlineData("echo\thello\tworld", new[] { "echo", "hello", "world" })]
    [InlineData("  echo  hello  ", new[] { "echo", "hello" })]
    public void Tokenize_MultipleSpaces_ShouldCollapseWhitespace(string input, string[] expected)
    {
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
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
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
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
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo 'it\"s fine'", new[] { "echo", "it\"s fine" })]
    [InlineData("echo \"it's fine\"", new[] { "echo", "it's fine" })]
    public void Tokenize_NestedQuotes_ShouldTreatOppositeQuoteAsLiteral(string input, string[] expected)
    {
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("cat '/tmp/file name'", new[] { "cat", "/tmp/file name" })]
    [InlineData("cat '/tmp/file name' '/tmp/another file'", new[] { "cat", "/tmp/file name", "/tmp/another file" })]
    public void Tokenize_SingleQuotedFilePaths_ShouldPreserveSpaces(string input, string[] expected)
    {
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo world\\ \\ \\ \\ \\ \\ script", new[] { "echo", "world      script" })]
    [InlineData("echo hello\\\\world", new[] { "echo", "hello\\world" })]
    [InlineData("echo \\'\\\"hello world\\\"\\'", new[] { "echo", "'\"hello", "world\"'" })]
    [InlineData("cat \"/tmp/file\\\\name\"", new[] { "cat", "/tmp/file\\name" })]
    [InlineData("cat \"/tmp/file\\ name\"", new[] { "cat", "/tmp/file\\ name" })]
    [InlineData("cat \"/tmp/file\\\\name\" \"/tmp/file\\ name\"", new[] { "cat", "/tmp/file\\name", "/tmp/file\\ name" })]
    [InlineData("echo before\\ after", new[] { "echo", "before after" })]
    public void Tokenize_BackslashEscaping_ShouldEscapeSpecialCharacters(string input, string[] expected)
    {
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo test\\nvalue", new[] { "echo", "testnvalue" })]
    [InlineData("echo \\$HOME", new[] { "echo", "$HOME" })]
    [InlineData("echo hello\\ \\ \\ \\ \\ \\ shell", new[] { "echo", "hello      shell" })]
    [InlineData("echo test\\nworld", new[] { "echo", "testnworld" })]
    [InlineData("cat \"/tmp/pig/f\\n24\" \"/tmp/pig/f\\56\" \"/tmp/pig/f'\\'4\"", new[] { "cat", "/tmp/pig/f\\n24", "/tmp/pig/f\\56", "/tmp/pig/f'\\'4" })]
    [InlineData("cat \"/tmp/rat/f\\\\n58\" \"/tmp/rat/f\\\\60\" \"/tmp/rat/f'\\\\20\"", new[] { "cat", "/tmp/rat/f\\n58", "/tmp/rat/f\\60", "/tmp/rat/f'\\20" })]
    public void Tokenize_BackslashEscaping_MatchesCodeCraftersRequirements(string input, string[] expected)
    {
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo 'no\\escape'", new[] { "echo", "no\\escape" })]
    [InlineData("echo 'test\\nvalue'", new[] { "echo", "test\\nvalue" })]
    public void Tokenize_BackslashInsideSingleQuotes_ShouldNotEscape(string input, string[] expected)
    {
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo hello > /tmp/output.txt", new[] { "echo", "hello", ">", "/tmp/output.txt" })]
    [InlineData("echo hello > output.txt", new[] { "echo", "hello", ">", "output.txt" })]
    [InlineData("cat file.txt > /tmp/foo/bar.md", new[] { "cat", "file.txt", ">", "/tmp/foo/bar.md" })]
    public void Tokenize_OutputRedirection_ShouldSplitRedirectionOperator(string input, string[] expected)
    {
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo hello 1> /tmp/output.txt", new[] { "echo", "hello", "1>", "/tmp/output.txt" })]
    [InlineData("cat file.txt 1> output.txt", new[] { "cat", "file.txt", "1>", "output.txt" })]
    [InlineData("echo 'Hello James' 1> /tmp/foo/foo.md", new[] { "echo", "Hello James", "1>", "/tmp/foo/foo.md" })]
    public void Tokenize_OutputRedirectionWithFileDescriptor_ShouldSplitRedirectionOperator(string input, string[] expected)
    {
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("ls /tmp/baz>/tmp/foo/baz.md", new[] { "ls", "/tmp/baz>/tmp/foo/baz.md" })]
    [InlineData("echo hello>output.txt", new[] { "echo", "hello>output.txt" })]
    [InlineData("cat file1 file2>combined.txt", new[] { "cat", "file1", "file2>combined.txt" })]
    public void Tokenize_OutputRedirectionWithoutSpaces_CurrentBehavior(string input, string[] expected)
    {
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("cat nonexistent 1> /tmp/foo/quz.md", new[] { "cat", "nonexistent", "1>", "/tmp/foo/quz.md" })]
    [InlineData("cat /tmp/baz/blueberry nonexistent 1> /tmp/foo/quz.md", new[] { "cat", "/tmp/baz/blueberry", "nonexistent", "1>", "/tmp/foo/quz.md" })]
    public void Tokenize_OutputRedirectionWithStderr_ShouldSplitRedirectionOperator(string input, string[] expected)
    {
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("echo hello > '/tmp/file name.txt'", new[] { "echo", "hello", ">", "/tmp/file name.txt" })]
    [InlineData("echo test 1> \"/tmp/foo bar/output.md\"", new[] { "echo", "test", "1>", "/tmp/foo bar/output.md" })]
    public void Tokenize_OutputRedirectionWithQuotedPaths_ShouldPreserveSpaces(string input, string[] expected)
    {
        var tokenizer = new ShellTokenizer();
        var result = tokenizer.Tokenize(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void Route_OutputRedirection_EchoToFile_ShouldWriteContentToFile()
    {
        // Arrange
        var outputFile = Path.Combine(_testDirectory, "output.txt");
        var ctx = CreateShellContext();
        var tokenizer = new ShellTokenizer();
        var router = new CommandRouter(ctx, tokenizer);

        // Act
        var exitCode = router.Route($"echo hello > \"{outputFile}\"");

        // Assert
        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputFile), "Output file should be created");
        var content = File.ReadAllText(outputFile).Trim();
        Assert.Equal("hello", content);
    }

    [Fact]
    public void Route_OutputRedirection_EchoWithExplicitFileDescriptor_ShouldWriteContentToFile()
    {
        var outputFile = Path.Combine(_testDirectory, "output.txt");
        var ctx = CreateShellContext();
        var tokenizer = new ShellTokenizer();
        var router = new CommandRouter(ctx, tokenizer);

        var exitCode = router.Route($"echo 'Hello James' 1> \"{outputFile}\"");

        Assert.Equal(0, exitCode);
        Assert.True(File.Exists(outputFile), "Output file should be created");
        var content = File.ReadAllText(outputFile).Trim();
        Assert.Equal("Hello James", content);
    }

    [Fact]
    public void Route_OutputRedirection_CatWithNonexistentFile_ShouldRedirectStdoutOnly()
    {
        // Arrange
        var existingFile = Path.Combine(_testDirectory, "existing.txt");
        File.WriteAllText(existingFile, "blueberry");

        var outputFile = Path.Combine(_testDirectory, "output.txt");
        var ctx = CreateShellContext();
        var tokenizer = new ShellTokenizer();
        var router = new CommandRouter(ctx, tokenizer);

        // Act - cat with one existing file and one nonexistent
        var exitCode = router.Route($"cat \"{existingFile}\" nonexistent 1> \"{outputFile}\"");

        // Assert
        Assert.NotEqual(0, exitCode); // Should fail because of nonexistent file
        Assert.True(File.Exists(outputFile), "Output file should be created");

        // Only stdout should be redirected, stderr should go to console
        var content = File.ReadAllText(outputFile).Trim();
        Assert.Equal("blueberry", content);
        Assert.DoesNotContain("nonexistent", content); // Error message should NOT be in file
    }

    [Fact]
    public void Route_OutputRedirection_OverwriteExistingFile_ShouldReplaceContent()
    {
        // Arrange
        var outputFile = Path.Combine(_testDirectory, "output.txt");
        File.WriteAllText(outputFile, "old content");

        var ctx = CreateShellContext();
        var tokenizer = new ShellTokenizer();
        var router = new CommandRouter(ctx, tokenizer);

        // Act
        var exitCode = router.Route($"echo new content > \"{outputFile}\"");

        // Assert
        Assert.Equal(0, exitCode);
        var content = File.ReadAllText(outputFile).Trim();
        Assert.Equal("new content", content);
        Assert.DoesNotContain("old content", content);
    }

    private IShellContext CreateShellContext()
    {
        var mockHistoryService = new MockHistoryService();
        var commands = new ICommand[]
        {
            new MiniShell.Commands.EchoCommand(),
            new MiniShell.Commands.PwdCommand(),
            new MiniShell.Commands.CdCommand(),
            new MiniShell.Commands.TypeCommand(),
            new MiniShell.Commands.ExitCommand(),
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
