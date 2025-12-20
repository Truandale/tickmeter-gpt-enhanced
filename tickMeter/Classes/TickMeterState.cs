using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using tickMeter.Classes; // Предполагается, что App и SettingsManager находятся здесь
using System.Windows.Forms; // <--- ДОБАВЛЕНО ДЛЯ Application.ProductVersion

namespace tickMeter
{
    public class TickMeterState
    {
        private int LastTicksCount = 0;
        private int _tickrate;
        private System.Timers.Timer MeterValidateTimer;
        private DateTime timeStamp = DateTime.MinValue; // Инициализация

        public bool IsTracking { get; set; } = false;
        public bool ConnectionsManagerFlag = false; // Используется ли ConnectionsManager

        public GameServer Server { get; set; }

        public string LocalIP { get; set; }
        private string _game;
        public string Game
        {
            get { return _game; }
            set { _game = value; }
        }

        public bool isBuiltInProfileActive = false;
        public bool isCustomProfileActive = false;

        public List<float> tickTimeBuffer = new List<float>();
        public List<float> tickrateBuffer = new List<float>(); // Новый буфер для графика тикрейта
        public List<float> pingBuffer = new List<float>();
        
        // Thread-safe access to buffers
        public readonly object _pingBufferLock = new object();
        public readonly object _tickTimeBufferLock = new object();
        public readonly object _tickrateBufferLock = new object();

        public int TickRate
        {
            get { return _tickrate; }
            set
            {
                _tickrate = value;
                SetMeterTimer(); // Таймер валидации активности
            }
        }

        public static DateTime Trim(DateTime date)
        {
            return new DateTime(date.Ticks - (date.Ticks % TimeSpan.TicksPerSecond), date.Kind);
        }

        public static float ComputeTickTimeMs(long previousTicks, long currentTicks)
        {
            if (previousTicks <= 0 || currentTicks <= 0) return 0f;
            long deltaTicks = currentTicks - previousTicks;
            if (deltaTicks <= 0) return 0f;
            float tickTime = deltaTicks / (float)TimeSpan.TicksPerMillisecond;
            if (tickTime > 100f) tickTime = 100f;
            return tickTime;
        }

        public void updateTicktimeBuffer(long packetTicks)
        {
            // Используем индивидуальные данные сервера
            if (Server != null)
            {
                Server.TotalTicksCount++;
            }
            
            lock (_tickTimeBufferLock)
            {
                if (tickTimeBuffer.Count > 511)
                {
                    tickTimeBuffer.RemoveAt(0);
                }
            }
            
            if (timeStamp != DateTime.MinValue)
            {
                float tickTime = ComputeTickTimeMs(timeStamp.Ticks, packetTicks);
                if (tickTime > 0f)
                {
                    lock (_tickTimeBufferLock)
                    {
                        tickTimeBuffer.Add(tickTime);
                    }
                    
                    // Детекция спайков ticktime
                    if (Server != null)
                    {
                        Server.CheckForTicktimeSpike(tickTime);
                    }
                }
            }
        }

        public void updateTickrateBuffer(int currentTickrate)
        {
            // Обновление буфера тикрейта для графика
            lock (_tickrateBufferLock)
            {
                if (tickrateBuffer.Count > 511)
                {
                    tickrateBuffer.RemoveAt(0);
                }
                tickrateBuffer.Add(currentTickrate);
            }
            DebugLogger.log($"[TickrateBuffer] Added {currentTickrate} Hz to buffer (size: {tickrateBuffer.Count})");
        }

        public void SetMeterTimer()
        {
            if (MeterValidateTimer == null || !MeterValidateTimer.Enabled)
            {
                MeterValidateTimer = new System.Timers.Timer();
                MeterValidateTimer.Elapsed += MeterValidateTimerTick;
                MeterValidateTimer.Interval = 2000; // Проверка каждые 2 секунды
                MeterValidateTimer.AutoReset = true;
                MeterValidateTimer.Enabled = true;
            }
        }

        public TickMeterState()
        {
            // Инициализация буферов
            for (int i = 0; i < 513; i++)
            {
                lock (_tickTimeBufferLock)
                {
                    tickTimeBuffer.Add(0);
                }
                lock (_tickrateBufferLock)
                {
                    tickrateBuffer.Add(0);
                }
                lock (_pingBufferLock)
                {
                    pingBuffer.Add(30);
                }
            }
            Server = new GameServer(); // Инициализация сервера до Reset
            Reset(); // Сброс всех счетчиков и состояния
            SetMeterTimer();
            _game = ""; // инициализация поля в конструкторе
        }

        private void MeterValidateTimerTick(Object source, System.Timers.ElapsedEventArgs e)
        {
            // Если отслеживание активно, игра определена, но новых "тиков" не было
            int currentTicksCount = Server?.TicksHistory?.Count ?? 0;
            if (IsTracking && Game != "" && LastTicksCount == currentTicksCount)
            {
                KillTimers(); // Остановить таймеры пинга и валидации
            }
            else if (IsTracking && Game != "" && Server != null) // Если есть активность
            {
                Server.SetPingTimer(); // Убедиться, что таймер пинга активен
            }
            LastTicksCount = currentTicksCount;
        }

        public DateTime CurrentTimestamp
        {
            get { return timeStamp; }
            set
            {
                if (!IsTracking) return;
                if (value.ToString() != timeStamp.ToString())
                {
                    int rawTickRate = TickRate;
                    // Применяем сглаживание, если включено
                    int smoothedTickrate = TickrateSmoothingManager.SmoothTickrate(rawTickRate);
                    
                    // Update individual server tickrate data
                    if (Server != null)
                    {
                        Server.UpdateTickrate(smoothedTickrate, value);
                    }
                    
                    // Обновляем новый буфер тикрейта для графика
                    updateTickrateBuffer(smoothedTickrate);
                    
                    // Reset tick counter for next measurement
                    TickRate = 0;

                    // --- Единый буфер для графика пинга: UDP > TCP > ICMP ---
                    int pingValue = 0;
                    if (Server != null && Server.IsUdpPingValid)
                        pingValue = (int)Math.Round(Server.UdpPing);
                    else if (Server != null && Server.Ping > 0 && Server.Ping < 10000)
                        pingValue = Server.Ping;
                    else if (Server != null && Server.IcmpPing > 0 && Server.IcmpPing < 10000)
                        pingValue = Server.IcmpPing;
                    else
                        pingValue = 0;

                    lock (_pingBufferLock)
                    {
                        pingBuffer.Add(pingValue);
                        if (pingBuffer.Count > 512)
                            pingBuffer.RemoveAt(0);
                    }
                }
                timeStamp = value;
            }
        }

        // Forward properties from current server
        public int OutputTickRate 
        { 
            get { return Server != null ? Server.OutputTickRate : 0; }
            set { if (Server != null) Server.OutputTickRate = value; }
        }
        
        public List<int> TicksHistory 
        { 
            get { return Server != null ? Server.TicksHistory : new List<int>(); }
        }
        
        public List<DateTime> TickTimestamps 
        { 
            get { return Server != null ? Server.TickTimestamps : new List<DateTime>(); }
        }
        
        public int AvgTickrate
        {
            get { return Server != null ? Server.AvgTickrate : 0; }
        }
        
        // Forward traffic data from current server
        public int UploadTraffic 
        { 
            get { return Server != null ? Server.UploadTraffic : 0; }
            set { if (Server != null) Server.UploadTraffic = value; }
        }
        
        public int DownloadTraffic 
        { 
            get { return Server != null ? Server.DownloadTraffic : 0; }
            set { if (Server != null) Server.DownloadTraffic = value; }
        }
        
        // Forward session data from current server
        public DateTime SessionStart 
        { 
            get { return Server != null ? Server.SessionStart : DateTime.Now; }
            set { if (Server != null) Server.SessionStart = value; }
        }
        
        // Forward loss data from current server
        public int totalTicksCnt 
        { 
            get { return Server != null ? Server.TotalTicksCount : 0; }
            set { if (Server != null) Server.TotalTicksCount = value; }
        }
        
        public int loss 
        { 
            get { return Server != null ? Server.LostTicks : 0; }
            set { if (Server != null) Server.LostTicks = value; }
        }
        
        public int avgStableTickrate 
        { 
            get { return Server != null ? Server.AvgStableTickrate : 0; }
            set { if (Server != null) Server.AvgStableTickrate = value; }
        }
        
        // Forward log and graph data from current server
        public string TickRateLog 
        { 
            get { return Server != null ? Server.TickRateLog : ""; }
            set { if (Server != null) Server.TickRateLog = value; }
        }
        
        public List<float> tickrateGraph 
        { 
            get { return Server != null ? Server.TickrateGraph : new List<float>(); }
        }

        // Forward TCP ping (current ping value) from GameServer
        public int TcpPing
        {
            get
            {
                return Server != null ? Server.Ping : 0;
            }
        }

        // Forward UDP ping validity from GameServer
        public bool IsUdpPingValid
        {
            get
            {
                return Server != null && Server.IsUdpPingValid;
            }
        }

        // Forward spike detection flags from GameServer
        public bool HasTickRateSpike
        {
            get
            {
                // FIX: Проверяем настройку show_tickrate_spikes
                bool showSpikeIndicator = App.settingsManager?.GetOption("show_tickrate_spikes", "True", "ADVANCED") == "True";
                if (!showSpikeIndicator)
                {
                    return false; // Если показ спайков выключен - возвращаем false
                }
                return Server != null && Server.HasActiveTickRateSpike;
            }
        }

