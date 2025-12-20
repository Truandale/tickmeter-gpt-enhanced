using System;
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<string, PingResult> _lastPingResults = new ConcurrentDictionary<string, PingResult>();
        private readonly object _pingLock = new object();
    private int _immediatePingActive = 0;
        
        // Настройки ping из универсальных флагов
        private bool _bindToInterface => _settingsManager.GetBool("ping_bind_to_interface", true);
        private bool _preferTcp => _settingsManager.GetBool("ping_tcp_prefer", true);
        private bool _fallbackToIcmp => _settingsManager.GetBool("ping_fallback_icmp", true);
        private bool _targetActiveOnly => _settingsManager.GetBool("ping_target_active_only", true);
        
        // Настройки интервала и портов
        private int _pingInterval => _settingsManager.GetInt("ping_interval", 5000);
        private string _pingPorts => _settingsManager.GetString("ping_ports", "80,443");
        private bool _keepAliveEnabled => _settingsManager.GetBool("ping_keepalive_enabled", true);
        private string _keepAliveHosts => _settingsManager.GetString("ping_keepalive_hosts", "1.1.1.1,8.8.8.8");
        
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

        public void RequestImmediatePing()
        {
            if (Interlocked.Exchange(ref _immediatePingActive, 1) == 1)
            {
                return;
            }

            Task.Run(async () =>
            {
                try
                {
                    await PerformPingAsync();
                }
                catch (Exception ex)
                {
                    Debug.Print($"[PingManager] Immediate ping failed: {ex.Message}");
                }
                finally
                {
                    Interlocked.Exchange(ref _immediatePingActive, 0);
                }
            });
        }
        
        private void OnPingTimer(object state)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await PerformPingAsync();
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[PingManager] Ping error: {ex.Message}");
                }
            });
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
                        _lastPingResults[target.Address] = result;
                        
                        PingResultReceived?.Invoke(this, new PingResultEventArgs(result));
                    }
                }
                catch (Exception)
                {
                    // Ошибка ping к конкретному target - игнорируем
                }
            }
        }
        
        private List<PingTarget> GetPingTargets()
        {
            var targets = new List<PingTarget>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            void TryAddTarget(PingTarget target)
            {
                if (target == null || string.IsNullOrWhiteSpace(target.Address))
                    return;
                
                var key = $"{target.Address}:{target.Port}";
                if (seen.Add(key))
                {
                    targets.Add(target);
                }
            }
            
            if (_targetActiveOnly)
            {
                foreach (var target in GetActiveConnectionTargets())
                {
                    TryAddTarget(target);
                }
            }
            else
            {
                foreach (var target in GetServerTargets())
                {
                    TryAddTarget(target);
                }
            }
            
            // Фолбек: даже в режиме targetActiveOnly пробуем текущий сервер
            if (targets.Count == 0)
            {
                foreach (var target in GetServerTargets())
                {
                    TryAddTarget(target);
                }
            }
            
            // Keep-alive: если целей всё ещё нет, используем запасные хосты
            if (targets.Count == 0 && _keepAliveEnabled)
            {
                foreach (var target in GetKeepAliveTargets())
                {
                    TryAddTarget(target);
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
            var serverIp = App.meterState?.Server?.Ip;
            if (_connectionsManager.UdpActiveConnections.Count > 0 && 
                !string.IsNullOrEmpty(serverIp))
            {
                var ports = ParsePorts(_pingPorts);
                foreach (var port in ports)
                {
                    targets.Add(new PingTarget 
                    { 
                        Address = serverIp, 
                        Port = port 
                    });
                }
            }
            
            return targets;
        }
        
        private IEnumerable<PingTarget> GetServerTargets()
        {
            var serverIp = App.meterState?.Server?.Ip;
            if (string.IsNullOrWhiteSpace(serverIp))
                yield break;
            
            foreach (var target in CreateTargets(serverIp, ParsePorts(_pingPorts)))
            {
                yield return target;
            }
        }
        
        private IEnumerable<PingTarget> GetKeepAliveTargets()
        {
            if (string.IsNullOrWhiteSpace(_keepAliveHosts))
                yield break;
            
            var hosts = _keepAliveHosts.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var defaultPorts = ParsePorts(_pingPorts);
            if (defaultPorts.Count == 0)
            {
                defaultPorts.Add(443); // разумный дефолт для TCP keep-alive
            }
            
            foreach (var rawHost in hosts)
            {
                if (string.IsNullOrWhiteSpace(rawHost))
                    continue;
                
                var entry = rawHost.Trim();
                string address = entry;
                List<int> portsOverride = null;
                var colonIndex = entry.LastIndexOf(':');
                if (colonIndex > 0 && colonIndex < entry.Length - 1)
                {
                    var portPart = entry.Substring(colonIndex + 1);
                    if (int.TryParse(portPart, out var explicitPort) && explicitPort > 0 && explicitPort <= 65535)
                    {
                        address = entry.Substring(0, colonIndex);
                        portsOverride = new List<int> { explicitPort };
                    }
                }
                
                var ports = portsOverride ?? defaultPorts;
                foreach (var target in CreateTargets(address, ports))
                {
                    yield return target;
                }
            }
        }
        
        private IEnumerable<PingTarget> CreateTargets(string address, List<int> ports)
        {
            if (string.IsNullOrWhiteSpace(address))
                yield break;
            
            if (ports == null || ports.Count == 0)
            {
                yield return new PingTarget
                {
                    Address = address,
                    Port = 443
                };
                yield break;
            }
            
            foreach (var port in ports)
            {
                yield return new PingTarget
                {
                    Address = address,
                    Port = port
                };
            }
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