using System;
using PcapDotNet.Core;
using PcapDotNet.Packets;

namespace tickMeter.Classes
{
    public static class PacketNormalizer
    {
        /// <summary>
        /// Determines whether the specified data-link kind is supported by the current capture profile.
        /// When VPN heavy mode is enabled, we allow raw IPv4 links in addition to Ethernet.
        /// </summary>
        public static bool IsSupported(DataLinkKind kind)
        {
            if (kind == DataLinkKind.Ethernet)
                return true;

            if (!VpnSettings.AllowNonEthernet)
                return false;

            switch (kind)
            {
                case DataLinkKind.IpV4:
                case DataLinkKind.PointToPointProtocolWithDirection:
                case DataLinkKind.LinuxSll:
                    return true;
            }

            var kindName = kind.ToString();
            if (string.Equals(kindName, "IpV6", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kindName, "Ipv6", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kindName, "Null", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kindName, "Loop", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(kindName, "Raw", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        public static string GetRecommendedBpf(DataLinkKind kind, bool forVpn)
        {
            if (forVpn)
                return null;

            return "ip or ip6";
        }

        /// <summary>
        /// Ensures that downstream consumers always see an Ethernet-framed packet.
        /// For non-Ethernet captures we wrap the payload in a synthetic Ethernet header.
        /// </summary>
        public static Packet EnsureEthernet(Packet packet, DataLinkKind kind)
        {
            if (packet == null)
                return null;

            if (kind == DataLinkKind.Ethernet)
                return packet;

            if (!VpnSettings.AllowNonEthernet)
                return packet;

            try
            {
                var ethernet = packet.Ethernet;
                if (ethernet != null)
                    return packet;
            }
            catch
            {
                // Ignored — we will rebuild below.
            }

            var sourceBytes = packet.Buffer;
            if (sourceBytes == null || sourceBytes.Length == 0)
                return packet;

            byte[] payload = null;
            ushort etherType = 0;

            var kindName = kind.ToString();

            if (kind == DataLinkKind.IpV4)
            {
                var packetLength = packet.Length;
                payload = new byte[packetLength];
                Buffer.BlockCopy(sourceBytes, 0, payload, 0, packetLength);
                etherType = GuessEtherTypeFromIpPayload(payload);
            }
            else if (string.Equals(kindName, "IpV6", StringComparison.OrdinalIgnoreCase) || string.Equals(kindName, "Ipv6", StringComparison.OrdinalIgnoreCase))
            {
                var packetLength = packet.Length;
                payload = new byte[packetLength];
                Buffer.BlockCopy(sourceBytes, 0, payload, 0, packetLength);
                etherType = 0x86DD;
            }
            else
            {
                // Heuristic for loopback / cooked headers (e.g., Null / LinuxSll)
                if (sourceBytes.Length > 4)
                {
                    var af = BitConverter.ToUInt32(sourceBytes, 0);
                    if (af == 2u || af == 24u)
                    {
                        var payloadLength = packet.Length - 4;
                        if (payloadLength > 0)
                        {
                            payload = new byte[payloadLength];
                            Buffer.BlockCopy(sourceBytes, 4, payload, 0, payloadLength);
                            etherType = af == 24u ? (ushort)0x86DD : (ushort)0x0800;
                        }
                    }
                }

                if (payload == null)
                {
                    // Fallback: treat as raw IP if header not recognized
                    var packetLength = packet.Length;
                    payload = new byte[packetLength];
                    Buffer.BlockCopy(sourceBytes, 0, payload, 0, packetLength);
                    etherType = GuessEtherTypeFromIpPayload(payload);
                }
            }

            if (payload == null || payload.Length == 0 || etherType == 0)
                return packet;

            var buffer = new byte[payload.Length + 14];
            buffer[12] = (byte)(etherType >> 8);
            buffer[13] = (byte)(etherType & 0xFF);
            Buffer.BlockCopy(payload, 0, buffer, 14, payload.Length);

            return new Packet(buffer, packet.Timestamp, DataLinkKind.Ethernet);
        }

        private static ushort GuessEtherTypeFromIpPayload(byte[] payload)
        {
            if (payload == null || payload.Length == 0)
                return 0;

            var version = (payload[0] >> 4) & 0x0F;

            if (version == 4)
                return 0x0800;
            if (version == 6)
                return 0x86DD;

            return 0;
        }
    }
}
