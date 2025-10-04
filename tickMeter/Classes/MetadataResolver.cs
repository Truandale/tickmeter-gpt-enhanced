using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace tickMeter.Classes
{
    public static class MetadataResolver
    {
        private readonly struct MetadataEntry
        {
            public readonly string Value;
            public readonly string Source;
            public readonly int Priority;
            public readonly DateTime ExpirationUtc;

            public MetadataEntry(string value, string source, int priority, DateTime expirationUtc)
            {
                Value = value;
                Source = source;
                Priority = priority;
                ExpirationUtc = expirationUtc;
            }

            public bool IsExpired(DateTime now)
            {
                if (ExpirationUtc == DateTime.MaxValue)
                    return false;
                return now >= ExpirationUtc;
            }
        }

        private static readonly ConcurrentDictionary<string, MetadataEntry> _reverseCache =
            new ConcurrentDictionary<string, MetadataEntry>(StringComparer.OrdinalIgnoreCase);

        private static readonly ConcurrentDictionary<string, byte> _pendingLookups =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        private const byte PendingValue = 1;

        private static readonly TimeSpan PositiveDnsTtl = TimeSpan.FromMinutes(15);
        private static readonly TimeSpan NegativeDnsTtl = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan DefaultEtwTtl = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DefaultTrackerTtl = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan DefaultRawTtl = TimeSpan.FromMinutes(5);

        private static int GetPriority(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return 0;

            switch (source.ToLowerInvariant())
            {
                case "etw":
                    return 20;
                case "tracker":
                    return 30;
                case "dns":
                    return 40;
                default:
                    return 0;
            }
        }

        private static TimeSpan GetDefaultTtl(string normalizedSource, bool hasValue)
        {
            switch (normalizedSource)
            {
                case "dns":
                    return hasValue ? PositiveDnsTtl : NegativeDnsTtl;
                case "tracker":
                    return DefaultTrackerTtl;
                case "etw":
                    return DefaultEtwTtl;
                default:
                    return hasValue ? DefaultRawTtl : TimeSpan.FromMinutes(1);
            }
        }

        public static (string remote, string source) Resolve(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return (string.Empty, "n/a");

            var now = DateTime.UtcNow;

            if (_reverseCache.TryGetValue(ipAddress, out var cached))
            {
                if (cached.IsExpired(now))
                {
                    _reverseCache.TryRemove(ipAddress, out _);
                }
                else
                {
                    var value = string.IsNullOrWhiteSpace(cached.Value) ? ipAddress : cached.Value;
                    var source = string.IsNullOrWhiteSpace(cached.Source) ? "raw" : cached.Source;
                    return (value, source);
                }
            }

            if (_pendingLookups.TryAdd(ipAddress, PendingValue))
            {
                Task.Run(async () =>
                {
                    try
                    {
                        var entry = await Dns.GetHostEntryAsync(ipAddress).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(entry?.HostName))
                        {
                            Promote(ipAddress, entry.HostName, "dns", PositiveDnsTtl);
                        }
                        else
                        {
                            Promote(ipAddress, string.Empty, "raw", NegativeDnsTtl);
                        }
                    }
                    catch
                    {
                        Promote(ipAddress, string.Empty, "raw", NegativeDnsTtl);
                    }
                    finally
                    {
                        _pendingLookups.TryRemove(ipAddress, out _);
                    }
                });
            }

            return (ipAddress, "raw");
        }

        public static void Promote(string ipAddress, string value, string source, TimeSpan? ttl = null)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return;

            var normalizedSource = string.IsNullOrWhiteSpace(source) ? "raw" : source;
            var normalizedValue = string.IsNullOrWhiteSpace(value) ? string.Empty : value;
            var priority = GetPriority(normalizedSource);
            var now = DateTime.UtcNow;
            var expiration = ttl.HasValue ? now.Add(ttl.Value) : now.Add(GetDefaultTtl(normalizedSource, !string.IsNullOrWhiteSpace(normalizedValue)));

            _reverseCache.AddOrUpdate(
                ipAddress,
                key => new MetadataEntry(normalizedValue, normalizedSource, priority, expiration),
                (key, existing) =>
                {
                    if (existing.IsExpired(now) || priority >= existing.Priority)
                    {
                        return new MetadataEntry(normalizedValue, normalizedSource, priority, expiration);
                    }

                    return existing;
                });
        }
    }
}
