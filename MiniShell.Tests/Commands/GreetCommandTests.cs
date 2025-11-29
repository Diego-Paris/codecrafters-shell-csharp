using MiniShell.Abstractions;
using MiniShell.Commands;
using MiniShell.Runtime;

namespace MiniShell.Tests.Commands;

public class GreetCommandTests
{
    [Fact]
    public void Name_ShouldReturnGreet()
    {
        var command = new GreetCommand();

        Assert.Equal("greet", command.Name);
    }

    [Fact]
    public void Description_ShouldHaveValue()
    {
        var command = new GreetCommand();

        Assert.NotNull(command.Description);
        Assert.NotEmpty(command.Description);
    }

    [Fact]
    public void Execute_ShouldPrintHelloWorld()
    {
        var command = new GreetCommand();
        var output = new StringWriter();
        var ctx = CreateShellContext(output);

        var exitCode = command.Execute(Array.Empty<string>(), ctx);

        Assert.Equal(0, exitCode);
        Assert.Equal("Hello, World!", output.ToString().Trim());
    }

    [Fact]
    public void Execute_WithArguments_ShouldIgnoreArgumentsAndPrintHelloWorld()
    {
        var command = new GreetCommand();
        var output = new StringWriter();
        var ctx = CreateShellContext(output);

        var exitCode = command.Execute(new[] { "arg1", "arg2" }, ctx);

        Assert.Equal(0, exitCode);
        Assert.Equal("Hello, World!", output.ToString().Trim());
    }

    private IShellContext CreateShellContext(TextWriter output)
    {
        var commands = new ICommand[] { new GreetCommand() };
        var pathResolver = new PathResolver();
        return new ShellContext(commands, pathResolver, TextReader.Null, output, TextWriter.Null);
    }
}
