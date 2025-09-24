using System;
using System.Collections.Generic;
using System.IO;
using System.Media;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace tickMeter.Classes
{
    /// <summary>
    /// Этап 8: Advanced Alerting System
    /// Управляет уведомлениями о спайках через звуки и Discord webhook
    /// </summary>
    public static class AlertManager
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly Dictionary<string, DateTime> _lastAlerts = new Dictionary<string, DateTime>();
        private static readonly object _alertLock = new object();
        
        // Звуковые файлы по умолчанию (Windows system sounds)
        private static readonly Dictionary<AlertType, string> _defaultSounds = new Dictionary<AlertType, string>
        {
            { AlertType.PingSpike, "SystemAsterisk" },
            { AlertType.TickrateSpike, "SystemHand" },
            { AlertType.TicktimeSpike, "SystemQuestion" },
            { AlertType.CriticalSpike, "SystemExclamation" }
        };
        
        public enum AlertType
        {
            PingSpike,
            TickrateSpike, 
            TicktimeSpike,
            CriticalSpike
        }
        
        /// <summary>
        /// Отправляет алерт о спайке
        /// </summary>
        public static async Task SendAlert(AlertType alertType, string metricName, double value, double threshold)
        {
            try
            {
                string alertKey = $"{alertType}_{metricName}";
                
                // Проверяем cooldown
                lock (_alertLock)
                {
                    if (_lastAlerts.ContainsKey(alertKey))
                    {
                        var cooldownSeconds = GetAlertCooldown();
                        if (DateTime.Now.Subtract(_lastAlerts[alertKey]).TotalSeconds < cooldownSeconds)
                        {
                            return; // Слишком рано для нового алерта
                        }
                    }
                    _lastAlerts[alertKey] = DateTime.Now;
                }
                
                // Проверяем настройки включения алертов
                bool soundEnabled = App.settingsManager?.GetOption("alert_sound_enabled", "False", "ADVANCED") == "True";
                bool discordEnabled = App.settingsManager?.GetOption("alert_discord_enabled", "False", "ADVANCED") == "True";
                
                if (!soundEnabled && !discordEnabled)
                    return;
                
                // Формируем сообщение
                string message = FormatAlertMessage(alertType, metricName, value, threshold);
                
                // Отправляем звуковой алерт
                if (soundEnabled)
                {
                    await PlaySoundAlert(alertType);
                }
                
                // Отправляем Discord webhook
                if (discordEnabled)
                {
                    await SendDiscordAlert(message, alertType);
                }
                
                Debug.Print($"[AlertManager] Alert sent: {alertType} - {metricName}: {value} (threshold: {threshold})");
            }
            catch (Exception ex)
            {
                Debug.Print($"[AlertManager] Error sending alert: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Воспроизводит звуковой алерт
        /// </summary>
        private static async Task PlaySoundAlert(AlertType alertType)
        {
            try
            {
                // Проверяем пользовательский звуковой файл
                string customSoundPath = GetCustomSoundPath(alertType);
                if (!string.IsNullOrEmpty(customSoundPath) && File.Exists(customSoundPath))
                {
                    await Task.Run(() =>
                    {
                        try
                        {
                            using (var player = new SoundPlayer(customSoundPath))
                            {
                                player.Play();
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.Print($"[AlertManager] Error playing custom sound: {ex.Message}");
                            // Fallback to system sound
                            PlaySystemSound(alertType);
                        }
                    });
                }
                else
                {
                    // Используем системный звук
                    PlaySystemSound(alertType);
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[AlertManager] Error playing sound alert: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Воспроизводит системный звук
        /// </summary>
        private static void PlaySystemSound(AlertType alertType)
        {
            try
            {
                if (_defaultSounds.ContainsKey(alertType))
                {
                    string soundName = _defaultSounds[alertType];
                    switch (soundName)
                    {
                        case "SystemAsterisk":
                            SystemSounds.Asterisk.Play();
                            break;
                        case "SystemHand":
                            SystemSounds.Hand.Play();
                            break;
                        case "SystemQuestion":
                            SystemSounds.Question.Play();
                            break;
                        case "SystemExclamation":
                            SystemSounds.Exclamation.Play();
                            break;
                        default:
                            SystemSounds.Beep.Play();
                            break;
                    }
                }
                else
                {
                    SystemSounds.Beep.Play();
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[AlertManager] Error playing system sound: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Отправляет уведомление в Discord через webhook
        /// </summary>
        private static async Task SendDiscordAlert(string message, AlertType alertType)
        {
            try
            {
                string webhookUrl = App.settingsManager?.GetOption("alert_discord_webhook", "", "ADVANCED");
                if (string.IsNullOrEmpty(webhookUrl))
                    return;
                
                // Выбираем цвет по типу алерта
                int color = GetDiscordColor(alertType);
                
                // Формируем JSON payload для Discord
                var payload = new
                {
                    embeds = new[]
                    {
                        new
                        {
                            title = "🚨 TickMeter Spike Alert",
                            description = message,
                            color = color,
                            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                            footer = new
                            {
                                text = "TickMeter Enhanced"
                            }
                        }
                    }
                };
                
                string jsonPayload = Newtonsoft.Json.JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                
                // Отправляем с таймаутом
                _httpClient.Timeout = TimeSpan.FromSeconds(5);
                var response = await _httpClient.PostAsync(webhookUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    Debug.Print($"[AlertManager] Discord alert sent successfully");
                }
                else
                {
                    Debug.Print($"[AlertManager] Discord alert failed: {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[AlertManager] Error sending Discord alert: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Формирует сообщение алерта
        /// </summary>
        private static string FormatAlertMessage(AlertType alertType, string metricName, double value, double threshold)
        {
            string emoji = GetAlertEmoji(alertType);
            string severity = GetSeverityLevel(value, threshold);
            
            return $"{emoji} **{alertType}** detected!\n" +
                   $"**Metric:** {metricName}\n" +
                   $"**Value:** {value:F2}\n" +
                   $"**Threshold:** {threshold:F2}\n" +
                   $"**Severity:** {severity}\n" +
                   $"**Time:** {DateTime.Now:HH:mm:ss}";
        }
        
        /// <summary>
        /// Получает emoji для типа алерта
        /// </summary>
        private static string GetAlertEmoji(AlertType alertType)
        {
            switch (alertType)
            {
                case AlertType.PingSpike: return "🏓";
                case AlertType.TickrateSpike: return "⚡";
                case AlertType.TicktimeSpike: return "⏱️";
                case AlertType.CriticalSpike: return "🔥";
                default: return "⚠️";
            }
        }
        
        /// <summary>
        /// Получает цвет Discord для типа алерта
        /// </summary>
        private static int GetDiscordColor(AlertType alertType)
        {
            switch (alertType)
            {
                case AlertType.PingSpike: return 0xFFFF00; // Желтый
                case AlertType.TickrateSpike: return 0xFF6600; // Оранжевый
                case AlertType.TicktimeSpike: return 0xFF0066; // Розовый
                case AlertType.CriticalSpike: return 0xFF0000; // Красный
                default: return 0x808080; // Серый
            }
        }
        
        /// <summary>
        /// Определяет уровень серьезности спайка
        /// </summary>
        private static string GetSeverityLevel(double value, double threshold)
        {
            double ratio = Math.Abs(value / threshold);
            if (ratio > 3.0) return "🔴 Critical";
            if (ratio > 2.0) return "🟠 High";
            if (ratio > 1.5) return "🟡 Medium";
            return "🟢 Low";
        }
        
        /// <summary>
        /// Получает путь к пользовательскому звуковому файлу
        /// </summary>
        private static string GetCustomSoundPath(AlertType alertType)
        {
            string settingKey = $"alert_sound_{alertType.ToString().ToLower()}_path";
            return App.settingsManager?.GetOption(settingKey, "", "ADVANCED");
        }
        
        /// <summary>
        /// Получает интервал cooldown между алертами
        /// </summary>
        private static int GetAlertCooldown()
        {
            string cooldownStr = App.settingsManager?.GetOption("alert_cooldown_seconds", "30", "ADVANCED");
            if (int.TryParse(cooldownStr, out int cooldown))
            {
                return Math.Max(5, Math.Min(300, cooldown)); // 5-300 секунд
            }
            return 30;
        }
        
        /// <summary>
        /// Тестирует отправку алерта
        /// </summary>
        public static async Task TestAlert(AlertType alertType)
        {
            await SendAlert(alertType, "Test", 100.0, 50.0);
        }
        
        /// <summary>
        /// Очищает историю алертов (для сброса cooldown)
        /// </summary>
        public static void ClearAlertHistory()
        {
            lock (_alertLock)
            {
                _lastAlerts.Clear();
            }
        }
        
        /// <summary>
        /// Освобождает ресурсы
        /// </summary>
        public static void Dispose()
        {
            try
            {
                _httpClient?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.Print($"[AlertManager] Error disposing: {ex.Message}");
            }
        }
    }
}