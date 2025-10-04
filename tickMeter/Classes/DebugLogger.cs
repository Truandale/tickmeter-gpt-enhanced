using System;
using System.Diagnostics;
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
        private const string LogFileName = "tickmeter-live.log";
        private static readonly string LogFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, LogFileName);
        private static bool _sessionStarted;

        public static string CurrentLogPath => LogFilePath;

        public static void StartNewSession()
        {
            lock (_logLock)
            {
                try
                {
                    using (var stream = new FileStream(LogFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [Startup] Log session initialized (pid={Process.GetCurrentProcess().Id})");
                    }
                    _sessionStarted = true;
                }
                catch
                {
                    // ignored
                }
            }
        }

        public static void log(String message)
        {
            try
            {
                if (!_sessionStarted)
                {
                    StartNewSession();
                }
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
        public static async void log(String[] messages)
        {
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
            await Task.Run(() =>
            {
                log($"Exception: {ex.GetType().FullName} {ex.Message}");
                if (!string.IsNullOrWhiteSpace(ex.StackTrace))
                {
                    log(ex.StackTrace);
                }
            });
        }
    }
}
