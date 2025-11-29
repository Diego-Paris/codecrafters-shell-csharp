using MiniShell.Abstractions;
using MiniShell.Commands;
using MiniShell.Runtime;

namespace MiniShell.Tests.Shell;

public class AppendRedirectionTests : IDisposable
{
    private readonly string _testDir;
    private readonly CommandRouter _router;
    private readonly StringWriter _stdout;
    private readonly StringWriter _stderr;

    public AppendRedirectionTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"shelltest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);

        _stdout = new StringWriter();
        _stderr = new StringWriter();

        var commands = new Dictionary<string, ICommand>
        {
            ["echo"] = new EchoCommand(),
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
    public void Route_AppendStdout_AppendsToFile()
    {
        var outFile = Path.Combine(_testDir, "output.txt");

        _router.Route($"echo 'Hello Emily' >> \"{outFile}\"");
        _router.Route($"echo 'Hello Maria' >> \"{outFile}\"");

        var content = File.ReadAllText(outFile);
        Assert.Contains("Hello Emily", content);
        Assert.Contains("Hello Maria", content);
    }

    [Fact]
    public void Route_AppendStdoutWith1Operator_AppendsToFile()
    {
        var outFile = Path.Combine(_testDir, "output.txt");

        _router.Route($"echo 'First line' 1>> \"{outFile}\"");
        _router.Route($"echo 'Second line' 1>> \"{outFile}\"");

        var content = File.ReadAllText(outFile);
        var lines = content.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(2, lines.Length);
        Assert.Contains("First line", lines[0]);
        Assert.Contains("Second line", lines[1]);
    }

    [Fact]
    public void Route_OverwriteThenAppend_CreatesFilesThenAppends()
    {
        var outFile = Path.Combine(_testDir, "output.txt");

        _router.Route($"echo 'List of files: ' > \"{outFile}\"");

        var existsFile = Path.Combine(_testDir, "baz");
        Directory.CreateDirectory(existsFile);
        File.WriteAllText(Path.Combine(existsFile, "apple"), "");
        File.WriteAllText(Path.Combine(existsFile, "banana"), "");
        File.WriteAllText(Path.Combine(existsFile, "blueberry"), "");

        _router.Route($"external ls \"{existsFile}\" >> \"{outFile}\"");

        var content = File.ReadAllText(outFile);
        Assert.Contains("List of files:", content);
        Assert.Contains("apple", content);
        Assert.Contains("banana", content);
        Assert.Contains("blueberry", content);
    }

    [Fact]
    public void Route_AppendStderr_AppendsToFile()
    {
        var errFile = Path.Combine(_testDir, "error.txt");

        _router.Route($"cat nonexistent1 2>> \"{errFile}\"");
        _router.Route($"cat nonexistent2 2>> \"{errFile}\"");

        var content = File.ReadAllText(errFile);
        Assert.Contains("nonexistent1", content);
        Assert.Contains("nonexistent2", content);
    }

    [Fact]
    public void Route_AppendToNonexistentFile_CreatesFile()
    {
        var outFile = Path.Combine(_testDir, "output.txt");

        _router.Route($"echo 'Hello World' >> \"{outFile}\"");

        Assert.True(File.Exists(outFile));
        var content = File.ReadAllText(outFile);
        Assert.Contains("Hello World", content);
    }

    [Fact]
    public void Route_AppendWithNestedPath_CreatesDirectories()
    {
        var outFile = Path.Combine(_testDir, "subdir", "nested", "output.txt");

        _router.Route($"echo 'test' >> \"{outFile}\"");

        Assert.True(File.Exists(outFile));
        Assert.True(Directory.Exists(Path.Combine(_testDir, "subdir", "nested")));
    }

    [Fact]
    public void Route_AppendDoesNotAffectStdout()
    {
        var outFile = Path.Combine(_testDir, "output.txt");

        _router.Route($"echo 'Hello World' >> \"{outFile}\"");

        var stdoutContent = _stdout.ToString();
        Assert.Empty(stdoutContent);
    }

    [Fact]
    public void Route_MixedOverwriteAndAppend_HandlesCorrectly()
    {
        var outFile = Path.Combine(_testDir, "output.txt");

        _router.Route($"echo 'First' > \"{outFile}\"");
        var content1 = File.ReadAllText(outFile);
        Assert.Contains("First", content1);
        Assert.DoesNotContain("Second", content1);

        _router.Route($"echo 'Second' >> \"{outFile}\"");
        var content2 = File.ReadAllText(outFile);
        Assert.Contains("First", content2);
        Assert.Contains("Second", content2);

        _router.Route($"echo 'Third' > \"{outFile}\"");
        var content3 = File.ReadAllText(outFile);
        Assert.DoesNotContain("First", content3);
        Assert.DoesNotContain("Second", content3);
        Assert.Contains("Third", content3);
    }

    [Fact]
    public void Route_BothAppendRedirections_AppendsToBothFiles()
    {
        var outFile = Path.Combine(_testDir, "output.txt");
        var errFile = Path.Combine(_testDir, "error.txt");
        var existsFile = Path.Combine(_testDir, "exists.txt");

        File.WriteAllText(existsFile, "file content");

        _router.Route($"cat \"{existsFile}\" >> \"{outFile}\"");
        _router.Route($"cat nonexistent >> \"{outFile}\" 2>> \"{errFile}\"");

        var outContent = File.ReadAllText(outFile);
        var errContent = File.ReadAllText(errFile);

        Assert.Contains("file content", outContent);
        Assert.Contains("nonexistent", errContent);
    }

    private class MockShellContext : IShellContext
    {
        public required IReadOnlyDictionary<string, ICommand> Commands { get; init; }
        public required TextReader In { get; init; }
        public required TextWriter Out { get; init; }
        public required TextWriter Err { get; init; }
        public IReadOnlyList<string> CommandHistory { get; init; } = Array.Empty<string>();
        public required IPathResolver PathResolver { get; init; }
        public void AddToHistory(string command) { }
        public IReadOnlyList<string> GetCommandsSinceLastAppend() => Array.Empty<string>();
        public void MarkLastAppendPosition() { }
        public void SaveHistoryToFile() { }
    }
}
