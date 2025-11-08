using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace tickMeter.Classes
{
    /// <summary>
    /// Общий менеджер сглаживания (EMA) для разных метрик.
    /// UNIFIED: Shared EMA instances for GUI and Overlay - same values everywhere!
    /// - Значения (пинг, трафик) используют долгоживущие EMA-инстансы
    /// - Серии для графиков сглаживаются на лету из исходного массива
    /// </summary>
    public static class SmoothingManager
    {
        private static readonly object _lock = new object();

        // UNIFIED: Single shared EMA instances for both GUI and Overlay
        private static ExponentialMovingAverage _emaPingValue;
        private static ExponentialMovingAverage _emaTickrateValue;
        private static ExponentialMovingAverage _emaTicktimeValue;
        private static ExponentialMovingAverage _emaUploadMb;
        private static ExponentialMovingAverage _emaDownloadMb;

        private static double GetAlpha()
        {
            // Используем общий коэффициент из tickrate_smoothing_alpha, если задан
            string alphaStr = App.settingsManager?.GetString("tickrate_smoothing_alpha", "0.15");
            if (SettingsManager.TryParseInvariantDouble(alphaStr?.Trim(), out double a) && a > 0 && a <= 1) return a;
            return 0.15;
        }

        // --- Тумблеры (UNIFIED: single setting for both GUI and Overlay) ---
        public static bool IsPingValueEnabled() => 
            App.settingsManager?.GetBool("smoothing_ping_value_gui", false, "ADVANCED") == true ||
            App.settingsManager?.GetBool("smoothing_ping_value_overlay", false, "ADVANCED") == true;
            
        public static bool IsTickrateValueEnabled() =>
            App.settingsManager?.GetBool("smoothing_tickrate_value_gui", false, "ADVANCED") == true ||
            App.settingsManager?.GetBool("smoothing_tickrate_value_overlay", false, "ADVANCED") == true;
            
        public static bool IsTicktimeValueEnabled() =>
            App.settingsManager?.GetBool("smoothing_ticktime_value_overlay", false, "ADVANCED") == true;
            
        public static bool IsTrafficValueEnabled() => 
            App.settingsManager?.GetBool("smoothing_traffic_value", false, "ADVANCED") == true ||
            App.settingsManager?.GetBool("smoothing_traffic_value_overlay", false, "ADVANCED") == true;
            
        public static bool IsTickrateGraphEnabled() => App.settingsManager?.GetBool("smoothing_tickrate_graph", false, "ADVANCED") == true;
        public static bool IsTicktimeGraphEnabled() => App.settingsManager?.GetBool("smoothing_ticktime_graph", false, "ADVANCED") == true;
        public static bool IsPingGraphEnabled() => App.settingsManager?.GetBool("smoothing_ping_graph", false, "ADVANCED") == true;
        public static bool IsTickrateGraphOverlayEnabled() => App.settingsManager?.GetBool("smoothing_tickrate_graph_overlay", false, "ADVANCED") == true;
        public static bool IsTicktimeGraphOverlayEnabled() => App.settingsManager?.GetBool("smoothing_ticktime_graph_overlay", false, "ADVANCED") == true;
        public static bool IsPingGraphOverlayEnabled() => App.settingsManager?.GetBool("smoothing_ping_graph_overlay", false, "ADVANCED") == true;

        // --- UNIFIED Value Smoothing (same EMA for GUI and Overlay) ---
        
        public static int SmoothPingValue(int raw)
        {
            if (!IsPingValueEnabled() || raw <= 0) return raw;
            lock (_lock)
            {
                if (_emaPingValue == null)
                {
                    _emaPingValue = new ExponentialMovingAverage(GetAlpha());
                }
                return (int)Math.Round(_emaPingValue.Update(raw));
            }
        }

        // Legacy methods for backward compatibility - redirect to unified method
        public static int SmoothPingValueGui(int raw) => SmoothPingValue(raw);
        public static int SmoothPingValueOverlay(int raw) => SmoothPingValue(raw);

        public static int SmoothTickrateValue(int raw)
        {
            if (!IsTickrateValueEnabled() || raw <= 0) return raw;
            lock (_lock)
            {
                if (_emaTickrateValue == null)
                {
                    _emaTickrateValue = new ExponentialMovingAverage(GetAlpha());
                }
                return (int)Math.Round(_emaTickrateValue.Update(raw));
            }
        }

        // Legacy methods for backward compatibility
        public static int SmoothTickrateValueGui(int raw) => SmoothTickrateValue(raw);
        public static int SmoothTickrateValueOverlay(int raw) => SmoothTickrateValue(raw);

        public static float SmoothTicktimeValue(float raw)
        {
            if (!IsTicktimeValueEnabled() || raw <= 0) return raw;
            lock (_lock)
            {
                if (_emaTicktimeValue == null)
                {
                    _emaTicktimeValue = new ExponentialMovingAverage(GetAlpha());
                }
                return (float)_emaTicktimeValue.Update(raw);
            }
        }

        // Legacy method for backward compatibility
        public static float SmoothTicktimeValueOverlay(float raw) => SmoothTicktimeValue(raw);

        public static float SmoothUploadMb(float rawMb)
        {
            if (!IsTrafficValueEnabled() || rawMb < 0) return rawMb;
            lock (_lock)
            {
                if (_emaUploadMb == null)
                {
                    _emaUploadMb = new ExponentialMovingAverage(GetAlpha());
                }
                return (float)_emaUploadMb.Update(rawMb);
            }
        }

        // Legacy method for backward compatibility
        public static float SmoothUploadMbOverlay(float rawMb) => SmoothUploadMb(rawMb);

        public static float SmoothDownloadMb(float rawMb)
        {
            if (!IsTrafficValueEnabled() || rawMb < 0) return rawMb;
            lock (_lock)
            {
                if (_emaDownloadMb == null)
                {
                    _emaDownloadMb = new ExponentialMovingAverage(GetAlpha());
                }
                return (float)_emaDownloadMb.Update(rawMb);
            }
        }

        // Legacy method for backward compatibility
        public static float SmoothDownloadMbOverlay(float rawMb) => SmoothDownloadMb(rawMb);

        // --- Серии ---
        public static float[] SmoothSeries(IEnumerable<float> series, bool enabled)
        {
            if (!enabled || series == null) return series?.ToArray() ?? Array.Empty<float>();
            double alpha = GetAlpha();
            var ema = new ExponentialMovingAverage(alpha);
            var output = new List<float>();
            foreach (var v in series)
            {
                output.Add((float)ema.Update(v));
            }
            return output.ToArray();
        }

        public static int[] SmoothSeries(IEnumerable<int> series, bool enabled)
        {
            if (!enabled || series == null) return series?.ToArray() ?? Array.Empty<int>();
            double alpha = GetAlpha();
            var ema = new ExponentialMovingAverage(alpha);
            var output = new List<int>();
            foreach (var v in series)
            {
                output.Add((int)Math.Round(ema.Update(v)));
            }
            return output.ToArray();
        }

        public static void ResetValueEmas()
        {
            lock (_lock)
            {
                // UNIFIED: Reset shared EMA instances
                _emaPingValue?.Reset();
                _emaTickrateValue?.Reset();
                _emaTicktimeValue?.Reset();
                _emaUploadMb?.Reset();
                _emaDownloadMb?.Reset();
                
                _emaPingValue = null;
                _emaTickrateValue = null;
                _emaTicktimeValue = null;
                _emaUploadMb = null;
                _emaDownloadMb = null;
            }
        }
    }
}
