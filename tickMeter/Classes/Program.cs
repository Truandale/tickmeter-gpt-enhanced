using Microsoft.Diagnostics.Tracing.Analysis;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using tickMeter.Classes;
using tickMeter.Forms;

namespace tickMeter
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            LogProcessSnapshot();

            int curId = Process.GetCurrentProcess().Id;
            Process[] instances = Process.GetProcessesByName("tickmeter");
            foreach(Process proc in instances)
            {
                if(proc.Id != curId)
                {
                    Application.Exit();
                    return;
                }
            }
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GUI());
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            MessageBox.Show(e.Message);
        }

        private static void LogProcessSnapshot()
        {
            try
            {
                var processes = Process.GetProcesses();
                try
                {
                    var snapshot = processes
                        .Select(p =>
                        {
                            string name;
                            try
                            {
                                name = p.ProcessName;
                            }
                            catch
                            {
                                name = string.Empty;
                            }

                            return new
                            {
                                p.Id,
                                Name = string.IsNullOrWhiteSpace(name) ? "<unknown>" : name
                            };
                        })
                        .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(p => p.Id)
                        .ToList();

                    DebugLogger.log($"[Startup] Process snapshot captured: count={snapshot.Count}");

                    foreach (var entry in snapshot)
                    {
                        DebugLogger.log($"[Startup] PID={entry.Id} Name={entry.Name}");
                    }
                }
                finally
                {
                    foreach (var process in processes)
                    {
                        try { process.Dispose(); }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[Startup] Failed to enumerate processes: {ex.Message}");
            }
        }
    }
}
