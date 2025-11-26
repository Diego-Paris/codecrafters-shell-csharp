# MiniShell Architecture

This document provides visual diagrams to help understand the structure and flow of the MiniShell project.

## Table of Contents
- [Project Structure](#project-structure)
- [Application Flow](#application-flow)
- [Command Execution Flow](#command-execution-flow)
- [Class Relationships](#class-relationships)
- [External Command Resolution](#external-command-resolution)

---

## Project Structure

```mermaid
graph TD
    A[codecrafters-shell-csharp] --> B[src/]
    B --> C[Program.cs]
    B --> D[Shell/]
    B --> E[Commands/]
    B --> F[Runtime/]
    B --> G[Abstractions/]

    D --> D1[Shell.cs]
    D --> D2[CommandRouter.cs]

    E --> E1[CdCommand.cs]
    E --> E2[EchoCommand.cs]
    E --> E3[ExitCommand.cs]
    E --> E4[GreetCommand.cs]
    E --> E5[PwdCommand.cs]
    E --> E6[TypeCommand.cs]
    E --> E7[ExternalCommand.cs]

    F --> F1[ShellContext.cs]
    F --> F2[PathResolver.cs]

    G --> G1[ICommand.cs]
    G --> G2[IShellContext.cs]
    G --> G3[IPathResolver.cs]

    style A fill:#e1f5ff
    style B fill:#fff4e1
    style D fill:#e8f5e9
    style E fill:#fce4ec
    style F fill:#f3e5f5
    style G fill:#fff9c4
```

---

## Application Flow

```mermaid
sequenceDiagram
    participant User
    participant Main as Program.cs
    participant DI as ServiceCollection
    participant Shell
    participant Router as CommandRouter
    participant Cmd as ICommand

    User->>Main: Start Application
    Main->>DI: Register Services
    DI->>DI: Register IPathResolver
    DI->>DI: Register IShellContext
    DI->>DI: Register All Commands
    DI->>DI: Register CommandRouter
    DI->>DI: Register Shell
    Main->>DI: BuildServiceProvider()
    Main->>Shell: GetRequiredService<Shell>()
    Main->>Shell: RunAsync()

    loop REPL Loop
        Shell->>User: Display "$ " prompt
        User->>Shell: Enter command
        Shell->>Router: Route(line)
        Router->>Router: Tokenize(line)
        Router->>Cmd: Execute(args, ctx)
        Cmd-->>Router: Return exit code
        Router-->>Shell: Return exit code
    end
```

---

## Command Execution Flow

```mermaid
flowchart TD
    Start([User enters command]) --> Input[Shell reads input line]
    Input --> Router[CommandRouter.Route]
    Router --> Tokenize[Tokenize input into parts]
    Tokenize --> CheckEmpty{Empty input?}
    CheckEmpty -->|Yes| Return0[Return 0]
    CheckEmpty -->|No| GetName[Extract command name]

    GetName --> CheckBuiltin{Is built-in command?}
    CheckBuiltin -->|Yes| ExecuteBuiltin[Execute built-in command]
    ExecuteBuiltin --> ReturnCode[Return exit code]

    CheckBuiltin -->|No| CheckExternal{ExternalCommand exists?}
    CheckExternal -->|Yes| ExecuteExternal[Execute external command]
    ExecuteExternal --> ReturnCode

    CheckExternal -->|No| NotFound[Print 'command not found']
    NotFound --> Return127[Return 127]

    Return0 --> End([Continue REPL])
    ReturnCode --> End
    Return127 --> End

    style Start fill:#e1f5ff
    style End fill:#e8f5e9
    style ExecuteBuiltin fill:#fff4e1
    style ExecuteExternal fill:#fce4ec
    style NotFound fill:#ffebee
```

---

## Class Relationships

```mermaid
classDiagram
    class ICommand {
        <<interface>>
        +string Name
        +string Description
        +Execute(string[] args, IShellContext ctx) int
    }

    class IShellContext {
        <<interface>>
        +IReadOnlyDictionary~string, ICommand~ Commands
        +TextReader In
        +TextWriter Out
        +TextWriter Err
        +IPathResolver PathResolver
    }

    class IPathResolver {
        <<interface>>
        +FindInPath(string command) string?
    }

    class Shell {
        -CommandRouter _router
        +RunAsync() Task~int~
    }

    class CommandRouter {
        -IShellContext _ctx
        +Route(string line) int
        -Tokenize(string input) List~string~
    }

    class ShellContext {
        +IReadOnlyDictionary~string, ICommand~ Commands
        +TextReader In
        +TextWriter Out
        +TextWriter Err
        +IPathResolver PathResolver
    }

    class PathResolver {
        +FindInPath(string command) string?
        -BuildWindowsCandidates(string cmd) IEnumerable~string~
        -IsExecutable(string path) bool
    }

    class CdCommand {
        +Name: "cd"
        +Description: "Change current working directory"
        +Execute(args, ctx) int
    }

    class EchoCommand {
        +Name: "echo"
        +Description: "Echo arguments"
        +Execute(args, ctx) int
    }

    class ExitCommand {
        +Name: "exit"
        +Description: "Exit shell"
        +Execute(args, ctx) int
    }

    class PwdCommand {
        +Name: "pwd"
        +Description: "Print current working directory"
        +Execute(args, ctx) int
    }

    class TypeCommand {
        +Name: "type"
        +Description: "Display command type"
        +Execute(args, ctx) int
    }

    class ExternalCommand {
        +Name: "external"
        +Description: "Runs external programs"
        +Execute(args, ctx) int
        -QuoteIfNeeded(string arg) string
    }

    ICommand <|.. CdCommand
    ICommand <|.. EchoCommand
    ICommand <|.. ExitCommand
    ICommand <|.. PwdCommand
    ICommand <|.. TypeCommand
    ICommand <|.. ExternalCommand

    IShellContext <|.. ShellContext
    IPathResolver <|.. PathResolver

    Shell --> CommandRouter
    CommandRouter --> IShellContext
    ShellContext --> ICommand
    ShellContext --> IPathResolver
    ExternalCommand ..> IPathResolver : uses
```

---

## External Command Resolution

```mermaid
flowchart TD
    Start([ExternalCommand.Execute]) --> CheckArgs{Has args?}
    CheckArgs -->|No| Error[Print 'missing operand']
    Error --> Return2[Return 2]

    CheckArgs -->|Yes| GetInput[Get command name]
    GetInput --> CheckPath{Contains path separators?}

    CheckPath -->|Yes| UseDirect[Use direct path]
    CheckPath -->|No| Resolve[PathResolver.FindInPath]

    Resolve --> SearchPath[Search PATH directories]
    SearchPath --> CheckOS{Windows?}
    CheckOS -->|Yes| WinCandidates[Build candidates with PATHEXT]
    CheckOS -->|No| UnixCandidates[Use command as-is]

    WinCandidates --> TryFind[Try each directory + candidate]
    UnixCandidates --> TryFind

    TryFind --> CheckExec{File exists & executable?}
    CheckExec -->|Yes| Found[Return full path]
    CheckExec -->|No| NextCandidate{More to try?}
    NextCandidate -->|Yes| TryFind
    NextCandidate -->|No| NotFound[Return null]

    UseDirect --> HasPath[Path resolved]
    Found --> HasPath
    NotFound --> PrintNotFound[Print 'not found']
    PrintNotFound --> Return127[Return 127]

    HasPath --> CheckBatch{Windows batch script?}
    CheckBatch -->|Yes| UseCmdExe[Use cmd.exe /c]
    CheckBatch -->|No| UseDirect2[Use executable directly]

    UseCmdExe --> BuildArgs[Build quoted arguments]
    UseDirect2 --> BuildArgs

    BuildArgs --> StartProcess[Start Process]
    StartProcess --> WaitExit[Wait for exit]
    WaitExit --> ReturnCode[Return process exit code]

    StartProcess --> CatchError{Exception?}
    CatchError -->|Yes| PrintError[Print error message]
    PrintError --> Return1[Return 1]

    Return2 --> End([End])
    Return127 --> End
    Return1 --> End
    ReturnCode --> End

    style Start fill:#e1f5ff
    style End fill:#e8f5e9
    style Error fill:#ffebee
    style PrintNotFound fill:#ffebee
    style PrintError fill:#ffebee
    style StartProcess fill:#fff4e1
```

---

## Built-in Commands Detail

```mermaid
graph LR
    subgraph Built-in Commands
        CD[cd<br/>Change directory]
        ECHO[echo<br/>Print arguments]
        EXIT[exit<br/>Exit shell]
        PWD[pwd<br/>Print working dir]
        TYPE[type<br/>Show command type]
        GREET[greet<br/>Greeting message]
    end

    subgraph Special Command
        EXT[external<br/>Execute programs]
    end

    CD -->|Validates directory| FS[File System]
    PWD -->|Reads directory| FS
    TYPE -->|Checks commands| CMDLIST[Command Registry]
    TYPE -->|Searches PATH| PATH[PathResolver]
    EXT -->|Resolves & executes| PATH
    EXT -->|Spawns process| PROC[Process]
    EXIT -->|Terminates| SHELL[Shell REPL]

    style CD fill:#b3e5fc
    style ECHO fill:#b3e5fc
    style EXIT fill:#b3e5fc
    style PWD fill:#b3e5fc
    style TYPE fill:#b3e5fc
    style GREET fill:#b3e5fc
    style EXT fill:#ffccbc
    style FS fill:#c8e6c9
    style PATH fill:#c8e6c9
    style PROC fill:#c8e6c9
    style SHELL fill:#c8e6c9
    style CMDLIST fill:#c8e6c9
```

---

## Dependency Injection Setup

```mermaid
flowchart LR
    subgraph Program.cs
        SC[ServiceCollection]
    end

    SC -->|Register| PR[PathResolver<br/>Singleton]
    SC -->|Register| CTX[ShellContext<br/>Singleton]
    SC -->|Register| CD[CdCommand]
    SC -->|Register| ECHO[EchoCommand]
    SC -->|Register| EXIT[ExitCommand]
    SC -->|Register| GREET[GreetCommand]
    SC -->|Register| PWD[PwdCommand]
    SC -->|Register| TYPE[TypeCommand]
    SC -->|Register| EXT[ExternalCommand]
    SC -->|Register| RTR[CommandRouter]
    SC -->|Register| SH[Shell]

    SC -->|Build| SP[ServiceProvider]
    SP -->|Resolve| Shell
    Shell -->|Inject| CommandRouter
    CommandRouter -->|Inject| ShellContext
    ShellContext -->|Inject| Commands[All ICommands]
    ShellContext -->|Inject| PathResolver

    style SC fill:#e1f5ff
    style SP fill:#fff4e1
    style Shell fill:#e8f5e9
```

---

## Key Design Patterns

### 1. **Dependency Injection**
All components are registered in the DI container and resolved at runtime, promoting loose coupling and testability.

### 2. **Command Pattern**
Each command implements the `ICommand` interface with a uniform `Execute` method, making it easy to add new commands.

### 3. **Strategy Pattern**
`PathResolver` uses different strategies for Windows vs Unix systems to locate executables.

### 4. **REPL (Read-Eval-Print Loop)**
The `Shell` class implements a continuous loop that reads user input, evaluates commands, and prints results.

### 5. **Router Pattern**
`CommandRouter` acts as a dispatcher, tokenizing input and routing to appropriate command handlers.

---

## Data Flow Summary

1. **User Input** → Shell reads from Console.In
2. **Tokenization** → CommandRouter splits input into command + args
3. **Routing** → Router looks up command in registry
4. **Execution** → Command executes with IShellContext
5. **Output** → Results written to Console.Out/Err
6. **Loop** → Shell continues REPL until exit

This architecture provides clean separation of concerns, making the codebase maintainable and extensible.
