using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using tickMeter.Classes; // Предполагается, что App и SettingsManager находятся здесь
using System.Windows.Forms; // <--- ДОБАВЛЕНО ДЛЯ Application.ProductVersion

namespace tickMeter
{
    public class TickMeterState
    {
        private int LastTicksCount = 0;
        private int _tickrate;
        private System.Timers.Timer MeterValidateTimer;
        private DateTime timeStamp = DateTime.MinValue; // Инициализация

        public bool IsTracking { get; set; } = false;
        public bool ConnectionsManagerFlag = false; // Используется ли ConnectionsManager

        public DateTime SessionStart { get; set; }
        public GameServer Server { get; set; }

        public string LocalIP { get; set; }
        private string _game;
        public string Game
        {
            get { return _game; }
            set { _game = value; }
        }

        public bool isBuiltInProfileActive = false;
        public bool isCustomProfileActive = false;

        public int AvgTickrate;
        public List<int> TicksHistory { get; set; }
        public List<float> tickTimeBuffer = new List<float>();
        public List<float> pingBuffer = new List<float>();
        public List<float> tickrateGraph = new List<float>();

        public int TickRate
        {
            get { return _tickrate; }
            set
            {
                _tickrate = value;
                SetMeterTimer(); // Таймер валидации активности
            }
        }

        public static DateTime Trim(DateTime date)
        {
            return new DateTime(date.Ticks - (date.Ticks % TimeSpan.TicksPerSecond), date.Kind);
        }

        public int avgStableTickrate = 0; // Попытка рассчитать "стабильный" тикрейт

        public int loss = 0;
        public int totalTicksCnt = 0;

        public void updateTicktimeBuffer(long packetTicks)
        {
            // Увеличиваем только если это действительно игровой tick-пакет
            totalTicksCnt++;
            if (tickTimeBuffer.Count > 511)
            {
                tickTimeBuffer.RemoveAt(0);
            }
            if (CurrentTimestamp != null)
            {
                float tickTime = (float)(packetTicks - CurrentTimestamp.Ticks) / 10000;
                if (OutputTickRate > 0)
                {
                    tickTime -= 500 / OutputTickRate;
                    if (tickTime < 0)
                    {
                        tickTime = 0;
                    }
                }
                tickTimeBuffer.Add(tickTime);
            }
        }

        public void SetMeterTimer()
        {
            if (MeterValidateTimer == null || !MeterValidateTimer.Enabled)
            {
                MeterValidateTimer = new System.Timers.Timer();
                MeterValidateTimer.Elapsed += MeterValidateTimerTick;
                MeterValidateTimer.Interval = 2000; // Проверка каждые 2 секунды
                MeterValidateTimer.AutoReset = true;
                MeterValidateTimer.Enabled = true;
            }
        }

        public TickMeterState()
        {
            // Инициализация буферов для графиков
            for (int i = 0; i < 513; i++)
            {
                tickTimeBuffer.Add(0);
                tickrateGraph.Add(0);
                pingBuffer.Add(30); // Начальное значение для графика пинга
            }
            Server = new GameServer(); // Инициализация сервера до Reset
            Reset(); // Сброс всех счетчиков и состояния
            SetMeterTimer();
            _game = ""; // инициализация поля в конструкторе
        }

        private void MeterValidateTimerTick(Object source, System.Timers.ElapsedEventArgs e)
        {
            // Если отслеживание активно, игра определена, но новых "тиков" не было
            if (IsTracking && Game != "" && LastTicksCount == TicksHistory.Count)
            {
                KillTimers(); // Остановить таймеры пинга и валидации
            }
            else if (IsTracking && Game != "" && Server != null) // Если есть активность
            {
                Server.SetPingTimer(); // Убедиться, что таймер пинга активен
            }
            LastTicksCount = TicksHistory.Count;
        }

