using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using tickMeter.Classes;
using tickMeter.Classes.SpikeDetection;

namespace tickMeter.Forms
{
    /// <summary>
    /// Форма для детальной аналитики и экспорта данных о спайках
    /// Stage 5: Advanced Analytics and Export
    /// </summary>
    public partial class SpikeAnalyticsForm : Form
    {
        private List<SpikeAnalyticsData> _analyticsHistory;
        private object _dataLock = new object();
        private System.Windows.Forms.Timer _updateTimer;
        
        public SpikeAnalyticsForm()
        {
            InitializeComponent();
            _analyticsHistory = new List<SpikeAnalyticsData>();
            
            SetupForm();
            InitializeCharts();
            StartRealTimeUpdates();
        }
        
        private void SetupForm()
        {
            this.Text = "Spike Analytics - Advanced Data Analysis";
            this.Size = new Size(1200, 800);
            this.MinimumSize = new Size(800, 600);
            this.WindowState = FormWindowState.Maximized;
            this.Icon = this.Owner?.Icon;
        }
        
        private void InitializeCharts()
        {
            // Настройка графика частоты спайков
            SetupFrequencyChart();
            
            // Настройка графика распределения
            SetupDistributionChart();
        }
        
        private void SetupFrequencyChart()
        {
            chartSpikeFrequency.Series.Clear();
            chartSpikeFrequency.ChartAreas.Clear();
            
            ChartArea area = new ChartArea("FrequencyArea");
            area.AxisX.Title = "Time";
            area.AxisY.Title = "Spikes per Minute";
            area.AxisX.LabelStyle.Format = "HH:mm";
            area.AxisX.IntervalType = DateTimeIntervalType.Minutes; // Интервалы по минутам
            area.AxisX.Interval = 15; // Метки каждые 15 минут
            area.BackColor = Color.FromArgb(245, 245, 245);
            chartSpikeFrequency.ChartAreas.Add(area);
            
            Series series = new Series("Frequency");
            series.ChartType = SeriesChartType.Line;
            series.Color = Color.FromArgb(220, 20, 60);
            series.BorderWidth = 2;
            series.MarkerStyle = MarkerStyle.Circle;
            series.MarkerSize = 4;
            series.XValueType = ChartValueType.DateTime; // ИСПРАВЛЕНИЕ: указываем тип данных для X-оси
            chartSpikeFrequency.Series.Add(series);
            
            chartSpikeFrequency.Titles.Clear();
            chartSpikeFrequency.Titles.Add("Spike Frequency Over Time");
        }
        
