using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;

namespace tickMeter.Classes
{
    public static class MetadataResolver
    {
        private static readonly ConcurrentDictionary<string, string> _reverseCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static readonly ConcurrentDictionary<string, byte> _pendingLookups = new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);
        private const byte PendingValue = 1;

        public static (string remote, string source) Resolve(string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(ipAddress))
                return (string.Empty, "n/a");

            if (_reverseCache.TryGetValue(ipAddress, out var cached))
            {
                if (!string.IsNullOrWhiteSpace(cached))
                    return (cached, "dns");

                return (ipAddress, "raw");
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
                            _reverseCache[ipAddress] = entry.HostName;
                        }
                        else
                        {
                            _reverseCache[ipAddress] = string.Empty;
                        }
                    }
                    catch
                    {
                        _reverseCache[ipAddress] = string.Empty;
                    }
                    finally
                    {
                        _pendingLookups.TryRemove(ipAddress, out _);
                    }
                });
            }

            return (ipAddress, "raw");
        }
    }
}
