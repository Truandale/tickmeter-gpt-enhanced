using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using tickMeter.Classes;

namespace tickMeter.Forms
{
    public partial class AdvancedSettingsForm : Form
    {
        private struct VpnPresetSnapshot
        {
            public bool CaptureAll;
            public bool IgnoreVirtual;
            public bool DedupMultiNic;
            public bool BasicMode;
            public bool HasValue;
        }

        private const string VpnRestoreCaptureAllKey = "vpn_bypass_restore_capture_all";
        private const string VpnRestoreIgnoreVirtualKey = "vpn_bypass_restore_ignore_virtual";
        private const string VpnRestoreDedupKey = "vpn_bypass_restore_dedup";
        private const string VpnRestoreBasicKey = "vpn_bypass_restore_basic";

        private bool _isLoadingSettings;
        private bool _suppressVpnPresetUpdates;
        private VpnPresetSnapshot _vpnPresetSnapshot;

        public AdvancedSettingsForm()
        {
            _isLoadingSettings = true;

            InitializeComponent();
            InitializeExtendedOverlayControls(); // Создаем контролы расширенной информации
            AttachEventHandlers();

            LoadSettings();
            _isLoadingSettings = false;

            if (chkVpnBypassAdvanced.Checked)
            {
                if (!TryLoadVpnPresetSnapshot(out _vpnPresetSnapshot))
                {
                    _vpnPresetSnapshot = default;
                }

                ApplyVpnBypassAdvancedPreset(captureSnapshot: false);
            }
            else
            {
                SetVpnPresetControlsEnabled(true);
                _vpnPresetSnapshot = default;
            }
        }

        private void LoadSettings()
        {
            try
            {
                // Live View настройки
                chkLiveMaxRows.Checked = App.settingsManager.GetOption("live_max_rows_enabled", "False", "ADVANCED") == "True";
                liveMaxRowsNumeric.Value = int.Parse(App.settingsManager.GetOption("live_max_rows", "1000", "ADVANCED"));
                chkOverlayFps.Checked = App.settingsManager.GetOption("overlay_fps_enabled", "False", "ADVANCED") == "True";
                overlayFpsNumeric.Value = int.Parse(App.settingsManager.GetOption("overlay_fps", "60", "ADVANCED"));
                
                // BPF фильтр
                chkBpfFilter.Checked = App.settingsManager.GetOption("bpf_filter_enabled", "False", "ADVANCED") == "True";
                captureFilterTextBox.Text = App.settingsManager.GetOption("capture_filter", "ip or ip6", "ADVANCED");
                
                // Capture all adapters
                chkCaptureAllAdapters.Checked = App.settingsManager.GetOption("capture_all_adapters", "False", "SETTINGS") == "True";
                chkIgnoreVirtualAdapters.Checked = App.settingsManager.GetOption("ignore_virtual_adapters", "True", "SETTINGS") == "True";

                // Универсальные чекбоксы
                chkPingBindToInterface.Checked = App.settingsManager.GetOption("ping_bind_to_interface", "True", "SETTINGS") == "True";
                chkPingTcpPrefer.Checked = App.settingsManager.GetOption("ping_tcp_prefer", "True", "SETTINGS") == "True";
                chkPingFallbackIcmp.Checked = App.settingsManager.GetOption("ping_fallback_icmp", "True", "SETTINGS") == "True";
                chkPingTargetActiveOnly.Checked = App.settingsManager.GetOption("ping_target_active_only", "True", "SETTINGS") == "True";
                chkTickrateSmoothing.Checked = App.settingsManager.GetOption("tickrate_smoothing", "True", "SETTINGS") == "True";
                chkPingGraphOverlaySmoothing.Checked = App.settingsManager.GetOption("smoothing_ping_graph_overlay", "True", "ADVANCED") == "True";
                chkTickrateGraphOverlaySmoothing.Checked = App.settingsManager.GetOption("smoothing_tickrate_graph_overlay", "True", "ADVANCED") == "True";
                chkTicktimeGraphOverlaySmoothing.Checked = App.settingsManager.GetOption("smoothing_ticktime_graph_overlay", "True", "ADVANCED") == "True";
                chkPingValueOverlaySmoothing.Checked = App.settingsManager.GetOption("smoothing_ping_value_overlay", "False", "ADVANCED") == "True";
                chkPingValueGuiSmoothing.Checked = App.settingsManager.GetOption("smoothing_ping_value_gui", "False", "ADVANCED") == "True";
                chkSyncPingOverlayWithGui.Checked = App.settingsManager.GetOption("sync_ping_overlay_with_gui", "True", "ADVANCED") == "True";
                chkTickrateValueGuiSmoothing.Checked = App.settingsManager.GetOption("smoothing_tickrate_value_gui", "False", "ADVANCED") == "True";
                chkSyncTickrateOverlayWithGui.Checked = App.settingsManager.GetOption("sync_tickrate_overlay_with_gui", "True", "ADVANCED") == "True";
                chkTickrateValueOverlaySmoothing.Checked = App.settingsManager.GetOption("smoothing_tickrate_value_overlay", "False", "ADVANCED") == "True";
                chkTicktimeValueOverlaySmoothing.Checked = App.settingsManager.GetOption("smoothing_ticktime_value_overlay", "False", "ADVANCED") == "True";
                chkTrafficValueOverlaySmoothing.Checked = App.settingsManager.GetOption("smoothing_traffic_value_overlay", "False", "ADVANCED") == "True";
                chkDedupMultiNic.Checked = App.settingsManager.GetOption("dedup_multi_nic", "True", "SETTINGS") == "True";
                chkEnableIPv6.Checked = App.settingsManager.GetOption("enable_ipv6", "True", "SETTINGS") == "True";
                chkRtssOnlyActive.Checked = App.settingsManager.GetOption("rtss_only_active", "True", "SETTINGS") == "True";
                chkUiRefreshHidden.Checked = App.settingsManager.GetOption("ui_refresh_hidden", "False", "SETTINGS") == "True";
                chkStunEnable.Checked = App.settingsManager.GetOption("stun_enable", "True", "SETTINGS") == "True";
                chkShowPingSpikes.Checked = App.settingsManager.GetOption("show_ping_spikes", "True", "ADVANCED") == "True";
                
                // TODO: Добавить chkAdvancedSpikeDetection в designer файл
                // chkAdvancedSpikeDetection.Checked = App.settingsManager.GetOption("advanced_spike_detection", "True", "ADVANCED") == "True";
                
                // Настройки порогов для спайков пинга
                numPingSpikeThreshold.Value = decimal.Parse(App.settingsManager.GetOption("ping_spike_threshold", "150", "ADVANCED"));
                
                // Spike Detection настройки
                InitSpikeDetectionCombos();
                LoadSpikeDetectionSettings();
                LoadAdvancedSpikeSettings();
                
                // VPN bypass настройки
                chkVpnBypassBasic.Checked = App.settingsManager.GetOption("vpn_bypass_basic", "False", "ADVANCED") == "True";
                chkVpnBypassAdvanced.Checked = App.settingsManager.GetOption("vpn_bypass_advanced", "False", "ADVANCED") == "True";
                
                // Debug settings
                chkEnableTextLogs.Checked = App.settingsManager.GetOption("enable_text_logs", "True", "ADVANCED") == "True";
                
                // Performance Optimization Phase 1-3 настройки
                chkAntiReentrancy.Checked = App.settingsManager.GetOption("anti_reentrancy", "True", "ADVANCED") == "True";
                chkRtssThrottling.Checked = App.settingsManager.GetOption("rtss_throttling", "True", "ADVANCED") == "True";
                chkPcapOptimization.Checked = App.settingsManager.GetOption("pcap_optimization", "True", "ADVANCED") == "True";
                numPcapKernelBufferMb.Value = decimal.Parse(App.settingsManager.GetOption("pcap_kernel_buffer_mb", "8", "ADVANCED"));
                numPcapMinToCopy.Value = decimal.Parse(App.settingsManager.GetOption("pcap_min_to_copy", "4096", "ADVANCED"));
                
                chkVirtualModeListView.Checked = App.settingsManager.GetOption("virtual_mode_listview", "True", "ADVANCED") == "True";
                numVirtualModeThreshold.Value = decimal.Parse(App.settingsManager.GetOption("virtual_mode_threshold", "2000", "ADVANCED"));
                numRingBufferSize.Value = decimal.Parse(App.settingsManager.GetOption("ring_buffer_size", "10000", "ADVANCED"));
                chkShowVirtualModeStats.Checked = App.settingsManager.GetOption("show_virtual_mode_stats", "False", "ADVANCED") == "True";
                
                chkHighPriorityThreads.Checked = App.settingsManager.GetOption("high_priority_threads", "True", "ADVANCED") == "True";
                chkSingleConsumerPattern.Checked = App.settingsManager.GetOption("single_consumer_pattern", "True", "ADVANCED") == "True";
                numUiProcessingRate.Value = decimal.Parse(App.settingsManager.GetOption("ui_processing_rate", "60", "ADVANCED"));
                numUiBatchSize.Value = decimal.Parse(App.settingsManager.GetOption("ui_batch_size", "10", "ADVANCED"));
                
                // Загружаем настройки алертов
                LoadAlertSettings();
                
                // Stage 6: Network Quality Analysis настройки
                LoadNetworkQualitySettings();
                
                // Stage 7: Network Optimizer настройки
                LoadNetworkOptimizerSettings();
                
                // Color Zone settings
                LoadColorZoneSettings();
                
                // Extended Overlay settings
                LoadExtendedOverlaySettings();
                
                // Tickrate Chart settings
                LoadTickrateChartSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AttachEventHandlers()
        {
            chkVpnBypassAdvanced.CheckedChanged += chkVpnBypassAdvanced_CheckedChanged;
            
            // Взаимоисключение для галок сглаживания пинга в оверлее
            chkSyncPingOverlayWithGui.CheckedChanged += chkSyncPingOverlayWithGui_CheckedChanged;
            chkPingValueOverlaySmoothing.CheckedChanged += chkPingValueOverlaySmoothing_CheckedChanged;
            chkPingGraphOverlaySmoothing.CheckedChanged += chkPingGraphOverlaySmoothing_CheckedChanged;
            
            // Взаимоисключение для галок сглаживания тикрейта в оверлее
            chkSyncTickrateOverlayWithGui.CheckedChanged += chkSyncTickrateOverlayWithGui_CheckedChanged;
            chkTickrateValueOverlaySmoothing.CheckedChanged += chkTickrateValueOverlaySmoothing_CheckedChanged;
            chkTickrateGraphOverlaySmoothing.CheckedChanged += chkTickrateGraphOverlaySmoothing_CheckedChanged;
        }

        private void chkVpnBypassAdvanced_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoadingSettings || _suppressVpnPresetUpdates)
            {
                return;
            }

            if (chkVpnBypassAdvanced.Checked)
            {
                ApplyVpnBypassAdvancedPreset(captureSnapshot: true);
                PersistVpnPresetSnapshot(_vpnPresetSnapshot);
            }
            else
            {
                RestoreVpnBypassSnapshot();
            }
        }

        // Обработчик для синхронизации пинга оверлея и GUI
        private void chkSyncPingOverlayWithGui_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoadingSettings)
                return;

