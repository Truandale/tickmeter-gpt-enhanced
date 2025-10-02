using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
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

    public static class VpnSettings
    {
        private static bool IsTrue(string key)
        {
            try
            {
                return App.settingsManager?.GetOption(key, "False", "ADVANCED") == "True";
            }
            catch
            {
                return false;
            }
        }

    public static bool AdvancedEnabled => IsTrue("vpn_bypass_advanced");

    public static bool ForceCaptureVirtual => AdvancedEnabled && IsTrue("vpn_capture_virtual");

    public static bool AllowNonEthernet => AdvancedEnabled && IsTrue("vpn_allow_non_ethernet");

    public static bool DisableBpf => AdvancedEnabled && IsTrue("vpn_disable_bpf");

    public static bool EnableEtwEnrichment => AdvancedEnabled && IsTrue("vpn_etw_enrichment");
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
        
        private readonly ConcurrentDictionary<Key, (Info info, long ts)> _map = new ConcurrentDictionary<Key, (Info info, long ts)>();
        private readonly ConcurrentDictionary<(byte proto, IPAddress local, int lport), int> _udpOwner = new ConcurrentDictionary<(byte proto, IPAddress local, int lport), int>(); // UDP без remote
        private readonly Thread _thread;
        private volatile bool _stop;
        private readonly int _ttlMs = 3000; // срок жизни записи

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
            // Полный 5-tuple
            if (_map.TryGetValue(new Key(proto, local, lport, remote, rport), out var x) && now - x.ts <= _ttlMs)
            { info = x.info; return true; }
            if (_map.TryGetValue(new Key(proto, remote, rport, local, lport), out x) && now - x.ts <= _ttlMs)
            { info = x.info; return true; }

            // UDP fallback: по локальной стороне (remote не всегда известен в таблице)
            if (proto == 17 && _udpOwner.TryGetValue((proto, local, lport), out var pidUdp))
            {
                info = new Info(pidUdp, TryGetExe(pidUdp));
                return true;
            }

            info = default;
            return false;
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
                if (now - kv.Value.ts > _ttlMs)
                    _map.TryRemove(kv.Key, out _);
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
                        var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(p);
                        p += Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();
                        var l = new IPAddress(row.localAddr);
                        var r = new IPAddress(row.remoteAddr);
                        int lp = (int)SwapUshort((ushort)(row.localPort_be >> 16));
                        int rp = (int)SwapUshort((ushort)(row.remotePort_be >> 16));
                        var info = new Info((int)row.owningPid, TryGetExe((int)row.owningPid));
                        _map[new Key(6, l, lp, r, rp)] = (info, now);
                    }
                }
                else
                {
                    int count = (int)Marshal.ReadInt32(buf);
                    IntPtr p = buf + 4;
                    for (int i = 0; i < count; i++)
                    {
                        var row = Marshal.PtrToStructure<MIB_TCP6ROW_OWNER_PID>(p);
                        p += Marshal.SizeOf<MIB_TCP6ROW_OWNER_PID>();
                        var l = new IPAddress(row.localAddr, (long)row.localScopeId);
                        var r = new IPAddress(row.remoteAddr, (long)row.remoteScopeId);
                        int lp = (int)SwapUshort((ushort)(row.localPort_be >> 16));
                        int rp = (int)SwapUshort((ushort)(row.remotePort_be >> 16));
                        var info = new Info((int)row.owningPid, TryGetExe((int)row.owningPid));
                        _map[new Key(6, l, lp, r, rp)] = (info, now);
                    }
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
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
                        var l = new IPAddress(row.localAddr);
                        int lp = (int)SwapUshort((ushort)(row.localPort_be >> 16));
                        _udpOwner[(17, l, lp)] = (int)row.owningPid;
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
                        int lp = (int)SwapUshort((ushort)(row.localPort_be >> 16));
                        _udpOwner[(17, l, lp)] = (int)row.owningPid;
                    }
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
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

    public static class VpnBypassHelper
    {
        private static readonly string[] SuspiciousTokens = new[]
        {
            "vpn", "wintun", "wireguard", "tap", "tun", "ksde", "secureline", "openvpn"
        };

        public static string MergeProcessName(string currentName, ConnectionTracker.Info? resolvedInfo)
        {
            if (!resolvedInfo.HasValue)
                return currentName;

            if (!ShouldOverride(currentName))
                return currentName;

            var candidate = resolvedInfo.Value.Exe;
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate;

            if (resolvedInfo.Value.Pid > 0)
                return resolvedInfo.Value.Pid.ToString();

            return currentName;
        }

        public static bool ShouldOverride(string currentName)
        {
            if (string.IsNullOrWhiteSpace(currentName))
                return true;

            var normalized = currentName.Trim();

            if (normalized.Equals(@"n\a", StringComparison.OrdinalIgnoreCase))
                return true;

            if (int.TryParse(normalized, out _))
                return true;

            foreach (var token in SuspiciousTokens)
            {
                if (normalized.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }
    }
}