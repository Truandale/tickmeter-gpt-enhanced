using Microsoft.Diagnostics.Tracing.Analysis;
using System;
using System.Diagnostics;
using System.Windows.Forms;
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
            
            // Игнорируем DisconnectedContext и другие COM-связанные ошибки
            if (e is System.Runtime.InteropServices.COMException comEx)
            {
                // Логируем COM ошибки, но не показываем пользователю
                System.Diagnostics.Debug.WriteLine($"COM Exception ignored: {comEx.Message} (HRESULT: 0x{comEx.HResult:X})");
                return;
            }
            
            // Показываем только критичные ошибки
            MessageBox.Show(e.Message);
        }
    }
}
