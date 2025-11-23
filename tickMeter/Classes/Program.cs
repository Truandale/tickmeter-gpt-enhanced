using Microsoft.Diagnostics.Tracing.Analysis;
using System;
using System.Diagnostics;
using System.IO;
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
            
            // Проверяем и обновляем путь автозагрузки если программа была перемещена
            CheckAndUpdateAutoStartPath();
            
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

        static void CheckAndUpdateAutoStartPath()
        {
            try
            {
                // Проверяем включена ли автозагрузка
                string autoStartEnabled = SettingsManager.ReadOptionDirect("run_on_startup", "SETTINGS");
                if (autoStartEnabled != "True")
                {
                    return; // Автозагрузка не включена, ничего не делаем
                }

                string currentPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string taskName = "tickMeter_AutoStart";
                
                // Получаем информацию о существующей задаче
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = $"/Query /TN \"{taskName}\" /FO LIST /V",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                Process process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    // Ищем строку с путем к задаче
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    string taskPath = null;
                    
                    foreach (var line in lines)
                    {
                        if (line.Contains("Task To Run:") || line.Contains("Задача для запуска:"))
                        {
                            taskPath = line.Split(':')[1].Trim().Trim('"');
                            break;
                        }
                    }

                    // Если путь изменился - обновляем задачу
                    if (taskPath != null && !taskPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Print($"[AutoStart] Путь изменился: {taskPath} -> {currentPath}");
                        
                        // Создаем XML для задачи с отключенными условиями
                        string xmlPath = Path.Combine(Path.GetTempPath(), "tickMeter_task_update.xml");
                        string taskXml = $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.2"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <RegistrationInfo>
    <Description>tickMeter автозагрузка с правами администратора</Description>
  </RegistrationInfo>
  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
    </LogonTrigger>
  </Triggers>
  <Principals>
    <Principal id=""Author"">
      <LogonType>InteractiveToken</LogonType>
      <RunLevel>HighestAvailable</RunLevel>
    </Principal>
  </Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <AllowHardTerminate>true</AllowHardTerminate>
    <StartWhenAvailable>true</StartWhenAvailable>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
    <IdleSettings>
      <StopOnIdleEnd>false</StopOnIdleEnd>
      <RestartOnIdle>false</RestartOnIdle>
    </IdleSettings>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <Enabled>true</Enabled>
    <Hidden>false</Hidden>
    <RunOnlyIfIdle>false</RunOnlyIfIdle>
    <WakeToRun>false</WakeToRun>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    <Priority>7</Priority>
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>{currentPath}</Command>
    </Exec>
  </Actions>
</Task>";

                        File.WriteAllText(xmlPath, taskXml, System.Text.Encoding.Unicode);
                        
                        // Пересоздаем задачу из XML
                        ProcessStartInfo updatePsi = new ProcessStartInfo
                        {
                            FileName = "schtasks",
                            Arguments = $"/Create /TN \"{taskName}\" /XML \"{xmlPath}\" /F",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };

                        Process updateProcess = Process.Start(updatePsi);
                        updateProcess.WaitForExit();

                        // Удаляем временный XML
                        try { File.Delete(xmlPath); } catch { }

                        if (updateProcess.ExitCode == 0)
                        {
                            Debug.Print("[AutoStart] Путь успешно обновлен в планировщике");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[AutoStart] Ошибка проверки/обновления пути: {ex.Message}");
                // Не показываем ошибку пользователю, это фоновая проверка
            }
        }
    }
}
