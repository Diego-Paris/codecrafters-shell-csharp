using MiniShell.Parsing;

namespace MiniShell.Tests.Parsing;

public class RedirectionParserTests
{
    [Fact]
    public void Parse_SimpleCommand_ReturnsCommandPartsOnly()
    {
        var tokens = new[] { "echo", "hello" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "echo", "hello" }, result.CommandParts);
        Assert.Null(result.StdoutFile);
        Assert.Null(result.StderrFile);
    }

    [Fact]
    public void Parse_StdoutRedirection_ExtractsFileAndCommand()
    {
        var tokens = new[] { "echo", "hello", ">", "output.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "echo", "hello" }, result.CommandParts);
        Assert.Equal("output.txt", result.StdoutFile);
        Assert.Null(result.StderrFile);
    }

    [Fact]
    public void Parse_StdoutRedirectionWith1Operator_ExtractsFileAndCommand()
    {
        var tokens = new[] { "echo", "hello", "1>", "output.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "echo", "hello" }, result.CommandParts);
        Assert.Equal("output.txt", result.StdoutFile);
        Assert.Null(result.StderrFile);
    }

    [Fact]
    public void Parse_StderrRedirection_ExtractsFileAndCommand()
    {
        var tokens = new[] { "cat", "nonexistent", "2>", "error.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "cat", "nonexistent" }, result.CommandParts);
        Assert.Null(result.StdoutFile);
        Assert.Equal("error.txt", result.StderrFile);
    }

    [Fact]
    public void Parse_BothStdoutAndStderrRedirection_ExtractsBothFiles()
    {
        var tokens = new[] { "cat", "file.txt", ">", "out.txt", "2>", "err.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "cat", "file.txt" }, result.CommandParts);
        Assert.Equal("out.txt", result.StdoutFile);
        Assert.Equal("err.txt", result.StderrFile);
    }

    [Fact]
    public void Parse_StderrBeforeStdout_ExtractsBothFiles()
    {
        var tokens = new[] { "cat", "file.txt", "2>", "err.txt", ">", "out.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "cat", "file.txt" }, result.CommandParts);
        Assert.Equal("out.txt", result.StdoutFile);
        Assert.Equal("err.txt", result.StderrFile);
    }

    [Fact]
    public void Parse_RedirectionWithPath_PreservesFullPath()
    {
        var tokens = new[] { "ls", "nonexistent", "2>", "test_output/subdir/error.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "ls", "nonexistent" }, result.CommandParts);
        Assert.Equal("test_output/subdir/error.txt", result.StderrFile);
    }

    [Fact]
    public void Parse_RedirectionOperatorAtEnd_IgnoresOperator()
    {
        var tokens = new[] { "echo", "hello", "2>" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "echo", "hello" }, result.CommandParts);
        Assert.Null(result.StderrFile);
    }

    [Fact]
    public void Parse_EmptyTokens_ReturnsEmptyCommandParts()
    {
        var tokens = Array.Empty<string>();

        var result = RedirectionParser.Parse(tokens);

        Assert.Empty(result.CommandParts);
        Assert.Null(result.StdoutFile);
        Assert.Null(result.StderrFile);
    }

    [Fact]
    public void Parse_OnlyRedirectionOperators_ReturnsEmptyCommandParts()
    {
        var tokens = new[] { ">", "out.txt", "2>", "err.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Empty(result.CommandParts);
        Assert.Equal("out.txt", result.StdoutFile);
        Assert.Equal("err.txt", result.StderrFile);
    }

    [Fact]
    public void Parse_MultipleStdoutRedirections_UsesLast()
    {
        var tokens = new[] { "echo", "test", ">", "first.txt", ">", "second.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "echo", "test" }, result.CommandParts);
        Assert.Equal("second.txt", result.StdoutFile);
    }

    [Fact]
    public void Parse_MultipleStderrRedirections_UsesLast()
    {
        var tokens = new[] { "cat", "nonexistent", "2>", "first.txt", "2>", "second.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "cat", "nonexistent" }, result.CommandParts);
        Assert.Equal("second.txt", result.StderrFile);
    }

    [Fact]
    public void Parse_AppendStdoutRedirection_SetsAppendFlag()
    {
        var tokens = new[] { "echo", "hello", ">>", "output.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "echo", "hello" }, result.CommandParts);
        Assert.Equal("output.txt", result.StdoutFile);
        Assert.True(result.AppendStdout);
    }

    [Fact]
    public void Parse_AppendStdoutWith1Operator_SetsAppendFlag()
    {
        var tokens = new[] { "echo", "hello", "1>>", "output.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "echo", "hello" }, result.CommandParts);
        Assert.Equal("output.txt", result.StdoutFile);
        Assert.True(result.AppendStdout);
    }

    [Fact]
    public void Parse_AppendStderrRedirection_SetsAppendFlag()
    {
        var tokens = new[] { "cat", "nonexistent", "2>>", "error.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "cat", "nonexistent" }, result.CommandParts);
        Assert.Equal("error.txt", result.StderrFile);
        Assert.True(result.AppendStderr);
    }

    [Fact]
    public void Parse_OverwriteThenAppend_UsesAppend()
    {
        var tokens = new[] { "echo", "test", ">", "first.txt", ">>", "second.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "echo", "test" }, result.CommandParts);
        Assert.Equal("second.txt", result.StdoutFile);
        Assert.True(result.AppendStdout);
    }

    [Fact]
    public void Parse_AppendThenOverwrite_UsesOverwrite()
    {
        var tokens = new[] { "echo", "test", ">>", "first.txt", ">", "second.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "echo", "test" }, result.CommandParts);
        Assert.Equal("second.txt", result.StdoutFile);
        Assert.False(result.AppendStdout);
    }

    [Fact]
    public void Parse_BothAppendRedirections_SetsBothFlags()
    {
        var tokens = new[] { "cat", "file.txt", ">>", "out.txt", "2>>", "err.txt" };

        var result = RedirectionParser.Parse(tokens);

        Assert.Equal(new[] { "cat", "file.txt" }, result.CommandParts);
        Assert.Equal("out.txt", result.StdoutFile);
        Assert.Equal("err.txt", result.StderrFile);
        Assert.True(result.AppendStdout);
        Assert.True(result.AppendStderr);
    }
}
