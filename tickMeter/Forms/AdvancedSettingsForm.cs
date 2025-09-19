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
    }
}