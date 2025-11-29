using System.Runtime.InteropServices;
using MiniShell.Abstractions;
using MiniShell.DataStructures;

namespace MiniShell.Runtime;

/// <summary>
/// Provides command completions from both built-in commands and PATH executables using a Trie for efficient prefix matching.
/// </summary>
public sealed class CommandCompletionProvider : ICompletionProvider
{
    private readonly IShellContext _context;
    private readonly ICompletionTrie _trie;
    private bool _initialized;

    public CommandCompletionProvider(IShellContext context, ICompletionTrie trie)
    {
        _context = context;
        _trie = trie;
    }

    public IEnumerable<string> GetCompletions(string prefix)
    {
        EnsureInitialized();
        return _trie.GetPrefixMatches(prefix);
    }

    private void EnsureInitialized()
    {
        if (_initialized) return;

        foreach (var cmdName in _context.Commands.Keys)
        {
            _trie.Add(cmdName);
        }

        var pathDirs = GetPathDirectories();
        foreach (var exe in GetExecutablesFromPath(pathDirs))
        {
            _trie.Add(exe);
        }

        _initialized = true;
    }

    private static string[] GetPathDirectories()
    {
        var path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        return path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
    }

    private static IEnumerable<string> GetExecutablesFromPath(string[] pathDirs)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        var results = new List<string>();

        foreach (var dir in pathDirs)
        {
            if (!Directory.Exists(dir)) continue;

            try
            {
                foreach (var filePath in Directory.EnumerateFiles(dir))
                {
                    if (!IsExecutable(filePath, isWindows)) continue;

                    var fileName = Path.GetFileNameWithoutExtension(filePath);
                    if (string.IsNullOrEmpty(fileName)) continue;

                    if (seen.Add(fileName))
                    {
                        results.Add(fileName);
                    }
                }
            }
            catch
            {
                // Skip directories we can't access
            }
        }

        return results;
    }

    private static bool IsExecutable(string filePath, bool isWindows)
    {
        if (!isWindows)
        {
            return access(filePath, X_OK) == 0;
        }

        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        var pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? ".COM;.EXE;.BAT;.CMD";
        var executableExts = pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(e => e.ToLowerInvariant());

        return executableExts.Contains(ext);
    }

    private const int X_OK = 0x1;

    [DllImport("libc", SetLastError = true)]
    private static extern int access(string pathname, int mode);
}
