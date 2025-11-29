[![progress-banner](https://backend.codecrafters.io/progress/shell/f4ad8db5-a556-4a13-8cf8-9c4ab9675d5e)](https://app.codecrafters.io/users/codecrafters-bot?r=2qF)

# MiniShell - A Modern Shell Implementation in C#

A fully-featured POSIX-compliant shell built from scratch in C#/.NET, featuring advanced input handling, tab completion with trie-based prefix matching, persistent command history, I/O redirection, and pipeline support.

## Why This Implementation Is Interesting

Most shell implementations use basic string matching and simple REPL loops. This implementation goes deeper:

- **Trie-based tab completion** for O(m) prefix matching instead of naive iteration
- **Full readline-style input handling** with arrow key navigation, backspace, and multi-match behavior
- **Persistent command history** with incremental file append and HISTFILE support
- **Cross-platform PATH resolution** handling Windows PATHEXT and Unix executable detection
- **Dependency injection architecture** making the shell fully testable with interface abstractions
- **Proper I/O redirection** with automatic directory creation and resource cleanup

## Features

### Core Shell Features
- REPL with command parsing and execution
- Built-in commands: `cd`, `pwd`, `echo`, `type`, `exit`, `greet`, `history`
- External command execution with PATH resolution
- Cross-platform support (Windows/Unix)

### Advanced Features
- **Tab Completion**: Smart prefix matching with multi-match display
- **Command History**: Persistent history with up/down arrow navigation
- **I/O Redirection**: Support for `>`, `>>`, `2>`, `2>>`
- **Pipelines**: Multi-command pipelines with `|`
- **Custom Input Handler**: Readline-like experience without external dependencies

## Quick Start

### Prerequisites
- .NET 9.0 SDK

### Running the Shell
```sh
dotnet run
```

Or use the provided script:
```sh
./your_program.sh
```

## Feature Demos

### 1. Tab Completion with Trie Matching

![Tab Completion Demo](assets/tab-completion.gif)

**Demo commands:**
```sh
# Type 'ec' then press TAB (should complete to 'echo ')
# Type 'e' then press TAB twice (should show all commands starting with 'e')
# Type 'pw' then press TAB (should complete to 'pwd ')
```

**What's interesting:** Uses a Trie data structure for O(m) prefix lookup where m is the prefix length. Double-tab shows all matches, intelligently completes common prefixes.

### 2. Command History with Arrow Keys

![Command History Demo](assets/command-history.gif)

**Demo commands:**
```sh
export HISTFILE=~/.minishell_history
echo "first command"
echo "second command"
# Press UP arrow (shows "second command")
# Press UP again (shows "first command")
# Press DOWN (shows "second command")
# Exit and restart shell - history persists
```

**What's interesting:** Full readline-style navigation. History persists across sessions via HISTFILE. Supports `history`, `history -w` (write), and `history -a` (append).

### 3. I/O Redirection with Auto-Directory Creation

![I/O Redirection Demo](assets/io-redirection.gif)

**Demo commands:**
```sh
# stdout redirection
echo "hello world" > output.txt
cat output.txt

# append mode
echo "line 2" >> output.txt
cat output.txt

# stderr redirection
ls /nonexistent 2> errors.txt
cat errors.txt

# automatic directory creation
echo "test" > /tmp/deep/nested/path/file.txt
cat /tmp/deep/nested/path/file.txt
```

**What's interesting:** Automatically creates parent directories. Proper resource cleanup with IDisposable pattern. Separate stdout/stderr redirection support.

### 4. Pipeline Execution

![Pipeline Execution Demo](assets/pipeline-execution.gif)

**Demo commands:**
```sh
echo "hello world" | tr 'a-z' 'A-Z'
cat file.txt | grep "pattern" | wc -l
```

**What's interesting:** Parser splits on `|` boundaries, executor wires up stdin/stdout between processes.

### 5. Cross-Platform PATH Resolution

![PATH Resolution Demo](assets/path-resolution.gif)

**Demo commands:**
```sh
type echo      # shows built-in
type cat       # shows path to executable
type ls        # shows path (different on Windows vs Unix)

# On Windows, resolves using PATHEXT (.exe, .cmd, .bat)
# On Unix, checks execute permission
```

**What's interesting:** Platform-specific executable detection. Windows uses PATHEXT environment variable to try multiple extensions. Unix checks file permissions.

### 6. Built-in Commands

![Built-in Commands Demo](assets/builtin-commands.gif)

**Demo commands:**
```sh
pwd                    # print working directory
cd /tmp               # change directory
pwd                   # verify change
echo hello world      # echo with args
type cd               # shows 'cd is a shell builtin'
greet                 # custom greeting command
history               # show command history
history -w            # write history to HISTFILE
```

## Architecture

The shell uses a clean, dependency-injected architecture with clear separation of concerns. See [ARCHITECTURE.md](ARCHITECTURE.md) for detailed diagrams.

**Key components:**
- `Shell`: REPL orchestrator
- `CommandRouter`: Tokenization and routing
- `CustomInputHandler`: Readline-style input with tab completion
- `CompletionTrie`: O(m) prefix matching data structure
- `FileRedirectionHandler`: I/O redirection with resource management
- `HistoryService`: Persistent command history
- `PathResolver`: Cross-platform executable resolution

## Project Structure

```
src/
├── Program.cs                    # DI setup and entry point
├── Shell/
│   ├── Shell.cs                 # REPL orchestrator
│   ├── CommandRouter.cs         # Command parsing and routing
│   └── PipelineExecutor.cs      # Pipeline execution
├── Commands/                     # Built-in command implementations
├── Runtime/
│   ├── CustomInputHandler.cs    # Readline-style input handler
│   ├── HistoryService.cs        # Persistent history
│   ├── PathResolver.cs          # Cross-platform PATH lookup
│   └── FileRedirectionHandler.cs # I/O redirection
├── Parsing/
│   ├── ShellTokenizer.cs        # Input tokenization
│   ├── PipelineParser.cs        # Pipeline parsing
│   └── RedirectionParser.cs     # Redirection parsing
├── DataStructures/
│   └── CompletionTrie.cs        # Tab completion trie
└── Abstractions/                 # Interfaces for testability
```

## Testing

```sh
dotnet test
```

The codebase includes comprehensive tests for:
- Tab completion and trie operations
- Command routing and execution
- I/O redirection (stdout, stderr, append modes)
- Custom input handling
- Pipeline parsing

## Technical Deep Dive

For a detailed explanation of implementation choices, architecture decisions, and lessons learned, see [BLOG_POST.md](BLOG_POST.md).

## Implementation Highlights

### Tab Completion Trie
Instead of iterating through all commands on each tab press, the shell uses a Trie (prefix tree) to achieve O(m) lookup time where m is the length of the prefix. The trie is built once at startup and updated when the PATH changes.

### Custom Input Handler
Rather than relying on `Console.ReadLine()`, the shell implements a custom input handler that reads individual keystrokes and maintains its own input buffer. This enables:
- Arrow key history navigation
- Tab completion with partial completion
- Backspace handling
- Multi-match beep behavior

### Dependency Injection
Unlike most shell implementations that use globals or singletons, this shell uses Microsoft's DI container. Every component receives its dependencies through constructor injection, making the codebase:
- Easy to test (mock any dependency)
- Easy to extend (register new commands)
- Easy to reason about (explicit dependencies)

## Lessons Learned

Building a shell from scratch teaches you things you don't learn from using shells:

1. **Input handling is hard** - What seems like simple text input is actually complex state management
2. **Cross-platform is messy** - Windows and Unix have fundamentally different ideas about what makes a file executable
3. **Proper cleanup matters** - File handles and redirections need careful resource management
4. **Architecture pays off** - The DI setup took time upfront but made testing and iteration much faster

---

Built as part of CodeCrafters ["Build Your Own Shell" Challenge](https://app.codecrafters.io/courses/shell/overview).
