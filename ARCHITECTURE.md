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
    A --> T[MiniShell.Tests/]
    B --> C[Program.cs]
    B --> D[Shell/]
    B --> E[Commands/]
    B --> F[Runtime/]
    B --> G[Abstractions/]
    B --> H[Parsing/]
    B --> I[DataStructures/]
    B --> J[Models/]

    D --> D1[Shell.cs]
    D --> D2[CommandRouter.cs]
    D --> D3[PipelineExecutor.cs]

    E --> E1[CdCommand.cs]
    E --> E2[EchoCommand.cs]
    E --> E3[ExitCommand.cs]
    E --> E4[GreetCommand.cs]
    E --> E5[PwdCommand.cs]
    E --> E6[TypeCommand.cs]
    E --> E7[HistoryCommand.cs]
    E --> E8[ExternalCommand.cs]

    F --> F1[ShellContext.cs]
    F --> F2[PathResolver.cs]
    F --> F3[HistoryService.cs]
    F --> F4[CustomInputHandler.cs]
    F --> F5[CommandCompletionProvider.cs]
    F --> F6[FileRedirectionHandler.cs]
    F --> F7[SystemConsole.cs]

    G --> G1[ICommand.cs]
    G --> G2[IShellContext.cs]
    G --> G3[IPathResolver.cs]
    G --> G4[IHistoryService.cs]
    G --> G5[IInputHandler.cs]
    G --> G6[ICompletionProvider.cs]
    G --> G7[IRedirectionHandler.cs]
    G --> G8[ITokenizer.cs]
    G --> G9[IConsole.cs]

    H --> H1[ShellTokenizer.cs]
    H --> H2[PipelineParser.cs]
    H --> H3[RedirectionParser.cs]

    I --> I1[CompletionTrie.cs]

    J --> J1[RedirectionInfo.cs]

    T --> T1[Commands/]
    T --> T2[Runtime/]
    T --> T3[Shell/]
    T --> T4[Parsing/]
    T --> T5[DataStructures/]

    style A fill:#e1f5ff
    style B fill:#fff4e1
    style T fill:#ffe4e1
    style D fill:#e8f5e9
    style E fill:#fce4ec
    style F fill:#f3e5f5
    style G fill:#fff9c4
    style H fill:#e1bee7
    style I fill:#c5e1a5
    style J fill:#ffccbc
```

---

## Application Flow

```mermaid
sequenceDiagram
    participant User
    participant Main as Program.cs
    participant DI as ServiceCollection
    participant Shell
    participant Input as CustomInputHandler
    participant History as HistoryService
    participant Router as CommandRouter
    participant Cmd as ICommand

    User->>Main: Start Application
    Main->>DI: Register Services
    DI->>DI: Register Infrastructure (PathResolver, Tokenizer, etc.)
    DI->>DI: Register ShellContext & HistoryService
    DI->>DI: Register CompletionTrie & CompletionProvider
    DI->>DI: Register CustomInputHandler
    DI->>DI: Register All Commands
    DI->>DI: Register CommandRouter & Shell
    Main->>DI: BuildServiceProvider()
    Main->>Shell: GetRequiredService<Shell>()
    Main->>Shell: RunAsync()
    Shell->>History: LoadFromFile()

    loop REPL Loop
        Shell->>Input: ReadInput("$ ")
        Input->>User: Display prompt & wait for input
        User->>Input: Type command (with tab/arrows)
        Input->>Input: Handle tab completion/history
        Input-->>Shell: Return command line
        Shell->>Shell: AddToHistory(line)
        Shell->>Router: Route(line)
        Router->>Router: Parse pipelines & redirections
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
flowchart TD
    subgraph Program.cs
        SC[ServiceCollection]
    end

    subgraph Infrastructure
        PR[PathResolver]
        TOK[ShellTokenizer]
        CTX[ShellContext]
        HIST[HistoryService]
    end

    subgraph Tab Completion
        TRIE[CompletionTrie]
        COMP[CommandCompletionProvider]
        INPUT[CustomInputHandler]
    end

    subgraph Commands
        CD[CdCommand]
        ECHO[EchoCommand]
        EXIT[ExitCommand]
        GREET[GreetCommand]
        HISTCMD[HistoryCommand]
        PWD[PwdCommand]
        TYPE[TypeCommand]
        EXT[ExternalCommand]
    end

    subgraph Shell Layer
        RTR[CommandRouter]
        SH[Shell]
    end

    SC -->|Register as Singleton| Infrastructure
    SC -->|Register as Singleton| Tab Completion
    SC -->|Register as Singleton| Commands
    SC -->|Register as Singleton| Shell Layer

    SC -->|Build| SP[ServiceProvider]
    SP -->|Resolve| SH

    SH -->|Inject| RTR
    SH -->|Inject| INPUT
    SH -->|Inject| CTX
    SH -->|Inject| HIST

    RTR -->|Inject| CTX
    INPUT -->|Inject| COMP
    INPUT -->|Inject| CTX
    COMP -->|Inject| TRIE
    COMP -->|Inject| CTX
    HIST -->|Inject| CTX
    CTX -->|Inject| Commands
    CTX -->|Inject| PR

    style SC fill:#e1f5ff
    style SP fill:#fff4e1
    style SH fill:#e8f5e9
    style Infrastructure fill:#c5e1a5
    style Tab Completion fill:#ffccbc
    style Commands fill:#fce4ec
    style Shell Layer fill:#e1bee7
