using Microsoft.Win32;
using Newtonsoft.Json;
using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Resources;
using System.Threading.Tasks;
using System.Windows.Forms;
using tickMeter.Classes;

namespace tickMeter.Forms
{
    public partial class SettingsForm : Form
    {
        public const float CURRENT_VERSION = 2.0f;

        public string verInfo;
        TagCollection TagsInfo;
        
    // Флаг для предотвращения рекурсивного обновления при программном изменении адаптера
    public bool IsUpdatingAdapter = false;

        public SettingsForm()
        {
            InitializeComponent();
            // Ensure the Advanced Settings button opens the dialog even if Designer wiring is missing
            WireAdvancedSettingsButton();
        }

        /// <summary>
        /// Guarantees Click wiring for the Advanced Settings button even if Designer didn't persist it.
        /// </summary>
        private void WireAdvancedSettingsButton()
        {
            try
            {
                // 1) If field exists and created by Designer
                if (this.btnAdvancedSettings != null)
                {
                    this.btnAdvancedSettings.Click -= btnAdvancedSettings_Click;
                    this.btnAdvancedSettings.Click += btnAdvancedSettings_Click;
                    return;
                }

                // 2) Fallback: find by Name in the control tree
                var found = this.Controls.Find("btnAdvancedSettings", true)
                                          .OfType<Button>()
                                          .FirstOrDefault();
                if (found != null)
                {
                    found.Click -= btnAdvancedSettings_Click;
                    found.Click += btnAdvancedSettings_Click;
                    // If the field exists in partial class, keep a reference handy
                    this.btnAdvancedSettings = found;
                }
            }
            catch
            {
                // No-op: keep UI responsive even if wiring fails in unusual Designer states
            }
        }
        private class TagInfo
        {
            [JsonProperty("name")]
            public float Version { get; set; }
        }

        private class TagCollection
        {
            [JsonProperty("values")]
            public List<TagInfo> Tags { get; set; }
        }

        /// <summary>
        /// Обновляет состояния выпадающего списка адаптеров и поля локального IP на основе настройки capture_all_adapters
        /// (чекбокс перенесён в AdvancedSettingsForm, поэтому здесь только читаем настройку)
        /// </summary>
        private void InitCaptureAllAdaptersState()
        {
            bool captureAllEnabled = App.settingsManager.GetOption("capture_all_adapters", "False", "SETTINGS") == "True";
            bool manualIpUnlocked = App.settingsManager.GetOption("manual_ip_unlocked", "False", "SETTINGS") == "True";
            
            if (adapters_list != null)
                adapters_list.Enabled = !captureAllEnabled;

            // Управление состоянием поля локального IP и кнопки разблокировки
            if (local_ip_textbox != null && btnUnlockLocalIP != null)
            {
                if (captureAllEnabled && !manualIpUnlocked)
                {
                    // Режим мультиадаптера: блокируем поле и показываем кнопку разблокировки
                    local_ip_textbox.Enabled = false;
                    btnUnlockLocalIP.Visible = true;
                    btnUnlockLocalIP.Text = "🔓";
                    
                    // НЕ вызываем автоопределение здесь - IP определяется в StartTracking
                }
                else
                {
                    // Обычный режим или разблокировано вручную: разрешаем редактирование
                    local_ip_textbox.Enabled = true;
                    btnUnlockLocalIP.Visible = captureAllEnabled; // Показываем кнопку только в режиме мультиадаптера
                    btnUnlockLocalIP.Text = "🔒";
                }
            }
        }

        // Удалены методы и обработчики для универсальных чекбоксов — они перенесены в AdvancedSettingsForm

        /// <summary>
        /// Удобный враппер для чтения булевых настроек
        /// </summary>
        private bool GetBool(string key, bool defVal)
        {
            string rawValue = App.settingsManager.GetOption(key, defVal ? "True" : "False", "SETTINGS");
            bool result = rawValue == "True";
            System.Diagnostics.Debug.WriteLine($"GetBool({key}): raw='{rawValue}', result={result}, default={defVal}");
            return result;
        }

        /// <summary>
        /// Удобный враппер для записи булевых настроек
        /// </summary>
        private void SetBool(string key, bool val) =>
            App.settingsManager.SetOption(key, val.ToString());

