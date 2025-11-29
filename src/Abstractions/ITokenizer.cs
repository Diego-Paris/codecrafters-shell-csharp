namespace MiniShell.Abstractions;

public interface ITokenizer
{
    List<string> Tokenize(string input);
}
