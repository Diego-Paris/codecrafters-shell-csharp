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

        string? outputFile = null;
        var commandParts = new List<string>();

        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] is ">" or "1>")
            {
                if (i + 1 < parts.Count)
                {
                    outputFile = parts[i + 1];
                    i++;
                }
                continue;
            }
            commandParts.Add(parts[i]);
        }

        if (commandParts.Count == 0) return 0;

        var name = commandParts[0];
        int exitCode;

        if (outputFile is not null)
        {
            var directory = Path.GetDirectoryName(outputFile);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fileStream = new FileStream(outputFile, FileMode.Create, FileAccess.Write, FileShare.Read);
            using var writer = new StreamWriter(fileStream);
            var originalOut = _ctx.Out;

            var contextWithRedirection = new RedirectedShellContext(_ctx, writer);

            if (contextWithRedirection.Commands.TryGetValue(name, out var cmd))
            {
                exitCode = cmd.Execute(commandParts.Skip(1).ToArray(), contextWithRedirection);
            }
            else if (contextWithRedirection.Commands.TryGetValue("external", out var external))
            {
                exitCode = external.Execute(commandParts.ToArray(), contextWithRedirection);
            }
            else
            {
                _ctx.Out.WriteLine($"{name}: command not found");
                return 127;
            }
        }
        else
        {
            if (_ctx.Commands.TryGetValue(name, out var cmd))
            {
                exitCode = cmd.Execute(commandParts.Skip(1).ToArray(), _ctx);
            }
            else if (_ctx.Commands.TryGetValue("external", out var external))
            {
                exitCode = external.Execute(commandParts.ToArray(), _ctx);
            }
            else
            {
                _ctx.Out.WriteLine($"{name}: command not found");
                return 127;
            }
        }

        return exitCode;
    }

    private sealed class RedirectedShellContext : IShellContext
    {
        private readonly IShellContext _inner;
        private readonly TextWriter _redirectedOut;

        public RedirectedShellContext(IShellContext inner, TextWriter redirectedOut)
        {
            _inner = inner;
            _redirectedOut = redirectedOut;
        }

        public IReadOnlyDictionary<string, ICommand> Commands => _inner.Commands;
        public TextReader In => _inner.In;
        public TextWriter Out => _redirectedOut;
        public TextWriter Err => _inner.Err;
        public IPathResolver PathResolver => _inner.PathResolver;
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
