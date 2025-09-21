namespace tickMeter.Forms
{
    partial class AdvancedSettingsForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.chkLiveMaxRows = new System.Windows.Forms.CheckBox();
            this.liveMaxRowsNumeric = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chkOverlayFps = new System.Windows.Forms.CheckBox();
            this.overlayFpsNumeric = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.chkBpfFilter = new System.Windows.Forms.CheckBox();
            this.captureFilterTextBox = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.chkCaptureAllAdapters = new System.Windows.Forms.CheckBox();
            this.chkIgnoreVirtualAdapters = new System.Windows.Forms.CheckBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.chkPingBindToInterface = new System.Windows.Forms.CheckBox();
            this.chkPingTcpPrefer = new System.Windows.Forms.CheckBox();
            this.chkPingFallbackIcmp = new System.Windows.Forms.CheckBox();
            this.chkPingTargetActiveOnly = new System.Windows.Forms.CheckBox();
            this.chkTickrateSmoothing = new System.Windows.Forms.CheckBox();
            this.chkDedupMultiNic = new System.Windows.Forms.CheckBox();
            this.chkEnableIPv6 = new System.Windows.Forms.CheckBox();
            this.chkRtssOnlyActive = new System.Windows.Forms.CheckBox();
            this.chkStunEnable = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.liveMaxRowsNumeric)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.overlayFpsNumeric)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            // Add the new group (DockStyle.Top). Adding it first keeps it at the bottom of the stack.
            this.panel1.Controls.Add(this.groupBox5);
            this.panel1.Controls.Add(this.groupBox4);
            this.panel1.Controls.Add(this.groupBox3);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(10);
            this.panel1.Size = new System.Drawing.Size(484, 461);
            this.panel1.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.liveMaxRowsNumeric);
            this.groupBox1.Controls.Add(this.chkLiveMaxRows);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(10, 10);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(464, 80);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Live View настройки";
            // 
            // chkLiveMaxRows
            // 
            this.chkLiveMaxRows.AutoSize = true;
            this.chkLiveMaxRows.Location = new System.Drawing.Point(15, 25);
            this.chkLiveMaxRows.Name = "chkLiveMaxRows";
            this.chkLiveMaxRows.Size = new System.Drawing.Size(162, 17);
            this.chkLiveMaxRows.TabIndex = 0;
            this.chkLiveMaxRows.Text = "Ограничить строки в таблице";
            this.chkLiveMaxRows.UseVisualStyleBackColor = true;
            this.chkLiveMaxRows.CheckedChanged += new System.EventHandler(this.chkLiveMaxRows_CheckedChanged);
            // 
            // liveMaxRowsNumeric
            // 
            this.liveMaxRowsNumeric.Enabled = false;
            this.liveMaxRowsNumeric.Location = new System.Drawing.Point(200, 50);
            this.liveMaxRowsNumeric.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.liveMaxRowsNumeric.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.liveMaxRowsNumeric.Name = "liveMaxRowsNumeric";
            this.liveMaxRowsNumeric.Size = new System.Drawing.Size(80, 20);
            this.liveMaxRowsNumeric.TabIndex = 1;
            this.liveMaxRowsNumeric.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 52);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(117, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "Максимум строк (50-5000):";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.overlayFpsNumeric);
            this.groupBox2.Controls.Add(this.chkOverlayFps);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox2.Location = new System.Drawing.Point(10, 90);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(464, 80);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "RTSS Overlay настройки";
            // 
            // chkOverlayFps
            // 
            this.chkOverlayFps.AutoSize = true;
            this.chkOverlayFps.Location = new System.Drawing.Point(15, 25);
            this.chkOverlayFps.Name = "chkOverlayFps";
            this.chkOverlayFps.Size = new System.Drawing.Size(144, 17);
            this.chkOverlayFps.TabIndex = 0;
            this.chkOverlayFps.Text = "Ограничить FPS оверлея";
            this.chkOverlayFps.UseVisualStyleBackColor = true;
            this.chkOverlayFps.CheckedChanged += new System.EventHandler(this.chkOverlayFps_CheckedChanged);
            // 
            // overlayFpsNumeric
            // 
            this.overlayFpsNumeric.Enabled = false;
            this.overlayFpsNumeric.Location = new System.Drawing.Point(200, 50);
            this.overlayFpsNumeric.Maximum = new decimal(new int[] {
            144,
            0,
            0,
            0});
            this.overlayFpsNumeric.Minimum = new decimal(new int[] {
            15,
            0,
            0,
            0});
            this.overlayFpsNumeric.Name = "overlayFpsNumeric";
            this.overlayFpsNumeric.Size = new System.Drawing.Size(80, 20);
            this.overlayFpsNumeric.TabIndex = 1;
            this.overlayFpsNumeric.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 52);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(102, 13);
            this.label2.TabIndex = 2;
            this.label2.Text = "FPS оверлея (15-144):";
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.captureFilterTextBox);
            this.groupBox3.Controls.Add(this.chkBpfFilter);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox3.Location = new System.Drawing.Point(10, 170);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(464, 80);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Фильтрация пакетов";
            // 
            // chkBpfFilter
            // 
            this.chkBpfFilter.AutoSize = true;
            this.chkBpfFilter.Location = new System.Drawing.Point(15, 25);
            this.chkBpfFilter.Name = "chkBpfFilter";
            this.chkBpfFilter.Size = new System.Drawing.Size(126, 17);
            this.chkBpfFilter.TabIndex = 0;
            this.chkBpfFilter.Text = "Использовать BPF фильтр";
            this.chkBpfFilter.UseVisualStyleBackColor = true;
            this.chkBpfFilter.CheckedChanged += new System.EventHandler(this.chkBpfFilter_CheckedChanged);
            // 
            // captureFilterTextBox
            // 
            this.captureFilterTextBox.Enabled = false;
            this.captureFilterTextBox.Location = new System.Drawing.Point(200, 50);
            this.captureFilterTextBox.Name = "captureFilterTextBox";
            this.captureFilterTextBox.Size = new System.Drawing.Size(200, 20);
            this.captureFilterTextBox.TabIndex = 1;
            this.captureFilterTextBox.Text = "ip or ip6";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 53);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(98, 13);
            this.label3.TabIndex = 2;
            this.label3.Text = "BPF фильтр (по умолчанию: ip or ip6):";
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.chkIgnoreVirtualAdapters);
            this.groupBox4.Controls.Add(this.chkCaptureAllAdapters);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox4.Location = new System.Drawing.Point(10, 250);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(464, 80);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Сетевые адаптеры";
            // 
            // chkCaptureAllAdapters
            // 
            this.chkCaptureAllAdapters.AutoSize = true;
            this.chkCaptureAllAdapters.Location = new System.Drawing.Point(15, 25);
            this.chkCaptureAllAdapters.Name = "chkCaptureAllAdapters";
            this.chkCaptureAllAdapters.Size = new System.Drawing.Size(194, 17);
            this.chkCaptureAllAdapters.TabIndex = 0;
            this.chkCaptureAllAdapters.Text = "Захватывать со всех адаптеров";
            this.chkCaptureAllAdapters.UseVisualStyleBackColor = true;
            // 
            // chkIgnoreVirtualAdapters
            // 
            this.chkIgnoreVirtualAdapters.AutoSize = true;
            this.chkIgnoreVirtualAdapters.Location = new System.Drawing.Point(15, 50);
            this.chkIgnoreVirtualAdapters.Name = "chkIgnoreVirtualAdapters";
            this.chkIgnoreVirtualAdapters.Size = new System.Drawing.Size(179, 17);
            this.chkIgnoreVirtualAdapters.TabIndex = 1;
            this.chkIgnoreVirtualAdapters.Text = "Игнорировать виртуальные адаптеры";
            this.chkIgnoreVirtualAdapters.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.chkStunEnable);
            this.groupBox5.Controls.Add(this.chkRtssOnlyActive);
            this.groupBox5.Controls.Add(this.chkEnableIPv6);
            this.groupBox5.Controls.Add(this.chkDedupMultiNic);
            this.groupBox5.Controls.Add(this.chkTickrateSmoothing);
            this.groupBox5.Controls.Add(this.chkPingTargetActiveOnly);
            this.groupBox5.Controls.Add(this.chkPingFallbackIcmp);
            this.groupBox5.Controls.Add(this.chkPingTcpPrefer);
            this.groupBox5.Controls.Add(this.chkPingBindToInterface);
            this.groupBox5.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox5.Location = new System.Drawing.Point(10, 330);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(464, 210);
            this.groupBox5.TabIndex = 4;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Универсальные";
            // 
            // chkPingBindToInterface
            // 
            this.chkPingBindToInterface.AutoSize = true;
            this.chkPingBindToInterface.Location = new System.Drawing.Point(15, 25);
            this.chkPingBindToInterface.Name = "chkPingBindToInterface";
            this.chkPingBindToInterface.Size = new System.Drawing.Size(239, 17);
            this.chkPingBindToInterface.TabIndex = 0;
            this.chkPingBindToInterface.Text = "Пинг привязывать к активному интерфейсу";
            this.chkPingBindToInterface.UseVisualStyleBackColor = true;
            // 
            // chkPingTcpPrefer
            // 
            this.chkPingTcpPrefer.AutoSize = true;
            this.chkPingTcpPrefer.Location = new System.Drawing.Point(15, 48);
            this.chkPingTcpPrefer.Name = "chkPingTcpPrefer";
            this.chkPingTcpPrefer.Size = new System.Drawing.Size(257, 17);
            this.chkPingTcpPrefer.TabIndex = 1;
            this.chkPingTcpPrefer.Text = "Предпочитать TCP-пинг по активному порту";
            this.chkPingTcpPrefer.UseVisualStyleBackColor = true;
            // 
            // chkPingFallbackIcmp
            // 
            this.chkPingFallbackIcmp.AutoSize = true;
            this.chkPingFallbackIcmp.Location = new System.Drawing.Point(15, 71);
            this.chkPingFallbackIcmp.Name = "chkPingFallbackIcmp";
            this.chkPingFallbackIcmp.Size = new System.Drawing.Size(230, 17);
            this.chkPingFallbackIcmp.TabIndex = 2;
            this.chkPingFallbackIcmp.Text = "Фолбэк на ICMP, если TCP заблокирован";
            this.chkPingFallbackIcmp.UseVisualStyleBackColor = true;
            // 
            // chkPingTargetActiveOnly
            // 
            this.chkPingTargetActiveOnly.AutoSize = true;
            this.chkPingTargetActiveOnly.Location = new System.Drawing.Point(15, 94);
            this.chkPingTargetActiveOnly.Name = "chkPingTargetActiveOnly";
            this.chkPingTargetActiveOnly.Size = new System.Drawing.Size(225, 17);
            this.chkPingTargetActiveOnly.TabIndex = 3;
            this.chkPingTargetActiveOnly.Text = "Пинговать только цель активного процесса";
            this.chkPingTargetActiveOnly.UseVisualStyleBackColor = true;
            // 
            // chkTickrateSmoothing
            // 
            this.chkTickrateSmoothing.AutoSize = true;
            this.chkTickrateSmoothing.Location = new System.Drawing.Point(15, 117);
            this.chkTickrateSmoothing.Name = "chkTickrateSmoothing";
            this.chkTickrateSmoothing.Size = new System.Drawing.Size(200, 17);
            this.chkTickrateSmoothing.TabIndex = 4;
            this.chkTickrateSmoothing.Text = "Сглаживание графика тикрейта (EMA)";
            this.chkTickrateSmoothing.UseVisualStyleBackColor = true;
            // 
            // chkDedupMultiNic
            // 
            this.chkDedupMultiNic.AutoSize = true;
            this.chkDedupMultiNic.Location = new System.Drawing.Point(15, 140);
            this.chkDedupMultiNic.Name = "chkDedupMultiNic";
            this.chkDedupMultiNic.Size = new System.Drawing.Size(224, 17);
            this.chkDedupMultiNic.TabIndex = 5;
            this.chkDedupMultiNic.Text = "Анти-дубли пакетов в мульти-режиме";
            this.chkDedupMultiNic.UseVisualStyleBackColor = true;
            // 
            // chkEnableIPv6
            // 
            this.chkEnableIPv6.AutoSize = true;
            this.chkEnableIPv6.Location = new System.Drawing.Point(15, 163);
            this.chkEnableIPv6.Name = "chkEnableIPv6";
            this.chkEnableIPv6.Size = new System.Drawing.Size(119, 17);
            this.chkEnableIPv6.TabIndex = 6;
            this.chkEnableIPv6.Text = "Включить анализ IPv6";
            this.chkEnableIPv6.UseVisualStyleBackColor = true;
            // 
            // chkRtssOnlyActive
            // 
            this.chkRtssOnlyActive.AutoSize = true;
            this.chkRtssOnlyActive.Location = new System.Drawing.Point(250, 25);
            this.chkRtssOnlyActive.Name = "chkRtssOnlyActive";
            this.chkRtssOnlyActive.Size = new System.Drawing.Size(243, 17);
            this.chkRtssOnlyActive.TabIndex = 7;
            this.chkRtssOnlyActive.Text = "RTSS: выводить только активный процесс";
            this.chkRtssOnlyActive.UseVisualStyleBackColor = true;
            // 
            // chkStunEnable
            // 
            this.chkStunEnable.AutoSize = true;
            this.chkStunEnable.Location = new System.Drawing.Point(250, 48);
            this.chkStunEnable.Name = "chkStunEnable";
            this.chkStunEnable.Size = new System.Drawing.Size(251, 17);
            this.chkStunEnable.TabIndex = 8;
            this.chkStunEnable.Text = "Определять внешний IP через STUN (в фоне)";
            this.chkStunEnable.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(316, 470);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 23);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(397, 470);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // AdvancedSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 501);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AdvancedSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Дополнительные настройки";
            this.panel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.liveMaxRowsNumeric)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.overlayFpsNumeric)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown liveMaxRowsNumeric;
        private System.Windows.Forms.CheckBox chkLiveMaxRows;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.NumericUpDown overlayFpsNumeric;
        private System.Windows.Forms.CheckBox chkOverlayFps;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox captureFilterTextBox;
        private System.Windows.Forms.CheckBox chkBpfFilter;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.CheckBox chkIgnoreVirtualAdapters;
        private System.Windows.Forms.CheckBox chkCaptureAllAdapters;
    private System.Windows.Forms.GroupBox groupBox5;
    private System.Windows.Forms.CheckBox chkPingBindToInterface;
    private System.Windows.Forms.CheckBox chkPingTcpPrefer;
    private System.Windows.Forms.CheckBox chkPingFallbackIcmp;
    private System.Windows.Forms.CheckBox chkPingTargetActiveOnly;
    private System.Windows.Forms.CheckBox chkTickrateSmoothing;
    private System.Windows.Forms.CheckBox chkDedupMultiNic;
    private System.Windows.Forms.CheckBox chkEnableIPv6;
    private System.Windows.Forms.CheckBox chkRtssOnlyActive;
    private System.Windows.Forms.CheckBox chkStunEnable;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}