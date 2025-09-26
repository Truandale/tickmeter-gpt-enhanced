using RTSS;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

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
                // Good - use direct green color like ping to avoid black color issue
                dropsColor = "<C=00FF00>";
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
                return "<S><C0>NET: <C=FFFF00>Unknown<C>" + Environment.NewLine;
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
                    return ("Excellent", "<C=00FF00>", "EXC");  // Прямой зеленый код
                case "good":
                    return ("Good", "<C=00FF00>", "GOOD");      // Прямой зеленый код
                case "fair":
                    return ("Fair", "<C=FFFF00>", "FAIR");      // Прямой желтый код
                default:
                    return ("Poor", "<C=FF0000>", "POOR");      // Прямой красный код
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
    }
}