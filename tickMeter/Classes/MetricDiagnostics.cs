using System;
using System.Drawing;
using Newtonsoft.Json;

namespace tickMeter.Classes
{
    /// <summary>
    /// Centralized diagnostics logging for GUI and overlay metrics snapshots.
    /// Produces structured JSON lines prefixed with "[METRICS]" so they are easy to locate in debug.log.
    /// </summary>
    public static class MetricDiagnostics
    {
        private static readonly object _sync = new object();
        private static DateTime _lastLogUtc = DateTime.MinValue;
        private static readonly TimeSpan LogInterval = TimeSpan.FromSeconds(1);
        private const string LogPrefix = "[METRICS] ";

        public static void TryLog(MetricDiagnosticPayload payload)
        {
            if (payload == null)
            {
                return;
            }

            var nowUtc = DateTime.UtcNow;
            bool shouldLog;

            lock (_sync)
            {
                shouldLog = (nowUtc - _lastLogUtc) >= LogInterval;
                if (shouldLog)
                {
                    _lastLogUtc = nowUtc;
                }
            }

            if (!shouldLog)
            {
                return;
            }

            try
            {
                payload.TimestampUtc = nowUtc;
                string json = JsonConvert.SerializeObject(payload, Formatting.None);
                DebugLogger.log(LogPrefix + json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[MetricDiagnostics] Failed to write snapshot: {ex.Message}");
            }
        }

        public static string ToHex(Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }
    }

    public class MetricDiagnosticPayload
    {
        public DateTime TimestampUtc { get; set; }
        public string Game { get; set; }
        public string ActiveProcess { get; set; }
        public string TargetKey { get; set; }
        public string LocalIp { get; set; }
        public bool IsTracking { get; set; }
        public bool GuiVisible { get; set; }
        public ServerMetrics Server { get; set; }
        public GuiMetrics Gui { get; set; }
        public OverlaySnapshot Overlay { get; set; }
        public ZoneMetrics Zones { get; set; }
        public SpikeMetrics Spikes { get; set; }
        public SmoothingFlags Smoothing { get; set; }
        public string Diagnostic { get; set; }
    }

    public class ServerMetrics
    {
        public string Ip { get; set; }
        public int PingPort { get; set; }
        public string Location { get; set; }
        public int OutputTickRate { get; set; }
        public int AvgTickrate { get; set; }
        public int AvgStableTickrate { get; set; }
        public int TotalTicks { get; set; }
        public int LostTicks { get; set; }
        public float PacketLossPercent { get; set; }
        public double AvgPingMs { get; set; }
        public double UdpPingMs { get; set; }
        public int TcpPingMs { get; set; }
        public int IcmpPingMs { get; set; }
    }

    public class GuiMetrics
    {
        public PingMetrics Ping { get; set; }
        public TickrateMetrics Tickrate { get; set; }
        public TrafficMetrics Traffic { get; set; }
        public string SessionDuration { get; set; }
    }

    public class OverlaySnapshot
    {
        public double PingMs { get; set; }
        public double TickrateAvgHz { get; set; }
        public double TicktimeAvgMs { get; set; }
        public double TargetHz { get; set; }
    }

    public class ZoneMetrics
    {
        public string Ping { get; set; }
        public string Tickrate { get; set; }
        public string Ticktime { get; set; }
    }

    public class SpikeMetrics
    {
        public bool Ping { get; set; }
        public bool Tickrate { get; set; }
        public bool Ticktime { get; set; }
    }

    public class SmoothingFlags
    {
        public bool PingGuiEnabled { get; set; }
        public bool PingOverlayEnabled { get; set; }
        public bool TickrateOverlayEnabled { get; set; }
        public bool TrafficOverlayEnabled { get; set; }
    }

    public class PingMetrics
    {
        public int RawMs { get; set; }
        public int GuiDisplayedMs { get; set; }
        public double OverlaySnapshotMs { get; set; }
        public string Source { get; set; }
        public string DisplayText { get; set; }
        public string ColorHex { get; set; }
    }

    public class TickrateMetrics
    {
        public int Raw { get; set; }
        public string GuiDisplayText { get; set; }
        public string ColorHex { get; set; }
        public double SnapshotAvgHz { get; set; }
    }

    public class TrafficMetrics
    {
        public double UploadMb { get; set; }
        public double DownloadMb { get; set; }
    }
}
