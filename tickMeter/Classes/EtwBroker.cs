using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Kernel;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace tickMeter.Classes
{
    public static class EtwBroker
    {
        private sealed class ConnectionEntry
        {
            public ConnectionEntry(byte protocol, IPAddress local, int localPort, IPAddress remote, int remotePort, int processId, string processName, DateTime timestampUtc)
            {
                Protocol = protocol;
                LocalAddress = local;
                LocalPort = localPort;
                RemoteAddress = remote;
                RemotePort = remotePort;
                ProcessId = processId;
                ProcessName = processName;
                TimestampUtc = timestampUtc;
            }

            public byte Protocol { get; }
            public IPAddress LocalAddress { get; }
            public int LocalPort { get; }
            public IPAddress RemoteAddress { get; }
            public int RemotePort { get; }
            public int ProcessId { get; }
            public string ProcessName { get; }
            public DateTime TimestampUtc { get; }

            public bool IsExpired(DateTime now, TimeSpan ttl) => now - TimestampUtc > ttl;

            public RemapResult ToRemapResult() => new RemapResult(Protocol, LocalAddress, LocalPort, RemoteAddress, RemotePort, ProcessId, ProcessName, TimestampUtc);
        }

        private readonly struct ConnectionKey : IEquatable<ConnectionKey>
        {
            public readonly byte Protocol;
            public readonly IPAddress Local;
            public readonly int LocalPort;
            public readonly IPAddress Remote;
            public readonly int RemotePort;

            public ConnectionKey(byte protocol, IPAddress local, int localPort, IPAddress remote, int remotePort)
            {
                Protocol = protocol;
                Local = local;
                LocalPort = localPort;
                Remote = remote;
                RemotePort = remotePort;
            }

            public bool Equals(ConnectionKey other)
            {
                return Protocol == other.Protocol &&
                       Equals(Local, other.Local) &&
                       LocalPort == other.LocalPort &&
                       Equals(Remote, other.Remote) &&
                       RemotePort == other.RemotePort;
            }

            public override bool Equals(object obj) => obj is ConnectionKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = Protocol.GetHashCode();
                    hash = (hash * 397) ^ (Local?.GetHashCode() ?? 0);
                    hash = (hash * 397) ^ LocalPort;
                    hash = (hash * 397) ^ (Remote?.GetHashCode() ?? 0);
                    hash = (hash * 397) ^ RemotePort;
                    return hash;
                }
            }
        }

        private readonly struct LocalKey : IEquatable<LocalKey>
        {
            public readonly byte Protocol;
            public readonly IPAddress Address;
            public readonly int Port;

            public LocalKey(byte protocol, IPAddress address, int port)
            {
                Protocol = protocol;
                Address = address;
                Port = port;
            }

            public bool Equals(LocalKey other)
            {
                return Protocol == other.Protocol &&
                       Equals(Address, other.Address) &&
                       Port == other.Port;
            }

            public override bool Equals(object obj) => obj is LocalKey other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = Protocol.GetHashCode();
                    hash = (hash * 397) ^ (Address?.GetHashCode() ?? 0);
                    hash = (hash * 397) ^ Port;
                    return hash;
                }
            }
        }

        public readonly struct RemapResult
        {
            public RemapResult(byte protocol, IPAddress local, int localPort, IPAddress remote, int remotePort, int processId, string processName, DateTime timestampUtc)
            {
                Protocol = protocol;
                LocalAddress = local;
                LocalPort = localPort;
                RemoteAddress = remote;
                RemotePort = remotePort;
                ProcessId = processId;
                ProcessName = processName;
                TimestampUtc = timestampUtc;
            }

            public byte Protocol { get; }
            public IPAddress LocalAddress { get; }
            public int LocalPort { get; }
            public IPAddress RemoteAddress { get; }
            public int RemotePort { get; }
            public int ProcessId { get; }
            public string ProcessName { get; }
            public DateTime TimestampUtc { get; }

            public TimeSpan Age => DateTime.UtcNow - TimestampUtc;
            public TimeSpan SuggestedTtl => TimeSpan.FromSeconds(30);
            public string RemoteString => RemoteAddress?.ToString() ?? string.Empty;
            public string SourceTag => "etw";
        }

        private static int _started;

        private static readonly ConcurrentDictionary<ConnectionKey, ConnectionEntry> _entries =
            new ConcurrentDictionary<ConnectionKey, ConnectionEntry>();

        private static readonly ConcurrentDictionary<LocalKey, ConnectionEntry> _localIndex =
            new ConcurrentDictionary<LocalKey, ConnectionEntry>();

        private static CancellationTokenSource _cts;
        private static Task _runner;
        private static int _eventLogCount;
        private static int _cleanupCounter;

        private static readonly TimeSpan EntryTtl = TimeSpan.FromSeconds(30);

        public static bool IsRunning => _started == 1;

        public static void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) == 1)
                return;

            try
            {
                bool elevated = TraceEventSession.IsElevated() ?? false;
                if (!elevated)
                {
                    DebugLogger.log("[EtwBroker] Kernel session requires administrative privileges.");
                    Interlocked.Exchange(ref _started, 0);
                    return;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[EtwBroker] Unable to verify elevation: {ex.GetType().Name} {ex.Message}");
                Interlocked.Exchange(ref _started, 0);
                return;
            }

            _cts = new CancellationTokenSource();
            _runner = Task.Factory.StartNew(() => Pump(_cts.Token),
                                            _cts.Token,
                                            TaskCreationOptions.LongRunning,
                                            TaskScheduler.Default);
        }

        public static void Stop()
        {
            var cts = Interlocked.Exchange(ref _cts, null);
            if (cts != null)
            {
                try { cts.Cancel(); } catch { }
            }
        }

        public static bool TryRemap(byte proto, string sourceIp, int sourcePort, string destIp, int destPort, out RemapResult result)
        {
            result = default;

            if (proto == 0)
                return false;

            if (!IPAddress.TryParse(sourceIp, out var src) ||
                !IPAddress.TryParse(destIp, out var dst))
            {
                return false;
            }

            var now = DateTime.UtcNow;

            if (TryGet(new ConnectionKey(proto, src, sourcePort, dst, destPort), now, out var entry) ||
                TryGet(new ConnectionKey(proto, dst, destPort, src, sourcePort), now, out entry) ||
                TryGet(new LocalKey(proto, src, sourcePort), now, out entry) ||
                TryGet(new LocalKey(proto, dst, destPort), now, out entry))
            {
                result = entry.ToRemapResult();
                return true;
            }

            return false;
        }

        private static bool TryGet(ConnectionKey key, DateTime now, out ConnectionEntry entry)
        {
            if (_entries.TryGetValue(key, out entry))
            {
                if (!entry.IsExpired(now, EntryTtl))
                    return true;

                _entries.TryRemove(key, out _);
            }

            return false;
        }

        private static bool TryGet(LocalKey key, DateTime now, out ConnectionEntry entry)
        {
            if (_localIndex.TryGetValue(key, out entry))
            {
                if (!entry.IsExpired(now, EntryTtl))
                    return true;

                _localIndex.TryRemove(key, out _);
            }

            return false;
        }

        private static void Pump(CancellationToken token)
        {
            try
            {
                using (var session = new TraceEventSession("tickMeter-etwBroker"))
                {
                    session.StopOnDispose = true;

                    token.Register(() =>
                    {
                        try { session.Stop(); } catch { }
                    });

                    session.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);

                    var source = session.Source;
                    var kernel = source.Kernel;

                    kernel.TcpIpConnect += data => HandleTcp(data, 6);
                    kernel.TcpIpAccept += data => HandleTcp(data, 6);
                    kernel.TcpIpReconnect += data => HandleTcpTrace(data, 6);
                    kernel.TcpIpSend += data => HandleTcpSend(data);
                    kernel.TcpIpRecv += data => HandleTcpTrace(data, 6);
                    kernel.TcpIpConnectIPV6 += data => HandleTcpV6(data);
                    kernel.TcpIpAcceptIPV6 += data => HandleTcpV6(data);
                    kernel.TcpIpSendIPV6 += data => HandleTcpV6Send(data);
                    kernel.TcpIpRecvIPV6 += data => HandleTcpV6Trace(data);
                    kernel.UdpIpSend += data => HandleUdp(data, 17);
                    kernel.UdpIpRecv += data => HandleUdp(data, 17);

                    source.Process();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[EtwBroker] Session terminated: {ex.GetType().Name} {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _started, 0);
            }
        }

        private static void HandleTcp(TcpIpConnectTraceData data, byte proto) =>
            Register(proto, data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, data.ProcessID, data.ProcessName);

        private static void HandleTcpTrace(TcpIpTraceData data, byte proto) =>
            Register(proto, data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, data.ProcessID, data.ProcessName);

        private static void HandleTcpSend(TcpIpSendTraceData data) =>
            Register(6, data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, data.ProcessID, data.ProcessName);

        private static void HandleTcpV6(TcpIpV6ConnectTraceData data) =>
            Register(6, data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, data.ProcessID, data.ProcessName);

        private static void HandleTcpV6Send(TcpIpV6SendTraceData data) =>
            Register(6, data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, data.ProcessID, data.ProcessName);

        private static void HandleTcpV6Trace(TcpIpV6TraceData data) =>
            Register(6, data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, data.ProcessID, data.ProcessName);

        private static void HandleUdp(UdpIpTraceData data, byte proto) =>
            Register(proto, data.saddr.ToString(), data.sport, data.daddr.ToString(), data.dport, data.ProcessID, data.ProcessName);

        private static void Register(byte proto, string localAddress, int localPort, string remoteAddress, int remotePort, int processId, string processName)
        {
            if (!IPAddress.TryParse(localAddress, out var local))
                return;

            if (!IPAddress.TryParse(remoteAddress, out var remote))
                return;

            var normalizedName = NormalizeProcessName(processName, processId);
            var entry = new ConnectionEntry(proto, local, localPort, remote, remotePort, processId, normalizedName, DateTime.UtcNow);

            _entries[new ConnectionKey(proto, local, localPort, remote, remotePort)] = entry;
            _localIndex[new LocalKey(proto, local, localPort)] = entry;

            CleanupIfNeeded();

            var logIndex = Interlocked.Increment(ref _eventLogCount);
            if (logIndex <= 100)
            {
                DebugLogger.log($"[EtwBroker] #{logIndex} proto={proto} pid={processId} {local}:{localPort} -> {remote}:{remotePort} name={normalizedName}");
            }
        }

        private static void CleanupIfNeeded()
        {
            var current = Interlocked.Increment(ref _cleanupCounter);
            if (current % 512 != 0)
                return;

            var now = DateTime.UtcNow;

            foreach (var kv in _entries)
            {
                if (kv.Value == null || kv.Value.IsExpired(now, EntryTtl))
                    _entries.TryRemove(kv.Key, out _);
            }

            foreach (var kv in _localIndex)
            {
                if (kv.Value == null || kv.Value.IsExpired(now, EntryTtl))
                    _localIndex.TryRemove(kv.Key, out _);
            }
        }

        private static string NormalizeProcessName(string processName, int processId)
        {
            if (!string.IsNullOrWhiteSpace(processName))
                return processName;

            if (processId <= 0)
                return string.Empty;

            try
            {
                using (var process = Process.GetProcessById(processId))
                {
                    return process.ProcessName;
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
