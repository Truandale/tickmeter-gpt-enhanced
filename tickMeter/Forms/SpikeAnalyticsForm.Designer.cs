namespace tickMeter.Forms
{
    partial class SpikeAnalyticsForm
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
            if (disposing)
            {
                _updateTimer?.Stop();
                _updateTimer?.Dispose();
                if (components != null)
                {
                    components.Dispose();
                }
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
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea2 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
            System.Windows.Forms.DataVisualization.Charting.Legend legend2 = new System.Windows.Forms.DataVisualization.Charting.Legend();
            this.tabControlMain = new System.Windows.Forms.TabControl();
            this.tabPageOverview = new System.Windows.Forms.TabPage();
            this.tableLayoutPanelOverview = new System.Windows.Forms.TableLayoutPanel();
            this.groupBoxStatistics = new System.Windows.Forms.GroupBox();
            this.lblSpikeRate = new System.Windows.Forms.Label();
            this.lblMaxSeverity = new System.Windows.Forms.Label();
            this.lblAvgSeverity = new System.Windows.Forms.Label();
            this.lblTicktimeSpikes = new System.Windows.Forms.Label();
            this.lblTickrateSpikes = new System.Windows.Forms.Label();
            this.lblPingSpikes = new System.Windows.Forms.Label();
            this.lblSpikes15m = new System.Windows.Forms.Label();
            this.lblSpikes1h = new System.Windows.Forms.Label();
            this.lblSpikes24h = new System.Windows.Forms.Label();
            this.lblTotalSpikes = new System.Windows.Forms.Label();
            this.chartSpikeFrequency = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.chartSpikeDistribution = new System.Windows.Forms.DataVisualization.Charting.Chart();
            this.tabPageDetailed = new System.Windows.Forms.TabPage();
            this.dataGridViewSpikes = new System.Windows.Forms.DataGridView();
            this.tabPageExport = new System.Windows.Forms.TabPage();
            this.groupBoxExport = new System.Windows.Forms.GroupBox();
            this.btnClearData = new System.Windows.Forms.Button();
            this.btnExportHTML = new System.Windows.Forms.Button();
            this.btnExportJSON = new System.Windows.Forms.Button();
            this.btnExportCSV = new System.Windows.Forms.Button();
            this.labelExportDescription = new System.Windows.Forms.Label();
            this.tabControlMain.SuspendLayout();
            this.tabPageOverview.SuspendLayout();
            this.tableLayoutPanelOverview.SuspendLayout();
            this.groupBoxStatistics.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartSpikeFrequency)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartSpikeDistribution)).BeginInit();
            this.tabPageDetailed.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSpikes)).BeginInit();
            this.tabPageExport.SuspendLayout();
            this.groupBoxExport.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControlMain
            // 
            this.tabControlMain.Controls.Add(this.tabPageOverview);
            this.tabControlMain.Controls.Add(this.tabPageDetailed);
            this.tabControlMain.Controls.Add(this.tabPageExport);
            this.tabControlMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControlMain.Location = new System.Drawing.Point(0, 0);
            this.tabControlMain.Name = "tabControlMain";
            this.tabControlMain.SelectedIndex = 0;
            this.tabControlMain.Size = new System.Drawing.Size(1184, 761);
            this.tabControlMain.TabIndex = 0;
            // 
            // tabPageOverview
            // 
            this.tabPageOverview.Controls.Add(this.tableLayoutPanelOverview);
            this.tabPageOverview.Location = new System.Drawing.Point(4, 25);
            this.tabPageOverview.Name = "tabPageOverview";
            this.tabPageOverview.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageOverview.Size = new System.Drawing.Size(1176, 732);
            this.tabPageOverview.TabIndex = 0;
            this.tabPageOverview.Text = "Обзор";
            this.tabPageOverview.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanelOverview
            // 
            this.tableLayoutPanelOverview.ColumnCount = 2;
            this.tableLayoutPanelOverview.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelOverview.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelOverview.Controls.Add(this.groupBoxStatistics, 0, 0);
            this.tableLayoutPanelOverview.Controls.Add(this.chartSpikeFrequency, 0, 1);
            this.tableLayoutPanelOverview.Controls.Add(this.chartSpikeDistribution, 1, 0);
            this.tableLayoutPanelOverview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelOverview.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanelOverview.Name = "tableLayoutPanelOverview";
            this.tableLayoutPanelOverview.RowCount = 2;
            this.tableLayoutPanelOverview.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelOverview.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanelOverview.Size = new System.Drawing.Size(1170, 726);
            this.tableLayoutPanelOverview.TabIndex = 0;
            // 
            // groupBoxStatistics
            // 
            this.groupBoxStatistics.Controls.Add(this.lblSpikeRate);
            this.groupBoxStatistics.Controls.Add(this.lblMaxSeverity);
            this.groupBoxStatistics.Controls.Add(this.lblAvgSeverity);
            this.groupBoxStatistics.Controls.Add(this.lblTicktimeSpikes);
            this.groupBoxStatistics.Controls.Add(this.lblTickrateSpikes);
            this.groupBoxStatistics.Controls.Add(this.lblPingSpikes);
            this.groupBoxStatistics.Controls.Add(this.lblSpikes15m);
            this.groupBoxStatistics.Controls.Add(this.lblSpikes1h);
            this.groupBoxStatistics.Controls.Add(this.lblSpikes24h);
            this.groupBoxStatistics.Controls.Add(this.lblTotalSpikes);
            this.groupBoxStatistics.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBoxStatistics.Location = new System.Drawing.Point(3, 3);
            this.groupBoxStatistics.Name = "groupBoxStatistics";
            this.groupBoxStatistics.Size = new System.Drawing.Size(579, 357);
            this.groupBoxStatistics.TabIndex = 0;
            this.groupBoxStatistics.TabStop = false;
            this.groupBoxStatistics.Text = "Статистика спайков";
            // 
            // lblSpikeRate
            // 
            this.lblSpikeRate.AutoSize = true;
            this.lblSpikeRate.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblSpikeRate.Location = new System.Drawing.Point(15, 310);
            this.lblSpikeRate.Name = "lblSpikeRate";
            this.lblSpikeRate.Size = new System.Drawing.Size(170, 20);
            this.lblSpikeRate.TabIndex = 9;
            this.lblSpikeRate.Text = "Частота: 0 спайков/мин";
            // 
            // lblMaxSeverity
            // 
            this.lblMaxSeverity.AutoSize = true;
            this.lblMaxSeverity.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblMaxSeverity.Location = new System.Drawing.Point(15, 280);
            this.lblMaxSeverity.Name = "lblMaxSeverity";
            this.lblMaxSeverity.Size = new System.Drawing.Size(195, 20);
            this.lblMaxSeverity.TabIndex = 8;
            this.lblMaxSeverity.Text = "Макс. серьезность: N/A";
            // 
            // lblAvgSeverity
            // 
            this.lblAvgSeverity.AutoSize = true;
            this.lblAvgSeverity.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblAvgSeverity.Location = new System.Drawing.Point(15, 250);
            this.lblAvgSeverity.Name = "lblAvgSeverity";
            this.lblAvgSeverity.Size = new System.Drawing.Size(218, 20);
            this.lblAvgSeverity.TabIndex = 7;
            this.lblAvgSeverity.Text = "Средняя серьезность: N/A";
            // 
            // lblTicktimeSpikes
            // 
            this.lblTicktimeSpikes.AutoSize = true;
            this.lblTicktimeSpikes.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblTicktimeSpikes.ForeColor = System.Drawing.Color.Orange;
            this.lblTicktimeSpikes.Location = new System.Drawing.Point(15, 210);
            this.lblTicktimeSpikes.Name = "lblTicktimeSpikes";
            this.lblTicktimeSpikes.Size = new System.Drawing.Size(95, 20);
            this.lblTicktimeSpikes.TabIndex = 6;
            this.lblTicktimeSpikes.Text = "Ticktime: 0";
            // 
            // lblTickrateSpikes
            // 
            this.lblTickrateSpikes.AutoSize = true;
            this.lblTickrateSpikes.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblTickrateSpikes.ForeColor = System.Drawing.Color.DarkOrange;
            this.lblTickrateSpikes.Location = new System.Drawing.Point(15, 180);
            this.lblTickrateSpikes.Name = "lblTickrateSpikes";
            this.lblTickrateSpikes.Size = new System.Drawing.Size(91, 20);
            this.lblTickrateSpikes.TabIndex = 5;
            this.lblTickrateSpikes.Text = "Tickrate: 0";
            // 
            // lblPingSpikes
            // 
            this.lblPingSpikes.AutoSize = true;
            this.lblPingSpikes.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblPingSpikes.ForeColor = System.Drawing.Color.Red;
            this.lblPingSpikes.Location = new System.Drawing.Point(15, 150);
            this.lblPingSpikes.Name = "lblPingSpikes";
            this.lblPingSpikes.Size = new System.Drawing.Size(59, 20);
            this.lblPingSpikes.TabIndex = 4;
            this.lblPingSpikes.Text = "Ping: 0";
            // 
            // lblSpikes15m
            // 
            this.lblSpikes15m.AutoSize = true;
            this.lblSpikes15m.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblSpikes15m.Location = new System.Drawing.Point(15, 120);
            this.lblSpikes15m.Name = "lblSpikes15m";
            this.lblSpikes15m.Size = new System.Drawing.Size(107, 20);
            this.lblSpikes15m.TabIndex = 3;
            this.lblSpikes15m.Text = "За 15 мин: 0";
            // 
            // lblSpikes1h
            // 
            this.lblSpikes1h.AutoSize = true;
            this.lblSpikes1h.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblSpikes1h.Location = new System.Drawing.Point(15, 90);
            this.lblSpikes1h.Name = "lblSpikes1h";
            this.lblSpikes1h.Size = new System.Drawing.Size(87, 20);
            this.lblSpikes1h.TabIndex = 2;
            this.lblSpikes1h.Text = "За час: 0";
            // 
            // lblSpikes24h
            // 
            this.lblSpikes24h.AutoSize = true;
            this.lblSpikes24h.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblSpikes24h.Location = new System.Drawing.Point(15, 60);
            this.lblSpikes24h.Name = "lblSpikes24h";
            this.lblSpikes24h.Size = new System.Drawing.Size(119, 20);
            this.lblSpikes24h.TabIndex = 1;
            this.lblSpikes24h.Text = "За 24 часа: 0";
            // 
            // lblTotalSpikes
            // 
            this.lblTotalSpikes.AutoSize = true;
            this.lblTotalSpikes.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblTotalSpikes.Location = new System.Drawing.Point(15, 30);
            this.lblTotalSpikes.Name = "lblTotalSpikes";
            this.lblTotalSpikes.Size = new System.Drawing.Size(179, 25);
            this.lblTotalSpikes.TabIndex = 0;
            this.lblTotalSpikes.Text = "Всего спайков: 0";
            // 
            // chartSpikeFrequency
            // 
            chartArea1.Name = "ChartArea1";
            this.chartSpikeFrequency.ChartAreas.Add(chartArea1);
            this.tableLayoutPanelOverview.SetColumnSpan(this.chartSpikeFrequency, 2);
            this.chartSpikeFrequency.Dock = System.Windows.Forms.DockStyle.Fill;
            legend1.Name = "Legend1";
            this.chartSpikeFrequency.Legends.Add(legend1);
            this.chartSpikeFrequency.Location = new System.Drawing.Point(3, 366);
            this.chartSpikeFrequency.Name = "chartSpikeFrequency";
            this.chartSpikeFrequency.Size = new System.Drawing.Size(1164, 357);
            this.chartSpikeFrequency.TabIndex = 1;
            this.chartSpikeFrequency.Text = "Частота спайков";
            // 
            // chartSpikeDistribution
            // 
            chartArea2.Name = "ChartArea1";
            this.chartSpikeDistribution.ChartAreas.Add(chartArea2);
            this.chartSpikeDistribution.Dock = System.Windows.Forms.DockStyle.Fill;
            legend2.Name = "Legend1";
            this.chartSpikeDistribution.Legends.Add(legend2);
            this.chartSpikeDistribution.Location = new System.Drawing.Point(588, 3);
            this.chartSpikeDistribution.Name = "chartSpikeDistribution";
            this.chartSpikeDistribution.Size = new System.Drawing.Size(579, 357);
            this.chartSpikeDistribution.TabIndex = 2;
            this.chartSpikeDistribution.Text = "Распределение спайков";
            // 
            // tabPageDetailed
            // 
            this.tabPageDetailed.Controls.Add(this.dataGridViewSpikes);
            this.tabPageDetailed.Location = new System.Drawing.Point(4, 25);
            this.tabPageDetailed.Name = "tabPageDetailed";
            this.tabPageDetailed.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageDetailed.Size = new System.Drawing.Size(1176, 732);
            this.tabPageDetailed.TabIndex = 1;
            this.tabPageDetailed.Text = "Детальные данные";
            this.tabPageDetailed.UseVisualStyleBackColor = true;
            // 
            // dataGridViewSpikes
            // 
            this.dataGridViewSpikes.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewSpikes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewSpikes.Location = new System.Drawing.Point(3, 3);
            this.dataGridViewSpikes.Name = "dataGridViewSpikes";
            this.dataGridViewSpikes.RowHeadersWidth = 51;
            this.dataGridViewSpikes.RowTemplate.Height = 24;
            this.dataGridViewSpikes.Size = new System.Drawing.Size(1170, 726);
            this.dataGridViewSpikes.TabIndex = 0;
            // 
            // tabPageExport
            // 
            this.tabPageExport.Controls.Add(this.groupBoxExport);
            this.tabPageExport.Location = new System.Drawing.Point(4, 25);
            this.tabPageExport.Name = "tabPageExport";
            this.tabPageExport.Padding = new System.Windows.Forms.Padding(3);
            this.tabPageExport.Size = new System.Drawing.Size(1176, 732);
            this.tabPageExport.TabIndex = 2;
            this.tabPageExport.Text = "Экспорт";
            this.tabPageExport.UseVisualStyleBackColor = true;
            // 
            // groupBoxExport
            // 
            this.groupBoxExport.Controls.Add(this.btnClearData);
            this.groupBoxExport.Controls.Add(this.btnExportHTML);
            this.groupBoxExport.Controls.Add(this.btnExportJSON);
            this.groupBoxExport.Controls.Add(this.btnExportCSV);
            this.groupBoxExport.Controls.Add(this.labelExportDescription);
            this.groupBoxExport.Location = new System.Drawing.Point(20, 20);
            this.groupBoxExport.Name = "groupBoxExport";
            this.groupBoxExport.Size = new System.Drawing.Size(600, 400);
            this.groupBoxExport.TabIndex = 0;
            this.groupBoxExport.TabStop = false;
            this.groupBoxExport.Text = "Экспорт данных";
            // 
            // btnClearData
            // 
            this.btnClearData.BackColor = System.Drawing.Color.IndianRed;
            this.btnClearData.ForeColor = System.Drawing.Color.White;
            this.btnClearData.Location = new System.Drawing.Point(30, 320);
            this.btnClearData.Name = "btnClearData";
            this.btnClearData.Size = new System.Drawing.Size(150, 40);
            this.btnClearData.TabIndex = 4;
            this.btnClearData.Text = "Очистить данные";
            this.btnClearData.UseVisualStyleBackColor = false;
            this.btnClearData.Click += new System.EventHandler(this.btnClearData_Click);
            // 
            // btnExportHTML
            // 
            this.btnExportHTML.Location = new System.Drawing.Point(30, 240);
            this.btnExportHTML.Name = "btnExportHTML";
            this.btnExportHTML.Size = new System.Drawing.Size(150, 40);
            this.btnExportHTML.TabIndex = 3;
            this.btnExportHTML.Text = "Экспорт в HTML";
            this.btnExportHTML.UseVisualStyleBackColor = true;
            this.btnExportHTML.Click += new System.EventHandler(this.btnExportHTML_Click);
            // 
            // btnExportJSON
            // 
            this.btnExportJSON.Location = new System.Drawing.Point(30, 180);
            this.btnExportJSON.Name = "btnExportJSON";
            this.btnExportJSON.Size = new System.Drawing.Size(150, 40);
            this.btnExportJSON.TabIndex = 2;
            this.btnExportJSON.Text = "Экспорт в JSON";
            this.btnExportJSON.UseVisualStyleBackColor = true;
            this.btnExportJSON.Click += new System.EventHandler(this.btnExportJSON_Click);
            // 
            // btnExportCSV
            // 
            this.btnExportCSV.Location = new System.Drawing.Point(30, 120);
            this.btnExportCSV.Name = "btnExportCSV";
            this.btnExportCSV.Size = new System.Drawing.Size(150, 40);
            this.btnExportCSV.TabIndex = 1;
            this.btnExportCSV.Text = "Экспорт в CSV";
            this.btnExportCSV.UseVisualStyleBackColor = true;
            this.btnExportCSV.Click += new System.EventHandler(this.btnExportCSV_Click);
            // 
            // labelExportDescription
            // 
            this.labelExportDescription.Location = new System.Drawing.Point(30, 30);
            this.labelExportDescription.Name = "labelExportDescription";
            this.labelExportDescription.Size = new System.Drawing.Size(540, 80);
            this.labelExportDescription.TabIndex = 0;
            this.labelExportDescription.Text = "Экспортируйте данные аналитики спайков в различных форматах:\r\n\r\n• CSV - для анал" +
    "иза в Excel или других таблицах\r\n• JSON - для программной обработки\r\n• HTML - дл" +
    "я создания отчетов";
            // 
            // SpikeAnalyticsForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1184, 761);
            this.Controls.Add(this.tabControlMain);
            this.Name = "SpikeAnalyticsForm";
            this.Text = "Spike Analytics - Аналитика спайков";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SpikeAnalyticsForm_FormClosing);
            this.tabControlMain.ResumeLayout(false);
            this.tabPageOverview.ResumeLayout(false);
            this.tableLayoutPanelOverview.ResumeLayout(false);
            this.groupBoxStatistics.ResumeLayout(false);
            this.groupBoxStatistics.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.chartSpikeFrequency)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.chartSpikeDistribution)).EndInit();
            this.tabPageDetailed.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewSpikes)).EndInit();
            this.tabPageExport.ResumeLayout(false);
            this.groupBoxExport.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControlMain;
        private System.Windows.Forms.TabPage tabPageOverview;
        private System.Windows.Forms.TabPage tabPageDetailed;
        private System.Windows.Forms.TabPage tabPageExport;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelOverview;
        private System.Windows.Forms.GroupBox groupBoxStatistics;
        private System.Windows.Forms.Label lblTotalSpikes;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartSpikeFrequency;
        private System.Windows.Forms.DataVisualization.Charting.Chart chartSpikeDistribution;
        private System.Windows.Forms.DataGridView dataGridViewSpikes;
        private System.Windows.Forms.GroupBox groupBoxExport;
        private System.Windows.Forms.Button btnExportCSV;
        private System.Windows.Forms.Label labelExportDescription;
        private System.Windows.Forms.Label lblSpikes24h;
        private System.Windows.Forms.Label lblSpikes1h;
        private System.Windows.Forms.Label lblSpikes15m;
        private System.Windows.Forms.Label lblPingSpikes;
        private System.Windows.Forms.Label lblTickrateSpikes;
        private System.Windows.Forms.Label lblTicktimeSpikes;
        private System.Windows.Forms.Label lblAvgSeverity;
        private System.Windows.Forms.Label lblMaxSeverity;
        private System.Windows.Forms.Label lblSpikeRate;
        private System.Windows.Forms.Button btnExportJSON;
        private System.Windows.Forms.Button btnExportHTML;
        private System.Windows.Forms.Button btnClearData;
    }
}