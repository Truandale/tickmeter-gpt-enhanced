using System;
using System.Windows.Forms;
using tickMeter.Classes;

namespace tickMeter.Forms
{
    public partial class AdvancedSettingsForm : Form
    {
        public AdvancedSettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                // Live View настройки
                chkLiveMaxRows.Checked = App.settingsManager.GetOption("live_max_rows_enabled", "False", "ADVANCED") == "True";
                liveMaxRowsNumeric.Value = int.Parse(App.settingsManager.GetOption("live_max_rows", "1000", "ADVANCED"));
                
                // RTSS настройки
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
                chkTickrateValueOverlaySmoothing.Checked = App.settingsManager.GetOption("smoothing_tickrate_value_overlay", "False", "ADVANCED") == "True";
                chkTrafficValueOverlaySmoothing.Checked = App.settingsManager.GetOption("smoothing_traffic_value_overlay", "False", "ADVANCED") == "True";
                chkDedupMultiNic.Checked = App.settingsManager.GetOption("dedup_multi_nic", "True", "SETTINGS") == "True";
                chkEnableIPv6.Checked = App.settingsManager.GetOption("enable_ipv6", "True", "SETTINGS") == "True";
                chkRtssOnlyActive.Checked = App.settingsManager.GetOption("rtss_only_active", "True", "SETTINGS") == "True";
                chkStunEnable.Checked = App.settingsManager.GetOption("stun_enable", "True", "SETTINGS") == "True";
                chkShowPingSpikes.Checked = App.settingsManager.GetOption("show_ping_spikes", "True", "ADVANCED") == "True";
                
                // Настройки порогов для спайков пинга
                numPingSpikeThreshold.Value = decimal.Parse(App.settingsManager.GetOption("ping_spike_threshold", "150", "ADVANCED"));
                
