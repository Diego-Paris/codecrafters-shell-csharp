# Building a Shell in C#: Architecture Decisions and Lessons Learned

*A deep dive into implementing a POSIX-compliant shell with dependency injection, trie-based tab completion, and cross-platform PATH resolution*

---

## Introduction

When you type a command in bash or zsh, a lot happens before that command runs. Building a shell from scratch for the CodeCrafters challenge forced me to think about problems I'd taken for granted: How does tab completion work? Why does pressing the up arrow show previous commands? How does the shell know where to find executables?

Turns out, the answer to all of these is "way more complicated than you'd think."

This post isn't about what the shell does, it's about **why I made specific implementation choices** and what I learned along the way.

**Tech stack:** C# 9.0, .NET Core, Microsoft.Extensions.DependencyInjection

**Source code:** [github.com/Diego-Paris/codecrafters-shell-csharp](https://github.com/Diego-Paris/codecrafters-shell-csharp)

---

## Custom Input Handling: Beyond Console.ReadLine()

### Why Not Use Console.ReadLine()?

`Console.ReadLine()` gives you a complete line after the user presses Enter. It doesn't let you:
- Handle tab completion
- Navigate history with arrow keys
- Show partial completions
- Implement backspace behavior

In other words, `Console.ReadLine()` gives you the finished product with no control over the process. For a real shell, I needed to read **individual keystrokes** and manage my own input buffer.

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

**Input handling is way harder than command execution.** Parsing and running commands is straightforward. Building a good REPL with tab completion, history, and proper cursor management is surprisingly complex. I spent more time getting backspace to work correctly than implementing the entire command execution engine. Let that sink in.

**State machines everywhere.** The input handler is essentially a state machine responding to key events. Understanding this early would have simplified the design.

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

This works. It's also O(n) where n is the number of commands. On my system, there are 2000+ executables in PATH. Every tab press would iterate through all of them. That's rough.

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
    public string LastPrefix { get; set} = string.Empty;
    public bool LastTabWasMultiMatch { get; set; }
}
```

If `LastTabWasMultiMatch` is true and the prefix hasn't changed, the second tab displays all matches.

**[View the implementation](https://github.com/Diego-Paris/codecrafters-shell-csharp/blob/master/src/Runtime/CustomInputHandler.cs#L152-L188)**

### What I Learned

**Small UX details have complex implementations.** The beep sound, the double-tab behavior, the space after single completion, users don't think about these, but they're load-bearing for the experience. Turns out the "invisible" features are the ones that take the most work.

---

## Command History: More Than Just a List

Implementing `history` seems simple: keep a list of commands. But getting the UX right is tricky.

When you press the up arrow, the shell should:
1. Save your current (unsent) input
2. Show the previous command
3. Let you keep scrolling back
4. When you scroll past the end, restore your original input

That last part is important. If you type "echo hello", press up a few times, then press down to get back to where you were, you should see "echo hello" again, not a blank line.

```csharp
private void HandleUpArrow(string prompt, InputState state)
{
    if (state.HistoryIndex > 0)
    {
        // First time going up? Save current input
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

### Persisting History

Bash stores history in `~/.bash_history`. When you exit and restart bash, your history is still there. I implemented the same using `HISTFILE`:

```csharp
public void LoadFromFile()
{
    var histFile = Environment.GetEnvironmentVariable("HISTFILE");
    if (string.IsNullOrEmpty(histFile) || !File.Exists(histFile))
        return;

    var lines = File.ReadAllLines(histFile);
    foreach (var line in lines.Where(l => !string.IsNullOrWhiteSpace(l)))
        _context.AddToHistory(line);
}
```

This wasn't in the CodeCrafters requirements. But users expect shells to work like bash. When your reference implementation is a 30-year-old piece of software, that's the standard you're measured against.

---

## Cross-Platform PATH Resolution: Windows vs Unix

Here's a question: how do you find an executable in PATH?

On Unix, it's straightforward:
1. Split PATH by `:`
2. Check each directory for the command
3. Verify the file has execute permissions

On Windows, it's... different:

```csharp
var candidates = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
    ? BuildWindowsCandidates(command)  // "cat" becomes ["cat.exe", "cat.cmd", "cat.bat"]
    : [command];                        // "cat" stays "cat"
```

Windows doesn't use execute permissions. Instead, it uses file extensions. The `PATHEXT` environment variable lists valid executable extensions (`.exe`, `.cmd`, `.bat`, `.com`). So when you type `cat`, Windows looks for `cat.exe`, `cat.cmd`, `cat.bat`, etc.

```csharp
private IEnumerable<string> BuildWindowsCandidates(string command)
{
    if (Path.HasExtension(command))
        return [command];

    var pathExt = Environment.GetEnvironmentVariable("PATHEXT")
        ?? ".exe;.cmd;.bat;.com";
    var extensions = pathExt.Split(';', StringSplitOptions.RemoveEmptyEntries);

    return extensions.Select(ext => command + ext);
}
```

Cross-platform code is always messier than you think. Every "simple" operation needs platform checks.

---

## I/O Redirection: Resource Management Matters

When a user runs `echo "test" > output.txt`, the shell needs to:
1. Parse the redirection
2. Open the file
3. Redirect stdout
4. Execute the command
5. **Close the file handle**

That last step is easy to forget. Without proper cleanup, file handles leak and files stay locked.

I used the `IDisposable` pattern to make cleanup explicit:

```csharp
public sealed class FileRedirectionHandler : IRedirectionHandler
{
    private readonly List<IDisposable> _disposables = [];

    public void Cleanup()
    {
        foreach (var disposable in _disposables)
            disposable.Dispose();
        _disposables.Clear();
    }
}
```

### The Little Details

Bash does something subtle with redirection: if you run `echo "test" > /tmp/foo/bar/output.txt` and `/tmp/foo/bar` doesn't exist, bash creates it automatically.

```csharp
var directory = Path.GetDirectoryName(filePath);
if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
    Directory.CreateDirectory(directory);
```

UX polish is made of tiny details like this. They're invisible when they work, glaring when they don't.

---

## Why Dependency Injection for a Shell?

### The Decision

Most shell implementations use global state or singletons. I went a different route: Microsoft's dependency injection container and constructor injection throughout. Yes, for a command-line shell. Bear with me.

### Why?

**Testability was the driver.** I wanted to write comprehensive tests without mocking the filesystem or spawning real processes. With DI:

```csharp
services.AddSingleton<IPathResolver, PathResolver>();
services.AddSingleton<IShellContext, ShellContext>();
services.AddSingleton<IHistoryService, HistoryService>();
services.AddSingleton<ICompletionTrie, CompletionTrie>();
services.AddSingleton<IInputHandler, CustomInputHandler>();

// All commands implement ICommand
services.AddSingleton<ICommand, CdCommand>();
services.AddSingleton<ICommand, EchoCommand>();
```

Why? Testability. With DI:
- Every `ICommand` can be tested in isolation
- `PathResolver` can be swapped for a test double
- `CustomInputHandler` gets an `IConsole` abstraction instead of using `Console` directly
- No global state to manage

The trade-off: you need to be careful about circular dependencies. I learned this the hard way when the app hung during startup with zero error messages. Drawing the dependency graph upfront would have saved me debugging time.

But would I do it again? Yes. The testing velocity was worth it.

---

## What I'd Do Differently

### 1. Benchmark Performance Assumptions

I assumed the Trie would be faster than LINQ for tab completion. I never measured it. BenchmarkDotNet would have answered this definitively.

### 2. Abstract Console from Day One

I initially used `Console.Write` directly, then had to refactor to `IConsole` for testing. Starting with abstractions would have avoided the refactor.

### 3. Property-Based Testing for Parsing

I wrote example-based tests for the tokenizer. Property-based testing (using FsCheck) would have caught more edge cases with escaped characters and nested quotes.

---

## Lessons Learned

**Input handling is harder than command execution.** I spent more time on backspace behavior than on the entire parsing engine. Building a good REPL is surprisingly complex.

**Cross-platform always means platform checks.** Windows vs Unix differences showed up everywhere: PATH resolution, file permissions, line endings. Every "simple" operation needed conditionals.

**Small UX details require disproportionate effort.** The beep on double-tab. The space after completion. Auto-creating directories. Users don't notice these features, but they notice when they're missing.

**Users expect bash behavior.** Even when features aren't required, users expect bash-like UX. Implementing `HISTFILE` and arrow navigation wasn't in the spec, but it made the shell feel complete.

**Dependency injection works outside web apps.** Most shells don't use DI. But it made this codebase easy to test and extend. Don't assume patterns only work in specific contexts.

---

## Conclusion

Building a shell from scratch forces you to think about problems you've taken for granted. What seems simple ("read input, execute command") turns out to have layers of complexity.

The real work isn't parsing commands. It's the invisible UX polish: tab completion that feels instant, history that preserves your unsent input, cross-platform PATH resolution that just works.

If you want to see the full implementation with architecture diagrams and comprehensive tests, check out the [GitHub repo](https://github.com/Diego-Paris/codecrafters-shell-csharp).

---

## Further Reading

- [CodeCrafters Shell Challenge](https://app.codecrafters.io/courses/shell/overview)
- [Trie Data Structure](https://en.wikipedia.org/wiki/Trie)
- [Microsoft DI Container](https://learn.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)
- [POSIX Shell Specification](https://pubs.opengroup.org/onlinepubs/9699919799/utilities/V3_chap02.html)
