using MiniShell.Abstractions;
using MiniShell.Models;
using MiniShell.Runtime;

namespace MiniShell.Tests.Runtime;

public class FileRedirectionHandlerTests : IDisposable
{
    private readonly string _testDir;

    public FileRedirectionHandlerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"shelltest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            Directory.Delete(_testDir, recursive: true);
        }
    }

    [Fact]
    public void CreateRedirectedContext_NoRedirection_ReturnsSameWriters()
    {
        var originalOut = new StringWriter();
        var originalErr = new StringWriter();
        var context = CreateMockContext(originalOut, originalErr);
        var info = new RedirectionInfo(new[] { "echo", "test" });
        var handler = new FileRedirectionHandler();

        var redirected = handler.CreateRedirectedContext(context, info);

        Assert.Same(originalOut, redirected.Out);
        Assert.Same(originalErr, redirected.Err);
    }

    [Fact]
    public void CreateRedirectedContext_StdoutRedirection_CreatesFile()
    {
        var outFile = Path.Combine(_testDir, "output.txt");
        var context = CreateMockContext(new StringWriter(), new StringWriter());
        var info = new RedirectionInfo(new[] { "echo", "test" }, StdoutFile: outFile);
        var handler = new FileRedirectionHandler();

        var redirected = handler.CreateRedirectedContext(context, info);
        redirected.Out.WriteLine("test output");
        redirected.Out.Flush();

        Assert.True(File.Exists(outFile));
        handler.Cleanup();

        var content = File.ReadAllText(outFile);
        Assert.Contains("test output", content);
    }

    [Fact]
    public void CreateRedirectedContext_StderrRedirection_CreatesFile()
    {
        var errFile = Path.Combine(_testDir, "error.txt");
        var context = CreateMockContext(new StringWriter(), new StringWriter());
        var info = new RedirectionInfo(new[] { "cat", "nonexistent" }, StderrFile: errFile);
        var handler = new FileRedirectionHandler();

        var redirected = handler.CreateRedirectedContext(context, info);
        redirected.Err.WriteLine("error message");
        redirected.Err.Flush();

        Assert.True(File.Exists(errFile));
        handler.Cleanup();

        var content = File.ReadAllText(errFile);
        Assert.Contains("error message", content);
    }

    [Fact]
    public void CreateRedirectedContext_BothRedirections_CreatesBothFiles()
    {
        var outFile = Path.Combine(_testDir, "output.txt");
        var errFile = Path.Combine(_testDir, "error.txt");
        var context = CreateMockContext(new StringWriter(), new StringWriter());
        var info = new RedirectionInfo(
            new[] { "test" },
            StdoutFile: outFile,
            StderrFile: errFile
        );
        var handler = new FileRedirectionHandler();

        var redirected = handler.CreateRedirectedContext(context, info);
        redirected.Out.WriteLine("stdout content");
        redirected.Err.WriteLine("stderr content");
        redirected.Out.Flush();
        redirected.Err.Flush();

        Assert.True(File.Exists(outFile));
        Assert.True(File.Exists(errFile));
        handler.Cleanup();

        Assert.Contains("stdout content", File.ReadAllText(outFile));
        Assert.Contains("stderr content", File.ReadAllText(errFile));
    }

    [Fact]
    public void CreateRedirectedContext_NestedDirectory_CreatesDirectory()
    {
        var outFile = Path.Combine(_testDir, "subdir", "nested", "output.txt");
        var context = CreateMockContext(new StringWriter(), new StringWriter());
        var info = new RedirectionInfo(new[] { "echo", "test" }, StdoutFile: outFile);
        var handler = new FileRedirectionHandler();

        var redirected = handler.CreateRedirectedContext(context, info);
        redirected.Out.WriteLine("test");
        redirected.Out.Flush();

        Assert.True(File.Exists(outFile));
        Assert.True(Directory.Exists(Path.Combine(_testDir, "subdir", "nested")));
        handler.Cleanup();
    }

    [Fact]
    public void Cleanup_DisposesStreams_AllowsFileAccess()
    {
        var outFile = Path.Combine(_testDir, "output.txt");
        var context = CreateMockContext(new StringWriter(), new StringWriter());
        var info = new RedirectionInfo(new[] { "echo", "test" }, StdoutFile: outFile);
        var handler = new FileRedirectionHandler();

        var redirected = handler.CreateRedirectedContext(context, info);
        redirected.Out.WriteLine("test");
        handler.Cleanup();

        var content = File.ReadAllText(outFile);
        Assert.Contains("test", content);
    }

    [Fact]
    public void CreateRedirectedContext_PreservesOtherContextProperties()
    {
        var outFile = Path.Combine(_testDir, "output.txt");
        var mockCommands = new Dictionary<string, ICommand>();
        var mockIn = new StringReader("");
        var mockPathResolver = new MockPathResolver();

        var context = new MockShellContext
        {
            Commands = mockCommands,
            In = mockIn,
            Out = new StringWriter(),
            Err = new StringWriter(),
            PathResolver = mockPathResolver
        };

        var info = new RedirectionInfo(new[] { "echo", "test" }, StdoutFile: outFile);
        var handler = new FileRedirectionHandler();

        var redirected = handler.CreateRedirectedContext(context, info);

        Assert.Same(mockCommands, redirected.Commands);
        Assert.Same(mockIn, redirected.In);
        Assert.Same(mockPathResolver, redirected.PathResolver);

        handler.Cleanup();
    }

    [Fact]
    public void CreateRedirectedContext_FileAlreadyExists_OverwritesFile()
    {
        var outFile = Path.Combine(_testDir, "output.txt");
        File.WriteAllText(outFile, "old content");

        var context = CreateMockContext(new StringWriter(), new StringWriter());
        var info = new RedirectionInfo(new[] { "echo", "test" }, StdoutFile: outFile);
        var handler = new FileRedirectionHandler();

        var redirected = handler.CreateRedirectedContext(context, info);
        redirected.Out.WriteLine("new content");
        handler.Cleanup();

        var content = File.ReadAllText(outFile);
        Assert.Contains("new content", content);
        Assert.DoesNotContain("old content", content);
    }

    private static IShellContext CreateMockContext(TextWriter output, TextWriter error)
    {
        return new MockShellContext
        {
            Commands = new Dictionary<string, ICommand>(),
            In = new StringReader(""),
            Out = output,
            Err = error,
            PathResolver = new MockPathResolver()
        };
    }

    private class MockShellContext : IShellContext
    {
        public required IReadOnlyDictionary<string, ICommand> Commands { get; init; }
        public required TextReader In { get; init; }
        public required TextWriter Out { get; init; }
        public required TextWriter Err { get; init; }
        public required IPathResolver PathResolver { get; init; }
        public void AddToHistory(string command) { }
        public IReadOnlyList<string> CommandHistory { get; init; } = Array.Empty<string>();
        public IReadOnlyList<string> GetCommandsSinceLastAppend() => Array.Empty<string>();
        public void MarkLastAppendPosition() { }
        public void SaveHistoryToFile() { }
    }

    private class MockPathResolver : IPathResolver
    {
        public string? FindInPath(string executable) => null;
    }
}
