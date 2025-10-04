using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.IpV6;
using PcapDotNet.Packets.Transport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using tickMeter.Classes;
using System.Runtime.CompilerServices;
using System.Net;
using System.Net.Sockets;
using tickMeter.WinDivertLayer;
using TunnelAutoAttachLib = tickMeter.Classes.TunnelAutoAttach;
using VpnHeuristicsLib = tickMeter.Classes.VpnHeuristics;

namespace tickMeter
{
    /// <summary>
    /// Легкая структура для представления пакета в VirtualMode
    /// </summary>
    public struct PacketRow
    {
        public DateTime Timestamp;
        public int Id;
        public string SourceIP;
        public uint SourcePort;
        public string DestIP;
        public uint DestPort;
        public int Length;
        public string Protocol;
        public string ProcessName;
        public string ResolvedRemote;
        public string ResolvedBy;
        
        public PacketRow(DateTime ts, int id, string srcIp, uint srcPort, string dstIp, uint dstPort, 
                        int len, string proto, string proc, string resolvedRemote, string resolvedBy)
        {
            Timestamp = ts;
            Id = id;
            SourceIP = srcIp;
            SourcePort = srcPort;
            DestIP = dstIp;
            DestPort = dstPort;
            Length = len;
            Protocol = proto;
            ProcessName = proc;
            ResolvedRemote = resolvedRemote;
            ResolvedBy = resolvedBy;
        }
    }
    
    public partial class PacketStats : Form
    {
        private sealed class CapturedPacket
        {
            public CapturedPacket(Packet packet, LivePacketDevice device, bool isVirtual)
            {
                Packet = packet;
                Device = device;
                IsVirtual = isVirtual;
                TimestampUtc = DateTime.UtcNow;
            }

            public Packet Packet { get; }
            public LivePacketDevice Device { get; }
            public bool IsVirtual { get; }
            public DateTime TimestampUtc { get; }
        }

        List<CapturedPacket> PacketBuffer = new List<CapturedPacket>();
        private readonly object _packetBufferLock = new object();  // Thread synchronization lock
        private const int MAX_PACKET_BUFFER_SIZE = 100;  // Максимальный размер буфера для Live View (уменьшен с 1000)
        private const int CRITICAL_BUFFER_SIZE = 200;    // Критический размер для экстренной очистки
        private int _refreshCounter = 0;                  // Счётчик для периодической очистки памяти
        public int inPackets = 0;
        public int outPackets = 0;
        public int inTraffic = 0;
        public int outTraffic = 0;
    private bool _vpnLogInitialized;
    private int _trackerLogCount;
    private int _transportDecodeErrors;
    private int _etwRemapLogCount;
    private const int TransportDecodeErrorLogLimit = 5;

        public ConnectionsManager connMngr;

        public bool tracking;
        Thread PcapThread;
        public BackgroundWorker pcapWorker;
        public PacketFilter packetFilter;
    private WinDivertSniffer _winDivertSniffer;

        // Multi-adapter support - DEPRECATED: переходим на CaptureService
    private readonly List<BackgroundWorker> _pcapWorkers = new List<BackgroundWorker>();
    private bool CaptureAll => VpnSettings.ForceCaptureVirtual || GetBoolSetting("capture_all_adapters", "False");
    private bool _ignoreVirtual => VpnSettings.ForceCaptureVirtual ? false : GetBoolSetting("ignore_virtual_adapters", "True");

        // CaptureService integration - ОСНОВНАЯ СИСТЕМА
        private tickMeter.Classes.CaptureService.Subscription _captureSub;
        private bool _captureRunning;
        private long _lastStartMs;
        
        // Анти-реэнтерабельность для предотвращения роста воркеров
        private int _subBusy = 0;
        private readonly object _restartLock = new object();

        // VirtualMode ListView support
        private readonly object _ringLock = new object();
        private PacketRow[] _ring;
        private int _ringHead = 0, _ringCount = 0; // head — индекс самого старого
        private bool _useVirtual = false;
        private int _packetIdCounter = 0;
    private bool _enableIpv6 = true;
        private bool _virtualModeSwitchLogged;
            private readonly Dictionary<string, CaptureService.Subscription> _tunnelSubscriptions = new Dictionary<string, CaptureService.Subscription>(StringComparer.OrdinalIgnoreCase);
            private readonly object _tunnelSubscriptionsLock = new object();

        private static bool GetBoolSetting(string key, string defaultValue, string section = "SETTINGS")
        {
            var raw = App.settingsManager?.GetOption(key, defaultValue, section) ?? defaultValue;
            return string.Equals(raw, "True", StringComparison.OrdinalIgnoreCase);
        }

        public PacketStats()
        {
            InitializeComponent();
            EtwBroker.Start();
            packetFilter = new PacketFilter();
            ResetTransportDecodeErrors();
            
            // Инициализация VirtualMode на основе настроек
            _useVirtual = App.settingsManager?.GetOption("live_virtual_list", "False", "ADVANCED") == "True";
            int maxRows = Math.Max(1000, int.Parse(App.settingsManager?.GetOption("live_max_rows", "5000", "ADVANCED") ?? "5000"));
            
            if (_useVirtual)
            {
                _ring = new PacketRow[maxRows];
                listView1.VirtualMode = true;
                listView1.RetrieveVirtualItem += ListView1_RetrieveVirtualItem;
                listView1.VirtualListSize = 0;
                Debug.Print($"[PacketStats] VirtualMode enabled with buffer size: {maxRows}");
            }
            else
            {
                Debug.Print("[PacketStats] Classic ListView mode");
            }

            // Динамический VPN-режим: проверяем наличие туннельных адаптеров
            bool hasTunnelAdapter = false;
            try
            {
                var allDevices = LivePacketDevice.AllLocalMachine;
                var tunnelHints = new[] { "wintun", "wireguard", "tap", "tun", "openvpn", "tailscale", "zerotier" };
                hasTunnelAdapter = allDevices.Any(d => TunDetector.IsTunLike(d, tunnelHints));
            }
            catch { }

            bool vpnMode = VpnSettings.ForceCaptureVirtual || hasTunnelAdapter;
            var effectiveIgnoreVirtual = vpnMode ? false : _ignoreVirtual;
            
            DebugLogger.log($"[LiveView] Initialized (virtual={_useVirtual}, maxRows={maxRows}, captureAll={CaptureAll}, ignoreVirtual={effectiveIgnoreVirtual}, vpnMode={vpnMode}, tunnelDetected={hasTunnelAdapter})");
            _virtualModeSwitchLogged = _useVirtual;
            
            // TunnelAutoAttach.Init() уже подписывается на EtwBroker.OnLocalTunnelObserved внутри
            TryInitTunnelAutoAttach();
        }
        public void InitWorker()
        {
            pcapWorker = new BackgroundWorker();
            pcapWorker.DoWork += PcapWorkerDoWork;
            pcapWorker.RunWorkerCompleted += PcapWorkerCompleted;
            pcapWorker.RunWorkerAsync();
        }

        

        /// <summary>
        /// Безопасный запуск с CaptureService и анти-реэнтерабельностью
        /// </summary>
        public void Start()
        {
            SafeRestartCapture();
        }
        