        private void SetupDistributionChart()
        {
            chartSpikeDistribution.Series.Clear();
            chartSpikeDistribution.ChartAreas.Clear();
            
            ChartArea area = new ChartArea("DistributionArea");
            area.AxisX.Title = "Spike Severity";
            area.AxisY.Title = "Count";
            area.BackColor = Color.FromArgb(245, 245, 245);
            chartSpikeDistribution.ChartAreas.Add(area);
            
            Series series = new Series("Distribution");
            series.ChartType = SeriesChartType.Column;
            series.Color = Color.FromArgb(70, 130, 180);
            chartSpikeDistribution.Series.Add(series);
            
            chartSpikeDistribution.Titles.Clear();
            chartSpikeDistribution.Titles.Add("Spike Severity Distribution");
        }
        

        
        private void StartRealTimeUpdates()
        {
            _updateTimer = new System.Windows.Forms.Timer();
            _updateTimer.Interval = 5000; // Обновление каждые 5 секунд
            _updateTimer.Tick += UpdateTimer_Tick;
            _updateTimer.Start();
        }
        
        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                CollectAnalyticsData();
                UpdateCharts();
                UpdateStatistics();
                UpdateDataGrid();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating analytics: {ex.Message}");
            }
        }
        
        private void CollectAnalyticsData()
        {
            lock (_dataLock)
            {
                DateTime now = DateTime.Now;
                
                // Генерируем тестовые данные для демонстрации аналитики
                var random = new Random();
                
                // Случайно добавляем данные о спайках
                if (random.Next(0, 10) < 3) // 30% вероятность
                {
                    var metricTypes = new[] { "Ping", "Tickrate", "Ticktime" };
                    var metricType = metricTypes[random.Next(metricTypes.Length)];
                    
                    var baseValue = metricType == "Ping" ? 50.0 : 
                                   metricType == "Tickrate" ? 64.0 : 15.0;
                    
                    var value = baseValue + random.NextDouble() * baseValue * 0.5; // Скачок на 0-50%
                    var baseline = baseValue;
                    
                    _analyticsHistory.Add(new SpikeAnalyticsData
                    {
                        Timestamp = now,
                        MetricType = metricType,
                        Value = value,
                        Baseline = baseline,
                        SeverityPercent = CalculateSeverityPercent(value, baseline),
                        Duration = TimeSpan.FromMilliseconds(random.Next(100, 2000))
                    });
                }
                
                // Ограничиваем размер истории (последние 24 часа)
                DateTime cutoff = now.AddHours(-24);
                _analyticsHistory.RemoveAll(data => data.Timestamp < cutoff);
            }
        }
        
        private double CalculateSeverityPercent(double value, double baseline)
        {
            if (baseline <= 0) return 0;
            return Math.Max(0, ((value - baseline) / baseline) * 100.0);
        }
        
        private void UpdateCharts()
        {
            UpdateFrequencyChart();
            UpdateDistributionChart();
        }
        
        private void UpdateFrequencyChart()
        {
            lock (_dataLock)
            {
                var series = chartSpikeFrequency.Series["Frequency"];
                series.Points.Clear();
                
                // Группируем спайки по минутам за последние 2 часа
                DateTime now = DateTime.Now;
                DateTime startTime = now.AddHours(-2);
                
                var minuteGroups = _analyticsHistory
                    .Where(data => data.Timestamp >= startTime)
                    .GroupBy(data => new DateTime(data.Timestamp.Year, data.Timestamp.Month, 
                                                  data.Timestamp.Day, data.Timestamp.Hour, 
                                                  data.Timestamp.Minute, 0))
                    .OrderBy(g => g.Key)
                    .ToList();
                
                foreach (var group in minuteGroups)
                {
                    series.Points.AddXY(group.Key, group.Count());
                }
                
                chartSpikeFrequency.Invalidate();
            }
        }
        
        private void UpdateDistributionChart()
        {
            lock (_dataLock)
            {
                var series = chartSpikeDistribution.Series["Distribution"];
                series.Points.Clear();
                
                // Группируем по уровням серьезности
                var severityGroups = _analyticsHistory
                    .Where(data => data.Timestamp >= DateTime.Now.AddHours(-1))
                    .GroupBy(data => GetSeverityLevel(data.SeverityPercent))
                    .OrderBy(g => g.Key)
                    .ToList();
                
                foreach (var group in severityGroups)
                {
                    series.Points.AddXY(group.Key, group.Count());
                }
                
                chartSpikeDistribution.Invalidate();
            }
        }
        
        private string GetSeverityLevel(double severityPercent)
        {
            if (severityPercent < 10) return "Low (0-10%)";
            if (severityPercent < 25) return "Medium (10-25%)";
            if (severityPercent < 50) return "High (25-50%)";
            return "Critical (50%+)";
        }
        

        
        private void UpdateStatistics()
        {
            lock (_dataLock)
            {
                DateTime now = DateTime.Now;
                
                // Статистика за последний час
                var hourData = _analyticsHistory.Where(data => data.Timestamp >= now.AddHours(-1)).ToList();
                
                // Статистика за последние 24 часа
                var dayData = _analyticsHistory.Where(data => data.Timestamp >= now.AddHours(-24)).ToList();
                
                // Обновляем метки статистики
                lblTotalSpikes.Text = $"Total Spikes (24h): {dayData.Count}";
                lblSpikeRate.Text = $"Spikes/Hour: {(dayData.Count > 0 ? dayData.Count / 24.0 : 0):F1}";
                
                if (hourData.Any())
                {
                    var avgSeverity = hourData.Average(data => data.SeverityPercent);
                    var maxSeverity = hourData.Max(data => data.SeverityPercent);
                    
                    lblAvgSeverity.Text = $"Avg Severity (1h): {avgSeverity:F1}%";
                    lblMaxSeverity.Text = $"Max Severity (1h): {maxSeverity:F1}%";
                }
                else
                {
                    lblAvgSeverity.Text = "Avg Severity (1h): 0.0%";
                    lblMaxSeverity.Text = "Max Severity (1h): 0.0%";
                }
                
                // Статистика по типам метрик
                var pingCount = hourData.Count(data => data.MetricType == "Ping");
                var tickrateCount = hourData.Count(data => data.MetricType == "Tickrate");
                var ticktimeCount = hourData.Count(data => data.MetricType == "Ticktime");
                
                lblPingSpikes.Text = $"Ping Spikes (1h): {pingCount}";
                lblTickrateSpikes.Text = $"Tickrate Spikes (1h): {tickrateCount}";
                lblTicktimeSpikes.Text = $"Ticktime Spikes (1h): {ticktimeCount}";
            }
        }
        
        private void UpdateDataGrid()
        {
            lock (_dataLock)
            {
                // Показываем последние 100 спайков (совместимо с .NET Framework)
                var recentSpikes = _analyticsHistory.Count > 100 
                    ? _analyticsHistory.Skip(_analyticsHistory.Count - 100).Reverse().ToList()
                    : _analyticsHistory.AsEnumerable().Reverse().ToList();
                
                dataGridViewSpikes.DataSource = null;
                dataGridViewSpikes.DataSource = recentSpikes;
                dataGridViewSpikes.Refresh();
            }
        }
        
        private void btnExport_Click(object sender, EventArgs e)
        {
            try
            {
                ExportToCSV();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting data: {ex.Message}", "Export Error", 
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ExportToCSV()
        {
            using (SaveFileDialog dialog = new SaveFileDialog())
            {
                dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
                dialog.DefaultExt = "csv";
                dialog.FileName = $"spike_analytics_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    ExportDataToFile(dialog.FileName);
                    MessageBox.Show($"Data exported successfully to:\n{dialog.FileName}", 
                                   "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }
        
        private void ExportDataToFile(string fileName)
        {
            lock (_dataLock)
            {
                using (StreamWriter writer = new StreamWriter(fileName, false, Encoding.UTF8))
                {
                    // Заголовки
                    writer.WriteLine("Timestamp,MetricType,Value,Baseline,SeverityPercent,Duration");
                    
                    // Экспортируем все доступные данные
                    var filteredData = _analyticsHistory
                        .OrderBy(data => data.Timestamp)
                        .ToList();
                    
                    // Данные
                    foreach (var data in filteredData)
                    {
                        writer.WriteLine($"{data.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                                       $"{data.MetricType}," +
                                       $"{data.Value.ToString(CultureInfo.InvariantCulture)}," +
                                       $"{data.Baseline.ToString(CultureInfo.InvariantCulture)}," +
                                       $"{data.SeverityPercent.ToString(CultureInfo.InvariantCulture)}," +
                                       $"{data.Duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}");
                    }
                }
            }
        }
        
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            try
            {
                CollectAnalyticsData();
                UpdateCharts();
                UpdateStatistics();
                UpdateDataGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing data: {ex.Message}", "Refresh Error", 
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void btnClearData_Click(object sender, EventArgs e)
        {
            try
            {
                lock (_dataLock)
                {
                    _analyticsHistory.Clear();
                }
                UpdateCharts();
                UpdateStatistics();
                UpdateDataGrid();
                MessageBox.Show("Analytics data cleared successfully.", "Clear Complete", 
                               MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error clearing data: {ex.Message}", "Clear Error", 
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void btnExportHTML_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*";
                    dialog.DefaultExt = "html";
                    dialog.FileName = $"spike_analytics_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.html";
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        ExportToHTML(dialog.FileName);
                        MessageBox.Show($"Data exported successfully to:\n{dialog.FileName}", 
                                       "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting HTML: {ex.Message}", "Export Error", 
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void btnExportJSON_Click(object sender, EventArgs e)
        {
            try
            {
                using (SaveFileDialog dialog = new SaveFileDialog())
                {
                    dialog.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                    dialog.DefaultExt = "json";
                    dialog.FileName = $"spike_analytics_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.json";
                    
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        ExportToJSON(dialog.FileName);
                        MessageBox.Show($"Data exported successfully to:\n{dialog.FileName}", 
                                       "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error exporting JSON: {ex.Message}", "Export Error", 
                               MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void btnExportCSV_Click(object sender, EventArgs e)
        {
            btnExport_Click(sender, e); // Переиспользуем существующий метод
        }
        
        private void ExportToHTML(string fileName)
        {
            lock (_dataLock)
            {
                using (StreamWriter writer = new StreamWriter(fileName, false, Encoding.UTF8))
                {
                    writer.WriteLine("<!DOCTYPE html>");
                    writer.WriteLine("<html><head><title>Spike Analytics Report</title>");
                    writer.WriteLine("<style>table{border-collapse:collapse;width:100%;}th,td{border:1px solid #ddd;padding:8px;text-align:left;}th{background-color:#f2f2f2;}</style>");
                    writer.WriteLine("</head><body>");
                    writer.WriteLine($"<h1>Spike Analytics Report - {DateTime.Now:yyyy-MM-dd HH:mm:ss}</h1>");
                    writer.WriteLine("<table><tr><th>Timestamp</th><th>Metric Type</th><th>Value</th><th>Baseline</th><th>Severity %</th><th>Duration (ms)</th></tr>");
                    
                    var orderedData = _analyticsHistory.OrderBy(data => data.Timestamp).ToList();
                    foreach (var data in orderedData)
                    {
                        writer.WriteLine($"<tr><td>{data.FormattedTimestamp}</td><td>{data.MetricType}</td><td>{data.FormattedValue}</td><td>{data.FormattedBaseline}</td><td>{data.FormattedSeverity}</td><td>{data.FormattedDuration}</td></tr>");
                    }
                    
                    writer.WriteLine("</table></body></html>");
                }
            }
        }
        
        private void ExportToJSON(string fileName)
        {
            lock (_dataLock)
            {
                using (StreamWriter writer = new StreamWriter(fileName, false, Encoding.UTF8))
                {
                    writer.WriteLine("{");
                    writer.WriteLine($"  \"exportTime\": \"{DateTime.Now:yyyy-MM-dd HH:mm:ss}\",");
                    writer.WriteLine($"  \"totalSpikes\": {_analyticsHistory.Count},");
                    writer.WriteLine("  \"spikes\": [");
                    
                    var orderedData = _analyticsHistory.OrderBy(data => data.Timestamp).ToList();
                    for (int i = 0; i < orderedData.Count; i++)
                    {
                        var data = orderedData[i];
                        writer.WriteLine("    {");
                        writer.WriteLine($"      \"timestamp\": \"{data.FormattedTimestamp}\",");
                        writer.WriteLine($"      \"metricType\": \"{data.MetricType}\",");
                        writer.WriteLine($"      \"value\": {data.Value.ToString(CultureInfo.InvariantCulture)},");
                        writer.WriteLine($"      \"baseline\": {data.Baseline.ToString(CultureInfo.InvariantCulture)},");
                        writer.WriteLine($"      \"severityPercent\": {data.SeverityPercent.ToString(CultureInfo.InvariantCulture)},");
                        writer.WriteLine($"      \"durationMs\": {data.Duration.TotalMilliseconds.ToString(CultureInfo.InvariantCulture)}");
                        writer.WriteLine(i < orderedData.Count - 1 ? "    }," : "    }");
                    }
                    
                    writer.WriteLine("  ]");
                    writer.WriteLine("}");
                }
            }
        }
        
        private void SpikeAnalyticsForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            _updateTimer?.Stop();
            _updateTimer?.Dispose();
        }
    }
    
    /// <summary>
    /// Структура данных для аналитики спайков
    /// </summary>
    public class SpikeAnalyticsData
    {
        public DateTime Timestamp { get; set; }
        public string MetricType { get; set; }
        public double Value { get; set; }
        public double Baseline { get; set; }
        public double SeverityPercent { get; set; }
        public TimeSpan Duration { get; set; }
        
        public string FormattedTimestamp => Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
        public string FormattedValue => Value.ToString("F2");
        public string FormattedBaseline => Baseline.ToString("F2");
        public string FormattedSeverity => $"{SeverityPercent:F1}%";
        public string FormattedDuration => $"{Duration.TotalMilliseconds:F0}ms";
    }
}