        public bool HasTickTimeSpike
        {
            get
            {
                // FIX: Проверяем настройку show_ticktime_spikes
                bool showSpikeIndicator = App.settingsManager?.GetOption("show_ticktime_spikes", "True", "ADVANCED") == "True";
                if (!showSpikeIndicator)
                {
                    return false; // Если показ спайков выключен - возвращаем false
                }
                return Server != null && Server.HasActiveTickTimeSpike;
            }
        }

        // Forward UDP ping string from GameServer
        public string GetUdpPingString()
        {
            return Server != null ? Server.GetUdpPingString() : "n/a";
        }

        // Forward ICMP ping (current ping value) from GameServer
        public int IcmpPing
        {
            get
            {
                return Server != null ? Server.IcmpPing : 0;
            }
        }

        public void Reset()
        {
            IsTracking = false;
            Game = "";
            
            // Reset server data (individual data will be reset in Server.Reset())
            if (Server != null)
            {
                Server.Reset();
            }

            lock (_tickTimeBufferLock)
            {
                tickTimeBuffer.Clear();
            }
            lock (_tickrateBufferLock)
            {
                tickrateBuffer.Clear();
            }
            lock (_pingBufferLock)
            {
                pingBuffer.Clear();
            }
            
            for (int i = 0; i < 513; i++)
            {
                lock (_tickTimeBufferLock)
                {
                    tickTimeBuffer.Add(0);
                }
                lock (_tickrateBufferLock)
                {
                    tickrateBuffer.Add(0);
                }
                lock (_pingBufferLock)
                {
                    pingBuffer.Add(30);
                }
            }

            TickRate = 0;
            timeStamp = DateTime.MinValue; // Сброс для корректной работы CurrentTimestamp
        }

        public void KillTimers()
        {
            if (MeterValidateTimer != null)
            {
                MeterValidateTimer.Enabled = false;
                MeterValidateTimer.Stop();
                MeterValidateTimer.Dispose();
                MeterValidateTimer = null;
                Debug.Print("killed meter timer");
            }
            Server?.KillTimer();
        }

        internal string GetDrops()
        {
            if (Server == null)
                return "0.00";
                
            return Server.GetDropsPercentage().ToString("n2");
        }
        
        internal float GetDropsNumber()
        {
            if (Server == null)
                return 0f;
                
            return Server.GetDropsPercentage();
        }

        public class GameServer
        {
            private string CurrentIP = "";
            private string LastKnownIP = ""; // Сохраняем последний известный IP для проверки смены сервера
            public int PingPort { get; set; } = 0;
            private int gamePort;
            
            // STUN внешний IP
            public string ExternalIp { get; set; } = "";

            private const int PingLimitMilliseconds = 1000;
            private const int SeverePingFailureThreshold = 12;
            private static readonly TimeSpan SeverePingFailureCooldown = TimeSpan.FromSeconds(20);
            private int _ping = 0;
            public int AvgPing { get; set; } = 0;
            public string Location { get; set; } = "";
            private System.Timers.Timer PingTimer;

            private List<int> UserDefinedFallbackPorts;
            private int currentFallbackPortIndex = -1;

            private int consecutivePingFails = 0;
            private DateTime lastSeverePingFailureUtc = DateTime.MinValue;

            private bool isPinging = false;

            // --- UDP Ping fields ---
            private DateTime lastUdpPacketTime = DateTime.MinValue;
            private Queue<float> udpIntervals = new Queue<float>();
            private const int UdpIntervalsWindow = 10; // размер окна для сглаживания

            // --- Individual Tickrate tracking for this server ---
            public List<int> TicksHistory { get; set; } = new List<int>();
            public List<DateTime> TickTimestamps { get; set; } = new List<DateTime>();
            public int OutputTickRate { get; set; } = 0;
            public int AvgTickrate { get; set; } = 0;
            private const int TickHistoryDownsampleThreshold = 6000;
            private const int TickHistoryRecentKeep = 2000;
            
            // History management constants
            private const int MaxHistoryAgeHours = 24; // Максимальный возраст истории (часы)
            private const int MaxGraphDataPoints = 1000; // Максимум точек для графика

            // --- Individual Traffic tracking for this server ---
            // NOTE: Must be fields (not auto-properties) for Interlocked.Add compatibility
            public int UploadTraffic = 0;
            public int DownloadTraffic = 0;
            
            // --- Individual Session tracking for this server ---
            public DateTime SessionStart { get; set; } = DateTime.Now;
            
            // --- Individual Loss tracking for this server ---
            public int TotalTicksCount { get; set; } = 0;
            public int LostTicks { get; set; } = 0;
            public int AvgStableTickrate { get; set; } = 0;
            
            // --- Individual Log for this server ---
            public string TickRateLog { get; set; } = "";
            
            // --- Individual Graph data for this server ---
            public List<float> TickrateGraph { get; set; } = new List<float>();

            // --- Advanced Ping Spike Detection ---
            private const int SpikeTimeoutMs = 5000; // время отображения индикатора спайка (5 секунд)
            
            // Система накопления спайков для UI индикатора
            private Queue<DateTime> recentSpikes = new Queue<DateTime>();
            private const int SpikeCountThreshold = 5; // показывать индикатор при 5+ спайках (понижено с 8 для новой системы)
            private const int SpikeAnalysisWindowSeconds = 30; // анализируем спайки за последние 30 секунд
            private bool indicatorShown = false;
            
            /// <summary>
            /// Порог для определения спайка (превышение среднего на указанное количество миллисекунд)
            /// </summary>
            private double SpikeThresholdMs
            {
                get
                {
                    string thresholdStr = App.settingsManager?.GetOption("ping_spike_threshold", "200", "ADVANCED");
                    if (!string.IsNullOrEmpty(thresholdStr) && SettingsManager.TryParseInvariantDouble(thresholdStr.Trim(), out double threshold) && threshold > 0)
                    {
                        return threshold;
                    }
                    return 200.0; // значение по умолчанию (увеличено со 150 до 200)
                }
            }
            
            /// <summary>
            /// Процентный порог для определения спайка (превышение среднего на указанный процент)
            /// </summary>
            private double SpikeThresholdPercent
            {
                get
                {
                    string percentStr = App.settingsManager?.GetOption("ping_spike_threshold_percent", "120", "ADVANCED"); // изменено с 80 на 120
                    if (!string.IsNullOrEmpty(percentStr) && SettingsManager.TryParseInvariantDouble(percentStr.Trim(), out double percent) && percent > 0)
                    {
                        return percent;
                    }
                    return 120.0; // значение по умолчанию (120% превышение) - изменено с 80% на 120%
                }
            }
            
            /// <summary>
            /// Тип порога для определения спайков: "absolute" (абсолютные мс) или "percent" (процентное превышение)
            /// </summary>
            private string SpikeThresholdMode
            {
                get
                {
                    return App.settingsManager?.GetOption("ping_spike_threshold_mode", "percent", "ADVANCED") ?? "percent";
                }
            }

            public float UdpPing
            {
                get
                {
                    if (udpIntervals.Count == 0)
                        return 0;
                    return udpIntervals.Average();
                }
            }
            public bool IsUdpPingValid => udpIntervals.Count > 0 && UdpPing > 0 && UdpPing < 1000;
            public string GetUdpPingString()
            {
                if (!IsUdpPingValid)
                    return "n/a";
                return UdpPing.ToString("0");
            }

            /// <summary>
            /// Определяет, есть ли сейчас спайк пинга на основе накопленной статистики и активного спайка
            /// </summary>
            public bool HasPingSpike
            {
                get
                {
                    // FIX: Проверяем настройку show_ping_spikes В ПЕРВУЮ ОЧЕРЕДЬ
                    bool showSpikeIndicator = App.settingsManager?.GetOption("show_ping_spikes", "True", "ADVANCED") == "True";
                    if (!showSpikeIndicator)
                    {
                        return false; // Если показ спайков выключен - возвращаем false независимо от детекции
                    }
                    
                    // Проверяем настройки продвинутой детекции
                    bool useAdvancedDetection = App.settingsManager?.GetOption("advanced_spike_detection", "True", "ADVANCED") == "True";
                    
                    // Если используем продвинутую детекцию, проверяем активный спайк
                    if (useAdvancedDetection && HasActivePingSpike)
                    {
                        return true;
                    }
                    
                    // Очищаем старые спайки из очереди
                    var cutoffTime = DateTime.Now.AddSeconds(-SpikeAnalysisWindowSeconds);
                    while (recentSpikes.Count > 0 && recentSpikes.Peek() < cutoffTime)
                    {
                        recentSpikes.Dequeue();
                    }
                    
                    // Подсчитываем количество спайков за последние 30 секунд
                    int spikeCount = recentSpikes.Count;
                    
                    // Логика показа/скрытия индикатора
                    if (!indicatorShown && spikeCount >= SpikeCountThreshold)
                    {
                        // Показать индикатор при накоплении достаточного количества спайков
                        indicatorShown = true;
                        Debug.Print($"[SPIKE INDICATOR] SHOW: {spikeCount} spikes in {SpikeAnalysisWindowSeconds}s (threshold: {SpikeCountThreshold})");
                    }
                    else if (indicatorShown && spikeCount == 0)
                    {
                        // Скрыть индикатор когда спайков нет совсем за 30 секунд
                        indicatorShown = false;
                        Debug.Print($"[SPIKE INDICATOR] HIDE: No spikes in {SpikeAnalysisWindowSeconds}s - connection stabilized");
                    }
                    else if (indicatorShown)
                    {
                        Debug.Print($"[SPIKE INDICATOR] CONTINUE: {spikeCount} spikes in {SpikeAnalysisWindowSeconds}s");
                    }
                    
                    return indicatorShown;
                }
            }

