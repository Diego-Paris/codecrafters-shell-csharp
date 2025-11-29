using MiniShell.Abstractions;
using MiniShell.Runtime;
using System.Diagnostics;
using System.Text;

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

        var buffers = new List<StringWriter?>();
        var processes = new List<Process>();
        var tasks = new List<Task<int>>();

        try
        {
            for (int i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                if (segment.Count == 0) continue;

                var commandName = segment[0];
                var args = segment.Skip(1).ToArray();

                var isBuiltin = _ctx.Commands.ContainsKey(commandName);

                if (isBuiltin)
                {
                    TextReader inputReader;
                    if (i > 0 && buffers[i - 1] != null)
                    {
                        var previousOutput = buffers[i - 1]!.ToString();
                        inputReader = new StringReader(previousOutput);
                    }
                    else
                    {
                        inputReader = _ctx.In;
                    }

                    TextWriter outputWriter;
                    StringWriter? buffer = null;

                    if (i < segments.Count - 1)
                    {
                        buffer = new StringWriter();
                        outputWriter = buffer;
                        buffers.Add(buffer);
                    }
                    else
                    {
                        outputWriter = _ctx.Out;
                    }

                    var errorWriter = _ctx.Err;

                    var builtinContext = new ShellContext(
                        _ctx.Commands.Values,
                        _ctx.PathResolver,
                        inputReader,
                        outputWriter,
                        errorWriter);

                    var localInput = inputReader;
                    var localOutput = outputWriter;
                    var task = Task.Run(() =>
                    {
                        try
                        {
                            var cmd = _ctx.Commands[commandName];
                            var exitCode = cmd.Execute(args, builtinContext);
                            localOutput.Flush();
                            return exitCode;
                        }
                        catch
                        {
                            return 1;
                        }
                        finally
                        {
                            if (localInput != _ctx.In)
                            {
                                localInput.Dispose();
                            }
                        }
                    });

                    tasks.Add(task);
                }
                else
                {
                    var resolvedPath = _ctx.PathResolver.FindInPath(commandName);
                    if (resolvedPath == null)
                    {
                        _ctx.Err.WriteLine($"{commandName}: command not found");
                        CleanupAll(processes, buffers);
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
                        if (buffers.Count > 0 && buffers[i - 1] != null)
                        {
                            var previousOutput = buffers[i - 1]!.ToString();
                            WriteToProcessInputAsync(previousOutput, process.StandardInput);
                        }
                        else if (processes.Count > 1)
                        {
                            var previousProcess = processes[processes.Count - 2];
                            CopyStreamAsync(previousProcess.StandardOutput, process.StandardInput);
                        }
                    }

                    if (i < segments.Count - 1)
                    {
                        buffers.Add(null);
                    }
                }
            }

            Task.WaitAll(tasks.ToArray());

            foreach (var p in processes)
            {
                if (!p.HasExited)
                {
                    p.WaitForExit();
                }
            }

            if (tasks.Count > 0)
            {
                return tasks[tasks.Count - 1].Result;
            }

            if (processes.Count > 0)
            {
                return processes[processes.Count - 1].ExitCode;
            }

            return 0;
        }
        catch (Exception ex)
        {
            _ctx.Err.WriteLine($"Pipeline error: {ex.Message}");
            CleanupAll(processes, buffers);
            return 1;
        }
        finally
        {
            foreach (var p in processes)
            {
                p.Dispose();
            }

            foreach (var buffer in buffers)
            {
                buffer?.Dispose();
            }
        }
    }

    private static async void WriteToProcessInputAsync(string content, StreamWriter destination)
    {
        try
        {
            await destination.WriteAsync(content);
            destination.Close();
        }
        catch
        {
            // Ignore pipe errors
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

    private static void CleanupAll(List<Process> processes, List<StringWriter?> buffers)
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

        foreach (var buffer in buffers)
        {
            try
            {
                buffer?.Dispose();
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
