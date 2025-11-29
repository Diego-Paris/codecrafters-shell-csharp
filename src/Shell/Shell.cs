using MiniShell.Abstractions;
using MiniShell.Runtime;

namespace MiniShell;

/// <summary>
/// Orchestrates the shell's Read-Eval-Print Loop (REPL), managing user interaction and command execution.
/// </summary>
public sealed class Shell
{
    private readonly CommandRouter _router;
    private readonly IInputHandler _inputHandler;
    private readonly ShellContext _context;
    private readonly IHistoryService _historyService;

    /// <summary>
    /// Initializes a new shell instance with the specified command router and input handler.
    /// </summary>
    /// <param name="router">The router responsible for parsing and dispatching commands.</param>
    /// <param name="inputHandler">The input handler for reading user input with tab completion support.</param>
    /// <param name="context">The shell context for tracking history.</param>
    /// <param name="historyService">The history service for loading and saving command history.</param>
    public Shell(CommandRouter router, IInputHandler inputHandler, ShellContext context, IHistoryService historyService)
    {
        _router = router;
        _inputHandler = inputHandler;
        _context = context;
        _historyService = historyService;
    }

    /// <summary>
    /// Runs the interactive shell loop, reading user input and routing commands until termination.
    /// </summary>
    /// <returns>Exit code (0 for normal termination, non-zero for errors).</returns>
    public Task<int> RunAsync()
    {
        _historyService.LoadFromFile();

        while (true)
        {
            var line = _inputHandler.ReadInput("$ ");
            if (line is null) return Task.FromResult(0);
            if (!string.IsNullOrWhiteSpace(line))
            {
                _context.AddToHistory(line);
            }
            _router.Route(line);
        }
    }
}
