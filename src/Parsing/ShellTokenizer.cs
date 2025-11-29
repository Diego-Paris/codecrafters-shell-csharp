using System.Text;
using MiniShell.Abstractions;

namespace MiniShell.Parsing;

public sealed class ShellTokenizer : ITokenizer
{
    public List<string> Tokenize(string input)
    {
        var list = new List<string>();
        if (string.IsNullOrWhiteSpace(input)) return list;

        var cur = new StringBuilder();
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
                    if (i + 1 < input.Length)
                    {
                        var next = input[i + 1];
                        if (next == '\\' || next == '"' || next == '$' || next == '`')
                        {
                            escapeNext = true;
                        }
                        else
                        {
                            cur.Append(ch);
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
