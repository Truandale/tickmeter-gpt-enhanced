using System;
using System.Collections.Generic;
using System.Linq;

namespace tickMeter.Classes
{
    /// <summary>
    /// Общий менеджер сглаживания (EMA) для разных метрик.
    /// - Значения (пинг, трафик) используют долгоживущие EMA-инстансы
    /// - Серии для графиков сглаживаются на лету из исходного массива
    /// </summary>
    public static class SmoothingManager
    {
        private static readonly object _lock = new object();

        private static ExponentialMovingAverage _emaPingValue;
        private static ExponentialMovingAverage _emaUploadMb;
        private static ExponentialMovingAverage _emaDownloadMb;
        
        // EMA для overlay значений
        private static ExponentialMovingAverage _emaPingValueOverlay;
        private static ExponentialMovingAverage _emaPingValueGui;
        private static ExponentialMovingAverage _emaTickrateValueOverlay;
        private static ExponentialMovingAverage _emaUploadMbOverlay;
        private static ExponentialMovingAverage _emaDownloadMbOverlay;

        private static double GetAlpha()
        {
            // Используем общий коэффициент из tickrate_smoothing_alpha, если задан
            string alphaStr = App.settingsManager?.GetString("tickrate_smoothing_alpha", "0.15");
            if (double.TryParse(alphaStr, out double a) && a > 0 && a <= 1) return a;
            return 0.15;
        }

        // --- Тумблеры ---
        public static bool IsPingValueEnabled() => App.settingsManager?.GetBool("smoothing_ping_value", false) == true;
        public static bool IsTrafficValueEnabled() => App.settingsManager?.GetBool("smoothing_traffic_value", false) == true;
        public static bool IsTickrateGraphEnabled() => App.settingsManager?.GetBool("smoothing_tickrate_graph", false) == true;
    public static bool IsTicktimeGraphEnabled() => App.settingsManager?.GetBool("smoothing_ticktime_graph", false) == true;
    public static bool IsPingGraphEnabled() => App.settingsManager?.GetBool("smoothing_ping_graph", false) == true;
    public static bool IsTickrateGraphOverlayEnabled() => App.settingsManager?.GetBool("smoothing_tickrate_graph_overlay", false, "ADVANCED") == true;
    public static bool IsTicktimeGraphOverlayEnabled() => App.settingsManager?.GetBool("smoothing_ticktime_graph_overlay", false, "ADVANCED") == true;
    public static bool IsPingGraphOverlayEnabled() => App.settingsManager?.GetBool("smoothing_ping_graph_overlay", false, "ADVANCED") == true;
    public static bool IsPingValueOverlayEnabled() => App.settingsManager?.GetBool("smoothing_ping_value_overlay", false, "ADVANCED") == true;
    public static bool IsPingValueGuiEnabled() => App.settingsManager?.GetBool("smoothing_ping_value_gui", false, "ADVANCED") == true;
    public static bool IsTickrateValueOverlayEnabled() => App.settingsManager?.GetBool("smoothing_tickrate_value_overlay", false, "ADVANCED") == true;
    public static bool IsTrafficValueOverlayEnabled() => App.settingsManager?.GetBool("smoothing_traffic_value_overlay", false, "ADVANCED") == true;

        // --- Значения ---
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

        // --- Overlay значения ---
        public static int SmoothPingValueOverlay(int raw)
        {
            if (!IsPingValueOverlayEnabled() || raw <= 0) return raw;
            lock (_lock)
            {
                if (_emaPingValueOverlay == null)
                {
                    _emaPingValueOverlay = new ExponentialMovingAverage(GetAlpha());
                }
                return (int)Math.Round(_emaPingValueOverlay.Update(raw));
            }
        }

        public static int SmoothPingValueGui(int raw)
        {
            if (!IsPingValueGuiEnabled() || raw <= 0) return raw;
            lock (_lock)
            {
                if (_emaPingValueGui == null)
                {
                    _emaPingValueGui = new ExponentialMovingAverage(GetAlpha());
                }
                return (int)Math.Round(_emaPingValueGui.Update(raw));
            }
        }

        public static int SmoothTickrateValueOverlay(int raw)
        {
            if (!IsTickrateValueOverlayEnabled() || raw <= 0) return raw;
            lock (_lock)
            {
                if (_emaTickrateValueOverlay == null)
                {
                    _emaTickrateValueOverlay = new ExponentialMovingAverage(GetAlpha());
                }
                return (int)Math.Round(_emaTickrateValueOverlay.Update(raw));
            }
        }

        public static float SmoothUploadMbOverlay(float rawMb)
        {
            if (!IsTrafficValueOverlayEnabled() || rawMb < 0) return rawMb;
            lock (_lock)
            {
                if (_emaUploadMbOverlay == null)
                {
                    _emaUploadMbOverlay = new ExponentialMovingAverage(GetAlpha());
                }
                return (float)_emaUploadMbOverlay.Update(rawMb);
            }
        }

        public static float SmoothDownloadMbOverlay(float rawMb)
        {
            if (!IsTrafficValueOverlayEnabled() || rawMb < 0) return rawMb;
            lock (_lock)
            {
                if (_emaDownloadMbOverlay == null)
                {
                    _emaDownloadMbOverlay = new ExponentialMovingAverage(GetAlpha());
                }
                return (float)_emaDownloadMbOverlay.Update(rawMb);
            }
        }

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
                _emaPingValue?.Reset();
                _emaUploadMb?.Reset();
                _emaDownloadMb?.Reset();
                _emaPingValueOverlay?.Reset();
                _emaTickrateValueOverlay?.Reset();
                _emaUploadMbOverlay?.Reset();
                _emaDownloadMbOverlay?.Reset();
                
                _emaPingValue = null;
                _emaUploadMb = null;
                _emaDownloadMb = null;
                _emaPingValueOverlay = null;
                _emaTickrateValueOverlay = null;
                _emaUploadMbOverlay = null;
                _emaDownloadMbOverlay = null;
            }
        }
    }
}
