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

            return kind == DataLinkKind.IpV4;
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

            byte[] payload;
            ushort etherType;

            switch (kind)
            {
                case DataLinkKind.IpV4:
                    var packetLength = packet.Length;
                    payload = new byte[packetLength];
                    Buffer.BlockCopy(sourceBytes, 0, payload, 0, packetLength);
                    etherType = 0x0800;
                    break;
                default:
                    return packet;
            }

            var buffer = new byte[payload.Length + 14];
            buffer[12] = (byte)(etherType >> 8);
            buffer[13] = (byte)(etherType & 0xFF);
            Buffer.BlockCopy(payload, 0, buffer, 14, payload.Length);

            return new Packet(buffer, packet.Timestamp, DataLinkKind.Ethernet);
        }
    }
}
