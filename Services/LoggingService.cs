using System;
using System.IO;

namespace DesktopTaskAid.Services
{
    public static class LoggingService
    {
        private static readonly string _logFilePath;
        private static readonly object _lockObject = new object();

        static LoggingService()
        {
            try
            {
                var logFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "DesktopTaskAid"
                );

                if (!Directory.Exists(logFolder))
                {
                    Directory.CreateDirectory(logFolder);
                }

                _logFilePath = Path.Combine(logFolder, $"app_log_{DateTime.Now:yyyyMMdd}.txt");
                
                // Write startup message directly to avoid calling Log() during static initialization
                try
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var startupMessage = $"[{timestamp}] [INFO] === APPLICATION STARTED ==={Environment.NewLine}";
                    File.AppendAllText(_logFilePath, startupMessage);
                }
                catch
                {
                    // If initial log write fails, continue anyway
                }
            }
            catch
            {
                // If static constructor fails, set a fallback path
                _logFilePath = Path.Combine(Path.GetTempPath(), $"DesktopTaskAid_log_{DateTime.Now:yyyyMMdd}.txt");
            }
        }

        public static void Log(string message, string category = "INFO")
        {
            try
            {
                lock (_lockObject)
                {
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logEntry = $"[{timestamp}] [{category}] {message}";
                    
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                    
                    // Also write to Debug output
                    System.Diagnostics.Debug.WriteLine(logEntry);
                }
            }
            catch
            {
                // If logging fails, try to write to a fallback location
                try
                {
                    var fallbackPath = Path.Combine(Path.GetTempPath(), "DesktopTaskAid_error.txt");
                    File.AppendAllText(fallbackPath, $"{DateTime.Now}: LOGGING FAILED - {message}{Environment.NewLine}");
                }
                catch
                {
                    // If even fallback fails, we can't do much
                }
            }
        }

        public static void LogError(string message, Exception ex = null)
        {
            var errorMessage = message;
            if (ex != null)
            {
                errorMessage += $"{Environment.NewLine}Exception: {ex.GetType().Name}{Environment.NewLine}Message: {ex.Message}{Environment.NewLine}StackTrace: {ex.StackTrace}";
                
                if (ex.InnerException != null)
                {
                    errorMessage += $"{Environment.NewLine}InnerException: {ex.InnerException.Message}";
                }
            }
            Log(errorMessage, "ERROR");
        }

        public static string GetLogFilePath()
        {
            return _logFilePath;
        }
    }
}
