using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;

namespace tickMeter.Classes
{
    /// <summary>
    /// Stage 6: Real-time Network Quality Analysis
    /// Анализирует качество сети в реальном времени, рассчитывает показатели стабильности
    /// и предсказывает потенциальные проблемы
    /// </summary>
    public static class NetworkQualityAnalyzer
    {
        // Буферы для хранения исторических данных
        private static readonly Queue<float> _pingHistory = new Queue<float>();
        private static readonly Queue<float> _tickrateHistory = new Queue<float>();
        private static readonly Queue<float> _ticktimeHistory = new Queue<float>();
        private static readonly Queue<float> _jitterHistory = new Queue<float>();
        private static readonly Queue<float> _packetLossHistory = new Queue<float>();
        
        // Настройки анализа
        private static int _historySize = 100; // Размер буфера для анализа
        private static float _stabilityThreshold = 0.15f; // Порог стабильности (15%)
        private static float _qualityThreshold = 0.8f; // Порог качества сети (80%)
        
        // Блокировка для thread-safety
        private static readonly object _lockObject = new object();
        
        // Результаты анализа
        public static float PingStability { get; private set; } = 1.0f;
        public static float TickrateStability { get; private set; } = 1.0f;
        public static float TicktimeStability { get; private set; } = 1.0f;
        public static float OverallQuality { get; private set; } = 1.0f;
        public static float AverageJitter { get; private set; } = 0.0f;
        public static string QualityRating { get; private set; } = "Excellent";
        public static bool IsPredictingIssues { get; private set; } = false;
        public static string PredictionDetails { get; private set; } = "";
        
        // События для уведомлений
        public static event Action<float> QualityChanged;
        public static event Action<string> QualityRatingChanged;
        public static event Action<bool, string> PredictionChanged;
        
