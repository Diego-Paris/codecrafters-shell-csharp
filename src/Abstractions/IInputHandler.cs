namespace MiniShell.Abstractions;

/// <summary>
/// Provides abstraction for reading user input with support for line editing and command completion.
/// </summary>
public interface IInputHandler
{
    /// <summary>
    /// Reads a line of input from the user with the specified prompt.
    /// </summary>
    /// <param name="prompt">The prompt to display to the user.</param>
    /// <returns>The line entered by the user, or null on EOF.</returns>
    string? ReadInput(string prompt);
}
