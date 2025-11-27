using MiniShell.Abstractions;
using MiniShell.Models;

namespace MiniShell.Runtime;

public sealed class FileRedirectionHandler : IRedirectionHandler
{
    private readonly List<IDisposable> _disposables = [];

    public IShellContext CreateRedirectedContext(IShellContext context, RedirectionInfo info)
    {
        var outWriter = CreateWriter(info.StdoutFile) ?? context.Out;
        var errWriter = CreateWriter(info.StderrFile) ?? context.Err;

        return new RedirectedShellContext(context, outWriter, errWriter);
    }

    public void Cleanup()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }

    private StreamWriter? CreateWriter(string? filePath)
    {
        if (filePath is null) return null;

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read);
        var writer = new StreamWriter(stream) { AutoFlush = true };

        _disposables.Add(writer);
        _disposables.Add(stream);

        return writer;
    }

    private sealed class RedirectedShellContext : IShellContext
    {
        private readonly IShellContext _inner;
        private readonly TextWriter _redirectedOut;
        private readonly TextWriter _redirectedErr;

        public RedirectedShellContext(IShellContext inner, TextWriter redirectedOut, TextWriter redirectedErr)
        {
            _inner = inner;
            _redirectedOut = redirectedOut;
            _redirectedErr = redirectedErr;
        }

        public IReadOnlyDictionary<string, ICommand> Commands => _inner.Commands;
        public TextReader In => _inner.In;
        public TextWriter Out => _redirectedOut;
        public TextWriter Err => _redirectedErr;
        public IPathResolver PathResolver => _inner.PathResolver;
    }
}
