using System;
using System.Collections.Generic;
using System.Linq;

namespace tickMeter.Classes.SpikeDetection
{
    /// <summary>
    /// Состояние метрики для детекции спайков
    /// </summary>
    internal class MetricState
    {
        public double EmaValue { get; set; } = double.NaN;
        public double EwSigma { get; set; } = double.NaN;
        public double CurrentThreshold { get; set; } = double.NaN;
        public bool IsInSpike { get; set; } = false;
        public DateTime LastSpikeTime { get; set; } = DateTime.MinValue;
        public DateTime SpikeStartTime { get; set; } = DateTime.MinValue;
        public double SpikeEnergy { get; set; } = 0.0;
        public Queue<double> InitWindow { get; set; } = new Queue<double>();
        public bool IsInitialized { get; set; } = false;
    }

    /// <summary>
    /// Основная реализация детектора спайков
    /// Использует алгоритм EMA + EW-σ + гистерезис + рефракторный период + энергия
    /// </summary>
    public class SpikeDetector : ISpikeDetector
    {
        private SpikeDetectorSettings _settings;
        private readonly Dictionary<MetricKind, MetricState> _metricStates;
        private readonly object _lock = new object();

        public event Action<SpikeEvent> SpikeDetected;

        public SpikeDetector(SpikeDetectorSettings settings = null)
        {
            _settings = settings ?? new SpikeDetectorSettings();
            _metricStates = new Dictionary<MetricKind, MetricState>();
            
            // Инициализируем состояния для всех типов метрик
            foreach (MetricKind metric in Enum.GetValues(typeof(MetricKind)))
            {
                _metricStates[metric] = new MetricState();
            }
        }

        public void AddValue(MetricKind metric, double value, DateTime timestamp)
        {
            if (!_settings.Enabled || !_settings.EnabledMetrics.Contains(metric))
                return;

            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                return;

            lock (_lock)
            {
                var state = _metricStates[metric];
                
                // Фаза инициализации: накапливаем данные для расчета начальных значений
                if (!state.IsInitialized)
                {
                    InitializeMetric(state, value);
                    return;
                }

                // Обновляем EMA (экспоненциальное скользящее среднее)
                UpdateEma(state, value);
                
                // Обновляем EW-σ (экспоненциально взвешенное стандартное отклонение)
                UpdateEwSigma(state, value);
                
                // Рассчитываем текущий порог с учетом чувствительности
                double threshold = state.EmaValue + (_settings.SensitivityMultiplier * state.EwSigma);
                state.CurrentThreshold = threshold;
                
                // Проверяем детекцию спайка
                CheckSpikeDetection(metric, state, value, timestamp, threshold);
            }
        }

        private void InitializeMetric(MetricState state, double value)
        {
            state.InitWindow.Enqueue(value);
            
            if (state.InitWindow.Count >= _settings.InitWindowSize)
            {
                // Рассчитываем начальные значения EMA и EW-σ
                var values = state.InitWindow.ToArray();
                state.EmaValue = values.Average();
                
                // Начальное стандартное отклонение
                double variance = values.Select(v => Math.Pow(v - state.EmaValue, 2)).Average();
                state.EwSigma = Math.Sqrt(variance);
                
                state.IsInitialized = true;
                state.InitWindow.Clear(); // Освобождаем память
            }
        }

        private void UpdateEma(MetricState state, double value)
        {
            if (double.IsNaN(state.EmaValue))
            {
                state.EmaValue = value;
            }
            else
            {
                state.EmaValue = _settings.EmaAlpha * value + (1 - _settings.EmaAlpha) * state.EmaValue;
            }
        }

        private void UpdateEwSigma(MetricState state, double value)
        {
            if (double.IsNaN(state.EwSigma))
            {
                state.EwSigma = Math.Abs(value - state.EmaValue);
            }
            else
            {
                double deviation = Math.Abs(value - state.EmaValue);
                state.EwSigma = _settings.EwSigmaAlpha * deviation + (1 - _settings.EwSigmaAlpha) * state.EwSigma;
            }
        }

