# LinkedIn Post Draft

---

Just finished building a shell from scratch in C# for the CodeCrafters challenge. Turns out, there's a lot more to input handling than `Console.ReadLine()`.

Some interesting problems I solved:

**Tab completion with a Trie** - Instead of iterating through all commands on every tab press, I built a prefix tree for O(m) lookups. Double-tab shows all matches, single-tab completes the common prefix.

**Custom input handler** - Implementing readline-style behavior without external dependencies meant reading individual keystrokes, managing an input buffer, and handling arrow key history navigation. What looks simple to users is surprisingly stateful under the hood.

**Cross-platform PATH resolution** - Windows and Unix have completely different ideas about what makes a file executable. Windows uses PATHEXT to try multiple extensions (.exe, .cmd, .bat). Unix checks file permissions. Same API surface, wildly different implementations.

**Dependency injection for a shell** - Most shells use globals. I used Microsoft's DI container. Every command is registered as a service, making the whole thing testable and easy to extend.

The full implementation is on GitHub with detailed architecture diagrams, demos, and a deep-dive into the design decisions.

What surprised me most: input handling is way harder than command execution. Building a proper REPL with tab completion and history navigation is more complex than parsing and running commands.

🔗 [Link to GitHub repo]

---

## Alternative Shorter Version

Built a shell from scratch in C#. Three things that surprised me:

1. Tab completion is more interesting with a Trie (O(m) prefix matching vs naive iteration)
2. Custom input handling is way harder than using Console.ReadLine() - managing state for tab completion and arrow key navigation is complex
3. Cross-platform PATH resolution is messy - Windows PATHEXT vs Unix file permissions

Used dependency injection for the whole thing, making it fully testable. All commands are registered as services.

Full implementation with architecture diagrams and design deep-dive on GitHub.

🔗 [Link to GitHub repo]

---

## Technical Hook Version (More Developer-Focused)

How do you make tab completion fast when you have hundreds of commands in PATH?

Just finished building a shell in C# using a Trie for O(m) prefix matching instead of iterating through all commands. When you press tab, it traverses the prefix chars and collects matches in a single pass.

Also implemented:
- Custom readline-style input handler (no Console.ReadLine())
- Persistent command history with HISTFILE support
- I/O redirection with automatic directory creation
- Cross-platform PATH resolution (Windows PATHEXT vs Unix permissions)
- DI architecture - every command is a registered service

The codebase shows how dependency injection works in unconventional places. Most shells use globals. This one doesn't.

Full source + architecture diagrams on GitHub.

🔗 [Link to GitHub repo]

---

## Notes for Posting

**Choose one version** based on your audience:
- **First version**: Best for broad technical audience, tells a story
- **Second version**: Shorter, punchier, better for quick engagement
- **Third version**: Most technical, good for developer-focused audience

**Best practices:**
- Post during weekday mornings (9-11am) for best engagement
- Add 2-3 relevant hashtags: #CSharp #DotNet #SoftwareEngineering
- Reply to comments within first hour to boost algorithm
- Consider adding a demo GIF or screenshot as the first comment
- Tag CodeCrafters if appropriate: @CodeCrafters

**Call to action:**
- Replace `[Link to GitHub repo]` with your actual GitHub URL
- Consider pinning your best demo GIF as first comment
- Could also share blog post link in comments after initial engagement