```

---

## Tab Completion Flow

```mermaid
flowchart TD
    Start([User presses TAB]) --> GetPrefix[Get current input buffer]
    GetPrefix --> Query[CompletionProvider.GetCompletions]
    Query --> CheckTrie{Check CompletionTrie}

    CheckTrie --> TraversePrefix[Traverse trie using prefix chars]
    TraversePrefix --> CollectMatches[Collect all words under prefix node]
    CollectMatches --> CountMatches{How many matches?}

    CountMatches -->|0 matches| Beep[Play beep sound \\x07]
    Beep --> Reset[Reset tab state]
    Reset --> End([Return to input])

    CountMatches -->|1 match| Complete[Auto-complete full word]
    Complete --> AddSpace[Append space]
    AddSpace --> DisplaySingle[Display completion]
    DisplaySingle --> ResetSingle[Reset tab state]
    ResetSingle --> End

    CountMatches -->|Multiple matches| FindCommon[Find longest common prefix]
    FindCommon --> HasRemaining{Remaining chars > 0?}

    HasRemaining -->|Yes| DisplayPartial[Display partial completion]
    DisplayPartial --> UpdateBuffer[Update input buffer]
    UpdateBuffer --> ResetPartial[Reset tab state]
    ResetPartial --> End

    HasRemaining -->|No| CheckDouble{Second TAB press?}
    CheckDouble -->|No| BeepFirst[Play beep sound]
    BeepFirst --> MarkDouble[Mark as multi-match state]
    MarkDouble --> SavePrefix[Save prefix for next TAB]
    SavePrefix --> End

    CheckDouble -->|Yes, same prefix| ShowAll[Display all matches in columns]
    ShowAll --> Newline[Print newline]
    Newline --> Redraw[Redraw prompt + buffer]
    Redraw --> ResetDouble[Reset tab state]
    ResetDouble --> End

    style Start fill:#e1f5ff
    style End fill:#e8f5e9
    style Complete fill:#c5e1a5
    style ShowAll fill:#ffccbc
    style Beep fill:#ffebee
    style BeepFirst fill:#ffebee
```

---

## Input Handler State Machine

```mermaid
stateDiagram-v2
    [*] --> WaitingForKey: Display prompt

    WaitingForKey --> ProcessEnter: Enter key
    WaitingForKey --> ProcessTab: Tab key
    WaitingForKey --> ProcessUpArrow: Up arrow
    WaitingForKey --> ProcessDownArrow: Down arrow
    WaitingForKey --> ProcessBackspace: Backspace
    WaitingForKey --> ProcessChar: Regular character

    ProcessEnter --> ReturnLine: Add newline
    ReturnLine --> [*]: Return buffer as string

    ProcessTab --> TabCompletion: Get completions
    TabCompletion --> UpdateDisplay: Show completion/matches
    UpdateDisplay --> WaitingForKey

    ProcessUpArrow --> NavigateHistory: Move back in history
    NavigateHistory --> UpdateDisplay

    ProcessDownArrow --> NavigateHistory: Move forward in history

    ProcessBackspace --> RemoveChar: Delete from buffer
    RemoveChar --> UpdateDisplay

    ProcessChar --> AddToBuffer: Append to buffer
    AddToBuffer --> UpdateDisplay

    note right of TabCompletion
        Handled by CompletionProvider
        Uses CompletionTrie for O(m) lookup
    end note

    note right of NavigateHistory
        Maintains history index
        Saves current input before navigating
        Restores on down-arrow at end
    end note
```

---

## I/O Redirection Flow

```mermaid
flowchart TD
    Start([Command with redirection]) --> Parse[RedirectionParser.Parse]
    Parse --> Extract[Extract command tokens & redirection info]
    Extract --> CreateInfo[Build RedirectionInfo object]

    CreateInfo --> CheckStdout{Has stdout redirect?}
    CheckStdout -->|Yes| StdoutMode{Append mode?}
    StdoutMode -->|">>"| CreateAppendOut[FileMode.Append for stdout]
    StdoutMode -->|">"| CreateOverwriteOut[FileMode.Create for stdout]

    CheckStdout -->|No| CheckStderr
    CreateAppendOut --> CheckStderr
    CreateOverwriteOut --> CheckStderr

    CheckStderr{Has stderr redirect?} -->|Yes| StderrMode{Append mode?}
    StderrMode -->|"2>>"| CreateAppendErr[FileMode.Append for stderr]
    StderrMode -->|"2>"| CreateOverwriteErr[FileMode.Create for stderr]

    CheckStderr -->|No| CreateContext
    CreateAppendErr --> CreateContext
    CreateOverwriteErr --> CreateContext

    CreateContext[FileRedirectionHandler.CreateRedirectedContext]
    CreateContext --> MakeDirs[Create parent directories if needed]
    MakeDirs --> OpenFiles[Open FileStreams]
    OpenFiles --> WrapStreams[Wrap in StreamWriters with AutoFlush]
    WrapStreams --> TrackDisposables[Add to disposables list]
    TrackDisposables --> CreateWrapper[Create RedirectedShellContext]

    CreateWrapper --> Execute[Execute command with redirected context]
    Execute --> Cleanup[FileRedirectionHandler.Cleanup]
    Cleanup --> DisposeAll[Dispose all streams & writers]
    DisposeAll --> End([Command complete])

    style Start fill:#e1f5ff
    style End fill:#e8f5e9
    style MakeDirs fill:#fff4e1
    style Execute fill:#c5e1a5
    style Cleanup fill:#ffccbc
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
