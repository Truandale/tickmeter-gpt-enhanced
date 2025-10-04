using PcapDotNet.Packets.IpV4;
using System;
using System.Diagnostics;

namespace tickMeter.Classes
{
    public class PacketFilter
    {
        public string DestIpFilter { get; set; } = "";
        public string SourceIpFilter { get; set; } = "";

        public string DestPortFilter { get; set; } = "";
        public string SourcePortFilter { get; set; } = "";

        public string ProtocolFilter { get; set; } = "";

        public string PacketSizeFilter { get; set; } = "";
        public string ProcessFilter { get; set; } = "";

        public IpV4Datagram ip;

        protected bool EqCheck(string filterPart, string packetValue)
        {
            if(filterPart.Contains("!"))
            {
                return filterPart != packetValue;
            }
            return filterPart == packetValue;
        }

        protected bool RangeCheck(string[] portParts,int portToCheck)
        {
            bool negative = false;
            if (portParts[0].Contains("!"))
            {
                negative = true;
                portParts[0] = portParts[0].Replace("!", "");
            }
            int fromPort = int.Parse(portParts[0]);
            int toPort = int.Parse(portParts[1]);
            if(negative)
            {
                return portToCheck > toPort && portToCheck < fromPort;
            } else
            {
                return portToCheck <= toPort && portToCheck >= fromPort;
            }
        }

        protected bool ValidateProtocol()
        {
            // Проверяем, что ip не null
            if (ip == null) return false;
            
            switch (ProtocolFilter.ToLower())
            {
                case "udp":
                    if (ip.Protocol == IpV4Protocol.Udp) return true;
                    break;
                case "tcp":
                    if (ip.Protocol == IpV4Protocol.Tcp) return true;
                    break;
                case "tcp and udp":
                    if (ip.Protocol == IpV4Protocol.Tcp || ip.Protocol == IpV4Protocol.Udp) return true;
                    break;
                case "":
                    return true;
            }
            return false;
        }

        protected bool ValidatePort(string port, string portToCheck)
        {
            if (portToCheck == "0" || portToCheck == "") return false;

            if (port == "") return true;

            string[] portParts = port.Split('-');
            if (portParts.Length == 2)
            {
                return RangeCheck(portParts, int.Parse(portToCheck));
            }
            portParts = port.Split(',');
            bool validFlag = false;
            foreach (string checkPort in portParts)
            {
                validFlag = EqCheck(checkPort, portToCheck.ToString()) || validFlag;
                    
            }
            return validFlag;
        }

        private bool ValidateIP(string ipFilter, string ip)
        {
            if (ipFilter == "") return true;
            if (ipFilter == ip) return true;
            if (ipFilter.Contains("!")) return ipFilter != ip;
            string[] ipFilterParts = ipFilter.Split('.');
            string[] ipParts = ip.Split('.');
            for (int i = 0; i < ipFilterParts.Length; i++)
            {
                try
                {
                    if (ipFilterParts[i] != ipParts[i]) return false;
                }
                catch (Exception) { Debug.Print("ValidateIP"); return false; }

            }
            return true;
        }

        public bool ValidatePacketSize()
        {
            int packetSize = ip.TotalLength;
            if (PacketSizeFilter == "") return true;
            string[] sizes = PacketSizeFilter.Split(',');
            bool validFlag = false;
            if (sizes.Length > 1)
            {
                foreach(string sizeP in sizes)
                {
                    validFlag = EqCheck(sizeP, packetSize.ToString()) || validFlag;
                }
                return validFlag;
            }

            sizes = PacketSizeFilter.Split('-');
            if (sizes.Length == 2)
            {
                return RangeCheck(sizes, packetSize);
            }
            if (PacketSizeFilter.Contains("!")) return int.Parse(PacketSizeFilter) != packetSize;
            return int.Parse(PacketSizeFilter) == packetSize;
        }

        public bool ValidateProcess(string ProcessToCheck)
        {
            if (ProcessFilter == "") return true;
            if (ProcessFilter.Contains("!")) return ProcessToCheck.ToLower() != ProcessFilter.ToLower();
            return ProcessToCheck.ToLower() == ProcessFilter.ToLower();
        }

