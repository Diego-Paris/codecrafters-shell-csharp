using Microsoft.Extensions.DependencyInjection;
using MiniShell;
using MiniShell.Abstractions;
using MiniShell.Commands;
using MiniShell.Runtime;

var services = new ServiceCollection();

// infrastructure
services.AddSingleton<IPathResolver, PathResolver>();
services.AddSingleton<IShellContext, ShellContext>();

// commands
services.AddSingleton<ICommand, EchoCommand>();
services.AddSingleton<ICommand, ExitCommand>();
services.AddSingleton<ICommand, GreetCommand>();
services.AddSingleton<ICommand, TypeCommand>();
services.AddSingleton<ICommand, ExternalCommand>();

// shell
services.AddSingleton<CommandRouter>();
services.AddSingleton<Shell>();

var app = services.BuildServiceProvider();
var shell = app.GetRequiredService<Shell>();
return await shell.RunAsync();
