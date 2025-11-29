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
    public void ReadInput_EmptyPrefixTab_ShouldNotRingBell()
    {
        var provider = new MockCompletionProvider(new[] { "echo", "exit" });
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
