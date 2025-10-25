namespace MiniShell;

public sealed class Shell
{
    private readonly CommandRouter _router;

    public Shell(CommandRouter router) => _router = router;

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
