class Program
{
    static int Main()
    {
        var commands = new Dictionary<string, Action<string[]>>
        {
            { "greet", args => Console.WriteLine("Hello, World!") },
            { "exit", args => {
                var exitCode = args.Length > 0 && int.TryParse(args[0], out var code) ? code : 0;
                Environment.Exit(exitCode);
            }}
        };

        while (true)
        {
            Console.Write("$ ");
            var input = Console.ReadLine();
            var parts = input?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            
            if (parts.Length > 0 && commands.TryGetValue(parts[0], out var command))
            {
                command(parts.Skip(1).ToArray());
            }
            else
            {
                Console.WriteLine($"{input}: command not found");
            }
        }
    }
}
