using RTSS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.NetworkInformation;

namespace tickMeter.Classes
{
    public static class RivaTuner
    {
        public static string rtss_exe = @"C:\Program Files (x86)\RivaTuner Statistics Server\RTSS.exe";
        static TickMeterState meterState;
        public static string LabelColor;
        public static string ColorBad;
        public static string ColorMid;
        public static string ColorGood;
        public static string ColorChart;
        public static Process RtssInstance;
        static OSD osd;
        public static string RivaOutput;
        public static uint chartOffset = 0;

        public static string DrawChart(
            float[] graphData,
            int min = 0,
            int max = 0,
            string label = "",
            string value = "",
            string valueColor = ""
        )
        {
            if (!VerifyRiva()) return "";
            if (osd == null)
            {
                try
                {
                    osd = new OSD("TickMeter");
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[RivaTuner] Не удалось создать OSD: {ex.Message}");
                    return "";
                }
            }
            if(graphData.Length < 512)
            {
                float[] tmp = new float[512];
                graphData.CopyTo(tmp,0);
                graphData = tmp;
            }
            uint chartSize;
            // Исправление: скорость движения графика зависит от того, как часто добавляются новые значения в graphData (pingBuffer)
            // Если pingBuffer обновляется чаще (например, раз в 100 мс), но BuildRivaOutput вызывается редко (например, раз в 500 мс),
            // то график будет двигаться медленно, потому что RTSS видит только новые точки при каждом вызове BuildRivaOutput.

            // Чтобы график двигался с той же скоростью, что и новые значения пинга:
            // 1. Убедитесь, что таймер, вызывающий BuildRivaOutput (обычно ticksLoop.Interval), тоже равен 100 мс.
            // 2. Если нужно, уменьшите ticksLoop.Interval в GUI.cs:
            //    ticksLoop.Interval = 100;

            if(max == 0)
            {
                max = 60;
                if (graphData.Max() > 62)
                {
                    max = 90;
                }
                if (graphData.Max() > 92)
                {
                    max = 120;
                }
                if (graphData.Max() > 132)
                {
                    max = 180;
                }
                if (graphData.Max() > 192)
                {
                    max = 250;
                }
            }
            
            unsafe
            {
                fixed (float* lpBuffer = graphData)
                {
                    try
                    {
                        chartSize = osd.EmbedGraph(chartOffset, lpBuffer: lpBuffer, dwBufferPos: 0, 512, dwWidth: -24, dwHeight: -3, dwMargin: 1, fltMin: min, fltMax: max, dwFlags: 0);
                    } catch (Exception e) {
                        chartSize = 3;
                        DebugLogger.log(e);
                    }
                }
                string chartEntry = "<C4><S2>" + max + "<OBJ=" + chartOffset.ToString("X8") + "><C>";
                if (!string.IsNullOrEmpty(value))
                {
                    // Только цветное значение без подписи
                    chartEntry += $" {valueColor}{value}<C>";
                }
                chartOffset += chartSize;
                return chartEntry;
            }
        }

        public static void Print(string text)
        {
            if (!VerifyRiva()) return;
            if (osd == null)
            {
                osd = new OSD("TickMeter");
            }
            osd.Update(text);
        }

        static RivaTuner()
        {
            try
            {
                // Проверяем наличие RTSS.dll перед любыми операциями
                string rtssPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RTSS.dll");
                if (!File.Exists(rtssPath))
                {
                    DebugLogger.log("[RivaTuner] RTSS.dll не найдена в папке программы. RivaTuner оверлей будет отключен.");
                    return;
                }

                if (!VerifyRiva()) return;
                if (!IsRivaRunning())
                {
                    RunRiva();
                } else
                {
                    osd = new OSD("TickMeter");
                }
            }
            catch (System.IO.FileNotFoundException ex)
            {
                DebugLogger.log($"[RivaTuner] Не найдена зависимость RTSS.dll: {ex.Message}");
                DebugLogger.log("[RivaTuner] Возможно отсутствует Visual C++ Redistributable. Оверлей будет отключен.");
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[RivaTuner] Ошибка инициализации: {ex.Message}");
                DebugLogger.log("[RivaTuner] RivaTuner оверлей будет отключен.");
            }
        }

        public static bool IsRivaRunning()
        {
            Process[] pname = Process.GetProcessesByName("RTSS");
            if (pname.Length == 0)
                return false;
            else
                return true;
        }

        public static bool VerifyRiva()
        {
           return File.Exists(rtss_exe);
        }

        public static void RunRiva()
        {
            FileInfo f = new FileInfo(rtss_exe);
            if (VerifyRiva())
            {
                try
                {
                    RtssInstance = Process.Start(f.FullName);
                    Thread.Sleep(2000);
                    hasToKillRtssFlag = true;
                }
                catch (Exception ex)
                {
                    DebugLogger.log(ex);
                }
            }
        }

        public static Boolean hasToKillRtssFlag = false;

        public static void KillRtss()
        {
            if (RtssInstance == null || !hasToKillRtssFlag) return;
            try
            {
                RtssInstance.Kill();
                Process[] proc = Process.GetProcessesByName("RTSSHooksLoader64");
                proc[0].Kill();
            }
            catch (Exception ex) {
                DebugLogger.log(ex);
            }
            
        }

        public static string TextFormat()
        {
            // ChatGPT Enhancement: Debug color values
            Console.WriteLine($"[RTSS] Colors: Good={ColorGood}, Mid={ColorMid}, Bad={ColorBad}");
            return "<C0=" + LabelColor + "><C1=" + ColorBad+ "><C2=" + ColorMid + "><C3=" + ColorGood + "><C4="+ColorChart+"><S0=47><S1=65><S2=55><A0=-2><A1=2>";
        }

