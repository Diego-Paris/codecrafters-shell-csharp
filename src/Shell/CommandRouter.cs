using MiniShell.Abstractions;
using MiniShell.Parsing;
using MiniShell.Runtime;

namespace MiniShell;

public sealed class CommandRouter
{
    private readonly IShellContext _ctx;

    public CommandRouter(IShellContext ctx) => _ctx = ctx;

    public int Route(string line)
    {
        var tokens = Tokenize(line);
        if (tokens.Count == 0) return 0;

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

            if (!inDoubleQuotes && !inSingleQuotes && char.IsWhiteSpace(ch))
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
