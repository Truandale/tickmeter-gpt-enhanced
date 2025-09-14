using System;
using tickMeter.Classes;

namespace tickMeter.Classes
{
    /// <summary>
    /// Класс для экспоненциально сглаженного скользящего среднего (EMA)
    /// </summary>
    public class ExponentialMovingAverage
    {
        private double _alpha;
        private double? _current;
        private readonly object _lock = new object();
        
        /// <summary>
        /// Создает новый EMA с указанным коэффициентом сглаживания
        /// </summary>
        /// <param name="alpha">Коэффициент сглаживания (0 < alpha <= 1). Меньшие значения означают большее сглаживание</param>
        public ExponentialMovingAverage(double alpha = 0.1)
        {
            if (alpha <= 0 || alpha > 1)
                throw new ArgumentException("Alpha must be between 0 and 1", nameof(alpha));
            
            _alpha = alpha;
        }
        
        /// <summary>
        /// Обновляет EMA новым значением
        /// </summary>
        /// <param name="newValue">Новое значение для обновления</param>
        /// <returns>Текущее сглаженное значение</returns>
        public double Update(double newValue)
        {
            lock (_lock)
            {
                if (_current == null)
                {
                    // Первое значение
                    _current = newValue;
                }
                else
                {
                    // EMA формула: EMA_current = alpha * value + (1 - alpha) * EMA_previous
                    _current = _alpha * newValue + (1 - _alpha) * _current.Value;
                }
                
                return _current.Value;
            }
        }
        
        /// <summary>
        /// Получает текущее сглаженное значение
        /// </summary>
        public double? Current
        {
            get
            {
                lock (_lock)
                {
                    return _current;
                }
            }
        }
        
        /// <summary>
        /// Получает целочисленное сглаженное значение
        /// </summary>
        public int CurrentInt => (int)Math.Round(Current ?? 0);
        
        /// <summary>
        /// Сбрасывает EMA
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _current = null;
            }
        }
        
        /// <summary>
        /// Устанавливает новый коэффициент сглаживания
        /// </summary>
        /// <param name="alpha">Новый коэффициент сглаживания</param>
        public void SetAlpha(double alpha)
        {
            if (alpha <= 0 || alpha > 1)
                throw new ArgumentException("Alpha must be between 0 and 1", nameof(alpha));
            
            lock (_lock)
            {
                _alpha = alpha;
            }
        }
    }
    
    /// <summary>
    /// Менеджер для сглаживания показателей tickrate
    /// </summary>
    public static class TickrateSmoothingManager
    {
        private static ExponentialMovingAverage _tickrateEMA;
        private static readonly object _lock = new object();
        
        /// <summary>
        /// Инициализирует сглаживание tickrate
        /// </summary>
        public static void Initialize()
        {
            lock (_lock)
            {
                // Коэффициент можно сделать настраиваемым через SettingsManager
                double alpha = App.settingsManager?.GetString("tickrate_smoothing_alpha", "0.15") is string alphaStr
                    && double.TryParse(alphaStr, out double parsedAlpha) ? parsedAlpha : 0.15;
                
                _tickrateEMA = new ExponentialMovingAverage(alpha);
            }
        }
        
        /// <summary>
        /// Проверяет, включено ли сглаживание
        /// </summary>
        public static bool IsEnabled()
        {
            return App.settingsManager?.GetBool("tickrate_smoothing", false) == true;
        }
        
        /// <summary>
        /// Применяет сглаживание к значению tickrate
        /// </summary>
        /// <param name="rawTickrate">Исходное значение tickrate</param>
        /// <returns>Сглаженное значение tickrate</returns>
        public static int SmoothTickrate(int rawTickrate)
        {
            if (!IsEnabled())
                return rawTickrate;
            
            lock (_lock)
            {
                if (_tickrateEMA == null)
                    Initialize();
                
                double smoothedValue = _tickrateEMA.Update(rawTickrate);
                return (int)Math.Round(smoothedValue);
            }
        }
        
        /// <summary>
        /// Сбрасывает состояние сглаживания
        /// </summary>
        public static void Reset()
        {
            lock (_lock)
            {
                _tickrateEMA?.Reset();
            }
        }
    }
}