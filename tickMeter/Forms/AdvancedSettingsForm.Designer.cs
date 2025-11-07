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
            this.groupBoxTickrateChart = new System.Windows.Forms.GroupBox();
            this.chkTickrateChartEnabled = new System.Windows.Forms.CheckBox();
            this.lblTickrateChartMode = new System.Windows.Forms.Label();
            this.cmbTickrateChartMode = new System.Windows.Forms.ComboBox();
            this.chkTickrateChartPerServer = new System.Windows.Forms.CheckBox();
            this.chkTickrateChartCompression = new System.Windows.Forms.CheckBox();
            this.chkTickrateChartTimeScale = new System.Windows.Forms.CheckBox();
            this.chkTickrateChartTrimming = new System.Windows.Forms.CheckBox();
            this.lblTickrateChartMaxPoints = new System.Windows.Forms.Label();
            this.numTickrateChartMaxPoints = new System.Windows.Forms.NumericUpDown();
            this.lblTickrateChartHistoryHours = new System.Windows.Forms.Label();
            this.numTickrateChartHistoryHours = new System.Windows.Forms.NumericUpDown();
            this.btnTickrateChartReset = new System.Windows.Forms.Button();
            this.groupBoxExtendedOverlay = new System.Windows.Forms.GroupBox();
            this.chkShowActiveProcess = new System.Windows.Forms.CheckBox();
            this.chkShowSessionTime = new System.Windows.Forms.CheckBox();
            this.chkShowExternalIP = new System.Windows.Forms.CheckBox();
            this.chkShowSessionStats = new System.Windows.Forms.CheckBox();
            this.chkShowServerInfo = new System.Windows.Forms.CheckBox();
            this.chkShowPacketCounters = new System.Windows.Forms.CheckBox();
            this.chkShowConnectionType = new System.Windows.Forms.CheckBox();
            this.chkShowDiagnosticInfo = new System.Windows.Forms.CheckBox();
            this.groupBoxDebugSettings = new System.Windows.Forms.GroupBox();
            this.chkEnableTextLogs = new System.Windows.Forms.CheckBox();
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
            this.groupBoxSpikeAdvanced = new System.Windows.Forms.GroupBox();
            this.lblEmaAlpha = new System.Windows.Forms.Label();
            this.numEmaAlpha = new System.Windows.Forms.NumericUpDown();
            this.lblEwSigmaAlpha = new System.Windows.Forms.Label();
            this.numEwSigmaAlpha = new System.Windows.Forms.NumericUpDown();
            this.lblSensitivityMultiplier = new System.Windows.Forms.Label();
            this.numSensitivityMultiplier = new System.Windows.Forms.NumericUpDown();
            this.lblHysteresisRatio = new System.Windows.Forms.Label();
            this.numHysteresisRatio = new System.Windows.Forms.NumericUpDown();
            this.lblRefractoryPeriod = new System.Windows.Forms.Label();
            this.numRefractoryPeriod = new System.Windows.Forms.NumericUpDown();
            this.lblMinEnergyThreshold = new System.Windows.Forms.Label();
            this.numMinEnergyThreshold = new System.Windows.Forms.NumericUpDown();
            this.lblInitWindowSize = new System.Windows.Forms.Label();
            this.numInitWindowSize = new System.Windows.Forms.NumericUpDown();
            this.btnResetSpikeDefaults = new System.Windows.Forms.Button();
            this.btnSpikePresetsSensitive = new System.Windows.Forms.Button();
            this.btnSpikePresetsBalanced = new System.Windows.Forms.Button();
            this.btnSpikePresetsConservative = new System.Windows.Forms.Button();
            this.groupBoxAlerts = new System.Windows.Forms.GroupBox();
            this.chkAlertSoundEnabled = new System.Windows.Forms.CheckBox();
            this.chkAlertDiscordEnabled = new System.Windows.Forms.CheckBox();
            this.lblAlertDiscordWebhook = new System.Windows.Forms.Label();
            this.txtAlertDiscordWebhook = new System.Windows.Forms.TextBox();
            this.lblAlertCooldown = new System.Windows.Forms.Label();
            this.numAlertCooldown = new System.Windows.Forms.NumericUpDown();
            this.btnTestDiscordAlert = new System.Windows.Forms.Button();
            this.btnTestSoundAlert = new System.Windows.Forms.Button();
            this.groupBoxAlertSounds = new System.Windows.Forms.GroupBox();
            this.lblAlertPingSoundPath = new System.Windows.Forms.Label();
            this.txtAlertPingSoundPath = new System.Windows.Forms.TextBox();
            this.btnBrowsePingSound = new System.Windows.Forms.Button();
            this.lblAlertTickrateSoundPath = new System.Windows.Forms.Label();
            this.txtAlertTickrateSoundPath = new System.Windows.Forms.TextBox();
            this.btnBrowseTickrateSound = new System.Windows.Forms.Button();
            this.lblAlertTicktimeSoundPath = new System.Windows.Forms.Label();
            this.txtAlertTicktimeSoundPath = new System.Windows.Forms.TextBox();
            this.btnBrowseTicktimeSound = new System.Windows.Forms.Button();
            this.groupBoxNetworkOptimizer = new System.Windows.Forms.GroupBox();
            this.chkNetworkOptimizationEnabled = new System.Windows.Forms.CheckBox();
            this.lblOptimizationThreshold = new System.Windows.Forms.Label();
            this.numOptimizationThreshold = new System.Windows.Forms.NumericUpDown();
            this.lblOptimizationInterval = new System.Windows.Forms.Label();
            this.numOptimizationInterval = new System.Windows.Forms.NumericUpDown();
            this.chkAggressiveOptimization = new System.Windows.Forms.CheckBox();
            this.btnManualOptimization = new System.Windows.Forms.Button();
            this.lblLastOptimization = new System.Windows.Forms.Label();
            this.lblOptimizationStats = new System.Windows.Forms.Label();
            this.btnClearOptimizationHistory = new System.Windows.Forms.Button();
            this.groupBoxColorZones = new System.Windows.Forms.GroupBox();
            this.lblColorZoneProfile = new System.Windows.Forms.Label();
            this.cmbColorZoneProfile = new System.Windows.Forms.ComboBox();
            this.lblPingGreen = new System.Windows.Forms.Label();
            this.numPingGreen = new System.Windows.Forms.NumericUpDown();
            this.lblPingYellow = new System.Windows.Forms.Label();
            this.numPingYellow = new System.Windows.Forms.NumericUpDown();
            this.lblTickrateGreen = new System.Windows.Forms.Label();
            this.numTickrateGreen = new System.Windows.Forms.NumericUpDown();
            this.lblTickrateYellow = new System.Windows.Forms.Label();
            this.numTickrateYellow = new System.Windows.Forms.NumericUpDown();
            this.lblTicktimeGreen = new System.Windows.Forms.Label();
            this.numTicktimeGreen = new System.Windows.Forms.NumericUpDown();
            this.lblTicktimeYellow = new System.Windows.Forms.Label();
            this.numTicktimeYellow = new System.Windows.Forms.NumericUpDown();
            this.btnResetColorZones = new System.Windows.Forms.Button();
            this.groupBoxNetworkQuality = new System.Windows.Forms.GroupBox();
            this.chkNetworkQualityEnabled = new System.Windows.Forms.CheckBox();
            this.chkNetworkQualityOverlay = new System.Windows.Forms.CheckBox();
            this.lblQualityHistorySize = new System.Windows.Forms.Label();
            this.numQualityHistorySize = new System.Windows.Forms.NumericUpDown();
            this.lblStabilityThreshold = new System.Windows.Forms.Label();
            this.numStabilityThreshold = new System.Windows.Forms.NumericUpDown();
            this.lblQualityThreshold = new System.Windows.Forms.Label();
            this.numQualityThreshold = new System.Windows.Forms.NumericUpDown();
            this.btnResetQualityAnalyzer = new System.Windows.Forms.Button();
            this.lblCurrentQuality = new System.Windows.Forms.Label();
            this.lblQualityRating = new System.Windows.Forms.Label();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.chkUiRefreshHidden = new System.Windows.Forms.CheckBox();
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
            this.chkTickrateValueGuiSmoothing = new System.Windows.Forms.CheckBox();
            this.chkTickrateValueOverlaySmoothing = new System.Windows.Forms.CheckBox();
            this.chkTicktimeValueOverlaySmoothing = new System.Windows.Forms.CheckBox();
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
            this.groupBoxTickrateChart.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTickrateChartMaxPoints)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTickrateChartHistoryHours)).BeginInit();
            this.groupBoxExtendedOverlay.SuspendLayout();
            this.groupBoxDebugSettings.SuspendLayout();
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
            this.groupBoxSpikeAdvanced.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numEmaAlpha)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEwSigmaAlpha)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSensitivityMultiplier)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHysteresisRatio)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRefractoryPeriod)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMinEnergyThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInitWindowSize)).BeginInit();
            this.groupBoxAlerts.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAlertCooldown)).BeginInit();
            this.groupBoxAlertSounds.SuspendLayout();
            this.groupBoxNetworkOptimizer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numOptimizationThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOptimizationInterval)).BeginInit();
            this.groupBoxColorZones.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPingGreen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPingYellow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTickrateGreen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTickrateYellow)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTicktimeGreen)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTicktimeYellow)).BeginInit();
            this.groupBoxNetworkQuality.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numQualityHistorySize)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numStabilityThreshold)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numQualityThreshold)).BeginInit();
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
            this.panel1.Controls.Add(this.groupBoxTickrateChart);
            this.panel1.Controls.Add(this.groupBoxExtendedOverlay);
            this.panel1.Controls.Add(this.groupBoxDebugSettings);
            this.panel1.Controls.Add(this.groupBoxVpnBypass);
            this.panel1.Controls.Add(this.groupBoxPhase3);
            this.panel1.Controls.Add(this.groupBoxPhase2);
            this.panel1.Controls.Add(this.groupBoxPhase1);
            this.panel1.Controls.Add(this.groupBoxSpikeDetection);
            this.panel1.Controls.Add(this.groupBoxSpikeAdvanced);
            this.panel1.Controls.Add(this.groupBoxAlerts);
            this.panel1.Controls.Add(this.groupBoxAlertSounds);
            this.panel1.Controls.Add(this.groupBoxNetworkOptimizer);
            this.panel1.Controls.Add(this.groupBoxColorZones);
            this.panel1.Controls.Add(this.groupBoxNetworkQuality);
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
            // groupBoxTickrateChart
            // 
            this.groupBoxTickrateChart.Controls.Add(this.chkTickrateChartEnabled);
            this.groupBoxTickrateChart.Controls.Add(this.lblTickrateChartMode);
            this.groupBoxTickrateChart.Controls.Add(this.cmbTickrateChartMode);
            this.groupBoxTickrateChart.Controls.Add(this.chkTickrateChartPerServer);
            this.groupBoxTickrateChart.Controls.Add(this.chkTickrateChartCompression);
            this.groupBoxTickrateChart.Controls.Add(this.chkTickrateChartTimeScale);
            this.groupBoxTickrateChart.Controls.Add(this.chkTickrateChartTrimming);
            this.groupBoxTickrateChart.Controls.Add(this.lblTickrateChartMaxPoints);
            this.groupBoxTickrateChart.Controls.Add(this.numTickrateChartMaxPoints);
            this.groupBoxTickrateChart.Controls.Add(this.lblTickrateChartHistoryHours);
            this.groupBoxTickrateChart.Controls.Add(this.numTickrateChartHistoryHours);
            this.groupBoxTickrateChart.Controls.Add(this.btnTickrateChartReset);
            this.groupBoxTickrateChart.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxTickrateChart.Location = new System.Drawing.Point(13, 3152);
            this.groupBoxTickrateChart.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxTickrateChart.Name = "groupBoxTickrateChart";
            this.groupBoxTickrateChart.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxTickrateChart.Size = new System.Drawing.Size(753, 220);
            this.groupBoxTickrateChart.TabIndex = 8;
            this.groupBoxTickrateChart.TabStop = false;
            this.groupBoxTickrateChart.Text = "Управление графиком тикрейта";
            // 
            // chkTickrateChartEnabled
            // 
            this.chkTickrateChartEnabled.AutoSize = true;
            this.chkTickrateChartEnabled.Location = new System.Drawing.Point(8, 25);
            this.chkTickrateChartEnabled.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateChartEnabled.Name = "chkTickrateChartEnabled";
            this.chkTickrateChartEnabled.Size = new System.Drawing.Size(144, 20);
            this.chkTickrateChartEnabled.TabIndex = 0;
            this.chkTickrateChartEnabled.Text = "Включить график";
            this.chkTickrateChartEnabled.UseVisualStyleBackColor = true;
            // 
            // lblTickrateChartMode
            // 
            this.lblTickrateChartMode.AutoSize = true;
            this.lblTickrateChartMode.Location = new System.Drawing.Point(8, 55);
            this.lblTickrateChartMode.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTickrateChartMode.Name = "lblTickrateChartMode";
            this.lblTickrateChartMode.Size = new System.Drawing.Size(112, 16);
            this.lblTickrateChartMode.TabIndex = 1;
            this.lblTickrateChartMode.Text = "Режим графика:";
            // 
            // cmbTickrateChartMode
            // 
            this.cmbTickrateChartMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTickrateChartMode.FormattingEnabled = true;
            this.cmbTickrateChartMode.Items.AddRange(new object[] {
            "Простой график (точки)",
            "График с временной шкалой",
            "Сжатый график",
            "Отключен"});
            this.cmbTickrateChartMode.Location = new System.Drawing.Point(120, 52);
            this.cmbTickrateChartMode.Margin = new System.Windows.Forms.Padding(4);
            this.cmbTickrateChartMode.Name = "cmbTickrateChartMode";
            this.cmbTickrateChartMode.Size = new System.Drawing.Size(200, 24);
            this.cmbTickrateChartMode.TabIndex = 2;
            // 
            // chkTickrateChartPerServer
            // 
            this.chkTickrateChartPerServer.AutoSize = true;
            this.chkTickrateChartPerServer.Location = new System.Drawing.Point(8, 85);
            this.chkTickrateChartPerServer.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateChartPerServer.Name = "chkTickrateChartPerServer";
            this.chkTickrateChartPerServer.Size = new System.Drawing.Size(211, 20);
            this.chkTickrateChartPerServer.TabIndex = 3;
            this.chkTickrateChartPerServer.Text = "Индивидуально по серверу";
            this.chkTickrateChartPerServer.UseVisualStyleBackColor = true;
            // 
            // chkTickrateChartCompression
            // 
            this.chkTickrateChartCompression.AutoSize = true;
            this.chkTickrateChartCompression.Location = new System.Drawing.Point(250, 85);
            this.chkTickrateChartCompression.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateChartCompression.Name = "chkTickrateChartCompression";
            this.chkTickrateChartCompression.Size = new System.Drawing.Size(138, 20);
            this.chkTickrateChartCompression.TabIndex = 4;
            this.chkTickrateChartCompression.Text = "Сжимать данные";
            this.chkTickrateChartCompression.UseVisualStyleBackColor = true;
            // 
            // chkTickrateChartTimeScale
            // 
            this.chkTickrateChartTimeScale.AutoSize = true;
            this.chkTickrateChartTimeScale.Location = new System.Drawing.Point(420, 85);
            this.chkTickrateChartTimeScale.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateChartTimeScale.Name = "chkTickrateChartTimeScale";
            this.chkTickrateChartTimeScale.Size = new System.Drawing.Size(145, 20);
            this.chkTickrateChartTimeScale.TabIndex = 5;
            this.chkTickrateChartTimeScale.Text = "Временная шкала";
            this.chkTickrateChartTimeScale.UseVisualStyleBackColor = true;
            // 
            // chkTickrateChartTrimming
            // 
            this.chkTickrateChartTrimming.AutoSize = true;
            this.chkTickrateChartTrimming.Location = new System.Drawing.Point(8, 115);
            this.chkTickrateChartTrimming.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateChartTrimming.Name = "chkTickrateChartTrimming";
            this.chkTickrateChartTrimming.Size = new System.Drawing.Size(144, 20);
            this.chkTickrateChartTrimming.TabIndex = 6;
            this.chkTickrateChartTrimming.Text = "Обрезать график";
            this.chkTickrateChartTrimming.UseVisualStyleBackColor = true;
            // 
            // lblTickrateChartMaxPoints
            // 
            this.lblTickrateChartMaxPoints.AutoSize = true;
            this.lblTickrateChartMaxPoints.Location = new System.Drawing.Point(8, 145);
            this.lblTickrateChartMaxPoints.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTickrateChartMaxPoints.Name = "lblTickrateChartMaxPoints";
            this.lblTickrateChartMaxPoints.Size = new System.Drawing.Size(177, 16);
            this.lblTickrateChartMaxPoints.TabIndex = 7;
            this.lblTickrateChartMaxPoints.Text = "Максимум точек графика:";
            // 
            // numTickrateChartMaxPoints
            // 
            this.numTickrateChartMaxPoints.Location = new System.Drawing.Point(181, 142);
            this.numTickrateChartMaxPoints.Margin = new System.Windows.Forms.Padding(4);
            this.numTickrateChartMaxPoints.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numTickrateChartMaxPoints.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numTickrateChartMaxPoints.Name = "numTickrateChartMaxPoints";
            this.numTickrateChartMaxPoints.Size = new System.Drawing.Size(100, 22);
            this.numTickrateChartMaxPoints.TabIndex = 8;
            this.numTickrateChartMaxPoints.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // lblTickrateChartHistoryHours
            // 
            this.lblTickrateChartHistoryHours.AutoSize = true;
            this.lblTickrateChartHistoryHours.Location = new System.Drawing.Point(320, 145);
            this.lblTickrateChartHistoryHours.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTickrateChartHistoryHours.Name = "lblTickrateChartHistoryHours";
            this.lblTickrateChartHistoryHours.Size = new System.Drawing.Size(108, 16);
            this.lblTickrateChartHistoryHours.TabIndex = 9;
            this.lblTickrateChartHistoryHours.Text = "История (часы):";
            // 
            // numTickrateChartHistoryHours
            // 
            this.numTickrateChartHistoryHours.Location = new System.Drawing.Point(462, 142);
            this.numTickrateChartHistoryHours.Margin = new System.Windows.Forms.Padding(4);
            this.numTickrateChartHistoryHours.Maximum = new decimal(new int[] {
            168,
            0,
            0,
            0});
            this.numTickrateChartHistoryHours.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numTickrateChartHistoryHours.Name = "numTickrateChartHistoryHours";
            this.numTickrateChartHistoryHours.Size = new System.Drawing.Size(70, 22);
            this.numTickrateChartHistoryHours.TabIndex = 10;
            this.numTickrateChartHistoryHours.Value = new decimal(new int[] {
            24,
            0,
            0,
            0});
            // 
            // btnTickrateChartReset
            // 
            this.btnTickrateChartReset.Location = new System.Drawing.Point(8, 180);
            this.btnTickrateChartReset.Margin = new System.Windows.Forms.Padding(4);
            this.btnTickrateChartReset.Name = "btnTickrateChartReset";
            this.btnTickrateChartReset.Size = new System.Drawing.Size(150, 28);
            this.btnTickrateChartReset.TabIndex = 11;
            this.btnTickrateChartReset.Text = "Сброс к умолчанию";
            this.btnTickrateChartReset.UseVisualStyleBackColor = true;
            // 
            // groupBoxExtendedOverlay
            // 
            this.groupBoxExtendedOverlay.Controls.Add(this.chkShowActiveProcess);
            this.groupBoxExtendedOverlay.Controls.Add(this.chkShowSessionTime);
            this.groupBoxExtendedOverlay.Controls.Add(this.chkShowExternalIP);
            this.groupBoxExtendedOverlay.Controls.Add(this.chkShowSessionStats);
            this.groupBoxExtendedOverlay.Controls.Add(this.chkShowServerInfo);
            this.groupBoxExtendedOverlay.Controls.Add(this.chkShowPacketCounters);
            this.groupBoxExtendedOverlay.Controls.Add(this.chkShowConnectionType);
            this.groupBoxExtendedOverlay.Controls.Add(this.chkShowDiagnosticInfo);
            this.groupBoxExtendedOverlay.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxExtendedOverlay.Location = new System.Drawing.Point(13, 2932);
            this.groupBoxExtendedOverlay.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxExtendedOverlay.Name = "groupBoxExtendedOverlay";
            this.groupBoxExtendedOverlay.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxExtendedOverlay.Size = new System.Drawing.Size(753, 220);
            this.groupBoxExtendedOverlay.TabIndex = 10;
            this.groupBoxExtendedOverlay.TabStop = false;
            this.groupBoxExtendedOverlay.Text = "Расширенная информация в оверлее";
            // 
            // chkShowActiveProcess
            // 
            this.chkShowActiveProcess.AutoSize = true;
            this.chkShowActiveProcess.Location = new System.Drawing.Point(21, 26);
            this.chkShowActiveProcess.Name = "chkShowActiveProcess";
            this.chkShowActiveProcess.Size = new System.Drawing.Size(274, 20);
            this.chkShowActiveProcess.TabIndex = 0;
            this.chkShowActiveProcess.Text = "Показывать отслеживаемый процесс";
            this.chkShowActiveProcess.UseVisualStyleBackColor = true;
            // 
            // chkShowSessionTime
            // 
            this.chkShowSessionTime.AutoSize = true;
            this.chkShowSessionTime.Location = new System.Drawing.Point(21, 54);
            this.chkShowSessionTime.Name = "chkShowSessionTime";
            this.chkShowSessionTime.Size = new System.Drawing.Size(258, 20);
            this.chkShowSessionTime.TabIndex = 1;
            this.chkShowSessionTime.Text = "Показывать время текущей сессии";
            this.chkShowSessionTime.UseVisualStyleBackColor = true;
            // 
            // chkShowExternalIP
            // 
            this.chkShowExternalIP.AutoSize = true;
            this.chkShowExternalIP.Location = new System.Drawing.Point(21, 82);
            this.chkShowExternalIP.Name = "chkShowExternalIP";
            this.chkShowExternalIP.Size = new System.Drawing.Size(226, 20);
            this.chkShowExternalIP.TabIndex = 2;
            this.chkShowExternalIP.Text = "Показывать внешний IP адрес";
            this.chkShowExternalIP.UseVisualStyleBackColor = true;
            // 
            // chkShowSessionStats
            // 
            this.chkShowSessionStats.AutoSize = true;
            this.chkShowSessionStats.Location = new System.Drawing.Point(21, 110);
            this.chkShowSessionStats.Name = "chkShowSessionStats";
            this.chkShowSessionStats.Size = new System.Drawing.Size(340, 20);
            this.chkShowSessionStats.TabIndex = 3;
            this.chkShowSessionStats.Text = "Показывать статистику сессии (мин/макс/сред)";
            this.chkShowSessionStats.UseVisualStyleBackColor = true;
            // 
            // chkShowServerInfo
            // 
            this.chkShowServerInfo.AutoSize = true;
            this.chkShowServerInfo.Location = new System.Drawing.Point(381, 26);
            this.chkShowServerInfo.Name = "chkShowServerInfo";
            this.chkShowServerInfo.Size = new System.Drawing.Size(267, 20);
            this.chkShowServerInfo.TabIndex = 4;
            this.chkShowServerInfo.Text = "Показывать информацию о сервере";
            this.chkShowServerInfo.UseVisualStyleBackColor = true;
            // 
            // chkShowPacketCounters
            // 
            this.chkShowPacketCounters.AutoSize = true;
            this.chkShowPacketCounters.Location = new System.Drawing.Point(381, 54);
            this.chkShowPacketCounters.Name = "chkShowPacketCounters";
            this.chkShowPacketCounters.Size = new System.Drawing.Size(280, 20);
            this.chkShowPacketCounters.TabIndex = 5;
            this.chkShowPacketCounters.Text = "Показывать счетчики пакетов (TX/RX)";
            this.chkShowPacketCounters.UseVisualStyleBackColor = true;
            // 
            // chkShowConnectionType
            // 
            this.chkShowConnectionType.AutoSize = true;
            this.chkShowConnectionType.Location = new System.Drawing.Point(381, 82);
            this.chkShowConnectionType.Name = "chkShowConnectionType";
            this.chkShowConnectionType.Size = new System.Drawing.Size(317, 20);
            this.chkShowConnectionType.TabIndex = 6;
            this.chkShowConnectionType.Text = "Показывать тип подключения (WiFi/Ethernet)";
            this.chkShowConnectionType.UseVisualStyleBackColor = true;
            // 
            // chkShowDiagnosticInfo
            // 
            this.chkShowDiagnosticInfo.AutoSize = true;
            this.chkShowDiagnosticInfo.Location = new System.Drawing.Point(381, 110);
            this.chkShowDiagnosticInfo.Name = "chkShowDiagnosticInfo";
            this.chkShowDiagnosticInfo.Size = new System.Drawing.Size(317, 20);
            this.chkShowDiagnosticInfo.TabIndex = 7;
            this.chkShowDiagnosticInfo.Text = "Показывать диагностическую информацию";
            this.chkShowDiagnosticInfo.UseVisualStyleBackColor = true;
            // 
            // groupBoxDebugSettings
            // 
            this.groupBoxDebugSettings.Controls.Add(this.chkEnableTextLogs);
            this.groupBoxDebugSettings.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxDebugSettings.Location = new System.Drawing.Point(13, 2882);
            this.groupBoxDebugSettings.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxDebugSettings.Name = "groupBoxDebugSettings";
            this.groupBoxDebugSettings.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxDebugSettings.Size = new System.Drawing.Size(753, 50);
            this.groupBoxDebugSettings.TabIndex = 4;
            this.groupBoxDebugSettings.TabStop = false;
            this.groupBoxDebugSettings.Text = "Настройки отладки";
            // 
            // chkEnableTextLogs
            // 
            this.chkEnableTextLogs.AutoSize = true;
            this.chkEnableTextLogs.Location = new System.Drawing.Point(21, 23);
            this.chkEnableTextLogs.Margin = new System.Windows.Forms.Padding(4);
            this.chkEnableTextLogs.Name = "chkEnableTextLogs";
            this.chkEnableTextLogs.Size = new System.Drawing.Size(303, 20);
            this.chkEnableTextLogs.TabIndex = 0;
            this.chkEnableTextLogs.Text = "Включить текстовые логи (debug.log и др.)";
            this.chkEnableTextLogs.UseVisualStyleBackColor = true;
            // 
            // groupBoxVpnBypass
            // 
            this.groupBoxVpnBypass.Controls.Add(this.chkVpnBypassAdvanced);
            this.groupBoxVpnBypass.Controls.Add(this.chkVpnBypassBasic);
            this.groupBoxVpnBypass.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxVpnBypass.Location = new System.Drawing.Point(13, 2784);
            this.groupBoxVpnBypass.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxVpnBypass.Name = "groupBoxVpnBypass";
            this.groupBoxVpnBypass.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxVpnBypass.Size = new System.Drawing.Size(753, 98);
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
            this.chkVpnBypassAdvanced.Size = new System.Drawing.Size(399, 20);
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
            this.chkVpnBypassBasic.Size = new System.Drawing.Size(318, 20);
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
            this.groupBoxPhase3.Location = new System.Drawing.Point(13, 2584);
            this.groupBoxPhase3.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxPhase3.Name = "groupBoxPhase3";
            this.groupBoxPhase3.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxPhase3.Size = new System.Drawing.Size(753, 200);
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
            this.chkHighPriorityThreads.Size = new System.Drawing.Size(280, 20);
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
            this.chkSingleConsumerPattern.Size = new System.Drawing.Size(301, 20);
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
            this.groupBoxPhase2.Location = new System.Drawing.Point(13, 2434);
            this.groupBoxPhase2.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxPhase2.Name = "groupBoxPhase2";
            this.groupBoxPhase2.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxPhase2.Size = new System.Drawing.Size(753, 150);
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
            this.chkVirtualModeListView.Size = new System.Drawing.Size(292, 20);
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
            this.chkShowVirtualModeStats.Size = new System.Drawing.Size(270, 20);
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
            this.groupBoxPhase1.Location = new System.Drawing.Point(13, 2284);
            this.groupBoxPhase1.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxPhase1.Name = "groupBoxPhase1";
            this.groupBoxPhase1.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxPhase1.Size = new System.Drawing.Size(753, 150);
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
            this.chkAntiReentrancy.Size = new System.Drawing.Size(231, 20);
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
            this.chkRtssThrottling.Size = new System.Drawing.Size(221, 20);
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
            this.chkPcapOptimization.Size = new System.Drawing.Size(219, 20);
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
            this.groupBoxSpikeDetection.Location = new System.Drawing.Point(13, 2084);
            this.groupBoxSpikeDetection.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxSpikeDetection.Name = "groupBoxSpikeDetection";
            this.groupBoxSpikeDetection.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxSpikeDetection.Size = new System.Drawing.Size(753, 200);
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
            this.chkSpikeDetectionEnable.Size = new System.Drawing.Size(217, 20);
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
            this.chkSpikeMetricPing.Size = new System.Drawing.Size(61, 20);
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
            this.chkSpikeMetricTickrate.Size = new System.Drawing.Size(170, 20);
            this.chkSpikeMetricTickrate.TabIndex = 2;
            this.chkSpikeMetricTickrate.Text = "Тикрейт (в разработке)";
            this.chkSpikeMetricTickrate.UseVisualStyleBackColor = true;
            // 
            // chkSpikeMetricTicktime
            // 
            this.chkSpikeMetricTicktime.AutoSize = true;
            this.chkSpikeMetricTicktime.Location = new System.Drawing.Point(500, 30);
            this.chkSpikeMetricTicktime.Margin = new System.Windows.Forms.Padding(4);
            this.chkSpikeMetricTicktime.Name = "chkSpikeMetricTicktime";
            this.chkSpikeMetricTicktime.Size = new System.Drawing.Size(171, 20);
            this.chkSpikeMetricTicktime.TabIndex = 3;
            this.chkSpikeMetricTicktime.Text = "Тиктайм (в разработке)";
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
            this.chkSpikeAutoCalibration.Size = new System.Drawing.Size(300, 20);
            this.chkSpikeAutoCalibration.TabIndex = 12;
            this.chkSpikeAutoCalibration.Text = "Автоматическая калибровка (в разработке)";
            this.chkSpikeAutoCalibration.UseVisualStyleBackColor = true;
            // 
            // groupBoxSpikeAdvanced
            // 
            this.groupBoxSpikeAdvanced.Controls.Add(this.lblEmaAlpha);
            this.groupBoxSpikeAdvanced.Controls.Add(this.numEmaAlpha);
            this.groupBoxSpikeAdvanced.Controls.Add(this.lblEwSigmaAlpha);
            this.groupBoxSpikeAdvanced.Controls.Add(this.numEwSigmaAlpha);
            this.groupBoxSpikeAdvanced.Controls.Add(this.lblSensitivityMultiplier);
            this.groupBoxSpikeAdvanced.Controls.Add(this.numSensitivityMultiplier);
            this.groupBoxSpikeAdvanced.Controls.Add(this.lblHysteresisRatio);
            this.groupBoxSpikeAdvanced.Controls.Add(this.numHysteresisRatio);
            this.groupBoxSpikeAdvanced.Controls.Add(this.lblRefractoryPeriod);
            this.groupBoxSpikeAdvanced.Controls.Add(this.numRefractoryPeriod);
            this.groupBoxSpikeAdvanced.Controls.Add(this.lblMinEnergyThreshold);
            this.groupBoxSpikeAdvanced.Controls.Add(this.numMinEnergyThreshold);
            this.groupBoxSpikeAdvanced.Controls.Add(this.lblInitWindowSize);
            this.groupBoxSpikeAdvanced.Controls.Add(this.numInitWindowSize);
            this.groupBoxSpikeAdvanced.Controls.Add(this.btnResetSpikeDefaults);
            this.groupBoxSpikeAdvanced.Controls.Add(this.btnSpikePresetsSensitive);
            this.groupBoxSpikeAdvanced.Controls.Add(this.btnSpikePresetsBalanced);
            this.groupBoxSpikeAdvanced.Controls.Add(this.btnSpikePresetsConservative);
            this.groupBoxSpikeAdvanced.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxSpikeAdvanced.Location = new System.Drawing.Point(13, 1804);
            this.groupBoxSpikeAdvanced.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxSpikeAdvanced.Name = "groupBoxSpikeAdvanced";
            this.groupBoxSpikeAdvanced.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxSpikeAdvanced.Size = new System.Drawing.Size(753, 280);
            this.groupBoxSpikeAdvanced.TabIndex = 6;
            this.groupBoxSpikeAdvanced.TabStop = false;
            this.groupBoxSpikeAdvanced.Text = "Расширенные настройки детекции спайков (Stage 4)";
            // 
            // lblEmaAlpha
            // 
            this.lblEmaAlpha.AutoSize = true;
            this.lblEmaAlpha.Location = new System.Drawing.Point(20, 30);
            this.lblEmaAlpha.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblEmaAlpha.Name = "lblEmaAlpha";
            this.lblEmaAlpha.Size = new System.Drawing.Size(77, 16);
            this.lblEmaAlpha.TabIndex = 0;
            this.lblEmaAlpha.Text = "EMA Alpha:";
            // 
            // numEmaAlpha
            // 
            this.numEmaAlpha.DecimalPlaces = 3;
            this.numEmaAlpha.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numEmaAlpha.Location = new System.Drawing.Point(157, 30);
            this.numEmaAlpha.Margin = new System.Windows.Forms.Padding(4);
            this.numEmaAlpha.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numEmaAlpha.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numEmaAlpha.Name = "numEmaAlpha";
            this.numEmaAlpha.Size = new System.Drawing.Size(80, 22);
            this.numEmaAlpha.TabIndex = 1;
            this.numEmaAlpha.Value = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            // 
            // lblEwSigmaAlpha
            // 
            this.lblEwSigmaAlpha.AutoSize = true;
            this.lblEwSigmaAlpha.Location = new System.Drawing.Point(257, 32);
            this.lblEwSigmaAlpha.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblEwSigmaAlpha.Name = "lblEwSigmaAlpha";
            this.lblEwSigmaAlpha.Size = new System.Drawing.Size(113, 16);
            this.lblEwSigmaAlpha.TabIndex = 2;
            this.lblEwSigmaAlpha.Text = "EW-Sigma Alpha:";
            // 
            // numEwSigmaAlpha
            // 
            this.numEwSigmaAlpha.DecimalPlaces = 3;
            this.numEwSigmaAlpha.Increment = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numEwSigmaAlpha.Location = new System.Drawing.Point(410, 30);
            this.numEwSigmaAlpha.Margin = new System.Windows.Forms.Padding(4);
            this.numEwSigmaAlpha.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numEwSigmaAlpha.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            196608});
            this.numEwSigmaAlpha.Name = "numEwSigmaAlpha";
            this.numEwSigmaAlpha.Size = new System.Drawing.Size(80, 22);
            this.numEwSigmaAlpha.TabIndex = 3;
            this.numEwSigmaAlpha.Value = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            // 
            // lblSensitivityMultiplier
            // 
            this.lblSensitivityMultiplier.AutoSize = true;
            this.lblSensitivityMultiplier.Location = new System.Drawing.Point(510, 32);
            this.lblSensitivityMultiplier.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSensitivityMultiplier.Name = "lblSensitivityMultiplier";
            this.lblSensitivityMultiplier.Size = new System.Drawing.Size(133, 16);
            this.lblSensitivityMultiplier.TabIndex = 4;
            this.lblSensitivityMultiplier.Text = "Множитель порога:";
            // 
            // numSensitivityMultiplier
            // 
            this.numSensitivityMultiplier.DecimalPlaces = 1;
            this.numSensitivityMultiplier.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numSensitivityMultiplier.Location = new System.Drawing.Point(660, 30);
            this.numSensitivityMultiplier.Margin = new System.Windows.Forms.Padding(4);
            this.numSensitivityMultiplier.Maximum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numSensitivityMultiplier.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numSensitivityMultiplier.Name = "numSensitivityMultiplier";
            this.numSensitivityMultiplier.Size = new System.Drawing.Size(80, 22);
            this.numSensitivityMultiplier.TabIndex = 5;
            this.numSensitivityMultiplier.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // lblHysteresisRatio
            // 
            this.lblHysteresisRatio.AutoSize = true;
            this.lblHysteresisRatio.Location = new System.Drawing.Point(20, 65);
            this.lblHysteresisRatio.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblHysteresisRatio.Name = "lblHysteresisRatio";
            this.lblHysteresisRatio.Size = new System.Drawing.Size(86, 16);
            this.lblHysteresisRatio.TabIndex = 6;
            this.lblHysteresisRatio.Text = "Гистерезис:";
            // 
            // numHysteresisRatio
            // 
            this.numHysteresisRatio.DecimalPlaces = 2;
            this.numHysteresisRatio.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numHysteresisRatio.Location = new System.Drawing.Point(157, 65);
            this.numHysteresisRatio.Margin = new System.Windows.Forms.Padding(4);
            this.numHysteresisRatio.Maximum = new decimal(new int[] {
            95,
            0,
            0,
            131072});
            this.numHysteresisRatio.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.numHysteresisRatio.Name = "numHysteresisRatio";
            this.numHysteresisRatio.Size = new System.Drawing.Size(80, 22);
            this.numHysteresisRatio.TabIndex = 7;
            this.numHysteresisRatio.Value = new decimal(new int[] {
            8,
            0,
            0,
            65536});
            // 
            // lblRefractoryPeriod
            // 
            this.lblRefractoryPeriod.AutoSize = true;
            this.lblRefractoryPeriod.Location = new System.Drawing.Point(257, 67);
            this.lblRefractoryPeriod.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblRefractoryPeriod.Name = "lblRefractoryPeriod";
            this.lblRefractoryPeriod.Size = new System.Drawing.Size(139, 16);
            this.lblRefractoryPeriod.TabIndex = 8;
            this.lblRefractoryPeriod.Text = "Период тишины (мс):";
            // 
            // numRefractoryPeriod
            // 
            this.numRefractoryPeriod.Increment = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numRefractoryPeriod.Location = new System.Drawing.Point(410, 65);
            this.numRefractoryPeriod.Margin = new System.Windows.Forms.Padding(4);
            this.numRefractoryPeriod.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.numRefractoryPeriod.Minimum = new decimal(new int[] {
            100,
            0,
            0,
            0});
            this.numRefractoryPeriod.Name = "numRefractoryPeriod";
            this.numRefractoryPeriod.Size = new System.Drawing.Size(80, 22);
            this.numRefractoryPeriod.TabIndex = 9;
            this.numRefractoryPeriod.Value = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            // 
            // lblMinEnergyThreshold
            // 
            this.lblMinEnergyThreshold.AutoSize = true;
            this.lblMinEnergyThreshold.Location = new System.Drawing.Point(258, 100);
            this.lblMinEnergyThreshold.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblMinEnergyThreshold.Name = "lblMinEnergyThreshold";
            this.lblMinEnergyThreshold.Size = new System.Drawing.Size(204, 16);
            this.lblMinEnergyThreshold.TabIndex = 10;
            this.lblMinEnergyThreshold.Text = "Мин. энергия спайка (мс×амп):";
            // 
            // numMinEnergyThreshold
            // 
            this.numMinEnergyThreshold.DecimalPlaces = 1;
            this.numMinEnergyThreshold.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numMinEnergyThreshold.Location = new System.Drawing.Point(480, 98);
            this.numMinEnergyThreshold.Margin = new System.Windows.Forms.Padding(4);
            this.numMinEnergyThreshold.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numMinEnergyThreshold.Name = "numMinEnergyThreshold";
            this.numMinEnergyThreshold.Size = new System.Drawing.Size(80, 22);
            this.numMinEnergyThreshold.TabIndex = 11;
            this.numMinEnergyThreshold.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // lblInitWindowSize
            // 
            this.lblInitWindowSize.AutoSize = true;
            this.lblInitWindowSize.Location = new System.Drawing.Point(20, 100);
            this.lblInitWindowSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblInitWindowSize.Name = "lblInitWindowSize";
            this.lblInitWindowSize.Size = new System.Drawing.Size(119, 16);
            this.lblInitWindowSize.TabIndex = 12;
            this.lblInitWindowSize.Text = "Размер выборки:";
            // 
            // numInitWindowSize
            // 
            this.numInitWindowSize.Location = new System.Drawing.Point(157, 100);
            this.numInitWindowSize.Margin = new System.Windows.Forms.Padding(4);
            this.numInitWindowSize.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numInitWindowSize.Name = "numInitWindowSize";
            this.numInitWindowSize.Size = new System.Drawing.Size(80, 22);
            this.numInitWindowSize.TabIndex = 13;
            this.numInitWindowSize.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // btnResetSpikeDefaults
            // 
            this.btnResetSpikeDefaults.Location = new System.Drawing.Point(20, 135);
            this.btnResetSpikeDefaults.Margin = new System.Windows.Forms.Padding(4);
            this.btnResetSpikeDefaults.Name = "btnResetSpikeDefaults";
            this.btnResetSpikeDefaults.Size = new System.Drawing.Size(120, 30);
            this.btnResetSpikeDefaults.TabIndex = 14;
            this.btnResetSpikeDefaults.Text = "По умолчанию";
            this.btnResetSpikeDefaults.UseVisualStyleBackColor = true;
            // 
            // btnSpikePresetsSensitive
            // 
            this.btnSpikePresetsSensitive.Location = new System.Drawing.Point(160, 135);
            this.btnSpikePresetsSensitive.Margin = new System.Windows.Forms.Padding(4);
            this.btnSpikePresetsSensitive.Name = "btnSpikePresetsSensitive";
            this.btnSpikePresetsSensitive.Size = new System.Drawing.Size(120, 30);
            this.btnSpikePresetsSensitive.TabIndex = 15;
            this.btnSpikePresetsSensitive.Text = "Чувствительный";
            this.btnSpikePresetsSensitive.UseVisualStyleBackColor = true;
            // 
            // btnSpikePresetsBalanced
            // 
            this.btnSpikePresetsBalanced.Location = new System.Drawing.Point(300, 135);
            this.btnSpikePresetsBalanced.Margin = new System.Windows.Forms.Padding(4);
            this.btnSpikePresetsBalanced.Name = "btnSpikePresetsBalanced";
            this.btnSpikePresetsBalanced.Size = new System.Drawing.Size(120, 30);
            this.btnSpikePresetsBalanced.TabIndex = 16;
            this.btnSpikePresetsBalanced.Text = "Сбалансированный";
            this.btnSpikePresetsBalanced.UseVisualStyleBackColor = true;
            // 
            // btnSpikePresetsConservative
            // 
            this.btnSpikePresetsConservative.Location = new System.Drawing.Point(440, 135);
            this.btnSpikePresetsConservative.Margin = new System.Windows.Forms.Padding(4);
            this.btnSpikePresetsConservative.Name = "btnSpikePresetsConservative";
            this.btnSpikePresetsConservative.Size = new System.Drawing.Size(120, 30);
            this.btnSpikePresetsConservative.TabIndex = 17;
            this.btnSpikePresetsConservative.Text = "Консервативный";
            this.btnSpikePresetsConservative.UseVisualStyleBackColor = true;
            // 
            // groupBoxAlerts
            // 
            this.groupBoxAlerts.Controls.Add(this.chkAlertSoundEnabled);
            this.groupBoxAlerts.Controls.Add(this.chkAlertDiscordEnabled);
            this.groupBoxAlerts.Controls.Add(this.lblAlertDiscordWebhook);
            this.groupBoxAlerts.Controls.Add(this.txtAlertDiscordWebhook);
            this.groupBoxAlerts.Controls.Add(this.lblAlertCooldown);
            this.groupBoxAlerts.Controls.Add(this.numAlertCooldown);
            this.groupBoxAlerts.Controls.Add(this.btnTestDiscordAlert);
            this.groupBoxAlerts.Controls.Add(this.btnTestSoundAlert);
            this.groupBoxAlerts.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxAlerts.Location = new System.Drawing.Point(13, 1624);
            this.groupBoxAlerts.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxAlerts.Name = "groupBoxAlerts";
            this.groupBoxAlerts.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxAlerts.Size = new System.Drawing.Size(753, 180);
            this.groupBoxAlerts.TabIndex = 6;
            this.groupBoxAlerts.TabStop = false;
            this.groupBoxAlerts.Text = "Stage 8: Advanced Alerting System";
            // 
            // chkAlertSoundEnabled
            // 
            this.chkAlertSoundEnabled.AutoSize = true;
            this.chkAlertSoundEnabled.Location = new System.Drawing.Point(8, 28);
            this.chkAlertSoundEnabled.Margin = new System.Windows.Forms.Padding(4);
            this.chkAlertSoundEnabled.Name = "chkAlertSoundEnabled";
            this.chkAlertSoundEnabled.Size = new System.Drawing.Size(211, 20);
            this.chkAlertSoundEnabled.TabIndex = 0;
            this.chkAlertSoundEnabled.Text = "Включить звуковые алерты";
            this.chkAlertSoundEnabled.UseVisualStyleBackColor = true;
            // 
            // chkAlertDiscordEnabled
            // 
            this.chkAlertDiscordEnabled.AutoSize = true;
            this.chkAlertDiscordEnabled.Location = new System.Drawing.Point(200, 28);
            this.chkAlertDiscordEnabled.Margin = new System.Windows.Forms.Padding(4);
            this.chkAlertDiscordEnabled.Name = "chkAlertDiscordEnabled";
            this.chkAlertDiscordEnabled.Size = new System.Drawing.Size(194, 20);
            this.chkAlertDiscordEnabled.TabIndex = 1;
            this.chkAlertDiscordEnabled.Text = "Включить Discord алерты";
            this.chkAlertDiscordEnabled.UseVisualStyleBackColor = true;
            // 
            // lblAlertDiscordWebhook
            // 
            this.lblAlertDiscordWebhook.AutoSize = true;
            this.lblAlertDiscordWebhook.Location = new System.Drawing.Point(8, 58);
            this.lblAlertDiscordWebhook.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAlertDiscordWebhook.Name = "lblAlertDiscordWebhook";
            this.lblAlertDiscordWebhook.Size = new System.Drawing.Size(149, 16);
            this.lblAlertDiscordWebhook.TabIndex = 2;
            this.lblAlertDiscordWebhook.Text = "Discord Webhook URL:";
            // 
            // txtAlertDiscordWebhook
            // 
            this.txtAlertDiscordWebhook.Location = new System.Drawing.Point(175, 56);
            this.txtAlertDiscordWebhook.Margin = new System.Windows.Forms.Padding(4);
            this.txtAlertDiscordWebhook.Name = "txtAlertDiscordWebhook";
            this.txtAlertDiscordWebhook.Size = new System.Drawing.Size(400, 22);
            this.txtAlertDiscordWebhook.TabIndex = 3;
            // 
            // lblAlertCooldown
            // 
            this.lblAlertCooldown.AutoSize = true;
            this.lblAlertCooldown.Location = new System.Drawing.Point(8, 88);
            this.lblAlertCooldown.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAlertCooldown.Name = "lblAlertCooldown";
            this.lblAlertCooldown.Size = new System.Drawing.Size(136, 16);
            this.lblAlertCooldown.TabIndex = 4;
            this.lblAlertCooldown.Text = "Cooldown (секунды):";
            // 
            // numAlertCooldown
            // 
            this.numAlertCooldown.Location = new System.Drawing.Point(164, 86);
            this.numAlertCooldown.Margin = new System.Windows.Forms.Padding(4);
            this.numAlertCooldown.Maximum = new decimal(new int[] {
            300,
            0,
            0,
            0});
            this.numAlertCooldown.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.numAlertCooldown.Name = "numAlertCooldown";
            this.numAlertCooldown.Size = new System.Drawing.Size(80, 22);
            this.numAlertCooldown.TabIndex = 5;
            this.numAlertCooldown.Value = new decimal(new int[] {
            30,
            0,
            0,
            0});
            // 
            // btnTestDiscordAlert
            // 
            this.btnTestDiscordAlert.Location = new System.Drawing.Point(8, 120);
            this.btnTestDiscordAlert.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestDiscordAlert.Name = "btnTestDiscordAlert";
            this.btnTestDiscordAlert.Size = new System.Drawing.Size(120, 28);
            this.btnTestDiscordAlert.TabIndex = 6;
            this.btnTestDiscordAlert.Text = "Тест Discord";
            this.btnTestDiscordAlert.UseVisualStyleBackColor = true;
            // 
            // btnTestSoundAlert
            // 
            this.btnTestSoundAlert.Location = new System.Drawing.Point(140, 120);
            this.btnTestSoundAlert.Margin = new System.Windows.Forms.Padding(4);
            this.btnTestSoundAlert.Name = "btnTestSoundAlert";
            this.btnTestSoundAlert.Size = new System.Drawing.Size(120, 28);
            this.btnTestSoundAlert.TabIndex = 7;
            this.btnTestSoundAlert.Text = "Тест звука";
            this.btnTestSoundAlert.UseVisualStyleBackColor = true;
            // 
            // groupBoxAlertSounds
            // 
            this.groupBoxAlertSounds.Controls.Add(this.lblAlertPingSoundPath);
            this.groupBoxAlertSounds.Controls.Add(this.txtAlertPingSoundPath);
            this.groupBoxAlertSounds.Controls.Add(this.btnBrowsePingSound);
            this.groupBoxAlertSounds.Controls.Add(this.lblAlertTickrateSoundPath);
            this.groupBoxAlertSounds.Controls.Add(this.txtAlertTickrateSoundPath);
            this.groupBoxAlertSounds.Controls.Add(this.btnBrowseTickrateSound);
            this.groupBoxAlertSounds.Controls.Add(this.lblAlertTicktimeSoundPath);
            this.groupBoxAlertSounds.Controls.Add(this.txtAlertTicktimeSoundPath);
            this.groupBoxAlertSounds.Controls.Add(this.btnBrowseTicktimeSound);
            this.groupBoxAlertSounds.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxAlertSounds.Location = new System.Drawing.Point(13, 1484);
            this.groupBoxAlertSounds.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxAlertSounds.Name = "groupBoxAlertSounds";
            this.groupBoxAlertSounds.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxAlertSounds.Size = new System.Drawing.Size(753, 140);
            this.groupBoxAlertSounds.TabIndex = 7;
            this.groupBoxAlertSounds.TabStop = false;
            this.groupBoxAlertSounds.Text = "Настройка звуков алертов";
            // 
            // lblAlertPingSoundPath
            // 
            this.lblAlertPingSoundPath.AutoSize = true;
            this.lblAlertPingSoundPath.Location = new System.Drawing.Point(8, 28);
            this.lblAlertPingSoundPath.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAlertPingSoundPath.Name = "lblAlertPingSoundPath";
            this.lblAlertPingSoundPath.Size = new System.Drawing.Size(78, 16);
            this.lblAlertPingSoundPath.TabIndex = 0;
            this.lblAlertPingSoundPath.Text = "Ping спайк:";
            // 
            // txtAlertPingSoundPath
            // 
            this.txtAlertPingSoundPath.Location = new System.Drawing.Point(100, 25);
            this.txtAlertPingSoundPath.Margin = new System.Windows.Forms.Padding(4);
            this.txtAlertPingSoundPath.Name = "txtAlertPingSoundPath";
            this.txtAlertPingSoundPath.Size = new System.Drawing.Size(500, 22);
            this.txtAlertPingSoundPath.TabIndex = 1;
            // 
            // btnBrowsePingSound
            // 
            this.btnBrowsePingSound.Location = new System.Drawing.Point(620, 23);
            this.btnBrowsePingSound.Margin = new System.Windows.Forms.Padding(4);
            this.btnBrowsePingSound.Name = "btnBrowsePingSound";
            this.btnBrowsePingSound.Size = new System.Drawing.Size(50, 28);
            this.btnBrowsePingSound.TabIndex = 2;
            this.btnBrowsePingSound.Text = "...";
            this.btnBrowsePingSound.UseVisualStyleBackColor = true;
            // 
            // lblAlertTickrateSoundPath
            // 
            this.lblAlertTickrateSoundPath.AutoSize = true;
            this.lblAlertTickrateSoundPath.Location = new System.Drawing.Point(8, 58);
            this.lblAlertTickrateSoundPath.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAlertTickrateSoundPath.Name = "lblAlertTickrateSoundPath";
            this.lblAlertTickrateSoundPath.Size = new System.Drawing.Size(100, 16);
            this.lblAlertTickrateSoundPath.TabIndex = 3;
            this.lblAlertTickrateSoundPath.Text = "Tickrate спайк:";
            // 
            // txtAlertTickrateSoundPath
            // 
            this.txtAlertTickrateSoundPath.Location = new System.Drawing.Point(120, 55);
            this.txtAlertTickrateSoundPath.Margin = new System.Windows.Forms.Padding(4);
            this.txtAlertTickrateSoundPath.Name = "txtAlertTickrateSoundPath";
            this.txtAlertTickrateSoundPath.Size = new System.Drawing.Size(480, 22);
            this.txtAlertTickrateSoundPath.TabIndex = 4;
            // 
            // btnBrowseTickrateSound
            // 
            this.btnBrowseTickrateSound.Location = new System.Drawing.Point(620, 53);
            this.btnBrowseTickrateSound.Margin = new System.Windows.Forms.Padding(4);
            this.btnBrowseTickrateSound.Name = "btnBrowseTickrateSound";
            this.btnBrowseTickrateSound.Size = new System.Drawing.Size(50, 28);
            this.btnBrowseTickrateSound.TabIndex = 5;
            this.btnBrowseTickrateSound.Text = "...";
            this.btnBrowseTickrateSound.UseVisualStyleBackColor = true;
            // 
            // lblAlertTicktimeSoundPath
            // 
            this.lblAlertTicktimeSoundPath.AutoSize = true;
            this.lblAlertTicktimeSoundPath.Location = new System.Drawing.Point(8, 88);
            this.lblAlertTicktimeSoundPath.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblAlertTicktimeSoundPath.Name = "lblAlertTicktimeSoundPath";
            this.lblAlertTicktimeSoundPath.Size = new System.Drawing.Size(102, 16);
            this.lblAlertTicktimeSoundPath.TabIndex = 6;
            this.lblAlertTicktimeSoundPath.Text = "Ticktime спайк:";
            // 
            // txtAlertTicktimeSoundPath
            // 
            this.txtAlertTicktimeSoundPath.Location = new System.Drawing.Point(120, 85);
            this.txtAlertTicktimeSoundPath.Margin = new System.Windows.Forms.Padding(4);
            this.txtAlertTicktimeSoundPath.Name = "txtAlertTicktimeSoundPath";
            this.txtAlertTicktimeSoundPath.Size = new System.Drawing.Size(480, 22);
            this.txtAlertTicktimeSoundPath.TabIndex = 7;
            // 
            // btnBrowseTicktimeSound
            // 
            this.btnBrowseTicktimeSound.Location = new System.Drawing.Point(620, 83);
            this.btnBrowseTicktimeSound.Margin = new System.Windows.Forms.Padding(4);
            this.btnBrowseTicktimeSound.Name = "btnBrowseTicktimeSound";
            this.btnBrowseTicktimeSound.Size = new System.Drawing.Size(50, 28);
            this.btnBrowseTicktimeSound.TabIndex = 8;
            this.btnBrowseTicktimeSound.Text = "...";
            this.btnBrowseTicktimeSound.UseVisualStyleBackColor = true;
            // 
            // groupBoxNetworkOptimizer
            // 
            this.groupBoxNetworkOptimizer.Controls.Add(this.chkNetworkOptimizationEnabled);
            this.groupBoxNetworkOptimizer.Controls.Add(this.lblOptimizationThreshold);
            this.groupBoxNetworkOptimizer.Controls.Add(this.numOptimizationThreshold);
            this.groupBoxNetworkOptimizer.Controls.Add(this.lblOptimizationInterval);
            this.groupBoxNetworkOptimizer.Controls.Add(this.numOptimizationInterval);
            this.groupBoxNetworkOptimizer.Controls.Add(this.chkAggressiveOptimization);
            this.groupBoxNetworkOptimizer.Controls.Add(this.btnManualOptimization);
            this.groupBoxNetworkOptimizer.Controls.Add(this.lblLastOptimization);
            this.groupBoxNetworkOptimizer.Controls.Add(this.lblOptimizationStats);
            this.groupBoxNetworkOptimizer.Controls.Add(this.btnClearOptimizationHistory);
            this.groupBoxNetworkOptimizer.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxNetworkOptimizer.Location = new System.Drawing.Point(13, 1204);
            this.groupBoxNetworkOptimizer.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxNetworkOptimizer.Name = "groupBoxNetworkOptimizer";
            this.groupBoxNetworkOptimizer.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxNetworkOptimizer.Size = new System.Drawing.Size(753, 280);
            this.groupBoxNetworkOptimizer.TabIndex = 15;
            this.groupBoxNetworkOptimizer.TabStop = false;
            this.groupBoxNetworkOptimizer.Text = "Этап 7: Интеллектуальная оптимизация сети";
            // 
            // chkNetworkOptimizationEnabled
            // 
            this.chkNetworkOptimizationEnabled.AutoSize = true;
            this.chkNetworkOptimizationEnabled.Location = new System.Drawing.Point(8, 25);
            this.chkNetworkOptimizationEnabled.Margin = new System.Windows.Forms.Padding(4);
            this.chkNetworkOptimizationEnabled.Name = "chkNetworkOptimizationEnabled";
            this.chkNetworkOptimizationEnabled.Size = new System.Drawing.Size(186, 20);
            this.chkNetworkOptimizationEnabled.TabIndex = 0;
            this.chkNetworkOptimizationEnabled.Text = "Включить оптимизацию";
            this.chkNetworkOptimizationEnabled.UseVisualStyleBackColor = true;
            // 
            // lblOptimizationThreshold
            // 
            this.lblOptimizationThreshold.AutoSize = true;
            this.lblOptimizationThreshold.Location = new System.Drawing.Point(8, 55);
            this.lblOptimizationThreshold.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblOptimizationThreshold.Name = "lblOptimizationThreshold";
            this.lblOptimizationThreshold.Size = new System.Drawing.Size(231, 16);
            this.lblOptimizationThreshold.TabIndex = 1;
            this.lblOptimizationThreshold.Text = "Порог качества для оптимизации:";
            // 
            // numOptimizationThreshold
            // 
            this.numOptimizationThreshold.DecimalPlaces = 1;
            this.numOptimizationThreshold.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.numOptimizationThreshold.Location = new System.Drawing.Point(261, 49);
            this.numOptimizationThreshold.Margin = new System.Windows.Forms.Padding(4);
            this.numOptimizationThreshold.Name = "numOptimizationThreshold";
            this.numOptimizationThreshold.Size = new System.Drawing.Size(80, 22);
            this.numOptimizationThreshold.TabIndex = 2;
            this.numOptimizationThreshold.Value = new decimal(new int[] {
            70,
            0,
            0,
            0});
            // 
            // lblOptimizationInterval
            // 
            this.lblOptimizationInterval.AutoSize = true;
            this.lblOptimizationInterval.Location = new System.Drawing.Point(8, 85);
            this.lblOptimizationInterval.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblOptimizationInterval.Name = "lblOptimizationInterval";
            this.lblOptimizationInterval.Size = new System.Drawing.Size(177, 16);
            this.lblOptimizationInterval.TabIndex = 3;
            this.lblOptimizationInterval.Text = "Интервал проверки (мин):";
            // 
            // numOptimizationInterval
            // 
            this.numOptimizationInterval.Location = new System.Drawing.Point(261, 79);
            this.numOptimizationInterval.Margin = new System.Windows.Forms.Padding(4);
            this.numOptimizationInterval.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numOptimizationInterval.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numOptimizationInterval.Name = "numOptimizationInterval";
            this.numOptimizationInterval.Size = new System.Drawing.Size(80, 22);
            this.numOptimizationInterval.TabIndex = 4;
            this.numOptimizationInterval.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // chkAggressiveOptimization
            // 
            this.chkAggressiveOptimization.AutoSize = true;
            this.chkAggressiveOptimization.Location = new System.Drawing.Point(8, 115);
            this.chkAggressiveOptimization.Margin = new System.Windows.Forms.Padding(4);
            this.chkAggressiveOptimization.Name = "chkAggressiveOptimization";
            this.chkAggressiveOptimization.Size = new System.Drawing.Size(203, 20);
            this.chkAggressiveOptimization.TabIndex = 5;
            this.chkAggressiveOptimization.Text = "Агрессивная оптимизация";
            this.chkAggressiveOptimization.UseVisualStyleBackColor = true;
            // 
            // btnManualOptimization
            // 
            this.btnManualOptimization.Location = new System.Drawing.Point(8, 145);
            this.btnManualOptimization.Margin = new System.Windows.Forms.Padding(4);
            this.btnManualOptimization.Name = "btnManualOptimization";
            this.btnManualOptimization.Size = new System.Drawing.Size(150, 28);
            this.btnManualOptimization.TabIndex = 6;
            this.btnManualOptimization.Text = "Запустить оптимизацию";
            this.btnManualOptimization.UseVisualStyleBackColor = true;
            // 
            // lblLastOptimization
            // 
            this.lblLastOptimization.AutoSize = true;
            this.lblLastOptimization.Location = new System.Drawing.Point(8, 185);
            this.lblLastOptimization.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblLastOptimization.Name = "lblLastOptimization";
            this.lblLastOptimization.Size = new System.Drawing.Size(229, 16);
            this.lblLastOptimization.TabIndex = 7;
            this.lblLastOptimization.Text = "Последняя оптимизация: Никогда";
            // 
            // lblOptimizationStats
            // 
            this.lblOptimizationStats.AutoSize = true;
            this.lblOptimizationStats.Location = new System.Drawing.Point(8, 215);
            this.lblOptimizationStats.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblOptimizationStats.Name = "lblOptimizationStats";
            this.lblOptimizationStats.Size = new System.Drawing.Size(232, 16);
            this.lblOptimizationStats.TabIndex = 8;
            this.lblOptimizationStats.Text = "Всего оптимизаций: 0, Успешных: 0";
            // 
            // btnClearOptimizationHistory
            // 
            this.btnClearOptimizationHistory.Location = new System.Drawing.Point(8, 245);
            this.btnClearOptimizationHistory.Margin = new System.Windows.Forms.Padding(4);
            this.btnClearOptimizationHistory.Name = "btnClearOptimizationHistory";
            this.btnClearOptimizationHistory.Size = new System.Drawing.Size(150, 28);
            this.btnClearOptimizationHistory.TabIndex = 9;
            this.btnClearOptimizationHistory.Text = "Очистить историю";
            this.btnClearOptimizationHistory.UseVisualStyleBackColor = true;
            // 
            // groupBoxColorZones
            // 
            this.groupBoxColorZones.Controls.Add(this.lblColorZoneProfile);
            this.groupBoxColorZones.Controls.Add(this.cmbColorZoneProfile);
            this.groupBoxColorZones.Controls.Add(this.lblPingGreen);
            this.groupBoxColorZones.Controls.Add(this.numPingGreen);
            this.groupBoxColorZones.Controls.Add(this.lblPingYellow);
            this.groupBoxColorZones.Controls.Add(this.numPingYellow);
            this.groupBoxColorZones.Controls.Add(this.lblTickrateGreen);
            this.groupBoxColorZones.Controls.Add(this.numTickrateGreen);
            this.groupBoxColorZones.Controls.Add(this.lblTickrateYellow);
            this.groupBoxColorZones.Controls.Add(this.numTickrateYellow);
            this.groupBoxColorZones.Controls.Add(this.lblTicktimeGreen);
            this.groupBoxColorZones.Controls.Add(this.numTicktimeGreen);
            this.groupBoxColorZones.Controls.Add(this.lblTicktimeYellow);
            this.groupBoxColorZones.Controls.Add(this.numTicktimeYellow);
            this.groupBoxColorZones.Controls.Add(this.btnResetColorZones);
            this.groupBoxColorZones.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxColorZones.Location = new System.Drawing.Point(13, 984);
            this.groupBoxColorZones.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxColorZones.Name = "groupBoxColorZones";
            this.groupBoxColorZones.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxColorZones.Size = new System.Drawing.Size(753, 220);
            this.groupBoxColorZones.TabIndex = 9;
            this.groupBoxColorZones.TabStop = false;
            this.groupBoxColorZones.Text = "Color Zone Profiles (ChatGPT Recommended)";
            // 
            // lblColorZoneProfile
            // 
            this.lblColorZoneProfile.AutoSize = true;
            this.lblColorZoneProfile.Location = new System.Drawing.Point(12, 25);
            this.lblColorZoneProfile.Name = "lblColorZoneProfile";
            this.lblColorZoneProfile.Size = new System.Drawing.Size(48, 16);
            this.lblColorZoneProfile.TabIndex = 0;
            this.lblColorZoneProfile.Text = "Profile:";
            // 
            // cmbColorZoneProfile
            // 
            this.cmbColorZoneProfile.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbColorZoneProfile.FormattingEnabled = true;
            this.cmbColorZoneProfile.Items.AddRange(new object[] {
            "Very Low",
            "Low",
            "Medium",
            "High",
            "Custom"});
            this.cmbColorZoneProfile.Location = new System.Drawing.Point(65, 22);
            this.cmbColorZoneProfile.Name = "cmbColorZoneProfile";
            this.cmbColorZoneProfile.Size = new System.Drawing.Size(100, 24);
            this.cmbColorZoneProfile.TabIndex = 1;
            // 
            // lblPingGreen
            // 
            this.lblPingGreen.AutoSize = true;
            this.lblPingGreen.Location = new System.Drawing.Point(12, 60);
            this.lblPingGreen.Name = "lblPingGreen";
            this.lblPingGreen.Size = new System.Drawing.Size(87, 16);
            this.lblPingGreen.TabIndex = 2;
            this.lblPingGreen.Text = "Ping Green ≤:";
            // 
            // numPingGreen
            // 
            this.numPingGreen.Location = new System.Drawing.Point(110, 58);
            this.numPingGreen.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.numPingGreen.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numPingGreen.Name = "numPingGreen";
            this.numPingGreen.Size = new System.Drawing.Size(60, 22);
            this.numPingGreen.TabIndex = 3;
            this.numPingGreen.Value = new decimal(new int[] {
            40,
            0,
            0,
            0});
            // 
            // lblPingYellow
            // 
            this.lblPingYellow.AutoSize = true;
            this.lblPingYellow.Location = new System.Drawing.Point(12, 88);
            this.lblPingYellow.Name = "lblPingYellow";
            this.lblPingYellow.Size = new System.Drawing.Size(90, 16);
            this.lblPingYellow.TabIndex = 4;
            this.lblPingYellow.Text = "Ping Yellow ≤:";
            // 
            // numPingYellow
            // 
            this.numPingYellow.Location = new System.Drawing.Point(110, 86);
            this.numPingYellow.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.numPingYellow.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numPingYellow.Name = "numPingYellow";
            this.numPingYellow.Size = new System.Drawing.Size(60, 22);
            this.numPingYellow.TabIndex = 5;
            this.numPingYellow.Value = new decimal(new int[] {
            80,
            0,
            0,
            0});
            // 
            // lblTickrateGreen
            // 
            this.lblTickrateGreen.AutoSize = true;
            this.lblTickrateGreen.Location = new System.Drawing.Point(200, 60);
            this.lblTickrateGreen.Name = "lblTickrateGreen";
            this.lblTickrateGreen.Size = new System.Drawing.Size(109, 16);
            this.lblTickrateGreen.TabIndex = 6;
            this.lblTickrateGreen.Text = "Tickrate Green ≥:";
            // 
            // numTickrateGreen
            // 
            this.numTickrateGreen.DecimalPlaces = 2;
            this.numTickrateGreen.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numTickrateGreen.Location = new System.Drawing.Point(320, 58);
            this.numTickrateGreen.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numTickrateGreen.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            131072});
            this.numTickrateGreen.Name = "numTickrateGreen";
            this.numTickrateGreen.Size = new System.Drawing.Size(70, 22);
            this.numTickrateGreen.TabIndex = 7;
            this.numTickrateGreen.Value = new decimal(new int[] {
            98,
            0,
            0,
            131072});
            // 
            // lblTickrateYellow
            // 
            this.lblTickrateYellow.AutoSize = true;
            this.lblTickrateYellow.Location = new System.Drawing.Point(200, 88);
            this.lblTickrateYellow.Name = "lblTickrateYellow";
            this.lblTickrateYellow.Size = new System.Drawing.Size(112, 16);
            this.lblTickrateYellow.TabIndex = 8;
            this.lblTickrateYellow.Text = "Tickrate Yellow ≥:";
            // 
            // numTickrateYellow
            // 
            this.numTickrateYellow.DecimalPlaces = 2;
            this.numTickrateYellow.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numTickrateYellow.Location = new System.Drawing.Point(320, 86);
            this.numTickrateYellow.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numTickrateYellow.Minimum = new decimal(new int[] {
            50,
            0,
            0,
            131072});
            this.numTickrateYellow.Name = "numTickrateYellow";
            this.numTickrateYellow.Size = new System.Drawing.Size(70, 22);
            this.numTickrateYellow.TabIndex = 9;
            this.numTickrateYellow.Value = new decimal(new int[] {
            95,
            0,
            0,
            131072});
            // 
            // lblTicktimeGreen
            // 
            this.lblTicktimeGreen.AutoSize = true;
            this.lblTicktimeGreen.Location = new System.Drawing.Point(420, 60);
            this.lblTicktimeGreen.Name = "lblTicktimeGreen";
            this.lblTicktimeGreen.Size = new System.Drawing.Size(111, 16);
            this.lblTicktimeGreen.TabIndex = 10;
            this.lblTicktimeGreen.Text = "Ticktime Green ≤:";
            // 
            // numTicktimeGreen
            // 
            this.numTicktimeGreen.DecimalPlaces = 2;
            this.numTicktimeGreen.Increment = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.numTicktimeGreen.Location = new System.Drawing.Point(540, 58);
            this.numTicktimeGreen.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numTicktimeGreen.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            131072});
            this.numTicktimeGreen.Name = "numTicktimeGreen";
            this.numTicktimeGreen.Size = new System.Drawing.Size(70, 22);
            this.numTicktimeGreen.TabIndex = 11;
            this.numTicktimeGreen.Value = new decimal(new int[] {
            60,
            0,
            0,
            131072});
            // 
            // lblTicktimeYellow
            // 
            this.lblTicktimeYellow.AutoSize = true;
            this.lblTicktimeYellow.Location = new System.Drawing.Point(420, 88);
            this.lblTicktimeYellow.Name = "lblTicktimeYellow";
            this.lblTicktimeYellow.Size = new System.Drawing.Size(114, 16);
            this.lblTicktimeYellow.TabIndex = 12;
            this.lblTicktimeYellow.Text = "Ticktime Yellow ≤:";
            // 
            // numTicktimeYellow
            // 
            this.numTicktimeYellow.DecimalPlaces = 2;
            this.numTicktimeYellow.Increment = new decimal(new int[] {
            5,
            0,
            0,
            131072});
            this.numTicktimeYellow.Location = new System.Drawing.Point(540, 86);
            this.numTicktimeYellow.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numTicktimeYellow.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            131072});
            this.numTicktimeYellow.Name = "numTicktimeYellow";
            this.numTicktimeYellow.Size = new System.Drawing.Size(70, 22);
            this.numTicktimeYellow.TabIndex = 13;
            this.numTicktimeYellow.Value = new decimal(new int[] {
            90,
            0,
            0,
            131072});
            // 
            // btnResetColorZones
            // 
            this.btnResetColorZones.Location = new System.Drawing.Point(630, 22);
            this.btnResetColorZones.Name = "btnResetColorZones";
            this.btnResetColorZones.Size = new System.Drawing.Size(100, 25);
            this.btnResetColorZones.TabIndex = 15;
            this.btnResetColorZones.Text = "Reset to Default";
            this.btnResetColorZones.UseVisualStyleBackColor = true;
            // 
            // groupBoxNetworkQuality
            // 
            this.groupBoxNetworkQuality.Controls.Add(this.chkNetworkQualityEnabled);
            this.groupBoxNetworkQuality.Controls.Add(this.chkNetworkQualityOverlay);
            this.groupBoxNetworkQuality.Controls.Add(this.lblQualityHistorySize);
            this.groupBoxNetworkQuality.Controls.Add(this.numQualityHistorySize);
            this.groupBoxNetworkQuality.Controls.Add(this.lblStabilityThreshold);
            this.groupBoxNetworkQuality.Controls.Add(this.numStabilityThreshold);
            this.groupBoxNetworkQuality.Controls.Add(this.lblQualityThreshold);
            this.groupBoxNetworkQuality.Controls.Add(this.numQualityThreshold);
            this.groupBoxNetworkQuality.Controls.Add(this.btnResetQualityAnalyzer);
            this.groupBoxNetworkQuality.Controls.Add(this.lblCurrentQuality);
            this.groupBoxNetworkQuality.Controls.Add(this.lblQualityRating);
            this.groupBoxNetworkQuality.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBoxNetworkQuality.Location = new System.Drawing.Point(13, 804);
            this.groupBoxNetworkQuality.Margin = new System.Windows.Forms.Padding(4);
            this.groupBoxNetworkQuality.Name = "groupBoxNetworkQuality";
            this.groupBoxNetworkQuality.Padding = new System.Windows.Forms.Padding(4);
            this.groupBoxNetworkQuality.Size = new System.Drawing.Size(753, 180);
            this.groupBoxNetworkQuality.TabIndex = 9;
            this.groupBoxNetworkQuality.TabStop = false;
            this.groupBoxNetworkQuality.Text = "Stage 6: Анализ качества сети";
            // 
            // chkNetworkQualityEnabled
            // 
            this.chkNetworkQualityEnabled.AutoSize = true;
            this.chkNetworkQualityEnabled.Location = new System.Drawing.Point(8, 25);
            this.chkNetworkQualityEnabled.Margin = new System.Windows.Forms.Padding(4);
            this.chkNetworkQualityEnabled.Name = "chkNetworkQualityEnabled";
            this.chkNetworkQualityEnabled.Size = new System.Drawing.Size(241, 20);
            this.chkNetworkQualityEnabled.TabIndex = 0;
            this.chkNetworkQualityEnabled.Text = "Включить анализ качества сети";
            this.chkNetworkQualityEnabled.UseVisualStyleBackColor = true;
            // 
            // chkNetworkQualityOverlay
            // 
            this.chkNetworkQualityOverlay.AutoSize = true;
            this.chkNetworkQualityOverlay.Location = new System.Drawing.Point(267, 23);
            this.chkNetworkQualityOverlay.Margin = new System.Windows.Forms.Padding(4);
            this.chkNetworkQualityOverlay.Name = "chkNetworkQualityOverlay";
            this.chkNetworkQualityOverlay.Size = new System.Drawing.Size(299, 20);
            this.chkNetworkQualityOverlay.TabIndex = 10;
            this.chkNetworkQualityOverlay.Text = "Показывать рейтинг качества в оверлее";
            this.chkNetworkQualityOverlay.UseVisualStyleBackColor = true;
            // 
            // lblQualityHistorySize
            // 
            this.lblQualityHistorySize.AutoSize = true;
            this.lblQualityHistorySize.Location = new System.Drawing.Point(8, 55);
            this.lblQualityHistorySize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblQualityHistorySize.Name = "lblQualityHistorySize";
            this.lblQualityHistorySize.Size = new System.Drawing.Size(114, 16);
            this.lblQualityHistorySize.TabIndex = 1;
            this.lblQualityHistorySize.Text = "Размер буфера:";
            // 
            // numQualityHistorySize
            // 
            this.numQualityHistorySize.Location = new System.Drawing.Point(160, 53);
            this.numQualityHistorySize.Margin = new System.Windows.Forms.Padding(4);
            this.numQualityHistorySize.Maximum = new decimal(new int[] {
            500,
            0,
            0,
            0});
            this.numQualityHistorySize.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numQualityHistorySize.Name = "numQualityHistorySize";
            this.numQualityHistorySize.Size = new System.Drawing.Size(80, 22);
            this.numQualityHistorySize.TabIndex = 2;
            this.numQualityHistorySize.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // lblStabilityThreshold
            // 
            this.lblStabilityThreshold.AutoSize = true;
            this.lblStabilityThreshold.Location = new System.Drawing.Point(250, 55);
            this.lblStabilityThreshold.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStabilityThreshold.Name = "lblStabilityThreshold";
            this.lblStabilityThreshold.Size = new System.Drawing.Size(144, 16);
            this.lblStabilityThreshold.TabIndex = 3;
            this.lblStabilityThreshold.Text = "Порог стабильности:";
            // 
            // numStabilityThreshold
            // 
            this.numStabilityThreshold.DecimalPlaces = 2;
            this.numStabilityThreshold.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numStabilityThreshold.Location = new System.Drawing.Point(412, 53);
            this.numStabilityThreshold.Margin = new System.Windows.Forms.Padding(4);
            this.numStabilityThreshold.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numStabilityThreshold.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numStabilityThreshold.Name = "numStabilityThreshold";
            this.numStabilityThreshold.Size = new System.Drawing.Size(80, 22);
            this.numStabilityThreshold.TabIndex = 4;
            this.numStabilityThreshold.Value = new decimal(new int[] {
            15,
            0,
            0,
            131072});
            // 
            // lblQualityThreshold
            // 
            this.lblQualityThreshold.AutoSize = true;
            this.lblQualityThreshold.Location = new System.Drawing.Point(8, 85);
            this.lblQualityThreshold.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblQualityThreshold.Name = "lblQualityThreshold";
            this.lblQualityThreshold.Size = new System.Drawing.Size(114, 16);
            this.lblQualityThreshold.TabIndex = 5;
            this.lblQualityThreshold.Text = "Порог качества:";
            // 
            // numQualityThreshold
            // 
            this.numQualityThreshold.DecimalPlaces = 2;
            this.numQualityThreshold.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numQualityThreshold.Location = new System.Drawing.Point(160, 83);
            this.numQualityThreshold.Margin = new System.Windows.Forms.Padding(4);
            this.numQualityThreshold.Maximum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.numQualityThreshold.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.numQualityThreshold.Name = "numQualityThreshold";
            this.numQualityThreshold.Size = new System.Drawing.Size(80, 22);
            this.numQualityThreshold.TabIndex = 6;
            this.numQualityThreshold.Value = new decimal(new int[] {
            80,
            0,
            0,
            131072});
            // 
            // btnResetQualityAnalyzer
            // 
            this.btnResetQualityAnalyzer.Location = new System.Drawing.Point(250, 81);
            this.btnResetQualityAnalyzer.Margin = new System.Windows.Forms.Padding(4);
            this.btnResetQualityAnalyzer.Name = "btnResetQualityAnalyzer";
            this.btnResetQualityAnalyzer.Size = new System.Drawing.Size(120, 28);
            this.btnResetQualityAnalyzer.TabIndex = 7;
            this.btnResetQualityAnalyzer.Text = "Сбросить анализ";
            this.btnResetQualityAnalyzer.UseVisualStyleBackColor = true;
            // 
            // lblCurrentQuality
            // 
            this.lblCurrentQuality.AutoSize = true;
            this.lblCurrentQuality.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblCurrentQuality.Location = new System.Drawing.Point(8, 120);
            this.lblCurrentQuality.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblCurrentQuality.Name = "lblCurrentQuality";
            this.lblCurrentQuality.Size = new System.Drawing.Size(173, 18);
            this.lblCurrentQuality.TabIndex = 8;
            this.lblCurrentQuality.Text = "Качество сети: 100%";
            // 
            // lblQualityRating
            // 
            this.lblQualityRating.AutoSize = true;
            this.lblQualityRating.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblQualityRating.ForeColor = System.Drawing.Color.Green;
            this.lblQualityRating.Location = new System.Drawing.Point(8, 145);
            this.lblQualityRating.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblQualityRating.Name = "lblQualityRating";
            this.lblQualityRating.Size = new System.Drawing.Size(148, 18);
            this.lblQualityRating.TabIndex = 9;
            this.lblQualityRating.Text = "Рейтинг: Excellent";
            // 
            // groupBox5
            // 
            this.groupBox5.Controls.Add(this.chkUiRefreshHidden);
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
            this.groupBox5.Controls.Add(this.chkTickrateValueGuiSmoothing);
            this.groupBox5.Controls.Add(this.chkTickrateValueOverlaySmoothing);
            this.groupBox5.Controls.Add(this.chkTicktimeValueOverlaySmoothing);
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
            this.groupBox5.Size = new System.Drawing.Size(753, 400);
            this.groupBox5.TabIndex = 4;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Универсальные";
            // 
            // chkUiRefreshHidden
            // 
            this.chkUiRefreshHidden.AutoSize = true;
            this.chkUiRefreshHidden.Location = new System.Drawing.Point(363, 160);
            this.chkUiRefreshHidden.Margin = new System.Windows.Forms.Padding(4);
            this.chkUiRefreshHidden.Name = "chkUiRefreshHidden";
            this.chkUiRefreshHidden.Size = new System.Drawing.Size(241, 20);
            this.chkUiRefreshHidden.TabIndex = 17;
            this.chkUiRefreshHidden.Text = "Обновлять окно, когда спрятано";
            this.chkUiRefreshHidden.UseVisualStyleBackColor = true;
            // 
            // chkStunEnable
            // 
            this.chkStunEnable.AutoSize = true;
            this.chkStunEnable.Location = new System.Drawing.Point(363, 56);
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
            this.chkShowPingSpikes.Location = new System.Drawing.Point(363, 84);
            this.chkShowPingSpikes.Margin = new System.Windows.Forms.Padding(4);
            this.chkShowPingSpikes.Name = "chkShowPingSpikes";
            this.chkShowPingSpikes.Size = new System.Drawing.Size(319, 20);
            this.chkShowPingSpikes.TabIndex = 9;
            this.chkShowPingSpikes.Text = "Показывать индикатор (!) при спайках пинга";
            this.chkShowPingSpikes.UseVisualStyleBackColor = true;
            // 
            // lblPingSpikeThreshold
            // 
            this.lblPingSpikeThreshold.AutoSize = true;
            this.lblPingSpikeThreshold.Location = new System.Drawing.Point(360, 112);
            this.lblPingSpikeThreshold.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPingSpikeThreshold.Name = "lblPingSpikeThreshold";
            this.lblPingSpikeThreshold.Size = new System.Drawing.Size(167, 16);
            this.lblPingSpikeThreshold.TabIndex = 10;
            this.lblPingSpikeThreshold.Text = "Порог спайка пинга (мс):";
            // 
            // numPingSpikeThreshold
            // 
            this.numPingSpikeThreshold.Location = new System.Drawing.Point(545, 110);
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
            this.chkRtssOnlyActive.Location = new System.Drawing.Point(363, 28);
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
            this.chkEnableIPv6.Location = new System.Drawing.Point(20, 255);
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
            this.chkDedupMultiNic.Location = new System.Drawing.Point(20, 227);
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
            this.chkTickrateSmoothing.Location = new System.Drawing.Point(20, 115);
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
            this.chkPingGraphOverlaySmoothing.Location = new System.Drawing.Point(20, 143);
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
            this.chkTickrateGraphOverlaySmoothing.Location = new System.Drawing.Point(20, 171);
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
            this.chkTicktimeGraphOverlaySmoothing.Location = new System.Drawing.Point(20, 199);
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
            this.chkPingValueOverlaySmoothing.Location = new System.Drawing.Point(20, 283);
            this.chkPingValueOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkPingValueOverlaySmoothing.Name = "chkPingValueOverlaySmoothing";
            this.chkPingValueOverlaySmoothing.Size = new System.Drawing.Size(295, 20);
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
            this.chkPingValueGuiSmoothing.Size = new System.Drawing.Size(262, 20);
            this.chkPingValueGuiSmoothing.TabIndex = 14;
            this.chkPingValueGuiSmoothing.Text = "Сглаживание значений пинга в GUI";
            this.chkPingValueGuiSmoothing.UseVisualStyleBackColor = true;
            // 
            // chkTickrateValueGuiSmoothing
            // 
            this.chkTickrateValueGuiSmoothing.AutoSize = true;
            this.chkTickrateValueGuiSmoothing.Location = new System.Drawing.Point(20, 339);
            this.chkTickrateValueGuiSmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateValueGuiSmoothing.Name = "chkTickrateValueGuiSmoothing";
            this.chkTickrateValueGuiSmoothing.Size = new System.Drawing.Size(285, 20);
            this.chkTickrateValueGuiSmoothing.TabIndex = 15;
            this.chkTickrateValueGuiSmoothing.Text = "Сглаживание значений тикрейта в GUI";
            this.chkTickrateValueGuiSmoothing.UseVisualStyleBackColor = true;
            // 
            // chkTickrateValueOverlaySmoothing
            // 
            this.chkTickrateValueOverlaySmoothing.AutoSize = true;
            this.chkTickrateValueOverlaySmoothing.Location = new System.Drawing.Point(20, 367);
            this.chkTickrateValueOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTickrateValueOverlaySmoothing.Name = "chkTickrateValueOverlaySmoothing";
            this.chkTickrateValueOverlaySmoothing.Size = new System.Drawing.Size(318, 20);
            this.chkTickrateValueOverlaySmoothing.TabIndex = 16;
            this.chkTickrateValueOverlaySmoothing.Text = "Сглаживание значений тикрейта в оверлее";
            this.chkTickrateValueOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkTicktimeValueOverlaySmoothing
            // 
            this.chkTicktimeValueOverlaySmoothing.AutoSize = true;
            this.chkTicktimeValueOverlaySmoothing.Location = new System.Drawing.Point(20, 395);
            this.chkTicktimeValueOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTicktimeValueOverlaySmoothing.Name = "chkTicktimeValueOverlaySmoothing";
            this.chkTicktimeValueOverlaySmoothing.Size = new System.Drawing.Size(318, 20);
            this.chkTicktimeValueOverlaySmoothing.TabIndex = 17;
            this.chkTicktimeValueOverlaySmoothing.Text = "Сглаживание значений тиктайма в оверлее";
            this.chkTicktimeValueOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkTrafficValueOverlaySmoothing
            // 
            this.chkTrafficValueOverlaySmoothing.AutoSize = true;
            this.chkTrafficValueOverlaySmoothing.Location = new System.Drawing.Point(20, 423);
            this.chkTrafficValueOverlaySmoothing.Margin = new System.Windows.Forms.Padding(4);
            this.chkTrafficValueOverlaySmoothing.Name = "chkTrafficValueOverlaySmoothing";
            this.chkTrafficValueOverlaySmoothing.Size = new System.Drawing.Size(314, 20);
            this.chkTrafficValueOverlaySmoothing.TabIndex = 18;
            this.chkTrafficValueOverlaySmoothing.Text = "Сглаживание значений трафика в оверлее";
            this.chkTrafficValueOverlaySmoothing.UseVisualStyleBackColor = true;
            // 
            // chkPingTargetActiveOnly
            // 
            this.chkPingTargetActiveOnly.AutoSize = true;
            this.chkPingTargetActiveOnly.Location = new System.Drawing.Point(363, 132);
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
            this.groupBox4.Location = new System.Drawing.Point(13, 306);
            this.groupBox4.Margin = new System.Windows.Forms.Padding(4);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Padding = new System.Windows.Forms.Padding(4);
            this.groupBox4.Size = new System.Drawing.Size(753, 98);
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
            this.groupBox3.Size = new System.Drawing.Size(753, 98);
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
            this.captureFilterTextBox.Location = new System.Drawing.Point(284, 62);
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
            this.groupBox2.Size = new System.Drawing.Size(753, 98);
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
            this.overlayFpsNumeric.Location = new System.Drawing.Point(182, 62);
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
            this.groupBox1.Size = new System.Drawing.Size(753, 98);
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
            this.liveMaxRowsNumeric.Location = new System.Drawing.Point(213, 62);
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
            this.groupBoxTickrateChart.ResumeLayout(false);
            this.groupBoxTickrateChart.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numTickrateChartMaxPoints)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTickrateChartHistoryHours)).EndInit();
            this.groupBoxExtendedOverlay.ResumeLayout(false);
            this.groupBoxExtendedOverlay.PerformLayout();
            this.groupBoxDebugSettings.ResumeLayout(false);
            this.groupBoxDebugSettings.PerformLayout();
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
            this.groupBoxSpikeAdvanced.ResumeLayout(false);
            this.groupBoxSpikeAdvanced.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numEmaAlpha)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numEwSigmaAlpha)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSensitivityMultiplier)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numHysteresisRatio)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numRefractoryPeriod)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMinEnergyThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numInitWindowSize)).EndInit();
            this.groupBoxAlerts.ResumeLayout(false);
            this.groupBoxAlerts.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numAlertCooldown)).EndInit();
            this.groupBoxAlertSounds.ResumeLayout(false);
            this.groupBoxAlertSounds.PerformLayout();
            this.groupBoxNetworkOptimizer.ResumeLayout(false);
            this.groupBoxNetworkOptimizer.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numOptimizationThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numOptimizationInterval)).EndInit();
            this.groupBoxColorZones.ResumeLayout(false);
            this.groupBoxColorZones.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numPingGreen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPingYellow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTickrateGreen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTickrateYellow)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTicktimeGreen)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTicktimeYellow)).EndInit();
            this.groupBoxNetworkQuality.ResumeLayout(false);
            this.groupBoxNetworkQuality.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numQualityHistorySize)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numStabilityThreshold)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numQualityThreshold)).EndInit();
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
    private System.Windows.Forms.CheckBox chkTickrateValueGuiSmoothing;
    private System.Windows.Forms.CheckBox chkTickrateValueOverlaySmoothing;
    private System.Windows.Forms.CheckBox chkTicktimeValueOverlaySmoothing;
    private System.Windows.Forms.CheckBox chkTrafficValueOverlaySmoothing;
    private System.Windows.Forms.CheckBox chkDedupMultiNic;
    private System.Windows.Forms.CheckBox chkEnableIPv6;
    private System.Windows.Forms.CheckBox chkRtssOnlyActive;
    private System.Windows.Forms.CheckBox chkUiRefreshHidden;
    private System.Windows.Forms.CheckBox chkStunEnable;
    private System.Windows.Forms.CheckBox chkShowPingSpikes;
    private System.Windows.Forms.NumericUpDown numPingSpikeThreshold;
    private System.Windows.Forms.Label lblPingSpikeThreshold;

        private System.Windows.Forms.GroupBox groupBoxVpnBypass;
        private System.Windows.Forms.CheckBox chkVpnBypassBasic;
        private System.Windows.Forms.CheckBox chkVpnBypassAdvanced;
        
        // Debug Settings Controls
        private System.Windows.Forms.GroupBox groupBoxDebugSettings;
        private System.Windows.Forms.CheckBox chkEnableTextLogs;
        
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
        private System.Windows.Forms.GroupBox groupBoxSpikeAdvanced;
        private System.Windows.Forms.Label lblEmaAlpha;
        private System.Windows.Forms.NumericUpDown numEmaAlpha;
        private System.Windows.Forms.Label lblEwSigmaAlpha;
        private System.Windows.Forms.NumericUpDown numEwSigmaAlpha;
        private System.Windows.Forms.Label lblSensitivityMultiplier;
        private System.Windows.Forms.NumericUpDown numSensitivityMultiplier;
        private System.Windows.Forms.Label lblHysteresisRatio;
        private System.Windows.Forms.NumericUpDown numHysteresisRatio;
        private System.Windows.Forms.Label lblRefractoryPeriod;
        private System.Windows.Forms.NumericUpDown numRefractoryPeriod;
        private System.Windows.Forms.Label lblMinEnergyThreshold;
        private System.Windows.Forms.NumericUpDown numMinEnergyThreshold;
        private System.Windows.Forms.Label lblInitWindowSize;
        private System.Windows.Forms.NumericUpDown numInitWindowSize;
        private System.Windows.Forms.Button btnResetSpikeDefaults;
        private System.Windows.Forms.Button btnSpikePresetsSensitive;
        private System.Windows.Forms.Button btnSpikePresetsBalanced;
        private System.Windows.Forms.Button btnSpikePresetsConservative;
        
        private System.Windows.Forms.Panel panelButtons;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnApply;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnReset;
        private System.Windows.Forms.GroupBox groupBoxAlerts;
        private System.Windows.Forms.CheckBox chkAlertSoundEnabled;
        private System.Windows.Forms.CheckBox chkAlertDiscordEnabled;
        private System.Windows.Forms.Label lblAlertDiscordWebhook;
        private System.Windows.Forms.TextBox txtAlertDiscordWebhook;
        private System.Windows.Forms.Label lblAlertCooldown;
        private System.Windows.Forms.NumericUpDown numAlertCooldown;
        private System.Windows.Forms.Button btnTestDiscordAlert;
        private System.Windows.Forms.Button btnTestSoundAlert;
        private System.Windows.Forms.GroupBox groupBoxAlertSounds;
        private System.Windows.Forms.Label lblAlertPingSoundPath;
        private System.Windows.Forms.TextBox txtAlertPingSoundPath;
        private System.Windows.Forms.Button btnBrowsePingSound;
        private System.Windows.Forms.Label lblAlertTickrateSoundPath;
        private System.Windows.Forms.TextBox txtAlertTickrateSoundPath;
        private System.Windows.Forms.Button btnBrowseTickrateSound;
        private System.Windows.Forms.Label lblAlertTicktimeSoundPath;
        private System.Windows.Forms.TextBox txtAlertTicktimeSoundPath;
        private System.Windows.Forms.Button btnBrowseTicktimeSound;
        private System.Windows.Forms.GroupBox groupBoxNetworkQuality;
        private System.Windows.Forms.CheckBox chkNetworkQualityEnabled;
        private System.Windows.Forms.CheckBox chkNetworkQualityOverlay;
        private System.Windows.Forms.Label lblQualityHistorySize;
        private System.Windows.Forms.NumericUpDown numQualityHistorySize;
        private System.Windows.Forms.Label lblStabilityThreshold;
        private System.Windows.Forms.NumericUpDown numStabilityThreshold;
        private System.Windows.Forms.Label lblQualityThreshold;
        private System.Windows.Forms.NumericUpDown numQualityThreshold;
        private System.Windows.Forms.Button btnResetQualityAnalyzer;
        private System.Windows.Forms.Label lblCurrentQuality;
        private System.Windows.Forms.Label lblQualityRating;
        
        // Tickrate Chart Settings
        private System.Windows.Forms.GroupBox groupBoxTickrateChart;
        private System.Windows.Forms.CheckBox chkTickrateChartEnabled;
        private System.Windows.Forms.ComboBox cmbTickrateChartMode;
        private System.Windows.Forms.Label lblTickrateChartMode;
        private System.Windows.Forms.CheckBox chkTickrateChartPerServer;
        private System.Windows.Forms.CheckBox chkTickrateChartCompression;
        private System.Windows.Forms.CheckBox chkTickrateChartTimeScale;
        private System.Windows.Forms.CheckBox chkTickrateChartTrimming;
        private System.Windows.Forms.Label lblTickrateChartMaxPoints;
        private System.Windows.Forms.NumericUpDown numTickrateChartMaxPoints;
        private System.Windows.Forms.Label lblTickrateChartHistoryHours;
        private System.Windows.Forms.NumericUpDown numTickrateChartHistoryHours;
        private System.Windows.Forms.Button btnTickrateChartReset;
        
        private System.Windows.Forms.GroupBox groupBoxNetworkOptimizer;
        private System.Windows.Forms.CheckBox chkNetworkOptimizationEnabled;
        private System.Windows.Forms.Label lblOptimizationThreshold;
        private System.Windows.Forms.NumericUpDown numOptimizationThreshold;
        private System.Windows.Forms.Label lblOptimizationInterval;
        private System.Windows.Forms.NumericUpDown numOptimizationInterval;
        private System.Windows.Forms.CheckBox chkAggressiveOptimization;
        private System.Windows.Forms.Button btnManualOptimization;
        private System.Windows.Forms.Label lblLastOptimization;
        private System.Windows.Forms.Label lblOptimizationStats;
        private System.Windows.Forms.Button btnClearOptimizationHistory;
        
        // Color Zones Settings
        private System.Windows.Forms.GroupBox groupBoxColorZones;
        private System.Windows.Forms.Label lblColorZoneProfile;
        private System.Windows.Forms.ComboBox cmbColorZoneProfile;
        private System.Windows.Forms.Label lblPingGreen;
        private System.Windows.Forms.NumericUpDown numPingGreen;
        private System.Windows.Forms.Label lblPingYellow;
        private System.Windows.Forms.NumericUpDown numPingYellow;
        private System.Windows.Forms.Label lblTickrateGreen;
        private System.Windows.Forms.NumericUpDown numTickrateGreen;
        private System.Windows.Forms.Label lblTickrateYellow;
        private System.Windows.Forms.NumericUpDown numTickrateYellow;
        private System.Windows.Forms.Label lblTicktimeGreen;
        private System.Windows.Forms.NumericUpDown numTicktimeGreen;
        private System.Windows.Forms.Label lblTicktimeYellow;
        private System.Windows.Forms.NumericUpDown numTicktimeYellow;
        private System.Windows.Forms.Button btnResetColorZones;
        
        // Extended Overlay Information
        private System.Windows.Forms.GroupBox groupBoxExtendedOverlay;
        private System.Windows.Forms.CheckBox chkShowActiveProcess;
        private System.Windows.Forms.CheckBox chkShowSessionTime;
        private System.Windows.Forms.CheckBox chkShowExternalIP;
        private System.Windows.Forms.CheckBox chkShowSessionStats;
        private System.Windows.Forms.CheckBox chkShowServerInfo;
        private System.Windows.Forms.CheckBox chkShowPacketCounters;
        private System.Windows.Forms.CheckBox chkShowConnectionType;
        private System.Windows.Forms.CheckBox chkShowDiagnosticInfo;
    }
}