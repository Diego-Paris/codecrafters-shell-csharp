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
                    // Inside double quotes:
                    // - \ " $ ` space: backslash escapes them (remove backslash, keep char)
                    // - single quote: keep backslash, skip the quote
                    // - Other chars (like n, 5): keep both backslash and character
                    if (i + 1 < input.Length)
                    {
                        var next = input[i + 1];
                        if (next == '\\' || next == '"' || next == '$' || next == '`' || next == ' ')
                        {
                            escapeNext = true;
                        }
                        else if (next == '\'')
                        {
                            cur.Append(ch); // Keep backslash
                            i++; // Skip the quote
                        }
                        else
                        {
                            cur.Append(ch); // Keep backslash, let next iteration handle the character
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
