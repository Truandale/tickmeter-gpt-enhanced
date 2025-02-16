public class UdpProcessRecord
{
    public string LocalAddress { get; set; }
    public int LocalPort { get; set; }
    public int ProcessId { get; set; }
    public string ProcessName { get; set; }

    public UdpProcessRecord(string localAddress, int localPort, int processId, string processName)
    {
        LocalAddress = localAddress;
        LocalPort = localPort;
        ProcessId = processId;
        ProcessName = processName;
    }
}
