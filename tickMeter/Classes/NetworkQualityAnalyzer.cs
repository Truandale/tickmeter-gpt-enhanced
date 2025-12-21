using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Globalization;

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
    private static float _stabilityThreshold = 0.15f; // Базовый порог стабильности (15%)
    private static float _pingStabilityThreshold = 0.15f;
    private static float _tickrateStabilityThreshold = 0.10f;
    private static float _ticktimeStabilityThreshold = 0.18f;
        private static float _qualityThreshold = 0.8f; // Порог качества сети (80%)
        
        // ChatGPT Optimization Settings
    private static float _manualTargetTickrate = 128f; // Пользовательский целевой тикрейт
    private static float _dynamicTargetTickrate = 128f; // Автоматически рассчитанный
    private static bool _targetTickrateAuto = true;
        private static float _pingGoodMs = 30f; // Хороший ping (мс)
        private static float _pingBadMs = 80f; // Плохой ping (мс)
        private static float _ticktimeGoodMs = 8f; // Хороший ticktime (мс)
        private static float _ticktimeBadMs = 16f; // Плохой ticktime (мс)
        private static float _emaAlpha = 0.15f; // Альфа для EMA сглаживания
        
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
        
        // NEW: Dual Quality System (Standard + Context)
        public static float StandardQuality { get; private set; } = 1.0f;  // Всегда Medium пороги
        public static string StandardRating { get; private set; } = "Excellent";
        public static float ContextQuality { get; private set; } = 1.0f;   // По профилю зон
        public static string ContextRating { get; private set; } = "Excellent";
        public static string ContextProfile { get; private set; } = "Medium";
        
        // EMA smoothing - раздельное для Standard и Context
        private static float _standardEma = -1f; // -1 означает неинициализировано
        private static float _contextEma = -1f;  // -1 означает неинициализировано

        // Отслеживание валидного пинга для борьбы с ложными скачками
        private static float _lastValidPing = -1f;
        private static int _missingPingSamples = 0;
        private const int MissingPingTolerance = 5;
        private const int MissingPingCritical = 30;
        
        // События для уведомлений
        public static event Action<float> QualityChanged;
        public static event Action<string> QualityRatingChanged;
        public static event Action<bool, string> PredictionChanged;
        
        // Кэш для отслеживания изменений профиля
        private static string _lastContextProfile = "";
        
        /// <summary>
        /// Загружает настройки Context профиля
        /// </summary>
        private static void LoadContextProfile()
        {
            try
            {
                string oldProfile = ContextProfile;
                
                // Проверяем синхронизацию с color zones
                bool contextSync = App.settingsManager?.GetOption("network_quality_context_sync", "True", "ADVANCED") == "True";
                
                if (contextSync)
                {
                    // Синхронизируем с профилем цветовых зон
                    ContextProfile = App.settingsManager?.GetOption("color_zone_profile", "Medium", "ZONES") ?? "Medium";
                }
                else
                {
                    // Используем отдельный профиль для Context
                    ContextProfile = App.settingsManager?.GetOption("network_quality_context_profile", "Medium", "ADVANCED") ?? "Medium";
                }
                
                ContextProfile = QualityDisplayThresholds.GetProfileDisplayName(ContextProfile);
                
                // Логируем изменение профиля (только один раз при изменении)
                if (oldProfile != ContextProfile && _lastContextProfile != ContextProfile)
                {
                    Debug.Print($"[NetworkQualityAnalyzer] Context Profile changed: {oldProfile} -> {ContextProfile}");
                    _lastContextProfile = ContextProfile;
                    
                    // CRITICAL FIX: Сбрасываем Context EMA при смене профиля
                    // Иначе старые значения будут смешиваться с новыми
                    _contextEma = -1f;
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkQualityAnalyzer] LoadContextProfile error: {ex.Message}");
                ContextProfile = "Medium";
            }
        }
        
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
                if (SettingsManager.TryParseInvariantFloat(stabilityStr?.Trim(), out float stability) && stability > 0 && stability < 1)
                {
                    _stabilityThreshold = stability;
                }

                var pingStabilityStr = App.settingsManager?.GetOption("stability_threshold_ping", stabilityStr ?? "0.15", "ADVANCED");
                if (!SettingsManager.TryParseInvariantFloat(pingStabilityStr?.Trim(), out _pingStabilityThreshold) || _pingStabilityThreshold <= 0)
                {
                    _pingStabilityThreshold = _stabilityThreshold;
                }

                var tickrateStabilityStr = App.settingsManager?.GetOption("stability_threshold_tickrate", "0.10", "ADVANCED");
                if (!SettingsManager.TryParseInvariantFloat(tickrateStabilityStr?.Trim(), out _tickrateStabilityThreshold) || _tickrateStabilityThreshold <= 0)
                {
                    _tickrateStabilityThreshold = Math.Max(0.05f, _stabilityThreshold * 0.75f);
                }

                var ticktimeStabilityStr = App.settingsManager?.GetOption("stability_threshold_ticktime", "0.18", "ADVANCED");
                if (!SettingsManager.TryParseInvariantFloat(ticktimeStabilityStr?.Trim(), out _ticktimeStabilityThreshold) || _ticktimeStabilityThreshold <= 0)
                {
                    _ticktimeStabilityThreshold = Math.Max(0.10f, _stabilityThreshold * 1.1f);
                }
                
                var qualityStr = App.settingsManager?.GetOption("quality_threshold", "0.8", "ADVANCED");
                if (SettingsManager.TryParseInvariantFloat(qualityStr?.Trim(), out float quality) && quality > 0 && quality <= 1)
                {
                    _qualityThreshold = quality;
                }
                
                // Загружаем ChatGPT optimization settings
                var targetMode = App.settingsManager?.GetOption("quality_target_tickrate_mode", "auto", "ADVANCED")?.Trim();
                var targetTickrateStr = App.settingsManager?.GetOption("quality_target_tickrate", "128", "ADVANCED");

                if (!string.IsNullOrWhiteSpace(targetTickrateStr) &&
                    SettingsManager.TryParseInvariantFloat(targetTickrateStr.Trim(), out float configuredTarget) && configuredTarget > 0)
                {
                    _manualTargetTickrate = configuredTarget;
                }

                _targetTickrateAuto = !string.Equals(targetMode, "manual", StringComparison.OrdinalIgnoreCase);
                
                var pingGoodStr = App.settingsManager?.GetOption("quality_ping_good_ms", "30", "ADVANCED");
                if (SettingsManager.TryParseInvariantFloat(pingGoodStr?.Trim(), out float pingGood) && pingGood > 0)
                {
                    _pingGoodMs = pingGood;
                }
                
                var pingBadStr = App.settingsManager?.GetOption("quality_ping_bad_ms", "80", "ADVANCED");
                if (SettingsManager.TryParseInvariantFloat(pingBadStr?.Trim(), out float pingBad) && pingBad > _pingGoodMs)
                {
                    _pingBadMs = pingBad;
                }
                
                var ticktimeGoodStr = App.settingsManager?.GetOption("quality_ticktime_good_ms", "8", "ADVANCED");
                if (SettingsManager.TryParseInvariantFloat(ticktimeGoodStr?.Trim(), out float ticktimeGood) && ticktimeGood > 0)
                {
                    _ticktimeGoodMs = ticktimeGood;
                }
                
                var ticktimeBadStr = App.settingsManager?.GetOption("quality_ticktime_bad_ms", "16", "ADVANCED");
                if (SettingsManager.TryParseInvariantFloat(ticktimeBadStr?.Trim(), out float ticktimeBad) && ticktimeBad > _ticktimeGoodMs)
                {
                    _ticktimeBadMs = ticktimeBad;
                }
                
                var emaAlphaStr = App.settingsManager?.GetOption("overall_quality_ema_alpha", "0.15", "ADVANCED");
                if (SettingsManager.TryParseInvariantFloat(emaAlphaStr?.Trim(), out float emaAlpha) && emaAlpha > 0 && emaAlpha <= 1)
                {
                    _emaAlpha = emaAlpha;
                }
                
                _dynamicTargetTickrate = _manualTargetTickrate;

                // NEW: Load Context Profile settings
                LoadContextProfile();

                Debug.Print($"[NetworkQualityAnalyzer] Initialized: history={_historySize}, stability(base)={_stabilityThreshold}, quality={_qualityThreshold}");
                Debug.Print($"[NetworkQualityAnalyzer] Stability thresholds => ping={_pingStabilityThreshold}, tickrate={_tickrateStabilityThreshold}, ticktime={_ticktimeStabilityThreshold}");
                Debug.Print($"[NetworkQualityAnalyzer] ChatGPT Settings: targetTickrateMode={( _targetTickrateAuto ? "auto" : _manualTargetTickrate.ToString("F0", CultureInfo.InvariantCulture))}, pingGood={_pingGoodMs}, pingBad={_pingBadMs}, ticktimeGood={_ticktimeGoodMs}, ticktimeBad={_ticktimeBadMs}, emaAlpha={_emaAlpha}");
                Debug.Print($"[NetworkQualityAnalyzer] Context Profile: {ContextProfile}");
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
                    if (TryAddPingSample(ping))
                    {
                        UpdateJitterFromPingHistory();
                    }

                    AddToBuffer(_tickrateHistory, tickrate);
                    AddToBuffer(_ticktimeHistory, ticktime);
                    AddToBuffer(_packetLossHistory, packetLoss);
                    
                    // Выполняем анализ только если есть достаточно данных
                    if (_pingHistory.Count >= 10 || _tickrateHistory.Count >= 10 || _ticktimeHistory.Count >= 10)
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

        private static bool TryAddPingSample(float ping)
        {
            if (ping > 0)
            {
                _lastValidPing = ping;
                _missingPingSamples = 0;
                AddToBuffer(_pingHistory, ping);
                return true;
            }

            _missingPingSamples = Math.Min(_missingPingSamples + 1, MissingPingCritical);
            return false;
        }

        private static void UpdateJitterFromPingHistory()
        {
            if (_pingHistory.Count < 2) return;
            var pingArray = _pingHistory.ToArray();
            float currentJitter = Math.Abs(pingArray[pingArray.Length - 1] - pingArray[pingArray.Length - 2]);
            AddToBuffer(_jitterHistory, currentJitter);
        }
        
        /// <summary>
        /// Выполняет анализ качества сети
        /// </summary>
        private static void PerformQualityAnalysis()
        {
            try
            {
                // FIX: Обновляем Context Profile перед каждым анализом для поддержки динамической смены профилей
                LoadContextProfile();
                
                // Рассчитываем стабильность для каждой метрики
                var oldPingStability = PingStability;
                var oldTickrateStability = TickrateStability;
                var oldTicktimeStability = TicktimeStability;
                var oldOverallQuality = OverallQuality;
                var oldQualityRating = QualityRating;
                var oldPredicting = IsPredictingIssues;
                var oldPredictionDetails = PredictionDetails;
                
                PingStability = CalculateStability(_pingHistory, _pingStabilityThreshold);
                TickrateStability = CalculateStability(_tickrateHistory, _tickrateStabilityThreshold);
                TicktimeStability = CalculateStability(_ticktimeHistory, _ticktimeStabilityThreshold);
                
                // Рассчитываем средний jitter
                if (_jitterHistory.Count > 0)
                {
                    AverageJitter = _jitterHistory.Average();
                }
                
                // NEW: Рассчитываем обе оценки качества
                // Standard Quality - всегда используется Medium профиль (объективная оценка)
                StandardQuality = CalculateQualityWithProfile("Medium");
                StandardRating = GetQualityRating(StandardQuality, "Medium");
                
                // Context Quality - используется профиль из настроек (контекстная оценка)
                ContextQuality = CalculateQualityWithProfile(ContextProfile);
                ContextRating = GetQualityRating(ContextQuality, ContextProfile);
                
                // OverallQuality - по умолчанию = StandardQuality для обратной совместимости
                OverallQuality = StandardQuality;
                QualityRating = StandardRating;
                
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
                
                Debug.Print($"[NetworkQualityAnalyzer] Standard: {StandardQuality:F2} ({StandardRating}) | " +
                           $"Context[{ContextProfile}]: {ContextQuality:F2} ({ContextRating}) | " +
                           $"Stability=> Ping:{PingStability:F2} TR:{TickrateStability:F2} TT:{TicktimeStability:F2} | " +
                           $"Jitter:{AverageJitter:F1}ms Target:{GetCurrentTargetTickrate():F1}Hz");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkQualityAnalyzer] Analysis error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Рассчитывает стабильность метрики (коэффициент вариации)
        /// </summary>
        private static float CalculateStability(Queue<float> data, float threshold)
        {
            if (data.Count < 3) return 1.0f;
            
            var values = data.Where(x => x > 0).ToArray();
            if (values.Length < 3) return 1.0f;
            
            float mean = values.Average();
            if (mean <= 0) return 1.0f;
            
            float variance = values.Select(x => (x - mean) * (x - mean)).Average();
            float stdDev = (float)Math.Sqrt(variance);
            if (threshold <= 0) threshold = 0.1f;
            float coefficientOfVariation = stdDev / mean;
            
            // Преобразуем в показатель стабильности (1 = стабильно, 0 = нестабильно)
            float stability = Math.Max(0, 1.0f - coefficientOfVariation / threshold);
            return Math.Min(1.0f, stability);
        }
        
        /// <summary>
        /// Рассчитывает общее качество сети с ChatGPT улучшениями
        /// </summary>
        /// <param name="profileName">Название профиля (Very Low/Low/Medium/High)</param>
        private static float CalculateQualityWithProfile(string profileName = "Medium")
        {
            // Получаем пороги для выбранного профиля
            var thresholds = QualityCalculationThresholds.GetThresholds(profileName);
            float pingGoodMs = thresholds.pingGood;
            float pingBadMs = thresholds.pingBad;
            float ticktimeGoodMs = thresholds.ticktimeGood;
            float ticktimeBadMs = thresholds.ticktimeBad;
            
            // Веса для разных метрик - TOTAL MUST = 1.0
            // Stability: 27% + 27% + 16% = 70%
            // Level penalties: 5% + 3% + 2% = 10%
            // Additional factors: 10% + 10% = 20%
            // TOTAL: 70% + 10% + 20% = 100% ✓
            float pingWeight = 0.27f;
            float tickrateWeight = 0.27f;
            float ticktimeWeight = 0.16f;
            float jitterWeight = 0.10f;
            float packetLossWeight = 0.10f;
            
            // Веса для level penalties (небольшие, чтобы не сломать базовую модель)
            float pingLevelWeight = 0.05f;
            float tickrateLevelWeight = 0.03f;
            float ticktimeLevelWeight = 0.02f;
            
            float quality = 0;
            
            // === СТАБИЛЬНОСТЬ (как раньше) ===
            quality += PingStability * pingWeight;
            quality += TickrateStability * tickrateWeight;
            quality += TicktimeStability * ticktimeWeight;
            
            // === LEVEL PENALTIES (ChatGPT recommendation) ===
            // Ping level penalty
            float avgPing = _pingHistory.Count > 0 ? _pingHistory.Average() : 0f;
            float pingLevelPenalty = 0f;
            float pingRange = pingBadMs - pingGoodMs;
            if (pingRange > 0 && avgPing > pingGoodMs)
            {
                pingLevelPenalty = Math.Min(1f, Math.Max(0f, (avgPing - pingGoodMs) / pingRange));
            }
            quality += (1f - pingLevelPenalty) * pingLevelWeight;
            
            // Tickrate level penalty
            float effectiveTargetTickrate = GetEffectiveTargetTickrate();
            float avgTickrate = _tickrateHistory.Count > 0 ? _tickrateHistory.Average() : 0f;
            float tickrateLevelPenalty = 0f;
            if (effectiveTargetTickrate > 0 && avgTickrate < effectiveTargetTickrate)
            {
                tickrateLevelPenalty = Math.Min(1f, Math.Max(0f, (effectiveTargetTickrate - avgTickrate) / effectiveTargetTickrate));
            }
            quality += (1f - tickrateLevelPenalty) * tickrateLevelWeight;
            
            // Ticktime level penalty
            float avgTicktime = _ticktimeHistory.Count > 0 ? _ticktimeHistory.Average() : 0f;
            float ticktimeLevelPenalty = 0f;
            float ticktimeRange = ticktimeBadMs - ticktimeGoodMs;
            if (ticktimeRange > 0 && avgTicktime > ticktimeGoodMs)
            {
                ticktimeLevelPenalty = Math.Min(1f, Math.Max(0f, (avgTicktime - ticktimeGoodMs) / ticktimeRange));
            }
            quality += (1f - ticktimeLevelPenalty) * ticktimeLevelWeight;
            
            // === JITTER И PACKET LOSS (как раньше) ===
            // Jitter penalty
            float jitterPenalty = Math.Min(1.0f, AverageJitter / 50.0f);
            quality += (1.0f - jitterPenalty) * jitterWeight;
            
            // Packet loss penalty
            if (_packetLossHistory.Count > 0)
            {
                float avgPacketLoss = _packetLossHistory.Average();
                float packetLossPenalty = Math.Min(1.0f, avgPacketLoss / 5.0f);
                quality += (1.0f - packetLossPenalty) * packetLossWeight;
            }
            else
            {
                quality += packetLossWeight;
            }

            if (_missingPingSamples >= MissingPingTolerance)
            {
                float availabilityPenalty = Math.Min(1f, (_missingPingSamples - MissingPingTolerance) / (float)Math.Max(1, MissingPingCritical - MissingPingTolerance));
                quality *= (1f - 0.25f * availabilityPenalty);
            }
            
            // Ограничиваем результат перед применением EMA
            quality = Math.Max(0f, Math.Min(1.0f, quality));
            
            // === EMA СГЛАЖИВАНИЕ (ChatGPT recommendation) ===
            // Используем раздельный EMA для Standard (Medium) и Context профилей
            bool isStandardProfile = profileName == "Medium";
            ref float emaRef = ref (isStandardProfile ? ref _standardEma : ref _contextEma);
            
            if (emaRef < 0)
            {
                emaRef = quality; // Первая инициализация
            }
            else 
            {
                emaRef = emaRef + _emaAlpha * (quality - emaRef);
            }
            
            return emaRef;
        }
        
        /// <summary>
        /// Получает текстовый рейтинг качества с учетом профиля
        /// </summary>
        private static string GetQualityRating(float quality, string profileName = "Medium")
        {
            // Используем адаптивные пороги для профиля
            var (excellentIn, _, goodIn, _, fairIn, _) = QualityDisplayThresholds.GetThresholds(profileName);
            
            if (quality >= excellentIn) return "Excellent";
            if (quality >= goodIn) return "Good";
            if (quality >= fairIn) return "Fair";
            if (quality >= 0.4f) return "Poor";
            return "Critical";
        }
        
        /// <summary>
        /// Предсказывает потенциальные проблемы сети
        /// </summary>
        private static void PredictNetworkIssues()
        {
                var issues = new List<string>();
                float effectiveTargetTickrate = GetCurrentTargetTickrate();
            
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
            
            // === ChatGPT: Дополнительные проверки ===
            // Проверяем рост packet loss
            if (_packetLossHistory.Count >= 20)
            {
                var recentLoss = _packetLossHistory.Skip(_packetLossHistory.Count - 10).Average();
                var olderLoss = _packetLossHistory.Skip(_packetLossHistory.Count - 20).Take(10).Average();
                
                if (recentLoss > olderLoss + 1.0f) // Увеличение на 1%
                {
                    issues.Add("Packet loss increasing");
                }
            }
            
            // Проверяем падение среднего tickrate
                if (effectiveTargetTickrate > 0 && _tickrateHistory.Count >= 10)
            {
                float avgTickrate = _tickrateHistory.Average();
                    if (avgTickrate < effectiveTargetTickrate * 0.9f) // Падение ниже 90% от цели
                {
                    issues.Add("Tickrate below target");
                }
            }
            
            // Проверяем общее качество
            if (OverallQuality < _qualityThreshold)
            {
                issues.Add("Overall network quality below threshold");
            }

                if (_missingPingSamples >= MissingPingTolerance)
                {
                    issues.Add("Ping data unavailable");
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
                    AveragePacketLoss = _packetLossHistory.Count > 0 ? _packetLossHistory.Average() : 0,
                    // NEW: Dual Quality System
                    StandardQuality = StandardQuality,
                    StandardRating = StandardRating,
                    ContextQuality = ContextQuality,
                    ContextRating = ContextRating,
                    ContextProfile = ContextProfile
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
                
                // Сбрасываем раздельные EMA
                _standardEma = -1f;
                _contextEma = -1f;
                _dynamicTargetTickrate = _manualTargetTickrate;
                _lastValidPing = -1f;
                _missingPingSamples = 0;
            }
        }

        private static float GetEffectiveTargetTickrate()
        {
            if (!_targetTickrateAuto)
            {
                return _manualTargetTickrate;
            }

            _dynamicTargetTickrate = CalculateDynamicTargetTickrate();
            return _dynamicTargetTickrate > 0 ? _dynamicTargetTickrate : _manualTargetTickrate;
        }

        private static float GetCurrentTargetTickrate()
        {
            return _targetTickrateAuto ? _dynamicTargetTickrate : _manualTargetTickrate;
        }

        private static float CalculateDynamicTargetTickrate()
        {
            var values = _tickrateHistory.Where(x => x > 0).ToArray();
            if (values.Length == 0)
            {
                return _manualTargetTickrate;
            }

            Array.Sort(values);
            int idx = (int)Math.Round(values.Length * 0.9) - 1;
            idx = Math.Max(0, Math.Min(idx, values.Length - 1));
            float percentile = values[idx];

            // Ограничиваем разумными пределами для игровых серверов
            percentile = Math.Max(30f, Math.Min(260f, percentile));

            // Плавно обновляем динамический таргет, чтобы избежать скачков
            if (_dynamicTargetTickrate <= 0)
            {
                return percentile;
            }

            float blendAlpha = 0.2f; // быстрая адаптация, но без резких прыжков
            return _dynamicTargetTickrate + blendAlpha * (percentile - _dynamicTargetTickrate);
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
        
        // NEW: Dual Quality System (Hybrid Mode)
        public float StandardQuality { get; set; }         // Always Medium profile (objective)
        public string StandardRating { get; set; }         // Rating for Standard quality
        public float ContextQuality { get; set; }          // User's selected profile (subjective)
        public string ContextRating { get; set; }          // Rating for Context quality
        public string ContextProfile { get; set; }         // Current context profile name
    }
}