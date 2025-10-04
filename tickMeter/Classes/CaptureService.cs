using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.IpV6;

namespace tickMeter.Classes
{
    /// <summary>
    /// Глобальный сервис захвата: один воркер на адаптер, рассылает пакеты подписчикам.
    /// Управляет жизненным циклом воркеров по ref-count.
    /// </summary>
    public sealed class CaptureService : IDisposable
    {
        private static string StableKey(LivePacketDevice d)
        {
            // Для Npcap это "\Device\NPF_{GUID}" — стабильный ключ
            var n = d?.Name ?? "";
            // На всякий извлекаем GUID — если формат другой
            var i = n.IndexOf("NPF_{", StringComparison.OrdinalIgnoreCase);
            if (i >= 0) return n.Substring(i); // "NPF_{GUID}..."
            return n;
        }
        private static readonly string[] VpnInterfaceHints =
        {
            "tun", "tap", "wintun", "wireguard", "zerotier", "tailscale", "vpn", "l2tp", "pppoe"
        };
        private sealed class WorkerEntry : IDisposable
        {
            public readonly LivePacketDevice Device;
            private PacketCommunicator _comm;
            private Thread _thread;
            private readonly CancellationTokenSource _cts = new CancellationTokenSource();
            private volatile bool _started;
            private readonly CaptureService _owner;

            public int RefCount => _refCount;
            private int _refCount = 0;

            public WorkerEntry(CaptureService owner, LivePacketDevice device)
            {
                _owner = owner; Device = device ?? throw new ArgumentNullException(nameof(device));
            }

            public void AddRef() => Interlocked.Increment(ref _refCount);
            public int Release() => Interlocked.Decrement(ref _refCount);

            public void EnsureStarted()
            {
                if (_started) return;
                _started = true;
                _thread = new Thread(CaptureLoop) { IsBackground = true, Name = $"pcap:{Device.Name}" };
                _thread.Start();
            }

            private void CaptureLoop()
            {
                try
                {
                    _comm = Device.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 150);
                    var linkKind = _comm.DataLink.Kind;
                    if (!PacketNormalizer.IsSupported(linkKind)) return;
                    TryApplyTunings(_comm, Device);

                    var token = _cts.Token;
                    _comm.ReceivePackets(0, packet =>
                    {
                        if (token.IsCancellationRequested)
                        {
                            try { _comm.Break(); } catch { }
                            return;
                        }
                        var normalized = PacketNormalizer.EnsureEthernet(packet, linkKind) ?? packet;
                        _owner.Dispatch(Device, normalized); // НИКАКОГО UI!
                    });
                }
                catch
                {
                    // по желанию: лог
                }
                finally
                {
                    try { _comm?.Dispose(); } catch { }
                    _comm = null;
                }
            }

            public void Dispose()
            {
                try { _cts.Cancel(); } catch { }
                try { _comm?.Break(); } catch { }
                try { if (_thread != null && _thread.IsAlive) _thread.Join(1000); } catch { }
                _cts.Dispose();
            }