        /// <summary>
        /// Безопасный перезапуск с debounce и анти-реэнтерабельностью
        /// </summary>
        private void SafeRestartCapture()
        {
            // Анти-реэнтерабельность: если уже идет restart - выходим
            if (Interlocked.Exchange(ref _subBusy, 1) == 1) 
            {
                Debug.Print("[PacketStats] SafeRestartCapture: already in progress, skipping");
                return;
            }
            
            try
            {
                // Debounce: не чаще чем раз в 500мс
                long now = Environment.TickCount;
                if (now - _lastStartMs < 500)
                {
                    Debug.Print("[PacketStats] SafeRestartCapture: debounce protection");
                    return;
                }
                _lastStartMs = now;
                
                Debug.Print("[PacketStats] SafeRestartCapture: starting");
                DebugLogger.log($"[Capture] Restart requested (captureAll={CaptureAll}, ignoreVirtual={_ignoreVirtual})");
                
                // Сначала останавливаем все предыдущие подписки
                StopSubscription();
                
                // Инициализация пакетного буфера и менеджера соединений
                if (PacketBuffer == null) PacketBuffer = new List<CapturedPacket>();
                else 
                {
                    lock (_packetBufferLock) { PacketBuffer.Clear(); }
                }
                if (connMngr == null) connMngr = new ConnectionsManager(500);
                
                // Настройка Local IP
                App.meterState.LocalIP = App.settingsForm.local_ip_textbox.Text;
                _enableIpv6 = App.settingsManager?.GetOption("enable_ipv6", "True", "SETTINGS") == "True";
                DebugLogger.log($"[Capture] Settings applied (localIp={App.meterState.LocalIP}, enableIpv6={_enableIpv6})");
                
                // Запуск CaptureService подписки
                StartCaptureService();
                
                // Включение UI таймеров
                RefreshTimer.Enabled = true;
                active_refresh.Enabled = true;
                avgStats.Enabled = true;
                tracking = true;
                
                // Сброс счетчиков
                inPackets = outPackets = inTraffic = outTraffic = 0;
                ResetTransportDecodeErrors();
                Interlocked.Exchange(ref _etwRemapLogCount, 0);
                
                Debug.Print("[PacketStats] SafeRestartCapture: completed successfully");
            }
            catch (Exception ex)
            {
                Debug.Print($"[PacketStats] SafeRestartCapture error: {ex.Message}");
                MessageBox.Show($"Packet capture start error: {ex.Message}");
            }
            finally
            {
                Volatile.Write(ref _subBusy, 0);
            }
        }
        
        /// <summary>
        /// Запуск подписки через CaptureService вместо BackgroundWorker
        /// </summary>
        private void StartCaptureService()
        {
            if (App.Capture == null)
            {
                Debug.Print("[PacketStats] StartCaptureService: App.Capture is null!");
                return;
            }
            
            // Получаем список адаптеров
            var devices = GetSelectedDevices();
            if (devices.Count == 0)
            {
                Debug.Print("[PacketStats] StartCaptureService: no devices selected");
                DebugLogger.log("[Capture] Subscription skipped: no devices selected");
                return;
            }
            
            // Создаем подписку через CaptureService (автоматический дедуп по StableKey)
            _captureSub = App.Capture.Subscribe(devices, OnPacketReceived);
            _captureRunning = true;
            
            Debug.Print($"[PacketStats] StartCaptureService: subscribed to {devices.Count} devices via CaptureService");
            DebugLogger.log($"[Capture] Subscription started on {devices.Count} device(s)");
        }
        
        /// <summary>
        /// Получить список адаптеров для захвата (с фильтрацией виртуальных)
        /// </summary>
        private List<LivePacketDevice> GetSelectedDevices()
        {
            var devices = new List<LivePacketDevice>();
            var allDevices = App.GetAdapters();
            
            if (CaptureAll)
            {
                // Захват всех адаптеров с фильтрацией
                foreach (var device in allDevices.Skip(1)) // Пропускаем первый элемент (заглушка)
                {
                    if (ShouldIncludeDevice(device))
                        devices.Add(device);
                }
            }
            else
            {
                // Выбранный адаптер
                int selectedIndex = App.settingsForm.adapters_list.SelectedIndex;
                if (selectedIndex > 0 && selectedIndex < allDevices.Count)
                {
                    devices.Add(allDevices[selectedIndex]);
                }
            }
            
            Debug.Print($"[PacketStats] GetSelectedDevices: {devices.Count} devices selected (CaptureAll={CaptureAll})");
            return devices;
        }
        
        /// <summary>
        /// Проверить, должен ли адаптер быть включен в захват
        /// </summary>
        private bool ShouldIncludeDevice(LivePacketDevice device)
        {
            if (device == null)
                return false;

            var description = (device.Description ?? string.Empty).ToLowerInvariant();

            if (description.Contains("loopback") || description.Contains("npcap loopback"))
                return false;

            // На VPN-профиле/при наличии туннеля разрешаем виртуальные адаптеры
            bool hasTunnel = false;
            try
            {
                var tunnelHints = new[] { "wintun", "wireguard", "tap", "tun", "openvpn", "tailscale", "zerotier" };
                hasTunnel = LivePacketDevice.AllLocalMachine.Any(d => TunDetector.IsTunLike(d, tunnelHints));
            }
            catch { }
            
            bool effectiveIgnoreVirtual = (VpnSettings.ForceCaptureVirtual || hasTunnel) ? false : _ignoreVirtual;
            if (effectiveIgnoreVirtual && IsVirtualDevice(device))
            {
                var label = string.Concat(device.Name ?? string.Empty, " ", device.Description ?? string.Empty).Trim();
                if (!VpnHeuristicsLib.IfaceLooksVpn(label))
                    return false;
            }

            return true;
        }

