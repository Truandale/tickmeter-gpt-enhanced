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
        public static PingManager pingManager;
        public static ConnectionTracker connectionTracker;
        public static Classes.CaptureService Capture;
        public static Classes.NetworkOptimizer networkOptimizer;
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
            pingManager = new PingManager(settingsManager, connMngr);
            
            // Инициализируем CaptureService для централизованного управления PCAP воркерами
            Capture = new Classes.CaptureService();
            Debug.Print("[App.Init] CaptureService initialized successfully");
            
            // Инициализируем VPN bypass компоненты
            connectionTracker = new ConnectionTracker();
            
            // Инициализируем детектор спайков
            Classes.SpikeDetection.SpikeDetectionManager.InitializeDetector();
            Debug.Print("[App.Init] Spike detection system initialized");
            
            // Инициализируем анализатор качества сети
            Classes.NetworkQualityAnalyzer.Initialize();
            Debug.Print("[App.Init] Network quality analyzer initialized");
            
            // Инициализируем оптимизатор сети
            networkOptimizer = new NetworkOptimizer();
            networkOptimizer.Initialize();
            Debug.Print("[App.Init] Network optimizer initialized");
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

            /// <summary>
            /// Safely invoke an action on the Settings form. If the form handle is not yet created,
            /// the action is queued to run once HandleCreated fires. If the form is null or disposed,
            /// the action is skipped.
            /// </summary>
            public static void SafeInvokeOnSettings(Action action)
            {
                try
                {
                    var f = settingsForm;
                    if (f == null || f.IsDisposed || action == null) return;

                    if (f.IsHandleCreated)
                    {
                        try { f.BeginInvoke(action); } catch { /* best-effort */ }
                        return;
                    }

                    EventHandler handler = null;
                    handler = (s, e) =>
                    {
                        try
                        {
                            f.HandleCreated -= handler;
                            if (!f.IsDisposed)
                            {
                                try { f.BeginInvoke(action); } catch { }
                            }
                        }
                        catch { }
                    };

                    f.HandleCreated += handler;
                }
                catch { }
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
        
        // Диагностические свойства для CaptureService
        public static int CaptureWorkersCount => Capture?.WorkersCount ?? 0;
        public static int CaptureSubsCount => Capture?.SubscriptionsCount ?? 0;
        public static long CaptureDedupDrops => Capture?.DedupDropped ?? 0L;

    }
}
