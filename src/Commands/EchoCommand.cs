using MiniShell.Abstractions;

namespace MiniShell.Commands;

public sealed class EchoCommand : ICommand
{
  public string Name => "echo";
  public string Description => "Echo arguments";
  public int Execute(string[] args, IShellContext ctx)
  {
    ctx.Out.WriteLine(string.Join(' ', args));
    return 0;
  }
}