using System;
using System.IO;
using System.Threading.Tasks;

namespace tickMeter.Classes
{
    public class DebugLogger
    {
        private DebugLogger() {
        }

        public static DebugLogger Instance { get; private set; } = new DebugLogger();
        private static readonly object _logLock = new object();
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");

        static DebugLogger()
        {
            try
            {
                // Truncate the log file once per application start so every run begins fresh
                File.WriteAllText(LogFilePath, string.Empty);
                PurgeAdditionalLogs();
            }
            catch
            {
                // Ignore file access issues; logging will still attempt append later
            }
        }

        private static void PurgeAdditionalLogs()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                // Clear auxiliary per-session log file if present
                string liveLogPath = Path.Combine(baseDir, "tickmeter-live.log");
                if (File.Exists(liveLogPath))
                {
                    File.WriteAllText(liveLogPath, string.Empty);
                }

                string logsDir = Path.Combine(baseDir, "logs");
                if (Directory.Exists(logsDir))
                {
                    foreach (string file in Directory.GetFiles(logsDir, "*", SearchOption.TopDirectoryOnly))
                    {
                        // Remove stale server stats or tick snapshots regardless of extension
                        try
                        {
                            File.Delete(file);
                        }
                        catch
                        {
                            // Fall back to truncation if deletion fails (e.g., locked file)
                            try { File.WriteAllText(file, string.Empty); } catch { }
                        }
                    }
                }
            }
            catch
            {
                // Suppress errors - logging should not break application startup
            }
        }

        public static void log(String message)
        {
            // Проверяем настройку включения логов
            bool isEnabled = IsLoggingEnabled();
            
            // ДИАГНОСТИКА: всегда пишем первое сообщение для проверки
            if (message.Contains("[LOGGER-TEST]"))
            {
                try
                {
                    lock (_logLock)
                    {
                        using (StreamWriter sw = new StreamWriter(LogFilePath, true))
                        {
                            sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + $" (IsLoggingEnabled={isEnabled})");
                        }
                    }
                }
                catch { }
                return;
            }
            
            if (!isEnabled)
                return;

            try
            {
                lock (_logLock)
                {
                    using (StreamWriter sw = new StreamWriter(LogFilePath, true))
                    {
                        sw.WriteLine(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// Проверяет, включено ли текстовое логирование в настройках
        /// </summary>
        private static bool IsLoggingEnabled()
        {
            try
            {
                return App.settingsManager?.GetOption("enable_text_logs", "True", "ADVANCED") == "True";
            }
            catch
            {
                // В случае ошибки - возвращаем true для обратной совместимости
                return true;
            }
        }
        public static async void log(String[] messages)
        {
            if (!IsLoggingEnabled())
                return;

            await Task.Run(() =>
            {
                foreach (String message in messages)
                {
                    log(message);
                }
            });
        }

        public static async void log(Exception ex)
        {
            if (!IsLoggingEnabled())
                return;

            await Task.Run(() =>
            {
                log(ex.Message);
                log(ex.StackTrace);
            });
        }
    }
}
