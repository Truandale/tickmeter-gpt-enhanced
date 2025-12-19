# FIX: Застревание оценки сети в "плохо" (Network Quality Hysteresis Bug)

**Дата**: 19.12.2025  
**Файл**: `tickMeter\Classes\RivaTuner.cs`  
**Статус**: ✅ Исправлено

## Описание проблемы

Пользователь заметил, что оценка качества сети в оверлее **застревает на "плохо" (POOR)** даже когда метрики находятся в зеленой или желтой зоне.

## Корневая причина

Найдена критическая ошибка в логике гистерезиса рейтинга качества:

### Проблема в коде

В `RivaTuner.cs` использовались **общие статические переменные** для отслеживания состояния гистерезиса:

```csharp
// СТАРЫЙ КОД (ПРОБЛЕМА):
private static double _lastQuality = 1.0;
private static DateTime _lastQualityChange = DateTime.MinValue;
private static string _lastQualityLevel = "excellent";
```

Но метод `GetQualityLevelWithHysteresis()` вызывался **дважды** в гибридном режиме:

```csharp
// В методе FormatHybridQuality():
var (stdLevel, stdColor, stdIcon) = GetQualityLevelWithHysteresis(stats.StandardQuality, "Medium");
var (ctxLevel, ctxColor, ctxIcon) = GetQualityLevelWithHysteresis(stats.ContextQuality, stats.ContextProfile);
```

### Что происходило:

1. **Первый вызов** (Standard): обновляет `_lastQualityLevel = "good"` для Standard Quality
2. **Второй вызов** (Context): видит `_lastQualityLevel = "good"` и применяет к нему гистерезис
3. **Результат**: Context Quality **застревает** на уровне, определенном Standard Quality

### Сценарий бага:

```
Время    | Standard Quality | Context Quality | _lastQualityLevel (общий) | Результат Context
---------|------------------|-----------------|--------------------------|------------------
T0       | 0.75 (good)      | 0.45 (poor)     | "good" (от Standard)     | "good" (ошибка!)
T1       | 0.76 (good)      | 0.46 (poor)     | "good" (держится)        | "good" (ошибка!)
T2       | 0.77 (good)      | 0.44 (poor)     | "good" (держится)        | "good" (ошибка!)
```

Context Quality показывает "good" даже при значении 0.44, которое должно быть "poor"!

## Решение

### Разделено состояние гистерезиса

Создано **раздельное отслеживание** для Standard и Context режимов:

```csharp
// НОВЫЙ КОД (ИСПРАВЛЕНИЕ):
// Раздельное отслеживание для Standard и Context
private static double _lastStandardQuality = 1.0;
private static DateTime _lastStandardQualityChange = DateTime.MinValue;
private static string _lastStandardQualityLevel = "excellent";

private static double _lastContextQuality = 1.0;
private static DateTime _lastContextQualityChange = DateTime.MinValue;
private static string _lastContextQualityLevel = "excellent";
```

### Обновлен метод GetQualityLevelWithHysteresis

Добавлен параметр `isContextMode` для выбора правильного состояния:

```csharp
private static (string level, string color, string icon) GetQualityLevelWithHysteresis(
    double quality, 
    string profileName = "Medium", 
    bool isContextMode = false)  // ← НОВЫЙ ПАРАМЕТР
{
    // Выбираем состояние в зависимости от режима
    ref double lastQuality = ref (isContextMode ? ref _lastContextQuality : ref _lastStandardQuality);
    ref DateTime lastQualityChange = ref (isContextMode ? ref _lastContextQualityChange : ref _lastStandardQualityChange);
    ref string lastQualityLevel = ref (isContextMode ? ref _lastContextQualityLevel : ref _lastStandardQualityLevel);
    
    // ... логика гистерезиса работает с правильными переменными
}
```

### Обновлены вызовы

Каждый режим теперь использует свое состояние:

```csharp
// Standard mode (стандартное качество)
FormatSingleQuality("NET", qualityStats.StandardQuality, "Medium", extras, isContextMode: false);

// Context mode (контекстное качество)
FormatSingleQuality(contextLabel, qualityStats.ContextQuality, qualityStats.ContextProfile, extras, isContextMode: true);

// Hybrid mode
var (stdLevel, stdColor, stdIcon) = GetQualityLevelWithHysteresis(stats.StandardQuality, "Medium", isContextMode: false);
var (ctxLevel, ctxColor, ctxIcon) = GetQualityLevelWithHysteresis(stats.ContextQuality, stats.ContextProfile, isContextMode: true);
```

## После исправления:

```
Время    | Standard Quality | Context Quality | Standard State | Context State | Результат Context
---------|------------------|-----------------|----------------|---------------|------------------
T0       | 0.75 (good)      | 0.45 (poor)     | "good"         | "poor"        | "poor" ✓
T1       | 0.76 (good)      | 0.46 (poor)     | "good"         | "poor"        | "poor" ✓
T2       | 0.77 (good)      | 0.80 (good)     | "good"         | "good"        | "good" ✓
```