        public DateTime CurrentTimestamp
        {
            get { return timeStamp; }
            set
            {
                if (!IsTracking) return;
                if (value.ToString() != timeStamp.ToString())
                {
                    OutputTickRate = TickRate;
                    AvgTickrate = (AvgTickrate + OutputTickRate) / 2;
                    if (avgStableTickrate == 0)
                    {
                        avgStableTickrate = OutputTickRate;
                    }
                    float ratio = ((float)avgStableTickrate / (float)AvgTickrate);

                    if (ratio < 1.5 && ratio > 0.5)
                    {
                        avgStableTickrate += (AvgTickrate + avgStableTickrate);
                        avgStableTickrate /= 3;
                    }
                    if (totalTicksCnt > 300)
                    {
                        avgStableTickrate = (int)Math.Round(avgStableTickrate / 5.0) * 5;
                        int dropped = avgStableTickrate - OutputTickRate;
                        if (dropped < 0) { dropped = 0; }
                        // Исправление: не допускаем loss > totalTicksCnt
                        loss += dropped;
                        if (loss > totalTicksCnt) loss = totalTicksCnt;
                    }

                    TicksHistory.Add(OutputTickRate);
                    if (tickrateGraph.Count > 511)
                    {
                        tickrateGraph.RemoveAt(0);
                    }
                    tickrateGraph.Add(OutputTickRate);
                    TickRateLog += timeStamp.ToString() + ";" + OutputTickRate.ToString() + Environment.NewLine;
                    TickRate = 0;

                    // --- Единый буфер для графика пинга: UDP > TCP > ICMP ---
                    int pingValue = 0;
                    if (Server != null && Server.IsUdpPingValid)
                        pingValue = (int)Math.Round(Server.UdpPing);
                    else if (Server != null && Server.Ping > 0 && Server.Ping < 10000)
                        pingValue = Server.Ping;
                    else if (Server != null && Server.IcmpPing > 0 && Server.IcmpPing < 10000)
                        pingValue = Server.IcmpPing;
                    else
                        pingValue = 0;

                    pingBuffer.Add(pingValue);
                    if (pingBuffer.Count > 512)
                        pingBuffer.RemoveAt(0);
                }
                timeStamp = value;
            }
        }

        public int OutputTickRate { get; set; } // Тикрейт, отображаемый пользователю
        public int UploadTraffic { get; set; } = 0;
        public int DownloadTraffic { get; set; } = 0;
        public string TickRateLog { get; set; } = "";

        // Forward TCP ping (current ping value) from GameServer
        public int TcpPing
        {
            get
            {
                return Server != null ? Server.Ping : 0;
            }
        }

        // Forward UDP ping validity from GameServer
        public bool IsUdpPingValid
        {
            get
            {
                return Server != null && Server.IsUdpPingValid;
            }
        }

        // Forward UDP ping string from GameServer
        public string GetUdpPingString()
        {
            return Server != null ? Server.GetUdpPingString() : "n/a";
        }

        // Forward ICMP ping (current ping value) from GameServer
        public int IcmpPing
        {
            get
            {
                return Server != null ? Server.IcmpPing : 0;
            }
        }

        public void Reset()
        {
            IsTracking = false;
            SessionStart = DateTime.Now;
            Game = "";
            TicksHistory = new List<int>();

            tickTimeBuffer.Clear();
            tickrateGraph.Clear();
            pingBuffer.Clear();
            for (int i = 0; i < 513; i++)
            {
                tickTimeBuffer.Add(0);
                tickrateGraph.Add(0);
                pingBuffer.Add(30);
            }

            UploadTraffic = 0;
            DownloadTraffic = 0;
            TickRate = 0;
            OutputTickRate = 0;
            totalTicksCnt = 0;
            loss = 0;
            AvgTickrate = 0;
            avgStableTickrate = 0;
            TickRateLog = "";
            timeStamp = DateTime.MinValue; // Сброс для корректной работы CurrentTimestamp
            Server?.Reset(); // Сброс состояния сервера
        }

