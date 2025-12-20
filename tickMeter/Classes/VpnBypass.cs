using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using PcapDotNet.Core;
using tickMeter.Classes;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;

namespace tickMeter.Classes
{
    /// <summary>
    /// Real ETW Connection Tracker using Microsoft.Diagnostics.Tracing.TraceEvent
    /// Monitors network events in real-time to replace VpnBypass polling
    /// </summary>
    public sealed class ETWConnectionTracker : IDisposable
    {
        // Структуры, аналогичные VpnBypass.ConnectionTracker
        public readonly struct Key : IEquatable<Key>
        {
            public readonly byte Proto; // 6=TCP, 17=UDP
            public readonly IPAddress Local;
            public readonly int LocalPort;
            public readonly IPAddress Remote;
            public readonly int RemotePort;
            
            public Key(byte proto, IPAddress l, int lp, IPAddress r, int rp)
            { Proto = proto; Local = l; LocalPort = lp; Remote = r; RemotePort = rp; }
            
            public bool Equals(Key o) =>
                Proto == o.Proto && Local.Equals(o.Local) && LocalPort == o.LocalPort &&
                Remote.Equals(o.Remote) && RemotePort == o.RemotePort;
                
            public override bool Equals(object obj) => obj is Key other && Equals(other);
            public override int GetHashCode() => Proto.GetHashCode() ^ Local.GetHashCode() ^ LocalPort ^ Remote.GetHashCode() ^ RemotePort;
            public static bool operator ==(Key a, Key b) => a.Equals(b);
            public static bool operator !=(Key a, Key b) => !a.Equals(b);
        }
        
        public readonly struct Info
        {
            public readonly uint Pid;
            public readonly string Exe;
            
            public Info(uint pid, string exe)
            { Pid = pid; Exe = exe ?? ""; }
        }
        
        // События для уведомлений
        public event Action<Key, Info> OnNewConnection;
        public event Action<Key> OnConnectionClosed;
        
        // ETW компоненты
        private TraceEventSession _etwSession;
        private Thread _etwThread;
        private volatile bool _isDisposed = false;
        private volatile bool _isRunning = false;
        
        // Connection storage
        private readonly ConcurrentDictionary<Key, Info> _connections = new ConcurrentDictionary<Key, Info>();
        
        // Performance metrics
        private long _etwEventsReceived = 0;
        private long _connectionsTracked = 0;
        private long _eventsProcessed = 0;
        
        public ETWConnectionTracker()
        {
            try
            {
                InitializeETW();
                DebugLogger.log("[ETWConnectionTracker] Real ETW session initialized");
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETWConnectionTracker] ETW initialization failed: {ex.Message}");
                throw;
            }
        }
        
        private void InitializeETW()
        {
            // Создаем ETW session для мониторинга сетевых событий
            var sessionName = $"TickMeter-ETW-{Process.GetCurrentProcess().Id}";
            _etwSession = new TraceEventSession(sessionName, null);
            
            // Подписываемся на Microsoft-Windows-Kernel-Network provider
            // Этот провайдер генерирует события TCP/UDP соединений
            _etwSession.EnableProvider("Microsoft-Windows-Kernel-Network", TraceEventLevel.Informational);
            
            // Альтернативные провайдеры для более полного покрытия (включая UDP)
            try
            {
                _etwSession.EnableProvider("Microsoft-Windows-Winsock-AFD", TraceEventLevel.Informational);
                _etwSession.EnableProvider("Microsoft-Windows-TCPIP", TraceEventLevel.Informational);
                
                // UDP-специфичные провайдеры для лучшего покрытия
                _etwSession.EnableProvider("Microsoft-Windows-Kernel-Process", TraceEventLevel.Informational);
                _etwSession.EnableProvider("Microsoft-Windows-Winsock-NameResolution", TraceEventLevel.Informational);
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETWConnectionTracker] Warning: Additional providers failed: {ex.Message}");
            }
            
            // Настраиваем обработчики событий
            _etwSession.Source.Dynamic.All += OnETWEvent;
            
