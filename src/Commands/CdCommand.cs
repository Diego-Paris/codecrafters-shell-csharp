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

    string targetPath = args[0];

    // For this stage, we're handling absolute paths
    if (!Directory.Exists(targetPath))
    {
      ctx.Err.WriteLine($"cd: {targetPath}: No such file or directory");
      return 1;
    }

    try
    {
      Directory.SetCurrentDirectory(targetPath);
      return 0;
    }
    catch (Exception ex)
    {
      ctx.Err.WriteLine($"cd: {targetPath}: {ex.Message}");
      return 1;
    }
  }
}
