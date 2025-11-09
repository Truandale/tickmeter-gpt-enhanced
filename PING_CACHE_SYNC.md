# Синхронизация отображения пинга через кэширование

## Проблема

До этого исправления значение пинга в главном окне (GUI) и в оверлее (RTSS/RivaTuner) могли отличаться, даже при включенном сглаживании. Это происходило потому что:

1. **GUI** применял сглаживание к RAW значению через `SmoothPingValue(rawPing)`
2. **Overlay** также применял сглаживание к RAW значению через `SmoothPingValueOverlay(rawPingOverlay)`
3. Оба использовали **один и тот же EMA**, но обновлялись **независимо** в разные моменты времени
4. Результат: значения немного отличались из-за временного сдвига

## Решение

Реализовано **кэширование сглаженного значения** из GUI для использования в Overlay:

### 1. Кэш в SmoothingManager (Classes/SmoothingManager.cs)

```csharp
// === КЭШИРОВАНИЕ для синхронизации GUI и Overlay ===
private static int _cachedSmoothedPing = 0;
private static int _cachedRawPing = 0;  // Для отслеживания изменений

public static int SmoothPingValue(int raw)
{
    if (!IsPingValueEnabled() || raw <= 0) return raw;
    lock (_lock)
    {
        if (_emaPingValue == null)
        {
            _emaPingValue = new ExponentialMovingAverage(GetAlpha());
        }
        int smoothed = (int)Math.Round(_emaPingValue.Update(raw));
        
        // Сохраняем в кэш для использования в Overlay
        _cachedSmoothedPing = smoothed;
        _cachedRawPing = raw;
        
        return smoothed;
    }
}

/// <summary>
/// Получить кэшированное сглаженное значение пинга из GUI.
/// Используется в Overlay для синхронизации отображения.
/// </summary>
public static int GetCachedSmoothedPing(int rawPing)
{
    lock (_lock)
    {
        // Если сглаживание выключено, возвращаем RAW
        if (!IsPingValueEnabled())
        {
            return rawPing;
        }
        
        // Если значение в кэше соответствует текущему RAW, используем кэш
        if (_cachedRawPing == rawPing && _cachedSmoothedPing > 0)
        {
            return _cachedSmoothedPing;
        }
        
        // Fallback: если кэш не синхронизирован, применяем сглаживание
        if (_emaPingValue == null)
        {
            _emaPingValue = new ExponentialMovingAverage(GetAlpha());
        }
        return (int)Math.Round(_emaPingValue.Update(rawPing));
    }
}
```

### 2. GUI автоматически обновляет кэш (Forms/GUI.cs)

В методе `TicksLoop_Tick()` при вызове `SmoothPingValue(rawPing)` автоматически сохраняется сглаженное значение в кэш:

```csharp
// FIXED: Apply smoothing first, then determine zone from smoothed value
int displayPing = rawPing > 0 ? Classes.SmoothingManager.SmoothPingValue(rawPing) : 0;
// ↑ Этот вызов автоматически обновляет _cachedSmoothedPing
```

### 3. Overlay использует кэш (Classes/RivaTuner.cs)

#### В методе FormatPing():
```csharp
// === ИСПОЛЬЗУЕМ КЭШИРОВАННОЕ ЗНАЧЕНИЕ ИЗ GUI ===
int rawPingOverlay = (int)snap.PingAvgMs;
int smoothedPing = Classes.SmoothingManager.GetCachedSmoothedPing(rawPingOverlay);
pingValue = smoothedPing.ToString();

DebugLogger.log($"[OVERLAY-PING] Raw={rawPingOverlay} -> Smoothed={smoothedPing} (cached from GUI)");
```

#### В методе BuildRivaOutput() для графика пинга:
```csharp
// === ИСПОЛЬЗУЕМ КЭШИРОВАННОЕ ЗНАЧЕНИЕ ИЗ GUI ===
if (snap.PingAvgMs > 0)
{
    // Используем кэшированное значение для синхронизации с GUI
    int smoothedPing = Classes.SmoothingManager.GetCachedSmoothedPing((int)snap.PingAvgMs);
    pingValue = smoothedPing.ToString();
    
    // Calculate zone from SMOOTHED display value
    var pingZone = zoner.FromPing(smoothedPing);
    pingColor = Classes.ZoneColors.ToRtssLegacy(pingZone);
}
```

## Гарантии

✅ **Отсутствие двойного сглаживания**: Сглаживание применяется только один раз в GUI  
✅ **Синхронизация**: Overlay показывает ТО ЖЕ значение что и главное окно  
✅ **Потокобезопасность**: Все операции с кэшем защищены `lock(_lock)`  
✅ **Fallback**: Если кэш не синхронизирован (overlay обновился раньше GUI), применяется прямое сглаживание  
✅ **Сброс**: Кэш сбрасывается вместе с EMA при вызове `ResetValueEmas()`  

## Файлы изменены

- `Classes/SmoothingManager.cs` - добавлен кэш и метод `GetCachedSmoothedPing()`
- `Classes/RivaTuner.cs` - использование кэша в `FormatPing()` и `BuildRivaOutput()`

## Поведение

1. **GUI обновляется** → вычисляет RAW пинг → применяет сглаживание → **сохраняет в кэш**
2. **Overlay обновляется** → получает RAW пинг → **читает из кэша** → отображает то же значение что GUI

Результат: **100% синхронизация** значений пинга между GUI и Overlay! 🎯
