namespace ImageTagger.Core.Interfaces;

public enum LogLevel
{
    Debug,
    Info,
    Warning,
    Error
}

public interface ILoggingService
{
    void Log(string message, LogLevel level = LogLevel.Info);
    void LogException(Exception ex, string context = "");
    void LogPerformance(string operation, TimeSpan duration);
    string LogFilePath { get; }
} 