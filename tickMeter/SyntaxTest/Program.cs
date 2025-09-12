// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;

namespace SyntaxTest
{
    // Тест синтаксиса наших добавлений из SettingsForm
    public class SettingsFormTest
    {
        private CheckBox captureAllAdaptersCheckbox;
        
        public void InitAllAdaptersCheckbox()
        {
            captureAllAdaptersCheckbox = new CheckBox();
            captureAllAdaptersCheckbox.AutoSize = true;
            captureAllAdaptersCheckbox.Text = "Захватывать со всех адаптеров";
            
            try
            {
                var p = new Point(10, 10);
                captureAllAdaptersCheckbox.Location = new Point(p.X, p.Y + 30);
            } 
            catch 
            { 
                captureAllAdaptersCheckbox.Location = new Point(10, 250);
            }

            bool enabled = true;
            captureAllAdaptersCheckbox.Checked = enabled;
            
            captureAllAdaptersCheckbox.CheckedChanged += (s, e) =>
            {
                Console.WriteLine("Setting changed: " + captureAllAdaptersCheckbox.Checked);
            };
        }
    }
    
    // Тест синтаксиса наших добавлений из GUI
    public class GUITest
    {
        private readonly List<object> _allSelectedAdapters = new List<object>();
        private readonly List<BackgroundWorker> _pcapWorkers = new List<BackgroundWorker>();
        private readonly Dictionary<ulong, long> _dedup = new Dictionary<ulong, long>(capacity: 8192);
        private readonly Stopwatch _dedupSw = Stopwatch.StartNew();
        private readonly object _dedupLock = new object();
        
        private bool IsDuplicate(byte[] bytes)
        {
            if (_allSelectedAdapters.Count == 0) return false;
            if (bytes == null) return false;
            int len = Math.Min(64, bytes.Length);
            ulong h = 1469598103934665603UL;
            for (int i = 0; i < len; i++) h = (h ^ bytes[i]) * 1099511628211UL;
            h ^= (ulong)bytes.Length;
            long now = _dedupSw.ElapsedMilliseconds;
            lock (_dedupLock)
            {
                if (_dedup.TryGetValue(h, out var ts) && now - ts < 3) return true;
                _dedup[h] = now;
                if (_dedup.Count > 20000)
                {
                    foreach (var key in _dedup.Where(kv => now - kv.Value > 250).Select(kv => kv.Key).ToList())
                        _dedup.Remove(key);
                }
            }
            return false;
        }
    }
    
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Syntax test passed!");
            
            var settingsTest = new SettingsFormTest();
            settingsTest.InitAllAdaptersCheckbox();
            
            var guiTest = new GUITest();
            var testBytes = new byte[] { 1, 2, 3, 4, 5 };
            bool isDup = guiTest.IsDuplicate(testBytes);
            
            Console.WriteLine($"Deduplication test: {isDup}");
        }
    }
}
