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

        var userInput = args[0];
        var additionalArgs = args.Skip(1).ToArray();

        var resolvedPath = userInput.IndexOfAny(new[] { '\\', '/', ':' }) >= 0
            ? userInput
            : ctx.PathResolver.FindInPath(userInput);

        if (resolvedPath is null)
        {
            ctx.Out.WriteLine($"{userInput}: not found");
            return 127;
        }

        var isWindows = OperatingSystem.IsWindows();
        var extension = isWindows ? Path.GetExtension(resolvedPath).ToLowerInvariant() : string.Empty;
        var isBatchScript = isWindows && (extension is ".cmd" or ".bat");

        var executable = isBatchScript
            ? Environment.GetEnvironmentVariable("ComSpec") ?? "cmd.exe"
            : (userInput.Contains(Path.DirectorySeparatorChar) ? resolvedPath : userInput);

        var joinedArgs = string.Join(' ', additionalArgs.Select(QuoteIfNeeded));
        var finalArgs = isBatchScript
            ? $"/c \"{resolvedPath}\"{(joinedArgs.Length > 0 ? " " + joinedArgs : "")}"
            : joinedArgs;

        try
        {
            using var process = new Process();
            process.StartInfo.FileName = executable;
            process.StartInfo.Arguments = finalArgs;
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

    private static string QuoteIfNeeded(string arg)
    {
        if (string.IsNullOrEmpty(arg)) return "\"\"";
        if (arg.Any(char.IsWhiteSpace) || arg.Contains('"'))
            return $"\"{arg.Replace("\"", "\\\"")}\"";
        return arg;
    }
}
