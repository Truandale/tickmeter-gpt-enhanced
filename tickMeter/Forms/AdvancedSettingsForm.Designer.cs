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
            this.groupBoxVpnBypass = new System.Windows.Forms.GroupBox();
            this.chkVpnBypassAdvanced = new System.Windows.Forms.CheckBox();
            this.chkVpnBypassBasic = new System.Windows.Forms.CheckBox();
            this.groupBoxPhase3 = new System.Windows.Forms.GroupBox();
            this.chkHighPriorityThreads = new System.Windows.Forms.CheckBox();
            this.chkSingleConsumerPattern = new System.Windows.Forms.CheckBox();
            this.lblUiProcessingRate = new System.Windows.Forms.Label();
            this.numUiProcessingRate = new System.Windows.Forms.NumericUpDown();
            this.lblUiBatchSize = new System.Windows.Forms.Label();
            this.numUiBatchSize = new System.Windows.Forms.NumericUpDown();
            this.groupBoxPhase2 = new System.Windows.Forms.GroupBox();
            this.chkVirtualModeListView = new System.Windows.Forms.CheckBox();
            this.lblVirtualModeThreshold = new System.Windows.Forms.Label();
            this.numVirtualModeThreshold = new System.Windows.Forms.NumericUpDown();
            this.lblRingBufferSize = new System.Windows.Forms.Label();
            this.numRingBufferSize = new System.Windows.Forms.NumericUpDown();
            this.chkShowVirtualModeStats = new System.Windows.Forms.CheckBox();
            this.groupBoxPhase1 = new System.Windows.Forms.GroupBox();
            this.chkAntiReentrancy = new System.Windows.Forms.CheckBox();
            this.chkRtssThrottling = new System.Windows.Forms.CheckBox();
            this.chkPcapOptimization = new System.Windows.Forms.CheckBox();
            this.lblPcapKernelBufferMb = new System.Windows.Forms.Label();
            this.numPcapKernelBufferMb = new System.Windows.Forms.NumericUpDown();
            this.lblPcapMinToCopy = new System.Windows.Forms.Label();
            this.numPcapMinToCopy = new System.Windows.Forms.NumericUpDown();
            this.groupBoxSpikeDetection = new System.Windows.Forms.GroupBox();
            this.chkSpikeDetectionEnable = new System.Windows.Forms.CheckBox();
            this.chkSpikeMetricPing = new System.Windows.Forms.CheckBox();
            this.chkSpikeMetricTickrate = new System.Windows.Forms.CheckBox();
            this.chkSpikeMetricTicktime = new System.Windows.Forms.CheckBox();
            this.lblSpikeSensitivity = new System.Windows.Forms.Label();
            this.cmbSpikeSensitivity = new System.Windows.Forms.ComboBox();
            this.lblSpikeDisplayMode = new System.Windows.Forms.Label();
            this.cmbSpikeDisplayMode = new System.Windows.Forms.ComboBox();
            this.lblSpikeMinDuration = new System.Windows.Forms.Label();
            this.numSpikeMinDuration = new System.Windows.Forms.NumericUpDown();
            this.lblSpikeHistory = new System.Windows.Forms.Label();
            this.numSpikeHistorySize = new System.Windows.Forms.NumericUpDown();
            this.chkSpikeAutoCalibration = new System.Windows.Forms.CheckBox();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.chkStunEnable = new System.Windows.Forms.CheckBox();
            this.chkShowPingSpikes = new System.Windows.Forms.CheckBox();
            this.lblPingSpikeThreshold = new System.Windows.Forms.Label();
            this.numPingSpikeThreshold = new System.Windows.Forms.NumericUpDown();
            this.chkRtssOnlyActive = new System.Windows.Forms.CheckBox();
            this.chkEnableIPv6 = new System.Windows.Forms.CheckBox();
            this.chkDedupMultiNic = new System.Windows.Forms.CheckBox();
            this.chkTickrateSmoothing = new System.Windows.Forms.CheckBox();
            this.chkPingGraphOverlaySmoothing = new System.Windows.Forms.CheckBox();
            this.chkTickrateGraphOverlaySmoothing = new System.Windows.Forms.CheckBox();
            this.chkTicktimeGraphOverlaySmoothing = new System.Windows.Forms.CheckBox();
            this.chkPingValueOverlaySmoothing = new System.Windows.Forms.CheckBox();
            this.chkPingValueGuiSmoothing = new System.Windows.Forms.CheckBox();
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
            this.panelButtons = new System.Windows.Forms.Panel();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnReset = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.panel1.SuspendLayout();
            this.groupBoxVpnBypass.SuspendLayout();
            this.groupBoxPhase3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numUiProcessingRate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUiBatchSize)).BeginInit();
            this.groupBoxPhase2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numVirtualModeThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRingBufferSize)).BeginInit();
            this.groupBoxPhase1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPcapKernelBufferMb)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPcapMinToCopy)).BeginInit();
            this.groupBoxSpikeDetection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSpikeMinDuration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSpikeHistorySize)).BeginInit();
            this.groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPingSpikeThreshold)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.overlayFpsNumeric)).BeginInit();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.liveMaxRowsNumeric)).BeginInit();
            this.panelButtons.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.AutoScroll = true;
            this.panel1.Controls.Add(this.groupBoxVpnBypass);
            this.panel1.Controls.Add(this.groupBoxPhase3);
            this.panel1.Controls.Add(this.groupBoxPhase2);
            this.panel1.Controls.Add(this.groupBoxPhase1);
            this.panel1.Controls.Add(this.groupBoxSpikeDetection);
            this.panel1.Controls.Add(this.groupBox5);
            this.panel1.Controls.Add(this.groupBox4);
            this.panel1.Controls.Add(this.groupBox3);
            this.panel1.Controls.Add(this.groupBox2);
            this.panel1.Controls.Add(this.groupBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(4);
            this.panel1.Name = "panel1";
            this.panel1.Padding = new System.Windows.Forms.Padding(13, 12, 13, 12);
            this.panel1.Size = new System.Drawing.Size(800, 550);
            this.panel1.TabIndex = 0;
            // 
            // groupBoxVpnBypass
            // 
            this.groupBoxVpnBypass.Controls.Add(this.chkVpnBypassAdvanced);
            this.groupBoxVpnBypass.Controls.Add(this.chkVpnBypassBasic);
            this.groupBoxVpnBypass.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxVpnBypass.Location = new System.Drawing.Point(13, 1504);
            this.groupBoxVpnBypass.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxVpnBypass.Name = "groupBoxVpnBypass";
            this.groupBoxVpnBypass.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxVpnBypass.Size = new System.Drawing.Size(757, 98);
            this.groupBoxVpnBypass.TabIndex = 5;
            this.groupBoxVpnBypass.TabStop = false;
            this.groupBoxVpnBypass.Text = "VPN Bypass";
            // 
            // chkVpnBypassAdvanced
            // 
            this.chkVpnBypassAdvanced.AutoSize = true;
            this.chkVpnBypassAdvanced.Location = new System.Drawing.Point(20, 59);
            this.chkVpnBypassAdvanced.Margin = new System.Windows.Forms.Padding(4);
            this.chkVpnBypassAdvanced.Name = "chkVpnBypassAdvanced";
            this.chkVpnBypassAdvanced.Size = new System.Drawing.Size(396, 20);
            this.chkVpnBypassAdvanced.TabIndex = 1;
            this.chkVpnBypassAdvanced.Text = "Сложный VPN bypass (отслеживание через IP Helper API)";
            this.chkVpnBypassAdvanced.UseVisualStyleBackColor = true;
            // 
            // chkVpnBypassBasic
            // 
            this.chkVpnBypassBasic.AutoSize = true;
            this.chkVpnBypassBasic.Location = new System.Drawing.Point(20, 31);
            this.chkVpnBypassBasic.Margin = new System.Windows.Forms.Padding(4);
            this.chkVpnBypassBasic.Name = "chkVpnBypassBasic";
            this.chkVpnBypassBasic.Size = new System.Drawing.Size(315, 20);
            this.chkVpnBypassBasic.TabIndex = 0;
            this.chkVpnBypassBasic.Text = "Простой VPN bypass (показать реальный IP)";
            this.chkVpnBypassBasic.UseVisualStyleBackColor = true;
            // 
            // groupBoxPhase3
            // 
            this.groupBoxPhase3.Controls.Add(this.chkHighPriorityThreads);
            this.groupBoxPhase3.Controls.Add(this.chkSingleConsumerPattern);
            this.groupBoxPhase3.Controls.Add(this.lblUiProcessingRate);
            this.groupBoxPhase3.Controls.Add(this.numUiProcessingRate);
            this.groupBoxPhase3.Controls.Add(this.lblUiBatchSize);
            this.groupBoxPhase3.Controls.Add(this.numUiBatchSize);
            this.groupBoxPhase3.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxPhase3.Location = new System.Drawing.Point(13, 1304);
            this.groupBoxPhase3.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxPhase3.Name = "groupBoxPhase3";
            this.groupBoxPhase3.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxPhase3.Size = new System.Drawing.Size(757, 200);
            this.groupBoxPhase3.TabIndex = 6;
            this.groupBoxPhase3.TabStop = false;
            this.groupBoxPhase3.Text = "Phase 3: Thread Priority & Single Consumer";
            // 
            // chkHighPriorityThreads
            // 
            this.chkHighPriorityThreads.AutoSize = true;
            this.chkHighPriorityThreads.Location = new System.Drawing.Point(20, 31);
            this.chkHighPriorityThreads.Margin = new System.Windows.Forms.Padding(4);
            this.chkHighPriorityThreads.Name = "chkHighPriorityThreads";
            this.chkHighPriorityThreads.Size = new System.Drawing.Size(277, 20);
            this.chkHighPriorityThreads.TabIndex = 0;
            this.chkHighPriorityThreads.Text = "Высокий приоритет для PCAP потоков";
            this.chkHighPriorityThreads.UseVisualStyleBackColor = true;
            // 
            // chkSingleConsumerPattern
            // 
            this.chkSingleConsumerPattern.AutoSize = true;
            this.chkSingleConsumerPattern.Location = new System.Drawing.Point(20, 59);
            this.chkSingleConsumerPattern.Margin = new System.Windows.Forms.Padding(4);
            this.chkSingleConsumerPattern.Name = "chkSingleConsumerPattern";
            this.chkSingleConsumerPattern.Size = new System.Drawing.Size(298, 20);
            this.chkSingleConsumerPattern.TabIndex = 1;
            this.chkSingleConsumerPattern.Text = "Single Consumer Pattern для UI обновлений";
            this.chkSingleConsumerPattern.UseVisualStyleBackColor = true;
            // 
            // lblUiProcessingRate
            // 
            this.lblUiProcessingRate.AutoSize = true;
            this.lblUiProcessingRate.Location = new System.Drawing.Point(20, 95);
            this.lblUiProcessingRate.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblUiProcessingRate.Name = "lblUiProcessingRate";
            this.lblUiProcessingRate.Size = new System.Drawing.Size(190, 16);
            this.lblUiProcessingRate.TabIndex = 2;
            this.lblUiProcessingRate.Text = "Частота обработки UI (FPS):";
            // 
            // numUiProcessingRate
            // 
            this.numUiProcessingRate.Location = new System.Drawing.Point(228, 93);
            this.numUiProcessingRate.Margin = new System.Windows.Forms.Padding(4);
            this.numUiProcessingRate.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.numUiProcessingRate.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numUiProcessingRate.Name = "numUiProcessingRate";
            this.numUiProcessingRate.Size = new System.Drawing.Size(80, 22);
            this.numUiProcessingRate.TabIndex = 3;
            this.numUiProcessingRate.Value = new decimal(new int[] {
            60,
            0,
            0,
            0});
            // 
            // lblUiBatchSize
            // 
            this.lblUiBatchSize.AutoSize = true;
            this.lblUiBatchSize.Location = new System.Drawing.Point(20, 125);
            this.lblUiBatchSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblUiBatchSize.Name = "lblUiBatchSize";
            this.lblUiBatchSize.Size = new System.Drawing.Size(155, 16);
            this.lblUiBatchSize.TabIndex = 4;
            this.lblUiBatchSize.Text = "Размер пакета UI (шт.):";
            // 
            // numUiBatchSize
            // 
            this.numUiBatchSize.Location = new System.Drawing.Point(228, 123);
            this.numUiBatchSize.Margin = new System.Windows.Forms.Padding(4);
            this.numUiBatchSize.Maximum = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.numUiBatchSize.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numUiBatchSize.Name = "numUiBatchSize";
            this.numUiBatchSize.Size = new System.Drawing.Size(80, 22);
            this.numUiBatchSize.TabIndex = 5;
            this.numUiBatchSize.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // groupBoxPhase2
            // 
            this.groupBoxPhase2.Controls.Add(this.chkVirtualModeListView);
            this.groupBoxPhase2.Controls.Add(this.lblVirtualModeThreshold);
            this.groupBoxPhase2.Controls.Add(this.numVirtualModeThreshold);
            this.groupBoxPhase2.Controls.Add(this.lblRingBufferSize);
            this.groupBoxPhase2.Controls.Add(this.numRingBufferSize);
            this.groupBoxPhase2.Controls.Add(this.chkShowVirtualModeStats);
            this.groupBoxPhase2.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxPhase2.Location = new System.Drawing.Point(13, 1154);
            this.groupBoxPhase2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxPhase2.Name = "groupBoxPhase2";
            this.groupBoxPhase2.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxPhase2.Size = new System.Drawing.Size(757, 150);
            this.groupBoxPhase2.TabIndex = 7;
            this.groupBoxPhase2.TabStop = false;
            this.groupBoxPhase2.Text = "Phase 2: VirtualMode ListView";
            // 
            // chkVirtualModeListView
            // 
            this.chkVirtualModeListView.AutoSize = true;
            this.chkVirtualModeListView.Location = new System.Drawing.Point(20, 31);
            this.chkVirtualModeListView.Margin = new System.Windows.Forms.Padding(4);
            this.chkVirtualModeListView.Name = "chkVirtualModeListView";
            this.chkVirtualModeListView.Size = new System.Drawing.Size(289, 20);
            this.chkVirtualModeListView.TabIndex = 0;
            this.chkVirtualModeListView.Text = "Автоматический VirtualMode для ListView";
            this.chkVirtualModeListView.UseVisualStyleBackColor = true;
            // 
            // lblVirtualModeThreshold
            // 
            this.lblVirtualModeThreshold.AutoSize = true;
            this.lblVirtualModeThreshold.Location = new System.Drawing.Point(20, 65);
            this.lblVirtualModeThreshold.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblVirtualModeThreshold.Name = "lblVirtualModeThreshold";
            this.lblVirtualModeThreshold.Size = new System.Drawing.Size(207, 16);
            this.lblVirtualModeThreshold.TabIndex = 1;
            this.lblVirtualModeThreshold.Text = "Порог переключения (пакеты):";
            // 
            // numVirtualModeThreshold
            // 
            this.numVirtualModeThreshold.Location = new System.Drawing.Point(248, 63);
            this.numVirtualModeThreshold.Margin = new System.Windows.Forms.Padding(4);
            this.numVirtualModeThreshold.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numVirtualModeThreshold.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numVirtualModeThreshold.Name = "numVirtualModeThreshold";
            this.numVirtualModeThreshold.Size = new System.Drawing.Size(80, 22);
            this.numVirtualModeThreshold.TabIndex = 2;
            this.numVirtualModeThreshold.Value = new decimal(new int[] {
            2000,
            0,
            0,
            0});
            // 
            // lblRingBufferSize
            // 
            this.lblRingBufferSize.AutoSize = true;
            this.lblRingBufferSize.Location = new System.Drawing.Point(361, 65);
            this.lblRingBufferSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblRingBufferSize.Name = "lblRingBufferSize";
            this.lblRingBufferSize.Size = new System.Drawing.Size(144, 16);
            this.lblRingBufferSize.TabIndex = 3;
            this.lblRingBufferSize.Text = "Размер буфера (шт.):";
            // 
            // numRingBufferSize
            // 
            this.numRingBufferSize.Location = new System.Drawing.Point(523, 63);
            this.numRingBufferSize.Margin = new System.Windows.Forms.Padding(4);
            this.numRingBufferSize.Maximum = new decimal(new int[] {
            50000,
            0,
            0,
            0});
            this.numRingBufferSize.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numRingBufferSize.Name = "numRingBufferSize";
            this.numRingBufferSize.Size = new System.Drawing.Size(80, 22);
            this.numRingBufferSize.TabIndex = 4;
            this.numRingBufferSize.Value = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            // 
            // chkShowVirtualModeStats
            // 
            this.chkShowVirtualModeStats.AutoSize = true;
            this.chkShowVirtualModeStats.Location = new System.Drawing.Point(20, 95);
            this.chkShowVirtualModeStats.Margin = new System.Windows.Forms.Padding(4);
            this.chkShowVirtualModeStats.Name = "chkShowVirtualModeStats";
            this.chkShowVirtualModeStats.Size = new System.Drawing.Size(267, 20);
            this.chkShowVirtualModeStats.TabIndex = 5;
            this.chkShowVirtualModeStats.Text = "Показывать диагностику VirtualMode";
            this.chkShowVirtualModeStats.UseVisualStyleBackColor = true;
            // 
            // groupBoxPhase1
            // 
            this.groupBoxPhase1.Controls.Add(this.chkAntiReentrancy);
            this.groupBoxPhase1.Controls.Add(this.chkRtssThrottling);
            this.groupBoxPhase1.Controls.Add(this.chkPcapOptimization);
            this.groupBoxPhase1.Controls.Add(this.lblPcapKernelBufferMb);
            this.groupBoxPhase1.Controls.Add(this.numPcapKernelBufferMb);
            this.groupBoxPhase1.Controls.Add(this.lblPcapMinToCopy);
            this.groupBoxPhase1.Controls.Add(this.numPcapMinToCopy);
            this.groupBoxPhase1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxPhase1.Location = new System.Drawing.Point(13, 1004);
            this.groupBoxPhase1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxPhase1.Name = "groupBoxPhase1";
            this.groupBoxPhase1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxPhase1.Size = new System.Drawing.Size(757, 150);
            this.groupBoxPhase1.TabIndex = 8;
            this.groupBoxPhase1.TabStop = false;
            this.groupBoxPhase1.Text = "Phase 1: Anti-Reentrancy & PCAP Optimization";
            // 
            // chkAntiReentrancy
            // 
            this.chkAntiReentrancy.AutoSize = true;
            this.chkAntiReentrancy.Location = new System.Drawing.Point(20, 31);
            this.chkAntiReentrancy.Margin = new System.Windows.Forms.Padding(4);
            this.chkAntiReentrancy.Name = "chkAntiReentrancy";
            this.chkAntiReentrancy.Size = new System.Drawing.Size(228, 20);
            this.chkAntiReentrancy.TabIndex = 0;
            this.chkAntiReentrancy.Text = "Защита от реэнтерабельности";
            this.chkAntiReentrancy.UseVisualStyleBackColor = true;
            // 
            // chkRtssThrottling
            // 
            this.chkRtssThrottling.AutoSize = true;
            this.chkRtssThrottling.Location = new System.Drawing.Point(320, 31);
            this.chkRtssThrottling.Margin = new System.Windows.Forms.Padding(4);
            this.chkRtssThrottling.Name = "chkRtssThrottling";
            this.chkRtssThrottling.Size = new System.Drawing.Size(218, 20);
            this.chkRtssThrottling.TabIndex = 1;
            this.chkRtssThrottling.Text = "Троттлинг RTSS обновлений";
            this.chkRtssThrottling.UseVisualStyleBackColor = true;
            // 
            // chkPcapOptimization
            // 
            this.chkPcapOptimization.AutoSize = true;
            this.chkPcapOptimization.Location = new System.Drawing.Point(20, 59);
            this.chkPcapOptimization.Margin = new System.Windows.Forms.Padding(4);
            this.chkPcapOptimization.Name = "chkPcapOptimization";
            this.chkPcapOptimization.Size = new System.Drawing.Size(216, 20);
            this.chkPcapOptimization.TabIndex = 2;
            this.chkPcapOptimization.Text = "Оптимизация PCAP буферов";
            this.chkPcapOptimization.UseVisualStyleBackColor = true;
            // 
            // lblPcapKernelBufferMb
            // 
            this.lblPcapKernelBufferMb.AutoSize = true;
            this.lblPcapKernelBufferMb.Location = new System.Drawing.Point(20, 95);
            this.lblPcapKernelBufferMb.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPcapKernelBufferMb.Name = "lblPcapKernelBufferMb";
            this.lblPcapKernelBufferMb.Size = new System.Drawing.Size(168, 16);
            this.lblPcapKernelBufferMb.TabIndex = 3;
            this.lblPcapKernelBufferMb.Text = "Kernel Buffer размер (MB):";
            // 
            // numPcapKernelBufferMb
            // 
            this.numPcapKernelBufferMb.Location = new System.Drawing.Point(206, 93);
            this.numPcapKernelBufferMb.Margin = new System.Windows.Forms.Padding(4);
            this.numPcapKernelBufferMb.Maximum = new decimal(new int[] {
            64,
            0,
            0,
            0});
            this.numPcapKernelBufferMb.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numPcapKernelBufferMb.Name = "numPcapKernelBufferMb";
            this.numPcapKernelBufferMb.Size = new System.Drawing.Size(80, 22);
            this.numPcapKernelBufferMb.TabIndex = 4;
            this.numPcapKernelBufferMb.Value = new decimal(new int[] {
            8,
            0,
            0,
            0});
            // 
            // lblPcapMinToCopy
            // 
            this.lblPcapMinToCopy.AutoSize = true;
            this.lblPcapMinToCopy.Location = new System.Drawing.Point(320, 95);
            this.lblPcapMinToCopy.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPcapMinToCopy.Name = "lblPcapMinToCopy";
            this.lblPcapMinToCopy.Size = new System.Drawing.Size(130, 16);
            this.lblPcapMinToCopy.TabIndex = 5;
            this.lblPcapMinToCopy.Text = "Min To Copy (bytes):";
            // 
            // numPcapMinToCopy
            // 
            this.numPcapMinToCopy.Location = new System.Drawing.Point(468, 93);
            this.numPcapMinToCopy.Margin = new System.Windows.Forms.Padding(4);
            this.numPcapMinToCopy.Maximum = new decimal(new int[] {
            65536,
            0,
            0,
            0});
            this.numPcapMinToCopy.Name = "numPcapMinToCopy";
            this.numPcapMinToCopy.Size = new System.Drawing.Size(80, 22);
            this.numPcapMinToCopy.TabIndex = 6;
            this.numPcapMinToCopy.Value = new decimal(new int[] {
            4096,
            0,
            0,
            0});
            // 
            // groupBoxSpikeDetection
            // 
            this.groupBoxSpikeDetection.Controls.Add(this.chkSpikeDetectionEnable);
            this.groupBoxSpikeDetection.Controls.Add(this.chkSpikeMetricPing);
            this.groupBoxSpikeDetection.Controls.Add(this.chkSpikeMetricTickrate);
            this.groupBoxSpikeDetection.Controls.Add(this.chkSpikeMetricTicktime);
            this.groupBoxSpikeDetection.Controls.Add(this.lblSpikeSensitivity);
            this.groupBoxSpikeDetection.Controls.Add(this.cmbSpikeSensitivity);
            this.groupBoxSpikeDetection.Controls.Add(this.lblSpikeDisplayMode);
            this.groupBoxSpikeDetection.Controls.Add(this.cmbSpikeDisplayMode);
            this.groupBoxSpikeDetection.Controls.Add(this.lblSpikeMinDuration);
            this.groupBoxSpikeDetection.Controls.Add(this.numSpikeMinDuration);
            this.groupBoxSpikeDetection.Controls.Add(this.lblSpikeHistory);
            this.groupBoxSpikeDetection.Controls.Add(this.numSpikeHistorySize);
            this.groupBoxSpikeDetection.Controls.Add(this.chkSpikeAutoCalibration);
            this.groupBoxSpikeDetection.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxSpikeDetection.Location = new System.Drawing.Point(13, 804);
            this.groupBoxSpikeDetection.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxSpikeDetection.Name = "groupBoxSpikeDetection";
            this.groupBoxSpikeDetection.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxSpikeDetection.Size = new System.Drawing.Size(757, 200);
            this.groupBoxSpikeDetection.TabIndex = 5;
            this.groupBoxSpikeDetection.TabStop = false;
            this.groupBoxSpikeDetection.Text = "Детекция спайков";
            // 
            // chkSpikeDetectionEnable
            // 
            this.chkSpikeDetectionEnable.AutoSize = true;
            this.chkSpikeDetectionEnable.Location = new System.Drawing.Point(20, 30);
            this.chkSpikeDetectionEnable.Margin = new System.Windows.Forms.Padding(4);
            this.chkSpikeDetectionEnable.Name = "chkSpikeDetectionEnable";
            this.chkSpikeDetectionEnable.Size = new System.Drawing.Size(214, 20);
            this.chkSpikeDetectionEnable.TabIndex = 0;
            this.chkSpikeDetectionEnable.Text = "Включить детекцию спайков";
            this.chkSpikeDetectionEnable.UseVisualStyleBackColor = true;
            // 
            // chkSpikeMetricPing
            // 
            this.chkSpikeMetricPing.AutoSize = true;
            this.chkSpikeMetricPing.Location = new System.Drawing.Point(250, 30);
            this.chkSpikeMetricPing.Margin = new System.Windows.Forms.Padding(4);
            this.chkSpikeMetricPing.Name = "chkSpikeMetricPing";
            this.chkSpikeMetricPing.Size = new System.Drawing.Size(58, 20);
            this.chkSpikeMetricPing.TabIndex = 1;
            this.chkSpikeMetricPing.Text = "Пинг";
            this.chkSpikeMetricPing.UseVisualStyleBackColor = true;
            // 
            // chkSpikeMetricTickrate
            // 
            this.chkSpikeMetricTickrate.AutoSize = true;
            this.chkSpikeMetricTickrate.Location = new System.Drawing.Point(320, 30);
            this.chkSpikeMetricTickrate.Margin = new System.Windows.Forms.Padding(4);
            this.chkSpikeMetricTickrate.Name = "chkSpikeMetricTickrate";
            this.chkSpikeMetricTickrate.Size = new System.Drawing.Size(81, 20);
            this.chkSpikeMetricTickrate.TabIndex = 2;
            this.chkSpikeMetricTickrate.Text = "Тикрейт";
            this.chkSpikeMetricTickrate.UseVisualStyleBackColor = true;
            // 
            // chkSpikeMetricTicktime
            // 
            this.chkSpikeMetricTicktime.AutoSize = true;
            this.chkSpikeMetricTicktime.Location = new System.Drawing.Point(410, 30);
            this.chkSpikeMetricTicktime.Margin = new System.Windows.Forms.Padding(4);
            this.chkSpikeMetricTicktime.Name = "chkSpikeMetricTicktime";
            this.chkSpikeMetricTicktime.Size = new System.Drawing.Size(82, 20);
            this.chkSpikeMetricTicktime.TabIndex = 3;
            this.chkSpikeMetricTicktime.Text = "Тиктайм";
            this.chkSpikeMetricTicktime.UseVisualStyleBackColor = true;
            // 
            // lblSpikeSensitivity
            // 
            this.lblSpikeSensitivity.AutoSize = true;
            this.lblSpikeSensitivity.Location = new System.Drawing.Point(20, 65);
            this.lblSpikeSensitivity.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSpikeSensitivity.Name = "lblSpikeSensitivity";
            this.lblSpikeSensitivity.Size = new System.Drawing.Size(132, 16);
            this.lblSpikeSensitivity.TabIndex = 4;
            this.lblSpikeSensitivity.Text = "Чувствительность:";
            // 
            // cmbSpikeSensitivity
            // 
            this.cmbSpikeSensitivity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSpikeSensitivity.FormattingEnabled = true;
            this.cmbSpikeSensitivity.Location = new System.Drawing.Point(176, 62);
            this.cmbSpikeSensitivity.Margin = new System.Windows.Forms.Padding(4);
            this.cmbSpikeSensitivity.Name = "cmbSpikeSensitivity";
            this.cmbSpikeSensitivity.Size = new System.Drawing.Size(120, 24);
            this.cmbSpikeSensitivity.TabIndex = 5;
            // 
            // lblSpikeDisplayMode
            // 
            this.lblSpikeDisplayMode.AutoSize = true;
            this.lblSpikeDisplayMode.Location = new System.Drawing.Point(326, 65);
            this.lblSpikeDisplayMode.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSpikeDisplayMode.Name = "lblSpikeDisplayMode";
            this.lblSpikeDisplayMode.Size = new System.Drawing.Size(143, 16);
            this.lblSpikeDisplayMode.TabIndex = 6;
            this.lblSpikeDisplayMode.Text = "Режим отображения:";
            // 
            // cmbSpikeDisplayMode
            // 
            this.cmbSpikeDisplayMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbSpikeDisplayMode.FormattingEnabled = true;
            this.cmbSpikeDisplayMode.Location = new System.Drawing.Point(499, 62);
            this.cmbSpikeDisplayMode.Margin = new System.Windows.Forms.Padding(4);
            this.cmbSpikeDisplayMode.Name = "cmbSpikeDisplayMode";
            this.cmbSpikeDisplayMode.Size = new System.Drawing.Size(150, 24);
            this.cmbSpikeDisplayMode.TabIndex = 7;
            // 
            // lblSpikeMinDuration
            // 
            this.lblSpikeMinDuration.AutoSize = true;
            this.lblSpikeMinDuration.Location = new System.Drawing.Point(20, 105);
            this.lblSpikeMinDuration.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSpikeMinDuration.Name = "lblSpikeMinDuration";
            this.lblSpikeMinDuration.Size = new System.Drawing.Size(161, 16);
            this.lblSpikeMinDuration.TabIndex = 8;
            this.lblSpikeMinDuration.Text = "Мин. длительность (мс):";
            // 
            // numSpikeMinDuration
            // 
            this.numSpikeMinDuration.Location = new System.Drawing.Point(206, 103);
            this.numSpikeMinDuration.Margin = new System.Windows.Forms.Padding(4);
            this.numSpikeMinDuration.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.numSpikeMinDuration.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numSpikeMinDuration.Name = "numSpikeMinDuration";
            this.numSpikeMinDuration.Size = new System.Drawing.Size(80, 22);
            this.numSpikeMinDuration.TabIndex = 9;
            this.numSpikeMinDuration.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // lblSpikeHistory
            // 
            this.lblSpikeHistory.AutoSize = true;
            this.lblSpikeHistory.Location = new System.Drawing.Point(326, 105);
            this.lblSpikeHistory.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSpikeHistory.Name = "lblSpikeHistory";
            this.lblSpikeHistory.Size = new System.Drawing.Size(117, 16);
            this.lblSpikeHistory.TabIndex = 10;
            this.lblSpikeHistory.Text = "Размер истории:";
            // 
            // numSpikeHistorySize
            // 
            this.numSpikeHistorySize.Location = new System.Drawing.Point(468, 103);
            this.numSpikeHistorySize.Margin = new System.Windows.Forms.Padding(4);
            this.numSpikeHistorySize.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numSpikeHistorySize.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numSpikeHistorySize.Name = "numSpikeHistorySize";
            this.numSpikeHistorySize.Size = new System.Drawing.Size(80, 22);
            this.numSpikeHistorySize.TabIndex = 11;
            this.numSpikeHistorySize.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // chkSpikeAutoCalibration
            // 
            this.chkSpikeAutoCalibration.AutoSize = true;
            this.chkSpikeAutoCalibration.Location = new System.Drawing.Point(20, 140);
            this.chkSpikeAutoCalibration.Margin = new System.Windows.Forms.Padding(4);
            this.chkSpikeAutoCalibration.Name = "chkSpikeAutoCalibration";
            this.chkSpikeAutoCalibration.Size = new System.Drawing.Size(216, 20);
            this.chkSpikeAutoCalibration.TabIndex = 12;
            this.chkSpikeAutoCalibration.Text = "Автоматическая калибровка";
            this.chkSpikeAutoCalibration.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.chkStunEnable);
            this.groupBox5.Controls.Add(this.chkShowPingSpikes);
            this.groupBox5.Controls.Add(this.lblPingSpikeThreshold);
            this.groupBox5.Controls.Add(this.numPingSpikeThreshold);
            this.groupBox5.Controls.Add(this.chkRtssOnlyActive);
            this.groupBox5.Controls.Add(this.chkEnableIPv6);
            this.groupBox5.Controls.Add(this.chkDedupMultiNic);
            this.groupBox5.Controls.Add(this.chkTickrateSmoothing);
            this.groupBox5.Controls.Add(this.chkPingGraphOverlaySmoothing);
            this.groupBox5.Controls.Add(this.chkTickrateGraphOverlaySmoothing);
            this.groupBox5.Controls.Add(this.chkTicktimeGraphOverlaySmoothing);
            this.groupBox5.Controls.Add(this.chkPingValueOverlaySmoothing);
            this.groupBox5.Controls.Add(this.chkPingValueGuiSmoothing);
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
            this.groupBox5.Size = new System.Drawing.Size(757, 400);
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
            this.chkStunEnable.Size = new System.Drawing.Size(321, 20);
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
            this.chkShowPingSpikes.Size = new System.Drawing.Size(316, 20);
            this.chkShowPingSpikes.TabIndex = 9;
            this.chkShowPingSpikes.Text = "Показывать индикатор (!) при спайках пинга";
            this.chkShowPingSpikes.UseVisualStyleBackColor = true;
            // 
            // lblPingSpikeThreshold
            // 
            this.lblPingSpikeThreshold.AutoSize = true;
            this.lblPingSpikeThreshold.Location = new System.Drawing.Point(333, 115);
            this.lblPingSpikeThreshold.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPingSpikeThreshold.Name = "lblPingSpikeThreshold";
            this.lblPingSpikeThreshold.Size = new System.Drawing.Size(167, 16);
            this.lblPingSpikeThreshold.TabIndex = 10;
            this.lblPingSpikeThreshold.Text = "Порог спайка пинга (мс):";
            // 
            // numPingSpikeThreshold
            // 
            this.numPingSpikeThreshold.Location = new System.Drawing.Point(518, 113);
            this.numPingSpikeThreshold.Margin = new System.Windows.Forms.Padding(4);
            this.numPingSpikeThreshold.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.numPingSpikeThreshold.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numPingSpikeThreshold.Name = "numPingSpikeThreshold";
            this.numPingSpikeThreshold.Size = new System.Drawing.Size(80, 22);
            this.numPingSpikeThreshold.TabIndex = 11;
            this.numPingSpikeThreshold.Value = new decimal(new int[] {
            150,
            0,
            0,
            0});
            // 
            // chkRtssOnlyActive
            // 
            this.chkRtssOnlyActive.AutoSize = true;
            this.chkRtssOnlyActive.Location = new System.Drawing.Point(333, 31);
            this.chkRtssOnlyActive.Margin = new System.Windows.Forms.Padding(4);
            this.chkRtssOnlyActive.Name = "chkRtssOnlyActive";
            this.chkRtssOnlyActive.Size = new System.Drawing.Size(303, 20);
            this.chkRtssOnlyActive.TabIndex = 8;
            this.chkRtssOnlyActive.Text = "RTSS: выводить только активный процесс";
            this.chkRtssOnlyActive.UseVisualStyleBackColor = true;
            // 
            // chkEnableIPv6
            // 
            this.chkEnableIPv6.AutoSize = true;
            this.chkEnableIPv6.Location = new System.Drawing.Point(20, 255);
            this.chkEnableIPv6.Margin = new System.Windows.Forms.Padding(4);
            this.chkEnableIPv6.Name = "chkEnableIPv6";
            this.chkEnableIPv6.Size = new System.Drawing.Size(170, 20);
            this.chkEnableIPv6.TabIndex = 7;
            this.chkEnableIPv6.Text = "Включить анализ IPv6";
            this.chkEnableIPv6.UseVisualStyleBackColor = true;
            // 
            // chkDedupMultiNic
            // 
            this.chkDedupMultiNic.AutoSize = true;
            this.chkDedupMultiNic.Location = new System.Drawing.Point(20, 227);
            this.chkDedupMultiNic.Margin = new System.Windows.Forms.Padding(4);
            this.chkDedupMultiNic.Name = "chkDedupMultiNic";
            this.chkDedupMultiNic.Size = new System.Drawing.Size(274, 20);
            this.chkDedupMultiNic.TabIndex = 6;
            this.chkDedupMultiNic.Text = "Анти-дубли пакетов в мульти-режиме";
            this.chkDedupMultiNic.UseVisualStyleBackColor = true;
            // 
            // chkTickrateSmoothing
            // 
            this.chkTickrateSmoothing.AutoSize = true;
            this.chkTickrateSmoothing.Location = new System.Drawing.Point(20, 115);
            this.chkTickrateSmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateSmoothing.Name = "chkTickrateSmoothing";
            this.chkTickrateSmoothing.Size = new System.Drawing.Size(277, 20);
            this.chkTickrateSmoothing.TabIndex = 4;
            this.chkTickrateSmoothing.Text = "Сглаживание графика тикрейта (EMA)";
            this.chkTickrateSmoothing.UseVisualStyleBackColor = true;
            // 
            // chkPingGraphOverlaySmoothing
            // 
            this.chkPingGraphOverlaySmoothing.AutoSize = true;
            this.chkPingGraphOverlaySmoothing.Location = new System.Drawing.Point(20, 143);
            this.chkPingGraphOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkPingGraphOverlaySmoothing.Name = "chkPingGraphOverlaySmoothing";
            this.chkPingGraphOverlaySmoothing.Size = new System.Drawing.Size(284, 20);
            this.chkPingGraphOverlaySmoothing.TabIndex = 10;
            this.chkPingGraphOverlaySmoothing.Text = "Сглаживание графика пинга в оверлее";
            this.chkPingGraphOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkTickrateGraphOverlaySmoothing
            // 
            this.chkTickrateGraphOverlaySmoothing.AutoSize = true;
            this.chkTickrateGraphOverlaySmoothing.Location = new System.Drawing.Point(20, 171);
            this.chkTickrateGraphOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateGraphOverlaySmoothing.Name = "chkTickrateGraphOverlaySmoothing";
            this.chkTickrateGraphOverlaySmoothing.Size = new System.Drawing.Size(307, 20);
            this.chkTickrateGraphOverlaySmoothing.TabIndex = 11;
            this.chkTickrateGraphOverlaySmoothing.Text = "Сглаживание графика тикрейта в оверлее";
            this.chkTickrateGraphOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkTicktimeGraphOverlaySmoothing
            // 
            this.chkTicktimeGraphOverlaySmoothing.AutoSize = true;
            this.chkTicktimeGraphOverlaySmoothing.Location = new System.Drawing.Point(20, 199);
            this.chkTicktimeGraphOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTicktimeGraphOverlaySmoothing.Name = "chkTicktimeGraphOverlaySmoothing";
            this.chkTicktimeGraphOverlaySmoothing.Size = new System.Drawing.Size(308, 20);
            this.chkTicktimeGraphOverlaySmoothing.TabIndex = 12;
            this.chkTicktimeGraphOverlaySmoothing.Text = "Сглаживание графика тиктайма в оверлее";
            this.chkTicktimeGraphOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkPingValueOverlaySmoothing
            // 
            this.chkPingValueOverlaySmoothing.AutoSize = true;
            this.chkPingValueOverlaySmoothing.Location = new System.Drawing.Point(20, 283);
            this.chkPingValueOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkPingValueOverlaySmoothing.Name = "chkPingValueOverlaySmoothing";
            this.chkPingValueOverlaySmoothing.Size = new System.Drawing.Size(292, 20);
            this.chkPingValueOverlaySmoothing.TabIndex = 13;
            this.chkPingValueOverlaySmoothing.Text = "Сглаживание значений пинга в оверлее";
            this.chkPingValueOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkPingValueGuiSmoothing
            // 
            this.chkPingValueGuiSmoothing.AutoSize = true;
            this.chkPingValueGuiSmoothing.Location = new System.Drawing.Point(20, 311);
            this.chkPingValueGuiSmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkPingValueGuiSmoothing.Name = "chkPingValueGuiSmoothing";
            this.chkPingValueGuiSmoothing.Size = new System.Drawing.Size(259, 20);
            this.chkPingValueGuiSmoothing.TabIndex = 14;
            this.chkPingValueGuiSmoothing.Text = "Сглаживание значений пинга в GUI";
            this.chkPingValueGuiSmoothing.UseVisualStyleBackColor = true;
            // 
            // chkTickrateValueOverlaySmoothing
            // 
            this.chkTickrateValueOverlaySmoothing.AutoSize = true;
            this.chkTickrateValueOverlaySmoothing.Location = new System.Drawing.Point(20, 339);
            this.chkTickrateValueOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateValueOverlaySmoothing.Name = "chkTickrateValueOverlaySmoothing";
            this.chkTickrateValueOverlaySmoothing.Size = new System.Drawing.Size(315, 20);
            this.chkTickrateValueOverlaySmoothing.TabIndex = 15;
            this.chkTickrateValueOverlaySmoothing.Text = "Сглаживание значений тикрейта в оверлее";
            this.chkTickrateValueOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkTrafficValueOverlaySmoothing
            // 
            this.chkTrafficValueOverlaySmoothing.AutoSize = true;
            this.chkTrafficValueOverlaySmoothing.Location = new System.Drawing.Point(20, 367);
            this.chkTrafficValueOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTrafficValueOverlaySmoothing.Name = "chkTrafficValueOverlaySmoothing";
            this.chkTrafficValueOverlaySmoothing.Size = new System.Drawing.Size(311, 20);
            this.chkTrafficValueOverlaySmoothing.TabIndex = 16;
            this.chkTrafficValueOverlaySmoothing.Text = "Сглаживание значений трафика в оверлее";
            this.chkTrafficValueOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkPingTargetActiveOnly
            // 
            this.chkPingTargetActiveOnly.AutoSize = true;
            this.chkPingTargetActiveOnly.Location = new System.Drawing.Point(333, 135);
            this.chkPingTargetActiveOnly.Margin = new System.Windows.Forms.Padding(4);
            this.chkPingTargetActiveOnly.Name = "chkPingTargetActiveOnly";
            this.chkPingTargetActiveOnly.Size = new System.Drawing.Size(314, 20);
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
            this.chkPingFallbackIcmp.Size = new System.Drawing.Size(295, 20);
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
            this.chkPingTcpPrefer.Size = new System.Drawing.Size(320, 20);
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
            this.chkPingBindToInterface.Size = new System.Drawing.Size(315, 20);
            this.chkPingBindToInterface.TabIndex = 0;
            this.chkPingBindToInterface.Text = "Пинг привязывать к активному интерфейсу";
            this.chkPingBindToInterface.UseVisualStyleBackColor = true;
            // 
            // groupBox4
            // 
            this.groupBox4.Controls.Add(this.chkIgnoreVirtualAdapters);
            this.groupBox4.Controls.Add(this.chkCaptureAllAdapters);
            this.groupBox4.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox4.Location = new System.Drawing.Point(13, 306);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox4.Size = new System.Drawing.Size(757, 98);
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
            this.chkIgnoreVirtualAdapters.Size = new System.Drawing.Size(277, 20);
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
            this.chkCaptureAllAdapters.Size = new System.Drawing.Size(235, 20);
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
            this.groupBox3.Size = new System.Drawing.Size(757, 98);
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
            this.chkBpfFilter.Size = new System.Drawing.Size(201, 20);
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
            this.groupBox2.Size = new System.Drawing.Size(757, 98);
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
            this.chkOverlayFps.Size = new System.Drawing.Size(191, 20);
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
            this.groupBox1.Size = new System.Drawing.Size(757, 98);
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
            this.chkLiveMaxRows.Size = new System.Drawing.Size(221, 20);
            this.chkLiveMaxRows.TabIndex = 0;
            this.chkLiveMaxRows.Text = "Ограничить строки в таблице";
            this.chkLiveMaxRows.UseVisualStyleBackColor = true;
            this.chkLiveMaxRows.CheckedChanged += new System.EventHandler(this.chkLiveMaxRows_CheckedChanged);
            // 
            // panelButtons
            // 
            this.panelButtons.Controls.Add(this.btnOK);
            this.panelButtons.Controls.Add(this.btnApply);
            this.panelButtons.Controls.Add(this.btnCancel);
            this.panelButtons.Controls.Add(this.btnReset);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelButtons.Location = new System.Drawing.Point(0, 550);
            this.panelButtons.Margin = new System.Windows.Forms.Padding(4);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(800, 50);
            this.panelButtons.TabIndex = 1;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(469, 12);
            this.btnOK.Margin = new System.Windows.Forms.Padding(4);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 28);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "ОК";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnApply
            // 
            this.btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnApply.Location = new System.Drawing.Point(577, 12);
            this.btnApply.Margin = new System.Windows.Forms.Padding(4);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(100, 28);
            this.btnApply.TabIndex = 1;
            this.btnApply.Text = "Применить";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(685, 12);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 28);
            this.btnCancel.TabIndex = 2;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnReset
            // 
            this.btnReset.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnReset.Location = new System.Drawing.Point(12, 12);
            this.btnReset.Margin = new System.Windows.Forms.Padding(4);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(120, 28);
            this.btnReset.TabIndex = 3;
            this.btnReset.Text = "Сброс (Опт.)";
            this.btnReset.UseVisualStyleBackColor = true;
            this.btnReset.Click += new System.EventHandler(this.btnReset_Click);
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
            this.btnSave.Visible = false;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // AdvancedSettingsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.panelButtons);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MinimumSize = new System.Drawing.Size(800, 400);
            this.Name = "AdvancedSettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Дополнительные настройки";
            this.panel1.ResumeLayout(false);
            this.groupBoxVpnBypass.ResumeLayout(false);
            this.groupBoxVpnBypass.PerformLayout();
            this.groupBoxPhase3.ResumeLayout(false);
            this.groupBoxPhase3.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numUiProcessingRate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numUiBatchSize)).EndInit();
            this.groupBoxPhase2.ResumeLayout(false);
            this.groupBoxPhase2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numVirtualModeThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRingBufferSize)).EndInit();
            this.groupBoxPhase1.ResumeLayout(false);
            this.groupBoxPhase1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPcapKernelBufferMb)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPcapMinToCopy)).EndInit();
            this.groupBoxSpikeDetection.ResumeLayout(false);
            this.groupBoxSpikeDetection.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numSpikeMinDuration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSpikeHistorySize)).EndInit();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPingSpikeThreshold)).EndInit();
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
            this.panelButtons.ResumeLayout(false);
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
    private System.Windows.Forms.CheckBox chkPingValueGuiSmoothing;
    private System.Windows.Forms.CheckBox chkTickrateValueOverlaySmoothing;
    private System.Windows.Forms.CheckBox chkTrafficValueOverlaySmoothing;
    private System.Windows.Forms.CheckBox chkDedupMultiNic;
    private System.Windows.Forms.CheckBox chkEnableIPv6;
    private System.Windows.Forms.CheckBox chkRtssOnlyActive;
    private System.Windows.Forms.CheckBox chkStunEnable;
    private System.Windows.Forms.CheckBox chkShowPingSpikes;
    private System.Windows.Forms.NumericUpDown numPingSpikeThreshold;
    private System.Windows.Forms.Label lblPingSpikeThreshold;

        private System.Windows.Forms.GroupBox groupBoxVpnBypass;
        private System.Windows.Forms.CheckBox chkVpnBypassBasic;
        private System.Windows.Forms.CheckBox chkVpnBypassAdvanced;
        
        // Performance Optimization Controls
        private System.Windows.Forms.GroupBox groupBoxPhase1;
        private System.Windows.Forms.CheckBox chkAntiReentrancy;
        private System.Windows.Forms.CheckBox chkRtssThrottling;
        private System.Windows.Forms.CheckBox chkPcapOptimization;
        private System.Windows.Forms.Label lblPcapKernelBufferMb;
        private System.Windows.Forms.NumericUpDown numPcapKernelBufferMb;
        private System.Windows.Forms.Label lblPcapMinToCopy;
        private System.Windows.Forms.NumericUpDown numPcapMinToCopy;
        
        private System.Windows.Forms.GroupBox groupBoxPhase2;
        private System.Windows.Forms.CheckBox chkVirtualModeListView;
        private System.Windows.Forms.Label lblVirtualModeThreshold;
        private System.Windows.Forms.NumericUpDown numVirtualModeThreshold;
        private System.Windows.Forms.Label lblRingBufferSize;
        private System.Windows.Forms.NumericUpDown numRingBufferSize;
        private System.Windows.Forms.CheckBox chkShowVirtualModeStats;
        
        private System.Windows.Forms.GroupBox groupBoxPhase3;
        private System.Windows.Forms.CheckBox chkHighPriorityThreads;
        private System.Windows.Forms.CheckBox chkSingleConsumerPattern;
        private System.Windows.Forms.Label lblUiProcessingRate;
        private System.Windows.Forms.NumericUpDown numUiProcessingRate;
        private System.Windows.Forms.Label lblUiBatchSize;
        private System.Windows.Forms.NumericUpDown numUiBatchSize;
        
        private System.Windows.Forms.GroupBox groupBoxSpikeDetection;
        private System.Windows.Forms.CheckBox chkSpikeDetectionEnable;
        private System.Windows.Forms.CheckBox chkSpikeMetricPing;
        private System.Windows.Forms.CheckBox chkSpikeMetricTickrate;
        private System.Windows.Forms.CheckBox chkSpikeMetricTicktime;
        private System.Windows.Forms.Label lblSpikeSensitivity;
        private System.Windows.Forms.ComboBox cmbSpikeSensitivity;
        private System.Windows.Forms.Label lblSpikeDisplayMode;
        private System.Windows.Forms.ComboBox cmbSpikeDisplayMode;
        private System.Windows.Forms.Label lblSpikeMinDuration;
        private System.Windows.Forms.NumericUpDown numSpikeMinDuration;
        private System.Windows.Forms.Label lblSpikeHistory;
        private System.Windows.Forms.NumericUpDown numSpikeHistorySize;
        private System.Windows.Forms.CheckBox chkSpikeAutoCalibration;
        
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnReset;
    }
}