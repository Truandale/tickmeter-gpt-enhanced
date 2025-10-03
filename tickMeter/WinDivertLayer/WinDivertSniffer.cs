using System;
using System.Diagnostics;

namespace tickMeter.WinDivertLayer
{
    public sealed class WinDivertSniffer : IDisposable
    {
        public bool IsRunning { get; private set; }

        public void Start()
        {
            if (IsRunning)
                return;

            try
            {
                Debug.WriteLine("[WinDivertSniffer] Starting WinDivert stub");
                IsRunning = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[WinDivertSniffer] Failed to start: {ex.Message}");
                IsRunning = false;
                throw;
            }
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            Debug.WriteLine("[WinDivertSniffer] Stop");
            IsRunning = false;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
