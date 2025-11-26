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

    private static List<string> Tokenize(string input)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(input)) return list;

        var cur = new System.Text.StringBuilder();
        bool inDoubleQuotes = false;
        bool inSingleQuotes = false;

        foreach (var ch in input)
        {
            if (ch == '"' && !inSingleQuotes)
            {
                inDoubleQuotes = !inDoubleQuotes;
                continue;
            }

            if (ch == '\'' && !inDoubleQuotes)
            {
                inSingleQuotes = !inSingleQuotes;
                continue;
            }

            if (!inDoubleQuotes && !inSingleQuotes && char.IsWhiteSpace(ch))
            {
                if (cur.Length > 0)
                {
                    list.Add(cur.ToString());
                    cur.Clear();
                }
            }
            else
            {
                cur.Append(ch);
            }
        }

        if (cur.Length > 0) list.Add(cur.ToString());
        return list;
    }
}
