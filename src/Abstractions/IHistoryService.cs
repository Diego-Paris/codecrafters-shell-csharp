namespace MiniShell.Abstractions;

public interface IHistoryService
{
    void LoadFromFile();
    void SaveToFile();
    void AppendNewCommandsToFile();
}
