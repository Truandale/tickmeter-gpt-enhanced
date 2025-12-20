using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                settings.Enabled = settingsManager.GetOption("spikes.enable", "True", "ADVANCED") == "True";

                // Загружаем включенные метрики из новой настройки Stage 4
                settings.EnabledMetrics.Clear();
                var metrics = settingsManager.GetOption("spikes.metrics", "ping,tickrate", "ADVANCED").ToLowerInvariant();
                if (metrics.Contains("ping"))
                    settings.EnabledMetrics.Add(MetricKind.Ping);
                if (metrics.Contains("tickrate"))
                    settings.EnabledMetrics.Add(MetricKind.Tickrate);
                if (metrics.Contains("ticktime"))
                    settings.EnabledMetrics.Add(MetricKind.Ticktime);

                // Используем расширенные настройки Stage 4 напрямую, если они есть
                var useAdvancedSettings = settingsManager.GetOption("spikes.ema_alpha", "", "ADVANCED");
                
                if (!string.IsNullOrEmpty(useAdvancedSettings))
                {
                    // Stage 4: используем точные значения из расширенных настроек
                    settings.EmaAlpha = settingsManager.GetDouble("spikes.ema_alpha", 0.1, "ADVANCED");
                    settings.EwSigmaAlpha = settingsManager.GetDouble("spikes.ew_sigma_alpha", 0.05, "ADVANCED");
                    settings.SensitivityMultiplier = settingsManager.GetDouble("spikes.sensitivity_multiplier", 2.0, "ADVANCED");
                    settings.HysteresisRatio = Math.Max(0.5, Math.Min(0.95, settingsManager.GetDouble("spikes.hysteresis_ratio", 0.8, "ADVANCED")));
                    settings.RefractoryPeriodMs = settingsManager.GetInt("spikes.refractory_period_ms", 1000, "ADVANCED");
                    settings.MinEnergyThreshold = settingsManager.GetDouble("spikes.min_energy_threshold", 1.0, "ADVANCED");
                    settings.InitWindowSize = settingsManager.GetInt("spikes.init_window_size", 20, "ADVANCED");
                }
                else
                {
                    // Fallback: используем старый метод через sensitivity preset
                    string sensitivityStr = settingsManager.GetOption("spikes.sensitivity", "medium", "ADVANCED");
                    switch (sensitivityStr?.ToLower())
                    {
                        case "low":
                            settings.SensitivityMultiplier = 3.0;
                            settings.EmaAlpha = 0.05;
                            settings.EwSigmaAlpha = 0.02;
                            settings.HysteresisRatio = 0.7;
                            settings.RefractoryPeriodMs = 2000;
                            settings.MinEnergyThreshold = 2.0;
                            break;
                        case "high":
                            settings.SensitivityMultiplier = 1.5;
                            settings.EmaAlpha = 0.2;
                            settings.EwSigmaAlpha = 0.1;
                            settings.HysteresisRatio = 0.9;
                            settings.RefractoryPeriodMs = 500;
                            settings.MinEnergyThreshold = 0.5;
                            break;
                        case "medium":
                        default:
                            settings.SensitivityMultiplier = 2.0;
                            settings.EmaAlpha = 0.1;
                            settings.EwSigmaAlpha = 0.05;
                            settings.HysteresisRatio = 0.8;
                            settings.RefractoryPeriodMs = 1000;
                            settings.MinEnergyThreshold = 1.0;
                            break;
                    }
                    settings.InitWindowSize = 20;
                }

                // Минимальная длительность спайка
                settings.MinSpikeDurationMs = settingsManager.GetInt("spikes.min_hold_ms", 120, "ADVANCED");
                settings.MinDeltaTimeSeconds = Math.Max(0.001, settingsManager.GetDouble("spikes.min_dt_sec", settings.MinDeltaTimeSeconds, "ADVANCED"));
                settings.MaxDeltaTimeSeconds = Math.Max(settings.MinDeltaTimeSeconds, settingsManager.GetDouble("spikes.max_dt_sec", settings.MaxDeltaTimeSeconds, "ADVANCED"));
                settings.DefaultSampleIntervalSeconds = Math.Max(settings.MinDeltaTimeSeconds, settingsManager.GetDouble("spikes.default_dt_sec", settings.DefaultSampleIntervalSeconds, "ADVANCED"));

                // Критическое исправление #8: Загружаем специфичные коэффициенты для метрик
                LoadMetricSpecificCoefficients(settings, settingsManager);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[SpikeDetectionManager] Error loading settings: {ex.Message}");
                // Возвращаем настройки по умолчанию при ошибке
            }

            return settings;
        }

        /// <summary>
        /// Загружает специфичные для каждой метрики коэффициенты чувствительности
        /// Критическое исправление #8 по рекомендации ChatGPT
        /// </summary>
        private static void LoadMetricSpecificCoefficients(SpikeDetectorSettings settings, SettingsManager settingsManager)
        {
            try
            {
                // Загружаем специфичные коэффициенты или используем значения по умолчанию
                double pingCoeff = settingsManager.GetDouble("spikes.ping_sensitivity", 2.0, "ADVANCED");
                double ticktimeCoeff = settingsManager.GetDouble("spikes.ticktime_sensitivity", 2.5, "ADVANCED");
                double tickrateCoeff = settingsManager.GetDouble("spikes.tickrate_sensitivity", 2.0, "ADVANCED");

                // Обновляем коэффициенты
                settings.MetricSensitivityCoefficients[MetricKind.Ping] = pingCoeff;
                settings.MetricSensitivityCoefficients[MetricKind.Ticktime] = ticktimeCoeff;
                settings.MetricSensitivityCoefficients[MetricKind.Tickrate] = tickrateCoeff;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[SpikeDetectionManager] Error loading metric coefficients: {ex.Message}");
                // Используем значения по умолчанию при ошибке - они уже установлены в конструкторе
            }
        }

        /// <summary>
        /// Обработчик события обнаружения спайка
        /// </summary>
        /// <param name="spikeEvent">Событие спайка</param>
        private static void OnSpikeDetected(SpikeEvent spikeEvent)
        {
            try
            {
                System.Diagnostics.Debug.Print($"[SpikeDetection] Spike {spikeEvent.Phase}: {spikeEvent.Metric} at {spikeEvent.Timestamp:HH:mm:ss.fff}, " +
                    $"value={spikeEvent.Value:F2}, baseline={spikeEvent.Baseline:F2}, threshold={spikeEvent.Threshold:F2}, energy={spikeEvent.Energy:F2}, " +
                    $"duration={spikeEvent.Duration.TotalMilliseconds:F0}ms, confirmed={spikeEvent.IsConfirmed}");

                // Вызываем глобальное событие
                SpikeDetected?.Invoke(spikeEvent);
                
                // Отправляем алерт (Этап 8: Advanced Alerting System)
                SendSpikeAlert(spikeEvent);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[SpikeDetectionManager] Error in spike event handler: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Отправляет алерт о спайке (Этап 8: Advanced Alerting System)
        /// </summary>
        /// <param name="spikeEvent">Событие спайка</param>
        private static void SendSpikeAlert(SpikeEvent spikeEvent)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    if (spikeEvent.Phase != SpikeEventPhase.End || !spikeEvent.IsConfirmed)
                    {
                        return;
                    }

                    // Определяем тип алерта на основе метрики
                    AlertManager.AlertType alertType;
                    switch (spikeEvent.Metric)
                    {
                        case MetricKind.Ping:
                            alertType = AlertManager.AlertType.PingSpike;
                            break;
                        case MetricKind.Tickrate:
                            alertType = AlertManager.AlertType.TickrateSpike;
                            break;
                        case MetricKind.Ticktime:
                            alertType = AlertManager.AlertType.TicktimeSpike;
                            break;
                        default:
                            alertType = AlertManager.AlertType.CriticalSpike;
                            break;
                    }
                    
                    // Отправляем алерт
                    await AlertManager.SendAlert(
                        alertType,
                        spikeEvent.Metric.ToString(),
                        spikeEvent.PeakValue,
                        spikeEvent.Threshold
                    );
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print($"[SpikeDetectionManager] Error sending spike alert: {ex.Message}");
                }
            });
        }
    }
}