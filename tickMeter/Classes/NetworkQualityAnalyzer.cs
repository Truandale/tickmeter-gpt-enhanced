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
    /// 
    /// NEW: Per-Endpoint Quality Tracking
    /// - Отслеживает качество для каждого IP:Port отдельно
    /// - Автоматически переключается на активный endpoint
    /// - TTL cleanup для неактивных endpoint'ов (120-300 секунд)
    /// </summary>
    public static class NetworkQualityAnalyzer
    {
        // Буферы для хранения исторических данных (ГЛОБАЛЬНЫЕ - для fallback)
        private static readonly Queue<float> _pingHistory = new Queue<float>();
        private static readonly Queue<float> _tickrateHistory = new Queue<float>();
        private static readonly Queue<float> _ticktimeHistory = new Queue<float>();
        private static readonly Queue<float> _jitterHistory = new Queue<float>();
        private static readonly Queue<float> _packetLossHistory = new Queue<float>();
        
        // NEW: Per-Endpoint Quality Tracking
        // Словарь: "IP:Port" -> EndpointQualityState
        private static readonly Dictionary<string, EndpointQualityState> _endpointStates = new Dictionary<string, EndpointQualityState>();
        private static string _activeEndpointKey = ""; // Текущий активный endpoint
        private static TimeSpan _endpointTtl = TimeSpan.FromSeconds(180); // TTL для неактивных endpoint'ов
        private static int _maxEndpoints = 64; // Максимальное количество отслеживаемых endpoint'ов
        private static DateTime _lastCleanup = DateTime.MinValue; // Время последней очистки
        private static TimeSpan _cleanupInterval = TimeSpan.FromSeconds(60); // Интервал очистки
        
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
                    DebugLogger.log($"[Quality] AddData ERROR: {ex.Message}");
                    Debug.Print($"[NetworkQualityAnalyzer] Error adding data: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// NEW: Добавляет данные для конкретного endpoint'а (IP:Port)
        /// </summary>
        /// <param name="endpointKey">Ключ endpoint'а в формате "IP:Port"</param>
        /// <param name="ping">Пинг (мс)</param>
        /// <param name="tickrate">Тикрейт (пакетов/сек)</param>
        /// <param name="ticktime">Тиктайм (мс)</param>
        /// <param name="packetLoss">Потери пакетов (%)</param>
        public static void AddNetworkData(string endpointKey, float ping, float tickrate, float ticktime, float packetLoss)
        {
            if (string.IsNullOrEmpty(endpointKey))
            {
                // Если endpoint не указан - используем глобальный fallback
                AddNetworkData(ping, tickrate, ticktime, packetLoss);
                return;
            }

            lock (_lockObject)
            {
                try
                {
                    // Получаем или создаём состояние endpoint'а
                    if (!_endpointStates.TryGetValue(endpointKey, out var state))
                    {
                        state = new EndpointQualityState(endpointKey, _historySize);
                        _endpointStates[endpointKey] = state;
                        Debug.Print($"[NetworkQualityAnalyzer] NEW endpoint tracked: {endpointKey}");
                    }

                    // Обновляем время последнего обращения
                    state.Touch();

                    // Добавляем данные в буферы endpoint'а
                    AddToBuffer(state.PingHistory, ping);
                    AddToBuffer(state.TickrateHistory, tickrate);
                    AddToBuffer(state.TicktimeHistory, ticktime);

                    // Обновляем метаданные
                    state.PacketCount++;

                    // ТАКЖЕ добавляем в глобальные буферы для fallback и общей статистики
                    if (TryAddPingSample(ping))
                    {
                        UpdateJitterFromPingHistory();
                    }
                    AddToBuffer(_tickrateHistory, tickrate);
                    AddToBuffer(_ticktimeHistory, ticktime);
                    AddToBuffer(_packetLossHistory, packetLoss);

                    // Периодическая очистка устаревших endpoint'ов
                    CleanupExpiredEndpoints();

                    // Выполняем анализ если достаточно данных
                    if (state.PingHistory.Count >= 10 || state.TickrateHistory.Count >= 10 || state.TicktimeHistory.Count >= 10)
                    {
                        PerformQualityAnalysis();
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[Quality] AddData(endpoint={endpointKey}) ERROR: {ex.Message}");
                    Debug.Print($"[NetworkQualityAnalyzer] Error adding data for endpoint {endpointKey}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Устанавливает активный endpoint для расчёта качества
        /// </summary>
        public static void SetActiveEndpoint(string endpointKey)
        {
            lock (_lockObject)
            {
                if (_activeEndpointKey != endpointKey && !string.IsNullOrEmpty(endpointKey))
                {
                    string oldEndpoint = _activeEndpointKey;
                    _activeEndpointKey = endpointKey;
                    Debug.Print($"[NetworkQualityAnalyzer] Active endpoint changed: {oldEndpoint} -> {endpointKey}");
                    
                    // При смене активного endpoint'а сбрасываем EMA для быстрой адаптации
                    _standardEma = -1f;
                    _contextEma = -1f;
                }
            }
        }
        
        /// <summary>
        /// Очищает устаревшие endpoint'ы по TTL
        /// </summary>
        private static void CleanupExpiredEndpoints()
        {
            var now = DateTime.UtcNow;
            if ((now - _lastCleanup) < _cleanupInterval)
                return;

            _lastCleanup = now;

            try
            {
                var expiredKeys = _endpointStates
                    .Where(kvp => kvp.Value.IsExpired(_endpointTtl))
                    .Select(kvp => kvp.Key)
                    .ToList();

                foreach (var key in expiredKeys)
                {
                    _endpointStates.Remove(key);
                    Debug.Print($"[NetworkQualityAnalyzer] Removed expired endpoint: {key}");
                }

                // Ограничиваем количество endpoint'ов (удаляем самые старые)
                if (_endpointStates.Count > _maxEndpoints)
                {
                    var toRemove = _endpointStates
                        .OrderBy(kvp => kvp.Value.LastUpdate)
                        .Take(_endpointStates.Count - _maxEndpoints)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    foreach (var key in toRemove)
                    {
                        _endpointStates.Remove(key);
                        Debug.Print($"[NetworkQualityAnalyzer] Removed oldest endpoint (limit reached): {key}");
                    }
                }

                if (expiredKeys.Count > 0 || _endpointStates.Count > _maxEndpoints)
                {
                    Debug.Print($"[NetworkQualityAnalyzer] Cleanup complete. Active endpoints: {_endpointStates.Count}");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkQualityAnalyzer] Cleanup error: {ex.Message}");
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
        /// NEW: Использует данные активного endpoint'а если установлен
        /// </summary>
        private static void PerformQualityAnalysis()
        {
            try
            {
                // FIX: Обновляем Context Profile перед каждым анализом для поддержки динамической смены профилей
                LoadContextProfile();
                
                // Определяем источник данных для анализа
                Queue<float> pingData, tickrateData, ticktimeData;
                bool usingEndpointData = false;
                string fallbackReason = "";
                int endpointPingCount = 0, endpointTickrateCount = 0;
                
                // NEW: Пытаемся использовать данные активного endpoint'а
                if (!string.IsNullOrEmpty(_activeEndpointKey) && _endpointStates.TryGetValue(_activeEndpointKey, out var activeState))
                {
                    endpointPingCount = activeState.PingHistory.Count;
                    endpointTickrateCount = activeState.TickrateHistory.Count;
                    
                    // Проверяем достаточно ли данных в endpoint'е
                    if (activeState.PingHistory.Count >= 10 || activeState.TickrateHistory.Count >= 10)
                    {
                        pingData = activeState.PingHistory;
                        tickrateData = activeState.TickrateHistory;
                        ticktimeData = activeState.TicktimeHistory;
                        usingEndpointData = true;
                    }
                    else
                    {
                        // Недостаточно данных - используем глобальные буферы
                        pingData = _pingHistory;
                        tickrateData = _tickrateHistory;
                        ticktimeData = _ticktimeHistory;
                        fallbackReason = $"NotEnoughData(Ping={endpointPingCount},TR={endpointTickrateCount})";
                    }
                }
                else
                {
                    // Нет активного endpoint'а - используем глобальные буферы (fallback)
                    pingData = _pingHistory;
                    tickrateData = _tickrateHistory;
                    ticktimeData = _ticktimeHistory;
                    fallbackReason = string.IsNullOrEmpty(_activeEndpointKey) ? "NoActiveEndpoint" : "EndpointNotFound";
                }
                
                // Рассчитываем стабильность для каждой метрики
                var oldPingStability = PingStability;
                var oldTickrateStability = TickrateStability;
                var oldTicktimeStability = TicktimeStability;
                var oldOverallQuality = OverallQuality;
                var oldQualityRating = QualityRating;
                var oldPredicting = IsPredictingIssues;
                var oldPredictionDetails = PredictionDetails;
                
                // Используем выбранный источник данных
                PingStability = CalculateStability(pingData, _pingStabilityThreshold);
                TickrateStability = CalculateStability(tickrateData, _tickrateStabilityThreshold);
                TicktimeStability = CalculateStability(ticktimeData, _ticktimeStabilityThreshold);
                
                // Рассчитываем средний jitter (всегда из глобального буфера)
                if (_jitterHistory.Count > 0)
                {
                    AverageJitter = _jitterHistory.Average();
                }
                
                // NEW: Рассчитываем обе оценки качества
                // Standard Quality - всегда используется Medium профиль (объективная оценка)
                StandardQuality = CalculateQualityWithProfile("Medium", pingData, tickrateData, ticktimeData);
                StandardRating = GetQualityRating(StandardQuality, "Medium");
                
                // Context Quality - используется профиль из настроек (контекстная оценка)
                ContextQuality = CalculateQualityWithProfile(ContextProfile, pingData, tickrateData, ticktimeData);
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
                
                // Логирование для анализа (расширенное для диагностики)
                string dataSource = usingEndpointData 
                    ? $"Endpoint[{_activeEndpointKey}]" 
                    : $"Global[Fallback:{fallbackReason}]";
                
                // Получаем пороги для диагностики
                var standardThresholds = QualityCalculationThresholds.GetThresholds("Medium");
                var contextThresholds = QualityCalculationThresholds.GetThresholds(ContextProfile);
                
                DebugLogger.log($"[Quality] {dataSource} Endpoints={_endpointStates.Count}");
                DebugLogger.log($"[Quality] Standard={StandardQuality:F3}({StandardRating}) [Medium: ping {standardThresholds.pingGood}-{standardThresholds.pingBad}ms, ticktime {standardThresholds.ticktimeGood}-{standardThresholds.ticktimeBad}ms]");
                DebugLogger.log($"[Quality] Context={ContextQuality:F3}({ContextRating}) [{ContextProfile}: ping {contextThresholds.pingGood}-{contextThresholds.pingBad}ms, ticktime {contextThresholds.ticktimeGood}-{contextThresholds.ticktimeBad}ms]");
                DebugLogger.log($"[Quality] Stability: Ping={PingStability:F2} TR={TickrateStability:F2} TT={TicktimeStability:F2} Jitter={AverageJitter:F1}ms");
                if (pingData.Count > 0 || tickrateData.Count > 0)
                {
                    float avgPing = pingData.Count > 0 ? pingData.Average() : 0;
                    float avgTickrate = tickrateData.Count > 0 ? tickrateData.Average() : 0;
                    float avgTicktime = ticktimeData.Count > 0 ? ticktimeData.Average() : 0;
                    DebugLogger.log($"[Quality] Metrics: Ping={avgPing:F1}ms TR={avgTickrate:F1}Hz TT={avgTicktime:F1}ms DataPoints={pingData.Count}");
                }
                if (usingEndpointData)
                {
                    DebugLogger.log($"[Quality] EndpointData: Ping={endpointPingCount} TR={endpointTickrateCount} samples");
                }
                
                Debug.Print($"[NetworkQualityAnalyzer] {dataSource} | Standard: {StandardQuality:F2} ({StandardRating}) | " +
                           $"Context[{ContextProfile}]: {ContextQuality:F2} ({ContextRating}) | " +
                           $"Stability=> Ping:{PingStability:F2} TR:{TickrateStability:F2} TT:{TicktimeStability:F2} | " +
                           $"Jitter:{AverageJitter:F1}ms Target:{GetCurrentTargetTickrate():F1}Hz | " +
                           $"Endpoints: {_endpointStates.Count}");
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[Quality] ERROR: {ex.Message}");
                Debug.Print($"[NetworkQualityAnalyzer] Analysis error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Вычисляет медиану из массива значений
        /// </summary>
        private static float GetMedian(Queue<float> data)
        {
            var sorted = data.Where(x => x > 0).OrderBy(x => x).ToArray();
            if (sorted.Length == 0) return 0f;
            if (sorted.Length == 1) return sorted[0];
            
            int mid = sorted.Length / 2;
            if (sorted.Length % 2 == 0)
                return (sorted[mid - 1] + sorted[mid]) / 2f;
            return sorted[mid];
        }
        
        /// <summary>
        /// Вычисляет Median Absolute Deviation (MAD)
        /// </summary>
        private static float GetMAD(float[] values, float median)
        {
            if (values.Length == 0) return 0f;
            var deviations = values.Select(v => Math.Abs(v - median)).OrderBy(d => d).ToArray();
            int mid = deviations.Length / 2;
            if (deviations.Length % 2 == 0)
                return (deviations[mid - 1] + deviations[mid]) / 2f;
            return deviations[mid];
        }
        
        /// <summary>
        /// Вычисляет устойчивое среднее значение с фильтрацией выбросов
        /// </summary>
        private static float GetRobustAverage(Queue<float> data, float outlierThreshold = 3.0f)
        {
            var values = data.Where(x => x > 0).ToArray();
            if (values.Length == 0) return 0f;
            if (values.Length <= 3) return values.Average();
            
            float median = GetMedian(data);
            float mad = GetMAD(values, median);
            
            // Если MAD слишком маленький, используем обычное среднее
            if (mad < 0.01f) return values.Average();
            
            // Фильтруем выбросы (>3 MAD от медианы)
            var filtered = values.Where(v => Math.Abs(v - median) <= outlierThreshold * mad).ToArray();
            
            return filtered.Length > 0 ? filtered.Average() : median;
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
        /// Рассчитывает общее качество сети с улучшениями:
        /// - Adaptive EMA (по профилям)
        /// - Recovery Detection (бонус за улучшение)
        /// - Stability Bonus (компенсация за хорошие средние значения)
        /// - Robust averaging (устойчивость к выбросам)
        /// - Умный Missing Ping Handler
        /// </summary>
        /// <param name="profileName">Название профиля (Very Low/Low/Medium/High)</param>
        /// <param name="pingData">Данные пинга (null = использовать глобальные)</param>
        /// <param name="tickrateData">Данные тикрейта (null = использовать глобальные)</param>
        /// <param name="ticktimeData">Данные тиктайма (null = использовать глобальные)</param>
        private static float CalculateQualityWithProfile(string profileName = "Medium", 
            Queue<float> pingData = null, 
            Queue<float> tickrateData = null, 
            Queue<float> ticktimeData = null)
        {
            // Используем переданные данные или fallback на глобальные
            pingData = pingData ?? _pingHistory;
            tickrateData = tickrateData ?? _tickrateHistory;
            ticktimeData = ticktimeData ?? _ticktimeHistory;
            // Получаем пороги для выбранного профиля
            var thresholds = QualityCalculationThresholds.GetThresholds(profileName);
            float pingGoodMs = thresholds.pingGood;
            float pingBadMs = thresholds.pingBad;
            float ticktimeGoodMs = thresholds.ticktimeGood;
            float ticktimeBadMs = thresholds.ticktimeBad;
            
            // Адаптивные параметры по профилю
            float stabilityFactor, levelFactor;
            float stabilityToleranceFactor; // Множитель для порога стабильности
            float recoveryBonusMax;          // Максимальный бонус за восстановление
            float stabilityBonusMax;         // Максимальный бонус за хорошие средние значения
            float missingPingSoftening;      // Смягчение штрафа за missing ping
            float availabilityPenaltyWeight; // Вес штрафа за отсутствие пинга
            bool useMedianForPing;           // Использовать median вместо mean для пинга
            
            switch (profileName?.ToLower().Replace(" ", ""))
            {
                case "verylow":
                case "very_low":
                    stabilityFactor = 0.50f;          // 50% stability (мягче к скачкам)
                    levelFactor = 0.40f;              // 40% level (важнее средние значения)
                    stabilityToleranceFactor = 1.8f;  // На 80% мягче к скачкам
                    recoveryBonusMax = 0.12f;         // +12% за восстановление
                    stabilityBonusMax = 0.20f;        // +20% за хорошие средние
                    missingPingSoftening = 0.85f;     // Почти не карать за missing ping
                    availabilityPenaltyWeight = 0.08f;// Минимальный штраф
                    useMedianForPing = true;          // Median устойчивее к выбросам
                    break;
                case "low":
                    stabilityFactor = 0.60f;
                    levelFactor = 0.30f;
                    stabilityToleranceFactor = 1.4f;  // На 40% мягче
                    recoveryBonusMax = 0.10f;         // +10%
                    stabilityBonusMax = 0.16f;        // +16%
                    missingPingSoftening = 0.65f;
                    availabilityPenaltyWeight = 0.15f;
                    useMedianForPing = true;          // Взвешенный median-mean
                    break;
                case "high":
                    stabilityFactor = 0.75f;          // 75% stability (строго к скачкам)
                    levelFactor = 0.15f;              // 15% level
                    stabilityToleranceFactor = 0.75f; // На 25% строже
                    recoveryBonusMax = 0.05f;         // +5% (маленький)
                    stabilityBonusMax = 0.10f;        // +10% (строго)
                    missingPingSoftening = 0.30f;     // Строго карать
                    availabilityPenaltyWeight = 0.28f;// Высокий штраф
                    useMedianForPing = false;         // Mean (чувствителен к выбросам)
                    break;
                default: // medium
                    stabilityFactor = 0.65f;
                    levelFactor = 0.25f;
                    stabilityToleranceFactor = 1.0f;  // Базовая
                    recoveryBonusMax = 0.08f;         // +8%
                    stabilityBonusMax = 0.12f;        // +12%
                    missingPingSoftening = 0.50f;
                    availabilityPenaltyWeight = 0.18f;
                    useMedianForPing = false;         // Обычное среднее
                    break;
            }
            
            // FIXED: Убрали двойной счёт - оставили только уникальные метрики
            // Stability weights (распределение внутри stabilityFactor)
            float pingStabilityWeight = stabilityFactor * 0.50f;     // 50% от stability
            float tickrateStabilityWeight = stabilityFactor * 0.50f; // 50% от stability
            // REMOVED: ticktimeWeight (это производная от tickrate - двойной счёт)
            // REMOVED: jitterWeight (уже входит в PingStability через CV)
            
            // Level penalties (распределение внутри levelFactor)
            float pingLevelWeight = levelFactor * 0.55f;      // 55% от level
            float tickrateLevelWeight = levelFactor * 0.45f;  // 45% от level
            
            // Packet loss (оставшиеся 10%)
            float packetLossWeight = 0.10f;
            
            float quality = 0;
            
            // === СТАБИЛЬНОСТЬ (FIXED: убран двойной счёт) ===
            quality += PingStability * pingStabilityWeight;
            quality += TickrateStability * tickrateStabilityWeight;
            // REMOVED: TicktimeStability (производная от tickrate)
            
            // === LEVEL PENALTIES ===
            // Ping level penalty с учётом профиля (median или robust average)
            float avgPing;
            if (useMedianForPing && pingData.Count >= 10)
            {
                // Very Low/Low профили: используем median или robust average
                if (profileName?.ToLower().Replace(" ", "") == "verylow" || 
                    profileName?.ToLower().Replace(" ", "") == "very_low")
                {
                    avgPing = GetMedian(pingData); // Чистый median
                }
                else // Low
                {
                    // Взвешенный median-mean (70% median, 30% mean)
                    avgPing = GetMedian(pingData) * 0.7f + pingData.Average() * 0.3f;
                }
            }
            else
            {
                // Medium/High: обычное среднее (High чувствителен к выбросам)
                avgPing = pingData.Count > 0 ? pingData.Average() : 0f;
            }
            
            float pingLevelPenalty = 0f;
            float pingRange = pingBadMs - pingGoodMs;
            if (pingRange > 0 && avgPing > pingGoodMs)
            {
                pingLevelPenalty = Math.Min(1f, Math.Max(0f, (avgPing - pingGoodMs) / pingRange));
            }
            quality += (1f - pingLevelPenalty) * pingLevelWeight;
            
            // Tickrate level penalty
            float effectiveTargetTickrate = GetEffectiveTargetTickrate();
            float avgTickrate = tickrateData.Count > 0 ? tickrateData.Average() : 0f;
            float tickrateLevelPenalty = 0f;
            if (effectiveTargetTickrate > 0 && avgTickrate < effectiveTargetTickrate)
            {
                tickrateLevelPenalty = Math.Min(1f, Math.Max(0f, (effectiveTargetTickrate - avgTickrate) / effectiveTargetTickrate));
            }
            quality += (1f - tickrateLevelPenalty) * tickrateLevelWeight;
            
            // REMOVED: Ticktime level penalty (двойной счёт с tickrate)
            // REMOVED: Jitter penalty (уже учтён в PingStability через CV)
            
            // === RECOVERY DETECTION (бонус за улучшение) ===
            float recoveryBonus = 0f;
            if (pingData.Count >= 20)
            {
                var recentPing = pingData.Skip(pingData.Count - 10).Average();
                var olderPing = pingData.Skip(pingData.Count - 20).Take(10).Average();
                
                // Порог улучшения зависит от профиля
                float improvementThreshold;
                string normalizedProfile = profileName?.ToLower().Replace(" ", "");
                if (normalizedProfile == "verylow" || normalizedProfile == "very_low")
                {
                    improvementThreshold = 0.90f;  // Улучшение на 10%+
                }
                else if (normalizedProfile == "low")
                {
                    improvementThreshold = 0.85f;  // Улучшение на 15%+
                }
                else if (normalizedProfile == "high")
                {
                    improvementThreshold = 0.80f;  // Нужно значительное улучшение (20%+)
                }
                else
                {
                    improvementThreshold = 0.85f;  // Medium: 15%+
                }
                
                if (recentPing < olderPing * improvementThreshold)
                {
                    // Линейный бонус в зависимости от степени улучшения
                    float improvementRatio = 1f - (recentPing / olderPing);
                    recoveryBonus = Math.Min(recoveryBonusMax, improvementRatio * recoveryBonusMax * 2f);
                }
            }
            
            // === STABILITY BONUS (компенсация за хорошие средние значения) ===
            float stabilityBonus = 0f;
            if (avgPing > 0 && avgPing < pingGoodMs)
            {
                // Качество пинга относительно порога "хорошего"
                float pingQuality = 1f - (avgPing / pingGoodMs);
                stabilityBonus = pingQuality * stabilityBonusMax;
            }
            
            // Дополнительный бонус если и стабильность хорошая (только для High профиля)
            if (profileName?.ToLower().Replace(" ", "") == "high" && 
                avgPing > 0 && avgPing < pingGoodMs * 0.8f && PingStability > 0.90f)
            {
                stabilityBonus *= 1.2f; // +20% к бонусу за идеальные условия
            }
            
            // === PACKET LOSS ===
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

            // === УМНЫЙ MISSING PING HANDLER ===
            if (_missingPingSamples >= MissingPingTolerance && _lastValidPing > 0)
            {
                // Если последний известный пинг был хорошим → смягчаем штраф
                float lastPingQuality = _lastValidPing < pingGoodMs ? 0.8f : 0.3f;
                
                float availabilityPenalty = Math.Min(1f, 
                    (_missingPingSamples - MissingPingTolerance) / 
                    (float)Math.Max(1, MissingPingCritical - MissingPingTolerance));
                
                // Применяем смягчение в зависимости от профиля и качества последнего пинга
                availabilityPenalty *= (1f - lastPingQuality * missingPingSoftening);
                
                quality *= (1f - availabilityPenaltyWeight * availabilityPenalty);
            }
            else if (_missingPingSamples >= MissingPingTolerance)
            {
                // Нет информации о последнем валидном пинге → обычный штраф
                float availabilityPenalty = Math.Min(1f, 
                    (_missingPingSamples - MissingPingTolerance) / 
                    (float)Math.Max(1, MissingPingCritical - MissingPingTolerance));
                quality *= (1f - availabilityPenaltyWeight * availabilityPenalty);
            }
            
            // Применяем бонусы
            quality += recoveryBonus + stabilityBonus;
            
            // Ограничиваем результат перед применением EMA
            quality = Math.Max(0f, Math.Min(1.0f, quality));
            
            // === ADAPTIVE EMA СГЛАЖИВАНИЕ (адаптивная скорость восстановления/ухудшения) ===
            // Используем раздельный EMA для Standard (Medium) и Context профилей
            bool isStandardProfile = profileName == "Medium";
            ref float emaRef = ref (isStandardProfile ? ref _standardEma : ref _contextEma);
            
            if (emaRef < 0)
            {
                emaRef = quality; // Первая инициализация
            }
            else 
            {
                // Определяем тренд (качество растёт или падает)
                float trend = quality - emaRef;
                
                // Адаптивный alpha на основе тренда и профиля
                float adaptiveAlpha;
                string normalizedProfile = profileName?.ToLower().Replace(" ", "");
                
                if (trend > 0)
                {
                    // Качество растёт → ускоряем восстановление
                    if (normalizedProfile == "verylow" || normalizedProfile == "very_low")
                    {
                        adaptiveAlpha = _emaAlpha * 3.5f;  // Очень быстрое
                    }
                    else if (normalizedProfile == "low")
                    {
                        adaptiveAlpha = _emaAlpha * 2.8f;  // Быстрое
                    }
                    else if (normalizedProfile == "high")
                    {
                        adaptiveAlpha = _emaAlpha * 2.0f;  // Умеренное
                    }
                    else
                    {
                        adaptiveAlpha = _emaAlpha * 2.5f;  // Medium: стандартное
                    }
                }
                else
                {
                    // Качество падает → медленное ухудшение (сохраняем стабильность)
                    if (normalizedProfile == "verylow" || normalizedProfile == "very_low")
                    {
                        adaptiveAlpha = _emaAlpha * 0.6f;  // Очень медленное
                    }
                    else if (normalizedProfile == "low")
                    {
                        adaptiveAlpha = _emaAlpha * 0.75f; // Медленное
                    }
                    else if (normalizedProfile == "high")
                    {
                        adaptiveAlpha = _emaAlpha * 1.2f;  // Быстрое (строгость)
                    }
                    else
                    {
                        adaptiveAlpha = _emaAlpha * 0.8f;  // Medium
                    }
                }
                
                // Ограничиваем alpha разумными пределами
                adaptiveAlpha = Math.Min(0.5f, Math.Max(0.05f, adaptiveAlpha));
                
                emaRef = emaRef + adaptiveAlpha * (quality - emaRef);
            }
            
            return emaRef;
        }
        
        /// <summary>
        /// Получает текстовый рейтинг качества с адаптивными порогами Poor/Critical по профилю
        /// </summary>
        private static string GetQualityRating(float quality, string profileName = "Medium")
        {
            // Используем адаптивные пороги для профиля
            var (excellentIn, _, goodIn, _, fairIn, _) = QualityDisplayThresholds.GetThresholds(profileName);
            
            // Адаптивные пороги Poor/Critical в зависимости от профиля
            float poorThreshold, criticalThreshold;
            switch (profileName?.ToLower().Replace(" ", ""))
            {
                case "verylow":
                case "very_low":
                    poorThreshold = 0.25f;    // Мягче: Poor только при <25%
                    criticalThreshold = 0.10f;
                    break;
                case "low":
                    poorThreshold = 0.28f;
                    criticalThreshold = 0.12f;
                    break;
                case "high":
                    poorThreshold = 0.45f;    // Строже: Poor уже при <45%
                    criticalThreshold = 0.25f;
                    break;
                default: // medium
                    poorThreshold = 0.35f;    // Снижено с 0.40 до 0.35
                    criticalThreshold = 0.15f;
                    break;
            }
            
            if (quality >= excellentIn) return "Excellent";
            if (quality >= goodIn) return "Good";
            if (quality >= fairIn) return "Fair";
            if (quality >= poorThreshold) return "Poor";
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
                
                // Очищаем словарь endpoint'ов
                _endpointStates.Clear();
                _activeEndpointKey = "";
                
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