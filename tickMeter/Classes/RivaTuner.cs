using RTSS;
using System;
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
        
        // Статические переменные для спайк-индикаторов
        public static bool PingSpike = false;
        public static bool TickrateSpike = false;
        public static bool TicktimeSpike = false;

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
            return "<C0=" + LabelColor + "><C1=" + ColorBad+ "><C2=" + ColorMid + "><C3=" + ColorGood + "><C4="+ColorChart+"><S0=47><S1=65><S2=55><A0=-2><A1=2>";
        }

        public static string FormatTickrate()
        {
            // Проверяем флаг оверлея для tickrate спайков
            bool showTickrateSpike = App.settingsManager.GetOption("overlay_tickrate_spike_marker", "False") == "True";
            
            string tickRateStr = "<S><C0>Tickrate: ";
            if (meterState.OutputTickRate < 30)
            {
                tickRateStr += "<C1>" + meterState.OutputTickRate.ToString();
            }
            else if (meterState.OutputTickRate < 50)
            {
                tickRateStr += "<C2>" + meterState.OutputTickRate.ToString();
            }
            else
            {
                tickRateStr += "<C3>" + meterState.OutputTickRate.ToString();
            }
            
            // Добавляем спайк-индикатор если включен и обнаружен спайк
            if (showTickrateSpike && TickrateSpike)
            {
                tickRateStr += " <C1>(!)</C>";
            }
            
            string output = tickRateStr + Environment.NewLine;
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
            return "<S><C0>UP/DL: <C>" + formatedUpload.ToString("N2") + " / " + formatedDownload.ToString("N2") + "<S0> Mb" + Environment.NewLine;
        }

        public static string FormatDrops()
        {
            string dropsStr = "<S><C0>Drops: ";
            float drops = meterState.GetDropsNumber();
            if (drops > 5)
            {
                dropsStr += "<C1>" + meterState.GetDrops();
            }
            else if (drops > 1)
            {
                dropsStr += "<C2>" + meterState.GetDrops();
            }
            else
            {
                dropsStr += "<C3>" + meterState.GetDrops();
            }

            return dropsStr + "%" + Environment.NewLine;
        }

        public static string FormatTime()
        {
            TimeSpan result = DateTime.Now.Subtract(App.meterState.SessionStart);
            string Duration = result.ToString("mm':'ss");
            return "<S><C0>Time: <C>" + Duration + Environment.NewLine;
        }

        public static string FormatPing()
        {
            // UDP > TCP > ICMP, всегда числовое значение
            string pingFont = "";
            string pingValue = "";
            string geo = meterState.Server.Location;

            if (App.meterState.TcpPing >= 1000 && App.meterState.IsUdpPingValid)
            {
                pingFont = "<C3>";
                // Показываем именно числовое значение UDP ping
                pingValue = App.meterState.Server.UdpPing.ToString("0");
            }
            else if (meterState.Server.Ping > 0 && meterState.Server.Ping < 10000)
            {
                if (meterState.Server.Ping < 100)
                    pingFont = "<C3>";
                else if (meterState.Server.Ping < 150)
                    pingFont = "<C2>";
                else
                    pingFont = "<C1>";
                pingValue = meterState.Server.Ping.ToString();
            }
            else if (App.meterState.IcmpPing > 0 && App.meterState.IcmpPing < 1000)
            {
                pingFont = "<C2>";
                pingValue = App.meterState.IcmpPing.ToString();
            }
            else
            {
                pingFont = "<C1>";
                pingValue = "n/a";
            }
            
            // Проверяем флаг оверлея для ping спайков и добавляем индикатор
            bool showPingSpike = App.settingsManager.GetOption("overlay_ping_spike_marker", "True") == "True";
            string spikeIndicator = (showPingSpike && PingSpike) ? " <C1>(!)</C>" : "";
            
            return "<S><C0>Ping: " + pingFont + pingValue + spikeIndicator + "<S0>ms <S0><C>(" + geo + ")" + Environment.NewLine;
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
            if (App.settingsForm.settings_chart_checkbox.Checked)
            {
                // --- Tickrate chart value and color ---
                float tickrateValue = App.meterState.OutputTickRate;
                string tickrateColor = "<C3>";
                if (tickrateValue < 30)
                    tickrateColor = "<C1>";
                else if (tickrateValue < 50)
                    tickrateColor = "<C2>";
                else
                    tickrateColor = "<C3>";

                output += "<S0><C4>Tickrate" + Environment.NewLine;
                
                // Используем сглаженные данные для графика если включено
                bool smoothCharts = App.settingsManager.GetOption("smooth_charts", "False") == "True";
                float[] tickrateChartData = smoothCharts ? 
                    App.meterState.tickrateGraphSmoothed.ToArray() : 
                    App.meterState.tickrateGraph.ToArray();
                
                output += DrawChart(
                    tickrateChartData,
                    0,
                    0,
                    "Tickrate",
                    tickrateValue > 0 ? tickrateValue.ToString("0") : "n/a",
                    tickrateColor
                ) + Environment.NewLine; // убрано дублирование <A0><S0>...
            }
            if (App.settingsForm.settings_ticktime_chart.Checked)
            {
                // --- Ticktime chart label and color ---
                float ticktimeValue = 0;
                string ticktimeColor = "<C3>"; // зелёный
                if (App.meterState.tickTimeBuffer.Count > 0)
                {
                    ticktimeValue = App.meterState.tickTimeBuffer.Last();
                    if (ticktimeValue < 7.0f)
                        ticktimeColor = "<C3>"; // зелёный
                    else if (ticktimeValue < 13.0f)
                        ticktimeColor = "<C2>"; // жёлтый
                    else if (ticktimeValue <= 16.6f)
                        ticktimeColor = "<C5>"; // оранжевый (или другой тег, если определён)
                    else
                        ticktimeColor = "<C1>"; // красный
                }
                output += Environment.NewLine + "<S0><C4>Ticktime" + Environment.NewLine;
                output += DrawChart(
                    App.meterState.tickTimeBuffer.ToArray(),
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
                    // --- Ping chart label and color ---
                    string pingValue = "";
                    string pingColor = "<C1>";
                    // UDP > TCP > ICMP, всегда числовое значение
                    if (App.meterState.TcpPing >= 1000 && App.meterState.IsUdpPingValid)
                    {
                        pingValue = App.meterState.Server.UdpPing.ToString("0");
                        pingColor = "<C3>";
                    }
                    else if (meterState.Server.Ping > 0 && meterState.Server.Ping < 10000)
                    {
                        pingValue = meterState.Server.Ping.ToString();
                        if (meterState.Server.Ping < 100)
                            pingColor = "<C3>";
                        else if (meterState.Server.Ping < 150)
                            pingColor = "<C2>";
                        else
                            pingColor = "<C1>";
                    }
                    else if (App.meterState.IcmpPing > 0 && App.meterState.IcmpPing < 1000)
                    {
                        pingValue = App.meterState.IcmpPing.ToString();
                        pingColor = "<C2>";
                    }
                    else
                    {
                        pingValue = "n/a";
                        pingColor = "<C1>";
                    }
                    output += Environment.NewLine + "<S0><C4>Ping" + Environment.NewLine;
                    
                    // Используем сглаженные данные для графика если включено
                    bool smoothCharts = App.settingsManager.GetOption("smooth_charts", "False") == "True";
                    float[] pingChartData = smoothCharts ? 
                        App.meterState.pingBufferSmoothed.ToArray() : 
                        App.meterState.pingBuffer.ToArray();
                    
                    output += DrawChart(
                        pingChartData,
                        (int)pingChartData.Min(),
                        0,
                        "Ping",
                        pingValue,
                        pingColor
                    );
                }
            } catch (InvalidOperationException) { }
            PrintData(output, true);
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