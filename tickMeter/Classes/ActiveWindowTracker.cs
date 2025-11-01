using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Net.NetworkInformation;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;

namespace tickMeter.Classes
{
    public static class ActiveWindowTracker
    {
        public static Dictionary<string, ProcessNetworkStats> connections = new Dictionary<string, ProcessNetworkStats>();
        public static readonly object connectionsLock = new object();
        
        // Кэш для Windows Statistics в обычном режиме
        private static long _lastTotalDownloaded = 0;
        private static long _lastTotalUploaded = 0;
        private static DateTime _lastStatsUpdate = DateTime.MinValue;
        private static readonly object _statsLock = new object();

        public static void AnalyzePacket(Packet packet)
        {
            if (App.meterState.isBuiltInProfileActive || App.meterState.isCustomProfileActive) { return; }
            if (!IsEnabled()) return;
            if (packet?.Ethernet == null) return; // Защита от null/поврежденных пакетов
            
            IpV4Datagram ip;
            try
            {
                ip = packet.Ethernet.IpV4;
                if (ip == null) return; // VPN/туннелированные пакеты могут не содержать IPv4
            }
            catch (IndexOutOfRangeException) 
            { 
                // Пакет поврежден или имеет недостаточный размер
                return; 
            }
            catch (Exception) 
            { 
                return; 
            }

            UdpDatagram udp = null;
            TcpDatagram tcp = null;
            
            try
            {
                udp = ip.Udp;
                tcp = ip.Tcp;
            }
            catch (IndexOutOfRangeException)
            {
                // Пакет не содержит полных UDP/TCP данных
                return;
            }
            catch (Exception)
            {
                return;
            }

            if (udp == null && tcp == null) return;

            string fromIp = ip.Source.ToString();
            string toIp = ip.Destination.ToString();

            uint packetSize = (uint)ip.TotalLength;

            string protocol = ip.Protocol.ToString();
            uint fromPort = 0;
            uint toPort = 0;
            string processName = @"n\a";
            uint id = 0;
            
            try
            {
                if (protocol == IpV4Protocol.Udp.ToString())
                {
                    if (udp == null) return; // Дополнительная проверка
                    fromPort = udp.SourcePort;
                    toPort = udp.DestinationPort;
                    try
                    {
                        UdpProcessRecord record;
                        List<UdpProcessRecord> UdpConnections = App.connMngr.UdpActiveConnections;
                        if (UdpConnections.Count > 0)
                        {
                            record = UdpConnections.Find(procReq => procReq.LocalPort == fromPort || procReq.LocalPort == toPort);

                            if (record != null)
                            {
                                processName = record.ProcessName != null ? record.ProcessName : record.ProcessId.ToString();
                            }
                        }
                    }
                    catch (Exception) { processName = @"n\a"; }
                }
                else
                {
                    if (tcp == null) return; // Дополнительная проверка
                    fromPort = tcp.SourcePort;
                    toPort = tcp.DestinationPort;
                    try
                    {
                        TcpProcessRecord record;
                        List<TcpProcessRecord> TcpConnections = App.connMngr.TcpActiveConnections;
                        if (TcpConnections.Count > 0)
                        {
                            record = TcpConnections.Find(procReq =>
                            (procReq.LocalPort == fromPort && procReq.RemotePort == toPort)
                            || (procReq.LocalPort == toPort && procReq.RemotePort == fromPort)
                            );
                            if (record != null)
                            {
                                processName = record.ProcessName;
                            }
                        }
                    }
                    catch (InvalidOperationException) { processName = @"n\a"; }
                }
            }
            catch (IndexOutOfRangeException)
            {
                // Пакет не содержит полной информации о портах
                return;
            }
            catch (Exception)
            {
                return;
            }

            if (processName == @"n\a")
            {
                processName = ETW.resolveProcessname(fromIp, toIp, fromPort, toPort);
            }

            string activeProcess = AutoDetectMngr.GetActiveProcessName();
            if (activeProcess != processName) { return; }
            uint remotePort = 0;
            uint localPort = 0;
            
            try
            {
                if (App.meterState.LocalIP == toIp.ToString())
                {
                    switch (protocol.ToLower())
                    {
                        case "udp":
                            if (udp != null)
                            {
                                remotePort = udp.SourcePort;
                                localPort = udp.DestinationPort;
                            }
                            break;
                        case "tcp":
                            if (tcp != null)
                            {
                                remotePort = tcp.SourcePort;
                                localPort = tcp.DestinationPort;
                            }
                            break;
                    }
                    trackTick(processName, protocol.ToLower(), App.meterState.LocalIP, localPort, remotePort, 1, 0, packetSize, packet.Timestamp, id);
                }
                else
                {
                    switch (protocol.ToLower())
                    {
                        case "udp":
                            if (udp != null)
                            {
                                remotePort = udp.DestinationPort;
                                localPort = udp.SourcePort;
                            }
                            break;
                        case "tcp":
                            if (tcp != null)
                            {
                                remotePort = tcp.DestinationPort;
                                localPort = tcp.SourcePort;
                            }
                            break;
                    }
                    trackTick(processName, protocol.ToLower(), App.meterState.LocalIP, localPort, remotePort, 0, 1, packetSize, packet.Timestamp, 0);
                }
            }
            catch (IndexOutOfRangeException)
            {
                // Пакет не содержит полной информации о портах
                return;
            }
            catch (Exception)
            {
                return;
            }
        }

