using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;

namespace tickMeter.Classes
{
    public static class ETW
    {
        /// <summary>
        /// Счетчики входящих пакетов для VPN bypass режима по процессам
        /// </summary>
        private static readonly ConcurrentDictionary<string, long> _incomingPacketCounters = new ConcurrentDictionary<string, long>();
        private static readonly ConcurrentDictionary<string, DateTime> _lastPacketTime = new ConcurrentDictionary<string, DateTime>();
        private static readonly ConcurrentDictionary<string, long> _packetsPerSecond = new ConcurrentDictionary<string, long>();
        
        /// <summary>
        /// Счетчики трафика для VPN bypass режима (байты)
        /// </summary>
        private static readonly ConcurrentDictionary<string, long> _uploadBytes = new ConcurrentDictionary<string, long>();
        private static readonly ConcurrentDictionary<string, long> _downloadBytes = new ConcurrentDictionary<string, long>();
        private static readonly ConcurrentDictionary<string, long> _uploadBytesPerSecond = new ConcurrentDictionary<string, long>();
        private static readonly ConcurrentDictionary<string, long> _downloadBytesPerSecond = new ConcurrentDictionary<string, long>();
        private static DateTime _lastTrafficUpdate = DateTime.Now;
        
        /// <summary>
        /// Активный процесс для мониторинга (из ActiveWindowTracker)
        /// </summary>
        private static string _activeProcessName = "";
        private static readonly object _activeProcessLock = new object();
        public class ProcessNetworkData
        {
            public string pName;
            public int pId;
            public string toIp;
            public string fromIp;
            public uint toPort;
            public uint fromPort;

            public ProcessNetworkData(string name, int pId, string toIp, string fromIp, uint toPort, uint fromPort)
            {
                this.pName = name;
                this.pId = pId;
                this.toIp = toIp;
                this.fromIp = fromIp;
                this.toPort = toPort;
                this.fromPort = fromPort;
            }

            public static string Hash(string name, int pId, string toIp, string fromIp, int toPort, int fromPort)
            {
                using (MD5 md5 = MD5.Create())
                {
                    byte[] inputBytes = new UTF8Encoding().GetBytes(name + pId.ToString() + toIp + fromIp + toPort.ToString() + fromPort.ToString());
                    byte[] hashBytes = md5.ComputeHash(inputBytes);

                    return BitConverter.ToString(hashBytes).Replace("-", string.Empty).ToLower();
                }
            }

            internal static void processEventData(string processName, int processID, string saddr, string daddr, int sport, int dport)
            {
                string hash = ProcessNetworkData.Hash(processName, processID, saddr, daddr, sport, dport);
                if (
                    processes.ContainsKey(hash) && (
                        processes[hash].pName != processName
                        || processes[hash].toIp != daddr
                        || processes[hash].fromIp != saddr
                        || processes[hash].toPort != dport
                        || processes[hash].fromPort != sport
                    )
                )
                {
                    processes.Remove(hash);
                }
                if (!processes.ContainsKey(hash))
                {
                    processes.Add(hash, new ProcessNetworkData(processName, processID, saddr, daddr, (uint)sport, (uint)dport));
                }
            }
        }

        public static Dictionary<string, ProcessNetworkData> processes = new Dictionary<string, ProcessNetworkData>();
        
        /// <summary>
        /// Устанавливает активный процесс для мониторинга пакетов в VPN bypass режиме
        /// </summary>
        public static void SetActiveProcess(string processName)
        {
            lock (_activeProcessLock)
            {
                if (!string.IsNullOrEmpty(processName) && _activeProcessName != processName)
                {
                    _activeProcessName = processName.Replace(".exe", "").ToLower();
                    DebugLogger.log($"[ETW-VPN] Active process set to: {_activeProcessName}");
                }
            }
        }
        
        /// <summary>
        /// Получает количество входящих пакетов в секунду для активного процесса
        /// </summary>
        public static long GetIncomingPacketsPerSecond(string processName = null)
        {
            string targetProcess = processName?.Replace(".exe", "").ToLower() ?? _activeProcessName;
            if (string.IsNullOrEmpty(targetProcess))
                return 0;
                
            return _packetsPerSecond.TryGetValue(targetProcess, out long count) ? count : 0;
        }
        
        /// <summary>
        /// Сбрасывает счетчики пакетов для всех процессов
        /// </summary>
        public static void ResetPacketCounters()
        {
            _incomingPacketCounters.Clear();
            _lastPacketTime.Clear();
            _packetsPerSecond.Clear();
            DebugLogger.log("[ETW-VPN] Packet counters reset");
        }
        
