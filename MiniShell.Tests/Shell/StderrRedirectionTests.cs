using MiniShell.Abstractions;
using MiniShell.Commands;
using MiniShell.Runtime;

namespace MiniShell.Tests.Shell;

public class StderrRedirectionTests : IDisposable
{
    private readonly string _testDir;
    private readonly CommandRouter _router;
    private readonly StringWriter _stdout;
    private readonly StringWriter _stderr;

    public StderrRedirectionTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"shelltest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);

        _stdout = new StringWriter();
        _stderr = new StringWriter();

        var commands = new Dictionary<string, ICommand>
        {
            ["echo"] = new EchoCommand(),
            ["pwd"] = new PwdCommand(),
            ["external"] = new ExternalCommand()
        };

        var context = new MockShellContext
        {
            Commands = commands,
            In = Console.In,
            Out = _stdout,
            Err = _stderr,
            PathResolver = new PathResolver()
        };

        _router = new CommandRouter(context);
    }

    public void Dispose()
    {
        _stdout.Dispose();
        _stderr.Dispose();

        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void Route_StderrRedirection_CreatesFile()
    {
        var errFile = Path.Combine(_testDir, "error.txt");
        var command = $"cat nonexistent 2> \"{errFile}\"";

        _router.Route(command);

        Assert.True(File.Exists(errFile));
        var content = File.ReadAllText(errFile);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void Route_StderrRedirection_DoesNotWriteToStderr()
    {
        var errFile = Path.Combine(_testDir, "error.txt");
        var command = $"cat nonexistent 2> \"{errFile}\"";

        _router.Route(command);

        var stderrContent = _stderr.ToString();
        Assert.Empty(stderrContent);
    }

    [Fact]
    public void Route_StdoutRedirection_OnlyRedirectsStdout()
    {
        var outFile = Path.Combine(_testDir, "output.txt");
        var command = $"cat nonexistent > \"{outFile}\"";

        _router.Route(command);

        var stderrContent = _stderr.ToString();
        Assert.NotEmpty(stderrContent);
        Assert.Contains("nonexistent", stderrContent);
    }

    [Fact]
    public void Route_BothRedirections_RedirectsBothStreams()
    {
        var outFile = Path.Combine(_testDir, "output.txt");
        var errFile = Path.Combine(_testDir, "error.txt");
        var existsFile = Path.Combine(_testDir, "exists.txt");

        File.WriteAllText(existsFile, "file content");
        var command = $"cat \"{existsFile}\" nonexistent > \"{outFile}\" 2> \"{errFile}\"";

        _router.Route(command);

        Assert.True(File.Exists(outFile));
        Assert.True(File.Exists(errFile));

        var outContent = File.ReadAllText(outFile);
        var errContent = File.ReadAllText(errFile);

        Assert.Contains("file content", outContent);
        Assert.Contains("nonexistent", errContent);
    }

    [Fact]
    public void Route_StderrRedirectionWithNestedPath_CreatesDirectories()
    {
        var errFile = Path.Combine(_testDir, "subdir", "nested", "error.txt");
        var command = $"cat nonexistent 2> \"{errFile}\"";

        _router.Route(command);

        Assert.True(File.Exists(errFile));
        Assert.True(Directory.Exists(Path.Combine(_testDir, "subdir", "nested")));
    }

    [Fact]
    public void Route_EchoWithStderrRedirection_OutputsToStdoutFileEmpty()
    {
        var errFile = Path.Combine(_testDir, "error.txt");
        var command = $"echo 'Hello World' 2> \"{errFile}\"";

        _router.Route(command);

        var stdoutContent = _stdout.ToString();
        Assert.Contains("Hello World", stdoutContent);

        Assert.True(File.Exists(errFile));
        var errContent = File.ReadAllText(errFile);
        Assert.Empty(errContent);
    }

    [Fact]
    public void Route_StderrRedirectionOverwritesExistingFile()
    {
        var errFile = Path.Combine(_testDir, "error.txt");
        File.WriteAllText(errFile, "old error content");

        var command = $"cat nonexistent 2> \"{errFile}\"";
        _router.Route(command);

        var content = File.ReadAllText(errFile);
        Assert.DoesNotContain("old error content", content);
        Assert.Contains("nonexistent", content);
    }

    [Fact]
    public void Route_MultipleCommandsWithStderrRedirection_EachCreatesOwnFile()
    {
        var errFile1 = Path.Combine(_testDir, "error1.txt");
        var errFile2 = Path.Combine(_testDir, "error2.txt");

        _router.Route($"cat nonexistent1 2> \"{errFile1}\"");
        _router.Route($"cat nonexistent2 2> \"{errFile2}\"");

        Assert.True(File.Exists(errFile1));
        Assert.True(File.Exists(errFile2));

        var content1 = File.ReadAllText(errFile1);
        var content2 = File.ReadAllText(errFile2);

        Assert.Contains("nonexistent1", content1);
        Assert.Contains("nonexistent2", content2);
    }

    [Fact]
    public void Route_StderrRedirectionWithRelativePath_CreatesFileRelativeToWorkingDir()
    {
        var currentDir = Directory.GetCurrentDirectory();
        var relativeFile = "test_output/test_error.txt";
        var absolutePath = Path.Combine(currentDir, relativeFile);

        try
        {
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);

            _router.Route($"cat nonexistent 2> {relativeFile}");

            Assert.True(File.Exists(absolutePath));
            var content = File.ReadAllText(absolutePath);
            Assert.Contains("nonexistent", content);
        }
        finally
        {
            if (File.Exists(absolutePath))
                File.Delete(absolutePath);
        }
    }

    [Fact]
    public void Route_CommandNotFound_StderrRedirectionStillWorks()
    {
        var errFile = Path.Combine(_testDir, "error.txt");
        var command = $"nonexistentcommand 2> \"{errFile}\"";

        var exitCode = _router.Route(command);

        Assert.Equal(127, exitCode);
        var stdoutContent = _stdout.ToString();
        Assert.Contains("not found", stdoutContent);
    }

    [Fact]
    public void Route_BothRedirectionsSameFile_FileContainsBothOutputs()
    {
        var file = Path.Combine(_testDir, "combined.txt");
        var existsFile = Path.Combine(_testDir, "exists.txt");
        File.WriteAllText(existsFile, "content");

        _router.Route($"cat \"{existsFile}\" > \"{file}\"");
        var outContent = File.ReadAllText(file);

        _router.Route($"cat nonexistent 2> \"{file}\"");
        var errContent = File.ReadAllText(file);

        Assert.Contains("content", outContent);
        Assert.Contains("nonexistent", errContent);
        Assert.DoesNotContain("content", errContent);
    }

    private static string Quote(string path)
    {
        return path.Contains(' ') ? $"\"{path}\"" : path;
    }

    private class MockShellContext : IShellContext
    {
        public required IReadOnlyDictionary<string, ICommand> Commands { get; init; }
        public required TextReader In { get; init; }
        public required TextWriter Out { get; init; }
        public required TextWriter Err { get; init; }
        public required IPathResolver PathResolver { get; init; }
    }
}