        public static void trackTick(string name, string protocol, string localIp, uint localPort, string remoteIp, uint remotePort, int tickIn, int tickOut, uint traffic, DateTime tickTime, uint id)
        {
            // Проверяем настройку - использовать ли Windows Statistics вместо PCAP трафика
            bool useWindowsStats = App.settingsManager?.GetOption("use_windows_stats", "True", "ADVANCED") == "True";
            
            string hash = Hash(name, remoteIp, remotePort);
            lock(connectionsLock)
            {
                if (!connections.ContainsKey(hash))
                {
                    connections.Add(hash, new ProcessNetworkStats());
                    connections[hash].name = name;
                    connections[hash].localIp = localIp;
                    connections[hash].remoteIp = remoteIp;
                    connections[hash].localPort = localPort;
                    connections[hash].remotePort = remotePort;
                    connections[hash].downloaded = 0;
                    connections[hash].sent = 0;
                    connections[hash].ticksIn = 0;
                    connections[hash].ticksOut = 0;
                    connections[hash].startTrack = tickTime;
                    connections[hash].id = 0;
                }
                connections[hash].protocol = protocol;
                connections[hash].ticksIn += tickIn;
                connections[hash].ticksOut += tickOut;

                if (tickIn > 0)
                {
                    connections[hash].updateTicktimeBuffer(tickTime.Ticks);
                    connections[hash].lastUpdate = tickTime;
                    connections[hash].id = id;

                    if (App.meterState.Server.Ip != remoteIp)
                        App.meterState.Server.Ip = remoteIp; // триггерит DetectLocation()

                    // Выбираем источник трафика
                    if (useWindowsStats)
                    {
                        // Используем Windows Statistics - обновим трафик позже через UpdateTrafficFromWindowsStats
                        // Здесь только отмечаем что есть активность
                    }
                    else
                    {
                        // Используем PCAP трафик (старый метод)
                        connections[hash].downloaded += (int)traffic;
                        App.meterState.DownloadTraffic += (int)traffic;
                    }
                }
                if (tickOut > 0)
                {
                    if (!useWindowsStats)
                    {
                        // Используем PCAP трафик (старый метод)
                        connections[hash].sent += (int)traffic;
                        App.meterState.UploadTraffic += (int)traffic;
                    }
                }
            }
        }

