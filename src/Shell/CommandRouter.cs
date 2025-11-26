using MiniShell.Abstractions;

namespace MiniShell;

public sealed class CommandRouter
{
    private readonly IShellContext _ctx;

    public CommandRouter(IShellContext ctx) => _ctx = ctx;

    public int Route(string line)
    {
        var parts = Tokenize(line);
        if (parts.Count == 0) return 0;

        var name = parts[0];

        if (_ctx.Commands.TryGetValue(name, out var cmd))
            return cmd.Execute(parts.Skip(1).ToArray(), _ctx);

        if (_ctx.Commands.TryGetValue("external", out var external))
            return external.Execute(parts.ToArray(), _ctx);

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
