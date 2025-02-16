public class TcpProcessRecord
{
    public string LocalAddress { get; set; }
    public int LocalPort { get; set; }
    public string RemoteAddress { get; set; }
    public int RemotePort { get; set; }
    public int ProcessId { get; set; }
    public string ProcessName { get; set; }
    public string State { get; set; }

    public TcpProcessRecord(string localAddress, int localPort, string remoteAddress, int remotePort, int processId, string processName, string state)
    {
        LocalAddress = localAddress;
        LocalPort = localPort;
        RemoteAddress = remoteAddress;
        RemotePort = remotePort;
        ProcessId = processId;
        ProcessName = processName;
        State = state;
    }
}