            /// <summary>
            /// Проверяет текущий пинг на предмет спайка с использованием улучшенного алгоритма
            /// </summary>
            private void CheckForPingSpike(int currentPing)
            {
                // Проверяем настройки улучшенной детекции
                bool useAdvancedDetection = App.settingsManager?.GetOption("advanced_spike_detection", "True", "ADVANCED") == "True";
                if (!useAdvancedDetection)
                {
                    // Используем классическую систему детекции
                    CheckForPingSpikeClassic(currentPing);
                    return;
                }

                // Улучшенная система детекции с EMA и EW-стандартным отклонением
                CheckForPingSpikeAdvanced(currentPing);
            }

            // EMA состояние для продвинутой детекции PING
            private double _pingEma = 0;
            private double _pingEwVar = 0;
            private bool _pingDetectorInitialized = false;
            private bool _inPingSpike = false;
            private DateTime _spikeStartTime = DateTime.MinValue;
            private DateTime _spikeEndTime = DateTime.MinValue;
            private double _spikeHoldTime = 0;
            private double _timeSinceSpike = 1000; // большое значение для начала
            private double _spikePeak = 0;

            // EMA состояние для продвинутой детекции TICKRATE
            private double _tickrateEma = 0;
            private double _tickrateEwVar = 0;
            private bool _tickrateDetectorInitialized = false;
            private bool _inTickRateSpike = false;
            private DateTime _tickrateSpikeStartTime = DateTime.MinValue;
            private DateTime _tickrateSpikeEndTime = DateTime.MinValue;
            private double _tickrateSpikeHoldTime = 0;
            private double _tickrateTimeSinceSpike = 1000;
            private double _tickrateSpikePeak = 0;

            // EMA состояние для продвинутой детекции TICKTIME
            private double _ticktimeEma = 0;
            private double _ticktimeEwVar = 0;
            private bool _ticktimeDetectorInitialized = false;
            private bool _inTickTimeSpike = false;
            private DateTime _ticktimeSpikeStartTime = DateTime.MinValue;
            private DateTime _ticktimeSpikeEndTime = DateTime.MinValue;
            private double _ticktimeSpikeHoldTime = 0;
            private double _ticktimeTimeSinceSpike = 1000;
            private double _ticktimeSpikePeak = 0;

            /// <summary>
            /// Продвинутая детекция спайков с EMA, EW-стандартным отклонением и гистерезисом
            /// </summary>
            private void CheckForPingSpikeAdvanced(int currentPing)
            {
                var now = DateTime.Now;
                var pingSeconds = currentPing / 1000.0; // конвертируем в секунды для внутренних расчетов
                
                // Читаем пресет чувствительности из настроек
                string sensitivityPreset = "very_low";
                if (App.settingsManager != null)
                {
                    sensitivityPreset = App.settingsManager.GetOption("spikes.sensitivity", "very_low", "ADVANCED");
                }
                
                // Параметры детектора в зависимости от пресета
                double tauSec, minAbsMs, minRel, kHi, kLo;
                
                switch (sensitivityPreset.ToLower())
                {
                    case "very_low":
                        // ОЧЕНЬ низкая чувствительность - только критические ситуации
                        tauSec = 10.0;
                        minAbsMs = 40.0;
                        minRel = 1.2;
                        kHi = 5.0;
                        kLo = 0.5;
                        break;
                        
                    case "low":
                        // Низкая чувствительность
                        tauSec = 8.0;
                        minAbsMs = 25.0;
                        minRel = 0.8;
                        kHi = 4.0;
                        kLo = 1.0;
                        break;
                        
                    case "medium":
                        // Средняя чувствительность (по умолчанию)
                        tauSec = 6.0;
                        minAbsMs = 15.0;
                        minRel = 0.6;
                        kHi = 3.0;
                        kLo = 1.5;
                        break;
                        
                    case "high":
                        // Высокая чувствительность
                        tauSec = 4.0;
                        minAbsMs = 10.0;
                        minRel = 0.4;
                        kHi = 2.5;
                        kLo = 1.8;
                        break;
                        
                    case "auto":
                        // Автоматическая (адаптивная) - используем medium как базу
                        tauSec = 6.0;
                        minAbsMs = 15.0;
                        minRel = 0.6;
                        kHi = 3.0;
                        kLo = 1.5;
                        break;
                        
                    default:
                        // По умолчанию medium
                        tauSec = 6.0;
                        minAbsMs = 15.0;
                        minRel = 0.6;
                        kHi = 3.0;
                        kLo = 1.5;
                        break;
                }
                
                // Читаем минимальную длительность из настроек
                int minHoldMs = 50;
                if (App.settingsManager != null)
                {
                    string minHoldStr = App.settingsManager.GetOption("spikes.min_hold_ms", "50", "ADVANCED");
                    if (int.TryParse(minHoldStr, out int parsedMinHold) && parsedMinHold > 0)
                    {
                        minHoldMs = parsedMinHold;
                    }
                }
                double minHoldSec = minHoldMs / 1000.0;
                double maxHoldSec = 10.0; // максимум 10 секунд для very_low
                double refractorySec = 1.5;
                double mergeWindow = 0.2;

                // Инициализация при первом запуске
                if (!_pingDetectorInitialized)
                {
                    _pingEma = pingSeconds;
                    _pingEwVar = Math.Pow(pingSeconds * 0.1, 2); // начальная дисперсия как 10% от начального значения
                    _pingDetectorInitialized = true;
                    Debug.Print($"[AdvancedPingSpike] Initialized with base {currentPing}ms");
                    return;
                }

                // Вычисляем dt для адаптации
                double dt = 0.1; // предполагаем ~100мс между обновлениями пинга
                
                // Обновляем EMA
                double alpha = dt / (tauSec + dt);
                double dx = pingSeconds - _pingEma;
                _pingEma += alpha * dx;

                // Обновляем EW дисперсию
                double beta = alpha;
                _pingEwVar = (1 - beta) * _pingEwVar + beta * dx * dx;
                double sigma = Math.Sqrt(Math.Max(_pingEwVar, 1e-12));

                // Вычисляем гибридный порог
                double thrAbs = minAbsMs / 1000.0; // конвертируем в секунды
                double thrRel = Math.Abs(minRel * _pingEma);
                double thrSig = kHi * sigma;
                double threshold = Math.Max(thrAbs, Math.Max(thrRel, thrSig));

                _timeSinceSpike += dt;

                if (!_inPingSpike)
                {
                    // Проверяем превышение порога для пинга (только возрастающие спайки)
                    bool cross = (pingSeconds - _pingEma) > threshold;
                    
                    if (cross && _timeSinceSpike >= refractorySec)
                    {
                        // Проверяем анти-бурст объединение
                        if (_spikeEndTime != DateTime.MinValue && 
                            (now - _spikeEndTime).TotalSeconds < mergeWindow)
                        {
                            Debug.Print($"[AdvancedPingSpike] Merging with recent spike (within {mergeWindow}s)");
                            _inPingSpike = true;
                            _spikePeak = Math.Max(_spikePeak, (pingSeconds - _pingEma) * 1000); // пик в мс
                            return;
                        }
                        
                        // Начинаем новый спайк
                        _spikeHoldTime = 0;
                        _inPingSpike = true;
                        _timeSinceSpike = 0;
                        _spikeStartTime = now;
                        _spikePeak = (pingSeconds - _pingEma) * 1000; // пик в мс
                        
                        // Добавляем в очередь для UI индикатора
                        recentSpikes.Enqueue(DateTime.Now);
                        
                        Debug.Print($"[AdvancedPingSpike] SPIKE START: {currentPing}ms, μ={_pingEma*1000:F1}ms, σ={sigma*1000:F2}ms, threshold={threshold*1000:F1}ms, peak={_spikePeak:F1}ms");
                    }
                }
                else
                {
                    _spikeHoldTime += dt;
                    
                    // Обновляем пик спайка
                    var currentDeviation = (pingSeconds - _pingEma) * 1000;
                    if (currentDeviation > _spikePeak)
                    {
                        _spikePeak = currentDeviation;
                    }
                    
                    // Проверяем условие выхода из спайка (гистерезис)
                    double lowerThreshold = kLo * sigma;
                    bool belowLower = (pingSeconds - _pingEma) < lowerThreshold;
                    
                    // УПРОЩЕННАЯ защита: снимаем если пинг вернулся к нормальному уровню
                    // Проверяем только если пинг близок к EMA (в пределах 1.3x)
                    double absoluteHighThreshold = (_pingEma * 1.3);
                    bool stillObjectivelyHigh = pingSeconds > absoluteHighThreshold;
                    
                    // Защита от "вечного" индикатора: принудительно снимаем после maxHoldSec
                    bool maxDurationExceeded = _spikeHoldTime >= maxHoldSec;
                    
                    // УПРОЩЕННОЕ условие снятия: снимаем если ЛИБО порог пройден, ЛИБО время вышло
                    // Убираем требование минимальной длительности для быстрого снятия
                    if (belowLower && !stillObjectivelyHigh)
                    {
                        _inPingSpike = false;
                        _timeSinceSpike = 0;
                        _spikeEndTime = now;
                        
                        double spikeEnergy = _spikePeak * _spikeHoldTime;
                        Debug.Print($"[AdvancedPingSpike] SPIKE END (NORMAL): duration={_spikeHoldTime:F3}s, peak={_spikePeak:F1}ms");
                        
                        _spikePeak = 0;
                        _spikeStartTime = DateTime.MinValue;
                    }
                    else if (maxDurationExceeded)
                    {
                        // Принудительное снятие по времени
                        _inPingSpike = false;
                        _timeSinceSpike = 0;
                        _spikeEndTime = now;
                        
                        Debug.Print($"[AdvancedPingSpike] SPIKE END (MAX_DURATION): forced removal after {maxHoldSec}s");
                        
                        _spikePeak = 0;
                        _spikeStartTime = DateTime.MinValue;
                    }
                    else if (DateTime.Now.Millisecond % 500 < 50) // логируем каждые ~500мс
                    {
                        Debug.Print($"[AdvancedPingSpike] ONGOING: current={currentPing}ms, EMA={_pingEma*1000:F1}ms, deviation={currentDeviation:F1}ms, threshold={lowerThreshold*1000:F1}ms, hold={_spikeHoldTime:F2}s");
                    }
                }
            }

