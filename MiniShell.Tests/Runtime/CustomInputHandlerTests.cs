using MiniShell.Abstractions;
using MiniShell.Runtime;
using System.Text;

namespace MiniShell.Tests.Runtime;

public class CustomInputHandlerTests
{
    [Fact]
    public void ReadInput_TabWithNoMatches_ShouldRingBell()
    {
        var provider = new MockCompletionProvider(Array.Empty<string>());
        var mockConsole = new MockConsole(new[] { 'x', 'y', 'z', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        Assert.Contains('\x07', mockConsole.Output);
        Assert.Equal("xyz", result);
    }

    [Fact]
    public void ReadInput_TabWithMatch_ShouldNotRingBell()
    {
        var provider = new MockCompletionProvider(new[] { "echo" });
        var mockConsole = new MockConsole(new[] { 'e', 'c', 'h', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        Assert.DoesNotContain('\x07', mockConsole.Output);
        Assert.Equal("echo ", result);
    }

    [Fact]
    public void ReadInput_TabWithMultipleInvalidAttempts_ShouldRingBellEachTime()
    {
        var provider = new MockCompletionProvider(Array.Empty<string>());
        var mockConsole = new MockConsole(new[] { 'x', 'y', 'z', '\t', '\t', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        var bellCount = mockConsole.Output.Count(c => c == '\x07');
        Assert.Equal(3, bellCount);
    }

    [Fact]
    public void ReadInput_TabAfterCompletedCommand_ShouldRingBell()
    {
        var provider = new MockCompletionProvider(new[] { "echo" });
        var mockConsole = new MockConsole(new[] { 'e', 'c', 'h', '\t', 'h', 'e', 'l', 'l', 'o', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        var bellCount = mockConsole.Output.Count(c => c == '\x07');
        Assert.Equal(1, bellCount);
        Assert.Equal("echo hello", result);
    }

    [Fact]
    public void ReadInput_EmptyPrefixTabWithSingleMatch_ShouldComplete()
    {
        var provider = new MockCompletionProvider(new[] { "echo" });
        var mockConsole = new MockConsole(new[] { '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        Assert.DoesNotContain('\x07', mockConsole.Output);
        Assert.Equal("echo ", result);
    }

    [Fact]
    public void ReadInput_PartialMatchThenNoMatch_ShouldRingBellOnSecondTab()
    {
        var provider = new MockCompletionProvider(new[] { "echo" });
        var mockConsole = new MockConsole(new[] { 'e', 'c', 'h', '\t', 'x', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        var bellCount = mockConsole.Output.Count(c => c == '\x07');
        Assert.Equal(1, bellCount);
    }

    [Fact]
    public void ReadInput_ValidCommandFollowedByArguments_AllowsExecution()
    {
        var provider = new MockCompletionProvider(new[] { "echo" });
        var mockConsole = new MockConsole(new[] { 'e', 'c', 'h', '\t', 'h', 'e', 'l', 'l', 'o', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        Assert.Equal("echo hello", result);
        Assert.Contains("$ ", mockConsole.Output);
        Assert.Contains("o ", mockConsole.Output);
    }

    [Fact]
    public void ReadInput_FirstTabWithMultipleMatches_ShouldRingBell()
    {
        var provider = new MockCompletionProvider(new[] { "xyz_bar", "xyz_baz", "xyz_quz" });
        var mockConsole = new MockConsole(new[] { 'x', 'y', 'z', '_', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        Assert.Contains('\x07', mockConsole.Output);
        Assert.Equal("xyz_", result);
    }

    [Fact]
    public void ReadInput_SecondTabWithMultipleMatches_ShouldDisplayAllMatchesSorted()
    {
        var provider = new MockCompletionProvider(new[] { "xyz_quz", "xyz_bar", "xyz_baz" });
        var mockConsole = new MockConsole(new[] { 'x', 'y', 'z', '_', '\t', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        var output = mockConsole.Output;
        Assert.Contains("xyz_bar  xyz_baz  xyz_quz", output);
        Assert.Contains('\x07', output);
        Assert.Equal("xyz_", result);
    }

    [Fact]
    public void ReadInput_SecondTabWithMultipleMatches_ShouldReprintPrompt()
    {
        var provider = new MockCompletionProvider(new[] { "abc", "abd", "abe" });
        var mockConsole = new MockConsole(new[] { 'a', 'b', '\t', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        var output = mockConsole.Output;
        var promptCount = CountOccurrences(output, "$ ");
        Assert.True(promptCount >= 2, $"Expected at least 2 prompts, found {promptCount}");
        Assert.Contains("$ ab", output);
    }

    [Fact]
    public void ReadInput_ThirdTabAfterDisplayingMatches_ShouldRingBellAgain()
    {
        var provider = new MockCompletionProvider(new[] { "test1", "test2", "test3" });
        var mockConsole = new MockConsole(new[] { 't', 'e', 's', 't', '\t', '\t', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        var bellCount = mockConsole.Output.Count(c => c == '\x07');
        Assert.Equal(2, bellCount);
    }

    [Fact]
    public void ReadInput_TabAfterTypingMoreCharacters_ShouldResetTabState()
    {
        var provider = new MockCompletionProvider(new[] { "abc", "abd", "xyz" });
        var mockConsole = new MockConsole(new[] { 'a', 'b', '\t', 'c', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        Assert.Equal("abc ", result);
        Assert.Contains('\x07', mockConsole.Output);
    }

    [Fact]
    public void ReadInput_MultipleMatchesWithDifferentPrefixes_HandlesCorrectly()
    {
        var provider = new MockCompletionProvider(new[] { "echo", "exit", "export" });
        var mockConsole = new MockConsole(new[] { 'e', '\t', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        Assert.Contains("echo  exit  export", mockConsole.Output);
        Assert.Equal("e", result);
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += pattern.Length;
        }
        return count;
    }

    [Fact]
    public void ReadInput_LongestCommonPrefix_CompletesToPrefix()
    {
        var provider = new MockCompletionProvider(new[] { "xyz_foo", "xyz_foo_bar", "xyz_foo_bar_baz" });
        var mockConsole = new MockConsole(new[] { 'x', 'y', 'z', '_', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        Assert.Equal("xyz_foo", result);
        Assert.DoesNotContain('\x07', mockConsole.Output);
    }

    [Fact]
    public void ReadInput_LongestCommonPrefix_ProgressiveDeepeningCompletion()
    {
        var provider = new MockCompletionProvider(new[] { "xyz_foo", "xyz_foo_bar", "xyz_foo_bar_baz" });
        var mockConsole = new MockConsole(new[] { 'x', 'y', 'z', '_', '\t', '_', '\t', '_', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        Assert.Equal("xyz_foo_bar_baz ", result);
    }

    [Fact]
    public void ReadInput_LongestCommonPrefix_NoCommonPrefixBeyondInput()
    {
        var provider = new MockCompletionProvider(new[] { "abc", "abd", "abe" });
        var mockConsole = new MockConsole(new[] { 'a', 'b', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        Assert.Contains('\x07', mockConsole.Output);
        Assert.Equal("ab", result);
    }

    [Fact]
    public void ReadInput_LongestCommonPrefix_PartialCompletionThenBell()
    {
        var provider = new MockCompletionProvider(new[] { "test_a", "test_b", "test_c" });
        var mockConsole = new MockConsole(new[] { 't', 'e', '\t', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        Assert.Equal("test_", result);
        Assert.Contains('\x07', mockConsole.Output);
    }

    [Fact]
    public void ReadInput_LongestCommonPrefix_ExactMatchIsPrefix()
    {
        var provider = new MockCompletionProvider(new[] { "foo", "foobar", "foobarbaz" });
        var mockConsole = new MockConsole(new[] { 'f', '\t', '\r' });
        var handler = new CustomInputHandler(provider, mockConsole);

        var result = handler.ReadInput("$ ");

        Assert.Equal("foo", result);
        Assert.DoesNotContain('\x07', mockConsole.Output);
    }

    private class MockConsole : IConsole
    {
        private readonly Queue<char> _inputQueue;
        private readonly StringBuilder _output;

        public MockConsole(IEnumerable<char> input)
        {
            _inputQueue = new Queue<char>(input);
            _output = new StringBuilder();
        }

        public string Output => _output.ToString();

        public ConsoleKeyInfo ReadKey(bool intercept)
        {
            if (_inputQueue.Count == 0)
                throw new InvalidOperationException("No more input available");

            var ch = _inputQueue.Dequeue();

            return ch switch
            {
                '\r' => new ConsoleKeyInfo('\r', ConsoleKey.Enter, false, false, false),
                '\t' => new ConsoleKeyInfo('\t', ConsoleKey.Tab, false, false, false),
                '\b' => new ConsoleKeyInfo('\b', ConsoleKey.Backspace, false, false, false),
                _ => new ConsoleKeyInfo(ch, ConsoleKey.A, false, false, false)
            };
        }

        public void Write(char value)
        {
            _output.Append(value);
        }

        public void Write(string value)
        {
            _output.Append(value);
        }

        public void WriteLine()
        {
            _output.AppendLine();
        }
    }

    private class MockCompletionProvider : ICompletionProvider
    {
        private readonly string[] _completions;

        public MockCompletionProvider(string[] completions)
        {
            _completions = completions;
        }

        public IEnumerable<string> GetCompletions(string prefix)
        {
            return _completions.Where(c => c.StartsWith(prefix, StringComparison.Ordinal));
        }
    }
}
