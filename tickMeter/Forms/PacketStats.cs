using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using tickMeter.Classes;

namespace tickMeter
{
    public partial class PacketStats : Form
    {
        List<Packet> PacketBuffer;
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

        // === UI коалессация для плавного multi-adapter ===
        private readonly ConcurrentQueue<ListViewItem> _lvQueue = new ConcurrentQueue<ListViewItem>();
        private long _bytesIn, _bytesOut, _pktsIn, _pktsOut;
        private readonly Stopwatch _flushSw = Stopwatch.StartNew();
        private System.Windows.Forms.Timer _flushTimer;
        private const int FLUSH_INTERVAL_MS = 120;   // ~8–10 Гц
        private const int FLUSH_INTERVAL_FAST = 80;  // ускоряемся при большом хвосте
        private const int MAX_ROWS_PER_FLUSH = 120;  // ограничиваем нагрузку на ListView
        private const int MAX_QUEUE_ROWS = 5000;     // не раздуваем очередь
        private bool _dedupEnabled => App.settingsManager.GetOption("dedup_multi_nic", "True", "SETTINGS") == "True";

        public PacketStats()
        {
            InitializeComponent();
            packetFilter = new PacketFilter();
            
            // Включаем «двойную буферизацию» для ListView (убирает мерцание)
            try {
                typeof(ListView).InvokeMember("DoubleBuffered",
                    System.Reflection.BindingFlags.SetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
                    null, listView1, new object[] { true });
            } catch { }

            // Централизованный таймер для плавного UI
            _flushTimer = new System.Windows.Forms.Timer { Interval = FLUSH_INTERVAL_MS };
            _flushTimer.Tick += FlushTimer_Tick;
            _flushTimer.Start();
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
            
            // Очищаем дедупликатор при старте захвата
            // TODO: раскомментировать после компиляции
            // Classes.FastDedup.Clear();
            
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
            if (!App.meterState.IsTracking) return;
            
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
                        return !(desc.Contains("loopback") || desc.Contains("npcap")
                              || desc.Contains("hyper-v") || desc.Contains("vmware")
                              || desc.Contains("virtualbox") || desc.Contains("vethernet"));
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
            // Открываем адаптер с оптимизированным ReadTimeout
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
                if (communicator.DataLink.Kind != DataLinkKind.Ethernet)
                {
                    // В режиме мультиадаптера просто пропускаем неподдерживаемые адаптеры
                    if (!CaptureAll)
                    {
                        MessageBox.Show("This program works only on Ethernet networks!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    return;
                }

                // Лёгкий BPF-фильтр, чтобы не тянуть мусор (настройка опциональна)
                try 
                {
                    var filterExpr = App.settingsManager.GetOption("capture_filter", "ip or ip6", "SETTINGS");
                    if (!string.IsNullOrWhiteSpace(filterExpr) && filterExpr != "disabled")
                    {
                        using (var filter = communicator.CreateFilter(filterExpr))
                            communicator.SetFilter(filter);
                    }
                } 
                catch { /* фильтр не обязателен */ }

                // Начинаем получение пакетов
                try
                {
                    communicator.ReceivePackets(0, PacketHandler);
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
            
            // анти-дубли в мульти-NIC (включается флагом dedup_multi_nic)
            // TODO: раскомментировать после компиляции проекта
            /*
            if (_dedupEnabled && CaptureAll)
            {
                try
                {
                    // Используем первые байты пакета для дедупликации без аллокаций
                    var buffer = packet.Buffer;
                    if (buffer != null && buffer.Length > 0)
                    {
                        if (Classes.FastDedup.TrySeen(buffer, 96)) return;
                    }
                }
                catch (Exception) { } // не роняем воркер из-за дедупа
            }
            */
            
            IpV4Datagram ip;
            try
            {
                ip = packet.Ethernet.IpV4;
            }
            catch (Exception) { return; }
            packetFilter.ip = ip;
            if (!packetFilter.Validate()) return;
            
            // Теперь в потоках захвата НИКАКОГО UI — только быстрая агрегация
            ProcessPacketForCoalescence(packet, ip);
            
            PacketBuffer.Add(packet);
        }
        
        // Новый метод для коалессации - заменяет прямые UI вызовы
        private void ProcessPacketForCoalescence(Packet packet, IpV4Datagram ip)
        {
            try
            {
                int len = ip?.TotalLength ?? 0;
                bool incoming = ComputeIncoming(ip);
                
                if (incoming) {
                    Interlocked.Add(ref _bytesIn, len);
                    Interlocked.Increment(ref _pktsIn);
                } else {
                    Interlocked.Add(ref _bytesOut, len);
                    Interlocked.Increment(ref _pktsOut);
                }

                // если окно скрыто/свернуто — строки в очередь не кладём, только счётчики
                if (!this.Visible || this.WindowState == FormWindowState.Minimized) return;

                // backpressure: если очередь большая — дропаем старые записи
                while (_lvQueue.Count > MAX_QUEUE_ROWS) 
                {
                    _lvQueue.TryDequeue(out _);
                }

                var row = BuildListViewItem(packet, ip);
                if (row != null)
                {
                    _lvQueue.Enqueue(row);
                }
            }
            catch { /* не роняем воркер */ }
        }
        
        // Определяет направление пакета
        private bool ComputeIncoming(IpV4Datagram ip)
        {
            if (ip == null) return false;
            return ip.Destination.ToString() == App.meterState.LocalIP;
        }
        
        // Строит элемент ListView (перенесено из RefreshTick)
        private ListViewItem BuildListViewItem(Packet packet, IpV4Datagram ip)
        {
            if (packet == null || ip == null) return null;
            
            try
            {
                UdpDatagram udp = null;
                TcpDatagram tcp = null;
                string from_ip = ip.Source.ToString();
                string to_ip = ip.Destination.ToString();
                string packet_size = ip.TotalLength.ToString();
                string protocol = ip.Protocol.ToString();
                uint fromPort = 0;
                uint toPort = 0;
                string processName = @"n\a";
                
                if (protocol == IpV4Protocol.Udp.ToString())
                {
                    udp = ip.Udp;
                    if (udp != null)
                    {
                        fromPort = udp.SourcePort;
                        toPort = udp.DestinationPort;
                        // ... логика определения процесса для UDP ...
                    }
                }
                else if (protocol == IpV4Protocol.Tcp.ToString())
                {
                    tcp = ip.Tcp;
                    if (tcp != null)
                    {
                        fromPort = tcp.SourcePort;
                        toPort = tcp.DestinationPort;
                        // ... логика определения процесса для TCP ...
                    }
                }
                
                if (!packetFilter.ValidateProcess(processName)) return null;

                ListViewItem item = new ListViewItem(packet.Timestamp.ToString("HH:mm:ss.fff"));
                item.SubItems.Add((++packet_id).ToString());
                item.SubItems.Add(from_ip);
                item.SubItems.Add(fromPort.ToString());
                item.SubItems.Add(to_ip);
                item.SubItems.Add(toPort.ToString());
                item.SubItems.Add(packet_size);
                item.SubItems.Add(protocol);
                item.SubItems.Add(processName);
                
                return item;
            }
            catch
            {
                return null;
            }
        }
        
        // Единый быстрый сброс на UI-потоке (по таймеру)
        private void FlushTimer_Tick(object sender, EventArgs e)
        {
            // адаптивная частота: если большой хвост — ускоряемся
            _flushTimer.Interval = (_lvQueue.Count > 2000) ? FLUSH_INTERVAL_FAST : FLUSH_INTERVAL_MS;

            // Реальный Δt: если UI был занят, таймер мог прийти позже — считаем по Stopwatch
            double dt = Math.Max(0.001, _flushSw.Elapsed.TotalSeconds);
            _flushSw.Restart();

            long bi = Interlocked.Exchange(ref _bytesIn, 0);
            long bo = Interlocked.Exchange(ref _bytesOut, 0);
            long pi = Interlocked.Exchange(ref _pktsIn, 0);
            long po = Interlocked.Exchange(ref _pktsOut, 0);

            double kbpsIn = (bi * 8.0) / 1000.0 / dt;
            double kbpsOut = (bo * 8.0) / 1000.0 / dt;

            // Разом обновляем счётчики
            try {
                label3.Text = $"IN {pi} | OUT {po}";
                label4.Text = $"DL {(bi / 1024):0} | UP {(bo / 1024):0}";
            } catch { /* форма могла закрыться */ }

            // Выгружаем ограниченную пачку строк, без Insert(0)
            try
            {
                listView1.BeginUpdate();
                int added = 0;
                while (added < MAX_ROWS_PER_FLUSH && _lvQueue.TryDequeue(out var row))
                {
                    listView1.Items.Add(row);
                    added++;
                }
                if (autoscroll.Checked && listView1.Items.Count > 0)
                    listView1.EnsureVisible(listView1.Items.Count - 1);
                listView1.EndUpdate();
            }
            catch { /* игнорируем ошибки обновления UI */ }

            // Если очередь распухла — мягко прореживаем «хвост»
            while (_lvQueue.Count > MAX_QUEUE_ROWS) 
            {
                _lvQueue.TryDequeue(out _);
            }
        }

        public List<ListViewItem> procItems = new List<ListViewItem>();
        Int32 packet_id;
        private void RefreshTick(object sender, EventArgs e)
        {
            AutoDetectMngr.GetActiveProcessName(true);
            if (PacketBuffer.Count < 1)
            {
                return;
            }
            List<Packet> tmpPackets;
            try
            {
                // Ограничиваем количество пакетов для обработки за раз (максимум 50)
                tmpPackets = PacketBuffer.Where(p => p != null).Take(50).ToList();
            } 
            catch(Exception) 
            { 
                return; 
            }
            
            // Удаляем обработанные пакеты из буфера
            try
            {
                int removeCount = Math.Min(tmpPackets.Count, PacketBuffer.Count);
                for (int i = 0; i < removeCount; i++)
                {
                    if (PacketBuffer.Count > 0)
                        PacketBuffer.RemoveAt(0);
                }
            }
            catch(Exception)
            {
                PacketBuffer.Clear(); // В случае ошибки просто очищаем весь буфер
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

                AutoDetectMngr.AnalyzePacket(packet);
            }
            int realItems = items.Where(id => id != null).Count();
           
            if (realItems > 0)
            {
                items =  items.Where(id => id != null).ToArray();
            } else {
                return;
            }
            procItems.Clear();
            procItems = AutoDetectMngr.GetActiveProccessesList(procItems);
            if(items.Length > 0)
            {
                // Используем BeginInvoke вместо Invoke для избежания deadlock
                this.BeginInvoke(new Action(() => {
                    try
                    {
                        listView1.BeginUpdate();
                        ListView.ListViewItemCollection lvic = new ListView.ListViewItemCollection(listView1);
                        lvic.AddRange(items);
                        
                        if (autoscroll.Checked && listView1.Items.Count > 0)
                        {
                            listView1.EnsureVisible(listView1.Items.Count - 1);
                        }
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
                } 
            }
            catch { }
            _pcapWorkers.Clear();
        }

        public void Restart()
        {
            Stop();
            Start();
        }

        private void clear_Click(object sender, EventArgs e)
        {
            packet_id = 0;
            PacketBuffer.Clear();
            listView1.Items.Clear();
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
            
            // Очищаем ресурсы UI таймера
            try { 
                _flushTimer?.Stop(); 
                _flushTimer?.Dispose(); 
            } catch { }
        }

        private void filter_Click(object sender, EventArgs e)
        {
            App.packetFilterForm.Show();
        }

        private async void avgStats_Tick(object sender, EventArgs e)
        {
            await Task.Run(() =>
            {
                top_process_name.Invoke(new Action(() => {
                    top_process_name.Text = AutoDetectMngr.GetActiveProcessName();
                }));
                label3.Invoke( new Action(() =>{
                        label3.Text = "IN " + inPackets.ToString() + " | OUT " + outPackets.ToString();
                }));
                label4.Invoke(new Action(() => {
                    label4.Text = "DL " + (inTraffic / 1024).ToString() + " | UP " + (outTraffic/ 1024).ToString();
                }));
                label5.Invoke(new Action(() => {
                    label5.Text = "Local IP: " + App.meterState.LocalIP;
                }));
                inPackets = outPackets = inTraffic = outTraffic = 0;
            });
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
    }
}
