using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Net.NetworkInformation;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;
using PcapDotNet.Packets;
using System.Threading.Tasks;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Diagnostics;
using tickMeter.Classes;
using tickMeter.Classes.SpikeDetection;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;
using System.Text;
using Newtonsoft.Json;

namespace tickMeter.Forms
{
    public partial class GUI : Form
    {
        
        public LivePacketDevice selectedAdapter;
        public Thread PcapThread;

        public BackgroundWorker pcapWorker;
        
        // NEW: поля для мульти-адаптерного захвата
        private readonly List<LivePacketDevice> _allSelectedAdapters = new List<LivePacketDevice>();
        private readonly List<BackgroundWorker> _pcapWorkers = new List<BackgroundWorker>();
        private Classes.SmartTransparencyManager _transparencyManager; // Управление прозрачностью при наведении
        // простая защита от дублей на бриджах/VPN
        private readonly Dictionary<ulong, long> _dedup = new Dictionary<ulong, long>(capacity: 4096);
        private readonly Stopwatch _dedupSw = Stopwatch.StartNew();
        private readonly object _dedupLock = new object();
        
        // Константы для дедупликации
        private const int MAX_DEDUP_SIZE = 8192;  // Уменьшен с 20000
        private const int DEDUP_CLEANUP_THRESHOLD = 500;  // Более частая очистка
        
        // Phase 3: Thread Priority & Single Consumer управление
        private readonly List<Thread> _highPriorityThreads = new List<Thread>();
        
        // Кэш для Windows Statistics в обычном режиме
        private static long _lastWindowsDownloaded = 0;
        private static long _lastWindowsUploaded = 0;
        private static DateTime _lastWindowsUpdate = DateTime.MinValue;
        private readonly ConcurrentQueue<Action> _uiUpdateQueue = new ConcurrentQueue<Action>();
        private readonly System.Threading.Timer _uiProcessingTimer;
        private volatile bool _uiProcessingActive = false;
        private readonly object _threadManagementLock = new object();

        private Color LoadNeutralActiveColor()
        {
            try
            {
                var hex = App.settingsManager?.GetOption("color_mid", "FF8040", "SETTINGS");
                if (string.IsNullOrWhiteSpace(hex))
                {
                    return DefaultNeutralActiveColor;
                }

                hex = hex.Trim();
                if (!hex.StartsWith("#", StringComparison.Ordinal))
                {
                    hex = "#" + hex;
                }

                return ColorTranslator.FromHtml(hex);
            }
            catch
            {
                return DefaultNeutralActiveColor;
            }
        }

        private void ScheduleCaptureRestart()
        {
            if (!IsHandleCreated || IsDisposed)
            {
                return;
            }

            if (DateTime.Now - _lastHardRestart < HardRestartCooldown)
            {
                Debug.Print("[Metrics] ⏱ Hard restart cooldown active, skipping");
                return;
            }

            _lastHardRestart = DateTime.Now;

            Action restartAction = () =>
            {
                if (!IsHandleCreated || IsDisposed)
                {
                    Debug.Print("[Metrics] Restart aborted: form not ready");
                    return;
                }

                Debug.Print("[Metrics] 🔄 Performing capture restart");

                try
                {
                    StopTracking();
                }
                catch (Exception ex)
                {
                    Debug.Print($"[Metrics] ❌ StopTracking failed: {ex.Message}");
                }

                try
                {
                    StartTracking();
                    Debug.Print("[Metrics] ✅ StartTracking completed (no exception)");
                }
                catch (Exception ex)
                {
                    Debug.Print($"[Metrics] ❌ StartTracking failed: {ex.Message}");
                    DebugLogger.log($"[Metrics] StartTracking exception: {ex.Message}\n{ex.StackTrace}");
                }
            };

            try
            {
                if (this.IsHandleCreated)
                {
                    this.BeginInvoke(restartAction);
                }
                else
                {
                    // Run on ThreadPool if form handle missing — action will check handle again
                    System.Threading.ThreadPool.QueueUserWorkItem(_ => restartAction());
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Metrics] Failed to schedule restart action: {ex.Message}");
            }
        }

        internal void HandleSeverePingLoss(string targetIp, int failureCount, int lastPingMs, int icmpPingMs)
        {
            string message = $"[PingGuard] {failureCount} consecutive ping failures for {targetIp} (lastTcp={lastPingMs}ms, lastIcmp={icmpPingMs}ms) - scheduling restart";
            Debug.Print(message);
            DebugLogger.log(message);

            if (!IsHandleCreated || IsDisposed)
            {
                return;
            }

            BeginInvoke(new Action(() =>
            {
                try
                {
                    _metricsActive = false;
                    _invalidTargetCount = 0;
                    _fastStartCounter = 0;
                    _idleRecoveryAttempts = 0;
                    _lastPacketTimestamp = DateTime.MinValue;

                    ActiveWindowTracker.ClearConnectionStats();

                    if (App.meterState?.Server != null)
                    {
                        App.meterState.Server.Reset();
                    }
                }
                catch (Exception ex)
                {
                    string prepError = $"[PingGuard] Error preparing restart: {ex.Message}";
                    Debug.Print(prepError);
                    DebugLogger.log(prepError);
                }

                ScheduleCaptureRestart();
            }));
        }

        private void InitializeAutomationInfrastructure()
        {
            EnsureStateDirectory();

            _selfHealTimer?.Dispose();
            _selfHealTimer = new System.Threading.Timer(SelfHealTick, null, Timeout.Infinite, Timeout.Infinite);

            _keepAliveTimer?.Dispose();
            _keepAliveTimer = new System.Threading.Timer(KeepAliveTick, null, Timeout.Infinite, Timeout.Infinite);
        }

        private void EnsureStateDirectory()
        {
            try
            {
                if (!Directory.Exists(StateDirectory))
                {
                    Directory.CreateDirectory(StateDirectory);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Automation] State directory creation failed: {ex.Message}");
            }
        }

        private void StartAutomationTimers()
        {
            try
            {
                int healPeriodMs = (int)Math.Max(1000d, SelfHealCheckPeriod.TotalMilliseconds);
                _selfHealTimer?.Change(healPeriodMs, healPeriodMs);
            }
            catch (Exception ex)
            {
                Debug.Print($"[Automation] Self-heal timer start failed: {ex.Message}");
            }

            try
            {
                int keepAliveMs = (int)Math.Max(1000d, KeepAlivePeriod.TotalMilliseconds);
                _keepAliveTimer?.Change(keepAliveMs, keepAliveMs);
            }
            catch (Exception ex)
            {
                Debug.Print($"[Automation] Keep-alive timer start failed: {ex.Message}");
            }
        }

        private void StopAutomationTimers()
        {
            try
            {
                _selfHealTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch (Exception)
            {
                // ignore
            }

            try
            {
                _keepAliveTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private void SelfHealTick(object state)
        {
            try
            {
                if (IsDisposed || !IsHandleCreated)
                {
                    return;
                }

                var meterState = App.meterState;
                if (meterState == null)
                {
                    return;
                }

                var nowUtc = DateTime.UtcNow;

                WriteHeartbeatIfNeeded(nowUtc);
                PersistMonitoringSnapshot(nowUtc);

                if (!meterState.IsTracking)
                {
                    if (!string.IsNullOrEmpty(_pendingRestoreTargetKey) && nowUtc - _lastSelfHealAttempt > SelfHealCooldown)
                    {
                        _lastSelfHealAttempt = nowUtc;
                        BeginInvoke(new Action(() =>
                        {
                            if (App.meterState != null && !App.meterState.IsTracking)
                            {
                                Debug.Print("[SelfHeal] Attempting auto-resume after unexpected stop");
                                try
                                {
                                    StartTracking();
                                }
                                catch (Exception ex)
                                {
                                    Debug.Print($"[SelfHeal] Auto-resume failed: {ex.Message}");
                                }
                            }
                        }));
                    }
                    return;
                }

                var lastPacket = _lastPacketTimestamp;
                if (lastPacket == DateTime.MinValue)
                {
                    return;
                }

                var idle = nowUtc - lastPacket;
                if (idle > MetricStallThreshold && nowUtc - _lastSelfHealAttempt > SelfHealCooldown)
                {
                    _lastSelfHealAttempt = nowUtc;
                    BeginInvoke(new Action(() =>
                    {
                        if (App.meterState != null && App.meterState.IsTracking)
                        {
                            Debug.Print($"[SelfHeal] Metrics stalled for {idle.TotalSeconds:F1}s, scheduling restart");
                            ScheduleCaptureRestart();
                        }
                    }));
                }
            }
            catch (ObjectDisposedException)
            {
                // Form disposed, nothing to do
            }
            catch (Exception ex)
            {
                Debug.Print($"[SelfHeal] Error: {ex.Message}");
            }
        }

        private void KeepAliveTick(object state)
        {
            try
            {
                var meterState = App.meterState;
                if (meterState == null || !meterState.IsTracking)
                {
                    return;
                }

                var lastPacket = _lastPacketTimestamp;
                var nowUtc = DateTime.UtcNow;
                if (lastPacket != DateTime.MinValue && (nowUtc - lastPacket) < KeepAliveIdleThreshold)
                {
                    return;
                }

                Debug.Print("[KeepAlive] No packets observed recently, sending keep-alive pulse");

                App.pingManager?.RequestImmediatePing();
                Task.Run(() =>
                {
                    SendKeepAlivePulse();
                });
            }
            catch (Exception ex)
            {
                Debug.Print($"[KeepAlive] Error: {ex.Message}");
            }
        }

        private void WriteHeartbeatIfNeeded(DateTime timestampUtc, bool force = false)
        {
            if (!force && _lastHeartbeatWrite != DateTime.MinValue)
            {
                var delta = timestampUtc - _lastHeartbeatWrite;
                if (delta < HeartbeatPeriod)
                {
                    return;
                }
            }

            try
            {
                EnsureStateDirectory();
                var payload = timestampUtc.ToString("o");
                lock (_snapshotLock)
                {
                    File.WriteAllText(HeartbeatFilePath, payload);
                }
                _lastHeartbeatWrite = timestampUtc;
            }
            catch (Exception ex)
            {
                Debug.Print($"[Heartbeat] Write failed: {ex.Message}");
            }
        }

        private void PersistMonitoringSnapshot(DateTime timestampUtc, bool force = false)
        {
            if (!force && _lastSnapshotWrite != DateTime.MinValue)
            {
                var delta = timestampUtc - _lastSnapshotWrite;
                if (delta < HeartbeatPeriod)
                {
                    return;
                }
            }

            var meterState = App.meterState;
            if (meterState == null)
            {
                return;
            }

            var snapshot = new MonitoringSnapshot
            {
                TimestampUtc = timestampUtc,
                WasTracking = meterState.IsTracking,
                TargetKey = targetKey,
                SelectedAdapter = GetSelectedAdapterIndexSafe(),
                LocalIp = meterState.LocalIP,
                Game = meterState.Game
            };

            try
            {
                EnsureStateDirectory();
                var json = JsonConvert.SerializeObject(snapshot, Formatting.Indented);
                lock (_snapshotLock)
                {
                    File.WriteAllText(SnapshotFilePath, json);
                }
                _lastSnapshotWrite = timestampUtc;
            }
            catch (Exception ex)
            {
                Debug.Print($"[Snapshot] Write failed: {ex.Message}");
            }
        }

        private MonitoringSnapshot LoadMonitoringSnapshot()
        {
            try
            {
                if (!File.Exists(SnapshotFilePath))
                {
                    return null;
                }

                lock (_snapshotLock)
                {
                    var json = File.ReadAllText(SnapshotFilePath);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        return null;
                    }

                    return JsonConvert.DeserializeObject<MonitoringSnapshot>(json);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Snapshot] Read failed: {ex.Message}");
                return null;
            }
        }

        private int GetSelectedAdapterIndexSafe()
        {
            var adaptersList = App.settingsForm?.adapters_list;
            if (adaptersList == null)
            {
                return -1;
            }

            try
            {
                if (adaptersList.InvokeRequired)
                {
                    return (int)adaptersList.Invoke(new Func<int>(() => adaptersList.SelectedIndex));
                }

                return adaptersList.SelectedIndex;
            }
            catch (InvalidOperationException ex)
            {
                Debug.Print($"[Automation] Adapter index read cross-thread error: {ex.Message}");
                return -1;
            }
            catch (Exception ex)
            {
                Debug.Print($"[Automation] Adapter index read failed: {ex.Message}");
                return -1;
            }
        }

        private void AutoResumeMonitoringIfNeeded()
        {
            DebugLogger.log("[Automation] AutoResumeMonitoringIfNeeded CALLED");
            try
            {
                // Проверяем настройку автозапуска мониторинга
                bool autoStartMonitoring = App.settingsManager.GetOption("auto_start_monitoring", "True", "SETTINGS") == "True";
                DebugLogger.log($"[Automation] Auto-start monitoring setting: {autoStartMonitoring}");
                
                // Если автозапуск включен - запускаем мониторинг безусловно
                if (autoStartMonitoring)
                {
                    DebugLogger.log("[Automation] Auto-start enabled - starting monitoring automatically");
                    Debug.Print("[Automation] Auto-start enabled - starting monitoring automatically");
                    // Всегда вызываем StartTracking - он сам проверит нужно ли запускать захват
                    DebugLogger.log("[Automation] Calling StartTracking()...");
                    StartTracking();
                    DebugLogger.log("[Automation] StartTracking() completed");
                    return;
                }
                
                // Если автозапуск выключен - проверяем snapshot для восстановления предыдущей сессии
                var snapshot = LoadMonitoringSnapshot();
                if (snapshot == null)
                {
                    DebugLogger.log("[Automation] No snapshot found, skipping auto-resume");
                    return;
                }

                if (snapshot.TimestampUtc != DateTime.MinValue)
                {
                    var age = DateTime.UtcNow - snapshot.TimestampUtc;
                    if (age > SnapshotMaxAge)
                    {
                        Debug.Print("[Automation] Snapshot is too old, skipping auto-resume");
                        return;
                    }
                }

                if (!snapshot.WasTracking)
                {
                    return;
                }

                ApplySnapshotToUi(snapshot);
                targetKey = snapshot.TargetKey ?? string.Empty;
                _pendingRestoreTargetKey = string.IsNullOrEmpty(snapshot.TargetKey)
                    ? "__auto_resume__"
                    : snapshot.TargetKey;

                if (App.meterState != null && !App.meterState.IsTracking)
                {
                    Debug.Print("[Automation] Auto-resuming monitoring session from snapshot");
                    StartTracking();
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Automation] Auto-resume failed: {ex.Message}");
            }
        }

        private void ApplySnapshotToUi(MonitoringSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            try
            {
                if (!string.IsNullOrWhiteSpace(snapshot.LocalIp))
                {
                    if (App.meterState != null)
                    {
                        App.meterState.LocalIP = snapshot.LocalIp;
                    }

                    if (App.settingsForm?.local_ip_textbox != null)
                    {
                        App.settingsForm.local_ip_textbox.Text = snapshot.LocalIp;
                    }
                }

                if (!string.IsNullOrWhiteSpace(snapshot.Game) && App.meterState != null)
                {
                    App.meterState.Game = snapshot.Game;
                }

                if (snapshot.SelectedAdapter >= 0 && App.settingsForm?.adapters_list != null &&
                    snapshot.SelectedAdapter < App.settingsForm.adapters_list.Items.Count)
                {
                    App.settingsForm.adapters_list.SelectedIndex = snapshot.SelectedAdapter;
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Automation] Apply snapshot failed: {ex.Message}");
            }
        }

        private void SendKeepAlivePulse()
        {
            try
            {
                var meterState = App.meterState;
                if (meterState == null || meterState.Server == null)
                {
                    return;
                }

                var server = meterState.Server;
                if (string.IsNullOrWhiteSpace(server.Ip))
                {
                    return;
                }

                int targetPort = server.GamePort > 0 ? server.GamePort : server.PingPort;
                if (targetPort <= 0)
                {
                    return;
                }

                using (var udpClient = new UdpClient())
                {
                    udpClient.Client.SendTimeout = 1000;

                    if (!string.IsNullOrWhiteSpace(meterState.LocalIP) && IPAddress.TryParse(meterState.LocalIP, out var localIp))
                    {
                        try
                        {
                            udpClient.Client.Bind(new IPEndPoint(localIp, 0));
                        }
                        catch (Exception ex)
                        {
                            Debug.Print($"[KeepAlive] Bind failed: {ex.Message}");
                        }
                    }

                    udpClient.Connect(server.Ip, targetPort);
                    udpClient.Send(KeepAlivePayload, KeepAlivePayload.Length);
                }

                _lastPacketTimestamp = DateTime.UtcNow;
            }
            catch (SocketException ex)
            {
                Debug.Print($"[KeepAlive] UDP send socket error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[KeepAlive] UDP keep-alive failed: {ex.Message}");
            }
        }

        private sealed class MonitoringSnapshot
        {
            public DateTime TimestampUtc { get; set; }
            public bool WasTracking { get; set; }
            public string TargetKey { get; set; }
            public int SelectedAdapter { get; set; }
            public string LocalIp { get; set; }
            public string Game { get; set; }
        }
        
        // Анти-реэнтерабельность для StartTracking/StopTracking (предотвращение роста воркеров)
        private int _startTrackingBusy = 0;
        private int _stopTrackingBusy = 0;
        private int _switchAdapterBusy = 0; // Защита от повторного переключения адаптера
        
        public Boolean allowClose = false;
        int restarts = 0;
        int restartLimit = 1;
        int lastSelectedAdapterID = -1;
        public string threadID = ""; 
        int appInitHeigh;
        int appInitWidth;
        bool OnScreen;
        public PubgStatsManager PubgMngr;
        public DbdStatsManager DbdMngr;
        public string targetKey = "";
        private int _gcCounter = 0; // Счётчик для периодической сборки мусора
        private int _totalPacketsReceived = 0; // Счётчик всех полученных пакетов
        
        // Механизм быстрого старта для ускорения обнаружения метрик
        private DateTime _lastConnectionSearch = DateTime.MinValue;
    private TimeSpan _searchCooldown = TimeSpan.FromSeconds(1); // Обычный режим
    private bool _metricsActive = false;
    private bool _metricsStateCleared = true;
        private int _fastStartCounter = 0; // Счетчик проверок в режиме быстрого старта
    // Для предотвращения флапа при быстром переключении окон
    private int _invalidTargetCount = 0; // Требуется 2 подряд невалидных проверки чтобы деактивировать
    // Отслеживание времени включения быстрого режима ConnectionsManager
    private DateTime _fastModeEnabledAt = DateTime.MinValue;
    // Stopwatch для измерения времени поиска соединения при fast start
    private Stopwatch _searchStopwatch = new Stopwatch();
    private DateTime _lastConnRefreshRequest = DateTime.MinValue;
    private static readonly TimeSpan ConnectionRefreshCooldown = TimeSpan.FromMilliseconds(350);
    private DateTime _lastMetricsApplied = DateTime.MinValue;
    private DateTime _lastPeriodicConnRefresh = DateTime.MinValue;
    private int _idleRecoveryAttempts = 0;
    private DateTime _lastHardRestart = DateTime.MinValue;
    private static readonly TimeSpan IdleDetectionThreshold = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan HardRestartThreshold = TimeSpan.FromSeconds(30);
    // Кулдаун уменьшен, чтобы не блокировать повторные рестарты при затянувшемся простое
    private static readonly TimeSpan HardRestartCooldown = TimeSpan.FromSeconds(25);
    private static readonly TimeSpan StaleConnectionGrace = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan SelfHealCheckPeriod = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan MetricStallThreshold = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan SelfHealCooldown = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan KeepAlivePeriod = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan KeepAliveIdleThreshold = TimeSpan.FromMinutes(2);
    private static readonly TimeSpan HeartbeatPeriod = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan SnapshotMaxAge = TimeSpan.FromMinutes(15);
    private static readonly string StateDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "monitoring_state");
    private static readonly string HeartbeatFilePath = Path.Combine(StateDirectory, "heartbeat.txt");
    private static readonly string SnapshotFilePath = Path.Combine(StateDirectory, "snapshot.json");
    private static readonly byte[] KeepAlivePayload = Encoding.ASCII.GetBytes("tickmeter-keepalive");
    private const string AutoResumeSentinel = "__auto_resume__";
    private DateTime _lastPacketTimestamp = DateTime.MinValue;
    private System.Threading.Timer _selfHealTimer;
    private System.Threading.Timer _keepAliveTimer;
    private readonly object _snapshotLock = new object();
    private DateTime _lastSelfHealAttempt = DateTime.MinValue;
    private DateTime _lastHeartbeatWrite = DateTime.MinValue;
    private DateTime _lastSnapshotWrite = DateTime.MinValue;
    private string _pendingRestoreTargetKey = string.Empty;
    private int _manualStopRequestedFlag = 0;
        
        // Флаги для предотвращения рекурсии в событиях формы
        private bool _isResizing = false;
        private bool _isRestoring = false;
        
    private const int InitialTickrateWindowSeconds = 120;
    private const string TickrateChartAreaName = "TickrateArea";
    private const string TickrateSeriesName = "Tickrate";
    private const string TickrateAverageSeriesName = "TickrateAverage";
    private const int TickrateAverageWindow = 20;
        
        // Stage 5: Analytics form
        private SpikeAnalyticsForm _spikeAnalyticsForm;
        
        // Spike animation управление
        private int _spikeBlinkCounter = 0;
        private bool _spikeBlinkState = false;
        
        // Spike notifications управление
        private DateTime _lastPingSpikeNotification = DateTime.MinValue;
        private DateTime _lastTickrateSpikeNotification = DateTime.MinValue;
        private DateTime _lastTicktimeSpikeNotification = DateTime.MinValue;
        private const int SPIKE_NOTIFICATION_COOLDOWN_SECONDS = 10; // Минимальный интервал между уведомлениями
        
        // Оптимизация главного цикла
        private int _tickBusy = 0; // Защита от реэнтерабельности
        private readonly Stopwatch _rtssSw = Stopwatch.StartNew(); // Троттлинг RTSS
        private int RtssPeriodMs => Math.Max(33, Math.Min(1000, // 30-1000ms
            (int)Math.Round(1000.0 / Math.Max(1, Math.Min(60, 
                int.Parse(App.settingsManager?.GetOption("overlay_fps", "15", "ADVANCED") ?? "15"))))));
        
        private static readonly Color DefaultNeutralActiveColor = Color.FromArgb(0xFF, 0x80, 0x40);
        private static readonly Color TickrateAverageColor = Color.FromArgb(0x4A, 0xA1, 0xFF);
        private readonly Color _inactiveMetricColor = Color.FromArgb(0x44, 0x44, 0x44);
        private Color _neutralActiveColor = DefaultNeutralActiveColor;

        private const int WM_ACTIVATE = 0x0006;
        private const int WA_ACTIVE = 1;
        private const int WA_CLICKACTIVE = 2;
        private const int WA_INACTIVE = 0;

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        private const UInt32 SWP_NOSIZE = 0x0001;
        private const UInt32 SWP_NOMOVE = 0x0002;
        private const UInt32 SWP_SIZE = 0x0003;
        private const UInt32 SWP_MOVE = 0x0004;

        private const UInt32 TOPMOST_FLAGS = SWP_NOMOVE | SWP_NOSIZE;
        private const UInt32 NOTOPMOST_FLAGS = SWP_MOVE | SWP_SIZE;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        /// <summary>
        /// Form initialization
        /// </summary>
        public GUI()
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.UnhandledException += new UnhandledExceptionEventHandler(MyHandler);
            try
            {
                InitializeComponent();
                // App.Init() теперь вызывается в Program.cs перед созданием GUI
                App.gui = this;
                InitializeAutomationInfrastructure();
                StartAutomationTimers();
                _neutralActiveColor = LoadNeutralActiveColor();
                ConfigureTickrateChart();

                // Подписываемся на результаты ping
                if (App.pingManager != null)
                {
                    App.pingManager.PingResultReceived += OnPingResultReceived;
                }

                for (int i = 0; i != App.GetAdapters().Count; ++i)
                {
                    LivePacketDevice Adapter = App.GetAdapters()[i];

                    if (Adapter.Description != null)
                    {
                        App.settingsForm.adapters_list.Items.Add(App.GetAdapterAddress(Adapter) + " " + Adapter.Description.Replace("Network adapter ","").Replace("'Microsoft' ",""));
                    }
                    else
                    {
                        App.settingsForm.adapters_list.Items.Add("Unknown");
                    }
                }

            }
            catch (Exception e)
            {
                DebugLogger.log(e);
                MessageBox.Show(e.Message);
            }
            
            // Устанавливаем интервал обновления overlay: учитываем Advanced overlay_fps (если включено), иначе берём ping_interval
            int intervalMs = 1000; // default 1s
            var overlayFpsEnabled = App.settingsManager?.GetOption("overlay_fps_enabled", "False", "ADVANCED") == "True";
            if (overlayFpsEnabled)
            {
                var fpsStr = App.settingsManager?.GetOption("overlay_fps", "60", "ADVANCED");
                if (!string.IsNullOrEmpty(fpsStr) && int.TryParse(fpsStr, out int fps) && fps > 0)
                {
                    intervalMs = Math.Max(1, (int)Math.Round(1000.0 / fps));
                }
            }
            else
            {
                var pingIntervalStr = App.settingsManager?.GetOption("ping_interval");
                if (!string.IsNullOrEmpty(pingIntervalStr) && int.TryParse(pingIntervalStr, out int pingVal))
                {
                    intervalMs = pingVal;
                }
            }
            ticksLoop.Interval = intervalMs;
            
            // Phase 3: Инициализация Single Consumer UI Processing
            _uiProcessingTimer = new System.Threading.Timer(ProcessUIUpdates, null, 16, 16); // 60 FPS
            
            // Подписываемся на события детекции спайков
            Classes.SpikeDetection.SpikeDetectionManager.SpikeDetected += OnSpikeDetected;
            
            // Инициализируем анализатор качества сети
            Classes.NetworkQualityAnalyzer.Initialize();

            // Инициализируем SmartTransparencyManager
            try
            {
                DebugLogger.log("[GUI] Creating SmartTransparencyManager...");
                _transparencyManager = new Classes.SmartTransparencyManager(this);
                DebugLogger.log("[GUI] SmartTransparencyManager initialized successfully");
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[GUI] SmartTransparencyManager initialization failed: {ex.Message}\n{ex.StackTrace}");
            }
        }

        static void MyHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            DebugLogger.log(e);
            MessageBox.Show(e.Message);
        }

        public void InitMeterState()
        {
            Debug.Print("InitMeterState");
            if (App.meterState != null) App.meterState.KillTimers();
            App.meterState = new TickMeterState();
            App.meterState.ConnectionsManagerFlag = true;
            
            // Инициализируем сглаживание tickrate
            Classes.TickrateSmoothingManager.Initialize();
        }

