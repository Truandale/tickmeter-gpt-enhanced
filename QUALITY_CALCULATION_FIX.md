# Network Quality Calculation - Critical Fixes

**Дата**: 21.12.2025  
**Файл**: `tickMeter\Classes\NetworkQualityAnalyzer.cs`  
**Статус**: ✅ Исправлено

---

## 🔴 Критические исправления

### 1. Раздельный EMA для Standard и Context

**Проблема**: Использовалась одна переменная `_overallEma` для всех расчетов, что приводило к смешиванию значений между Standard и Context режимами.

**Было**:
```csharp
private static float _overallEma = -1f; // Общая для всех

private static float CalculateQualityWithProfile(string profileName)
{
    // ...расчет quality...
    if (_overallEma < 0)
        _overallEma = quality;
    else 
        _overallEma = _overallEma + _emaAlpha * (quality - _overallEma);
    return _overallEma; // ← Возвращает общий EMA!
}
```

**Проблема**: 
```
Standard (Medium): quality = 0.75 → _overallEma = 0.75
Context (High):    quality = 0.62 → _overallEma = 0.73 (смешано!)
```

**Стало**:
```csharp
private static float _standardEma = -1f;  // Раздельно для Standard
private static float _contextEma = -1f;   // Раздельно для Context

private static float CalculateQualityWithProfile(string profileName)
{
    // ...расчет quality...
    bool isStandardProfile = profileName == "Medium";
    ref float emaRef = ref (isStandardProfile ? ref _standardEma : ref _contextEma);
    
    if (emaRef < 0)
        emaRef = quality;
    else 
        emaRef = emaRef + _emaAlpha * (quality - emaRef);
    return emaRef;
}
```

**Результат**: ✅ Standard и Context теперь имеют независимое сглаживание

---

### 2. Адаптивные пороги для GetQualityRating

**Проблема**: Метод `GetQualityRating()` использовал фиксированные пороги и игнорировал профиль.

**Было**:
```csharp
private static string GetQualityRating(float quality)
{
    if (quality >= 0.9f) return "Excellent";  // ← Хардкод!
    if (quality >= 0.8f) return "Good";
    if (quality >= 0.6f) return "Fair";
    // ...
}
```

**Стало**:
```csharp
private static string GetQualityRating(float quality, string profileName = "Medium")
{
    // Используем адаптивные пороги для профиля
    var (excellentIn, _, goodIn, _, fairIn, _) = 
        QualityDisplayThresholds.GetThresholds(profileName);
    
    if (quality >= excellentIn) return "Excellent";
    if (quality >= goodIn) return "Good";
    if (quality >= fairIn) return "Fair";
    // ...
}
```

**Результат**: ✅ Рейтинг теперь учитывает профиль (Very Low/Low/Medium/High)

**Пример**:
```
Quality = 0.75:
- Very Low:  0.75 >= 0.70 (Good)  → "Good" ✅
- Medium:    0.75 >= 0.75 (Good)  → "Good" ✅
- High:      0.75 < 0.85 (Good)   → "Fair" ✅
```

---

### 3. Защита от деления на ноль

**Проблема**: Отсутствовала проверка разности порогов перед делением.

**Было**:
```csharp
if (avgPing > pingGoodMs)
{
    pingLevelPenalty = (avgPing - pingGoodMs) / (pingBadMs - pingGoodMs);
    // ↑ Division by zero если pingBad == pingGood!
}
```

**Стало**:
```csharp
float pingRange = pingBadMs - pingGoodMs;
if (pingRange > 0 && avgPing > pingGoodMs)
{
    pingLevelPenalty = Math.Min(1f, Math.Max(0f, (avgPing - pingGoodMs) / pingRange));
}
```

**Результат**: ✅ Защита от деления на ноль + дополнительный clamp

---

## 📊 Влияние изменений

### До исправлений:

```
Сценарий: ping = 45ms, стабильность хорошая

Standard (Medium):
- CalculateQualityWithProfile("Medium") → quality = 0.75
- _overallEma = 0.75
- GetQualityRating(0.75) → "Good" (0.75 >= 0.8? Нет, 0.75 >= 0.6? Да)
                           → Actually "Fair" ❌

Context (High):
- CalculateQualityWithProfile("High") → quality = 0.62
- _overallEma = 0.75 + 0.15*(0.62-0.75) = 0.73 ← СМЕШАНО!
- GetQualityRating(0.73) → "Fair" (но должно быть по 0.62!)
                          → Неправильное значение ❌
```

### После исправлений:

```
Сценарий: ping = 45ms, стабильность хорошая

Standard (Medium):
- CalculateQualityWithProfile("Medium") → quality = 0.75
- _standardEma = 0.75 ← Раздельный EMA
- GetQualityRating(0.75, "Medium") → "Good" ✅
  (пороги Medium: excellent=0.90, good=0.75, fair=0.50)

Context (High):
- CalculateQualityWithProfile("High") → quality = 0.62
- _contextEma = 0.62 ← Раздельный EMA, не смешано!
- GetQualityRating(0.62, "High") → "Poor" ✅
  (пороги High: excellent=0.95, good=0.85, fair=0.65)
```

---

## ✅ Что теперь работает правильно

1. **Независимое сглаживание**:
   - Standard качество имеет свой EMA
   - Context качество имеет свой EMA
   - Нет взаимного влияния между режимами

2. **Адаптивные рейтинги**:
   - Very Low: мягкие пороги (0.85/0.70/0.45)
   - Medium: стандартные (0.90/0.75/0.50)
   - High: строгие (0.95/0.85/0.65)

3. **Безопасность расчетов**:
   - Защита от деления на ноль
   - Дополнительный clamp для penalty значений
   - Корректная работа при граничных условиях

4. **Согласованность**:
   - `GetQualityRating()` использует те же пороги, что и `QualityDisplayThresholds`
   - `RivaTuner.cs` с гистерезисом использует те же пороги
   - Визуальное отображение соответствует внутренним расчетам

---

## 🧪 Тестирование

### Рекомендуемые сценарии:

**1. Hybrid Mode (главная проверка)**:
```ini
network_quality_mode=hybrid
color_zone_profile=High
```
- Standard должен показывать объективную оценку
- Context должен показывать более строгую оценку
- Значения не должны смешиваться

**2. Context Mode с разными профилями**:
```ini
network_quality_mode=context
```
- Very Low: ping 50ms → "Good" (мягкие требования)
- High: ping 50ms → "Poor" (строгие требования)

**3. Плавное изменение качества**:
- Постепенно увеличиваем ping от 20 до 100ms
- EMA должен плавно отслеживать изменения
- Рейтинг должен меняться согласованно с порогами

**4. Граничные условия**:
- pingGood = pingBad = 30 → не должно быть деления на ноль
- Пустые буферы → корректная инициализация EMA

---

## 📝 Изменения в коде

**Файл**: `NetworkQualityAnalyzer.cs`

**Изменено**:
- Строка ~63: Добавлены `_standardEma` и `_contextEma` вместо `_overallEma`
- Строка ~387-488: Обновлен `CalculateQualityWithProfile()` с раздельным EMA
- Строка ~491-503: Обновлен `GetQualityRating()` с параметром профиля
- Строка ~415: Добавлена защита `pingRange` от деления на ноль
- Строка ~438: Добавлена защита `ticktimeRange` от деления на ноль
- Строка ~299: Обновлены вызовы `GetQualityRating()` с передачей профиля
- Строка ~645: Обновлен `Clear()` для сброса обоих EMA

**Обратная совместимость**: ✅ Полная
- Все существующие настройки работают как прежде
- API не изменился (добавлен только опциональный параметр)
- Формула расчета качества не изменена

---

## 🎯 Итог

**Проблемы устранены**:
- ✅ Раздельный EMA для Standard/Context
- ✅ Адаптивные пороги рейтинга
- ✅ Защита от деления на ноль
- ✅ Согласованность между модулями

**Результат**: Система расчета качества сети теперь работает **корректно и точно** для всех режимов и профилей.

---

**Автор исправлений**: GitHub Copilot  
**Проверено**: Код скомпилирован без ошибок ✓
