using MiniShell.Abstractions;
using MiniShell.Models;

namespace MiniShell.Runtime;

/// <summary>
/// Implements file-based stdout/stderr redirection with automatic directory creation and resource cleanup.
/// </summary>
public sealed class FileRedirectionHandler : IRedirectionHandler
{
    private readonly List<IDisposable> _disposables = [];

    /// <summary>
    /// Creates a shell context with stdout/stderr redirected according to the redirection configuration.
    /// </summary>
    /// <param name="context">The original shell context to wrap.</param>
    /// <param name="info">Redirection configuration specifying target files and append modes.</param>
    /// <returns>A wrapped context with redirected output streams.</returns>
    public IShellContext CreateRedirectedContext(IShellContext context, RedirectionInfo info)
    {
        var outWriter = CreateWriter(info.StdoutFile, info.AppendStdout) ?? context.Out;
        var errWriter = CreateWriter(info.StderrFile, info.AppendStderr) ?? context.Err;

        return new RedirectedShellContext(context, outWriter, errWriter);
    }

    /// <summary>
    /// Releases resources held by redirected file streams, ensuring all buffered data is flushed.
    /// </summary>
    public void Cleanup()
    {
        foreach (var disposable in _disposables)
        {
            disposable.Dispose();
        }
        _disposables.Clear();
    }

    /// <summary>
    /// Creates a file writer for redirection, creating directories as needed and tracking resources for cleanup.
    /// </summary>
    /// <param name="filePath">The target file path, or null if no redirection.</param>
    /// <param name="append">Whether to append to the file rather than overwrite.</param>
    /// <returns>A StreamWriter for the file, or null if filePath is null.</returns>
    private StreamWriter? CreateWriter(string? filePath, bool append)
    {
        if (filePath is null) return null;

        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var mode = append ? FileMode.Append : FileMode.Create;
        var stream = new FileStream(filePath, mode, FileAccess.Write, FileShare.Read);
        var writer = new StreamWriter(stream) { AutoFlush = true };

        _disposables.Add(writer);
        _disposables.Add(stream);

        return writer;
    }

    /// <summary>
    /// Wraps a shell context with redirected output streams while preserving all other context properties.
    /// </summary>
    private sealed class RedirectedShellContext : IShellContext
    {
        private readonly IShellContext _inner;
        private readonly TextWriter _redirectedOut;
        private readonly TextWriter _redirectedErr;

        /// <summary>
        /// Initializes a redirected context wrapper with the specified output streams.
        /// </summary>
        /// <param name="inner">The inner context to wrap.</param>
        /// <param name="redirectedOut">The redirected stdout writer.</param>
        /// <param name="redirectedErr">The redirected stderr writer.</param>
        public RedirectedShellContext(IShellContext inner, TextWriter redirectedOut, TextWriter redirectedErr)
        {
            _inner = inner;
            _redirectedOut = redirectedOut;
            _redirectedErr = redirectedErr;
        }

        /// <summary>
        /// Gets the command registry from the inner context.
        /// </summary>
        public IReadOnlyDictionary<string, ICommand> Commands => _inner.Commands;

        /// <summary>
        /// Gets the stdin from the inner context (unchanged by redirection).
        /// </summary>
        public TextReader In => _inner.In;

        /// <summary>
        /// Gets the redirected stdout writer.
        /// </summary>
        public TextWriter Out => _redirectedOut;

        /// <summary>
        /// Gets the redirected stderr writer.
        /// </summary>
        public TextWriter Err => _redirectedErr;

        /// <summary>
        /// Gets the path resolver from the inner context.
        /// </summary>
        public IPathResolver PathResolver => _inner.PathResolver;

        /// <summary>
        /// Gets the command history from the inner context.
        /// </summary>
        public IReadOnlyList<string> CommandHistory => _inner.CommandHistory;
    }
}
