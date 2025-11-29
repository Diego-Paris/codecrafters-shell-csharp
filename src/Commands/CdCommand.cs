using MiniShell.Abstractions;

namespace MiniShell.Commands;

/// <summary>
/// Implements the cd command for changing the shell's working directory.
/// Supports home directory expansion (~) and relative path navigation.
/// </summary>
public sealed class CdCommand : ICommand
{
    /// <summary>
    /// Gets the command name used for shell invocation.
    /// </summary>
    public string Name => "cd";

    /// <summary>
    /// Gets a human-readable description of what the command does.
    /// </summary>
    public string Description => "Change current working directory";

    /// <summary>
    /// Executes the cd command to change the process working directory.
    /// </summary>
    /// <param name="args">Command arguments containing the target directory path.</param>
    /// <param name="ctx">Shell execution context for error output.</param>
    /// <returns>Exit code (0 for success, 1 for errors).</returns>
    public int Execute(string[] args, IShellContext ctx)
    {
        if (args.Length == 0)
        {
            ctx.Err.WriteLine("cd: missing argument");
            return 1;
        }

        string targetPath = ExpandPath(args[0]);

        if (!Directory.Exists(targetPath))
        {
            ctx.Err.WriteLine($"cd: {args[0]}: No such file or directory");
            return 1;
        }

        try
        {
            Directory.SetCurrentDirectory(targetPath);
            return 0;
        }
        catch (Exception ex)
        {
            ctx.Err.WriteLine($"cd: {args[0]}: {ex.Message}");
            return 1;
        }
    }

    /// <summary>
    /// Expands special path syntax (~ for home, relative paths) to absolute paths for directory navigation.
    /// </summary>
    /// <param name="path">The path to expand.</param>
    /// <returns>Fully qualified absolute path.</returns>
    private static string ExpandPath(string path)
    {
        // Handle home directory expansion
        if (path == "~")
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        if (path.StartsWith("~/") || path.StartsWith("~\\"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, path.Substring(2));
        }

        // Handle relative paths (., .., ./, ../, etc.)
        // Path.GetFullPath handles these automatically
        return Path.GetFullPath(path);
    }
}