        public static string FormatTickrate()
        {
            string tickRateStr = "<S><C0>Tickrate: ";
            
            // === ChatGPT ENHANCED: Zone from smoothed display value ===
            var snap = Classes.UnifiedDataSource.Snapshot();
            var profile = App.settingsManager.GetColorZoneProfile();
            var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
            
            // === ПОЛУЧАЕМ ЗНАЧЕНИЕ: из кэша GUI (если sync=true) или через независимое сглаживание ===
            int rawTickrateOverlay = meterState.OutputTickRate;
            int displayTickrate = Classes.SmoothingManager.GetCachedSmoothedTickrate(rawTickrateOverlay);
            
            // DEBUG: Log overlay smoothing for verification
            string mode = Classes.SmoothingManager.IsTickrateOverlaySyncWithGui() ? "synced with GUI" : "independent";
            DebugLogger.log($"[OVERLAY-TICKRATE] Raw={rawTickrateOverlay} -> Smoothed={displayTickrate} ({mode})");
            
            // Calculate zone from SMOOTHED display value, not raw snapshot
            var tickrateZone = zoner.FromTickrate(displayTickrate);
            string tickrateColor = Classes.ZoneColors.ToRtssLegacy(tickrateZone);
            
            tickRateStr += tickrateColor + displayTickrate.ToString();
            
            // Добавляем индикатор спайка для tickrate
            bool showTickrateSpikes = App.settingsManager?.GetOption("show_tickrate_spikes", "True", "ADVANCED") == "True";
            if (showTickrateSpikes && App.meterState.HasTickRateSpike)
            {
                tickRateStr += $" {tickrateColor}(!)";
            }
            
            // Добавляем ticktime рядом с tickrate
            tickRateStr += " <S0><C0>/ ";
            
            // ВАЖНО: Ticktime обратно пропорционален tickrate, поэтому используем ТОТ ЖЕ цвет
            // Tickrate↑ (зеленый) → Ticktime↓ (тоже зеленый) - оба показывают ХОРОШЕЕ состояние
            // Tickrate↓ (красный) → Ticktime↑ (тоже красный) - оба показывают ПЛОХОЕ состояние
            // Ticktime = 1000 / Tickrate, поэтому цветовая зона ОБРАТНО ПРОПОРЦИОНАЛЬНА
            
            // СИНХРОНИЗАЦИЯ: Рассчитываем ticktime из того же displayTickrate, который уже был сглажен выше
            // Это гарантирует синхронизацию значений tickrate и ticktime в оверлее
            float ticktimeValue = displayTickrate > 0 ? (1000.0f / displayTickrate) : 0f;
            string ticktimeDisplay = ticktimeValue > 0 ? ticktimeValue.ToString("0.0") : "n/a";
            
            tickRateStr += tickrateColor + ticktimeDisplay + " <S0>ms";
            
            // Добавляем индикатор спайка для ticktime (того же цвета что и tickrate)
            bool showTicktimeSpikes = App.settingsManager?.GetOption("show_ticktime_spikes", "True", "ADVANCED") == "True";
            if (showTicktimeSpikes && App.meterState.HasTickTimeSpike)
            {
                tickRateStr += $" {tickrateColor}(!)";
            }
            
            string output = tickRateStr + "<C>" + Environment.NewLine;
            return output;
        }

        public static string FormatServer()
        {
            return "<S><C0>IP: <C>" + meterState.Server.Ip + Environment.NewLine;
        }

        public static string FormatTraffic()
        {
            float formatedUpload = (float)meterState.UploadTraffic / (1024 * 1024);
            float formatedDownload = (float)meterState.DownloadTraffic / (1024 * 1024);
            
            // Применяем сглаживание для overlay значений трафика, если включено
            float displayUpload = Classes.SmoothingManager.SmoothUploadMbOverlay(formatedUpload);
            float displayDownload = Classes.SmoothingManager.SmoothDownloadMbOverlay(formatedDownload);
            
            return "<S><C0>UP/DL: <C>" + displayUpload.ToString("N2") + " / " + displayDownload.ToString("N2") + "<S0> Mb" + Environment.NewLine;
        }

        public static string FormatDrops()
        {
            string dropsStr = "<S><C0>Drops: ";
            float drops = meterState.GetDropsNumber();
            string dropsColor;
            
            if (drops > 5)
            {
                dropsColor = "<C1>"; // Bad - use palette red
            }
            else if (drops > 1)
            {
                dropsColor = "<C2>"; // Mid - use palette yellow
            }
            else
            {
                // Good - use palette green color (ColorGood setting)
                dropsColor = "<C3>";
            }
            
            dropsStr += dropsColor + meterState.GetDrops() + "%" + "<C>";

            return dropsStr + Environment.NewLine;
        }

        public static string FormatTime()
        {
            TimeSpan result = DateTime.Now.Subtract(App.meterState.SessionStart);
            string Duration = result.ToString("mm':'ss");
            return "<S><C0>Time: <C>" + Duration + Environment.NewLine;
        }

        public static string FormatPing()
        {
            // === ChatGPT ENHANCED: Zone from smoothed display value ===
            var snap = Classes.UnifiedDataSource.Snapshot();
            var profile = App.settingsManager.GetColorZoneProfile(); 
            var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
            
            string pingValue = "";
            string geo = meterState.Server.Location;
            string pingFont;
            
            // Format display value with smoothing FIRST
            if (snap.PingAvgMs > 0)
            {
                // === ПОЛУЧАЕМ ЗНАЧЕНИЕ: из кэша GUI (если sync=true) или через независимое сглаживание ===
                int rawPingOverlay = (int)snap.PingAvgMs;
                int smoothedPing = Classes.SmoothingManager.GetCachedSmoothedPing(rawPingOverlay);
                pingValue = smoothedPing.ToString();
                
                // DEBUG: Log overlay smoothing for verification
                string mode = Classes.SmoothingManager.IsPingOverlaySyncWithGui() ? "synced with GUI" : "independent";
                DebugLogger.log($"[OVERLAY-PING] Raw={rawPingOverlay} -> Smoothed={smoothedPing} ({mode})");
                
                // Calculate zone from SMOOTHED display value, not raw snapshot
                var pingZone = zoner.FromPing(smoothedPing);
                pingFont = Classes.ZoneColors.ToRtssLegacy(pingZone);
            }
            else
            {
                pingValue = "n/a";
                pingFont = "<C1>"; // Red for n/a
            }
            
            // Добавляем индикатор спайка если включена соответствующая настройка
            bool showSpikeIndicator = App.settingsManager?.GetOption("show_ping_spikes", "True", "ADVANCED") == "True";
            string spikeIndicator = "";
            if (showSpikeIndicator)
            {
                Debug.Print($"[RTSS] Spike check: HasPingSpike={meterState.Server.HasPingSpike}, ShowSetting={showSpikeIndicator}");
                if (meterState.Server.HasPingSpike)
                {
                    // Используем тот же цвет что и ping значение (сохраняем цвет зоны)
                    spikeIndicator = $" {pingFont}(!)";
                    Debug.Print($"[RTSS] Spike indicator added to overlay with zone color");
                }
            }
            
            // ChatGPT Enhancement: Add diagnostic line for testing (temporary)
            string diagnostic = "";
            bool showDiagnostics = App.settingsManager?.GetOption("debug_zone_diagnostics", "False", "ADVANCED") == "True";
            if (showDiagnostics)
            {
                diagnostic = Environment.NewLine + "<C0><S0>Diag RTSS: " + zoner.GetDiagnostic(snap) + "<C>";
            }
            
            return "<S><C0>Ping: " + pingFont + pingValue + " <S0>ms" + spikeIndicator + "<C> <C0>(" + geo + ")" + diagnostic + Environment.NewLine;
        }

