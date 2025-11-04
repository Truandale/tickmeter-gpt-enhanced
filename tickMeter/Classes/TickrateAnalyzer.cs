using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace tickMeter.Classes
{
    /// <summary>
    /// Анализирует расхождения между Windows статистикой и PCAP данными
    /// </summary>
    public static class TickrateAnalyzer
    {
        private static List<TickrateComparison> comparisons = new List<TickrateComparison>();
        private static readonly object lockObject = new object();

        public struct TickrateComparison
        {
            public DateTime Timestamp;
            public string ProcessName;
            
            // Windows данные
            public double WindowsTickrate;
            public int WindowsTickCount;
            public double WindowsMeasurementPeriod;
            
            // PCAP данные
            public double PcapTickrate;
            public int PcapTickCount;
            public double PcapMeasurementPeriod;
            
            // Анализ
            public double Difference;
            public double RelativeDifference;
            public string Analysis;
        }

        public static void AddComparison(string processName, 
            double windowsTickrate, int windowsTickCount, double windowsMeasurementPeriod,
            double pcapTickrate, int pcapTickCount, double pcapMeasurementPeriod)
        {
            var comp = new TickrateComparison
            {
                Timestamp = DateTime.Now,
                ProcessName = processName,
                WindowsTickrate = windowsTickrate,
                WindowsTickCount = windowsTickCount,
                WindowsMeasurementPeriod = windowsMeasurementPeriod,
                PcapTickrate = pcapTickrate,
                PcapTickCount = pcapTickCount,
                PcapMeasurementPeriod = pcapMeasurementPeriod,
                Difference = windowsTickrate - pcapTickrate,
                RelativeDifference = windowsTickrate > 0 ? ((windowsTickrate - pcapTickrate) / windowsTickrate * 100) : 0
            };

            // Детальный анализ причин расхождений
            comp.Analysis = AnalyzeDiscrepancy(comp);
            
            lock (lockObject)
            {
                comparisons.Add(comp);
            }

            // Логируем каждое сравнение с детальным анализом
            DebugLogger.log($"[TICKRATE-ANALYSIS] Process: {comp.ProcessName}");
            DebugLogger.log($"[TICKRATE-ANALYSIS] Windows: {comp.WindowsTickrate:F1} Hz ({comp.WindowsTickCount} ticks / {comp.WindowsMeasurementPeriod:F2}s)");
            DebugLogger.log($"[TICKRATE-ANALYSIS] PCAP: {comp.PcapTickrate:F1} Hz ({comp.PcapTickCount} ticks / {comp.PcapMeasurementPeriod:F2}s)");
            DebugLogger.log($"[TICKRATE-ANALYSIS] Difference: {comp.Difference:F1} Hz ({comp.RelativeDifference:F1}%)");
            DebugLogger.log($"[TICKRATE-ANALYSIS] Period ratio: Windows/PCAP = {comp.WindowsMeasurementPeriod / comp.PcapMeasurementPeriod:F3}");
            
            // Детальный анализ при больших расхождениях
            if (Math.Abs(comp.RelativeDifference) > 10)
            {
                DebugLogger.log($"[TICKRATE-ANALYSIS] WARNING: Large discrepancy detected! Investigating...");
                DebugLogger.log($"[TICKRATE-ANALYSIS] Analysis: {comp.Analysis}");
            }
        }

        private static string AnalyzeDiscrepancy(TickrateComparison comp)
        {
            var analyses = new List<string>();
            
            // Анализ временных окон
            double periodRatio = comp.WindowsMeasurementPeriod / comp.PcapMeasurementPeriod;
            if (periodRatio > 1.5)
            {
                analyses.Add($"PCAP uses much longer measurement period ({periodRatio:F1}x longer)");
                DebugLogger.log($"[TICKRATE-ANALYSIS] CAUSE: PCAP uses much longer measurement period ({periodRatio:F1}x longer)");
            }
            else if (periodRatio < 0.67)
            {
                analyses.Add($"PCAP uses much shorter measurement period ({1/periodRatio:F1}x shorter)");
                DebugLogger.log($"[TICKRATE-ANALYSIS] CAUSE: PCAP uses much shorter measurement period ({1/periodRatio:F1}x shorter)");
            }
            
            // Анализ количества пакетов
            double tickRatio = (double)comp.WindowsTickCount / comp.PcapTickCount;
            if (tickRatio > 1.5)
            {
                analyses.Add($"PCAP sees significantly fewer packets - possible VPN filtering");
                DebugLogger.log($"[TICKRATE-ANALYSIS] CAUSE: PCAP sees significantly fewer packets - possible VPN filtering");
            }
            else if (tickRatio < 0.67)
            {
                analyses.Add($"PCAP sees more packets - possible additional traffic or different filtering");
                DebugLogger.log($"[TICKRATE-ANALYSIS] CAUSE: PCAP sees more packets - possible additional traffic or different filtering");
            }
            
            // Нормализованные измерения (приведение к одинаковому временному окну)
            double windowsNormalized = comp.WindowsTickCount / comp.WindowsMeasurementPeriod;
            double pcapNormalized = comp.PcapTickCount / comp.PcapMeasurementPeriod;
            
            DebugLogger.log($"[TICKRATE-ANALYSIS] Normalized rates: Windows={windowsNormalized:F2}, PCAP={pcapNormalized:F2}");
            
            return analyses.Count > 0 ? string.Join("; ", analyses) : "No significant methodological differences detected";
        }

        public static void OutputStatistics()
        {
            lock (lockObject)
            {
                if (comparisons.Count < 5) return;
                
                var recent = comparisons.Skip(Math.Max(0, comparisons.Count - 20)).ToList();
                
                var avgWindowsTickrate = recent.Average(c => c.WindowsTickrate);
                var avgPcapTickrate = recent.Average(c => c.PcapTickrate);
                var avgDifference = recent.Average(c => c.Difference);
                var avgRelativeDifference = recent.Average(c => c.RelativeDifference);
                
                DebugLogger.log("=== TICKRATE ANALYSIS STATISTICS (last 20 measurements) ===");
                DebugLogger.log($"Average Windows Tickrate: {avgWindowsTickrate:F2} Hz");
                DebugLogger.log($"Average PCAP Tickrate: {avgPcapTickrate:F2} Hz");
                DebugLogger.log($"Average Difference: {avgDifference:F2} Hz ({avgRelativeDifference:F1}%)");
                
                var largeDiscrepancies = recent.Where(c => Math.Abs(c.RelativeDifference) > 15).ToList();
                if (largeDiscrepancies.Count > 0)
                {
                    DebugLogger.log($"Large discrepancies detected: {largeDiscrepancies.Count}/{recent.Count} measurements");
                    foreach (var disc in largeDiscrepancies)
                    {
                        DebugLogger.log($"  {disc.Timestamp:HH:mm:ss} - {disc.ProcessName}: Win={disc.WindowsTickrate:F1} vs PCAP={disc.PcapTickrate:F1} ({disc.RelativeDifference:F1}%)");
                    }
                }
                
                DebugLogger.log("=== END STATISTICS ===");
            }
        }

        public static void ExportToCSV(string filename = null)
        {
            lock (lockObject)
            {
                if (comparisons.Count == 0) return;
                
                if (string.IsNullOrEmpty(filename))
                {
                    filename = $"tickrate_analysis_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                }
                
                try
                {
                    using (var writer = new StreamWriter(filename))
                    {
                        // Заголовки
                        writer.WriteLine("Timestamp,ProcessName,WindowsTickrate,WindowsTickCount,WindowsMeasurementPeriod,PcapTickrate,PcapTickCount,PcapMeasurementPeriod,Difference,RelativeDifference,Analysis");
                        
                        // Данные
                        foreach (var comp in comparisons)
                        {
                            writer.WriteLine($"{comp.Timestamp:yyyy-MM-dd HH:mm:ss.fff},{comp.ProcessName},{comp.WindowsTickrate:F2},{comp.WindowsTickCount},{comp.WindowsMeasurementPeriod:F3},{comp.PcapTickrate:F2},{comp.PcapTickCount},{comp.PcapMeasurementPeriod:F3},{comp.Difference:F2},{comp.RelativeDifference:F1},\"{comp.Analysis}\"");
                        }
                    }
                    
                    DebugLogger.log($"[TICKRATE-ANALYSIS] Data exported to {filename} ({comparisons.Count} records)");
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[TICKRATE-ANALYSIS] Error exporting to CSV: {ex.Message}");
                }
            }
        }

        public static void ClearData()
        {
            lock (lockObject)
            {
                comparisons.Clear();
                DebugLogger.log("[TICKRATE-ANALYSIS] Data cleared");
            }
        }

        public static int GetComparisonCount()
        {
            lock (lockObject)
            {
                return comparisons.Count;
            }
        }
    }
}