            /// <summary>
            /// Определяет, есть ли сейчас активный спайк пинга (для UI)
            /// </summary>
            public bool HasActivePingSpike => _inPingSpike;

            /// <summary>
            /// Определяет, есть ли сейчас активный спайк тикрейта (для UI)
            /// </summary>
            public bool HasActiveTickRateSpike => _inTickRateSpike;

            /// <summary>
            /// Определяет, есть ли сейчас активный спайк тиктайма (для UI)
            /// </summary>
            public bool HasActiveTickTimeSpike => _inTickTimeSpike;

            /// <summary>
            /// Устанавливает флаг спайка пинга (для внешних детекторов)
            /// </summary>
            public void SetPingSpike(bool hasSpike)
            {
                _inPingSpike = hasSpike;
            }

            /// <summary>
            /// Устанавливает флаг спайка тикрейта (для внешних детекторов)
            /// </summary>
            public void SetTickRateSpike(bool hasSpike)
            {
                _inTickRateSpike = hasSpike;
            }

            /// <summary>
            /// Устанавливает флаг спайка тиктайма (для внешних детекторов)
            /// </summary>
            public void SetTickTimeSpike(bool hasSpike)
            {
                _inTickTimeSpike = hasSpike;
            }

            /// <summary>
            /// Продвинутая детекция спайков TICKRATE с EMA, EW-стандартным отклонением и гистерезисом
            /// ВАЖНО: Детектируем ПАДЕНИЕ tickrate (инверсия относительно ping!)
            /// </summary>
            public void CheckForTickrateSpike(float currentTickrate)
            {
                // Проверяем настройки детекции tickrate
                bool detectTickrateSpikes = App.settingsManager?.GetOption("spikes.detect_tickrate", "True", "ADVANCED") == "True";
                if (!detectTickrateSpikes || currentTickrate <= 0)
                {
                    return;
                }

                var now = DateTime.Now;
                
                // Читаем пресет чувствительности из настроек
                string sensitivityPreset = "medium";
                if (App.settingsManager != null)
                {
                    sensitivityPreset = App.settingsManager.GetOption("spikes.sensitivity", "medium", "ADVANCED");
                }
                
                // Параметры детектора в зависимости от пресета (для TICKRATE)
                double tauSec, minAbsHz, minRel, kHi, kLo;
                
                switch (sensitivityPreset.ToLower())
                {
                    case "very_low":
                        tauSec = 10.0;
                        minAbsHz = 25.0;
                        minRel = 0.40;
                        kHi = 5.0;
                        kLo = 2.0; // выше чем у ping для быстрого снятия
                        break;
                        
                    case "low":
                        tauSec = 8.0;
                        minAbsHz = 18.0;
                        minRel = 0.30;
                        kHi = 4.0;
                        kLo = 1.5;
                        break;
                        
                    case "medium":
                        tauSec = 6.0;
                        minAbsHz = 12.0;
                        minRel = 0.22;
                        kHi = 3.0;
                        kLo = 1.8;
                        break;
                        
                    case "high":
                        tauSec = 4.0;
                        minAbsHz = 8.0;
                        minRel = 0.15;
                        kHi = 2.5;
                        kLo = 2.0;
                        break;
                        
                    case "auto":
                        tauSec = 6.0;
                        minAbsHz = 12.0;
                        minRel = 0.22;
                        kHi = 3.0;
                        kLo = 1.8;
                        break;
                        
                    default:
                        tauSec = 6.0;
                        minAbsHz = 12.0;
                        minRel = 0.22;
                        kHi = 3.0;
                        kLo = 1.8;
                        break;
                }
                
                double minHoldSec = 0.05; // 50ms
                double maxHoldSec = 10.0;
                double refractorySec = 1.5;
                double mergeWindow = 0.2;

                // Инициализация при первом запуске
                if (!_tickrateDetectorInitialized)
                {
                    _tickrateEma = currentTickrate;
                    _tickrateEwVar = Math.Pow(currentTickrate * 0.1, 2);
                    _tickrateDetectorInitialized = true;
                    Debug.Print($"[AdvancedTickrateSpike] Initialized with base {currentTickrate:F1} Hz");
                    return;
                }

                double dt = 0.1; // ~100мс между обновлениями
                
                // Обновляем EMA
                double alpha = dt / (tauSec + dt);
                double dx = currentTickrate - _tickrateEma;
                _tickrateEma += alpha * dx;

                // Обновляем EW дисперсию
                double beta = alpha;
                _tickrateEwVar = (1 - beta) * _tickrateEwVar + beta * dx * dx;
                double sigma = Math.Sqrt(Math.Max(_tickrateEwVar, 1e-12));

                // Вычисляем гибридный порог
                double thrAbs = minAbsHz;
                double thrRel = Math.Abs(minRel * _tickrateEma);
                double thrSig = kHi * sigma;
                double threshold = Math.Max(thrAbs, Math.Max(thrRel, thrSig));

                _tickrateTimeSinceSpike += dt;

                if (!_inTickRateSpike)
                {
                    // ИНВЕРСИЯ: Проверяем ПАДЕНИЕ tickrate (EMA - current > threshold)
                    bool cross = (_tickrateEma - currentTickrate) > threshold;
                    
                    if (cross && _tickrateTimeSinceSpike >= refractorySec)
                    {
                        // Проверяем анти-бурст объединение
                        if (_tickrateSpikeEndTime != DateTime.MinValue && 
                            (now - _tickrateSpikeEndTime).TotalSeconds < mergeWindow)
                        {
                            Debug.Print($"[AdvancedTickrateSpike] Merging with recent spike");
                            _inTickRateSpike = true;
                            _tickrateSpikePeak = Math.Max(_tickrateSpikePeak, (_tickrateEma - currentTickrate));
                            return;
                        }
                        
                        // Начинаем новый спайк
                        _tickrateSpikeHoldTime = 0;
                        _inTickRateSpike = true;
                        _tickrateTimeSinceSpike = 0;
                        _tickrateSpikeStartTime = now;
                        _tickrateSpikePeak = (_tickrateEma - currentTickrate);
                        
                        Debug.Print($"[AdvancedTickrateSpike] SPIKE START: {currentTickrate:F1}Hz (dropped from μ={_tickrateEma:F1}Hz), σ={sigma:F2}Hz, threshold={threshold:F1}Hz, peak={_tickrateSpikePeak:F1}Hz");
                    }
                }
                else
                {
                    _tickrateSpikeHoldTime += dt;
                    
                    // Обновляем пик спайка (максимальное падение)
                    var currentDeviation = (_tickrateEma - currentTickrate);
                    if (currentDeviation > _tickrateSpikePeak)
                    {
                        _tickrateSpikePeak = currentDeviation;
                    }
                    
                    // Проверяем условие выхода из спайка (гистерезис)
                    double lowerThreshold = kLo * sigma;
                    bool belowLower = (_tickrateEma - currentTickrate) < lowerThreshold;
                    
                    // Защита от "вечного" индикатора
                    bool maxDurationExceeded = _tickrateSpikeHoldTime >= maxHoldSec;
                    
                    if (belowLower)
                    {
                        _inTickRateSpike = false;
                        _tickrateTimeSinceSpike = 0;
                        _tickrateSpikeEndTime = now;
                        
                        Debug.Print($"[AdvancedTickrateSpike] SPIKE END (NORMAL): duration={_tickrateSpikeHoldTime:F3}s, peak={_tickrateSpikePeak:F1}Hz drop");
                        
                        _tickrateSpikePeak = 0;
                        _tickrateSpikeStartTime = DateTime.MinValue;
                    }
                    else if (maxDurationExceeded)
                    {
                        _inTickRateSpike = false;
                        _tickrateTimeSinceSpike = 0;
                        _tickrateSpikeEndTime = now;
                        
                        Debug.Print($"[AdvancedTickrateSpike] SPIKE END (MAX_DURATION): forced after {maxHoldSec}s");
                        
                        _tickrateSpikePeak = 0;
                        _tickrateSpikeStartTime = DateTime.MinValue;
                    }
                    else if (DateTime.Now.Millisecond % 500 < 50)
                    {
                        Debug.Print($"[AdvancedTickrateSpike] ONGOING: current={currentTickrate:F1}Hz, EMA={_tickrateEma:F1}Hz, deviation={currentDeviation:F1}Hz, threshold={lowerThreshold:F1}Hz, hold={_tickrateSpikeHoldTime:F2}s");
                    }
                }
            }