        private static int _buildRivaOutputInProgress = 0; // Interlocked flag

        public static void BuildRivaOutput()
        {
            // Anti-reentrancy protection with Interlocked for thread-safety
            bool antiReentrancy = App.settingsManager?.GetOption("anti_reentrancy", "True", "ADVANCED") == "True";
            if (antiReentrancy && System.Threading.Interlocked.Exchange(ref _buildRivaOutputInProgress, 1) == 1)
            {
                return; // Already in progress - prevent reentrant call
            }

            try
            {
                
                string output = "";
                var state = App.meterState;

                if (!HasActiveMetrics(state))
                {
                    PrintData(FormatNoTrafficPlaceholder(), true);
                    return;
                }

                chartOffset = 0;
                meterState = state;
            if(App.settingsForm.settings_tickrate_show.Checked)
            {
                output += FormatTickrate();
            }

            if (App.settingsForm.settings_ip_checkbox.Checked)
            {
                output += FormatServer();
            }

            // Используем FormatPing с UDP приоритетом
            if (App.settingsForm.settings_ping_checkbox.Checked)
            {
                output += FormatPing();
            }
            if (App.settingsForm.settings_traffic_checkbox.Checked)
            {
                output += FormatTraffic();
            }
            if (App.settingsForm.settings_session_time_checkbox.Checked)
            {
                output += FormatTime();
            }
            if (App.settingsForm.packet_drops_checkbox.Checked)
            {
                output += FormatDrops();
            }
            
            // Добавляем рейтинг качества сети если включена соответствующая галка
            bool showNetworkQuality = App.settingsManager?.GetOption("network_quality_overlay", "False", "SETTINGS") == "True";
            if (showNetworkQuality)
            {
                output += FormatNetworkQuality();
            }
            if (App.settingsForm.settings_chart_checkbox.Checked)
            {
                // === ChatGPT ENHANCED: Zone from smoothed display value for chart ===
                var snap = Classes.UnifiedDataSource.Snapshot();
                var profile = App.settingsManager.GetColorZoneProfile();
                var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
                
                // СИНХРОНИЗАЦИЯ: Используем тот же метод что и в FormatTickrate() для получения значения
                int rawTickrateForGraph = App.meterState.OutputTickRate;
                int tickrateValue = Classes.SmoothingManager.GetCachedSmoothedTickrate(rawTickrateForGraph);
                
                // Calculate zone from SMOOTHED display value, not raw snapshot
                var tickrateZone = zoner.FromTickrate(tickrateValue);
                string tickrateColor = Classes.ZoneColors.ToRtssLegacy(tickrateZone);

                // Добавляем индикатор спайка рядом со значением (как у пинга)
                string tickrateValueDisplay = tickrateValue > 0 ? tickrateValue.ToString() : "n/a";
                bool showTickrateSpikes = App.settingsManager?.GetOption("show_tickrate_spikes", "True", "ADVANCED") == "True";
                if (showTickrateSpikes && App.meterState.HasTickRateSpike)
                {
                    // Используем тот же цвет что и значение тикрейта (сохраняем цвет зоны)
                    tickrateValueDisplay += $" {tickrateColor}(!)";
                }
                
                output += "<S0><C4>Tickrate" + Environment.NewLine;
                
                // Применяем сглаживание графика тикрейта, если включено
                float[] tickrateGraphData = Classes.SmoothingManager.SmoothSeries(
                    App.meterState.tickrateBuffer.ToArray(),
                    Classes.SmoothingManager.IsTickrateGraphOverlayEnabled()
                );
                
                // Debug: логируем данные графика тикрейта для RTSS
                if (tickrateGraphData.Length > 0)
                {
                    int startIndex = Math.Max(0, tickrateGraphData.Length - 10);
                    var recentValues = new float[Math.Min(10, tickrateGraphData.Length)];
                    Array.Copy(tickrateGraphData, startIndex, recentValues, 0, recentValues.Length);
                    DebugLogger.log($"[RTSS TickrateGraph] Sending to RTSS: [{string.Join(", ", recentValues)}] (total points: {tickrateGraphData.Length})");
                }
                
                output += DrawChart(
                    tickrateGraphData,
                    0,
                    0,
                    "Tickrate",
                    tickrateValueDisplay,
                    tickrateColor
                ) + Environment.NewLine; // убрано дублирование <A0><S0>...
            }
            if (App.settingsForm.settings_ticktime_chart.Checked)
            {
                // === ChatGPT ENHANCED: Zone from smoothed tickrate value (inverse relationship) ===
                // ВАЖНО: Ticktime обратно пропорционален tickrate, используем ТОТ ЖЕ цвет
                var snap = Classes.UnifiedDataSource.Snapshot();
                var profile = App.settingsManager.GetColorZoneProfile();
                var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
                
                // СИНХРОНИЗАЦИЯ: Используем тот же метод что и в FormatTickrate() для получения значения
                int rawTickrateForGraph = App.meterState.OutputTickRate;
                int smoothedTickrate = Classes.SmoothingManager.GetCachedSmoothedTickrate(rawTickrateForGraph);
                var tickrateZone = zoner.FromTickrate(smoothedTickrate);
                string ticktimeColor = Classes.ZoneColors.ToRtssLegacy(tickrateZone);
                
                // СИНХРОНИЗАЦИЯ: Рассчитываем ticktime из того же smoothedTickrate для синхронности с текстом
                float ticktimeValue = smoothedTickrate > 0 ? (1000.0f / smoothedTickrate) : 0f;
                string ticktimeValueDisplay = ticktimeValue > 0 ? ticktimeValue.ToString("0.0") : "n/a";
                
                // Добавляем индикатор спайка рядом со значением (как у пинга)
                bool showTicktimeSpikes = App.settingsManager?.GetOption("show_ticktime_spikes", "True", "ADVANCED") == "True";
                if (showTicktimeSpikes && App.meterState.HasTickTimeSpike)
                {
                    // Используем тот же цвет что и tickrate (обратная пропорциональность)
                    ticktimeValueDisplay += $" {ticktimeColor}(!)";
                }
                
                output += Environment.NewLine + "<S0><C4>Ticktime" + Environment.NewLine;
                
                // Применяем сглаживание графика тиктайма, если включено
                float[] ticktimeGraphData = Classes.SmoothingManager.SmoothSeries(
                    App.meterState.tickTimeBuffer.ToArray(),
                    Classes.SmoothingManager.IsTicktimeGraphOverlayEnabled()
                );
                
                output += DrawChart(
                    ticktimeGraphData,
                    0,
                    100,
                    "Ticktime",
                    ticktimeValueDisplay,
                    ticktimeColor
                );
            }
                try
                {
                    if (App.settingsForm.settings_ping_chart.Checked && App.meterState.pingBuffer.Count() > 1)
                    {
                        // === ChatGPT ENHANCED: Zone from smoothed display value for ping chart ===
                        var snap = Classes.UnifiedDataSource.Snapshot();
                        var profile = App.settingsManager.GetColorZoneProfile();
                        var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
                        
                        // === ИСПОЛЬЗУЕМ КЭШИРОВАННОЕ ЗНАЧЕНИЕ ИЗ GUI ===
                        // Format display value WITH SMOOTHING first
                        string pingValue = "";
                        string pingColor;
                        if (snap.PingAvgMs > 0)
                        {
                            // Используем кэшированное значение для синхронизации с GUI
                            int smoothedPing = Classes.SmoothingManager.GetCachedSmoothedPing((int)snap.PingAvgMs);
                            pingValue = smoothedPing.ToString();
                            
                            // Calculate zone from SMOOTHED display value, not raw snapshot
                            var pingZone = zoner.FromPing(smoothedPing);
                            pingColor = Classes.ZoneColors.ToRtssLegacy(pingZone);
                        }
                        else
                        {
                            pingValue = "n/a";
                            pingColor = "<C1>"; // Red for n/a
                        }
                        
                        // Добавляем индикатор спайка если включена соответствующая настройка
                        bool showSpikeIndicator = App.settingsManager?.GetOption("show_ping_spikes", "True", "ADVANCED") == "True";
                        if (showSpikeIndicator && meterState.Server.HasPingSpike)
                        {
                            pingValue += " (!)";
                        }
                        
                        // ChatGPT Enhancement: Add diagnostic for ping chart (temporary)
                        bool showDiagnostics = App.settingsManager?.GetOption("debug_zone_diagnostics", "False", "ADVANCED") == "True";
                        if (showDiagnostics)
                        {
                            Console.WriteLine($"[RTSS CHART] {zoner.GetDiagnostic(snap)} | Color: {pingColor} | Value: {pingValue}");
                        }
                        
                        output += Environment.NewLine + "<S0><C4>Ping" + Environment.NewLine;
                    
                    // Применяем сглаживание к графику пинга если включено
                    float[] pingGraphData = Classes.SmoothingManager.SmoothSeries(
                        App.meterState.pingBuffer, 
                        Classes.SmoothingManager.IsPingGraphOverlayEnabled()
                    );
                    
                    output += DrawChart(
                        pingGraphData,
                        (int)pingGraphData.Min(),
                        0,
                        "Ping",
                        pingValue,
                        pingColor
                    );
                }
            } catch (InvalidOperationException) { }
            
            // Extended overlay information
            bool showActiveProcess = App.settingsManager?.GetOption("show_active_process", "False", "EXTENDED") == "True";
            if (showActiveProcess)
            {
                output += Environment.NewLine + FormatActiveProcess();
            }
            
            bool showSessionTime = App.settingsManager?.GetOption("show_session_time", "False", "EXTENDED") == "True";
            if (showSessionTime)
            {
                output += Environment.NewLine + FormatSessionTime();
            }
            
            bool showExternalIP = App.settingsManager?.GetOption("show_external_ip", "False", "EXTENDED") == "True";
            if (showExternalIP)
            {
                output += Environment.NewLine + FormatExternalIP();
            }
            
            bool showSessionStats = App.settingsManager?.GetOption("show_session_stats", "False", "EXTENDED") == "True";
            if (showSessionStats)
            {
                output += Environment.NewLine + FormatSessionStats();
            }
            
            bool showServerInfo = App.settingsManager?.GetOption("show_server_info", "False", "EXTENDED") == "True";
            if (showServerInfo)
            {
                output += Environment.NewLine + FormatServerInfo();
            }
            
            bool showPacketCounters = App.settingsManager?.GetOption("show_packet_counters", "False", "EXTENDED") == "True";
            if (showPacketCounters)
            {
                output += Environment.NewLine + FormatPacketCounters();
            }
            
            bool showConnectionType = App.settingsManager?.GetOption("show_connection_type", "False", "EXTENDED") == "True";
            if (showConnectionType)
            {
                output += Environment.NewLine + FormatConnectionType();
            }
            
            bool showDiagnosticInfo = App.settingsManager?.GetOption("show_diagnostic_info", "False", "EXTENDED") == "True";
            if (showDiagnosticInfo)
            {
                output += Environment.NewLine + FormatDiagnosticInfo();
            }
            
            // FPS оверлея (выключен по умолчанию)
            bool showOverlayFps = App.settingsManager?.GetOption("show_overlay_fps", "False", "EXTENDED") == "True";
            if (showOverlayFps)
            {
                output += Environment.NewLine + FormatOverlayFPS();
            }
            
            PrintData(output, true);
            }
            finally
            {
                if (App.settingsManager?.GetOption("anti_reentrancy", "True", "ADVANCED") == "True") 
                {
                    System.Threading.Interlocked.Exchange(ref _buildRivaOutputInProgress, 0);
                }
            }
        }