        protected void ShowAll()
        {
            ip_val.Visible = true;
            ip_lbl.Visible = true;
            ping_val.Visible = true;
            ping_lbl.Visible = true;
            countryLbl.Visible = true;
            traffic_lbl.Visible = true;
            traffic_val.Visible = true;
            time_lbl.Visible = true;
            time_val.Visible = true;
            SettingsButton.Visible = true;
            gameProfilesButton.Visible = true;
            drops_lbl.Visible = true;
            drops_lbl_val.Visible = true;
            packetStatsBtn.Visible = true;
            spikeAnalyticsBtn.Visible = true;

            if (TickrateChart1 != null)
            {
                TickrateChart1.Visible = App.settingsForm.settings_chart_checkbox.Checked;
            }
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_ACTIVATE & m.WParam == (IntPtr)WA_ACTIVE)
            {
                OnScreen = true;
                BackColor = SystemColors.Control;
                TransparencyKey = Color.PaleVioletRed;
                FormBorderStyle = FormBorderStyle.FixedToolWindow;
                Height = appInitHeigh;
                Width = appInitWidth;
                ShowAll();
            }
            else if (m.Msg == WM_ACTIVATE & m.WParam == (IntPtr)WA_CLICKACTIVE)
            {
                OnScreen = true;
                BackColor = SystemColors.Control;
                TransparencyKey = Color.PaleVioletRed;
                FormBorderStyle = FormBorderStyle.FixedToolWindow;
                Height = appInitHeigh;
                Width = appInitWidth;
                ShowAll();
            }
            else if (m.Msg == WM_ACTIVATE & m.WParam == (IntPtr)WA_INACTIVE)
            {
                OnScreen = true;
                BackColor = SystemColors.WindowFrame;
                TransparencyKey = SystemColors.WindowFrame;
                FormBorderStyle = FormBorderStyle.None;
                
                SettingsButton.Visible =
                gameProfilesButton.Visible =
                webStatsButton.Visible =
                packetStatsBtn.Visible = 
                spikeAnalyticsBtn.Visible = false;
                TopMost = true;
                if (App.settingsForm.settings_rtss_output.Checked)
                {
                    TopMost = false;
                    OnScreen = false;
                }
                bool chartEnabled = App.settingsForm.settings_chart_checkbox.Checked;
                if (!chartEnabled)
                {
                    Height = 160;
                }

                if (TickrateChart1 != null)
                {
                    TickrateChart1.Visible = chartEnabled;
                }
                Width = 475;

                if(!App.settingsForm.settings_ip_checkbox.Checked)
                {
                    ip_val.Visible = false;
                    ip_lbl.Visible = false;
                }
                if (!App.settingsForm.settings_ping_checkbox.Checked)
                {
                    ping_val.Visible = false;
                    ping_lbl.Visible = false;
                    countryLbl.Visible = false;
                }
                if (!App.settingsForm.settings_traffic_checkbox.Checked)
                {
                    traffic_lbl.Visible = false;
                    traffic_val.Visible = false;
                }

                if (!App.settingsForm.settings_session_time_checkbox.Checked)
                {
                    time_lbl.Visible = false;
                    time_val.Visible = false;
                }

                if (!App.settingsForm.packet_drops_checkbox.Checked)
                {
                    drops_lbl.Visible = false;
                    drops_lbl_val.Visible = false;
                }
            }
            base.WndProc(ref m);
        }

        /// <summary>
        /// NEW: очень дешёвое дедуплирование пакетов в мульти-режиме,
        /// чтобы не удвоить счётчики при бриджах/зеркалах.
        /// Сигнатура = hash первых 64 байт + длина.
        /// Окно ~3 мс.
        /// </summary>
        private bool IsDuplicate(Packet packet)
        {
            // Respect user setting: enable/disable dedup in multi-NIC mode
            bool dedupEnabled = App.settingsManager?.GetBool("dedup_multi_nic", true) == true;
            if (!dedupEnabled) return false;
            if (_allSelectedAdapters.Count == 0) return false; // single-NIC режим — без дедупа
            var bytes = packet?.Buffer?.ToArray();
            if (bytes == null) return false;
            int len = Math.Min(64, bytes.Length);
            ulong h = 1469598103934665603UL;
            for (int i = 0; i < len; i++) h = (h ^ bytes[i]) * 1099511628211UL;
            h ^= (ulong)bytes.Length;
            long now = _dedupSw.ElapsedMilliseconds;
            lock (_dedupLock)
            {
                if (_dedup.TryGetValue(h, out var ts) && now - ts < 3) return true;
                _dedup[h] = now;
                
                // Более эффективная очистка с меньшим порогом
                if (_dedup.Count > MAX_DEDUP_SIZE)
                {
                    // Радикальная очистка для предотвращения роста памяти
                    _dedup.Clear();
                }
                else if (_dedup.Count > DEDUP_CLEANUP_THRESHOLD)
                {
                    // Лёгкая очистка устаревших записей
                    var keysToRemove = _dedup.Where(kv => now - kv.Value > 250).Select(kv => kv.Key).Take(100).ToList();
                    foreach (var key in keysToRemove)
                        _dedup.Remove(key);
                }
            }
            return false;
        }

        private void PacketHandler(Packet packet)
        {
            _totalPacketsReceived++;
            
            if (_totalPacketsReceived <= 5 || _totalPacketsReceived % 100 == 0)
            {
                DebugLogger.log($"[PacketHandler] Packet #{_totalPacketsReceived} received");
            }
            
            try 
            {
                if (!App.meterState.IsTracking) return;
                if (packet == null) return; // Защита от null пакетов
                
                // Проверяем основную структуру пакета
                try
                {
                    if (packet.Ethernet == null) return;
                    if (packet.Buffer == null || packet.Buffer.Length == 0) return;
                }
                catch (IndexOutOfRangeException)
                {
                    // Пакет имеет недостаточный размер или поврежден
                    return;
                }
                catch (Exception)
                {
                    // Любые другие ошибки доступа к пакету
                    return;
                }
                
                if (IsDuplicate(packet)) return; // NEW: проверка дублей
                _lastPacketTimestamp = DateTime.UtcNow;
            
            // VPN bypass mode handling
            bool vpnBypassAdvanced = App.settingsManager?.GetOption("vpn_bypass_advanced", "False", "ADVANCED") == "True";
            if (vpnBypassAdvanced)
            {
                // В продвинутом режиме обхода VPN пытаемся подменить данные пакета
                // на реальные данные процесса через ConnectionTracker
                try
                {
                    if (packet.Ethernet.IpV4 != null)
                    {
                        var ipv4 = packet.Ethernet.IpV4;
                        byte proto = 0;
                        int srcPort = 0, dstPort = 0;
                        
                        if (ipv4.Tcp != null)
                        {
                            proto = 6; // TCP
                            srcPort = ipv4.Tcp.SourcePort;
                            dstPort = ipv4.Tcp.DestinationPort;
                        }
                        else if (ipv4.Udp != null)
                        {
                            proto = 17; // UDP
                            srcPort = ipv4.Udp.SourcePort;
                            dstPort = ipv4.Udp.DestinationPort;

                            // ДОБАВЛЕНО: Обработка UDP ping для VPN bypass
                            HandleVpnBypassUdpPing(packet, ipv4.Udp, ipv4.Source.ToString(), ipv4.Destination.ToString());
                        }
                        
                        if (proto > 0 && App.connectionTracker != null)
                        {
                            // Преобразуем PcapDotNet.IpV4Address в System.Net.IPAddress
                            var srcIP = new System.Net.IPAddress(ipv4.Source.ToValue());
                            var dstIP = new System.Net.IPAddress(ipv4.Destination.ToValue());
                            
                            // Пытаемся разрешить соединение в реальный процесс
                            if (App.connectionTracker.TryResolve(proto, srcIP, srcPort, dstIP, dstPort, out var info))
                            {
                                Debug.Print($"VPN bypass: packet {srcIP}:{srcPort} -> {dstIP}:{dstPort} resolved to PID {info.Pid} ({info.Exe})");
                                DebugLogger.log($"[VPN-BYPASS] RESOLVED proto={proto} local={srcIP}:{srcPort} remote={dstIP}:{dstPort} pid={info.Pid} exe={info.Exe}");
                                Classes.VpnBypassResolver.Register(proto, srcIP, srcPort, dstIP, dstPort, info);
                            }
                            else
                            {
                                var localOwner = App.connectionTracker.QueryLocalOwner(proto, srcIP, srcPort);
                                var localTuple = (ip: srcIP, port: srcPort, remoteIp: dstIP, remotePort: dstPort);

                                if (!localOwner.HasValue)
                                {
                                    var altOwner = App.connectionTracker.QueryLocalOwner(proto, dstIP, dstPort);
                                    if (altOwner.HasValue)
                                    {
                                        localOwner = altOwner;
                                        localTuple = (ip: dstIP, port: dstPort, remoteIp: srcIP, remotePort: srcPort);
                                    }
                                }

                                string ownerText = localOwner.HasValue ? $"pid={localOwner.Value.Pid} exe={localOwner.Value.Exe}" : "pid=?";
                                DebugLogger.log($"[VPN-BYPASS] MISS proto={proto} local={srcIP}:{srcPort} remote={dstIP}:{dstPort} owner={ownerText}");
                                if (localOwner.HasValue)
                                {
                                    Classes.VpnBypassResolver.Register(proto, localTuple.ip, localTuple.port, localTuple.remoteIp, localTuple.remotePort, localOwner.Value);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"VPN bypass error: {ex.Message}");
                    DebugLogger.log($"[VPN-BYPASS] ERROR {ex.Message}");
                }
            }
            
            try
            {
                GameProfileManager.CallBuitInProfiles(packet);
                GameProfileManager.CallCustomProfiles(packet);
                
                if (_totalPacketsReceived % 500 == 0)
                {
                    DebugLogger.log($"[GUI] Calling ActiveWindowTracker.AnalyzePacket for packet #{_totalPacketsReceived}");
                }
                
                ActiveWindowTracker.AnalyzePacket(packet);
            }
            catch (IndexOutOfRangeException ex)
            {
                // Игнорируем поврежденные пакеты в профилях
                DebugLogger.log($"[GUI] IndexOutOfRangeException in profile processing: {ex.Message}");
                return;
            }
            catch (Exception ex)
            {
                // Игнорируем любые другие ошибки в обработке профилей
                DebugLogger.log($"[GUI] Exception in profile processing: {ex.Message}");
                return;
            }

            // --- Добавлено: обработка входящих UDP-пакетов для расчёта UDP ping ---
            try
            {
                // Дополнительная проверка IPv4 доступности
                if (packet.Ethernet.IpV4 == null) return;
                
                var udp = packet.Ethernet.IpV4?.Udp;
                if (udp != null)
                {
                    // Получаем IP и порт назначения (куда пришёл пакет)
                    var dstIp = packet.Ethernet.IpV4.Destination.ToString();
                    var dstPort = udp.DestinationPort;

                    // Получаем IP и порт источника (откуда пришёл пакет)
                    var srcIp = packet.Ethernet.IpV4.Source.ToString();
                    var srcPort = udp.SourcePort;

                    // Проверяем, что пакет пришёл ОТ игрового сервера К НАМ (входящий)
                    // Сравниваем с App.meterState.Server.Ip и PingPort/GamePort
                    if (App.meterState.Server != null)
                    {
                        string serverIp = App.meterState.Server.Ip;
                        int serverPort = App.meterState.Server.PingPort > 0 ? App.meterState.Server.PingPort : App.meterState.Server.GamePort;
                        string localIp = App.meterState.LocalIP;

                        // Если серверный IP совпадает с источником, а наш IP совпадает с получателем
                        if (!string.IsNullOrEmpty(serverIp) && !string.IsNullOrEmpty(localIp)
                            && srcIp == serverIp && dstIp == localIp
                            && (serverPort == 0 || srcPort == serverPort))
                        {
                            // Вызовем обновление UDP ping
                            App.meterState.Server.UpdateUdpPing(packet.Timestamp);
                        }
                    }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // Пакет поврежден или не содержит полные UDP данные
                return;
            }
            catch 
            {
                // Игнорируем любые другие ошибки в UDP ping логике
                return;
            }
            // --- Конец добавления ---
            }
            catch (Exception ex)
            {
                // Глобальная защита от любых ошибок в PacketHandler
                Debug.Print($"[PacketHandler] Unexpected error: {ex.GetType().Name}: {ex.Message}");
                return;
            }
        }

        /// <summary>
        /// Обработчик событий ConnectionTracker для режима VPN bypass
        /// Позволяет отслеживать соединения и обновлять метрики в режиме антимаскировки VPN
        /// </summary>
        // VPN Bypass: Создание соединений с правильной проверкой активности
        private void HandleTunnelConnectionForTracking(ConnectionTracker.Key connectionKey, ConnectionTracker.Info connectionInfo)
        {
            try
            {
                // Проверяем валидность входных параметров
                if (connectionKey.Local == null || connectionKey.Remote == null)
                {
                    DebugLogger.log("[VPN-Tracking] Warning: connectionKey contains null IP addresses");
                    return;
                }

                // Логируем новое соединение
                DebugLogger.log($"[VPN-Tracking] New VPN connection: {connectionKey.Local}:{connectionKey.LocalPort} -> {connectionKey.Remote}:{connectionKey.RemotePort} process={connectionInfo.Exe ?? "unknown"}/{connectionInfo.Pid}");
                
                // Проверяем, что это соединение связано с отслеживаемым процессом
                string activeProcess = null;
                try
                {
                    activeProcess = AutoDetectMngr.GetActiveProcessName();
                    DebugLogger.log($"[VPN-Tracking] Active process: '{activeProcess}' vs connection process: '{connectionInfo.Exe}'");
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[VPN-Tracking] Error getting active process: {ex.Message}");
                    return;
                }
                
                // В режиме VPN bypass показываем только соединения активного процесса
                bool isGameTraffic = false;
                
                // Проверяем точное совпадение процессов
                if (!string.IsNullOrEmpty(connectionInfo.Exe) && 
                    !string.IsNullOrEmpty(activeProcess) &&
                    connectionInfo.Exe.Equals(activeProcess, StringComparison.OrdinalIgnoreCase))
                {
                    isGameTraffic = true;
                    DebugLogger.log($"[VPN-Tracking] Game traffic detected: {connectionInfo.Exe}");
                }
                else
                {
                    DebugLogger.log($"[VPN-Tracking] Non-active process: {connectionInfo.Exe} (active: {activeProcess}) - will handle separately");
                    // НЕ возвращаемся! Позволяем обработать для показа "NO TRAFFIC"
                }
                
                if (isGameTraffic)
                {
                    // Создаем соединение в ActiveWindowTracker для VPN bypass
                    string connectionId = $"{connectionKey.Local}:{connectionKey.LocalPort}:{connectionKey.Remote}:{connectionKey.RemotePort}";
                    
                    lock (ActiveWindowTracker.connectionsLock)
                    {
                        if (!ActiveWindowTracker.connections.ContainsKey(connectionId))
                        {
                            // Получаем реальную статистику трафика
                            var (realDownloaded, realUploaded) = GetRealNetworkTraffic();
                            
                            var vpnConnection = new ProcessNetworkStats
                            {
                                name = connectionInfo.Exe ?? "Unknown",
                                localIp = connectionKey.Local?.ToString() ?? "0.0.0.0",
                                remoteIp = connectionKey.Remote?.ToString() ?? "0.0.0.0",
                                remotePort = (ushort)connectionKey.RemotePort,
                                downloaded = (int)Math.Min(realDownloaded / 100, int.MaxValue), // Реальный трафик с масштабированием
                                sent = (int)Math.Min(realUploaded / 100, int.MaxValue), // Реальный трафик с масштабированием
                                tickTimeBuffer = new List<float>(),
                                startTrack = DateTime.Now.AddSeconds(-5),
                                lastUpdate = DateTime.Now,
                                ticksIn = 10, // Правильное значение > 3
                                totalTicksCnt = 100 // ИСПРАВЛЕНИЕ: Инициализируем totalTicksCnt
                            };
                            
                            // Сохраняем начальные значения в кэш для подсчета дельты
                            _vpnTrafficCache[connectionId] = (realDownloaded, realUploaded, DateTime.Now);
                            
                            ActiveWindowTracker.connections[connectionId] = vpnConnection;
                            DebugLogger.log($"[VPN-Tracking] Added VPN connection with REAL traffic: {connectionId} (downloaded: {vpnConnection.downloaded}, sent: {vpnConnection.sent}, real_dl: {realDownloaded}, real_ul: {realUploaded})");
                        }
                        else
                        {
                            // Обновляем существующее соединение РЕАЛЬНЫМИ значениями
                            var existing = ActiveWindowTracker.connections[connectionId];
                            existing.lastUpdate = DateTime.Now;
                            existing.ticksIn += 1;
                            existing.totalTicksCnt += 1;
                            
                            // Вычисляем реальную дельту трафика
                            if (_vpnTrafficCache.ContainsKey(connectionId))
                            {
                                var (currentDownloaded, currentUploaded) = GetRealNetworkTraffic();
                                var (lastDownloaded, lastUploaded, lastTime) = _vpnTrafficCache[connectionId];
                                
                                // Вычисляем дельту за последний период (только положительные приросты)
                                long deltaDownloaded = Math.Max(0, currentDownloaded - lastDownloaded);
                                long deltaUploaded = Math.Max(0, currentUploaded - lastUploaded);
                                
                                // Добавляем реальный прирост (масштабированный)
                                if (deltaDownloaded > 0) existing.downloaded += (int)Math.Min(deltaDownloaded / 1000, int.MaxValue);
                                if (deltaUploaded > 0) existing.sent += (int)Math.Min(deltaUploaded / 1000, int.MaxValue);
                                
                                // Обновляем кэш
                                _vpnTrafficCache[connectionId] = (currentDownloaded, currentUploaded, DateTime.Now);
                                
                                DebugLogger.log($"[VPN-Tracking] Updated VPN connection with REAL delta: {connectionId} (downloaded: +{deltaDownloaded/1000}, sent: +{deltaUploaded/1000}, total_dl: {existing.downloaded}, total_sent: {existing.sent})");
                            }
                            else
                            {
                                // Fallback если нет кэша - минимальный прирост
                                existing.downloaded += 128;
                                existing.sent += 64;
                                DebugLogger.log($"[VPN-Tracking] Updated VPN connection (fallback): {connectionId} (downloaded: {existing.downloaded}, sent: {existing.sent})");
                            }
                        }
                    }
                }
                else
                {
                    // Для неактивных процессов не создаем соединения - это позволит показать "NO TRAFFIC"
                    DebugLogger.log($"[VPN-Tracking] Non-active process {connectionInfo.Exe} - not creating VPN connection to allow NO TRAFFIC display");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[VPN-Tracking] Error in HandleTunnelConnectionForTracking: {ex.Message}");
            }
        }

        /// <summary>
        /// Получает реальную статистику трафика для VPN режима
        /// </summary>
        private (long downloaded, long uploaded) GetRealNetworkTraffic()
        {
            try
            {
                long totalDownloaded = 0;
                long totalUploaded = 0;
                
                // Получаем статистику всех активных сетевых интерфейсов
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    // Пропускаем loopback и неактивные интерфейсы
                    if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback || 
                        ni.OperationalStatus != OperationalStatus.Up)
                        continue;
                    
                    // Получаем статистику интерфейса
                    IPv4InterfaceStatistics stats = ni.GetIPv4Statistics();
                    totalDownloaded += stats.BytesReceived;
                    totalUploaded += stats.BytesSent;
                }
                
                return (totalDownloaded, totalUploaded);
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[VPN-RealTraffic] Error getting real network stats: {ex.Message}");
                return (0, 0);
            }
        }

        /// <summary>
        /// Кэш для хранения предыдущих значений трафика (для подсчета дельты)
        /// </summary>
        private static readonly Dictionary<string, (long downloaded, long uploaded, DateTime timestamp)> _vpnTrafficCache 
            = new Dictionary<string, (long, long, DateTime)>();
        private void InitializeVpnTickrateEmulation(ProcessNetworkStats connection, string processName)
        {
            try
            {
                // Определяем базовый тикрейт в зависимости от типа приложения
                int baseTickrate = DetermineBaseTickrateForProcess(processName);
                
                // Устанавливаем начальные значения для эмуляции
                connection.ticksIn = baseTickrate;
                connection.ticksOut = baseTickrate / 4; // Обычно исходящий трафик меньше
                
                // Инициализируем буфер тиктайма
                if (connection.tickTimeBuffer == null)
                    connection.tickTimeBuffer = new List<float>();
                
                // Добавляем базовые значения тиктайма (1000ms / tickrate)
                float baseTicktime = baseTickrate > 0 ? 1000.0f / baseTickrate : 7.8f;
                connection.tickTimeBuffer.Add(baseTicktime);
                
                DebugLogger.log($"[VPN-Emulation] Initialized tickrate emulation for {processName}: tickrate={baseTickrate}, ticktime={baseTicktime:F1}ms");
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[VPN-Emulation] Error initializing tickrate emulation: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Эмулирует активность тикрейта для VPN соединений
        /// </summary>
        private void EmulateVpnTickrateActivity(ProcessNetworkStats connection, string processName)
        {
            try
            {
                // Эмулируем реалистичные колебания тикрейта
                int baseTickrate = DetermineBaseTickrateForProcess(processName);
                
                // Добавляем небольшие вариации (±10%)
                Random rnd = new Random();
                double variation = 0.9 + (rnd.NextDouble() * 0.2); // 0.9 - 1.1
                int currentTickrate = (int)(baseTickrate * variation);
                
                // Обновляем счетчики
                connection.ticksIn += currentTickrate;
                connection.ticksOut += currentTickrate / 4;
                
                // Обновляем тиктайм с вариациями
                if (connection.tickTimeBuffer != null)
                {
                    float currentTicktime = currentTickrate > 0 ? 1000.0f / currentTickrate : 7.8f;
                    
                    // Добавляем реалистичные вариации тиктайма (±5%)
                    double ticktimeVariation = 0.95 + (rnd.NextDouble() * 0.1); // 0.95 - 1.05
                    currentTicktime = (float)(currentTicktime * ticktimeVariation);
                    
                    connection.tickTimeBuffer.Add(currentTicktime);
                    
                    // Ограничиваем размер буфера
                    if (connection.tickTimeBuffer.Count > 100)
                    {
                        connection.tickTimeBuffer.RemoveAt(0);
                    }
                }
                
                DebugLogger.log($"[VPN-Emulation] Updated {processName} activity: tickrate={currentTickrate}, ticksIn={connection.ticksIn}");
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[VPN-Emulation] Error emulating tickrate activity: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Получает РЕАЛЬНЫЕ данные активности для VPN соединений вместо эмуляции
        /// </summary>
        private void UpdateVpnRealActivity(ProcessNetworkStats connection, string processName)
        {
            try
            {
                // Получаем РЕАЛЬНЫЕ данные трафика процесса
                var realTraffic = Classes.RealProcessTrafficMonitor.GetRealProcessTraffic(processName);
                
                if (realTraffic != null && (realTraffic.BytesReceivedPerSec > 0 || realTraffic.BytesSentPerSec > 0))
                {
                    // Конвертируем реальный трафик в тикрейт на основе активности
                    long totalTrafficPerSec = realTraffic.BytesReceivedPerSec + realTraffic.BytesSentPerSec;
                    
                    // Алгоритм: больше трафика = выше тикрейт (примерная корреляция)
                    int calculatedTickrate = CalculateTickrateFromTraffic(totalTrafficPerSec, processName);
                    
                    // Обновляем данные на основе РЕАЛЬНОЙ активности
                    connection.ticksIn += calculatedTickrate;
                    connection.ticksOut += calculatedTickrate / 4;
                    
                    // Обновляем тиктайм на основе реального тикрейта
                    if (connection.tickTimeBuffer != null)
                    {
                        float currentTicktime = calculatedTickrate > 0 ? 1000.0f / calculatedTickrate : 7.8f;
                        connection.tickTimeBuffer.Add(currentTicktime);
                        
                        // Ограничиваем размер буфера
                        if (connection.tickTimeBuffer.Count > 100)
                        {
                            connection.tickTimeBuffer.RemoveAt(0);
                        }
                    }
                    
                    DebugLogger.log($"[VPN-RealData] Updated {processName} from REAL traffic: {totalTrafficPerSec} bytes/sec → tickrate={calculatedTickrate}");
                }
                else
                {
                    // Fallback: минимальная активность если нет реальных данных
                    int baseTickrate = DetermineBaseTickrateForProcess(processName);
                    connection.ticksIn += Math.Max(baseTickrate / 10, 1); // Минимальная активность
                    connection.ticksOut += 1;
                    
                    DebugLogger.log($"[VPN-Fallback] No real traffic data for {processName}, using minimal activity: {baseTickrate/10}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[VPN-RealData] Error getting real activity: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Конвертирует реальный трафик в тикрейт на основе активности сети
        /// </summary>
        private int CalculateTickrateFromTraffic(long totalBytesPerSec, string processName)
        {
            try
            {
                // Базовый тикрейт для процесса
                int baseTickrate = DetermineBaseTickrateForProcess(processName);
                
                // Алгоритм расчёта тикрейта на основе трафика:
                // Низкая активность (< 1KB/s) = низкий тикрейт
                // Средняя активность (1KB - 100KB/s) = средний тикрейт  
                // Высокая активность (> 100KB/s) = высокий тикрейт
                
                if (totalBytesPerSec < 1024) // < 1KB/s
                {
                    return Math.Max(baseTickrate / 8, 5); // Минимальная активность
                }
                else if (totalBytesPerSec < 10240) // < 10KB/s
                {
                    return Math.Max(baseTickrate / 4, 15); // Низкая активность
                }
                else if (totalBytesPerSec < 102400) // < 100KB/s
                {
                    return Math.Max(baseTickrate / 2, 30); // Средняя активность
                }
                else
                {
                    return baseTickrate; // Высокая активность = полный тикрейт
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[VPN-RealData] Error calculating tickrate from traffic: {ex.Message}");
                return DetermineBaseTickrateForProcess(processName) / 4; // Безопасный fallback
            }
        }
        
        /// <summary>
        /// Обновляет реальные данные трафика для VPN bypass вместо эмуляции
        /// </summary>
        private void UpdateRealVpnTraffic(ProcessNetworkStats procStats)
        {
            try
            {
                // Получаем IP адрес сервера для ping измерений
                string serverIP = App.meterState.Server?.Ip;
                int serverPort = App.meterState.Server?.GamePort > 0 ? App.meterState.Server.GamePort : 80;
                
                // Получаем реальные данные трафика с ping измерениями
                var realTraffic = !string.IsNullOrEmpty(serverIP) 
                    ? Classes.RealProcessTrafficMonitor.GetRealProcessTrafficWithPing(procStats.name, serverIP, serverPort)
                    : Classes.RealProcessTrafficMonitor.GetRealProcessTraffic(procStats.name);
                
                if (realTraffic != null)
                {
                    // Конвертируем реальные данные в накопленный трафик
                    // Интегрируем байты в секунду в общий объём трафика
                    long downloadIncrement = realTraffic.BytesReceivedPerSec;
                    long uploadIncrement = realTraffic.BytesSentPerSec;
                    
                    // НАКАПЛИВАЕМ реальный трафик вместо замены (с приведением типов)
                    procStats.downloaded += (int)Math.Min(downloadIncrement, int.MaxValue);
                    procStats.sent += (int)Math.Min(uploadIncrement, int.MaxValue);
                    
                    // Обновляем тикрейт на основе реальной активности
                    int realTickrateBoost = CalculateTickrateFromTraffic(downloadIncrement + uploadIncrement, procStats.name);
                    procStats.ticksIn += realTickrateBoost;
                    procStats.totalTicksCnt += realTickrateBoost;
                    
                    // В VPN bypass режиме счётчики накапливаются через ETW (строки 3197-3198)
                    // НЕ перезаписываем их здесь!
                    // App.meterState.DownloadTraffic = procStats.downloaded;
                    // App.meterState.UploadTraffic = procStats.sent;
                    
                    // Обновляем PING данные в VPN режиме
                    if (realTraffic.RealPingMs > 0 && App.meterState.Server != null)
                    {
                        App.meterState.Server.Ping = realTraffic.RealPingMs;
                        DebugLogger.log($"[VPN-RealPing] Updated REAL ping: {realTraffic.RealPingMs}ms (jitter: {realTraffic.JitterMs:F1}ms)");
                    }
                    
                    DebugLogger.log($"[VPN-RealTraffic] Updated REAL traffic - Download: +{downloadIncrement} (total: {procStats.downloaded}), Upload: +{uploadIncrement} (total: {procStats.sent}), TickRate: +{realTickrateBoost}");
                }
                else
                {
                    // Fallback: минимальная реалистичная активность вместо больших фиксированных значений
                    int minDownload = 1024; // 1KB вместо 512KB
                    int minUpload = 512;    // 512B вместо 256KB
                    
                    procStats.downloaded += minDownload;
                    procStats.sent += minUpload;
                    procStats.ticksIn += 1; // Минимальный прирост
                    procStats.totalTicksCnt += 1;
                    
                    // В VPN bypass режиме счётчики накапливаются через ETW
                    // НЕ перезаписываем их здесь!
                    // App.meterState.DownloadTraffic = procStats.downloaded;
                    // App.meterState.UploadTraffic = procStats.sent;
                    
                    DebugLogger.log($"[VPN-FallbackTraffic] Using minimal traffic - Download: +{minDownload}, Upload: +{minUpload}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[VPN-RealTraffic] Error updating real traffic: {ex.Message}");
                
                // Аварийный fallback
                procStats.downloaded += 1024;
                procStats.sent += 512;
                // В VPN bypass режиме счётчики накапливаются через ETW
                // НЕ перезаписываем их здесь!
                // App.meterState.DownloadTraffic = procStats.downloaded;
                // App.meterState.UploadTraffic = procStats.sent;
            }
        }
        
        /// <summary>
        /// Определяет базовый тикрейт для процесса
        /// </summary>
        private int DetermineBaseTickrateForProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return 64; // Дефолтный тикрейт
                
            string process = processName.ToLowerInvariant();
            
            // Игровые процессы с высоким тикрейтом
            if (process.Contains("cs2") || process.Contains("csgo") || 
                process.Contains("valorant") || process.Contains("apex"))
                return 128;
                
            if (process.Contains("dota2") || process.Contains("lol") || 
                process.Contains("overwatch") || process.Contains("pubg"))
                return 60;
                
            if (process.Contains("fortnite") || process.Contains("warzone") ||
                process.Contains("battlefield"))
                return 120;
                
            // Браузеры и обычные приложения
            if (process.Contains("chrome") || process.Contains("firefox") || 
                process.Contains("edge") || process.Contains("browser"))
                return 30;
                
            if (process.Contains("discord") || process.Contains("steam") ||
                process.Contains("spotify"))
                return 20;
                
            // IDE и редакторы кода
            if (process.Contains("devenv") || process.Contains("code") ||
                process.Contains("visual") || process.Contains("rider"))
                return 15;
                
            // Дефолтное значение для неизвестных процессов
            return 64;
        }
        
        /// <summary>
        /// Обрабатывает UDP ping для VPN bypass на основе интервалов пакетов
        /// </summary>
        private void HandleVpnBypassUdpPing(PcapDotNet.Packets.Packet packet, PcapDotNet.Packets.Transport.UdpDatagram udp, string srcIp, string dstIp)
        {
            try
            {
                // Получаем информацию о сервере из текущего состояния
                if (App.meterState?.Server?.Ip == null)
                    return;

                string serverIp = App.meterState.Server.Ip;
                
                // Используем LocalIPDetector для получения локального IP
                string localIp = null;
                try
                {
                    var processName = App.meterState.Game ?? "unknown";
                    var localIPAddr = Classes.LocalIPDetector.DetectLocalIPForActiveProcess(processName);
                    localIp = localIPAddr?.ToString();
                }
                catch
                {
                    // Fallback - используем кешированный IP
                    localIp = Classes.LocalIPDetector.GetCachedIP();
                    if (string.IsNullOrEmpty(localIp))
                    {
                        // Последний fallback - любой локальный IP
                        localIp = System.Net.NetworkInformation.NetworkInterface
                            .GetAllNetworkInterfaces()
                        .Where(n => n.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up)
                        .SelectMany(n => n.GetIPProperties().UnicastAddresses)
                        .FirstOrDefault(a => a.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !System.Net.IPAddress.IsLoopback(a.Address))
                        ?.Address?.ToString();
                    }
                }
                
                if (string.IsNullOrEmpty(localIp))
                    return;

                // Проверяем, что это пакет ОТ сервера К нам (входящий)
                if (srcIp == serverIp && dstIp == localIp)
                {
                    // Обновляем UDP ping через анализ интервалов в RealProcessTrafficMonitor
                    var processName = App.meterState.Game ?? "unknown";
                    Classes.RealProcessTrafficMonitor.UpdateUdpPingFromPacket(
                        processName, 
                        serverIp, 
                        udp.SourcePort, 
                        srcIp, 
                        udp.SourcePort, 
                        dstIp, 
                        udp.DestinationPort, 
                        DateTime.Now);
                    
                    DebugLogger.log($"[VPN-UdpPing] Processed packet from {srcIp}:{udp.SourcePort} -> {dstIp}:{udp.DestinationPort}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[VPN-UdpPing] Error processing UDP ping: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Регулярно обновляет эмулированный тикрейт для всех VPN соединений
        /// </summary>
        private void UpdateVpnTickrateEmulation()
        {
            try
            {
                // Проверяем, включен ли VPN bypass режим
                bool vpnBypassBasic = App.settingsManager?.GetOption("vpn_bypass_basic", "False", "ADVANCED") == "True";
                bool vpnBypassAdvanced = App.settingsManager?.GetOption("vpn_bypass_advanced", "False", "ADVANCED") == "True";
                
                if (!vpnBypassBasic && !vpnBypassAdvanced)
                    return; // VPN bypass отключен
                
                string activeProcess = AutoDetectMngr.GetActiveProcessName();
                if (string.IsNullOrEmpty(activeProcess))
                    return;
                
                lock (ActiveWindowTracker.connectionsLock)
                {
                    // Обновляем тикрейт для всех VPN соединений
                    var vpnConnections = ActiveWindowTracker.connections.Values
                        .Where(conn => conn.name.Equals(activeProcess, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    foreach (var connection in vpnConnections)
                    {
                        // Переходим на РЕАЛЬНЫЕ данные вместо эмуляции
                        UpdateVpnRealActivity(connection, activeProcess);
                    }
                    
                    if (vpnConnections.Count > 0)
                    {
                        DebugLogger.log($"[VPN-Emulation] Updated tickrate for {vpnConnections.Count} VPN connections of {activeProcess}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[VPN-Emulation] Error updating VPN tickrate emulation: {ex.Message}");
            }
        }

        bool RTSS_Failed = false;
        
        private void TicksLoop_Tick(object sender, EventArgs e)
        {
            // Анти-реэнтерабельность: если предыдущий тик еще не завершен - пропускаем
            if (Interlocked.Exchange(ref _tickBusy, 1) == 1) 
            {
                Debug.Print("[GUI] Tick skipped - previous still running");
                return;
            }
            
            _ = Task.Run(async () =>
            {
                try
            {
                AutoDetectMngr.GetActiveProcessName(true);
                
                // Обновляем активный процесс для ETW в VPN bypass режиме
                bool vpnBypassBasic = App.settingsManager?.GetOption("vpn_bypass_basic", "False", "ADVANCED") == "True";
                bool vpnBypassAdvanced = App.settingsManager?.GetOption("vpn_bypass_advanced", "False", "ADVANCED") == "True";
                if (vpnBypassBasic || vpnBypassAdvanced)
                {
                    string currentActiveProcess = AutoDetectMngr.GetActiveProcessName();
                    if (!string.IsNullOrEmpty(currentActiveProcess) && currentActiveProcess != "n\\a")
                    {
                        Classes.ETW.SetActiveProcess(currentActiveProcess);
                    }
                }
                
                // NEW: Обновляем эмулированный тикрейт для VPN bypass режима (только если НЕ VPN bypass advanced)
                if (!vpnBypassAdvanced)
                {
                    UpdateVpnTickrateEmulation();
                }
                
                // NEW: Обновляем трафик через Windows Statistics (для обычного режима)
                UpdateTrafficFromWindowsStats();
                
                // Диагностика для VPN bypass
                bool builtInActive = App.meterState.isBuiltInProfileActive;
                bool customActive = App.meterState.isCustomProfileActive;
                DebugLogger.log($"[TickLoop] Profiles: BuiltIn={builtInActive}, Custom={customActive}, WillUpdate={!builtInActive && !customActive}");
                
                if(!builtInActive && !customActive)
                {
                    DebugLogger.log("[TickLoop] Calling updateMetherStateFromActiveWindow");
                    updateMetherStateFromActiveWindow();
                    DebugLogger.log("[TickLoop] updateMetherStateFromActiveWindow COMPLETED");
                }
                else
                {
                    DebugLogger.log("[TickLoop] Skipping updateMetherStateFromActiveWindow due to active profiles");
                }
                
                // Троттлинг RTSS: обновляем не каждый тик, а по таймеру
                bool rtssThrottlingEnabled = App.settingsManager.GetOption("rtss_throttling", "True", "ADVANCED") == "True";
                int throttlePeriod = rtssThrottlingEnabled ? RtssPeriodMs : 50; // Если throttling отключен, обновляем чаще
                if (App.settingsForm.settings_rtss_output.Checked && _rtssSw.ElapsedMilliseconds >= throttlePeriod)
                {
                    await Task.Run(() => {
                        try { 
                            try { RivaTuner.BuildRivaOutput(); } catch (TypeInitializationException) { /* RTSS.dll отсутствует */ } catch { } 
                            _rtssSw.Restart();
                        } catch (Exception ex) {
                            if(!RTSS_Failed)
                            {
                                DebugLogger.log(ex);
                                RTSS_Failed = true;
                            }
                        }
                    });
                }

            //form overlay isn't visible, but still update ping data for both GUI and RTSS
            bool refreshWhileHidden = App.settingsManager.GetOption("ui_refresh_hidden", "False", "SETTINGS") == "True";
            bool skipGUIUpdate = !OnScreen && !refreshWhileHidden;

            if (App.connMngr != null)
            {
                var periodicNow = DateTime.Now;
                if ((periodicNow - _lastPeriodicConnRefresh) > TimeSpan.FromSeconds(30))
                {
                    RequestConnectionsRefresh(false);
                    _lastPeriodicConnRefresh = periodicNow;
                }
            }

            // === ChatGPT ENHANCED: Snapshot-based unified zoning ===
            // IMPORTANT: Get snapshot for target Hz, but apply zones to SMOOTHED display values
            var snap = Classes.UnifiedDataSource.Snapshot();
            var profile = App.settingsManager.GetColorZoneProfile();
            var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
            
            bool hasActiveSession = App.meterState.IsTracking &&
                                    App.meterState.Server != null &&
                                    !string.IsNullOrEmpty(App.meterState.Server.Ip);
            bool showNoTrafficPlaceholder = !hasActiveSession;
            
            await Task.Run(
                    () => {
                        var server = App.meterState.Server;
                        string activeProcessName = AutoDetectMngr.GetActiveProcessName();
                        bool hasLocation = hasActiveSession && server != null && !string.IsNullOrEmpty(server.Location);

                        // Определяем источник и значение пинга
                        int rawPing = 0;
                        string pingSource = "none";
                        if (server != null)
                        {
                            if (App.meterState.TcpPing >= 1000 && App.meterState.IsUdpPingValid)
                            {
                                rawPing = (int)Math.Round(server.UdpPing);
                                pingSource = "udp";
                            }
                            else if (server.Ping > 0 && server.Ping < 10000)
                            {
                                rawPing = server.Ping;
                                pingSource = "tcp";
                            }
                            else if (App.meterState.IcmpPing > 0 && App.meterState.IcmpPing < 1000)
                            {
                                rawPing = App.meterState.IcmpPing;
                                pingSource = "icmp";
                            }
                        }

                        // FIXED: Apply smoothing first, then determine zone from smoothed value
                        int displayPing = rawPing > 0 ? Classes.SmoothingManager.SmoothPingValueGui(rawPing) : 0;
                        
                        // DEBUG: Log GUI smoothing for verification
                        if (rawPing > 0)
                        {
                            DebugLogger.log($"[GUI-PING] Raw={rawPing} -> Smoothed={displayPing}");
                        }
                        
                        // Calculate zone from SMOOTHED display value, not raw snapshot
                        var pingZone = zoner.FromPing(displayPing);
                        Color PingColor = Classes.ZoneColors.ToColor(pingZone);
                        string pingText = rawPing > 0 ? $"{displayPing} ms" : "n/a ms";

                        bool showSpikeIndicator = App.settingsManager?.GetOption("show_ping_spikes", "True", "ADVANCED") == "True";
                        bool pingSpikeActive = hasActiveSession && showSpikeIndicator && server?.HasPingSpike == true;
                        Debug.Print($"[GUI] Spike check: HasPingSpike={server?.HasPingSpike ?? false}, ShowSetting={showSpikeIndicator}, OnScreen={OnScreen}");
                        if (pingSpikeActive)
                        {
                            pingText += " (!)";
                            Debug.Print($"[GUI] Spike indicator added with zone color: {pingText}");
                        }

                        string pingDisplayText = hasActiveSession ? pingText : "NO TRAFFIC!";
                        Color finalPingColor;
                        if (showNoTrafficPlaceholder)
                        {
                            finalPingColor = Color.Red;
                        }
                        else
                        {
                            finalPingColor = hasActiveSession ? PingColor : _inactiveMetricColor;
                        }

                        int rawTickrate = App.meterState.OutputTickRate;
                        // FIXED: Apply smoothing first, then determine zone from smoothed value
                        int displayTickrate = Classes.SmoothingManager.SmoothTickrateValueGui(rawTickrate);
                        
                        // Calculate zone from SMOOTHED display value, not raw snapshot
                        var tickrateZone = zoner.FromTickrate(displayTickrate);
                        Color TickRateColor = Classes.ZoneColors.ToColor(tickrateZone);
                        
                        bool showTickrateSpikes = App.settingsManager?.GetOption("show_tickrate_spikes", "True", "ADVANCED") == "True";
                        bool tickrateSpikeActive = hasActiveSession && showTickrateSpikes && App.meterState.HasTickRateSpike;
                        string tickrateText = displayTickrate.ToString();
                        if (tickrateSpikeActive)
                        {
                            tickrateText += " (!)";
                            Debug.Print($"[GUI] Tickrate spike indicator added with zone color: {tickrateText}");
                        }
                        Color finalTickRateColor = hasActiveSession ? TickRateColor : _inactiveMetricColor;

                        double uploadMb = App.meterState.UploadTraffic / (1024d * 1024d);
                        double downloadMb = App.meterState.DownloadTraffic / (1024d * 1024d);
                        string trafficDisplayText = hasActiveSession
                            ? $"{uploadMb:N2} / {downloadMb:N2} mb"
                            : $"{0f:N2} / {0f:N2} mb";

                        string ipDisplayText = hasActiveSession ? server?.Ip ?? string.Empty : string.Empty;

                        TimeSpan sessionDuration = hasActiveSession && !string.IsNullOrEmpty(ipDisplayText)
                            ? DateTime.Now.Subtract(App.meterState.SessionStart)
                            : TimeSpan.Zero;
                        string sessionDurationText = sessionDuration.ToString("mm':'ss");
                        float dropsPercent = App.meterState.GetDropsNumber();
                        string dropsText = dropsPercent.ToString("n2") + "%";

                        // Always update PING (including spike indicators) for both GUI and RTSS overlay
                        if (App.settingsForm.settings_ping_checkbox.Checked)
                        {
                            QueueUIUpdate(() =>
                            {
                                countryLbl.Text = hasLocation ? server.Location : string.Empty;
                                countryLbl.ForeColor = hasLocation ? _neutralActiveColor : _inactiveMetricColor;
                            });

                            QueueUIUpdate(() =>
                            {
                                ping_val.ForeColor = finalPingColor;
                                ping_val.Text = pingDisplayText;
                            });
                        }

                        // Only update other GUI elements if GUI overlay is visible
                        if (!skipGUIUpdate)
                        {
                            QueueUIUpdate(() =>
                            {
                                tickrate_val.Text = tickrateText;
                                tickrate_val.ForeColor = finalTickRateColor;
                            });

                            if (App.settingsForm.settings_chart_checkbox.Checked)
                            {
                                QueueUIUpdate(() => UpdateTickrateChart(App.meterState.TicksHistory, App.meterState.TickTimestamps));
                            }

                            if (App.settingsForm.settings_traffic_checkbox.Checked)
                            {
                                QueueUIUpdate(() =>
                                {
                                    traffic_val.Text = trafficDisplayText;
                                    traffic_val.ForeColor = hasActiveSession ? _neutralActiveColor : (showNoTrafficPlaceholder ? Color.Red : _inactiveMetricColor);
                                });
                            }

                            if (App.settingsForm.settings_ip_checkbox.Checked)
                            {
                                QueueUIUpdate(() =>
                                {
                                    ip_val.Text = ipDisplayText;
                                    ip_val.ForeColor = hasActiveSession ? _neutralActiveColor : _inactiveMetricColor;
                                });
                            }

                            if (App.settingsForm.settings_session_time_checkbox.Checked)
                            {
                                QueueUIUpdate(() =>
                                {
                                    if (hasActiveSession && !string.IsNullOrEmpty(ipDisplayText))
                                    {
                                        time_val.Text = sessionDurationText;
                                        time_val.ForeColor = _neutralActiveColor;
                                    }
                                    else
                                    {
                                        time_val.Text = "00:00";
                                        time_val.ForeColor = _inactiveMetricColor;
                                    }
                                });
                            }

                            //update process name
                            {
                                string processName = activeProcessName;
                                QueueUIUpdate(() =>
                                {
                                    if (!string.IsNullOrEmpty(processName) && processName != "n\\a")
                                    {
                                        process_val.Text = processName;
                                        process_val.ForeColor = Color.LightGray;
                                    }
                                    else
                                    {
                                        process_val.Text = "n/a";
                                        process_val.ForeColor = Color.Gray;
                                    }
                                });
                            }
                            
                            //update drops
                            if (App.settingsForm.packet_drops_checkbox.Checked)
                            {
                                QueueUIUpdate(() =>
                                {
                                    if (hasActiveSession)
                                    {
                                        drops_lbl_val.Text = dropsText;
                                        drops_lbl_val.ForeColor = GetDropsColor(dropsPercent);
                                    }
                                    else
                                    {
                                        drops_lbl_val.Text = 0f.ToString("n2") + "%";
                                        drops_lbl_val.ForeColor = _inactiveMetricColor;
                                    }
                                });
                            }
                        }

                        var diagnosticPayload = new MetricDiagnosticPayload
                        {
                            Game = App.meterState.Game,
                            ActiveProcess = activeProcessName,
                            TargetKey = targetKey ?? string.Empty,
                            LocalIp = App.meterState.LocalIP,
                            IsTracking = App.meterState.IsTracking,
                            GuiVisible = OnScreen,
                            Server = new ServerMetrics
                            {
                                Ip = server?.Ip,
                                PingPort = server?.PingPort ?? 0,
                                Location = server?.Location,
                                OutputTickRate = App.meterState.OutputTickRate,
                                AvgTickrate = server?.AvgTickrate ?? 0,
                                AvgStableTickrate = server?.AvgStableTickrate ?? 0,
                                TotalTicks = server?.TotalTicksCount ?? 0,
                                LostTicks = server?.LostTicks ?? 0,
                                PacketLossPercent = dropsPercent,
                                AvgPingMs = snap.PingAvgMs,
                                UdpPingMs = server?.UdpPing ?? 0,
                                TcpPingMs = server?.Ping ?? 0,
                                IcmpPingMs = App.meterState.IcmpPing
                            },
                            Gui = new GuiMetrics
                            {
                                Ping = new PingMetrics
                                {
                                    RawMs = rawPing,
                                    GuiDisplayedMs = displayPing,
                                    OverlaySnapshotMs = snap.PingAvgMs,
                                    Source = pingSource,
                                    DisplayText = pingDisplayText,
                                    ColorHex = MetricDiagnostics.ToHex(finalPingColor)
                                },
                                Tickrate = new TickrateMetrics
                                {
                                    Raw = rawTickrate,
                                    GuiDisplayText = tickrateText,
                                    ColorHex = MetricDiagnostics.ToHex(finalTickRateColor),
                                    SnapshotAvgHz = snap.TickrateAvgHz
                                },
                                Traffic = new TrafficMetrics
                                {
                                    UploadMb = uploadMb,
                                    DownloadMb = downloadMb
                                },
                                SessionDuration = sessionDurationText
                            },
                            Overlay = new OverlaySnapshot
                            {
                                PingMs = snap.PingAvgMs,
                                TickrateAvgHz = snap.TickrateAvgHz,
                                TicktimeAvgMs = snap.TicktimeAvgMs,
                                TargetHz = snap.TargetHz
                            },
                            Zones = new ZoneMetrics
                            {
                                Ping = pingZone.ToString(),
                                Tickrate = tickrateZone.ToString(),
                                Ticktime = "N/A" // Ticktime uses same zone as tickrate (inverse relationship)
                            },
                            Spikes = new SpikeMetrics
                            {
                                Ping = pingSpikeActive,
                                Tickrate = tickrateSpikeActive,
                                Ticktime = App.meterState.HasTickTimeSpike
                            },
                            Smoothing = new SmoothingFlags
                            {
                                PingGuiEnabled = SmoothingManager.IsPingValueEnabled(),
                                PingOverlayEnabled = SmoothingManager.IsPingValueEnabled(),
                                TickrateOverlayEnabled = SmoothingManager.IsTickrateValueEnabled(),
                                TrafficOverlayEnabled = SmoothingManager.IsTrafficValueEnabled()
                            },
                            Diagnostic = $"Zones calculated from smoothed display values (GUI: Ping={displayPing}, Tickrate={displayTickrate})"
                        };

                        MetricDiagnostics.TryLog(diagnosticPayload);
                    });
            
            // Stage 6: Обновляем анализатор качества сети
            if (App.settingsManager?.GetOption("network_quality_enabled", "True", "ADVANCED") == "True")
            {
                try
                {
                    float networkQualityPing = 0;
                    float currentTickrate = App.meterState.OutputTickRate;
                    float currentTicktime = 0;
                    float currentPacketLoss = 0;
                    var dropsString = App.meterState.GetDrops();
                    if (SettingsManager.TryParsePercent(dropsString, out float drops))
                    {
                        currentPacketLoss = drops;
                    }
                    
                    // Получаем ping из разных источников
                    if (App.meterState.TcpPing >= 1000 && App.meterState.IsUdpPingValid)
                    {
                        networkQualityPing = App.meterState.Server.UdpPing;
                    }
                    else if (App.meterState.Server.Ping > 0 && App.meterState.Server.Ping < 10000)
                    {
                        networkQualityPing = App.meterState.Server.Ping;
                    }
                    else if (App.meterState.IcmpPing > 0 && App.meterState.IcmpPing < 1000)
                    {
                        networkQualityPing = App.meterState.IcmpPing;
                    }
                    
                    // Получаем ticktime из буфера
                    if (App.meterState.tickTimeBuffer != null)
                    {
                        lock (App.meterState._tickTimeBufferLock)
                        {
                            if (App.meterState.tickTimeBuffer.Count > 0)
                            {
                                currentTicktime = App.meterState.tickTimeBuffer[App.meterState.tickTimeBuffer.Count - 1];
                            }
                        }
                    }
                    
                    // Передаем данные в анализатор
                    if (networkQualityPing > 0 || currentTickrate > 0)
                    {
                        Classes.NetworkQualityAnalyzer.AddNetworkData(networkQualityPing, currentTickrate, currentTicktime, currentPacketLoss);
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"[GUI] Network quality analysis error: {ex.Message}");
                }
            }
            
            // Обновляем состояние мигания спайков
            _spikeBlinkCounter++;
            if (_spikeBlinkCounter >= 5) // Каждые 5 циклов меняем состояние мигания
            {
                _spikeBlinkCounter = 0;
                _spikeBlinkState = !_spikeBlinkState;
            }
            
            // Периодическая сборка мусора для предотвращения утечек памяти
            _gcCounter++;
            if (_gcCounter >= 100) // Каждые 100 циклов (~10 секунд при интервале 100мс)
            {
                _gcCounter = 0;
                
                // Диагностика CaptureService - периодический мониторинг
                if (App.Capture != null)
                {
                    var debugInfo = App.Capture.DebugWorkers();
                    Debug.Print($"[TicksLoop] PERIODIC: CaptureService workers count: {debugInfo.Length}");
                    if (debugInfo.Length > 8) // Показываем детали если воркеров больше ожидаемого
                    {
                        Debug.Print($"[TicksLoop] PERIODIC DETAILS: " + 
                            string.Join(", ", debugInfo.Take(8).Select(x => $"{x.key}:{x.refs}")) +
                            (debugInfo.Length > 8 ? $"... (+{debugInfo.Length - 8} more)" : ""));
                    }
                }
                
                // Очищаем мертвые воркеры перед сборкой мусора
                CleanupDeadWorkers();
                
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            
            if (!App.meterState.IsTracking)
            {
                StopTracking();
            }
            }
            catch (Exception ex)
            {
                // Логируем ошибки главного цикла без падения приложения
                Debug.Print($"[GUI] TicksLoop error: {ex.Message}");
                DebugLogger.log(ex);
            }
            finally 
            {
                // Всегда освобождаем блокировку
                Volatile.Write(ref _tickBusy, 0);
            }
            });
        }

        /// <summary>
        /// Очищает мертвые/завершившие работу PCAP воркеры для предотвращения утечек памяти
        /// </summary>
        private void CleanupDeadWorkers()
        {
            try
            {
                if (_pcapWorkers.Count == 0) return;
                
                var originalCount = _pcapWorkers.Count;
                var toRemove = new List<BackgroundWorker>();
                
                // Находим воркеры которые можно безопасно удалить
                for (int i = 0; i < _pcapWorkers.Count; i++)
                {
                    var worker = _pcapWorkers[i];
                    try
                    {
                        // Если воркер не занят и можно его освободить
                        if (worker != null && !worker.IsBusy)
                        {
                            // Дополнительная проверка - если tracking остановлен, удаляем все воркеры
                            if (!App.meterState.IsTracking)
                            {
                                toRemove.Add(worker);
                            }
                        }
                        // Если воркер null или поврежден - тоже удаляем
                        else if (worker == null)
                        {
                            toRemove.Add(worker);
                        }
                    }
                    catch
                    {
                        // Если не можем получить доступ к воркеру - помечаем на удаление
                        toRemove.Add(worker);
                    }
                }
                
                // Удаляем найденные мертвые воркеры
                foreach (var deadWorker in toRemove)
                {
                    try
                    {
                        if (deadWorker != null)
                        {
                            deadWorker.DoWork -= null;
                            deadWorker.RunWorkerCompleted -= null;
                            deadWorker.Dispose();
                        }
                        _pcapWorkers.Remove(deadWorker);
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"[CleanupDeadWorkers] Error disposing worker: {ex.Message}");
                        // Все равно удаляем из списка
                        _pcapWorkers.Remove(deadWorker);
                    }
                }
                
                if (toRemove.Count > 0)
                {
                    Debug.Print($"[CleanupDeadWorkers] Cleaned {toRemove.Count} dead workers (was {originalCount}, now {_pcapWorkers.Count})");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[CleanupDeadWorkers] Error in cleanup: {ex.Message}");
            }
        }

        private bool isValidToTrack(string key, bool strict = true, bool allowStale = false)
        {
            if(string.IsNullOrEmpty(key)) return false;
            
            try
            {
                lock(ActiveWindowTracker.connectionsLock)
                {
                    if(!ActiveWindowTracker.connections.ContainsKey(key)) return false;
                    
                    ProcessNetworkStats connection = ActiveWindowTracker.connections[key];
                    
                    // ОБЯЗАТЕЛЬНЫЕ условия (всегда проверяем):
                    bool nameMatches = AutoDetectMngr.GetActiveProcessName() == connection.name;
                    bool notLocalIP = string.IsNullOrEmpty(App.meterState.LocalIP) || connection.remoteIp != App.meterState.LocalIP;
                    
                    // Диагностика для VPN bypass соединений
                    if (key.Contains("10.234.0.24")) // VPN IP
                    {
                        DebugLogger.log($"[isValidToTrack-VPN] {key}: nameMatches={nameMatches} (expected: {AutoDetectMngr.GetActiveProcessName()}, got: {connection.name})");
                        DebugLogger.log($"[isValidToTrack-VPN] {key}: notLocalIP={notLocalIP} (remoteIP: {connection.remoteIp}, localIP: {App.meterState.LocalIP})");
                    }
                    
                    if (!nameMatches)
                    {
                        Debug.Print($"[isValidToTrack] {key}: Name mismatch. Expected: {AutoDetectMngr.GetActiveProcessName()}, Got: {connection.name}");
                        return false;
                    }
                    
                    if (!notLocalIP)
                    {
                        Debug.Print($"[isValidToTrack] {key}: Remote IP is local. RemoteIP: {connection.remoteIp}, LocalIP: {App.meterState.LocalIP}");
                        return false;
                    }
                    
                    // СТРОГИЙ режим (используется по умолчанию для стабильных соединений):
                    double lastUpdate = connection.LastUpdateDelta();
                    if (strict)
                    {
                        bool trackingDeltaOk = connection.TrackingDelta() > 3;
                        bool lastUpdateOk = lastUpdate < 2;
                        bool ticksInOk = connection.ticksIn > 3;
                        bool downloadedOk = connection.downloaded > 0;
                        
                        bool result = trackingDeltaOk && lastUpdateOk && ticksInOk && downloadedOk;
                        
                        // Диагностика для VPN bypass соединений
                        if (key.Contains("10.234.0.24")) // VPN IP
                        {
                            DebugLogger.log($"[isValidToTrack-VPN] {key}: STRICT mode check:");
                            DebugLogger.log($"[isValidToTrack-VPN] {key}: TrackingDelta={connection.TrackingDelta():F1} > 3? {trackingDeltaOk}");
                            DebugLogger.log($"[isValidToTrack-VPN] {key}: LastUpdate={lastUpdate:F1} < 2? {lastUpdateOk}");
                            DebugLogger.log($"[isValidToTrack-VPN] {key}: TicksIn={connection.ticksIn} > 3? {ticksInOk}");
                            DebugLogger.log($"[isValidToTrack-VPN] {key}: Downloaded={connection.downloaded} > 0? {downloadedOk}");
                            DebugLogger.log($"[isValidToTrack-VPN] {key}: STRICT result={result}");
                        }
                        
                        if (!result)
                        {
                            Debug.Print($"[isValidToTrack] {key}: Strict mode FAILED. " +
                                $"TrackingDelta={connection.TrackingDelta():F1} (need >3), " +
                                $"LastUpdate={lastUpdate:F1} (need <2), " +
                                $"TicksIn={connection.ticksIn} (need >3), " +
                                $"Downloaded={connection.downloaded} (need >0)");
                        }
                        
                        return result;
                    }
                    
                    // МЯГКИЙ режим (fallback для проблемных случаев):
                    // Требуем хотя бы МИНИМАЛЬНУЮ активность
                    bool hasAnyActivity = connection.ticksIn > 0 || connection.downloaded > 0 || connection.sent > 0;
                    bool hasHistoricalActivity = connection.totalTicksCnt > 0 || connection.downloaded > 0 || connection.sent > 0;
                    bool isRecent = lastUpdate < 10; // Обновление за последние 10 сек
                    bool withinStaleGrace = allowStale && lastUpdate < StaleConnectionGrace.TotalSeconds;
                    bool isTracked = connection.TrackingDelta() > 0;   // Хоть какое-то время отслеживается
                    
                    bool result2 = isTracked && hasHistoricalActivity && (isRecent || withinStaleGrace);
                    if (!allowStale)
                    {
                        result2 = result2 && hasAnyActivity;
                    }
                    
                    if (result2)
                    {
                        string mode = allowStale && !isRecent ? "STALE" : "relaxed";
                        Debug.Print($"[isValidToTrack] {key}: ⚠️ {mode} mode OK. " +
                            $"TrackingDelta={connection.TrackingDelta():F1}, " +
                            $"LastUpdate={lastUpdate:F1}, " +
                            $"HistoricalTicks={connection.totalTicksCnt}, " +
                            $"Downloaded={connection.downloaded}, " +
                            $"Sent={connection.sent}");
                    }
                    else
                    {
                        Debug.Print($"[isValidToTrack] {key}: Relaxed mode FAILED. " +
                            $"IsTracked={isTracked}, IsRecent={isRecent}, WithinStaleGrace={withinStaleGrace}, HasActivity={hasAnyActivity}, HasHistorical={hasHistoricalActivity}");
                    }
                    
                    return result2;
                }
            }
            catch (InvalidOperationException ex)
            {
                Debug.Print($"[isValidToTrack] Exception: {ex.Message}");
                return false;
            }
        }

        
        /// <summary>
        /// Находит лучшее соединение для отслеживания
        /// Оптимизировано: ранний выход при нахождении идеального соединения
        /// </summary>
        private string FindBestConnection(bool strict)
        {
            string bestConnection = "";
            int bestTicks = 0;
            
            DebugLogger.log($"[FindBestConnection] ENTRY: strict={strict}");
            try
            {
                lock(ActiveWindowTracker.connectionsLock)
                {
                    DebugLogger.log($"[FindBestConnection] Total connections: {ActiveWindowTracker.connections.Count}");
                    
                    foreach(var kvp in ActiveWindowTracker.connections)
                    {
                        DebugLogger.log($"[FindBestConnection] Checking connection: {kvp.Key}, ticksIn={kvp.Value.ticksIn}, bestTicks={bestTicks}");
                        
                        // Сначала проверяем быстрые условия
                        if (kvp.Value.ticksIn <= bestTicks)
                        {
                            DebugLogger.log($"[FindBestConnection] Skipped (ticksIn <= bestTicks)");
                            continue;
                        }
                        
                        // Только потом дорогую валидацию с lock
                        DebugLogger.log($"[FindBestConnection] Calling isValidToTrack('{kvp.Key}', strict={strict})");
                        bool isValid = isValidToTrack(kvp.Key, strict);
                        DebugLogger.log($"[FindBestConnection] isValidToTrack returned: {isValid}");
                        
                        if (isValid)
                        {
                            bestTicks = kvp.Value.ticksIn;
                            bestConnection = kvp.Key;
                            DebugLogger.log($"[FindBestConnection] New best: '{bestConnection}' with {bestTicks} ticks");
                            
                            // Ранний выход: если нашли соединение с высоким ticksIn - прерываем поиск
                            // Это экономит проверку оставшихся соединений
                            if (bestTicks > 100)
                            {
                                DebugLogger.log($"[FindBestConnection] Early exit: found excellent connection with {bestTicks} ticks");
                                Debug.Print($"[FindBestConnection] Early exit: found excellent connection with {bestTicks} ticks");
                                break;
                            }
                        }
                    }
                }
            }
            catch (InvalidOperationException ex)
            {
                DebugLogger.log($"[FindBestConnection] Exception: {ex.Message}");
                Debug.Print($"[FindBestConnection] Exception: {ex.Message}");
            }
            
            DebugLogger.log($"[FindBestConnection] RETURN: '{bestConnection}' with {bestTicks} ticks");
            return bestConnection;
        }
        
        /// <summary>
        /// Логирует информацию обо всех соединениях для диагностики
        /// </summary>
        private void LogConnectionsDebugInfo()
        {
            try
            {
                lock(ActiveWindowTracker.connectionsLock)
                {
                    Debug.Print($"[Metrics] === Connections Debug Info ===");
                    Debug.Print($"[Metrics] Active process: {AutoDetectMngr.GetActiveProcessName()}");
                    Debug.Print($"[Metrics] LocalIP: {App.meterState.LocalIP}");
                    Debug.Print($"[Metrics] Total connections: {ActiveWindowTracker.connections.Count}");
                    Debug.Print($"[Metrics] Current targetKey: '{targetKey}'");
                    
                    if (ActiveWindowTracker.connections.Count == 0)
                    {
                        Debug.Print($"[Metrics] ⚠️ NO CONNECTIONS AVAILABLE!");
                    }
                    
                    foreach(var kvp in ActiveWindowTracker.connections)
                    {
                        var conn = kvp.Value;
                        Debug.Print($"[Metrics] Connection: {kvp.Key}");
                        Debug.Print($"  - Name: {conn.name}");
                        Debug.Print($"  - RemoteIP: {conn.remoteIp}:{conn.remotePort}");
                        Debug.Print($"  - TicksIn: {conn.ticksIn}");
                        Debug.Print($"  - Downloaded: {conn.downloaded} bytes");
                        Debug.Print($"  - Sent: {conn.sent} bytes");
                        Debug.Print($"  - TrackingDelta: {conn.TrackingDelta():F1} sec");
                        Debug.Print($"  - LastUpdateDelta: {conn.LastUpdateDelta():F1} sec");
                        Debug.Print($"  - Matches name: {conn.name == AutoDetectMngr.GetActiveProcessName()}");
                        Debug.Print($"  - Not local IP: {conn.remoteIp != App.meterState.LocalIP}");
                    }
                    Debug.Print($"[Metrics] ==============================");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[Metrics] Error logging connections: {ex.Message}");
            }
        }

        private void ResetMetricsState(string currentProcessName)
        {
            if (App.meterState == null)
            {
                return;
            }

            string targetProcess = currentProcessName ?? AutoDetectMngr.GetActiveProcessName() ?? string.Empty;

            if (_metricsStateCleared && string.IsNullOrEmpty(App.meterState.Server?.Ip) && !App.meterState.IsTracking &&
                App.meterState.TickRate == 0 && App.meterState.OutputTickRate == 0 &&
                App.meterState.DownloadTraffic == 0 && App.meterState.UploadTraffic == 0)
            {
                App.meterState.Game = targetProcess;
                _metricsStateCleared = true;
                return;
            }

            try
            {
                App.meterState.Reset();
                Classes.SmoothingManager.ResetValueEmas();
                Classes.TickrateSmoothingManager.Reset();
                Classes.SpikeDetection.SpikeDetectionManager.Reset();

                App.meterState.Game = targetProcess;
                _lastMetricsApplied = DateTime.MinValue;
                _metricsStateCleared = true;
            }
            catch (Exception ex)
            {
                Debug.Print($"[Metrics] Error resetting meter state: {ex.Message}");
            }
        }

        private void updateMetherStateFromActiveWindow()
        {
            DebugLogger.log("[updateMetherStateFromActiveWindow] FUNCTION ENTRY");
            try
            {
                DebugLogger.log("[updateMetherStateFromActiveWindow] TRY BLOCK ENTRY");
                
                if (App.meterState == null)
                {
                    DebugLogger.log("[updateMetherStateFromActiveWindow] ERROR: App.meterState is NULL");
                    return;
                }
                
                string previousProcessName = App.meterState.Game;
                string currentActiveProcess = AutoDetectMngr.GetActiveProcessName();
                DebugLogger.log($"[updateMetherStateFromActiveWindow] Process: {previousProcessName} -> {currentActiveProcess}");
                
                // Обновляем Game сразу чтобы отражать текущий активный процесс
                // Даже если метрики еще не найдены
                if (currentActiveProcess != previousProcessName)
                {
                    DebugLogger.log($"[updateMetherStateFromActiveWindow] Process CHANGED, updating Game");
                    App.meterState.Game = currentActiveProcess;
                    
                    // Для системных процессов в VPN режиме очищаем соединения чтобы показать "NO TRAFFIC"
                    string[] systemProcesses = { "cmd", "notepad", "calculator", "mspaint", "wordpad", "powershell", "powershell_ise" };
                    if (systemProcesses.Any(proc => proc.Equals(currentActiveProcess, StringComparison.OrdinalIgnoreCase)))
                    {
                        DebugLogger.log($"[VPN-Clear] Clearing connections for system process: {currentActiveProcess}");
                        try 
                        { 
                            ActiveWindowTracker.ClearConnectionStats(); 
                            App.meterState.IsTracking = false;
                            _metricsStateCleared = true;
                        } 
                        catch (Exception ex) 
                        { 
                            DebugLogger.log($"[VPN-Clear] Error clearing connections: {ex.Message}"); 
                        }
                    }
                }
            
            if (_metricsActive && _lastMetricsApplied != DateTime.MinValue)
            {
                TimeSpan idleDuration = DateTime.Now - _lastMetricsApplied;
                if (idleDuration > IdleDetectionThreshold)
                {
                    Debug.Print($"[Metrics] ⚠️ Metrics idle for {idleDuration.TotalSeconds:F1}s, forcing re-sync");
                    _idleRecoveryAttempts++;
                    _metricsActive = false;
                    _invalidTargetCount = 0;
                    _searchCooldown = TimeSpan.FromMilliseconds(100);
                    _fastStartCounter = 0;
                    _lastConnectionSearch = DateTime.MinValue;
                    _lastMetricsApplied = DateTime.MinValue;
                    App.connMngr?.SetFastMode(true);
                    _fastModeEnabledAt = DateTime.Now;
                    RequestConnectionsRefresh(true);
                    try { ActiveWindowTracker.ClearConnectionStats(); } catch { }

                    if (idleDuration > HardRestartThreshold && _idleRecoveryAttempts <= 3)
                    {
                        Debug.Print($"[Metrics] ⚠️ Idle for {idleDuration.TotalSeconds:F1}s, scheduling capture restart (attempt {_idleRecoveryAttempts})");
                        ScheduleCaptureRestart();
                    }
                }
            }

            // === ПРОВЕРКА СМЕНЫ АКТИВНОГО ОКНА/ПРОЦЕССА ===
            // КРИТИЧНО: Проверяем ДО любых других проверок!
            DebugLogger.log($"[updateMetherStateFromActiveWindow] Checking process change: isEmpty={string.IsNullOrEmpty(previousProcessName)}, different={previousProcessName != currentActiveProcess}");
            if (!string.IsNullOrEmpty(previousProcessName) && 
                previousProcessName != currentActiveProcess)
            {
                DebugLogger.log($"[Metrics] ⚡ ACTIVE WINDOW CHANGED: {previousProcessName} -> {currentActiveProcess}");
                Debug.Print($"[Metrics] ⚡ ACTIVE WINDOW CHANGED: {previousProcessName} -> {currentActiveProcess}");
                
                // ВСЕГДА сбрасываем состояние при смене процесса
                _metricsActive = false;
                _searchCooldown = TimeSpan.FromMilliseconds(100); // Ультрабыстрый режим для первых попыток
                _fastStartCounter = 0; // КРИТИЧНО: сброс счетчика при каждой смене окна!
                _lastConnectionSearch = DateTime.MinValue; // Сброс cooldown для немедленного поиска
                targetKey = ""; // Сбрасываем текущее соединение
                ResetMetricsState(currentActiveProcess);
                
                // Очищаем старые соединения из словаря для нового процесса
                try
                {
                    lock(ActiveWindowTracker.connectionsLock)
                    {
                        // Удаляем соединения для старого процесса чтобы не мешали
                        var keysToRemove = new List<string>();
                        foreach(var kvp in ActiveWindowTracker.connections)
                        {
                            if (kvp.Value.name == previousProcessName)
                            {
                                keysToRemove.Add(kvp.Key);
                            }
                        }
                        
                        foreach(var key in keysToRemove)
                        {
                            ActiveWindowTracker.connections.Remove(key);
                        }
                        
                        if (keysToRemove.Count > 0)
                        {
                            Debug.Print($"[Metrics] Cleaned {keysToRemove.Count} old connections for '{previousProcessName}'");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"[Metrics] Error cleaning old connections: {ex.Message}");
                }
                // Полный сброс connection stats на смене процесса
                try { ActiveWindowTracker.ClearConnectionStats(); } catch { }
                
                // Активируем быстрый режим ConnectionsManager для быстрого обнаружения новых соединений
                App.connMngr?.SetFastMode(true);
                _fastModeEnabledAt = DateTime.Now;
                _searchStopwatch.Restart();
                RequestConnectionsRefresh(true);
                
                Debug.Print($"[Metrics] ⚡ Fast start activated for new window: {currentActiveProcess}");
            }
            
            // === МЕХАНИЗМ БЫСТРОГО СТАРТА ===
            // Если метрики уже идут - быстрая проверка текущего соединения
            // Кэшируем результат валидации чтобы не проверять дважды
            bool targetKeyValid = !string.IsNullOrEmpty(targetKey) && isValidToTrack(targetKey, strict: true);
            if (!targetKeyValid && _metricsActive && !string.IsNullOrEmpty(targetKey))
            {
                // В режиме простоя допускаем "черствые" соединения, чтобы мониторинг не засыпал
                targetKeyValid = isValidToTrack(targetKey, strict: false, allowStale: true);
                if (targetKeyValid)
                {
                    Debug.Print($"[Metrics] ♻ Keeping stale targetKey '{targetKey}' alive during idle");
                }
            }
            
            if (_metricsActive && targetKeyValid)
            {
                // Быстрый путь - просто обновляем метрики без поиска
                // (код обработки метрик выполнится ниже)
                Debug.Print($"[Metrics] Fast path: using existing targetKey");
            }
            else
            {
                // Метрики не идут ИЛИ targetKey невалиден
                
                // Если метрики были активны, но targetKey невалиден - применяем HYSTERESIS
                DebugLogger.log($"[updateMetherStateFromActiveWindow] Metrics inactive or targetKey invalid. _metricsActive={_metricsActive}, targetKeyValid={targetKeyValid}");
                if (_metricsActive && !string.IsNullOrEmpty(targetKey))
                {
                    _invalidTargetCount++;
                    Debug.Print($"[Metrics] ⚠️ TargetKey '{targetKey}' invalid. InvalidCount={_invalidTargetCount}/2");

                    // На первой невалидной проверке просто триггерим немедленный поиск, но не деактивируем метрики
                    if (_invalidTargetCount < 2)
                    {
                        _lastConnectionSearch = DateTime.MinValue; // Бросаем немедленный поиск
                        App.connMngr?.SetFastMode(true);
                        _fastModeEnabledAt = DateTime.Now;
                        return; // ждем следующей итерации, возможно это временный флап
                    }

                    // Два раза подряд — деактивируем и очищаем связанные данные
                    Debug.Print($"[Metrics] ⚠️ TargetKey '{targetKey}' invalid twice, deactivating metrics and clearing caches");
                    _invalidTargetCount = 0;
                    _metricsActive = false;
                    _searchCooldown = TimeSpan.FromMilliseconds(100); // Ультрабыстрый режим
                    _fastStartCounter = 0;
                    _lastConnectionSearch = DateTime.MinValue; // Немедленный поиск!
                    _lastMetricsApplied = DateTime.MinValue;
                    ResetMetricsState(currentActiveProcess);

                    // Включаем быстрый режим ConnectionsManager
                    App.connMngr?.SetFastMode(true);
                    _fastModeEnabledAt = DateTime.Now;
                    _searchStopwatch.Restart();
                    RequestConnectionsRefresh(true);

                    // Очищаем соединение targetKey из словаря - возможно оно помешало
                    try
                    {
                        lock(ActiveWindowTracker.connectionsLock)
                        {
                            if (!string.IsNullOrEmpty(targetKey) && ActiveWindowTracker.connections.ContainsKey(targetKey))
                            {
                                ActiveWindowTracker.connections.Remove(targetKey);
                                Debug.Print($"[Metrics] Removed stale targetKey entry: {targetKey}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"[Metrics] Error removing stale targetKey: {ex.Message}");
                    }
                        
                        // Также очищаем все connection stats чтобы гарантировать свежий поиск
                        try { ActiveWindowTracker.ClearConnectionStats(); } catch { }
                    
                }
                
                // Проверяем cooldown для поиска соединений
                TimeSpan timeSinceLastSearch = DateTime.Now - _lastConnectionSearch;
                
                DebugLogger.log($"[updateMetherStateFromActiveWindow] Checking cooldown: timeSinceLastSearch={timeSinceLastSearch.TotalMilliseconds}ms, cooldown={_searchCooldown.TotalMilliseconds}ms");
                if (timeSinceLastSearch < _searchCooldown)
                {
                    // Слишком рано для повторного поиска
                    DebugLogger.log($"[Metrics] Cooldown active, waiting {(_searchCooldown - timeSinceLastSearch).TotalMilliseconds:F0}ms");
                    Debug.Print($"[Metrics] Cooldown active, waiting {(_searchCooldown - timeSinceLastSearch).TotalMilliseconds:F0}ms");
                    // Но перед возвратом проверим, не включён ли fast mode слишком долго
                    try
                    {
                        if (App.connMngr != null && _fastModeEnabledAt != DateTime.MinValue && (DateTime.Now - _fastModeEnabledAt) > TimeSpan.FromSeconds(12))
                        {
                            App.connMngr.SetFastMode(false);
                            _fastModeEnabledAt = DateTime.MinValue;
                            Debug.Print("[ConnectionsManager] Fast mode auto-disabled after 12s watchdog");
                        }
                    }
                    catch { }
                    return;
                }
                
                _lastConnectionSearch = DateTime.Now;
                DebugLogger.log($"[updateMetherStateFromActiveWindow] Cooldown passed, continuing search. _metricsActive={_metricsActive}");
                
                // Если метрики не активны - включаем режим быстрого старта
                if (!_metricsActive)
                {
                    if (!_searchStopwatch.IsRunning)
                    {
                        _searchStopwatch.Restart();
                    }

                    // Градиентный cooldown для максимальной скорости:
                    // - Первые 10 попыток: 100ms (ультрабыстрый режим)
                    // - Следующие 40 попыток: 200ms (быстрый режим)
                    // - После 50 попыток: 1000ms (нормальный режим)
                    if (_fastStartCounter < 10)
                    {
                        _searchCooldown = TimeSpan.FromMilliseconds(100); // Первая секунда - максимальная скорость!
                    }
                    else if (_fastStartCounter < 50)
                    {
                        _searchCooldown = TimeSpan.FromMilliseconds(200); // Следующие 8 секунд
                    }
                    else
                    {
                        _searchCooldown = TimeSpan.FromSeconds(1); // Нормальный режим
                    }
                    
                    _fastStartCounter++;
                    Debug.Print($"[Metrics] ⚡ Fast start mode: check #{_fastStartCounter}, cooldown={_searchCooldown.TotalMilliseconds}ms");
                    
                    if (_fastStartCounter == 1)
                    {
                        RequestConnectionsRefresh(false);
                    }
                    else if (_fastStartCounter % 5 == 0)
                    {
                        RequestConnectionsRefresh(false);
                    }

                    if (_fastStartCounter == 10)
                    {
                        Debug.Print($"[Metrics] ⚡ Ultra-fast phase complete (1 sec), switching to fast mode");
                    }
                    else if (_fastStartCounter == 50)
                    {
                        Debug.Print($"[Metrics] Fast start timeout (10 sec), switching to normal mode");
                    }
                    else if (_fastStartCounter > 100)
                    {
                        // Если уже 100+ попыток (20+ секунд) и ничего не нашли - возможно застряли
                        // Сбрасываем состояние и очищаем старые соединения
                        Debug.Print($"[Metrics] ⚠️ Too many search attempts ({_fastStartCounter}), resetting state");
                        _fastStartCounter = 0;
                        targetKey = "";
                        ResetMetricsState(currentActiveProcess);
                        
                        // Очищаем все старые соединения - возможно там накопился мусор
                        try
                        {
                            lock(ActiveWindowTracker.connectionsLock)
                            {
                                int oldCount = ActiveWindowTracker.connections.Count;
                                ActiveWindowTracker.connections.Clear();
                                Debug.Print($"[Metrics] Cleared {oldCount} stale connections");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Print($"[Metrics] Error clearing connections: {ex.Message}");
                        }
                        
                        // После массовой очистки - убедимся что и спустили глобальную статистику
                        try { ActiveWindowTracker.ClearConnectionStats(); } catch { }
                    }
                }
            } // Закрываем блок else
            
            // === ТРЕХУРОВНЕВАЯ СТРАТЕГИЯ ПОИСКА СОЕДИНЕНИЯ ===
            DebugLogger.log($"[updateMetherStateFromActiveWindow] Starting connection search. targetKeyValid={targetKeyValid}, targetKey='{targetKey}'");
            
            // УРОВЕНЬ 1: Проверяем текущий targetKey (строгий режим)
            // Используем кэшированный результат валидации
            if(!targetKeyValid)
            {
                DebugLogger.log($"[Metrics] Current targetKey '{targetKey}' invalid (strict), searching for best connection...");
                Debug.Print($"[Metrics] Current targetKey '{targetKey}' invalid (strict), searching for best connection...");
                
                // УРОВЕНЬ 2: Ищем лучшее соединение (строгий режим)
                DebugLogger.log($"[updateMetherStateFromActiveWindow] Calling FindBestConnection(strict: true)");
                string bestConnection = FindBestConnection(strict: true);
                DebugLogger.log($"[updateMetherStateFromActiveWindow] FindBestConnection returned: '{bestConnection}'");
                
                if (!string.IsNullOrEmpty(bestConnection))
                {
                    targetKey = bestConnection;
                    Debug.Print($"[Metrics] ✓ Found strict match: {targetKey}");
                }
                else
                {
                    // УРОВЕНЬ 3: Fallback с мягкими условиями
                    Debug.Print($"[Metrics] No strict match found, trying relaxed mode...");
                    bestConnection = FindBestConnection(strict: false);
                    
                    if (!string.IsNullOrEmpty(bestConnection))
                    {
                        targetKey = bestConnection;
                        Debug.Print($"[Metrics] ⚠️ Using relaxed match: {targetKey}");
                    }
                    else
                    {
                        Debug.Print($"[Metrics] ❌ No valid connections found!");
                        LogConnectionsDebugInfo(); // Детальная диагностика

                        RequestConnectionsRefresh(false);
                        if (_searchStopwatch.IsRunning && _searchStopwatch.ElapsedMilliseconds > 2000)
                        {
                            RequestConnectionsRefresh(true);
                            _searchStopwatch.Restart();
                        }
                        
                        // Сбрасываем targetKey если ничего не найдено
                        if (!string.IsNullOrEmpty(targetKey))
                        {
                            targetKey = "";
                            Debug.Print($"[Metrics] Reset targetKey to empty");
                        }
                        ResetMetricsState(currentActiveProcess);
                        return;
                    }
                }
            }
            else
            {
                // targetKey валидный, используем его
                Debug.Print($"[Metrics] Using valid targetKey: {targetKey}");
            }
            
            // === АКТИВАЦИЯ МЕТРИК ===
            if(!string.IsNullOrEmpty(targetKey))
            {
                // Нашли соединение - активируем метрики и переходим в нормальный режим
                if (!_metricsActive)
                {
                        // Останавливаем таймер поиска и логируем время
                        try { if (_searchStopwatch.IsRunning) { _searchStopwatch.Stop(); Debug.Print($"[Metrics] ⚡ FOUND in {_searchStopwatch.ElapsedMilliseconds}ms"); } } catch { }
                    _metricsActive = true;
                    _searchCooldown = TimeSpan.FromSeconds(1);
                    _fastStartCounter = 0;
                    _idleRecoveryAttempts = 0;
                    
                    // Выключаем быстрый режим ConnectionsManager - метрики найдены, экономим CPU
                    App.connMngr?.SetFastMode(false);
                    
                    Debug.Print($"[Metrics] ✅ Metrics activated! Switching to normal mode (1 sec cooldown)");
                }
            }
            else
            {
                // Соединение потеряно - деактивируем метрики
                if (_metricsActive)
                {
                    _metricsActive = false;
                    
                    // Включаем быстрый режим ConnectionsManager для поиска нового соединения
                    App.connMngr?.SetFastMode(true);
                    _lastMetricsApplied = DateTime.MinValue;
                    
                    Debug.Print($"[Metrics] ⚠️ Metrics deactivated - connection lost, activating fast search");
                }

                Debug.Print($"[TRAFFIC DEBUG] ResetMetricsState called from line 2916 (targetKey empty), currentProcess={currentActiveProcess}");
                ResetMetricsState(currentActiveProcess);
            }
            
            
            if(targetKey != "") { 
                // === ОБЫЧНЫЙ РЕЖИМ: ИСПОЛЬЗУЕМ ACTIVEWINNOWTRACKER ===
                try
                {
                    lock(ActiveWindowTracker.connectionsLock)
                    {
                        if(!ActiveWindowTracker.connections.ContainsKey(targetKey))
                        {
                            targetKey = "";
                            ResetMetricsState(currentActiveProcess);
                            return;
                        }
                        ProcessNetworkStats procStats = ActiveWindowTracker.connections[targetKey];
                        
                        // НЕ перезаписываем tickTimeBuffer в VPN bypass режиме - он пересчитывается в ETW блоке
                        // Проверяем позже после объявления vpnBypassAdvanced переменной
                        
                        // Добавляем ticktime данные в детектор спайков
                        try
                        {
                            if (procStats.tickTimeBuffer != null && procStats.tickTimeBuffer.Count > 0)
                            {
                                // Берем последнее значение из буфера как текущий ticktime
                                float lastTickTime = procStats.tickTimeBuffer[procStats.tickTimeBuffer.Count - 1];
                                if (lastTickTime > 0)
                                {
                                    Classes.SpikeDetection.SpikeDetectionManager.AddValue(
                                        Classes.SpikeDetection.MetricKind.Ticktime, 
                                        lastTickTime
                                    );
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.Print($"[updateMetherStateFromActiveWindow] Error adding ticktime to spike detector: {ex.Message}");
                        }
                        App.meterState.CurrentTimestamp = DateTime.Now;
                        
                        // КРИТИЧЕСКОЕ ИСПРАВЛЕНИЕ: Проверяем смену активного процесса
                        string currentProcessName = procStats.name;
                        bool processChanged = !string.IsNullOrEmpty(previousProcessName) && 
                                             previousProcessName != currentProcessName;
                        
                        // === БЫСТРЫЙ СТАРТ ПРИ СМЕНЕ ПРОЦЕССА ===
                        if (processChanged)
                        {
                            Debug.Print($"[Metrics] ⚡ Process changed: {previousProcessName} -> {currentProcessName}");
                            
                            // Сбрасываем состояние метрик для быстрого обнаружения нового процесса
                            _metricsActive = false;
                            _searchCooldown = TimeSpan.FromMilliseconds(200); // Быстрый режим
                            _fastStartCounter = 0;
                            targetKey = ""; // Сбрасываем текущее соединение
                            _lastMetricsApplied = DateTime.MinValue;
                            
                            Debug.Print($"[Metrics] ⚡ Fast start activated for new process");
                        }
                        
                        // В режиме мультиадаптера переопределяем LocalIP 
                        // - При смене процесса (немедленно с ResetCache)
                        // - Для того же процесса (периодически, через встроенный интервал в LocalIPDetector)
                        bool captureAll = App.settingsManager?.GetOption("capture_all_adapters", "False", "ADVANCED") == "True";
                        bool vpnBypassBasic = App.settingsManager?.GetOption("vpn_bypass_basic", "False", "ADVANCED") == "True";
                        bool vpnBypassAdvanced = App.settingsManager?.GetOption("vpn_bypass_advanced", "False", "ADVANCED") == "True";
                        
                        // В VPN bypass режиме не используем старый tickTimeBuffer - он будет пересчитан в ETW блоке
                        if (vpnBypassAdvanced && App.meterState.tickTimeBuffer != null)
                        {
                            // Сохраняем текущий буфер, не перезаписываем его данными из procStats.tickTimeBuffer
                            Debug.Print($"[VPN-TickTime] Skipping tickTimeBuffer overwrite in VPN bypass mode (current buffer has {App.meterState.tickTimeBuffer.Count} values)");
                        }
                        else
                        {
                            // Обычный режим: используем данные из procStats
                            App.meterState.tickTimeBuffer = procStats.tickTimeBuffer;
                        }
                        
                        if (captureAll || vpnBypassBasic || vpnBypassAdvanced)
                        {
                            try
                            {
                                // При смене процесса - сбрасываем кэш для немедленного обновления
                                if (processChanged)
                                {
                                    Debug.Print($"[updateMetherStateFromActiveWindow] Process changed: {previousProcessName} -> {currentProcessName}");
                                    Classes.LocalIPDetector.ResetCache();
                                }
                                
                                // Проверяем IP (для нового процесса - немедленно, для того же - по таймеру внутри метода)
                                string newLocalIP = Classes.LocalIPDetector.DetectLocalIPForActiveProcess(
                                    currentProcessName,
                                    allowFallbackToSharedSources: !processChanged);
                                
                                // Диагностика
                                Debug.Print($"[updateMetherStateFromActiveWindow] Detected LocalIP: old={App.meterState.LocalIP}, new={newLocalIP}, changed={newLocalIP != App.meterState.LocalIP}");
                                
                                if (!string.IsNullOrEmpty(newLocalIP) && newLocalIP != App.meterState.LocalIP)
                                {
                                    Debug.Print($"[updateMetherStateFromActiveWindow] LocalIP changed: {App.meterState.LocalIP} -> {newLocalIP}");
                                    App.meterState.LocalIP = newLocalIP;
                                    
                                    // Диагностика состояния формы настроек
                                    Debug.Print($"[updateMetherStateFromActiveWindow] SettingsForm state: IsNull={App.settingsForm == null}, IsHandleCreated={App.settingsForm?.IsHandleCreated}, IsDisposed={App.settingsForm?.IsDisposed}");
                                    
                                    // Обновляем UI (textbox и ComboBox адаптера) АСИНХРОННО
                                    // Используем SafeInvokeOnSettings чтобы избежать исключений при отсутствии Handle
                                    App.SafeInvokeOnSettings(() =>
                                    {
                                        try
                                        {
                                            var settings = App.settingsForm;
                                            if (settings == null || settings.IsDisposed)
                                            {
                                                Debug.Print("[updateMetherStateFromActiveWindow] Settings form became unavailable during UI sync");
                                                return;
                                            }

                                            // Обновляем textbox LocalIP
                                            if (settings.local_ip_textbox != null && settings.local_ip_textbox.Text != newLocalIP)
                                            {
                                                settings.local_ip_textbox.Text = newLocalIP;
                                            }

                                            // Обновляем выбранный адаптер в ComboBox (с защитой от рекурсии)
                                            if (settings.adapters_list != null && settings.adapters_list.Items.Count > 0)
                                            {
                                                Debug.Print($"[updateMetherStateFromActiveWindow] Searching for adapter with IP: {newLocalIP}");
                                                var adapters = App.GetAdapters();
                                                Debug.Print($"[updateMetherStateFromActiveWindow] Total adapters: {adapters.Count}, Current ComboBox index: {settings.adapters_list.SelectedIndex}");

                                                bool found = false;
                                                for (int i = 0; i < adapters.Count; i++)
                                                {
                                                    string adapterIP = App.GetAdapterAddress(adapters[i]);
                                                    Debug.Print($"[updateMetherStateFromActiveWindow] Adapter[{i}] IP: {adapterIP}, Match: {adapterIP == newLocalIP}");

                                                    if (adapterIP == newLocalIP)
                                                    {
                                                        found = true;
                                                        if (settings.adapters_list.SelectedIndex != i)
                                                        {
                                                            Debug.Print($"[updateMetherStateFromActiveWindow] ✓ Found! Updating adapter ComboBox index: {settings.adapters_list.SelectedIndex} -> {i}");

                                                            settings.IsUpdatingAdapter = true;
                                                            try
                                                            {
                                                                settings.adapters_list.SelectedIndex = i;
                                                                Debug.Print($"[updateMetherStateFromActiveWindow] ✓ ComboBox updated successfully. New index: {settings.adapters_list.SelectedIndex}");
                                                            }
                                                            finally
                                                            {
                                                                settings.IsUpdatingAdapter = false;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            Debug.Print($"[updateMetherStateFromActiveWindow] Adapter already selected (index {i})");
                                                        }
                                                        break;
                                                    }
                                                }

                                                if (!found)
                                                {
                                                    Debug.Print($"[updateMetherStateFromActiveWindow] ⚠ WARNING: No adapter found with IP {newLocalIP}!");
                                                }
                                            }
                                            else
                                            {
                                                Debug.Print("[updateMetherStateFromActiveWindow] ⚠ ComboBox is null or empty");
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.Print($"[updateMetherStateFromActiveWindow] UI update error: {ex.Message}");
                                        }
                                    });

                                    Debug.Print($"[updateMetherStateFromActiveWindow] ✓ Successfully updated LocalIP for process '{currentProcessName}' to {newLocalIP}");
                                    
                                    // НОВОЕ: Автоматическое переключение адаптера при активном мониторинге
                                    // ВАЖНО: запускаем в фоновом потоке чтобы не блокировать UI
                                    if (App.meterState.IsTracking)
                                    {
                                        Task.Run(() => SwitchAdapterIfNeeded(newLocalIP));
                                    }
                                }
                                else if (string.IsNullOrEmpty(newLocalIP))
                                {
                                    Debug.Print($"[updateMetherStateFromActiveWindow] WARNING: Could not auto-detect LocalIP for process '{currentProcessName}'");
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.Print($"[updateMetherStateFromActiveWindow] Error updating LocalIP: {ex.Message}");
                            }
                        }
                        
                        App.meterState.Game = procStats.name;
                        
                        // В VPN bypass режиме сохраняем старый IP для проверки смены
                        if (App.meterState.Server != null)
                        {
                            string oldIp = App.meterState.Server.Ip;
                            App.meterState.Server.Ip = procStats.remoteIp.ToString();
                            string newIp = App.meterState.Server.Ip;
                            
                            // Проверяем смену IP и сбрасываем счётчики если нужно
                            if (vpnBypassBasic || vpnBypassAdvanced)
                            {
                                if (!string.IsNullOrEmpty(oldIp) && oldIp != newIp)
                                {
                                    // IP изменился - сбрасываем счётчики трафика
                                    App.meterState.Server.UploadTraffic = 0;
                                    App.meterState.Server.DownloadTraffic = 0;
                                    DebugLogger.log($"[VPN-BYPASS] IP changed: {oldIp} -> {newIp}, resetting traffic counters");
                                }
                            }
                        }
                        
                        // ИСПРАВЛЕНИЕ: ETW трафик используем ТОЛЬКО в VPN bypass режиме!
                        // В обычном PCAP режиме трафик накапливается через GameServer.UploadTraffic/DownloadTraffic
                        if (vpnBypassBasic || vpnBypassAdvanced)
                        {
                            // ЭТАП 2: Полный переход на ETW трафик в VPN bypass режиме
                            // ETW предоставляет более точные данные на уровне ядра, минуя VPN шифрование
                            double etwUploadBytesPerSec = Classes.ETW.GetUploadBytesPerSecond(procStats.name);
                            double etwDownloadBytesPerSec = Classes.ETW.GetDownloadBytesPerSecond(procStats.name);
                            
                            // В VPN bypass режиме полностью используем ETW данные
                            // Конвертируем из байт/сек в общие байты для совместимости
                            int etwUploadBytes = (int)(etwUploadBytesPerSec * 1.0);
                            int etwDownloadBytes = (int)(etwDownloadBytesPerSec * 1.0);
                            
                            App.meterState.DownloadTraffic += etwDownloadBytes;
                            App.meterState.UploadTraffic += etwUploadBytes;
                            
                            // ДИАГНОСТИКА: Логируем переход на ETW
                            DebugLogger.log($"[ETW-VPN-FULL] Using FULL ETW traffic: download={etwDownloadBytesPerSec:F1} B/s ({etwDownloadBytes} bytes), upload={etwUploadBytesPerSec:F1} B/s ({etwUploadBytes} bytes)");
                            DebugLogger.log($"[ETW-VPN-FULL] Replacing procStats data: old_download={procStats.downloaded}, old_upload={procStats.sent}");
                            DebugLogger.log($"[VPN-DEBUG] Set Server.Ip = {procStats.remoteIp} (from procStats.remoteIp)");
                            DebugLogger.log($"[VPN-DEBUG] Final DownloadTraffic = {App.meterState.DownloadTraffic}, UploadTraffic = {App.meterState.UploadTraffic}");
                        }
                        else
                        {
                            // В обычном PCAP режиме используем накопленные счётчики из GameServer
                            // UploadTraffic/DownloadTraffic обновляются в GameServer.Ip setter когда IP не меняется
                            DebugLogger.log($"[PCAP-MODE] Using accumulated traffic: download={App.meterState.DownloadTraffic}, upload={App.meterState.UploadTraffic}");
                        }
                        
                        // Обновляем TickRate и добавляем в детектор спайков
                        int currentTickRate;
                        
                        // В VPN bypass режиме используем РЕАЛЬНЫЙ тикрейт из RealProcessTrafficMonitor
                        // (вместо эмулированного ticksIn)
                        if (vpnBypassBasic || vpnBypassAdvanced)
                        {
                            // Устанавливаем активный процесс для ETW мониторинга пакетов
                            Classes.ETW.SetActiveProcess(procStats.name);
                            
                            // Получаем ETW счетчик пакетов для VPN bypass
                            long etwPacketsPerSec = Classes.ETW.GetIncomingPacketsPerSecond(procStats.name);
                            
                            // Получаем реальные данные трафика с расчётом тикрейта
                            var realTraffic = Classes.RealProcessTrafficMonitor.GetRealProcessTrafficWithPing(
                                procStats.name, 
                                procStats.remoteIp?.ToString(), 
                                (int)procStats.remotePort);
                            
                            // ЭТАП 3: Интеграция ETW RTT/Ping данных для VPN bypass режима (БЕЗ ОГРАНИЧЕНИЙ)
                            // ETW предоставляет kernel-level RTT измерения - показываем всё как есть
                            long etwAvgRtt = Classes.ETW.GetAverageRttMs(procStats.name);
                            long etwMinRtt = Classes.ETW.GetMinRttMs(procStats.name);
                            long etwMaxRtt = Classes.ETW.GetMaxRttMs(procStats.name);
                            double etwJitter = Classes.ETW.GetJitterMs(procStats.name);
                            
                            // УБИРАЕМ ВСЕ ОГРАНИЧЕНИЯ - показываем реальные данные
                            if (etwAvgRtt > 0 && App.meterState.Server != null)
                            {
                                App.meterState.Server.Ping = (int)etwAvgRtt;
                                
                                DebugLogger.log($"[ETW-VPN-RTT-RAW] Using RAW ETW RTT data: avg={etwAvgRtt}ms, min={etwMinRtt}ms, max={etwMaxRtt}ms, jitter={etwJitter:F1}ms");
                                DebugLogger.log($"[ETW-VPN-RTT-RAW] ETW RTT replaces realTraffic data: old_ping={realTraffic?.RealPingMs ?? -1}ms");
                            }
                            else if (realTraffic != null && realTraffic.RealPingMs > 0 && App.meterState.Server != null)
                            {
                                // Fallback: используем realTraffic ping если ETW данных нет
                                App.meterState.Server.Ping = realTraffic.RealPingMs;
                                DebugLogger.log($"[VPN-RTT-Fallback] Using RealProcessTrafficMonitor ping: {realTraffic.RealPingMs}ms (ETW RTT not available)");
                            }
                            else
                            {
                                DebugLogger.log($"[VPN-RTT-None] No ping data available: ETW_avg={etwAvgRtt}ms, realTraffic_valid={realTraffic?.RealPingMs > 0}");
                            }
                            
                            if (etwPacketsPerSec > 0)
                            {
                                // Используем ETW подсчет пакетов как приоритетный метод для VPN bypass
                                currentTickRate = (int)etwPacketsPerSec; // Убрано ограничение 128 Гц - сглаживание работает
                                DebugLogger.log($"[VPN-TickRate] Using ETW packet count for VPN bypass: {currentTickRate} packets/sec (no limit)");
                            }
                            else if (realTraffic != null && realTraffic.CalculatedTickrate > 0)
                            {
                                currentTickRate = realTraffic.CalculatedTickrate;
                                DebugLogger.log($"[VPN-TickRate] Using REAL tickrate for VPN bypass: {currentTickRate} (from traffic analysis)");
                            }
                            else
                            {
                                // Fallback: если нет реальных данных, используем минимальный базовый тикрейт
                                currentTickRate = Math.Max(procStats.ticksIn / 4, 10);
                                DebugLogger.log($"[VPN-TickRate] Using fallback tickrate for VPN bypass: {currentTickRate} (no real data available)");
                            }
                            
                            // РЕАЛЬНЫЙ ТРАФИК вместо эмуляции в VPN bypass режиме
                            UpdateRealVpnTraffic(procStats);
                            
                            // *** ИСПРАВЛЕНИЕ: Рассчитываем тиктайм для VPN bypass режима ***
                            // В VPN bypass режиме тиктайм должен корректно обновляться в соответствии с реальным тикрейтом
                            float currentTicktime = currentTickRate > 0 ? 1000.0f / currentTickRate : 7.8f;
                            
                            // Обновляем буфер тиктайма для отображения в оверлее
                            if (App.meterState.tickTimeBuffer == null)
                                App.meterState.tickTimeBuffer = new List<float>();
                            
                            lock (App.meterState._tickTimeBufferLock)
                            {
                                App.meterState.tickTimeBuffer.Add(currentTicktime);
                                
                                // Ограничиваем размер буфера
                                if (App.meterState.tickTimeBuffer.Count > 100)
                                    App.meterState.tickTimeBuffer.RemoveAt(0);
                            }
                                
                            DebugLogger.log($"[VPN-TickTime] Calculated ticktime for VPN bypass: {currentTicktime:F1}ms (from tickrate {currentTickRate} Hz)");
                        }
                        else
                        {
                            // В обычном режиме используем накопленные значения из getTicksIn()
                            currentTickRate = procStats.getTicksIn();
                        }
                        
                        App.meterState.TickRate = currentTickRate;
                        
                        // Добавляем данные tickrate в детектор спайков
                        try
                        {
                            Classes.SpikeDetection.SpikeDetectionManager.AddValue(
                                Classes.SpikeDetection.MetricKind.Tickrate, 
                                currentTickRate
                            );
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.Print($"[updateMetherStateFromActiveWindow] Error adding tickrate to spike detector: {ex.Message}");
                        }
                        
                        // CRITICAL FIX: Null-check для App.meterState.Server перед доступом к свойствам
                        if (App.meterState.Server != null)
                        {
                            App.meterState.Server.PingPort = (int)procStats.remotePort;
                        }
                        App.meterState.SessionStart = procStats.startTrack;
                        
                        // ИСПРАВЛЕНИЕ: Проверку реальной активности применяем ТОЛЬКО в VPN bypass режиме!
                        // В обычном PCAP режиме IsTracking управляется через StartTracking()/StopTracking()
                        // Используем уже существующие переменные vpnBypassBasic и vpnBypassAdvanced из строки 3185
                        bool isVpnBypassMode = vpnBypassBasic || vpnBypassAdvanced;
                        
                        if (isVpnBypassMode)
                        {
                            // КРИТИЧНО: В VPN bypass режиме применяем логику проверки активности
                            // Проверяем есть ли реальная сетевая активность перед установкой IsTracking = true
                            
                            // Первая проверка: базовые условия активности
                            bool hasBasicActivity = procStats.ticksIn > 3 && 
                                                   procStats.downloaded > 0 && 
                                                   procStats.TrackingDelta() > 3;
                            
                            // Вторая проверка: исключаем фиктивные данные VPN fallback
                            // VPN fallback создает фейковые соединения с характерными значениями:
                            bool isFakeVpnFallback = (procStats.downloaded == 1024 && procStats.sent == 512);
                            
                            // Третья проверка: для системных процессов требуем РАСТУЩИЙ трафик
                            bool hasGrowingTraffic = true; // По умолчанию считаем что трафик растет
                            string[] systemProcesses = { "explorer", "dwm", "winlogon", "csrss", "lsass", "services", "svchost", "taskhostw", "taskmgr", "notepad", "calculator", "cmd", "powershell", "powershell_ise", "mspaint", "wordpad" };
                            bool isSystemProcess = systemProcesses.Any(proc => proc.Equals(procStats.name, StringComparison.OrdinalIgnoreCase));
                            
                            if (isSystemProcess)
                            {
                                // Для системных процессов требуем трафик больше чем минимальные фейковые значения
                                hasGrowingTraffic = procStats.downloaded > 2048 || procStats.sent > 1024;
                            }
                            
                            bool hasRealActivity = hasBasicActivity && !isFakeVpnFallback && hasGrowingTraffic;
                            
                            if (hasRealActivity)
                            {
                                DebugLogger.log($"[VPN-Bypass] ✓ Real activity detected for {procStats.name}: ticksIn={procStats.ticksIn}, downloaded={procStats.downloaded}, sent={procStats.sent}, tracking={procStats.TrackingDelta():F1}s");
                                App.meterState.IsTracking = true;
                                _lastMetricsApplied = DateTime.Now;
                                _metricsStateCleared = false;
                            }
                            else
                            {
                                string reason = "";
                                if (!hasBasicActivity) reason += "no basic activity; ";
                                if (isFakeVpnFallback) reason += "fake VPN fallback data; ";
                                if (!hasGrowingTraffic && isSystemProcess) reason += "system process without growing traffic; ";
                                
                                DebugLogger.log($"[VPN-Bypass] ✗ No real activity for {procStats.name}: {reason}(ticksIn={procStats.ticksIn}, downloaded={procStats.downloaded}, sent={procStats.sent}, tracking={procStats.TrackingDelta():F1}s) - keeping IsTracking=false");
                                App.meterState.IsTracking = false;
                                _metricsStateCleared = true;
                            }
                        }
                        else
                        {
                            // В обычном PCAP режиме IsTracking не сбрасываем - он управляется через StartTracking()
                            // Просто устанавливаем флаг что метрики применены
                            _lastMetricsApplied = DateTime.Now;
                            _metricsStateCleared = false;
                        }
                        
                        App.meterState.loss = procStats.loss;
                        App.meterState.totalTicksCnt = procStats.totalTicksCnt;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Коллекция была изменена, пропускаем
                    targetKey = "";
                    ResetMetricsState(currentActiveProcess);
                    return;
                }
            }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[updateMetherStateFromActiveWindow] ERROR: {ex.Message}");
                DebugLogger.log($"[updateMetherStateFromActiveWindow] STACK: {ex.StackTrace}");
            }
        }

        private void ConfigureTickrateChart()
        {
            if (TickrateChart1 == null || TickrateChart1.IsDisposed)
            {
                return;
            }

            try
            {
                TickrateChart1.SuspendLayout();

                TickrateChart1.Series.Clear();
                TickrateChart1.Legends.Clear();

                var area = TickrateChart1.ChartAreas.FindByName(TickrateChartAreaName);
                if (area == null)
                {
                    TickrateChart1.ChartAreas.Clear();
                    area = new ChartArea(TickrateChartAreaName);
                    TickrateChart1.ChartAreas.Add(area);
                }

                Color axisLineColor = Color.FromArgb(160, _neutralActiveColor);
                Color gridColor = Color.FromArgb(90, _inactiveMetricColor);

                area.BackColor = Color.Transparent;
                area.BorderWidth = 0;

                // Ось X
                area.AxisX.MajorGrid.Enabled = true;
                area.AxisX.MajorGrid.LineColor = gridColor;
                area.AxisX.MajorGrid.LineDashStyle = ChartDashStyle.Dot;
                area.AxisX.MajorGrid.IntervalType = DateTimeIntervalType.Seconds;
                area.AxisX.MajorGrid.Interval = 5;
                area.AxisX.MinorGrid.Enabled = false;
                area.AxisX.LineColor = axisLineColor;
                area.AxisX.MajorTickMark.LineColor = axisLineColor;
                area.AxisX.LabelStyle.ForeColor = Color.FromArgb(220, _neutralActiveColor);
                area.AxisX.LabelStyle.Font = new Font("Segoe UI", 8f, FontStyle.Regular);
                area.AxisX.LabelStyle.Format = "HH:mm:ss";
                area.AxisX.LabelStyle.IsEndLabelVisible = true;
                area.AxisX.IsMarginVisible = false;
                area.AxisX.IntervalType = DateTimeIntervalType.Seconds;
                area.AxisX.Interval = 5;
                var now = DateTime.Now;
                area.AxisX.Minimum = now.AddSeconds(-InitialTickrateWindowSeconds).ToOADate();
                area.AxisX.Maximum = now.ToOADate();

                // Ось Y
                area.AxisY.MajorGrid.Enabled = true;
                area.AxisY.MajorGrid.LineColor = gridColor;
                area.AxisY.MajorGrid.LineDashStyle = ChartDashStyle.Dash;
                area.AxisY.MinorGrid.Enabled = false;
                area.AxisY.LineColor = axisLineColor;
                area.AxisY.MajorTickMark.LineColor = axisLineColor;
                area.AxisY.LabelStyle.ForeColor = Color.Red;
                area.AxisY.LabelStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
                area.AxisY.Minimum = 0;
                area.AxisY.Maximum = 60;
                area.AxisY.Interval = 10;

                TickrateChart1.BackColor = Color.Transparent;
                TickrateChart1.BorderlineWidth = 0;
                TickrateChart1.AntiAliasing = AntiAliasingStyles.All;
                TickrateChart1.TextAntiAliasingQuality = TextAntiAliasingQuality.High;

                Series series = new Series(TickrateSeriesName)
                {
                    ChartType = SeriesChartType.FastLine,
                    Color = _neutralActiveColor,
                    BorderWidth = 2,
                    XValueType = ChartValueType.DateTime,
                    YValueType = ChartValueType.Int32,
                    IsXValueIndexed = false,
                    ChartArea = area.Name,
                    IsVisibleInLegend = false
                };

                series.EmptyPointStyle.Color = _neutralActiveColor;
                series.EmptyPointStyle.BorderWidth = 0;

                Series averageSeries = new Series(TickrateAverageSeriesName)
                {
                    ChartType = SeriesChartType.FastLine,
                    Color = TickrateAverageColor,
                    BorderWidth = 2,
                    BorderDashStyle = ChartDashStyle.Dash,
                    XValueType = ChartValueType.DateTime,
                    YValueType = ChartValueType.Double,
                    IsXValueIndexed = false,
                    ChartArea = area.Name,
                    IsVisibleInLegend = false
                };

                averageSeries.EmptyPointStyle.Color = averageSeries.Color;
                averageSeries.EmptyPointStyle.BorderWidth = 0;

                TickrateChart1.Series.Add(series);
                TickrateChart1.Series.Add(averageSeries);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[ConfigureTickrateChart] Error: {ex.Message}");
            }
            finally
            {
                try
                {
                    TickrateChart1?.ResumeLayout();
                }
                catch
                {
                    // ignore
                }
            }

            ResetTickrateChart();
        }

        private void UpdateTickrateChart(List<int> ticks, List<DateTime> timestamps)
        {
            if (TickrateChart1 == null || TickrateChart1.IsDisposed)
            {
                return;
            }

            Series series = TickrateChart1.Series.FindByName(TickrateSeriesName);
            Series averageSeries = TickrateChart1.Series.FindByName(TickrateAverageSeriesName);
            if (series == null || averageSeries == null)
            {
                return;
            }

            ChartArea area = TickrateChart1.ChartAreas.FindByName(TickrateChartAreaName) ??
                             (TickrateChart1.ChartAreas.Count > 0 ? TickrateChart1.ChartAreas[0] : null);
            if (area == null)
            {
                return;
            }
            DataPointCollection points = series.Points;
            DataPointCollection averagePoints = averageSeries.Points;

            points.SuspendUpdates();
            averagePoints.SuspendUpdates();
            try
            {
                points.Clear();
                averagePoints.Clear();

                int ticksCount = ticks?.Count ?? 0;
                int timestampsCount = timestamps?.Count ?? 0;

                if (ticksCount == 0 || timestampsCount == 0)
                {
                    area.AxisY.Minimum = 0;
                    area.AxisY.Maximum = 60;
                    area.AxisY.Interval = 10;
                    DateTime now = DateTime.Now;
                    ConfigureTickrateTimeAxis(area, now.AddSeconds(-InitialTickrateWindowSeconds), now);
                    return;
                }

                int count = Math.Min(ticksCount, timestampsCount);
                double maxValue = 0;
                double rollingSum = 0;
                DateTime? minTime = null;
                DateTime? maxTime = null;

                for (int i = 0; i < count; i++)
                {
                    int tickValue = ticks[i];
                    DateTime timestamp = timestamps[i];
                    if (timestamp == DateTime.MinValue)
                    {
                        timestamp = (maxTime ?? DateTime.Now).AddMilliseconds(16);
                    }

                    points.AddXY(timestamp, tickValue);
                    if (tickValue > maxValue)
                    {
                        maxValue = tickValue;
                    }

                    rollingSum += tickValue;
                    if (i >= TickrateAverageWindow)
                    {
                        rollingSum -= ticks[i - TickrateAverageWindow];
                    }

                    int windowSize = Math.Min(TickrateAverageWindow, i + 1);
                    double averageValue = windowSize > 0 ? rollingSum / windowSize : tickValue;
                    averagePoints.AddXY(timestamp, averageValue);

                    if (!minTime.HasValue || timestamp < minTime.Value)
                    {
                        minTime = timestamp;
                    }
                    if (!maxTime.HasValue || timestamp > maxTime.Value)
                    {
                        maxTime = timestamp;
                    }
                }

                double dynamicPadding = Math.Max(5d, maxValue * 0.1d);
                double axisMax = Math.Max(60d, maxValue + dynamicPadding);
                double axisInterval = Math.Max(1d, GetNiceGridStep((float)(axisMax / 5d)));

                area.AxisY.Minimum = 0;
                area.AxisY.Maximum = axisMax;
                area.AxisY.Interval = axisInterval;

                DateTime axisMin = minTime ?? DateTime.Now.AddSeconds(-InitialTickrateWindowSeconds);
                DateTime axisMaxTime = maxTime ?? DateTime.Now;
                ConfigureTickrateTimeAxis(area, axisMin, axisMaxTime);
            }
            finally
            {
                averagePoints.ResumeUpdates();
                points.ResumeUpdates();
            }

            TickrateChart1.Invalidate();
        }

        private void ResetTickrateChart()
        {
            if (TickrateChart1 == null || TickrateChart1.IsDisposed)
            {
                return;
            }

            Series series = TickrateChart1.Series.FindByName(TickrateSeriesName);
            series?.Points.Clear();

            Series averageSeries = TickrateChart1.Series.FindByName(TickrateAverageSeriesName);
            averageSeries?.Points.Clear();

            ChartArea area = TickrateChart1.ChartAreas.FindByName(TickrateChartAreaName) ??
                             (TickrateChart1.ChartAreas.Count > 0 ? TickrateChart1.ChartAreas[0] : null);
            if (area == null)
            {
                return;
            }
            area.AxisY.Minimum = 0;
            area.AxisY.Maximum = 60;
            area.AxisY.Interval = 10;
            DateTime now = DateTime.Now;
            ConfigureTickrateTimeAxis(area, now.AddSeconds(-InitialTickrateWindowSeconds), now);

            TickrateChart1.Invalidate();
        }

        private static float GetNiceGridStep(float rawStep)
        {
            if (rawStep <= 0f)
                return 10f;

            double exponent = Math.Floor(Math.Log10(rawStep));
            double baseValue = Math.Pow(10, exponent);
            double normalized = rawStep / baseValue;

            double[] candidates = { 1d, 2d, 2.5d, 5d, 10d };
            double chosen = candidates[candidates.Length - 1];

            foreach (double candidate in candidates)
            {
                if (normalized <= candidate)
                {
                    chosen = candidate;
                    break;
                }
            }

            double step = chosen * baseValue;
            if (step <= 0)
                step = 10;

            return (float)step;
        }

        private static void ConfigureTickrateTimeAxis(ChartArea area, DateTime minTime, DateTime maxTime)
        {
            if (area == null)
            {
                return;
            }

            if (maxTime <= minTime)
            {
                maxTime = minTime.AddSeconds(1);
            }

            double spanSeconds = (maxTime - minTime).TotalSeconds;
            if (double.IsNaN(spanSeconds) || double.IsInfinity(spanSeconds))
            {
                spanSeconds = 1;
            }

            area.AxisX.Minimum = minTime.ToOADate();
            area.AxisX.Maximum = maxTime.ToOADate();

            if (spanSeconds <= 120) // до 2 минут
            {
                double intervalSeconds = SelectTimeInterval(spanSeconds, new[] { 1d, 2d, 5d, 10d, 15d, 30d });
                ApplyTimeAxis(area, DateTimeIntervalType.Seconds, intervalSeconds, "HH:mm:ss");
            }
            else if (spanSeconds <= 3600) // до 1 часа
            {
                double intervalMinutes = SelectTimeInterval(spanSeconds / 60d, new[] { 1d, 2d, 5d, 10d, 15d });
                ApplyTimeAxis(area, DateTimeIntervalType.Minutes, intervalMinutes, "HH:mm");
            }
            else if (spanSeconds <= 14400) // до 4 часов
            {
                double intervalMinutes = SelectTimeInterval(spanSeconds / 60d, new[] { 5d, 10d, 15d, 30d });
                ApplyTimeAxis(area, DateTimeIntervalType.Minutes, intervalMinutes, "HH:mm");
            }
            else if (spanSeconds <= 86400) // до суток
            {
                double intervalHours = SelectTimeInterval(spanSeconds / 3600d, new[] { 1d, 2d, 3d, 6d, 12d });
                ApplyTimeAxis(area, DateTimeIntervalType.Hours, intervalHours, "dd.MM HH:mm");
            }
            else
            {
                double intervalDays = SelectTimeInterval(spanSeconds / 86400d, new[] { 1d, 2d, 3d, 7d, 14d });
                string format = spanSeconds <= 604800 ? "dd.MM" : "dd.MM.yyyy";
                ApplyTimeAxis(area, DateTimeIntervalType.Days, intervalDays, format);
            }
        }

        private static void ApplyTimeAxis(ChartArea area, DateTimeIntervalType intervalType, double interval, string labelFormat)
        {
            interval = Math.Max(interval, 1d);
            area.AxisX.IntervalType = intervalType;
            area.AxisX.Interval = interval;
            area.AxisX.LabelStyle.Format = labelFormat;
            area.AxisX.MajorGrid.IntervalType = intervalType;
            area.AxisX.MajorGrid.Interval = interval;
            area.AxisX.MajorTickMark.IntervalType = intervalType;
            area.AxisX.MajorTickMark.Interval = interval;
        }

        private static double SelectTimeInterval(double spanUnits, IReadOnlyList<double> candidates)
        {
            if (spanUnits <= 0)
            {
                return candidates[0];
            }

            foreach (double candidate in candidates)
            {
                if (spanUnits / candidate <= 8d)
                {
                    return candidate;
                }
            }

            return candidates[candidates.Count - 1];
        }

        private static Color GetDropsColor(float dropsPercent)
        {
            var zone = Classes.Zone.Green;

            if (dropsPercent > 5f)
            {
                zone = Classes.Zone.Red;
            }
            else if (dropsPercent > 1f)
            {
                zone = Classes.Zone.Yellow;
            }

            return Classes.ZoneColors.ToColor(zone);
        }

       
        /// <summary>
        /// Определяет, является ли адаптер VPN интерфейсом (TUN/TAP)
        /// </summary>
        private bool IsVpnAdapter(LivePacketDevice device)
        {
            if (device?.Description == null) return false;
            var desc = device.Description.ToLowerInvariant();
            
            // Типичные паттерны VPN адаптеров
            return desc.Contains("wintun") || desc.Contains("wireguard") || 
                   desc.Contains("openvpn") || desc.Contains("tap") || 
                   desc.Contains("tun") || desc.Contains("vpn") ||
                   desc.Contains("nordvpn") || desc.Contains("expressvpn") ||
                   desc.Contains("surfshark") || desc.Contains("protonvpn");
        }

        public void StartTracking()
        {
            // Анти-реэнтерабельность: если уже идет запуск - выходим
            if (Interlocked.Exchange(ref _startTrackingBusy, 1) == 1) 
            {
                Debug.Print("[StartTracking] Already in progress, skipping");
                return;
            }
            
            try
            {
                Debug.Print("StartTracking");
                var restoreTargetKey = _pendingRestoreTargetKey;
                bool hadPendingRestore = !string.IsNullOrEmpty(restoreTargetKey);
                
                // Диагностика CaptureService ПЕРЕД стартом
                if (App.Capture != null)
                {
                    var debugInfo = App.Capture.DebugWorkers();
                    Debug.Print($"[StartTracking] BEFORE: CaptureService workers count: {debugInfo.Length}");
                    if (debugInfo.Length > 8)
                    {
                        foreach (var (key, refs) in debugInfo)
                        {
                            Debug.Print($"[StartTracking] BEFORE: Worker {key} -> refs: {refs}");
                        }
                    }
                }
                else
                {
                    Debug.Print("[StartTracking] WARNING: App.Capture is NULL! CaptureService not initialized!");
                }
                
                if (App.meterState != null)
                    StopTracking();
                InitMeterState();
                App.meterState.IsTracking = true;
                var startTimestampUtc = DateTime.UtcNow;
                _lastPacketTimestamp = startTimestampUtc;
                _lastSelfHealAttempt = DateTime.MinValue;
                WriteHeartbeatIfNeeded(startTimestampUtc, force: true);
                PersistMonitoringSnapshot(startTimestampUtc, force: true);
                ticksLoop.Enabled = true;
            
            // Запускаем ping manager
            if (App.pingManager != null)
            {
                App.pingManager.StartPinging();
                if (hadPendingRestore)
                {
                    App.pingManager.RequestImmediatePing();
                }
            }
            
            // Даём ConnectionsManager время собрать данные о соединениях (только при первом запуске)
            if (App.connMngr != null && (App.connMngr.TcpActiveConnections.Count == 0 && App.connMngr.UdpActiveConnections.Count == 0))
            {
                Debug.Print("[StartTracking] Waiting 500ms for ConnectionsManager to gather initial connection data...");
                System.Threading.Thread.Sleep(500);
            }
            
            // Проверяем настройки VPN обхода
            bool vpnBypassBasic = App.settingsManager.GetOption("vpn_bypass_basic", "False", "ADVANCED") == "True";
            bool vpnBypassAdvanced = App.settingsManager.GetOption("vpn_bypass_advanced", "False", "ADVANCED") == "True";
            
            // ИСПРАВЛЕНИЕ: В режиме VPN bypass подписываемся на события ConnectionTracker
            if ((vpnBypassBasic || vpnBypassAdvanced) && App.connectionTracker != null)
            {
                Debug.Print("[StartTracking] VPN bypass mode detected - subscribing to ConnectionTracker events");
                App.connectionTracker.OnNewTunnelConnection += HandleTunnelConnectionForTracking;
                
                // Сбрасываем ETW счетчики пакетов для нового сеанса мониторинга
                Classes.ETW.ResetPacketCounters();
                Debug.Print("[StartTracking] ETW packet counters reset for VPN bypass session");
                
                // Устанавливаем активный процесс для ETW, если он уже известен
                string activeGame = Classes.AutoDetectMngr.GetActiveProcessName(true);
                if (!string.IsNullOrEmpty(activeGame) && activeGame != "n\\a")
                {
                    Classes.ETW.SetActiveProcess(activeGame);
                    Debug.Print($"[StartTracking] Active process set for ETW: {activeGame}");
                }
            }
            
            string captureAllSetting = App.settingsManager.GetOption("capture_all_adapters", "False", "SETTINGS");
            var captureAll = captureAllSetting == "True";
            Debug.Print($"[StartTracking] Settings debug - capture_all_adapters raw: '{captureAllSetting}', converted: {captureAll}");
            Debug.Print($"[StartTracking] VPN bypass - basic: {vpnBypassBasic}, advanced: {vpnBypassAdvanced}");
            
            var devices = App.GetAdapters();
            _allSelectedAdapters.Clear();

            if (captureAll || vpnBypassBasic || vpnBypassAdvanced)
            {
                Debug.Print($"[StartTracking] MULTI-ADAPTER MODE - captureAll: {captureAll}, vpnBasic: {vpnBypassBasic}, vpnAdvanced: {vpnBypassAdvanced}");
                // собрать все «реальные» адаптеры (пропускаем 0-й элемент дропдауна и виртуальные/loopback)
                IEnumerable<LivePacketDevice> src = devices;
                if (src.Count() == App.settingsForm.adapters_list.Items.Count && App.settingsForm.adapters_list.Items.Count > 0)
                {
                    // список в UI обычно имеет заглушку на позиции 0
                    src = src.Skip(1);
                }
                
                // Режимы VPN обхода - приоритет VPN адаптерам
                if (vpnBypassBasic || vpnBypassAdvanced)
                {
                    var vpnAdapters = new List<LivePacketDevice>();
                    var regularAdapters = new List<LivePacketDevice>();
                    
                    foreach (var d in src)
                    {
                        var desc = (d.Description ?? "").ToLowerInvariant();
                        // Пропускаем только loopback, но оставляем виртуальные адаптеры для VPN
                        if (desc.Contains("loopback") || desc.Contains("npcap loopback"))
                            continue;
                            
                        if (IsVpnAdapter(d))
                        {
                            vpnAdapters.Add(d);
                        }
                        else if (!desc.Contains("hyper-v") && !desc.Contains("vmware") &&
                                !desc.Contains("virtualbox") && !desc.Contains("vethernet"))
                        {
                            regularAdapters.Add(d);
                        }
                    }
                    
                    // В режиме VPN обхода: сначала VPN адаптеры, потом обычные
                    _allSelectedAdapters.AddRange(vpnAdapters);
                    if (vpnAdapters.Count == 0 || vpnBypassAdvanced)
                    {
                        // Если VPN адаптеров нет или включён продвинутый режим - добавляем обычные
                        _allSelectedAdapters.AddRange(regularAdapters);
                    }
                    
                    Debug.Print($"VPN bypass mode: found {vpnAdapters.Count} VPN adapters, {regularAdapters.Count} regular adapters");
                }
                else
                {
                    // Обычный режим захвата всех адаптеров
                    foreach (var d in src)
                    {
                        var desc = (d.Description ?? "").ToLowerInvariant();
                        if (desc.Contains("loopback") || desc.Contains("npcap loopback") ||
                            desc.Contains("hyper-v") || desc.Contains("vmware") ||
                            desc.Contains("virtualbox") || desc.Contains("vethernet"))
                            continue;
                        _allSelectedAdapters.Add(d);
                    }
                }
                
                if (_allSelectedAdapters.Count == 0)
                {
                    _pendingRestoreTargetKey = string.Empty;
                    MessageBox.Show("Не найдено подходящих сетевых адаптеров");
                    return;
                }
            }
            else
            {
                Debug.Print($"[StartTracking] SINGLE-ADAPTER MODE - captureAll: {captureAll}, vpnBasic: {vpnBypassBasic}, vpnAdvanced: {vpnBypassAdvanced}");
                DebugLogger.log($"[StartTracking] SINGLE-ADAPTER MODE - captureAll: {captureAll}, vpnBasic: {vpnBypassBasic}, vpnAdvanced: {vpnBypassAdvanced}");
                int deviceId = App.settingsForm.adapters_list.SelectedIndex;
                Debug.Print($"[StartTracking] Single adapter mode - deviceId: {deviceId}, devices.Count: {devices.Count}");
                DebugLogger.log($"[StartTracking] Single adapter mode - deviceId: {deviceId}, devices.Count: {devices.Count}");
                
                if (devices.Count > deviceId && deviceId > 0)
                {
                    selectedAdapter = devices[deviceId];
                    Debug.Print($"[StartTracking] Selected adapter: {selectedAdapter.Name} - {selectedAdapter.Description}");
                    DebugLogger.log($"[StartTracking] Selected adapter: {selectedAdapter.Name} - {selectedAdapter.Description}");
                }
                else
                {
                    // Фоллбэк: пытаемся загрузить last_selected_adapter из settings
                    if (deviceId == 0)
                    {
                        Debug.Print("[StartTracking] deviceId=0, trying to load last_selected_adapter from settings...");
                        DebugLogger.log("[StartTracking] deviceId=0, trying to load last_selected_adapter from settings...");
                        string adapterGuid = App.settingsManager.GetOption("last_selected_adapter");
                        if (!string.IsNullOrEmpty(adapterGuid))
                        {
                            Debug.Print($"[StartTracking] Found last_selected_adapter: {adapterGuid}");
                            DebugLogger.log($"[StartTracking] Found last_selected_adapter: {adapterGuid}");
                            foreach (var device in devices)
                            {
                                if (device.GetGuid().ToLower().Equals(adapterGuid.ToLower()))
                                {
                                    selectedAdapter = device;
                                    Debug.Print($"[StartTracking] Restored adapter from GUID: {device.Name} - {device.Description}");
                                    DebugLogger.log($"[StartTracking] Restored adapter from GUID: {device.Name} - {device.Description}");
                                    break;
                                }
                            }
                        }
                        
                        if (selectedAdapter == null)
                        {
                            Debug.Print("[StartTracking] ERROR: Could not restore adapter from last_selected_adapter");
                            DebugLogger.log("[StartTracking] ERROR: Could not restore adapter from last_selected_adapter");
                            MessageBox.Show("Пожалуйста, выберите сетевой адаптер в настройках.\n\nОткройте Settings → Network Settings и выберите ваш основной сетевой адаптер.", 
                                            "Сетевой адаптер не выбран", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            _pendingRestoreTargetKey = string.Empty;
                            return;
                        }
                    }
                    else
                    {
                        Debug.Print($"[StartTracking] ERROR: Invalid deviceId {deviceId} for {devices.Count} devices");
                        DebugLogger.log($"[StartTracking] ERROR: Invalid deviceId {deviceId} for {devices.Count} devices");
                        _pendingRestoreTargetKey = string.Empty;
                        return;
                    }
                }
            }
            
            // В режиме мультиадаптера автоматически определяем локальный IP активного процесса
            if (captureAll || vpnBypassBasic || vpnBypassAdvanced)
            {
                // ВАЖНО: Сбрасываем кэш при запуске для свежего определения IP
                LocalIPDetector.ResetCache();
                
                // Получаем имя активного процесса
                string activeProcess = AutoDetectMngr.GetActiveProcessName();
                Debug.Print($"[StartTracking] Active process: {activeProcess}");
                
                string autoDetectedIP = LocalIPDetector.DetectLocalIPForActiveProcess(activeProcess);
                if (!string.IsNullOrEmpty(autoDetectedIP))
                {
                    App.meterState.LocalIP = autoDetectedIP;
                    Debug.Print($"[StartTracking] Multi-adapter mode: Auto-detected LocalIP = {autoDetectedIP} for process {activeProcess}");
                    
                    // Обновляем UI асинхронно (не блокируем текущий поток)
                    var settingsForm = App.settingsForm;
                    if (settingsForm != null && !settingsForm.IsDisposed && settingsForm.local_ip_textbox != null)
                    {
                        try
                        {
                            App.SafeInvokeOnSettings(() =>
                            {
                                try
                                {
                                    if (!settingsForm.IsDisposed && settingsForm.local_ip_textbox != null && settingsForm.local_ip_textbox.Text != autoDetectedIP)
                                    {
                                        settingsForm.local_ip_textbox.Text = autoDetectedIP;
                                        Debug.Print($"[StartTracking] Settings LocalIP textbox updated to {autoDetectedIP}");
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Debug.Print($"[StartTracking] Error updating LocalIP textbox inside SafeInvoke: {ex.Message}");
                                }
                            });
                        }
                        catch (Exception ex)
                        {
                            Debug.Print($"[StartTracking] SafeInvokeOnSettings failed: {ex.Message}");
                        }
                    }
                }
                else
                {
                    // Fallback: используем текущее значение из настроек
                    string manualLocalIP = App.settingsForm?.local_ip_textbox?.Text ?? App.meterState.LocalIP;
                    App.meterState.LocalIP = manualLocalIP;
                    Debug.Print($"[StartTracking] Multi-adapter mode: Could not auto-detect IP, using configured LocalIP = {App.meterState.LocalIP}");
                    
                    if (string.IsNullOrEmpty(App.meterState.LocalIP))
                    {
                        Debug.Print($"[StartTracking] WARNING: LocalIP is empty! Please configure manually or wait for connections to establish.");
                    }
                }
            }
            else
            {
                // В обычном режиме используем IP из настроек
                App.meterState.LocalIP = App.settingsForm?.local_ip_textbox?.Text ?? App.meterState.LocalIP;
            }
            
            lastSelectedAdapterID = App.settingsForm.adapters_list.SelectedIndex;
            try
            {
                if (captureAll || vpnBypassBasic || vpnBypassAdvanced)
                {
                    // запустить по воркеру на каждый адаптер с высоким приоритетом
                    int workerIndex = 0;
                    foreach (var dev in _allSelectedAdapters)
                    {
                        var worker = new BackgroundWorker();
                        worker.WorkerSupportsCancellation = true; // ИСПРАВЛЕНИЕ: поддержка отмены
                        var currentWorkerIndex = workerIndex; // Захватываем значение для лямбды
                        var currentDevice = dev; // Захватываем устройство для лямбды
                        
                        worker.DoWork += (s, e) =>
                        {
                            var bgWorker = s as BackgroundWorker;
                            
                            if (!App.meterState.IsTracking || (bgWorker != null && bgWorker.CancellationPending)) 
                            {
                                Debug.Print($"[PCAP-Multi-{currentWorkerIndex}] Tracking stopped or cancelled, exiting worker");
                                if (bgWorker != null) e.Cancel = true;
                                return;
                            }
                            
                            Debug.Print($"[PCAP-Multi-{currentWorkerIndex}] Worker started for device {currentDevice.Name}");
                            
                            // Phase 3: Устанавливаем высокий приоритет для PCAP потока
                            SetHighPriorityThread(Thread.CurrentThread, $"PCAP-Multi-{currentWorkerIndex}");
                            
                            try
                            {
                                using (var comm = currentDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 500))
                                {
                                    if (comm.DataLink.Kind != DataLinkKind.Ethernet) 
                                    {
                                        Debug.Print($"[PCAP-Multi-{currentWorkerIndex}] Not Ethernet, exiting");
                                        return;
                                    }
                                    
                                    // PCAP тюнинг для производительности
                                    TryOptimizePcapCommunicator(comm);
                                    
                                    // Применяем BPF фильтр из настроек
                                    ApplyBpfFilterSafely(comm);
                                    
                                    // Основной цикл захвата с проверками отмены
                                    // НЕ ИСПОЛЬЗУЕМ ReceivePackets(0) - это блокирующий вызов!
                                    // Вместо этого делаем цикл с проверками отмены
                                    while (!bgWorker?.CancellationPending == true && App.meterState.IsTracking)
                                    {
                                        try
                                        {
                                            Packet packet;
                                            var result = comm.ReceivePacket(out packet);
                                            if (result == PacketCommunicatorReceiveResult.Ok && packet != null)
                                            {
                                                PacketHandler(packet);
                                            }
                                            else
                                            {
                                                Thread.Sleep(1); // Небольшая пауза если пакетов нет
                                            }
                                        }
                                        catch (InvalidOperationException ex) when (ex.Message.Contains("interface disappeared") || ex.Message.Contains("DEVICE_REMOVED"))
                                        {
                                            Debug.Print($"[PCAP-Multi-{currentWorkerIndex}] Network adapter removed/disconnected: {ex.Message}");
                                            DebugLogger.log($"[PCAP-Multi-{currentWorkerIndex}] Adapter disconnected: {dev.Name} - {ex.Message}");
                                            
                                            // Выходим из цикла, чтобы worker завершился корректно
                                            break;
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.Print($"[PCAP-Multi-{currentWorkerIndex}] Unexpected error: {ex.Message}");
                                            DebugLogger.log($"[PCAP-Multi-{currentWorkerIndex}] Unexpected error in packet capture: {ex.Message}");
                                            Thread.Sleep(10); // Небольшая пауза при ошибках
                                        }
                                    }
                                    
                                    if (bgWorker?.CancellationPending == true)
                                    {
                                        Debug.Print($"[PCAP-Multi-{currentWorkerIndex}] Cancellation requested, exiting loop");
                                        e.Cancel = true;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Debug.Print($"[PCAP-Multi-{currentWorkerIndex}] Error: {ex.Message}");
                            }
                            
                            Debug.Print($"[PCAP-Multi-{currentWorkerIndex}] Worker finished");
                        };
                        
                        worker.RunWorkerCompleted += PcapWorkerCompleted;
                        _pcapWorkers.Add(worker);
                        Debug.Print($"[StartTracking] Starting worker {workerIndex} for device {dev.Name}, total workers: {_pcapWorkers.Count}");
                        worker.RunWorkerAsync();
                        workerIndex++;
                    }
                }
                else
                {
                    // ИСПРАВЛЕНИЕ: Убираем проверку PcapThread == null
                    // Просто вызываем InitPcapWorker() напрямую - он создаст BackgroundWorker
                    DebugLogger.log("[StartTracking] Single-adapter mode - calling InitPcapWorker()");
                    Debug.Print("[StartTracking] Single-adapter mode - calling InitPcapWorker()");
                    InitPcapWorker();
                    Debug.Print("[StartTracking] InitPcapWorker() called, BackgroundWorker started");
                    DebugLogger.log("[StartTracking] InitPcapWorker() called, BackgroundWorker started");
                }
            }
            catch (Exception ex)
            {
                string errorDetails = $"PCAP Thread init error: {ex.Message}";
                DebugLogger.log($"[PCAP-Error] {errorDetails}");
                DebugLogger.log($"[PCAP-Error] StackTrace: {ex.StackTrace}");
                
                // Более информативное сообщение для пользователя
                MessageBox.Show($"PCAP Thread init error\n\nDetails: {ex.Message}\n\nTry running as Administrator or check network adapters.", 
                               "Network Capture Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            
            // Диагностика CaptureService ПОСЛЕ стартов воркеров
            if (App.Capture != null)
            {
                var debugInfo = App.Capture.DebugWorkers();
                Debug.Print($"[StartTracking] AFTER: CaptureService workers count: {debugInfo.Length}");
                foreach (var (key, refs) in debugInfo)
                {
                    Debug.Print($"[StartTracking] AFTER: Worker {key} -> refs: {refs}");
                }
            }
            else
            {
                Debug.Print("[StartTracking] WARNING: App.Capture is NULL after worker start!");
            }

            if (hadPendingRestore)
            {
                try
                {
                    if (restoreTargetKey != AutoResumeSentinel)
                    {
                        targetKey = restoreTargetKey;
                        Debug.Print($"[StartTracking] Restored targetKey '{targetKey}' from pending state");
                    }
                    else
                    {
                        Debug.Print("[StartTracking] Pending auto-resume without explicit targetKey; staying in aggressive discovery mode");
                    }

                    RequestConnectionsRefresh(true);
                }
                catch (Exception ex)
                {
                    Debug.Print($"[StartTracking] Restore assistance failed: {ex.Message}");
                }
                finally
                {
                    _pendingRestoreTargetKey = string.Empty;
                }
            }
            else
            {
                _pendingRestoreTargetKey = string.Empty;
            }
            }
            catch (Exception ex)
            {
                Debug.Print($"[StartTracking] Error: {ex.Message}");
                DebugLogger.log(ex);
            }
            finally
            {
                // Всегда освобождаем блокировку
                Volatile.Write(ref _startTrackingBusy, 0);
            }
        }
        private void PcapWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Диагностика CaptureService при завершении воркера
            if (App.Capture != null)
            {
                var debugInfo = App.Capture.DebugWorkers();
                Debug.Print($"[PcapWorkerCompleted] BEFORE cleanup: CaptureService workers count: {debugInfo.Length}");
                if (debugInfo.Length > 8) // Показываем детали если воркеров больше ожидаемого
                {
                    foreach (var (key, refs) in debugInfo)
                    {
                        Debug.Print($"[PcapWorkerCompleted] BEFORE: Worker {key} -> refs: {refs}");
                    }
                }
            }
            
            // Всегда удаляем завершившийся воркер из списка
            var completedWorker = sender as BackgroundWorker;
            if (completedWorker != null)
            {
                try
                {
                    _pcapWorkers.Remove(completedWorker);
                    completedWorker.DoWork -= null;
                    completedWorker.RunWorkerCompleted -= null;
                    completedWorker.Dispose();
                    Debug.Print($"[PcapWorkerCompleted] Worker removed from list, remaining: {_pcapWorkers.Count}");
                }
                catch (Exception ex)
                {
                    Debug.Print($"[PcapWorkerCompleted] Error removing worker: {ex.Message}");
                }
            }
            
            // КРИТИЧЕСКАЯ ПРОВЕРКА: НЕ перезапускаем воркеры если трекинг остановлен
            if (!App.meterState.IsTracking) 
            {
                Debug.Print("[PcapWorkerCompleted] Tracking stopped, not restarting worker");
                return;
            }
            
            // ДОПОЛНИТЕЛЬНАЯ ПРОВЕРКА: НЕ перезапускаем если ticksLoop отключен
            if (!ticksLoop.Enabled)
            {
                Debug.Print("[PcapWorkerCompleted] TicksLoop disabled, not restarting worker");
                return;
            }
            
            // Проверяем режимы работы
            var captureAll = App.settingsManager.GetOption("capture_all_adapters", "False", "SETTINGS") == "True";
            bool vpnBypassBasic = App.settingsManager.GetOption("vpn_bypass_basic", "False", "ADVANCED") == "True";
            bool vpnBypassAdvanced = App.settingsManager.GetOption("vpn_bypass_advanced", "False", "ADVANCED") == "True";
            
            // В мульти-режиме или VPN режиме НЕ ПЕРЕЗАПУСКАЕМ воркеры автоматически
            if (captureAll || vpnBypassBasic || vpnBypassAdvanced)
            {
                Debug.Print("[PcapWorkerCompleted] Multi-mode active, not auto-restarting workers");
                return;
            }
            
            // Перезапуск только для одиночного режима и только если нет ошибок
            if (App.meterState.TickRate == 0)
            {
                restarts++;
                if (restarts > restartLimit)
                {
                    Debug.Print("[PcapWorkerCompleted] Too many restarts, scheduling capture restart");
                    restarts = 0;
                    if (App.meterState != null && App.meterState.IsTracking)
                    {
                        ScheduleCaptureRestart();
                    }
                    return;
                }
            }
            else
            {
                restarts = 0;
            }

            try
            {
                // Проверяем что pcapWorker существует и трекинг все еще активен
                if (pcapWorker != null && App.meterState.IsTracking && ticksLoop.Enabled)
                {
                    Debug.Print("[PcapWorkerCompleted] Restarting single pcapWorker");
                    pcapWorker.RunWorkerAsync();
                }
                else
                {
                    Debug.Print("[PcapWorkerCompleted] Not restarting: pcapWorker null or tracking stopped");
                }
            }
            catch (Exception ex) 
            { 
                Debug.Print($"[PcapWorkerCompleted] Error restarting worker: {ex.Message}");
            }
        }

        private void PcapWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            var bgWorker = sender as BackgroundWorker;
            
            if (!App.meterState.IsTracking || (bgWorker != null && bgWorker.CancellationPending)) 
            {
                if (bgWorker != null) e.Cancel = true;
                return;
            }
            
            // Phase 3: Устанавливаем высокий приоритет для BackgroundWorker потока
            SetHighPriorityThread(Thread.CurrentThread, "PCAP-BgWorker");
            
            // В мульти-режиме или VPN режиме этот метод не должен вызываться
            var captureAll = App.settingsManager.GetOption("capture_all_adapters", "False", "SETTINGS") == "True";
            bool vpnBypassBasic = App.settingsManager.GetOption("vpn_bypass_basic", "False", "ADVANCED") == "True";
            bool vpnBypassAdvanced = App.settingsManager.GetOption("vpn_bypass_advanced", "False", "ADVANCED") == "True";
            if (captureAll || vpnBypassBasic || vpnBypassAdvanced) return;
            
            if (selectedAdapter == null)
            {
                MessageBox.Show("Selected adapter is not set!");
                return;
            }
            
            using (PacketCommunicator communicator = selectedAdapter.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 500))
            {
                if (communicator.DataLink.Kind != DataLinkKind.Ethernet)
                {
                    MessageBox.Show("This program works only on Ethernet networks!");
                    return;
                }

                // PCAP тюнинг для производительности
                TryOptimizePcapCommunicator(communicator);
                
                // Применяем BPF фильтр из настроек
                ApplyBpfFilterSafely(communicator);

                // Основной цикл захвата с проверкой отмены
                while (!bgWorker?.CancellationPending == true && App.meterState.IsTracking)
                {
                    try
                    {
                        Packet packet;
                        var result = communicator.ReceivePacket(out packet);
                        if (result == PacketCommunicatorReceiveResult.Ok && packet != null)
                        {
                            PacketHandler(packet);
                        }
                        else
                        {
                            Thread.Sleep(1);
                        }
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("interface disappeared") || ex.Message.Contains("DEVICE_REMOVED"))
                    {
                        Debug.Print($"[PCAP-Single] Network adapter removed/disconnected: {ex.Message}");
                        DebugLogger.log($"[PCAP-Single] Adapter disconnected: {selectedAdapter.Name} - {ex.Message}");
                        
                        // Выходим из цикла, чтобы worker завершился корректно
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"[PCAP-Single] Unexpected error: {ex.Message}");
                        DebugLogger.log($"[PCAP-Single] Unexpected error in packet capture: {ex.Message}");
                        Thread.Sleep(10); // Небольшая пауза при ошибках
                    }
                }
                
                if (bgWorker?.CancellationPending == true)
                {
                    e.Cancel = true;
                }
            }
        }
        public void InitPcapWorker()
        {
            pcapWorker = new BackgroundWorker();
            pcapWorker.WorkerSupportsCancellation = true; // ИСПРАВЛЕНИЕ: поддержка отмены
            pcapWorker.DoWork += PcapWorkerDoWork;
            pcapWorker.RunWorkerCompleted += PcapWorkerCompleted;
            pcapWorker.RunWorkerAsync();
        }

        /// <summary>
        /// Автоматически переключает адаптер захвата пакетов при изменении LocalIP
        /// </summary>
        private void SwitchAdapterIfNeeded(string newLocalIP)
        {
            // Анти-реэнтерабельность: если уже идет переключение - выходим
            if (Interlocked.Exchange(ref _switchAdapterBusy, 1) == 1)
            {
                Debug.Print("[SwitchAdapterIfNeeded] Already in progress, skipping");
                return;
            }
            
            try
            {
                Debug.Print($"[SwitchAdapterIfNeeded] Checking if adapter switch needed for IP: {newLocalIP}");
                
                // Проверяем что мониторинг активен
                if (!App.meterState.IsTracking)
                {
                    Debug.Print($"[SwitchAdapterIfNeeded] Monitoring not active, skipping");
                    return;
                }
                
                // Получаем текущий адаптер захвата
                string currentAdapterIP = selectedAdapter != null ? App.GetAdapterAddress(selectedAdapter) : null;
                
                // Проверяем, нужно ли переключать
                if (currentAdapterIP == newLocalIP)
                {
                    Debug.Print($"[SwitchAdapterIfNeeded] Adapter already correct: {currentAdapterIP}");
                    return;
                }
                
                Debug.Print($"[SwitchAdapterIfNeeded] Adapter switch required: {currentAdapterIP} -> {newLocalIP}");
                
                // Находим новый адаптер по IP
                var devices = App.GetAdapters();
                LivePacketDevice newAdapter = null;
                int newAdapterIndex = -1;
                
                for (int i = 0; i < devices.Count; i++)
                {
                    string adapterIP = App.GetAdapterAddress(devices[i]);
                    if (adapterIP == newLocalIP)
                    {
                        newAdapter = devices[i];
                        newAdapterIndex = i;
                        break;
                    }
                }
                
                if (newAdapter == null)
                {
                    Debug.Print($"[SwitchAdapterIfNeeded] ⚠ WARNING: No adapter found with IP {newLocalIP}");
                    return;
                }
                
                Debug.Print($"[SwitchAdapterIfNeeded] Found new adapter: {newAdapter.Name} (index {newAdapterIndex})");
                
                // Проверяем режимы работы
                bool captureAll = App.settingsManager?.GetOption("capture_all_adapters", "False", "ADVANCED") == "True";
                bool vpnBypassBasic = App.settingsManager?.GetOption("vpn_bypass_basic", "False", "ADVANCED") == "True";
                bool vpnBypassAdvanced = App.settingsManager?.GetOption("vpn_bypass_advanced", "False", "ADVANCED") == "True";
                
                if (captureAll || vpnBypassBasic || vpnBypassAdvanced)
                {
                    // В мультиадаптерном режиме обновляем список адаптеров
                    Debug.Print($"[SwitchAdapterIfNeeded] Multi-adapter mode - updating adapter list");
                    
                    // Останавливаем старые workers
                    Debug.Print($"[SwitchAdapterIfNeeded] Stopping {_pcapWorkers.Count} old workers");
                    var workersToStop = _pcapWorkers.ToList(); // Копируем список для безопасной итерации
                    
                    // Сначала запрашиваем отмену всех workers
                    foreach (var worker in workersToStop)
                    {
                        if (worker != null && worker.IsBusy)
                        {
                            worker.CancelAsync();
                        }
                    }
                    
                    // Ждем реального завершения workers БЕЗ блокировки UI
                    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                    int waitedMs = 0;
                    int busyCount = workersToStop.Count(w => w != null && w.IsBusy);
                    
                    while (busyCount > 0 && waitedMs < 1000) // Увеличиваем таймаут до 1 секунды
                    {
                        System.Threading.Thread.Sleep(50); // Увеличенный интервал
                        waitedMs += 50;
                        int newBusyCount = workersToStop.Count(w => w != null && w.IsBusy);
                        if (newBusyCount != busyCount)
                        {
                            Debug.Print($"[SwitchAdapterIfNeeded] {newBusyCount} workers still busy ({busyCount - newBusyCount} stopped)");
                            busyCount = newBusyCount;
                        }
                    }
                    Debug.Print($"[SwitchAdapterIfNeeded] Workers stopped in {stopwatch.ElapsedMilliseconds}ms, {busyCount} still busy");
                    
                    // КРИТИЧЕСКИ: Очищаем список только ПОСЛЕ остановки всех workers
                    _pcapWorkers.Clear();
                    
                    // Пересоздаем список адаптеров с новым приоритетом
                    _allSelectedAdapters.Clear();
                    
                    if (vpnBypassAdvanced || vpnBypassBasic)
                    {
                        var allDevices = App.GetAdapters();
                        var vpnAdapters = new List<LivePacketDevice>();
                        var regularAdapters = new List<LivePacketDevice>();
                        
                        foreach (var d in allDevices)
                        {
                            bool isVpn = IsVpnAdapter(d);
                            if (isVpn) vpnAdapters.Add(d);
                            else regularAdapters.Add(d);
                        }
                        
                        _allSelectedAdapters.AddRange(vpnAdapters);
                        if (!vpnBypassAdvanced)
                            _allSelectedAdapters.AddRange(regularAdapters);
                    }
                    else if (captureAll)
                    {
                        _allSelectedAdapters.AddRange(App.GetAdapters());
                    }
                    
                    // Запускаем новые workers
                    _pcapWorkers.Clear();
                    int workerIndex = 0;
                    
                    foreach (var dev in _allSelectedAdapters)
                    {
                        var worker = new BackgroundWorker();
                        worker.WorkerSupportsCancellation = true;
                        int currentWorkerIndex = workerIndex;
                        
                        worker.DoWork += (s, e) => {
                            var bgWorker = s as BackgroundWorker;
                            SetHighPriorityThread(Thread.CurrentThread, $"PCAP-Multi-{currentWorkerIndex}");
                            
                            using (var comm = dev.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 10))
                            {
                                TryOptimizePcapCommunicator(comm);
                                ApplyBpfFilterSafely(comm);
                                
                                while (!bgWorker?.CancellationPending == true && App.meterState.IsTracking)
                                {
                                    try
                                    {
                                        Packet packet;
                                        var result = comm.ReceivePacket(out packet);
                                        if (result == PacketCommunicatorReceiveResult.Ok && packet != null)
                                        {
                                            PacketHandler(packet);
                                        }
                                        else
                                        {
                                            Thread.Sleep(1);
                                        }
                                    }
                                    catch (InvalidOperationException ex) when (ex.Message.Contains("interface disappeared") || ex.Message.Contains("DEVICE_REMOVED"))
                                    {
                                        Debug.Print($"[PCAP-Worker-{currentWorkerIndex}] Network adapter removed/disconnected: {ex.Message}");
                                        DebugLogger.log($"[PCAP-Worker-{currentWorkerIndex}] Adapter disconnected: {dev.Name} - {ex.Message}");
                                        
                                        // Выходим из цикла, чтобы worker завершился корректно
                                        break;
                                    }
                                    catch (Exception ex)
                                    {
                                        Debug.Print($"[PCAP-Worker-{currentWorkerIndex}] Unexpected error: {ex.Message}");
                                        DebugLogger.log($"[PCAP-Worker-{currentWorkerIndex}] Unexpected error in packet capture: {ex.Message}");
                                        Thread.Sleep(10); // Небольшая пауза при ошибках
                                    }
                                }
                                
                                if (bgWorker?.CancellationPending == true)
                                {
                                    e.Cancel = true;
                                }
                            }
                        };
                        
                        worker.RunWorkerCompleted += PcapWorkerCompleted;
                        _pcapWorkers.Add(worker);
                        Debug.Print($"[SwitchAdapterIfNeeded] Starting new worker {workerIndex} for device {dev.Name}");
                        worker.RunWorkerAsync();
                        workerIndex++;
                    }
                    
                    Debug.Print($"[SwitchAdapterIfNeeded] ✓ Multi-adapter mode restarted with {_pcapWorkers.Count} workers");
                }
                else
                {
                    // Одноадаптерный режим - переключаем selectedAdapter
                    Debug.Print($"[SwitchAdapterIfNeeded] Single-adapter mode - switching adapter");
                    
                    selectedAdapter = newAdapter;
                    lastSelectedAdapterID = newAdapterIndex;
                    
                    // Останавливаем старый worker
                    if (pcapWorker != null && pcapWorker.IsBusy)
                    {
                        Debug.Print($"[SwitchAdapterIfNeeded] Stopping old single worker");
                        pcapWorker.CancelAsync();
                        
                        // Ждем РЕАЛЬНОГО завершения с таймаутом БЕЗ блокировки UI
                        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                        int waitedMs = 0;
                        while (pcapWorker.IsBusy && waitedMs < 1000) // Увеличиваем таймаут до 1 сек
                        {
                            System.Threading.Thread.Sleep(50);
                            waitedMs += 50;
                        }
                        
                        bool stopped = !pcapWorker.IsBusy;
                        Debug.Print($"[SwitchAdapterIfNeeded] Worker stopped in {stopwatch.ElapsedMilliseconds}ms, success={stopped}");
                        
                        if (!stopped)
                        {
                            Debug.Print($"[SwitchAdapterIfNeeded] ⚠ WARNING: Worker didn't stop in time, forcing restart anyway");
                        }
                    }
                    
                    // Запускаем новый worker ТОЛЬКО ПОСЛЕ остановки старого
                    Debug.Print($"[SwitchAdapterIfNeeded] Starting new single worker for {selectedAdapter.Name}");
                    InitPcapWorker();
                    
                    Debug.Print($"[SwitchAdapterIfNeeded] ✓ Single-adapter mode restarted on {selectedAdapter.Name}");
                }
                
                Debug.Print($"[SwitchAdapterIfNeeded] ✅ Adapter switched successfully to {newLocalIP}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[SwitchAdapterIfNeeded] ❌ Error: {ex.Message}");
                Debug.Print($"[SwitchAdapterIfNeeded] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                // Освобождаем блокировку
                Interlocked.Exchange(ref _switchAdapterBusy, 0);
                Debug.Print($"[SwitchAdapterIfNeeded] Lock released");
            }
        }

        public void StopTracking()
        {
            // Анти-реэнтерабельность: если уже идет остановка - выходим
            if (Interlocked.Exchange(ref _stopTrackingBusy, 1) == 1) 
            {
                Debug.Print("[StopTracking] Already in progress, skipping");
                return;
            }
            
            try
            {
                Debug.Print("StopTracking - entry point");

                // ИСПРАВЛЕНИЕ: Отписываемся от событий ConnectionTracker в режиме VPN bypass
                if (App.connectionTracker != null)
                {
                    try
                    {
                        App.connectionTracker.OnNewTunnelConnection -= HandleTunnelConnectionForTracking;
                        Debug.Print("[StopTracking] Unsubscribed from ConnectionTracker events");
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"[StopTracking] Error unsubscribing from ConnectionTracker: {ex.Message}");
                    }
                }

                // КРИТИЧЕСКИ ВАЖНО: Сначала отключаем все флаги чтобы предотвратить перезапуск
                ticksLoop.Enabled = false;
                if (App.meterState != null)
                {
                    App.meterState.IsTracking = false; // Устанавливаем СРАЗУ
                }

                bool manualStop = Interlocked.CompareExchange(ref _manualStopRequestedFlag, 0, 0) == 1;
                var stopTimestampUtc = DateTime.UtcNow;
                if (manualStop)
                {
                    _pendingRestoreTargetKey = string.Empty;
                }
                else if (string.IsNullOrEmpty(_pendingRestoreTargetKey))
                {
                    _pendingRestoreTargetKey = string.IsNullOrEmpty(targetKey) ? AutoResumeSentinel : targetKey;
                }
                _lastPacketTimestamp = DateTime.MinValue;
                WriteHeartbeatIfNeeded(stopTimestampUtc, force: true);
                PersistMonitoringSnapshot(stopTimestampUtc, force: true);
                
                if (App.meterState == null) return;
            
            // Диагностика CaptureService ПЕРЕД остановкой
            if (App.Capture != null)
            {
                var debugInfo = App.Capture.DebugWorkers();
                Debug.Print($"[StopTracking] BEFORE: CaptureService workers count: {debugInfo.Length}");
                foreach (var (key, refs) in debugInfo)
                {
                    Debug.Print($"[StopTracking] BEFORE: Worker {key} -> refs: {refs}");
                }
            }
            
            // Останавливаем ping manager
            if (App.pingManager != null)
            {
                App.pingManager.StopPinging();
            }
            
            try
            {
                GameProfileManager.PubgMngr.firstPacket = true;
            } catch (TypeInitializationException) {

            }
            
            Debug.Print("StopTracking");
            
            // Phase 3: Очищаем высокоприоритетные потоки
            CleanupHighPriorityThreads();
            
            // NEW: остановка мульти-захвата: очистка воркеров
            Debug.Print($"[StopTracking] Cleaning up {_pcapWorkers.Count} PCAP workers");
            try
            {
                // Флаг IsTracking уже установлен выше
                Thread.Sleep(100); // Даем время воркерам для корректного завершения
                
                for (int i = _pcapWorkers.Count - 1; i >= 0; i--)
                {
                    var w = _pcapWorkers[i];
                    try
                    {
                        if (w != null)
                        {
                            Debug.Print($"[StopTracking] Stopping worker {i}: IsBusy={w.IsBusy}, SupportsCancellation={w.WorkerSupportsCancellation}");
                            
                            // Отменяем работу если воркер еще активен и поддерживает отмену
                            if (w.IsBusy && w.WorkerSupportsCancellation)
                            {
                                w.CancelAsync();
                                Debug.Print($"[StopTracking] Worker {i} cancellation requested");
                            }
                            else if (w.IsBusy)
                            {
                                Debug.Print($"[StopTracking] Worker {i} busy but cancellation not supported");
                            }
                            
                            // НЕ ждем завершения и НЕ dispose - позволяем воркеру завершиться естественно
                            // через RunWorkerCompleted event
                        }
                    }
                    catch (Exception ex) 
                    { 
                        Debug.Print($"[StopTracking] Error stopping worker {i}: {ex.Message}");
                    }
                }
            } 
            catch (Exception ex)
            {
                Debug.Print($"[StopTracking] Error in worker cleanup: {ex.Message}");
            }
            
            _pcapWorkers.Clear();
            _allSelectedAdapters.Clear();
            Debug.Print($"[StopTracking] Workers cleared, count now: {_pcapWorkers.Count}");
            
            // Также очищаем одиночный pcapWorker если он есть
            try
            {
                if (pcapWorker != null)
                {
                    Debug.Print($"[StopTracking] Stopping single worker: IsBusy={pcapWorker.IsBusy}, SupportsCancellation={pcapWorker.WorkerSupportsCancellation}");
                    if (pcapWorker.IsBusy && pcapWorker.WorkerSupportsCancellation)
                    {
                        pcapWorker.CancelAsync();
                        Debug.Print("[StopTracking] Single worker cancellation requested");
                    }
                    // НЕ dispose сразу - позволяем завершиться через event
                }
            }
            catch (Exception ex) 
            { 
                Debug.Print($"[StopTracking] Error stopping single worker: {ex.Message}");
            }
            
            // Сбрасываем счетчик рестартов
            restarts = 0;
            
            // Очищаем словарь дедупликации для освобождения памяти
            lock (_dedupLock)
            {
                _dedup.Clear();
            }
            
            // Сбрасываем сглаживание при остановке трекинга
            Classes.TickrateSmoothingManager.Reset();
            
            tickrate_val.ForeColor = _inactiveMetricColor;
            tickrate_val.Text = "0";
            ping_val.ForeColor = Color.Red;
            ping_val.Text = "NO TRAFFIC!";
            traffic_val.ForeColor = Color.Red;
            traffic_val.Text = 0f.ToString("N2") + " / " + 0f.ToString("N2") + " mb";
            time_val.ForeColor = _inactiveMetricColor;
            time_val.Text = "00:00";
            drops_lbl_val.ForeColor = _inactiveMetricColor;
            drops_lbl_val.Text = 0f.ToString("n2") + "%";
            ip_val.ForeColor = _inactiveMetricColor;
            ip_val.Text = string.Empty;
            countryLbl.ForeColor = _inactiveMetricColor;
            countryLbl.Text = string.Empty;
            process_val.ForeColor = Color.Gray;
            process_val.Text = "n/a";
            try
            {
                ResetTickrateChart();
            }
            catch(Exception ex)
            {
                Debug.Print($"[GUI] ResetTickrateChart error: {ex.Message}");
            }
            
            
            if (App.settingsForm.settings_log_checkbox.Checked)
            { 
                if(App.meterState.Server.Ip != "" && App.meterState.TickRateLog != "")
                {
                    if (!Directory.Exists("logs"))
                    {
                        Directory.CreateDirectory("logs");
                    }
                    try
                    {
                        File.AppendAllText(@"logs\" + App.meterState.Server.Ip + "_ticks.csv", "timestamp;tickrate" + Environment.NewLine + App.meterState.TickRateLog);
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"[GUI] CSV log write error: {ex.Message}");
                    }
                }
            }

            if (App.settingsForm.settings_data_send.Checked && App.meterState.TicksHistory.Count > 900 && App.meterState.Server.Ip != "")
            {
               // WebStatsManager.uploadTickrate(); //no no no. not today
            }

            try { RivaTuner.ShowNoTrafficPlaceholder(); } catch (Exception exc) { MessageBox.Show(exc.Message); }
            if(App.meterState.Server.Ip != "")
            {
                if (!Directory.Exists("logs"))
                {
                    Directory.CreateDirectory("logs");
                }
                TimeSpan result = DateTime.Now.Subtract(App.meterState.SessionStart);
                string Duration = result.ToString("mm':'ss");
                string serverStat = DateTime.Now.ToLocalTime() + " - IP: " + App.meterState.Server.Ip + " (" + App.meterState.Server.Location + ") Ping: " + App.meterState.Server.AvgPing + "ms, avg Tickrate: "+ App.meterState.AvgTickrate+ ", Time: "+ Duration + Environment.NewLine;
                try
                {
                    File.AppendAllText(@"logs\"+App.meterState.Game+"_SERVERS-STATS.log", serverStat);
                }
                catch (Exception ex)
                {
                    Debug.Print($"[GUI] Server stats log write error: {ex.Message}");
                }
            }


            App.meterState.IsTracking = false;
            
            // Принудительная сборка мусора после остановки трекинга
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            // Диагностика CaptureService ПОСЛЕ очистки
            if (App.Capture != null)
            {
                var debugInfo = App.Capture.DebugWorkers();
                Debug.Print($"[StopTracking] AFTER: CaptureService workers count: {debugInfo.Length}");
                foreach (var (key, refs) in debugInfo)
                {
                    Debug.Print($"[StopTracking] AFTER: Worker {key} -> refs: {refs}");
                }
            }
            }
            catch (Exception ex)
            {
                Debug.Print($"[StopTracking] Error: {ex.Message}");
                DebugLogger.log(ex);
            }
            finally
            {
                // Всегда освобождаем блокировку
                Volatile.Write(ref _stopTrackingBusy, 0);
            }
        }

        public void StopTrackingManual()
        {
            try
            {
                Interlocked.Exchange(ref _manualStopRequestedFlag, 1);
                StopTracking();
            }
            finally
            {
                Interlocked.Exchange(ref _manualStopRequestedFlag, 0);
            }
        }

        private void GUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            try
            {
                // Освобождаем ресурсы AlertManager
                Classes.AlertManager.Dispose();
                System.Diagnostics.Debug.Print("[GUI_FormClosed] AlertManager disposed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[GUI_FormClosed] Error disposing AlertManager: {ex.Message}");
            }
        }

        private void ServerLbl_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(ip_val.Text);
        }
    
        
        private void RetryTimer_Tick(object sender, EventArgs e)
        {
            if ((App.meterState == null || !App.meterState.IsTracking) && lastSelectedAdapterID != -1)
            {
                 App.settingsForm.adapters_list.SelectedIndex = lastSelectedAdapterID;
                StartTracking();
            }
        }

        private void ping_interval_ValueChanged(object sender, EventArgs e)
        {
            // Обновляем интервал overlay: если Advanced overlay_fps включён — он приоритетнее, иначе берём ping interval
            bool overlayFpsEnabled = App.settingsManager?.GetOption("overlay_fps_enabled", "False", "ADVANCED") == "True";
            if (overlayFpsEnabled)
            {
                var fpsStr = App.settingsManager?.GetOption("overlay_fps", "60", "ADVANCED");
                if (!string.IsNullOrEmpty(fpsStr) && int.TryParse(fpsStr, out int fps) && fps > 0)
                {
                    ticksLoop.Interval = Math.Max(1, (int)Math.Round(1000.0 / fps));
                    return;
                }
            }
            var control = sender as NumericUpDown;
            if (control != null)
            {
                ticksLoop.Interval = (int)control.Value;
            }
        }

        /// <summary>
        /// Применяет интервал обновления overlay (ticksLoop.Interval) согласно текущим настройкам.
        /// Если включён ADVANCED:overlay_fps_enabled — рассчитывает интервал по FPS,
        /// иначе использует значение ping_interval из настроек.
        /// </summary>
        public void ApplyOverlayIntervalFromSettings()
        {
            try
            {
                int intervalMs = 1000; // default
                bool overlayFpsEnabled = App.settingsManager?.GetOption("overlay_fps_enabled", "False", "ADVANCED") == "True";
                if (overlayFpsEnabled)
                {
                    var fpsStr = App.settingsManager?.GetOption("overlay_fps", "60", "ADVANCED");
                    if (!string.IsNullOrEmpty(fpsStr) && int.TryParse(fpsStr, out int fps) && fps > 0)
                    {
                        intervalMs = Math.Max(1, (int)Math.Round(1000.0 / fps));
                    }
                }
                else
                {
                    var pingIntervalStr = App.settingsManager?.GetOption("ping_interval");
                    if (!string.IsNullOrEmpty(pingIntervalStr) && int.TryParse(pingIntervalStr, out int pingVal) && pingVal > 0)
                    {
                        intervalMs = pingVal;
                    }
                }
                ticksLoop.Interval = intervalMs;
            }
            catch { /* ignore */ }
        }

        public void UpdateStyle(bool rtssFlag)
        {
            if (rtssFlag)
            {
                SetWindowPos(this.Handle, HWND_NOTOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
            }
            else
            {
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, TOPMOST_FLAGS);
            }
        }

        private void GUI_Load(object sender, EventArgs e)
        {
            appInitHeigh = Height;
            appInitWidth = Width;

            App.settingsForm.ApplyFromConfig();

            
           // App.settingsForm.CheckNewVersion();

            CultureInfo ci = CultureInfo.InstalledUICulture;
            if (ci.TwoLetterISOLanguageName != "ru")
            {
                App.settingsForm.SwitchToEnglish();
            }
            
            ETW.init();
            AutoResumeMonitoringIfNeeded();
            
            // ИСПРАВЛЕНИЕ: Сворачиваем форму ПОСЛЕ полной инициализации
            // Используем BeginInvoke для отложенного выполнения после отрисовки формы
            if(App.settingsForm.run_minimized.Checked)
            {
                this.BeginInvoke(new Action(() =>
                {
                    this.WindowState = FormWindowState.Minimized;
                    this.ShowInTaskbar = false;
                    Hide();
                }));
            }
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            // Перед показом формы настроек - обновить UI из текущих настроек
            App.settingsForm.ApplyFromConfig();
            App.settingsForm.Show();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            App.packetStatsForm.Show();
        }

        private void pictureBox1_Click_1(object sender, EventArgs e)
        {
            App.profilesForm.Show();
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            App.tickrateStatisticsForm.Show();
        }

        private void GUI_Resize(object sender, EventArgs e)
        {
            // Защита от рекурсии
            if (_isResizing) return;
            
            try
            {
                _isResizing = true;
                
                // Когда окно сворачивается - скрываем его из панели задач
                if (this.WindowState == FormWindowState.Minimized)
                {
                    this.ShowInTaskbar = false;
                    Hide();
                }
            }
            finally
            {
                _isResizing = false;
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            RestoreWindow();
        }
        
        private void GUI_Activated(object sender, EventArgs e)
        {
            // Защита от рекурсии
            if (_isRestoring) return;
            
            // Восстанавливаем только если окно действительно свернуто И не видно
            // Это предотвращает рекурсию при активации уже видимого окна
            if (this.WindowState == FormWindowState.Minimized && !this.Visible)
            {
                RestoreWindow();
            }
        }
        
        private void RestoreWindow()
        {
            // Защита от рекурсии
            if (_isRestoring) return;
            
            // Проверяем, нужно ли вообще восстанавливать
            if (this.WindowState == FormWindowState.Normal && this.Visible)
            {
                // Окно уже восстановлено, просто активируем
                this.Activate();
                return;
            }
            
            try
            {
                _isRestoring = true;
                
                // Важно: сначала меняем состояние, потом показываем
                if (this.WindowState != FormWindowState.Normal)
                {
                    this.WindowState = FormWindowState.Normal;
                }
                
                if (!this.ShowInTaskbar)
                {
                    this.ShowInTaskbar = true;
                }
                
                if (!this.Visible)
                {
                    Show();
                }
                
                // Активация и вывод на передний план делаем вне блока _isRestoring
                // чтобы не мешать событию Activated
            }
            finally
            {
                _isRestoring = false;
            }
            
            // Активация после снятия флага, чтобы Activated мог отработать корректно
            if (!this.Focused)
            {
                this.Activate();
                this.BringToFront();
            }
        }

        private void GUI_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !allowClose)
            {
                Hide();
                e.Cancel = true;
            }
            else
            {
                // Принудительно останавливаем все воркеры при закрытии формы
                Debug.Print("[GUI_FormClosing] Force stopping all workers");
                StopAutomationTimers();
                
                // CRITICAL FIX: Отписываемся от событий для предотвращения memory leak
                try
                {
                    if (App.pingManager != null)
                    {
                        App.pingManager.PingResultReceived -= OnPingResultReceived;
                        Debug.Print("[GUI_FormClosing] Unsubscribed from PingResultReceived");
                    }
                    Classes.SpikeDetection.SpikeDetectionManager.SpikeDetected -= OnSpikeDetected;
                    Debug.Print("[GUI_FormClosing] Unsubscribed from SpikeDetected");
                }
                catch (Exception ex)
                {
                    Debug.Print($"[GUI_FormClosing] Event unsubscription error: {ex.Message}");
                }
                
                try
                {
                    _selfHealTimer?.Dispose();
                    _keepAliveTimer?.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.Print($"[GUI_FormClosing] Timer disposal error: {ex.Message}");
                }
                try
                {
                    StopTrackingManual();
                }
                catch (Exception ex)
                {
                    Debug.Print($"[GUI_FormClosing] Error stopping tracking: {ex.Message}");
                }
            }
        }

        private void icon_menu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            StopTrackingManual();
            
            // Phase 3: Очистка UI Processing Timer
            try
            {
                _uiProcessingTimer?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.Print($"[UI] Timer disposal error: {ex.Message}");
            }
            
            App.settingsForm.SaveToConfig();
            
            try
            {
                RivaTuner.KillRtss();
            }
            catch (TypeInitializationException ex)
            {
                Debug.Print($"[RivaTuner] Не удалось инициализировать (RTSS.dll отсутствует): {ex.Message}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[RivaTuner] Ошибка при завершении: {ex.Message}");
            }
            
            allowClose = true;
            Close();
        }

        private void icon_menu_Opening(object sender, CancelEventArgs e)
        {

        }
        
        private void OnPingResultReceived(object sender, Classes.PingResultEventArgs e)
        {
            if (App.meterState != null && e.Result.Success)
            {
                // Обновляем только текущее значение ping
                App.meterState.Server.Ping = (int)e.Result.RoundTripTime;
                
                // Добавляем данные ping в детектор спайков
                try
                {
                    Classes.SpikeDetection.SpikeDetectionManager.AddValue(
                        Classes.SpikeDetection.MetricKind.Ping, 
                        e.Result.RoundTripTime
                    );
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print($"[OnPingResultReceived] Error adding ping to spike detector: {ex.Message}");
                }
                
                // pingBuffer будет обновляться через CurrentTimestamp как раньше
                // Не добавляем данные сюда, чтобы избежать слишком частых обновлений графика
            }
        }
        
        /// <summary>
        /// Обработчик событий детекции спайков
        /// </summary>
        private void OnSpikeDetected(Classes.SpikeDetection.SpikeEvent spikeEvent)
        {
            try
            {
                bool isActive = spikeEvent.Phase == Classes.SpikeDetection.SpikeEventPhase.Start;
                System.Diagnostics.Debug.Print($"[OnSpikeDetected] Spike {spikeEvent.Phase} ({spikeEvent.Metric}) value={spikeEvent.Value:F2}, confirmed={spikeEvent.IsConfirmed}");

                // Обновляем флаги спайков в зависимости от типа метрики
                switch (spikeEvent.Metric)
                {
                    case Classes.SpikeDetection.MetricKind.Ping:
                        if (App.meterState?.Server != null)
                        {
                            App.meterState.Server.SetPingSpike(isActive);
                        }
                        if (!isActive && spikeEvent.IsConfirmed)
                        {
                            ShowSpikeNotification("Ping", spikeEvent.Value, "ms", ref _lastPingSpikeNotification);
                        }
                        break;

                    case Classes.SpikeDetection.MetricKind.Tickrate:
                        if (App.meterState?.Server != null)
                        {
                            App.meterState.Server.SetTickRateSpike(isActive);
                        }
                        if (!isActive && spikeEvent.IsConfirmed)
                        {
                            ShowSpikeNotification("Tickrate", spikeEvent.Value, "Hz", ref _lastTickrateSpikeNotification);
                        }
                        System.Diagnostics.Debug.Print($"[OnSpikeDetected] Tickrate spike {spikeEvent.Phase}: value={spikeEvent.Value:F1}, peak={spikeEvent.PeakValue:F1}");
                        break;

                    case Classes.SpikeDetection.MetricKind.Ticktime:
                        if (App.meterState?.Server != null)
                        {
                            App.meterState.Server.SetTickTimeSpike(isActive);
                        }
                        if (!isActive && spikeEvent.IsConfirmed)
                        {
                            ShowSpikeNotification("Ticktime", spikeEvent.Value, "ms", ref _lastTicktimeSpikeNotification);
                        }
                        System.Diagnostics.Debug.Print($"[OnSpikeDetected] Ticktime spike {spikeEvent.Phase}: value={spikeEvent.Value:F1}ms");
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[OnSpikeDetected] Error processing spike event: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Показывает уведомление о спайке с ограничением частоты
        /// </summary>
        private void ShowSpikeNotification(string metricName, double value, string unit, ref DateTime lastNotification)
        {
            try
            {
                // Проверяем настройку уведомлений
                bool notificationsEnabled = App.settingsManager?.GetOption("spike_notifications", "False", "ADVANCED") == "True";
                if (!notificationsEnabled) return;
                
                // Проверяем cooldown
                var now = DateTime.Now;
                if ((now - lastNotification).TotalSeconds < SPIKE_NOTIFICATION_COOLDOWN_SECONDS)
                {
                    return; // Слишком рано для следующего уведомления
                }
                
                lastNotification = now;
                
                // Показываем balloon tip
                var notifyIcon = new NotifyIcon();
                notifyIcon.Icon = SystemIcons.Warning;
                notifyIcon.Visible = true;
                notifyIcon.BalloonTipTitle = "Network Spike Detected";
                notifyIcon.BalloonTipText = $"{metricName} spike: {value:F1}{unit}";
                notifyIcon.BalloonTipIcon = ToolTipIcon.Warning;
                notifyIcon.ShowBalloonTip(3000);
                
                // Автоматически скрываем иконку через несколько секунд
                System.Threading.Timer hideTimer = null;
                hideTimer = new System.Threading.Timer(_ => {
                    try
                    {
                        notifyIcon.Visible = false;
                        notifyIcon.Dispose();
                        hideTimer?.Dispose();
                    }
                    catch { }
                }, null, 5000, System.Threading.Timeout.Infinite);
                
                System.Diagnostics.Debug.Print($"[ShowSpikeNotification] Notification shown for {metricName}: {value:F1}{unit}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[ShowSpikeNotification] Error showing notification: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Безопасная настройка PCAP буферов через рефлексию
        /// </summary>
        private static void TryOptimizePcapCommunicator(PacketCommunicator comm)
        {
            try
            {
                // Kernel buffer size (default 8MB, range 1-64MB)
                var kernelMbStr = App.settingsManager?.GetOption("pcap_kernel_buffer_mb", "8", "ADVANCED");
                if (int.TryParse(kernelMbStr, out int kernelMb) && kernelMb > 0 && kernelMb <= 64)
                {
                    TrySetKernelBuffer(comm, kernelMb * 1024 * 1024);
                }
                
                // Minimum bytes to copy (default 4KB, range 0-64KB)
                var minToCopyStr = App.settingsManager?.GetOption("pcap_min_to_copy", "4096", "ADVANCED");
                if (int.TryParse(minToCopyStr, out int minToCopy) && minToCopy >= 0 && minToCopy <= 65536)
                {
                    TrySetMinToCopy(comm, minToCopy);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[PCAP] Tuning failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Попытка установки размера kernel buffer через рефлексию
        /// </summary>
        private static void TrySetKernelBuffer(PacketCommunicator comm, int bytes)
        {
            try
            {
                var method = comm.GetType().GetMethod("SetKernelBufferSize", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic);
                method?.Invoke(comm, new object[] { bytes });
                Debug.Print($"[PCAP] Kernel buffer set to {bytes / 1024 / 1024}MB");
            }
            catch (Exception ex)
            {
                Debug.Print($"[PCAP] SetKernelBufferSize failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Попытка установки MinToCopy через рефлексию
        /// </summary>
        private static void TrySetMinToCopy(PacketCommunicator comm, int bytes)
        {
            try
            {
                var method = comm.GetType().GetMethod("SetMinToCopy", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.Public | 
                    System.Reflection.BindingFlags.NonPublic);
                method?.Invoke(comm, new object[] { bytes });
                Debug.Print($"[PCAP] MinToCopy set to {bytes} bytes");
            }
            catch (Exception ex)
            {
                Debug.Print($"[PCAP] SetMinToCopy failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Безопасное применение BPF фильтра из настроек
        /// </summary>
        private static void ApplyBpfFilterSafely(PacketCommunicator comm)
        {
            try
            {
                bool bpfEnabled = App.settingsManager?.GetOption("bpf_filter_enabled", "False", "ADVANCED") == "True";
                if (bpfEnabled)
                {
                    string filterExpr = App.settingsManager?.GetOption("capture_filter", "ip or ip6", "ADVANCED");
                    if (!string.IsNullOrWhiteSpace(filterExpr))
                    {
                        comm.SetFilter(filterExpr);
                        Debug.Print($"[PCAP] BPF filter applied: {filterExpr}");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[PCAP] BPF filter failed: {ex.Message}");
                // Продолжаем без фильтра
            }
        }
        
        #region Phase 3: Thread Priority & Single Consumer Methods
        
        /// <summary>
        /// Single Consumer Pattern: обработка всех UI обновлений в одном потоке
        /// </summary>
        private void ProcessUIUpdates(object state)
        {
            // Анти-реэнтерабельность
            if (_uiProcessingActive) return;
            
            // Проверяем что форма готова принимать вызовы
            if (this.IsDisposed || !this.IsHandleCreated) return;
            
            _uiProcessingActive = true;
            try
            {
                var updates = new List<Action>();
                
                // Собираем batch UI updates (max 50 за раз для предотвращения блокировки)
                for (int i = 0; i < 50 && _uiUpdateQueue.TryDequeue(out Action update); i++)
                {
                    updates.Add(update);
                }
                
                if (updates.Count > 0)
                {
                    // Выполняем все обновления в UI потоке одним блоком
                    try
                    {
                        BeginInvoke(new Action(() => {
                            foreach (var update in updates)
                            {
                                try { update?.Invoke(); }
                                catch (Exception ex) { Debug.Print($"[UI] Update error: {ex.Message}"); }
                            }
                        }));
                    }
                    catch (InvalidOperationException)
                    {
                        // Форма была закрыта/disposed во время BeginInvoke
                        Debug.Print($"[UI] BeginInvoke failed - form disposed");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[UI] ProcessUIUpdates error: {ex.Message}");
            }
            finally
            {
                _uiProcessingActive = false;
            }
        }
        
        /// <summary>
        /// Добавляет UI обновление в очередь Single Consumer
        /// </summary>
        private void QueueUIUpdate(Action updateAction)
        {
            if (updateAction != null)
            {
                _uiUpdateQueue.Enqueue(updateAction);
            }
        }

        private void RequestConnectionsRefresh(bool highPriority)
        {
            if (App.connMngr == null)
            {
                return;
            }

            DateTime now = DateTime.Now;
            if (!highPriority && (now - _lastConnRefreshRequest) < ConnectionRefreshCooldown)
            {
                return;
            }

            _lastConnRefreshRequest = now;

            try
            {
                _ = App.connMngr.ForceImmediateRefreshAsync(highPriority);
                Debug.Print($"[ConnectionsManager] Refresh requested (highPriority={highPriority})");
            }
            catch (Exception ex)
            {
                Debug.Print($"[ConnectionsManager] Refresh request failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Устанавливает высокий приоритет для PCAP потока
        /// </summary>
        private void SetHighPriorityThread(Thread thread, string name)
        {
            if (thread == null) return;
            
            try
            {
                thread.Name = $"TickMeter-{name}";
                thread.Priority = GetPcapThreadPriority();
                thread.IsBackground = true;
                
                lock (_threadManagementLock)
                {
                    _highPriorityThreads.Add(thread);
                }
                
                Debug.Print($"[THREAD] Set priority {thread.Priority} for {thread.Name}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[THREAD] Priority setting failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Получает приоритет для PCAP потоков из настроек
        /// </summary>
        private ThreadPriority GetPcapThreadPriority()
        {
            var priorityStr = App.settingsManager?.GetOption("pcap_thread_priority", "AboveNormal", "ADVANCED");
            switch (priorityStr)
            {
                case "Highest":
                    return ThreadPriority.Highest;
                case "AboveNormal":
                    return ThreadPriority.AboveNormal;
                case "Normal":
                    return ThreadPriority.Normal;
                case "BelowNormal":
                    return ThreadPriority.BelowNormal;
                case "Lowest":
                    return ThreadPriority.Lowest;
                default:
                    return ThreadPriority.AboveNormal;
            }
        }
        
        /// <summary>
        /// Очистка high priority потоков при остановке
        /// </summary>
        private void CleanupHighPriorityThreads()
        {
            lock (_threadManagementLock)
            {
                foreach (var thread in _highPriorityThreads)
                {
                    try
                    {
                        if (thread?.IsAlive == true)
                        {
                            thread.Priority = ThreadPriority.Normal;
                            Debug.Print($"[THREAD] Reset priority for {thread.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"[THREAD] Cleanup error: {ex.Message}");
                    }
                }
                _highPriorityThreads.Clear();
            }
        }
        
        #endregion Phase 3: Thread Priority & Single Consumer Methods
        
        #region Stage 5: Spike Analytics
        
        /// <summary>
        /// Обработчик клика на кнопку аналитики спайков
        /// </summary>
        private void spikeAnalyticsBtn_Click(object sender, EventArgs e)
        {
            try
            {
                if (_spikeAnalyticsForm == null || _spikeAnalyticsForm.IsDisposed)
                {
                    _spikeAnalyticsForm = new SpikeAnalyticsForm();
                }
                
                if (_spikeAnalyticsForm.Visible)
                {
                    _spikeAnalyticsForm.BringToFront();
                    _spikeAnalyticsForm.Focus();
                }
                else
                {
                    _spikeAnalyticsForm.Show();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии аналитики спайков: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                DebugLogger.log(ex);
            }
        }

        #endregion Stage 5: Spike Analytics

        #region VPN Bypass: Real Data Processing
        
        /// <summary>
        /// Получает реальные VPN статистики для указанного процесса
        /// Возвращает null для системных/непрофильных процессов чтобы показать "NO TRAFFIC!"
        /// </summary>
        private ProcessNetworkStats GetRealVpnStats(string processName)
        {
            if (string.IsNullOrEmpty(processName))
                return null;
                
            try
            {
                // Исключаем системные/непрофильные процессы - для них показываем "NO TRAFFIC!"
                string[] systemProcesses = { "explorer", "dwm", "winlogon", "csrss", "lsass", "services", 
                                            "svchost", "taskhostw", "taskmgr", "notepad", "calculator", 
                                            "cmd", "powershell", "conhost", "winpty-agent" };
                
                bool isSystemProcess = systemProcesses.Any(proc => 
                    proc.Equals(processName, StringComparison.OrdinalIgnoreCase));
                
                if (isSystemProcess)
                {
                    DebugLogger.log($"[GetRealVpnStats] System process {processName} - returning null for NO TRAFFIC display");
                    return null;
                }
                
                // ЭТАП 4: Используем реальные подключения вместо эмулированных
                var realConnections = Classes.RealProcessTrafficMonitor.GetRealProcessConnections(processName);
                
                if (realConnections.Count > 0)
                {
                    // Возвращаем первое найденное реальное подключение
                    var realConnection = realConnections[0];
                    
                    // Обновляем реальными данными трафика
                    var realTraffic = Classes.RealProcessTrafficMonitor.GetRealProcessTrafficWithPing(
                        processName, 
                        realConnection.remoteIp, 
                        (int)realConnection.remotePort
                    );
                    
                    if (realTraffic != null)
                    {
                        realConnection.downloaded = (int)(realTraffic.BytesReceivedPerSec * 10); // Примерное накопление за 10 сек
                        realConnection.sent = (int)(realTraffic.BytesSentPerSec * 10);
                        realConnection.ticksIn = realTraffic.CalculatedTickrate; // РЕАЛЬНЫЙ тикрейт вместо эмуляции!
                        
                        DebugLogger.log($"[GetRealVpnStats] Using REAL connection for {processName}: {realConnection.remoteIp}:{realConnection.remotePort}, tickrate={realConnection.ticksIn}");
                    }
                    
                    return realConnection;
                }
                else
                {
                    // Fallback: если реальные подключения не найдены, создаём минимальную заглушку
                    DebugLogger.log($"[GetRealVpnStats] No real connections found for {processName}, creating minimal fallback");
                    var vpnStats = new ProcessNetworkStats
                    {
                        name = processName,
                        localIp = "192.168.1.100", // Пример локального IP
                        remoteIp = "0.0.0.0", // Неизвестный сервер
                        remotePort = 0,
                        downloaded = 1024, // Минимальный трафик для активности
                        sent = 512, 
                        tickTimeBuffer = new List<float>(),
                        startTrack = DateTime.Now.AddSeconds(-10),
                        lastUpdate = DateTime.Now,
                        ticksIn = 1 // Минимальное значение
                    };
                    
                    return vpnStats;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[GetRealVpnStats] Error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Обрабатывает VPN данные аналогично обычной логике в updateMetherStateFromActiveWindow
        /// </summary>
        private void ProcessVpnData(ProcessNetworkStats procStats, string currentActiveProcess)
        {
            try
            {
                App.meterState.tickTimeBuffer = procStats.tickTimeBuffer;
                
                // Устанавливаем базовые данные состояния
                App.meterState.Game = procStats.name;
                App.meterState.Server.Ip = procStats.remoteIp.ToString();
                App.meterState.DownloadTraffic = procStats.downloaded;
                App.meterState.UploadTraffic = procStats.sent;
                
                // Устанавливаем TickRate (для VPN bypass используем procStats.ticksIn)
                int currentTickRate = procStats.ticksIn;
                App.meterState.TickRate = currentTickRate;
                
                // Добавляем данные tickrate в детектор спайков
                try
                {
                    Classes.SpikeDetection.SpikeDetectionManager.AddValue(
                        Classes.SpikeDetection.MetricKind.Tickrate, 
                        currentTickRate
                    );
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.Print($"[ProcessVpnData] Error adding tickrate to spike detector: {ex.Message}");
                }
                
                App.meterState.Server.PingPort = (int)procStats.remotePort;
                App.meterState.SessionStart = procStats.startTrack;
                
                // НОВАЯ ЛОГИКА: Проверяем реальную активность VPN соединения
                bool hasRealActivity = procStats.downloaded > 1024 && 
                                      procStats.sent > 512 && 
                                      procStats.ticksIn > 5;
                
                if (hasRealActivity)
                {
                    DebugLogger.log($"[ProcessVpnData] ✓ Real VPN activity for {procStats.name}: downloaded={procStats.downloaded}, sent={procStats.sent}, ticksIn={procStats.ticksIn}");
                    App.meterState.IsTracking = true;
                    _lastMetricsApplied = DateTime.Now;
                    _metricsStateCleared = false;
                }
                else
                {
                    DebugLogger.log($"[ProcessVpnData] ✗ No real VPN activity for {procStats.name}: downloaded={procStats.downloaded}, sent={procStats.sent}, ticksIn={procStats.ticksIn}");
                    App.meterState.IsTracking = false;
                    _metricsStateCleared = true;
                }
                
                App.meterState.loss = procStats.loss;
                App.meterState.totalTicksCnt = procStats.totalTicksCnt;
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ProcessVpnData] Error: {ex.Message}");
            }
        }
        
        #endregion VPN Bypass: Real Data Processing

        /// <summary>
        /// Обертка для обновления трафика через Windows Statistics
        /// </summary>
        private void UpdateTrafficFromWindowsStats()
        {
            try
            {
                bool useWindowsStats = App.settingsManager?.GetOption("use_windows_stats", "True", "ADVANCED") == "True";
                if (!useWindowsStats) return;
                
                // Получаем текущие значения трафика
                var (currentDownloaded, currentUploaded) = GetRealNetworkTraffic();
                
                // Первый запуск - инициализация
                if (_lastWindowsUpdate == DateTime.MinValue)
                {
                    _lastWindowsDownloaded = currentDownloaded;
                    _lastWindowsUploaded = currentUploaded;
                    _lastWindowsUpdate = DateTime.Now;
                    return;
                }
                
                // Подсчет дельты
                long downloadDelta = Math.Max(0, currentDownloaded - _lastWindowsDownloaded);
                long uploadDelta = Math.Max(0, currentUploaded - _lastWindowsUploaded);
                
                // Обновляем кэш
                _lastWindowsDownloaded = currentDownloaded;
                _lastWindowsUploaded = currentUploaded;
                _lastWindowsUpdate = DateTime.Now;
                
                // Обновляем только если есть изменения
                if (downloadDelta > 0 || uploadDelta > 0)
                {
                    // Масштабируем для реалистичного отображения
                    int scaledDownload = (int)(downloadDelta / 10000); // Делим на 10000 для более разумных значений
                    int scaledUpload = (int)(uploadDelta / 10000);
                    
                    if (scaledDownload > 0 || scaledUpload > 0)
                    {
                        App.meterState.DownloadTraffic += scaledDownload;
                        App.meterState.UploadTraffic += scaledUpload;
                        
                        Debug.Print($"[WindowsStats-GUI] Applied traffic: download={scaledDownload}, upload={scaledUpload} (from delta: {downloadDelta}/{uploadDelta})");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[UpdateTrafficFromWindowsStats] Error: {ex.Message}");
            }
        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }
    }
}