        private void CheckSpikeDetection(MetricKind metric, MetricState state, double value, DateTime timestamp, double threshold)
        {
            // Проверяем рефракторный период
            bool inRefractoryPeriod = (timestamp - state.LastSpikeTime).TotalMilliseconds < _settings.RefractoryPeriodMs;
            
            if (state.IsInSpike)
            {
                // Логика завершения спайка с гистерезисом
                double exitThreshold = state.EmaValue + (_settings.HysteresisRatio * _settings.SensitivityMultiplier * state.EwSigma);
                
                if (value <= exitThreshold)
                {
                    // Завершаем спайк
                    EndSpike(metric, state, timestamp);
                }
                else
                {
                    // Продолжаем накапливать энергию спайка
                    double excessValue = Math.Max(0, value - state.EmaValue);
                    state.SpikeEnergy += excessValue * excessValue; // Квадратичная энергия
                }
            }
            else
            {
                // Логика начала спайка
                if (value > threshold && !inRefractoryPeriod)
                {
                    StartSpike(metric, state, value, timestamp, threshold);
                }
            }
        }

        private void StartSpike(MetricKind metric, MetricState state, double value, DateTime timestamp, double threshold)
        {
            state.IsInSpike = true;
            state.SpikeStartTime = timestamp;
            
            // Начальная энергия спайка
            double excessValue = Math.Max(0, value - state.EmaValue);
            state.SpikeEnergy = excessValue * excessValue;
        }

        private void EndSpike(MetricKind metric, MetricState state, DateTime timestamp)
        {
            var spikeDuration = timestamp - state.SpikeStartTime;
            
            // Проверяем минимальную длительность и энергию спайка
            if (spikeDuration.TotalMilliseconds >= _settings.MinSpikeDurationMs && 
                state.SpikeEnergy >= _settings.MinEnergyThreshold)
            {
                // Создаем событие спайка
                var spikeEvent = new SpikeEvent(
                    state.SpikeStartTime,
                    metric,
                    0, // Значение будет установлено позже если нужно
                    state.EmaValue,
                    state.CurrentThreshold,
                    state.SpikeEnergy
                );
                spikeEvent.Duration = spikeDuration;
                
                // Вызываем событие
                SpikeDetected?.Invoke(spikeEvent);
            }
            
            // Сбрасываем состояние спайка
            state.IsInSpike = false;
            state.LastSpikeTime = timestamp;
            state.SpikeEnergy = 0.0;
        }

        public bool HasActiveSpike(MetricKind metric)
        {
            lock (_lock)
            {
                return _metricStates.ContainsKey(metric) && _metricStates[metric].IsInSpike;
            }
        }

        public double GetBaseline(MetricKind metric)
        {
            lock (_lock)
            {
                return _metricStates.ContainsKey(metric) ? _metricStates[metric].EmaValue : double.NaN;
            }
        }

        public double GetThreshold(MetricKind metric)
        {
            lock (_lock)
            {
                return _metricStates.ContainsKey(metric) ? _metricStates[metric].CurrentThreshold : double.NaN;
            }
        }

        public void Reset()
        {
            lock (_lock)
            {
                foreach (var state in _metricStates.Values)
                {
                    state.EmaValue = double.NaN;
                    state.EwSigma = double.NaN;
                    state.CurrentThreshold = double.NaN;
                    state.IsInSpike = false;
                    state.LastSpikeTime = DateTime.MinValue;
                    state.SpikeStartTime = DateTime.MinValue;
                    state.SpikeEnergy = 0.0;
                    state.InitWindow.Clear();
                    state.IsInitialized = false;
                }
            }
        }

        public void UpdateSettings(SpikeDetectorSettings settings)
        {
            lock (_lock)
            {
                _settings = settings ?? new SpikeDetectorSettings();
                
                // При изменении настроек можем сбросить состояние для корректной работы
                // или оставить как есть для плавного перехода
            }
        }
    }
}