        private static string GetDeviceKey(LivePacketDevice device)
        {
            if (device == null)
                return string.Empty;

            var name = device.Name ?? string.Empty;
            var idx = name.IndexOf("NPF_{", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                return name.Substring(idx);

            if (!string.IsNullOrWhiteSpace(name))
                return name;

            return device.Description ?? Guid.NewGuid().ToString("N");
        }

        private static bool IsVirtualDevice(LivePacketDevice device)
        {
            if (device == null)
                return false;

            var hints = new[] { "virtual", "vpn", "wireguard", "tun", "tap", "tailscale", "zerotier", "vmware", "hyper-v", "virtualbox" };
            if (TunDetector.IsTunLike(device, hints))
                return true;

            var description = (device.Description ?? string.Empty).ToLowerInvariant();
            if (description.Contains("vethernet") || description.Contains("loopback"))
                return true;

            return false;
        }

        private void EnqueuePacket(CapturedPacket captured)
        {
            if (captured?.Packet == null)
                return;

            lock (_packetBufferLock)
            {
                if (PacketBuffer.Count >= CRITICAL_BUFFER_SIZE)
                {
                    PacketBuffer.Clear();
                    GC.Collect();
                    return;
                }

                if (PacketBuffer.Count >= MAX_PACKET_BUFFER_SIZE)
                {
                    int removeCount = (int)(PacketBuffer.Count * 0.8);
                    PacketBuffer.RemoveRange(0, removeCount);
                }

                PacketBuffer.Add(captured);
            }
        }

        private void TryInitTunnelAutoAttach()
        {
            try
            {
                TunnelAutoAttachLib.Init(EnumerateTunnelCandidates, StartTunnelCapture, StopTunnelCapture);
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[PacketStats] AutoAttach init failed: {ex.GetType().Name} {ex.Message}");
            }
        }

        private IEnumerable<LivePacketDevice> EnumerateTunnelCandidates()
        {
            try
            {
                return LivePacketDevice.AllLocalMachine.Where(d => d != null).ToList();
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[PacketStats] Unable to enumerate adapters for auto attach: {ex.GetType().Name} {ex.Message}");
                return Enumerable.Empty<LivePacketDevice>();
            }
        }

        private void StartTunnelCapture(LivePacketDevice device)
        {
            if (device == null || App.Capture == null)
                return;

            var key = GetDeviceKey(device);
            lock (_tunnelSubscriptionsLock)
            {
                if (_tunnelSubscriptions.ContainsKey(key))
                    return;

                try
                {
                    var sub = App.Capture.Subscribe(new[] { device }, OnPacketReceived);
                    _tunnelSubscriptions[key] = sub;
                    DebugLogger.log($"[AutoAttach] Tunnel capture attached: {device.Description ?? device.Name}");
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[AutoAttach] Failed to attach {device.Description ?? device.Name}: {ex.GetType().Name} {ex.Message}");
                }
            }
        }

        private void StopTunnelCapture(LivePacketDevice device)
        {
            if (device == null)
                return;

            var key = GetDeviceKey(device);
            lock (_tunnelSubscriptionsLock)
            {
                if (_tunnelSubscriptions.TryGetValue(key, out var sub))
                {
                    _tunnelSubscriptions.Remove(key);
                    try { sub.Dispose(); }
                    catch (Exception ex)
                    {
                        DebugLogger.log($"[AutoAttach] Failed to detach {device.Description ?? device.Name}: {ex.GetType().Name} {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Обработчик пакетов от CaptureService
        /// </summary>
        private void OnPacketReceived(Packet packet, LivePacketDevice device)
        {
            try
            {
                if (!tracking || packet == null)
                    return;

                if (!ShouldIncludePacket(packet))
                    return;

                var captured = new CapturedPacket(packet, device, IsVirtualDevice(device));
                EnqueuePacket(captured);

                UpdateTrafficCounters(packet);
            }
            catch (Exception ex)
            {
                Debug.Print($"[PacketStats] OnPacketReceived error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Упрощенная проверка фильтров пакетов
        /// </summary>
        private bool ShouldIncludePacket(Packet packet)
        {
            try
            {
                var ethernet = packet.Ethernet;
                if (ethernet == null)
                    return false;

                var ipv4 = ethernet.IpV4;
                var ipv6 = _enableIpv6 ? ethernet.IpV6 : null;

                if (ipv4 == null && ipv6 == null)
                    return false;

                if (!string.IsNullOrEmpty(packetFilter.DestIpFilter) || !string.IsNullOrEmpty(packetFilter.SourceIpFilter))
                {
                    bool matches = false;
                    if (ipv4 != null)
                        matches |= MatchesIpFilters(ipv4.Source.ToString(), ipv4.Destination.ToString());
                    if (!matches && ipv6 != null)
                        matches |= MatchesIpFilters(ipv6.Source.ToString(), ipv6.CurrentDestination.ToString());

                    if (!matches)
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool MatchesIpFilters(string source, string dest)
        {
            bool matchesSrc = string.IsNullOrEmpty(packetFilter.SourceIpFilter) || source.IndexOf(packetFilter.SourceIpFilter, StringComparison.OrdinalIgnoreCase) >= 0;
            bool matchesDst = string.IsNullOrEmpty(packetFilter.DestIpFilter) || dest.IndexOf(packetFilter.DestIpFilter, StringComparison.OrdinalIgnoreCase) >= 0;
            return matchesSrc || matchesDst;
        }

        private static bool IsLocalAddress(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return false;

            if (!IPAddress.TryParse(ip, out var address))
                return false;

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                var bytes = address.GetAddressBytes();
                if (bytes[0] == 10) return true;
                if (bytes[0] == 127) return true;
                if (bytes[0] == 192 && bytes[1] == 168) return true;
                if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31) return true;
                return false;
            }

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                if (IPAddress.IsLoopback(address)) return true;
                if (address.IsIPv6LinkLocal || address.IsIPv6SiteLocal) return true;
                var bytes = address.GetAddressBytes();
                if ((bytes[0] & 0xfe) == 0xfc) return true; // Unique local fc00::/7
                return false;
            }

            return false;
        }
        
        /// <summary>
        /// Обновление счетчиков трафика
        /// </summary>
        private void UpdateTrafficCounters(Packet packet)
        {
            try
            {
                var ip = packet.Ethernet?.IpV4;
                if (ip == null) return;
                
                string sourceIP = ip.Source.ToString();
                string destIP = ip.Destination.ToString();
                
                // Определяем направление трафика
                bool sourceIsLocal = IsLocalIP(sourceIP);
                bool destIsLocal = IsLocalIP(destIP);
                
                if (sourceIsLocal && !destIsLocal)
                {
                    // Исходящий трафик
                    outPackets++;
                    outTraffic += ip.TotalLength;
                }
                else if (!sourceIsLocal && destIsLocal)
                {
                    // Входящий трафик
                    inPackets++;
                    inTraffic += ip.TotalLength;
                }
                else
                {
                    // Внутренний трафик считаем как исходящий
                    outPackets++;
                    outTraffic += ip.TotalLength;
                }
            }
            catch
            {
                // Игнорируем ошибки при подсчете трафика
            }
        }
        
        /// <summary>
        /// Проверка является ли IP локальным
        /// </summary>
        private bool IsLocalIP(string ip)
        {
            return ip.StartsWith("192.168.") || ip.StartsWith("10.") ||
                   ip.StartsWith("172.16.") || ip.StartsWith("172.17.") ||
                   ip.StartsWith("172.18.") || ip.StartsWith("172.19.") ||
                   ip.StartsWith("172.2") || ip.StartsWith("172.30.") ||
                   ip.StartsWith("127.") || ip == "::1";
        }
        
        /// <summary>
        /// Остановка подписки CaptureService
        /// </summary>
        private void StopSubscription()
        {
            try
            {
                if (_captureSub != null)
                {
                    Debug.Print("[PacketStats] StopSubscription: disposing CaptureService subscription");
                    _captureSub.Dispose();
                    _captureSub = null;
                }
                _captureRunning = false;

                lock (_tunnelSubscriptionsLock)
                {
                    if (_tunnelSubscriptions.Count > 0)
                    {
                        foreach (var sub in _tunnelSubscriptions.Values)
                        {
                            try { sub.Dispose(); } catch { }
                        }
                        _tunnelSubscriptions.Clear();
                    }
                }
                TunnelAutoAttachLib.DetachAll();
                
                // Дополнительно очищаем старые BackgroundWorker (для совместимости)
                if (_pcapWorkers.Count > 0)
                {
                    Debug.Print($"[PacketStats] StopSubscription: cleaning up {_pcapWorkers.Count} legacy workers");
                    foreach (var worker in _pcapWorkers)
                    {
                        try
                        {
                            if (worker != null)
                            {
                                if (worker.IsBusy) worker.CancelAsync();
                                worker.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Print($"[PacketStats] Error disposing legacy worker: {ex.Message}");
                        }
                    }
                    _pcapWorkers.Clear();
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[PacketStats] StopSubscription error: {ex.Message}");
            }
        }

        private void PacketStats_Shown(object sender, EventArgs e)
        {
            Start();
        }

        private void PcapWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            try
            {
                pcapWorker.RunWorkerAsync();

            } catch(Exception) { }

        }

        private void MultiAdapterWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!tracking) return;
            
            // Перезапускаем завершившийся воркер
            var worker = sender as BackgroundWorker;
            if (worker != null && !worker.CancellationPending)
            {
                try
                {
                    worker.RunWorkerAsync();
                }
                catch (Exception ex)
                {
                    // В случае ошибки выводим в консоль отладки, но не останавливаем другие воркеры
                    System.Diagnostics.Debug.WriteLine($"[MultiAdapter] Error restarting worker: {ex.Message}");
                }
            }
        }


        private void PcapWorkerDoWork(object sender, DoWorkEventArgs e)
        {
            // MULTI: слушаем ВСЕ «реальные» адаптеры
            if (CaptureAll)
            {
                var all = App.GetAdapters();
                var real = all
                    .Skip(1) // 0-й элемент обычно заглушка в UI
                    .Where(d =>
                    {
                        if (!_ignoreVirtual) return true;
                        var desc = (d.Description ?? string.Empty).ToLowerInvariant();
                        var name = (d.Name ?? string.Empty).ToLowerInvariant();
                        return !(desc.Contains("loopback") || desc.Contains("npcap")
                              || desc.Contains("hyper-v") || desc.Contains("vmware")
                              || desc.Contains("virtualbox") || desc.Contains("vethernet")
                              || name.Contains("loopback") || desc.Contains("microsoft loopback"));
                    })
                    .ToList();

                if (real.Count == 0)
                    return; // в мульти-режиме выходим тихо, без MessageBox

                foreach (var dev in real)
                {
                    var adapter = (PacketDevice)dev;
                    var w = new BackgroundWorker { WorkerSupportsCancellation = true };
                    w.DoWork += (s, args) => OpenAndCaptureFromAdapter(adapter);
                    w.RunWorkerCompleted += MultiAdapterWorkerCompleted;
                    _pcapWorkers.Add(w);
                    w.RunWorkerAsync();
                }
                return;
            }

            // SINGLE: как было — требуем выбранный адаптер
            if (App.gui.selectedAdapter == null)
                return; // без MessageBox в мульти-режиме

            OpenAndCaptureFromAdapter(App.gui.selectedAdapter);
        }

        /// <summary>
        /// Открывает указанный адаптер и начинает захват пакетов
        /// </summary>
        private void OpenAndCaptureFromAdapter(PacketDevice adapter)
        {
            // Открываем адаптер
            PacketCommunicator communicator = adapter.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 150);
            if (communicator == null)
            {
                // В режиме мультиадаптера просто пропускаем проблемные адаптеры
                if (!CaptureAll)
                {
                    MessageBox.Show("Failed to open the selected adapter!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                return;
            }

            using (communicator)
            {
                var linkKind = communicator.DataLink.Kind;
                try
                {
                    if (!PacketNormalizer.IsSupported(linkKind))
                    {
                        if (!CaptureAll)
                        {
                            MessageBox.Show("This adapter type is not supported in the current configuration!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        return;
                    }
                }
                catch (NotSupportedException)
                {
                    if (!CaptureAll)
                    {
                        MessageBox.Show("This adapter type is not supported!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }

                // Применяем опциональный BPF фильтр из Advanced настроек
                try
                {
                    bool disableBpf = VpnSettings.DisableBpf;
                    bool bpfEnabled = App.settingsManager?.GetOption("bpf_filter_enabled", "False", "ADVANCED") == "True";
                    if (!disableBpf && bpfEnabled)
                    {
                        string filterExpr = App.settingsManager?.GetOption("capture_filter", "ip or ip6", "ADVANCED");
                        if (!string.IsNullOrWhiteSpace(filterExpr))
                        {
                            communicator.SetFilter(filterExpr);
                        }
                    }
                }
                catch { /* ignore filter errors */ }

                // Начинаем получение пакетов с проверкой на остановку
                try
                {
                    while (tracking)
                    {
                        try
                        {
                            // Получаем пакеты порциями с коротким таймаутом
                            var result = communicator.ReceivePackets(100, p =>
                            {
                                var normalized = PacketNormalizer.EnsureEthernet(p, linkKind) ?? p;
                                PacketHandler(normalized);
                            });
                            if (result == PacketCommunicatorReceiveResult.Timeout)
                            {
                                // Таймаут - проверяем флаг tracking и продолжаем
                                continue;
                            }
                            if (result == PacketCommunicatorReceiveResult.BreakLoop)
                            {
                                // Break вызван - выходим
                                break;
                            }
                        }
                        catch (Exception)
                        {
                            // Ошибка чтения - прерываем цикл
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // В режиме мультиадаптера просто пропускаем проблемные адаптеры
                    if (!CaptureAll)
                    {
                        MessageBox.Show($"An error occurred while receiving packets: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }



        private void PacketHandler(Packet packet)
        {
            if (!tracking) return;

            IpV4Datagram ip4 = null;
            IpV6Datagram ip6 = null;

            try
            {
                var ethernet = packet.Ethernet;
                if (ethernet == null)
                    return;

                ip4 = ethernet.IpV4;
                if (ip4 == null && _enableIpv6)
                {
                    ip6 = ethernet.IpV6;
                }
            }
            catch (Exception)
            {
                return;
            }

            if (ip4 == null && ip6 == null)
                return;

            if (ip4 != null)
            {
                packetFilter.ip = ip4;
                if (!packetFilter.Validate()) return;
            }
            else if (!_enableIpv6)
            {
                return;
            }
            
            EnqueuePacket(new CapturedPacket(packet, null, false));

            // Простая логика: подсчитываем все пакеты
            // Исходящие: если source из приватной подсети (наша сеть)
            string sourceIP;
            string destIP;
            int packetLength;

            if (ip4 != null)
            {
                sourceIP = ip4.Source.ToString();
                destIP = ip4.Destination.ToString();
                packetLength = ip4.TotalLength;
            }
            else
            {
                sourceIP = ip6.Source.ToString();
                destIP = ip6.CurrentDestination.ToString();
                packetLength = packet.Length;
            }

            if (ShouldSkipAddressForDisplay(sourceIP) || ShouldSkipAddressForDisplay(destIP))
                return;

            bool sourceIsLocal = IsLocalAddress(sourceIP);
            bool destIsLocal = IsLocalAddress(destIP);

            if (sourceIsLocal && !destIsLocal)
            {
                // Исходящий трафик: из локальной сети в интернет
                outPackets++;
                outTraffic += packetLength;
            }
            else if (!sourceIsLocal && destIsLocal)
            {
                // Входящий трафик: из интернета в локальную сеть
                inPackets++;
                inTraffic += packetLength;
            }
            else
            {
                // Внутренний трафик или неопределенный - считаем как исходящий
                outPackets++;
                outTraffic += packetLength;
            }
        }

        public List<ListViewItem> procItems = new List<ListViewItem>();
        Int32 packet_id;
        private void RefreshTick(object sender, EventArgs e)
        {
            AutoDetectMngr.GetActiveProcessName(true);
            
            // Периодическая принудительная очистка памяти для Live View
            _refreshCounter++;
            if (_refreshCounter >= 50) // Каждые 50 циклов (~5 секунд)
            {
                _refreshCounter = 0;
                lock (_packetBufferLock)
                {
                    // Агрессивная очистка буфера для Live View
                    if (PacketBuffer.Count > MAX_PACKET_BUFFER_SIZE / 2)
                    {
                        PacketBuffer.Clear();
                    }
                }
                // Принудительная сборка мусора
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
            
            // Thread-safe check of PacketBuffer count
            int bufferCount;
            lock (_packetBufferLock)
            {
                bufferCount = PacketBuffer.Count;
            }
            
            if (bufferCount < 1)
            {
                return;
            }
            
            List<CapturedPacket> tmpPackets;
            try
            {
                // Thread-safe extraction of packets from buffer with aggressive cleanup
                lock (_packetBufferLock)
                {
                    // Для Live View обрабатываем меньше пакетов за раз, но чаще
                    int processCount = Math.Min(50, PacketBuffer.Count);
                    tmpPackets = PacketBuffer.Take(processCount).Where(p => p?.Packet != null).ToList();
                    
                    // Удаляем обработанные пакеты из буфера более эффективно
                    if (processCount > 0)
                    {
                        PacketBuffer.RemoveRange(0, processCount);
                    }
                    
                    // Дополнительная защита: если буфер превышает критический размер, полная очистка
                    if (PacketBuffer.Count > CRITICAL_BUFFER_SIZE)
                    {
                        PacketBuffer.Clear();
                        System.GC.Collect(); // Принудительная сборка мусора при критическом переполнении
                    }
                }
            } 
            catch(Exception) 
            { 
                // В случае ошибки безопасно очищаем буфер
                lock (_packetBufferLock)
                {
                    try
                    {
                        PacketBuffer.Clear();
                    }
                    catch (Exception)
                    {
                        // Если даже Clear() падает, пересоздаём список
                        PacketBuffer = new List<CapturedPacket>();
                    }
                }
                return; 
            }
            
            ListViewItem[] items = new ListViewItem[tmpPackets.Count];
            
            Int32 iKey = 0;
            bool vpnBypassAdvanced = VpnSettings.AdvancedEnabled;
            if (!_vpnLogInitialized)
            {
                _vpnLogInitialized = true;
                var basicFlag = App.settingsManager?.GetOption("vpn_bypass_basic", "False", "ADVANCED");
                DebugLogger.log($"[PacketStats] VPN flags: advanced={vpnBypassAdvanced}, basic={basicFlag}, captureVirtual={VpnSettings.ForceCaptureVirtual}, allowRaw={VpnSettings.AllowNonEthernet}, disableBpf={VpnSettings.DisableBpf}, etw={VpnSettings.EnableEtwEnrichment}, tracker={(App.connectionTracker != null)}");
            }
            foreach (var captured in tmpPackets) {
                var packet = captured?.Packet;
                if (packet == null)
                    continue;
                var deviceIsVirtual = captured.IsVirtual;
                var interfaceLabel = string.Concat(captured?.Device?.Name ?? string.Empty, " ", captured?.Device?.Description ?? string.Empty).Trim();
                var interfaceLooksVpn = VpnHeuristicsLib.IfaceLooksVpn(interfaceLabel);

                IpV4Datagram ip4 = null;
                IpV6Datagram ip6 = null;
                try
                {
                    var ethernet = packet.Ethernet;
                    if (ethernet == null)
                        continue;

                    ip4 = ethernet.IpV4;
                    if (ip4 == null && _enableIpv6)
                    {
                        ip6 = ethernet.IpV6;
                    }
                }
                catch (Exception)
                {
                    continue;
                }

                if (ip4 == null && ip6 == null)
                    continue;

                UdpDatagram udp = null;
                TcpDatagram tcp = null;
                string from_ip;
                string to_ip;
                int packetLength;
                string packet_size;
                ConnectionTracker.Info? trackerOverride = null;
                byte trackerProto = 0;
                int trackerSrcPort = 0;
                int trackerDstPort = 0;
                uint fromPort = 0;
                uint toPort = 0;
                string protocol;

                try
                {
                    if (ip4 != null)
                    {
                        udp = ip4.Udp;
                        tcp = ip4.Tcp;
                        from_ip = ip4.Source.ToString();
                        to_ip = ip4.Destination.ToString();
                        packetLength = ip4.TotalLength;
                        protocol = ip4.Protocol.ToString();
                    }
                    else
                    {
                        udp = ip6?.Udp;
                        tcp = ip6?.Tcp;
                        from_ip = ip6.Source.ToString();
                        to_ip = ip6.CurrentDestination.ToString();
                        packetLength = packet.Length;
                        protocol = ip6.NextHeader.ToString();
                    }
                }
                catch (Exception)
                {
                    continue;
                }

                if (ShouldSkipAddressForDisplay(from_ip) || ShouldSkipAddressForDisplay(to_ip))
                    continue;

                bool transportDecodeFailed = false;
                Exception transportException = null;
                string transportKind = "RAW";
                string protocolBeforeOverride = protocol;

                if (udp != null)
                {
                    try
                    {
                        fromPort = udp.SourcePort;
                        toPort = udp.DestinationPort;
                        trackerProto = 17;
                        transportKind = "UDP";
                        protocol = "UDP";
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        transportDecodeFailed = true;
                        transportException = ex;
                        transportKind = "UDP";
                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        transportDecodeFailed = true;
                        transportException = ex;
                        transportKind = "UDP";
                    }
                }
                else if (tcp != null)
                {
                    try
                    {
                        fromPort = tcp.SourcePort;
                        toPort = tcp.DestinationPort;
                        trackerProto = 6;
                        transportKind = "TCP";
                        protocol = "TCP";
                    }
                    catch (ArgumentOutOfRangeException ex)
                    {
                        transportDecodeFailed = true;
                        transportException = ex;
                        transportKind = "TCP";
                    }
                    catch (IndexOutOfRangeException ex)
                    {
                        transportDecodeFailed = true;
                        transportException = ex;
                        transportKind = "TCP";
                    }
                }
                else
                {
                    protocol = protocolBeforeOverride;
                }

                if (transportDecodeFailed)
                {
                    RegisterTransportDecodeFailure(transportKind, protocolBeforeOverride, packetLength, packet, transportException);
                    continue;
                }

                if (trackerProto != 0)
                {
                    trackerSrcPort = (int)fromPort;
                    trackerDstPort = (int)toPort;
                }

                if (trackerProto == 0)
                {
                    protocol = protocolBeforeOverride;
                }

                if (trackerProto != 0 && VpnSettings.EnableEtwEnrichment)
                {
                    try
                    {
                        if (EtwBroker.TryRemap(trackerProto, from_ip, trackerSrcPort, to_ip, trackerDstPort, out var remap))
                        {
                            var remapRemote = remap.RemoteString;
                            if (string.IsNullOrWhiteSpace(remapRemote))
                            {
                                MetadataResolver.Promote(to_ip, string.Empty, remap.SourceTag, remap.SuggestedTtl);
                            }
                            else
                            {
                                MetadataResolver.Promote(to_ip, remapRemote, remap.SourceTag, remap.SuggestedTtl);
                                if (!string.Equals(remapRemote, to_ip, StringComparison.OrdinalIgnoreCase))
                                {
                                    var logIndex = Interlocked.Increment(ref _etwRemapLogCount);
                                    if (logIndex <= 100)
                                    {
                                        DebugLogger.log($"[PacketStats] ETW remap: proto={trackerProto} {from_ip}:{trackerSrcPort} -> {to_ip}:{trackerDstPort} => real={remapRemote} pid={remap.ProcessId} name={remap.ProcessName}");
                                    }
                                }
                            }

                            if (!trackerOverride.HasValue && remap.ProcessId > 0)
                            {
                                trackerOverride = new ConnectionTracker.Info(remap.ProcessId, remap.ProcessName);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"[PacketStats] ETW remap error: {ex.Message}");
                    }
                }

                packet_size = packetLength.ToString();
                string processName = @"n\a";
                if (trackerProto == 17 && udp != null)
                {
                    try
                    {
                        var udpConnections = connMngr.UdpActiveConnections;
                        if (udpConnections.Count > 0)
                        {
                            var record = udpConnections.Find(procReq => procReq.LocalPort == fromPort || procReq.LocalPort == toPort);
                            if (record != null)
                            {
                                processName = record.ProcessName ?? record.ProcessId.ToString();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        processName = @"n\a";
                    }
                }
                else if (trackerProto == 6 && tcp != null)
                {
                    try
                    {
                        var tcpConnections = connMngr.TcpActiveConnections;
                        if (tcpConnections.Count > 0)
                        {
                            var record = tcpConnections.Find(procReq => (procReq.LocalPort == fromPort && procReq.RemotePort == toPort)
                                                                       || (procReq.LocalPort == toPort && procReq.RemotePort == fromPort));
                            if (record != null)
                            {
                                processName = record.ProcessName ?? record.ProcessId.ToString();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        processName = @"n\a";
                    }
                }
                if (vpnBypassAdvanced && trackerProto != 0 && App.connectionTracker != null)
                {
                    try
                    {
                        if (IPAddress.TryParse(from_ip, out var srcAddress) && IPAddress.TryParse(to_ip, out var dstAddress))
                        {
                            bool swappedLookup = false;
                            if (App.connectionTracker.TryResolve(trackerProto, srcAddress, trackerSrcPort, dstAddress, trackerDstPort, out var info) ||
                                (swappedLookup = App.connectionTracker.TryResolve(trackerProto, dstAddress, trackerDstPort, srcAddress, trackerSrcPort, out info)))
                            {
                                trackerOverride = info;
                                if (_trackerLogCount < 100)
                                {
                                    _trackerLogCount++;
                                    if (!swappedLookup)
                                    {
                                        DebugLogger.log($"[PacketStats] Tracker HIT proto={trackerProto} {srcAddress}:{trackerSrcPort} -> {dstAddress}:{trackerDstPort} => PID={info.Pid} EXE={info.Exe}");
                                    }
                                    else
                                    {
                                        DebugLogger.log($"[PacketStats] Tracker HIT proto={trackerProto} (swapped) {dstAddress}:{trackerDstPort} -> {srcAddress}:{trackerSrcPort} => PID={info.Pid} EXE={info.Exe}");
                                    }
                                }
                            }
                            else if (_trackerLogCount < 100)
                            {
                                _trackerLogCount++;
                                DebugLogger.log($"[PacketStats] Tracker MISS proto={trackerProto} {srcAddress}:{trackerSrcPort} -> {dstAddress}:{trackerDstPort} (initialProcess={processName})");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print($"[PacketStats] VPN bypass resolve error: {ex.Message}");
                    }
                }
                if(processName == @"n\a")
                {
                    processName = ETW.resolveProcessname(from_ip, to_ip, fromPort, toPort);
                }
                if (vpnBypassAdvanced && trackerOverride.HasValue)
                {
                    var mergedName = VpnBypassHelper.MergeProcessName(processName, trackerOverride);
                    if (!string.Equals(processName, mergedName, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Print($"[PacketStats] VPN bypass override: {processName} -> {mergedName} (PID {trackerOverride.Value.Pid})");
                    }
                    processName = mergedName;
                }
                
                if (!packetFilter.ValidateProcess(processName)) continue;

                var resolved = ResolveRemoteMetadata(to_ip, toPort, trackerOverride);

                ApplyVpnHeuristics(
                    ref resolved,
                    deviceIsVirtual,
                    interfaceLooksVpn,
                    processName,
                    trackerOverride,
                    trackerProto,
                    trackerSrcPort,
                    trackerDstPort,
                    from_ip,
                    to_ip,
                    fromPort,
                    toPort);

                var resolvedRemote = resolved.remote;
                var resolvedBy = resolved.resolvedBy;

                // Финальная подмена на физике: если процесс похож на VPN-шелл, пытаемся взять реальный endpoint из ETW
                if (!deviceIsVirtual && VpnSettings.EnableEtwEnrichment && trackerProto != 0 && trackerOverride.HasValue)
                {
                    var pid = trackerOverride.Value.Pid;
                    var localPort = trackerSrcPort > 0 ? trackerSrcPort : (int)fromPort;
                    var protocolType = trackerProto == 6 ? ProtocolType.Tcp : (trackerProto == 17 ? ProtocolType.Udp : (ProtocolType?)null);
                    var dstPortForCheck = trackerDstPort > 0 ? trackerDstPort : (int)toPort;

                    if (protocolType.HasValue && VpnHeuristicsLib.LooksLikeVpnShell(processName, protocolType.Value, dstPortForCheck))
                    {
                        if (IPAddress.TryParse(from_ip, out var localAddress))
                        {
                            if (EtwBroker.TryGetRemote(pid, localPort, protocolType.Value, localAddress, out var realEndpoint) && IsRoutableEndpoint(realEndpoint))
                            {
                                resolvedRemote = realEndpoint.ToString();
                                resolvedBy = "ETW-Physical";
                            }
                        }
                    }
                }

                if (_useVirtual)
                {
                    // VirtualMode: добавляем в кольцевой буфер
                    var row = CreatePacketRow(packet, from_ip, fromPort, to_ip, toPort, packetLength, protocol, processName, resolvedRemote, resolvedBy);
                    LogLiveViewEntry(row);
                    RingAdd(row);
                }
                else
                {
                    // Классический режим: создаем ListViewItem
                    ListViewItem item = new ListViewItem(packet.Timestamp.ToString("HH:mm:ss.fff"));

                    packet_id++;
                    string id = packet_id.ToString();
                    item.SubItems.Add(id);
                    item.SubItems.Add(from_ip);
                    item.SubItems.Add(fromPort.ToString());
                    item.SubItems.Add(to_ip);
                    item.SubItems.Add(toPort.ToString());
                    item.SubItems.Add(resolvedRemote);
                    item.SubItems.Add(resolvedBy);
                    item.SubItems.Add(packet_size);
                    item.SubItems.Add(protocol);
                    item.SubItems.Add(processName);
                    
                    items[iKey] = item;
                    iKey++;

                    LogLiveViewEntry(packet.Timestamp, packet_id, from_ip ?? string.Empty, fromPort, to_ip ?? string.Empty, toPort, packetLength, protocol, processName, resolvedRemote, resolvedBy);
                }

                AutoDetectMngr.AnalyzePacket(packet);
            }
            
            procItems.Clear();
            procItems = AutoDetectMngr.GetActiveProccessesList(procItems);
            
            if (_useVirtual)
            {
                // VirtualMode: обновляем размер виртуального списка
                this.BeginInvoke(new Action(() => {
                    try
                    {
                        lock (_ringLock)
                        {
                            listView1.VirtualListSize = _ringCount;
                            if (_ringCount > 0 && autoscroll.Checked)
                            {
                                listView1.EnsureVisible(_ringCount - 1);
                            }
                        }
                    }
                    catch(Exception) 
                    { 
                        // Игнорируем ошибки обновления UI
                    }
                }));
            }
            else if(items.Length > 0)
            {
                int realItems = items.Where(id => id != null).Count();
               
                if (realItems > 0)
                {
                    items =  items.Where(id => id != null).ToArray();
                } else {
                    return;
                }
                
                // Классический режим: используем BeginInvoke вместо Invoke для избежания deadlock
                this.BeginInvoke(new Action(() => {
                    try
                    {
                        listView1.BeginUpdate();
                        ListView.ListViewItemCollection lvic = new ListView.ListViewItemCollection(listView1);
                        // Enforce live view max rows if enabled in Advanced
                        bool limitRows = App.settingsManager?.GetOption("live_max_rows_enabled", "False", "ADVANCED") == "True";
                        int maxRows = 1000;
                        if (limitRows)
                        {
                            var rowsStr = App.settingsManager?.GetOption("live_max_rows", "1000", "ADVANCED");
                            if (!string.IsNullOrEmpty(rowsStr) && int.TryParse(rowsStr, out int parsed) && parsed > 0)
                                maxRows = parsed;
                        }

                        // Add new items
                        lvic.AddRange(items);

                        // Trim excess from the top if exceeding maxRows
                        if (limitRows)
                        {
                            while (listView1.Items.Count > maxRows)
                            {
                                listView1.Items.RemoveAt(0);
                            }
                        }
                        
                        if (autoscroll.Checked && listView1.Items.Count > 0)
                        {
                            listView1.EnsureVisible(listView1.Items.Count - 1);
                        }
                        
                        // Проверяем необходимость переключения на VirtualMode
                        CheckAndSwitchMode();
                        listView1.EndUpdate();
                    }
                    catch(Exception) 
                    { 
                        // Игнорируем ошибки обновления UI
                    }
                }));
            }
            


        }

        
        /// <summary>
        /// Остановка мониторинга с полной очисткой CaptureService подписки
        /// </summary>
        public void Stop()
        {
            Debug.Print("[PacketStats] Stop: beginning");
            DebugLogger.log($"[Capture] Stop requested (inPackets={inPackets}, outPackets={outPackets}, inTraffic={inTraffic}, outTraffic={outTraffic})");
            
            tracking = false;
            RefreshTimer.Enabled = false;
            avgStats.Enabled = false;
            active_refresh.Enabled = false;
            
            // Останавливаем CaptureService подписку (основная система)
            StopSubscription();
            
            // Агрессивная очистка PacketBuffer при остановке
            lock (_packetBufferLock)
            {
                try
                {
                    PacketBuffer.Clear();
                }
                catch (Exception)
                {
                    // Если даже Clear() падает, пересоздаём список
                    PacketBuffer = new List<CapturedPacket>();
                }
            }
            
            // Сброс счётчиков
            _refreshCounter = 0;
            inPackets = outPackets = inTraffic = outTraffic = 0;
            
            // Принудительная сборка мусора после остановки
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            Debug.Print("[PacketStats] Stop: completed");
        }

        /// <summary>
        /// Безопасный перезапуск с debounce
        /// </summary>
        public void Restart()
        {
            Debug.Print("[PacketStats] Restart: called");
            SafeRestartCapture();
        }

        private void clear_Click(object sender, EventArgs e)
        {
            packet_id = 0;
            _packetIdCounter = 0;
            
            lock (_packetBufferLock)
            {
                try
                {
                    PacketBuffer.Clear();
                }
                catch (Exception)
                {
                    // Если Clear() падает, пересоздаём список
                    PacketBuffer = new List<CapturedPacket>();
                }
            }
            
            if (_useVirtual)
            {
                lock (_ringLock)
                {
                    _ringHead = 0;
                    _ringCount = 0;
                    listView1.VirtualListSize = 0;
                }
            }
            else
            {
                listView1.Items.Clear();
            }
        }

        private void stop_Click(object sender, EventArgs e)
        {
            if (tracking)
                Stop();
        }

        private void start_Click(object sender, EventArgs e)
        {
            if (!tracking)
            Start();
        }

        

        private void PacketStats_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        TunnelAutoAttachLib.Dispose();
            if (tracking)
                Stop();
        }

        private void filter_Click(object sender, EventArgs e)
        {
            App.packetFilterForm.Show();
        }

        private void avgStats_Tick(object sender, EventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new MethodInvoker(() => avgStats_Tick(sender, e)));
                return;
            }
            
            // Очищаем название процесса из старого места, теперь оно будет в label5
            top_process_name.Text = "";
            label3.Text = "IN " + inPackets.ToString() + " | OUT " + outPackets.ToString();
            label4.Text = "DL " + (inTraffic / 1024).ToString() + " | UP " + (outTraffic / 1024).ToString();
            
            // Диагностические счетчики - используем CaptureService метрики
            int activeWorkers = App.Capture?.WorkersCount ?? 0;
            int activeSubs = App.Capture?.SubscriptionsCount ?? 0;
            long dedupDrops = App.Capture?.DedupDropped ?? 0;
            int queueSize;
            int bufferCount;
            lock (_packetBufferLock)
            {
                queueSize = PacketBuffer?.Count ?? 0;
                bufferCount = _useVirtual ? _ringCount : (listView1?.Items?.Count ?? 0);
            }
            
            label5.Text = $"Workers: {activeWorkers} | Subs: {activeSubs} | Queue: {queueSize} | Items: {bufferCount}" + 
                         (dedupDrops > 0 ? $" | Dedup drop: {dedupDrops}" : "") +
                         (_useVirtual ? " (Virtual)" : "") + 
                         $"\nLocal IP: {App.meterState.LocalIP}" +
                         $"\n{AutoDetectMngr.GetActiveProcessName()}";
                         
            // Лайв-дамп воркеров каждые 10 секунд для диагностики роста
            if (App.Capture != null && Environment.TickCount % 10000 < 1000) // примерно каждые 10с
            {
                var dump = App.Capture.DebugWorkers();
                if (dump.Length > 8) // Показываем детали только если воркеров больше ожидаемого
                {
                    Debug.Print($"[PacketStats] LIVE DUMP: Workers={dump.Length} :: " +
                        string.Join(", ", dump.Take(10).Select(x => $"{x.key}:{x.refs}")) + 
                        (dump.Length > 10 ? "..." : ""));
                }
            }
        }

        private async void active_refresh_Tick(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {

                listView2.Invoke(new Action(() => {

                    listView2.BeginUpdate();
                    ListView.ListViewItemCollection lvic = new ListView.ListViewItemCollection(listView2);
                    lvic.Clear();
                    try
                    {
                        lvic.AddRange(procItems.ToArray());
                    }
                    catch (Exception)
                    {

                    }

                    listView2.EndUpdate();
                }));
            });
        }
        
        /// <summary>
        /// Обработчик для VirtualMode ListView
        /// </summary>
        private void ListView1_RetrieveVirtualItem(object sender, RetrieveVirtualItemEventArgs e)
        {
            lock (_ringLock)
            {
                if (e.ItemIndex >= 0 && e.ItemIndex < _ringCount)
                {
                    var row = RingGetUnsafe(e.ItemIndex);
                    e.Item = CreateListViewItem(row);
                }
                else
                {
                    e.Item = new ListViewItem("-");
                }
            }
        }
        
        /// <summary>
        /// Создает ListViewItem из PacketRow
        /// </summary>
        private ListViewItem CreateListViewItem(PacketRow row)
        {
            var item = new ListViewItem(row.Timestamp.ToString("HH:mm:ss.fff"));
            item.SubItems.Add(row.Id.ToString());
            item.SubItems.Add(row.SourceIP);
            item.SubItems.Add(row.SourcePort.ToString());
            item.SubItems.Add(row.DestIP);
            item.SubItems.Add(row.DestPort.ToString());
            item.SubItems.Add(row.ResolvedRemote ?? string.Empty);
            item.SubItems.Add(row.ResolvedBy ?? string.Empty);
            item.SubItems.Add(row.Length.ToString());
            item.SubItems.Add(row.Protocol);
            item.SubItems.Add(row.ProcessName);
            return item;
        }
        
        /// <summary>
        /// Добавляет пакет в кольцевой буфер для VirtualMode
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RingAdd(PacketRow row)
        {
            lock (_ringLock)
            {
                int cap = Math.Max(1000, int.Parse(App.settingsManager?.GetOption("live_max_rows", "5000", "ADVANCED") ?? "5000"));
                if (_ring.Length != cap)
                {
                    // Переаллокация под новый лимит
                    var newBuf = new PacketRow[cap];
                    int toCopy = Math.Min(_ringCount, cap);
                    for (int i = 0; i < toCopy; i++) 
                        newBuf[i] = RingGetUnsafe(i);
                    _ring = newBuf; 
                    _ringHead = 0; 
                    _ringCount = toCopy;
                }
                
                if (_ringCount < _ring.Length)
                {
                    _ring[(_ringHead + _ringCount) % _ring.Length] = row;
                    _ringCount++;
                }
                else
                {
                    // Переполнение: перезаписываем самый старый и двигаем head
                    _ring[_ringHead] = row;
                    _ringHead = (_ringHead + 1) % _ring.Length;
                }
            }
        }
        
        /// <summary>
        /// Получает элемент из кольцевого буфера по индексу
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private PacketRow RingGetUnsafe(int index)
        {
            // index: 0.._ringCount-1
            return _ring[(_ringHead + index) % _ring.Length];
        }
        
        /// <summary>
        /// Создает PacketRow из пакета для VirtualMode
        /// </summary>
        private PacketRow CreatePacketRow(Packet packet, string sourceIp, uint sourcePort, string destIp, uint destPort, int length, string protocol, string processName, string resolvedRemote, string resolvedBy)
        {
            return new PacketRow(
                packet.Timestamp,
                Interlocked.Increment(ref _packetIdCounter),
                sourceIp ?? string.Empty,
                sourcePort,
                destIp ?? string.Empty,
                destPort,
                length,
                string.IsNullOrWhiteSpace(protocol) ? "Unknown" : protocol,
                processName ?? "n/a",
                resolvedRemote ?? string.Empty,
                resolvedBy ?? "raw"
            );
        }

        private void LogLiveViewEntry(PacketRow row)
        {
            LogLiveViewEntry(row.Timestamp, row.Id, row.SourceIP, row.SourcePort, row.DestIP, row.DestPort, row.Length, row.Protocol, row.ProcessName, row.ResolvedRemote, row.ResolvedBy);
        }

        private void LogLiveViewEntry(DateTime timestamp, int id, string sourceIp, uint sourcePort, string destIp, uint destPort, int length, string protocol, string processName, string resolvedRemote, string resolvedBy)
        {
            DebugLogger.log($"[LiveView] {timestamp:HH:mm:ss.fff} #{id} {sourceIp}:{sourcePort.ToString(CultureInfo.InvariantCulture)} -> {destIp}:{destPort.ToString(CultureInfo.InvariantCulture)} len={length} proto={protocol} proc={processName} remote={resolvedRemote} by={resolvedBy}");
        }

        private void ResetTransportDecodeErrors()
        {
            Interlocked.Exchange(ref _transportDecodeErrors, 0);
            UpdateTransportDecodeStatus(0);
        }

        private void UpdateTransportDecodeStatus(int count)
        {
            if (transportStatusLabel == null)
                return;

            Action updateAction = () =>
            {
                transportStatusLabel.Visible = count > 0;
                transportStatusLabel.Text = count > 0 ? $"Truncated packets: {count}" : string.Empty;
            };

            if (InvokeRequired)
            {
                try { BeginInvoke(updateAction); } catch { }
            }
            else
            {
                updateAction();
            }
        }

        private void RegisterTransportDecodeFailure(string transportKind, string protocolName, int packetLength, Packet packet, Exception ex)
        {
            var current = Interlocked.Increment(ref _transportDecodeErrors);

            if (current <= TransportDecodeErrorLogLimit)
            {
                var captureLength = packet?.Buffer?.Length ?? 0;
                var errorDetails = ex != null ? $" ex={ex.GetType().Name}:{ex.Message}" : string.Empty;
                DebugLogger.log($"[PacketStats] Skipping {transportKind} packet ({protocolName}) len={packetLength} captured={captureLength}{errorDetails}");
            }

            UpdateTransportDecodeStatus(current);
        }

        private (string remote, string resolvedBy) ResolveRemoteMetadata(string destinationIp, uint destinationPort, ConnectionTracker.Info? trackerOverride)
        {
            string remote = string.IsNullOrWhiteSpace(destinationIp) ? string.Empty : destinationIp;
            string resolvedBy = "raw";

            if (!string.IsNullOrWhiteSpace(destinationIp))
            {
                var meta = MetadataResolver.Resolve(destinationIp);
                if (!string.IsNullOrWhiteSpace(meta.remote) && !string.Equals(meta.remote, destinationIp, StringComparison.OrdinalIgnoreCase))
                {
                    remote = EnsureEndpoint(meta.remote, destinationIp, destinationPort);
                    resolvedBy = meta.source;
                }
                else
                {
                    remote = FormatEndpoint(destinationIp, destinationPort);
                }
            }

            if (trackerOverride.HasValue)
            {
                resolvedBy = AppendSourceTag(resolvedBy, "tracker");
            }

            return (remote, resolvedBy);
        }

        private void ApplyVpnHeuristics(
            ref (string remote, string resolvedBy) resolved,
            bool deviceIsVirtual,
            bool interfaceLooksVpn,
            string processName,
            ConnectionTracker.Info? trackerOverride,
            byte trackerProto,
            int trackerSrcPort,
            int trackerDstPort,
            string fromIp,
            string toIp,
            uint fromPort,
            uint toPort)
        {
            resolved.remote = EnsureEndpoint(resolved.remote, toIp, toPort);

            // Фильтруем мусорные адреса до попыток резолва
            if (ShouldSkipAddressForDisplay(fromIp) || ShouldSkipAddressForDisplay(toIp))
                return;
            
            // Дропаем loopback-пары (127.0.0.1 ↔ 127.0.0.1)
            if (IPAddress.TryParse(fromIp, out var srcAddr) && IPAddress.TryParse(toIp, out var dstAddr))
            {
                if (IPAddress.IsLoopback(srcAddr) && IPAddress.IsLoopback(dstAddr))
                    return;
            }

            if (!VpnSettings.AdvancedEnabled)
                return;

            if (deviceIsVirtual || interfaceLooksVpn)
            {
                resolved.resolvedBy = AppendSourceTag(resolved.resolvedBy, "pcap-vpn");
                return;
            }

            var protocolType = GetProtocol(trackerProto);
            if (!protocolType.HasValue)
                return;

            var targetPort = trackerDstPort > 0 ? trackerDstPort : (int)(toPort > ushort.MaxValue ? ushort.MaxValue : toPort);
            if (!VpnHeuristicsLib.LooksLikeVpnShell(processName, protocolType.Value, targetPort))
                return;

            if (!VpnSettings.EnableEtwEnrichment)
                return;

            int pid = trackerOverride?.Pid ?? 0;
            int localPort = trackerSrcPort > 0 ? trackerSrcPort : (int)(fromPort > ushort.MaxValue ? ushort.MaxValue : fromPort);
            if (pid <= 0 || localPort <= 0)
                return;

            IPAddress localAddress = IPAddress.Any;
            if (!string.IsNullOrWhiteSpace(fromIp) && !IPAddress.TryParse(fromIp, out localAddress))
            {
                localAddress = IPAddress.Any;
            }

            if (EtwBroker.TryGetRemote(pid, localPort, protocolType.Value, localAddress, out var remoteEndpoint) && IsRoutableEndpoint(remoteEndpoint))
            {
                var realRemoteFormatted = FormatEndpoint(remoteEndpoint.Address, remoteEndpoint.Port);
                resolved.remote = realRemoteFormatted;
                resolved.resolvedBy = AppendSourceTag(resolved.resolvedBy, "etw-vpn");
                // Записываем полный endpoint с портом в MetadataResolver для последующих lookup
                MetadataResolver.Promote(toIp, realRemoteFormatted, "etw-vpn", TimeSpan.FromSeconds(5));
                return;
            }

            if (EtwBroker.TryGetRecentRemote(TimeSpan.FromSeconds(2), out var fallbackEndpoint) && IsRoutableEndpoint(fallbackEndpoint))
            {
                var fallbackRemoteFormatted = FormatEndpoint(fallbackEndpoint.Address, fallbackEndpoint.Port);
                resolved.remote = fallbackRemoteFormatted;
                resolved.resolvedBy = AppendSourceTag(resolved.resolvedBy, "etw-recent");
                // Записываем полный endpoint с портом в MetadataResolver
                MetadataResolver.Promote(toIp, fallbackRemoteFormatted, "etw-recent", TimeSpan.FromSeconds(3));
            }
        }

        private static ProtocolType? GetProtocol(byte proto)
        {
            switch (proto)
            {
                case 6:
                    return ProtocolType.Tcp;
                case 17:
                    return ProtocolType.Udp;
                default:
                    return null;
            }
        }

        private static string EnsureEndpoint(string current, string fallbackIp, uint port)
        {
            if (string.IsNullOrWhiteSpace(fallbackIp))
                return current ?? string.Empty;

            if (string.IsNullOrWhiteSpace(current))
                return FormatEndpoint(fallbackIp, port);

            if (string.Equals(current, fallbackIp, StringComparison.OrdinalIgnoreCase))
                return FormatEndpoint(fallbackIp, port);

            if (current.IndexOf(':') < 0 && port > 0)
                return FormatEndpoint(current, port);

            return current;
        }

        private static bool ShouldSkipAddressForDisplay(string ip)
        {
            if (string.IsNullOrWhiteSpace(ip))
                return true;

            if (string.Equals(ip, "0.0.0.0", StringComparison.Ordinal) || string.Equals(ip, "::", StringComparison.Ordinal))
                return true;

            if (IPAddress.TryParse(ip, out var parsed) && (IPAddress.IsLoopback(parsed) || parsed.Equals(IPAddress.Any) || parsed.Equals(IPAddress.IPv6Any)))
                return true;

            return false;
        }

        private static bool IsRoutableEndpoint(IPEndPoint endpoint)
        {
            if (endpoint == null || endpoint.Port <= 0)
                return false;

            return IsRoutableAddress(endpoint.Address);
        }

        private static bool IsRoutableAddress(IPAddress address)
        {
            if (address == null)
                return false;

            if (address.Equals(IPAddress.Any) || address.Equals(IPAddress.None) || address.Equals(IPAddress.IPv6Any))
                return false;

            if (IPAddress.IsLoopback(address))
                return false;

            return true;
        }

        private static string FormatEndpoint(string hostOrIp, uint port)
        {
            if (string.IsNullOrWhiteSpace(hostOrIp))
                return string.Empty;

            if (port == 0)
                return hostOrIp;

            if (IPAddress.TryParse(hostOrIp, out var parsed) && parsed.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                return $"[{parsed}]:{port}";
            }

            return $"{hostOrIp}:{port}";
        }

        private static string FormatEndpoint(IPAddress address, int port)
        {
            if (address == null)
                return string.Empty;

            if (port <= 0)
                return address.ToString();

            if (address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                return $"[{address}]:{port}";
            }

            return $"{address}:{port}";
        }

        private static string AppendSourceTag(string existing, string tag)
        {
            if (string.IsNullOrWhiteSpace(tag))
                return existing;

            if (string.IsNullOrWhiteSpace(existing) || string.Equals(existing, "raw", StringComparison.OrdinalIgnoreCase))
                return tag;

            if (existing.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
                return existing;

            return $"{existing}+{tag}";
        }

        private void CheckAndSwitchMode()
        {
            const int VIRTUAL_THRESHOLD = 2000;
            int currentCount = _useVirtual ? _ringCount : listView1.Items.Count;
            
            // Переход на Virtual mode при превышении порога
            if (!_useVirtual && currentCount >= VIRTUAL_THRESHOLD)
            {
                listView1.VirtualMode = true;
                listView1.VirtualListSize = currentCount;
                _useVirtual = true;
                
                // Перенос данных из Items в Ring Buffer
                for (int i = 0; i < Math.Min(currentCount, _ring.Length); i++)
                {
                    var item = listView1.Items[i];
                    
                    // Парсим данные из SubItems
                    DateTime.TryParse(item.SubItems[0].Text, out DateTime ts);
                    int.TryParse(item.SubItems[1].Text, out int id);
                    uint.TryParse(item.SubItems[3].Text, out uint srcPort);
                    uint.TryParse(item.SubItems[5].Text, out uint dstPort);
                    int.TryParse(item.SubItems[8].Text, out int len);
                    
                    _ring[i] = new PacketRow(
                        ts, id,
                        item.SubItems[2].Text, srcPort,          // source IP, source port
                        item.SubItems[4].Text, dstPort,          // dest IP, dest port
                        len,
                        item.SubItems[9].Text,                   // protocol
                        item.SubItems[10].Text,                  // process
                        item.SubItems[6].Text,                   // resolved remote
                        item.SubItems[7].Text                    // resolved by
                    );
                }
                _ringCount = Math.Min(currentCount, _ring.Length);
                _ringHead = _ringCount % _ring.Length;
                
                listView1.Items.Clear();
                if (!_virtualModeSwitchLogged)
                {
                    _virtualModeSwitchLogged = true;
                    DebugLogger.log($"[LiveView] Switched to virtual mode (count={currentCount})");
                }
            }
        }
    }
}
