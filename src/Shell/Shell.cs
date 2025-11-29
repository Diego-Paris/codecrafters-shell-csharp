namespace MiniShell;

/// <summary>
/// Orchestrates the shell's Read-Eval-Print Loop (REPL), managing user interaction and command execution.
/// </summary>
public sealed class Shell
{
    private readonly CommandRouter _router;

    /// <summary>
    /// Initializes a new shell instance with the specified command router.
    /// </summary>
    /// <param name="router">The router responsible for parsing and dispatching commands.</param>
    public Shell(CommandRouter router) => _router = router;

    /// <summary>
    /// Runs the interactive shell loop, reading user input and routing commands until termination.
    /// </summary>
    /// <returns>Exit code (0 for normal termination, non-zero for errors).</returns>
    public async Task<int> RunAsync()
    {
        while (true)
        {
            Console.Write("$ ");
            var line = await Console.In.ReadLineAsync();
            if (line is null) return 0;
            _router.Route(line);
        }
    }
}
