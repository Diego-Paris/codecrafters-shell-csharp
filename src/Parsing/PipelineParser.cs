namespace MiniShell.Parsing;

public static class PipelineParser
{
    public static List<List<string>> Parse(List<string> tokens)
    {
        var segments = new List<List<string>>();
        var current = new List<string>();

        foreach (var token in tokens)
        {
            if (token == "|")
            {
                if (current.Count > 0)
                {
                    segments.Add(current);
                    current = new List<string>();
                }
            }
            else
            {
                current.Add(token);
            }
        }

        if (current.Count > 0)
        {
            segments.Add(current);
        }

        return segments;
    }
}
