using PcapDotNet.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using tickMeter.Forms;

namespace tickMeter.Classes
{
    public static class App
    {
        // COM initialization constants and imports
        private const uint COINIT_APARTMENTTHREADED = 0x2;
        private const uint COINIT_DISABLE_OLE1DDE = 0x4;
        private const int RPC_E_CHANGED_MODE = unchecked((int)0x80010106);
        
        [DllImport("ole32.dll")]
        private static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);
        
        /// <summary>
        /// Безопасная инициализация COM - игнорирует ошибку если COM уже инициализирован
        /// </summary>
        private static void SafeCoInitialize()
        {
            int hr = CoInitializeEx(IntPtr.Zero, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
            if (hr != 0 && hr != RPC_E_CHANGED_MODE)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
        }

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
        public static PingManager pingManager;
        
        // EMA фильтры для сглаживания графиков
        public static Ema emaChartTickrate;
        public static Ema emaChartPing;
        static List<LivePacketDevice> AdaptersList;

        public static void Init()
        {
            // Сначала инициализируем settingsManager, так как он нужен для SettingsForm
            settingsManager = new SettingsManager();
            connMngr = new ConnectionsManager();
            
            tickrateStatisticsForm = new TickrateStatistics();
            packetFilterForm = new PacketFilterForm();
            settingsForm = new SettingsForm();
            packetStatsForm = new PacketStats();
            profilesForm = new ProfilesForm();
            profileEditForm = new ProfileEditForm();
            pingManager = new PingManager(settingsManager, connMngr);

        }

        public static List<LivePacketDevice> GetAdapters()
        {
            try
            {
                // Безопасно инициализируем COM перед работой с PcapDotNet
                SafeCoInitialize();
                
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
