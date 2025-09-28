using System;
using System.Threading;
using PcapDotNet.Core;
using PcapDotNet.Packets;

namespace tickMeter.Classes
{
    internal sealed class PcapWorker : IDisposable
    {
        public readonly LivePacketDevice Device;
        private PacketCommunicator _comm;
        private Thread _thread;
        private CancellationTokenSource _cts;
        private readonly Action<Packet, LivePacketDevice> _onPacket;
        private volatile bool _started;

        public PcapWorker(LivePacketDevice device, Action<Packet, LivePacketDevice> onPacket)
        {
            Device = device ?? throw new ArgumentNullException(nameof(device));
            _onPacket = onPacket ?? throw new ArgumentNullException(nameof(onPacket));
        }

        public void Start()
        {
            if (_started) return;
            _started = true;
            _cts = new CancellationTokenSource();
            _thread = new Thread(CaptureLoop) { IsBackground = true, Name = $"pcap:{Device.Name}" };
            _thread.Start();
        }

        private void CaptureLoop()
        {
            try
            {
                _comm = Device.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 150);
                if (_comm.DataLink.Kind != DataLinkKind.Ethernet) return;

                // BPF, буферы — по желанию, как раньше (без падений на ошибках):
                TryApplyTunings(_comm);

                var token = _cts.Token;

                // ВАЖНО: никаких подписок типа device.OnPacketArrival += ...
                // Только ReceivePackets в нашем потоке — так не останется «висячих» делегатов.
                _comm.ReceivePackets(0, packet =>
                {
                    if (token.IsCancellationRequested)
                    {
                        // Аккуратно прерываем ReceivePackets
                        _comm.Break();
                        return;
                    }
                    _onPacket(packet, Device); // НИ одного обращения к UI тут!
                });
            }
            catch
            {
                // лог — по желанию
            }
            finally
            {
                try { _comm?.Dispose(); } catch { }
                _comm = null;
            }
        }

        public void Stop()
        {
            try { _cts?.Cancel(); } catch { }
            try { _comm?.Break(); } catch { } // снимает блокировку ReceivePackets
            try { if (_thread != null && _thread.IsAlive) _thread.Join(1000); } catch { }
        }

        public void Dispose()
        {
            Stop();
            try { _cts?.Dispose(); } catch { }
            _cts = null;
        }

        private static void TryApplyTunings(PacketCommunicator comm)
        {
            // Pcap optimization settings
            bool pcapOptimization = App.settingsManager?.GetOption("pcap_optimization", "True", "ADVANCED") == "True";
            
            // безопасно: если методов нет — просто молчим
            try
            {
                var expr = App.settingsManager.GetOption("capture_filter", "ip or ip6");
                using (var f = comm.CreateFilter(expr)) comm.SetFilter(f);
            } catch { }

            if (pcapOptimization)
            {
                try
                {
                    var mi = comm.GetType().GetMethod("SetKernelBufferSize",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (mi != null)
                    {
                        int mb = 8;
                        int.TryParse(App.settingsManager.GetOption("pcap.kernel_buffer_mb", "8"), out mb);
                        mi.Invoke(comm, new object[] { Math.Max(1, mb) * 1024 * 1024 });
                    }
                } catch { }
            }

            if (pcapOptimization)
            {
                try
                {
                    var mi = comm.GetType().GetMethod("SetMinToCopy",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (mi != null)
                    {
                        int v = 4096;
                        int.TryParse(App.settingsManager.GetOption("pcap.min_to_copy", "4096"), out v);
                        mi.Invoke(comm, new object[] { Math.Max(0, v) });
                    }
                } catch { }
            }
        }
    }
}