        public void KillTimers()
        {
            if (MeterValidateTimer != null)
            {
                MeterValidateTimer.Enabled = false;
                MeterValidateTimer.Stop();
                MeterValidateTimer.Dispose();
                MeterValidateTimer = null;
                Debug.Print("killed meter timer");
            }
            Server?.KillTimer();
        }

        internal string GetDrops()
        {
            // Исправление: защита от деления на 0 и от аномалий
            if (totalTicksCnt == 0) return "0.00";
            float percent = Math.Min(100, (((float)loss / (float)totalTicksCnt) * 100));
            if (percent < 0) percent = 0;
            return percent.ToString("n2");
        }
        internal float GetDropsNumber()
        {
            if (totalTicksCnt == 0) return 0;
            float percent = Math.Min(100, (((float)loss / (float)totalTicksCnt) * 100));
            if (percent < 0) percent = 0;
            return percent;
        }

        public class GameServer
        {
            private string CurrentIP = "";
            public int PingPort { get; set; } = 0;
            private int gamePort;

            private const int PingLimitMilliseconds = 1000;
            private int _ping = 0;
            public int AvgPing { get; set; } = 0;
            public string Location { get; set; } = "";
            private System.Timers.Timer PingTimer;

            private List<int> UserDefinedFallbackPorts;
            private int currentFallbackPortIndex = -1;

            private int consecutivePingFails = 0;

            private bool isPinging = false;

            // --- UDP Ping fields ---
            private DateTime lastUdpPacketTime = DateTime.MinValue;
            private Queue<float> udpIntervals = new Queue<float>();
            private const int UdpIntervalsWindow = 10; // размер окна для сглаживания

            public float UdpPing
            {
                get
                {
                    if (udpIntervals.Count == 0)
                        return 0;
                    return udpIntervals.Average();
                }
            }
            public bool IsUdpPingValid => udpIntervals.Count > 0 && UdpPing > 0 && UdpPing < 1000;
            public string GetUdpPingString()
            {
                if (!IsUdpPingValid)
                    return "n/a";
                return UdpPing.ToString("0");
            }

            public void UpdateUdpPing(DateTime packetTime)
            {
                if (lastUdpPacketTime != DateTime.MinValue)
                {
                    float interval = (float)(packetTime - lastUdpPacketTime).TotalMilliseconds;
                    // Фильтр: только реальные интервалы (например, 5мс < x < 1000мс)
                    if (interval > 5 && interval < 1000)
                    {
                        udpIntervals.Enqueue(interval);
                        if (udpIntervals.Count > UdpIntervalsWindow)
                            udpIntervals.Dequeue();
                    }
                    // Добавим логирование каждого интервала и текущего среднего UDP ping
                    Debug.Print($"[UDP PING DEBUG] interval={interval} ms, avgUdpPing={UdpPing:0} ms, intervalsCount={udpIntervals.Count}");
                }
                else
                {
                    Debug.Print("[UDP PING DEBUG] first UDP packet");
                }
                lastUdpPacketTime = packetTime;
            }

            public void ResetUdpPing()
            {
                lastUdpPacketTime = DateTime.MinValue;
                udpIntervals.Clear();
            }

            public GameServer()
            {
                string ping_ports_setting = App.settingsManager.GetOption("ping_ports");
                if (!string.IsNullOrEmpty(ping_ports_setting))
                {
                    UserDefinedFallbackPorts = ping_ports_setting.Split(',')
                        .Select(p_str => int.TryParse(p_str.Trim(), out int p_val) ? p_val : -1)
                        .Where(p_val => p_val > 0 && p_val <= 65535)
                        .ToList();
                }
                else
                {
                    UserDefinedFallbackPorts = new List<int>() { 80 };
                }
            }

            public int Ping
            {
                get { return _ping; }
                set
                {
                    _ping = value;
                    // УДАЛЕНО: добавление в pingBuffer (теперь только через CurrentTimestamp)
                    /*
                    if (App.meterState != null)
                    {
                        App.meterState.pingBuffer.Add(_ping);
                        if (App.meterState.pingBuffer.Count > 512)
                        {
                            App.meterState.pingBuffer.RemoveAt(0);
                        }
                    }
                    */
                    if (AvgPing == 0 && _ping > 0) AvgPing = _ping;
                    else if (_ping > 0) AvgPing = (AvgPing + _ping) / 2;
                }
            }

