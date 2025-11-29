using MiniShell.Abstractions;
using System.Diagnostics;

namespace MiniShell;

public class PipelineExecutor
{
    private readonly IShellContext _ctx;

    public PipelineExecutor(IShellContext ctx)
    {
        _ctx = ctx;
    }

    public int Execute(List<List<string>> segments)
    {
        if (segments.Count == 0) return 0;
        if (segments.Count == 1) return 0;

        var processes = new List<Process>();

        try
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Count == 0) continue;

                var commandName = segment[0];
                var args = segment.Skip(1).ToArray();

                var resolvedPath = _ctx.PathResolver.FindInPath(commandName);
                if (resolvedPath == null)
                {
                    _ctx.Err.WriteLine($"{commandName}: command not found");
                    CleanupProcesses(processes);
                    return 127;
                }

                var process = new Process();
                process.StartInfo.FileName = resolvedPath;
                process.StartInfo.Arguments = string.Join(' ', args.Select(QuoteIfNeeded));
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                if (i == 0)
                {
                    process.StartInfo.RedirectStandardInput = false;
                }
                else
                {
                    process.StartInfo.RedirectStandardInput = true;
                }

                if (i == segments.Count - 1)
                {
                    process.StartInfo.RedirectStandardOutput = false;
                    process.StartInfo.RedirectStandardError = false;
                }
                else
                {
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                }

                process.Start();
                processes.Add(process);

                if (i > 0)
                {
                    var previousProcess = processes[i - 1];
                    CopyStreamAsync(previousProcess.StandardOutput, process.StandardInput);
                }
            }

            var lastProcess = processes[processes.Count - 1];
            lastProcess.WaitForExit();

            foreach (var p in processes)
            {
                if (!p.HasExited)
                {
                    p.WaitForExit();
                }
            }

            return lastProcess.ExitCode;
        }
        catch (Exception ex)
        {
            _ctx.Err.WriteLine($"Pipeline error: {ex.Message}");
            CleanupProcesses(processes);
            return 1;
        }
        finally
        {
            foreach (var p in processes)
            {
                p.Dispose();
            }
        }
    }

    private static async void CopyStreamAsync(StreamReader source, StreamWriter destination)
    {
        try
        {
            await source.BaseStream.CopyToAsync(destination.BaseStream);
            destination.Close();
        }
        catch
        {
            // Ignore pipe errors
        }
    }

    private static void CleanupProcesses(List<Process> processes)
    {
        foreach (var p in processes)
        {
            try
            {
                if (!p.HasExited)
                {
                    p.Kill();
                }
            }
            catch
            {
                // Ignore
            }
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