            private static void TryApplyTunings(PacketCommunicator comm, LivePacketDevice device)
            {
                // BPF
                try {
                    bool disableBpf = VpnSettings.DisableBpf;
                    bool bpfEnabled = App.settingsManager?.GetOption("bpf_filter_enabled", "False", "ADVANCED") == "True";
                    bool isVpnDevice = device != null && TunDetector.IsTunLike(device, VpnInterfaceHints);
                    string configuredFilter = App.settingsManager?.GetOption("capture_filter", string.Empty, "ADVANCED") ?? string.Empty;
                    if (string.Equals(configuredFilter, "auto", StringComparison.OrdinalIgnoreCase))
                        configuredFilter = string.Empty;

                    if (!disableBpf && bpfEnabled)
                    {
                        string expr = null;
                        if (!string.IsNullOrWhiteSpace(configuredFilter))
                        {
                            expr = configuredFilter;
                        }
                        else if (!isVpnDevice)
                        {
                            expr = PacketNormalizer.GetRecommendedBpf(comm.DataLink.Kind, false);
                        }

                        if (!string.IsNullOrWhiteSpace(expr))
                        {
                            using (var filter = comm.CreateFilter(expr))
                            {
                                comm.SetFilter(filter);
                            }
                        }
                    }
                } catch { }
                // Kernel buffer (через рефлексию; молчим если метода нет)
                try {
                    var mi = comm.GetType().GetMethod("SetKernelBufferSize",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (mi != null)
                    {
                        int mb = 8; int.TryParse(App.settingsManager.GetOption("pcap.kernel_buffer_mb","8"), out mb);
                        mi.Invoke(comm, new object[]{ Math.Max(1, mb) * 1024 * 1024 });
                    }
                } catch { }
                // MinToCopy
                try {
                    var mi = comm.GetType().GetMethod("SetMinToCopy",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (mi != null)
                    {
                        int v = 4096; int.TryParse(App.settingsManager.GetOption("pcap.min_to_copy","4096"), out v);
                        mi.Invoke(comm, new object[]{ Math.Max(0, v) });
                    }
                } catch { }
            }
        }

        public sealed class Subscription : IDisposable
        {
            private readonly CaptureService _owner;
            internal readonly int Id;
            internal readonly HashSet<string> DeviceNames;
            internal readonly Action<Packet, LivePacketDevice> Callback;
            internal volatile bool Disposed;

            internal Subscription(CaptureService owner, int id,
                                  IEnumerable<LivePacketDevice> devices,
                                  Action<Packet, LivePacketDevice> cb)
            {
                _owner = owner; Id = id; Callback = cb;
                DeviceNames = new HashSet<string>(devices.Select(d => StableKey(d)));
            }

            public void Dispose()
            {
                if (Disposed) return;
                Disposed = true;
                _owner.Unsubscribe(this);
            }
        }

        private readonly object _gate = new object();
        private readonly ConcurrentDictionary<string, WorkerEntry> _workers = new ConcurrentDictionary<string, WorkerEntry>(); // key = device.Name
        private readonly ConcurrentDictionary<int, Subscription> _subs = new ConcurrentDictionary<int, Subscription>();
        private int _nextId = 0;
        private volatile bool _disposed;

        // --------- Глобальный дедуп ---------
        private readonly bool _dedupEnabled =
            App.settingsManager?.GetOption("dedup_multi_nic","True") == "True"
            || App.settingsManager?.GetOption("capture.dedup_global","False") == "True";
        private const int DedupRingSize = 1 << 16; // 65536
        private readonly uint[] _dedupRing = new uint[DedupRingSize];
        private int _dedupCursor = 0;
        private long _dedupDrops = 0;

        public int WorkersCount => _workers.Count;
        public int SubscriptionsCount => _subs.Count;
        public long DedupDropped => Interlocked.Read(ref _dedupDrops);
        
        public (string key, int refs)[] DebugWorkers()
        {
            return _workers.Select(kv => (kv.Key, kv.Value.RefCount)).ToArray();
        }

        public void Reset()
        {
            lock (_gate)
            {
                foreach (var s in _subs.Values) s.Disposed = true;
                _subs.Clear();
                foreach (var w in _workers.Values) { try { w.Dispose(); } catch { } }
                _workers.Clear();
                _nextId = 0;
            }
        }

        public Subscription Subscribe(IEnumerable<LivePacketDevice> devices, Action<Packet, LivePacketDevice> onPacket)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(CaptureService));
            if (devices == null) throw new ArgumentNullException(nameof(devices));
            // 1) удаляем дубликаты адаптеров по стабильному ключу
            var devList = devices
                .Where(d => d != null)
                .GroupBy(d => StableKey(d))
                .Select(g => g.First())
                .ToList();
            if (devList.Count == 0) throw new ArgumentException("No devices provided");
            if (onPacket == null) throw new ArgumentNullException(nameof(onPacket));

            var id = Interlocked.Increment(ref _nextId);
            var sub = new Subscription(this, id, devList, onPacket);

            lock (_gate)
            {
                foreach (var dev in devList)
                {
                    var we = _workers.GetOrAdd(StableKey(dev), _ => new WorkerEntry(this, dev));
                    we.AddRef();
                    we.EnsureStarted();
                }
                _subs.TryAdd(id, sub);
            }
            return sub;
        }

        private void Unsubscribe(Subscription sub)
        {
            if (sub == null) return;
            lock (_gate)
            {
                _subs.TryRemove(sub.Id, out _);
                foreach (var devName in sub.DeviceNames)
                {
                    if (_workers.TryGetValue(devName, out var we))
                    {
                        var rc = we.Release();
                        if (rc <= 0)
                        {
                            _workers.TryRemove(devName, out _);
                            try { we.Dispose(); } catch { }
                        }
                    }
                }
            }
        }

        internal void Dispatch(LivePacketDevice device, Packet packet)
        {
            // Глобальный дедуп между всеми адаптерами/подписками
            if (_dedupEnabled && SeenBefore(HashPacketFast(packet)))
            {
                Interlocked.Increment(ref _dedupDrops);
                return;
            }
            // Рассылка подписчикам, которые подписаны на этот адаптер. Без UI/аллоц.
            foreach (var kv in _subs)
            {
                var sub = kv.Value;
                if (sub.Disposed) continue;
                if (sub.DeviceNames.Contains(StableKey(device)))
                {
                    try { sub.Callback(packet, device); } catch { /* изоляция подписчика */ }
                }
            }
        }

