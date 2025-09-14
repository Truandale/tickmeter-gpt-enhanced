using System;
using System.IO;
using System.Windows.Forms;
using IniParser;
using IniParser.Model;

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

            if (data[scope] != null && data[scope][optionName] != null)
            {
                return data[scope][optionName];
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
            if (data[scope] == null) return;
            data[scope][optionName] = value;
            
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

        public void SaveConfig()
        {
            try { 
                parser.WriteFile("settings.ini", data);
            } catch(Exception) { MessageBox.Show("Не могу сохранить настройки. Не хватает прав на запись."); }
}
    }
}
