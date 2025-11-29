using MiniShell.Abstractions;
using MiniShell.Parsing;
using MiniShell.Runtime;

namespace MiniShell;

/// <summary>
/// Routes shell commands to their handlers, managing tokenization, redirection parsing, and command dispatch.
/// </summary>
public sealed class CommandRouter
{
    private readonly IShellContext _ctx;
    private readonly ITokenizer _tokenizer;

    /// <summary>
    /// Initializes a new command router with the specified shell context and tokenizer.
    /// </summary>
    /// <param name="ctx">The shell context providing commands and I/O streams.</param>
    /// <param name="tokenizer">The tokenizer for parsing command line input.</param>
    public CommandRouter(IShellContext ctx, ITokenizer tokenizer)
    {
        _ctx = ctx;
        _tokenizer = tokenizer;
    }

    /// <summary>
    /// Parses and executes a command line, handling redirection and command dispatch.
    /// </summary>
    /// <param name="line">The raw command line input from the user.</param>
    /// <returns>Exit code from the executed command.</returns>
    public int Route(string line)
    {
        var tokens = _tokenizer.Tokenize(line);
        if (tokens.Count == 0) return 0;

        if (tokens.Contains("|"))
        {
            var segments = PipelineParser.Parse(tokens);
            var executor = new PipelineExecutor(_ctx);
            return executor.Execute(segments);
        }

        var redirectionInfo = RedirectionParser.Parse(tokens);
        if (redirectionInfo.CommandParts.Length == 0) return 0;

        var name = redirectionInfo.CommandParts[0];
        var hasRedirection = redirectionInfo.StdoutFile is not null || redirectionInfo.StderrFile is not null;

        if (hasRedirection)
        {
            var handler = new FileRedirectionHandler();
            try
            {
                var context = handler.CreateRedirectedContext(_ctx, redirectionInfo);
                return ExecuteCommand(name, redirectionInfo.CommandParts, context);
            }
            finally
            {
                handler.Cleanup();
            }
        }

        return ExecuteCommand(name, redirectionInfo.CommandParts, _ctx);
    }

    /// <summary>
    /// Executes a command by name, dispatching to built-in handlers or the external command handler.
    /// </summary>
    /// <param name="name">The command name to execute.</param>
    /// <param name="commandParts">The full command line parts including the command name.</param>
    /// <param name="context">The shell context (potentially redirected) to use for execution.</param>
    /// <returns>Exit code from the command.</returns>
    private int ExecuteCommand(string name, string[] commandParts, IShellContext context)
    {
        if (context.Commands.TryGetValue(name, out var cmd))
        {
            return cmd.Execute(commandParts.Skip(1).ToArray(), context);
        }

        if (context.Commands.TryGetValue("external", out var external))
        {
            return external.Execute(commandParts, context);
        }

        _ctx.Out.WriteLine($"{name}: command not found");
        return 127;
    }
}
