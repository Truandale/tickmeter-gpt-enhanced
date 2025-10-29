using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using PcapDotNet.Core;

namespace tickMeter.Classes
{
    public static class TunDetector
    {
        public static bool IsTunLike(LivePacketDevice d, string[] hints)
        {
            var s = (((d.Description ?? string.Empty) + " " + (d.Name ?? string.Empty))).ToLowerInvariant();
            foreach (var h in hints)
                if (!string.IsNullOrWhiteSpace(h) && s.Contains(h.Trim().ToLowerInvariant()))
                    return true;
            return false;
        }
    }

    /// <summary>
    /// Быстрый трекер соединений: (proto, local(ip,port), remote(ip,port)) -> { pid, exe }
    /// Источник: IP Helper (v4+v6). Период обновления ~300 мс. ETW можно добавить позднее.
    /// </summary>
    public sealed class ConnectionTracker : IDisposable
    {
        public readonly struct Key : IEquatable<Key>
        {
            public readonly byte Proto; // 6=TCP, 17=UDP
            public readonly IPAddress Local;
            public readonly int LocalPort;
            public readonly IPAddress Remote;
            public readonly int RemotePort;
            
            public Key(byte proto, IPAddress l, int lp, IPAddress r, int rp)
            { Proto = proto; Local = l; LocalPort = lp; Remote = r; RemotePort = rp; }
            
            public bool Equals(Key o) =>
                Proto == o.Proto && Local.Equals(o.Local) && LocalPort == o.LocalPort &&
                Remote.Equals(o.Remote) && RemotePort == o.RemotePort;
            
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + Proto.GetHashCode();
                    hash = hash * 23 + (Local?.GetHashCode() ?? 0);
                    hash = hash * 23 + LocalPort.GetHashCode();
                    hash = hash * 23 + (Remote?.GetHashCode() ?? 0);
                    hash = hash * 23 + RemotePort.GetHashCode();
                    return hash;
                }
            }
            public override bool Equals(object obj) => obj is Key other && Equals(other);
        }
        
        public readonly struct Info
        {
            public readonly int Pid;
            public readonly string Exe;
            public Info(int pid, string exe) { Pid = pid; Exe = exe; }
        }
        
    public event Action<Key, Info> OnNewTunnelConnection;

    private readonly ConcurrentDictionary<Key, (Info info, long ts)> _map = new ConcurrentDictionary<Key, (Info info, long ts)>();
    private readonly ConcurrentDictionary<(byte proto, IPAddress local, int lport), int> _udpOwner = new ConcurrentDictionary<(byte proto, IPAddress local, int lport), int>(); // UDP без remote
    private readonly HashSet<Key> _reportedKeys = new HashSet<Key>();
    private int _udpOwnerLogCount;
    private int _lookupLogCount;
    private int _lastDumpTick;
    private readonly Thread _thread;
    private volatile bool _stop;
    private readonly int _ttlMs = 3000; // срок жизни записи
    private volatile int _eventInvokeCount = 0; // Счетчик вызовов событий для защиты от переполнения

        public ConnectionTracker()
        {
            _thread = new Thread(Loop) { IsBackground = true, Name = "ConnectionTracker" };
            _thread.Start();
        }
        
        public void Dispose() 
        { 
            _stop = true; 
            try { _thread.Join(1000); } catch { } 
        }

        public bool TryResolve(byte proto, IPAddress local, int lport, IPAddress remote, int rport, out Info info)
        {
            var now = Environment.TickCount;
            if (TryGetFiveTuple(proto, local, lport, remote, rport, now, out info))
            {
                return true;
            }

            if (proto == 17)
            {
                if (TryResolveUdpOwner((proto, local, lport), out info))
                {
                    LogLookup("HIT udpOwner", proto, local, lport, remote, rport,
                        extra: $"owner={(info.Exe ?? string.Empty)}/{info.Pid}");
                    return true;
                }

                if (TryResolveUdpOwner((proto, remote, rport), out info))
                {
                    LogLookup("HIT udpOwner swapped", proto, local, lport, remote, rport,
                        extra: $"owner={(info.Exe ?? string.Empty)}/{info.Pid}");
                    return true;
                }
            }

            info = default;
            LogLookup("MISS", proto, local, lport, remote, rport);
            return false;
        }
        
        public Info? QueryLocalOwner(byte proto, IPAddress local, int lport)
        {
            try
            {
                var now = Environment.TickCount;

                foreach (var kv in _map)
                {
                    if (kv.Key.Proto == proto && kv.Key.LocalPort == lport && kv.Key.Local.Equals(local))
                    {
                        if (now - kv.Value.ts <= _ttlMs)
                        {
                            return kv.Value.info;
                        }
                    }
                }

                if (proto == 17 && TryResolveUdpOwner((proto, local, lport), out var udpInfo))
                {
                    return udpInfo;
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }

        private void Loop()
        {
            var sw = Stopwatch.StartNew();
            while (!_stop)
            {
                try
                {
                    RefreshTcp(AF_INET);
                    RefreshTcp(AF_INET6);
                    RefreshUdp(AF_INET);
                    RefreshUdp(AF_INET6);
                    EvictExpired();
                    DumpProcessSnapshotIfNeeded();
                }
                catch { /* ignore all */ }
                var due = 300 - (int)sw.ElapsedMilliseconds;
                if (due < 30) due = 30;
                Thread.Sleep(due);
                sw.Restart();
            }
        }

        private void EvictExpired()
        {
            var now = Environment.TickCount;
            foreach (var kv in _map)
            {
                if (now - kv.Value.ts > _ttlMs)
                {
                    _map.TryRemove(kv.Key, out _);
                    _reportedKeys.Remove(kv.Key);
                }
            }

            var expired = new List<Key>();
            foreach (var key in _reportedKeys)
            {
                if (!_map.ContainsKey(key))
                {
                    expired.Add(key);
                }
            }

            foreach (var key in expired)
            {
                _reportedKeys.Remove(key);
            }
        }

        // ---------- IP Helper ----------
        private const int AF_INET = 2, AF_INET6 = 23;
        private enum TCP_TABLE_CLASS : int { TCP_TABLE_OWNER_PID_ALL = 5 }
        private enum UDP_TABLE_CLASS : int { UDP_TABLE_OWNER_PID = 1 }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, TCP_TABLE_CLASS tblClass, int reserved);
        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, bool sort, int ipVersion, UDP_TABLE_CLASS tblClass, int reserved);

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint state, localAddr, localPort_be, remoteAddr, remotePort_be, owningPid;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
            // followed by MIB_TCPROW_OWNER_PID[dwNumEntries]
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPROW_OWNER_PID
        {
            public uint localAddr, localPort_be, owningPid;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
            // followed by MIB_UDPROW_OWNER_PID[dwNumEntries]
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCP6ROW_OWNER_PID
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] localAddr;
            public uint localScopeId;
            public uint localPort_be;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] remoteAddr;
            public uint remoteScopeId;
            public uint remotePort_be;
            public uint state;
            public uint owningPid;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCP6TABLE_OWNER_PID
        {
            public uint dwNumEntries;
            // followed by MIB_TCP6ROW_OWNER_PID[dwNumEntries]
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDP6ROW_OWNER_PID
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] localAddr;
            public uint localScopeId;
            public uint localPort_be;
            public uint owningPid;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDP6TABLE_OWNER_PID
        {
            public uint dwNumEntries;
            // followed by MIB_UDP6ROW_OWNER_PID[dwNumEntries]
        }

        private void RefreshTcp(int af)
        {
            try
            {
                // Проверяем, что объект не disposed и коллекции инициализированы
                if (_map == null || _reportedKeys == null || _stop)
                {
                    return;
                }

                int len = 0;
                uint ret = GetExtendedTcpTable(IntPtr.Zero, ref len, true, af, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != 0x7A) return; // ERROR_INSUFFICIENT_BUFFER
                var buf = Marshal.AllocHGlobal(len);
            try
            {
                ret = GetExtendedTcpTable(buf, ref len, true, af, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != 0) return;
                var now = Environment.TickCount;
                if (af == AF_INET)
                {
                    int count = (int)Marshal.ReadInt32(buf);
                    IntPtr p = buf + 4;
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(p);
                            p += Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();
                            var l = ToIPv4(row.localAddr);
                            var r = ToIPv4(row.remoteAddr);
                            
                            // Дополнительная проверка на null
                            if (l == null || r == null)
                            {
                                DebugLogger.log($"[Tracker] Warning: ToIPv4 returned null - localAddr={row.localAddr}, remoteAddr={row.remoteAddr}");
                                continue;
                            }
                            
                            int lp = ReadPort(row.localPort_be);
                            int rp = ReadPort(row.remotePort_be);
                            var info = new Info((int)row.owningPid, TryGetExe((int)row.owningPid));
                            var key = new Key(6, l, lp, r, rp);
                            _map[key] = (info, now);

                            if (IsTunnelIP(l) && _reportedKeys.Add(key))
                            {
                                if (OnNewTunnelConnection != null)
                                {
                                    try
                                    {
                                        string logMessage = $"[Tracker] NewTunnel IPv4: {l}:{lp} -> {r}:{rp} proc={info.Exe ?? "?"}/{info.Pid}";
                                        DebugLogger.log(logMessage);
                                        
                                        // Дополнительная диагностика подписчиков (с защитой от null)
                                        try 
                                        {
                                            var eventCopy = OnNewTunnelConnection; // Копируем ссылку для thread-safety
                                            if (eventCopy != null)
                                            {
                                                var invocationList = eventCopy.GetInvocationList();
                                                DebugLogger.log($"[Tracker] Event has {invocationList?.Length ?? 0} subscribers");
                                            }
                                        }
                                        catch (Exception diagEx)
                                        {
                                            DebugLogger.log($"[Tracker] Error getting invocation list: {diagEx.Message}");
                                        }
                                        
                                        // Дополнительная проверка параметров перед вызовом события
                                        if (key.Local != null && key.Remote != null && OnNewTunnelConnection != null)
                                        {
                                            // Защита от переполнения вызовов событий
                                            if (System.Threading.Interlocked.Increment(ref _eventInvokeCount) > 1000)
                                            {
                                                DebugLogger.log($"[Tracker] Warning: Event invoke limit reached, resetting counter");
                                                System.Threading.Interlocked.Exchange(ref _eventInvokeCount, 0);
                                            }
                                            
                                            try 
                                            {
                                                OnNewTunnelConnection.Invoke(key, info);
                                            }
                                            catch (Exception invokeEx)
                                            {
                                                DebugLogger.log($"[Tracker] Error invoking OnNewTunnelConnection: {invokeEx.Message}");
                                            }
                                        }
                                        else
                                        {
                                            DebugLogger.log($"[Tracker] Warning: Skipping event invoke due to null parameters - Local={key.Local != null}, Remote={key.Remote != null}, Event={OnNewTunnelConnection != null}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugLogger.log($"[Tracker] Error in OnNewTunnelConnection handler: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    DebugLogger.log($"[Tracker] NewTunnel IPv4 NO subscribers: {l}:{lp} -> {r}:{rp}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.log($"[Tracker] Error processing TCP row {i}: {ex.Message}");
                            // Сдвигаем указатель даже при ошибке
                            p += Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();
                        }
                    }
                }
                else
                {
                    int count = (int)Marshal.ReadInt32(buf);
                    IntPtr p = buf + 4;
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            var row = Marshal.PtrToStructure<MIB_TCP6ROW_OWNER_PID>(p);
                            p += Marshal.SizeOf<MIB_TCP6ROW_OWNER_PID>();
                            var l = new IPAddress(row.localAddr, (long)row.localScopeId);
                            var r = new IPAddress(row.remoteAddr, (long)row.remoteScopeId);
                            
                            // Дополнительная проверка на null
                            if (l == null || r == null)
                            {
                                DebugLogger.log($"[Tracker] Warning: IPv6 address creation failed for row {i}");
                                continue;
                            }
                            
                            int lp = ReadPort(row.localPort_be);
                            int rp = ReadPort(row.remotePort_be);
                            var info = new Info((int)row.owningPid, TryGetExe((int)row.owningPid));
                            var key = new Key(6, l, lp, r, rp);
                            _map[key] = (info, now);

                            if (IsTunnelIP(l) && _reportedKeys.Add(key))
                            {
                                if (OnNewTunnelConnection != null)
                                {
                                    try
                                    {
                                        string logMessage = $"[Tracker] NewTunnel IPv6: {l}:{lp} -> {r}:{rp} proc={info.Exe ?? "?"}/{info.Pid}";
                                        DebugLogger.log(logMessage);
                                        
                                        // Дополнительная проверка параметров перед вызовом события
                                        if (key.Local != null && key.Remote != null)
                                        {
                                            // Защита от переполнения вызовов событий
                                            if (System.Threading.Interlocked.Increment(ref _eventInvokeCount) > 1000)
                                            {
                                                DebugLogger.log($"[Tracker] Warning: IPv6 Event invoke limit reached, resetting counter");
                                                System.Threading.Interlocked.Exchange(ref _eventInvokeCount, 0);
                                            }
                                            OnNewTunnelConnection.Invoke(key, info);
                                        }
                                        else
                                        {
                                            DebugLogger.log($"[Tracker] Warning: Skipping IPv6 event invoke due to null key fields");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugLogger.log($"[Tracker] Error in OnNewTunnelConnection handler (IPv6): {ex.Message}");
                                    }
                                }
                                else
                                {
                                    DebugLogger.log($"[Tracker] NewTunnel IPv6 NO subscribers: {l}:{lp} -> {r}:{rp}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.log($"[Tracker] Error processing TCP6 row {i}: {ex.Message}");
                            // Сдвигаем указатель даже при ошибке
                            p += Marshal.SizeOf<MIB_TCP6ROW_OWNER_PID>();
                        }
                    }
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
            }
            catch (Exception ex)
            {
                // Логируем глобальные ошибки в RefreshTcp
                try
                {
                    DebugLogger.log($"[Tracker] Critical error in RefreshTcp(af={af}): {ex.Message}");
                }
                catch
                {
                    // Если даже логирование не работает - не падаем
                }
            }
        }

        private void RefreshUdp(int af)
        {
            int len = 0;
            uint ret = GetExtendedUdpTable(IntPtr.Zero, ref len, true, af, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
            if (ret != 0x7A) return;
            var buf = Marshal.AllocHGlobal(len);
            try
            {
                ret = GetExtendedUdpTable(buf, ref len, true, af, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
                if (ret != 0) return;
                if (af == AF_INET)
                {
                    int count = (int)Marshal.ReadInt32(buf);
                    IntPtr p = buf + 4;
                    for (int i = 0; i < count; i++)
                    {
                        var row = Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(p);
                        p += Marshal.SizeOf<MIB_UDPROW_OWNER_PID>();
                        var l = ToIPv4(row.localAddr);
                        int lp = ReadPort(row.localPort_be);
                        var pid = (int)row.owningPid;
                        _udpOwner[(17, l, lp)] = pid;
                        LogUdpOwnerSnapshot(17, l, lp, pid);
                    }
                }
                else
                {
                    int count = (int)Marshal.ReadInt32(buf);
                    IntPtr p = buf + 4;
                    for (int i = 0; i < count; i++)
                    {
                        var row = Marshal.PtrToStructure<MIB_UDP6ROW_OWNER_PID>(p);
                        p += Marshal.SizeOf<MIB_UDP6ROW_OWNER_PID>();
                        var l = new IPAddress(row.localAddr, (long)row.localScopeId);
                        int lp = ReadPort(row.localPort_be);
                        var pid = (int)row.owningPid;
                        _udpOwner[(17, l, lp)] = pid;
                        LogUdpOwnerSnapshot(17, l, lp, pid);
                    }
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
        }

        private bool TryGetFiveTuple(byte proto, IPAddress local, int lport, IPAddress remote, int rport, int now, out Info info)
        {
            if (_map.TryGetValue(new Key(proto, local, lport, remote, rport), out var direct) && now - direct.ts <= _ttlMs)
            {
                info = direct.info;
                LogLookup("HIT fiveTuple direct", proto, local, lport, remote, rport,
                    extra: $"owner={(info.Exe ?? string.Empty)}/{info.Pid}");
                return true;
            }

            if (_map.TryGetValue(new Key(proto, remote, rport, local, lport), out var reverse) && now - reverse.ts <= _ttlMs)
            {
                info = reverse.info;
                LogLookup("HIT fiveTuple reverse", proto, local, lport, remote, rport,
                    extra: $"owner={(info.Exe ?? string.Empty)}/{info.Pid}");
                return true;
            }

            info = default;
            return false;
        }

        private bool TryResolveUdpOwner((byte proto, IPAddress ip, int port) candidate, out Info info)
        {
            info = default;

            if (candidate.port <= 0)
                return false;

            if (_udpOwner.TryGetValue(candidate, out var pid))
            {
                info = new Info(pid, TryGetExe(pid));
                return true;
            }

            IPAddress wildcard = null;
            if (candidate.ip != null)
            {
                if (candidate.ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    wildcard = IPAddress.Any;
                }
                else if (candidate.ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    wildcard = IPAddress.IPv6Any;
                }
            }

            if (wildcard != null && _udpOwner.TryGetValue((candidate.proto, wildcard, candidate.port), out pid))
            {
                info = new Info(pid, TryGetExe(pid));
                return true;
            }

            return false;
        }

        private void LogUdpOwnerSnapshot(byte proto, IPAddress local, int port, int pid)
        {
            if (pid <= 0 || port <= 0)
                return;

            var index = Interlocked.Increment(ref _udpOwnerLogCount);
            if (index > 200)
                return;

            var exe = TryGetExe(pid);
            DebugLogger.log($"[Tracker] UdpOwner map proto={proto} local={local}:{port} pid={pid} exe={exe}");
        }

        private void LogLookup(string stage, byte proto, IPAddress local, int lport, IPAddress remote, int rport, string extra = null)
        {
            var index = Interlocked.Increment(ref _lookupLogCount);
            if (index > 300)
                return;

            if (stage == "MISS")
            {
                if (local == null || remote == null ||
                    local.Equals(IPAddress.Any) || local.Equals(IPAddress.IPv6Any) ||
                    remote.Equals(IPAddress.Any) || remote.Equals(IPAddress.IPv6Any))
                {
                    return;
                }

                if (IPAddress.IsLoopback(local) && IPAddress.IsLoopback(remote))
                    return;

                if (rport == 0)
                    return;
            }

            var localIp = local?.ToString() ?? "<null>";
            var remoteIp = remote?.ToString() ?? "<null>";
            var sb = new StringBuilder();
            sb.Append("[Tracker] Resolve ")
              .Append(stage)
              .Append(" proto=").Append(proto)
              .Append(" local=").Append(localIp).Append(':').Append(lport)
              .Append(" remote=").Append(remoteIp).Append(':').Append(rport);

            if (!string.IsNullOrWhiteSpace(extra))
            {
                sb.Append(' ').Append(extra);
            }

            DebugLogger.log(sb.ToString());
        }

        private void DumpProcessSnapshotIfNeeded()
        {
            var now = Environment.TickCount;
            if (_lastDumpTick != 0 && unchecked(now - _lastDumpTick) < 2000)
                return;

            _lastDumpTick = now;

            var snapshot = new Dictionary<int, string>();
            var details = new Dictionary<int, SnapshotDetails>();

            foreach (var entry in _map)
            {
                var info = entry.Value.info;
                if (info.Pid <= 0)
                    continue;

                var name = info.Exe;
                if (string.IsNullOrWhiteSpace(name))
                    name = TryGetExe(info.Pid);

                if (snapshot.TryGetValue(info.Pid, out var existingName))
                {
                    if (string.IsNullOrWhiteSpace(existingName) && !string.IsNullOrWhiteSpace(name))
                        snapshot[info.Pid] = name;
                }
                else
                {
                    snapshot[info.Pid] = name;
                }

                var key = entry.Key;
                var detail = GetOrCreateDetail(details, info.Pid, name);
                detail.ConnectionCount++;
                if (detail.Samples.Count < 3)
                {
                    detail.Samples.Add($"{key.Local}:{key.LocalPort}->{key.Remote}:{key.RemotePort}");
                }
            }

            foreach (var entry in _udpOwner)
            {
                var pid = entry.Value;
                if (pid <= 0)
                    continue;

                var name = TryGetExe(pid);

                if (snapshot.TryGetValue(pid, out var existingName))
                {
                    if (string.IsNullOrWhiteSpace(existingName) && !string.IsNullOrWhiteSpace(name))
                        snapshot[pid] = name;
                }
                else
                {
                    snapshot[pid] = name;
                }

                var detail = GetOrCreateDetail(details, pid, name);
                detail.ConnectionCount++;
                if (detail.UdpEndpoints.Count < 3)
                {
                    var key = entry.Key;
                    detail.UdpEndpoints.Add($"{key.local}:{key.lport}");
                }
            }

            if (snapshot.Count == 0)
                return;

            var builder = new StringBuilder();
            builder.Append("[Tracker] Active processes: ");

            var first = true;
            foreach (var kv in snapshot.OrderBy(p => p.Key))
            {
                if (!first)
                    builder.Append("; ");
                builder.Append(kv.Key);
                if (!string.IsNullOrWhiteSpace(kv.Value))
                    builder.Append("=").Append(kv.Value);
                first = false;
            }

            DebugLogger.log(builder.ToString());

            foreach (var kv in details)
            {
                var detail = kv.Value;
                if (!string.Equals(detail.Name, "browser", StringComparison.OrdinalIgnoreCase))
                    continue;

                var detailBuilder = new StringBuilder();
                detailBuilder.Append($"[Tracker] Detail PID={kv.Key} Name={detail.Name ?? "<unknown>"} conn={detail.ConnectionCount}");

                if (detail.Samples.Count > 0)
                {
                    detailBuilder.Append(" samples=").Append(string.Join(", ", detail.Samples));
                }

                if (detail.UdpEndpoints.Count > 0)
                {
                    detailBuilder.Append(" udpLocal=").Append(string.Join(", ", detail.UdpEndpoints));
                }

                DebugLogger.log(detailBuilder.ToString());
            }
        }

        private static SnapshotDetails GetOrCreateDetail(Dictionary<int, SnapshotDetails> map, int pid, string name)
        {
            if (!map.TryGetValue(pid, out var detail))
            {
                detail = new SnapshotDetails();
                map[pid] = detail;
            }

            if (!string.IsNullOrWhiteSpace(name))
                detail.Name = name;

            return detail;
        }

        private sealed class SnapshotDetails
        {
            public string Name;
            public int ConnectionCount;
            public List<string> Samples { get; } = new List<string>();
            public List<string> UdpEndpoints { get; } = new List<string>();
        }

        private static bool IsTunnelIP(IPAddress ip)
        {
            if (ip == null || ip.AddressFamily != AddressFamily.InterNetwork)
                return false;

            var bytes = ip.GetAddressBytes();

            if (bytes[0] == 10)
                return true;

            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            return false;
        }

        private static IPAddress ToIPv4(uint raw)
        {
            if (raw == 0)
            {
                return IPAddress.Any;
            }

            // IP Helper already exposes IPv4 values in network byte order.
            return new IPAddress(raw);
        }

        private static int ReadPort(uint raw)
        {
            // Lower 16 bits contain the port in network byte order, the rest is padding.
            return (int)SwapUshort((ushort)raw);
        }

        private static ushort SwapUshort(ushort x) => (ushort)((x >> 8) | (x << 8));

        // Путь к exe — best effort (без падений)
        private static string TryGetExe(int pid)
        {
            try
            {
                using (var p = Process.GetProcessById(pid))
                {
                    return p.ProcessName;
                }
            }
            catch { return string.Empty; }
        }
    }
}