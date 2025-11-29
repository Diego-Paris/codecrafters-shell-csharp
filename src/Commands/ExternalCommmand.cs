using MiniShell.Abstractions;
using System.Diagnostics;

namespace MiniShell.Commands;

/// <summary>
/// Launches external programs as child processes, handling platform-specific execution (Windows batch scripts, argument quoting).
/// </summary>
public class ExternalCommand : ICommand
{
    /// <summary>
    /// Gets the command name used for shell invocation.
    /// </summary>
    public string Name => "external";

    /// <summary>
    /// Gets a human-readable description of what the command does.
    /// </summary>
    public string Description => "Runs external programs";

    /// <summary>
    /// Executes an external program by resolving its path and launching it as a child process.
    /// Handles Windows batch scripts specially by invoking cmd.exe.
    /// </summary>
    /// <param name="args">External command name and its arguments.</param>
    /// <param name="ctx">Shell execution context for path resolution and I/O.</param>
    /// <returns>Exit code from the external process, or 1/127 for errors.</returns>
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
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;

            process.OutputDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    ctx.Out.WriteLine(e.Data);
            };

            process.ErrorDataReceived += (_, e) =>
            {
                if (e.Data is not null)
                    ctx.Err.WriteLine(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            return process.ExitCode;
        }
        catch (Exception e)
        {
            ctx.Err.WriteLine(e.Message);
            return 1;
        }
    }

    /// <summary>
    /// Quotes an argument if it contains whitespace or quotes, ensuring proper shell argument passing.
    /// </summary>
    /// <param name="arg">The argument to potentially quote.</param>
    /// <returns>Quoted argument if needed, original argument otherwise.</returns>
    private static string QuoteIfNeeded(string arg)
    {
        if (string.IsNullOrEmpty(arg)) return "\"\"";
        if (arg.Any(char.IsWhiteSpace) || arg.Contains('"'))
            return $"\"{arg.Replace("\"", "\\\"")}\"";
        return arg;
    }
}