            /// <summary>
            /// Продвинутая детекция спайков TICKTIME с EMA, EW-стандартным отклонением и гистерезисом
            /// Детектируем УВЕЛИЧЕНИЕ ticktime (аналогично ping)
            /// </summary>
            public void CheckForTicktimeSpike(float currentTicktime)
            {
                // Проверяем настройки детекции ticktime
                bool detectTicktimeSpikes = App.settingsManager?.GetOption("spikes.detect_ticktime", "True", "ADVANCED") == "True";
                if (!detectTicktimeSpikes || currentTicktime <= 0)
                {
                    return;
                }

                var now = DateTime.Now;
                var ticktimeSeconds = currentTicktime / 1000.0; // конвертируем в секунды
                
                // Читаем пресет чувствительности из настроек
                string sensitivityPreset = "medium";
                if (App.settingsManager != null)
                {
                    sensitivityPreset = App.settingsManager.GetOption("spikes.sensitivity", "medium", "ADVANCED");
                }
                
                // Параметры детектора в зависимости от пресета (для TICKTIME)
                double tauSec, minAbsMs, minRel, kHi, kLo;
                
                switch (sensitivityPreset.ToLower())
                {
                    case "very_low":
                        tauSec = 10.0;
                        minAbsMs = 25.0;
                        minRel = 1.0;
                        kHi = 5.0;
                        kLo = 1.0; // выше чем у ping
                        break;
                        
                    case "low":
                        tauSec = 8.0;
                        minAbsMs = 18.0;
                        minRel = 0.7;
                        kHi = 4.0;
                        kLo = 1.2;
                        break;
                        
                    case "medium":
                        tauSec = 6.0;
                        minAbsMs = 12.0;
                        minRel = 0.55;
                        kHi = 3.0;
                        kLo = 1.6;
                        break;
                        
                    case "high":
                        tauSec = 4.0;
                        minAbsMs = 8.0;
                        minRel = 0.38;
                        kHi = 2.5;
                        kLo = 1.9;
                        break;
                        
                    case "auto":
                        tauSec = 6.0;
                        minAbsMs = 12.0;
                        minRel = 0.55;
                        kHi = 3.0;
                        kLo = 1.6;
                        break;
                        
                    default:
                        tauSec = 6.0;
                        minAbsMs = 12.0;
                        minRel = 0.55;
                        kHi = 3.0;
                        kLo = 1.6;
                        break;
                }
                
                double minHoldSec = 0.05; // 50ms
                double maxHoldSec = 10.0;
                double refractorySec = 1.5;
                double mergeWindow = 0.2;

                // Инициализация при первом запуске
                if (!_ticktimeDetectorInitialized)
                {
                    _ticktimeEma = ticktimeSeconds;
                    _ticktimeEwVar = Math.Pow(ticktimeSeconds * 0.1, 2);
                    _ticktimeDetectorInitialized = true;
                    Debug.Print($"[AdvancedTicktimeSpike] Initialized with base {currentTicktime:F1}ms");
                    return;
                }

                double dt = 0.1; // ~100мс между обновлениями
                
                // Обновляем EMA
                double alpha = dt / (tauSec + dt);
                double dx = ticktimeSeconds - _ticktimeEma;
                _ticktimeEma += alpha * dx;

                // Обновляем EW дисперсию
                double beta = alpha;
                _ticktimeEwVar = (1 - beta) * _ticktimeEwVar + beta * dx * dx;
                double sigma = Math.Sqrt(Math.Max(_ticktimeEwVar, 1e-12));

                // Вычисляем гибридный порог
                double thrAbs = minAbsMs / 1000.0; // в секунды
                double thrRel = Math.Abs(minRel * _ticktimeEma);
                double thrSig = kHi * sigma;
                double threshold = Math.Max(thrAbs, Math.Max(thrRel, thrSig));

                _ticktimeTimeSinceSpike += dt;

                if (!_inTickTimeSpike)
                {
                    // Проверяем УВЕЛИЧЕНИЕ ticktime (аналогично ping)
                    bool cross = (ticktimeSeconds - _ticktimeEma) > threshold;
                    
                    if (cross && _ticktimeTimeSinceSpike >= refractorySec)
                    {
                        // Проверяем анти-бурст объединение
                        if (_ticktimeSpikeEndTime != DateTime.MinValue && 
                            (now - _ticktimeSpikeEndTime).TotalSeconds < mergeWindow)
                        {
                            Debug.Print($"[AdvancedTicktimeSpike] Merging with recent spike");
                            _inTickTimeSpike = true;
                            _ticktimeSpikePeak = Math.Max(_ticktimeSpikePeak, (ticktimeSeconds - _ticktimeEma) * 1000);
                            return;
                        }
                        
                        // Начинаем новый спайк
                        _ticktimeSpikeHoldTime = 0;
                        _inTickTimeSpike = true;
                        _ticktimeTimeSinceSpike = 0;
                        _ticktimeSpikeStartTime = now;
                        _ticktimeSpikePeak = (ticktimeSeconds - _ticktimeEma) * 1000; // пик в мс
                        
                        Debug.Print($"[AdvancedTicktimeSpike] SPIKE START: {currentTicktime:F1}ms (up from μ={_ticktimeEma*1000:F1}ms), σ={sigma*1000:F2}ms, threshold={threshold*1000:F1}ms, peak={_ticktimeSpikePeak:F1}ms");
                    }
                }
                else
                {
                    _ticktimeSpikeHoldTime += dt;
                    
                    // Обновляем пик спайка
                    var currentDeviation = (ticktimeSeconds - _ticktimeEma) * 1000;
                    if (currentDeviation > _ticktimeSpikePeak)
                    {
                        _ticktimeSpikePeak = currentDeviation;
                    }
                    
                    // Проверяем условие выхода из спайка (гистерезис)
                    double lowerThreshold = kLo * sigma;
                    bool belowLower = (ticktimeSeconds - _ticktimeEma) < lowerThreshold;
                    
                    // Защита от "вечного" индикатора
                    bool maxDurationExceeded = _ticktimeSpikeHoldTime >= maxHoldSec;
                    
                    if (belowLower)
                    {
                        _inTickTimeSpike = false;
                        _ticktimeTimeSinceSpike = 0;
                        _ticktimeSpikeEndTime = now;
                        
                        Debug.Print($"[AdvancedTicktimeSpike] SPIKE END (NORMAL): duration={_ticktimeSpikeHoldTime:F3}s, peak={_ticktimeSpikePeak:F1}ms");
                        
                        _ticktimeSpikePeak = 0;
                        _ticktimeSpikeStartTime = DateTime.MinValue;
                    }
                    else if (maxDurationExceeded)
                    {
                        _inTickTimeSpike = false;
                        _ticktimeTimeSinceSpike = 0;
                        _ticktimeSpikeEndTime = now;
                        
                        Debug.Print($"[AdvancedTicktimeSpike] SPIKE END (MAX_DURATION): forced after {maxHoldSec}s");
                        
                        _ticktimeSpikePeak = 0;
                        _ticktimeSpikeStartTime = DateTime.MinValue;
                    }
                    else if (DateTime.Now.Millisecond % 500 < 50)
                    {
                        Debug.Print($"[AdvancedTicktimeSpike] ONGOING: current={currentTicktime:F1}ms, EMA={_ticktimeEma*1000:F1}ms, deviation={currentDeviation:F1}ms, threshold={lowerThreshold*1000:F1}ms, hold={_ticktimeSpikeHoldTime:F2}s");
                    }
                }
            }