            public string Ip
            {
                get { return CurrentIP; }
                set
                {
                    string oldIP = CurrentIP;
                    CurrentIP = value;
                    if (oldIP != CurrentIP && !string.IsNullOrEmpty(CurrentIP))
                    {
                        if (App.meterState != null)
                        {
                            App.meterState.totalTicksCnt = 0;
                            App.meterState.loss = 0;
                            App.meterState.avgStableTickrate = 0;
                            App.meterState.SessionStart = DateTime.Now;
                        }

                        if (PingTimer == null || !PingTimer.Enabled)
                        {
                            SetPingTimer();
                        }
                        DetectLocation();
                        consecutivePingFails = 0;
                        currentFallbackPortIndex = -1;
                        this.PingPort = this.GamePort > 0 ? this.GamePort : 0;
                    }
                }
            }

            public int GamePort
            {
                get => gamePort;
                set
                {
                    if (value > 0 && value <= 65535 && value != gamePort)
                    {
                        gamePort = value;
                        this.PingPort = gamePort;
                        consecutivePingFails = 0;
                        currentFallbackPortIndex = -1;
                        Debug.Print($"GamePort set to: {gamePort}. PingPort also set to {this.PingPort}.");
                    }
                    else if (value <= 0 && gamePort != 0)
                    {
                        gamePort = 0;
                        Debug.Print($"GamePort unset (was {this.PingPort}). PingPort remains {this.PingPort} for now.");
                    }
                }
            }

            internal void Reset()
            {
                CurrentIP = "";
                Location = "N/A";
                PingPort = 0;
                gamePort = 0;
                AvgPing = 0;
                _ping = 0;
                consecutivePingFails = 0;
                currentFallbackPortIndex = -1;
                KillTimer();
            }

            public void KillTimer()
            {
                if (PingTimer != null)
                {
                    PingTimer.Enabled = false;
                    PingTimer.Stop();
                    PingTimer.Dispose();
                    PingTimer = null;
                    Debug.Print("killed server timer");
                }
            }

            public void SetPingTimer()
            {
                ICMPfails = 0;
                int PingInterval = 2000;
                // Интервал пинга берется из настроек
                string intervalStr = App.settingsManager.GetOption("ping_interval");
                if (!string.IsNullOrEmpty(intervalStr))
                {
                    int parsed;
                    if (int.TryParse(intervalStr, out parsed) && parsed > 0)
                        PingInterval = parsed;
                }
                // Если таймер уже существует, просто обновляем интервал
                if (PingTimer != null)
                {
                    PingTimer.Interval = PingInterval;
                    PingTimer.Enabled = true;
                }
                else
                {
                    PingTimer = new System.Timers.Timer
                    {
                        Interval = PingInterval
                    };
                    PingTimer.Elapsed += PingServerTimer;
                    PingTimer.AutoReset = true;
                    PingTimer.Enabled = true;
                }
            }

            private async void PingServerTimer(Object source, System.Timers.ElapsedEventArgs e)
            {
                if (string.IsNullOrEmpty(Ip) || isPinging) return;

                isPinging = true;
                await Task.Run(() => { PingServer(); }).ConfigureAwait(false);
                isPinging = false;
            }

            private class IpInfo
            {
                [JsonProperty("ip")] public string Ip { get; set; }
                [JsonProperty("hostname")] public string Hostname { get; set; }
                [JsonProperty("city")] public string City { get; set; }
                [JsonProperty("region")] public string Region { get; set; }
                [JsonProperty("country")] public string Country { get; set; }
                [JsonProperty("loc")] public string Loc { get; set; }
                [JsonProperty("org")] public string Org { get; set; }
                [JsonProperty("postal")] public string Postal { get; set; }
            }