        public bool ValidateForOutputPacket()
        {
            try
            {
                Debug.Print($"[PacketFilter] <<<<<< ValidateForOutputPacket() ENTRY - ip={ip?.GetType().Name ?? "NULL"}");
                
                if (ip == null) return false;
                
                if (!ValidateProtocol()) return false;

                string SourcePort = "";
                string DestPort = "";

                if (ip.Protocol == IpV4Protocol.Udp)
                {
                    // RACE CONDITION FIX: сохраняем в локальную переменную
                    var udp = ip.Udp;
                    if (udp == null) return false;
                    try
                    {
                        DestPort = udp.SourcePort.ToString();
                        SourcePort = udp.DestinationPort.ToString();
                    }
                    catch
                    {
                        return false;
                    }
                }
                else if (ip.Protocol == IpV4Protocol.Tcp)
                {
                    // RACE CONDITION FIX: сохраняем в локальную переменную
                    var tcp = ip.Tcp;
                    if (tcp == null) return false;
                    try
                    {
                        DestPort = tcp.SourcePort.ToString();
                        SourcePort = tcp.DestinationPort.ToString();
                    }
                    catch
                    {
                        return false;
                    }
                }
                
                if (!ValidatePort(SourcePortFilter, SourcePort)) return false;
                if (!ValidatePort(DestPortFilter, DestPort)) return false;

                string DestIp = "";
                string SourceIp = "";
                
                try
                {
                    // RACE CONDITION FIX: сохраняем в локальные переменные
                    var src = ip.Source;
                    var dst = ip.Destination;
                    
                    if (src != null)
                        DestIp = src.ToString();
                    if (dst != null)
                        SourceIp = dst.ToString();
                }
                catch
                {
                    return false;
                }

                if (!ValidateIP(SourceIpFilter, SourceIp)) return false;
                if (!ValidateIP(DestIpFilter, DestIp)) return false;

                if (!ValidatePacketSize()) return false;

                return true;
            }
            catch (Exception ex)
            {
                Debug.Print($"[PacketFilter] ValidateForOutputPacket error: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }

        public bool Validate()
        {
            try
            {
                Debug.Print($"[PacketFilter] >>>>>> Validate() ENTRY - ip={ip?.GetType().Name ?? "NULL"}");
                
                // Проверяем, что ip не null (может быть null при VPN/тунелировании)
                if (ip == null) return false;
                
                if (!ValidateProtocol()) return false;

                string SourcePort = "";
                string DestPort = "";
                
                switch(ip.Protocol)
                {
                    case IpV4Protocol.Udp:
                        // RACE CONDITION FIX: сохраняем в локальную переменную
                        var udp = ip.Udp;
                        if (udp == null) return false;
                        try
                        {
                            SourcePort = udp.SourcePort.ToString();
                            DestPort = udp.DestinationPort.ToString();
                        }
                        catch
                        {
                            return false; // Если порты недоступны, пропускаем
                        }
                        break;
                    case IpV4Protocol.Tcp:
                        // RACE CONDITION FIX: сохраняем в локальную переменную
                        var tcp = ip.Tcp;
                        if (tcp == null) return false;
                        try
                        {
                            SourcePort = tcp.SourcePort.ToString();
                            DestPort = tcp.DestinationPort.ToString();
                        }
                        catch
                        {
                            return false; // Если порты недоступны, пропускаем
                        }
                        break;
                }

                if (!ValidatePort(SourcePortFilter, SourcePort)) return false;

                if (!ValidatePort(DestPortFilter, DestPort)) return false;

                string SourceIp = "";
                string DestIp = "";
                
                try
                {
                    // RACE CONDITION FIX: сохраняем в локальные переменные
                    var src = ip.Source;
                    var dst = ip.Destination;
                    
                    if (src != null)
                        SourceIp = src.ToString();
                    if (dst != null)
                        DestIp = dst.ToString();
                }
                catch
                {
                    return false; // Если IP адреса недоступны
                }

                if (!ValidateIP(SourceIpFilter, SourceIp)) return false;
                if (!ValidateIP(DestIpFilter, DestIp)) return false;

                if (!ValidatePacketSize()) return false;

                return true;
            }
            catch (Exception ex)
            {
                Debug.Print($"[PacketFilter] Validate error: {ex.GetType().Name} - {ex.Message}");
                return false;
            }
        }

    }
}
