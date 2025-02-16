using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using tickMeter.Classes;
using System.Net.NetworkInformation;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Net;

namespace tickMeter
{
    public class ConnectionsManager
    {
        

        public static List<TcpProcessRecord> TcpActiveConnections { get; set; } = new List<TcpProcessRecord>();
        public static List<UdpProcessRecord> UdpActiveConnections { get; set; } = new List<UdpProcessRecord>();
        //SOURCES
        // https://code.msdn.microsoft.com/windowsdesktop/C-Sample-to-list-all-the-4817b58f
        // https://www.codeproject.com/Articles/4298/Getting-active-TCP-UDP-connections-on-a-box

        // The version of IP used by the TCP/UDP endpoint. AF_INET is used for IPv4.
        private const int AF_INET = 2;
        public const string dllFile = "iphlpapi.dll";
        public int timerInterval = 500;

        public Process[] ProcessInfoList;

        private System.Timers.Timer MngrTimer;


        private void SetConnectionsManagerTimer()
        {
            if (MngrTimer == null)
            {
                MngrTimer = new System.Timers.Timer
                {
                    Interval = timerInterval
                };
                MngrTimer.Elapsed += MngrTimerTick;
                MngrTimer.AutoReset = true;
                MngrTimer.Enabled = true;
            }
        }

        private async void MngrTimerTick(Object source, System.Timers.ElapsedEventArgs e)
        {
            if (App.meterState == null || !App.meterState.ConnectionsManagerFlag) return;
            await Task.Run(() =>
            {
                ProcessInfoList = Process.GetProcesses();
                TcpActiveConnections = GetAllTcpConnections();
                UdpActiveConnections = GetAllUdpConnections();
                Process[] proccArray;
                for (var i = 0; i < TcpActiveConnections.Count; i++)
                {
                    proccArray = ProcessInfoList.Where(process => TcpActiveConnections[i].ProcessId == process.Id).ToArray();
                    if (proccArray.Length > 0)
                    {
                        TcpActiveConnections[i].ProcessName = proccArray.First().ProcessName;
                    } else {
                        ETW.ProcessNetworkData procData = ETW.processes.Where(processData => TcpActiveConnections[i].ProcessId == processData.Value.pId).First().Value;
                        if (procData != null)
                        {
                            TcpActiveConnections[i].ProcessName = procData.pName;
                        }
                    }
                }

                for (var i = 0; i < UdpActiveConnections.Count; i++)
                {
                    proccArray = ProcessInfoList.Where(process => UdpActiveConnections[i].ProcessId == process.Id).ToArray();
                    if (proccArray.Length > 0)
                    {
                        UdpActiveConnections[i].ProcessName = proccArray.First().ProcessName;
                    } else {
                        ETW.ProcessNetworkData procData = ETW.processes.Where(processData => UdpActiveConnections[i].ProcessId == processData.Value.pId).First().Value;
                        if (procData != null)
                        {
                            UdpActiveConnections[i].ProcessName = procData.pName;
                        }
                    }
                }
            });
        }

        public ConnectionsManager(int timerInt = 5000)
        {
            timerInterval = timerInt;
            SetConnectionsManagerTimer();
        }