            if (chkSyncPingOverlayWithGui.Checked)
            {
                // Отключаем независимое сглаживание для оверлея
                chkPingValueOverlaySmoothing.Checked = false;
                chkPingGraphOverlaySmoothing.Checked = false;
            }
        }

        // Обработчик для сглаживания значения пинга в оверлее
        private void chkPingValueOverlaySmoothing_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoadingSettings)
                return;

            if (chkPingValueOverlaySmoothing.Checked)
            {
                // Отключаем синхронизацию с GUI
                chkSyncPingOverlayWithGui.Checked = false;
            }
        }

        // Обработчик для сглаживания графика пинга в оверлее
        private void chkPingGraphOverlaySmoothing_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoadingSettings)
                return;

            if (chkPingGraphOverlaySmoothing.Checked)
            {
                // Отключаем синхронизацию с GUI
                chkSyncPingOverlayWithGui.Checked = false;
            }
        }

        // Обработчик для синхронизации тикрейта оверлея и GUI
        private void chkSyncTickrateOverlayWithGui_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoadingSettings)
                return;

            if (chkSyncTickrateOverlayWithGui.Checked)
            {
                // Отключаем независимое сглаживание для оверлея
                chkTickrateValueOverlaySmoothing.Checked = false;
                chkTickrateGraphOverlaySmoothing.Checked = false;
            }
        }

        // Обработчик для сглаживания значения тикрейта в оверлее
        private void chkTickrateValueOverlaySmoothing_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoadingSettings)
                return;

            if (chkTickrateValueOverlaySmoothing.Checked)
            {
                // Отключаем синхронизацию с GUI
                chkSyncTickrateOverlayWithGui.Checked = false;
            }
        }

        // Обработчик для сглаживания графика тикрейта в оверлее
        private void chkTickrateGraphOverlaySmoothing_CheckedChanged(object sender, EventArgs e)
        {
            if (_isLoadingSettings)
                return;

            if (chkTickrateGraphOverlaySmoothing.Checked)
            {
                // Отключаем синхронизацию с GUI
                chkSyncTickrateOverlayWithGui.Checked = false;
            }
        }

        private void ApplyVpnBypassAdvancedPreset(bool captureSnapshot)
        {
            if (captureSnapshot && !_vpnPresetSnapshot.HasValue)
            {
                _vpnPresetSnapshot = CaptureCurrentVpnPresetSnapshot();
            }

            _suppressVpnPresetUpdates = true;
            chkVpnBypassBasic.Checked = true;
            chkCaptureAllAdapters.Checked = true;
            chkIgnoreVirtualAdapters.Checked = false;
            chkDedupMultiNic.Checked = true;
            _suppressVpnPresetUpdates = false;

            SetVpnPresetControlsEnabled(false);
        }

        private void RestoreVpnBypassSnapshot()
        {
            var snapshot = _vpnPresetSnapshot;

            if (!snapshot.HasValue)
            {
                if (!TryLoadVpnPresetSnapshot(out snapshot))
                {
                    snapshot = GetDefaultVpnPresetSnapshot();
                }
            }

            SetVpnPresetControlsEnabled(true);

            _suppressVpnPresetUpdates = true;
            chkCaptureAllAdapters.Checked = snapshot.CaptureAll;
            chkIgnoreVirtualAdapters.Checked = snapshot.IgnoreVirtual;
            chkDedupMultiNic.Checked = snapshot.DedupMultiNic;
            chkVpnBypassBasic.Checked = snapshot.BasicMode;
            _suppressVpnPresetUpdates = false;

            _vpnPresetSnapshot = default;
        }

        private VpnPresetSnapshot CaptureCurrentVpnPresetSnapshot()
        {
            return new VpnPresetSnapshot
            {
                CaptureAll = chkCaptureAllAdapters.Checked,
                IgnoreVirtual = chkIgnoreVirtualAdapters.Checked,
                DedupMultiNic = chkDedupMultiNic.Checked,
                BasicMode = chkVpnBypassBasic.Checked,
                HasValue = true
            };
        }

        private static VpnPresetSnapshot GetDefaultVpnPresetSnapshot()
        {
            return new VpnPresetSnapshot
            {
                CaptureAll = false,
                IgnoreVirtual = true,
                DedupMultiNic = true,
                BasicMode = false,
                HasValue = true
            };
        }

        private void SetVpnPresetControlsEnabled(bool enabled)
        {
            chkCaptureAllAdapters.Enabled = enabled;
            chkIgnoreVirtualAdapters.Enabled = enabled;
            chkDedupMultiNic.Enabled = enabled;
            chkVpnBypassBasic.Enabled = enabled;
        }

        private bool TryLoadVpnPresetSnapshot(out VpnPresetSnapshot snapshot)
        {
            snapshot = default;

            var manager = App.settingsManager;
            if (manager == null)
            {
                return false;
            }

            string captureAll = manager.GetOption(VpnRestoreCaptureAllKey, null, "ADVANCED");
            string ignoreVirtual = manager.GetOption(VpnRestoreIgnoreVirtualKey, null, "ADVANCED");
            string dedup = manager.GetOption(VpnRestoreDedupKey, null, "ADVANCED");
            string basic = manager.GetOption(VpnRestoreBasicKey, null, "ADVANCED");

            if (string.IsNullOrEmpty(captureAll) &&
                string.IsNullOrEmpty(ignoreVirtual) &&
                string.IsNullOrEmpty(dedup) &&
                string.IsNullOrEmpty(basic))
            {
                return false;
            }

            snapshot = new VpnPresetSnapshot
            {
                CaptureAll = ParseBoolOrDefault(captureAll, false),
                IgnoreVirtual = ParseBoolOrDefault(ignoreVirtual, true),
                DedupMultiNic = ParseBoolOrDefault(dedup, true),
                BasicMode = ParseBoolOrDefault(basic, false),
                HasValue = true
            };

            return true;
        }

        private void PersistVpnPresetSnapshot(VpnPresetSnapshot snapshot)
        {
            var manager = App.settingsManager;
            if (manager == null || !snapshot.HasValue)
            {
                return;
            }

            manager.SetOption(VpnRestoreCaptureAllKey, snapshot.CaptureAll.ToString(), "ADVANCED");
            manager.SetOption(VpnRestoreIgnoreVirtualKey, snapshot.IgnoreVirtual.ToString(), "ADVANCED");
            manager.SetOption(VpnRestoreDedupKey, snapshot.DedupMultiNic.ToString(), "ADVANCED");
            manager.SetOption(VpnRestoreBasicKey, snapshot.BasicMode.ToString(), "ADVANCED");
        }

        private void ClearPersistedVpnPresetSnapshot()
        {
            var manager = App.settingsManager;
            if (manager == null)
            {
                return;
            }

            manager.SetOption(VpnRestoreCaptureAllKey, string.Empty, "ADVANCED");
            manager.SetOption(VpnRestoreIgnoreVirtualKey, string.Empty, "ADVANCED");
            manager.SetOption(VpnRestoreDedupKey, string.Empty, "ADVANCED");
            manager.SetOption(VpnRestoreBasicKey, string.Empty, "ADVANCED");
        }

        private void PersistVpnPresetSnapshotForSave()
        {
            if (chkVpnBypassAdvanced.Checked)
            {
                if (!_vpnPresetSnapshot.HasValue)
                {
                    TryLoadVpnPresetSnapshot(out _vpnPresetSnapshot);
                }

                PersistVpnPresetSnapshot(_vpnPresetSnapshot);
            }
            else
            {
                ClearPersistedVpnPresetSnapshot();
                _vpnPresetSnapshot = default;
            }
        }

        private static bool ParseBoolOrDefault(string value, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return defaultValue;
            }

            if (bool.TryParse(value, out bool parsed))
            {
                return parsed;
            }

            if (value == "1")
            {
                return true;
            }

            if (value == "0")
            {
                return false;
            }

            return defaultValue;
        }

        private void SaveSettings()
        {
            try
            {
                // Live View настройки
                App.settingsManager.SetOption("live_max_rows_enabled", chkLiveMaxRows.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("live_max_rows", SettingsManager.ToInvariantString((int)liveMaxRowsNumeric.Value), "ADVANCED");
                
                // RTSS настройки
                App.settingsManager.SetOption("overlay_fps_enabled", chkOverlayFps.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("overlay_fps", SettingsManager.ToInvariantString((int)overlayFpsNumeric.Value), "ADVANCED");
                
                // BPF фильтр
                App.settingsManager.SetOption("bpf_filter_enabled", chkBpfFilter.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("capture_filter", captureFilterTextBox.Text, "ADVANCED");
                
                // Capture all adapters - запоминаем старое значение перед сохранением
                bool wasCaptureAllEnabled = App.settingsManager.GetOption("capture_all_adapters", "False", "SETTINGS") == "True";
                bool isCaptureAllEnabled = chkCaptureAllAdapters.Checked;
                
                App.settingsManager.SetOption("capture_all_adapters", chkCaptureAllAdapters.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("ignore_virtual_adapters", chkIgnoreVirtualAdapters.Checked.ToString(), "SETTINGS");
                
                // Если мультиадаптер только что включили - сбрасываем флаг ручной разблокировки
                if (!wasCaptureAllEnabled && isCaptureAllEnabled)
                {
                    App.settingsManager.SetOption("manual_ip_unlocked", "False", "SETTINGS");
                    System.Diagnostics.Debug.WriteLine("[AdvancedSettings] Мультиадаптер включен, сброшен флаг manual_ip_unlocked");
                }

                // Универсальные чекбоксы
                App.settingsManager.SetOption("ping_bind_to_interface", chkPingBindToInterface.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("ping_tcp_prefer", chkPingTcpPrefer.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("ping_fallback_icmp", chkPingFallbackIcmp.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("ping_target_active_only", chkPingTargetActiveOnly.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("tickrate_smoothing", chkTickrateSmoothing.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("smoothing_ping_graph_overlay", chkPingGraphOverlaySmoothing.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("smoothing_tickrate_graph_overlay", chkTickrateGraphOverlaySmoothing.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("smoothing_ticktime_graph_overlay", chkTicktimeGraphOverlaySmoothing.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("smoothing_ping_value_overlay", chkPingValueOverlaySmoothing.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("smoothing_ping_value_gui", chkPingValueGuiSmoothing.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("sync_ping_overlay_with_gui", chkSyncPingOverlayWithGui.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("smoothing_tickrate_value_gui", chkTickrateValueGuiSmoothing.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("sync_tickrate_overlay_with_gui", chkSyncTickrateOverlayWithGui.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("smoothing_tickrate_value_overlay", chkTickrateValueOverlaySmoothing.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("smoothing_ticktime_value_overlay", chkTicktimeValueOverlaySmoothing.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("smoothing_traffic_value_overlay", chkTrafficValueOverlaySmoothing.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("dedup_multi_nic", chkDedupMultiNic.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("enable_ipv6", chkEnableIPv6.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("rtss_only_active", chkRtssOnlyActive.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("ui_refresh_hidden", chkUiRefreshHidden.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("stun_enable", chkStunEnable.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("show_ping_spikes", chkShowPingSpikes.Checked.ToString(), "ADVANCED");
                
                // TODO: Добавить chkAdvancedSpikeDetection в designer файл
                // App.settingsManager.SetOption("advanced_spike_detection", chkAdvancedSpikeDetection.Checked.ToString(), "ADVANCED");
                
                // Настройки порогов для спайков пинга
                App.settingsManager.SetOption("ping_spike_threshold", SettingsManager.ToInvariantString((int)numPingSpikeThreshold.Value), "ADVANCED");
                
                // VPN bypass настройки
                App.settingsManager.SetOption("vpn_bypass_basic", chkVpnBypassBasic.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("vpn_bypass_advanced", chkVpnBypassAdvanced.Checked.ToString(), "ADVANCED");
                PersistVpnPresetSnapshotForSave();
                
                // Debug settings
                App.settingsManager.SetOption("enable_text_logs", chkEnableTextLogs.Checked.ToString(), "ADVANCED");
                
                // Performance Optimization Phase 1-3 настройки  
                App.settingsManager.SetOption("anti_reentrancy", chkAntiReentrancy.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("rtss_throttling", chkRtssThrottling.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("pcap_optimization", chkPcapOptimization.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("pcap_kernel_buffer_mb", SettingsManager.ToInvariantString((int)numPcapKernelBufferMb.Value), "ADVANCED");
                App.settingsManager.SetOption("pcap_min_to_copy", SettingsManager.ToInvariantString((int)numPcapMinToCopy.Value), "ADVANCED");
                
                App.settingsManager.SetOption("virtual_mode_listview", chkVirtualModeListView.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("virtual_mode_threshold", SettingsManager.ToInvariantString((int)numVirtualModeThreshold.Value), "ADVANCED");
                App.settingsManager.SetOption("ring_buffer_size", SettingsManager.ToInvariantString((int)numRingBufferSize.Value), "ADVANCED");
                App.settingsManager.SetOption("show_virtual_mode_stats", chkShowVirtualModeStats.Checked.ToString(), "ADVANCED");
                
                App.settingsManager.SetOption("high_priority_threads", chkHighPriorityThreads.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("single_consumer_pattern", chkSingleConsumerPattern.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("ui_processing_rate", SettingsManager.ToInvariantString((int)numUiProcessingRate.Value), "ADVANCED");
                App.settingsManager.SetOption("ui_batch_size", SettingsManager.ToInvariantString((int)numUiBatchSize.Value), "ADVANCED");
                
                // Spike Detection настройки
                SaveSpikeDetectionSettings();
                SaveAdvancedSpikeSettings();
                
                // Alert Settings
                SaveAlertSettings();
                
                // Stage 6: Network Quality Analysis настройки
                SaveNetworkQualitySettings();
                
                // Stage 7: Network Optimizer настройки
                SaveNetworkOptimizerSettings();
                
                // Color Zone settings
                SaveColorZoneSettings();
                
                // Extended Overlay settings  
                SaveExtendedOverlaySettings();
                
                // Tickrate Chart settings
                SaveTickrateChartSettings();
                
                // Применяем новые настройки интервала overlay
                App.gui?.ApplyOverlayIntervalFromSettings();
                
                MessageBox.Show("Настройки сохранены", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            // Отменяем изменения - просто закрываем форму без сохранения
            this.Close();
        }

        private void chkLiveMaxRows_CheckedChanged(object sender, EventArgs e)
        {
            liveMaxRowsNumeric.Enabled = chkLiveMaxRows.Checked;
        }

        private void chkOverlayFps_CheckedChanged(object sender, EventArgs e)
        {
            overlayFpsNumeric.Enabled = chkOverlayFps.Checked;
        }

        private void chkBpfFilter_CheckedChanged(object sender, EventArgs e)
        {
            captureFilterTextBox.Enabled = chkBpfFilter.Checked;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            SaveSettings();
            this.Close();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }

        private void btnReset_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show(
                "Установить проверенные оптимальные настройки?\n\n" +
                "Эти настройки протестированы для стабильной работы:\n" +
                "• Обновление каждую секунду (ping_interval = 1000 мс)\n" +
                "• Гибридный режим PCAP+Windows Stats\n" +
                "• Мульти-адаптерный захват с игнорированием виртуальных\n" +
                "• Все виды сглаживания включены\n" +
                "• Детекция спайков ping и tickrate\n" +
                "• Оптимизированные буферы (8MB PCAP, 10K ring buffer)\n" +
                "• Минимальный расширенный оверлей (процесс + время сессии)\n\n" +
                "Все параметры настроены для повседневного использования.",
                "Сброс к оптимальным настройкам",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                SetOptimalSettings();
                MessageBox.Show(
                    "Проверенные оптимальные настройки успешно установлены!\n\n" +
                    "✓ Обновление: каждую секунду для точности\n" +
                    "✓ Гибридный режим: PCAP точность + Windows Stats реализм\n" +
                    "✓ Сглаживание: все виды активированы для стабильности\n" +
                    "✓ Детекция спайков: настроена для ping (150мс) и tickrate\n" +
                    "✓ Мульти-адаптер: с игнорированием виртуальных устройств\n" +
                    "✓ Производительность: оптимизированные буферы и потоки\n" +
                    "✓ Оверлей: минимальная полезная информация\n\n" +
                    "Настройки сохранены. Рекомендуется перезапустить программу.",
                    "Настройки обновлены",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Устанавливает оптимальные настройки для повседневного использования
        /// Основано на проверенных настройках пользователя из settings.ini
        /// </summary>
        private void SetOptimalSettings()
        {
            // === ОСНОВНЫЕ [SETTINGS] НАСТРОЙКИ ИЗ ВАШИХ РАБОЧИХ ЗНАЧЕНИЙ ===
            
            // Output - что отображать (все включено, как у вас)
            App.settingsManager.SetOption("rtss", "True", "SETTINGS");                    // RTSS оверлей ✓
            App.settingsManager.SetOption("autodetect", "True", "SETTINGS");              // Автоматическое отслеживание ✓
            App.settingsManager.SetOption("capture_all_adapters", "True", "SETTINGS");    // Захват всех адаптеров ✓
            App.settingsManager.SetOption("chart", "True", "SETTINGS");                   // График тикрейта ✓
            App.settingsManager.SetOption("ip", "True", "SETTINGS");                      // Отображать IP ✓
            App.settingsManager.SetOption("tickrate", "True", "SETTINGS");                // Отображать Tickrate ✓
            App.settingsManager.SetOption("ticktime", "True", "SETTINGS");                // График времени пакета ✓
            App.settingsManager.SetOption("ping_chart", "True", "SETTINGS");              // График пинга ✓
            App.settingsManager.SetOption("ping", "True", "SETTINGS");                    // Отображать Ping и страну ✓
            App.settingsManager.SetOption("traffic", "True", "SETTINGS");                 // Отображать трафик ✓
            App.settingsManager.SetOption("session_time", "True", "SETTINGS");            // Время сессии ✓
            App.settingsManager.SetOption("show_packet_drops", "True", "SETTINGS");       // Потери пакетов ✓
            
            // Интервалы и порты (ваши значения)
            App.settingsManager.SetOption("ping_interval", "1000", "SETTINGS");           // Интервал пинга 1000ms ✓
            App.settingsManager.SetOption("ping_ports", "", "SETTINGS");                  // Порты для пинга (пустые) ✓
            
            // Системные настройки (ваши проверенные значения)
            App.settingsManager.SetOption("data_send", "False", "SETTINGS");              // Логировать тикрейт в CSV ✗
            App.settingsManager.SetOption("run_minimized", "True", "SETTINGS");           // Запускать свёрнутым ✓
            App.settingsManager.SetOption("manual_ip_unlocked", "False", "SETTINGS");     // Ручной IP заблокирован ✗
            
            // Сетевые настройки (все ваши оптимальные значения)
            App.settingsManager.SetOption("ping_bind_to_interface", "True", "SETTINGS");  // Привязка пинга к интерфейсу ✓
            App.settingsManager.SetOption("ping_tcp_prefer", "True", "SETTINGS");         // Предпочитать TCP ✓
            App.settingsManager.SetOption("ping_fallback_icmp", "True", "SETTINGS");      // Fallback на ICMP ✓
            App.settingsManager.SetOption("ping_target_active_only", "True", "SETTINGS"); // Пинг только активных ✓
            App.settingsManager.SetOption("tickrate_smoothing", "True", "SETTINGS");      // Сглаживание тикрейта ✓
            App.settingsManager.SetOption("dedup_multi_nic", "True", "SETTINGS");         // Дедупликация NIC ✓
            App.settingsManager.SetOption("enable_ipv6", "True", "SETTINGS");             // IPv6 ✓
            App.settingsManager.SetOption("ignore_virtual_adapters", "True", "SETTINGS"); // Игнорировать виртуальные ✓
            App.settingsManager.SetOption("rtss_only_active", "True", "SETTINGS");        // RTSS только для активных ✓
            App.settingsManager.SetOption("stun_enable", "True", "SETTINGS");             // STUN ✓
            App.settingsManager.SetOption("network_quality_overlay", "True", "SETTINGS"); // Качество сети ✓
            App.settingsManager.SetOption("ui_refresh_hidden", "True", "SETTINGS");       // UI обновление скрытых ✓
            
            // Цвета оверлея (ваши проверенные значения)
            App.settingsManager.SetOption("color_label", "636BDA", "SETTINGS");           // Цвет лейблов (синий) ✓
            App.settingsManager.SetOption("color_bad", "FF0000", "SETTINGS");             // Красный для плохих значений ✓
            App.settingsManager.SetOption("color_mid", "FF8040", "SETTINGS");             // Оранжевый для средних ✓
            App.settingsManager.SetOption("color_good", "00FF00", "SETTINGS");            // Зелёный для хороших ✓
            App.settingsManager.SetOption("color_chart", "FF0080", "SETTINGS");           // Розовый для графиков ✓
            
            // === ADVANCED НАСТРОЙКИ ИЗ ВАШИХ РАБОЧИХ ЗНАЧЕНИЙ ===
            
            // Live View
            App.settingsManager.SetOption("live_max_rows_enabled", "True", "ADVANCED");    // Ограничение строк ✓
            App.settingsManager.SetOption("live_max_rows", "1000", "ADVANCED");            // 1000 строк ✓
            
            // RTSS и FPS
            App.settingsManager.SetOption("overlay_fps_enabled", "False", "ADVANCED");     // FPS ограничение ✗
            App.settingsManager.SetOption("overlay_fps", "60", "ADVANCED");                // 60 FPS ✓
            
            // BPF фильтр
            App.settingsManager.SetOption("bpf_filter_enabled", "False", "ADVANCED");      // BPF фильтр ✗
            App.settingsManager.SetOption("capture_filter", "ip or ip6", "ADVANCED");      // Фильтр захвата ✓
            
            // Сглаживания (все включены, как у вас)
            App.settingsManager.SetOption("smoothing_ping_value", "True", "ADVANCED");     // Сглаживание значений пинга ✓
            App.settingsManager.SetOption("smoothing_traffic_value", "True", "ADVANCED");  // Сглаживание трафика ✓
            App.settingsManager.SetOption("smoothing_tickrate_graph", "True", "ADVANCED"); // Сглаживание графика тикрейта ✓
            App.settingsManager.SetOption("smoothing_ticktime_graph", "True", "ADVANCED"); // Сглаживание графика тиктайма ✓
            App.settingsManager.SetOption("use_windows_stats", "False", "ADVANCED");       // Windows Stats ✗
            App.settingsManager.SetOption("hybrid_pcap_windows", "True", "ADVANCED");      // Гибридный режим ✓
            App.settingsManager.SetOption("smoothing_ping_graph", "True", "ADVANCED");     // Сглаживание графика пинга ✓
            App.settingsManager.SetOption("smoothing_ping_graph_overlay", "True", "ADVANCED");     // Оверлей пинг-график ✓
            App.settingsManager.SetOption("smoothing_tickrate_graph_overlay", "True", "ADVANCED"); // Оверлей тикрейт-график ✓
            App.settingsManager.SetOption("smoothing_ticktime_graph_overlay", "True", "ADVANCED"); // Оверлей тиктайм-график ✓
            App.settingsManager.SetOption("smoothing_ping_value_overlay", "True", "ADVANCED");     // Оверлей значения пинга ✓
            App.settingsManager.SetOption("smoothing_tickrate_value_overlay", "True", "ADVANCED"); // Оверлей значения тикрейта ✓
            App.settingsManager.SetOption("smoothing_traffic_value_overlay", "True", "ADVANCED");  // Оверлей значения трафика ✓
            App.settingsManager.SetOption("smoothing_ping_value_gui", "True", "ADVANCED");          // GUI сглаживание пинга ✓
            
            // Ping Spikes (ваши настройки)
            App.settingsManager.SetOption("show_ping_spikes", "True", "ADVANCED");         // Показывать спайки пинга ✓
            App.settingsManager.SetOption("ping_spike_threshold", "150", "ADVANCED");      // Порог спайков 150ms ✓
            
            // VPN bypass (ИСПРАВЛЕННЫЕ значения - при включении расширенного включается и простой)
            App.settingsManager.SetOption("vpn_bypass_basic", "True", "ADVANCED");         // Простой VPN bypass ✓ (должен быть включен вместе с расширенным)
            App.settingsManager.SetOption("vpn_bypass_advanced", "True", "ADVANCED");      // Сложный VPN bypass ✓ (включен на скриншоте)
            
            // Debug settings - отключаем логирование для оптимальной производительности
            App.settingsManager.SetOption("enable_text_logs", "False", "ADVANCED");        // Отключить текстовые логи ✗ (для простых игроков)
            
            // Оптимизации производительности (ваши значения)
            App.settingsManager.SetOption("anti_reentrancy", "True", "ADVANCED");          // Защита от реентерабельности ✓
            App.settingsManager.SetOption("rtss_throttling", "True", "ADVANCED");          // RTSS троттлинг ✓
            App.settingsManager.SetOption("pcap_optimization", "True", "ADVANCED");        // PCAP оптимизация ✓
            App.settingsManager.SetOption("pcap_kernel_buffer_mb", "8", "ADVANCED");       // Буфер ядра 8MB ✓
            App.settingsManager.SetOption("pcap_min_to_copy", "4096", "ADVANCED");         // Минимум копирования 4KB ✓
            
            // Virtual Mode (ваши настройки)
            App.settingsManager.SetOption("virtual_mode_listview", "True", "ADVANCED");    // Виртуальный режим ✓
            App.settingsManager.SetOption("virtual_mode_threshold", "1000", "ADVANCED");   // Порог 1000 ✓
            App.settingsManager.SetOption("ring_buffer_size", "10000", "ADVANCED");        // Размер буфера 10000 ✓
            App.settingsManager.SetOption("show_virtual_mode_stats", "True", "ADVANCED");  // Статистика виртуального режима ✓
            
            // Thread Management (ваши оптимальные значения)
            App.settingsManager.SetOption("high_priority_threads", "True", "ADVANCED");    // Высокий приоритет потоков ✓
            App.settingsManager.SetOption("single_consumer_pattern", "False", "ADVANCED"); // Single consumer ✗
            App.settingsManager.SetOption("ui_processing_rate", "60", "ADVANCED");         // Частота UI 60Hz ✓
            App.settingsManager.SetOption("ui_batch_size", "10", "ADVANCED");              // Размер пакета UI 10 ✓
            
            // Spike Detection (ваши РЕАЛЬНЫЕ настройки со скриншота)
            App.settingsManager.SetOption("spikes.enable", "True", "ADVANCED");            // Детекция спайков ✓
            App.settingsManager.SetOption("spikes.metrics", "ping,tickrate,ticktime", "ADVANCED"); // Метрики ✓
            App.settingsManager.SetOption("spikes.display", "both", "ADVANCED");           // Отображение обоих ✓
            App.settingsManager.SetOption("spikes.sensitivity", "very_low", "ADVANCED");   // ОЧЕНЬ низкая чувствительность (новый пресет) ✓
            App.settingsManager.SetOption("spikes.min_hold_ms", "50", "ADVANCED");         // Минимальная длительность 50ms (быстрое снятие) ✓
            App.settingsManager.SetOption("spikes.history_size", "1000", "ADVANCED");      // Размер истории 1000 ✓
            // App.settingsManager.SetOption("spikes.auto.enable", "True", "ADVANCED");    // Автокалибровка - УДАЛЕНО (dead code)
            
            // ИСПРАВЛЕННЫЕ значения со скриншота Stage 4:
            App.settingsManager.SetOption("spikes.ema_alpha", "0.050", "ADVANCED");        // EMA alpha 0.050 (не 0.1) ✓
            App.settingsManager.SetOption("spikes.ew_sigma_alpha", "0.020", "ADVANCED");   // EW sigma alpha 0.020 (не 0.05) ✓
            App.settingsManager.SetOption("spikes.sensitivity_multiplier", "3.0", "ADVANCED"); // Множитель 3.0 (не 2) ✓
            App.settingsManager.SetOption("spikes.hysteresis_ratio", "0.70", "ADVANCED");  // Гистерезис 0.70 (не 0.8) ✓
            App.settingsManager.SetOption("spikes.refractory_period_ms", "2000", "ADVANCED"); // Период тишины 2000ms (не 1000) ✓
            App.settingsManager.SetOption("spikes.min_energy_threshold", "2.0", "ADVANCED"); // Мин энергия 2.0 (не 1) ✓
            App.settingsManager.SetOption("spikes.init_window_size", "30", "ADVANCED");    // Размер выборки 30 (не 20) ✓
            
            // Алерты (ваши настройки)
            App.settingsManager.SetOption("alert_sound_enabled", "True", "ADVANCED");      // Звуковые алерты ✓
            App.settingsManager.SetOption("alert_discord_enabled", "True", "ADVANCED");    // Discord алерты ✓
            App.settingsManager.SetOption("alert_discord_webhook", "", "ADVANCED");        // Webhook пустой ✓
            App.settingsManager.SetOption("alert_cooldown_seconds", "30", "ADVANCED");     // Задержка алертов 30s ✓
            
            // Network Quality (ваши настройки)
            App.settingsManager.SetOption("network_quality_enabled", "True", "ADVANCED");  // Анализ качества сети ✓
            App.settingsManager.SetOption("quality_history_size", "100", "ADVANCED");      // Размер истории качества 100 ✓
            App.settingsManager.SetOption("stability_threshold", "0.15", "ADVANCED");      // Порог стабильности 0.15 ✓
            App.settingsManager.SetOption("quality_threshold", "0.8", "ADVANCED");         // Порог качества 0.8 ✓
            App.settingsManager.SetOption("network_optimization_enabled", "False", "ADVANCED"); // Сетевая оптимизация ✗
            App.settingsManager.SetOption("optimization_threshold", "70", "ADVANCED");     // Порог оптимизации 70 ✓
            App.settingsManager.SetOption("optimization_interval", "5", "ADVANCED");       // Интервал оптимизации 5 ✓
            App.settingsManager.SetOption("aggressive_optimization", "False", "ADVANCED"); // Агрессивная оптимизация ✗
            
            // Spike Detection UI
            App.settingsManager.SetOption("spike_detection_enable", "True", "ADVANCED");   // Общая детекция ✓
            App.settingsManager.SetOption("spike_sensitivity", "High", "ADVANCED");        // Высокая чувствительность ✓
            App.settingsManager.SetOption("tickrate_chart_enabled", "True", "ADVANCED");   // График тикрейта ✓
            
            // VPN дополнительные настройки (ваши значения)
            App.settingsManager.SetOption("vpn_capture_virtual", "False", "ADVANCED");     // Захват виртуальных для VPN ✗
            App.settingsManager.SetOption("vpn_allow_non_ethernet", "False", "ADVANCED");  // Разрешить не-Ethernet ✗
            App.settingsManager.SetOption("vpn_disable_bpf", "False", "ADVANCED");         // Отключить BPF ✗
            App.settingsManager.SetOption("vpn_etw_enrichment", "False", "ADVANCED");      // ETW обогащение ✗
            App.settingsManager.SetOption("vpn_bypass_restore_capture_all", "True", "ADVANCED");    // Восстановить захват всех ✓
            App.settingsManager.SetOption("vpn_bypass_restore_ignore_virtual", "True", "ADVANCED"); // Восстановить игнорирование виртуальных ✓
            App.settingsManager.SetOption("vpn_bypass_restore_dedup", "True", "ADVANCED");          // Восстановить дедупликацию ✓
            App.settingsManager.SetOption("vpn_bypass_restore_basic", "True", "ADVANCED");          // Восстановить базовые настройки ✓
            
            // === ДОПОЛНИТЕЛЬНЫЕ СЕКЦИИ ===
            
            // ZONES
            App.settingsManager.SetOption("color_zone_profile", "Very Low", "ZONES");      // Профиль цветовых зон ✓
            
            // EXTENDED оверлей (ИСПРАВЛЕННЫЕ значения со скриншота)
            App.settingsManager.SetOption("show_active_process", "True", "EXTENDED");      // Показывать отслеживаемый процесс ✓
            App.settingsManager.SetOption("show_session_time", "True", "EXTENDED");        // Показывать время текущей сессии ✓
            App.settingsManager.SetOption("show_external_ip", "True", "EXTENDED");         // Показывать внешний IP адрес ✓ (включено на скриншоте)
            App.settingsManager.SetOption("show_session_stats", "False", "EXTENDED");      // Показывать статистику сессии ✗ (отключено на скриншоте)
            App.settingsManager.SetOption("show_server_info", "False", "EXTENDED");        // Показывать информацию о сервере ✗ (отключено на скриншоте)
            App.settingsManager.SetOption("show_packet_counters", "False", "EXTENDED");    // Показывать счетчики пакетов ✗ (отключено на скриншоте)
            App.settingsManager.SetOption("show_connection_type", "False", "EXTENDED");    // Показывать тип подключения ✗ (отключено на скриншоте)
            App.settingsManager.SetOption("show_diagnostic_info", "False", "EXTENDED");    // Показывать диагностическую информацию ✗ (отключено на скриншоте)
            
            // TICKRATE_CHART (ваши настройки)
            App.settingsManager.SetOption("tickrate_chart_enabled", "True", "TICKRATE_CHART");        // График включен ✓
            App.settingsManager.SetOption("tickrate_chart_per_server", "True", "TICKRATE_CHART");     // По серверу ✓
            App.settingsManager.SetOption("tickrate_chart_compression", "True", "TICKRATE_CHART");    // Сжатие ✓
            App.settingsManager.SetOption("tickrate_chart_time_scale", "True", "TICKRATE_CHART");     // Временная шкала ✓
            App.settingsManager.SetOption("tickrate_chart_trimming", "True", "TICKRATE_CHART");       // Обрезка ✓
            App.settingsManager.SetOption("tickrate_chart_mode", "Сжатый график", "TICKRATE_CHART");  // Режим сжатый ✓
            App.settingsManager.SetOption("tickrate_chart_max_points", "1500", "TICKRATE_CHART");     // Макс точек 1500 ✓
            App.settingsManager.SetOption("tickrate_chart_history_hours", "24", "TICKRATE_CHART");    // История 24ч ✓
            
            // PROFILES (ваши настройки)
            App.settingsManager.SetOption("current_profile", "Стандарт (Balanced)", "PROFILES");      // Текущий профиль ✓
            App.settingsManager.SetOption("advanced_profile", "Streamer", "PROFILES");                // Продвинутый профиль ✓
            
            // Обновляем UI-элементы формы в соответствии с настройками
            LoadSettings();
            
            // Сохраняем все изменения
            SaveSettings();
            
            // Принудительно перезагружаем настройки
            try
            {
                App.settingsManager.ReloadConfig();
                
                // Обновляем основную форму настроек
                if (App.settingsForm != null && !App.settingsForm.IsDisposed)
                {
                    App.settingsForm.ApplyFromConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка перезагрузки: {ex.Message}");
            }
            
            // Уведомляем об успешном применении ВАШИХ оптимальных настроек
            MessageBox.Show("Восстановлены ваши проверенные оптимальные настройки! ✅\n\n" +
                           "📊 ОВЕРЛЕЙ:\n" +
                           "• Все основные метрики: Tickrate, Ping, IP, Traffic, Session Time\n" +
                           "• Все графики: Tickrate Chart, Ping Chart, Ticktime Chart\n" +
                           "• Потери пакетов и качество сети\n" +
                           "• Расширенная информация: процесс, время сессии, внешний IP\n" +
                           "• Цветовая схема: синий/красный/оранжевый/зелёный/розовый\n\n" +
                           "⚙️ СИСТЕМА:\n" +
                           "• Интервал пинга: 1000ms | RTSS только для активных окон\n" +
                           "• Автоматическое отслеживание | Запуск свёрнутым\n" +
                           "• Multi-adapter режим | IPv6 поддержка\n" +
                           "• TCP ping + ICMP fallback | STUN включен\n\n" +
                           "🔧 ПРОИЗВОДИТЕЛЬНОСТЬ:\n" +
                           "• Все сглаживания включены | Гибридный PCAP+Windows\n" +
                           "• Virtual mode (1000 строк) | Ring buffer 10K\n" +
                           "• PCAP оптимизация: 8MB kernel buffer, 4KB min copy\n" +
                           "• UI: 60Hz обработка, batch size 10\n\n" +
                           "🛡️ VPN & БЕЗОПАСНОСТЬ:\n" +
                           "• Простой VPN bypass включен\n" +
                           "• Сложный VPN bypass включен (IP Helper API)\n" +
                           "• Оба режима работают совместно\n\n" +
                           "🎯 ДЕТЕКЦИЯ СПАЙКОВ (Stage 4):\n" +
                           "• Ping/Tickrate/Ticktime мониторинг\n" +
                           "• Порог: 150ms | Чувствительность: низкая\n" +
                           "• EMA Alpha: 0.050 | EW-Sigma: 0.020\n" +
                           "• Множитель: 3.0 | Гистерезис: 0.70\n" +
                           "• Период тишины: 2000ms | Выборка: 30\n" +
                           "• Мин. энергия: 2.0 | Автокалибровка включена\n" +
                           "• Звуковые и Discord алерты (30s cooldown)",
                           "Ваши точные настройки применены!", 
                           MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void InitSpikeDetectionCombos()
        {
            // Инициализация ComboBox для чувствительности
            cmbSpikeSensitivity.Items.Clear();
            cmbSpikeSensitivity.Items.AddRange(new object[] { "very_low", "low", "medium", "high", "auto (в разработке)" });
            
            // Инициализация ComboBox для режима отображения
            cmbSpikeDisplayMode.Items.Clear();
            cmbSpikeDisplayMode.Items.AddRange(new object[] { "off", "value", "bar", "both" });
        }

        private void LoadSpikeDetectionSettings()
        {
            try
            {
                // Основные настройки
                chkSpikeDetectionEnable.Checked = App.settingsManager.GetOption("spikes.enable", "True", "ADVANCED") == "True";

                // Метрики
                var metrics = App.settingsManager.GetOption("spikes.metrics", "ping,tickrate", "ADVANCED").ToLowerInvariant();
                chkSpikeMetricPing.Checked = metrics.Contains("ping");
                chkSpikeMetricTickrate.Checked = metrics.Contains("tickrate");
                chkSpikeMetricTicktime.Checked = metrics.Contains("ticktime");

                // Режим отображения
                var display = App.settingsManager.GetOption("spikes.display", "both", "ADVANCED");
                cmbSpikeDisplayMode.SelectedItem = cmbSpikeDisplayMode.Items.Cast<string>().Contains(display) ? display : "both";

                // Чувствительность
                var sensitivity = App.settingsManager.GetOption("spikes.sensitivity", "medium", "ADVANCED");
                // Поддержка "(в разработке)" в UI
                var displaySensitivity = sensitivity == "auto" ? "auto (в разработке)" : sensitivity;
                cmbSpikeSensitivity.SelectedItem = cmbSpikeSensitivity.Items.Cast<string>().Contains(displaySensitivity) ? displaySensitivity : "medium";

                // Минимальная длительность
                int minDuration = SafeInt(App.settingsManager.GetOption("spikes.min_hold_ms", "120", "ADVANCED"), 120);
                numSpikeMinDuration.Value = Math.Max(numSpikeMinDuration.Minimum, Math.Min(numSpikeMinDuration.Maximum, minDuration));

                // Размер истории
                int historySize = SafeInt(App.settingsManager.GetOption("spikes.history_size", "1000", "ADVANCED"), 1000);
                numSpikeHistorySize.Value = Math.Max(numSpikeHistorySize.Minimum, Math.Min(numSpikeHistorySize.Maximum, historySize));

                // Автокалибровка
                chkSpikeAutoCalibration.Checked = App.settingsManager.GetOption("spikes.auto.enable", "True", "ADVANCED") == "True";

                // Ручная настройка
                chkSpikeManualSettings.Checked = App.settingsManager.GetOption("spikes.manual_mode", "False", "ADVANCED") == "True";
                ToggleSpikeSettingsMode(chkSpikeManualSettings.Checked);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки настроек спайк-детекции: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveSpikeDetectionSettings()
        {
            try
            {
                // Основные настройки
                App.settingsManager.SetOption("spikes.enable", chkSpikeDetectionEnable.Checked.ToString(), "ADVANCED");

                // Метрики
                var metrics = string.Join(",", new[] {
                    chkSpikeMetricPing.Checked ? "ping" : null,
                    chkSpikeMetricTickrate.Checked ? "tickrate" : null,
                    chkSpikeMetricTicktime.Checked ? "ticktime" : null
                }.Where(x => x != null));
                if (string.IsNullOrEmpty(metrics)) metrics = "ping"; // На всякий случай
                App.settingsManager.SetOption("spikes.metrics", metrics, "ADVANCED");

                // Режим отображения
                var display = (cmbSpikeDisplayMode.SelectedItem as string) ?? "both";
                App.settingsManager.SetOption("spikes.display", display, "ADVANCED");

                // Чувствительность
                var sensitivityUI = (cmbSpikeSensitivity.SelectedItem as string) ?? "medium";
                // Убираем "(в разработке)" при сохранении
                var sensitivity = sensitivityUI.Replace(" (в разработке)", "");
                App.settingsManager.SetOption("spikes.sensitivity", sensitivity, "ADVANCED");

                // Минимальная длительность
                App.settingsManager.SetOption("spikes.min_hold_ms", ((int)numSpikeMinDuration.Value).ToString(), "ADVANCED");

                // Размер истории
                App.settingsManager.SetOption("spikes.history_size", ((int)numSpikeHistorySize.Value).ToString(), "ADVANCED");

                // Автокалибровка
                App.settingsManager.SetOption("spikes.auto.enable", chkSpikeAutoCalibration.Checked.ToString(), "ADVANCED");

                // Ручная настройка
                App.settingsManager.SetOption("spikes.manual_mode", chkSpikeManualSettings.Checked.ToString(), "ADVANCED");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения настроек спайк-детекции: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static int SafeInt(string s, int def)
        {
            return int.TryParse(s, out var v) ? v : def;
        }

        /// <summary>
        /// Переключает режим настроек спайк-детекции между пресетами и ручным режимом
        /// </summary>
        private void ToggleSpikeSettingsMode(bool manualMode)
        {
            // Контролы основной секции "Детекция спайков" (groupBoxSpikeDetection)
            // Отключаем все, кроме chkSpikeDetectionEnable и chkSpikeManualSettings
            foreach (Control ctrl in groupBoxSpikeDetection.Controls)
            {
                if (ctrl != chkSpikeDetectionEnable && ctrl != chkSpikeManualSettings)
                {
                    ctrl.Enabled = !manualMode;
                }
            }

            // Контролы расширенной ручной настройки (groupBoxSpikeAdvanced)
            // Включаем все контролы, когда манульный режим активен
            if (groupBoxSpikeAdvanced != null)
            {
                groupBoxSpikeAdvanced.Enabled = manualMode;
            }
        }

        /// <summary>
        /// Обработчик переключения чекбокса "Ручная настройка"
        /// </summary>
        private void chkSpikeManualSettings_CheckedChanged(object sender, EventArgs e)
        {
            ToggleSpikeSettingsMode(chkSpikeManualSettings.Checked);
        }

        #region Stage 4: Advanced Spike Detection Settings

        /// <summary>
        /// Загружает расширенные настройки детекции спайков (Stage 4)
        /// </summary>
        private void LoadAdvancedSpikeSettings()
        {
            try
            {
                // EMA параметры
                numEmaAlpha.Value = SafeDecimal(App.settingsManager.GetOption("spikes.ema_alpha", "0.1", "ADVANCED"), 0.1m);
                
                // EW-Sigma параметры
                numEwSigmaAlpha.Value = SafeDecimal(App.settingsManager.GetOption("spikes.ew_sigma_alpha", "0.05", "ADVANCED"), 0.05m);
                
                // Множитель чувствительности
                numSensitivityMultiplier.Value = SafeDecimal(App.settingsManager.GetOption("spikes.sensitivity_multiplier", "2.0", "ADVANCED"), 2.0m);
                
                // Гистерезис
                numHysteresisRatio.Value = SafeDecimal(App.settingsManager.GetOption("spikes.hysteresis_ratio", "0.8", "ADVANCED"), 0.8m);
                
                // Рефракторный период
                numRefractoryPeriod.Value = SafeDecimal(App.settingsManager.GetOption("spikes.refractory_period_ms", "1000", "ADVANCED"), 1000m);
                
                // Минимальная энергия спайка
                numMinEnergyThreshold.Value = SafeDecimal(App.settingsManager.GetOption("spikes.min_energy_threshold", "1.0", "ADVANCED"), 1.0m);
                
                // Размер окна инициализации
                numInitWindowSize.Value = SafeDecimal(App.settingsManager.GetOption("spikes.init_window_size", "20", "ADVANCED"), 20m);

                // Подключаем обработчики кнопок пресетов
                WireAdvancedSpikeButtons();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки расширенных настроек спайк-детекции: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Сохраняет расширенные настройки детекции спайков (Stage 4)
        /// </summary>
        private void SaveAdvancedSpikeSettings()
        {
            try
            {
                // EMA параметры
                App.settingsManager.SetOption("spikes.ema_alpha", SettingsManager.ToInvariantString((float)numEmaAlpha.Value), "ADVANCED");
                
                // EW-Sigma параметры
                App.settingsManager.SetOption("spikes.ew_sigma_alpha", SettingsManager.ToInvariantString((float)numEwSigmaAlpha.Value), "ADVANCED");
                
                // Множитель чувствительности
                App.settingsManager.SetOption("spikes.sensitivity_multiplier", SettingsManager.ToInvariantString((float)numSensitivityMultiplier.Value), "ADVANCED");
                
                // Гистерезис (зажимаем в разумные рамки 0.5-0.95)
                float hysteresisValue = Math.Max(0.5f, Math.Min(0.95f, (float)numHysteresisRatio.Value));
                App.settingsManager.SetOption("spikes.hysteresis_ratio", SettingsManager.ToInvariantString(hysteresisValue), "ADVANCED");
                
                // Рефракторный период
                App.settingsManager.SetOption("spikes.refractory_period_ms", SettingsManager.ToInvariantString((int)numRefractoryPeriod.Value), "ADVANCED");
                
                // Минимальная энергия спайка
                App.settingsManager.SetOption("spikes.min_energy_threshold", SettingsManager.ToInvariantString((float)numMinEnergyThreshold.Value), "ADVANCED");
                
                // Размер окна инициализации
                App.settingsManager.SetOption("spikes.init_window_size", SettingsManager.ToInvariantString((int)numInitWindowSize.Value), "ADVANCED");

                // Уведомляем SpikeDetectionManager об изменении настроек
                Classes.SpikeDetection.SpikeDetectionManager.UpdateSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения расширенных настроек спайк-детекции: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Подключает обработчики событий кнопок для расширенных настроек спайков
        /// </summary>
        private void WireAdvancedSpikeButtons()
        {
            try
            {
                // Сброс к значениям по умолчанию
                if (btnResetSpikeDefaults != null)
                {
                    btnResetSpikeDefaults.Click -= btnResetSpikeDefaults_Click;
                    btnResetSpikeDefaults.Click += btnResetSpikeDefaults_Click;
                }

                // Пресеты
                if (btnSpikePresetsSensitive != null)
                {
                    btnSpikePresetsSensitive.Click -= btnSpikePresetsSensitive_Click;
                    btnSpikePresetsSensitive.Click += btnSpikePresetsSensitive_Click;
                }

                if (btnSpikePresetsBalanced != null)
                {
                    btnSpikePresetsBalanced.Click -= btnSpikePresetsBalanced_Click;
                    btnSpikePresetsBalanced.Click += btnSpikePresetsBalanced_Click;
                }

                if (btnSpikePresetsConservative != null)
                {
                    btnSpikePresetsConservative.Click -= btnSpikePresetsConservative_Click;
                    btnSpikePresetsConservative.Click += btnSpikePresetsConservative_Click;
                }
            }
            catch
            {
                // Игнорируем ошибки подключения обработчиков
            }
        }

        /// <summary>
        /// Обработчик кнопки сброса настроек спайков к значениям по умолчанию
        /// </summary>
        private void btnResetSpikeDefaults_Click(object sender, EventArgs e)
        {
            try
            {
                numEmaAlpha.Value = 0.1m;
                numEwSigmaAlpha.Value = 0.05m;
                numSensitivityMultiplier.Value = 2.0m;
                numHysteresisRatio.Value = 0.8m;
                numRefractoryPeriod.Value = 1000m;
                numMinEnergyThreshold.Value = 1.0m;
                numInitWindowSize.Value = 20m;
                
                MessageBox.Show("Настройки сброшены к значениям по умолчанию", "Сброс настроек", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сброса настроек: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Обработчик кнопки применения чувствительного пресета
        /// </summary>
        private void btnSpikePresetsSensitive_Click(object sender, EventArgs e)
        {
            try
            {
                // Чувствительный режим: быстрая реакция, низкие пороги
                numEmaAlpha.Value = 0.2m;              // Быстрая адаптация базовой линии
                numEwSigmaAlpha.Value = 0.1m;          // Быстрая адаптация стандартного отклонения
                numSensitivityMultiplier.Value = 1.5m; // Низкий порог детекции
                numHysteresisRatio.Value = 0.9m;       // Небольшой гистерезис
                numRefractoryPeriod.Value = 500m;      // Короткий период тишины
                numMinEnergyThreshold.Value = 0.5m;    // Низкая минимальная энергия
                numInitWindowSize.Value = 15m;         // Небольшое окно инициализации

                MessageBox.Show("Применен чувствительный пресет - быстрая реакция на спайки", "Пресет применен", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка применения пресета: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Обработчик кнопки применения сбалансированного пресета
        /// </summary>
        private void btnSpikePresetsBalanced_Click(object sender, EventArgs e)
        {
            try
            {
                // Сбалансированный режим: значения по умолчанию с небольшими корректировками
                numEmaAlpha.Value = 0.1m;              // Умеренная адаптация базовой линии
                numEwSigmaAlpha.Value = 0.05m;         // Умеренная адаптация стандартного отклонения
                numSensitivityMultiplier.Value = 2.0m; // Средний порог детекции
                numHysteresisRatio.Value = 0.8m;       // Умеренный гистерезис
                numRefractoryPeriod.Value = 1000m;     // Средний период тишины
                numMinEnergyThreshold.Value = 1.0m;    // Средняя минимальная энергия
                numInitWindowSize.Value = 20m;         // Стандартное окно инициализации

                MessageBox.Show("Применен сбалансированный пресет - оптимальный баланс", "Пресет применен", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка применения пресета: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Обработчик кнопки применения консервативного пресета
        /// </summary>
        private void btnSpikePresetsConservative_Click(object sender, EventArgs e)
        {
            try
            {
                // Консервативный режим: медленная реакция, высокие пороги
                numEmaAlpha.Value = 0.05m;             // Медленная адаптация базовой линии
                numEwSigmaAlpha.Value = 0.02m;         // Медленная адаптация стандартного отклонения
                numSensitivityMultiplier.Value = 3.0m; // Высокий порог детекции
                numHysteresisRatio.Value = 0.7m;       // Большой гистерезис
                numRefractoryPeriod.Value = 2000m;     // Длинный период тишины
                numMinEnergyThreshold.Value = 2.0m;    // Высокая минимальная энергия
                numInitWindowSize.Value = 30m;         // Большое окно инициализации

                MessageBox.Show("Применен консервативный пресет - минимум ложных срабатываний", "Пресет применен", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка применения пресета: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Безопасное преобразование строки в decimal с fallback значением
        /// </summary>
        private static decimal SafeDecimal(string s, decimal def)
        {
            return decimal.TryParse(s, out var v) ? v : def;
        }

        #endregion Stage 4: Advanced Spike Detection Settings
        
        #region Stage 8: Advanced Alerting System
        
        /// <summary>
        /// Загружает настройки алертов
        /// </summary>
        private void LoadAlertSettings()
        {
            try
            {
                // Основные настройки алертов
                chkAlertSoundEnabled.Checked = App.settingsManager.GetOption("alert_sound_enabled", "False", "ADVANCED") == "True";
                chkAlertDiscordEnabled.Checked = App.settingsManager.GetOption("alert_discord_enabled", "False", "ADVANCED") == "True";
                txtAlertDiscordWebhook.Text = App.settingsManager.GetOption("alert_discord_webhook", "", "ADVANCED");
                numAlertCooldown.Value = decimal.Parse(App.settingsManager.GetOption("alert_cooldown_seconds", "30", "ADVANCED"));
                
                // Пути к звуковым файлам
                txtAlertPingSoundPath.Text = App.settingsManager.GetOption("alert_sound_pingspike_path", "", "ADVANCED");
                txtAlertTickrateSoundPath.Text = App.settingsManager.GetOption("alert_sound_tickratespike_path", "", "ADVANCED");
                txtAlertTicktimeSoundPath.Text = App.settingsManager.GetOption("alert_sound_ticktimespike_path", "", "ADVANCED");
                
                // Подписываемся на события кнопок
                btnTestDiscordAlert.Click += BtnTestDiscordAlert_Click;
                btnTestSoundAlert.Click += BtnTestSoundAlert_Click;
                btnBrowsePingSound.Click += BtnBrowsePingSound_Click;
                btnBrowseTickrateSound.Click += BtnBrowseTickrateSound_Click;
                btnBrowseTicktimeSound.Click += BtnBrowseTicktimeSound_Click;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[LoadAlertSettings] Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Сохраняет настройки алертов
        /// </summary>
        private void SaveAlertSettings()
        {
            try
            {
                // Основные настройки алертов
                App.settingsManager.SetOption("alert_sound_enabled", chkAlertSoundEnabled.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("alert_discord_enabled", chkAlertDiscordEnabled.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("alert_discord_webhook", txtAlertDiscordWebhook.Text, "ADVANCED");
                App.settingsManager.SetOption("alert_cooldown_seconds", SettingsManager.ToInvariantString((int)numAlertCooldown.Value), "ADVANCED");
                
                // Пути к звуковым файлам
                App.settingsManager.SetOption("alert_sound_pingspike_path", txtAlertPingSoundPath.Text, "ADVANCED");
                App.settingsManager.SetOption("alert_sound_tickratespike_path", txtAlertTickrateSoundPath.Text, "ADVANCED");
                App.settingsManager.SetOption("alert_sound_ticktimespike_path", txtAlertTicktimeSoundPath.Text, "ADVANCED");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[SaveAlertSettings] Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Тестирует Discord алерт
        /// </summary>
        private async void BtnTestDiscordAlert_Click(object sender, EventArgs e)
        {
            try
            {
                btnTestDiscordAlert.Enabled = false;
                btnTestDiscordAlert.Text = "Отправка...";
                
                // Временно сохраняем настройки для теста
                var oldWebhook = App.settingsManager.GetOption("alert_discord_webhook", "", "ADVANCED");
                var oldEnabled = App.settingsManager.GetOption("alert_discord_enabled", "False", "ADVANCED");
                
                App.settingsManager.SetOption("alert_discord_webhook", txtAlertDiscordWebhook.Text, "ADVANCED");
                App.settingsManager.SetOption("alert_discord_enabled", "True", "ADVANCED");
                
                await Classes.AlertManager.TestAlert(Classes.AlertManager.AlertType.PingSpike);
                
                // Восстанавливаем настройки
                App.settingsManager.SetOption("alert_discord_webhook", oldWebhook, "ADVANCED");
                App.settingsManager.SetOption("alert_discord_enabled", oldEnabled, "ADVANCED");
                
                MessageBox.Show("Тестовое Discord уведомление отправлено!", "Тест", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки Discord алерта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTestDiscordAlert.Enabled = true;
                btnTestDiscordAlert.Text = "Тест Discord";
            }
        }
        
        /// <summary>
        /// Тестирует звуковой алерт
        /// </summary>
        private async void BtnTestSoundAlert_Click(object sender, EventArgs e)
        {
            try
            {
                btnTestSoundAlert.Enabled = false;
                btnTestSoundAlert.Text = "Тест...";
                
                // Временно сохраняем настройки для теста
                var oldEnabled = App.settingsManager.GetOption("alert_sound_enabled", "False", "ADVANCED");
                var oldPath = App.settingsManager.GetOption("alert_sound_pingspike_path", "", "ADVANCED");
                
                App.settingsManager.SetOption("alert_sound_enabled", "True", "ADVANCED");
                App.settingsManager.SetOption("alert_sound_pingspike_path", txtAlertPingSoundPath.Text, "ADVANCED");
                
                await Classes.AlertManager.TestAlert(Classes.AlertManager.AlertType.PingSpike);
                
                // Восстанавливаем настройки
                App.settingsManager.SetOption("alert_sound_enabled", oldEnabled, "ADVANCED");
                App.settingsManager.SetOption("alert_sound_pingspike_path", oldPath, "ADVANCED");
                
                MessageBox.Show("Тестовый звуковой алерт воспроизведен!", "Тест", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка воспроизведения звукового алерта: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                btnTestSoundAlert.Enabled = true;
                btnTestSoundAlert.Text = "Тест звука";
            }
        }
        
        /// <summary>
        /// Обзор звукового файла для ping спайков
        /// </summary>
        private void BtnBrowsePingSound_Click(object sender, EventArgs e)
        {
            BrowseSoundFile(txtAlertPingSoundPath);
        }
        
        /// <summary>
        /// Обзор звукового файла для tickrate спайков
        /// </summary>
        private void BtnBrowseTickrateSound_Click(object sender, EventArgs e)
        {
            BrowseSoundFile(txtAlertTickrateSoundPath);
        }
        
        /// <summary>
        /// Обзор звукового файла для ticktime спайков
        /// </summary>
        private void BtnBrowseTicktimeSound_Click(object sender, EventArgs e)
        {
            BrowseSoundFile(txtAlertTicktimeSoundPath);
        }
        
        /// <summary>
        /// Универсальный метод для обзора звуковых файлов
        /// </summary>
        private void BrowseSoundFile(TextBox targetTextBox)
        {
            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Title = "Выберите звуковой файл";
                    openFileDialog.Filter = "Звуковые файлы (*.wav;*.mp3)|*.wav;*.mp3|Все файлы (*.*)|*.*";
                    openFileDialog.FilterIndex = 1;
                    openFileDialog.RestoreDirectory = true;
                    
                    if (!string.IsNullOrEmpty(targetTextBox.Text) && System.IO.File.Exists(targetTextBox.Text))
                    {
                        openFileDialog.InitialDirectory = System.IO.Path.GetDirectoryName(targetTextBox.Text);
                        openFileDialog.FileName = System.IO.Path.GetFileName(targetTextBox.Text);
                    }
                    
                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        targetTextBox.Text = openFileDialog.FileName;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка выбора файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        #endregion Stage 8: Advanced Alerting System
        
        #region Stage 6: Network Quality Analysis
        
        private void LoadNetworkQualitySettings()
        {
            try
            {
                chkNetworkQualityEnabled.Checked = App.settingsManager.GetOption("network_quality_enabled", "True", "ADVANCED") == "True";
                chkNetworkQualityOverlay.Checked = App.settingsManager.GetOption("network_quality_overlay", "False", "SETTINGS") == "True";
                numQualityHistorySize.Value = decimal.Parse(App.settingsManager.GetOption("quality_history_size", "100", "ADVANCED"));
                numStabilityThreshold.Value = decimal.Parse(App.settingsManager.GetOption("stability_threshold", "0.15", "ADVANCED"));
                numQualityThreshold.Value = decimal.Parse(App.settingsManager.GetOption("quality_threshold", "0.8", "ADVANCED"));
                
                // Инициализируем анализатор если он включен
                if (chkNetworkQualityEnabled.Checked)
                {
                    NetworkQualityAnalyzer.Initialize();
                    
                    // Подписываемся на события анализатора
                    NetworkQualityAnalyzer.QualityChanged += OnQualityChanged;
                    NetworkQualityAnalyzer.QualityRatingChanged += OnQualityRatingChanged;
                    NetworkQualityAnalyzer.PredictionChanged += OnPredictionChanged;
                }
                
                // Устанавливаем обработчики событий
                chkNetworkQualityEnabled.CheckedChanged += ChkNetworkQualityEnabled_CheckedChanged;
                chkNetworkQualityOverlay.CheckedChanged += ChkNetworkQualityOverlay_CheckedChanged;
                numQualityHistorySize.ValueChanged += NumQualityHistorySize_ValueChanged;
                numStabilityThreshold.ValueChanged += NumStabilityThreshold_ValueChanged;
                numQualityThreshold.ValueChanged += NumQualityThreshold_ValueChanged;
                btnResetQualityAnalyzer.Click += BtnResetQualityAnalyzer_Click;
                
                UpdateQualityDisplay();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[LoadNetworkQualitySettings] Error: {ex.Message}");
            }
        }
        
        private void SaveNetworkQualitySettings()
        {
            try
            {
                App.settingsManager.SetOption("network_quality_enabled", chkNetworkQualityEnabled.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("network_quality_overlay", chkNetworkQualityOverlay.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("quality_history_size", SettingsManager.ToInvariantString((int)numQualityHistorySize.Value), "ADVANCED");
                App.settingsManager.SetOption("stability_threshold", SettingsManager.ToInvariantString((float)numStabilityThreshold.Value), "ADVANCED");
                App.settingsManager.SetOption("quality_threshold", SettingsManager.ToInvariantString((float)numQualityThreshold.Value), "ADVANCED");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[SaveNetworkQualitySettings] Error: {ex.Message}");
            }
        }
        
        private void ChkNetworkQualityEnabled_CheckedChanged(object sender, EventArgs e)
        {
            if (chkNetworkQualityEnabled.Checked)
            {
                NetworkQualityAnalyzer.Initialize();
                NetworkQualityAnalyzer.QualityChanged += OnQualityChanged;
                NetworkQualityAnalyzer.QualityRatingChanged += OnQualityRatingChanged;
                NetworkQualityAnalyzer.PredictionChanged += OnPredictionChanged;
            }
            else
            {
                NetworkQualityAnalyzer.QualityChanged -= OnQualityChanged;
                NetworkQualityAnalyzer.QualityRatingChanged -= OnQualityRatingChanged;
                NetworkQualityAnalyzer.PredictionChanged -= OnPredictionChanged;
                NetworkQualityAnalyzer.Clear();
            }
            UpdateQualityDisplay();
        }
        
        private void ChkNetworkQualityOverlay_CheckedChanged(object sender, EventArgs e)
        {
            SaveNetworkQualitySettings();
        }
        
        private void NumQualityHistorySize_ValueChanged(object sender, EventArgs e)
        {
            if (chkNetworkQualityEnabled.Checked)
            {
                NetworkQualityAnalyzer.Initialize(); // Переинициализируем с новыми настройками
            }
        }
        
        private void NumStabilityThreshold_ValueChanged(object sender, EventArgs e)
        {
            if (chkNetworkQualityEnabled.Checked)
            {
                NetworkQualityAnalyzer.Initialize(); // Переинициализируем с новыми настройками  
            }
        }
        
        private void NumQualityThreshold_ValueChanged(object sender, EventArgs e)
        {
            if (chkNetworkQualityEnabled.Checked)
            {
                NetworkQualityAnalyzer.Initialize(); // Переинициализируем с новыми настройками
            }
        }
        
        private void BtnResetQualityAnalyzer_Click(object sender, EventArgs e)
        {
            NetworkQualityAnalyzer.Clear();
            UpdateQualityDisplay();
            MessageBox.Show("Анализатор качества сети сброшен", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void OnQualityChanged(float quality)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<float>(OnQualityChanged), quality);
                return;
            }
            UpdateQualityDisplay();
        }
        
        private void OnQualityRatingChanged(string rating)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(OnQualityRatingChanged), rating);
                return;
            }
            UpdateQualityDisplay();
        }
        
        private void OnPredictionChanged(bool isPredicting, string details)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool, string>(OnPredictionChanged), isPredicting, details);
                return;
            }
            
            if (isPredicting)
            {
                System.Diagnostics.Debug.Print($"[NetworkQuality] Prediction: {details}");
            }
        }
        
        private void UpdateQualityDisplay()
        {
            try
            {
                if (!chkNetworkQualityEnabled.Checked)
                {
                    lblCurrentQuality.Text = "Анализ качества сети отключен";
                    lblQualityRating.Text = "";
                    lblCurrentQuality.ForeColor = System.Drawing.Color.Gray;
                    lblQualityRating.ForeColor = System.Drawing.Color.Gray;
                    chkNetworkQualityOverlay.Enabled = false;
                    return;
                }
                
                chkNetworkQualityOverlay.Enabled = true;
                
                var stats = NetworkQualityAnalyzer.GetDetailedStats();
                
                lblCurrentQuality.Text = $"Качество сети: {(stats.OverallQuality * 100):F0}%";
                lblQualityRating.Text = $"Рейтинг: {stats.QualityRating}";
                
                // Цветовая индикация качества
                if (stats.OverallQuality >= 0.9f)
                {
                    lblQualityRating.ForeColor = System.Drawing.Color.Green;
                }
                else if (stats.OverallQuality >= 0.7f)
                {
                    lblQualityRating.ForeColor = System.Drawing.Color.Orange;
                }
                else
                {
                    lblQualityRating.ForeColor = System.Drawing.Color.Red;
                }
                
                lblCurrentQuality.ForeColor = System.Drawing.Color.Black;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[UpdateQualityDisplay] Error: {ex.Message}");
            }
        }
        
        #endregion Stage 6: Network Quality Analysis

        #region Stage 7: Network Optimizer

        private void LoadNetworkOptimizerSettings()
        {
            try
            {
                chkNetworkOptimizationEnabled.Checked = App.settingsManager.GetOption("network_optimization_enabled", "False", "ADVANCED") == "True";
                numOptimizationThreshold.Value = decimal.Parse(App.settingsManager.GetOption("optimization_threshold", "70", "ADVANCED"));
                numOptimizationInterval.Value = decimal.Parse(App.settingsManager.GetOption("optimization_interval", "5", "ADVANCED"));
                chkAggressiveOptimization.Checked = App.settingsManager.GetOption("aggressive_optimization", "False", "ADVANCED") == "True";
                
                // Подписываемся на события
                btnManualOptimization.Click += BtnManualOptimization_Click;
                btnClearOptimizationHistory.Click += BtnClearOptimizationHistory_Click;
                chkNetworkOptimizationEnabled.CheckedChanged += ChkNetworkOptimizationEnabled_CheckedChanged;
                
                // Обновляем статус
                UpdateOptimizerStatus();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[LoadNetworkOptimizerSettings] Error: {ex.Message}");
            }
        }

        private void SaveNetworkOptimizerSettings()
        {
            try
            {
                App.settingsManager.SetOption("network_optimization_enabled", chkNetworkOptimizationEnabled.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("optimization_threshold", SettingsManager.ToInvariantString((float)numOptimizationThreshold.Value), "ADVANCED");
                App.settingsManager.SetOption("optimization_interval", SettingsManager.ToInvariantString((int)numOptimizationInterval.Value), "ADVANCED");
                App.settingsManager.SetOption("aggressive_optimization", chkAggressiveOptimization.Checked.ToString(), "ADVANCED");
                
                // Применяем настройки к оптимизатору
                if (App.networkOptimizer != null)
                {
                    App.networkOptimizer.SetEnabled(chkNetworkOptimizationEnabled.Checked);
                    App.networkOptimizer.SetQualityThreshold((float)(numOptimizationThreshold.Value / 100));
                    App.networkOptimizer.SetOptimizationInterval((int)numOptimizationInterval.Value);
                    App.networkOptimizer.SetAggressiveMode(chkAggressiveOptimization.Checked);
                    
                    System.Diagnostics.Debug.Print($"[SaveNetworkOptimizerSettings] Applied settings to optimizer");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[SaveNetworkOptimizerSettings] Error: {ex.Message}");
            }
        }

        private void BtnManualOptimization_Click(object sender, EventArgs e)
        {
            try
            {
                if (App.networkOptimizer != null)
                {
                    var task = System.Threading.Tasks.Task.Run(() => App.networkOptimizer.PerformOptimization());
                    btnManualOptimization.Text = "Оптимизация...";
                    btnManualOptimization.Enabled = false;
                    
                    task.ContinueWith(t => 
                    {
                        if (InvokeRequired)
                        {
                            Invoke(new Action(() => 
                            {
                                btnManualOptimization.Text = "Запустить оптимизацию";
                                btnManualOptimization.Enabled = true;
                                UpdateOptimizerStatus();
                            }));
                        }
                    });
                }
                else
                {
                    MessageBox.Show("Оптимизатор не инициализирован", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка запуска оптимизации: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnManualOptimization.Text = "Запустить оптимизацию";
                btnManualOptimization.Enabled = true;
            }
        }

        private void BtnClearOptimizationHistory_Click(object sender, EventArgs e)
        {
            try
            {
                if (App.networkOptimizer != null)
                {
                    App.networkOptimizer.ClearHistory();
                    UpdateOptimizerStatus();
                    MessageBox.Show("История оптимизации очищена", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка очистки истории: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ChkNetworkOptimizationEnabled_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (App.networkOptimizer != null)
                {
                    App.networkOptimizer.SetEnabled(chkNetworkOptimizationEnabled.Checked);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[ChkNetworkOptimizationEnabled_CheckedChanged] Error: {ex.Message}");
            }
        }

        private void UpdateOptimizerStatus()
        {
            try
            {
                if (App.networkOptimizer != null)
                {
                    if (InvokeRequired)
                    {
                        Invoke(new Action(UpdateOptimizerStatus));
                        return;
                    }
                    
                    var stats = App.networkOptimizer.GetStats();
                    lblOptimizationStats.Text = $"Всего оптимизаций: {stats.total}, Успешных: {stats.successful}";
                    if (stats.lastOptimization == DateTime.MinValue)
                    {
                        lblLastOptimization.Text = "Последняя оптимизация: Никогда";
                    }
                    else
                    {
                        lblLastOptimization.Text = $"Последняя оптимизация: {stats.lastOptimization:HH:mm:ss dd.MM.yyyy}";
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[UpdateOptimizerStatus] Error: {ex.Message}");
            }
        }

        #endregion Stage 7: Network Optimizer

        #region Color Zone Settings Methods

        private void LoadColorZoneSettings()
        {
            try
            {
                // Load current profile
                var profile = App.settingsManager.GetColorZoneProfile();
                cmbColorZoneProfile.SelectedItem = profile.Name;
                
                // Load values from profile
                numPingGreen.Value = (decimal)profile.PingGreenMs;
                numPingYellow.Value = (decimal)profile.PingYellowMs;
                numTickrateGreen.Value = (decimal)profile.TickrateGreenRatio;
                numTickrateYellow.Value = (decimal)profile.TickrateYellowRatio;
                numTicktimeGreen.Value = (decimal)profile.TicktimeGreenRatio;
                numTicktimeYellow.Value = (decimal)profile.TicktimeYellowRatio;
                
                // Enable/disable controls based on profile type
                bool isCustom = profile.Name == "Custom";
                numPingGreen.Enabled = isCustom;
                numPingYellow.Enabled = isCustom;
                numTickrateGreen.Enabled = isCustom;
                numTickrateYellow.Enabled = isCustom;
                numTicktimeGreen.Enabled = isCustom;
                numTicktimeYellow.Enabled = isCustom;
                
                // Setup events
                cmbColorZoneProfile.SelectedIndexChanged += CmbColorZoneProfile_SelectedIndexChanged;
                btnResetColorZones.Click += BtnResetColorZones_Click;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[LoadColorZoneSettings] Error: {ex.Message}");
            }
        }

        private void SaveColorZoneSettings()
        {
            try
            {
                string selectedProfile = cmbColorZoneProfile.SelectedItem?.ToString() ?? "Medium";
                
                if (selectedProfile == "Custom")
                {
                    // Save custom values
                    App.settingsManager.SetCustomColorZones(
                        (float)numPingGreen.Value,
                        (float)numPingYellow.Value,
                        (float)numTickrateGreen.Value,
                        (float)numTickrateYellow.Value,
                        (float)numTicktimeGreen.Value,
                        (float)numTicktimeYellow.Value
                    );
                }
                else
                {
                    // Save selected profile
                    App.settingsManager.SetColorZoneProfile(selectedProfile);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[SaveColorZoneSettings] Error: {ex.Message}");
            }
        }

        private void CmbColorZoneProfile_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                // Защита от COM ошибок при работе с ComboBox
                if (cmbColorZoneProfile.InvokeRequired)
                {
                    cmbColorZoneProfile.Invoke(new Action(() => CmbColorZoneProfile_SelectedIndexChanged(sender, e)));
                    return;
                }

                string selectedProfile = cmbColorZoneProfile.SelectedItem?.ToString() ?? "Medium";
                System.Diagnostics.Debug.Print($"[ColorZoneProfile] Selected: {selectedProfile}");
                
                var profile = ColorZoneProfile.GetProfile(selectedProfile);
                
                // Update numeric controls с защитой от COM ошибок
                Application.DoEvents(); // Обрабатываем pending UI events
                
                numPingGreen.Value = (decimal)profile.PingGreenMs;
                numPingYellow.Value = (decimal)profile.PingYellowMs;
                numTickrateGreen.Value = (decimal)profile.TickrateGreenRatio;
                numTickrateYellow.Value = (decimal)profile.TickrateYellowRatio;
                numTicktimeGreen.Value = (decimal)profile.TicktimeGreenRatio;
                numTicktimeYellow.Value = (decimal)profile.TicktimeYellowRatio;
                
                // Enable/disable controls
                bool isCustom = selectedProfile == "Custom";
                numPingGreen.Enabled = isCustom;
                numPingYellow.Enabled = isCustom;
                numTickrateGreen.Enabled = isCustom;
                numTickrateYellow.Enabled = isCustom;
                numTicktimeGreen.Enabled = isCustom;
                numTicktimeYellow.Enabled = isCustom;
                
                System.Diagnostics.Debug.Print($"[ColorZoneProfile] Updated UI for: {selectedProfile}");
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                System.Diagnostics.Debug.Print($"[CmbColorZoneProfile_SelectedIndexChanged] COM Error (ignored): {comEx.Message}");
                // COM ошибки с ComboBox можно игнорировать - они не критичны
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[CmbColorZoneProfile_SelectedIndexChanged] Error: {ex.Message}");
            }
        }

        private void BtnResetColorZones_Click(object sender, EventArgs e)
        {
            try
            {
                cmbColorZoneProfile.SelectedItem = "Medium";
                var profile = ColorZoneProfile.GetProfile("Medium");
                
                numPingGreen.Value = (decimal)profile.PingGreenMs;
                numPingYellow.Value = (decimal)profile.PingYellowMs;
                numTickrateGreen.Value = (decimal)profile.TickrateGreenRatio;
                numTickrateYellow.Value = (decimal)profile.TickrateYellowRatio;
                numTicktimeGreen.Value = (decimal)profile.TicktimeGreenRatio;
                numTicktimeYellow.Value = (decimal)profile.TicktimeYellowRatio;
                
                MessageBox.Show("Color zone settings reset to Medium profile defaults.", "Reset Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting color zones: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region Extended Overlay Methods
        
        /// <summary>
        /// Инициализирует блок расширенной информации для оверлея
        /// </summary>
        private void InitializeExtendedOverlayControls()
        {
            // Контролы теперь создаются в Designer.cs
            // Метод оставлен для совместимости
        }
        
        /// <summary>
        /// Загружает настройки расширенной информации из конфига
        /// </summary>
        private void LoadExtendedOverlaySettings()
        {
            if (App.settingsManager == null) return;
            
            try
            {
                // Базовые настройки (включены по умолчанию для удобства)
                chkShowActiveProcess.Checked = App.settingsManager.GetOption("show_active_process", "True", "EXTENDED") == "True";
                chkShowSessionTime.Checked = App.settingsManager.GetOption("show_session_time", "True", "EXTENDED") == "True";
                
                // Расширенные настройки (выключены по умолчанию)
                chkShowExternalIP.Checked = App.settingsManager.GetOption("show_external_ip", "False", "EXTENDED") == "True";
                chkShowSessionStats.Checked = App.settingsManager.GetOption("show_session_stats", "False", "EXTENDED") == "True";
                chkShowServerInfo.Checked = App.settingsManager.GetOption("show_server_info", "False", "EXTENDED") == "True";
                chkShowPacketCounters.Checked = App.settingsManager.GetOption("show_packet_counters", "False", "EXTENDED") == "True";
                chkShowConnectionType.Checked = App.settingsManager.GetOption("show_connection_type", "False", "EXTENDED") == "True";
                
                // Диагностика (выключена по умолчанию)
                chkShowDiagnosticInfo.Checked = App.settingsManager.GetOption("show_diagnostic_info", "False", "EXTENDED") == "True";
                
                // TODO: Добавить контролы для TTL публичного IP и FPS оверлея в Designer
                // TTL для публичного IP (30 минут по умолчанию) - будет добавлено позже
                //if (numExternalIPTtl != null)
                //{
                //    numExternalIPTtl.Value = decimal.Parse(App.settingsManager.GetOption("external_ip_ttl_min", "30", "EXTENDED"));
                //}
                
                // FPS оверлея (выключен по умолчанию) - будет добавлено позже
                //if (chkShowOverlayFps != null)
                //{
                //    chkShowOverlayFps.Checked = App.settingsManager.GetOption("show_overlay_fps", "False", "EXTENDED") == "True";
                //}
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[AdvancedSettings] Error loading extended overlay settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Сохраняет настройки расширенной информации в конфиг
        /// </summary>
        private void SaveExtendedOverlaySettings()
        {
            if (App.settingsManager == null) return;
            
            try
            {
                App.settingsManager.SetOption("show_active_process", chkShowActiveProcess.Checked ? "True" : "False", "EXTENDED");
                App.settingsManager.SetOption("show_session_time", chkShowSessionTime.Checked ? "True" : "False", "EXTENDED");
                App.settingsManager.SetOption("show_external_ip", chkShowExternalIP.Checked ? "True" : "False", "EXTENDED");
                App.settingsManager.SetOption("show_session_stats", chkShowSessionStats.Checked ? "True" : "False", "EXTENDED");
                App.settingsManager.SetOption("show_server_info", chkShowServerInfo.Checked ? "True" : "False", "EXTENDED");
                App.settingsManager.SetOption("show_packet_counters", chkShowPacketCounters.Checked ? "True" : "False", "EXTENDED");
                App.settingsManager.SetOption("show_connection_type", chkShowConnectionType.Checked ? "True" : "False", "EXTENDED");
                App.settingsManager.SetOption("show_diagnostic_info", chkShowDiagnosticInfo.Checked ? "True" : "False", "EXTENDED");
                
                // TODO: Добавить сохранение TTL и FPS после добавления контролов в Designer
                // Сохраняем TTL для публичного IP - будет добавлено позже
                //if (numExternalIPTtl != null)
                //{
                //    App.settingsManager.SetOption("external_ip_ttl_min", numExternalIPTtl.Value.ToString(), "EXTENDED");
                //}
                
                // Сохраняем настройку FPS оверлея - будет добавлено позже
                //if (chkShowOverlayFps != null)
                //{
                //    App.settingsManager.SetOption("show_overlay_fps", chkShowOverlayFps.Checked ? "True" : "False", "EXTENDED");
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AdvancedSettings] Error saving extended overlay settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Загружает настройки графика тикрейта из конфига
        /// </summary>
        private void LoadTickrateChartSettings()
        {
            if (App.settingsManager == null) return;
            
            try
            {
                // Основные настройки
                chkTickrateChartEnabled.Checked = App.settingsManager.GetOption("tickrate_chart_enabled", "True", "TICKRATE_CHART") == "True";
                chkTickrateChartPerServer.Checked = App.settingsManager.GetOption("tickrate_chart_per_server", "True", "TICKRATE_CHART") == "True";
                chkTickrateChartCompression.Checked = App.settingsManager.GetOption("tickrate_chart_compression", "True", "TICKRATE_CHART") == "True";
                chkTickrateChartTimeScale.Checked = App.settingsManager.GetOption("tickrate_chart_time_scale", "False", "TICKRATE_CHART") == "True";
                chkTickrateChartTrimming.Checked = App.settingsManager.GetOption("tickrate_chart_trimming", "True", "TICKRATE_CHART") == "True";
                
                // Режим графика
                string chartMode = App.settingsManager.GetOption("tickrate_chart_mode", "Простой график (точки)", "TICKRATE_CHART");
                if (cmbTickrateChartMode.Items.Contains(chartMode))
                {
                    cmbTickrateChartMode.SelectedItem = chartMode;
                }
                else
                {
                    cmbTickrateChartMode.SelectedIndex = 0; // Простой график по умолчанию
                }
                
                // Численные настройки
                numTickrateChartMaxPoints.Value = decimal.Parse(App.settingsManager.GetOption("tickrate_chart_max_points", "1000", "TICKRATE_CHART"));
                numTickrateChartHistoryHours.Value = decimal.Parse(App.settingsManager.GetOption("tickrate_chart_history_hours", "24", "TICKRATE_CHART"));
                
                // Подключение обработчиков событий
                chkTickrateChartEnabled.CheckedChanged += OnTickrateChartSettingsChanged;
                cmbTickrateChartMode.SelectedIndexChanged += OnTickrateChartSettingsChanged;
                chkTickrateChartPerServer.CheckedChanged += OnTickrateChartSettingsChanged;
                chkTickrateChartCompression.CheckedChanged += OnTickrateChartSettingsChanged;
                chkTickrateChartTimeScale.CheckedChanged += OnTickrateChartSettingsChanged;
                chkTickrateChartTrimming.CheckedChanged += OnTickrateChartSettingsChanged;
                numTickrateChartMaxPoints.ValueChanged += OnTickrateChartSettingsChanged;
                numTickrateChartHistoryHours.ValueChanged += OnTickrateChartSettingsChanged;
                btnTickrateChartReset.Click += OnTickrateChartReset;
                
                // Обновляем состояние контролов
                UpdateTickrateChartControlsState();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[AdvancedSettings] Error loading tickrate chart settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Сохраняет настройки графика тикрейта в конфиг
        /// </summary>
        private void SaveTickrateChartSettings()
        {
            if (App.settingsManager == null) return;
            
            try
            {
                App.settingsManager.SetOption("tickrate_chart_enabled", chkTickrateChartEnabled.Checked ? "True" : "False", "TICKRATE_CHART");
                App.settingsManager.SetOption("tickrate_chart_per_server", chkTickrateChartPerServer.Checked ? "True" : "False", "TICKRATE_CHART");
                App.settingsManager.SetOption("tickrate_chart_compression", chkTickrateChartCompression.Checked ? "True" : "False", "TICKRATE_CHART");
                App.settingsManager.SetOption("tickrate_chart_time_scale", chkTickrateChartTimeScale.Checked ? "True" : "False", "TICKRATE_CHART");
                App.settingsManager.SetOption("tickrate_chart_trimming", chkTickrateChartTrimming.Checked ? "True" : "False", "TICKRATE_CHART");
                App.settingsManager.SetOption("tickrate_chart_mode", cmbTickrateChartMode.SelectedItem?.ToString() ?? "Простой график (точки)", "TICKRATE_CHART");
                App.settingsManager.SetOption("tickrate_chart_max_points", numTickrateChartMaxPoints.Value.ToString(), "TICKRATE_CHART");
                App.settingsManager.SetOption("tickrate_chart_history_hours", numTickrateChartHistoryHours.Value.ToString(), "TICKRATE_CHART");
                
                // Применяем настройки к системе
                ApplyTickrateChartSettings();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print($"[AdvancedSettings] Error saving tickrate chart settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Обновляет состояние контролов графика тикрейта
        /// </summary>
        private void UpdateTickrateChartControlsState()
        {
            bool isEnabled = chkTickrateChartEnabled.Checked;
            bool isDisabled = cmbTickrateChartMode.SelectedItem?.ToString() == "Отключен";
            
            // Включение/отключение контролов в зависимости от основного флага
            cmbTickrateChartMode.Enabled = isEnabled;
            chkTickrateChartPerServer.Enabled = isEnabled && !isDisabled;
            chkTickrateChartCompression.Enabled = isEnabled && !isDisabled;
            chkTickrateChartTimeScale.Enabled = isEnabled && !isDisabled;
            chkTickrateChartTrimming.Enabled = isEnabled && !isDisabled;
            numTickrateChartMaxPoints.Enabled = isEnabled && !isDisabled;
            numTickrateChartHistoryHours.Enabled = isEnabled && !isDisabled;
            btnTickrateChartReset.Enabled = isEnabled;
            
            // Логическая связанность опций
            if (cmbTickrateChartMode.SelectedItem?.ToString() == "Сжатый график")
            {
                chkTickrateChartCompression.Checked = true;
                chkTickrateChartCompression.Enabled = false; // Принудительно включено
            }
            
            if (cmbTickrateChartMode.SelectedItem?.ToString() == "График с временной шкалой")
            {
                chkTickrateChartTimeScale.Checked = true;
                chkTickrateChartTimeScale.Enabled = false; // Принудительно включено
            }
        }
        
        /// <summary>
        /// Обработчик изменения настроек графика тикрейта
        /// </summary>
        private void OnTickrateChartSettingsChanged(object sender, EventArgs e)
        {
            UpdateTickrateChartControlsState();
        }
        
        /// <summary>
        /// Сброс настроек графика тикрейта к умолчанию
        /// </summary>
        private void OnTickrateChartReset(object sender, EventArgs e)
        {
            chkTickrateChartEnabled.Checked = true;
            cmbTickrateChartMode.SelectedIndex = 0; // Простой график
            chkTickrateChartPerServer.Checked = true;
            chkTickrateChartCompression.Checked = true;
            chkTickrateChartTimeScale.Checked = false;
            chkTickrateChartTrimming.Checked = true;
            numTickrateChartMaxPoints.Value = 1000;
            numTickrateChartHistoryHours.Value = 24;
            
            UpdateTickrateChartControlsState();
        }
        
        /// <summary>
        /// Применяет настройки графика тикрейта к системе
        /// </summary>
        private void ApplyTickrateChartSettings()
        {
            // Обновляем константы в TickMeterState на основе настроек
            if (App.meterState != null)
            {
                // Через reflection обновляем константы (если это возможно)
                // Или добавляем свойства в TickMeterState для динамического изменения
                
                // Пока что просто уведомляем о необходимости перезапуска для применения изменений
                // В будущем можно добавить динамическое изменение параметров
            }
        }
        
        #endregion

    }
}