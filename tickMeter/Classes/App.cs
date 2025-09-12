using PcapDotNet.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using tickMeter.Forms;

namespace tickMeter.Classes
{
    public static class App
    {
        public static GUI gui;
        public static PacketFilterForm packetFilterForm;
        public static ProfileEditForm profileEditForm;
        public static SettingsForm settingsForm;
        public static TickrateStatistics tickrateStatisticsForm;
        public static PacketStats packetStatsForm;
        public static ProfilesForm profilesForm;
        public static TickMeterState meterState;
        public static SettingsManager settingsManager;
        public static ConnectionsManager connMngr;
        static List<LivePacketDevice> AdaptersList;

        public static void Init()
        {
            tickrateStatisticsForm = new TickrateStatistics();
            packetFilterForm = new PacketFilterForm();
            settingsForm = new SettingsForm();
            packetStatsForm = new PacketStats();
            profilesForm = new ProfilesForm();
            profileEditForm = new ProfileEditForm();
            settingsManager = new SettingsManager();
            connMngr = new ConnectionsManager();

            // Debug: вывод интервалов пинга для TCP и ICMP
            #if DEBUG
            int tcpPingInterval = 0;
            int icmpPingInterval = 0;
            var tcpIntervalStr = settingsManager?.GetOption("ping_interval");
            if (!string.IsNullOrEmpty(tcpIntervalStr) && int.TryParse(tcpIntervalStr, out int tcpVal))
                tcpPingInterval = tcpVal;
            // Если есть отдельная настройка для ICMP, используйте её, иначе используйте TCP
            var icmpIntervalStr = settingsManager?.GetOption("icmp_ping_interval");
            if (!string.IsNullOrEmpty(icmpIntervalStr) && int.TryParse(icmpIntervalStr, out int icmpVal))
                icmpPingInterval = icmpVal;
            else
                icmpPingInterval = tcpPingInterval;
            Debug.WriteLine($"[DEBUG] TCP ping interval: {tcpPingInterval} ms");
            Debug.WriteLine($"[DEBUG] ICMP ping interval: {icmpPingInterval} ms");
            #endif
        }

        public static List<LivePacketDevice> GetAdapters()
        {
            try
            {
                AdaptersList = LivePacketDevice.AllLocalMachine.ToList();
            }
            catch (Exception)
            {
                MessageBox.Show("Install NPCAP. Try to run as Admin");
                if (MessageBox.Show("Download NPCAP?", "NPCAP", MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    Process.Start("https://npcap.com/dist/npcap-1.76.exe");
                }
            }

            if (AdaptersList.Count == 0)
            {
                MessageBox.Show("No network connections found");
            }
            return AdaptersList;
        }

        public static string GetAdapterAddress(LivePacketDevice Adapter)
        {
            if (Adapter.Description != null)
            {
                Match match;
                foreach (DeviceAddress address in Adapter.Addresses) {
                    match = Regex.Match(address.Address.ToString(), "(\\d)+\\.(\\d)+\\.(\\d)+\\.(\\d)+");
                    if(match.Value != "")
                    {
                        return match.Value;
                    }
                }
                DeviceAddress adapterAddress = Adapter.Addresses.LastOrDefault();
                string addr = "";
                if (adapterAddress != null)
                    addr = adapterAddress.ToString();

                match = Regex.Match(addr, "(\\d)+\\.(\\d)+\\.(\\d)+\\.(\\d)+");
                if (match.Value == "")
                {
                    if (Adapter.Addresses.Count > 1)
                    {
                        addr = Adapter.Addresses[1].ToString();
                        match = Regex.Match(addr, "(\\d)+\\.(\\d)+\\.(\\d)+\\.(\\d)+");
                    }
                    return "";
                }
                return match.Value;
            }
            return "";
        }

    }
}
