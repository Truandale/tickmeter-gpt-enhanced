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
                osd = new OSD("TickMeter");
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
            if (!VerifyRiva()) return;
            if (!IsRivaRunning())
            {
                RunRiva();
            } else
            {
                osd = new OSD("TickMeter");
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
            
            // === ChatGPT ENHANCED: Snapshot-based unified tickrate zoning ===
            // Use SAME snapshot as FormatPing() for perfect consistency
            var snap = Classes.UnifiedDataSource.Snapshot();
            var profile = App.settingsManager.GetColorZoneProfile();
            var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
            
            // Get zone from SAME snapshot data as GUI
            var tickrateZone = zoner.FromTickrate(snap.TickrateAvgHz);
            
            // Use SAME color mapping as FormatPing() but for RTSS format
            string tickrateColor = Classes.ZoneColors.ToRtssLegacy(tickrateZone);
            
            // Применяем сглаживание для overlay значений тикрейта, если включено
            int displayTickrate = Classes.SmoothingManager.SmoothTickrateValueOverlay(meterState.OutputTickRate);
            
            tickRateStr += tickrateColor + displayTickrate.ToString();
            
            // Добавляем индикатор спайка для tickrate
            bool showTickrateSpikes = App.settingsManager?.GetOption("show_tickrate_spikes", "True", "ADVANCED") == "True";
            if (showTickrateSpikes && App.meterState.HasTickRateSpike)
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
            // === ChatGPT ENHANCED: Snapshot-based unified zoning ===
            // Use SAME snapshot as GUI for perfect consistency
            var snap = Classes.UnifiedDataSource.Snapshot();
            var profile = App.settingsManager.GetColorZoneProfile(); 
            var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
            
            // Get zone from SAME snapshot data as GUI
            var pingZone = zoner.FromPing(snap.PingAvgMs);
            
            // Use SAME color mapping as GUI but for RTSS format
            string pingFont = Classes.ZoneColors.ToRtssLegacy(pingZone);
            
            string pingValue = "";
            string geo = meterState.Server.Location;
            
            // Format display value from snapshot
            if (snap.PingAvgMs > 0)
            {
                pingValue = ((int)snap.PingAvgMs).ToString();
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

        public static void BuildRivaOutput()
        {
            string output = "";
            if(App.meterState.TickRate == 0 && App.meterState.Game == "")
            {
                PrintData(output, true);
                return;
            }
            chartOffset = 0;
            meterState = App.meterState;
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
                // === ChatGPT ENHANCED: Use unified zoning for tickrate chart ===
                var snap = Classes.UnifiedDataSource.Snapshot();
                var profile = App.settingsManager.GetColorZoneProfile();
                var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
                var tickrateZone = zoner.FromTickrate(snap.TickrateAvgHz);
                string tickrateColor = Classes.ZoneColors.ToRtssLegacy(tickrateZone);
                
                float tickrateValue = App.meterState.OutputTickRate;

                // Добавляем индикатор спайка для tickrate chart
                string tickrateChartLabel = "Tickrate";
                bool showTickrateSpikes = App.settingsManager?.GetOption("show_tickrate_spikes", "True", "ADVANCED") == "True";
                if (showTickrateSpikes && App.meterState.HasTickRateSpike)
                {
                    tickrateChartLabel += " (!)";
                }
                
                output += "<S0><C4>" + tickrateChartLabel + Environment.NewLine;
                
                // Применяем сглаживание графика тикрейта, если включено
                float[] tickrateGraphData = Classes.SmoothingManager.SmoothSeries(
                    App.meterState.tickrateGraph.ToArray(),
                    Classes.SmoothingManager.IsTickrateGraphOverlayEnabled()
                );
                
                output += DrawChart(
                    tickrateGraphData,
                    0,
                    0,
                    "Tickrate",
                    tickrateValue > 0 ? tickrateValue.ToString("0") : "n/a",
                    tickrateColor
                ) + Environment.NewLine; // убрано дублирование <A0><S0>...
            }
            if (App.settingsForm.settings_ticktime_chart.Checked)
            {
                // === ChatGPT ENHANCED: Use unified zoning for ticktime chart ===
                var snap = Classes.UnifiedDataSource.Snapshot();
                var profile = App.settingsManager.GetColorZoneProfile();
                var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
                var ticktimeZone = zoner.FromTicktime(snap.TicktimeAvgMs);
                string ticktimeColor = Classes.ZoneColors.ToRtssLegacy(ticktimeZone);
                
                float ticktimeValue = 0;
                if (App.meterState.tickTimeBuffer.Count > 0)
                {
                    ticktimeValue = App.meterState.tickTimeBuffer.Last();
                }
                // Добавляем индикатор спайка для ticktime
                string ticktimeLabel = "Ticktime";
                bool showTicktimeSpikes = App.settingsManager?.GetOption("show_ticktime_spikes", "True", "ADVANCED") == "True";
                if (showTicktimeSpikes && App.meterState.HasTickTimeSpike)
                {
                    ticktimeLabel += " (!)";
                }
                
                output += Environment.NewLine + "<S0><C4>" + ticktimeLabel + Environment.NewLine;
                
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
                    ticktimeValue > 0 ? ticktimeValue.ToString("0.0") : "n/a",
                    ticktimeColor
                );
            }
                try
                {
                    if (App.settingsForm.settings_ping_chart.Checked && App.meterState.pingBuffer.Count() > 1)
                    {
                        // === ChatGPT ENHANCED: Snapshot-based ping chart ===
                        // Use SAME snapshot as FormatPing() for perfect consistency
                        var snap = Classes.UnifiedDataSource.Snapshot();
                        var profile = App.settingsManager.GetColorZoneProfile();
                        var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
                        
                        // Get zone from SAME snapshot data as FormatPing()
                        var pingZone = zoner.FromPing(snap.PingAvgMs);
                        
                        // Use SAME color mapping as FormatPing() but for RTSS format
                        string pingColor = Classes.ZoneColors.ToRtssLegacy(pingZone);
                        
                        // Format display value from snapshot
                        string pingValue = "";
                        if (snap.PingAvgMs > 0)
                        {
                            pingValue = ((int)snap.PingAvgMs).ToString();
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

        // Hysteresis для предотвращения дребезга рейтинга
        private static double _lastQuality = 1.0;
        private static DateTime _lastQualityChange = DateTime.MinValue;
        private static string _lastQualityLevel = "excellent";

        /// <summary>
        /// Форматирует рейтинг качества сети для RTSS оверлея с анти-дребезгом и компактным форматом
        /// </summary>
        public static string FormatNetworkQuality()
        {
            try
            {
                // Получаем статистику качества сети
                var qualityStats = Classes.NetworkQualityAnalyzer.GetDetailedStats();
                var currentQuality = qualityStats.OverallQuality;
                
                // Применяем hysteresis для стабильного отображения
                var (level, color, icon) = GetQualityLevelWithHysteresis(currentQuality);
                
                // Формируем компактную строку для RTSS
                int qualityPercent = (int)Math.Round(currentQuality * 100);
                
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
                
                // Формируем финальную строку (максимум 60 символов для RTSS)
                var result = $"<S><C0>NET: {color}{icon} {qualityPercent}%<C>";
                
                if (extras.Count > 0)
                {
                    var extrasText = string.Join(" ", extras);
                    result += $" | {extrasText}";
                }
                
                result += Environment.NewLine;
                
                // Обрезаем если слишком длинно
                if (result.Length > 80) // учитываем RTSS теги
                {
                    result = $"<S><C0>NET: {color}{icon} {qualityPercent}%<C>" + Environment.NewLine;
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
        /// Определяет уровень качества с hysteresis для предотвращения дребезга
        /// </summary>
        private static (string level, string color, string icon) GetQualityLevelWithHysteresis(double quality)
        {
            const double EXCELLENT_IN = 0.90, EXCELLENT_OUT = 0.85;
            const double GOOD_IN = 0.75, GOOD_OUT = 0.70;
            const double FAIR_IN = 0.50, FAIR_OUT = 0.45;
            const double HOLD_TIME_SECONDS = 3.0; // Минимальное время удержания уровня
            
            var now = DateTime.Now;
            bool shouldHold = (now - _lastQualityChange).TotalSeconds < HOLD_TIME_SECONDS;
            
            string newLevel;
            
            if (shouldHold)
            {
                // Применяем выходные пороги для текущего уровня
                switch (_lastQualityLevel)
                {
                    case "excellent":
                        newLevel = quality < EXCELLENT_OUT ? GetQualityLevel(quality, GOOD_IN, FAIR_IN) : "excellent";
                        break;
                    case "good":
                        newLevel = quality >= EXCELLENT_IN ? "excellent" : 
                                  quality < GOOD_OUT ? GetQualityLevel(quality, GOOD_IN, FAIR_IN) : "good";
                        break;
                    case "fair":
                        newLevel = quality >= GOOD_IN ? GetQualityLevel(quality, GOOD_IN, FAIR_IN) : 
                                  quality < FAIR_OUT ? "poor" : "fair";
                        break;
                    default: // poor
                        newLevel = quality >= FAIR_IN ? GetQualityLevel(quality, GOOD_IN, FAIR_IN) : "poor";
                        break;
                }
            }
            else
            {
                // Применяем входные пороги
                newLevel = GetQualityLevel(quality, GOOD_IN, FAIR_IN);
            }
            
            // Обновляем состояние если изменился уровень
            if (newLevel != _lastQualityLevel)
            {
                _lastQuality = quality;
                _lastQualityChange = now;
                _lastQualityLevel = newLevel;
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

        // Кэш для процесса с TTL
        private static string _cachedProcessInfo = "";
        private static DateTime _lastProcessUpdate = DateTime.MinValue;
        private static readonly TimeSpan PROCESS_TTL = TimeSpan.FromSeconds(1);
        
        private static string FormatActiveProcess()
        {
            try
            {
                var now = DateTime.UtcNow;
                if (now - _lastProcessUpdate > PROCESS_TTL)
                {
                    if (!string.IsNullOrEmpty(App.meterState?.Game))
                    {
                        string processName = App.meterState.Game;
                        
                        // Обрезаем имя до 15 символов для компактности
                        if (processName.Length > 15)
                        {
                            processName = processName.Substring(0, 12) + "...";
                        }
                        
                        // Добавляем PID если доступен
                        _cachedProcessInfo = $"Game: {processName}";
                    }
                    else
                    {
                        _cachedProcessInfo = "Game: Not detected";
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
                    if (App.meterState?.pingBuffer != null && App.meterState.pingBuffer.Count > 10)
                    {
                        var pings = App.meterState.pingBuffer.ToArray();
                        var validPings = pings.Where(p => p > 0).ToArray();
                        
                        if (validPings.Length > 5)
                        {
                            var avgPing = validPings.Average();
                            var p95Ping = GetPercentile(validPings, 0.95);
                            
                            // Также показываем ticktime если доступен
                            if (App.meterState.tickTimeBuffer?.Count > 5)
                            {
                                var ticktimes = App.meterState.tickTimeBuffer.Where(t => t > 0).ToArray();
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