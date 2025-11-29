using MiniShell.Models;

namespace MiniShell.Abstractions;

/// <summary>
/// Handles stdout and stderr redirection to files, managing file streams and context wrapping.
/// </summary>
public interface IRedirectionHandler
{
    /// <summary>
    /// Creates a shell context with stdout/stderr redirected according to the redirection configuration.
    /// </summary>
    /// <param name="context">The original shell context to wrap.</param>
    /// <param name="info">Redirection configuration specifying target files and append modes.</param>
    /// <returns>A wrapped context with redirected output streams.</returns>
    IShellContext CreateRedirectedContext(IShellContext context, RedirectionInfo info);

    /// <summary>
    /// Releases resources held by redirected file streams, ensuring all buffered data is flushed.
    /// </summary>
    void Cleanup();
}
