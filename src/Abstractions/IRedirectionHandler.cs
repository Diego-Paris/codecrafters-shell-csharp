using MiniShell.Models;

namespace MiniShell.Abstractions;

public interface IRedirectionHandler
{
    IShellContext CreateRedirectedContext(IShellContext context, RedirectionInfo info);
    void Cleanup();
}
