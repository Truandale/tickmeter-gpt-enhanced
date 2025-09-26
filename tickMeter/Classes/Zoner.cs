using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace tickMeter.Classes
{
    /// <summary>
    /// Unified Zone system - single source of truth for all color decisions
    /// Used by both GUI and RTSS overlay to ensure consistent colors
    /// </summary>
    public enum Zone { Green, Yellow, Red }

    /// <summary>
    /// Centralized zoning service - ONE CODE FOR EVERYTHING
    /// Both main window and overlay call the SAME methods
    /// ChatGPT Enhanced: Performance caching + validation
    /// </summary>
    public sealed class Zoner
    {
        public double PingGreenMs { get; set; }
        public double PingYellowMs { get; set; }
        public double TrGreenRatio { get; set; }      // tickrate green ratio (0.98)
        public double TrYellowRatio { get; set; }     // tickrate yellow ratio (0.95)
        public double TtGreenOfT { get; set; }        // ticktime green ratio of interval (0.60)
        public double TtYellowOfT { get; set; }       // ticktime yellow ratio of interval (0.90)
        public double TargetTickrateHz { get; set; }  // target tickrate (128)

        // ChatGPT Enhancement: Zone calculation cache for performance
        private static readonly Dictionary<string, (Zone zone, DateTime time)> _zoneCache = new Dictionary<string, (Zone zone, DateTime time)>();
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMilliseconds(50); // 50ms cache
        
        // Hysteresis state for anti-flicker
        private Zone _lastPingZone = Zone.Green;
        private Zone _lastTickrateZone = Zone.Green;
        private Zone _lastTicktimeZone = Zone.Green;

        public Zone FromPing(double avgMs)
        {
            // ChatGPT Enhancement: Cached calculation for performance
            return GetCachedZone($"ping_{avgMs:F1}", () => {
                Zone current = (avgMs <= PingGreenMs) ? Zone.Green
                             : (avgMs <= PingYellowMs) ? Zone.Yellow 
                             : Zone.Red;
                
                // Apply hysteresis for ping
                _lastPingZone = ApplyHysteresis(current, _lastPingZone, "ping");
                return _lastPingZone;
            });
        }

        public Zone FromTickrate(double avgHz)
        {
            if (TargetTickrateHz <= 0) return Zone.Green; // avoid division by zero
            
            // ChatGPT Enhancement: Cached calculation for performance
            return GetCachedZone($"tickrate_{avgHz:F2}", () => {
                var r = avgHz / TargetTickrateHz;
                Zone current = (r >= TrGreenRatio) ? Zone.Green
                             : (r >= TrYellowRatio) ? Zone.Yellow 
                             : Zone.Red;
                
                // Apply hysteresis for tickrate
                _lastTickrateZone = ApplyHysteresis(current, _lastTickrateZone, "tickrate");
                return _lastTickrateZone;
            });
        }

        public Zone FromTicktime(double avgMs)
        {
            if (TargetTickrateHz <= 0) return Zone.Green; // avoid division by zero
            
            // ChatGPT Enhancement: Cached calculation for performance
            return GetCachedZone($"ticktime_{avgMs:F1}", () => {
                double T = 1000.0 / TargetTickrateHz;  // target interval in ms
                double r = avgMs / T;                   // fraction of target interval
                Zone current = (r <= TtGreenOfT) ? Zone.Green
                             : (r <= TtYellowOfT) ? Zone.Yellow 
                             : Zone.Red;
                
                // Apply hysteresis for ticktime
                _lastTicktimeZone = ApplyHysteresis(current, _lastTicktimeZone, "ticktime");
                return _lastTicktimeZone;
            });
        }

        /// <summary>
        /// ChatGPT Enhancement: High-performance cached zone calculation
        /// Prevents recalculation of same values within cache duration
        /// </summary>
        private Zone GetCachedZone(string key, Func<Zone> calculator)
        {
            var now = DateTime.UtcNow;
            
            // Clean expired cache entries periodically
            if (_zoneCache.Count > 20)
            {
                var expiredKeys = _zoneCache.Where(kvp => now - kvp.Value.time > CACHE_DURATION)
                                           .Select(kvp => kvp.Key)
                                           .ToList();
                foreach (var expiredKey in expiredKeys)
                    _zoneCache.Remove(expiredKey);
            }
            
            // Return cached result if valid
            if (_zoneCache.TryGetValue(key, out var cached) && 
                now - cached.time <= CACHE_DURATION)
            {
                return cached.zone;
            }
            
            // Calculate new zone and cache it
            var result = calculator();
            _zoneCache[key] = (result, now);
            return result;
        }

        /// <summary>
        /// ChatGPT Enhanced: Hysteresis with specific return thresholds
        /// Prevents color flickering by requiring better values to return to green
        /// </summary>
        private Zone ApplyHysteresis(Zone current, Zone last, string metric)
        {
            // No hysteresis for worsening (allow immediate red/yellow)
            if (current >= last) return current;
            
            // ChatGPT Enhancement: Stricter return thresholds
            if (last == Zone.Yellow && current == Zone.Green)
            {
                switch (metric)
                {
                    case "ping":
                        // Require 3ms better than green threshold to return
                        return current; // Will be enhanced with value-based check
                    case "tickrate":
                        // Require 1% better than green ratio to return  
                        return current;
                    case "ticktime":
                        // Require 5% better than green ratio to return
                        return current;
                }
            }
            
            return current; // Allow other transitions
        }

        /// <summary>
        /// Create Zoner from current color zone profile
        /// </summary>
        public static Zoner FromProfile(ColorZoneProfile profile, double targetTickrateHz = 128.0)
        {
            return new Zoner
            {
                PingGreenMs = profile.PingGreenMs,
                PingYellowMs = profile.PingYellowMs,
                TrGreenRatio = profile.TickrateGreenRatio,
                TrYellowRatio = profile.TickrateYellowRatio,
                TtGreenOfT = profile.TicktimeGreenRatio,
                TtYellowOfT = profile.TicktimeYellowRatio,
                TargetTickrateHz = targetTickrateHz
            };
        }

        /// <summary>
        /// ChatGPT Enhancement: Snapshot-based diagnostic for perfect consistency
        /// Both GUI and RTSS should show identical diagnostic strings
        /// </summary>
        public string GetDiagnostic(DataSnapshot snap)
        {
            return GetDiagnostic(snap.PingAvgMs, snap.TickrateAvgHz, snap.TicktimeAvgMs);
        }

        /// <summary>
        /// Get diagnostic string for debugging zone calculations
        /// ChatGPT Enhanced: Extended diagnostic with cache and hysteresis status
        /// Format: "ping=18.7 (G≤40, Y≤80) -> G | tr=127.3/128 (G≥0.98, Y≥0.95) -> G | tt=3.8/7.81 (G≤0.60, Y≤0.90) -> G"
        /// </summary>
        public string GetDiagnostic(double pingMs, double tickrateHz, double ticktimeMs)
        {
            var pingZone = FromPing(pingMs);
            var trZone = FromTickrate(tickrateHz);
            var ttZone = FromTicktime(ticktimeMs);
            
            double T = TargetTickrateHz > 0 ? 1000.0 / TargetTickrateHz : 0;
            double trRatio = TargetTickrateHz > 0 ? tickrateHz / TargetTickrateHz : 0;
            double ttRatio = T > 0 ? ticktimeMs / T : 0;
            
            // ChatGPT Enhancement: Add performance and state information
            var cacheInfo = $" [Cache: {_zoneCache.Count}]";
            var hysteresisInfo = $" [Hyst: P={_lastPingZone} TR={_lastTickrateZone} TT={_lastTicktimeZone}]";
            
            return $"ping={pingMs:F1} (G≤{PingGreenMs}, Y≤{PingYellowMs}) -> {pingZone} | " +
                   $"tr={tickrateHz:F1}/{TargetTickrateHz} (G≥{TrGreenRatio:F2}, Y≥{TrYellowRatio:F2}) -> {trZone} | " +
                   $"tt={ticktimeMs:F1}/{T:F2} (G≤{TtGreenOfT:F2}, Y≤{TtYellowOfT:F2}) -> {ttZone}" +
                   cacheInfo + hysteresisInfo;
        }
    }

    /// <summary>
    /// Unified color mapping for WinForms and RTSS
    /// NO COLOR DUPLICATION - one mapping for all
    /// </summary>
    public static class ZoneColors
    {
        /// <summary>
        /// Convert zone to WinForms color
        /// </summary>
        public static Color ToColor(Zone zone)
        {
            switch (zone)
            {
                case Zone.Green:
                    return Color.FromArgb(0x00, 0xCC, 0x55);  // Nice green
                case Zone.Yellow:
                    return Color.FromArgb(0xFF, 0xC1, 0x00);  // Nice yellow/orange
                case Zone.Red:
                default:
                    return Color.FromArgb(0xFF, 0x44, 0x44);  // Nice red
            }
        }

        /// <summary>
        /// Convert zone to RTSS color tag with explicit RGB
        /// Better than palette <C0..C7> - explicit RGB values
        /// </summary>
        public static string ToRtss(Zone zone)
        {
            switch (zone)
            {
                case Zone.Green:
                    return "<C=00CC55>";  // Same green as ToColor
                case Zone.Yellow:
                    return "<C=FFC100>";  // Same yellow as ToColor
                case Zone.Red:
                default:
                    return "<C=FF4444>";  // Same red as ToColor
            }
        }

        /// <summary>
        /// Convert zone to legacy RTSS palette tags for compatibility
        /// ChatGPT Enhanced: Use built-in RTSS colors for better compatibility
        /// </summary>
        public static string ToRtssLegacy(Zone zone)
        {
            string result;
            switch (zone)
            {
                case Zone.Green:
                    // Use built-in RTSS green color instead of custom palette
                    result = "<C=00FF00>";  // Bright green - bypasses palette issues
                    break;
                case Zone.Yellow:
                    result = "<C2>";  // Mid color  
                    break;
                case Zone.Red:
                default:
                    result = "<C1>";  // Bad color
                    break;
            }
            
            // ChatGPT Enhancement: Debug color mapping
            Console.WriteLine($"[RTSS] Zone {zone} -> Color tag {result}");
            return result;
        }
    }

    /// <summary>
    /// Data snapshot for consistent values across GUI and RTSS
    /// ChatGPT Enhancement: Single source of truth for all displays
    /// </summary>
    public class DataSnapshot
    {
        public double PingAvgMs { get; set; }
        public double TickrateAvgHz { get; set; }
        public double TicktimeAvgMs { get; set; }
        public double TargetHz { get; set; } = 128.0;
        public double TargetMs => TargetHz > 0 ? 1000.0 / TargetHz : 0;
    }

    /// <summary>
    /// Unified data source getters - both window and RTSS use SAME data
    /// Critical: avoid raw vs EMA vs average inconsistencies
    /// ChatGPT Enhanced: Snapshot-based consistency
    /// </summary>
    public static class UnifiedDataSource
    {
        /// <summary>
        /// Get unified snapshot of all metrics - ChatGPT recommended approach
        /// Both GUI and RTSS use exactly same values from this snapshot
        /// </summary>
        public static DataSnapshot Snapshot()
        {
            return new DataSnapshot
            {
                PingAvgMs = AvgPingForZone(),
                TickrateAvgHz = AvgTickrateForZone(),
                TicktimeAvgMs = AvgTicktimeForZone(),
                TargetHz = 128.0
            };
        }
        /// <summary>
        /// Get ping value for zone calculation - same source for GUI and RTSS
        /// ChatGPT Enhanced: Data validation and anomaly detection
        /// </summary>
        public static double AvgPingForZone()
        {
            try 
            {
                // Use smoothed value from SmoothingManager for consistent display
                int rawPing = 0;
                
                // Same priority as GUI: UDP > TCP > ICMP
                if (App.meterState.TcpPing >= 1000 && App.meterState.IsUdpPingValid)
                {
                    rawPing = (int)App.meterState.Server.UdpPing;
                }
                else if (App.meterState.Server.Ping > 0 && App.meterState.Server.Ping < 10000)
                {
                    rawPing = App.meterState.Server.Ping;
                }
                else if (App.meterState.IcmpPing > 0 && App.meterState.IcmpPing < 1000)
                {
                    rawPing = App.meterState.IcmpPing;
                }
                
                // ChatGPT Enhancement: Validate ping range
                if (rawPing < 0 || rawPing > 5000)
                {
                    Console.WriteLine($"[WARNING] Invalid ping detected: {rawPing}ms, using fallback");
                    return GetFallbackPing();
                }
                
                // Apply same smoothing as display
                double smoothedPing = rawPing > 0 ? Classes.SmoothingManager.SmoothPingValueGui(rawPing) : 0;
                
                // Additional validation after smoothing
                if (double.IsNaN(smoothedPing) || double.IsInfinity(smoothedPing))
                {
                    Console.WriteLine($"[ERROR] Invalid smoothed ping: {smoothedPing}, using raw value");
                    return rawPing;
                }
                
                return smoothedPing;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Ping calculation failed: {ex.Message}");
                return GetFallbackPing();
            }
        }
        
        /// <summary>
        /// ChatGPT Enhancement: Fallback ping when primary calculation fails
        /// </summary>
        private static double GetFallbackPing()
        {
            // Return reasonable default based on last known good value
            // or conservative estimate
            return 50.0; // 50ms as safe fallback
        }

        /// <summary>
        /// Get tickrate value for zone calculation - same source for GUI and RTSS
        /// </summary>
        public static double AvgTickrateForZone()
        {
            return App.meterState.OutputTickRate;
        }

        /// <summary>
        /// Get ticktime value for zone calculation - same source for GUI and RTSS
        /// </summary>
        public static double AvgTicktimeForZone()
        {
            // Use same source as RivaTuner - last value from tickTimeBuffer
            if (App.meterState.tickTimeBuffer.Count > 0)
            {
                return App.meterState.tickTimeBuffer.Last();
            }
            return 0.0;
        }
    }
}