using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace tickMeter.Classes
{
    public static class VpnHeuristics
    {
        private static readonly HashSet<string> GatewayProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "wireguard",
            "wg",
            "openvpn",
            "vpnui",
            "vpnclient",
            "vpnagent",
            "ksde",
            "vpnsvc",
            "nordvpn-service",
            "nordvpn",
            "protonvpn",
            "tailscaled",
            "tailscale",
            "zerotier-one",
            "zerotier",
            "windscribe",
            "surfshark",
            "expressvpn",
            "pia-service",
            "pia-client",
            "merlinclient",
            "clash",
            "clash-for-windows",
            "softether",
            "anyconnect",
            "forticlient",
            "cloudflared",
            "warp-svc",
            "warpcli",
            "stealthguard",
            "pritunl",
            "wg-quick",
            "strongswan",
            "tunsafe",
            "ikev2",
            "avp",
            "zerotier_core"
        };

        private static readonly string[] VpnProcessPatterns =
        {
            "wireguard",
            "wintun",
            "openvpn",
            "vpn",
            "ikev2",
            "zt",
            "zerotier",
            "tunsafe",
            "strongswan",
            "clash",
            "tailscale",
            "wg"
        };

        private static readonly string[] VpnInterfacePatterns =
        {
            "wintun",
            "wireguard",
            "openvpn",
            "zerotier",
            "zt",
            "tap",
            "tun",
            "l2tp",
            "pppoe",
            "tailscale",
            "ikev",
            "vpn"
        };

        private static readonly HashSet<int> VpnShellPorts = new HashSet<int>
        {
            51820, 1194, 1701, 4500, 500
        };

        public static bool IsLikelyVpnProcess(string exeName)
        {
            if (string.IsNullOrWhiteSpace(exeName))
                return false;

            if (GatewayProcessNames.Contains(exeName))
                return true;

            var lower = exeName.ToLowerInvariant();
            return VpnProcessPatterns.Any(p => lower.Contains(p));
        }

        public static bool IsVpnShellPort(int port)
        {
            if (port <= 0)
                return false;

            return VpnShellPorts.Contains(port);
        }

        public static bool LooksLikeVpnShell(string exeName, ProtocolType protocol, int destinationPort)
        {
            if (protocol == ProtocolType.Udp && IsVpnShellPort(destinationPort))
                return true;

            return IsLikelyVpnProcess(exeName);
        }

        public static bool IfaceLooksVpn(string ifaceName)
        {
            if (string.IsNullOrWhiteSpace(ifaceName))
                return false;

            var lower = ifaceName.ToLowerInvariant();
            return VpnInterfacePatterns.Any(p => lower.Contains(p));
        }
    }
}
