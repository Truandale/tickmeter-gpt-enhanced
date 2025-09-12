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

        public static void log(String message)
        {
            try
            {
                lock (_logLock)
                {
                    using (StreamWriter sw = new StreamWriter("debug.log", true))
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
                log(ex.Message);
                log(ex.StackTrace);
            });
        }
    }
}
