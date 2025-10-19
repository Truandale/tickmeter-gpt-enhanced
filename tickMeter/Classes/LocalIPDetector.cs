using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using static tickMeter.ConnectionsManager;

namespace tickMeter.Classes
{
    /// <summary>
    /// Автоматически определяет локальный IP адрес на основе активного процесса и его сетевых соединений
    /// </summary>
    public static class LocalIPDetector
    {
        private static string _lastDetectedIP = string.Empty;
        private static DateTime _lastDetectionTime = DateTime.MinValue;
        private static readonly TimeSpan DetectionCooldown = TimeSpan.FromSeconds(5); // Не обновлять чаще раз в 5 секунд
        
        /// <summary>
        /// Определяет оптимальный локальный IP для текущего активного процесса
        /// </summary>
        /// <param name="processName">Имя отслеживаемого процесса (опционально)</param>
        /// <returns>Локальный IP или null если не удалось определить</returns>
        public static string DetectLocalIPForActiveProcess(string processName = null)
        {
            try
            {
                // Защита от слишком частых вызовов
                if (!string.IsNullOrEmpty(_lastDetectedIP) && 
                    (DateTime.Now - _lastDetectionTime) < DetectionCooldown)
                {
                    return _lastDetectedIP;
                }
                
                string detectedIP = null;
                
                // Метод 1: Поиск по активным TCP соединениям процесса
                if (!string.IsNullOrEmpty(processName))
                {
                    detectedIP = GetLocalIPFromProcessConnections(processName);
                    if (!string.IsNullOrEmpty(detectedIP))
                    {
                        Debug.WriteLine($"[LocalIPDetector] Определен IP по соединениям процесса {processName}: {detectedIP}");
                        UpdateCache(detectedIP);
                        return detectedIP;
                    }
                }
                
                // Метод 2: Поиск по активным соединениям всех процессов
                detectedIP = GetLocalIPFromAllConnections();
                if (!string.IsNullOrEmpty(detectedIP))
                {
                    Debug.WriteLine($"[LocalIPDetector] Определен IP по всем активным соединениям: {detectedIP}");
                    UpdateCache(detectedIP);
                    return detectedIP;
                }
                
                // Метод 3: Выбор первого физического адаптера с трафиком
                detectedIP = GetLocalIPFromActiveAdapter();
                if (!string.IsNullOrEmpty(detectedIP))
                {
                    Debug.WriteLine($"[LocalIPDetector] Определен IP по активному адаптеру: {detectedIP}");
                    UpdateCache(detectedIP);
                    return detectedIP;
                }
                
                // Метод 4 (крайний fallback): Если ничего не нашли, но есть кэшированный IP - используем его
                if (!string.IsNullOrEmpty(_lastDetectedIP))
                {
                    Debug.WriteLine($"[LocalIPDetector] Используем кэшированный IP: {_lastDetectedIP}");
                    return _lastDetectedIP;
                }
                
                Debug.WriteLine("[LocalIPDetector] КРИТИЧНО: Не удалось определить IP никаким методом!");
                return null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalIPDetector] Ошибка определения IP: {ex.Message}");
                return _lastDetectedIP;
            }
        }
        