            /// <summary>
            /// Классическая система детекции спайков (для совместимости)
            /// </summary>
            private void CheckForPingSpikeClassic(int currentPing)
            {
                if (App.meterState?.pingBuffer == null)
                    return;

                // Thread-safe копирование pingBuffer для избежания InvalidOperationException
                List<float> recentPings;
                lock (App.meterState._pingBufferLock)
                {
                    if (App.meterState.pingBuffer.Count < 5)
                        return;

                    // Создаем копию последних 10 значений для безопасной обработки
                    var bufferCount = App.meterState.pingBuffer.Count;
                    var startIndex = Math.Max(0, bufferCount - 10);
                    recentPings = new List<float>();
                    
                    for (int i = startIndex; i < bufferCount; i++)
                    {
                        if (App.meterState.pingBuffer[i] > 0)
                            recentPings.Add(App.meterState.pingBuffer[i]);
                    }
                }
                    
                if (recentPings.Count < 3)
                    return;

                double avgPing = recentPings.Average();
                
                // Определяем порог в зависимости от режима
                double threshold;
                bool isSpike = false;
                string debugInfo;
                
                if (SpikeThresholdMode == "percent")
                {
                    // Умная адаптивная система детекции спайков
                    
                    // 1. Базовый процентный порог
                    threshold = avgPing * (SpikeThresholdPercent / 100.0);
                    
                    // 2. Адаптивный минимальный порог в зависимости от базового пинга
                    double adaptiveMinThreshold;
                    if (avgPing < 50)
                        adaptiveMinThreshold = 50.0;  // Для низкого пинга - высокий порог (увеличено с 30 до 50)
                    else if (avgPing < 100)
                        adaptiveMinThreshold = Math.Max(60.0, avgPing * 0.6); // 60% от базового пинга, минимум 60мс (было 40% и 40мс)
                    else if (avgPing < 200)
                        adaptiveMinThreshold = Math.Max(80.0, avgPing * 0.5); // 50% от базового пинга, минимум 80мс (было 30% и 50мс)
                    else
                        adaptiveMinThreshold = Math.Max(100.0, avgPing * 0.4); // 40% от базового пинга, минимум 100мс (было 25% и 60мс)
                    
                    threshold = Math.Max(threshold, adaptiveMinThreshold);
                    
                    // 3. Абсолютный минимум - спайком считается только пинг выше определенного значения
                    double absoluteMinimum = Math.Max(150, avgPing * 2.0); // минимум 150мс или в 2 раза больше среднего (было 80мс и 1.5)
                    
                    isSpike = currentPing > avgPing + threshold && currentPing > absoluteMinimum;
                    debugInfo = $"Current: {currentPing}ms, Average: {avgPing:F1}ms, Adaptive threshold: +{threshold:F1}ms (min {adaptiveMinThreshold:F1}ms), Abs min: {absoluteMinimum:F1}ms";
                }
                else
                {
                    // Абсолютное превышение в миллисекундах
                    threshold = SpikeThresholdMs;
                    double absoluteMinimum = Math.Max(180, avgPing * 2.5); // более строгий минимум для абсолютного режима (было 100 и 1.8)
                    isSpike = currentPing > avgPing + threshold && currentPing > absoluteMinimum;
                    debugInfo = $"Current: {currentPing}ms, Average: {avgPing:F1}ms, Absolute threshold: +{threshold}ms, Abs min: {absoluteMinimum:F1}ms";
                }
                
                if (isSpike)
                {
                    recentSpikes.Enqueue(DateTime.Now);
                    Debug.Print($"[CLASSIC PING SPIKE DETECTED] {debugInfo}, Total spikes: {recentSpikes.Count}");
                }
            }

            public void UpdateUdpPing(DateTime packetTime)
            {
                if (lastUdpPacketTime != DateTime.MinValue)
                {
                    float interval = (float)(packetTime - lastUdpPacketTime).TotalMilliseconds;
                    // Фильтр: только реальные интервалы (например, 5мс < x < 1000мс)
                    if (interval > 5 && interval < 1000)
                    {
                        udpIntervals.Enqueue(interval);
                        if (udpIntervals.Count > UdpIntervalsWindow)
                            udpIntervals.Dequeue();
                        
                        // Проверяем на спайк UDP пинга
                        CheckForPingSpike((int)UdpPing);
                    }
                    // Добавим логирование каждого интервала и текущего среднего UDP ping
                    Debug.Print($"[UDP PING DEBUG] interval={interval} ms, avgUdpPing={UdpPing:0} ms, intervalsCount={udpIntervals.Count}");
                }
                else
                {
                    Debug.Print("[UDP PING DEBUG] first UDP packet");
                }
                lastUdpPacketTime = packetTime;
            }

            public void ResetUdpPing()
            {
                lastUdpPacketTime = DateTime.MinValue;
                udpIntervals.Clear();
            }

            public GameServer()
            {
                string ping_ports_setting = App.settingsManager.GetOption("ping_ports");
                if (!string.IsNullOrEmpty(ping_ports_setting))
                {
                    UserDefinedFallbackPorts = ping_ports_setting.Split(',')
                        .Select(p_str => int.TryParse(p_str.Trim(), out int p_val) ? p_val : -1)
                        .Where(p_val => p_val > 0 && p_val <= 65535)
                        .ToList();
                }
                else
                {
                    UserDefinedFallbackPorts = new List<int>() { 80 };
                }
            }

            public int Ping
            {
                get { return _ping; }
                set
                {
                    _ping = value;
                    // Обновляем pingBuffer при изменении пинга
                    if (App.meterState != null && value > 0)
                    {
                        lock (App.meterState._pingBufferLock)
                        {
                            App.meterState.pingBuffer.Add(_ping);
                            if (App.meterState.pingBuffer.Count > 512)
                            {
                                App.meterState.pingBuffer.RemoveAt(0);
                            }
                        }
                        
                        // Проверяем на спайк пинга
                        CheckForPingSpike(_ping);
                    }
                    if (AvgPing == 0 && _ping > 0) AvgPing = _ping;
                    else if (_ping > 0) AvgPing = (AvgPing + _ping) / 2;
                }
            }

            public string Ip
            {
                get { return CurrentIP; }
                set
                {
                    string oldIP = CurrentIP;
                    CurrentIP = value;
                    
                    // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Сбрасываем счетчики только при смене РЕАЛЬНОГО IP сервера
                    // Сравниваем с LastKnownIP, а не с oldIP (который может быть пустым после Reset())
                    bool serverChanged = !string.IsNullOrEmpty(CurrentIP) && 
                                        CurrentIP != LastKnownIP && 
                                        !string.IsNullOrEmpty(LastKnownIP);
                    
                    if (!string.IsNullOrEmpty(CurrentIP))
                    {
                        if (serverChanged)
                        {
                            // Сервер действительно изменился - сбрасываем счетчики трафика
                            Debug.Print($"[GameServer] IP changed: {LastKnownIP} -> {CurrentIP}, resetting traffic counters");
                            UploadTraffic = 0;
                            DownloadTraffic = 0;
                            SessionStart = DateTime.Now;
                        }
                        else if (string.IsNullOrEmpty(LastKnownIP))
                        {
                            // Первая установка IP - инициализируем счетчики
                            Debug.Print($"[GameServer] Initial IP set: {CurrentIP}");
                            UploadTraffic = 0;
                            DownloadTraffic = 0;
                            SessionStart = DateTime.Now;
                        }
                        else
                        {
                            // Тот же IP - НЕ сбрасываем счетчики, они продолжают накапливаться
                            Debug.Print($"[GameServer] IP unchanged: {CurrentIP}, preserving traffic counters (DL={DownloadTraffic}, UL={UploadTraffic})");
                        }
                        
                        // КРИТИЧЕСКОЕ: Инициализация метрик ТОЛЬКО при первом запуске или реальной смене сервера
                        bool needsInitialization = string.IsNullOrEmpty(LastKnownIP) || serverChanged;
                        
                        // Обновляем LastKnownIP для следующей проверки
                        LastKnownIP = CurrentIP;
                        
                        if (needsInitialization)
                        {
                            // Reset individual server data for new IP
                            // This ensures each IP has its own fresh metrics
                            TicksHistory.Clear();
                            TickTimestamps.Clear();
                            // TickrateGraph.Clear(); // ОТКЛЮЧЕНО: не очищаем график тикрейта для непрерывного отображения
                            OutputTickRate = 0;
                            AvgTickrate = 0;
                            TotalTicksCount = 0;
                            LostTicks = 0;
                            AvgStableTickrate = 0;
                            TickRateLog = "";

                            if (PingTimer == null || !PingTimer.Enabled)
                            {
                                SetPingTimer();
                            }
                            DetectLocation();
                            consecutivePingFails = 0;
                            currentFallbackPortIndex = -1;
                            this.PingPort = this.GamePort > 0 ? this.GamePort : 0;
                        }
                    }
                }
            }

            public int GamePort
            {
                get => gamePort;
                set
                {
                    if (value > 0 && value <= 65535 && value != gamePort)
                    {
                        gamePort = value;
                        this.PingPort = gamePort;
                        consecutivePingFails = 0;
                        currentFallbackPortIndex = -1;
                        Debug.Print($"GamePort set to: {gamePort}. PingPort also set to {this.PingPort}.");
                    }
                    else if (value <= 0 && gamePort != 0)
                    {
                        gamePort = 0;
                        Debug.Print($"GamePort unset (was {this.PingPort}). PingPort remains {this.PingPort} for now.");
                    }
                }
            }

            internal void Reset()
            {
                CurrentIP = "";
                // КРИТИЧЕСКОЕ: НЕ очищаем LastKnownIP - он нужен для проверки смены сервера
                // LastKnownIP сохраняется между вызовами Reset()
                
                ExternalIp = "";
                Location = "N/A";
                PingPort = 0;
                gamePort = 0;
                AvgPing = 0;
                _ping = 0;
                consecutivePingFails = 0;
                currentFallbackPortIndex = -1;
                
                // Reset individual tickrate data
                TicksHistory.Clear();
                TickTimestamps.Clear();
                OutputTickRate = 0;
                AvgTickrate = 0;
                
                // ИСПРАВЛЕНИЕ: НЕ сбрасываем счетчики трафика в Reset()
                // Счетчики должны сбрасываться ТОЛЬКО при смене IP в Ip setter
                // UploadTraffic = 0;
                // DownloadTraffic = 0;
                
                // Reset individual session data
                // НЕ сбрасываем SessionStart - он сохраняется для текущего IP
                // SessionStart = DateTime.Now;
                
                // Reset individual loss data
                TotalTicksCount = 0;
                LostTicks = 0;
                AvgStableTickrate = 0;
                
                // Reset individual log and graph data
                TickRateLog = "";
                // TickrateGraph.Clear(); // ОТКЛЮЧЕНО: не очищаем график тикрейта для непрерывного отображения
                
                KillTimer();
            }

