using MiniShell.Models;

namespace MiniShell.Parsing;

public static class RedirectionParser
{
    public static RedirectionInfo Parse(IReadOnlyList<string> tokens)
    {
        string? stdoutFile = null;
        string? stderrFile = null;
        var commandParts = new List<string>();

        for (int i = 0; i < tokens.Count; i++)
        {
            if (tokens[i] is ">" or "1>")
            {
                if (i + 1 < tokens.Count)
                {
                    stdoutFile = tokens[i + 1];
                    i++;
                }
                continue;
            }

            if (tokens[i] == "2>")
            {
                if (i + 1 < tokens.Count)
                {
                    stderrFile = tokens[i + 1];
                    i++;
                }
                continue;
            }

            commandParts.Add(tokens[i]);
        }

        return new RedirectionInfo(
            commandParts.ToArray(),
            stdoutFile,
            stderrFile
        );
    }
}
