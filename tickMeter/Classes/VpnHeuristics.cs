using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace tickMeter.Classes
{
    public static class VpnHeuristics
    {
        private static readonly HashSet<string> GatewayProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "wireguard",
            "openvpn",
            "ksde",
            "vpnsvc",
            "nordvpn-service",
            "nordvpn",
            "tailscaled",
            "tailscale",
            "zerotier-one",
            "windscribe",
            "protonvpn",
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
            "strongswan"
        };

        public static bool LooksLikeVpnShell(string exeName, ProtocolType protocol, int destinationPort)
        {
            if (string.IsNullOrWhiteSpace(exeName))
                return false;

            if (GatewayProcessNames.Contains(exeName))
                return true;

            if (protocol == ProtocolType.Udp)
            {
                if (destinationPort == 51820 || destinationPort == 1194 || destinationPort == 1701 || destinationPort == 4500)
                    return true;
            }

            return false;
        }
    }
}
