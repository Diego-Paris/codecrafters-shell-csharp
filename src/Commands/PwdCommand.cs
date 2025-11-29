using MiniShell.Abstractions;

namespace MiniShell.Commands;

/// <summary>
/// Implements the pwd command for displaying the current working directory.
/// </summary>
public sealed class PwdCommand : ICommand
{
  /// <summary>
  /// Gets the command name used for shell invocation.
  /// </summary>
  public string Name => "pwd";

  /// <summary>
  /// Gets a human-readable description of what the command does.
  /// </summary>
  public string Description => "Print current working directory";

  /// <summary>
  /// Executes the pwd command to print the current working directory.
  /// </summary>
  /// <param name="args">Arguments (unused).</param>
  /// <param name="ctx">Shell execution context for stdout.</param>
  /// <returns>Always returns 0 (success).</returns>
  public int Execute(string[] args, IShellContext ctx)
  {
    ctx.Out.WriteLine(Directory.GetCurrentDirectory());
    return 0;
  }
}
