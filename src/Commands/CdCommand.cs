using MiniShell.Abstractions;

namespace MiniShell.Commands;

public sealed class CdCommand : ICommand
{
    public string Name => "cd";
    public string Description => "Change current working directory";
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