        [DllImport(dllFile, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint GetExtendedTcpTable(IntPtr pTcpTable, ref int pdwSize,
            bool bOrder, int ulAf, TcpTableClass tableClass, uint reserved = 0);


        [DllImport(dllFile, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern uint GetExtendedUdpTable(IntPtr pUdpTable, ref int pdwSize,
            bool bOrder, int ulAf, UdpTableClass tableClass, uint reserved = 0);


        public List<TcpProcessRecord> GetAllTcpConnections()
        {
            int bufferSize = 0;
            List<TcpProcessRecord> tcpTableRecords = new List<TcpProcessRecord>();

            uint result = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET,
                TcpTableClass.TCP_TABLE_OWNER_PID_ALL);

            IntPtr tcpTableRecordsPtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                result = GetExtendedTcpTable(tcpTableRecordsPtr, ref bufferSize, true,
                    AF_INET, TcpTableClass.TCP_TABLE_OWNER_PID_ALL);

                if (result != 0)
                    return new List<TcpProcessRecord>();

                MIB_TCPTABLE_OWNER_PID tcpRecordsTable = (MIB_TCPTABLE_OWNER_PID)
                                        Marshal.PtrToStructure(tcpTableRecordsPtr,
                                        typeof(MIB_TCPTABLE_OWNER_PID));
                IntPtr tableRowPtr = (IntPtr)((long)tcpTableRecordsPtr +
                                        Marshal.SizeOf(tcpRecordsTable.dwNumEntries));

                for (int row = 0; row < tcpRecordsTable.dwNumEntries; row++)
                {
                    MIB_TCPROW_OWNER_PID tcpRow = (MIB_TCPROW_OWNER_PID)Marshal.
                        PtrToStructure(tableRowPtr, typeof(MIB_TCPROW_OWNER_PID));

                    tcpTableRecords.Add(new TcpProcessRecord(
                                                new IPAddress(tcpRow.localAddr),
                                                new IPAddress(tcpRow.remoteAddr),
                                                BitConverter.ToUInt16(new byte[2] {
                                            tcpRow.localPort[1],
                                            tcpRow.localPort[0] }, 0),
                                                BitConverter.ToUInt16(new byte[2] {
                                            tcpRow.remotePort[1],
                                            tcpRow.remotePort[0] }, 0),
                                                tcpRow.owningPid, tcpRow.state));
                    tableRowPtr = (IntPtr)((long)tableRowPtr + Marshal.SizeOf(tcpRow));
                }
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                MessageBox.Show(outOfMemoryException.Message, "Out Of Memory",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Exception",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            finally
            {
                Marshal.FreeHGlobal(tcpTableRecordsPtr);
            }
            return tcpTableRecords != null ? tcpTableRecords.Distinct()
                .ToList<TcpProcessRecord>() : new List<TcpProcessRecord>();
        }

        public static void UpdateUdpActiveConnections()
        {
            // Очищаем старые данные
            UdpActiveConnections.Clear();

            // Используем уже существующий метод для получения соединений
            var udpConnections = new ConnectionsManager().GetAllUdpConnections();

            // Заполняем список
            foreach (var connection in udpConnections)
            {
                UdpActiveConnections.Add(connection);
            }
        }

        public static void UpdateTcpActiveConnections()
        {
            if ((DateTime.Now - lastVpnCheck).TotalSeconds > 10)
            {
                DetectVPNAndRealIP();
                lastVpnCheck = DateTime.Now;
            }

            // Очищаем список перед обновлением
            TcpActiveConnections.Clear();

            // Получаем соединения и добавляем их в список
            var connections = new ConnectionsManager().GetAllTcpConnections();
            foreach (var connection in connections)
            {
                TcpActiveConnections.Add(connection);
            }
        }

        public List<UdpProcessRecord> GetAllUdpConnections()
        {
            int bufferSize = 0;
            List<UdpProcessRecord> udpTableRecords = new List<UdpProcessRecord>();

            uint result = GetExtendedUdpTable(IntPtr.Zero, ref bufferSize, true,
                AF_INET, UdpTableClass.UDP_TABLE_OWNER_PID);

            IntPtr udpTableRecordPtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                result = GetExtendedUdpTable(udpTableRecordPtr, ref bufferSize, true,
                    AF_INET, UdpTableClass.UDP_TABLE_OWNER_PID);

                if (result != 0)
                    return new List<UdpProcessRecord>();
                MIB_UDPTABLE_OWNER_PID udpRecordsTable = (MIB_UDPTABLE_OWNER_PID)
                    Marshal.PtrToStructure(udpTableRecordPtr, typeof(MIB_UDPTABLE_OWNER_PID));

                IntPtr tableRowPtr = (IntPtr)((long)udpTableRecordPtr +
                    Marshal.SizeOf(udpRecordsTable.dwNumEntries));

                for (int i = 0; i < udpRecordsTable.dwNumEntries; i++)
                {

                    MIB_UDPROW_OWNER_PID udpRow = (MIB_UDPROW_OWNER_PID)
                        Marshal.PtrToStructure(tableRowPtr, typeof(MIB_UDPROW_OWNER_PID));
                    udpTableRecords.Add(new UdpProcessRecord(new IPAddress(udpRow.localAddr),
                        BitConverter.ToUInt16(new byte[2] { udpRow.localPort[1],
                            udpRow.localPort[0] }, 0), udpRow.owningPid));
                    tableRowPtr = (IntPtr)((long)tableRowPtr + Marshal.SizeOf(udpRow));
                }
            }
            catch (OutOfMemoryException outOfMemoryException)
            {
                MessageBox.Show(outOfMemoryException.Message, "Out Of Memory",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message, "Exception",
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
            finally
            {
                Marshal.FreeHGlobal(udpTableRecordPtr);
            }
            return udpTableRecords != null ? udpTableRecords.Distinct()
                .ToList<UdpProcessRecord>() : new List<UdpProcessRecord>();
        }


        private static string lastRealIP = null;
        private static DateTime lastVpnCheck = DateTime.MinValue;
        public static void DetectVPNAndRealIP()
        {
            try
            {
                if ((DateTime.Now - lastVpnCheck).TotalSeconds < 10)
                {
                    return; // Проверяем VPN не чаще, чем раз в 10 секунд
                }

                lastVpnCheck = DateTime.Now;

                NetworkInterface adapter = GetActiveNetworkAdapter();
                if (adapter == null) return;

                bool isVPN = IsVPNAdapter(adapter);
                Console.WriteLine($"Адаптер: {adapter.Name}, VPN: {isVPN}");

                if (!isVPN)
                {
                    Console.WriteLine("VPN не используется, пропускаем проверку маршрутов.");
                    return;
                }

                string realIP = GetRealIPFromRoutes();
                if (realIP != null && realIP != lastRealIP)
                {
                    Console.WriteLine($"Реальный внешний IP: {realIP}");
                    lastRealIP = realIP;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при определении VPN: {ex.Message}");
            }
        }


        public static NetworkInterface GetActiveNetworkAdapter()
        {
            foreach (var adapter in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (adapter.OperationalStatus == OperationalStatus.Up &&
                    adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                {
                    return adapter;
                }
            }
            return null;
        }

        public static bool IsVPNAdapter(NetworkInterface adapter)
        {
            string desc = adapter.Description.ToLower();
            return desc.Contains("vpn") || desc.Contains("tun") || desc.Contains("tap") || desc.Contains("wireguard");
        }



        public static string GetRealIPFromRoutes()
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c netsh interface ip show addresses",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = new Process { StartInfo = psi })
                {
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();

                    Regex regex = new Regex(@"\s*IP Address:\s*(\S+)");
                    Match match = regex.Match(output);

                    if (match.Success)
                    {
                        return match.Groups[1].Value; // Получаем реальный IP
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при получении маршрутов: {ex.Message}");
            }

            return null;
        }


    }

    public enum Protocol
    {
        TCP,
        UDP
    }

    public enum TcpTableClass
    {
        TCP_TABLE_BASIC_LISTENER,
        TCP_TABLE_BASIC_CONNECTIONS,
        TCP_TABLE_BASIC_ALL,
        TCP_TABLE_OWNER_PID_LISTENER,
        TCP_TABLE_OWNER_PID_CONNECTIONS,
        TCP_TABLE_OWNER_PID_ALL,
        TCP_TABLE_OWNER_MODULE_LISTENER,
        TCP_TABLE_OWNER_MODULE_CONNECTIONS,
        TCP_TABLE_OWNER_MODULE_ALL
    }

    public enum UdpTableClass
    {
        UDP_TABLE_BASIC,
        UDP_TABLE_OWNER_PID,
        UDP_TABLE_OWNER_MODULE
    }

    public enum MibTcpState
    {
        CLOSED = 1,
        LISTENING = 2,
        SYN_SENT = 3,
        SYN_RCVD = 4,
        ESTABLISHED = 5,
        FIN_WAIT1 = 6,
        FIN_WAIT2 = 7,
        CLOSE_WAIT = 8,
        CLOSING = 9,
        LAST_ACK = 10,
        TIME_WAIT = 11,
        DELETE_TCB = 12,
        NONE = 0
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPROW_OWNER_PID
    {
        public MibTcpState state;
        public uint localAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] localPort;
        public uint remoteAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] remotePort;
        public int owningPid;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_TCPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 1)]
        public MIB_TCPROW_OWNER_PID[] table;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class TcpProcessRecord
    {
        [DisplayName("Local Address")]
        public IPAddress LocalAddress { get; set; }
        [DisplayName("Local Port")]
        public ushort LocalPort { get; set; }
        [DisplayName("Remote Address")]
        public IPAddress RemoteAddress { get; set; }
        [DisplayName("Remote Port")]
        public ushort RemotePort { get; set; }
        [DisplayName("State")]
        public MibTcpState State { get; set; }
        [DisplayName("Process ID")]
        public int ProcessId { get; set; }
        [DisplayName("Process Name")]
        public string ProcessName { get; set; }

        public TcpProcessRecord(IPAddress localIp, IPAddress remoteIp, ushort localPort,
            ushort remotePort, int pId, MibTcpState state)
        {
            LocalAddress = localIp;
            RemoteAddress = remoteIp;
            LocalPort = localPort;
            RemotePort = remotePort;
            State = state;
            ProcessId = pId;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPROW_OWNER_PID
    {
        public uint localAddr;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] localPort;
        public int owningPid;
    }


    [StructLayout(LayoutKind.Sequential)]
    public struct MIB_UDPTABLE_OWNER_PID
    {
        public uint dwNumEntries;
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType = UnmanagedType.Struct, SizeConst = 1)]
        public MIB_UDPROW_OWNER_PID[] table;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class UdpProcessRecord
    {
        [DisplayName("Local Address")]
        public IPAddress LocalAddress { get; set; }
        [DisplayName("Local Port")]
        public uint LocalPort { get; set; }
        [DisplayName("Process ID")]
        public int ProcessId { get; set; }
        [DisplayName("Process Name")]
        public string ProcessName { get; set; }

        public UdpProcessRecord(IPAddress localAddress, uint localPort, int pId)
        {
            LocalAddress = localAddress;
            LocalPort = localPort;
            ProcessId = pId;
        }
        



   

        


    }
}