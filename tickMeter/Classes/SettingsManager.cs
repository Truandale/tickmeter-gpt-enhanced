using System;
using System.IO;
using System.Windows.Forms;
using IniParser;
using IniParser.Model;
using System.Globalization;

namespace tickMeter
{
    public class SettingsManager
    {
        FileIniDataParser parser;
        IniData data;

        public SettingsManager()
        {
            parser = new FileIniDataParser();
            if (!File.Exists("settings.ini"))
            {
                File.WriteAllText("settings.ini", "[SETTINGS]"+Environment.NewLine);
            }
            data = parser.ReadFile("settings.ini");
        }

        public int GetIntOption(string optionName, int defaultValue)
        {
            return GetIntOption(optionName, "SETTINGS", defaultValue);
        }

        public int GetIntOption(string optionName, string scope = "SETTINGS", int defaultValue = 0)
        {
            String rawValue = GetOption(optionName, scope);
            int val = defaultValue;
            try
            {
                val = int.Parse(rawValue);
            } catch (FormatException) {
            }
            return val;
        }

        public string GetOption(string optionName,string scope = "SETTINGS")
        {
            if (data[scope] != null)
            {
                if (data[scope][optionName] != null)
                {
                    return data[scope][optionName];
                }
            }
            return "";
        }

        public string GetOption(string optionName, string defaultValue, string scope = "SETTINGS")
        {

            if (data[scope] != null && data[scope][optionName] != null)
            {
                return data[scope][optionName];
            }
            return defaultValue;
        }

        public void SetOption(string optionName, string value, string scope = "SETTINGS")
        {
            if (data[scope] == null)
            {
                data.Sections.AddSection(scope);
            }
            data[scope][optionName] = value;
            SaveConfig(); // Автоматически сохраняем изменения
        }

        // Дополнительные методы для универсальности
        public bool GetBool(string optionName, bool defaultValue, string scope = "SETTINGS")
        {
            string value = GetOption(optionName, scope);
            if (string.IsNullOrEmpty(value))
                return defaultValue;
            
            return value.ToLower() == "true" || value == "1";
        }
        
        public int GetInt(string optionName, int defaultValue, string scope = "SETTINGS")
        {
            return GetIntOption(optionName, scope, defaultValue);
        }
        
        public string GetString(string optionName, string defaultValue, string scope = "SETTINGS")
        {
            return GetOption(optionName, defaultValue, scope);
        }
        
        public double GetDouble(string optionName, double defaultValue, string scope = "SETTINGS")
        {
            string value = GetOption(optionName, scope);
            if (string.IsNullOrEmpty(value))
                return defaultValue;
            
            if (TryParseInvariantDouble(value.Trim(), out double result))
                return result;
            
            return defaultValue;
        }
        
        public float GetFloat(string optionName, float defaultValue, string scope = "SETTINGS")
        {
            string value = GetOption(optionName, scope);
            if (string.IsNullOrEmpty(value))
                return defaultValue;
            
            if (TryParseInvariantFloat(value.Trim(), out float result))
                return result;
            
            return defaultValue;
        }
        
        // Cached InvariantCulture for micro-optimization
        private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        
        /// <summary>
        /// Helper method for invariant culture float parsing with NaN/Infinity protection
        /// </summary>
        public static bool TryParseInvariantFloat(string s, out float v)
        {
            if (float.TryParse(s, NumberStyles.Float, Inv, out v))
            {
                return !float.IsNaN(v) && !float.IsInfinity(v);
            }
            v = 0f;
            return false;
        }
        
        /// <summary>
        /// Helper method for invariant culture double parsing with NaN/Infinity protection
        /// </summary>
        public static bool TryParseInvariantDouble(string s, out double v)
        {
            if (double.TryParse(s, NumberStyles.Float, Inv, out v))
            {
                return !double.IsNaN(v) && !double.IsInfinity(v);
            }
            v = 0.0;
            return false;
        }
        
        /// <summary>
        /// Unified helper for parsing percentages (handles "1.2%", " 1,2 % ", etc.)
        /// </summary>
        public static bool TryParsePercent(string s, out float v)
        {
            if (string.IsNullOrWhiteSpace(s)) 
            { 
                v = 0f; 
                return false; 
            }
            
            s = s.Trim();
            if (s.EndsWith("%")) 
                s = s.TrimEnd('%', ' ');
            
            return TryParseInvariantFloat(s, out v);
        }
        
        /// <summary>
        /// Helper method for invariant culture formatting
        /// </summary>
        public static string ToInvariantString(float value) =>
            value.ToString(Inv);
        
        /// <summary>
        /// Helper method for invariant culture formatting
        /// </summary>
        public static string ToInvariantString(double value) =>
            value.ToString(Inv);
        
        /// <summary>
        /// Helper method for invariant culture formatting (int overload)
        /// </summary>
        public static string ToInvariantString(int value) =>
            value.ToString(Inv);

        public void SaveConfig()
        {
            try { 
                parser.WriteFile("settings.ini", data);
            } catch(Exception) { MessageBox.Show("Не могу сохранить настройки. Не хватает прав на запись."); }
        }

        public void ReloadConfig()
        {
            try 
            {
                data = parser.ReadFile("settings.ini");
            } 
            catch(Exception) 
            { 
                MessageBox.Show("Не могу загрузить настройки."); 
            }
        }
    }
}
