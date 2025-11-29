# Building a Shell in C#: Architecture Decisions and Lessons Learned

*A deep dive into implementing a POSIX-compliant shell with dependency injection, trie-based tab completion, and cross-platform PATH resolution*

---

## Introduction

When you type a command in bash or zsh, a lot happens before that command runs. Building a shell from scratch for the CodeCrafters challenge forced me to think about problems I'd taken for granted: How does tab completion work? Why does pressing the up arrow show previous commands? How does the shell know where to find executables?

This post isn't about what the shell does, it's about **why I made specific implementation choices** and what I learned along the way.

**Tech stack:** C# 9.0, .NET Core, Microsoft.Extensions.DependencyInjection

**Source code:** [github.com/Diego-Paris/codecrafters-shell-csharp](https://github.com/Diego-Paris/codecrafters-shell-csharp)

---

## Why Dependency Injection for a Shell?

### The Decision

Most shell implementations use global state or singletons. I used Microsoft's dependency injection container and constructor injection throughout.

### Why?

**Testability was the driver.** I wanted to write comprehensive tests without mocking the filesystem or spawning real processes. With DI:

- Every `ICommand` can be tested in isolation
- `PathResolver` can be swapped for a test double
- `CustomInputHandler` gets an `IConsole` abstraction instead of using `Console` directly
- `CommandRouter` doesn't know about `Console.Out`, it uses `IShellContext.Out`

Here's what the DI setup looks like in `Program.cs`:

```csharp
services.AddSingleton<IPathResolver, PathResolver>();
services.AddSingleton<ITokenizer, ShellTokenizer>();
services.AddSingleton<IShellContext, ShellContext>();
services.AddSingleton<IHistoryService, HistoryService>();
services.AddSingleton<ICompletionTrie, CompletionTrie>();
services.AddSingleton<ICompletionProvider, CommandCompletionProvider>();
services.AddSingleton<IInputHandler, CustomInputHandler>();

// All commands implement ICommand
services.AddSingleton<ICommand, CdCommand>();
services.AddSingleton<ICommand, EchoCommand>();
// ... more commands

services.AddSingleton<CommandRouter>();
services.AddSingleton<Shell>();
```

### The Trade-off

**Complexity vs testability.** The DI setup adds cognitive overhead, you need to understand the dependency graph. But it paid off during development. When I refactored `CustomInputHandler` to support history navigation, I didn't touch a single command implementation. When I added redirection, I just wrapped `IShellContext`, no global state to chase down.

**The catch:** You need to be careful about circular dependencies. I initially had `ExitCommand` depend on `IHistoryService`, which depended on `IShellContext`, which needed all `ICommand` instances, creating a cycle. The fix was simple: `ExitCommand` now calls `ctx.SaveHistoryToFile()` directly instead of injecting the service.

**Would I do it again?** Yes. The testing velocity was worth the upfront cost.

---

## The Tab Completion Problem

### The Naive Approach

Tab completion seems simple: when the user presses tab, find all commands that start with what they've typed. The naive implementation:

```csharp
public IEnumerable<string> GetCompletions(string prefix)
{
    return _allCommands.Where(cmd => cmd.StartsWith(prefix));
}
```

This works. It's also O(n) where n is the number of commands. On my system, there are 2000+ executables in PATH. Every tab press would iterate through all of them.

### The Trie Solution

I implemented a **Trie (prefix tree)** for O(m) lookups where m is the prefix length.

**How it works:**
1. At startup, build a trie containing all built-in commands + executables in PATH
2. On tab press, traverse the trie using the prefix characters
3. Collect all words under the final prefix node

```csharp
public IEnumerable<string> GetPrefixMatches(string prefix)
{
    var current = _root;
    foreach (var ch in prefix)
    {
        if (!current.Children.TryGetValue(ch, out var next))
            return Enumerable.Empty<string>();
        current = next;
    }
    return CollectAllWords(current);
}
```

**[View the implementation](https://github.com/Diego-Paris/codecrafters-shell-csharp/blob/master/src/DataStructures/CompletionTrie.cs)**

### The Double-Tab Behavior

Real shells have subtle UX polish. If you type `ec` and press tab:
- One match: auto-complete and add space: `echo `
- Multiple matches: complete common prefix and beep
- Second tab: show all matches

Implementing this required state tracking:

```csharp
private sealed class InputState
{
    public string LastPrefix { get; set; } = string.Empty;
    public bool LastTabWasMultiMatch { get; set; }
}
```

If `LastTabWasMultiMatch` is true and the prefix hasn't changed, the second tab displays all matches.

**[View the implementation](https://github.com/Diego-Paris/codecrafters-shell-csharp/blob/master/src/Runtime/CustomInputHandler.cs#L152-L188)**

### What I Learned

**Small UX details have complex implementations.** The beep sound, the double-tab behavior, the space after single completion, users don't think about these, but they're load-bearing for the experience.

---

## Custom Input Handling: Beyond Console.ReadLine()

### Why Not Use Console.ReadLine()?

`Console.ReadLine()` gives you a complete line after the user presses Enter. It doesn't let you:
- Handle tab completion
- Navigate history with arrow keys
- Show partial completions
- Implement backspace behavior

To build a real shell, I needed to read **individual keystrokes** and manage my own input buffer.

### The Implementation

`CustomInputHandler` reads keys one at a time and maintains state:

```csharp
public string? ReadInput(string prompt)
{
    Console.Write(prompt);
    var state = new InputState { HistoryIndex = _context.CommandHistory.Count };

    while (true)
    {
        var key = Console.ReadKey(intercept: true);

        if (key.Key == ConsoleKey.Enter)
        {
            Console.WriteLine();
            return state.Buffer.ToString();
        }

        if (key.Key == ConsoleKey.Tab)
            HandleTab(prompt, state);
        else if (key.Key == ConsoleKey.UpArrow)
            HandleUpArrow(prompt, state);
        // ... more key handlers
    }
}
```

**[View the implementation](https://github.com/Diego-Paris/codecrafters-shell-csharp/blob/master/src/Runtime/CustomInputHandler.cs#L38-L74)**

### The Arrow Key Challenge

When you press up arrow, the shell should:
1. Save your current input (if you're at the end of history)
2. Move back one position in history
3. Clear the current line
4. Display the history entry

Pressing down at the end of history should **restore your original input**, not show a blank line. This required careful state management:

```csharp
private void HandleUpArrow(string prompt, InputState state)
{
    if (state.HistoryIndex > 0)
    {
        // Save current input before navigating
        if (state.HistoryIndex == _context.CommandHistory.Count)
            state.CurrentInput = state.Buffer.ToString();

        state.HistoryIndex--;
        ClearCurrentLine(prompt, state.Buffer.Length);
        state.Buffer.Clear();
        state.Buffer.Append(_context.CommandHistory[state.HistoryIndex]);
        Console.Write(prompt + state.Buffer.ToString());
    }
}
```

**[View the implementation](https://github.com/Diego-Paris/codecrafters-shell-csharp/blob/master/src/Runtime/CustomInputHandler.cs#L76-L94)**

### What I Learned

**Input handling is way harder than command execution.** Parsing and running commands is straightforward. Building a good REPL with tab completion, history, and proper cursor management is surprisingly complex.

**State machines everywhere.** The input handler is essentially a state machine responding to key events. Understanding this early would have simplified the design.

---

## Cross-Platform PATH Resolution

### The Problem

Windows and Unix have fundamentally different ideas about executables:

**Unix:**
- A file is executable if it has execute permissions (`chmod +x`)
- You check permissions with `File.GetUnixFileMode()`

**Windows:**
- Executable determined by file extension
- The `PATHEXT` environment variable lists valid extensions: `.exe;.cmd;.bat;.com`
- `cat.exe` is executable, `cat` is not (unless it's a `cat.bat` or `cat.cmd`)

### The Solution

`PathResolver` implements platform-specific logic:

```csharp
public string? FindInPath(string command)
{
    var pathDirs = Environment.GetEnvironmentVariable("PATH")?.Split(Path.PathSeparator);
    if (pathDirs is null) return null;

    var candidates = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
        ? BuildWindowsCandidates(command)  // cat becomes [cat.exe, cat.cmd, cat.bat, cat.com]
        : [command];                        // cat becomes [cat]

    foreach (var dir in pathDirs)
    {
        foreach (var candidate in candidates)
        {
            var fullPath = Path.Combine(dir, candidate);
            if (IsExecutable(fullPath))
                return fullPath;
        }
    }
    return null;
}
```

**[View the implementation](https://github.com/Diego-Paris/codecrafters-shell-csharp/blob/master/src/Runtime/PathResolver.cs)**

### Windows PATHEXT Handling

```csharp
private IEnumerable<string> BuildWindowsCandidates(string command)
{
    if (Path.HasExtension(command))
        return [command];  // Already has extension

    var pathExt = Environment.GetEnvironmentVariable("PATHEXT") ?? ".exe;.cmd;.bat;.com";
    var extensions = pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries);

    return extensions.Select(ext => command + ext);
}
```

### What I Learned

**Cross-platform code is messier than you think.** Even something as simple as "find this command" requires platform detection and different logic paths.

**Environment variables matter.** `PATHEXT` is easy to forget about, but it's critical for Windows shell behavior.

---

## I/O Redirection with Proper Resource Management

### The Challenge

When a user runs `echo "test" > output.txt`, the shell needs to:
1. Parse the redirection operator (`>`)
2. Open a file for writing
3. Redirect stdout to that file
4. Execute the command
5. **Clean up the file handle**

Missing step 5 leads to leaked file handles and locked files.

### The Design

I created a `FileRedirectionHandler` that implements `IDisposable` pattern:

```csharp
public sealed class FileRedirectionHandler : IRedirectionHandler
{
    private readonly List<IDisposable> _disposables = [];

    public IShellContext CreateRedirectedContext(IShellContext context, RedirectionInfo info)
    {
        var outWriter = CreateWriter(info.StdoutFile, info.AppendStdout) ?? context.Out;
        var errWriter = CreateWriter(info.StderrFile, info.AppendStderr) ?? context.Err;
        return new RedirectedShellContext(context, outWriter, errWriter);
    }

    public void Cleanup()
    {
        foreach (var disposable in _disposables)
            disposable.Dispose();
        _disposables.Clear();
    }
}
```

**[View the implementation](https://github.com/Diego-Paris/codecrafters-shell-csharp/blob/master/src/Runtime/FileRedirectionHandler.cs)**

### Automatic Directory Creation

One detail: if you run `echo "test" > /tmp/foo/bar/output.txt` and `/tmp/foo/bar` doesn't exist, bash creates it automatically. My shell does too:

```csharp
private StreamWriter? CreateWriter(string? filePath, bool append)
{
    if (filePath is null) return null;

    var directory = Path.GetDirectoryName(filePath);
    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        Directory.CreateDirectory(directory);

    var mode = append ? FileMode.Append : FileMode.Create;
    var stream = new FileStream(filePath, mode, FileAccess.Write, FileShare.Read);
    var writer = new StreamWriter(stream) { AutoFlush = true };

    _disposables.Add(writer);
    _disposables.Add(stream);

    return writer;
}
```

### What I Learned

**Resource cleanup is easy to forget.** Without explicit `Cleanup()` calls, file handles leak silently. Implementing `IDisposable` makes the cleanup contract explicit.

**UX details matter.** Auto-creating directories is a small touch that makes the shell feel polished.

---

## Persistent Command History

### The Feature

When you exit bash and restart it, your command history is still there. This is powered by `HISTFILE` (usually `~/.bash_history`).

I implemented the same behavior:

1. On startup: `HistoryService.LoadFromFile()` reads `$HISTFILE`
2. During REPL: commands are added to in-memory list
3. On exit: `HistoryService.SaveToFile()` writes everything back
4. `history -a`: appends only new commands (incremental save)

### The Implementation

```csharp
public sealed class HistoryService : IHistoryService
{
    private readonly IShellContext _context;

    public void LoadFromFile()
    {
        var histFile = Environment.GetEnvironmentVariable("HISTFILE");
        if (string.IsNullOrEmpty(histFile) || !File.Exists(histFile))
            return;

        var lines = File.ReadAllLines(histFile);
        foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
            _context.AddToHistory(line);
    }

    public void SaveToFile()
    {
        var histFile = Environment.GetEnvironmentVariable("HISTFILE");
        if (string.IsNullOrEmpty(histFile))
            return;

        EnsureDirectoryExists(histFile);
        File.WriteAllLines(histFile, _context.CommandHistory);
    }

    public void AppendNewCommandsToFile()
    {
        var histFile = Environment.GetEnvironmentVariable("HISTFILE");
        if (string.IsNullOrEmpty(histFile))
            return;

        var newCommands = _context.GetCommandsSinceLastAppend();
        if (newCommands.Count > 0)
        {
            EnsureDirectoryExists(histFile);
            File.AppendAllLines(histFile, newCommands);
        }

        _context.MarkLastAppendPosition();
    }
}
```

**[View the implementation](https://github.com/Diego-Paris/codecrafters-shell-csharp/blob/master/src/Runtime/HistoryService.cs)**

### Incremental Append Tracking

To implement `history -a` (append only new commands), `ShellContext` tracks the last append position:

```csharp
public IReadOnlyList<string> GetCommandsSinceLastAppend()
{
    return _history.Skip(_lastAppendIndex).ToList();
}

public void MarkLastAppendPosition()
{
    _lastAppendIndex = _history.Count;
}
```

### What I Learned

**Bash behavior is the spec.** Users expect shells to work like bash. Implementing `HISTFILE` wasn't in the CodeCrafters requirements, but it makes the shell feel complete.

**Incremental operations need careful state tracking.** Append-only requires knowing what's new vs what's already persisted.

---

## What I'd Do Differently

### 1. Watch for Circular Dependencies in DI

I didn't notice the circular dependency (Shell depends on IShellContext, which depends on ICommand, which depends on IHistoryService, which depends back on IShellContext) until the app started hanging during startup. Drawing the dependency graph upfront would have caught this immediately.

**Lesson:** For DI-heavy applications, diagram the dependency graph before writing code. Watch for cycles.

### 2. Abstract Console Earlier

I initially used `Console.Write` directly, then had to refactor to `IConsole` when I wanted to test `CustomInputHandler`. Starting with the abstraction would have avoided the refactor.

**Lesson:** If you know you'll need to test something, abstract it from day one.

### 3. Property-Based Testing for Tokenization

I wrote example-based tests for `ShellTokenizer`, but property-based testing would have caught more edge cases (nested quotes, escaped characters, etc.).

**Lesson:** Parsers benefit from property-based testing (e.g., FsCheck in C#).

### 4. Performance Benchmarking

I never benchmarked tab completion. The Trie is theoretically O(m), but I don't know if it's actually faster than LINQ for 2000 commands. BenchmarkDotNet would have answered this.

**Lesson:** Measure performance assumptions, don't just assume algorithmic complexity translates to real-world speed.

---

## Lessons Learned

### 1. Input Handling Is Harder Than Command Execution

Building the REPL with tab completion, history, and proper cursor management was more complex than parsing and executing commands. I underestimated this.

### 2. Cross-Platform Is Always Messier

Windows vs Unix differences showed up everywhere: PATH resolution, file permissions, line endings. Every "simple" operation needed platform checks.

### 3. The Devil Is in the UX Details

The beep sound on double-tab. The space after single completion. Auto-creating directories for redirection. These small touches require disproportionate effort.

### 4. Dependency Injection Works in Unconventional Places

Most shells don't use DI. But it made this codebase easy to test and extend. Don't assume patterns are limited to web apps or services.

### 5. Users Expect Bash Behavior

Even when the spec doesn't require it, users expect bash-like UX. Implementing `HISTFILE` and arrow key navigation wasn't required, but it made the shell feel professional.

### 6. Abstractions Enable Testing

Every place I used an interface (`ICommand`, `IPathResolver`, `IConsole`), testing was easy. Every place I used concrete types or statics, testing was hard.

---

## Conclusion

Building a shell from scratch is a great learning exercise. It forces you to think about problems you've taken for granted and make explicit design decisions.

The key takeaways:
- **Dependency injection** works outside web apps
- **Input handling** is surprisingly complex
- **Cross-platform** code requires careful platform detection
- **UX polish** (tab completion, history, beep sounds) requires disproportionate effort
- **Resource cleanup** (file handles, streams) needs explicit management

If you're interested in the full implementation, check out the [GitHub repo](https://github.com/Diego-Paris/codecrafters-shell-csharp) with architecture diagrams and comprehensive tests.

---

## Further Reading

- [CodeCrafters Shell Challenge](https://app.codecrafters.io/courses/shell/overview)
- [Trie Data Structure](https://en.wikipedia.org/wiki/Trie)
- [Microsoft DI Container](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [POSIX Shell Specification](https://pubs.opengroup.org/onlinepubs/9699919799/utilities/V3_chap02.html)

---

## About This Project

**Built with:** C# 9.0, .NET Core
**Challenge:** CodeCrafters "Build Your Own Shell"
**Lines of code:** ~2000 (including tests)
**Test coverage:** Comprehensive unit tests for all major components
**Source:** [github.com/Diego-Paris/codecrafters-shell-csharp](https://github.com/Diego-Paris/codecrafters-shell-csharp)
