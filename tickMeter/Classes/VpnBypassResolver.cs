using System;
using System.Collections.Concurrent;
using System.Net;

namespace tickMeter.Classes
{
    /// <summary>
    /// Хранит переопределения процессов для соединений, чтобы скрыть VPN маскировку.
    /// </summary>
    public static class VpnBypassResolver
    {
        private struct OverrideEntry
        {
            public int Pid;
            public string Exe;
            public DateTime TimestampUtc;
        }

        private static readonly ConcurrentDictionary<string, OverrideEntry> _tupleOverrides = new ConcurrentDictionary<string, OverrideEntry>();
        private static readonly ConcurrentDictionary<string, OverrideEntry> _localOverrides = new ConcurrentDictionary<string, OverrideEntry>();
        private const double EntryTtlMs = 120000; // 2 минуты

        private static string BuildTupleKey(byte proto, string localIp, int localPort, string remoteIp, int remotePort) =>
            $"{proto}:{localIp}:{localPort}:{remoteIp}:{remotePort}";

        private static string BuildLocalKey(byte proto, string localIp, int localPort) =>
            $"{proto}:{localIp}:{localPort}";

        public static void Register(byte proto, IPAddress local, int localPort, IPAddress remote, int remotePort, ConnectionTracker.Info info)
        {
            if (local == null || remote == null) return;

            var entry = new OverrideEntry
            {
                Pid = info.Pid,
                Exe = info.Exe,
                TimestampUtc = DateTime.UtcNow
            };

            string localStr = local.ToString();
            string remoteStr = remote.ToString();

            _tupleOverrides[BuildTupleKey(proto, localStr, localPort, remoteStr, remotePort)] = entry;
            _tupleOverrides[BuildTupleKey(proto, remoteStr, remotePort, localStr, localPort)] = entry;
            _localOverrides[BuildLocalKey(proto, localStr, localPort)] = entry;
        }

        private static bool TryGetEntry(ConcurrentDictionary<string, OverrideEntry> dict, string key, out OverrideEntry entry)
        {
            if (dict.TryGetValue(key, out entry))
            {
                double ageMs = (DateTime.UtcNow - entry.TimestampUtc).TotalMilliseconds;
                if (ageMs <= EntryTtlMs && ageMs >= 0)
                {
                    return true;
                }

                dict.TryRemove(key, out _);
            }

            entry = default;
            return false;
        }

        public static bool TryOverrideTcp(tickMeter.TcpProcessRecord record)
        {
            if (record == null) return false;

            string key = BuildTupleKey(
                6,
                record.LocalAddress?.ToString() ?? string.Empty,
                record.LocalPort,
                record.RemoteAddress?.ToString() ?? string.Empty,
                record.RemotePort);
            if (!TryGetEntry(_tupleOverrides, key, out var entry))
            {
                return false;
            }

            Apply(record, entry);
            return true;
        }

        public static bool TryOverrideUdp(tickMeter.UdpProcessRecord record)
        {
            if (record == null) return false;

            string key = BuildLocalKey(
                17,
                record.LocalAddress?.ToString() ?? string.Empty,
                unchecked((int)record.LocalPort));
            if (!TryGetEntry(_localOverrides, key, out var entry))
            {
                return false;
            }

            Apply(record, entry);
            return true;
        }

        private static void Apply(tickMeter.TcpProcessRecord record, OverrideEntry entry)
        {
            record.ProcessId = entry.Pid;
            if (!string.IsNullOrEmpty(entry.Exe))
            {
                record.ProcessName = entry.Exe;
            }
        }

        private static void Apply(tickMeter.UdpProcessRecord record, OverrideEntry entry)
        {
            record.ProcessId = entry.Pid;
            if (!string.IsNullOrEmpty(entry.Exe))
            {
                record.ProcessName = entry.Exe;
            }
        }
    }
}
