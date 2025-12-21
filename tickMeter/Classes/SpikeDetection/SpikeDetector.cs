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
        public DateTime LastUpdateTime { get; set; } = DateTime.MinValue; // Для расчета Δt
        public double SpikeEnergy { get; set; } = 0.0;
        public Queue<double> InitWindow { get; set; } = new Queue<double>();
        public bool IsInitialized { get; set; } = false;
        public double LastValue { get; set; } = double.NaN;
        public double PeakValue { get; set; } = double.NaN;
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

                // Обновляем EMA и EW-σ одновременно (исправленный алгоритм)
                UpdateEmaAndSigma(state, value);
                
                // Рассчитываем текущий порог с учетом чувствительности и направленности
                double threshold = CalculateThreshold(metric, state.EmaValue, state.EwSigma);
                state.CurrentThreshold = threshold;

                state.LastValue = value;
                
                // Проверяем детекцию спайка
                CheckSpikeDetection(metric, state, value, timestamp, threshold);
            }
        }

        private void InitializeMetric(MetricState state, double value)
        {
            state.LastValue = value;
            if (double.IsNaN(state.PeakValue))
            {
                state.PeakValue = value;
            }

            state.InitWindow.Enqueue(value);
            
            if (state.InitWindow.Count >= _settings.InitWindowSize)
            {
                // Рассчитываем начальные значения EMA и EW-σ
                var values = state.InitWindow.ToArray();
                state.EmaValue = values.Average();
                
                // Начальное стандартное отклонение
                double variance = values.Select(v => Math.Pow(v - state.EmaValue, 2)).Average();
                state.EwSigma = Math.Sqrt(variance);
                state.LastValue = values.Last();
                state.PeakValue = values.Last();
                
                state.IsInitialized = true;
                state.InitWindow.Clear(); // Освобождаем память
            }
        }

        private void UpdateEmaAndSigma(MetricState state, double value)
        {
            // Критическое исправление #1: Правильный порядок обновления EMA и EW-σ
            // по формуле Дж. Уэйнрайта для численной стабильности
            
            if (double.IsNaN(state.EmaValue))
            {
                // Первое значение
                state.EmaValue = value;
                state.EwSigma = 0.1; // Минимальная σ для избежания деления на ноль
                state.LastValue = value;
                state.PeakValue = value;
                return;
            }

            // Определяем коэффициенты α и β в зависимости от состояния спайка
            double alpha = _settings.EmaAlpha;
            double beta = _settings.EwSigmaAlpha;
            
            // Критическое исправление #3: Заморозка базовой линии во время спайка
            if (state.IsInSpike)
            {
                alpha /= 10.0; // Сильно уменьшаем обновление среднего
                beta /= 10.0;  // Сильно уменьшаем обновление σ
            }

            // Правильная формула обновления по Уэйнрайту:
            double m_prev = state.EmaValue;
            double m_new = m_prev + alpha * (value - m_prev);
            
            // EW-variance: используем разность к старому И новому среднему
            double variance_update = beta * (value - m_prev) * (value - m_new);
            double current_variance = Math.Max(0, state.EwSigma * state.EwSigma); // σ² -> variance
            double new_variance = (1 - beta) * current_variance + variance_update;
            
            // Обновляем состояние
            state.EmaValue = m_new;
            state.EwSigma = Math.Sqrt(Math.Max(new_variance, 1e-6)); // Минимальная σ для стабильности
        }

        /// <summary>
        /// Критическое исправление #1: Направленная детекция спайков
        /// Ping, TickTime — спайк только "вверх": x > μ + k·σ
        /// TickRate — спайк "вниз": x < μ - k·σ
        /// </summary>
        private bool IsSpikeOn(MetricKind metric, double value, double baseline, double sigma)
        {
            // Получаем специфичный коэффициент для метрики
            double k = GetMetricSensitivityCoefficient(metric);
            
            switch (metric)
            {
                case MetricKind.Tickrate:
                    // TickRate: спайк когда значение падает ниже нормы
                    return value < (baseline - k * sigma);
                
                case MetricKind.Ping:
                case MetricKind.Ticktime:
                default:
                    // Ping, TickTime: спайк когда значение растет выше нормы
                    return value > (baseline + k * sigma);
            }
        }

        /// <summary>
        /// Направленная проверка выхода из спайка с гистерезисом
        /// </summary>
        private bool IsSpikeOff(MetricKind metric, double value, double baseline, double sigma)
        {
            // Получаем специфичный коэффициент для метрики с гистерезисом
            double k_off = GetMetricSensitivityCoefficient(metric) * _settings.HysteresisRatio;
            
            switch (metric)
            {
                case MetricKind.Tickrate:
                    // TickRate: выходим из спайка когда значение поднимается выше порога выхода
                    return value > (baseline - k_off * sigma);
                
                case MetricKind.Ping:
                case MetricKind.Ticktime:
                default:
                    // Ping, TickTime: выходим из спайка когда значение падает ниже порога выхода
                    return value < (baseline + k_off * sigma);
            }
        }

        /// <summary>
        /// Получить коэффициент чувствительности для конкретной метрики
        /// </summary>
        private double GetMetricSensitivityCoefficient(MetricKind metric)
        {
            if (_settings.MetricSensitivityCoefficients != null && 
                _settings.MetricSensitivityCoefficients.ContainsKey(metric))
            {
                return _settings.MetricSensitivityCoefficients[metric];
            }
            
            // Fallback на общий множитель
            return _settings.SensitivityMultiplier;
        }

        /// <summary>
        /// Рассчитать порог для метрики с учетом направленности
        /// </summary>
        private double CalculateThreshold(MetricKind metric, double baseline, double sigma)
        {
            double k = GetMetricSensitivityCoefficient(metric);
            
            switch (metric)
            {
                case MetricKind.Tickrate:
                    return baseline - k * sigma; // Порог снизу для TickRate
                
                case MetricKind.Ping:
                case MetricKind.Ticktime:
                default:
                    return baseline + k * sigma; // Порог сверху для Ping, TickTime
            }
        }

        private void CheckSpikeDetection(MetricKind metric, MetricState state, double value, DateTime timestamp, double threshold)
        {
            // Проверяем рефракторный период
            bool inRefractoryPeriod = (timestamp - state.LastSpikeTime).TotalMilliseconds < _settings.RefractoryPeriodMs;
            
            // Критическое исправление #4: Энергия по времени, а не по тикам
            double deltaTime;
            if (state.LastUpdateTime != DateTime.MinValue)
            {
                deltaTime = (timestamp - state.LastUpdateTime).TotalSeconds;
                if (double.IsNaN(deltaTime) || double.IsInfinity(deltaTime) || deltaTime <= 0)
                {
                    deltaTime = _settings.DefaultSampleIntervalSeconds;
                }
            }
            else
            {
                deltaTime = _settings.DefaultSampleIntervalSeconds;
            }

            deltaTime = Clamp(deltaTime, _settings.MinDeltaTimeSeconds, _settings.MaxDeltaTimeSeconds);
            state.LastUpdateTime = timestamp;
            
            if (state.IsInSpike)
            {
                // Критическое исправление #1: Направленная логика завершения спайка с гистерезисом
                if (IsSpikeOff(metric, value, state.EmaValue, state.EwSigma))
                {
                    // Завершаем спайк
                    EndSpike(metric, state, timestamp);
                }
                else
                {
                    // Критическое исправление #4: Накапливаем энергию по времени
                    double baseline = state.EmaValue;
                    double threshold_on = GetMetricSensitivityCoefficient(metric) * state.EwSigma;
                    
                    double residual = 0.0;
                    switch (metric)
                    {
                        case MetricKind.Tickrate:
                            residual = Math.Max(0, baseline - value - threshold_on); // Спайк вниз
                            state.PeakValue = double.IsNaN(state.PeakValue) ? value : Math.Min(state.PeakValue, value);
                            break;
                        case MetricKind.Ping:
                        case MetricKind.Ticktime:
                        default:
                            residual = Math.Max(0, value - baseline - threshold_on); // Спайк вверх
                            state.PeakValue = double.IsNaN(state.PeakValue) ? value : Math.Max(state.PeakValue, value);
                            break;
                    }
                    
                    // E += (residual - threshold_on) * Δt
                    if (residual > 0 && deltaTime > 0)
                    {
                        state.SpikeEnergy += residual * deltaTime;
                    }
                }
            }
            else
            {
                // Критическое исправление #1: Направленная логика начала спайка
                if (IsSpikeOn(metric, value, state.EmaValue, state.EwSigma) && !inRefractoryPeriod)
                {
                    StartSpike(metric, state, value, timestamp, threshold);
                }
            }
        }

        private void StartSpike(MetricKind metric, MetricState state, double value, DateTime timestamp, double threshold)
        {
            state.IsInSpike = true;
            state.SpikeStartTime = timestamp;
            state.PeakValue = value;
            state.LastValue = value;
            
            // Начальная энергия спайка
            double excessValue = Math.Max(0, value - state.EmaValue);
            state.SpikeEnergy = excessValue * excessValue;

            var spikeEvent = new SpikeEvent(timestamp, metric, value, state.EmaValue, threshold, 0.0)
            {
                Phase = SpikeEventPhase.Start,
                IsConfirmed = false,
                PeakValue = value,
                LastValue = value
            };
            
            // Логирование начала спайка
            tickMeter.Classes.DebugLogger.log($"[Spike] {metric} START: value={value:F1} baseline={state.EmaValue:F1} threshold={threshold:F1} time={timestamp:HH:mm:ss.fff}");

            SpikeDetected?.Invoke(spikeEvent);
        }

        private void EndSpike(MetricKind metric, MetricState state, DateTime timestamp)
        {
            var spikeDuration = timestamp - state.SpikeStartTime;
            bool isConfirmed = spikeDuration.TotalMilliseconds >= _settings.MinSpikeDurationMs &&
                               state.SpikeEnergy >= _settings.MinEnergyThreshold;

            var spikeEvent = new SpikeEvent(state.SpikeStartTime, metric, state.PeakValue, state.EmaValue, state.CurrentThreshold, state.SpikeEnergy)
            {
                Duration = spikeDuration,
                Phase = SpikeEventPhase.End,
                IsConfirmed = isConfirmed,
                PeakValue = state.PeakValue,
                LastValue = state.LastValue
            };
            
            // Логирование окончания спайка
            tickMeter.Classes.DebugLogger.log($"[Spike] {metric} END: peak={state.PeakValue:F1} duration={spikeDuration.TotalMilliseconds:F0}ms energy={state.SpikeEnergy:F1} confirmed={isConfirmed}");

            SpikeDetected?.Invoke(spikeEvent);

            // Сбрасываем состояние спайка
            state.IsInSpike = false;
            state.LastSpikeTime = timestamp;
            state.SpikeEnergy = 0.0;
            state.PeakValue = double.NaN;
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
                    state.LastValue = double.NaN;
                    state.PeakValue = double.NaN;
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

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}