using System.Runtime.InteropServices;
using MiniShell.Abstractions;

namespace MiniShell.Runtime;

/// <summary>
/// Locates executable files in the system PATH with cross-platform support for Windows file extensions and Unix executable permissions.
/// </summary>
public sealed class PathResolver : IPathResolver
{
    /// <summary>
    /// Searches for an executable in the system PATH.
    /// </summary>
    /// <param name="command">The command name to locate.</param>
    /// <returns>The full path to the executable if found; otherwise, null.</returns>
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

    /// <summary>
    /// Generates file name variants for Windows, appending PATHEXT extensions to support implicit executable extensions.
    /// </summary>
    /// <param name="cmd">The base command name.</param>
    /// <returns>Sequence of file name candidates to try, with extensions from PATHEXT.</returns>
    private static IEnumerable<string> BuildWindowsCandidates(string cmd)
    {
        if (!string.IsNullOrEmpty(Path.GetExtension(cmd))) return new[] { cmd };
        var pathext = Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD";
        var exts = pathext.Split(';', StringSplitOptions.RemoveEmptyEntries)
                          .Select(e => e.StartsWith('.') ? e : "." + e);
        return exts.Select(ext => cmd + ext).Prepend(cmd);
    }

    /// <summary>
    /// Checks if a file is executable using platform-specific logic (Windows: always true, Unix: checks execute bit via libc).
    /// </summary>
    /// <param name="path">The file path to check.</param>
    /// <returns>True if the file is executable on this platform.</returns>
    private static bool IsExecutable(string path)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return true;
        return access(path, X_OK) == 0;
    }

    /// <summary>
    /// Unix execute permission bit for access(2) system call.
    /// </summary>
    private const int X_OK = 0x1;

    /// <summary>
    /// P/Invoke to Unix libc access(2) for checking file execute permissions.
    /// </summary>
    [DllImport("libc", SetLastError = true)]
    private static extern int access(string pathname, int mode);
}