        public static string Hash(string name, string remoteIp, uint remotePort)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] data = Encoding.UTF8.GetBytes(name + remoteIp + remotePort);
                byte[] hashData = sha1.ComputeHash(data);
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashData)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }
        
        /// <summary>
        /// Получение реального трафика через Windows Statistics (альтернатива PCAP)
        /// </summary>
        public static (long downloadDelta, long uploadDelta) GetWindowsNetworkStats()
        {
            lock (_statsLock)
            {
                try
                {
                    long currentDownloaded = 0;
                    long currentUploaded = 0;
                    
                    // Получаем статистику всех активных сетевых интерфейсов
                    foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                    {
                        // Пропускаем loopback и неактивные интерфейсы
                        if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback || 
                            ni.OperationalStatus != OperationalStatus.Up)
                            continue;
                        
                        // Получаем статистику интерфейса
                        IPv4InterfaceStatistics stats = ni.GetIPv4Statistics();
                        currentDownloaded += stats.BytesReceived;
                        currentUploaded += stats.BytesSent;
                    }
                    
                    // Первый запуск - инициализация
                    if (_lastStatsUpdate == DateTime.MinValue)
                    {
                        _lastTotalDownloaded = currentDownloaded;
                        _lastTotalUploaded = currentUploaded;
                        _lastStatsUpdate = DateTime.Now;
                        return (0, 0);
                    }
                    
                    // Подсчет дельты
                    long downloadDelta = Math.Max(0, currentDownloaded - _lastTotalDownloaded);
                    long uploadDelta = Math.Max(0, currentUploaded - _lastTotalUploaded);
                    
                    // Обновляем кэш
                    _lastTotalDownloaded = currentDownloaded;
                    _lastTotalUploaded = currentUploaded;
                    _lastStatsUpdate = DateTime.Now;
                    
                    // Логирование для отладки
                    if (downloadDelta > 0 || uploadDelta > 0)
                    {
                        Debug.Print($"[WindowsStats] Delta: download={downloadDelta}, upload={uploadDelta}");
                    }
                    
                    return (downloadDelta, uploadDelta);
                }
                catch (Exception ex)
                {
                    Debug.Print($"[WindowsStats] Error: {ex.Message}");
                    return (0, 0);
                }
            }
        }
        
        /// <summary>
        /// Обновляет трафик через Windows Statistics (вызывается периодически)
        /// </summary>
        public static void UpdateTrafficFromWindowsStats()
        {
            bool useWindowsStats = App.settingsManager?.GetOption("use_windows_stats", "True", "ADVANCED") == "True";
            if (!useWindowsStats) return;
            
            var (downloadDelta, uploadDelta) = GetWindowsNetworkStats();
            
            if (downloadDelta > 0 || uploadDelta > 0)
            {
                // Масштабируем значения для реалистичного отображения
                int scaledDownload = (int)(downloadDelta / 1000); // Делим на 1000 для более разумных значений
                int scaledUpload = (int)(uploadDelta / 1000);
                
                // Обновляем глобальный трафик
                App.meterState.DownloadTraffic += scaledDownload;
                App.meterState.UploadTraffic += scaledUpload;
                
                // Обновляем трафик активных соединений
                lock (connectionsLock)
                {
                    foreach (var connection in connections.Values)
                    {
                        if (connection.lastUpdate > DateTime.Now.AddSeconds(-5)) // Только активные соединения
                        {
                            connection.downloaded += scaledDownload / Math.Max(1, connections.Count);
                            connection.sent += scaledUpload / Math.Max(1, connections.Count);
                        }
                    }
                }
                
                Debug.Print($"[WindowsStats] Applied traffic: download={scaledDownload}, upload={scaledUpload} (from delta: {downloadDelta}/{uploadDelta})");
            }
        }
        
        /// <summary>
        /// Очищает статистику соединений (при смене активного процесса)
        /// </summary>
        public static void ClearConnectionStats()
        {
            lock (connectionsLock)
            {
                connections.Clear();
                Debug.WriteLine("[ActiveWindowTracker] Connection stats cleared");
            }
        }
    }
}