        private static bool HasActiveMetrics(TickMeterState state)
        {
            if (state == null || !state.IsTracking)
            {
                return false;
            }

            var server = state.Server;
            if (server == null || string.IsNullOrEmpty(server.Ip))
            {
                return false;
            }

            bool hasPing = false;
            if (server.IsUdpPingValid)
            {
                hasPing = true;
            }
            else if (server.Ping > 0 && server.Ping < 10000)
            {
                hasPing = true;
            }

            if (!hasPing && state.IcmpPing > 0 && state.IcmpPing < 1000)
            {
                hasPing = true;
            }

            bool hasTickrate = state.OutputTickRate > 0 || state.TickRate > 0 || (server.TicksHistory?.Count ?? 0) > 0;
            bool hasTraffic = server.UploadTraffic > 0 || server.DownloadTraffic > 0;

            if (!(hasPing || hasTickrate || hasTraffic))
            {
                try
                {
                    var snapshot = Classes.UnifiedDataSource.Snapshot();
                    hasPing |= snapshot.PingAvgMs > 0;
                    hasTickrate |= snapshot.TickrateAvgHz > 0;
                    hasTraffic |= snapshot.TicktimeAvgMs > 0; // Non-zero ticktime implies packets detected
                }
                catch
                {
                    // Ignore snapshot errors, rely on existing indicators
                }
            }

            return hasPing || hasTickrate || hasTraffic;
        }