        public async Task CheckNewVersion()
        {
            await Task.Run(() =>
            {
                try
                {
                    verInfo = new WebClient().DownloadString("https://api.github.com/repos/igentuman/tickmeter/releases/latest");
                    TagsInfo = JsonConvert.DeserializeObject<TagCollection>(verInfo);
                    float lastVersion = 1;
                    foreach(TagInfo ver in TagsInfo.Tags)
                    {
                        if(lastVersion < ver.Version)
                        {
                            lastVersion = ver.Version;
                        }
                    }
                    if(CURRENT_VERSION < lastVersion)
                    {
                        updateLbl.Text += TagsInfo.Tags[TagsInfo.Tags.Count - 1].Version;
                        updateLbl.Visible = true;
                        if(App.settingsManager.GetOption("last_checked_version") != TagsInfo.Tags[TagsInfo.Tags.Count - 1].Version.ToString())
                        {
                            MessageBox.Show(updateLbl.Text, "Update", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Process.Start("https://api.github.com/repos/igentuman/tickmeter/releases/latest");
                        }
                        App.settingsManager.SetOption("last_checked_version", TagsInfo.Tags[TagsInfo.Tags.Count - 1].Version.ToString());
                    }
                }
                catch (Exception ex)
                {
                    Debug.Print($"[SettingsForm] Version check error: {ex.Message}");
                }
                
            });
        }

        private static String HexConverter(Color c)
        {
            return c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }

        public void InitRtss(bool last = false)
        {

            if (File.Exists(App.settingsManager.GetOption("rtss_exe_path")))
            {
                try { RivaTuner.rtss_exe = App.settingsManager.GetOption("rtss_exe_path"); } catch (TypeInitializationException) { /* RTSS.dll отсутствует */ }
                return;
            }

            Object uninstallVal = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\RTSS", "UninstallString", null);
            string RtssPath = "";
            if (uninstallVal != null)
            {
                RtssPath = Path.GetDirectoryName(uninstallVal.ToString().Replace("\"", ""));
                App.settingsManager.SetOption("rtss_exe_path", RtssPath + "/RTSS.exe");
            }
            if (RtssPath == "" && MessageBox.Show("RTSS not found. Download?", "RTSS", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                Process.Start("https://www.guru3d.com/download/rtss-rivatuner-statistics-server-download");
                Close();
            }

            if (!File.Exists(RtssPath) && MessageBox.Show("Find RTSS.exe location?", "RTSS", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                rtss_dialog.InitialDirectory = RtssPath;
                rtss_dialog.ShowDialog();
                if (File.Exists(rtss_dialog.FileName))
                {
                    App.settingsManager.SetOption("rtss_exe_path", rtss_dialog.FileName);
                    try { RivaTuner.rtss_exe = rtss_dialog.FileName; } catch (TypeInitializationException) { /* RTSS.dll отсутствует */ }
                    return;
                }
            }
            settings_rtss_output.Checked = settings_rtss_output.Enabled = false;
        }

        public void ApplyFromConfig()
        {
            System.Diagnostics.Debug.WriteLine("ApplyFromConfig() НАЧАЛО");
            
            // SAFETY: Проверяем инициализацию контролов
            if (settings_chart_checkbox == null)
            {
                System.Diagnostics.Debug.WriteLine("ОШИБКА: settings_chart_checkbox = null. InitializeComponent() не завершился!");
                MessageBox.Show("Критическая ошибка инициализации формы настроек. Контролы не созданы.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            
            // CRITICAL: Перечитываем настройки из файла перед загрузкой в UI
            App.settingsManager.ReloadConfig();
            System.Diagnostics.Debug.WriteLine("ApplyFromConfig() - ReloadConfig() завершен");
            
            settings_chart_checkbox.Checked = App.settingsManager.GetOption("chart") == "True";
            settings_ip_checkbox.Checked = App.settingsManager.GetOption("ip") == "True";
            settings_ping_checkbox.Checked = App.settingsManager.GetOption("ping") == "True";

            settings_traffic_checkbox.Checked = App.settingsManager.GetOption("traffic") == "True";
            packet_drops_checkbox.Checked = App.settingsManager.GetOption("show_packet_drops") == "True";
            settings_rtss_output.Checked = App.settingsManager.GetOption("rtss") == "True";
            settings_tickrate_show.Checked = App.settingsManager.GetOption("tickrate") == "True";
            settings_autodetect_checkbox.Checked = App.settingsManager.GetOption("autodetect") == "True";
            settings_data_send.Checked = App.settingsManager.GetOption("data_send") == "True";
            settings_session_time_checkbox.Checked = App.settingsManager.GetOption("session_time") == "True";
            settings_ticktime_chart.Checked = App.settingsManager.GetOption("ticktime") == "True";
            settings_ping_chart.Checked = App.settingsManager.GetOption("ping_chart") == "True";
            run_minimized.Checked = App.settingsManager.GetOption("run_minimized") == "True";
            run_on_startup.Checked = App.settingsManager.GetOption("run_on_startup") == "True";
            
            // Проверяем и обновляем путь в планировщике если программа была перемещена
            if (run_on_startup.Checked)
            {
                CheckAndUpdateScheduledTaskPath();
            }
            
            ping_ports.Text = App.settingsManager.GetOption("ping_ports");
            ping_interval.Value = App.settingsManager.GetIntOption("ping_interval", 400);
            
            // NEW: обновление состояния UI после загрузки всех настроек
            InitCaptureAllAdaptersState();
            
            // Загружаем последний выбранный адаптер если сохранен
            try
            {
                adapters_list.SelectedIndex = 0;
                string adapterID = App.settingsManager.GetOption("last_selected_adapter");
                if(!adapterID.IsNullOrEmpty())
                {
                    int i = 0;
                    foreach(LivePacketDevice device in App.GetAdapters())
                    {
                        if(device.GetGuid().ToLower().Equals(adapterID))
                        {
                            adapters_list.SelectedIndex = i; break;
                        }
                        i++;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[SettingsForm] Adapter selection restore error: {ex.Message}");
            }
                
            string localIp = App.settingsManager.GetOption("local_ip");
            if(localIp != null && localIp != "")
            {
                local_ip_textbox.Text = localIp;
            }
            ColorLabel.ForeColor = ColorTranslator.FromHtml("#"+ App.settingsManager.GetOption("color_label", "636BDA", "SETTINGS"));
            ColorBad.ForeColor = ColorTranslator.FromHtml("#"+ App.settingsManager.GetOption("color_bad", "FF0000", "SETTINGS"));
            ColorMid.ForeColor = ColorTranslator.FromHtml("#"+ App.settingsManager.GetOption("color_mid", "FF8040", "SETTINGS"));
            ColorGood.ForeColor = ColorTranslator.FromHtml("#"+ App.settingsManager.GetOption("color_good", "008000", "SETTINGS"));
            ColorChart.ForeColor = ColorTranslator.FromHtml("#"+ App.settingsManager.GetOption("color_chart", "FF0080", "SETTINGS"));
            try
            {
                RivaTuner.LabelColor = App.settingsManager.GetOption("color_label");
                RivaTuner.ColorBad = App.settingsManager.GetOption("color_bad");
                RivaTuner.ColorMid = App.settingsManager.GetOption("color_mid");
                RivaTuner.ColorGood = App.settingsManager.GetOption("color_good");
                RivaTuner.ColorChart = App.settingsManager.GetOption("color_chart");
            }
            catch (TypeInitializationException)
            {
                Debug.Print("[RivaTuner] Не удалось загрузить (RTSS.dll отсутствует)");
            }

            App.gui.drops_lbl_val.ForeColor = ColorBad.ForeColor;

            if (App.gui != null)
            {
                App.gui.tickrate_lbl.ForeColor =
                    App.gui.ping_lbl.ForeColor =
                    App.gui.ip_lbl.ForeColor =
                    App.gui.traffic_lbl.ForeColor =
                    App.gui.drops_lbl.ForeColor =
                    App.gui.time_lbl.ForeColor =
                    ColorLabel.ForeColor;
            }
            InitRtss();
            
            // NEW: инициализация состояния чекбокса после загрузки всех настроек
            InitCaptureAllAdaptersState();
            
            // Синхронизируем ComboBox с текущим LocalIP при открытии формы
            SyncAdapterComboBoxToCurrentIP();
        }
        
        /// <summary>
        /// Синхронизирует выбранный адаптер в ComboBox с текущим LocalIP из App.meterState
        /// </summary>
        private void SyncAdapterComboBoxToCurrentIP()
        {
            try
            {
                string currentIP = App.meterState?.LocalIP;
                if (string.IsNullOrEmpty(currentIP)) return;
                if (adapters_list == null || adapters_list.Items.Count == 0) return;
                
                Debug.Print($"[SettingsForm] Syncing adapter ComboBox to current IP: {currentIP}");
                
                var adapters = App.GetAdapters();
                for (int i = 0; i < adapters.Count; i++)
                {
                    string adapterIP = App.GetAdapterAddress(adapters[i]);
                    if (adapterIP == currentIP && adapters_list.SelectedIndex != i)
                    {
                        Debug.Print($"[SettingsForm] Initial sync: ComboBox {adapters_list.SelectedIndex} -> {i} ({currentIP})");
                        
                        IsUpdatingAdapter = true;
                        try
                        {
                            adapters_list.SelectedIndex = i;
                        }
                        finally
                        {
                            IsUpdatingAdapter = false;
                        }
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[SettingsForm] Error syncing adapter ComboBox: {ex.Message}");
            }
        }

        public void SaveToConfig()
        {
            App.settingsManager.BeginBatchUpdate();
            try
            {
                App.settingsManager.SetOption("chart", settings_chart_checkbox.Checked.ToString());
                App.settingsManager.SetOption("ip", settings_ip_checkbox.Checked.ToString());
                App.settingsManager.SetOption("tickrate", settings_tickrate_show.Checked.ToString());
                App.settingsManager.SetOption("ticktime", settings_ticktime_chart.Checked.ToString());
                App.settingsManager.SetOption("ping_chart", settings_ping_chart.Checked.ToString());
                App.settingsManager.SetOption("ping", settings_ping_checkbox.Checked.ToString());
                App.settingsManager.SetOption("ping_interval", SettingsManager.ToInvariantString((int)ping_interval.Value));
                App.settingsManager.SetOption("ping_ports", ping_ports.Text);
                App.settingsManager.SetOption("traffic", settings_traffic_checkbox.Checked.ToString());
                App.settingsManager.SetOption("color_label", HexConverter(ColorLabel.ForeColor));
                App.settingsManager.SetOption("color_bad", HexConverter(ColorBad.ForeColor));
                App.settingsManager.SetOption("color_mid", HexConverter(ColorMid.ForeColor));
                App.settingsManager.SetOption("color_good", HexConverter(ColorGood.ForeColor));
                App.settingsManager.SetOption("color_chart", HexConverter(ColorChart.ForeColor));
                App.settingsManager.SetOption("rtss", settings_rtss_output.Checked.ToString());
                App.settingsManager.SetOption("autodetect", settings_autodetect_checkbox.Checked.ToString());
                App.settingsManager.SetOption("data_send", settings_data_send.Checked.ToString());
                App.settingsManager.SetOption("session_time", settings_session_time_checkbox.Checked.ToString());
                int selectedAdapter = adapters_list.SelectedIndex;
                if(selectedAdapter < 0) selectedAdapter = 0;
                App.settingsManager.SetOption("last_selected_adapter", App.GetAdapters()[selectedAdapter].GetGuid().ToLower());
                App.settingsManager.SetOption("run_minimized", run_minimized.Checked.ToString());
                App.settingsManager.SetOption("run_on_startup", run_on_startup.Checked.ToString());
                App.settingsManager.SetOption("local_ip", local_ip_textbox.Text);
                App.settingsManager.SetOption("show_packet_drops", packet_drops_checkbox.Checked.ToString());
                    
                // Advanced flags сохраняются через AdvancedSettingsForm
            }
            finally
            {
                App.settingsManager.EndBatchUpdate();
            }
        }

        public void SwitchToEnglish()
        {
            ResourceManager eng = Resources.en.ResourceManager;
            Text = eng.GetString("settings");
            settings_rtss_output.Text = eng.GetString(settings_rtss_output.Name);
            settings_log_checkbox.Text = eng.GetString(settings_log_checkbox.Name);
            settings_ping_ports_lbl.Text = eng.GetString(settings_ping_ports_lbl.Name);
            settings_ping_interval_lbl.Text = eng.GetString(settings_ping_interval_lbl.Name);
            settings_ip_checkbox.Text = eng.GetString(settings_ip_checkbox.Name);
            settings_ping_checkbox.Text = eng.GetString(settings_ping_checkbox.Name);
            settings_traffic_checkbox.Text = eng.GetString(settings_traffic_checkbox.Name);
            settings_chart_checkbox.Text = eng.GetString(settings_chart_checkbox.Name);
            settings_session_time_checkbox.Text = eng.GetString(settings_session_time_checkbox.Name);
            settings_ping_chart.Text = eng.GetString(settings_ping_chart.Name);
            settings_autodetect_checkbox.Text = eng.GetString(settings_autodetect_checkbox.Name);
            packet_drops_checkbox.Text = eng.GetString(packet_drops_checkbox.Name);
            ColorLabel.Text = eng.GetString(ColorLabel.Name);
            ColorBad.Text = eng.GetString(ColorBad.Name);
            ColorMid.Text = eng.GetString(ColorMid.Name);
            ColorGood.Text = eng.GetString(ColorGood.Name);
            ColorChart.Text = eng.GetString(ColorChart.Name);
            settings_ticktime_chart.Text = eng.GetString(settings_ticktime_chart.Name);
            settings_tickrate_show.Text = eng.GetString(settings_tickrate_show.Name);
            updateLbl.Text = eng.GetString(updateLbl.Name);
           
            donate_lbl.Text = eng.GetString(donate_lbl.Name);
            settings_data_send.Text = eng.GetString(settings_data_send.Name);
        }

        /// <summary>
        /// NEW: Обработчик кнопки "Сохранить"
        /// Записывает все настройки на диск и закрывает форму
        /// </summary>
        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            // SaveToConfig() уже вызывает SaveConfig() внутри себя
            SaveToConfig();
            // Применяем интервал overlay сразу после сохранения настроек
            App.gui?.ApplyOverlayIntervalFromSettings();
            
            // Закрыть форму настроек
            this.Close();
        }

        /// <summary>
        /// Открывает форму дополнительных настроек
        /// </summary>
        private void btnAdvancedSettings_Click(object sender, EventArgs e)
        {
            try
            {
                var advancedForm = new tickMeter.Forms.AdvancedSettingsForm();
                advancedForm.ShowDialog(this);
                // After dialog closes, reload settings and refresh UI that depends on them
                App.settingsManager.ReloadConfig();
                InitCaptureAllAdaptersState();
                // Применить изменения по overlay FPS без перезапуска
                App.gui?.ApplyOverlayIntervalFromSettings();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка открытия дополнительных настроек: {ex.Message}", 
                              "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LabelsColor_Click(object sender, EventArgs e)
        {
            colorDialog1.ShowDialog();
            ColorLabel.ForeColor = colorDialog1.Color;
            App.gui.tickrate_lbl.ForeColor =
                App.gui.ping_lbl.ForeColor =
                App.gui.ip_lbl.ForeColor =
                App.gui.traffic_lbl.ForeColor =
                ColorLabel.ForeColor;
            SaveToConfig();
            ApplyFromConfig();
        }

        private void ColorBad_Click(object sender, EventArgs e)
        {
            colorDialog1.ShowDialog();
            ColorBad.ForeColor = colorDialog1.Color;
            SaveToConfig();
            ApplyFromConfig();
        }

        private void ColorMid_Click(object sender, EventArgs e)
        {
            colorDialog1.ShowDialog();
            ColorMid.ForeColor = colorDialog1.Color;
            SaveToConfig();
            ApplyFromConfig();
        }

        private void ColorGood_Click(object sender, EventArgs e)
        {
            colorDialog1.ShowDialog();
            ColorGood.ForeColor = colorDialog1.Color;
            SaveToConfig();
            ApplyFromConfig();
        }

        private void SettingsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void possible_risks_lbl_Click(object sender, EventArgs e)
        {
            Process.Start("https://bitbucket.org/dvman8bit/tickmeter/wiki/%D0%92%D0%BE%D0%B7%D0%BC%D0%BE%D0%B6%D0%BD%D1%8B%D0%B5%20%D1%80%D0%B8%D1%81%D0%BA%D0%B8%20%7C%20Possible%20risks");
        }

        private void label8_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.youtube.com/channel/UConzx4k6IVXSs9PsY9Snkbg");
        }

        private void settings_rtss_output_CheckedChanged(object sender, EventArgs e)
        {
            _ = Task.Run(async () => {
                try { 
                    await Task.Run(() => RivaTuner.PrintData(""));
                    this.Invoke(new Action(() => App.gui.UpdateStyle(settings_rtss_output.Checked)));
                } 
                catch (Exception exc) { 
                    this.Invoke(new Action(() => MessageBox.Show(exc.Message))); 
                }
            });
        }

        private void adapters_list_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Игнорируем событие если мы сами программно меняем адаптер
            if (IsUpdatingAdapter)
                return;
                
            if (adapters_list.SelectedIndex > -1)
            {
                // Обновляем IP только если поле не заблокировано или мультиадаптер выключен
                bool captureAllEnabled = App.settingsManager.GetOption("capture_all_adapters", "False", "SETTINGS") == "True";
                bool manualIpUnlocked = App.settingsManager.GetOption("manual_ip_unlocked", "False", "SETTINGS") == "True";
                
                if (!captureAllEnabled || manualIpUnlocked)
                {
                    // В обычном режиме или при разблокированном поле - обновляем IP
                    App.settingsForm.local_ip_textbox.Text = App.GetAdapterAddress(App.GetAdapters()[adapters_list.SelectedIndex]);
                }
                
                App.gui.StartTracking();
            }
        }

        private void netInfo_Click(object sender, EventArgs e)
        {

            MessageBox.Show("Отключить если вылетает PUBG или tickMeter. Отключение ухудшит качество данных.", "Help",MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void updateLbl_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.org/igentuman/tickmeter/downloads/");
        }


        private void label1_Click(object sender, EventArgs e)
        {
            Hide();
            App.profilesForm.Show();
        }

        private void ColorChart_Click(object sender, EventArgs e)
        {
            colorDialog1.ShowDialog();
            ColorChart.ForeColor = colorDialog1.Color;
            SaveToConfig();
            ApplyFromConfig();
        }

        private void donate_lbl_Click(object sender, EventArgs e)
        {
            Process.Start("http://www.donationalerts.ru/r/gen2man");
        }

        private void settings_autodetect_checkbox_CheckedChanged(object sender, EventArgs e)
        {
            App.settingsManager.SetOption("autodetect", settings_autodetect_checkbox.Checked.ToString());
        }

        private void ColorLabel_Click(object sender, EventArgs e)
        {
            colorDialog1.ShowDialog();
            ColorLabel.ForeColor = colorDialog1.Color;
            SaveToConfig();
            ApplyFromConfig();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void local_ip_textbox_TextChanged(object sender, EventArgs e)
        {
            if (App.gui != null)
            {
                App.gui.StartTracking();
            }
        }

        private void rtss_dialog_FileOk(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }

        /// <summary>
        /// Обработчик кнопки разблокировки/блокировки поля локального IP
        /// </summary>
        private void btnUnlockLocalIP_Click(object sender, EventArgs e)
        {
            bool captureAllEnabled = App.settingsManager.GetOption("capture_all_adapters", "False", "SETTINGS") == "True";
            
            if (!captureAllEnabled)
            {
                // Если мультиадаптер отключен, кнопка не должна быть видна
                btnUnlockLocalIP.Visible = false;
                return;
            }

            bool currentlyUnlocked = App.settingsManager.GetOption("manual_ip_unlocked", "False", "SETTINGS") == "True";
            bool newUnlockedState = !currentlyUnlocked;
            
            // Сохраняем новое состояние
            App.settingsManager.SetOption("manual_ip_unlocked", newUnlockedState.ToString(), "SETTINGS");
            
            if (newUnlockedState)
            {
                // Разблокировка: разрешаем ручное редактирование
                local_ip_textbox.Enabled = true;
                btnUnlockLocalIP.Text = "🔒";
                MessageBox.Show("Поле локального IP разблокировано для ручного редактирования.\n\nВНИМАНИЕ: В режиме мультиадаптера убедитесь, что указан правильный IP адаптера, через который проходит игровой трафик.", 
                               "Локальный IP разблокирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                // Блокировка: возвращаемся к автоматическому определению
                local_ip_textbox.Enabled = false;
                btnUnlockLocalIP.Text = "🔓";
                
                // Автоопределение IP теперь происходит в StartTracking через LocalIPDetector
                // Сбрасываем кэш для принудительного обновления
                LocalIPDetector.ResetCache();
                
                MessageBox.Show("Поле локального IP заблокировано.\n\nIP адрес будет определяться автоматически при запуске мониторинга на основе активного процесса.", 
                               "Локальный IP заблокирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
                
                // Перезапускаем мониторинг для применения изменений
                App.gui.StartTracking();
            }
        }

        // Сделайте ping_interval публичным свойством, чтобы к нему можно было обращаться из других классов
        public NumericUpDown PingIntervalControl => ping_interval;

        private void run_on_startup_CheckedChanged(object sender, EventArgs e)
        {
            try
            {
                if (run_on_startup.Checked)
                {
                    CreateScheduledTask();
                }
                else
                {
                    RemoveScheduledTask();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при настройке автозагрузки: {ex.Message}", 
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                run_on_startup.Checked = !run_on_startup.Checked; // Откатываем изменение
            }
        }

        private void CheckAndUpdateScheduledTaskPath()
        {
            try
            {
                string currentPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string taskName = "tickMeter_AutoStart";
                
                // Получаем информацию о существующей задаче
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = $"/Query /TN \"{taskName}\" /FO LIST /V",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                Process process = Process.Start(psi);
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                {
                    // Ищем строку с путем к задаче
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    string taskPath = null;
                    
                    foreach (var line in lines)
                    {
                        if (line.Contains("Task To Run:") || line.Contains("Задача для запуска:"))
                        {
                            taskPath = line.Split(':')[1].Trim().Trim('"');
                            break;
                        }
                    }

                    // Если путь изменился - обновляем задачу
                    if (taskPath != null && !taskPath.Equals(currentPath, StringComparison.OrdinalIgnoreCase))
                    {
                        Debug.Print($"[AutoStart] Путь изменился: {taskPath} -> {currentPath}");
                        CreateScheduledTask(silent: true); // Пересоздаем задачу с новым путем без показа окна
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[AutoStart] Ошибка проверки пути: {ex.Message}");
                // Не показываем ошибку пользователю, это фоновая проверка
            }
        }

        private void CreateScheduledTask(bool silent = false)
        {
            try
            {
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string workingDir = Path.GetDirectoryName(exePath);
                string taskName = "tickMeter_AutoStart";
                
                // Создаем XML для задачи с отключенными условиями
                string xmlPath = Path.Combine(Path.GetTempPath(), "tickMeter_task.xml");
                string taskXml = $@"<?xml version=""1.0"" encoding=""UTF-16""?>
<Task version=""1.2"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
  <RegistrationInfo>
    <Description>tickMeter автозагрузка с правами администратора</Description>
  </RegistrationInfo>
  <Triggers>
    <LogonTrigger>
      <Enabled>true</Enabled>
    </LogonTrigger>
  </Triggers>
  <Principals>
    <Principal id=""Author"">
      <LogonType>InteractiveToken</LogonType>
      <RunLevel>HighestAvailable</RunLevel>
    </Principal>
  </Principals>
  <Settings>
    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
    <AllowHardTerminate>true</AllowHardTerminate>
    <StartWhenAvailable>true</StartWhenAvailable>
    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
    <IdleSettings>
      <StopOnIdleEnd>false</StopOnIdleEnd>
      <RestartOnIdle>false</RestartOnIdle>
    </IdleSettings>
    <AllowStartOnDemand>true</AllowStartOnDemand>
    <Enabled>true</Enabled>
    <Hidden>false</Hidden>
    <RunOnlyIfIdle>false</RunOnlyIfIdle>
    <WakeToRun>false</WakeToRun>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
    <Priority>7</Priority>
  </Settings>
  <Actions Context=""Author"">
    <Exec>
      <Command>""{exePath}""</Command>
      <WorkingDirectory>{workingDir}</WorkingDirectory>
    </Exec>
  </Actions>
</Task>";

                File.WriteAllText(xmlPath, taskXml, System.Text.Encoding.Unicode);
                
                // Создаем задачу из XML
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = $"/Create /TN \"{taskName}\" /XML \"{xmlPath}\" /F",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                Process process = Process.Start(psi);
                process.WaitForExit();

                // Удаляем временный XML
                try { File.Delete(xmlPath); } catch { }

                if (process.ExitCode != 0)
                {
                    throw new Exception($"schtasks вернул код ошибки: {process.ExitCode}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось создать задачу в планировщике: {ex.Message}");
            }
        }

        private void RemoveScheduledTask(bool silent = false)
        {
            try
            {
                string taskName = "tickMeter_AutoStart";
                
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "schtasks",
                    Arguments = $"/Delete /TN \"{taskName}\" /F",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                };

                Process process = Process.Start(psi);
                process.WaitForExit();
            }
            catch (Exception ex)
            {
                throw new Exception($"Не удалось удалить задачу из планировщика: {ex.Message}");
            }
        }
    }
}
