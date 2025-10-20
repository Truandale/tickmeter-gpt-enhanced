using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using System.Threading.Tasks;
using System.Security.Permissions;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Diagnostics;
using tickMeter.Classes;
using System.Threading;
using System.Net.Sockets;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;

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
        // простая защита от дублей на бриджах/VPN
        private readonly Dictionary<ulong, long> _dedup = new Dictionary<ulong, long>(capacity: 4096);
        private readonly Stopwatch _dedupSw = Stopwatch.StartNew();
        private readonly object _dedupLock = new object();
        
        // Константы для дедупликации
        private const int MAX_DEDUP_SIZE = 8192;  // Уменьшен с 20000
        private const int DEDUP_CLEANUP_THRESHOLD = 500;  // Более частая очистка
        
        // Phase 3: Thread Priority & Single Consumer управление
        private readonly List<Thread> _highPriorityThreads = new List<Thread>();
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
        
        // Анти-реэнтерабельность для StartTracking/StopTracking (предотвращение роста воркеров)
        private int _startTrackingBusy = 0;
        private int _stopTrackingBusy = 0;
        
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
                App.Init();
                App.gui = this;
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
                                // TODO: подменить данные пакета для отображения реального процесса
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"VPN bypass error: {ex.Message}");
                }
            }
            
            try
            {
                GameProfileManager.CallBuitInProfiles(packet);
                GameProfileManager.CallCustomProfiles(packet);
                ActiveWindowTracker.AnalyzePacket(packet);
            }
            catch (IndexOutOfRangeException)
            {
                // Игнорируем поврежденные пакеты в профилях
                return;
            }
            catch (Exception)
            {
                // Игнорируем любые другие ошибки в обработке профилей
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

        bool RTSS_Failed = false;
        
        private async void TicksLoop_Tick(object sender, EventArgs e)
        {
            // Анти-реэнтерабельность: если предыдущий тик еще не завершен - пропускаем
            if (Interlocked.Exchange(ref _tickBusy, 1) == 1) 
            {
                Debug.Print("[GUI] Tick skipped - previous still running");
                return;
            }
            
            try
            {
                AutoDetectMngr.GetActiveProcessName(true);
                if(!App.meterState.isBuiltInProfileActive && !App.meterState.isCustomProfileActive)
                {
                    updateMetherStateFromActiveWindow();
                }
                
                // Троттлинг RTSS: обновляем не каждый тик, а по таймеру
                bool rtssThrottlingEnabled = App.settingsManager.GetOption("rtss_throttling", "True", "ADVANCED") == "True";
                int throttlePeriod = rtssThrottlingEnabled ? RtssPeriodMs : 50; // Если throttling отключен, обновляем чаще
                if (App.settingsForm.settings_rtss_output.Checked && _rtssSw.ElapsedMilliseconds >= throttlePeriod)
                {
                    await Task.Run(() => {
                        try { 
                            RivaTuner.BuildRivaOutput(); 
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

            // === ChatGPT ENHANCED: Snapshot-based unified zoning ===
            // Use SAME snapshot as RTSS for perfect consistency
            var snap = Classes.UnifiedDataSource.Snapshot();
            var profile = App.settingsManager.GetColorZoneProfile();
            var zoner = Classes.Zoner.FromProfile(profile, snap.TargetHz);
            
            // Calculate zones using SAME snapshot data as RTSS
            var pingZone = zoner.FromPing(snap.PingAvgMs);
            var tickrateZone = zoner.FromTickrate(snap.TickrateAvgHz);
            var ticktimeZone = zoner.FromTicktime(snap.TicktimeAvgMs);
            
            // Convert zones to colors - SAME mapping for GUI and RTSS
            Color PingColor = Classes.ZoneColors.ToColor(pingZone);
            Color TickRateColor = Classes.ZoneColors.ToColor(tickrateZone);
            bool hasActiveSession = App.meterState.IsTracking &&
                                    App.meterState.Server != null &&
                                    !string.IsNullOrEmpty(App.meterState.Server.Ip);
            
            // ChatGPT Enhancement: Snapshot-based diagnostic for perfect consistency
            System.Diagnostics.Debug.Print($"[ZONER GUI] {zoner.GetDiagnostic(snap)}");
            
            await Task.Run(
                    () => {
                        // Always update PING (including spike indicators) for both GUI and RTSS overlay
                        if (App.settingsForm.settings_ping_checkbox.Checked)
                        {
                            // Phase 3: Single Consumer Pattern - queue UI updates instead of direct Invoke
                            QueueUIUpdate(() =>
                            {
                                var server = App.meterState.Server;
                                if (hasActiveSession && server != null && !string.IsNullOrEmpty(server.Location))
                                {
                                    countryLbl.Text = server.Location;
                                    countryLbl.ForeColor = _neutralActiveColor;
                                }
                                else
                                {
                                    countryLbl.Text = string.Empty;
                                    countryLbl.ForeColor = _inactiveMetricColor;
                                }
                            });
                            QueueUIUpdate(() => {
                                var server = App.meterState.Server;
                                string pingText;
                                int rawPing = 0;
                                
                                // Определяем сырое значение пинга UDP > TCP > ICMP
                                if (App.meterState.TcpPing >= 1000 && App.meterState.IsUdpPingValid)
                                {
                                    rawPing = (int)Math.Round(server.UdpPing);
                                }
                                else if (server.Ping > 0 && server.Ping < 10000)
                                {
                                    rawPing = server.Ping;
                                }
                                else if (App.meterState.IcmpPing > 0 && App.meterState.IcmpPing < 1000)
                                {
                                    rawPing = App.meterState.IcmpPing;
                                }
                                
                                // Применяем сглаживание если включено и есть валидные данные
                                if (rawPing > 0)
                                {
                                    int displayPing = Classes.SmoothingManager.SmoothPingValueGui(rawPing);
                                    pingText = $"{displayPing} ms";
                                }
                                else
                                {
                                    pingText = "n/a ms";
                                }
                                
                                // Применяем цвет на основе зоны (ПРИОРИТЕТ ЗОНЫ)
                                Color finalPingColor = hasActiveSession ? PingColor : _inactiveMetricColor;
                                
                                // Добавляем индикатор спайка если включена соответствующая настройка
                                // ВАЖНО: индикатор наследует цвет зоны, а не перезаписывает его
                                bool showSpikeIndicator = App.settingsManager?.GetOption("show_ping_spikes", "True", "ADVANCED") == "True";
                                Debug.Print($"[GUI] Spike check: HasPingSpike={server.HasPingSpike}, ShowSetting={showSpikeIndicator}, OnScreen={OnScreen}");
                                if (hasActiveSession && showSpikeIndicator && server.HasPingSpike)
                                {
                                    pingText += " (!)";
                                    // Сохраняем цвет зоны - индикатор спайка того же цвета что и значение
                                    Debug.Print($"[GUI] Spike indicator added with zone color: {pingText}");
                                }
                                
                                // Применяем финальный цвет (цвет зоны сохраняется)
                                ping_val.ForeColor = finalPingColor;
                                ping_val.Text = hasActiveSession ? pingText : "n/a ms";
                            });
                        }
                        
                        // Only update other GUI elements if GUI overlay is visible
                        if (!skipGUIUpdate)
                        {
                            // Phase 3: Single Consumer Pattern - queue UI updates
                            QueueUIUpdate(() => {
                                string tickrateText = App.meterState.OutputTickRate.ToString();
                                Color finalTickRateColor = hasActiveSession
                                    ? TickRateColor
                                    : _inactiveMetricColor;
                                
                                // Добавляем индикатор спайка для tickrate если включена соответствующая настройка
                                bool showTickrateSpikes = App.settingsManager?.GetOption("show_tickrate_spikes", "True", "ADVANCED") == "True";
                                if (hasActiveSession && showTickrateSpikes && App.meterState.HasTickRateSpike)
                                {
                                    tickrateText += " (!)";
                                    // Сохраняем цвет зоны - индикатор спайка того же цвета что и значение
                                    // finalTickRateColor остается TickRateColor (цвет зоны)
                                    Debug.Print($"[GUI] Tickrate spike indicator added with zone color: {tickrateText}");
                                }
                                
                                tickrate_val.Text = tickrateText;
                                tickrate_val.ForeColor = finalTickRateColor;
                            });
                            
                            //update tickrate chart
                            if (App.settingsForm.settings_chart_checkbox.Checked)
                            {
                                QueueUIUpdate(() => UpdateTickrateChart(App.meterState.TicksHistory, App.meterState.TickTimestamps));
                            }
                            
                            //update traffic
                            if (App.settingsForm.settings_traffic_checkbox.Checked)
                            {
                                float formatedUpload = (float)App.meterState.UploadTraffic / (1024 * 1024);
                                float formatedDownload = (float)App.meterState.DownloadTraffic / (1024 * 1024);
                                string activeTrafficText = formatedUpload.ToString("N2") + " / " + formatedDownload.ToString("N2") + " mb";
                                QueueUIUpdate(() =>
                                {
                                    if (hasActiveSession)
                                    {
                                        traffic_val.Text = activeTrafficText;
                                        traffic_val.ForeColor = _neutralActiveColor;
                                    }
                                    else
                                    {
                                        traffic_val.Text = 0f.ToString("N2") + " / " + 0f.ToString("N2") + " mb";
                                        traffic_val.ForeColor = _inactiveMetricColor;
                                    }
                                });
                            }
                            
                            //update IP
                            if (App.settingsForm.settings_ip_checkbox.Checked)
                            {
                                QueueUIUpdate(() =>
                                {
                                    ip_val.Text = hasActiveSession ? App.meterState.Server.Ip : string.Empty;
                                    ip_val.ForeColor = hasActiveSession ? _neutralActiveColor : _inactiveMetricColor;
                                });
                            }
                            
                            //update time
                            if (App.settingsForm.settings_session_time_checkbox.Checked)
                            {
                                TimeSpan result = hasActiveSession
                                    ? DateTime.Now.Subtract(App.meterState.SessionStart)
                                    : TimeSpan.Zero;
                                string duration = result.ToString("mm':'ss");
                                QueueUIUpdate(() =>
                                {
                                    if (hasActiveSession && !string.IsNullOrEmpty(App.meterState.Server.Ip))
                                    {
                                        time_val.Text = duration;
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
                                string processName = AutoDetectMngr.GetActiveProcessName();
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
                                    float dropsPercent = App.meterState.GetDropsNumber();
                                    if (hasActiveSession)
                                    {
                                        drops_lbl_val.Text = App.meterState.GetDrops() + "%";
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
                    if (App.meterState.tickTimeBuffer != null && App.meterState.tickTimeBuffer.Count > 0)
                    {
                        currentTicktime = App.meterState.tickTimeBuffer[App.meterState.tickTimeBuffer.Count - 1];
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

        private bool isValidToTrack(string key)
        {
            if(string.IsNullOrEmpty(key)) return false;
            
            try
            {
                lock(ActiveWindowTracker.connectionsLock)
                {
                    if(!ActiveWindowTracker.connections.ContainsKey(key)) return false;
                    
                    ProcessNetworkStats connection = ActiveWindowTracker.connections[key];
                    return
                        AutoDetectMngr.GetActiveProcessName() == connection.name
                        && connection.TrackingDelta() > 3
                        && connection.LastUpdateDelta() < 2
                        && connection.remoteIp != App.meterState.LocalIP
                        && connection.ticksIn > 3
                        && connection.downloaded > 0;
                }
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private void updateMetherStateFromActiveWindow()
        {
            string previousProcessName = App.meterState.Game;
            
            if(!isValidToTrack(targetKey))
            {
                try
                {
                    // Оптимизированный поиск без полного копирования словаря
                    string bestConnection = "";
                    int bestTicks = 0;
                    
                    lock(ActiveWindowTracker.connectionsLock)
                    {
                        foreach(var kvp in ActiveWindowTracker.connections)
                        {
                            if (kvp.Value.ticksIn > bestTicks && isValidToTrack(kvp.Key))
                            {
                                bestTicks = kvp.Value.ticksIn;
                                bestConnection = kvp.Key;
                            }
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(bestConnection))
                    {
                        targetKey = bestConnection;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Коллекция была изменена, пропускаем этот цикл
                    return;
                }
            }
            
            
            if(targetKey != "") { 
                try
                {
                    lock(ActiveWindowTracker.connectionsLock)
                    {
                        if(!ActiveWindowTracker.connections.ContainsKey(targetKey))
                        {
                            targetKey = "";
                            return;
                        }
                        ProcessNetworkStats procStats = ActiveWindowTracker.connections[targetKey];
                        App.meterState.tickTimeBuffer = procStats.tickTimeBuffer;
                        
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
                        
                        // В режиме мультиадаптера переопределяем LocalIP 
                        // - При смене процесса (немедленно с ResetCache)
                        // - Для того же процесса (периодически, через встроенный интервал в LocalIPDetector)
                        bool captureAll = App.settingsManager?.GetOption("capture_all_adapters", "False", "ADVANCED") == "True";
                        bool vpnBypassBasic = App.settingsManager?.GetOption("vpn_bypass_basic", "False", "ADVANCED") == "True";
                        bool vpnBypassAdvanced = App.settingsManager?.GetOption("vpn_bypass_advanced", "False", "ADVANCED") == "True";
                        
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
                                string newLocalIP = Classes.LocalIPDetector.DetectLocalIPForActiveProcess(currentProcessName);
                                
                                // Диагностика
                                Debug.Print($"[updateMetherStateFromActiveWindow] Detected LocalIP: old={App.meterState.LocalIP}, new={newLocalIP}, changed={newLocalIP != App.meterState.LocalIP}");
                                
                                if (!string.IsNullOrEmpty(newLocalIP) && newLocalIP != App.meterState.LocalIP)
                                {
                                    Debug.Print($"[updateMetherStateFromActiveWindow] LocalIP changed: {App.meterState.LocalIP} -> {newLocalIP}");
                                    App.meterState.LocalIP = newLocalIP;
                                    Debug.Print($"[updateMetherStateFromActiveWindow] LocalIP changed: {App.meterState.LocalIP} -> {newLocalIP}");
                                    App.meterState.LocalIP = newLocalIP;
                                    
                                    // Диагностика состояния формы настроек
                                    Debug.Print($"[updateMetherStateFromActiveWindow] SettingsForm state: IsNull={App.settingsForm == null}, IsHandleCreated={App.settingsForm?.IsHandleCreated}, IsDisposed={App.settingsForm?.IsDisposed}");
                                    
                                    // Обновляем UI (textbox и ComboBox адаптера)
                                    if (App.settingsForm != null && App.settingsForm.IsHandleCreated && !App.settingsForm.IsDisposed)
                                    {
                                        try
                                        {
                                            App.settingsForm.Invoke((Action)(() =>
                                            {
                                                // Обновляем textbox LocalIP
                                                if (App.settingsForm.local_ip_textbox != null && 
                                                    App.settingsForm.local_ip_textbox.Text != newLocalIP)
                                                {
                                                    App.settingsForm.local_ip_textbox.Text = newLocalIP;
                                                }
                                                
                                                // Обновляем выбранный адаптер в ComboBox (с защитой от рекурсии)
                                                if (App.settingsForm.adapters_list != null && App.settingsForm.adapters_list.Items.Count > 0)
                                                {
                                                    Debug.Print($"[updateMetherStateFromActiveWindow] Searching for adapter with IP: {newLocalIP}");
                                                    var adapters = App.GetAdapters();
                                                    Debug.Print($"[updateMetherStateFromActiveWindow] Total adapters: {adapters.Count}, Current ComboBox index: {App.settingsForm.adapters_list.SelectedIndex}");
                                                    
                                                    bool found = false;
                                                    for (int i = 0; i < adapters.Count; i++)
                                                    {
                                                        string adapterIP = App.GetAdapterAddress(adapters[i]);
                                                        Debug.Print($"[updateMetherStateFromActiveWindow] Adapter[{i}] IP: {adapterIP}, Match: {adapterIP == newLocalIP}");
                                                        
                                                        if (adapterIP == newLocalIP)
                                                        {
                                                            found = true;
                                                            if (App.settingsForm.adapters_list.SelectedIndex != i)
                                                            {
                                                                Debug.Print($"[updateMetherStateFromActiveWindow] ✓ Found! Updating adapter ComboBox index: {App.settingsForm.adapters_list.SelectedIndex} -> {i}");
                                                                
                                                                // Устанавливаем флаг чтобы избежать рекурсивного обновления
                                                                App.settingsForm.IsUpdatingAdapter = true;
                                                                try
                                                                {
                                                                    App.settingsForm.adapters_list.SelectedIndex = i;
                                                                    Debug.Print($"[updateMetherStateFromActiveWindow] ✓ ComboBox updated successfully. New index: {App.settingsForm.adapters_list.SelectedIndex}");
                                                                }
                                                                finally
                                                                {
                                                                    App.settingsForm.IsUpdatingAdapter = false;
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
                                                    Debug.Print($"[updateMetherStateFromActiveWindow] ⚠ ComboBox is null or empty");
                                                }
                                            }));
                                        }
                                        catch (Exception ex)
                                        {
                                            Debug.Print($"[updateMetherStateFromActiveWindow] UI update error: {ex.Message}");
                                        }
                                    }
                                    
                                    Debug.Print($"[updateMetherStateFromActiveWindow] ✓ Successfully updated LocalIP for process '{currentProcessName}' to {newLocalIP}");
                                    
                                    // НОВОЕ: Автоматическое переключение адаптера при активном мониторинге
                                    if (App.meterState.IsTracking)
                                    {
                                        SwitchAdapterIfNeeded(newLocalIP);
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
                        App.meterState.Server.Ip = procStats.remoteIp.ToString();
                        App.meterState.DownloadTraffic = procStats.downloaded;
                        App.meterState.UploadTraffic = procStats.sent;
                        
                        // Обновляем TickRate и добавляем в детектор спайков
                        int currentTickRate = procStats.getTicksIn();
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
                        
                        App.meterState.Server.PingPort = (int)procStats.remotePort;
                        App.meterState.SessionStart = procStats.startTrack;
                        App.meterState.IsTracking = true;
                        App.meterState.loss = procStats.loss;
                        App.meterState.totalTicksCnt = procStats.totalTicksCnt;
                    }
                }
                catch (InvalidOperationException)
                {
                    // Коллекция была изменена, пропускаем
                    targetKey = "";
                    return;
                }
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
                ticksLoop.Enabled = true;
            
            // Запускаем ping manager
            if (App.pingManager != null)
            {
                App.pingManager.StartPinging();
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
                    MessageBox.Show("Не найдено подходящих сетевых адаптеров");
                    return;
                }
            }
            else
            {
                Debug.Print($"[StartTracking] SINGLE-ADAPTER MODE - captureAll: {captureAll}, vpnBasic: {vpnBypassBasic}, vpnAdvanced: {vpnBypassAdvanced}");
                int deviceId = App.settingsForm.adapters_list.SelectedIndex;
                Debug.Print($"[StartTracking] Single adapter mode - deviceId: {deviceId}, devices.Count: {devices.Count}");
                
                if (devices.Count > deviceId && deviceId > 0)
                {
                    selectedAdapter = devices[deviceId];
                    Debug.Print($"[StartTracking] Selected adapter: {selectedAdapter.Name} - {selectedAdapter.Description}");
                }
                else
                {
                    if (deviceId == 0)
                    {
                        Debug.Print("[StartTracking] ERROR: Please select a network adapter in settings (deviceId=0 means no adapter selected)");
                        MessageBox.Show("Пожалуйста, выберите сетевой адаптер в настройках.\n\nОткройте Settings → Network Settings и выберите ваш основной сетевой адаптер.", 
                                        "Сетевой адаптер не выбран", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                    else
                    {
                        Debug.Print($"[StartTracking] ERROR: Invalid deviceId {deviceId} for {devices.Count} devices");
                    }
                    return;
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
                    
                    // Обновляем UI (но не вызываем TextChanged event)
                    if (App.settingsForm.local_ip_textbox.Text != autoDetectedIP)
                    {
                        App.settingsForm.Invoke((Action)(() =>
                        {
                            App.settingsForm.local_ip_textbox.Text = autoDetectedIP;
                        }));
                    }
                }
                else
                {
                    // Fallback: используем текущее значение из настроек
                    App.meterState.LocalIP = App.settingsForm.local_ip_textbox.Text;
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
                App.meterState.LocalIP = App.settingsForm.local_ip_textbox.Text;
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
                    if (PcapThread == null)
                    {
                        PcapThread = new Thread(InitPcapWorker);
                        
                        // Phase 3: Устанавливаем высокий приоритет для одиночного PCAP потока
                        SetHighPriorityThread(PcapThread, "PCAP-Single");
                        
                        PcapThread.Start();
                        PcapThread.Join();
                        Debug.Print("Starting thread " + PcapThread.ManagedThreadId.ToString());
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show("PCAP Thread init error");
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
                    Debug.Print("[PcapWorkerCompleted] Too many restarts, stopping tracking");
                    StopTracking();
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
            try
            {
                Debug.Print($"[SwitchAdapterIfNeeded] Checking if adapter switch needed for IP: {newLocalIP}");
                
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
                    foreach (var worker in _pcapWorkers)
                    {
                        if (worker != null && worker.IsBusy)
                        {
                            worker.CancelAsync();
                        }
                    }
                    
                    // Ждем немного для корректной остановки
                    System.Threading.Thread.Sleep(100);
                    
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
                        System.Threading.Thread.Sleep(100);
                    }
                    
                    // Запускаем новый worker
                    Debug.Print($"[SwitchAdapterIfNeeded] Starting new single worker for {selectedAdapter.Name}");
                    InitPcapWorker();
                    
                    Debug.Print($"[SwitchAdapterIfNeeded] ✓ Single-adapter mode restarted on {selectedAdapter.Name}");
                }
                
                Debug.Print($"[SwitchAdapterIfNeeded] ✅ Adapter switched successfully to {newLocalIP}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[SwitchAdapterIfNeeded] ❌ Error: {ex.Message}");
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

                // КРИТИЧЕСКИ ВАЖНО: Сначала отключаем все флаги чтобы предотвратить перезапуск
                ticksLoop.Enabled = false;
                if (App.meterState != null)
                {
                    App.meterState.IsTracking = false; // Устанавливаем СРАЗУ
                }
                
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
            ping_val.ForeColor = _inactiveMetricColor;
            ping_val.Text = "n/a ms";
            traffic_val.ForeColor = _inactiveMetricColor;
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
            try { ResetTickrateChart(); } catch(Exception) {  }
            
            
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
                    catch (Exception) { }
                }
            }

            if (App.settingsForm.settings_data_send.Checked && App.meterState.TicksHistory.Count > 900 && App.meterState.Server.Ip != "")
            {
               // WebStatsManager.uploadTickrate(); //no no no. not today
            }

            try { RivaTuner.PrintData(""); } catch (Exception exc) { MessageBox.Show(exc.Message); }
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
                catch (Exception) { }
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
                try
                {
                    StopTracking();
                }
                catch (Exception ex)
                {
                    Debug.Print($"[GUI_FormClosing] Error stopping tracking: {ex.Message}");
                }
            }
        }

        private void icon_menu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            StopTracking();
            
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
            RivaTuner.KillRtss();
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
            if (_uiProcessingActive || !InvokeRequired) return;
            
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
                    BeginInvoke(new Action(() => {
                        foreach (var update in updates)
                        {
                            try { update?.Invoke(); }
                            catch (Exception ex) { Debug.Print($"[UI] Update error: {ex.Message}"); }
                        }
                    }));
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

        private void chart1_Click(object sender, EventArgs e)
        {

        }
    }
}