        /// <summary>
        /// Инициализация анализатора с настройками
        /// </summary>
        public static void Initialize()
        {
            try
            {
                // Загружаем настройки из settings manager
                var historyStr = App.settingsManager?.GetOption("quality_history_size", "100", "ADVANCED");
                if (int.TryParse(historyStr, out int history) && history > 10 && history <= 500)
                {
                    _historySize = history;
                }
                
                var stabilityStr = App.settingsManager?.GetOption("stability_threshold", "0.15", "ADVANCED");
                if (float.TryParse(stabilityStr, out float stability) && stability > 0 && stability < 1)
                {
                    _stabilityThreshold = stability;
                }
                
                var qualityStr = App.settingsManager?.GetOption("quality_threshold", "0.8", "ADVANCED");
                if (float.TryParse(qualityStr, out float quality) && quality > 0 && quality <= 1)
                {
                    _qualityThreshold = quality;
                }
                
                Debug.Print($"[NetworkQualityAnalyzer] Initialized: history={_historySize}, stability={_stabilityThreshold}, quality={_qualityThreshold}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkQualityAnalyzer] Initialization error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Добавляет новые данные для анализа
        /// </summary>
        public static void AddNetworkData(float ping, float tickrate, float ticktime, float packetLoss)
        {
            lock (_lockObject)
            {
                try
                {
                    // Добавляем данные в буферы
                    AddToBuffer(_pingHistory, ping);
                    AddToBuffer(_tickrateHistory, tickrate);
                    AddToBuffer(_ticktimeHistory, ticktime);
                    AddToBuffer(_packetLossHistory, packetLoss);
                    
                    // Рассчитываем jitter (изменчивость ping)
                    if (_pingHistory.Count >= 2)
                    {
                        var pingArray = _pingHistory.ToArray();
                        float currentJitter = Math.Abs(pingArray[pingArray.Length - 1] - pingArray[pingArray.Length - 2]);
                        AddToBuffer(_jitterHistory, currentJitter);
                    }
                    
                    // Выполняем анализ только если есть достаточно данных
                    if (_pingHistory.Count >= 10)
                    {
                        PerformQualityAnalysis();
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"[NetworkQualityAnalyzer] Error adding data: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Добавляет значение в буфер с ограничением размера
        /// </summary>
        private static void AddToBuffer(Queue<float> buffer, float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value) || value < 0) return;
            
            buffer.Enqueue(value);
            while (buffer.Count > _historySize)
            {
                buffer.Dequeue();
            }
        }
        
        /// <summary>
        /// Выполняет анализ качества сети
        /// </summary>
        private static void PerformQualityAnalysis()
        {
            try
            {
                // Рассчитываем стабильность для каждой метрики
                var oldPingStability = PingStability;
                var oldTickrateStability = TickrateStability;
                var oldTicktimeStability = TicktimeStability;
                var oldOverallQuality = OverallQuality;
                var oldQualityRating = QualityRating;
                var oldPredicting = IsPredictingIssues;
                var oldPredictionDetails = PredictionDetails;
                
                PingStability = CalculateStability(_pingHistory);
                TickrateStability = CalculateStability(_tickrateHistory);
                TicktimeStability = CalculateStability(_ticktimeHistory);
                
                // Рассчитываем средний jitter
                if (_jitterHistory.Count > 0)
                {
                    AverageJitter = _jitterHistory.Average();
                }
                
                // Рассчитываем общее качество сети
                OverallQuality = CalculateOverallQuality();
                
                // Определяем рейтинг качества
                QualityRating = GetQualityRating(OverallQuality);
                
                // Предсказываем проблемы
                PredictNetworkIssues();
                
                // Вызываем события если значения изменились
                if (Math.Abs(oldOverallQuality - OverallQuality) > 0.05f)
                {
                    QualityChanged?.Invoke(OverallQuality);
                }
                
                if (oldQualityRating != QualityRating)
                {
                    QualityRatingChanged?.Invoke(QualityRating);
                }
                
                if (oldPredicting != IsPredictingIssues || oldPredictionDetails != PredictionDetails)
                {
                    PredictionChanged?.Invoke(IsPredictingIssues, PredictionDetails);
                }
                
                Debug.Print($"[NetworkQualityAnalyzer] Quality: {OverallQuality:F2} ({QualityRating}), " +
                           $"Ping: {PingStability:F2}, Tickrate: {TickrateStability:F2}, " +
                           $"Ticktime: {TicktimeStability:F2}, Jitter: {AverageJitter:F1}ms");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkQualityAnalyzer] Analysis error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Рассчитывает стабильность метрики (коэффициент вариации)
        /// </summary>
        private static float CalculateStability(Queue<float> data)
        {
            if (data.Count < 3) return 1.0f;
            
            var values = data.Where(x => x > 0).ToArray();
            if (values.Length < 3) return 1.0f;
            
            float mean = values.Average();
            if (mean <= 0) return 1.0f;
            
            float variance = values.Select(x => (x - mean) * (x - mean)).Average();
            float stdDev = (float)Math.Sqrt(variance);
            float coefficientOfVariation = stdDev / mean;
            
            // Преобразуем в показатель стабильности (1 = стабильно, 0 = нестабильно)
            float stability = Math.Max(0, 1.0f - coefficientOfVariation / _stabilityThreshold);
            return Math.Min(1.0f, stability);
        }
        
        /// <summary>
        /// Рассчитывает общее качество сети
        /// </summary>
        private static float CalculateOverallQuality()
        {
            // Веса для разных метрик
            float pingWeight = 0.3f;
            float tickrateWeight = 0.3f;
            float ticktimeWeight = 0.2f;
            float jitterWeight = 0.1f;
            float packetLossWeight = 0.1f;
            
            float quality = 0;
            
            // Ping стабильность
            quality += PingStability * pingWeight;
            
            // Tickrate стабильность
            quality += TickrateStability * tickrateWeight;
            
            // Ticktime стабильность
            quality += TicktimeStability * ticktimeWeight;
            
            // Jitter penalty (чем больше jitter, тем хуже)
            float jitterPenalty = Math.Min(1.0f, AverageJitter / 50.0f); // Normalize to 50ms max
            quality += (1.0f - jitterPenalty) * jitterWeight;
            
            // Packet loss penalty
            if (_packetLossHistory.Count > 0)
            {
                float avgPacketLoss = _packetLossHistory.Average();
                float packetLossPenalty = Math.Min(1.0f, avgPacketLoss / 5.0f); // Normalize to 5% max
                quality += (1.0f - packetLossPenalty) * packetLossWeight;
            }
            else
            {
                quality += packetLossWeight; // Assume no packet loss if no data
            }
            
            return Math.Max(0, Math.Min(1.0f, quality));
        }
        
        /// <summary>
        /// Получает текстовый рейтинг качества
        /// </summary>
        private static string GetQualityRating(float quality)
        {
            if (quality >= 0.9f) return "Excellent";
            if (quality >= 0.8f) return "Good";
            if (quality >= 0.6f) return "Fair";
            if (quality >= 0.4f) return "Poor";
            return "Critical";
        }
        
        /// <summary>
        /// Предсказывает потенциальные проблемы сети
        /// </summary>
        private static void PredictNetworkIssues()
        {
            var issues = new List<string>();
            
            // Проверяем тренды в данных
            if (_pingHistory.Count >= 20)
            {
                var recentPing = _pingHistory.Skip(_pingHistory.Count - 10).ToArray();
                var olderPing = _pingHistory.Skip(_pingHistory.Count - 20).Take(10).ToArray();
                
                float recentAvg = recentPing.Average();
                float olderAvg = olderPing.Average();
                
                if (recentAvg > olderAvg * 1.2f)
                {
                    issues.Add("Ping deterioration detected");
                }
            }
            
            // Проверяем нестабильность
            if (PingStability < 0.7f)
            {
                issues.Add("High ping instability");
            }
            
            if (TickrateStability < 0.8f)
            {
                issues.Add("Tickrate fluctuations");
            }
            
            if (AverageJitter > 30.0f)
            {
                issues.Add("High network jitter");
            }
            
            // Проверяем общее качество
            if (OverallQuality < _qualityThreshold)
            {
                issues.Add("Overall network quality below threshold");
            }
            
            IsPredictingIssues = issues.Count > 0;
            PredictionDetails = issues.Count > 0 ? string.Join(", ", issues) : "";
        }
        
        /// <summary>
        /// Получает детальную статистику
        /// </summary>
        public static NetworkQualityStats GetDetailedStats()
        {
            lock (_lockObject)
            {
                return new NetworkQualityStats
                {
                    PingStability = PingStability,
                    TickrateStability = TickrateStability,
                    TicktimeStability = TicktimeStability,
                    OverallQuality = OverallQuality,
                    AverageJitter = AverageJitter,
                    QualityRating = QualityRating,
                    IsPredictingIssues = IsPredictingIssues,
                    PredictionDetails = PredictionDetails,
                    DataPoints = _pingHistory.Count,
                    AveragePing = _pingHistory.Count > 0 ? _pingHistory.Average() : 0,
                    AverageTickrate = _tickrateHistory.Count > 0 ? _tickrateHistory.Average() : 0,
                    AveragePacketLoss = _packetLossHistory.Count > 0 ? _packetLossHistory.Average() : 0
                };
            }
        }
        
        /// <summary>
        /// Очищает все буферы
        /// </summary>
        public static void Clear()
        {
            lock (_lockObject)
            {
                _pingHistory.Clear();
                _tickrateHistory.Clear();
                _ticktimeHistory.Clear();
                _jitterHistory.Clear();
                _packetLossHistory.Clear();
                
                PingStability = 1.0f;
                TickrateStability = 1.0f;
                TicktimeStability = 1.0f;
                OverallQuality = 1.0f;
                AverageJitter = 0.0f;
                QualityRating = "Excellent";
                IsPredictingIssues = false;
                PredictionDetails = "";
            }
        }
    }
    
    /// <summary>
    /// Структура для детальной статистики качества сети
    /// </summary>
    public class NetworkQualityStats
    {
        public float PingStability { get; set; }
        public float TickrateStability { get; set; }
        public float TicktimeStability { get; set; }
        public float OverallQuality { get; set; }
        public float AverageJitter { get; set; }
        public string QualityRating { get; set; }
        public bool IsPredictingIssues { get; set; }
        public string PredictionDetails { get; set; }
        public int DataPoints { get; set; }
        public float AveragePing { get; set; }
        public float AverageTickrate { get; set; }
        public float AveragePacketLoss { get; set; }
    }
}