        /// <summary>
        /// Обновляет счетчик входящих пакетов для процесса
        /// </summary>
        private static void IncrementIncomingPackets(string processName, string sourceIP, string destIP)
        {
            if (string.IsNullOrEmpty(processName))
                return;
                
            string cleanProcessName = processName.Replace(".exe", "").ToLower();
            DateTime now = DateTime.UtcNow;
            
            // Проверяем, что это входящий пакет (destIP должен быть локальным)
            if (!IsLocalIP(destIP))
                return;
            
            // Увеличиваем счетчик
            long newCount = _incomingPacketCounters.AddOrUpdate(cleanProcessName, 1, (key, oldValue) => oldValue + 1);
            
            // Обновляем время последнего пакета
            DateTime lastTime = _lastPacketTime.TryGetValue(cleanProcessName, out DateTime prevTime) ? prevTime : now;
            _lastPacketTime[cleanProcessName] = now;
            
            // Рассчитываем пакеты в секунду каждую секунду
            double timeDiff = (now - lastTime).TotalSeconds;
            if (timeDiff >= 1.0)
            {
                long prevCount = _incomingPacketCounters.TryGetValue(cleanProcessName + "_prev", out long prev) ? prev : 0;
                long packetsInLastSecond = newCount - prevCount;
                _packetsPerSecond[cleanProcessName] = Math.Max(0, (long)(packetsInLastSecond / timeDiff));
                _incomingPacketCounters[cleanProcessName + "_prev"] = newCount;
                
                // Логируем только для активного процесса
                if (cleanProcessName == _activeProcessName && _packetsPerSecond[cleanProcessName] > 0)
                {
                    DebugLogger.log($"[ETW-VPN] {cleanProcessName}: {_packetsPerSecond[cleanProcessName]} packets/sec (from {sourceIP})");
                }
            }
        }
        
        /// <summary>
        /// Проверяет, является ли IP-адрес локальным
        /// </summary>
        private static bool IsLocalIP(string ip)
        {
            if (string.IsNullOrEmpty(ip))
                return false;
                
            try
            {
                // Получаем локальный IP из LocalIPDetector для более точной проверки
                var cachedLocalIp = Classes.LocalIPDetector.GetCachedIP();
                if (!string.IsNullOrEmpty(cachedLocalIp) && ip == cachedLocalIp)
                    return true;
                
                // Простая проверка локальных адресов
                return ip.StartsWith("192.168.") || 
                       ip.StartsWith("10.") || 
                       ip.StartsWith("172.") ||
                       ip.StartsWith("127.") ||
                       ip == "localhost" ||
                       ip.StartsWith("169.254."); // Link-local addresses
            }
            catch
            {
                // Fallback к простой проверке при ошибках
                return ip.StartsWith("192.168.") || 
                       ip.StartsWith("10.") || 
                       ip.StartsWith("127.");
            }
        }
        public static void init()
        {
            Thread t = new Thread(ETWSessionThread);
            t.IsBackground = true;
            t.Start();
        }

