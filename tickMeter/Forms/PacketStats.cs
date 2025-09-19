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
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using tickMeter.Classes;

namespace tickMeter
{
    public partial class PacketStats : Form
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

        public PacketStats()
        {
            InitializeComponent();
            packetFilter = new PacketFilter();
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
            // Безопасно инициализируем COM для текущего потока
            SafeCoInitialize();
            
            try
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
                    if (communicator.DataLink.Kind != DataLinkKind.Ethernet)
                    {
                        // В режиме мультиадаптера просто пропускаем неподдерживаемые адаптеры
                        if (!CaptureAll)
                        {
                            MessageBox.Show("This program works only on Ethernet networks!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        return;
                    }

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
                                    // Прокачиваем сообщения Windows каждый таймаут
                                    PumpMessages();
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
            finally
            {
                // Не вызываем CoUninitialize для Thread потоков,
                // так как они могут использовать уже существующий COM контекст
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
            PacketBuffer.Add(packet);

            if (ip.Source.ToString() == App.meterState.LocalIP)
            {
                outPackets++;
                outTraffic += ip.TotalLength;
            }
            if (ip.Destination.ToString() == App.meterState.LocalIP)
            {
                inPackets++;
                inTraffic += ip.TotalLength;
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
                // ETW removed: do not attempt resolve via ETW
                
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
            
            // Даём время воркерам завершиться корректно
            System.Threading.Thread.Sleep(200);
            
            // Останавливаем все multi-adapter воркеры
            try 
            { 
                foreach (var worker in _pcapWorkers) 
                { 
                    if (worker.IsBusy)
                        worker.CancelAsync();
                } 
                
                // Ждём завершения воркеров (кратко)
                for (int i = 0; i < 20; i++)
                {
                    bool anyBusy = false;
                    foreach (var worker in _pcapWorkers)
                    {
                        if (worker.IsBusy) { anyBusy = true; break; }
                    }
                    if (!anyBusy) break;
                    
                    // Прокачиваем сообщения Windows во время ожидания
                    PumpMessages();
                    System.Threading.Thread.Sleep(50);
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
