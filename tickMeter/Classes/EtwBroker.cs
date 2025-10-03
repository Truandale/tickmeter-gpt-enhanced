using System;
using System.Diagnostics;
using System.Threading;

namespace tickMeter.Classes
{
    public static class EtwBroker
    {
        private static int _started;

        public static bool IsRunning => _started == 1;

        public static void Start()
        {
            if (Interlocked.Exchange(ref _started, 1) == 1)
                return;

            try
            {
                Debug.WriteLine("[EtwBroker] Starting ETW enrichment stub");
                // TODO: Actual ETW subscription will be implemented in phase 2
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[EtwBroker] Failed to start: {ex.Message}");
                Interlocked.Exchange(ref _started, 0);
            }
        }
    }
}