            public void KillTimer()
            {
                if (PingTimer != null)
                {
                    PingTimer.Enabled = false;
                    PingTimer.Stop();
                    PingTimer.Dispose();
                    PingTimer = null;
                    Debug.Print("killed server timer");
                }
            }

            public void SetPingTimer()
            {
                ICMPfails = 0;
                int PingInterval = 2000;
                // Интервал пинга берется из настроек
                string intervalStr = App.settingsManager.GetOption("ping_interval");
                if (!string.IsNullOrEmpty(intervalStr))
                {
                    int parsed;
                    if (int.TryParse(intervalStr, out parsed) && parsed > 0)
                        PingInterval = parsed;
                }
                // Если таймер уже существует, просто обновляем интервал
                if (PingTimer != null)
                {
                    PingTimer.Interval = PingInterval;
                    PingTimer.Enabled = true;
                }
                else
                {
                    PingTimer = new System.Timers.Timer
                    {
                        Interval = PingInterval
                    };
                    PingTimer.Elapsed += PingServerTimer;
                    PingTimer.AutoReset = true;
                    PingTimer.Enabled = true;
                }
            }

            private void PingServerTimer(Object source, System.Timers.ElapsedEventArgs e)
            {
                if (string.IsNullOrEmpty(Ip) || isPinging) return;

                isPinging = true;
                _ = Task.Run(async () => {
                    try
                    {
                        await Task.Run(() => PingServer()).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.log($"[PingServerTimer] Error: {ex.Message}");
                    }
                    finally
                    {
                        isPinging = false;
                    }
                });
            }

            /// <summary>
            /// Updates individual tickrate data for this specific server
            /// </summary>
            public void UpdateTickrate(int currentTickrate, DateTime timestamp)
            {
                OutputTickRate = currentTickrate;
                TicksHistory.Add(currentTickrate);
                TickTimestamps.Add(timestamp);
                
                // Детекция спайков tickrate
                CheckForTickrateSpike(currentTickrate);
                
                // Calculate average tickrate
                if (AvgTickrate == 0)
                {
                    AvgTickrate = currentTickrate;
                }
                else
                {
                    AvgTickrate = (AvgTickrate + currentTickrate) / 2;
                }
                
                // Update individual loss calculations
                if (AvgStableTickrate == 0)
                {
                    AvgStableTickrate = currentTickrate;
                }
                
                float ratio = AvgStableTickrate > 0 ? ((float)AvgStableTickrate / (float)AvgTickrate) : 1.0f;
                if (ratio < 1.5 && ratio > 0.5)
                {
                    AvgStableTickrate += (AvgTickrate + AvgStableTickrate);
                    AvgStableTickrate /= 3;
                }
                
                if (TotalTicksCount > 300)
                {
                    int dropped = AvgStableTickrate - currentTickrate;
                    if (dropped < 0) { dropped = 0; }
                    LostTicks += dropped;
                    if (LostTicks < 0) LostTicks = 0;
                }
                
                TotalTicksCount++;
                
                // Update individual graph data
                if (TickrateGraph.Count > 511)
                {
                    TickrateGraph.RemoveAt(0);
                }
                TickrateGraph.Add(currentTickrate);
                
                // Debug: добавляем логирование для отладки графика тикрейта
                DebugLogger.log($"[TickrateGraph] Added to graph: {currentTickrate} Hz (graph size: {TickrateGraph.Count})");
                
                // Update individual log
                TickRateLog += timestamp.ToString() + ";" + currentTickrate.ToString() + Environment.NewLine;
                
                // Downsample history if needed
                DownsampleTickHistoryIfNeeded();
                
                // Clean old data periodically (every 100 updates)
                if (TotalTicksCount % 100 == 0)
                {
                    CleanOldData();
                }
            }
            
            /// <summary>
            /// Updates individual traffic data for this specific server
            /// </summary>
            public void UpdateTraffic(int uploadBytes, int downloadBytes)
            {
                UploadTraffic += uploadBytes;
                DownloadTraffic += downloadBytes;
            }
            
            /// <summary>
            /// Gets individual drops percentage for this server
            /// </summary>
            public float GetDropsPercentage()
            {
                int totalSamples = TotalTicksCount + LostTicks;
                if (totalSamples <= 0)
                    return 0f;
                    
                float percent = ((float)LostTicks / (float)totalSamples) * 100f;
                percent = Math.Max(0f, Math.Min(100f, percent));
                return percent;
            }
            
            /// <summary>
            /// Gets individual session duration for this server
            /// </summary>
            public TimeSpan GetSessionDuration()
            {
                return DateTime.Now.Subtract(SessionStart);
            }
            
            /// <summary>
            /// Cleans old data based on age and size limits
            /// </summary>
            public void CleanOldData()
            {
                CleanOldTickHistory();
                CleanOldGraphData();
            }
            
            /// <summary>
            /// Removes tick history older than MaxHistoryAgeHours
            /// </summary>
            private void CleanOldTickHistory()
            {
                if (TickTimestamps == null || TicksHistory == null || TickTimestamps.Count == 0)
                    return;
                    
                DateTime cutoffTime = DateTime.Now.AddHours(-MaxHistoryAgeHours);
                int removeCount = 0;
                
                for (int i = 0; i < TickTimestamps.Count; i++)
                {
                    if (TickTimestamps[i] >= cutoffTime)
                        break;
                    removeCount++;
                }
                
                if (removeCount > 0)
                {
                    TickTimestamps.RemoveRange(0, removeCount);
                    TicksHistory.RemoveRange(0, Math.Min(removeCount, TicksHistory.Count));
                }
            }
            
            /// <summary>
            /// Keeps graph data within reasonable limits
            /// </summary>
            private void CleanOldGraphData()
            {
                if (TickrateGraph == null)
                    return;
                    
                while (TickrateGraph.Count > MaxGraphDataPoints)
                {
                    TickrateGraph.RemoveAt(0);
                }
            }
            
            /// <summary>
            /// Gets server statistics summary
            /// </summary>
            public string GetServerStatsSummary()
            {
                var duration = GetSessionDuration();
                var drops = GetDropsPercentage();
                
                return $"IP: {Ip}\n" +
                       $"Session: {duration:hh\\:mm\\:ss}\n" +
                       $"Avg Tickrate: {AvgTickrate}\n" +
                       $"Packet Loss: {drops:F2}%\n" +
                       $"Traffic: ↑{UploadTraffic / (1024 * 1024):F2} ↓{DownloadTraffic / (1024 * 1024):F2} MB\n" +
                       $"Data Points: {TicksHistory.Count}";
            }

            private void DownsampleTickHistoryIfNeeded()
            {
                if (TicksHistory == null || TickTimestamps == null)
                {
                    return;
                }

                int totalCount = TicksHistory.Count;
                if (totalCount <= TickHistoryDownsampleThreshold)
                {
                    return;
                }

                int keepRecent = Math.Min(TickHistoryRecentKeep, totalCount);
                int compressCount = totalCount - keepRecent;
                if (compressCount < 4)
                {
                    return;
                }

                int targetCapacity = keepRecent + (compressCount + 1) / 2;
                var downsampledTicks = new List<int>(targetCapacity);
                var downsampledTimestamps = new List<DateTime>(targetCapacity);

                int index = 0;
                for (; index + 1 < compressCount; index += 2)
                {
                    int tickA = TicksHistory[index];
                    int tickB = TicksHistory[index + 1];
                    int averagedTick = (int)Math.Round((tickA + tickB) / 2.0);

                    DateTime timeA = TickTimestamps[index];
                    DateTime timeB = TickTimestamps[index + 1];
                    long averagedTicks = timeA.Ticks + ((timeB.Ticks - timeA.Ticks) / 2);
                    var averagedTime = new DateTime(averagedTicks, timeA.Kind);

                    downsampledTicks.Add(averagedTick);
                    downsampledTimestamps.Add(averagedTime);
                }

                if (index < compressCount)
                {
                    downsampledTicks.Add(TicksHistory[index]);
                    downsampledTimestamps.Add(TickTimestamps[index]);
                    index++;
                }

                for (; index < totalCount; index++)
                {
                    downsampledTicks.Add(TicksHistory[index]);
                    downsampledTimestamps.Add(TickTimestamps[index]);
                }

                TicksHistory = downsampledTicks;
                TickTimestamps = downsampledTimestamps;
            }

