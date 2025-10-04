using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using PcapDotNet.Core;

namespace tickMeter.Classes
{
    public static class TunnelAutoAttach
    {
        private static readonly TimeSpan Cooldown = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan DebounceWindow = TimeSpan.FromSeconds(10);
    private static readonly ConcurrentDictionary<string, DateTime> PendingIps = new ConcurrentDictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
    private static readonly ConcurrentDictionary<string, (DateTime attachedAt, LivePacketDevice device)> ActiveDevices = new ConcurrentDictionary<string, (DateTime, LivePacketDevice)>(StringComparer.OrdinalIgnoreCase);
        private static readonly string[] VirtualHints =
        {
            "tun", "tap", "wintun", "wireguard", "vpn", "wg", "openvpn", "tailscale", "zerotier", "forti", "checkpoint", "l2tp", "pppoe"
        };

        private static Func<IEnumerable<LivePacketDevice>> _deviceProvider;
        private static Action<LivePacketDevice> _startCapture;
        private static Action<LivePacketDevice> _stopCapture;
        private static volatile bool _initialized;

        public static void Init(
            Func<IEnumerable<LivePacketDevice>> deviceProvider,
            Action<LivePacketDevice> startCapture,
            Action<LivePacketDevice> stopCapture)
        {
            if (_initialized)
                return;

            if (!VpnSettings.AdvancedEnabled || VpnSettings.ForceCaptureVirtual)
                return;

            _deviceProvider = deviceProvider ?? throw new ArgumentNullException(nameof(deviceProvider));
            _startCapture = startCapture ?? throw new ArgumentNullException(nameof(startCapture));
            _stopCapture = stopCapture;

            EtwBroker.OnLocalTunnelObserved += HandleLocalTunnelObserved;
            _initialized = true;
            DebugLogger.log("[AutoAttach] VPN tunnel auto attach initialized");
        }

        public static void Dispose()
        {
            if (!_initialized)
                return;

            try
            {
                EtwBroker.OnLocalTunnelObserved -= HandleLocalTunnelObserved;
            }
            finally
            {
                _initialized = false;
            }
        }

        private static void HandleLocalTunnelObserved(IPAddress address)
        {
            if (!_initialized || address == null)
                return;

            var key = address.ToString();
            var now = DateTime.UtcNow;

            if (PendingIps.TryGetValue(key, out var lastSeen) && now - lastSeen < DebounceWindow)
                return;

            PendingIps[key] = now;
            DebugLogger.log($"[AutoAttach] Tunnel hint from {key}");

            Task.Run(() => TryAttachVirtualDevices(key, now));
        }

        private static void TryAttachVirtualDevices(string hintKey, DateTime triggeredAt)
        {
            IEnumerable<LivePacketDevice> devices;
            try
            {
                devices = _deviceProvider?.Invoke() ?? Enumerable.Empty<LivePacketDevice>();
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[AutoAttach] Unable to enumerate adapters: {ex.GetType().Name} {ex.Message}");
                return;
            }

            foreach (var device in devices)
            {
                if (device == null)
                    continue;

                if (!TunDetector.IsTunLike(device, VirtualHints))
                    continue;

                var key = BuildDeviceKey(device);
                var now = DateTime.UtcNow;

                if (ActiveDevices.TryGetValue(key, out var active) && now - active.attachedAt < Cooldown)
                    continue;

                ActiveDevices[key] = (now, device);

                try
                {
                    DebugLogger.log($"[AutoAttach] Tunnel IP {hintKey} → device {device.Name} ({device.Description})");
                    _startCapture?.Invoke(device);
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[AutoAttach] Failed to start tunnel capture on {device.Description ?? device.Name}: {ex.GetType().Name} {ex.Message}");
                }
            }
        }

        public static void DetachAll()
        {
            try
            {
                foreach (var kv in ActiveDevices.ToArray())
                {
                    ActiveDevices.TryRemove(kv.Key, out _);
                    try
                    {
                        _stopCapture?.Invoke(kv.Value.device);
                    }
                    catch
                    {
                        // ignore stop failures
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        private static string BuildDeviceKey(LivePacketDevice device)
        {
            if (device == null)
                return string.Empty;

            var name = device.Name ?? string.Empty;
            var idx = name.IndexOf("NPF_{", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
                return name.Substring(idx);

            if (!string.IsNullOrWhiteSpace(name))
                return name;

            return device.Description ?? Guid.NewGuid().ToString("N");
        }
    }
}
