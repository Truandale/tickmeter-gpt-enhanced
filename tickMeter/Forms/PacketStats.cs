using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using tickMeter.Classes;
using System.Runtime.CompilerServices;

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
        
        public PacketRow(DateTime ts, int id, string srcIp, uint srcPort, string dstIp, uint dstPort, 
                        int len, string proto, string proc)
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
        }
    }
    
    public partial class PacketStats : Form
    {
        List<Packet> PacketBuffer;
        private readonly object _packetBufferLock = new object();  // Thread synchronization lock
        private const int MAX_PACKET_BUFFER_SIZE = 100;  // Максимальный размер буфера для Live View (уменьшен с 1000)
        private const int CRITICAL_BUFFER_SIZE = 200;    // Критический размер для экстренной очистки
        private int _refreshCounter = 0;                  // Счётчик для периодической очистки памяти
        public int inPackets = 0;
        public int outPackets = 0;
        public int inTraffic = 0;
        public int outTraffic = 0;

        public ConnectionsManager connMngr;

        public bool tracking;
        Thread PcapThread;
        public BackgroundWorker pcapWorker;
        public PacketFilter packetFilter;

        // Multi-adapter support
        private readonly List<BackgroundWorker> _pcapWorkers = new List<BackgroundWorker>();
        private bool CaptureAll => App.settingsManager.GetOption("capture_all_adapters", "False", "SETTINGS") == "True";
        private bool _ignoreVirtual => App.settingsManager.GetOption("ignore_virtual_adapters", "True", "SETTINGS") == "True";

        // CaptureService integration - временно отключено до исправления проблем компиляции
        // private tickMeter.Classes.CaptureService.Subscription _captureSub;
        private bool _captureRunning;
        private long _lastStartMs;

        // VirtualMode ListView support
        private readonly object _ringLock = new object();
        private PacketRow[] _ring;
        private int _ringHead = 0, _ringCount = 0; // head — индекс самого старого
        private bool _useVirtual = false;
        private int _packetIdCounter = 0;

        public PacketStats()
        {
            InitializeComponent();
            packetFilter = new PacketFilter();
            
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
        }
        public void InitWorker()
        {
            pcapWorker = new BackgroundWorker();
            pcapWorker.DoWork += PcapWorkerDoWork;
            pcapWorker.RunWorkerCompleted += PcapWorkerCompleted;
            pcapWorker.RunWorkerAsync();
        }

        

        public void Start()
        {
            
            PacketBuffer = new List<Packet>();
            connMngr = new ConnectionsManager(500);
            try
            {
                if (PcapThread == null)
                {
                    PcapThread = new Thread(InitWorker);
                    PcapThread.Start();
                }
                
                
            } catch (Exception)
            {
                MessageBox.Show("NPCAP Thread init error");
            }
            
            App.meterState.LocalIP = App.settingsForm.local_ip_textbox.Text;
            RefreshTimer.Enabled = true;
            active_refresh.Enabled = true;
            avgStats.Enabled = true;
            tracking = true;
            
            // Сбрасываем счетчики при запуске
            inPackets = outPackets = inTraffic = outTraffic = 0;

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
                // Проверяем, что адаптер поддерживает Ethernet
                try
                {
                    if (communicator.DataLink.Kind != DataLinkKind.Ethernet)
                    {
                        // В режиме мультиадаптера просто пропускаем неподдерживаемые адаптеры
                        if (!CaptureAll)
                        {
                            MessageBox.Show("This program works only on Ethernet networks!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        return;
                    }
                }
                catch (NotSupportedException)
                {
                    // Неподдерживаемый тип адаптера (например, loopback) - пропускаем
                    if (!CaptureAll)
                    {
                        MessageBox.Show("This adapter type is not supported!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }

                // Применяем опциональный BPF фильтр из Advanced настроек
                try
                {
                    bool bpfEnabled = App.settingsManager?.GetOption("bpf_filter_enabled", "False", "ADVANCED") == "True";
                    if (bpfEnabled)
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
                            var result = communicator.ReceivePackets(100, PacketHandler);
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
            IpV4Datagram ip;
            try
            {
                ip = packet.Ethernet.IpV4;
            }
            catch (Exception) { return; }
            packetFilter.ip = ip;
            if (!packetFilter.Validate()) return;
            
            // Thread-safe addition to PacketBuffer with aggressive size limit for Live View
            lock (_packetBufferLock)
            {
                // Критическая проверка: если буфер сильно превысил лимит - полная очистка
                if (PacketBuffer.Count >= CRITICAL_BUFFER_SIZE)
                {
                    PacketBuffer.Clear();
                    // Форсированная сборка мусора при критическом переполнении
                    GC.Collect();
                    return; // Пропускаем этот пакет
                }
                
                // Обычная очистка при достижении лимита
                if (PacketBuffer.Count >= MAX_PACKET_BUFFER_SIZE)
                {
                    // Удаляем 80% старых пакетов для Live View
                    int removeCount = (int)(PacketBuffer.Count * 0.8);
                    PacketBuffer.RemoveRange(0, removeCount);
                }
                
                PacketBuffer.Add(packet);
            }

            // Простая логика: подсчитываем все пакеты
            // Исходящие: если source из приватной подсети (наша сеть)
            string sourceIP = ip.Source.ToString();
            string destIP = ip.Destination.ToString();
            
            bool sourceIsLocal = sourceIP.StartsWith("192.168.") || sourceIP.StartsWith("10.") || 
                               sourceIP.StartsWith("172.16.") || sourceIP.StartsWith("172.17.") ||
                               sourceIP.StartsWith("172.18.") || sourceIP.StartsWith("172.19.") ||
                               sourceIP.StartsWith("172.2") || sourceIP.StartsWith("172.30.") ||
                               sourceIP.StartsWith("127.") || sourceIP == "::1";
                               
            bool destIsLocal = destIP.StartsWith("192.168.") || destIP.StartsWith("10.") || 
                             destIP.StartsWith("172.16.") || destIP.StartsWith("172.17.") ||
                             destIP.StartsWith("172.18.") || destIP.StartsWith("172.19.") ||
                             destIP.StartsWith("172.2") || destIP.StartsWith("172.30.") ||
                             destIP.StartsWith("127.") || destIP == "::1";

            if (sourceIsLocal && !destIsLocal)
            {
                // Исходящий трафик: из локальной сети в интернет
                outPackets++;
                outTraffic += ip.TotalLength;
            }
            else if (!sourceIsLocal && destIsLocal)
            {
                // Входящий трафик: из интернета в локальную сеть
                inPackets++;
                inTraffic += ip.TotalLength;
            }
            else
            {
                // Внутренний трафик или неопределенный - считаем как исходящий
                outPackets++;
                outTraffic += ip.TotalLength;
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
            
            List<Packet> tmpPackets;
            try
            {
                // Thread-safe extraction of packets from buffer with aggressive cleanup
                lock (_packetBufferLock)
                {
                    // Для Live View обрабатываем меньше пакетов за раз, но чаще
                    int processCount = Math.Min(50, PacketBuffer.Count);
                    tmpPackets = PacketBuffer.Take(processCount).Where(p => p != null).ToList();
                    
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
                        PacketBuffer = new List<Packet>();
                    }
                }
                return; 
            }
            
            ListViewItem[] items = new ListViewItem[tmpPackets.Count];
            
            Int32 iKey = 0;
            foreach (Packet packet in tmpPackets) {
                // Проверяем, что пакет не null
                if (packet == null)
                    continue;
                    
                IpV4Datagram ip;
                try
                {
                    // Проверяем, что пакет имеет Ethernet заголовок
                    if (packet.Ethernet == null)
                        continue;
                    
                    // Проверяем, что это IPv4 пакет
                    ip = packet.Ethernet.IpV4;
                    if (ip == null)
                        continue;
                }
                catch (Exception) { continue; } // Продолжаем обработку следующего пакета

                UdpDatagram udp = null;
                TcpDatagram tcp = null;
                string from_ip = "";
                string to_ip = "";
                string packet_size = "";
                
                try
                {
                    udp = ip.Udp;
                    tcp = ip.Tcp;
                    
                    from_ip = ip.Source.ToString();
                    to_ip = ip.Destination.ToString();
                    packet_size = ip.TotalLength.ToString();
                }
                catch (Exception) { continue; } // Пропускаем пакет, если не можем получить базовую информацию

                string protocol = ip.Protocol.ToString();
                uint fromPort = 0;
                uint toPort = 0;
                string processName = @"n\a";
                if (protocol == IpV4Protocol.Udp.ToString() && udp != null)
                {
                    fromPort = udp.SourcePort;
                    toPort = udp.DestinationPort;
                    try
                    {
                        UdpProcessRecord record;
                        List<UdpProcessRecord> UdpConnections = connMngr.UdpActiveConnections;
                        if (UdpConnections.Count > 0)
                        {
                            record = UdpConnections.Find(
                                procReq => procReq.LocalPort == fromPort || procReq.LocalPort == toPort
                                );

                            if (record != null)
                            {
                                processName = record.ProcessName;
                                if(processName == null)
                                {
                                    processName = record.ProcessId.ToString();
                                }
                            }
                        }
                    } catch(Exception) { 
                        processName = @"n\a"; 
                    }
                    
                }
                else if (protocol == IpV4Protocol.Tcp.ToString() && tcp != null)
                {
                    fromPort = tcp.SourcePort;
                    toPort = tcp.DestinationPort;
                    try
                    {
                        TcpProcessRecord record;
                        List<TcpProcessRecord> TcpConnections = connMngr.TcpActiveConnections;
                        if(TcpConnections.Count > 0)
                        {
                            record = TcpConnections.Find(
                                procReq => (procReq.LocalPort == fromPort && procReq.RemotePort == toPort)
                                || (procReq.LocalPort == toPort && procReq.RemotePort == fromPort)
                                );
                            if (record != null)
                            {
                                processName = record.ProcessName;
                                if (processName == null)
                                {
                                    processName = record.ProcessId.ToString();
                                }
                            }
                        }
                        
                    } catch (Exception) { 
                        processName = @"n\a"; 
                    }

                }
                if(processName == @"n\a")
                {
                    processName = ETW.resolveProcessname(from_ip, to_ip, fromPort, toPort);
                }
                
                if (!packetFilter.ValidateProcess(processName)) continue;

                if (_useVirtual)
                {
                    // VirtualMode: добавляем в кольцевой буфер
                    var row = CreatePacketRow(packet, ip, udp, tcp, processName);
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
                    item.SubItems.Add(packet_size);
                    item.SubItems.Add(protocol);
                    item.SubItems.Add(processName);
                    
                    items[iKey] = item;
                    iKey++;
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

        
        public void Stop()
        {
            tracking = false;
            RefreshTimer.Enabled = false;
            
            // Останавливаем все multi-adapter воркеры
            try 
            { 
                foreach (var worker in _pcapWorkers) 
                { 
                    if (worker.IsBusy)
                        worker.CancelAsync();
                    worker?.Dispose();
                } 
            }
            catch { }
            _pcapWorkers.Clear();
            
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
                    PacketBuffer = new List<Packet>();
                }
            }
            
            // Сброс счётчиков
            _refreshCounter = 0;
            
            // Принудительная сборка мусора после остановки
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        public void Restart()
        {
            Stop();
            Start();
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
                    PacketBuffer = new List<Packet>();
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
            
            top_process_name.Text = AutoDetectMngr.GetActiveProcessName();
            label3.Text = "IN " + inPackets.ToString() + " | OUT " + outPackets.ToString();
            label4.Text = "DL " + (inTraffic / 1024).ToString() + " | UP " + (outTraffic / 1024).ToString();
            
            // Диагностические счетчики
            int activeWorkers = _pcapWorkers.Count;
            int queueSize;
            int bufferCount;
            lock (_packetBufferLock)
            {
                queueSize = PacketBuffer?.Count ?? 0;
                bufferCount = _useVirtual ? _ringCount : (listView1?.Items?.Count ?? 0);
            }
            
            label5.Text = $"Local IP: {App.meterState.LocalIP} | Workers: {activeWorkers} | Queue: {queueSize} | Items: {bufferCount}" + 
                         (_useVirtual ? " (Virtual)" : "");
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
        private PacketRow CreatePacketRow(Packet packet, IpV4Datagram ip, UdpDatagram udp, TcpDatagram tcp, string processName)
        {
            string protocol = ip.Protocol.ToString();
            uint fromPort = 0;
            uint toPort = 0;
            
            if (protocol == IpV4Protocol.Udp.ToString() && udp != null)
            {
                fromPort = udp.SourcePort;
                toPort = udp.DestinationPort;
            }
            else if (protocol == IpV4Protocol.Tcp.ToString() && tcp != null)
            {
                fromPort = tcp.SourcePort;
                toPort = tcp.DestinationPort;
            }
            
            return new PacketRow(
                packet.Timestamp,
                Interlocked.Increment(ref _packetIdCounter),
                ip.Source.ToString(),
                fromPort,
                ip.Destination.ToString(),
                toPort,
                ip.TotalLength,
                protocol,
                processName ?? "n/a"
            );
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
                    uint.TryParse(item.SubItems[2].Text, out uint srcPort);
                    uint.TryParse(item.SubItems[4].Text, out uint dstPort);
                    int.TryParse(item.SubItems[5].Text, out int len);
                    
                    _ring[i] = new PacketRow(
                        ts, id, 
                        item.SubItems[3].Text, srcPort,  // source IP, source port
                        item.SubItems[4].Text, dstPort,  // dest IP, dest port
                        len, 
                        item.SubItems[6].Text,           // protocol
                        item.SubItems[7].Text            // process
                    );
                }
                _ringCount = Math.Min(currentCount, _ring.Length);
                _ringHead = _ringCount % _ring.Length;
                
                listView1.Items.Clear();
            }
        }
    }
}
