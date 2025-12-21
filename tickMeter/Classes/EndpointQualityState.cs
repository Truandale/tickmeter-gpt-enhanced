using System;
using System.Collections.Generic;

namespace tickMeter.Classes
{
    /// <summary>
    /// Состояние качества сети для конкретного endpoint'а (IP:Port).
    /// Хранит историю метрик и кэшированные значения качества.
    /// </summary>
    public class EndpointQualityState
    {
        // История метрик для конкретного endpoint'а
        public Queue<float> PingHistory { get; set; }
        public Queue<float> TickrateHistory { get; set; }
        public Queue<float> TicktimeHistory { get; set; }

        // Время последнего обновления (для TTL cleanup)
        public DateTime LastUpdate { get; set; }

        // Кэшированные значения качества
        public float StandardQuality { get; set; }
        public float ContextQuality { get; set; }

        // Метаданные endpoint'а
        public long BytesIn { get; set; }
        public long BytesOut { get; set; }
        public int PacketCount { get; set; }

        // Endpoint ключ (IP:Port)
        public string EndpointKey { get; set; }

        public EndpointQualityState(string endpointKey, int historyCapacity = 128)
        {
            EndpointKey = endpointKey;
            PingHistory = new Queue<float>(historyCapacity);
            TickrateHistory = new Queue<float>(historyCapacity);
            TicktimeHistory = new Queue<float>(historyCapacity);
            LastUpdate = DateTime.UtcNow;
            StandardQuality = 0f;
            ContextQuality = 0f;
            BytesIn = 0;
            BytesOut = 0;
            PacketCount = 0;
        }

        /// <summary>
        /// Проверяет истёк ли TTL для этого endpoint'а
        /// </summary>
        public bool IsExpired(TimeSpan ttl)
        {
            return (DateTime.UtcNow - LastUpdate) > ttl;
        }

        /// <summary>
        /// Обновляет время последнего обращения
        /// </summary>
        public void Touch()
        {
            LastUpdate = DateTime.UtcNow;
        }
    }
}
