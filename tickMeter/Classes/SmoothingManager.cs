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

        // === КЭШИРОВАНИЕ для синхронизации GUI и Overlay ===
        // Сохраняем последнее сглаженное значение из GUI для использования в Overlay
        private static int _cachedSmoothedPing = 0;
        private static int _cachedRawPing = 0;  // Для отслеживания изменений
        
        private static int _cachedSmoothedTickrate = 0;
        private static int _cachedRawTickrate = 0;  // Для отслеживания изменений

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
        
        // Проверка режима синхронизации оверлея с GUI
        public static bool IsPingOverlaySyncWithGui() =>
            App.settingsManager?.GetBool("sync_ping_overlay_with_gui", true, "ADVANCED") == true;
        
        public static bool IsTickrateOverlaySyncWithGui() =>
            App.settingsManager?.GetBool("sync_tickrate_overlay_with_gui", true, "ADVANCED") == true;
            
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
                int smoothed = (int)Math.Round(_emaPingValue.Update(raw));
                
                // Сохраняем в кэш для использования в Overlay
                _cachedSmoothedPing = smoothed;
                _cachedRawPing = raw;
                
                // Логирование для анализа (только при значительном изменении)
                if (Math.Abs(raw - smoothed) > 5)
                {
                    DebugLogger.log($"[Smooth-Ping] Raw={raw}ms Smoothed={smoothed}ms Delta={raw-smoothed}ms Alpha={GetAlpha():F2}");
                }
                
                return smoothed;
            }
        }

        /// <summary>
        /// Получить кэшированное сглаженное значение пинга из GUI.
        /// Используется в Overlay для синхронизации отображения.
        /// Если сглаживание выключено или значения нет, возвращает RAW значение.
        /// </summary>
        public static int GetCachedSmoothedPing(int rawPing)
        {
            lock (_lock)
            {
                // Режим 1: Синхронизация с GUI через кэш
                if (IsPingOverlaySyncWithGui())
                {
                    // Если сглаживание выключено, возвращаем RAW
                    if (!IsPingValueEnabled())
                    {
                        return rawPing;
                    }
                    
                    // Если значение в кэше соответствует текущему RAW, используем кэш
                    if (_cachedRawPing == rawPing && _cachedSmoothedPing > 0)
                    {
                        return _cachedSmoothedPing;
                    }
                    
                    // Если кэш пуст или не синхронизирован, применяем сглаживание напрямую
                    // (это может произойти если Overlay обновляется раньше GUI)
                    if (_emaPingValue == null)
                    {
                        _emaPingValue = new ExponentialMovingAverage(GetAlpha());
                    }
                    return (int)Math.Round(_emaPingValue.Update(rawPing));
                }
                else
                {
                    // Режим 2: Независимое сглаживание через общую EMA (без кэша)
                    // Проверяем настройку сглаживания для оверлея
                    bool overlaySmoothing = App.settingsManager?.GetBool("smoothing_ping_value_overlay", false, "ADVANCED") == true;
                    
                    if (!overlaySmoothing || rawPing <= 0)
                    {
                        return rawPing;
                    }
                    
                    // Используем ту же EMA что и GUI, но без кэша
                    if (_emaPingValue == null)
                    {
                        _emaPingValue = new ExponentialMovingAverage(GetAlpha());
                    }
                    return (int)Math.Round(_emaPingValue.Update(rawPing));
                }
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
                int smoothed = (int)Math.Round(_emaTickrateValue.Update(raw));
                
                // Сохраняем в кэш для использования в Overlay
                _cachedSmoothedTickrate = smoothed;
                _cachedRawTickrate = raw;
                
                // Логирование для анализа (только при значительном изменении)
                if (Math.Abs(raw - smoothed) > 3)
                {
                    DebugLogger.log($"[Smooth-Tickrate] Raw={raw}Hz Smoothed={smoothed}Hz Delta={raw-smoothed}Hz Alpha={GetAlpha():F2}");
                }
                
                return smoothed;
            }
        }

        /// <summary>
        /// Получить сглаженное значение тикрейта для Overlay.
        /// Два режима работы:
        /// 1. Синхронизация с GUI (sync_tickrate_overlay_with_gui=true) - берет из кэша GUI
        /// 2. Независимое сглаживание (sync_tickrate_overlay_with_gui=false) - использует общую EMA напрямую
        /// </summary>
        public static int GetCachedSmoothedTickrate(int rawTickrate)
        {
            lock (_lock)
            {
                bool syncWithGui = App.settingsManager?.GetBool("sync_tickrate_overlay_with_gui", true, "ADVANCED") == true;
                
                // РЕЖИМ 1: Синхронизация с GUI через кэш
                if (syncWithGui)
                {
                    // Если сглаживание в GUI выключено, возвращаем RAW
                    if (!App.settingsManager?.GetBool("smoothing_tickrate_value_gui", false, "ADVANCED") == true)
                    {
                        return rawTickrate;
                    }
                    
                    // Если значение в кэше соответствует текущему RAW, используем кэш
                    if (_cachedRawTickrate == rawTickrate && _cachedSmoothedTickrate > 0)
                    {
                        return _cachedSmoothedTickrate;
                    }
                    
                    // Если кэш пуст или не синхронизирован, применяем сглаживание напрямую
                    if (_emaTickrateValue == null)
                    {
                        _emaTickrateValue = new ExponentialMovingAverage(GetAlpha());
                    }
                    return (int)Math.Round(_emaTickrateValue.Update(rawTickrate));
                }
                
                // РЕЖИМ 2: Независимое сглаживание через общую EMA (без кэша)
                bool overlaySmoothing = App.settingsManager?.GetBool("smoothing_tickrate_value_overlay", false, "ADVANCED") == true;
                
                if (!overlaySmoothing || rawTickrate <= 0)
                {
                    return rawTickrate;
                }
                
                // Используем ту же EMA что и GUI, но вызываем напрямую (не из кэша)
                if (_emaTickrateValue == null)
                {
                    _emaTickrateValue = new ExponentialMovingAverage(GetAlpha());
                }
                return (int)Math.Round(_emaTickrateValue.Update(rawTickrate));
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
                
                // Сбрасываем кэш
                _cachedSmoothedPing = 0;
                _cachedRawPing = 0;
            }
        }
    }
}
