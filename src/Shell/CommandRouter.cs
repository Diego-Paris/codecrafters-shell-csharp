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

    /// <summary>
    /// Initializes a new command router with the specified shell context.
    /// </summary>
    /// <param name="ctx">The shell context providing commands and I/O streams.</param>
    public CommandRouter(IShellContext ctx) => _ctx = ctx;

    /// <summary>
    /// Parses and executes a command line, handling redirection and command dispatch.
    /// </summary>
    /// <param name="line">The raw command line input from the user.</param>
    /// <returns>Exit code from the executed command.</returns>
    public int Route(string line)
    {
        var tokens = Tokenize(line);
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

    /// <summary>
    /// Tokenizes a command line, handling quotes, escapes, and whitespace to produce shell syntax tokens.
    /// Supports single quotes (no escapes), double quotes (limited escapes), and backslash escaping.
    /// </summary>
    /// <param name="input">The raw command line to tokenize.</param>
    /// <returns>List of tokens extracted from the input.</returns>
    internal static List<string> Tokenize(string input)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(input)) return list;

        var cur = new System.Text.StringBuilder();
        bool inDoubleQuotes = false;
        bool inSingleQuotes = false;
        bool escapeNext = false;
        bool hadQuotes = false;

        for (int i = 0; i < input.Length; i++)
        {
            var ch = input[i];

            if (escapeNext)
            {
                cur.Append(ch);
                escapeNext = false;
                continue;
            }

            if (ch == '\\')
            {
                if (inSingleQuotes)
                {
                    cur.Append(ch);
                }
                else if (inDoubleQuotes)
                {
                    // Inside double quotes, backslash only escapes specific characters: \ " $ `
                    // For other characters, the backslash is literal
                    if (i + 1 < input.Length)
                    {
                        var next = input[i + 1];
                        if (next == '\\' || next == '"' || next == '$' || next == '`')
                        {
                            escapeNext = true; // Remove backslash, keep next char
                        }
                        else
                        {
                            cur.Append(ch); // Keep the backslash as literal
                        }
                    }
                    else
                    {
                        cur.Append(ch);
                    }
                }
                else
                {
                    escapeNext = true;
                }
                continue;
            }

            if (ch == '"' && !inSingleQuotes)
            {
                inDoubleQuotes = !inDoubleQuotes;
                hadQuotes = true;
                continue;
            }

            if (ch == '\'' && !inDoubleQuotes)
            {
                inSingleQuotes = !inSingleQuotes;
                hadQuotes = true;
                continue;
            }

            if (!inDoubleQuotes && !inSingleQuotes && ch == '|')
            {
                if (cur.Length > 0 || hadQuotes)
                {
                    list.Add(cur.ToString());
                    cur.Clear();
                    hadQuotes = false;
                }
                list.Add("|");
            }
            else if (!inDoubleQuotes && !inSingleQuotes && char.IsWhiteSpace(ch))
            {
                if (cur.Length > 0 || hadQuotes)
                {
                    list.Add(cur.ToString());
                    cur.Clear();
                    hadQuotes = false;
                }
            }
            else
            {
                cur.Append(ch);
            }
        }

        if (cur.Length > 0 || hadQuotes) list.Add(cur.ToString());
        return list;
    }
}
