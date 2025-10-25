class Program
{
    static int Main()
    {
        var commands = new Dictionary<string, Action<string[]>>();

        commands["greet"] = args => Console.WriteLine("Hello, World!");

        commands["exit"] = args =>
        {
            var exitCode = args.Length > 0 && int.TryParse(args[0], out var code) ? code : 0;
            Environment.Exit(exitCode);
        };

        commands["echo"] = words => Console.WriteLine(string.Join(' ', words));

        commands["type"] = args =>
        {
            var command = args[0];
            var envPath = Environment.GetEnvironmentVariable("PATH");

            var directories = envPath?.Split(Path.PathSeparator) ?? Array.Empty<string>();

            foreach (var dir in directories)
            {
                var fullPath = Path.Combine(dir, $"{command}");
                if (File.Exists(fullPath))
                {
                    Console.WriteLine($"{command} is {fullPath}");
                    return;
                }
            }

            Console.WriteLine(commands.ContainsKey(command) && args.Length == 1 ? $"{command} is a shell builtin" : $"{command}: not found");


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
