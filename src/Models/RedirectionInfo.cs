namespace MiniShell.Models;

public sealed record RedirectionInfo(
    string[] CommandParts,
    string? StdoutFile = null,
    string? StderrFile = null,
    bool AppendStdout = false,
    bool AppendStderr = false
);