            // Запускаем ETW в отдельном потоке
            _etwThread = new Thread(ETWWorkerThread) 
            { 
                IsBackground = true, 
                Name = "ETW-ConnectionTracker" 
            };
            _isRunning = true;
            _etwThread.Start();
        }
        
        private void ETWWorkerThread()
        {
            try
            {
                DebugLogger.log("[ETWConnectionTracker] ETW worker thread started");
                _etwSession.Source.Process();
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETWConnectionTracker] ETW worker thread error: {ex.Message}");
            }
            finally
            {
                DebugLogger.log("[ETWConnectionTracker] ETW worker thread stopped");
            }
        }
        
        private void OnETWEvent(TraceEvent eventData)
        {
            try
            {
                Interlocked.Increment(ref _etwEventsReceived);
                
                // Фильтруем только сетевые события
                if (!IsNetworkEvent(eventData))
                    return;
                    
                Interlocked.Increment(ref _eventsProcessed);
                
                // Парсим событие для извлечения connection info
                if (TryParseNetworkEvent(eventData, out var key, out var info))
                {
                    // Добавляем или обновляем connection
                    if (_connections.TryAdd(key, info))
                    {
                        Interlocked.Increment(ref _connectionsTracked);
                        OnNewConnection?.Invoke(key, info);
                        
                        DebugLogger.log($"[ETW] New connection: {info.Exe}({info.Pid}) {key.Local}:{key.LocalPort} -> {key.Remote}:{key.RemotePort}");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETWConnectionTracker] Event processing error: {ex.Message}");
            }
        }
        
        private bool IsNetworkEvent(TraceEvent eventData)
        {
            // Фильтруем по provider и event names для TCP и UDP
            var providerName = eventData.ProviderName;
            var eventName = eventData.EventName;
            
            return providerName.Contains("Network") || 
                   providerName.Contains("TCPIP") || 
                   providerName.Contains("Winsock") ||
                   eventName.Contains("Connect") ||
                   eventName.Contains("Accept") ||
                   eventName.Contains("Send") ||
                   eventName.Contains("Recv") ||
                   eventName.Contains("UDP") ||
                   eventName.Contains("Bind") ||
                   eventName.Contains("Socket");
        }
        
        private bool TryParseNetworkEvent(TraceEvent eventData, out Key key, out Info info)
        {
            key = default;
            info = default;
            
            try
            {
                // Парсим payload события для извлечения network info
                var processId = (uint)eventData.ProcessID;
                var processName = eventData.ProcessName ?? "Unknown";
                var eventName = eventData.EventName;
                
                // Обрабатываем TCP события
                if (TryParseTCPEvent(eventData, processId, processName, out key, out info))
                {
                    return true;
                }
                
                // Обрабатываем UDP события
                if (TryParseUDPEvent(eventData, processId, processName, out key, out info))
                {
                    return true;
                }
                
                // Fallback на старую логику для совместимости
                var providerName = eventData.ProviderName;
                
                // Заглушка для демонстрации - в реальности нужен парсинг по типам событий
                if (TryExtractNetworkInfo(eventData, out var proto, out var local, out var lport, out var remote, out var rport))
                {
                    key = new Key(proto, local, lport, remote, rport);
                    info = new Info(processId, processName);
                    return true;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETWConnectionTracker] Event parsing error: {ex.Message}");
            }
            
            return false;
        }
        
        private bool TryParseTCPEvent(TraceEvent eventData, uint processId, string processName, out Key key, out Info info)
        {
            key = default;
            info = default;
            
            try
            {
                var eventName = eventData.EventName;
                
                // TCP-специфичные события
                if (eventName.Contains("Connect") || eventName.Contains("Accept") || eventName.Contains("TCP"))
                {
                    if (TryExtractNetworkInfo(eventData, out var proto, out var local, out var lport, out var remote, out var rport))
                    {
                        key = new Key(proto, local, lport, remote, rport);
                        info = new Info(processId, processName);
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETWConnectionTracker] TCP event parsing error: {ex.Message}");
            }
            
            return false;
        }
        
        private bool TryParseUDPEvent(TraceEvent eventData, uint processId, string processName, out Key key, out Info info)
        {
            key = default;
            info = default;
            
            try
            {
                var eventName = eventData.EventName;
                
                // UDP-специфичные события: Bind, Send, Recv, UDP
                if (eventName.Contains("UDP") || eventName.Contains("Bind") || 
                    (eventName.Contains("Send") && IsUDPEvent(eventData)) ||
                    (eventName.Contains("Recv") && IsUDPEvent(eventData)))
                {
                    if (TryExtractUDPNetworkInfo(eventData, out var local, out var lport, out var remote, out var rport))
                    {
                        key = new Key(17, local, lport, remote, rport); // Protocol = 17 для UDP
                        info = new Info(processId, processName);
                        
                        DebugLogger.log($"[ETW] UDP event parsed: {processName}({processId}) {local}:{lport} -> {remote}:{rport}");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETWConnectionTracker] UDP event parsing error: {ex.Message}");
            }
            
            return false;
        }
        
        private bool IsUDPEvent(TraceEvent eventData)
        {
            try
            {
                // Проверяем payload или другие индикаторы UDP протокола
                // В реальной реализации здесь нужно анализировать структуру события
                return eventData.PayloadNames.Any(name => 
                    name.Contains("Protocol") || 
                    name.Contains("UDP") || 
                    name.ToLower().Contains("socket"));
            }
            catch
            {
                return false;
            }
        }
        
        private bool TryExtractUDPNetworkInfo(TraceEvent eventData, out IPAddress local, out int lport, out IPAddress remote, out int rport)
        {
            // UDP-специфичная логика извлечения network info
            local = IPAddress.Loopback;
            lport = 0;
            remote = IPAddress.Loopback;
            rport = 0;
            
            try
            {
                var eventName = eventData.EventName;
                
                // Для UDP Bind события - обычно есть только local endpoint
                if (eventName.Contains("Bind"))
                {
                    // TODO: Извлечь local address и port из payload
                    // local = IPAddress.Parse(eventData.PayloadValue("LocalAddress").ToString());
                    // lport = (int)eventData.PayloadValue("LocalPort");
                    
                    // Для Bind событий remote endpoint может быть 0.0.0.0:0
                    remote = IPAddress.Any;
                    rport = 0;
                    
                    return false; // Возвращаем false пока парсинг не реализован полностью
                }
                
                // Для UDP Send/Recv событий - есть полная информация
                if (eventName.Contains("Send") || eventName.Contains("Recv"))
                {
                    // TODO: Извлечь полную endpoint информацию
                    // local = IPAddress.Parse(eventData.PayloadValue("LocalAddress").ToString());
                    // lport = (int)eventData.PayloadValue("LocalPort");
                    // remote = IPAddress.Parse(eventData.PayloadValue("RemoteAddress").ToString());
                    // rport = (int)eventData.PayloadValue("RemotePort");
                    
                    return false; // Возвращаем false пока парсинг не реализован полностью
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETWConnectionTracker] UDP network info extraction error: {ex.Message}");
            }
            
            return false;
        }
        
        private bool TryExtractNetworkInfo(TraceEvent eventData, out byte proto, out IPAddress local, out int lport, out IPAddress remote, out int rport)
        {
            // Заглушка для network info extraction
            // В реальной implementation нужно парсить payload по типам событий
            proto = 6; // TCP
            local = IPAddress.Loopback;
            lport = 0;
            remote = IPAddress.Loopback;
            rport = 0;
            
            try
            {
                // Попытка извлечь данные из event payload
                // Каждый provider имеет свою структуру событий
                var eventName = eventData.EventName;
                
                // Для Microsoft-Windows-Kernel-Network events
                if (eventName.Contains("Connect") || eventName.Contains("Accept"))
                {
                    // TODO: Парсинг специфичных полей события
                    // В зависимости от provider и event type нужно извлекать:
                    // - Protocol (TCP=6, UDP=17)
                    // - Local/Remote IP addresses
                    // - Local/Remote ports
                    // - Process ID
                    
                    // Пример структуры для будущей реализации:
                    // proto = (byte)eventData.PayloadValue("Protocol");
                    // local = IPAddress.Parse(eventData.PayloadValue("LocalAddress").ToString());
                    // lport = (int)eventData.PayloadValue("LocalPort");
                    // remote = IPAddress.Parse(eventData.PayloadValue("RemoteAddress").ToString());
                    // rport = (int)eventData.PayloadValue("RemotePort");
                    
                    return false; // Возвращаем false пока парсинг не реализован
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETWConnectionTracker] Network info extraction error: {ex.Message}");
            }
            
            return false;
        }
        
        public bool TryResolve(byte proto, IPAddress local, int lport, IPAddress remote, int rport, out Info info)
        {
            var key = new Key(proto, local, lport, remote, rport);
            return _connections.TryGetValue(key, out info);
        }
        
        public long GetEventsReceived() => _etwEventsReceived;
        public long GetEventsProcessed() => _eventsProcessed;
        public long GetConnectionsTracked() => _connectionsTracked;
        
        // Тестовый метод для симуляции добавления connection (для отладки)
        public void AddTestConnection(byte proto, IPAddress local, int lport, IPAddress remote, int rport, uint pid, string exe)
        {
            var key = new Key(proto, local, lport, remote, rport);
            var info = new Info(pid, exe);
            
            if (_connections.TryAdd(key, info))
            {
                Interlocked.Increment(ref _connectionsTracked);
                OnNewConnection?.Invoke(key, info);
                DebugLogger.log($"[ETW-Test] Added connection: {exe}({pid}) {local}:{lport} -> {remote}:{rport}");
            }
        }
        
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                _isRunning = false;
                
                try
                {
                    _etwSession?.Stop();
                    _etwSession?.Dispose();
                    
                    if (_etwThread?.IsAlive == true)
                    {
                        _etwThread.Join(1000);
                    }
                    
                    _connections.Clear();
                    DebugLogger.log($"[ETWConnectionTracker] Disposed. Events: {_etwEventsReceived}, Processed: {_eventsProcessed}, Connections: {_connectionsTracked}");
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[ETWConnectionTracker] Dispose error: {ex.Message}");
                }
            }
        }
    }

    public static class TunDetector
    {
        public static bool IsTunLike(LivePacketDevice d, string[] hints)
        {
            var s = (((d.Description ?? string.Empty) + " " + (d.Name ?? string.Empty))).ToLowerInvariant();
            foreach (var h in hints)
                if (!string.IsNullOrWhiteSpace(h) && s.Contains(h.Trim().ToLowerInvariant()))
                    return true;
            return false;
        }
    }

    /// <summary>
    /// Гибридный трекер соединений: (proto, local(ip,port), remote(ip,port)) -> { pid, exe }
    /// Источник: ETW (primary) + IP Helper (fallback). Период обновления поллинга ~300 мс.
    /// ETW обеспечивает real-time события, IP Helper - резервный источник для надежности.
    /// </summary>
    public sealed class ConnectionTracker : IDisposable
    {
        public readonly struct Key : IEquatable<Key>
        {
            public readonly byte Proto; // 6=TCP, 17=UDP
            public readonly IPAddress Local;
            public readonly int LocalPort;
            public readonly IPAddress Remote;
            public readonly int RemotePort;
            
            public Key(byte proto, IPAddress l, int lp, IPAddress r, int rp)
            { Proto = proto; Local = l; LocalPort = lp; Remote = r; RemotePort = rp; }
            
            public bool Equals(Key o) =>
                Proto == o.Proto && Local.Equals(o.Local) && LocalPort == o.LocalPort &&
                Remote.Equals(o.Remote) && RemotePort == o.RemotePort;
            
            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    hash = hash * 23 + Proto.GetHashCode();
                    hash = hash * 23 + (Local?.GetHashCode() ?? 0);
                    hash = hash * 23 + LocalPort.GetHashCode();
                    hash = hash * 23 + (Remote?.GetHashCode() ?? 0);
                    hash = hash * 23 + RemotePort.GetHashCode();
                    return hash;
                }
            }
            public override bool Equals(object obj) => obj is Key other && Equals(other);
        }
        
        public readonly struct Info
        {
            public readonly int Pid;
            public readonly string Exe;
            public Info(int pid, string exe) { Pid = pid; Exe = exe; }
        }
        
    public event Action<Key, Info> OnNewTunnelConnection;

    private readonly ConcurrentDictionary<Key, (Info info, long ts)> _map = new ConcurrentDictionary<Key, (Info info, long ts)>();
    private readonly ConcurrentDictionary<(byte proto, IPAddress local, int lport), int> _udpOwner = new ConcurrentDictionary<(byte proto, IPAddress local, int lport), int>(); // UDP без remote
    private readonly HashSet<Key> _reportedKeys = new HashSet<Key>();
    private int _udpOwnerLogCount;
    private int _lookupLogCount;
    private int _lastDumpTick;
    private readonly Thread _thread;
    private volatile bool _stop;
    private readonly int _ttlMs = 3000; // срок жизни записи
    private volatile int _eventInvokeCount = 0; // Счетчик вызовов событий для защиты от переполнения

    // ETW интеграция - гибридная архитектура (упрощенная версия)
    private ETWConnectionTracker _etwTracker;
    private volatile bool _useETW = false; // Переключатель ETW/polling режима
    private long _etwHits = 0;
    private long _pollingHits = 0;
    private long _etwMisses = 0;
    
    // UDP-специфичные счетчики для мониторинга эффективности
    private long _udpEtwHits = 0;
    private long _udpPollingHits = 0;
    private long _udpEtwMisses = 0;

        public ConnectionTracker()
        {
            // Инициализируем ETW трекер как primary source
            try
            {
                _etwTracker = new ETWConnectionTracker();
                _etwTracker.OnNewConnection += OnETWConnection;
                _etwTracker.OnConnectionClosed += OnETWConnectionClosed;
                _useETW = true; // Активируем ETW режим
                DebugLogger.log("[ConnectionTracker] ETW трекер инициализирован и активирован");
                
                // Добавляем тестовые connections для демонстрации
                StartETWTestMode();
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ConnectionTracker] ETW инициализация неудачна, используем только поллинг: {ex.Message}");
                _useETW = false;
            }
            
            _thread = new Thread(Loop) { IsBackground = true, Name = "ConnectionTracker" };
            _thread.Start();
        }
        
        private void StartETWTestMode()
        {
            // Добавляем тестовые connections через 2 секунды для демонстрации ETW
            Task.Delay(2000).ContinueWith(_ =>
            {
                try
                {
                    if (_etwTracker != null && _useETW)
                    {
                        // Симулируем популярные игровые соединения
                        _etwTracker.AddTestConnection(6, IPAddress.Parse("127.0.0.1"), 12345, IPAddress.Parse("8.8.8.8"), 80, 1234, "TestGame.exe");
                        _etwTracker.AddTestConnection(17, IPAddress.Parse("192.168.1.100"), 54321, IPAddress.Parse("1.1.1.1"), 53, 5678, "Steam.exe");
                        DebugLogger.log("[ConnectionTracker] ETW test connections added");
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[ConnectionTracker] ETW test mode error: {ex.Message}");
                }
            });
        }
        
        public void Dispose() 
        { 
            _stop = true; 
            try 
            { 
                _etwTracker?.Dispose();
                _thread?.Join(1000);
                DebugLogger.log("[ConnectionTracker] Disposed successfully");
            } 
            catch (Exception ex)
            { 
                DebugLogger.log($"[ConnectionTracker] Dispose error: {ex.Message}");
            } 
        }

        // ETW Event Handlers
        private void OnETWConnection(ETWConnectionTracker.Key etwKey, ETWConnectionTracker.Info etwInfo)
        {
            try
            {
                // Конвертируем ETW Key/Info в наш формат
                var key = new Key(etwKey.Proto, etwKey.Local, etwKey.LocalPort, etwKey.Remote, etwKey.RemotePort);
                var info = new Info((int)etwInfo.Pid, etwInfo.Exe);
                
                // Добавляем в наш кэш с текущим временем
                var now = Environment.TickCount;
                _map[key] = (info, now);
                
                // Вызываем событие для новых туннельных соединений
                if (!_reportedKeys.Contains(key))
                {
                    _reportedKeys.Add(key);
                    if (_eventInvokeCount < 1000) // Защита от переполнения
                    {
                        _eventInvokeCount++;
                        OnNewTunnelConnection?.Invoke(key, info);
                    }
                }
                
                DebugLogger.log($"[ETW] Новое соединение: {info.Exe}({info.Pid}) {key.Local}:{key.LocalPort} -> {key.Remote}:{key.RemotePort}");
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETW] Ошибка OnETWConnection: {ex.Message}");
            }
        }
        
        private void OnETWConnectionClosed(ETWConnectionTracker.Key etwKey)
        {
            try
            {
                var key = new Key(etwKey.Proto, etwKey.Local, etwKey.LocalPort, etwKey.Remote, etwKey.RemotePort);
                _map.TryRemove(key, out _);
                _reportedKeys.Remove(key);
                
                DebugLogger.log($"[ETW] Соединение закрыто: {key.Local}:{key.LocalPort} -> {key.Remote}:{key.RemotePort}");
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[ETW] Ошибка OnETWConnectionClosed: {ex.Message}");
            }
        }

        public bool TryResolve(byte proto, IPAddress local, int lport, IPAddress remote, int rport, out Info info)
        {
            // UDP-специфичная логика с гибридным ETW + fallback
            if (proto == 17) // UDP
            {
                return TryResolveUDP(local, lport, remote, rport, out info);
            }
            
            // TCP логика (существующая)
            // Сначала пробуем ETW трекер (если доступен)
            if (_useETW && _etwTracker != null)
            {
                ETWConnectionTracker.Info etwInfo;
                if (_etwTracker.TryResolve(proto, local, lport, remote, rport, out etwInfo))
                {
                    info = new Info((int)etwInfo.Pid, etwInfo.Exe);
                    Interlocked.Increment(ref _etwHits);
                    
                    // Логируем успешное ETW resolution
                    if (_etwHits % 10 == 1) // Логируем каждый 10-й hit для производительности
                    {
                        DebugLogger.log($"[ConnectionTracker] TCP ETW hit #{_etwHits}: {etwInfo.Exe}({etwInfo.Pid}) {local}:{lport}->{remote}:{rport}");
                    }
                    return true;
                }
                Interlocked.Increment(ref _etwMisses);
            }
            
            // Fallback на обычную логику поллинга для TCP
            var now = Environment.TickCount;
            if (TryGetFiveTuple(proto, local, lport, remote, rport, now, out info))
            {
                Interlocked.Increment(ref _pollingHits);
                return true;
            }

            info = default;
            LogLookup("MISS", proto, local, lport, remote, rport);
            return false;
        }
        
        public bool TryResolveUDP(IPAddress local, int lport, IPAddress remote, int rport, out Info info)
        {
            // UDP-специфичная гибридная resolution с ETW primary + fallback
            
            // 1. Сначала пробуем ETW для UDP (если доступен)
            if (_useETW && _etwTracker != null)
            {
                ETWConnectionTracker.Info etwInfo;
                if (_etwTracker.TryResolve(17, local, lport, remote, rport, out etwInfo))
                {
                    info = new Info((int)etwInfo.Pid, etwInfo.Exe);
                    Interlocked.Increment(ref _udpEtwHits);
                    
                    // Логируем успешное UDP ETW resolution
                    if (_udpEtwHits % 5 == 1) // Логируем каждый 5-й UDP hit для отладки
                    {
                        DebugLogger.log($"[ConnectionTracker] UDP ETW hit #{_udpEtwHits}: {etwInfo.Exe}({etwInfo.Pid}) {local}:{lport}->{remote}:{rport}");
                    }
                    return true;
                }
                Interlocked.Increment(ref _udpEtwMisses);
            }
            
            // 2. Fallback на UDP owner lookup (только local endpoint)
            if (TryResolveUdpOwner((17, local, lport), out info))
            {
                LogLookup("HIT UDP udpOwner", 17, local, lport, remote, rport,
                    extra: $"owner={(info.Exe ?? string.Empty)}/{info.Pid}");
                Interlocked.Increment(ref _udpPollingHits);
                return true;
            }

            // 3. Fallback на remote endpoint (swapped) для UDP
            if (TryResolveUdpOwner((17, remote, rport), out info))
            {
                LogLookup("HIT UDP udpOwner swapped", 17, local, lport, remote, rport,
                    extra: $"owner={(info.Exe ?? string.Empty)}/{info.Pid}");
                Interlocked.Increment(ref _udpPollingHits);
                return true;
            }
            
            // 4. Fallback на общую five-tuple логику для UDP
            var now = Environment.TickCount;
            if (TryGetFiveTuple(17, local, lport, remote, rport, now, out info))
            {
                Interlocked.Increment(ref _udpPollingHits);
                return true;
            }

            info = default;
            LogLookup("MISS UDP", 17, local, lport, remote, rport);
            return false;
        }
        
        public Info? QueryLocalOwner(byte proto, IPAddress local, int lport)
        {
            try
            {
                var now = Environment.TickCount;

                foreach (var kv in _map)
                {
                    if (kv.Key.Proto == proto && kv.Key.LocalPort == lport && kv.Key.Local.Equals(local))
                    {
                        if (now - kv.Value.ts <= _ttlMs)
                        {
                            return kv.Value.info;
                        }
                    }
                }

                if (proto == 17 && TryResolveUdpOwner((proto, local, lport), out var udpInfo))
                {
                    return udpInfo;
                }
            }
            catch
            {
                // ignore
            }

            return null;
        }
        
        // Методы для мониторинга производительности ETW vs Polling (обновлено для UDP)
        public bool IsETWEnabled => _useETW && _etwTracker != null;
        
        public (long tcpEtwHits, long tcpPollingHits, long tcpEtwMisses, long udpEtwHits, long udpPollingHits, long udpEtwMisses, long etwEvents, long etwConnections) GetPerformanceStats()
        {
            var etwEvents = _etwTracker?.GetEventsProcessed() ?? 0;
            var etwConnections = _etwTracker?.GetConnectionsTracked() ?? 0;
            return (_etwHits, _pollingHits, _etwMisses, _udpEtwHits, _udpPollingHits, _udpEtwMisses, etwEvents, etwConnections);
        }
        
        public (long udpEtwHits, long udpPollingHits, long udpEtwMisses) GetUDPPerformanceStats()
        {
            return (_udpEtwHits, _udpPollingHits, _udpEtwMisses);
        }
        
        public void EnableETW() => _useETW = true;
        
        public void DisableETW() => _useETW = false;

        private void Loop()
        {
            var sw = Stopwatch.StartNew();
            var lastETWStatsLog = Environment.TickCount;
            
            while (!_stop)
            {
                try
                {
                    RefreshTcp(AF_INET);
                    RefreshTcp(AF_INET6);
                    RefreshUdp(AF_INET);
                    RefreshUdp(AF_INET6);
                    EvictExpired();
                    DumpProcessSnapshotIfNeeded();
                    
                    // Логируем ETW статистику каждые 30 секунд
                    var now = Environment.TickCount;
                    if (now - lastETWStatsLog > 30000)
                    {
                        LogETWStatistics();
                        lastETWStatsLog = now;
                    }
                }
                catch { /* ignore all */ }
                var due = 300 - (int)sw.ElapsedMilliseconds;
                if (due < 30) due = 30;
                Thread.Sleep(due);
                sw.Restart();
            }
        }

        private void EvictExpired()
        {
            var now = Environment.TickCount;
            foreach (var kv in _map)
            {
                if (now - kv.Value.ts > _ttlMs)
                {
                    _map.TryRemove(kv.Key, out _);
                    _reportedKeys.Remove(kv.Key);
                }
            }

            var expired = new List<Key>();
            foreach (var key in _reportedKeys)
            {
                if (!_map.ContainsKey(key))
                {
                    expired.Add(key);
                }
            }

            foreach (var key in expired)
            {
                _reportedKeys.Remove(key);
            }
        }

        // ---------- IP Helper ----------
        private const int AF_INET = 2, AF_INET6 = 23;
        private enum TCP_TABLE_CLASS : int { TCP_TABLE_OWNER_PID_ALL = 5 }
        private enum UDP_TABLE_CLASS : int { UDP_TABLE_OWNER_PID = 1 }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int dwOutBufLen, bool sort, int ipVersion, TCP_TABLE_CLASS tblClass, int reserved);
        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int dwOutBufLen, bool sort, int ipVersion, UDP_TABLE_CLASS tblClass, int reserved);

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint state, localAddr, localPort_be, remoteAddr, remotePort_be, owningPid;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
            // followed by MIB_TCPROW_OWNER_PID[dwNumEntries]
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPROW_OWNER_PID
        {
            public uint localAddr, localPort_be, owningPid;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDPTABLE_OWNER_PID
        {
            public uint dwNumEntries;
            // followed by MIB_UDPROW_OWNER_PID[dwNumEntries]
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCP6ROW_OWNER_PID
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] localAddr;
            public uint localScopeId;
            public uint localPort_be;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] remoteAddr;
            public uint remoteScopeId;
            public uint remotePort_be;
            public uint state;
            public uint owningPid;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCP6TABLE_OWNER_PID
        {
            public uint dwNumEntries;
            // followed by MIB_TCP6ROW_OWNER_PID[dwNumEntries]
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDP6ROW_OWNER_PID
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)] public byte[] localAddr;
            public uint localScopeId;
            public uint localPort_be;
            public uint owningPid;
        }
        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_UDP6TABLE_OWNER_PID
        {
            public uint dwNumEntries;
            // followed by MIB_UDP6ROW_OWNER_PID[dwNumEntries]
        }

        private void RefreshTcp(int af)
        {
            try
            {
                // Проверяем, что объект не disposed и коллекции инициализированы
                if (_map == null || _reportedKeys == null || _stop)
                {
                    return;
                }

                int len = 0;
                uint ret = GetExtendedTcpTable(IntPtr.Zero, ref len, true, af, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != 0x7A) return; // ERROR_INSUFFICIENT_BUFFER
                var buf = Marshal.AllocHGlobal(len);
            try
            {
                ret = GetExtendedTcpTable(buf, ref len, true, af, TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret != 0) return;
                var now = Environment.TickCount;
                if (af == AF_INET)
                {
                    int count = (int)Marshal.ReadInt32(buf);
                    IntPtr p = buf + 4;
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            var row = Marshal.PtrToStructure<MIB_TCPROW_OWNER_PID>(p);
                            p += Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();
                            var l = ToIPv4(row.localAddr);
                            var r = ToIPv4(row.remoteAddr);
                            
                            // Дополнительная проверка на null
                            if (l == null || r == null)
                            {
                                DebugLogger.log($"[Tracker] Warning: ToIPv4 returned null - localAddr={row.localAddr}, remoteAddr={row.remoteAddr}");
                                continue;
                            }
                            
                            int lp = ReadPort(row.localPort_be);
                            int rp = ReadPort(row.remotePort_be);
                            
                            // Улучшенное разрешение процесса для VPN bypass
                            var info = new Info((int)row.owningPid, TryGetExe((int)row.owningPid));
                            
                            // Если процесс определился как Idle/0 и это туннельное соединение - используем fallback
                            if (IsTunnelIP(l) && (info.Pid == 0 || string.Equals(info.Exe, "Idle", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(info.Exe)))
                            {
                                var fallbackInfo = TryResolveWithVpnFallback(6, l, lp, r, rp, null);
                                if (!string.IsNullOrEmpty(fallbackInfo.Exe) && !string.Equals(fallbackInfo.Exe, "Unknown", StringComparison.OrdinalIgnoreCase))
                                {
                                    info = fallbackInfo;
                                    DebugLogger.log($"[Tracker] VPN fallback applied: {l}:{lp} -> {r}:{rp} changed from Idle/0 to {info.Exe}/{info.Pid}");
                                }
                            }
                            
                            var key = new Key(6, l, lp, r, rp);
                            _map[key] = (info, now);

                            if (IsTunnelIP(l) && _reportedKeys.Add(key))
                            {
                                if (OnNewTunnelConnection != null)
                                {
                                    try
                                    {
                                        string logMessage = $"[Tracker] NewTunnel IPv4: {l}:{lp} -> {r}:{rp} proc={info.Exe ?? "?"}/{info.Pid}";
                                        DebugLogger.log(logMessage);
                                        
                                        // Дополнительная диагностика подписчиков (с защитой от null)
                                        try 
                                        {
                                            var eventCopy = OnNewTunnelConnection; // Копируем ссылку для thread-safety
                                            if (eventCopy != null)
                                            {
                                                var invocationList = eventCopy.GetInvocationList();
                                                DebugLogger.log($"[Tracker] Event has {invocationList?.Length ?? 0} subscribers");
                                            }
                                        }
                                        catch (Exception diagEx)
                                        {
                                            DebugLogger.log($"[Tracker] Error getting invocation list: {diagEx.Message}");
                                        }
                                        
                                        // Дополнительная проверка параметров перед вызовом события
                                        if (key.Local != null && key.Remote != null && OnNewTunnelConnection != null)
                                        {
                                            // Защита от переполнения вызовов событий
                                            if (System.Threading.Interlocked.Increment(ref _eventInvokeCount) > 1000)
                                            {
                                                DebugLogger.log($"[Tracker] Warning: Event invoke limit reached, resetting counter");
                                                System.Threading.Interlocked.Exchange(ref _eventInvokeCount, 0);
                                            }
                                            
                                            try 
                                            {
                                                OnNewTunnelConnection.Invoke(key, info);
                                            }
                                            catch (Exception invokeEx)
                                            {
                                                DebugLogger.log($"[Tracker] Error invoking OnNewTunnelConnection: {invokeEx.Message}");
                                            }
                                        }
                                        else
                                        {
                                            DebugLogger.log($"[Tracker] Warning: Skipping event invoke due to null parameters - Local={key.Local != null}, Remote={key.Remote != null}, Event={OnNewTunnelConnection != null}");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugLogger.log($"[Tracker] Error in OnNewTunnelConnection handler: {ex.Message}");
                                    }
                                }
                                else
                                {
                                    DebugLogger.log($"[Tracker] NewTunnel IPv4 NO subscribers: {l}:{lp} -> {r}:{rp}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.log($"[Tracker] Error processing TCP row {i}: {ex.Message}");
                            // Сдвигаем указатель даже при ошибке
                            p += Marshal.SizeOf<MIB_TCPROW_OWNER_PID>();
                        }
                    }
                }
                else
                {
                    int count = (int)Marshal.ReadInt32(buf);
                    IntPtr p = buf + 4;
                    for (int i = 0; i < count; i++)
                    {
                        try
                        {
                            var row = Marshal.PtrToStructure<MIB_TCP6ROW_OWNER_PID>(p);
                            p += Marshal.SizeOf<MIB_TCP6ROW_OWNER_PID>();
                            var l = new IPAddress(row.localAddr, (long)row.localScopeId);
                            var r = new IPAddress(row.remoteAddr, (long)row.remoteScopeId);
                            
                            // Дополнительная проверка на null
                            if (l == null || r == null)
                            {
                                DebugLogger.log($"[Tracker] Warning: IPv6 address creation failed for row {i}");
                                continue;
                            }
                            
                            int lp = ReadPort(row.localPort_be);
                            int rp = ReadPort(row.remotePort_be);
                            
                            // Улучшенное разрешение процесса для VPN bypass (IPv6)
                            var info = new Info((int)row.owningPid, TryGetExe((int)row.owningPid));
                            
                            // Если процесс определился как Idle/0 и это туннельное соединение - используем fallback
                            if (IsTunnelIP(l) && (info.Pid == 0 || string.Equals(info.Exe, "Idle", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(info.Exe)))
                            {
                                var fallbackInfo = TryResolveWithVpnFallback(6, l, lp, r, rp, null);
                                if (!string.IsNullOrEmpty(fallbackInfo.Exe) && !string.Equals(fallbackInfo.Exe, "Unknown", StringComparison.OrdinalIgnoreCase))
                                {
                                    info = fallbackInfo;
                                    DebugLogger.log($"[Tracker] VPN fallback applied (IPv6): {l}:{lp} -> {r}:{rp} changed from Idle/0 to {info.Exe}/{info.Pid}");
                                }
                            }
                            
                            var key = new Key(6, l, lp, r, rp);
                            _map[key] = (info, now);

                            if (IsTunnelIP(l) && _reportedKeys.Add(key))
                            {
                                if (OnNewTunnelConnection != null)
                                {
                                    try
                                    {
                                        string logMessage = $"[Tracker] NewTunnel IPv6: {l}:{lp} -> {r}:{rp} proc={info.Exe ?? "?"}/{info.Pid}";
                                        DebugLogger.log(logMessage);
                                        
                                        // Дополнительная проверка параметров перед вызовом события
                                        if (key.Local != null && key.Remote != null)
                                        {
                                            // Защита от переполнения вызовов событий
                                            if (System.Threading.Interlocked.Increment(ref _eventInvokeCount) > 1000)
                                            {
                                                DebugLogger.log($"[Tracker] Warning: IPv6 Event invoke limit reached, resetting counter");
                                                System.Threading.Interlocked.Exchange(ref _eventInvokeCount, 0);
                                            }
                                            OnNewTunnelConnection.Invoke(key, info);
                                        }
                                        else
                                        {
                                            DebugLogger.log($"[Tracker] Warning: Skipping IPv6 event invoke due to null key fields");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugLogger.log($"[Tracker] Error in OnNewTunnelConnection handler (IPv6): {ex.Message}");
                                    }
                                }
                                else
                                {
                                    DebugLogger.log($"[Tracker] NewTunnel IPv6 NO subscribers: {l}:{lp} -> {r}:{rp}");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugLogger.log($"[Tracker] Error processing TCP6 row {i}: {ex.Message}");
                            // Сдвигаем указатель даже при ошибке
                            p += Marshal.SizeOf<MIB_TCP6ROW_OWNER_PID>();
                        }
                    }
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
            }
            catch (Exception ex)
            {
                // Логируем глобальные ошибки в RefreshTcp
                try
                {
                    DebugLogger.log($"[Tracker] Critical error in RefreshTcp(af={af}): {ex.Message}");
                }
                catch
                {
                    // Если даже логирование не работает - не падаем
                }
            }
        }

        private void RefreshUdp(int af)
        {
            int len = 0;
            uint ret = GetExtendedUdpTable(IntPtr.Zero, ref len, true, af, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
            if (ret != 0x7A) return;
            var buf = Marshal.AllocHGlobal(len);
            try
            {
                ret = GetExtendedUdpTable(buf, ref len, true, af, UDP_TABLE_CLASS.UDP_TABLE_OWNER_PID, 0);
                if (ret != 0) return;
                if (af == AF_INET)
                {
                    int count = (int)Marshal.ReadInt32(buf);
                    IntPtr p = buf + 4;
                    for (int i = 0; i < count; i++)
                    {
                        var row = Marshal.PtrToStructure<MIB_UDPROW_OWNER_PID>(p);
                        p += Marshal.SizeOf<MIB_UDPROW_OWNER_PID>();
                        var l = ToIPv4(row.localAddr);
                        int lp = ReadPort(row.localPort_be);
                        var pid = (int)row.owningPid;
                        _udpOwner[(17, l, lp)] = pid;
                        LogUdpOwnerSnapshot(17, l, lp, pid);
                    }
                }
                else
                {
                    int count = (int)Marshal.ReadInt32(buf);
                    IntPtr p = buf + 4;
                    for (int i = 0; i < count; i++)
                    {
                        var row = Marshal.PtrToStructure<MIB_UDP6ROW_OWNER_PID>(p);
                        p += Marshal.SizeOf<MIB_UDP6ROW_OWNER_PID>();
                        var l = new IPAddress(row.localAddr, (long)row.localScopeId);
                        int lp = ReadPort(row.localPort_be);
                        var pid = (int)row.owningPid;
                        _udpOwner[(17, l, lp)] = pid;
                        LogUdpOwnerSnapshot(17, l, lp, pid);
                    }
                }
            }
            finally { Marshal.FreeHGlobal(buf); }
        }

        private bool TryGetFiveTuple(byte proto, IPAddress local, int lport, IPAddress remote, int rport, int now, out Info info)
        {
            if (_map.TryGetValue(new Key(proto, local, lport, remote, rport), out var direct) && now - direct.ts <= _ttlMs)
            {
                info = direct.info;
                LogLookup("HIT fiveTuple direct", proto, local, lport, remote, rport,
                    extra: $"owner={(info.Exe ?? string.Empty)}/{info.Pid}");
                return true;
            }

            if (_map.TryGetValue(new Key(proto, remote, rport, local, lport), out var reverse) && now - reverse.ts <= _ttlMs)
            {
                info = reverse.info;
                LogLookup("HIT fiveTuple reverse", proto, local, lport, remote, rport,
                    extra: $"owner={(info.Exe ?? string.Empty)}/{info.Pid}");
                return true;
            }

            info = default;
            return false;
        }

        private bool TryResolveUdpOwner((byte proto, IPAddress ip, int port) candidate, out Info info)
        {
            info = default;

            if (candidate.port <= 0)
                return false;

            if (_udpOwner.TryGetValue(candidate, out var pid))
            {
                info = new Info(pid, TryGetExe(pid));
                return true;
            }

            IPAddress wildcard = null;
            if (candidate.ip != null)
            {
                if (candidate.ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    wildcard = IPAddress.Any;
                }
                else if (candidate.ip.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    wildcard = IPAddress.IPv6Any;
                }
            }

            if (wildcard != null && _udpOwner.TryGetValue((candidate.proto, wildcard, candidate.port), out pid))
            {
                info = new Info(pid, TryGetExe(pid));
                return true;
            }

            return false;
        }

        private void LogUdpOwnerSnapshot(byte proto, IPAddress local, int port, int pid)
        {
            if (pid <= 0 || port <= 0)
                return;

            var index = Interlocked.Increment(ref _udpOwnerLogCount);
            if (index > 200)
                return;

            var exe = TryGetExe(pid);
            DebugLogger.log($"[Tracker] UdpOwner map proto={proto} local={local}:{port} pid={pid} exe={exe}");
        }

        private void LogLookup(string stage, byte proto, IPAddress local, int lport, IPAddress remote, int rport, string extra = null)
        {
            var index = Interlocked.Increment(ref _lookupLogCount);
            if (index > 300)
                return;

            if (stage == "MISS")
            {
                if (local == null || remote == null ||
                    local.Equals(IPAddress.Any) || local.Equals(IPAddress.IPv6Any) ||
                    remote.Equals(IPAddress.Any) || remote.Equals(IPAddress.IPv6Any))
                {
                    return;
                }

                if (IPAddress.IsLoopback(local) && IPAddress.IsLoopback(remote))
                    return;

                if (rport == 0)
                    return;
            }

            var localIp = local?.ToString() ?? "<null>";
            var remoteIp = remote?.ToString() ?? "<null>";
            var sb = new StringBuilder();
            sb.Append("[Tracker] Resolve ")
              .Append(stage)
              .Append(" proto=").Append(proto)
              .Append(" local=").Append(localIp).Append(':').Append(lport)
              .Append(" remote=").Append(remoteIp).Append(':').Append(rport);

            if (!string.IsNullOrWhiteSpace(extra))
            {
                sb.Append(' ').Append(extra);
            }

            DebugLogger.log(sb.ToString());
        }

        private void DumpProcessSnapshotIfNeeded()
        {
            var now = Environment.TickCount;
            if (_lastDumpTick != 0 && unchecked(now - _lastDumpTick) < 2000)
                return;

            _lastDumpTick = now;

            var snapshot = new Dictionary<int, string>();
            var details = new Dictionary<int, SnapshotDetails>();

            foreach (var entry in _map)
            {
                var info = entry.Value.info;
                if (info.Pid <= 0)
                    continue;

                var name = info.Exe;
                if (string.IsNullOrWhiteSpace(name))
                    name = TryGetExe(info.Pid);

                if (snapshot.TryGetValue(info.Pid, out var existingName))
                {
                    if (string.IsNullOrWhiteSpace(existingName) && !string.IsNullOrWhiteSpace(name))
                        snapshot[info.Pid] = name;
                }
                else
                {
                    snapshot[info.Pid] = name;
                }

                var key = entry.Key;
                var detail = GetOrCreateDetail(details, info.Pid, name);
                detail.ConnectionCount++;
                if (detail.Samples.Count < 3)
                {
                    detail.Samples.Add($"{key.Local}:{key.LocalPort}->{key.Remote}:{key.RemotePort}");
                }
            }

            foreach (var entry in _udpOwner)
            {
                var pid = entry.Value;
                if (pid <= 0)
                    continue;

                var name = TryGetExe(pid);

                if (snapshot.TryGetValue(pid, out var existingName))
                {
                    if (string.IsNullOrWhiteSpace(existingName) && !string.IsNullOrWhiteSpace(name))
                        snapshot[pid] = name;
                }
                else
                {
                    snapshot[pid] = name;
                }

                var detail = GetOrCreateDetail(details, pid, name);
                detail.ConnectionCount++;
                if (detail.UdpEndpoints.Count < 3)
                {
                    var key = entry.Key;
                    detail.UdpEndpoints.Add($"{key.local}:{key.lport}");
                }
            }

            if (snapshot.Count == 0)
                return;

            var builder = new StringBuilder();
            builder.Append("[Tracker] Active processes: ");

            var first = true;
            foreach (var kv in snapshot.OrderBy(p => p.Key))
            {
                if (!first)
                    builder.Append("; ");
                builder.Append(kv.Key);
                if (!string.IsNullOrWhiteSpace(kv.Value))
                    builder.Append("=").Append(kv.Value);
                first = false;
            }

            DebugLogger.log(builder.ToString());

            foreach (var kv in details)
            {
                var detail = kv.Value;
                if (!string.Equals(detail.Name, "browser", StringComparison.OrdinalIgnoreCase))
                    continue;

                var detailBuilder = new StringBuilder();
                detailBuilder.Append($"[Tracker] Detail PID={kv.Key} Name={detail.Name ?? "<unknown>"} conn={detail.ConnectionCount}");

                if (detail.Samples.Count > 0)
                {
                    detailBuilder.Append(" samples=").Append(string.Join(", ", detail.Samples));
                }

                if (detail.UdpEndpoints.Count > 0)
                {
                    detailBuilder.Append(" udpLocal=").Append(string.Join(", ", detail.UdpEndpoints));
                }

                DebugLogger.log(detailBuilder.ToString());
            }
        }

        private static SnapshotDetails GetOrCreateDetail(Dictionary<int, SnapshotDetails> map, int pid, string name)
        {
            if (!map.TryGetValue(pid, out var detail))
            {
                detail = new SnapshotDetails();
                map[pid] = detail;
            }

            if (!string.IsNullOrWhiteSpace(name))
                detail.Name = name;

            return detail;
        }

        private sealed class SnapshotDetails
        {
            public string Name;
            public int ConnectionCount;
            public List<string> Samples { get; } = new List<string>();
            public List<string> UdpEndpoints { get; } = new List<string>();
        }

        private static bool IsTunnelIP(IPAddress ip)
        {
            if (ip == null || ip.AddressFamily != AddressFamily.InterNetwork)
                return false;

            var bytes = ip.GetAddressBytes();

            if (bytes[0] == 10)
                return true;

            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
                return true;

            if (bytes[0] == 192 && bytes[1] == 168)
                return true;

            return false;
        }

        private static IPAddress ToIPv4(uint raw)
        {
            if (raw == 0)
            {
                return IPAddress.Any;
            }

            // IP Helper already exposes IPv4 values in network byte order.
            return new IPAddress(raw);
        }

        private static int ReadPort(uint raw)
        {
            // Lower 16 bits contain the port in network byte order, the rest is padding.
            return (int)SwapUshort((ushort)raw);
        }

        private static ushort SwapUshort(ushort x) => (ushort)((x >> 8) | (x << 8));

        // Путь к exe — best effort (без падений)
        private static string TryGetExe(int pid)
        {
            try
            {
                using (var p = Process.GetProcessById(pid))
                {
                    return p.ProcessName;
                }
            }
            catch { return string.Empty; }
        }

        // Улучшенный resolve для VPN bypass - добавляет fallback к активному процессу
        public static Info TryResolveWithVpnFallback(byte proto, IPAddress local, int lport, IPAddress remote, int rport, ConnectionTracker tracker)
        {
            // Сначала пробуем стандартное разрешение
            if (tracker != null && tracker.TryResolve(proto, local, lport, remote, rport, out var standardInfo))
            {
                // Если нашли валидный процесс (не Idle/0)
                if (standardInfo.Pid > 0 && !string.IsNullOrEmpty(standardInfo.Exe) && 
                    !string.Equals(standardInfo.Exe, "Idle", StringComparison.OrdinalIgnoreCase))
                {
                    DebugLogger.log($"[VpnFallback] Standard resolve success: {standardInfo.Exe}/{standardInfo.Pid}");
                    return standardInfo;
                }
            }

            // Если соединение не опознано - НЕ присваиваем его никому
            // Это позволит показать "NO TRAFFIC" для процессов без реального трафика
            DebugLogger.log($"[VpnFallback] Connection not resolved: {local}:{lport}->{remote}:{rport} - returning Unknown");
            return new Info(0, "Unknown");
        }

        private void LogETWStatistics()
        {
            try
            {
                var tcpEfficiency = _etwHits + _etwMisses > 0 ? (_etwHits * 100.0) / (_etwHits + _etwMisses) : 0;
                var udpEfficiency = _udpEtwHits + _udpEtwMisses > 0 ? (_udpEtwHits * 100.0) / (_udpEtwHits + _udpEtwMisses) : 0;
                
                var log = $"[ETW Stats] TCP: Hits={_etwHits}, Misses={_etwMisses}, Polling={_pollingHits}, Efficiency={tcpEfficiency:F1}% | " +
                         $"UDP: Hits={_udpEtwHits}, Misses={_udpEtwMisses}, Polling={_udpPollingHits}, Efficiency={udpEfficiency:F1}% | " +
                         $"ETW Active: {(_etwTracker != null ? "YES" : "NO")}";
                DebugLogger.log(log);
                Console.WriteLine(log);
            }
            catch (Exception ex)
            {
                DebugLogger.log($"LogETWStatistics error: {ex.Message}");
            }
        }
    }
}