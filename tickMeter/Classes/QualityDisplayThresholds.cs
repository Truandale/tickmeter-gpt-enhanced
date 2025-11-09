using System;

namespace tickMeter.Classes
{
    /// <summary>
    /// Адаптивные пороги для отображения рейтинга качества сети
    /// Синхронизированы с профилями цветовых зон для визуальной согласованности
    /// </summary>
    public static class QualityDisplayThresholds
    {
        /// <summary>
        /// Получает пороги для рейтинга качества по профилю
        /// </summary>
        /// <param name="profile">Название профиля (very_low, low, medium, high)</param>
        /// <returns>Пороги для Excellent, Good, Fair (с гистерезисом)</returns>
        public static (double excellentIn, double excellentOut, double goodIn, double goodOut, double fairIn, double fairOut) GetThresholds(string profile)
        {
            const double HYSTERESIS = 0.05; // 5% гистерезис для стабильности
            
            switch (profile?.ToLower().Replace(" ", ""))
            {
                case "verylow":
                case "very_low":
                    // Мягкие пороги для VPN/удаленных игроков
                    return (
                        excellentIn: 0.85, excellentOut: 0.80,
                        goodIn: 0.70, goodOut: 0.65,
                        fairIn: 0.45, fairOut: 0.40
                    );
                    
                case "low":
                    // Сбалансированные пороги
                    return (
                        excellentIn: 0.88, excellentOut: 0.83,
                        goodIn: 0.73, goodOut: 0.68,
                        fairIn: 0.48, fairOut: 0.43
                    );
                    
                case "high":
                    // Строгие пороги для про-игроков
                    return (
                        excellentIn: 0.95, excellentOut: 0.90,
                        goodIn: 0.85, goodOut: 0.80,
                        fairIn: 0.65, fairOut: 0.60
                    );
                    
                default: // medium
                    // Стандартные пороги (текущие)
                    return (
                        excellentIn: 0.90, excellentOut: 0.85,
                        goodIn: 0.75, goodOut: 0.70,
                        fairIn: 0.50, fairOut: 0.45
                    );
            }
        }
        
        /// <summary>
        /// Получает название профиля с правильным форматированием
        /// </summary>
        public static string GetProfileDisplayName(string profile)
        {
            switch (profile?.ToLower().Replace(" ", ""))
            {
                case "verylow":
                case "very_low":
                    return "Very Low";
                case "low":
                    return "Low";
                case "high":
                    return "High";
                default:
                    return "Medium";
            }
        }
        
        /// <summary>
        /// Получает краткое обозначение профиля для overlay
        /// </summary>
        public static string GetProfileShortName(string profile)
        {
            switch (profile?.ToLower().Replace(" ", ""))
            {
                case "verylow":
                case "very_low":
                    return "VL";
                case "low":
                    return "L";
                case "high":
                    return "H";
                default:
                    return "M";
            }
        }
    }
    
    /// <summary>
    /// Пороги для расчета Network Quality (ping/ticktime thresholds)
    /// </summary>
    public static class QualityCalculationThresholds
    {
        /// <summary>
        /// Получает пороги для расчета level penalties
        /// </summary>
        public static (float pingGood, float pingBad, float ticktimeGood, float ticktimeBad) GetThresholds(string profile)
        {
            switch (profile?.ToLower().Replace(" ", ""))
            {
                case "verylow":
                case "very_low":
                    return (
                        pingGood: 50f,
                        pingBad: 150f,
                        ticktimeGood: 10f,
                        ticktimeBad: 20f
                    );
                    
                case "low":
                    return (
                        pingGood: 45f,
                        pingBad: 100f,
                        ticktimeGood: 9f,
                        ticktimeBad: 18f
                    );
                    
                case "high":
                    return (
                        pingGood: 20f,
                        pingBad: 60f,
                        ticktimeGood: 6f,
                        ticktimeBad: 12f
                    );
                    
                default: // medium
                    return (
                        pingGood: 30f,
                        pingBad: 80f,
                        ticktimeGood: 8f,
                        ticktimeBad: 16f
                    );
            }
        }
    }
}
