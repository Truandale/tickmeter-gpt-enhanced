using Microsoft.Diagnostics.Tracing.Analysis;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using tickMeter.Forms;
using tickMeter.Classes;

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
            
            // Добавляем обработчик завершения для очистки ресурсов
            Application.ApplicationExit += Application_ApplicationExit;
            currentDomain.ProcessExit += CurrentDomain_ProcessExit;
            
            // КРИТИЧНО: Проверяем наличие settings.ini перед запуском GUI
            // Если файл отсутствует - создаем его с оптимальными настройками
            SettingsManager.EnsureSettingsFileExists();
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GUI());
        }

        static void Application_ApplicationExit(object sender, EventArgs e)
        {
            try
            {
                RealProcessTrafficMonitor.DisposeAll();
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[Program] Error during application exit cleanup: {ex.Message}");
            }
        }

        static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            try
            {
                RealProcessTrafficMonitor.DisposeAll();
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[Program] Error during process exit cleanup: {ex.Message}");
            }
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            MessageBox.Show(e.Message);
        }
    }
}