            private async void DetectLocation()
            {
                if (string.IsNullOrEmpty(Ip)) { Location = "N/A"; return; }

                string ipToDetect = Ip;
                await Task.Run(() =>
                {
                    IpInfo ipInfo = new IpInfo();
                    try
                    {
                        using (WebClient webClient = new WebClient())
                        {
                            webClient.Headers.Add("User-Agent", "tickMeter/" + System.Windows.Forms.Application.ProductVersion); // ИСПРАВЛЕНО
                            string info = webClient.DownloadString("http://ipinfo.io/" + ipToDetect + "/json");
                            ipInfo = JsonConvert.DeserializeObject<IpInfo>(info);
                            if (!string.IsNullOrEmpty(ipInfo.Country))
                            {
                                RegionInfo myRI1 = new RegionInfo(ipInfo.Country);
                                ipInfo.Country = myRI1.EnglishName;
                            }
                        }
                    }
                    catch (WebException wex)
                    {
                        DebugLogger.log($"DetectLocation WebException for IP {ipToDetect}: {wex.Message} (Status: {wex.Status})");
                        ipInfo.Country = "Error";
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.log($"DetectLocation Exception for IP {ipToDetect}: {ex.Message}");
                        ipInfo.Country = "Error";
                    }

                    if (this.CurrentIP == ipToDetect)
                    {
                        Location = ipInfo.Country ?? "N/A";
                        if (!string.IsNullOrEmpty(ipInfo.City) && ipInfo.Country != "Error" && ipInfo.Country != "N/A")
                        {
                            Location += ", " + ipInfo.City;
                        }
                    }
                }).ConfigureAwait(false);
            }

            public static IPEndPoint CreateIPEndPoint(string endPoint, int port)
            {
                if (port <= 0 || port > 65535)
                {
                    DebugLogger.log($"CreateIPEndPoint: Invalid port {port} for IP {endPoint}");
                    return null;
                }
                IPAddress ip;
                if (!IPAddress.TryParse(endPoint, out ip))
                {
                    DebugLogger.log($"CreateIPEndPoint: Invalid IP address {endPoint}");
                    return null;
                }
                return new IPEndPoint(ip, port);
            }

            // ICMP ping field and property
            private int _icmpPing = 0;
            public int IcmpPing
            {
                get { return _icmpPing; }
                set { _icmpPing = value; }
            }

            private int ICMPfails = 0;

            private int PingICMP()
            {
                if (string.IsNullOrEmpty(Ip)) return PingLimitMilliseconds;
                System.Net.NetworkInformation.Ping pingSender = null;
                try
                {
                    pingSender = new System.Net.NetworkInformation.Ping();
                    System.Net.NetworkInformation.PingReply pingReply = pingSender.Send(Ip, PingLimitMilliseconds);
                    if (pingReply.Status == System.Net.NetworkInformation.IPStatus.Success)
                    {
                        IcmpPing = (int)pingReply.RoundtripTime;
                        ICMPfails = 0;
                        return (int)pingReply.RoundtripTime;
                    }
                    else
                    {
                        ICMPfails++;
                        DebugLogger.log($"PingICMP to {Ip} failed with status: {pingReply.Status}");
                    }
                }
                catch (Exception ex)
                {
                    ICMPfails++;
                    DebugLogger.log($"PingICMP to {Ip} exception: {ex.Message}");
                    IcmpPing = PingLimitMilliseconds;
                    return PingLimitMilliseconds;
                }
                finally
                {
                    pingSender?.Dispose();
                }
                IcmpPing = PingLimitMilliseconds;
                return PingLimitMilliseconds;
            }