        private static string FormatNoTrafficPlaceholder()
        {
            return "<S><C1>NO TRAFFIC!<C>" + Environment.NewLine;
        }

        public static void ShowNoTrafficPlaceholder()
        {
            PrintData(FormatNoTrafficPlaceholder(), true);
        }

        // Hysteresis для предотвращения дребезга рейтинга
        // FIX: Раздельное отслеживание для Standard и Context (гибридный режим)
        private static double _lastStandardQuality = 1.0;
        private static DateTime _lastStandardQualityChange = DateTime.MinValue;
        private static string _lastStandardQualityLevel = "excellent";
        
        private static double _lastContextQuality = 1.0;
        private static DateTime _lastContextQualityChange = DateTime.MinValue;
        private static string _lastContextQualityLevel = "excellent";

        /// <summary>
        /// Форматирует рейтинг качества сети для RTSS оверлея с анти-дребезгом и компактным форматом
        /// </summary>
        public static string FormatNetworkQuality()
        {
            try
            {
                // Получаем статистику качества сети
                var qualityStats = Classes.NetworkQualityAnalyzer.GetDetailedStats();
                
                // Читаем режим отображения
                string displayMode = (App.settingsManager?.GetOption("network_quality_mode", "hybrid", "ADVANCED") ?? "hybrid").ToLower();
                
                // Собираем дополнительную информацию компактно
                var extras = new List<string>();
                
                // Спайки за последнее время (если есть)
                if (qualityStats.IsPredictingIssues)
                {
                    extras.Add("!");
                }
                
                // Джиттер если высокий
                if (qualityStats.AverageJitter > 20)
                {
                    extras.Add($"jit{qualityStats.AverageJitter:F0}");
                }
                
                string result;
                
                switch (displayMode)
                {
                    case "standard":
                        // Только стандартное качество (объективное, Medium профиль)
                        result = FormatSingleQuality("NET", qualityStats.StandardQuality, "Medium", extras, isContextMode: false);
                        break;
                    
                    case "context":
                        // Только контекстное качество (субъективное, пользовательский профиль)
                        string contextLabel = GetContextLabel(qualityStats.ContextProfile);
                        result = FormatSingleQuality(contextLabel, qualityStats.ContextQuality, qualityStats.ContextProfile, extras, isContextMode: true);
                        break;
                    
                    case "hybrid":
                    default:
                        // Гибридный режим: показываем оба
                        result = FormatHybridQuality(qualityStats, extras);
                        break;
                }
                
                return result;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[FormatNetworkQuality] Error: {ex.Message}");
                return "<S><C0>NET: <C2>Unknown<C>" + Environment.NewLine;
            }
        }
        
        /// <summary>
        /// Форматирует одно значение качества для RTSS
        /// </summary>
        private static string FormatSingleQuality(string label, double quality, string profileName, List<string> extras, bool isContextMode = false)
        {
            var (level, color, icon) = GetQualityLevelWithHysteresis(quality, profileName, isContextMode);
            int qualityPercent = (int)Math.Round(quality * 100);
            
            var result = $"<S><C0>{label}: {color}{icon} {qualityPercent}%<C>";
            
            if (extras.Count > 0)
            {
                var extrasText = string.Join(" ", extras);
                result += $" | {extrasText}";
            }
            
            result += Environment.NewLine;
            
            // Обрезаем если слишком длинно
            if (result.Length > 80)
            {
                result = $"<S><C0>{label}: {color}{icon} {qualityPercent}%<C>" + Environment.NewLine;
            }
            
            return result;
        }
        
        /// <summary>
        /// Форматирует гибридный режим (Standard + Context)
        /// </summary>
        private static string FormatHybridQuality(tickMeter.Classes.NetworkQualityStats stats, List<string> extras)
        {
            var (stdLevel, stdColor, stdIcon) = GetQualityLevelWithHysteresis(stats.StandardQuality, "Medium", isContextMode: false);
            var (ctxLevel, ctxColor, ctxIcon) = GetQualityLevelWithHysteresis(stats.ContextQuality, stats.ContextProfile, isContextMode: true);
            
            int stdPercent = (int)Math.Round(stats.StandardQuality * 100);
            int ctxPercent = (int)Math.Round(stats.ContextQuality * 100);
            
            string contextLabel = Classes.QualityDisplayThresholds.GetProfileShortName(stats.ContextProfile);
            
            var result = $"<S><C0>NET: {stdColor}{stdIcon} {stdPercent}%<C> | {contextLabel}: {ctxColor}{ctxIcon} {ctxPercent}%<C>";
            
            if (extras.Count > 0)
            {
                var extrasText = string.Join(" ", extras);
                result += $" | {extrasText}";
            }
            
            result += Environment.NewLine;
            
            // Обрезаем если слишком длинно
            if (result.Length > 100)
            {
                // Упрощенная версия без extras
                result = $"<S><C0>NET: {stdColor}{stdIcon} {stdPercent}%<C> | {contextLabel}: {ctxColor}{ctxIcon} {ctxPercent}%<C>" + Environment.NewLine;
            }
            
            return result;
        }
        
