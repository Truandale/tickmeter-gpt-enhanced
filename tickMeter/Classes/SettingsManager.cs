using System;
using System.IO;
using System.Windows.Forms;
using IniParser;
using IniParser.Model;
using System.Globalization;
using tickMeter.Classes;

namespace tickMeter
{
    public class SettingsManager
    {
        FileIniDataParser parser;
        IniData data;
        private readonly object _lock = new object(); // Thread-safety для всех операций
        private volatile bool _batchMode = false; // Режим пакетного сохранения (не сохраняем после каждого SetOption)
        private int _batchDepth = 0; // Счетчик вложенности BeginBatchUpdate/EndBatchUpdate (защита от вложенных вызов)

        /// <summary>
        /// Проверяет наличие settings.ini и создает его с оптимальными настройками если файл отсутствует
        /// </summary>
        public static void EnsureSettingsFileExists()
        {
            string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");
            
            if (File.Exists(settingsPath))
            {
                DebugLogger.log("[SettingsManager] settings.ini найден, продолжаем запуск");
                return;
            }
            
            DebugLogger.log("[SettingsManager] settings.ini не найден! Создаем файл с оптимальными настройками...");
            
            try
            {
                // Создаем временный SettingsManager для записи оптимальных настроек
                var tempManager = new SettingsManager();
                tempManager.CreateOptimalSettings();
                
                DebugLogger.log("[SettingsManager] settings.ini успешно создан с оптимальными настройками!");
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[SettingsManager] ОШИБКА при создании settings.ini: {ex.Message}");
                MessageBox.Show(
                    $"Не удалось создать файл настроек settings.ini!\n\nОшибка: {ex.Message}\n\nПрограмма будет закрыта.",
                    "Критическая ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error
                );
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Статический метод для чтения настройки напрямую из файла (до инициализации GUI)
        /// </summary>
        public static string ReadOptionDirect(string optionName, string scope = "SETTINGS")
        {
            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");
                if (!File.Exists(settingsPath))
                {
                    return "";
                }

                var parser = new FileIniDataParser();
                var data = parser.ReadFile(settingsPath);
                
                if (data[scope] != null && data[scope][optionName] != null)
                {
                    return data[scope][optionName];
                }
                return "";
            }
            catch
            {
                return "";
            }
        }

        public SettingsManager()
        {
            parser = new FileIniDataParser();
            if (!File.Exists("settings.ini"))
            {
                File.WriteAllText("settings.ini", "[SETTINGS]"+Environment.NewLine);
            }
            data = parser.ReadFile("settings.ini");
        }
        
        /// <summary>
        /// Создает settings.ini с оптимальными настройками (идентично кнопке "Сброс к оптимальным")
        /// </summary>
        private void CreateOptimalSettings()
        {
            // === ОСНОВНЫЕ [SETTINGS] НАСТРОЙКИ ===
            SetOption("rtss", "True", "SETTINGS");
            SetOption("autodetect", "True", "SETTINGS");
            SetOption("capture_all_adapters", "True", "SETTINGS");
            SetOption("chart", "True", "SETTINGS");
            SetOption("ip", "True", "SETTINGS");
            SetOption("tickrate", "True", "SETTINGS");
            SetOption("ticktime", "True", "SETTINGS");
            SetOption("ping_chart", "True", "SETTINGS");
            SetOption("ping", "True", "SETTINGS");
            SetOption("traffic", "True", "SETTINGS");
            SetOption("session_time", "True", "SETTINGS");
            SetOption("show_packet_drops", "True", "SETTINGS");
            SetOption("ping_interval", "1000", "SETTINGS");
            SetOption("ping_ports", "", "SETTINGS");
            SetOption("data_send", "False", "SETTINGS");
            SetOption("run_minimized", "True", "SETTINGS");
            SetOption("manual_ip_unlocked", "False", "SETTINGS");
            SetOption("ping_bind_to_interface", "True", "SETTINGS");
            SetOption("ping_tcp_prefer", "True", "SETTINGS");
            SetOption("ping_fallback_icmp", "True", "SETTINGS");
            SetOption("ping_target_active_only", "True", "SETTINGS");
            SetOption("tickrate_smoothing", "True", "SETTINGS");
            SetOption("dedup_multi_nic", "True", "SETTINGS");
            SetOption("enable_ipv6", "True", "SETTINGS");
            SetOption("ignore_virtual_adapters", "True", "SETTINGS");
            SetOption("rtss_only_active", "True", "SETTINGS");
            SetOption("stun_enable", "True", "SETTINGS");
            SetOption("network_quality_overlay", "True", "SETTINGS");
            SetOption("ui_refresh_hidden", "True", "SETTINGS");
            SetOption("color_label", "636BDA", "SETTINGS");
            SetOption("color_bad", "FF0000", "SETTINGS");
            SetOption("color_mid", "FF8040", "SETTINGS");
            SetOption("color_good", "00FF00", "SETTINGS");
            SetOption("color_chart", "FF0080", "SETTINGS");
            SetOption("last_selected_adapter", "", "SETTINGS");
            SetOption("local_ip", "", "SETTINGS");
            SetOption("auto_start_monitoring", "True", "SETTINGS"); // Автозапуск мониторинга при старте программы
            
            // === ADVANCED НАСТРОЙКИ ===
            SetOption("live_max_rows_enabled", "True", "ADVANCED");
            SetOption("live_max_rows", "1000", "ADVANCED");
            SetOption("overlay_fps_enabled", "False", "ADVANCED");
            SetOption("overlay_fps", "60", "ADVANCED");
            SetOption("bpf_filter_enabled", "False", "ADVANCED");
            SetOption("capture_filter", "ip or ip6", "ADVANCED");
            SetOption("smoothing_ping_value", "True", "ADVANCED");
            SetOption("smoothing_traffic_value", "True", "ADVANCED");
            SetOption("smoothing_tickrate_graph", "True", "ADVANCED");
            SetOption("smoothing_ticktime_graph", "True", "ADVANCED");
            SetOption("use_windows_stats", "False", "ADVANCED");
            SetOption("hybrid_pcap_windows", "True", "ADVANCED");
            SetOption("smoothing_ping_graph", "True", "ADVANCED");
            SetOption("smoothing_ping_graph_overlay", "True", "ADVANCED");
            SetOption("smoothing_tickrate_graph_overlay", "True", "ADVANCED");
            SetOption("smoothing_ticktime_graph_overlay", "True", "ADVANCED");
            SetOption("smoothing_ping_value_overlay", "True", "ADVANCED");
            SetOption("smoothing_tickrate_value_overlay", "True", "ADVANCED");
            SetOption("smoothing_traffic_value_overlay", "True", "ADVANCED");
            SetOption("smoothing_ping_value_gui", "True", "ADVANCED");
            SetOption("show_ping_spikes", "True", "ADVANCED");
            SetOption("ping_spike_threshold", "150", "ADVANCED");
            SetOption("vpn_bypass_basic", "True", "ADVANCED");
            SetOption("vpn_bypass_advanced", "True", "ADVANCED");
            SetOption("enable_text_logs", "False", "ADVANCED");
            SetOption("anti_reentrancy", "True", "ADVANCED");
            SetOption("rtss_throttling", "True", "ADVANCED");
            SetOption("pcap_optimization", "True", "ADVANCED");
            SetOption("pcap_kernel_buffer_mb", "8", "ADVANCED");
            SetOption("pcap_min_to_copy", "4096", "ADVANCED");
            // Phase 2/3 settings removed - obsolete after optimizations
            SetOption("spikes.enable", "True", "ADVANCED");
            SetOption("spikes.metrics", "ping,tickrate,ticktime", "ADVANCED");
            SetOption("spikes.display", "both", "ADVANCED");
            SetOption("spikes.sensitivity", "very_low", "ADVANCED");  // ОЧЕНЬ низкая чувствительность (новый пресет)
            SetOption("spikes.min_hold_ms", "50", "ADVANCED");        // Быстрое снятие индикатора
            SetOption("spikes.history_size", "1000", "ADVANCED");
            // SetOption("spikes.auto.enable", "True", "ADVANCED");   // УДАЛЕНО - dead code
            SetOption("spikes.ema_alpha", "0.050", "ADVANCED");
            SetOption("spikes.ew_sigma_alpha", "0.020", "ADVANCED");
            SetOption("spikes.sensitivity_multiplier", "3.0", "ADVANCED");
            SetOption("spikes.hysteresis_ratio", "0.70", "ADVANCED");
            SetOption("spikes.refractory_period_ms", "2000", "ADVANCED");
            SetOption("spikes.min_energy_threshold", "2.0", "ADVANCED");
            SetOption("spikes.init_window_size", "30", "ADVANCED");
            SetOption("alert_sound_enabled", "True", "ADVANCED");
            SetOption("alert_discord_enabled", "True", "ADVANCED");
            SetOption("alert_discord_webhook", "", "ADVANCED");
            SetOption("alert_cooldown_seconds", "30", "ADVANCED");
            SetOption("network_quality_enabled", "True", "ADVANCED");
            SetOption("network_quality_mode", "context", "ADVANCED");
            SetOption("network_quality_context_sync", "True", "ADVANCED");
            SetOption("network_quality_context_profile", "very_low", "ADVANCED");
            SetOption("network_quality_use_smoothed", "False", "ADVANCED");
            SetOption("quality_history_size", "100", "ADVANCED");
            SetOption("stability_threshold", "0.15", "ADVANCED");
            SetOption("quality_threshold", "0.8", "ADVANCED");
            SetOption("quality_profile_stability_tolerance", "False", "ADVANCED");
            SetOption("network_optimization_enabled", "False", "ADVANCED");
            SetOption("optimization_threshold", "70", "ADVANCED");
            SetOption("optimization_interval", "5", "ADVANCED");
            SetOption("aggressive_optimization", "False", "ADVANCED");
            SetOption("spike_detection_enable", "True", "ADVANCED");
            SetOption("spike_sensitivity", "High", "ADVANCED");
            SetOption("tickrate_chart_enabled", "True", "ADVANCED");
            SetOption("vpn_capture_virtual", "False", "ADVANCED");
            SetOption("vpn_allow_non_ethernet", "False", "ADVANCED");
            SetOption("vpn_disable_bpf", "False", "ADVANCED");
            SetOption("vpn_etw_enrichment", "False", "ADVANCED");
            SetOption("vpn_bypass_restore_capture_all", "True", "ADVANCED");
            SetOption("vpn_bypass_restore_ignore_virtual", "True", "ADVANCED");
            SetOption("vpn_bypass_restore_dedup", "True", "ADVANCED");
            SetOption("vpn_bypass_restore_basic", "True", "ADVANCED");
            SetOption("smoothing_tickrate_value_gui", "False", "ADVANCED");
            SetOption("alert_sound_pingspike_path", "", "ADVANCED");
            SetOption("alert_sound_tickratespike_path", "", "ADVANCED");
            SetOption("alert_sound_ticktimespike_path", "", "ADVANCED");
            SetOption("sync_ping_overlay_with_gui", "True", "ADVANCED");
            SetOption("sync_tickrate_overlay_with_gui", "True", "ADVANCED");
            
            // === ZONES ===
            SetOption("color_zone_profile", "Very Low", "ZONES");
            
            // === EXTENDED ===
            SetOption("show_active_process", "True", "EXTENDED");
            SetOption("show_session_time", "True", "EXTENDED");
            SetOption("show_external_ip", "True", "EXTENDED");
            SetOption("show_session_stats", "False", "EXTENDED");
            SetOption("show_server_info", "False", "EXTENDED");
            SetOption("show_packet_counters", "False", "EXTENDED");
            SetOption("show_connection_type", "False", "EXTENDED");
            SetOption("show_diagnostic_info", "False", "EXTENDED");
            
            // === TICKRATE_CHART ===
            SetOption("tickrate_chart_enabled", "True", "TICKRATE_CHART");
            SetOption("tickrate_chart_per_server", "True", "TICKRATE_CHART");
            SetOption("tickrate_chart_compression", "True", "TICKRATE_CHART");
            SetOption("tickrate_chart_time_scale", "True", "TICKRATE_CHART");
            SetOption("tickrate_chart_trimming", "True", "TICKRATE_CHART");
            SetOption("tickrate_chart_mode", "Сжатый график", "TICKRATE_CHART");
            SetOption("tickrate_chart_max_points", "1500", "TICKRATE_CHART");
            SetOption("tickrate_chart_history_hours", "24", "TICKRATE_CHART");
            
            // === PROFILES ===
            SetOption("current_profile", "Стандарт (Balanced)", "PROFILES");
            SetOption("advanced_profile", "Streamer", "PROFILES");
            
            DebugLogger.log("[SettingsManager] Оптимальные настройки записаны в settings.ini");
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
            lock (_lock)
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
        }

        public string GetOption(string optionName, string defaultValue, string scope = "SETTINGS")
        {
            lock (_lock)
            {
                if (data[scope] != null && data[scope][optionName] != null)
                {
                    return data[scope][optionName];
                }
                return defaultValue;
            }
        }

        public void SetOption(string optionName, string value, string scope = "SETTINGS")
        {
            lock (_lock)
            {
                if (data[scope] == null)
                {
                    data.Sections.AddSection(scope);
                }
                data[scope][optionName] = value;
                
                // Сохраняем только если не в режиме пакетного обновления
                if (!_batchMode)
                {
                    SaveConfigInternal(); // Вызов внутреннего метода без повторной блокировки
                }
            }
        }

        /// <summary>
        /// Начинает пакетное обновление настроек (без автосохранения после каждого SetOption)
        /// Поддерживает вложенные вызовы с помощью счетчика глубины
        /// </summary>
        public void BeginBatchUpdate()
        {
            lock (_lock)
            {
                _batchDepth++;
                if (_batchDepth == 1)
                {
                    _batchMode = true;
                    DebugLogger.log("[SettingsManager] Начато пакетное обновление настроек");
                }
                else
                {
                    DebugLogger.log($"[SettingsManager] Вложенный BeginBatchUpdate (глубина: {_batchDepth})");
                }
            }
        }

        /// <summary>
        /// Завершает пакетное обновление и сохраняет все изменения одним вызовом
        /// Поддерживает вложенные вызовы - сохраняет только при возврате к глубине 0
        /// </summary>
        public void EndBatchUpdate()
        {
            lock (_lock)
            {
                if (_batchDepth <= 0)
                {
                    DebugLogger.log("[SettingsManager] ПРЕДУПРЕЖДЕНИЕ: EndBatchUpdate вызван без соответствующего BeginBatchUpdate!");
                    return;
                }
                
                _batchDepth--;
                
                if (_batchDepth == 0)
                {
                    _batchMode = false;
                    SaveConfigInternal(); // Вызов внутреннего метода без повторной блокировки
                    DebugLogger.log("[SettingsManager] Завершено пакетное обновление, настройки сохранены");
                }
                else
                {
                    DebugLogger.log($"[SettingsManager] Вложенный EndBatchUpdate (осталось глубины: {_batchDepth})");
                }
            }
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
        
        /// <summary>
        /// Helper method for parsing decimal with invariant culture
        /// </summary>
        public static decimal ParseDecimalInvariant(string value)
        {
            return decimal.Parse(value, Inv);
        }
        
        /// <summary>
        /// Helper method for parsing float with invariant culture
        /// </summary>
        public static float ParseFloatInvariant(string value)
        {
            return float.Parse(value, Inv);
        }
        
        /// <summary>
        /// Helper method for parsing double with invariant culture
        /// </summary>
        public static double ParseDoubleInvariant(string value)
        {
            return double.Parse(value, Inv);
        }
        
        /// <summary>
        /// Helper method for parsing int with invariant culture
        /// </summary>
        public static int ParseIntInvariant(string value)
        {
            return int.Parse(value, Inv);
        }

        /// <summary>
        /// Внутренний метод сохранения без блокировки (предполагается вызов внутри lock)
        /// </summary>
        private void SaveConfigInternal()
        {
            int maxRetries = 3;
            int retryDelayMs = 100;
            Exception lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    parser.WriteFile("settings.ini", data);
                    DebugLogger.log($"[SettingsManager] Настройки успешно сохранены (попытка {attempt})");
                    return; // Успешно сохранено
                }
                catch (IOException ioEx)
                {
                    lastException = ioEx;
                    DebugLogger.log($"[SettingsManager] Попытка {attempt}/{maxRetries} сохранения не удалась: {ioEx.Message}");
                    
                    if (attempt < maxRetries)
                    {
                        System.Threading.Thread.Sleep(retryDelayMs);
                    }
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    lastException = uaEx;
                    DebugLogger.log($"[SettingsManager] Нет прав доступа для записи: {uaEx.Message}");
                    break; // Нет смысла повторять, если нет прав
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    DebugLogger.log($"[SettingsManager] Неожиданная ошибка при сохранении: {ex.Message}");
                    break;
                }
            }

            // Если мы здесь, то все попытки не удались
            string errorMessage = "Не удалось сохранить настройки в settings.ini";
            
            if (lastException is UnauthorizedAccessException)
            {
                errorMessage += "\n\nПричина: Недостаточно прав доступа.\n" +
                               "Решение: Запустите программу от имени администратора.";
            }
            else if (lastException is IOException)
            {
                errorMessage += "\n\nПричина: Файл может быть заблокирован другим процессом.\n" +
                               "Решение: Закройте другие программы, которые могут использовать файл настроек.";
            }
            else if (lastException != null)
            {
                errorMessage += $"\n\nОшибка: {lastException.Message}";
            }

            MessageBox.Show(errorMessage, "Ошибка сохранения", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        /// <summary>
        /// Публичный метод сохранения с блокировкой
        /// </summary>
        public void SaveConfig()
        {
            lock (_lock)
            {
                SaveConfigInternal();
            }
        }

        public void ReloadConfig()
        {
            lock (_lock)
            {
                // КРИТИЧНО: Сбрасываем batch mode при перезагрузке, иначе может остаться несогласованное состояние
                if (_batchMode || _batchDepth > 0)
                {
                    DebugLogger.log($"[SettingsManager] ПРЕДУПРЕЖДЕНИЕ: ReloadConfig вызван во время batch mode (depth={_batchDepth}), сбрасываем состояние");
                    _batchMode = false;
                    _batchDepth = 0;
                }
                
                int maxRetries = 3;
            int retryDelayMs = 100;
            Exception lastException = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    data = parser.ReadFile("settings.ini");
                    DebugLogger.log($"[SettingsManager] Настройки успешно загружены (попытка {attempt})");
                    return; // Успешно загружено
                }
                catch (IOException ioEx)
                {
                    lastException = ioEx;
                    DebugLogger.log($"[SettingsManager] Попытка {attempt}/{maxRetries} загрузки не удалась: {ioEx.Message}");
                    
                    if (attempt < maxRetries)
                    {
                        System.Threading.Thread.Sleep(retryDelayMs);
                    }
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    DebugLogger.log($"[SettingsManager] Неожиданная ошибка при загрузке: {ex.Message}");
                    break;
                }
            }

            // Если мы здесь, то все попытки не удались
            string errorMessage = "Не удалось загрузить настройки из settings.ini";
            
            if (lastException is IOException)
            {
                errorMessage += "\n\nПричина: Файл может быть заблокирован или поврежден.\n" +
                               "Решение: Проверьте файл настроек или попробуйте перезапустить программу.";
            }
            else if (lastException != null)
            {
                errorMessage += $"\n\nОшибка: {lastException.Message}";
            }

            MessageBox.Show(errorMessage, "Ошибка загрузки", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // Color Zone Profile management
        public ColorZoneProfile GetColorZoneProfile()
        {
            string profileName = GetOption("color_zone_profile", "Medium", "ZONES");
            return ColorZoneProfile.GetProfile(profileName, this);
        }

        public void SetColorZoneProfile(string profileName)
        {
            SetOption("color_zone_profile", profileName, "ZONES");
        }

        public void SetCustomColorZones(float pingGreen, float pingYellow, float tickrateGreen, float tickrateYellow, float ticktimeGreen, float ticktimeYellow)
        {
            // Используем BeginBatchUpdate/EndBatchUpdate для атомарной записи всех значений
            BeginBatchUpdate();
            try
            {
                SetOption("ping_green_threshold", pingGreen.ToString(CultureInfo.InvariantCulture), "ZONES");
                SetOption("ping_yellow_threshold", pingYellow.ToString(CultureInfo.InvariantCulture), "ZONES");
                SetOption("tickrate_green_ratio", tickrateGreen.ToString(CultureInfo.InvariantCulture), "ZONES");
                SetOption("tickrate_yellow_ratio", tickrateYellow.ToString(CultureInfo.InvariantCulture), "ZONES");
                SetOption("ticktime_green_ratio", ticktimeGreen.ToString(CultureInfo.InvariantCulture), "ZONES");
                SetOption("ticktime_yellow_ratio", ticktimeYellow.ToString(CultureInfo.InvariantCulture), "ZONES");
                SetOption("color_zone_profile", "Custom", "ZONES");
            }
            finally
            {
                EndBatchUpdate();
            }
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

        public static ColorZoneProfile GetProfile(string name, SettingsManager settings = null)
        {
            switch (name.ToLower())
            {
                case "very low":
                case "verylow":
                    // SYNC: Синхронизировано с QualityCalculationThresholds
                    return new ColorZoneProfile
                    {
                        Name = "Very Low",
                        PingGreenMs = 50f,          // = pingGood (QualityCalculationThresholds)
                        PingYellowMs = 150f,        // = pingBad (QualityCalculationThresholds)
                        TickrateGreenRatio = 120f / 128f, // Зеленая зона ~120 Гц
                        TickrateYellowRatio = 60f / 128f, // Желтая зона ~60-90 Гц
                        TicktimeGreenRatio = 0.80f, // Толерантность к медленной обработке
                        TicktimeYellowRatio = 1.20f  // Даже +20% от целевого времени = желтый
                    };
                case "low":
                    // SYNC: Синхронизировано с QualityCalculationThresholds
                    return new ColorZoneProfile
                    {
                        Name = "Low",
                        PingGreenMs = 45f,          // = pingGood (QualityCalculationThresholds)
                        PingYellowMs = 100f,        // = pingBad (QualityCalculationThresholds)
                        TickrateGreenRatio = 0.97f,
                        TickrateYellowRatio = 0.93f,
                        TicktimeGreenRatio = 0.70f,
                        TicktimeYellowRatio = 0.95f
                    };
                case "high":
                    // SYNC: Синхронизировано с QualityCalculationThresholds
                    return new ColorZoneProfile
                    {
                        Name = "High",
                        PingGreenMs = 20f,          // = pingGood (QualityCalculationThresholds)
                        PingYellowMs = 60f,         // = pingBad (QualityCalculationThresholds)
                        TickrateGreenRatio = 0.99f,
                        TickrateYellowRatio = 0.97f,
                        TicktimeGreenRatio = 0.50f,
                        TicktimeYellowRatio = 0.85f
                    };
                case "custom":
                    return LoadCustomProfile(settings);
                default: // Medium
                    // SYNC: Синхронизировано с QualityCalculationThresholds
                    return new ColorZoneProfile
                    {
                        Name = "Medium",
                        PingGreenMs = 30f,          // = pingGood (QualityCalculationThresholds)
                        PingYellowMs = 80f,         // = pingBad (QualityCalculationThresholds)
                        TickrateGreenRatio = 0.98f,
                        TickrateYellowRatio = 0.95f,
                        TicktimeGreenRatio = 0.60f,
                        TicktimeYellowRatio = 0.90f
                    };
            }
        }

        private static ColorZoneProfile LoadCustomProfile(SettingsManager settings)
        {
            const float defaultPingGreen = 40f;
            const float defaultPingYellow = 80f;
            const float defaultTickrateGreen = 0.98f;
            const float defaultTickrateYellow = 0.95f;
            const float defaultTicktimeGreen = 0.60f;
            const float defaultTicktimeYellow = 0.90f;

            var manager = settings ?? tickMeter.Classes.App.settingsManager;

            float pingGreen = defaultPingGreen;
            float pingYellow = defaultPingYellow;
            float tickrateGreen = defaultTickrateGreen;
            float tickrateYellow = defaultTickrateYellow;
            float ticktimeGreen = defaultTicktimeGreen;
            float ticktimeYellow = defaultTicktimeYellow;

            if (manager != null)
            {
                pingGreen = manager.GetFloat("ping_green_threshold", defaultPingGreen, "ZONES");
                pingYellow = manager.GetFloat("ping_yellow_threshold", defaultPingYellow, "ZONES");
                tickrateGreen = manager.GetFloat("tickrate_green_ratio", defaultTickrateGreen, "ZONES");
                tickrateYellow = manager.GetFloat("tickrate_yellow_ratio", defaultTickrateYellow, "ZONES");
                ticktimeGreen = manager.GetFloat("ticktime_green_ratio", defaultTicktimeGreen, "ZONES");
                ticktimeYellow = manager.GetFloat("ticktime_yellow_ratio", defaultTicktimeYellow, "ZONES");

                // На всякий случай защитимся от нулевых/отрицательных значений
                pingGreen = Math.Max(1f, pingGreen);
                pingYellow = Math.Max(pingGreen, pingYellow);
                tickrateGreen = ClampRatio(tickrateGreen);
                tickrateYellow = ClampRatio(tickrateYellow);
                ticktimeGreen = ClampRatio(ticktimeGreen, upperBound: 2f);
                ticktimeYellow = ClampRatio(ticktimeYellow, upperBound: 2f);
            }

            return new ColorZoneProfile
            {
                Name = "Custom",
                PingGreenMs = pingGreen,
                PingYellowMs = pingYellow,
                TickrateGreenRatio = tickrateGreen,
                TickrateYellowRatio = tickrateYellow,
                TicktimeGreenRatio = ticktimeGreen,
                TicktimeYellowRatio = ticktimeYellow
            };
        }

        private static float ClampRatio(float value, float lowerBound = 0.1f, float upperBound = 1.5f)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                return lowerBound;
            }

            if (value < lowerBound) return lowerBound;
            if (value > upperBound) return upperBound;
            return value;
        }

        public static string[] GetProfileNames()
        {
            return new string[] { "Very Low", "Low", "Medium", "High", "Custom" };
        }
    }
}
