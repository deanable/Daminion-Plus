using ImageTagger.Core.Interfaces;
using System.Text;

namespace ImageTagger.Infrastructure;

public class FileLoggingService : ILoggingService
{
    private readonly string _logFilePath;
    private readonly object _lockObject = new();
    private readonly LogLevel _minimumLogLevel;
    private readonly bool _logToConsole;

    public string LogFilePath => _logFilePath;

    public FileLoggingService(string? logFilePath = null, LogLevel minimumLogLevel = LogLevel.Info, bool logToConsole = false)
    {
        if (logFilePath != null)
        {
            _logFilePath = logFilePath;
        }
        else
        {
            // Create logs directory in application directory
            var appDirectory = AppDomain.CurrentDomain.BaseDirectory;
            var logsDirectory = Path.Combine(appDirectory, "Logs");
            
            // Ensure logs directory exists
            if (!Directory.Exists(logsDirectory))
            {
                Directory.CreateDirectory(logsDirectory);
            }
            
            // Clean up old log files (keep last 30 days)
            CleanupOldLogFiles(logsDirectory);
            
            // Create log file with date and time in filename for each run
            var dateTimeStamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            var logFileName = $"ImageTagger_{dateTimeStamp}.log";
            _logFilePath = Path.Combine(logsDirectory, logFileName);
        }
        
        _minimumLogLevel = minimumLogLevel;
        _logToConsole = logToConsole;
        
        // Log the log file location and session start
        var sessionId = Guid.NewGuid().ToString("N")[..8]; // Short session ID
        var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Info] ===== SESSION START - Session ID: {sessionId} =====";
        var logFileEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [Info] Logging initialized - Log file: {_logFilePath}";
        
        try
        {
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
            File.AppendAllText(_logFilePath, logFileEntry + Environment.NewLine, Encoding.UTF8);
        }
        catch
        {
            // If we can't write to the log file, just continue
        }
    }

    private void CleanupOldLogFiles(string logsDirectory)
    {
        try
        {
            var cutoffDate = DateTime.Now.AddDays(-30); // Keep last 30 days
            var logFiles = Directory.GetFiles(logsDirectory, "ImageTagger_*.log");
            
            foreach (var logFile in logFiles)
            {
                var fileInfo = new FileInfo(logFile);
                if (fileInfo.CreationTime < cutoffDate)
                {
                    File.Delete(logFile);
                    Console.WriteLine($"Cleaned up old log file: {Path.GetFileName(logFile)}");
                }
            }
        }
        catch
        {
            // If cleanup fails, just continue - don't break logging
        }
    }

    public void Log(string message, LogLevel level = LogLevel.Info)
    {
        // Check if we should log this level
        if (level < _minimumLogLevel)
            return;

        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}";
            
            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine, Encoding.UTF8);
            }

            // Console logging if enabled
            if (_logToConsole)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = level switch
                {
                    LogLevel.Error => ConsoleColor.Red,
                    LogLevel.Warning => ConsoleColor.Yellow,
                    LogLevel.Debug => ConsoleColor.Gray,
                    _ => ConsoleColor.White
                };
                Console.WriteLine(logEntry);
                Console.ForegroundColor = originalColor;
            }

            // Always log to debug output for development
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

    public void LogVerbose(string message)
    {
        Log(message, LogLevel.Debug);
    }

    public void LogMethodEntry(string methodName, params (string name, object? value)[] parameters)
    {
        var paramString = string.Join(", ", parameters.Select(p => $"{p.name}={p.value}"));
        Log($"Entering {methodName}({paramString})", LogLevel.Debug);
    }

    public void LogMethodExit(string methodName, object? result = null)
    {
        var resultString = result != null ? $" -> {result}" : "";
        Log($"Exiting {methodName}{resultString}", LogLevel.Debug);
    }
} 