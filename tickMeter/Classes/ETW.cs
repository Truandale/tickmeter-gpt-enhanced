using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
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
        /// RTT/Ping метрики для VPN bypass режима (Phase 3)
        /// </summary>
        private static readonly ConcurrentDictionary<string, List<long>> _rttHistory = new ConcurrentDictionary<string, List<long>>();
        private static readonly ConcurrentDictionary<string, long> _avgRttMs = new ConcurrentDictionary<string, long>();
        private static readonly ConcurrentDictionary<string, long> _minRttMs = new ConcurrentDictionary<string, long>();
        private static readonly ConcurrentDictionary<string, long> _maxRttMs = new ConcurrentDictionary<string, long>();
        private static readonly ConcurrentDictionary<string, double> _jitterMs = new ConcurrentDictionary<string, double>();
        private static readonly ConcurrentDictionary<string, DateTime> _lastRttUpdate = new ConcurrentDictionary<string, DateTime>();
        private static DateTime _lastRttCleanup = DateTime.Now;
        
        /// <summary>
        /// Отслеживание TCP пакетов для RTT измерений
        /// Ключ: "process:srcIP:srcPort:dstIP:dstPort", Значение: timestamp отправки
        /// </summary>
        private static readonly ConcurrentDictionary<string, DateTime> _tcpSentPackets = new ConcurrentDictionary<string, DateTime>();
        
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
                if (!string.IsNullOrEmpty(processName))
                {
                    string cleanProcessName = processName.Replace(".exe", "").ToLower();
                    if (_activeProcessName != cleanProcessName)
                    {
                        _activeProcessName = cleanProcessName;
                        DebugLogger.log($"[ETW-VPN] Active process set to: {_activeProcessName}");
                    }
                }
            }
        }

        /// <summary>
        /// Проверяет, является ли процесс активным игровым процессом
        /// </summary>
        private static bool IsActiveGameProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName) || string.IsNullOrEmpty(_activeProcessName))
                return false;
            
            string cleanProcessName = processName.Replace(".exe", "").ToLower();
            return cleanProcessName == _activeProcessName;
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
            
            // Фильтруем только для активного игрового процесса
            if (!IsActiveGameProcess(processName))
                return;
            
            // Увеличиваем счетчик для активного игрового процесса
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
                long rawPacketsPerSec = Math.Max(0, (long)(packetsInLastSecond / timeDiff));
                
                // ETW система полностью без ограничений - синхронно с VPN bypass
                // Никаких искусственных лимитов, только реальные данные
                _packetsPerSecond[cleanProcessName] = rawPacketsPerSec;
                _incomingPacketCounters[cleanProcessName + "_prev"] = newCount;
                
                // Логируем для активного игрового процесса
                if (rawPacketsPerSec > 0)
                {
                    DebugLogger.log($"[ETW-UNLIMITED] {cleanProcessName}: {rawPacketsPerSec} packets/sec (no limits) from {sourceIP}");
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
                
                // Phase 3: Измеряем RTT для входящих TCP пакетов
                MeasureTcpRtt(session.ProcessName, session.saddr.ToString(), session.sport, session.daddr.ToString(), session.dport, session.TimeStamp, false);
            }
        }

        private static void tcpIpSend(TcpIpSendTraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
            
            // Добавляем трафик для VPN bypass (исходящий TCP)
            if (IsLocalIP(session.saddr.ToString()))
            {
                AddTrafficData(session.ProcessName, true, session.size); // upload
                
                // Phase 3: Записываем время отправки TCP пакета для RTT измерений
                MeasureTcpRtt(session.ProcessName, session.saddr.ToString(), session.sport, session.daddr.ToString(), session.dport, session.TimeStamp, true);
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
                
                // Очищаем старые RTT данные каждые 30 секунд
                if ((now - _lastRttCleanup).TotalSeconds >= 30.0)
                {
                    CleanupOldRttData();
                    _lastRttCleanup = now;
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

        /// <summary>
        /// Phase 3: Добавляет RTT измерение для процесса (БЕЗ ОГРАНИЧЕНИЙ - показывает всё как есть)
        /// </summary>
        public static void AddRttData(string processName, long rttMs)
        {
            // Фильтруем только для активного игрового процесса
            if (string.IsNullOrEmpty(processName) || rttMs <= 0) return;
            
            if (!IsActiveGameProcess(processName))
            {
                return; // Игнорируем RTT от неактивных процессов
            }

            try
            {
                string process = processName.Replace(".exe", "").ToLower();
                
                // Инициализируем историю RTT если её нет
                if (!_rttHistory.ContainsKey(process))
                {
                    _rttHistory[process] = new List<long>();
                }
                
                var history = _rttHistory[process];
                lock (history)
                {
                    // Добавляем новое RTT значение
                    history.Add(rttMs);
                    
                    // Ограничиваем размер истории (последние 20 значений для расчета jitter)
                    while (history.Count > 20)
                    {
                        history.RemoveAt(0);
                    }
                    
                    // Обновляем статистику
                    UpdateRttStatistics(process, history);
                }
                
                // Обновляем время последнего RTT измерения
                _lastRttUpdate[process] = DateTime.Now;
                
                DebugLogger.log($"[ETW-RTT-GAME] Active game RTT for {process}: {rttMs}ms (avg: {GetAverageRttMs(processName)}ms, jitter: {GetJitterMs(processName):F1}ms)");
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETW-RTT] Error adding RTT data: {ex.Message}");
            }
        }

        /// <summary>
        /// Обновляет RTT статистику для процесса
        /// </summary>
        private static void UpdateRttStatistics(string processName, List<long> history)
        {
            if (history.Count == 0) return;

            // Среднее RTT
            long avgRtt = (long)history.Average();
            _avgRttMs[processName] = avgRtt;

            // Минимальное и максимальное RTT
            _minRttMs[processName] = history.Min();
            _maxRttMs[processName] = history.Max();

            // Jitter (стандартное отклонение RTT)
            if (history.Count > 1)
            {
                double variance = history.Select(x => Math.Pow(x - avgRtt, 2)).Average();
                double jitter = Math.Sqrt(variance);
                _jitterMs[processName] = jitter;
            }
            else
            {
                _jitterMs[processName] = 0.0;
            }
        }

        /// <summary>
        /// Получает среднее RTT для процесса в миллисекундах
        /// </summary>
        public static long GetAverageRttMs(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return 0;
            string process = processName.Replace(".exe", "").ToLower();
            long rtt;
            return _avgRttMs.TryGetValue(process, out rtt) ? rtt : 0;
        }

        /// <summary>
        /// Получает минимальное RTT для процесса в миллисекундах
        /// </summary>
        public static long GetMinRttMs(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return 0;
            string process = processName.Replace(".exe", "").ToLower();
            long rtt;
            return _minRttMs.TryGetValue(process, out rtt) ? rtt : 0;
        }

        /// <summary>
        /// Получает максимальное RTT для процесса в миллисекундах
        /// </summary>
        public static long GetMaxRttMs(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return 0;
            string process = processName.Replace(".exe", "").ToLower();
            long rtt;
            return _maxRttMs.TryGetValue(process, out rtt) ? rtt : 0;
        }

        /// <summary>
        /// Получает jitter (вариации RTT) для процесса в миллисекундах
        /// </summary>
        public static double GetJitterMs(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return 0.0;
            string process = processName.Replace(".exe", "").ToLower();
            double jitter;
            return _jitterMs.TryGetValue(process, out jitter) ? jitter : 0.0;
        }

        /// <summary>
        /// Phase 3: Измеряет RTT для TCP пакетов (БЕЗ ФИЛЬТРОВ - все данные как есть)
        /// </summary>
        private static void MeasureTcpRtt(string processName, string srcIP, int srcPort, string dstIP, int dstPort, DateTime timestamp, bool isOutgoing)
        {
            if (string.IsNullOrEmpty(processName)) return;
            
            try
            {
                // Создаем более специфичный ключ для отслеживания пары пакетов
                string connectionKey = $"{processName}:{srcIP}:{dstIP}";
                string key = $"{connectionKey}:{srcPort}:{dstPort}";
                string reverseKey = $"{connectionKey}:{dstPort}:{srcPort}";
                
                if (isOutgoing)
                {
                    // Записываем время отправки исходящего пакета
                    _tcpSentPackets[key] = timestamp;
                    
                    // Очищаем старые записи (старше 2 секунд)
                    CleanupOldTcpPackets(timestamp);
                }
                else
                {
                    // Ищем соответствующий исходящий пакет для вычисления RTT
                    DateTime sentTime;
                    if (_tcpSentPackets.TryGetValue(reverseKey, out sentTime))
                    {
                        var rtt = timestamp - sentTime;
                        long rttMs = (long)rtt.TotalMilliseconds;
                        
                        // Фильтруем localhost и другие локальные соединения
                        bool isLocalhost = srcIP == "127.0.0.1" || dstIP == "127.0.0.1" || 
                                          srcIP == "::1" || dstIP == "::1";
                        
                        // Фильтруем только для активного игрового процесса и разумные RTT значения
                        if (rttMs > 0 && rttMs <= 500 && IsActiveGameProcess(processName) && !isLocalhost)
                        {
                            AddRttData(processName, rttMs);
                            
                            // Удаляем использованную запись
                            _tcpSentPackets.TryRemove(reverseKey, out _);
                            
                            DebugLogger.log($"[ETW-RTT-GAME] Measured RTT for active game {processName}: {rttMs}ms (connection: {srcIP}:{srcPort} -> {dstIP}:{dstPort})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETW-RTT] Error measuring TCP RTT: {ex.Message}");
            }
        }

        /// <summary>
        /// ИСПРАВЛЕНИЕ: Проверяет является ли процесс игровым
        /// </summary>
        private static bool IsGameProcess(string processName)
        {
            if (string.IsNullOrEmpty(processName)) return false;
            
            string process = processName.ToLowerInvariant();
            
            // Список известных игровых процессов
            string[] gameProcesses = {
                "overwatch", "csgo", "cs2", "valorant", "apex", "dota2", "lol", 
                "fortnite", "warzone", "battlefield", "pubg", "destiny2", "siege",
                "minecraft", "wow", "diablo", "hearthstone", "starcraft", "hots"
            };
            
            return gameProcesses.Any(game => process.Contains(game));
        }

        /// <summary>
        /// ИСПРАВЛЕНИЕ: Проверяет является ли порт игровым (избегаем HTTP/HTTPS/браузеры)
        /// </summary>
        private static bool IsGamePort(int dstPort, int srcPort)
        {
            // Исключаем стандартные веб порты
            int[] webPorts = { 80, 443, 8080, 8443, 3000, 8000 };
            if (webPorts.Contains(dstPort) || webPorts.Contains(srcPort)) return false;
            
            // Исключаем системные порты
            if (dstPort < 1024 && dstPort != 80 && dstPort != 443) return false;
            
            // Включаем типичные игровые порты
            int[] gamePorts = { 3724, 1119, 6113, 7777, 7778, 27015, 27016 };
            if (gamePorts.Contains(dstPort) || gamePorts.Contains(srcPort)) return true;
            
            // Включаем диапазоны игровых портов
            return (dstPort >= 3000 && dstPort <= 65000) || (srcPort >= 3000 && srcPort <= 65000);
        }

        /// <summary>
        /// Очищает старые TCP пакеты для экономии памяти (УСКОРЕННАЯ ОЧИСТКА)
        /// </summary>
        private static void CleanupOldTcpPackets(DateTime currentTime)
        {
            var cutoff = currentTime.AddSeconds(-2); // Очищаем пакеты старше 2 секунд (вместо 5)
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _tcpSentPackets)
            {
                if (kvp.Value < cutoff)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _tcpSentPackets.TryRemove(key, out _);
            }
            
            if (keysToRemove.Count > 0)
            {
                DebugLogger.log($"[ETW-RTT] Cleaned {keysToRemove.Count} old TCP packet entries");
            }
        }
        private static void CleanupOldRttData()
        {
            var now = DateTime.Now;
            var cutoff = now.AddMinutes(-5); // Очищаем данные старше 5 минут
            
            var keysToRemove = new List<string>();
            
            foreach (var kvp in _lastRttUpdate)
            {
                if (kvp.Value < cutoff)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
            
            foreach (var key in keysToRemove)
            {
                _rttHistory.TryRemove(key, out _);
                _avgRttMs.TryRemove(key, out _);
                _minRttMs.TryRemove(key, out _);
                _maxRttMs.TryRemove(key, out _);
                _jitterMs.TryRemove(key, out _);
                _lastRttUpdate.TryRemove(key, out _);
            }
            
            if (keysToRemove.Count > 0)
            {
                DebugLogger.log($"[ETW-RTT] Cleaned up {keysToRemove.Count} old RTT entries");
            }
        }
    }
}