        /// <summary>
        /// Получает метку для контекстного режима
        /// </summary>
        private static string GetContextLabel(string profileName)
        {
            string shortName = Classes.QualityDisplayThresholds.GetProfileShortName(profileName);
            return shortName == "NET" ? "NET" : shortName; // По умолчанию NET для Medium
        }
        
        /// <summary>
        /// Определяет уровень качества с hysteresis для предотвращения дребезга
        /// </summary>
        /// <param name="quality">Значение качества (0.0-1.0)</param>
        /// <param name="profileName">Имя профиля для адаптивных порогов (Very Low/Low/Medium/High). По умолчанию Medium.</param>
        /// <param name="isContextMode">True для Context mode, false для Standard mode (разное состояние гистерезиса)</param>
        private static (string level, string color, string icon) GetQualityLevelWithHysteresis(double quality, string profileName = "Medium", bool isContextMode = false)
        {
            // Получаем адаптивные пороги для профиля
            var (excellentIn, excellentOut, goodIn, goodOut, fairIn, fairOut) = 
                Classes.QualityDisplayThresholds.GetThresholds(profileName);
            
            const double HOLD_TIME_SECONDS = 3.0; // Минимальное время удержания уровня
            
            var now = DateTime.Now;
            
            // Выбираем состояние в зависимости от режима
            ref double lastQuality = ref (isContextMode ? ref _lastContextQuality : ref _lastStandardQuality);
            ref DateTime lastQualityChange = ref (isContextMode ? ref _lastContextQualityChange : ref _lastStandardQualityChange);
            ref string lastQualityLevel = ref (isContextMode ? ref _lastContextQualityLevel : ref _lastStandardQualityLevel);
            
            bool shouldHold = (now - lastQualityChange).TotalSeconds < HOLD_TIME_SECONDS;
            
            string newLevel;
            
            if (shouldHold)
            {
                // Применяем выходные пороги для текущего уровня
                switch (lastQualityLevel)
                {
                    case "excellent":
                        newLevel = quality < excellentOut ? GetQualityLevel(quality, goodIn, fairIn) : "excellent";
                        break;
                    case "good":
                        newLevel = quality >= excellentIn ? "excellent" : 
                                  quality < goodOut ? GetQualityLevel(quality, goodIn, fairIn) : "good";
                        break;
                    case "fair":
                        newLevel = quality >= goodIn ? GetQualityLevel(quality, goodIn, fairIn) : 
                                  quality < fairOut ? "poor" : "fair";
                        break;
                    default: // poor
                        newLevel = quality >= fairIn ? GetQualityLevel(quality, goodIn, fairIn) : "poor";
                        break;
                }
            }
            else
            {
                // Применяем входные пороги
                newLevel = GetQualityLevel(quality, goodIn, fairIn);
            }
            
            // Обновляем состояние если изменился уровень
            if (newLevel != lastQualityLevel)
            {
                lastQuality = quality;
                lastQualityChange = now;
                lastQualityLevel = newLevel;
            }
            
            // Возвращаем параметры отображения с прямыми цветовыми кодами
            switch (newLevel)
            {
                case "excellent":
                    return ("Excellent", "<C3>", "EXC");  // Палитра зеленый (ColorGood)
                case "good":
                    return ("Good", "<C3>", "GOOD");      // Палитра зеленый (ColorGood)
                case "fair":
                    return ("Fair", "<C2>", "FAIR");      // Палитра желтый (ColorMid)
                default:
                    return ("Poor", "<C1>", "POOR");      // Палитра красный (ColorBad)
            }
        }
        
        /// <summary>
        /// Определяет базовый уровень качества по входным порогам
        /// </summary>
        private static string GetQualityLevel(double quality, double goodThreshold, double fairThreshold)
        {
            if (quality >= 0.90) return "excellent";
            if (quality >= goodThreshold) return "good";
            if (quality >= fairThreshold) return "fair";
            return "poor";
        }

        public static void PrintData(string text, bool RunRivaFlag = false)
        {
            if ((!IsRivaRunning() && !RunRivaFlag) || !VerifyRiva()) return;

            if (!IsRivaRunning() && RunRivaFlag)
            {
                RunRiva();
            }
            if (text != "")
            {
                text = TextFormat() + text;
            }
            Print(text);
        }

        // Кэш для процесса с TTL (уменьшен для быстрого отклика)
        private static string _cachedProcessInfo = "";
        private static DateTime _lastProcessUpdate = DateTime.MinValue;
        private static readonly TimeSpan PROCESS_TTL = TimeSpan.FromMilliseconds(500); // 0.5 секунды
        
        private static string FormatActiveProcess()
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now - _lastProcessUpdate > PROCESS_TTL)
                {
                    // Получаем реальное активное окно в реальном времени
                    string activeProcessName = Classes.AutoDetectMngr.GetActiveProcessName(true);
                    
                    if (!string.IsNullOrEmpty(activeProcessName))
                    {
                        string processName = activeProcessName;
                        
                        // Обрезаем имя до 15 символов для компактности
                        if (processName.Length > 15)
                        {
                            processName = processName.Substring(0, 12) + "...";
                        }
                        
                        _cachedProcessInfo = $"Active: {processName}";
                    }
                    else
                    {
                        _cachedProcessInfo = "Active: Unknown";
                    }
                    _lastProcessUpdate = now;
                }
                
