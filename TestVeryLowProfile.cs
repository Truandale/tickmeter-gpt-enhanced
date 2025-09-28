// Тест профиля Very Low
using System;
using tickMeter;

class TestVeryLowProfile
{
    static void Main()
    {
        // Тестируем профиль Very Low
        var profile = ColorZoneProfile.GetProfile("Very Low");
        
        Console.WriteLine($"Профиль: {profile.Name}");
        Console.WriteLine($"Ping зоны: 0-{profile.PingGreenMs}ms = зеленый, {profile.PingGreenMs+1}-{profile.PingYellowMs}ms = желтый, {profile.PingYellowMs+1}ms+ = красный");
        Console.WriteLine($"Tickrate зоны: >={profile.TickrateGreenRatio*100:F0}% = зеленый, >={profile.TickrateYellowRatio*100:F0}% = желтый");
        Console.WriteLine($"Ticktime зоны: <={profile.TicktimeGreenRatio*100:F0}% = зеленый, <={profile.TicktimeYellowRatio*100:F0}% = желтый");
        
        // Проверяем список профилей
        Console.WriteLine("\nДоступные профили:");
        foreach(var name in ColorZoneProfile.GetProfileNames())
        {
            Console.WriteLine($"- {name}");
        }
    }
}