                // VPN bypass настройки
                chkVpnBypassBasic.Checked = App.settingsManager.GetOption("vpn_bypass_basic", "False", "ADVANCED") == "True";
                chkVpnBypassAdvanced.Checked = App.settingsManager.GetOption("vpn_bypass_advanced", "False", "ADVANCED") == "True";
                
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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки настроек: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Live View настройки
                App.settingsManager.SetOption("live_max_rows_enabled", chkLiveMaxRows.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("live_max_rows", liveMaxRowsNumeric.Value.ToString(), "ADVANCED");
                
                // RTSS настройки
                App.settingsManager.SetOption("overlay_fps_enabled", chkOverlayFps.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("overlay_fps", overlayFpsNumeric.Value.ToString(), "ADVANCED");
                
                // BPF фильтр
                App.settingsManager.SetOption("bpf_filter_enabled", chkBpfFilter.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("capture_filter", captureFilterTextBox.Text, "ADVANCED");
                
                // Capture all adapters
                App.settingsManager.SetOption("capture_all_adapters", chkCaptureAllAdapters.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("ignore_virtual_adapters", chkIgnoreVirtualAdapters.Checked.ToString(), "SETTINGS");

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
                App.settingsManager.SetOption("smoothing_tickrate_value_overlay", chkTickrateValueOverlaySmoothing.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("smoothing_traffic_value_overlay", chkTrafficValueOverlaySmoothing.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("dedup_multi_nic", chkDedupMultiNic.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("enable_ipv6", chkEnableIPv6.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("rtss_only_active", chkRtssOnlyActive.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("stun_enable", chkStunEnable.Checked.ToString(), "SETTINGS");
                App.settingsManager.SetOption("show_ping_spikes", chkShowPingSpikes.Checked.ToString(), "ADVANCED");
                
                // Настройки порогов для спайков пинга
                App.settingsManager.SetOption("ping_spike_threshold", numPingSpikeThreshold.Value.ToString(), "ADVANCED");
                
                // VPN bypass настройки
                App.settingsManager.SetOption("vpn_bypass_basic", chkVpnBypassBasic.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("vpn_bypass_advanced", chkVpnBypassAdvanced.Checked.ToString(), "ADVANCED");
                
                // Performance Optimization Phase 1-3 настройки  
                App.settingsManager.SetOption("anti_reentrancy", chkAntiReentrancy.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("rtss_throttling", chkRtssThrottling.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("pcap_optimization", chkPcapOptimization.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("pcap_kernel_buffer_mb", numPcapKernelBufferMb.Value.ToString(), "ADVANCED");
                App.settingsManager.SetOption("pcap_min_to_copy", numPcapMinToCopy.Value.ToString(), "ADVANCED");
                
                App.settingsManager.SetOption("virtual_mode_listview", chkVirtualModeListView.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("virtual_mode_threshold", numVirtualModeThreshold.Value.ToString(), "ADVANCED");
                App.settingsManager.SetOption("ring_buffer_size", numRingBufferSize.Value.ToString(), "ADVANCED");
                App.settingsManager.SetOption("show_virtual_mode_stats", chkShowVirtualModeStats.Checked.ToString(), "ADVANCED");
                
                App.settingsManager.SetOption("high_priority_threads", chkHighPriorityThreads.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("single_consumer_pattern", chkSingleConsumerPattern.Checked.ToString(), "ADVANCED");
                App.settingsManager.SetOption("ui_processing_rate", numUiProcessingRate.Value.ToString(), "ADVANCED");
                App.settingsManager.SetOption("ui_batch_size", numUiBatchSize.Value.ToString(), "ADVANCED");
                
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

        private void chkTickrateGraphOverlaySmoothing_CheckedChanged(object sender, EventArgs e)
        {

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
                "Установить оптимальные настройки для повседневного использования?\n\n" +
                "Это изменит все параметры на рекомендуемые значения.",
                "Сброс к оптимальным настройкам",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                SetOptimalSettings();
                MessageBox.Show("Оптимальные настройки установлены!", "Успех", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>
        /// Устанавливает оптимальные настройки для повседневного использования
        /// </summary>
        private void SetOptimalSettings()
        {
            // Live View - умеренные настройки
            chkLiveMaxRows.Checked = true;
            liveMaxRowsNumeric.Value = 1000;
            
            // RTSS - отключаем FPS ограничение, используем ping_interval
            chkOverlayFps.Checked = false;
            overlayFpsNumeric.Value = 60;
            
            // BPF фильтр - отключен для простоты
            chkBpfFilter.Checked = false;
            captureFilterTextBox.Text = "ip or ip6";
            
            // Основные настройки - все активные
            chkCaptureAllAdapters.Checked = true;
            chkIgnoreVirtualAdapters.Checked = true;
            chkPingBindToInterface.Checked = true;
            chkPingTcpPrefer.Checked = true;
            chkPingFallbackIcmp.Checked = true;
            chkPingTargetActiveOnly.Checked = true;
            chkTickrateSmoothing.Checked = true;
            chkDedupMultiNic.Checked = true;
            chkEnableIPv6.Checked = true;
            chkRtssOnlyActive.Checked = true;
            chkStunEnable.Checked = true;
            
            // Smoothing - включены для стабильности
            chkPingGraphOverlaySmoothing.Checked = true;
            chkTickrateGraphOverlaySmoothing.Checked = true;
            chkTicktimeGraphOverlaySmoothing.Checked = true;
            chkPingValueOverlaySmoothing.Checked = true;
            chkPingValueGuiSmoothing.Checked = true;
            chkTickrateValueOverlaySmoothing.Checked = true;
            chkTrafficValueOverlaySmoothing.Checked = true;
            
            // Ping Spikes - полезно для геймеров
            chkShowPingSpikes.Checked = true;
            numPingSpikeThreshold.Value = 150;
            
            // VPN bypass - отключен по умолчанию
            chkVpnBypassBasic.Checked = false;
            chkVpnBypassAdvanced.Checked = false;
            
            // === PHASE 1: Anti-reentrancy (ВКЛЮЧЕНЫ) ===
            chkAntiReentrancy.Checked = true;
            chkRtssThrottling.Checked = true;
            chkPcapOptimization.Checked = true;
            
            // === PHASE 2: PCAP optimization (ОПТИМАЛЬНЫЕ ЗНАЧЕНИЯ) ===
            numPcapKernelBufferMb.Value = 8;  // 8MB для хорошей производительности
            numPcapMinToCopy.Value = 4096;    // Оптимальный размер
            
            // === PHASE 3: Virtual Mode & Priorities (СБАЛАНСИРОВАННЫЕ) ===
            chkVirtualModeListView.Checked = true;
            numVirtualModeThreshold.Value = 1000; // Умеренный порог
            numRingBufferSize.Value = 10000;      // Достаточный буфер
            chkShowVirtualModeStats.Checked = true;
            
            // === PHASE 3: Thread Management (КОНСЕРВАТИВНЫЕ) ===
            chkHighPriorityThreads.Checked = true;
            chkSingleConsumerPattern.Checked = false; // Пока экспериментальное
            numUiProcessingRate.Value = 60;           // 60 FPS UI
            numUiBatchSize.Value = 10;                // Оптимальный batch размер
        }
    }
}