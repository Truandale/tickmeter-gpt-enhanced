using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace tickMeter.Classes
{
    public class PingManager
    {
        private readonly SettingsManager _settingsManager;
        private readonly ConnectionsManager _connectionsManager;
        
        private Timer _pingTimer;
        private readonly Dictionary<string, PingResult> _lastPingResults = new Dictionary<string, PingResult>();
        private readonly object _pingLock = new object();
        
        // Настройки ping из универсальных флагов
        private bool _bindToInterface => _settingsManager.GetBool("ping_bind_to_interface", true);
        private bool _preferTcp => _settingsManager.GetBool("ping_tcp_prefer", true);
        private bool _fallbackToIcmp => _settingsManager.GetBool("ping_fallback_icmp", true);
        private bool _targetActiveOnly => _settingsManager.GetBool("ping_target_active_only", true);
        
        // Настройки интервала и портов
        private int _pingInterval => _settingsManager.GetInt("ping_interval", 5000);
        private string _pingPorts => _settingsManager.GetString("ping_ports", "80,443");
        
        public event EventHandler<PingResultEventArgs> PingResultReceived;
        
        public PingManager(SettingsManager settingsManager, ConnectionsManager connectionsManager)
        {
            _settingsManager = settingsManager ?? throw new ArgumentNullException(nameof(settingsManager));
            _connectionsManager = connectionsManager ?? throw new ArgumentNullException(nameof(connectionsManager));
        }
        
        public void StartPinging()
        {
            StopPinging();
            
            if (_pingInterval > 0)
            {
                _pingTimer = new Timer(OnPingTimer, null, 0, _pingInterval);
            }
        }
        
        public void StopPinging()
        {
            _pingTimer?.Dispose();
            _pingTimer = null;
        }
        
        private async void OnPingTimer(object state)
        {
            try
            {
                await PerformPingAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ping error: {ex.Message}");
            }
        }
        
        private async Task PerformPingAsync()
        {
            var targets = GetPingTargets();
            
            foreach (var target in targets)
            {
                try
                {
                    var result = await PingTargetAsync(target);
                    if (result != null)
                    {
                        lock (_pingLock)
                        {
                            _lastPingResults[target.Address] = result;
                        }
                        
                        PingResultReceived?.Invoke(this, new PingResultEventArgs(result));
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Ping to {target.Address}:{target.Port} failed: {ex.Message}");
                }
            }
        }
        
        private List<PingTarget> GetPingTargets()
        {
            var targets = new List<PingTarget>();
            
            if (_targetActiveOnly)
            {
                // Получаем цели из активных соединений
                targets.AddRange(GetActiveConnectionTargets());
            }
            else
            {
                // Используем текущий сервер из App.meterState
                if (!string.IsNullOrEmpty(App.meterState.Server.Ip))
                {
                    var ports = ParsePorts(_pingPorts);
                    foreach (var port in ports)
                    {
                        targets.Add(new PingTarget { Address = App.meterState.Server.Ip, Port = port });
                    }
                }
            }
            
            return targets;
        }
        
        private List<PingTarget> GetActiveConnectionTargets()
        {
            var targets = new List<PingTarget>();
            
            // TCP соединения
            foreach (var conn in _connectionsManager.TcpActiveConnections)
            {
                if (conn.State == MibTcpState.ESTABLISHED && 
                    !IsLocalAddress(conn.RemoteAddress))
                {
                    targets.Add(new PingTarget 
                    { 
                        Address = conn.RemoteAddress.ToString(), 
                        Port = conn.RemotePort 
                    });
                }
            }
            
            // UDP соединения (используем только информацию о том, что процесс активен)
            // UDP не имеет удалённых адресов, поэтому пингуем текущий сервер
            if (_connectionsManager.UdpActiveConnections.Count > 0 && 
                !string.IsNullOrEmpty(App.meterState.Server.Ip))
            {
                var ports = ParsePorts(_pingPorts);
                foreach (var port in ports)
                {
                    targets.Add(new PingTarget 
                    { 
                        Address = App.meterState.Server.Ip, 
                        Port = port 
                    });
                }
            }
            
            return targets;
        }
        
        private async Task<PingResult> PingTargetAsync(PingTarget target)
        {
            var stopwatch = Stopwatch.StartNew();
            
            if (_preferTcp)
            {
                // Сначала пробуем TCP ping
                var tcpResult = await TcpPingAsync(target.Address, target.Port, 3000);
                if (tcpResult.Success)
                {
                    return tcpResult;
                }
                
                // Если TCP не удался и включен fallback - пробуем ICMP
                if (_fallbackToIcmp)
                {
                    return await IcmpPingAsync(target.Address, 3000);
                }
                
                return tcpResult; // Возвращаем неудачный TCP результат
            }
            else
            {
                // Используем только ICMP
                return await IcmpPingAsync(target.Address, 3000);
            }
        }
        
        private async Task<PingResult> TcpPingAsync(string address, int port, int timeoutMs)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                using (var tcpClient = new TcpClient())
                {
                    // Если включена привязка к интерфейсу
                    if (_bindToInterface && !string.IsNullOrEmpty(App.meterState.LocalIP))
                    {
                        var localEndPoint = new IPEndPoint(IPAddress.Parse(App.meterState.LocalIP), 0);
                        tcpClient.Client.Bind(localEndPoint);
                    }
                    
                    var connectTask = tcpClient.ConnectAsync(address, port);
                    var timeoutTask = Task.Delay(timeoutMs);
                    
                    var completedTask = await Task.WhenAny(connectTask, timeoutTask);
                    stopwatch.Stop();
                    
                    if (completedTask == connectTask && !connectTask.IsFaulted)
                    {
                        return new PingResult
                        {
                            Success = true,
                            RoundTripTime = stopwatch.ElapsedMilliseconds,
                            Address = address,
                            Port = port,
                            Method = PingMethod.TCP
                        };
                    }
                    else
                    {
                        return new PingResult
                        {
                            Success = false,
                            RoundTripTime = -1,
                            Address = address,
                            Port = port,
                            Method = PingMethod.TCP,
                            ErrorMessage = connectTask.IsFaulted ? connectTask.Exception?.GetBaseException().Message : "Timeout"
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                return new PingResult
                {
                    Success = false,
                    RoundTripTime = -1,
                    Address = address,
                    Port = port,
                    Method = PingMethod.TCP,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        private async Task<PingResult> IcmpPingAsync(string address, int timeoutMs)
        {
            try
            {
                using (var ping = new Ping())
                {
                    var options = new PingOptions
                    {
                        DontFragment = true,
                        Ttl = 64
                    };
                    
                    var buffer = new byte[32];
                    var reply = await ping.SendPingAsync(address, timeoutMs, buffer, options);
                    
                    return new PingResult
                    {
                        Success = reply.Status == IPStatus.Success,
                        RoundTripTime = reply.Status == IPStatus.Success ? reply.RoundtripTime : -1,
                        Address = address,
                        Port = 0, // ICMP не использует порты
                        Method = PingMethod.ICMP,
                        ErrorMessage = reply.Status != IPStatus.Success ? reply.Status.ToString() : null
                    };
                }
            }
            catch (Exception ex)
            {
                return new PingResult
                {
                    Success = false,
                    RoundTripTime = -1,
                    Address = address,
                    Port = 0,
                    Method = PingMethod.ICMP,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        private bool IsLocalAddress(IPAddress address)
        {
            if (address == null) return true;
            
            var addressString = address.ToString();
            
            // Проверяем локальные диапазоны
            return addressString.StartsWith("127.") ||
                   addressString.StartsWith("192.168.") ||
                   addressString.StartsWith("10.") ||
                   (addressString.StartsWith("172.") && 
                    int.TryParse(addressString.Split('.')[1], out var second) && 
                    second >= 16 && second <= 31) ||
                   addressString == "::1" ||
                   addressString.StartsWith("fe80:");
        }
        
        private List<int> ParsePorts(string portString)
        {
            var ports = new List<int>();
            
            if (string.IsNullOrWhiteSpace(portString))
                return ports;
            
            var portParts = portString.Split(',');
            foreach (var part in portParts)
            {
                if (int.TryParse(part.Trim(), out var port) && port > 0 && port <= 65535)
                {
                    ports.Add(port);
                }
            }
            
            return ports;
        }
        
        public Dictionary<string, PingResult> GetLastPingResults()
        {
            lock (_pingLock)
            {
                return new Dictionary<string, PingResult>(_lastPingResults);
            }
        }
    }
    
    public class PingTarget
    {
        public string Address { get; set; }
        public int Port { get; set; }
    }
    
    public class PingResult
    {
        public bool Success { get; set; }
        public long RoundTripTime { get; set; }
        public string Address { get; set; }
        public int Port { get; set; }
        public PingMethod Method { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
    
    public enum PingMethod
    {
        TCP,
        ICMP
    }
    
    public class PingResultEventArgs : EventArgs
    {
        public PingResult Result { get; }
        
        public PingResultEventArgs(PingResult result)
        {
            Result = result;
        }
    }
}