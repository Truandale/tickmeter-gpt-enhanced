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

namespace tickMeter.Forms
{
    public partial class GUI : Form
    {
        // COM initialization constants and imports
        private const uint COINIT_APARTMENTTHREADED = 0x2;
        private const uint COINIT_DISABLE_OLE1DDE = 0x4;
        private const int RPC_E_CHANGED_MODE = unchecked((int)0x80010106);
        
        [DllImport("ole32.dll")]
        private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);
        
        [DllImport("ole32.dll")]
        private static extern void CoUninitialize();
        
        /// <summary>
        /// Прокачивает сообщения Windows для предотвращения блокировки STA потоков
        /// </summary>
        private static void PumpMessages()
        {
            Application.DoEvents();
        }
        
        /// <summary>
        /// Безопасная инициализация COM - игнорирует ошибку если COM уже инициализирован
        /// </summary>
        private static void SafeCoInitialize()
        {
            int hr = CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
            if (hr != 0 && hr != RPC_E_CHANGED_MODE)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }
        
        public PacketDevice selectedAdapter;
        public Thread PcapThread;

        public BackgroundWorker pcapWorker;
        
        // NEW: поля для мульти-адаптерного захвата
        private readonly List<PacketDevice> _allSelectedAdapters = new List<PacketDevice>();
        private readonly List<BackgroundWorker> _pcapWorkers = new List<BackgroundWorker>();
        // простая защита от дублей на бриджах/VPN
        private readonly Dictionary<ulong, long> _dedup = new Dictionary<ulong, long>(capacity: 8192);
        private readonly Stopwatch _dedupSw = Stopwatch.StartNew();
        private readonly object _dedupLock = new object();
        
        public Boolean allowClose = false;
        int restarts = 0;
        int restartLimit = 1;
        int lastSelectedAdapterID = -1;
        public string threadID = ""; 
        Bitmap chartBckg;
        int chartLeftPadding = 25;
        int chartXStep = 4;
        int appInitHeigh;
        int appInitWidth;
        bool OnScreen;
        public PubgStatsManager PubgMngr;
        public DbdStatsManager DbdMngr;
        public string targetKey = "";

        // EMA сглаживание для overlay
        private readonly Ema emaTickrate = new Ema();
        private readonly Ema emaPing = new Ema();
        
        // EMA фильтры для графиков
        private readonly Ema emaChartTickrate = new Ema();
        private readonly Ema emaChartPing = new Ema();

        // Baseline для детекции спайков
        private double _pingBaselineMs = 0;
        private double _tickrateBaseline = 0;

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
            
