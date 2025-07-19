using ImageTagger.Core.Interfaces;
using System.Text;

namespace ImageTagger.Infrastructure;

public class FileLoggingService : ILoggingService
{
    private readonly string _logFilePath;
    private readonly object _lockObject = new();

    public string LogFilePath => _logFilePath;

    public FileLoggingService(string? logFilePath = null)
    {
        _logFilePath = logFilePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ImageTagger.log");
    }

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
            
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
            }

            // Also log to debug output for development
            System.Diagnostics.Debug.WriteLine(logEntry);
        }
        catch (Exception ex)
        {
            // Fallback to console if file logging fails
            Console.WriteLine($"Logging failed: {ex.Message}");
            Console.WriteLine($"Original message: {message}");
        }
    }

    public void LogException(Exception ex, string context = "")
    {
        var contextPrefix = string.IsNullOrEmpty(context) ? "" : $"[{context}] ";
        Log($"{contextPrefix}Exception: {ex.Message}", LogLevel.Error);
        Log($"{contextPrefix}Stack Trace: {ex.StackTrace}", LogLevel.Error);
        
        if (ex.InnerException != null)
        {
            Log($"{contextPrefix}Inner Exception: {ex.InnerException.Message}", LogLevel.Error);
        }
    }

    public void LogPerformance(string operation, TimeSpan duration)
    {
        Log($"Performance: {operation} took {duration.TotalMilliseconds:F2}ms", LogLevel.Info);
    }
} 