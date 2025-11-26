using MiniShell.Abstractions;

namespace MiniShell.Commands;

public sealed class PwdCommand : ICommand
{
  public string Name => "pwd";
  public string Description => "Print current working directory";
  public int Execute(string[] args, IShellContext ctx)
  {
    ctx.Out.WriteLine(Directory.GetCurrentDirectory());
    return 0;
  }
}
