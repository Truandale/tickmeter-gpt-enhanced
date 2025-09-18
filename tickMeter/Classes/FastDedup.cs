using System;
using System.Threading;

namespace tickMeter.Classes
{
    /// <summary>
    /// Быстрый дедупликатор пакетов без аллокаций для мультиадаптерного режима
    /// </summary>
    internal static class FastDedup
    {
        // lock-free кольцо хэшей (стандартный размер степени двойки)
        private const int RingSize = 8192;
        private static readonly uint[] _ring = new uint[RingSize];
        private static int _cursor;

        /// <summary>
        /// Проверяет, был ли уже обработан пакет с таким хэшем
        /// </summary>
        /// <param name="data">Данные пакета для хэширования</param>
        /// <returns>true если пакет уже был обработан</returns>
        public static bool TrySeen(ReadOnlySpan<byte> data)
        {
            // хэш по первым 96 байтам с алгоритмом FNV-1a
            int n = Math.Min(data.Length, 96);
            uint h = 2166136261;
            for (int i = 0; i < n; i++)
                h = (h ^ data[i]) * 16777619;

            int idx = Interlocked.Increment(ref _cursor) & (RingSize - 1);
            uint prev = _ring[idx];
            if (prev == h) return true;
            _ring[idx] = h;
            return false;
        }

        /// <summary>
        /// Проверяет, был ли уже обработан пакет с таким хэшем (версия для byte[])
        /// </summary>
        /// <param name="data">Данные пакета для хэширования</param>
        /// <param name="length">Длина данных для обработки</param>
        /// <returns>true если пакет уже был обработан</returns>
        public static bool TrySeen(byte[] data, int length = -1)
        {
            if (data == null) return false;
            
            // хэш по первым 96 байтам с алгоритмом FNV-1a
            int n = Math.Min(length < 0 ? data.Length : length, Math.Min(data.Length, 96));
            uint h = 2166136261;
            for (int i = 0; i < n; i++)
                h = (h ^ data[i]) * 16777619;

            int idx = Interlocked.Increment(ref _cursor) & (RingSize - 1);
            uint prev = _ring[idx];
            if (prev == h) return true;
            _ring[idx] = h;
            return false;
        }

        /// <summary>
        /// Очищает кольцо хэшей (при перезапуске захвата)
        /// </summary>
        public static void Clear()
        {
            Array.Clear(_ring, 0, _ring.Length);
            _cursor = 0;
        }
    }
}