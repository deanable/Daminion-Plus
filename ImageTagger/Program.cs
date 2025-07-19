namespace ImageTagger;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        // Set up global exception handling
        Application.ThreadException += Application_ThreadException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }

    private static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
    {
        // Log the exception
        var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        if (!Directory.Exists(logPath))
        {
            Directory.CreateDirectory(logPath);
        }
        
        var logFile = Path.Combine(logPath, $"UnhandledException_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
        var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] Unhandled Thread Exception: {e.Exception.Message}\nStack Trace: {e.Exception.StackTrace}";
        
        try
        {
            File.AppendAllText(logFile, logMessage + Environment.NewLine);
        }
        catch
        {
            // If we can't log, at least show the error
        }

        MessageBox.Show($"An unexpected error occurred:\n{e.Exception.Message}\n\nCheck the logs for more details.", 
            "Unexpected Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        if (exception != null)
        {
            // Log the exception
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logPath))
            {
                Directory.CreateDirectory(logPath);
            }
            
            var logFile = Path.Combine(logPath, $"UnhandledException_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");
            var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [FATAL] Unhandled Domain Exception: {exception.Message}\nStack Trace: {exception.StackTrace}";
            
            try
            {
                File.AppendAllText(logFile, logMessage + Environment.NewLine);
            }
            catch
            {
                // If we can't log, at least show the error
            }

            MessageBox.Show($"A critical error occurred:\n{exception.Message}\n\nCheck the logs for more details.", 
                "Critical Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}