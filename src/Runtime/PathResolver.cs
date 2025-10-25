using System.Runtime.InteropServices;
using MiniShell.Abstractions;

namespace MiniShell.Runtime;

public sealed class PathResolver : IPathResolver
{
    public string? FindInPath(string command)
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        var dirs = path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);

        var candidates = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? BuildWindowsCandidates(command)
            : new[] { command };

        foreach (var dir in dirs)
        {
            foreach (var c in candidates)
            {
                var full = Path.Combine(dir, c);
                if (File.Exists(full) && IsExecutable(full))
                    return full;
            }
        }
        return null;
    }

    private static IEnumerable<string> BuildWindowsCandidates(string cmd)
    {
        if (!string.IsNullOrEmpty(Path.GetExtension(cmd))) return new[] { cmd };
        var pathext = Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD";
        var exts = pathext.Split(';', StringSplitOptions.RemoveEmptyEntries)
                          .Select(e => e.StartsWith('.') ? e : "." + e);
        return exts.Select(ext => cmd + ext).Prepend(cmd);
    }

    private static bool IsExecutable(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return true;
        return access(path, X_OK) == 0;
    }

    private const int X_OK = 0x1;
    [DllImport("libc", SetLastError = true)]
    private static extern int access(string pathname, int mode);
}
