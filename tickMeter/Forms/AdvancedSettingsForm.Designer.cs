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
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.chkStunEnable = new System.Windows.Forms.CheckBox();
            this.chkShowPingSpikes = new System.Windows.Forms.CheckBox();
            this.chkRtssOnlyActive = new System.Windows.Forms.CheckBox();
            this.chkEnableIPv6 = new System.Windows.Forms.CheckBox();
            this.chkDedupMultiNic = new System.Windows.Forms.CheckBox();
            this.chkTickrateSmoothing = new System.Windows.Forms.CheckBox();
            this.chkPingGraphOverlaySmoothing = new System.Windows.Forms.CheckBox();
            this.chkTickrateGraphOverlaySmoothing = new System.Windows.Forms.CheckBox();
            this.chkTicktimeGraphOverlaySmoothing = new System.Windows.Forms.CheckBox();
            this.chkPingValueOverlaySmoothing = new System.Windows.Forms.CheckBox();
            this.chkTickrateValueOverlaySmoothing = new System.Windows.Forms.CheckBox();
            this.chkTrafficValueOverlaySmoothing = new System.Windows.Forms.CheckBox();
            this.chkPingTargetActiveOnly = new System.Windows.Forms.CheckBox();
            this.chkPingFallbackIcmp = new System.Windows.Forms.CheckBox();
            this.chkPingTcpPrefer = new System.Windows.Forms.CheckBox();
            this.chkPingBindToInterface = new System.Windows.Forms.CheckBox();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.chkIgnoreVirtualAdapters = new System.Windows.Forms.CheckBox();
            this.chkCaptureAllAdapters = new System.Windows.Forms.CheckBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.label3 = new System.Windows.Forms.Label();
            this.captureFilterTextBox = new System.Windows.Forms.TextBox();
            this.chkBpfFilter = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label2 = new System.Windows.Forms.Label();
            this.overlayFpsNumeric = new System.Windows.Forms.NumericUpDown();
            this.chkOverlayFps = new System.Windows.Forms.CheckBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.label1 = new System.Windows.Forms.Label();
            this.liveMaxRowsNumeric = new System.Windows.Forms.NumericUpDown();
            this.chkLiveMaxRows = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.groupBox5.SuspendLayout();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.overlayFpsNumeric)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.liveMaxRowsNumeric)).BeginInit();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.Controls.Add(this.groupBox5);
            this.panel1.Controls.Add(this.groupBox4);
            this.panel1.Controls.Add(this.groupBox3);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 50);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(13, 12, 13, 50);
            this.panel1.Size = new System.Drawing.Size(800, 600);
            this.panel1.TabIndex = 0;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.chkStunEnable);
            this.groupBox5.Controls.Add(this.chkShowPingSpikes);
            this.groupBox5.Controls.Add(this.chkRtssOnlyActive);
            this.groupBox5.Controls.Add(this.chkEnableIPv6);
            this.groupBox5.Controls.Add(this.chkDedupMultiNic);
            this.groupBox5.Controls.Add(this.chkTickrateSmoothing);
            this.groupBox5.Controls.Add(this.chkPingGraphOverlaySmoothing);
            this.groupBox5.Controls.Add(this.chkTickrateGraphOverlaySmoothing);
            this.groupBox5.Controls.Add(this.chkTicktimeGraphOverlaySmoothing);
            this.groupBox5.Controls.Add(this.chkPingValueOverlaySmoothing);
            this.groupBox5.Controls.Add(this.chkTickrateValueOverlaySmoothing);
            this.groupBox5.Controls.Add(this.chkTrafficValueOverlaySmoothing);
            this.groupBox5.Controls.Add(this.chkPingTargetActiveOnly);
            this.groupBox5.Controls.Add(this.chkPingFallbackIcmp);
            this.groupBox5.Controls.Add(this.chkPingTcpPrefer);
            this.groupBox5.Controls.Add(this.chkPingBindToInterface);
            this.groupBox5.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox5.Location = new System.Drawing.Point(13, 404);
            this.groupBox5.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox5.Size = new System.Drawing.Size(774, 400);
            this.groupBox5.TabIndex = 4;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Универсальные";
            // 
            // chkStunEnable
            // 
            this.chkStunEnable.AutoSize = true;
            this.chkStunEnable.Location = new System.Drawing.Point(333, 59);
            this.chkStunEnable.Margin = new System.Windows.Forms.Padding(4);
            this.chkStunEnable.Name = "chkStunEnable";
            this.chkStunEnable.Size = new System.Drawing.Size(324, 20);
            this.chkStunEnable.TabIndex = 8;
            this.chkStunEnable.Text = "Определять внешний IP через STUN (в фоне)";
            this.chkStunEnable.UseVisualStyleBackColor = true;
            // 
            // chkShowPingSpikes
            // 
            this.chkShowPingSpikes.AutoSize = true;
            this.chkShowPingSpikes.Location = new System.Drawing.Point(333, 87);
            this.chkShowPingSpikes.Margin = new System.Windows.Forms.Padding(4);
            this.chkShowPingSpikes.Name = "chkShowPingSpikes";
            this.chkShowPingSpikes.Size = new System.Drawing.Size(280, 20);
            this.chkShowPingSpikes.TabIndex = 9;
            this.chkShowPingSpikes.Text = "Показывать индикатор (!) при спайках пинга";
            this.chkShowPingSpikes.UseVisualStyleBackColor = true;
            // 
            // chkRtssOnlyActive
            // 
            this.chkRtssOnlyActive.AutoSize = true;
            this.chkRtssOnlyActive.Location = new System.Drawing.Point(333, 31);
            this.chkRtssOnlyActive.Margin = new System.Windows.Forms.Padding(4);
            this.chkRtssOnlyActive.Name = "chkRtssOnlyActive";
            this.chkRtssOnlyActive.Size = new System.Drawing.Size(306, 20);
            this.chkRtssOnlyActive.TabIndex = 8;
            this.chkRtssOnlyActive.Text = "RTSS: выводить только активный процесс";
            this.chkRtssOnlyActive.UseVisualStyleBackColor = true;
            // 
            // chkEnableIPv6
            // 
            this.chkEnableIPv6.AutoSize = true;
            this.chkEnableIPv6.Location = new System.Drawing.Point(20, 283);
            this.chkEnableIPv6.Margin = new System.Windows.Forms.Padding(4);
            this.chkEnableIPv6.Name = "chkEnableIPv6";
            this.chkEnableIPv6.Size = new System.Drawing.Size(173, 20);
            this.chkEnableIPv6.TabIndex = 7;
            this.chkEnableIPv6.Text = "Включить анализ IPv6";
            this.chkEnableIPv6.UseVisualStyleBackColor = true;
            // 
            // chkDedupMultiNic
            // 
            this.chkDedupMultiNic.AutoSize = true;
            this.chkDedupMultiNic.Location = new System.Drawing.Point(20, 255);
            this.chkDedupMultiNic.Margin = new System.Windows.Forms.Padding(4);
            this.chkDedupMultiNic.Name = "chkDedupMultiNic";
            this.chkDedupMultiNic.Size = new System.Drawing.Size(277, 20);
            this.chkDedupMultiNic.TabIndex = 6;
            this.chkDedupMultiNic.Text = "Анти-дубли пакетов в мульти-режиме";
            this.chkDedupMultiNic.UseVisualStyleBackColor = true;
            // 
            // chkTickrateSmoothing
            // 
            this.chkTickrateSmoothing.AutoSize = true;
            this.chkTickrateSmoothing.Location = new System.Drawing.Point(20, 144);
            this.chkTickrateSmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateSmoothing.Name = "chkTickrateSmoothing";
            this.chkTickrateSmoothing.Size = new System.Drawing.Size(280, 20);
            this.chkTickrateSmoothing.TabIndex = 4;
            this.chkTickrateSmoothing.Text = "Сглаживание графика тикрейта (EMA)";
            this.chkTickrateSmoothing.UseVisualStyleBackColor = true;
            // 
            // chkPingGraphOverlaySmoothing
            // 
            this.chkPingGraphOverlaySmoothing.AutoSize = true;
            this.chkPingGraphOverlaySmoothing.Location = new System.Drawing.Point(20, 173);
            this.chkPingGraphOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkPingGraphOverlaySmoothing.Name = "chkPingGraphOverlaySmoothing";
            this.chkPingGraphOverlaySmoothing.Size = new System.Drawing.Size(287, 20);
            this.chkPingGraphOverlaySmoothing.TabIndex = 10;
            this.chkPingGraphOverlaySmoothing.Text = "Сглаживание графика пинга в оверлее";
            this.chkPingGraphOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkTickrateGraphOverlaySmoothing
            // 
            this.chkTickrateGraphOverlaySmoothing.AutoSize = true;
            this.chkTickrateGraphOverlaySmoothing.Location = new System.Drawing.Point(20, 200);
            this.chkTickrateGraphOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateGraphOverlaySmoothing.Name = "chkTickrateGraphOverlaySmoothing";
            this.chkTickrateGraphOverlaySmoothing.Size = new System.Drawing.Size(310, 20);
            this.chkTickrateGraphOverlaySmoothing.TabIndex = 11;
            this.chkTickrateGraphOverlaySmoothing.Text = "Сглаживание графика тикрейта в оверлее";
            this.chkTickrateGraphOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkTicktimeGraphOverlaySmoothing
            // 
            this.chkTicktimeGraphOverlaySmoothing.AutoSize = true;
            this.chkTicktimeGraphOverlaySmoothing.Location = new System.Drawing.Point(20, 227);
            this.chkTicktimeGraphOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTicktimeGraphOverlaySmoothing.Name = "chkTicktimeGraphOverlaySmoothing";
            this.chkTicktimeGraphOverlaySmoothing.Size = new System.Drawing.Size(311, 20);
            this.chkTicktimeGraphOverlaySmoothing.TabIndex = 12;
            this.chkTicktimeGraphOverlaySmoothing.Text = "Сглаживание графика тиктайма в оверлее";
            this.chkTicktimeGraphOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkPingValueOverlaySmoothing
            // 
            this.chkPingValueOverlaySmoothing.AutoSize = true;
            this.chkPingValueOverlaySmoothing.Location = new System.Drawing.Point(20, 311);
            this.chkPingValueOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkPingValueOverlaySmoothing.Name = "chkPingValueOverlaySmoothing";
            this.chkPingValueOverlaySmoothing.Size = new System.Drawing.Size(290, 20);
            this.chkPingValueOverlaySmoothing.TabIndex = 13;
            this.chkPingValueOverlaySmoothing.Text = "Сглаживание значений пинга в оверлее";
            this.chkPingValueOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkTickrateValueOverlaySmoothing
            // 
            this.chkTickrateValueOverlaySmoothing.AutoSize = true;
            this.chkTickrateValueOverlaySmoothing.Location = new System.Drawing.Point(20, 338);
            this.chkTickrateValueOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateValueOverlaySmoothing.Name = "chkTickrateValueOverlaySmoothing";
            this.chkTickrateValueOverlaySmoothing.Size = new System.Drawing.Size(313, 20);
            this.chkTickrateValueOverlaySmoothing.TabIndex = 14;
            this.chkTickrateValueOverlaySmoothing.Text = "Сглаживание значений тикрейта в оверлее";
            this.chkTickrateValueOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkTrafficValueOverlaySmoothing
            // 
            this.chkTrafficValueOverlaySmoothing.AutoSize = true;
            this.chkTrafficValueOverlaySmoothing.Location = new System.Drawing.Point(20, 365);
            this.chkTrafficValueOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTrafficValueOverlaySmoothing.Name = "chkTrafficValueOverlaySmoothing";
            this.chkTrafficValueOverlaySmoothing.Size = new System.Drawing.Size(311, 20);
            this.chkTrafficValueOverlaySmoothing.TabIndex = 15;
            this.chkTrafficValueOverlaySmoothing.Text = "Сглаживание значений трафика в оверлее";
            this.chkTrafficValueOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkPingTargetActiveOnly
            // 
            this.chkPingTargetActiveOnly.AutoSize = true;
            this.chkPingTargetActiveOnly.Location = new System.Drawing.Point(20, 116);
            this.chkPingTargetActiveOnly.Margin = new System.Windows.Forms.Padding(4);
            this.chkPingTargetActiveOnly.Name = "chkPingTargetActiveOnly";
            this.chkPingTargetActiveOnly.Size = new System.Drawing.Size(317, 20);
            this.chkPingTargetActiveOnly.TabIndex = 3;
            this.chkPingTargetActiveOnly.Text = "Пинговать только цель активного процесса";
            this.chkPingTargetActiveOnly.UseVisualStyleBackColor = true;
            // 
            // chkPingFallbackIcmp
            // 
            this.chkPingFallbackIcmp.AutoSize = true;
            this.chkPingFallbackIcmp.Location = new System.Drawing.Point(20, 87);
            this.chkPingFallbackIcmp.Margin = new System.Windows.Forms.Padding(4);
            this.chkPingFallbackIcmp.Name = "chkPingFallbackIcmp";
            this.chkPingFallbackIcmp.Size = new System.Drawing.Size(298, 20);
            this.chkPingFallbackIcmp.TabIndex = 2;
            this.chkPingFallbackIcmp.Text = "Фолбэк на ICMP, если TCP заблокирован";
            this.chkPingFallbackIcmp.UseVisualStyleBackColor = true;
            // 
            // chkPingTcpPrefer
            // 
            this.chkPingTcpPrefer.AutoSize = true;
            this.chkPingTcpPrefer.Location = new System.Drawing.Point(20, 59);
            this.chkPingTcpPrefer.Margin = new System.Windows.Forms.Padding(4);
            this.chkPingTcpPrefer.Name = "chkPingTcpPrefer";
            this.chkPingTcpPrefer.Size = new System.Drawing.Size(323, 20);
            this.chkPingTcpPrefer.TabIndex = 1;
            this.chkPingTcpPrefer.Text = "Предпочитать TCP-пинг по активному порту";
            this.chkPingTcpPrefer.UseVisualStyleBackColor = true;
            // 
            // chkPingBindToInterface
            // 
            this.chkPingBindToInterface.AutoSize = true;
            this.chkPingBindToInterface.Location = new System.Drawing.Point(20, 31);
            this.chkPingBindToInterface.Margin = new System.Windows.Forms.Padding(4);
            this.chkPingBindToInterface.Name = "chkPingBindToInterface";
            this.chkPingBindToInterface.Size = new System.Drawing.Size(318, 20);
            this.chkPingBindToInterface.TabIndex = 0;
            this.chkPingBindToInterface.Text = "Пинг привязывать к активному интерфейсу";
            this.chkPingBindToInterface.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.chkIgnoreVirtualAdapters);
            this.groupBox4.Controls.Add(this.chkCaptureAllAdapters);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox4.Location = new System.Drawing.Point(13, 817);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox4.Size = new System.Drawing.Size(755, 98);
            this.groupBox4.TabIndex = 3;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Сетевые адаптеры";
            // 
            // chkIgnoreVirtualAdapters
            // 
            this.chkIgnoreVirtualAdapters.AutoSize = true;
            this.chkIgnoreVirtualAdapters.Location = new System.Drawing.Point(20, 62);
            this.chkIgnoreVirtualAdapters.Margin = new System.Windows.Forms.Padding(4);
            this.chkIgnoreVirtualAdapters.Name = "chkIgnoreVirtualAdapters";
            this.chkIgnoreVirtualAdapters.Size = new System.Drawing.Size(280, 20);
            this.chkIgnoreVirtualAdapters.TabIndex = 1;
            this.chkIgnoreVirtualAdapters.Text = "Игнорировать виртуальные адаптеры";
            this.chkIgnoreVirtualAdapters.UseVisualStyleBackColor = true;
            // 
            // chkCaptureAllAdapters
            // 
            this.chkCaptureAllAdapters.AutoSize = true;
            this.chkCaptureAllAdapters.Location = new System.Drawing.Point(20, 31);
            this.chkCaptureAllAdapters.Margin = new System.Windows.Forms.Padding(4);
            this.chkCaptureAllAdapters.Name = "chkCaptureAllAdapters";
            this.chkCaptureAllAdapters.Size = new System.Drawing.Size(238, 20);
            this.chkCaptureAllAdapters.TabIndex = 0;
            this.chkCaptureAllAdapters.Text = "Захватывать со всех адаптеров";
            this.chkCaptureAllAdapters.UseVisualStyleBackColor = true;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.label3);
            this.groupBox3.Controls.Add(this.captureFilterTextBox);
            this.groupBox3.Controls.Add(this.chkBpfFilter);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox3.Location = new System.Drawing.Point(13, 208);
            this.groupBox3.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox3.Size = new System.Drawing.Size(755, 98);
            this.groupBox3.TabIndex = 2;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Фильтрация пакетов";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 65);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(246, 16);
            this.label3.TabIndex = 2;
            this.label3.Text = "BPF фильтр (по умолчанию: ip or ip6):";
            // 
            // captureFilterTextBox
            // 
            this.captureFilterTextBox.Enabled = false;
            this.captureFilterTextBox.Location = new System.Drawing.Point(267, 62);
            this.captureFilterTextBox.Margin = new System.Windows.Forms.Padding(4);
            this.captureFilterTextBox.Name = "captureFilterTextBox";
            this.captureFilterTextBox.Size = new System.Drawing.Size(265, 22);
            this.captureFilterTextBox.TabIndex = 1;
            this.captureFilterTextBox.Text = "ip or ip6";
            // 
            // chkBpfFilter
            // 
            this.chkBpfFilter.AutoSize = true;
            this.chkBpfFilter.Location = new System.Drawing.Point(20, 31);
            this.chkBpfFilter.Margin = new System.Windows.Forms.Padding(4);
            this.chkBpfFilter.Name = "chkBpfFilter";
            this.chkBpfFilter.Size = new System.Drawing.Size(204, 20);
            this.chkBpfFilter.TabIndex = 0;
            this.chkBpfFilter.Text = "Использовать BPF фильтр";
            this.chkBpfFilter.UseVisualStyleBackColor = true;
            this.chkBpfFilter.CheckedChanged += new System.EventHandler(this.chkBpfFilter_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label2);
            this.groupBox2.Controls.Add(this.overlayFpsNumeric);
            this.groupBox2.Controls.Add(this.chkOverlayFps);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox2.Location = new System.Drawing.Point(13, 110);
            this.groupBox2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox2.Size = new System.Drawing.Size(755, 98);
            this.groupBox2.TabIndex = 1;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "RTSS Overlay настройки";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 64);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(144, 16);
            this.label2.TabIndex = 2;
            this.label2.Text = "FPS оверлея (15-144):";
            // 
            // overlayFpsNumeric
            // 
            this.overlayFpsNumeric.Enabled = false;
            this.overlayFpsNumeric.Location = new System.Drawing.Point(267, 62);
            this.overlayFpsNumeric.Margin = new System.Windows.Forms.Padding(4);
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
            this.overlayFpsNumeric.Size = new System.Drawing.Size(107, 22);
            this.overlayFpsNumeric.TabIndex = 1;
            this.overlayFpsNumeric.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // chkOverlayFps
            // 
            this.chkOverlayFps.AutoSize = true;
            this.chkOverlayFps.Location = new System.Drawing.Point(20, 31);
            this.chkOverlayFps.Margin = new System.Windows.Forms.Padding(4);
            this.chkOverlayFps.Name = "chkOverlayFps";
            this.chkOverlayFps.Size = new System.Drawing.Size(194, 20);
            this.chkOverlayFps.TabIndex = 0;
            this.chkOverlayFps.Text = "Ограничить FPS оверлея";
            this.chkOverlayFps.UseVisualStyleBackColor = true;
            this.chkOverlayFps.CheckedChanged += new System.EventHandler(this.chkOverlayFps_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.liveMaxRowsNumeric);
            this.groupBox1.Controls.Add(this.chkLiveMaxRows);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(13, 12);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox1.Size = new System.Drawing.Size(755, 98);
            this.groupBox1.TabIndex = 0;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Live View настройки";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 64);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(174, 16);
            this.label1.TabIndex = 2;
            this.label1.Text = "Максимум строк (50-5000):";
            // 
            // liveMaxRowsNumeric
            // 
            this.liveMaxRowsNumeric.Enabled = false;
            this.liveMaxRowsNumeric.Location = new System.Drawing.Point(267, 62);
            this.liveMaxRowsNumeric.Margin = new System.Windows.Forms.Padding(4);
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
            this.liveMaxRowsNumeric.Size = new System.Drawing.Size(107, 22);
            this.liveMaxRowsNumeric.TabIndex = 1;
            this.liveMaxRowsNumeric.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // chkLiveMaxRows
            // 
            this.chkLiveMaxRows.AutoSize = true;
            this.chkLiveMaxRows.Location = new System.Drawing.Point(20, 31);
            this.chkLiveMaxRows.Margin = new System.Windows.Forms.Padding(4);
            this.chkLiveMaxRows.Name = "chkLiveMaxRows";
            this.chkLiveMaxRows.Size = new System.Drawing.Size(224, 20);
            this.chkLiveMaxRows.TabIndex = 0;
            this.chkLiveMaxRows.Text = "Ограничить строки в таблице";
            this.chkLiveMaxRows.UseVisualStyleBackColor = true;
            this.chkLiveMaxRows.CheckedChanged += new System.EventHandler(this.chkLiveMaxRows_CheckedChanged);
            // 
            // btnSave
            // 
            this.btnSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSave.Location = new System.Drawing.Point(577, 565);
            this.btnSave.Margin = new System.Windows.Forms.Padding(4);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(100, 28);
            this.btnSave.TabIndex = 1;
            this.btnSave.Text = "Сохранить";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(685, 565);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 28);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // AdvancedSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = true;
            this.MinimizeBox = true;
            this.MinimumSize = new System.Drawing.Size(800, 400);
            this.Name = "AdvancedSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Дополнительные настройки";
            this.panel1.ResumeLayout(false);
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.overlayFpsNumeric)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.liveMaxRowsNumeric)).EndInit();
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
    private System.Windows.Forms.CheckBox chkPingGraphOverlaySmoothing;
    private System.Windows.Forms.CheckBox chkTickrateGraphOverlaySmoothing;
    private System.Windows.Forms.CheckBox chkTicktimeGraphOverlaySmoothing;
    private System.Windows.Forms.CheckBox chkPingValueOverlaySmoothing;
    private System.Windows.Forms.CheckBox chkTickrateValueOverlaySmoothing;
    private System.Windows.Forms.CheckBox chkTrafficValueOverlaySmoothing;
    private System.Windows.Forms.CheckBox chkDedupMultiNic;
    private System.Windows.Forms.CheckBox chkEnableIPv6;
    private System.Windows.Forms.CheckBox chkRtssOnlyActive;
    private System.Windows.Forms.CheckBox chkStunEnable;
    private System.Windows.Forms.CheckBox chkShowPingSpikes;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
    }
}