Теперь каждый режим корректно отслеживает свое состояние!

## Тестирование

### Что проверить:

1. **Standard mode** (`network_quality_mode=standard`):
   - Рейтинг должен корректно меняться при изменении качества
   - Гистерезис работает (нет мерцания между уровнями)

2. **Context mode** (`network_quality_mode=context`):
   - Рейтинг учитывает пороги профиля (Very Low/Low/Medium/High)
   - Не застревает на неправильных уровнях

3. **Hybrid mode** (`network_quality_mode=hybrid`) - **ГЛАВНАЯ ПРОВЕРКА**:
   - Standard и Context показывают РАЗНЫЕ оценки независимо
   - Context не застревает на уровне Standard
   - Оба работают с гистерезисом без конфликтов

### Сценарии тестирования:

#### Тест 1: Плохое качество в Context
- Профиль: High (строгие пороги)
- Качество: 0.60 (должно быть Poor для High, но Fair для Medium)
- Ожидание:
  - Standard (Medium): **FAIR** или **GOOD** (зеленый/желтый)
  - Context (High): **POOR** (красный) ✓

#### Тест 2: Хорошее качество в Standard, плохое в Context
- Профиль: High
- Качество: 0.75
- Ожидание:
  - Standard (Medium): **GOOD** (зеленый)
  - Context (High): **FAIR** или **POOR** (желтый/красный)
  - Context НЕ должен показывать "GOOD"! ✓

#### Тест 3: Переход между уровнями
- Постепенно ухудшаем качество с 0.90 до 0.40
- Ожидание:
  - Плавный переход: Excellent → Good → Fair → Poor
  - Нет застревания на одном уровне
  - Гистерезис работает (уровень держится ~3 секунды)

## Файлы изменены

- ✅ `tickMeter\Classes\RivaTuner.cs`:
  - Разделены переменные состояния гистерезиса
  - Обновлен метод `GetQualityLevelWithHysteresis()`
  - Обновлены методы `FormatSingleQuality()` и `FormatHybridQuality()`

## Обновление 2: Динамическое обновление профилей ✅

**Дата**: 19.12.2025 12:53  
**Проблема**: Context Profile загружался только один раз при инициализации

### Дополнительные исправления:

1. **Динамическое обновление профиля**:
   - `LoadContextProfile()` теперь вызывается в начале каждого `PerformQualityAnalysis()`
   - Профиль обновляется автоматически при изменении настроек
   - Добавлено логирование изменений профиля

2. **Улучшенное логирование**:
   - Debug вывод теперь показывает обе оценки: Standard и Context
   - Отображается текущий профиль Context
   - Логируются изменения профиля

3. **Пример нового логирования**:
   ```
   [NetworkQualityAnalyzer] Context Profile changed: Medium -> High
   [NetworkQualityAnalyzer] Standard: 0.75 (Good) | Context[High]: 0.62 (Fair) | 
                            Stability=> Ping:0.85 TR:0.92 TT:0.88 | Jitter:12.5ms Target:128.0Hz
   ```

### Что это дает:

- ✅ Смена профиля в настройках применяется **мгновенно**
- ✅ Context Quality всегда использует актуальный профиль
- ✅ Можно менять профиль во время игры - изменения видны сразу
- ✅ Логи помогают отладить работу системы

## Следующие шаги

1. ✅ Пересобрать проект (Release x64)  
2. ✅ Исправлена проблема с динамическим обновлением профилей
3. ⏳ Протестировать в игре с разными профилями
4. ⏳ Проверить что Context mode работает корректно
5. ⏳ Убедиться что нет застревания на неправильных уровнях
6. ⏳ Проверить смену профиля во время игры

## Технические детали

### Гистерезис (Hysteresis)

Механизм предотвращения "дребезга" (мерцания) рейтинга:

```
Входной порог (In):  0.75 - качество должно превысить для перехода вверх
Выходной порог (Out): 0.70 - качество должно упасть ниже для перехода вниз
Время удержания: 3 секунды
```

### Профили порогов (QualityDisplayThresholds)

| Профиль   | Excellent In | Good In | Fair In | Для кого               |
|-----------|-------------|---------|---------|------------------------|
| Very Low  | 0.85        | 0.70    | 0.45    | VPN, удаленные игроки  |
| Low       | 0.88        | 0.73    | 0.48    | Обычные игроки         |
| Medium    | 0.90        | 0.75    | 0.50    | Стандарт (по умолчанию)|
| High      | 0.95        | 0.85    | 0.65    | Про-игроки             |

---

**Автор**: GitHub Copilot  
**Проверено**: Код скомпилирован без ошибок ✓