        // --------- Быстрый хэш пакета без аллокаций ---------
        // Цель: устойчив к захвату одним и тем же L3/L4 пакетом на разных NIC/моментах.
        private static uint HashPacketFast(Packet p)
        {
            const uint FNV_OFF = 2166136261u;
            const uint FNV_PRM = 16777619u;
            uint h = FNV_OFF;
            try
            {
                var eth = p?.Ethernet;
                if (eth == null)
                {
                    // Фолбэк: длина + timestamp ticks (грубая защита)
                    h = (h ^ (uint)(p?.Length ?? 0)) * FNV_PRM;
                    h = (h ^ (uint)(p?.Timestamp.Ticks ?? 0)) * FNV_PRM;
                    return h;
                }

                // L3 — IPv4/IPv6
                var ip4 = eth.IpV4;
                if (ip4 != null)
                {
                    // src/dst IPv4
                    h = (h ^ ip4.Source.ToValue()) * FNV_PRM;
                    h = (h ^ ip4.Destination.ToValue()) * FNV_PRM;
                    // proto + длина L3
                    h = (h ^ (uint)ip4.Protocol) * FNV_PRM;
                    h = (h ^ (uint)ip4.TotalLength) * FNV_PRM;
                    // L4 (порты)
                    if (ip4.Protocol == IpV4Protocol.Tcp)
                    {
                        var tcp = ip4.Tcp;
                        h = (h ^ (uint)tcp.SourcePort) * FNV_PRM;
                        h = (h ^ (uint)tcp.DestinationPort) * FNV_PRM;
                        // можно добавить seq/ack, но это уже излишне
                    }
                    else if (ip4.Protocol == IpV4Protocol.Udp)
                    {
                        var udp = ip4.Udp;
                        h = (h ^ (uint)udp.SourcePort) * FNV_PRM;
                        h = (h ^ (uint)udp.DestinationPort) * FNV_PRM;
                        h = (h ^ (uint)udp.Length) * FNV_PRM;
                    }
                    return h;
                }

                var ip6 = eth.IpV6;
                if (ip6 != null)
                {
                    // src/dst IPv6 - используем только старшие биты для производительности
                    var srcVal = ip6.Source.ToValue();
                    var dstVal = ip6.CurrentDestination.ToValue();
                    
                    // Извлекаем 32-битные части из 128-битных адресов
                    h = (h ^ (uint)(srcVal >> 96)) * FNV_PRM; // старшие 32 бита src
                    h = (h ^ (uint)(dstVal >> 96)) * FNV_PRM; // старшие 32 бита dst
                    h = (h ^ (uint)ip6.NextHeader) * FNV_PRM;
                    // L4 порты
                    if (ip6.NextHeader == IpV4Protocol.Tcp)
                    {
                        var tcp = ip6.Tcp;
                        h = (h ^ (uint)tcp.SourcePort) * FNV_PRM;
                        h = (h ^ (uint)tcp.DestinationPort) * FNV_PRM;
                    }
                    else if (ip6.NextHeader == IpV4Protocol.Udp)
                    {
                        var udp = ip6.Udp;
                        h = (h ^ (uint)udp.SourcePort) * FNV_PRM;
                        h = (h ^ (uint)udp.DestinationPort) * FNV_PRM;
                        h = (h ^ (uint)udp.Length) * FNV_PRM;
                    }
                    return h;
                }

                // Не-IP кадры: используем EtherType + длину
                h = (h ^ (uint)eth.EtherType) * FNV_PRM;
                h = (h ^ (uint)p.Length) * FNV_PRM;
                return h;
            }
            catch
            {
                // На всякий — фолбэк
                h = (h ^ (uint)(p?.Length ?? 0)) * FNV_PRM;
                return h;
            }
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        private bool SeenBefore(uint hash)
        {
            int idx = System.Threading.Interlocked.Increment(ref _dedupCursor) & (DedupRingSize - 1);
            uint prev = System.Threading.Volatile.Read(ref _dedupRing[idx]);
            if (prev == hash) return true;
            System.Threading.Volatile.Write(ref _dedupRing[idx], hash);
            return false;
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            lock (_gate)
            {
                foreach (var s in _subs.Values) s.Disposed = true;
                _subs.Clear();
                foreach (var w in _workers.Values) { try { w.Dispose(); } catch { } }
                _workers.Clear();
            }
        }
    }
}