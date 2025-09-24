using System;
using System.Collections.Generic;

namespace tickMeter.Classes.SpikeDetection
{
    /// <summary>
    /// Типы метрик для детекции спайков
    /// </summary>
    public enum MetricKind
    {
        Ping,
        Tickrate,
        Ticktime
    }

    /// <summary>
    /// Событие обнаружения спайка
    /// </summary>
    public struct SpikeEvent
    {
        public DateTime Timestamp { get; set; }
        public MetricKind Metric { get; set; }
        public double Value { get; set; }
        public double Baseline { get; set; }
        public double Threshold { get; set; }
        public double Energy { get; set; }
        public TimeSpan Duration { get; set; }
        
        public SpikeEvent(DateTime timestamp, MetricKind metric, double value, double baseline, double threshold, double energy)
        {
            Timestamp = timestamp;
            Metric = metric;
            Value = value;
            Baseline = baseline;
            Threshold = threshold;
            Energy = energy;
            Duration = TimeSpan.Zero;
        }
    }

    /// <summary>
    /// Интерфейс детектора спайков
    /// </summary>
    public interface ISpikeDetector
    {
        /// <summary>
        /// Событие обнаружения спайка
        /// </summary>
        event Action<SpikeEvent> SpikeDetected;

        /// <summary>
        /// Добавить новое значение метрики для анализа
        /// </summary>
        /// <param name="metric">Тип метрики</param>
        /// <param name="value">Значение метрики</param>
        /// <param name="timestamp">Временная метка</param>
        void AddValue(MetricKind metric, double value, DateTime timestamp);

        /// <summary>
        /// Проверить наличие активного спайка для указанной метрики
        /// </summary>
        /// <param name="metric">Тип метрики</param>
        /// <returns>True если спайк активен</returns>
        bool HasActiveSpike(MetricKind metric);

        /// <summary>
        /// Получить текущую базовую линию для метрики
        /// </summary>
        /// <param name="metric">Тип метрики</param>
        /// <returns>Значение базовой линии или NaN если недостаточно данных</returns>
        double GetBaseline(MetricKind metric);

        /// <summary>
        /// Получить текущий порог детекции для метрики
        /// </summary>
        /// <param name="metric">Тип метрики</param>
        /// <returns>Значение порога или NaN если недостаточно данных</returns>
        double GetThreshold(MetricKind metric);

        /// <summary>
        /// Очистить историю и сбросить состояние детектора
        /// </summary>
        void Reset();

        /// <summary>
        /// Обновить настройки детектора
        /// </summary>
        /// <param name="settings">Новые настройки</param>
        void UpdateSettings(SpikeDetectorSettings settings);
    }

    /// <summary>
    /// Настройки детектора спайков
    /// </summary>
    public class SpikeDetectorSettings
    {
        // Общие настройки
        public bool Enabled { get; set; } = true;
        public List<MetricKind> EnabledMetrics { get; set; } = new List<MetricKind> { MetricKind.Ping };
        
        // EMA параметры
        public double EmaAlpha { get; set; } = 0.1;
        
        // EW-σ параметры  
        public double EwSigmaAlpha { get; set; } = 0.05;
        public double SensitivityMultiplier { get; set; } = 2.0;
        
        // Гистерезис
        public double HysteresisRatio { get; set; } = 0.8;
        
        // Рефракторный период (мс)
        public int RefractoryPeriodMs { get; set; } = 1000;
        
        // Энергетические пороги
        public double MinEnergyThreshold { get; set; } = 1.0;
        
        // Минимальная длительность спайка (мс)
        public int MinSpikeDurationMs { get; set; } = 100;
        
        // Размер окна для инициализации
        public int InitWindowSize { get; set; } = 20;

        public SpikeDetectorSettings()
        {
            EnabledMetrics = new List<MetricKind> { MetricKind.Ping };
        }
    }
}