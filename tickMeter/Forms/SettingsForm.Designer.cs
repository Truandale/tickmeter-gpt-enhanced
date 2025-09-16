using System.Windows.Forms;

namespace tickMeter.Forms
{
    partial class SettingsForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingsForm));
            this.settings_log_checkbox = new System.Windows.Forms.CheckBox();
            this.network_connection_lbl = new System.Windows.Forms.Label();
            this.adapters_list = new System.Windows.Forms.ComboBox();
            this.colorDialog1 = new System.Windows.Forms.ColorDialog();
            this.btnSaveSettings = new System.Windows.Forms.Button();
            this.rtss_dialog = new System.Windows.Forms.OpenFileDialog();
            this.settings_data_send = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.ColorChart = new System.Windows.Forms.Label();
            this.ColorGood = new System.Windows.Forms.Label();
            this.ColorMid = new System.Windows.Forms.Label();
            this.ColorBad = new System.Windows.Forms.Label();
            this.ColorLabel = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.packet_drops_checkbox = new System.Windows.Forms.CheckBox();
            this.settings_ping_chart = new System.Windows.Forms.CheckBox();
            this.settings_tickrate_show = new System.Windows.Forms.CheckBox();
            this.settings_ticktime_chart = new System.Windows.Forms.CheckBox();
            this.settings_session_time_checkbox = new System.Windows.Forms.CheckBox();
            this.settings_traffic_checkbox = new System.Windows.Forms.CheckBox();
            this.settings_ping_checkbox = new System.Windows.Forms.CheckBox();
            this.settings_ip_checkbox = new System.Windows.Forms.CheckBox();
            this.settings_chart_checkbox = new System.Windows.Forms.CheckBox();
            this.settings_rtss_output = new System.Windows.Forms.CheckBox();
            this.ping_interval = new System.Windows.Forms.NumericUpDown();
            this.settings_ping_interval_lbl = new System.Windows.Forms.Label();
            this.settings_ping_ports_lbl = new System.Windows.Forms.Label();
            this.ping_ports = new System.Windows.Forms.TextBox();
            this.settings_autodetect_checkbox = new System.Windows.Forms.CheckBox();
            this.run_minimized = new System.Windows.Forms.CheckBox();
            this.local_ip_lbl = new System.Windows.Forms.Label();
            this.local_ip_textbox = new System.Windows.Forms.TextBox();
            this.captureAllAdaptersCheckbox = new System.Windows.Forms.CheckBox();
            this.chkPingBindToInterface = new System.Windows.Forms.CheckBox();
            this.chkPingTcpPrefer = new System.Windows.Forms.CheckBox();
            this.chkPingFallbackIcmp = new System.Windows.Forms.CheckBox();
            this.chkPingTargetActiveOnly = new System.Windows.Forms.CheckBox();
            this.chkTickrateSmoothing = new System.Windows.Forms.CheckBox();
            this.chkDedupMultiNic = new System.Windows.Forms.CheckBox();
            this.chkEnableIPv6 = new System.Windows.Forms.CheckBox();
            this.chkIgnoreVirtualAdapters = new System.Windows.Forms.CheckBox();
            this.chkRtssOnlyActive = new System.Windows.Forms.CheckBox();
            this.chkStunEnable = new System.Windows.Forms.CheckBox();
            this.chkOverlayPingSpike = new System.Windows.Forms.CheckBox();
            this.chkOverlayTickrateSpike = new System.Windows.Forms.CheckBox();
            this.chkOverlayTicktimeSpike = new System.Windows.Forms.CheckBox();
            this.chkUiPingSpike = new System.Windows.Forms.CheckBox();
            this.chkUiTickrateSpike = new System.Windows.Forms.CheckBox();
            this.label8 = new System.Windows.Forms.Label();
            this.donate_lbl = new System.Windows.Forms.Label();
            this.updateLbl = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ping_interval)).BeginInit();
            this.SuspendLayout();
            // 
            // settings_log_checkbox
            // 
            resources.ApplyResources(this.settings_log_checkbox, "settings_log_checkbox");
            this.settings_log_checkbox.ForeColor = System.Drawing.Color.Black;
            this.settings_log_checkbox.Name = "settings_log_checkbox";
            this.settings_log_checkbox.UseVisualStyleBackColor = true;
            // 
            // network_connection_lbl
            // 
            resources.ApplyResources(this.network_connection_lbl, "network_connection_lbl");
            this.network_connection_lbl.BackColor = System.Drawing.Color.Transparent;
            this.network_connection_lbl.ForeColor = System.Drawing.Color.Black;
            this.network_connection_lbl.Name = "network_connection_lbl";
            // 
            // adapters_list
            // 
            this.adapters_list.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            resources.ApplyResources(this.adapters_list, "adapters_list");
            this.adapters_list.FormattingEnabled = true;
            this.adapters_list.Name = "adapters_list";
            this.adapters_list.SelectedIndexChanged += new System.EventHandler(this.adapters_list_SelectedIndexChanged);
            // 
            // btnSaveSettings
            // 
            this.btnSaveSettings.ForeColor = System.Drawing.Color.Black;
            resources.ApplyResources(this.btnSaveSettings, "btnSaveSettings");
            this.btnSaveSettings.Name = "btnSaveSettings";
            this.btnSaveSettings.UseVisualStyleBackColor = true;
            this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
            // 
            // rtss_dialog
            // 
            this.rtss_dialog.DefaultExt = "exe";
            this.rtss_dialog.FileName = "RTSS.exe";
            resources.ApplyResources(this.rtss_dialog, "rtss_dialog");
            this.rtss_dialog.InitialDirectory = "C:\\";
            this.rtss_dialog.FileOk += new System.ComponentModel.CancelEventHandler(this.rtss_dialog_FileOk);
            // 
            // settings_data_send
            // 
            resources.ApplyResources(this.settings_data_send, "settings_data_send");
            this.settings_data_send.ForeColor = System.Drawing.Color.Black;
            this.settings_data_send.Name = "settings_data_send";
            this.settings_data_send.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.ColorChart);
            this.groupBox1.Controls.Add(this.ColorGood);
            this.groupBox1.Controls.Add(this.ColorMid);
            this.groupBox1.Controls.Add(this.ColorBad);
            this.groupBox1.Controls.Add(this.ColorLabel);
            resources.ApplyResources(this.groupBox1, "groupBox1");
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.TabStop = false;
            // 
            // ColorChart
            // 
            resources.ApplyResources(this.ColorChart, "ColorChart");
            this.ColorChart.BackColor = System.Drawing.Color.Transparent;
            this.ColorChart.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ColorChart.ForeColor = System.Drawing.Color.DarkRed;
            this.ColorChart.Name = "ColorChart";
            this.ColorChart.Click += new System.EventHandler(this.ColorChart_Click);
            // 
            // ColorGood
            // 
            resources.ApplyResources(this.ColorGood, "ColorGood");
            this.ColorGood.BackColor = System.Drawing.Color.Transparent;
            this.ColorGood.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ColorGood.ForeColor = System.Drawing.Color.DarkGreen;
            this.ColorGood.Name = "ColorGood";
            this.ColorGood.Click += new System.EventHandler(this.ColorGood_Click);
            // 
            // ColorMid
            // 
            resources.ApplyResources(this.ColorMid, "ColorMid");
            this.ColorMid.BackColor = System.Drawing.Color.Transparent;
            this.ColorMid.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ColorMid.ForeColor = System.Drawing.Color.DarkOrange;
            this.ColorMid.Name = "ColorMid";
            this.ColorMid.Click += new System.EventHandler(this.ColorMid_Click);
            // 
            // ColorBad
            // 
            resources.ApplyResources(this.ColorBad, "ColorBad");
            this.ColorBad.BackColor = System.Drawing.Color.Transparent;
            this.ColorBad.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ColorBad.ForeColor = System.Drawing.Color.DarkRed;
            this.ColorBad.Name = "ColorBad";
            this.ColorBad.Click += new System.EventHandler(this.ColorBad_Click);
            // 
            // ColorLabel
            // 
            resources.ApplyResources(this.ColorLabel, "ColorLabel");
            this.ColorLabel.BackColor = System.Drawing.Color.Transparent;
            this.ColorLabel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.ColorLabel.ForeColor = System.Drawing.Color.DarkRed;
            this.ColorLabel.Name = "ColorLabel";
            this.ColorLabel.Click += new System.EventHandler(this.ColorLabel_Click);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.packet_drops_checkbox);
            this.groupBox2.Controls.Add(this.settings_ping_chart);
            this.groupBox2.Controls.Add(this.settings_tickrate_show);
            this.groupBox2.Controls.Add(this.settings_ticktime_chart);
            this.groupBox2.Controls.Add(this.settings_session_time_checkbox);
            this.groupBox2.Controls.Add(this.settings_traffic_checkbox);
            this.groupBox2.Controls.Add(this.settings_ping_checkbox);
            this.groupBox2.Controls.Add(this.settings_ip_checkbox);
            this.groupBox2.Controls.Add(this.settings_chart_checkbox);
            this.groupBox2.Controls.Add(this.settings_rtss_output);
            resources.ApplyResources(this.groupBox2, "groupBox2");
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.TabStop = false;
            // 
            // packet_drops_checkbox
            // 
            resources.ApplyResources(this.packet_drops_checkbox, "packet_drops_checkbox");
            this.packet_drops_checkbox.Checked = true;
            this.packet_drops_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.packet_drops_checkbox.ForeColor = System.Drawing.Color.Black;
            this.packet_drops_checkbox.Name = "packet_drops_checkbox";
            this.packet_drops_checkbox.UseVisualStyleBackColor = true;
            // 
            // settings_ping_chart
            // 
            resources.ApplyResources(this.settings_ping_chart, "settings_ping_chart");
            this.settings_ping_chart.Checked = true;
            this.settings_ping_chart.CheckState = System.Windows.Forms.CheckState.Checked;
            this.settings_ping_chart.ForeColor = System.Drawing.Color.Black;
            this.settings_ping_chart.Name = "settings_ping_chart";
            this.settings_ping_chart.UseVisualStyleBackColor = true;
            // 
            // settings_tickrate_show
            // 
            resources.ApplyResources(this.settings_tickrate_show, "settings_tickrate_show");
            this.settings_tickrate_show.Checked = true;
            this.settings_tickrate_show.CheckState = System.Windows.Forms.CheckState.Checked;
            this.settings_tickrate_show.ForeColor = System.Drawing.Color.Black;
            this.settings_tickrate_show.Name = "settings_tickrate_show";
            this.settings_tickrate_show.UseVisualStyleBackColor = true;
            // 
            // settings_ticktime_chart
            // 
            resources.ApplyResources(this.settings_ticktime_chart, "settings_ticktime_chart");
            this.settings_ticktime_chart.Checked = true;
            this.settings_ticktime_chart.CheckState = System.Windows.Forms.CheckState.Checked;
            this.settings_ticktime_chart.ForeColor = System.Drawing.Color.Black;
            this.settings_ticktime_chart.Name = "settings_ticktime_chart";
            this.settings_ticktime_chart.UseVisualStyleBackColor = true;
            // 
            // settings_session_time_checkbox
            // 
            resources.ApplyResources(this.settings_session_time_checkbox, "settings_session_time_checkbox");
            this.settings_session_time_checkbox.Checked = true;
            this.settings_session_time_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.settings_session_time_checkbox.ForeColor = System.Drawing.Color.Black;
            this.settings_session_time_checkbox.Name = "settings_session_time_checkbox";
            this.settings_session_time_checkbox.UseVisualStyleBackColor = true;
            // 
            // settings_traffic_checkbox
            // 
            resources.ApplyResources(this.settings_traffic_checkbox, "settings_traffic_checkbox");
            this.settings_traffic_checkbox.Checked = true;
            this.settings_traffic_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.settings_traffic_checkbox.ForeColor = System.Drawing.Color.Black;
            this.settings_traffic_checkbox.Name = "settings_traffic_checkbox";
            this.settings_traffic_checkbox.UseVisualStyleBackColor = true;
            // 
            // settings_ping_checkbox
            // 
            resources.ApplyResources(this.settings_ping_checkbox, "settings_ping_checkbox");
            this.settings_ping_checkbox.Checked = true;
            this.settings_ping_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.settings_ping_checkbox.ForeColor = System.Drawing.Color.Black;
            this.settings_ping_checkbox.Name = "settings_ping_checkbox";
            this.settings_ping_checkbox.UseVisualStyleBackColor = true;
            // 
            // settings_ip_checkbox
            // 
            resources.ApplyResources(this.settings_ip_checkbox, "settings_ip_checkbox");
            this.settings_ip_checkbox.Checked = true;
            this.settings_ip_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.settings_ip_checkbox.ForeColor = System.Drawing.Color.Black;
            this.settings_ip_checkbox.Name = "settings_ip_checkbox";
            this.settings_ip_checkbox.UseVisualStyleBackColor = true;
            // 
            // settings_chart_checkbox
            // 
            resources.ApplyResources(this.settings_chart_checkbox, "settings_chart_checkbox");
            this.settings_chart_checkbox.Checked = true;
            this.settings_chart_checkbox.CheckState = System.Windows.Forms.CheckState.Checked;
            this.settings_chart_checkbox.ForeColor = System.Drawing.Color.Black;
            this.settings_chart_checkbox.Name = "settings_chart_checkbox";
            this.settings_chart_checkbox.UseVisualStyleBackColor = true;
            // 
            // settings_rtss_output
            // 
            resources.ApplyResources(this.settings_rtss_output, "settings_rtss_output");
            this.settings_rtss_output.Checked = true;
            this.settings_rtss_output.CheckState = System.Windows.Forms.CheckState.Checked;
            this.settings_rtss_output.ForeColor = System.Drawing.Color.Black;
            this.settings_rtss_output.Name = "settings_rtss_output";
            this.settings_rtss_output.UseVisualStyleBackColor = true;
            // 
            // ping_interval
            // 
            this.ping_interval.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            resources.ApplyResources(this.ping_interval, "ping_interval");
            this.ping_interval.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.ping_interval.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.ping_interval.Name = "ping_interval";
            this.ping_interval.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // settings_ping_interval_lbl
            // 
            resources.ApplyResources(this.settings_ping_interval_lbl, "settings_ping_interval_lbl");
            this.settings_ping_interval_lbl.Name = "settings_ping_interval_lbl";
            // 
            // settings_ping_ports_lbl
            // 
            resources.ApplyResources(this.settings_ping_ports_lbl, "settings_ping_ports_lbl");
            this.settings_ping_ports_lbl.Name = "settings_ping_ports_lbl";
            // 
            // ping_ports
            // 
            resources.ApplyResources(this.ping_ports, "ping_ports");
            this.ping_ports.Name = "ping_ports";
            // 
            // settings_autodetect_checkbox
            // 
            resources.ApplyResources(this.settings_autodetect_checkbox, "settings_autodetect_checkbox");
            this.settings_autodetect_checkbox.ForeColor = System.Drawing.Color.Black;
            this.settings_autodetect_checkbox.Name = "settings_autodetect_checkbox";
            this.settings_autodetect_checkbox.UseVisualStyleBackColor = true;
            this.settings_autodetect_checkbox.CheckedChanged += new System.EventHandler(this.settings_autodetect_checkbox_CheckedChanged);
            // 
            // run_minimized
            // 
            resources.ApplyResources(this.run_minimized, "run_minimized");
            this.run_minimized.ForeColor = System.Drawing.Color.Black;
            this.run_minimized.Name = "run_minimized";
            this.run_minimized.UseVisualStyleBackColor = true;
            this.run_minimized.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // local_ip_lbl
            // 
            resources.ApplyResources(this.local_ip_lbl, "local_ip_lbl");
            this.local_ip_lbl.Name = "local_ip_lbl";
            // 
            // local_ip_textbox
            // 
            resources.ApplyResources(this.local_ip_textbox, "local_ip_textbox");
            this.local_ip_textbox.Name = "local_ip_textbox";
            this.local_ip_textbox.TextChanged += new System.EventHandler(this.local_ip_textbox_TextChanged);
            // 
            // captureAllAdaptersCheckbox
            // 
            resources.ApplyResources(this.captureAllAdaptersCheckbox, "captureAllAdaptersCheckbox");
            this.captureAllAdaptersCheckbox.ForeColor = System.Drawing.Color.Black;
            this.captureAllAdaptersCheckbox.Name = "captureAllAdaptersCheckbox";
            this.captureAllAdaptersCheckbox.UseVisualStyleBackColor = true;
            // 
            // chkPingBindToInterface
            // 
            resources.ApplyResources(this.chkPingBindToInterface, "chkPingBindToInterface");
            this.chkPingBindToInterface.ForeColor = System.Drawing.Color.Black;
            this.chkPingBindToInterface.Name = "chkPingBindToInterface";
            this.chkPingBindToInterface.UseVisualStyleBackColor = true;
            // 
            // chkPingTcpPrefer
            // 
            resources.ApplyResources(this.chkPingTcpPrefer, "chkPingTcpPrefer");
            this.chkPingTcpPrefer.ForeColor = System.Drawing.Color.Black;
            this.chkPingTcpPrefer.Name = "chkPingTcpPrefer";
            this.chkPingTcpPrefer.UseVisualStyleBackColor = true;
            // 
            // chkPingFallbackIcmp
            // 
            resources.ApplyResources(this.chkPingFallbackIcmp, "chkPingFallbackIcmp");
            this.chkPingFallbackIcmp.ForeColor = System.Drawing.Color.Black;
            this.chkPingFallbackIcmp.Name = "chkPingFallbackIcmp";
            this.chkPingFallbackIcmp.UseVisualStyleBackColor = true;
            // 
            // chkPingTargetActiveOnly
            // 
            resources.ApplyResources(this.chkPingTargetActiveOnly, "chkPingTargetActiveOnly");
            this.chkPingTargetActiveOnly.ForeColor = System.Drawing.Color.Black;
            this.chkPingTargetActiveOnly.Name = "chkPingTargetActiveOnly";
            this.chkPingTargetActiveOnly.UseVisualStyleBackColor = true;
            // 
            // chkTickrateSmoothing
            // 
            resources.ApplyResources(this.chkTickrateSmoothing, "chkTickrateSmoothing");
            this.chkTickrateSmoothing.ForeColor = System.Drawing.Color.Black;
            this.chkTickrateSmoothing.Name = "chkTickrateSmoothing";
            this.chkTickrateSmoothing.UseVisualStyleBackColor = true;
            // 
            // chkDedupMultiNic
            // 
            resources.ApplyResources(this.chkDedupMultiNic, "chkDedupMultiNic");
            this.chkDedupMultiNic.ForeColor = System.Drawing.Color.Black;
            this.chkDedupMultiNic.Name = "chkDedupMultiNic";
            this.chkDedupMultiNic.UseVisualStyleBackColor = true;
            // 
            // chkEnableIPv6
            // 
            resources.ApplyResources(this.chkEnableIPv6, "chkEnableIPv6");
            this.chkEnableIPv6.ForeColor = System.Drawing.Color.Black;
            this.chkEnableIPv6.Name = "chkEnableIPv6";
            this.chkEnableIPv6.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreVirtualAdapters
            // 
            resources.ApplyResources(this.chkIgnoreVirtualAdapters, "chkIgnoreVirtualAdapters");
            this.chkIgnoreVirtualAdapters.ForeColor = System.Drawing.Color.Black;
            this.chkIgnoreVirtualAdapters.Name = "chkIgnoreVirtualAdapters";
            this.chkIgnoreVirtualAdapters.UseVisualStyleBackColor = true;
            // 
            // chkRtssOnlyActive
            // 
            resources.ApplyResources(this.chkRtssOnlyActive, "chkRtssOnlyActive");
            this.chkRtssOnlyActive.ForeColor = System.Drawing.Color.Black;
            this.chkRtssOnlyActive.Name = "chkRtssOnlyActive";
            this.chkRtssOnlyActive.UseVisualStyleBackColor = true;
            // 
            // chkStunEnable
            // 
            resources.ApplyResources(this.chkStunEnable, "chkStunEnable");
            this.chkStunEnable.ForeColor = System.Drawing.Color.Black;
            this.chkStunEnable.Name = "chkStunEnable";
            this.chkStunEnable.UseVisualStyleBackColor = true;
            // 
            // chkOverlayPingSpike
            // 
            resources.ApplyResources(this.chkOverlayPingSpike, "chkOverlayPingSpike");
            this.chkOverlayPingSpike.Checked = true;
            this.chkOverlayPingSpike.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkOverlayPingSpike.ForeColor = System.Drawing.Color.Black;
            this.chkOverlayPingSpike.Name = "chkOverlayPingSpike";
            this.chkOverlayPingSpike.UseVisualStyleBackColor = true;
            // 
            // chkOverlayTickrateSpike
            // 
            resources.ApplyResources(this.chkOverlayTickrateSpike, "chkOverlayTickrateSpike");
            this.chkOverlayTickrateSpike.ForeColor = System.Drawing.Color.Black;
            this.chkOverlayTickrateSpike.Name = "chkOverlayTickrateSpike";
            this.chkOverlayTickrateSpike.UseVisualStyleBackColor = true;
            // 
            // chkOverlayTicktimeSpike
            // 
            resources.ApplyResources(this.chkOverlayTicktimeSpike, "chkOverlayTicktimeSpike");
            this.chkOverlayTicktimeSpike.ForeColor = System.Drawing.Color.Black;
            this.chkOverlayTicktimeSpike.Name = "chkOverlayTicktimeSpike";
            this.chkOverlayTicktimeSpike.UseVisualStyleBackColor = true;
            // 
            // chkUiPingSpike
            // 
            resources.ApplyResources(this.chkUiPingSpike, "chkUiPingSpike");
            this.chkUiPingSpike.Checked = true;
            this.chkUiPingSpike.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkUiPingSpike.ForeColor = System.Drawing.Color.Black;
            this.chkUiPingSpike.Name = "chkUiPingSpike";
            this.chkUiPingSpike.UseVisualStyleBackColor = true;
            // 
            // chkUiTickrateSpike
            // 
            resources.ApplyResources(this.chkUiTickrateSpike, "chkUiTickrateSpike");
            this.chkUiTickrateSpike.ForeColor = System.Drawing.Color.Black;
            this.chkUiTickrateSpike.Name = "chkUiTickrateSpike";
            this.chkUiTickrateSpike.UseVisualStyleBackColor = true;
            // 
            // label8
            // 
            resources.ApplyResources(this.label8, "label8");
            this.label8.BackColor = System.Drawing.Color.Transparent;
            this.label8.Cursor = System.Windows.Forms.Cursors.Hand;
            this.label8.ForeColor = System.Drawing.Color.Blue;
            this.label8.Name = "label8";
            this.label8.Click += new System.EventHandler(this.label8_Click);
            // 
            // donate_lbl
            // 
            resources.ApplyResources(this.donate_lbl, "donate_lbl");
            this.donate_lbl.BackColor = System.Drawing.Color.Transparent;
            this.donate_lbl.Cursor = System.Windows.Forms.Cursors.Hand;
            this.donate_lbl.ForeColor = System.Drawing.Color.Blue;
            this.donate_lbl.Name = "donate_lbl";
            this.donate_lbl.Click += new System.EventHandler(this.donate_lbl_Click);
            // 
            // updateLbl
            // 
            resources.ApplyResources(this.updateLbl, "updateLbl");
            this.updateLbl.BackColor = System.Drawing.Color.Transparent;
            this.updateLbl.Cursor = System.Windows.Forms.Cursors.Hand;
            this.updateLbl.ForeColor = System.Drawing.Color.Blue;
            this.updateLbl.Name = "updateLbl";
            this.updateLbl.Click += new System.EventHandler(this.updateLbl_Click);
            // 
            // SettingsForm
            // 
            resources.ApplyResources(this, "$this");
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.chkPingBindToInterface);
            this.Controls.Add(this.chkPingTcpPrefer);
            this.Controls.Add(this.chkPingFallbackIcmp);
            this.Controls.Add(this.chkPingTargetActiveOnly);
            this.Controls.Add(this.chkTickrateSmoothing);
            this.Controls.Add(this.chkDedupMultiNic);
            this.Controls.Add(this.chkEnableIPv6);
            this.Controls.Add(this.chkIgnoreVirtualAdapters);
            this.Controls.Add(this.chkRtssOnlyActive);
            this.Controls.Add(this.chkStunEnable);
            this.Controls.Add(this.chkOverlayPingSpike);
            this.Controls.Add(this.chkOverlayTickrateSpike);
            this.Controls.Add(this.chkOverlayTicktimeSpike);
            this.Controls.Add(this.chkUiPingSpike);
            this.Controls.Add(this.chkUiTickrateSpike);
            this.Controls.Add(this.captureAllAdaptersCheckbox);
            this.Controls.Add(this.local_ip_textbox);
            this.Controls.Add(this.local_ip_lbl);
            this.Controls.Add(this.run_minimized);
            this.Controls.Add(this.settings_autodetect_checkbox);
            this.Controls.Add(this.ping_ports);
            this.Controls.Add(this.settings_ping_ports_lbl);
            this.Controls.Add(this.settings_ping_interval_lbl);
            this.Controls.Add(this.ping_interval);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.settings_data_send);
            this.Controls.Add(this.donate_lbl);
            this.Controls.Add(this.updateLbl);
            this.Controls.Add(this.btnSaveSettings);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.settings_log_checkbox);
            this.Controls.Add(this.network_connection_lbl);
            this.Controls.Add(this.adapters_list);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "SettingsForm";
            this.ShowInTaskbar = false;
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SettingsForm_FormClosing);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.ping_interval)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        public System.Windows.Forms.ComboBox adapters_list;
        public System.Windows.Forms.CheckBox settings_log_checkbox;
        public System.Windows.Forms.Label network_connection_lbl;
        public System.Windows.Forms.Button btnSaveSettings;
        private System.Windows.Forms.OpenFileDialog rtss_dialog;
        public CheckBox settings_data_send;
        private GroupBox groupBox1;
        public Label ColorChart;
        public Label ColorGood;
        public Label ColorMid;
        public Label ColorBad;
        public Label ColorLabel;
        private GroupBox groupBox2;
        public CheckBox settings_tickrate_show;
        public CheckBox settings_ticktime_chart;
        public CheckBox settings_session_time_checkbox;
        public CheckBox settings_traffic_checkbox;
        public CheckBox settings_ping_checkbox;
        public CheckBox settings_ip_checkbox;
        public CheckBox settings_chart_checkbox;
        public CheckBox settings_rtss_output;
        private NumericUpDown ping_interval;
        private Label settings_ping_interval_lbl;
        private Label settings_ping_ports_lbl;
        private TextBox ping_ports;
        public CheckBox settings_autodetect_checkbox;
        public CheckBox settings_ping_chart;
        public CheckBox run_minimized;
        private Label local_ip_lbl;
        public TextBox local_ip_textbox;
        public ColorDialog colorDialog1;
        public CheckBox packet_drops_checkbox;
        // NEW: Advanced universality checkboxes fields
        public CheckBox chkPingBindToInterface;
        public CheckBox chkPingTcpPrefer;
        public CheckBox chkPingFallbackIcmp;
        public CheckBox chkPingTargetActiveOnly;
        public CheckBox chkTickrateSmoothing;
        public CheckBox chkDedupMultiNic;
        public CheckBox chkEnableIPv6;
        public CheckBox chkIgnoreVirtualAdapters;
        public CheckBox chkRtssOnlyActive;
        public CheckBox chkStunEnable;
        
        // Чекбоксы для спайк-маркеров
        public CheckBox chkOverlayPingSpike;
        public CheckBox chkOverlayTickrateSpike;
        public CheckBox chkOverlayTicktimeSpike;
        public CheckBox chkUiPingSpike;
        public CheckBox chkUiTickrateSpike;
        public CheckBox captureAllAdaptersCheckbox;
        public Label label8;
        public Label donate_lbl;
        public Label updateLbl;
    }
}