        /// <summary>
        /// Определяет локальный IP по активным соединениям конкретного процесса (TCP + UDP)
        /// </summary>
        private static string GetLocalIPFromProcessConnections(string processName)
        {
            try
            {
                // Используем существующий ConnectionsManager
                if (App.connMngr == null)
                {
                    Debug.WriteLine("[LocalIPDetector] ConnectionsManager не инициализирован");
                    return null;
                }
                
                var allIPs = new List<string>();
                
                // TCP соединения
                var tcpConnections = App.connMngr.TcpActiveConnections;
                if (tcpConnections != null && tcpConnections.Count > 0)
                {
                    var tcpIPs = tcpConnections
                        .Where(c => c.ProcessName != null && 
                                   c.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                        .Where(c => c.State == MibTcpState.ESTABLISHED) // Только установленные соединения
                        .Select(c => c.LocalAddress.ToString())
                        .ToList();
                    allIPs.AddRange(tcpIPs);
                }
                
                // UDP соединения
                var udpConnections = App.connMngr.UdpActiveConnections;
                if (udpConnections != null && udpConnections.Count > 0)
                {
                    var udpIPs = udpConnections
                        .Where(c => c.ProcessName != null && 
                                   c.ProcessName.Equals(processName, StringComparison.OrdinalIgnoreCase))
                        .Select(c => c.LocalAddress.ToString())
                        .ToList();
                    allIPs.AddRange(udpIPs);
                }
                
                if (allIPs.Count == 0) return null;
                
                // Группируем по локальному IP и выбираем наиболее частый
                var ipGroups = allIPs
                    .GroupBy(ip => ip)
                    .OrderByDescending(g => g.Count())
                    .ToList();
                
                foreach (var group in ipGroups)
                {
                    string ip = group.Key;
                    if (IsValidLocalIP(ip))
                    {
                        Debug.WriteLine($"[LocalIPDetector] Процесс {processName} использует IP {ip} ({group.Count()} TCP+UDP соединений)");
                        return ip;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalIPDetector] Ошибка GetLocalIPFromProcessConnections: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Определяет наиболее используемый локальный IP по всем активным соединениям (TCP + UDP)
        /// </summary>
        private static string GetLocalIPFromAllConnections()
        {
            try
            {
                if (App.connMngr == null) return null;
                
                var allIPs = new List<string>();
                
                // TCP соединения
                var tcpConnections = App.connMngr.TcpActiveConnections;
                if (tcpConnections != null && tcpConnections.Count > 0)
                {
                    var tcpIPs = tcpConnections
                        .Where(c => c.State == MibTcpState.ESTABLISHED)
                        .Select(c => c.LocalAddress.ToString())
                        .ToList();
                    allIPs.AddRange(tcpIPs);
                }
                
                // UDP соединения
                var udpConnections = App.connMngr.UdpActiveConnections;
                if (udpConnections != null && udpConnections.Count > 0)
                {
                    var udpIPs = udpConnections
                        .Select(c => c.LocalAddress.ToString())
                        .ToList();
                    allIPs.AddRange(udpIPs);
                }
                
                if (allIPs.Count == 0) return null;
                
                // Группируем по локальному IP
                var ipStats = allIPs
                    .GroupBy(ip => ip)
                    .Select(g => new { IP = g.Key, Count = g.Count() })
                    .Where(x => IsValidLocalIP(x.IP))
                    .OrderByDescending(x => x.Count)
                    .ToList();
                
                if (ipStats.Count > 0)
                {
                    var topIP = ipStats.First();
                    Debug.WriteLine($"[LocalIPDetector] Наиболее используемый IP: {topIP.IP} ({topIP.Count} TCP+UDP соединений)");
                    return topIP.IP;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalIPDetector] Ошибка GetLocalIPFromAllConnections: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Определяет IP активного физического адаптера
        /// </summary>
        private static string GetLocalIPFromActiveAdapter()
        {
            try
            {
                var adapters = App.GetAdapters();
                if (adapters == null || adapters.Count == 0) return null;
                
                foreach (var adapter in adapters)
                {
                    string ip = App.GetAdapterAddress(adapter);
                    if (IsValidLocalIP(ip))
                    {
                        // Проверяем, не виртуальный ли это адаптер
                        if (!IsVirtualAdapter(adapter.Description))
                        {
                            return ip;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalIPDetector] Ошибка GetLocalIPFromActiveAdapter: {ex.Message}");
            }
            
            return null;
        }
        
        /// <summary>
        /// Проверяет, является ли IP валидным локальным адресом
        /// </summary>
        private static bool IsValidLocalIP(string ip)
        {
            if (string.IsNullOrEmpty(ip)) return false;
            
            // Исключаем localhost
            if (ip == "127.0.0.1" || ip == "::1") return false;
            
            // Исключаем APIPA адреса
            if (ip.StartsWith("169.254.")) return false;
            
            // Проверяем формат IPv4
            string[] parts = ip.Split('.');
            if (parts.Length != 4) return false;
            
            foreach (string part in parts)
            {
                if (!int.TryParse(part, out int num) || num < 0 || num > 255)
                    return false;
            }
            
            // Исключаем 0.0.0.0
            if (ip == "0.0.0.0") return false;
            
            return true;
        }
        
        /// <summary>
        /// Проверяет, является ли адаптер виртуальным
        /// </summary>
        private static bool IsVirtualAdapter(string description)
        {
            if (string.IsNullOrEmpty(description)) return false;
            
            string desc = description.ToLower();
            return desc.Contains("vmware") ||
                   desc.Contains("virtualbox") ||
                   desc.Contains("hyper-v") ||
                   desc.Contains("tap-") ||
                   desc.Contains("openvpn") ||
                   desc.Contains("wireguard") ||
                   desc.Contains("nordvpn") ||
                   desc.Contains("expressvpn") ||
                   desc.Contains("virtual") ||
                   desc.Contains("loopback");
        }
        
        /// <summary>
        /// Обновляет кэш определенного IP
        /// </summary>
        private static void UpdateCache(string ip)
        {
            _lastDetectedIP = ip;
            _lastDetectionTime = DateTime.Now;
        }
        
        /// <summary>
        /// Сбрасывает кэш для принудительного обновления
        /// </summary>
        public static void ResetCache()
        {
            _lastDetectedIP = string.Empty;
            _lastDetectionTime = DateTime.MinValue;
            Debug.WriteLine("[LocalIPDetector] Кэш сброшен");
        }
        
        /// <summary>
        /// Получает последний определенный IP из кэша
        /// </summary>
        public static string GetCachedIP()
        {
            return _lastDetectedIP;
        }
    }
}