                return _cachedProcessInfo;
            }
            catch
            {
                return "Game: Unknown";
            }
        }

        // Кэш для времени сессии с TTL
        private static string _cachedSessionTime = "";
        private static DateTime _lastSessionTimeUpdate = DateTime.MinValue;
        private static readonly TimeSpan SESSION_TIME_TTL = TimeSpan.FromSeconds(1);
        
        private static string FormatSessionTime()
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now - _lastSessionTimeUpdate > SESSION_TIME_TTL)
                {
                    var sessionTime = DateTime.Now - Process.GetCurrentProcess().StartTime;
                    _cachedSessionTime = $"Session: {sessionTime.Hours:D2}:{sessionTime.Minutes:D2}:{sessionTime.Seconds:D2}";
                    _lastSessionTimeUpdate = now;
                }
                
                return _cachedSessionTime;
            }
            catch
            {
                return "Session: Unknown";
            }
        }

        // Кэш для внешнего IP с длительным TTL
        private static string _cachedExternalIP = "";
        private static DateTime _lastExternalIPUpdate = DateTime.MinValue;
        
        private static string FormatExternalIP()
        {
            try
            {
                var now = DateTime.UtcNow;
                
                // Получаем TTL из настроек (по умолчанию 30 минут)
                int ttlMinutes = 30;
                try
                {
                    ttlMinutes = int.Parse(App.settingsManager?.GetOption("external_ip_ttl_min", "30", "EXTENDED") ?? "30");
                }
                catch { /* используем значение по умолчанию */ }
                
                var externalIPTTL = TimeSpan.FromMinutes(ttlMinutes);
                
                if (now - _lastExternalIPUpdate > externalIPTTL)
                {
                    // TODO: Здесь можно добавить асинхронный запрос к внешнему сервису для получения публичного IP
                    // Пока показываем IP сервера, к которому подключены
                    if (!string.IsNullOrEmpty(App.meterState?.Server?.Ip))
                    {
                        _cachedExternalIP = $"Internet: {App.meterState.Server.Ip}";
                    }
                    else
                    {
                        _cachedExternalIP = "Internet: Not available";
                    }
                    _lastExternalIPUpdate = now;
                }
                
                return _cachedExternalIP;
            }
            catch
            {
                return "Internet: Unknown";
            }
        }

        // Кэш для статистики сессии с TTL
        private static string _cachedSessionStats = "";
        private static DateTime _lastSessionStatsUpdate = DateTime.MinValue;
        private static readonly TimeSpan SESSION_STATS_TTL = TimeSpan.FromSeconds(2);
        
        private static string FormatSessionStats()
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now - _lastSessionStatsUpdate > SESSION_STATS_TTL)
                {
                    if (App.meterState?.pingBuffer != null)
                    {
                        float[] pings;
                        lock (App.meterState._pingBufferLock)
                        {
                            if (App.meterState.pingBuffer.Count < 10)
                                return string.Empty;
                            pings = App.meterState.pingBuffer.ToArray();
                        }
                        
                        var validPings = pings.Where(p => p > 0).ToArray();
                        
                        if (validPings.Length > 5)
                        {
                            var avgPing = validPings.Average();
                            var p95Ping = GetPercentile(validPings, 0.95);
                            
                            // Также показываем ticktime если доступен
                            if (App.meterState.tickTimeBuffer?.Count > 5)
                            {
                                float[] ticktimes;
                                lock (App.meterState._tickTimeBufferLock)
                                {
                                    ticktimes = App.meterState.tickTimeBuffer.Where(t => t > 0).ToArray();
                                }
                                if (ticktimes.Length > 0)
                                {
                                    var avgTt = ticktimes.Average();
                                    var p95Tt = GetPercentile(ticktimes, 0.95);
                                    _cachedSessionStats = $"Session: Ping avg {avgPing:F1} (max {p95Ping:F0}) | Time avg {avgTt:F1} (max {p95Tt:F1})";
                                }
                                else
                                {
                                    _cachedSessionStats = $"Session: Ping avg {avgPing:F1} (max {p95Ping:F0})";
                                }
                            }
                            else
                            {
                                _cachedSessionStats = $"Session: Ping avg {avgPing:F1} (max {p95Ping:F0})";
                            }
                        }
                        else
                        {
                            _cachedSessionStats = "Session: Insufficient data";
                        }
                    }
                    else
                    {
                        _cachedSessionStats = "Session: No data";
                    }
                    _lastSessionStatsUpdate = now;
                }
                
                return _cachedSessionStats;
            }
            catch
            {
                return "Session: Error";
            }
        }
        
        // Вспомогательная функция для расчета перцентилей
        private static float GetPercentile(float[] values, double percentile)
        {
            if (values.Length == 0) return 0;
            
            var sorted = values.OrderBy(x => x).ToArray();
            int index = (int)Math.Ceiling(percentile * sorted.Length) - 1;
            index = Math.Max(0, Math.Min(index, sorted.Length - 1));
            
            return sorted[index];
        }

        // Кэш для информации о сервере с TTL
        private static string _cachedServerInfo = "";
        private static DateTime _lastServerInfoUpdate = DateTime.MinValue;
        private static readonly TimeSpan SERVER_INFO_TTL = TimeSpan.FromSeconds(5);
        
        private static string FormatServerInfo()
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now - _lastServerInfoUpdate > SERVER_INFO_TTL)
                {
                    if (App.meterState?.Server != null)
                    {
                        string location = !string.IsNullOrEmpty(App.meterState.Server.Location) 
                            ? App.meterState.Server.Location 
                            : "Unknown";
                        
                        // Обрезаем название региона для компактности
                        if (location.Length > 12)
                        {
                            location = location.Substring(0, 9) + "...";
                        }
                        
                        // TODO: Добавить поддержку порта в GameServer классе
                        string port = "";
                        // if (App.meterState.Server.Port > 0)
                        // {
                        //     port = $" :{App.meterState.Server.Port}";
                        // }
                        
                        _cachedServerInfo = $"Server: {location}{port}";
                    }
                    else
                    {
                        _cachedServerInfo = "Server: Not connected";
                    }
                    _lastServerInfoUpdate = now;
                }
                
                return _cachedServerInfo;
            }
            catch
            {
                return "Server: Unknown";
            }
        }

        // Кэш для счетчиков пакетов с TTL
        private static string _cachedPacketCounters = "";
        private static DateTime _lastPacketCountersUpdate = DateTime.MinValue;
        private static readonly TimeSpan PACKET_COUNTERS_TTL = TimeSpan.FromMilliseconds(500);
        
        private static string FormatPacketCounters()
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now - _lastPacketCountersUpdate > PACKET_COUNTERS_TTL)
                {
                    // TODO: Реализовать подсчет пакетов через интеграцию с сетевым адаптером
                    // Пока показываем примерные данные на основе трафика
                    if (App.meterState != null)
                    {
                        // TODO: Добавить реальные счетчики пакетов
                        // Примерная оценка на основе трафика (если доступен)
                        long estimatedDownPackets = 0;
                        long estimatedUpPackets = 0;
                        
                        // Пока используем примерные значения на основе статистики пингов
                        if (App.meterState.pingBuffer?.Count > 0)
                        {
                            estimatedDownPackets = App.meterState.pingBuffer.Count; // Примерно 1 пакет на ping
                            estimatedUpPackets = App.meterState.pingBuffer.Count / 2; // Ответы меньше
                        }
                        
                        string downStr = FormatCount(estimatedDownPackets);
                        string upStr = FormatCount(estimatedUpPackets);
                        
                        _cachedPacketCounters = $"Traffic: ↓{downStr} ↑{upStr}";
                    }
                    else
                    {
                        _cachedPacketCounters = "Traffic: N/A";
                    }
                    _lastPacketCountersUpdate = now;
                }
                
                return _cachedPacketCounters;
            }
            catch
            {
                return "Traffic: Error";
            }
        }
        
        // Вспомогательная функция для форматирования больших чисел
        private static string FormatCount(long count)
        {
            if (count >= 1000000)
                return $"{count / 1000000.0:F1}M";
            else if (count >= 1000)
                return $"{count / 1000.0:F1}K";
            else
                return count.ToString();
        }

        // Кэш для типа подключения с TTL
        private static string _cachedConnectionType = "";
        private static DateTime _lastConnectionTypeUpdate = DateTime.MinValue;
        private static readonly TimeSpan CONNECTION_TYPE_TTL = TimeSpan.FromSeconds(10);
        
        private static string FormatConnectionType()
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now - _lastConnectionTypeUpdate > CONNECTION_TYPE_TTL)
                {
                    // TODO: Реализовать определение типа адаптера и WiFi RSSI
                    // Пока показываем базовую информацию
                    string connectionType = "Unknown";
                    
                    try
                    {
                        // Простая проверка через System.Net.NetworkInformation
                        var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                        var activeInterface = interfaces.FirstOrDefault(ni => 
                            ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                            ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback);
                        
                        if (activeInterface != null)
                        {
                            switch (activeInterface.NetworkInterfaceType)
                            {
                                case System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211:
                                    connectionType = "Wi-Fi";
                                    // TODO: Добавить RSSI если доступен
                                    break;
                                case System.Net.NetworkInformation.NetworkInterfaceType.Ethernet:
                                    connectionType = "Ethernet";
                                    break;
                                default:
                                    connectionType = activeInterface.NetworkInterfaceType.ToString();
                                    if (connectionType.Length > 8)
                                        connectionType = connectionType.Substring(0, 5) + "...";
                                    break;
                            }
                        }
                    }
                    catch { /* используем значение по умолчанию */ }
                    
                    _cachedConnectionType = $"Connection: {connectionType}";
                    _lastConnectionTypeUpdate = now;
                }
                
                return _cachedConnectionType;
            }
            catch
            {
                return "Connection: Unknown";
            }
        }

        // Кэш для диагностической информации с TTL
        private static string _cachedDiagnosticInfo = "";
        private static DateTime _lastDiagnosticInfoUpdate = DateTime.MinValue;
        private static readonly TimeSpan DIAGNOSTIC_INFO_TTL = TimeSpan.FromSeconds(2);
        
        private static string FormatDiagnosticInfo()
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now - _lastDiagnosticInfoUpdate > DIAGNOSTIC_INFO_TTL)
                {
                    var memory = GC.GetTotalMemory(false) / (1024 * 1024);
                    
                    // Показываем диагностику зон если включена
                    bool showZoneDiag = App.settingsManager?.GetOption("show_diagnostic_info", "False", "EXTENDED") == "True";
                    if (showZoneDiag)
                    {
                        try
                        {
                            var snap = Classes.UnifiedDataSource.Snapshot();
                            var profile = App.settingsManager.GetColorZoneProfile();
                            var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
                            
                            var pingZone = zoner.FromPing(snap.PingAvgMs);
                            var trZone = zoner.FromTickrate(snap.TickrateAvgHz);
                            
                            _cachedDiagnosticInfo = $"Diag: ping={snap.PingAvgMs:F1} ({pingZone}) | tr={snap.TickrateAvgHz:F1} ({trZone}) | mem={memory}MB";
                        }
                        catch
                        {
                            _cachedDiagnosticInfo = $"Diag: Memory {memory}MB";
                        }
                    }
                    else
                    {
                        _cachedDiagnosticInfo = $"Memory: {memory}MB";
                    }
                    
                    _lastDiagnosticInfoUpdate = now;
                }
                
                return _cachedDiagnosticInfo;
            }
            catch
            {
                return "Diag: N/A";
            }
        }
        
        // Кэш для FPS оверлея с TTL
        private static string _cachedOverlayFPS = "";
        private static DateTime _lastOverlayFPSUpdate = DateTime.MinValue;
        private static readonly TimeSpan OVERLAY_FPS_TTL = TimeSpan.FromSeconds(1);
        private static int _frameCounter = 0;
        private static DateTime _lastFPSMeasurement = DateTime.MinValue;
        private static float _currentFPS = 0;
        
        private static string FormatOverlayFPS()
        {
            try
            {
                var now = DateTime.UtcNow;
                
                // Считаем FPS оверлея
                _frameCounter++;
                if (now - _lastFPSMeasurement > TimeSpan.FromSeconds(1))
                {
                    _currentFPS = _frameCounter / (float)(now - _lastFPSMeasurement).TotalSeconds;
                    _frameCounter = 0;
                    _lastFPSMeasurement = now;
                }
                
                if (now - _lastOverlayFPSUpdate > OVERLAY_FPS_TTL)
                {
                    _cachedOverlayFPS = $"Overlay: {_currentFPS:F0} FPS";
                    _lastOverlayFPSUpdate = now;
                }
                
                return _cachedOverlayFPS;
            }
            catch
            {
                return "Overlay: N/A FPS";
            }
        }
    }
}