            // Устанавливаем интервал обновления overlay из настроек ping (как было изначально)
            var pingIntervalStr = App.settingsManager?.GetOption("ping_interval");
            if (!string.IsNullOrEmpty(pingIntervalStr) && int.TryParse(pingIntervalStr, out int pingVal))
            {
                ticksLoop.Interval = pingVal;
            }
            else
            {
                ticksLoop.Interval = 1000; // по умолчанию 1 секунда
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
            
            // Инициализируем EMA фильтры для графиков
            App.emaChartTickrate = new Ema();
            App.emaChartPing = new Ema();
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
            packetStatsBtn.Visible = true;
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
                packetStatsBtn.Visible = false;
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
                if (_dedup.Count > 20000)
                {
                    // лёгкая чистка
                    foreach (var key in _dedup.Where(kv => now - kv.Value > 250).Select(kv => kv.Key).ToList())
                        _dedup.Remove(key);
                }
            }
            return false;
        }

        private void PacketHandler(Packet packet)
        {
            if (!App.meterState.IsTracking) return;
            if (IsDuplicate(packet)) return; // NEW: проверка дублей
            GameProfileManager.CallBuitInProfiles(packet);
            GameProfileManager.CallCustomProfiles(packet);
            ActiveWindowTracker.AnalyzePacket(packet);

            // --- Добавлено: обработка входящих UDP-пакетов для расчёта UDP ping ---
            try
            {
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
            catch { /* ignore errors in UDP ping logic */ }
            // --- Конец добавления ---
        }

        bool RTSS_Failed = false;
        
        private async void TicksLoop_Tick(object sender, EventArgs e)
        {
            AutoDetectMngr.GetActiveProcessName(true);
            if(!App.meterState.isBuiltInProfileActive && !App.meterState.isCustomProfileActive)
            {
                updateMetherStateFromActiveWindow();
            }
            
            // Детекция спайков и сглаживание
            double dt = Math.Max(0.001, ticksLoop.Interval / 1000.0);
            bool smooth = App.settingsManager.GetOption("tickrate_smoothing", "True", "SETTINGS") == "True";
            
            // Сырые значения (используются в логике)
            double rawTickrate = App.meterState.OutputTickRate;
            double rawPingMs = GetEffectivePing();
            
            // Детекция спайков
            bool pingSpike = false;
            bool tickrateSpike = false;
            
            if (rawPingMs > 0)
            {
                // Ping spike detection
                double pingBaseline = (_pingBaselineMs <= 0) ? rawPingMs : _pingBaselineMs;
                pingSpike = Math.Abs(rawPingMs - pingBaseline) >= 25.0 ||
                           (pingBaseline > 0 && Math.Abs(rawPingMs - pingBaseline) / pingBaseline >= 0.40);

                // Лёгкая EMA-база для следующего шага
                double pingAlpha = 1 - Math.Exp(-dt / 1.0); // τ≈1 c
                double forPingEma = pingSpike ? pingBaseline : rawPingMs;
                _pingBaselineMs = (_pingBaselineMs <= 0) ? forPingEma : pingAlpha * forPingEma + (1 - pingAlpha) * _pingBaselineMs;
            }
            
            if (rawTickrate > 0)
            {
                // Tickrate spike detection (падение)
                double tickrateBaseline = (_tickrateBaseline <= 0) ? rawTickrate : _tickrateBaseline;
                tickrateSpike = (tickrateBaseline > 0) && (rawTickrate < tickrateBaseline * 0.65); // падение более чем на 35%

                // EMA для tickrate baseline
                double tickrateAlpha = 1 - Math.Exp(-dt / 0.8); // τ≈0.8 c
                double forTickrateEma = tickrateSpike ? tickrateBaseline : rawTickrate;
                _tickrateBaseline = (_tickrateBaseline <= 0) ? forTickrateEma : tickrateAlpha * forTickrateEma + (1 - tickrateAlpha) * _tickrateBaseline;
            }
            
            // Значения для отображения (используем baseline как сглаженное значение)
            double dispTickrate = smooth ? _tickrateBaseline : rawTickrate;
            double dispPing = (smooth && rawPingMs > 0) ? _pingBaselineMs : rawPingMs;
            
            // Читаем флаги спайк-маркеров
            bool ovPingSpike = App.settingsManager.GetOption("overlay_ping_spike_marker", "True", "SETTINGS") == "True";
            bool ovTickrSpike = App.settingsManager.GetOption("overlay_tickrate_spike_marker", "False", "SETTINGS") == "True";
            bool uiPingSpike = App.settingsManager.GetOption("ui_ping_spike_marker", "True", "SETTINGS") == "True";
            bool uiTickrSpike = App.settingsManager.GetOption("ui_tickrate_spike_marker", "False", "SETTINGS") == "True";
            
            if (App.settingsForm.settings_rtss_output.Checked)
            {
                await Task.Run(() => {
                    try { 
                        // NEW: Передаём сглаженные «display» значения в RivaTuner.
                        // Если EMA выключена — disp* = raw*, всё равно корректно.
                        RivaTuner.DisplayPingMs = dispPing;
                        RivaTuner.DisplayTickrate = dispTickrate;
                        
                        Debug.WriteLine($"[GUI] Передача в RivaTuner: dispPing={dispPing:F1}, dispTickrate={dispTickrate:F1}");
                        
                        // Устанавливаем статические переменные для спайк-индикаторов в RivaTuner
                        RivaTuner.PingSpike = ovPingSpike && pingSpike;
                        RivaTuner.TickrateSpike = ovTickrSpike && tickrateSpike;
                        RivaTuner.TicktimeSpike = false; // пока не используется
                        
                        RivaTuner.BuildRivaOutput(); 
                    } catch (Exception ex) {
                        if(!RTSS_Failed)
                        {
                            DebugLogger.log(ex);
                            RTSS_Failed = true;
                        }
                    }
                });
            }

            //form overlay isn't visible, quit
            if (!OnScreen) return;

            //update tickrate
            Color TickRateColor = App.settingsForm.ColorGood.ForeColor;
            if (rawTickrate < 30)
            {
                TickRateColor = App.settingsForm.ColorBad.ForeColor;
            }
            else if (rawTickrate < 50)
            {
                TickRateColor = App.settingsForm.ColorMid.ForeColor;
            }
            
            await Task.Run(
                    () => {
                        tickrate_val.Invoke(new Action(() => {
                            // Добавляем спайк-маркер для UI если включен
                            string tickrateText = Math.Round(dispTickrate, 1).ToString();
                            if (uiTickrSpike && tickrateSpike)
                                tickrateText += " (!)";
                            
                            tickrate_val.Text = tickrateText;
                            tickrate_val.ForeColor = TickRateColor;
                        }));
                        //update tickrate chart
                        if (App.settingsForm.settings_chart_checkbox.Checked)
                        {
                            // Проверяем настройку сглаживания графиков
                            bool smoothCharts = App.settingsManager.GetOption("smooth_charts", "False", "SETTINGS") == "True";
                            List<int> chartData;
                            
                            if (smoothCharts)
                            {
                                // Добавляем сглаженное значение в буфер
                                double smoothedTickrate = emaChartTickrate.Update(rawTickrate, Alpha(0.8, dt));
                                App.meterState.TicksHistorySmoothed.Add((int)Math.Round(smoothedTickrate));
                                
                                // Ограничиваем размер буфера
                                if (App.meterState.TicksHistorySmoothed.Count > 511)
                                {
                                    App.meterState.TicksHistorySmoothed.RemoveAt(0);
                                }
                                
                                chartData = App.meterState.TicksHistorySmoothed;
                            }
                            else
                            {
                                chartData = App.meterState.TicksHistory;
                            }
                            
                            graph.Invoke(new Action(() => graph.Image = UpdateGraph(chartData)));
                        }
                        //update traffic
                        if (App.settingsForm.settings_traffic_checkbox.Checked)
                        {
                            float formatedUpload = (float)App.meterState.UploadTraffic / (1024 * 1024);
                            float formatedDownload = (float)App.meterState.DownloadTraffic / (1024 * 1024);
                            traffic_val.Invoke(new Action(() => traffic_val.Text = formatedUpload.ToString("N2") + " / " + formatedDownload.ToString("N2") + " mb"));
                        }
                        //update IP
                        if (App.settingsForm.settings_ip_checkbox.Checked)
                        {
                        ip_val.Invoke(new Action(() => ip_val.Text = App.meterState.Server.Ip));
                        }
                        //update PING
                        if (App.settingsForm.settings_ping_checkbox.Checked)
                        {
                        // Получаем цвет для пинга на основе сырого значения
                        Color pingColor = App.settingsForm.ColorMid.ForeColor; // По умолчанию
                        if (rawPingMs > 0)
                        {
                            if (rawPingMs < 100)
                            {
                                pingColor = App.settingsForm.ColorGood.ForeColor;
                            }
                            else if (rawPingMs < 150)
                            {
                                pingColor = App.settingsForm.ColorMid.ForeColor;
                            }
                            else
                            {
                                pingColor = App.settingsForm.ColorBad.ForeColor;
                            }
                        }
                        
                        countryLbl.Invoke(new Action(() => countryLbl.Text = App.meterState.Server.Location));
                        ping_val.Invoke(new Action(() =>
                        {
                            string pingText;
                            
                            if (dispPing > 0)
                            {
                                // Добавляем спайк-маркер для UI если включен
                                string spikeIndicator = (uiPingSpike && pingSpike) ? " (!)" : "";
                                pingText = $"{Math.Round(dispPing, 0)}{spikeIndicator} ms";
                            }
                            else
                            {
                                pingText = "n/a ms";
                            }
                            
                            ping_val.Text = pingText;
                            ping_val.ForeColor = pingColor;
                        }));
                        }
                        //update time
                        if (App.settingsForm.settings_session_time_checkbox.Checked && App.meterState.Server.Ip != "")
                        {
                            TimeSpan result = DateTime.Now.Subtract(App.meterState.SessionStart);
                            string Duration = result.ToString("mm':'ss");
                            ip_val.Invoke(new Action(() => time_val.Text = Duration));
                        }
                        //update drops
                        if (App.settingsForm.packet_drops_checkbox.Checked && App.meterState.Server.Ip != "")
                        {
                            ip_val.Invoke(new Action(() => drops_lbl_val.Text = App.meterState.GetDrops()+"%"));
                        }
                    });
            if (!App.meterState.IsTracking)
            {
                StopTracking();
            }
        }

        private bool isValidToTrack(string key)
        {
            if(key != "" && ActiveWindowTracker.connections.Keys.Contains(key))
            {
                ProcessNetworkStats connection = ActiveWindowTracker.connections[key];
                return
                    AutoDetectMngr.GetActiveProcessName() == connection.name
                    && ActiveWindowTracker.connections[key].TrackingDelta() > 3
                    && ActiveWindowTracker.connections[key].LastUpdateDelta() < 2
                    && ActiveWindowTracker.connections[key].remoteIp != App.meterState.LocalIP
                    && ActiveWindowTracker.connections[key].ticksIn > 3
                    && ActiveWindowTracker.connections[key].downloaded > 0;
            }
            return false;
        }

        private void updateMetherStateFromActiveWindow()
        {
            int maxTicks = 0;

            if(!isValidToTrack(targetKey))
            {
                string[] connectionNames = ActiveWindowTracker.connections.Keys.ToArray();
                foreach (string connection in connectionNames)
                {
                    if(!ActiveWindowTracker.connections.ContainsKey(connection)) { continue; }
                    if (
                        ActiveWindowTracker.connections[connection].ticksIn > maxTicks
                        && isValidToTrack(connection)
                        )
                    {
                        maxTicks = ActiveWindowTracker.connections[connection].ticksIn;
                        targetKey = connection;
                    }
                }
            }
            
            
            if(targetKey != "") { 
                if(!ActiveWindowTracker.connections.ContainsKey(targetKey))
                {
                    targetKey = "";
                    return;
                }
                ProcessNetworkStats procStats = ActiveWindowTracker.connections[targetKey];
                App.meterState.tickTimeBuffer = procStats.tickTimeBuffer;
                App.meterState.CurrentTimestamp = DateTime.Now;
                App.meterState.Game = procStats.name;
                App.meterState.Server.Ip = procStats.remoteIp.ToString();
                App.meterState.DownloadTraffic = procStats.downloaded;
                App.meterState.TickRate = procStats.getTicksIn();
                App.meterState.Server.PingPort = (int)procStats.remotePort;
                App.meterState.SessionStart = procStats.startTrack;
                App.meterState.IsTracking = true;
                App.meterState.loss = procStats.loss;
                App.meterState.totalTicksCnt = procStats.totalTicksCnt;
            }
        }

        public Bitmap UpdateGraph(List<int> ticks)
        {
            chartBckg = new Bitmap(graph.InitialImage);
            if (ticks.Count < 2) return chartBckg;
            Graphics g = Graphics.FromImage(chartBckg);
            int w = graph.Image.Width;
            int h = graph.Image.Height;
            float scale =  (float)h / 61; //2.8
            int GraphMaxTicks = (w - chartLeftPadding) / chartXStep;
            Pen pen = new Pen(Color.Red, 1);
            int stepX = 0;
            for (int i = ticks.Count-2; i >= 0 && ticks.Count - i - 1 < GraphMaxTicks; i--)
            {
                stepX++;
                g.DrawLine(pen, new Point(chartLeftPadding + (stepX - 1) * chartXStep, h - (int)((float)ticks[i + 1]*scale)), new Point(chartLeftPadding + stepX * chartXStep, h - (int)((float)ticks[i]*scale)));
            }
            return chartBckg;
        }

       
        public void StartTracking()
        {
            Debug.Print("StartTracking");
            
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
            
            var captureAll = App.settingsManager.GetOption("capture_all_adapters", "False", "SETTINGS") == "True";
            var devices = App.GetAdapters();
            _allSelectedAdapters.Clear();

            if (captureAll)
            {
                // собрать все «реальные» адаптеры (пропускаем 0-й элемент дропдауна и виртуальные/loopback)
                IEnumerable<LivePacketDevice> src = devices;
                if (src.Count() == App.settingsForm.adapters_list.Items.Count && App.settingsForm.adapters_list.Items.Count > 0)
                {
                    // список в UI обычно имеет заглушку на позиции 0
                    src = src.Skip(1);
                }
                
                // Проверяем настройку "ignore virtual adapters"
                bool ignoreVirtual = App.settingsManager.GetOption("ignore_virtual_adapters", "True", "SETTINGS") == "True";
                
                foreach (var d in src)
                {
                    var desc = (d.Description ?? "").ToLowerInvariant();
                    
                    // Фильтруем виртуальные адаптеры только если настройка включена
                    if (ignoreVirtual && (desc.Contains("loopback") || desc.Contains("npcap loopback") ||
                        desc.Contains("hyper-v") || desc.Contains("vmware") ||
                        desc.Contains("virtualbox") || desc.Contains("vethernet")))
                        continue;
                        
                    _allSelectedAdapters.Add(d);
                }
                if (_allSelectedAdapters.Count == 0)
                {
                    MessageBox.Show("Не найдено подходящих сетевых адаптеров");
                    return;
                }
            }
            else
            {
                int deviceId = App.settingsForm.adapters_list.SelectedIndex;
                if (devices.Count > deviceId && deviceId > 0)
                {
                    selectedAdapter = devices[deviceId];
                }
                else
                {
                    return;
                }
            }
            
            App.meterState.LocalIP = App.settingsForm.local_ip_textbox.Text;
            lastSelectedAdapterID = App.settingsForm.adapters_list.SelectedIndex;
            try
            {
                if (captureAll)
                {
                    // запустить по воркеру на каждый адаптер
                    foreach (var dev in _allSelectedAdapters)
                    {
                        var worker = new BackgroundWorker();
                        worker.DoWork += (s, e) =>
                        {
                            if (!App.meterState.IsTracking) return;
                            
                            // Безопасно инициализируем COM для текущего потока
                            SafeCoInitialize();
                            
                            try
                            {
                                using (var comm = dev.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 150))
                                {
                                    if (comm.DataLink.Kind != DataLinkKind.Ethernet) return;
                                    // Cooperative packet receiving with tracking flag check
                                    while (App.meterState.IsTracking)
                                    {
                                        try
                                        {
                                            var result = comm.ReceivePackets(100, PacketHandler);
                                            if (result == PacketCommunicatorReceiveResult.Timeout)
                                            {
                                                // Прокачиваем сообщения Windows каждый таймаут
                                                PumpMessages();
                                                continue;
                                            }
                                            if (result == PacketCommunicatorReceiveResult.BreakLoop)
                                                break;
                                        }
                                        catch { break; }
                                    }
                                }
                            }
                            finally
                            {
                                // Не вызываем CoUninitialize для BackgroundWorker потоков,
                                // так как они могут использовать уже существующий COM контекст
                            }
                        };
                        worker.RunWorkerCompleted += PcapWorkerCompleted;
                        _pcapWorkers.Add(worker);
                        worker.RunWorkerAsync();
                    }
                }
                else
                {
                    if (PcapThread == null)
                    {
                        PcapThread = new Thread(InitPcapWorker);
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
        }
        private void PcapWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!App.meterState.IsTracking) return;
            
            // В мульти-режиме перезапускаем конкретный воркер
            var captureAll = App.settingsManager.GetOption("capture_all_adapters", "False", "SETTINGS") == "True";
            if (captureAll) 
            {
                // Перезапускаем завершившийся воркер в мультирежиме
                var worker = sender as BackgroundWorker;
                if (worker != null && !worker.CancellationPending)
                {
                    try
                    {
                        worker.RunWorkerAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MultiAdapter GUI] Error restarting worker: {ex.Message}");
                    }
                }
                return;
            }
            
            if (App.meterState.TickRate == 0)
            {
                restarts++;
                if (restarts > restartLimit)
                {
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
                if (pcapWorker != null)
                {
                    pcapWorker.RunWorkerAsync();
                }
            }
            catch (Exception) { }

        }

        private void PcapWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            if (!App.meterState.IsTracking) return;
            
            // В мульти-режиме этот метод не должен вызываться
            var captureAll = App.settingsManager.GetOption("capture_all_adapters", "False", "SETTINGS") == "True";
            if (captureAll) return;
            
            if (selectedAdapter == null)
            {
                MessageBox.Show("Selected adapter is not set!");
                return;
            }
            
            // Безопасно инициализируем COM для текущего потока
            SafeCoInitialize();
            
            try
            {
                using (PacketCommunicator communicator = selectedAdapter.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 150))
                {
                    if (communicator.DataLink.Kind != DataLinkKind.Ethernet)
                    {
                        MessageBox.Show("This program works only on Ethernet networks!");
                        return;
                    }

                    // Cooperative packet receiving with tracking flag check
                    while (App.meterState.IsTracking)
                    {
                        try
                        {
                            var result = communicator.ReceivePackets(100, PacketHandler);
                            if (result == PacketCommunicatorReceiveResult.Timeout)
                            {
                                // Прокачиваем сообщения Windows каждый таймаут
                                PumpMessages();
                                continue;
                            }
                            if (result == PacketCommunicatorReceiveResult.BreakLoop)
                                break;
                        }
                        catch { break; }
                    }
                }
            }
            finally
            {
                // Не вызываем CoUninitialize для BackgroundWorker потоков
            }
        }
        public void InitPcapWorker()
        {
            pcapWorker = new BackgroundWorker();
            pcapWorker.DoWork += PcapWorkerDoWork;
            pcapWorker.RunWorkerCompleted += PcapWorkerCompleted;
            pcapWorker.RunWorkerAsync();
        }

        public void StopTracking()
        {

            ticksLoop.Enabled = false;
            if (App.meterState == null) return;
            
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
            
            // NEW: остановка мульти-захвата: флаг IsTracking и очистка воркеров
            try
            {
                // Дайте время воркерам завершиться после изменения IsTracking
                System.Threading.Thread.Sleep(200);
                
                foreach (var w in _pcapWorkers)
                {
                    if (w.IsBusy)
                        w.CancelAsync();
                }
                
                // Ждём завершения воркеров (кратко)
                for (int i = 0; i < 20; i++)
                {
                    bool anyBusy = false;
                    foreach (var w in _pcapWorkers)
                    {
                        if (w.IsBusy) { anyBusy = true; break; }
                    }
                    if (!anyBusy) break;
                    
                    // Прокачиваем сообщения Windows во время ожидания
                    PumpMessages();
                    System.Threading.Thread.Sleep(50);
                }
            } catch { }
            _pcapWorkers.Clear();
            _allSelectedAdapters.Clear();
            
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
        }

        private void GUI_FormClosed(object sender, FormClosedEventArgs e)
        {
            // Принудительная остановка всех операций
            try 
            {
                if (App.meterState != null)
                    App.meterState.IsTracking = false;
                    
                // Очистка всех worker'ов
                foreach (var worker in _pcapWorkers)
                {
                    try 
                    {
                        if (worker.IsBusy)
                            worker.CancelAsync();
                    }
                    catch { }
                }
                
                // Ждем недолго и принудительно очищаем
                for (int i = 0; i < 10; i++)
                {
                    PumpMessages();
                    System.Threading.Thread.Sleep(50);
                }
                
                _pcapWorkers.Clear();
                _allSelectedAdapters.Clear();
            }
            catch { }
            
            // Принудительная очистка COM объектов и сборка мусора
            try 
            {
                // Множественная сборка мусора для гарантированного освобождения COM объектов
                for (int i = 0; i < 3; i++)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
                GC.Collect();
                
                // Освобождение всех COM объектов
                Marshal.CleanupUnusedObjectsInCurrentContext();
            }
            catch { }
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
            // Обновляем интервал overlay согласно настройкам ping interval
            var control = sender as NumericUpDown;
            if (control != null)
            {
                ticksLoop.Interval = (int)control.Value;
            }
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
            // ETW removed
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
        }

        private void icon_menu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            StopTracking();
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
                
                // pingBuffer будет обновляться через CurrentTimestamp как раньше
                // Не добавляем данные сюда, чтобы избежать слишком частых обновлений графика
            }
        }
        
        // EMA сглаживание - вычисление коэффициента альфа
        private static double Alpha(double tauSec, double dtSec) => 1 - Math.Exp(-dtSec / tauSec);
        
        // Получает эффективное значение пинга из доступных источников
        private double GetEffectivePing()
        {
            var server = App.meterState.Server;
            
            // UDP > TCP > ICMP приоритет
            if (App.meterState.TcpPing >= 1000 && App.meterState.IsUdpPingValid)
            {
                return server.UdpPing;
            }
            else if (server.Ping > 0 && server.Ping < 10000)
            {
                return server.Ping;
            }
            else if (App.meterState.IcmpPing > 0 && App.meterState.IcmpPing < 1000)
            {
                return App.meterState.IcmpPing;
            }
            
            return -1; // нет валидного пинга
        }
        
        // Сглаживание пинга с защитой от спайков
        private (double val, bool spike) SmoothPing(double raw, double dtSec)
        {
            // Получаем параметры из настроек
            double tau = double.Parse(App.settingsManager.GetOption("smoothing.ping.tau", "1.0", "SETTINGS"), CultureInfo.InvariantCulture);
            double spikeAbsMs = double.Parse(App.settingsManager.GetOption("smoothing.ping.spike_abs_ms", "25", "SETTINGS"), CultureInfo.InvariantCulture);
            double spikeRel = double.Parse(App.settingsManager.GetOption("smoothing.ping.spike_rel", "0.40", "SETTINGS"), CultureInfo.InvariantCulture);
            
            double a = Alpha(tau, dtSec);
            double baseLine = emaPing.ValueOr(raw);
            bool spike = Math.Abs(raw - baseLine) >= spikeAbsMs || 
                        (baseLine > 0 && Math.Abs(raw - baseLine) / baseLine >= spikeRel);
            double forEma = spike ? baseLine : raw;
            return (emaPing.Update(forEma, a), spike);
        }
    }

    /// <summary>
    /// Класс для экспоненциального сглаживания (EMA)
    /// </summary>
    public sealed class Ema
    {
        private double? v;
        
        /// <summary>
        /// Обновляет фильтр новым значением
        /// </summary>
        /// <param name="x">Новое значение</param>
        /// <param name="a">Коэффициент альфа (0-1)</param>
        /// <returns>Сглаженное значение</returns>
        public double Update(double x, double a) 
        {
            v = v is null ? x : a * x + (1 - a) * v.Value;
            return v.Value;
        }
        
        /// <summary>
        /// Возвращает текущее значение или fallback если не инициализирован
        /// </summary>
        /// <param name="x">Значение по умолчанию</param>
        /// <returns>Текущее или дефолтное значение</returns>
        public double ValueOr(double x) => v ?? x;
        
        /// <summary>
        /// Сбрасывает фильтр
        /// </summary>
        public void Reset() => v = null;
    }
}
