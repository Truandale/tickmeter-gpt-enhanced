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

        // Color Zone Profile management
        public ColorZoneProfile GetColorZoneProfile()
        {
            string profileName = GetOption("color_zone_profile", "Medium", "ZONES");
            return ColorZoneProfile.GetProfile(profileName);
        }

        public void SetColorZoneProfile(string profileName)
        {
            SetOption("color_zone_profile", profileName, "ZONES");
        }

        public void SetCustomColorZones(float pingGreen, float pingYellow, float tickrateGreen, float tickrateYellow, float ticktimeGreen, float ticktimeYellow)
        {
            SetOption("ping_green_threshold", pingGreen.ToString(CultureInfo.InvariantCulture), "ZONES");
            SetOption("ping_yellow_threshold", pingYellow.ToString(CultureInfo.InvariantCulture), "ZONES");
            SetOption("tickrate_green_ratio", tickrateGreen.ToString(CultureInfo.InvariantCulture), "ZONES");
            SetOption("tickrate_yellow_ratio", tickrateYellow.ToString(CultureInfo.InvariantCulture), "ZONES");
            SetOption("ticktime_green_ratio", ticktimeGreen.ToString(CultureInfo.InvariantCulture), "ZONES");
            SetOption("ticktime_yellow_ratio", ticktimeYellow.ToString(CultureInfo.InvariantCulture), "ZONES");
            SetColorZoneProfile("Custom");
        }
    }

    // Color Zone Profile system based on ChatGPT recommendations
    public class ColorZoneProfile
    {
        public string Name { get; set; }
        public float PingGreenMs { get; set; }
        public float PingYellowMs { get; set; }
        public float TickrateGreenRatio { get; set; }
        public float TickrateYellowRatio { get; set; }
        public float TicktimeGreenRatio { get; set; }
        public float TicktimeYellowRatio { get; set; }

        public static ColorZoneProfile GetProfile(string name)
        {
            switch (name.ToLower())
            {
                case "very low":
                case "verylow":
                    return new ColorZoneProfile
                    {
                        Name = "Very Low",
                        PingGreenMs = 50f,          // 0-50ms = зеленый (идеально для VPN gaming)
                        PingYellowMs = 150f,        // 50-150ms = желтый (терпимо для VPN)
                        TickrateGreenRatio = 0.95f, // Очень мягкие требования к серверу
                        TickrateYellowRatio = 0.90f,
                        TicktimeGreenRatio = 0.80f, // Толерантность к медленной обработке
                        TicktimeYellowRatio = 1.20f  // Даже +20% от целевого времени = желтый
                    };
                case "low":
                    return new ColorZoneProfile
                    {
                        Name = "Low",
                        PingGreenMs = 55f,
                        PingYellowMs = 100f,
                        TickrateGreenRatio = 0.97f,
                        TickrateYellowRatio = 0.93f,
                        TicktimeGreenRatio = 0.70f,
                        TicktimeYellowRatio = 0.95f
                    };
                case "high":
                    return new ColorZoneProfile
                    {
                        Name = "High",
                        PingGreenMs = 30f,
                        PingYellowMs = 60f,
                        TickrateGreenRatio = 0.99f,
                        TickrateYellowRatio = 0.97f,
                        TicktimeGreenRatio = 0.50f,
                        TicktimeYellowRatio = 0.85f
                    };
                case "custom":
                    return LoadCustomProfile();
                default: // Medium
                    return new ColorZoneProfile
                    {
                        Name = "Medium",
                        PingGreenMs = 40f,
                        PingYellowMs = 80f,
                        TickrateGreenRatio = 0.98f,
                        TickrateYellowRatio = 0.95f,
                        TicktimeGreenRatio = 0.60f,
                        TicktimeYellowRatio = 0.90f
                    };
            }
        }

        private static ColorZoneProfile LoadCustomProfile()
        {
            // Return Medium profile as fallback - custom settings will be handled in the main app
            return new ColorZoneProfile
            {
                Name = "Custom",
                PingGreenMs = 40f,
                PingYellowMs = 80f,
                TickrateGreenRatio = 0.98f,
                TickrateYellowRatio = 0.95f,
                TicktimeGreenRatio = 0.60f,
                TicktimeYellowRatio = 0.90f
            };
        }

        public static string[] GetProfileNames()
        {
            return new string[] { "Very Low", "Low", "Medium", "High", "Custom" };
        }
    }
}
