using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
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
        
        public PacketDevice selectedAdapter;
        public Thread PcapThread;

        public BackgroundWorker pcapWorker;
        
        // NEW: поля для мульти-адаптерного захвата
        private readonly List<PacketDevice> _allSelectedAdapters = new List<PacketDevice>();
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
        
        // Анти-реэнтерабельность для StartTracking/StopTracking (предотвращение роста воркеров)
        private int _startTrackingBusy = 0;
        private int _stopTrackingBusy = 0;
        
        public Boolean allowClose = false;
        int restarts = 0;
        int restartLimit = 1;
        int lastSelectedAdapterID = -1;
        public string threadID = ""; 
        int chartLeftPadding = 25;
        int chartXStep = 4;
        int appInitHeigh;
        int appInitWidth;
        bool OnScreen;
        public PubgStatsManager PubgMngr;
        public DbdStatsManager DbdMngr;
        public string targetKey = "";
        private int _gcCounter = 0; // Счётчик для периодической сборки мусора
        
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
        
        // Убираем chartBckg как поле класса - теперь создаётся локально в UpdateGraph()

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
            ip_val.Visible = 
            ip_lbl.Visible = 
            ping_val.Visible = 
            ping_lbl.Visible = 
            countryLbl.Visible = 
            traffic_lbl.Visible = 
            traffic_val.Visible = 
            time_lbl.Visible = 
            time_val.Visible = 
            SettingsButton.Visible =
            gameProfilesButton.Visible =
            drops_lbl.Visible = 
            drops_lbl_val.Visible = 
            packetStatsBtn.Visible = 
            spikeAnalyticsBtn.Visible = true;
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
                if (!App.settingsForm.settings_chart_checkbox.Checked)
                {
                    Height = 160;
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
                if (App.settingsForm.settings_rtss_output.Checked && _rtssSw.ElapsedMilliseconds >= RtssPeriodMs)
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
            bool skipGUIUpdate = !OnScreen;

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
            
            // ChatGPT Enhancement: Snapshot-based diagnostic for perfect consistency
            System.Diagnostics.Debug.Print($"[ZONER GUI] {zoner.GetDiagnostic(snap)}");
            
            await Task.Run(
                    () => {
                        // Always update PING (including spike indicators) for both GUI and RTSS overlay
                        if (App.settingsForm.settings_ping_checkbox.Checked)
                        {
                            // Phase 3: Single Consumer Pattern - queue UI updates instead of direct Invoke
                            QueueUIUpdate(() => countryLbl.Text = App.meterState.Server.Location);
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
                                Color finalPingColor = PingColor;
                                
                                // Добавляем индикатор спайка если включена соответствующая настройка
                                // ВАЖНО: индикатор наследует цвет зоны, а не перезаписывает его
                                bool showSpikeIndicator = App.settingsManager?.GetOption("show_ping_spikes", "True", "ADVANCED") == "True";
                                Debug.Print($"[GUI] Spike check: HasPingSpike={server.HasPingSpike}, ShowSetting={showSpikeIndicator}, OnScreen={OnScreen}");
                                if (showSpikeIndicator && server.HasPingSpike)
                                {
                                    pingText += " (!)";
                                    // Сохраняем цвет зоны - индикатор спайка того же цвета что и значение
                                    Debug.Print($"[GUI] Spike indicator added with zone color: {pingText}");
                                }
                                
                                // Применяем финальный цвет (цвет зоны сохраняется)
                                ping_val.ForeColor = finalPingColor;
                                
                                ping_val.Text = pingText;
                            });
                        }
                        
                        // Only update other GUI elements if GUI overlay is visible
                        if (!skipGUIUpdate)
                        {
                            // Phase 3: Single Consumer Pattern - queue UI updates
                            QueueUIUpdate(() => {
                                string tickrateText = App.meterState.OutputTickRate.ToString();
                                Color finalTickRateColor = TickRateColor;
                                
                                // Добавляем индикатор спайка для tickrate если включена соответствующая настройка
                                bool showTickrateSpikes = App.settingsManager?.GetOption("show_tickrate_spikes", "True", "ADVANCED") == "True";
                                if (showTickrateSpikes && App.meterState.HasTickRateSpike)
                                {
                                    tickrateText += " (!)";
                                    // Мигающий эффект для tickrate спайка
                                    finalTickRateColor = _spikeBlinkState ? Color.Red : Color.Orange;
                                    Debug.Print($"[GUI] Tickrate spike indicator added to display: {tickrateText}");
                                }
                                
                                tickrate_val.Text = tickrateText;
                                tickrate_val.ForeColor = finalTickRateColor;
                            });
                            
                            //update tickrate chart
                            if (App.settingsForm.settings_chart_checkbox.Checked)
                            {
                                QueueUIUpdate(() => graph.Image = UpdateGraph(App.meterState.TicksHistory));
                            }
                            
                            //update traffic
                            if (App.settingsForm.settings_traffic_checkbox.Checked)
                            {
                                float formatedUpload = (float)App.meterState.UploadTraffic / (1024 * 1024);
                                float formatedDownload = (float)App.meterState.DownloadTraffic / (1024 * 1024);
                                QueueUIUpdate(() => traffic_val.Text = formatedUpload.ToString("N2") + " / " + formatedDownload.ToString("N2") + " mb");
                            }
                            
                            //update IP
                            if (App.settingsForm.settings_ip_checkbox.Checked)
                            {
                                QueueUIUpdate(() => ip_val.Text = App.meterState.Server.Ip);
                            }
                            
                            //update time
                            if (App.settingsForm.settings_session_time_checkbox.Checked && App.meterState.Server.Ip != "")
                            {
                                TimeSpan result = DateTime.Now.Subtract(App.meterState.SessionStart);
                                string Duration = result.ToString("mm':'ss");
                                QueueUIUpdate(() => time_val.Text = Duration);
                            }
                            
                            //update drops
                            if (App.settingsForm.packet_drops_checkbox.Checked && App.meterState.Server.Ip != "")
                            {
                                QueueUIUpdate(() => drops_lbl_val.Text = App.meterState.GetDrops()+"%");
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
                                Classes.SpikeDetection.SpikeDetectionManager.AddValue(
                                    Classes.SpikeDetection.MetricKind.Ticktime, 
                                    lastTickTime
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.Print($"[updateMetherStateFromActiveWindow] Error adding ticktime to spike detector: {ex.Message}");
                        }
                        App.meterState.CurrentTimestamp = DateTime.Now;
                        App.meterState.Game = procStats.name;
                        App.meterState.Server.Ip = procStats.remoteIp.ToString();
                        App.meterState.DownloadTraffic = procStats.downloaded;
                        
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

        public Bitmap UpdateGraph(List<int> ticks)
        {
            if (ticks.Count < 2) 
            {
                // Возвращаем копию начального изображения без создания лишних объектов
                return new Bitmap(graph.InitialImage);
            }

            // Используем using для автоматического освобождения ресурсов
            using (var chartBckg = new Bitmap(graph.InitialImage))
            using (var g = Graphics.FromImage(chartBckg))
            using (var pen = new Pen(Color.Red, 1))
            {
                int w = graph.Image.Width;
                int h = graph.Image.Height;
                float scale = (float)h / 61; //2.8
                int GraphMaxTicks = (w - chartLeftPadding) / chartXStep;
                int stepX = 0;
                
                for (int i = ticks.Count - 2; i >= 0 && ticks.Count - i - 1 < GraphMaxTicks; i--)
                {
                    stepX++;
                    g.DrawLine(pen, 
                        new Point(chartLeftPadding + (stepX - 1) * chartXStep, h - (int)((float)ticks[i + 1] * scale)), 
                        new Point(chartLeftPadding + stepX * chartXStep, h - (int)((float)ticks[i] * scale)));
                }
                
                // Возвращаем копию, чтобы исходный bitmap можно было корректно освободить
                return new Bitmap(chartBckg);
            }
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
            
            App.meterState.LocalIP = App.settingsForm.local_ip_textbox.Text;
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
            
            tickrate_val.ForeColor = App.settingsForm.ColorBad.ForeColor;
            ping_val.ForeColor = App.settingsForm.ColorMid.ForeColor;
            try { graph.Image = graph.InitialImage; } catch(Exception) {  }
            
            
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
            if(App.settingsForm.run_minimized.Checked)
            {
                Hide();
            }
            ETW.init();
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
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            Show();
            this.WindowState = FormWindowState.Normal;
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
                System.Diagnostics.Debug.Print($"[OnSpikeDetected] Spike detected: {spikeEvent.Metric} at {spikeEvent.Timestamp:HH:mm:ss.fff}");
                
                // Обновляем флаги спайков в зависимости от типа метрики
                switch (spikeEvent.Metric)
                {
                    case Classes.SpikeDetection.MetricKind.Ping:
                        if (App.meterState?.Server != null)
                        {
                            App.meterState.Server.SetPingSpike(true);
                        }
                        ShowSpikeNotification("Ping", spikeEvent.Value, "ms", ref _lastPingSpikeNotification);
                        break;
                        
                    case Classes.SpikeDetection.MetricKind.Tickrate:
                        if (App.meterState?.Server != null)
                        {
                            App.meterState.Server.SetTickRateSpike(true);
                        }
                        ShowSpikeNotification("Tickrate", spikeEvent.Value, "Hz", ref _lastTickrateSpikeNotification);
                        System.Diagnostics.Debug.Print($"[OnSpikeDetected] Tickrate spike detected: {spikeEvent.Value:F1}");
                        break;
                        
                    case Classes.SpikeDetection.MetricKind.Ticktime:
                        if (App.meterState?.Server != null)
                        {
                            App.meterState.Server.SetTickTimeSpike(true);
                        }
                        ShowSpikeNotification("Ticktime", spikeEvent.Value, "ms", ref _lastTicktimeSpikeNotification);
                        System.Diagnostics.Debug.Print($"[OnSpikeDetected] Ticktime spike detected: {spikeEvent.Value:F1}ms");
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
    }
}
