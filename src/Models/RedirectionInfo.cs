namespace MiniShell.Models;

/// <summary>
/// Holds parsed redirection configuration extracted from shell command tokens.
/// Supports stdout (>, >>), stderr (2>, 2>>), and mixed redirections.
/// </summary>
public sealed record RedirectionInfo(
    /// <summary>
    /// Gets the command tokens after redirection operators have been removed.
    /// </summary>
    string[] CommandParts,

    /// <summary>
    /// Gets the file path for stdout redirection, or null if stdout is not redirected.
    /// </summary>
    string? StdoutFile = null,

    /// <summary>
    /// Gets the file path for stderr redirection, or null if stderr is not redirected.
    /// </summary>
    string? StderrFile = null,

    /// <summary>
    /// Gets whether stdout should be appended (>>) rather than overwritten (>).
    /// </summary>
    bool AppendStdout = false,

    /// <summary>
    /// Gets whether stderr should be appended (2>>) rather than overwritten (2>).
    /// </summary>
    bool AppendStderr = false
);