            private int PingSocket(int portToPing)
            {
                if (string.IsNullOrEmpty(Ip) || portToPing <= 0)
                {
                    return PingLimitMilliseconds + 1;
                }

                IPEndPoint ep = CreateIPEndPoint(Ip, portToPing);
                if (ep == null) return PingLimitMilliseconds + 1;

                using (Socket sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    Stopwatch stopwatch = new Stopwatch();
                    try
                    {
                        stopwatch.Start();
                        IAsyncResult result = sock.BeginConnect(ep, null, null);
                        bool success = result.AsyncWaitHandle.WaitOne(PingLimitMilliseconds, true);

                        if (success && sock.Connected)
                        {
                            sock.EndConnect(result);
                            stopwatch.Stop();
                            return (int)stopwatch.ElapsedMilliseconds;
                        }
                        else
                        {
                            sock.Close();
                            stopwatch.Stop();
                            DebugLogger.log($"PingSocket to {Ip}:{portToPing} timed out or failed to connect within {PingLimitMilliseconds}ms.");
                            return PingLimitMilliseconds;
                        }
                    }
                    catch (SocketException se)
                    {
                        DebugLogger.log($"PingSocket to {Ip}:{portToPing} SocketException: {se.Message} (Code: {se.SocketErrorCode})");
                        return PingLimitMilliseconds;
                    }
                    catch (Exception ex)
                    {
                        DebugLogger.log($"PingSocket to {Ip}:{portToPing} Exception: {ex.Message}");
                        return PingLimitMilliseconds;
                    }
                }
            }

            private void PingServer()
            {
                if (string.IsNullOrEmpty(Ip))
                {
                    isPinging = false;
                    return;
                }

                int icmpPingTime = PingICMP();
                Debug.Print($"ICMP Ping to {Ip}: {icmpPingTime}ms");

                int finalPingTime;

                if (icmpPingTime < PingLimitMilliseconds)
                {
                    finalPingTime = icmpPingTime;
                    consecutivePingFails = 0;
                }
                else
                {
                    int portToTryTcp = this.PingPort;
                    if (portToTryTcp <= 0 && this.GamePort > 0)
                    {
                        portToTryTcp = this.GamePort;
                    }

                    if (portToTryTcp > 0)
                    {
                        finalPingTime = PingSocket(portToTryTcp);
                        Debug.Print($"TCP Ping to {Ip}:{portToTryTcp} (primary/dynamic): {finalPingTime}ms");

                        if (finalPingTime >= PingLimitMilliseconds)
                        {
                            consecutivePingFails++;
                            if (UserDefinedFallbackPorts != null && UserDefinedFallbackPorts.Any() && consecutivePingFails > 2)
                            {
                                currentFallbackPortIndex++;
                                if (currentFallbackPortIndex >= UserDefinedFallbackPorts.Count)
                                {
                                    currentFallbackPortIndex = 0;
                                }
                                int fallbackPort = UserDefinedFallbackPorts[currentFallbackPortIndex];
                                this.PingPort = fallbackPort;
                                finalPingTime = PingSocket(fallbackPort);
                                Debug.Print($"TCP Ping to {Ip}:{fallbackPort} (fallback): {finalPingTime}ms");
                            }
                        }
                        else
                        {
                            consecutivePingFails = 0;
                        }
                    }
                    else
                    {
                        consecutivePingFails++;
                        if (UserDefinedFallbackPorts != null && UserDefinedFallbackPorts.Any() && consecutivePingFails > 2)
                        {
                            currentFallbackPortIndex++;
                            if (currentFallbackPortIndex >= UserDefinedFallbackPorts.Count)
                            {
                                currentFallbackPortIndex = 0;
                            }
                            int fallbackPort = UserDefinedFallbackPorts[currentFallbackPortIndex];
                            this.PingPort = fallbackPort;
                            finalPingTime = PingSocket(fallbackPort);
                            Debug.Print($"TCP Ping to {Ip}:{fallbackPort} (fallback, no dynamic port): {finalPingTime}ms");
                        }
                        else
                        {
                            finalPingTime = PingLimitMilliseconds;
                        }
                    }

                    if (finalPingTime < PingLimitMilliseconds) consecutivePingFails = 0;

                }

                Ping = finalPingTime;
            }
        }
    } // конец класса TickMeterState
} // конец пространства имен tickMeter