using MiniShell.Abstractions;
using System.Diagnostics;

namespace MiniShell.Commands;

public class ExternalCommand : ICommand
{
    public string Name => "external";
    public string Description => "Runs external programs";

    public int Execute(string[] args, IShellContext ctx)
    {
        if (args.Length == 0)
        {
            ctx.Out.WriteLine("external: missing operand");
            return 2;
        }

        var name = args[0];
        var rest = args.Skip(1).ToArray();

        string? target =
            name.IndexOfAny(new[] { '\\', '/', ':' }) >= 0 ? name
            : ctx.PathResolver.FindInPath(name);

        if (target is null)
        {
            ctx.Out.WriteLine($"{name}: not found");
            return 127;
        }

        // Detect .bat/.cmd scripts and run them via cmd.exe /c,
        // otherwise launch executables directly
        var ext = Path.GetExtension(target)
            .ToLowerInvariant();
        var isCmdScript = ext is ".cmd" or ".bat";
        var fileName = isCmdScript
            ? Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe"
            : target;

        var joinedArgs = string.Join(' ', rest.Select(QuoteIfNeeded));
        var arguments = isCmdScript
            ? $"/c \"{target}\"{(joinedArgs.Length > 0 ? " " + joinedArgs : "")}"
            : joinedArgs;

        if (target is null || (!Path.IsPathRooted(target) && ctx.PathResolver.FindInPath(name) is null))
        {
            ctx.Out.WriteLine($"{name}: not found");
            return 127;
        }

        try
        {
            using var process = new Process();
            process.StartInfo.FileName = fileName;
            process.StartInfo.Arguments = arguments;

            // Share the current console (interactive works)
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.RedirectStandardError = false;
            process.StartInfo.CreateNoWindow = false;

            process.Start();
            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception e)
        {
            ctx.Err.WriteLine(e.Message);
            return 1;
        }
    }

    private static string QuoteIfNeeded(string s)
    {
        if (string.IsNullOrEmpty(s)) return "\"\"";
        if (s.Any(char.IsWhiteSpace) || s.Contains('"'))
            return $"\"{s.Replace("\"", "\\\"")}\"";
        return s;
    }
}
