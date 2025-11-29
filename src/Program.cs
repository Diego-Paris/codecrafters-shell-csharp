/// <summary>
/// Application entry point that bootstraps the dependency injection container and starts the shell REPL.
/// Registers all built-in commands, infrastructure services, and the shell orchestrator.
/// </summary>
using Microsoft.Extensions.DependencyInjection;
using MiniShell;
using MiniShell.Abstractions;
using MiniShell.Commands;
using MiniShell.DataStructures;
using MiniShell.Parsing;
using MiniShell.Runtime;

var services = new ServiceCollection();

// infrastructure
services.AddSingleton<IPathResolver, PathResolver>();
services.AddSingleton<ITokenizer, ShellTokenizer>();
services.AddSingleton<ShellContext>();
services.AddSingleton<IShellContext>(sp => sp.GetRequiredService<ShellContext>());
services.AddSingleton<IHistoryService, HistoryService>();

// tab completion
services.AddSingleton<ICompletionTrie, CompletionTrie>();
services.AddSingleton<ICompletionProvider, CommandCompletionProvider>();
services.AddSingleton<IInputHandler>(sp => new CustomInputHandler(
    sp.GetRequiredService<ICompletionProvider>(),
    sp.GetRequiredService<IShellContext>()
));

// commands
services.AddSingleton<ICommand, CdCommand>();
services.AddSingleton<ICommand, EchoCommand>();
services.AddSingleton<ICommand, ExitCommand>();
services.AddSingleton<ICommand, GreetCommand>();
services.AddSingleton<ICommand, HistoryCommand>();
services.AddSingleton<ICommand, PwdCommand>();
services.AddSingleton<ICommand, TypeCommand>();
services.AddSingleton<ICommand, ExternalCommand>();

// shell
services.AddSingleton<CommandRouter>();
services.AddSingleton<Shell>();

var app = services.BuildServiceProvider();
var shell = app.GetRequiredService<Shell>();
return await shell.RunAsync();
