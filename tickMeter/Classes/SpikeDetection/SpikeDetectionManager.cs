using System;
using System.Collections.Generic;
using System.Linq;

namespace tickMeter.Classes.SpikeDetection
{
    /// <summary>
    /// Менеджер детекторов спайков
    /// Управляет созданием и настройкой детекторов на основе пользовательских настроек
    /// </summary>
    public static class SpikeDetectionManager
    {
        private static ISpikeDetector _currentDetector;
        private static readonly object _lock = new object();

        /// <summary>
        /// Событие обнаружения спайка (глобальное)
        /// </summary>
        public static event Action<SpikeEvent> SpikeDetected;

        /// <summary>
        /// Получить или создать текущий детектор спайков
        /// </summary>
        /// <returns>Экземпляр детектора или null если детекция отключена</returns>
        public static ISpikeDetector GetDetector()
        {
            lock (_lock)
            {
                if (_currentDetector == null)
                {
                    InitializeDetector();
                }
                return _currentDetector;
            }
        }

        /// <summary>
        /// Инициализировать детектор на основе текущих настроек
        /// </summary>
        public static void InitializeDetector()
        {
            lock (_lock)
            {
                try
                {
                    var settings = LoadSettingsFromConfig();
                    
                    if (!settings.Enabled)
                    {
                        _currentDetector = null;
                        return;
                    }

                    // Создаем новый детектор
                    var detector = new SpikeDetector(settings);
                    
                    // Подписываемся на события
                    detector.SpikeDetected += OnSpikeDetected;

                    _currentDetector = detector;
                }
                catch (Exception ex)
                {
                    // В случае ошибки отключаем детекцию
                    System.Diagnostics.Debug.Print($"[SpikeDetectionManager] Error initializing detector: {ex.Message}");
                    _currentDetector = null;
                }
            }
        }

        /// <summary>
        /// Обновить настройки детектора
        /// </summary>
        public static void UpdateSettings()
        {
            lock (_lock)
            {
                var settings = LoadSettingsFromConfig();
                
                if (!settings.Enabled)
                {
                    // Отключаем детекцию
                    if (_currentDetector != null)
                    {
                        _currentDetector.SpikeDetected -= OnSpikeDetected;
                        _currentDetector = null;
                    }
                    return;
                }

                if (_currentDetector != null)
                {
                    // Обновляем существующий детектор
                    _currentDetector.UpdateSettings(settings);
                }
                else
                {
                    // Создаем новый детектор
                    InitializeDetector();
                }
            }
        }

        /// <summary>
        /// Добавить значение метрики для анализа
        /// </summary>
        /// <param name="metric">Тип метрики</param>
        /// <param name="value">Значение</param>
        /// <param name="timestamp">Временная метка (опционально)</param>
        public static void AddValue(MetricKind metric, double value, DateTime? timestamp = null)
        {
            var detector = GetDetector();
            if (detector != null)
            {
                detector.AddValue(metric, value, timestamp ?? DateTime.Now);
            }
        }

        /// <summary>
        /// Проверить наличие активного спайка для метрики
        /// </summary>
        /// <param name="metric">Тип метрики</param>
        /// <returns>True если спайк активен</returns>
        public static bool HasActiveSpike(MetricKind metric)
        {
            var detector = GetDetector();
            return detector?.HasActiveSpike(metric) == true;
        }

        /// <summary>
        /// Сбросить состояние детектора
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _currentDetector?.Reset();
            }
        }

        /// <summary>
        /// Загрузить настройки из конфигурации приложения
        /// </summary>
        /// <returns>Настройки детектора</returns>
        private static SpikeDetectorSettings LoadSettingsFromConfig()
        {
            var settings = new SpikeDetectorSettings();

            try
            {
                // Загружаем настройки из App.settingsManager
                var settingsManager = App.settingsManager;
                if (settingsManager == null)
                {
                    return settings; // Возвращаем настройки по умолчанию
                }

                // Основные настройки
                settings.Enabled = settingsManager.GetBool("spike_detection_enabled", true);

                // Загружаем включенные метрики
                settings.EnabledMetrics.Clear();
                if (settingsManager.GetBool("spike_detect_ping", true))
                    settings.EnabledMetrics.Add(MetricKind.Ping);
                if (settingsManager.GetBool("spike_detect_tickrate", false))
                    settings.EnabledMetrics.Add(MetricKind.Tickrate);
                if (settingsManager.GetBool("spike_detect_ticktime", false))
                    settings.EnabledMetrics.Add(MetricKind.Ticktime);

                // Параметры чувствительности
                string sensitivityStr = settingsManager.GetOption("spike_sensitivity", "Medium", "ADVANCED");
                switch (sensitivityStr?.ToLower())
                {
                    case "low":
                        settings.SensitivityMultiplier = 3.0;
                        settings.EmaAlpha = 0.05;
                        settings.EwSigmaAlpha = 0.02;
                        break;
                    case "high":
                        settings.SensitivityMultiplier = 1.5;
                        settings.EmaAlpha = 0.2;
                        settings.EwSigmaAlpha = 0.1;
                        break;
                    case "medium":
                    default:
                        settings.SensitivityMultiplier = 2.0;
                        settings.EmaAlpha = 0.1;
                        settings.EwSigmaAlpha = 0.05;
                        break;
                }

                // Дополнительные параметры из Advanced секции
                settings.HysteresisRatio = settingsManager.GetDouble("spike_hysteresis_ratio", 0.8, "ADVANCED");
                settings.RefractoryPeriodMs = settingsManager.GetInt("spike_refractory_ms", 1000, "ADVANCED");
                settings.MinEnergyThreshold = settingsManager.GetDouble("spike_min_energy", 1.0, "ADVANCED");
                settings.MinSpikeDurationMs = settingsManager.GetInt("spike_min_duration_ms", 100, "ADVANCED");
                settings.InitWindowSize = settingsManager.GetInt("spike_init_window", 20, "ADVANCED");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[SpikeDetectionManager] Error loading settings: {ex.Message}");
                // Возвращаем настройки по умолчанию при ошибке
            }

            return settings;
        }

        /// <summary>
        /// Обработчик события обнаружения спайка
        /// </summary>
        /// <param name="spikeEvent">Событие спайка</param>
        private static void OnSpikeDetected(SpikeEvent spikeEvent)
        {
            try
            {
                System.Diagnostics.Debug.Print($"[SpikeDetection] Spike detected: {spikeEvent.Metric} at {spikeEvent.Timestamp:HH:mm:ss.fff}, " +
                    $"baseline={spikeEvent.Baseline:F1}, threshold={spikeEvent.Threshold:F1}, energy={spikeEvent.Energy:F2}, " +
                    $"duration={spikeEvent.Duration.TotalMilliseconds:F0}ms");

                // Вызываем глобальное событие
                SpikeDetected?.Invoke(spikeEvent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[SpikeDetectionManager] Error in spike event handler: {ex.Message}");
            }
        }
    }
}