using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace tickMeter.Classes
{
    /// <summary>
    /// Простой STUN клиент для определения внешнего IP адреса
    /// </summary>
    public class StunClient
    {
        private static readonly string[] StunServers = new[]
        {
            "stun.l.google.com:19302",
            "stun1.l.google.com:19302", 
            "stun2.l.google.com:19302",
            "stun.cloudflare.com:3478",
            "stun.nextcloud.com:443"
        };
        
        /// <summary>
        /// Получает внешний IP адрес используя STUN протокол
        /// </summary>
        /// <param name="timeoutMs">Таймаут в миллисекундах</param>
        /// <returns>Внешний IP адрес или null при ошибке</returns>
        public static async Task<IPAddress> GetExternalIpAsync(int timeoutMs = 5000)
        {
            foreach (var server in StunServers)
            {
                try
                {
                    var ip = await GetExternalIpFromServerAsync(server, timeoutMs);
                    if (ip != null)
                    {
                        Debug.WriteLine($"STUN: External IP {ip} from {server}");
                        return ip;
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"STUN: Failed to query {server}: {ex.Message}");
                }
            }
            
            return null;
        }
        
        private static async Task<IPAddress> GetExternalIpFromServerAsync(string serverAddress, int timeoutMs)
        {
            var parts = serverAddress.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out int port))
                throw new ArgumentException("Invalid server address format");
            
            var serverEndPoint = new IPEndPoint(Dns.GetHostAddresses(parts[0])[0], port);
            
            using (var udpClient = new UdpClient())
            {
                udpClient.Client.ReceiveTimeout = timeoutMs;
                udpClient.Client.SendTimeout = timeoutMs;
                
                // STUN Binding Request
                var bindingRequest = CreateStunBindingRequest();
                
                await udpClient.SendAsync(bindingRequest, bindingRequest.Length, serverEndPoint);
                
                var result = await udpClient.ReceiveAsync();
                var response = result.Buffer;
                
                return ParseStunResponse(response);
            }
        }
        
        private static byte[] CreateStunBindingRequest()
        {
            var transactionId = new byte[12];
            new Random().NextBytes(transactionId);
            
            var request = new byte[20];
            
            // STUN Message Type: Binding Request (0x0001)
            request[0] = 0x00;
            request[1] = 0x01;
            
            // Message Length: 0 (no attributes)
            request[2] = 0x00;
            request[3] = 0x00;
            
            // Magic Cookie: 0x2112A442
            request[4] = 0x21;
            request[5] = 0x12;
            request[6] = 0xA4;
            request[7] = 0x42;
            
            // Transaction ID (12 bytes)
            Array.Copy(transactionId, 0, request, 8, 12);
            
            return request;
        }
        
        private static IPAddress ParseStunResponse(byte[] response)
        {
            if (response.Length < 20)
                throw new ArgumentException("Invalid STUN response length");
            
            // Check if it's a success response (0x0101)
            if (response[0] != 0x01 || response[1] != 0x01)
                throw new ArgumentException("Not a successful binding response");
            
            var messageLength = (response[2] << 8) | response[3];
            var offset = 20; // Skip STUN header
            
            while (offset < 20 + messageLength)
            {
                if (offset + 4 > response.Length)
                    break;
                
                var attributeType = (response[offset] << 8) | response[offset + 1];
                var attributeLength = (response[offset + 2] << 8) | response[offset + 3];
                
                if (attributeType == 0x0001) // MAPPED-ADDRESS
                {
                    return ParseMappedAddress(response, offset + 4, attributeLength);
                }
                else if (attributeType == 0x0020) // XOR-MAPPED-ADDRESS
                {
                    return ParseXorMappedAddress(response, offset + 4, attributeLength);
                }
                
                offset += 4 + attributeLength;
                // Pad to 4-byte boundary
                if (attributeLength % 4 != 0)
                    offset += 4 - (attributeLength % 4);
            }
            
            throw new ArgumentException("No mapped address found in STUN response");
        }
        
        private static IPAddress ParseMappedAddress(byte[] data, int offset, int length)
        {
            if (length < 8)
                throw new ArgumentException("Invalid mapped address length");
            
            var family = data[offset + 1];
            if (family == 0x01) // IPv4
            {
                var ipBytes = new byte[4];
                Array.Copy(data, offset + 4, ipBytes, 0, 4);
                return new IPAddress(ipBytes);
            }
            
            throw new NotSupportedException("Only IPv4 addresses are supported");
        }
        
        private static IPAddress ParseXorMappedAddress(byte[] data, int offset, int length)
        {
            if (length < 8)
                throw new ArgumentException("Invalid XOR mapped address length");
            
            var family = data[offset + 1];
            if (family == 0x01) // IPv4
            {
                var ipBytes = new byte[4];
                Array.Copy(data, offset + 4, ipBytes, 0, 4);
                
                // XOR with magic cookie (0x2112A442)
                ipBytes[0] ^= 0x21;
                ipBytes[1] ^= 0x12;
                ipBytes[2] ^= 0xA4;
                ipBytes[3] ^= 0x42;
                
                return new IPAddress(ipBytes);
            }
            
            throw new NotSupportedException("Only IPv4 addresses are supported");
        }
    }
    
    /// <summary>
    /// Менеджер для определения внешнего IP через STUN
    /// </summary>
    public static class StunManager
    {
        private static IPAddress _lastExternalIp;
        private static DateTime _lastUpdate = DateTime.MinValue;
        private static readonly TimeSpan CacheTimeout = TimeSpan.FromMinutes(10);
        private static readonly object _lock = new object();
        
        /// <summary>
        /// Проверяет, включено ли STUN определение внешнего IP
        /// </summary>
        public static bool IsEnabled()
        {
            return App.settingsManager?.GetBool("stun_enable", false) == true;
        }
        
        /// <summary>
        /// Получает внешний IP адрес с кэшированием
        /// </summary>
        /// <returns>Внешний IP адрес или null</returns>
        public static async Task<IPAddress> GetExternalIpAsync()
        {
            if (!IsEnabled())
                return null;
            
            lock (_lock)
            {
                // Возвращаем кэшированное значение если оно свежее
                if (_lastExternalIp != null && DateTime.Now - _lastUpdate < CacheTimeout)
                {
                    return _lastExternalIp;
                }
            }
            
            try
            {
                var externalIp = await StunClient.GetExternalIpAsync();
                
                lock (_lock)
                {
                    if (externalIp != null)
                    {
                        _lastExternalIp = externalIp;
                        _lastUpdate = DateTime.Now;
                    }
                }
                
                return externalIp;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"STUN Manager error: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Получает внешний IP адрес как строку
        /// </summary>
        /// <returns>Внешний IP адрес как строка или null</returns>
        public static async Task<string> GetExternalIpStringAsync()
        {
            var ip = await GetExternalIpAsync();
            return ip?.ToString();
        }
        
        /// <summary>
        /// Получает кэшированный внешний IP без запроса к серверу
        /// </summary>
        /// <returns>Кэшированный внешний IP или null</returns>
        public static IPAddress GetCachedExternalIp()
        {
            lock (_lock)
            {
                if (_lastExternalIp != null && DateTime.Now - _lastUpdate < CacheTimeout)
                {
                    return _lastExternalIp;
                }
                return null;
            }
        }
        
        /// <summary>
        /// Очищает кэш внешнего IP
        /// </summary>
        public static void ClearCache()
        {
            lock (_lock)
            {
                _lastExternalIp = null;
                _lastUpdate = DateTime.MinValue;
            }
        }
    }
}