            private void DetectLocation()
            {
                if (string.IsNullOrEmpty(Ip)) { Location = "N/A"; return; }
                
                _ = Task.Run(async () =>
                {
                    try
                    {
                        string ipToDetect = Ip;
                        
                        // Если включен STUN, попробуем определить внешний IP
                        if (StunManager.IsEnabled())
                        {
                            try
                            {
                                var externalIp = await StunManager.GetExternalIpStringAsync();
                                if (!string.IsNullOrEmpty(externalIp))
                                {
                                    ExternalIp = externalIp;
                                    Debug.WriteLine($"STUN detected external IP: {externalIp}, current server IP: {ipToDetect}");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"STUN detection failed: {ex.Message}");
                            }
                        }
                        
                        // Используем новый robust геолокационный сервис с fallback'ами
                        try
                        {
                            // Проверяем статистику геолокации - новая система автоматически обрабатывает rate limiting
                            string cacheInfo = Classes.GeolocationService.GetCacheInfo();
                            DebugLogger.log($"[DetectLocation] Starting geolocation for IP: {ipToDetect}. Cache: {cacheInfo}");
                            
                            var locationInfo = await Classes.GeolocationService.GetLocationAsync(ipToDetect);
                            
                            if (this.CurrentIP == ipToDetect)
                            {
                                Location = locationInfo?.FormattedLocation ?? "N/A";
                                
                                DebugLogger.log($"[DetectLocation] Location detected: {Location} (Source: {locationInfo?.Source})");
                                
                                // Дополнительная информация для дебага
                                if (locationInfo != null && !string.IsNullOrEmpty(locationInfo.Isp))
                                {
                                    DebugLogger.log($"[DetectLocation] ISP: {locationInfo.Isp}");
                                }
                                
                                // Логируем статус провайдеров при проблемах
                                if (locationInfo?.Country == "Error" || locationInfo?.Country == "Service Disabled" || locationInfo?.Country == "All Providers Failed")
                                {
                                    string providerStatus = Classes.GeolocationService.GetProviderStatus();
                                    DebugLogger.log($"[DetectLocation] Provider status:\n{providerStatus}");
                                }
                            }
                        }
                        catch (WebException webEx) when (webEx.Response is HttpWebResponse response && (int)response.StatusCode == 429)
                        {
                            DebugLogger.log($"[DetectLocation] Geolocation rate limited (429) for IP {ipToDetect}. Keeping previous location: {Location}");
                            // Не обновляем Location если получили rate limit - оставляем предыдущее значение
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.log($"[DetectLocation] Geolocation failed for IP {ipToDetect}: {ex.Message}");
                            
                            if (this.CurrentIP == ipToDetect)
                            {
                                Location = "Error";
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.log($"[DetectLocation] Task exception: {ex.Message}");
                    }
                });
            }

            public static IPEndPoint CreateIPEndPoint(string endPoint, int port)
            {
                if (port <= 0 || port > 65535)
                {
                    DebugLogger.log($"CreateIPEndPoint: Invalid port {port} for IP {endPoint}");
                    return null;
                }
                IPAddress ip;
                if (!IPAddress.TryParse(endPoint, out ip))
                {
                    DebugLogger.log($"CreateIPEndPoint: Invalid IP address {endPoint}");
                    return null;
                }
                return new IPEndPoint(ip, port);
            }

            // ICMP ping field and property
            private int _icmpPing = 0;
            public int IcmpPing
            {
                get { return _icmpPing; }
                set 
                { 
                    _icmpPing = value;
                    // Проверяем на спайк ICMP пинга
                    if (value > 0)
                    {
                        CheckForPingSpike(value);
                    }
                }
            }

            private int ICMPfails = 0;

            private int PingICMP()
            {
                if (string.IsNullOrEmpty(Ip)) return PingLimitMilliseconds;
                System.Net.NetworkInformation.Ping pingSender = null;
                try
                {
                    pingSender = new System.Net.NetworkInformation.Ping();
                    System.Net.NetworkInformation.PingReply pingReply = pingSender.Send(Ip, PingLimitMilliseconds);
                    if (pingReply.Status == System.Net.NetworkInformation.IPStatus.Success)
                    {
                        IcmpPing = (int)pingReply.RoundtripTime;
                        ICMPfails = 0;
                        return (int)pingReply.RoundtripTime;
                    }
                    else
                    {
                        ICMPfails++;
                        DebugLogger.log($"PingICMP to {Ip} failed with status: {pingReply.Status}");
                    }
                }
                catch (Exception ex)
                {
                    ICMPfails++;
                    DebugLogger.log($"PingICMP to {Ip} exception: {ex.Message}");
                    IcmpPing = PingLimitMilliseconds;
                    return PingLimitMilliseconds;
                }
                finally
                {
                    pingSender?.Dispose();
                }
                IcmpPing = PingLimitMilliseconds;
                return PingLimitMilliseconds;
            }

            private int PingSocket(int portToPing)
            {
                if (string.IsNullOrEmpty(Ip) || portToPing <= 0)
                {
                    return PingLimitMilliseconds + 1;
                }

                IPEndPoint ep = CreateIPEndPoint(Ip, portToPing);
                if (ep == null) return PingLimitMilliseconds + 1;

                using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    Stopwatch stopwatch = new Stopwatch();
                    try
                    {
                        stopwatch.Start();
                        IAsyncResult result = sock.BeginConnect(ep, null, null);
                        bool success = result.AsyncWaitHandle.WaitOne(PingLimitMilliseconds, true);

                        if (success && sock.Connected)
                        {
                            sock.EndConnect(result);
                            stopwatch.Stop();
                            return (int)stopwatch.ElapsedMilliseconds;
                        }
                        else
                        {
                            sock.Close();
                            stopwatch.Stop();
                            DebugLogger.log($"PingSocket to {Ip}:{portToPing} timed out or failed to connect within {PingLimitMilliseconds}ms.");
                            return PingLimitMilliseconds;
                        }
                    }
                    catch (SocketException se)
                    {
                        DebugLogger.log($"PingSocket to {Ip}:{portToPing} SocketException: {se.Message} (Code: {se.SocketErrorCode})");
                        return PingLimitMilliseconds;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.log($"PingSocket to {Ip}:{portToPing} Exception: {ex.Message}");
                        return PingLimitMilliseconds;
                    }
                }
            }

            private void PingServer()
            {
                if (string.IsNullOrEmpty(Ip))
                {
                    isPinging = false;
                    return;
                }

                int icmpPingTime = PingICMP();
                Debug.Print($"ICMP Ping to {Ip}: {icmpPingTime}ms");

                int finalPingTime;

                if (icmpPingTime < PingLimitMilliseconds)
                {
                    finalPingTime = icmpPingTime;
                    consecutivePingFails = 0;
                }
                else
                {
                    int portToTryTcp = this.PingPort;
                    if (portToTryTcp <= 0 && this.GamePort > 0)
                    {
                        portToTryTcp = this.GamePort;
                    }

                    if (portToTryTcp > 0)
                    {
                        finalPingTime = PingSocket(portToTryTcp);
                        Debug.Print($"TCP Ping to {Ip}:{portToTryTcp} (primary/dynamic): {finalPingTime}ms");

                        if (finalPingTime >= PingLimitMilliseconds)
                        {
                            consecutivePingFails++;
                            if (UserDefinedFallbackPorts != null && UserDefinedFallbackPorts.Any() && consecutivePingFails > 2)
                            {
                                currentFallbackPortIndex++;
                                if (currentFallbackPortIndex >= UserDefinedFallbackPorts.Count)
                                {
                                    currentFallbackPortIndex = 0;
                                }
                                int fallbackPort = UserDefinedFallbackPorts[currentFallbackPortIndex];
                                this.PingPort = fallbackPort;
                                finalPingTime = PingSocket(fallbackPort);
                                Debug.Print($"TCP Ping to {Ip}:{fallbackPort} (fallback): {finalPingTime}ms");
                            }
                        }
                        else
                        {
                            consecutivePingFails = 0;
                        }
                    }
                    else
                    {
                        consecutivePingFails++;
                        if (UserDefinedFallbackPorts != null && UserDefinedFallbackPorts.Any() && consecutivePingFails > 2)
                        {
                            currentFallbackPortIndex++;
                            if (currentFallbackPortIndex >= UserDefinedFallbackPorts.Count)
                            {
                                currentFallbackPortIndex = 0;
                            }
                            int fallbackPort = UserDefinedFallbackPorts[currentFallbackPortIndex];
                            this.PingPort = fallbackPort;
                            finalPingTime = PingSocket(fallbackPort);
                            Debug.Print($"TCP Ping to {Ip}:{fallbackPort} (fallback, no dynamic port): {finalPingTime}ms");
                        }
                        else
                        {
                            finalPingTime = PingLimitMilliseconds;
                        }
                    }

                    if (finalPingTime < PingLimitMilliseconds) consecutivePingFails = 0;

                }

                if (consecutivePingFails >= SeverePingFailureThreshold)
                {
                    DateTime nowUtc = DateTime.UtcNow;
                    if (nowUtc - lastSeverePingFailureUtc > SeverePingFailureCooldown)
                    {
                        lastSeverePingFailureUtc = nowUtc;
                        string guardMessage = $"[PingGuard] {consecutivePingFails} consecutive ping failures for {Ip} (ICMPfails={ICMPfails}, lastTcp={finalPingTime}ms, lastIcmp={IcmpPing}ms)";
                        Debug.Print(guardMessage);
                        DebugLogger.log(guardMessage);

                        if (App.meterState?.IsTracking == true && App.gui != null)
                        {
                            App.gui.HandleSeverePingLoss(Ip, consecutivePingFails, finalPingTime, IcmpPing);
                        }

                        consecutivePingFails = 0;
                    }
                }

                Ping = finalPingTime;
            }
        }
    } // конец класса TickMeterState
} // конец пространства имен tickMeter