        private static async void ETWSessionThread()
        {
            await Task.Run(() =>
            {
                using (var kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName))
                {
                    kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);


                    kernelSession.Source.Kernel.TcpIpAccept += acceptTCPIP;
                    kernelSession.Source.Kernel.TcpIpPartACK += ackTCPIP;
                    kernelSession.Source.Kernel.TcpIpFullACK += ackTCPIP;
                    kernelSession.Source.Kernel.TcpIpDupACK += ackTCPIP;
                    kernelSession.Source.Kernel.TcpIpConnect += acceptTCPIP;
                    kernelSession.Source.Kernel.TcpIpReconnect += tcpIpTrace;
                    kernelSession.Source.Kernel.TcpIpConnectIPV6 += acceptTCPIPv6;
                    kernelSession.Source.Kernel.TcpIpSendIPV6 += sendTCPIPv6;
                    kernelSession.Source.Kernel.TcpIpRecvIPV6 += recvTCPIPv6;
                    kernelSession.Source.Kernel.TcpIpAcceptIPV6 += acceptTCPIPv6;
                    kernelSession.Source.Kernel.TcpIpSend += tcpIpSend;
                    kernelSession.Source.Kernel.TcpIpRecv += tcpIpTrace;
                    kernelSession.Source.Kernel.UdpIpSend += udpSendTrace;
                    kernelSession.Source.Kernel.UdpIpRecv += udpSendTrace;

                    kernelSession.Source.Process();
                }
            });
        }

        private static void ackTCPIP(TcpIpTraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
            // Подсчитываем входящие пакеты для VPN bypass
            IncrementIncomingPackets(session.ProcessName, session.saddr.ToString(), session.daddr.ToString());
        }

        private static void recvTCPIPv6(TcpIpV6TraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
            // Подсчитываем входящие пакеты для VPN bypass
            IncrementIncomingPackets(session.ProcessName, session.saddr.ToString(), session.daddr.ToString());
        }

        private static void sendTCPIPv6(TcpIpV6SendTraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
        }


        private static void acceptTCPIPv6(TcpIpV6ConnectTraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
        }


        private static void udpSendTrace(UdpIpTraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
            // Подсчитываем входящие UDP пакеты для VPN bypass (только если это receive, не send)
            // В ETW UdpIpRecv и UdpIpSend используют один обработчик, поэтому проверяем направление
            if (IsLocalIP(session.daddr.ToString()))
            {
                IncrementIncomingPackets(session.ProcessName, session.saddr.ToString(), session.daddr.ToString());
                // Добавляем трафик для VPN bypass (входящий UDP)
                AddTrafficData(session.ProcessName, false, session.size); // download
            }
            else if (IsLocalIP(session.saddr.ToString()))
            {
                // Добавляем трафик для VPN bypass (исходящий UDP)
                AddTrafficData(session.ProcessName, true, session.size); // upload
            }
        }


        private static void acceptTCPIP(TcpIpConnectTraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
        }

        private static void tcpIpTrace(TcpIpTraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
            // Подсчитываем входящие TCP пакеты для VPN bypass
            IncrementIncomingPackets(session.ProcessName, session.saddr.ToString(), session.daddr.ToString());
            
            // Добавляем трафик для VPN bypass (входящий TCP)
            if (IsLocalIP(session.daddr.ToString()))
            {
                AddTrafficData(session.ProcessName, false, session.size); // download
            }
        }

        private static void tcpIpSend(TcpIpSendTraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
            
            // Добавляем трафик для VPN bypass (исходящий TCP)
            if (IsLocalIP(session.saddr.ToString()))
            {
                AddTrafficData(session.ProcessName, true, session.size); // upload
            }
        }

        public static string resolveProcessname(string fromIp, string toIp, uint fromPort, uint toPort)
        {
            try
            {
                foreach (ProcessNetworkData procData in processes.Values)
                {
                    if(procData == null) continue;
                    if (
                            (procData.toPort == toPort
                        && procData.fromPort == fromPort
                        && procData.toIp == toIp
                        && procData.fromIp == fromIp)
                        ||
                        (procData.toPort == fromPort
                        && procData.fromPort == toPort
                        && procData.toIp == fromIp
                        && procData.fromIp == toIp))
                    {
                        return procData.pName;
                    }
                }
            } catch { }
            return @"n\a";
        }

        /// <summary>
        /// Добавляет данные о трафике для процесса
        /// </summary>
        private static void AddTrafficData(string processName, bool isUpload, long bytes)
        {
            if (string.IsNullOrEmpty(processName)) return;
            
            try
            {
                if (isUpload)
                {
                    _uploadBytes.AddOrUpdate(processName, bytes, (key, oldValue) => oldValue + bytes);
                }
                else
                {
                    _downloadBytes.AddOrUpdate(processName, bytes, (key, oldValue) => oldValue + bytes);
                }
                
                // Обновляем счетчики байт/сек каждую секунду
                var now = DateTime.Now;
                if ((now - _lastTrafficUpdate).TotalSeconds >= 1.0)
                {
                    UpdateTrafficCounters();
                    _lastTrafficUpdate = now;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETW-Traffic] Error adding traffic data: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновляет счетчики байт/сек для всех процессов
        /// </summary>
        private static void UpdateTrafficCounters()
        {
            foreach (var process in _uploadBytes.Keys)
            {
                long bytes;
                if (_uploadBytes.TryGetValue(process, out bytes))
                {
                    _uploadBytesPerSecond[process] = bytes;
                    _uploadBytes[process] = 0; // Сброс счетчика
                }
            }
            
            foreach (var process in _downloadBytes.Keys)
            {
                long bytes;
                if (_downloadBytes.TryGetValue(process, out bytes))
                {
                    _downloadBytesPerSecond[process] = bytes;
                    _downloadBytes[process] = 0; // Сброс счетчика
                }
            }
        }

        /// <summary>
        /// Получает скорость загрузки для процесса в байтах/сек
        /// </summary>
        public static long GetUploadBytesPerSecond(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return 0;
            long bytes;
            return _uploadBytesPerSecond.TryGetValue(processName, out bytes) ? bytes : 0;
        }

        /// <summary>
        /// Получает скорость скачивания для процесса в байтах/сек  
        /// </summary>
        public static long GetDownloadBytesPerSecond(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return 0;
            long bytes;
            return _downloadBytesPerSecond.TryGetValue(processName, out bytes) ? bytes : 0;
        }
    }
}
