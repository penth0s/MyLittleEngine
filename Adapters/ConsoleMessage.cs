namespace Adapters;

public enum LogType
{
    Log,
    Warning,
    Error
}

public class ConsoleMessage(string message, LogType type)
{
    public string Message { get; } = message;
    public LogType Type { get; } = type;
    public DateTime Timestamp { get; } = DateTime.Now;
    public int Count { get; set; } = 1; 
}