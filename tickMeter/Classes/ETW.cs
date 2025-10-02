using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
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
        public class ProcessNetworkData
        {
            public string pName;
            public int pId;
            public string toIp;
            public string fromIp;
            public uint toPort;
            public uint fromPort;

            private static int _eventLogCount;

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
                    var loggedCount = System.Threading.Interlocked.Increment(ref _eventLogCount);
                    if (loggedCount <= 100)
                    {
                        DebugLogger.log($"[ETW] Event #{loggedCount}: PID={processID} NAME={processName} {saddr}:{sport} -> {daddr}:{dport}");
                    }
                }
            }
        }

        public static Dictionary<string, ProcessNetworkData> processes = new Dictionary<string, ProcessNetworkData>();
        private static readonly object _initLock = new object();
        private static bool _initialized;

        public static bool IsInitialized
        {
            get
            {
                lock (_initLock)
                {
                    return _initialized;
                }
            }
        }

        public static void init()
        {
            lock (_initLock)
            {
                if (_initialized)
                    return;

                _initialized = true;
            }

            DebugLogger.log("[ETW] init() вызван: запускаем поток ядровой трассировки.");

            Thread t = new Thread(ETWSessionThread)
            {
                IsBackground = true,
                Name = "ETWSession"
            };
            t.Start();
        }

        private static readonly int AccessDeniedHresult = unchecked((int)0x80070005);

        private static async void ETWSessionThread()
        {
            try
            {
                bool elevated = TraceEventSession.IsElevated() ?? false;
                if (!elevated)
                {
                    DebugLogger.log("[ETW] Недостаточно прав: запуск сессии Kernel невозможен без запуска tickMeter от имени администратора.");
                    lock (_initLock)
                    {
                        _initialized = false;
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETW] Проверка прав завершилась исключением: {ex.GetType().Name} {ex.Message}");
                lock (_initLock)
                {
                    _initialized = false;
                }
                return;
            }

            try
            {
                DebugLogger.log("[ETW] Запуск ядровой ETW-сессии...");
                await Task.Run(() =>
                {
                    using (var kernelSession = new TraceEventSession(KernelTraceEventParser.KernelSessionName))
                    {
                        kernelSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                        DebugLogger.log("[ETW] Kernel session started: трассировка TCP/UDP активирована.");


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
            catch (Exception ex)
            {
                Debug.Print($"[ETW] Session terminated: {ex.Message}");
                DebugLogger.log($"[ETW] Session terminated: {ex.Message}");
                if (ex.HResult == AccessDeniedHresult)
                {
                    DebugLogger.log("[ETW] Access denied: перезапустите tickMeter от имени администратора или запустите системный сервис-драйвер.");
                }
            }
            finally
            {
                lock (_initLock)
                {
                    _initialized = false;
                }
            }
        }

        private static void ackTCPIP(TcpIpTraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
        }

        private static void recvTCPIPv6(TcpIpV6TraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
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
        }


        private static void acceptTCPIP(TcpIpConnectTraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
        }

        private static void tcpIpTrace(TcpIpTraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
        }

        private static void tcpIpSend(TcpIpSendTraceData session)
        {
            ProcessNetworkData.processEventData(session.ProcessName, session.ProcessID, session.saddr.ToString(), session.daddr.ToString(), session.sport, session.dport);
        }

        public static string resolveProcessname(string fromIp, string toIp, uint fromPort, uint toPort)
        {
            try
            {
                if (processes.Count == 0)
                {
                    var missCount = System.Threading.Interlocked.Increment(ref _resolveEmptyLogCount);
                    if (missCount <= 50)
                    {
                        DebugLogger.log($"[ETW] resolveProcessname: кэш пуст. from={fromIp}:{fromPort} to={toIp}:{toPort}");
                    }
                }
            }
            catch { }
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
                        var resolvedName = EnsureProcessName(procData, fromIp, toIp, fromPort, toPort);
                        var hitCount = System.Threading.Interlocked.Increment(ref _resolveHitLogCount);
                        if (hitCount <= 100)
                        {
                            DebugLogger.log($"[ETW] resolveProcessname HIT #{hitCount}: {resolvedName} PID={procData.pId} from={fromIp}:{fromPort} to={toIp}:{toPort}");
                        }
                        return resolvedName;
                    }
                }
            } catch { }
            var miss = System.Threading.Interlocked.Increment(ref _resolveMissLogCount);
            if (miss <= 100)
            {
                DebugLogger.log($"[ETW] resolveProcessname MISS #{miss}: from={fromIp}:{fromPort} to={toIp}:{toPort} (cache={processes.Count})");
            }
            return @"n\a";
        }

        private static int _resolveHitLogCount;
        private static int _resolveMissLogCount;
        private static int _resolveEmptyLogCount;

        private static string EnsureProcessName(ProcessNetworkData procData, string fromIp, string toIp, uint fromPort, uint toPort)
        {
            if (procData == null)
                return "<unknown>";

            if (!string.IsNullOrWhiteSpace(procData.pName))
                return procData.pName;

            string resolved = TryGetProcessName(procData.pId);

            if (string.IsNullOrWhiteSpace(resolved))
            {
                resolved = ResolveViaConnectionTracker(fromIp, toIp, fromPort, toPort);
            }

            if (string.IsNullOrWhiteSpace(resolved) && procData.pId > 0)
            {
                resolved = procData.pId.ToString();
            }

            if (string.IsNullOrWhiteSpace(resolved))
            {
                resolved = "<unknown>";
            }

            procData.pName = resolved;
            return resolved;
        }

        private static string TryGetProcessName(int pid)
        {
            if (pid <= 0)
                return string.Empty;

            try
            {
                using (var process = Process.GetProcessById(pid))
                {
                    return process.ProcessName;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ResolveViaConnectionTracker(string fromIp, string toIp, uint fromPort, uint toPort)
        {
            if (App.connectionTracker == null)
                return string.Empty;

            if (!IPAddress.TryParse(fromIp, out var src) || !IPAddress.TryParse(toIp, out var dst))
                return string.Empty;

            ConnectionTracker.Info info;

            if (App.connectionTracker.TryResolve(6, src, (int)fromPort, dst, (int)toPort, out info))
                return info.Exe;

            if (App.connectionTracker.TryResolve(17, src, (int)fromPort, dst, (int)toPort, out info))
                return info.Exe;

            if (App.connectionTracker.TryResolve(6, dst, (int)toPort, src, (int)fromPort, out info))
                return info.Exe;

            if (App.connectionTracker.TryResolve(17, dst, (int)toPort, src, (int)fromPort, out info))
                return info.Exe;

            return string.Empty;
        }
    }
}
