# Анализ настроек Network Quality для Гибридного режима

## 📋 ТЕКУЩИЕ НАСТРОЙКИ (Скриншот)

```
Stage 6: Анализ качества сети
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

☑ Включить анализ качества сети
☐ Показывать рейтинг качества в оверлее
☑ Использовать сглаженные данные для анализа

Размер буфера: 100
Порог стабильности: 0.15
Порог качества: 0.80

Качество сети: 100%
Рейтинг: Excellent
```

---

## 🔍 РАЗБОР КАЖДОЙ НАСТРОЙКИ

### 1. ☑ Включить анализ качества сети

**Текущее назначение:**
- Включает/выключает весь NetworkQualityAnalyzer
- Если выключено - не считается вообще

**После гибридного режима:**
- ✅ **ОСТАЕТСЯ** - базовый переключатель
- Без изменений

**Вердикт:** ✅ НУЖНА

---

### 2. ☐ Показывать рейтинг качества в оверлее

**Текущее назначение:**
- Показывать `NET: GOOD 85%` в RTSS оверлее
- Если выключено - качество считается, но не отображается

**После гибридного режима:**
- ✅ **ОСТАЕТСЯ** - переключатель отображения
- Новая опция: что показывать (Standard/Context/Both)

**Вердикт:** ✅ НУЖНА, но нужна НОВАЯ настройка

---

### 3. ☑ Использовать сглаженные данные для анализа

**Текущее назначение:**
- RAW данные (ping, tickrate) vs Smoothed (сглаженные)
- Влияет на входные данные в анализатор

**После гибридного режима:**
- ✅ **ОСТАЕТСЯ** - независимая настройка
- Не связана с профилями (это про входные данные)

**Вердикт:** ✅ НУЖНА

---

### 4. Размер буфера: 100

**Текущее назначение:**
- Сколько сэмплов хранить для анализа стабильности
- Влияет на расчет Coefficient of Variation (CV)

**После гибридного режима:**
- ✅ **ОСТАЕТСЯ** - размер истории для CV
- Не зависит от профилей

**Вердикт:** ✅ НУЖНА

---

### 5. Порог стабильности: 0.15

**Текущее назначение:**
```csharp
private static float _stabilityThreshold = 0.15f;
```
- Базовый порог для определения "стабильности"
- Используется для ping/tickrate/ticktime stability

**Текущее использование:**
```csharp
// NetworkQualityAnalyzer.cs
var stabilityStr = App.settingsManager?.GetOption("stability_threshold", "0.15", "ADVANCED");
if (SettingsManager.TryParseInvariantFloat(stabilityStr?.Trim(), out float stability))
{
    _stabilityThreshold = stability;
}

// Дальше используется для расчета отдельных порогов:
_pingStabilityThreshold = 0.15f;
_tickrateStabilityThreshold = 0.10f;
_ticktimeStabilityThreshold = 0.18f;
```

**После гибридного режима:**
- ✅ **ОСТАЕТСЯ** - порог для расчета CV стабильности
- Не зависит от профилей (стабильность = объективна)

**Вердикт:** ✅ НУЖНА

---

### 6. Порог качества: 0.80

**Текущее назначение:**
```csharp
private static float _qualityThreshold = 0.8f; // Порог качества сети (80%)
```

**Где используется:**
```csharp
// NetworkQualityAnalyzer.cs - строка 487
if (OverallQuality < _qualityThreshold)
{
    // Предсказание проблем
}

// NetworkOptimizer.cs - строка 156
if (_enabled && NetworkQualityAnalyzer.OverallQuality < _qualityThreshold)
{
    // Запуск оптимизации
}
```

**Назначение:** Порог для определения "нужна ли оптимизация сети"

**После гибридного режима:**
- ⚠️ **НЕЯСНО** - какой качество использовать? Standard или Context?
- Вероятно нужно для Standard (объективная оценка)

**Вердикт:** ✅ НУЖНА, но требует уточнения использования

---

## 🆕 НОВЫЕ НАСТРОЙКИ ДЛЯ ГИБРИДНОГО РЕЖИМА

### 7. Network Quality Mode (НОВАЯ!)

**Назначение:** Выбор режима отображения

```ini
[ADVANCED]
network_quality_mode = hybrid  # standard, context, hybrid
```

**Варианты:**
- `standard` - показывать только Standard (Medium пороги)
- `context` - показывать только Context (по профилю зон)
- `hybrid` - показывать оба

**UI:**
```
Режим отображения качества:
  ( ) Стандартная оценка
  ( ) Контекстная оценка (по профилю зон)
  (•) Гибридный (обе оценки)
```

**Вердикт:** 🆕 НУЖНА НОВАЯ

---

### 8. Context Quality Sync (НОВАЯ!)

**Назначение:** Синхронизация Context качества с Color Zone Profile

```ini
[ADVANCED]
network_quality_context_sync = true  # true = auto, false = manual
network_quality_context_profile = medium  # если sync=false
```

**UI:**
```
Контекстная оценка:
  ☑ Синхронизировать с профилем цветовых зон
  
  Профиль для контекстной оценки: [Medium ▼]
    (активно только если галочка снята)
```

**Вердикт:** 🆕 НУЖНА НОВАЯ

---

### 9. Color Zones for Quality Display (НОВАЯ!)

**Назначение:** Цветовые зоны для ОТОБРАЖЕНИЯ рейтинга в оверлее

**Проблема:**
```
Сейчас в RivaTuner.cs:
const double EXCELLENT_IN = 0.90;  // Хардкод
const double GOOD_IN = 0.75;       // Хардкод
const double FAIR_IN = 0.50;       // Хардкод
```

**Но!** Разные профили могут иметь разные пороги:

```
Very Low Profile (мягкие требования):
  🟢 Excellent: 85-100%  ← Ниже, чем сейчас
  🟢 Good:      70-84%
  🟡 Fair:      50-69%
  🔴 Poor:      0-49%

High Profile (строгие требования):
  🟢 Excellent: 95-100%  ← Выше, чем сейчас
  🟢 Good:      85-94%
  🟡 Fair:      65-84%
  🔴 Poor:      0-64%
```

**Решение:**
```ini
[ADVANCED]
# Пороги для Standard Quality (всегда Medium)
quality_excellent_threshold = 0.90
quality_good_threshold = 0.75
quality_fair_threshold = 0.50

# Пороги для Context Quality (по профилю)
quality_context_thresholds = sync_with_profile  # или custom
```

**Вердикт:** 🆕 НУЖНА НОВАЯ (или адаптация существующих)

---

## 📊 ИТОГОВАЯ СТРУКТУРА НАСТРОЕК

### СЕКЦИЯ: Stage 6 - Анализ качества сети

```
┌─────────────────────────────────────────────────────────────┐
│ ☑ Включить анализ качества сети                            │ ✅ Существует
│ ☐ Показывать рейтинг качества в оверлее                    │ ✅ Существует
│ ☑ Использовать сглаженные данные для анализа               │ ✅ Существует
├─────────────────────────────────────────────────────────────┤
│ Размер буфера:        [100 ↕]                              │ ✅ Существует
│ Порог стабильности:   [0.15 ↕]                             │ ✅ Существует
│ Порог качества:       [0.80 ↕]                             │ ✅ Существует
├─────────────────────────────────────────────────────────────┤
│ ═══ НОВЫЕ НАСТРОЙКИ ═══                                     │
│                                                             │
│ Режим отображения качества:                                │ 🆕 Новая
│   ( ) Стандартная оценка (Medium пороги)                   │
│   ( ) Контекстная оценка (по профилю зон)                  │
│   (•) Гибридный (обе оценки)                               │
│                                                             │
│ Контекстная оценка:                                        │ 🆕 Новая
│   ☑ Синхронизировать с профилем цветовых зон              │
│   Профиль: [Very Low ▼] (неактивен если галочка)          │
│                                                             │
│ Цветовые зоны рейтинга:                                    │ 🆕 Новая
│   (•) Адаптивные (по выбранному профилю)                   │
│   ( ) Фиксированные (90%/75%/50%)                          │
├─────────────────────────────────────────────────────────────┤
│ Качество сети: 100%                                         │ ✅ Существует
│ Рейтинг: Excellent                                          │ ✅ Существует
│                                                             │
│ Стандартная оценка: GOOD 85%        (Medium)              │ 🆕 Новая
│ Контекстная оценка: EXCELLENT 92%   (Very Low)            │ 🆕 Новая
└─────────────────────────────────────────────────────────────┘
```

---

## 🎨 ЦВЕТОВЫЕ ЗОНЫ - ДЕТАЛЬНЫЙ РАЗБОР

### Проблема:

**Сейчас в коде (RivaTuner.cs):**
```csharp
const double EXCELLENT_IN = 0.90, EXCELLENT_OUT = 0.85;
const double GOOD_IN = 0.75, GOOD_OUT = 0.70;
const double FAIR_IN = 0.50, FAIR_OUT = 0.45;
```

**Это фиксированные пороги для ВСЕХ профилей!**

### Пример несоответствия:

**Very Low Profile + Ping 80ms:**

```
Color Zones (Visual):
  Ping 80ms → Yellow Zone (50-150ms)
  
Standard Quality (Medium thresholds):
  80ms → (80-30)/(80-30) = 1.0 → penalty
  Result: 68% → FAIR 🟡
  Visual: NET: 🟡 FAIR 68%  ✅ Согласованно!

Context Quality (Very Low thresholds):
  80ms → (80-50)/(150-50) = 0.30 → penalty
  Result: 82% → GOOD 🟢
  Visual: Context: 🟢 GOOD 82%
  
НО! Цветовой порог:
  82% < 90% (EXCELLENT_IN) → GOOD
  ✅ Правильно окрашен в зеленый
```

**High Profile + Ping 80ms:**

```
Color Zones (Visual):
  Ping 80ms → Red Zone (>60ms)
  
Standard Quality (Medium thresholds):
  Result: 68% → FAIR 🟡
  Visual: NET: 🟡 FAIR 68%  ⚠️ Не согласовано с Red zone!

Context Quality (High thresholds):
  80ms → (80-20)/(60-20) = 1.5 → cap at 1.0 → penalty
  Result: 48% → POOR 🔴
  Visual: Context: 🔴 POOR 48%  ✅ Согласовано!
  
Цветовой порог:
  48% < 50% (FAIR_IN) → POOR
  ✅ Правильно окрашен в красный
```

### Решение: Адаптивные пороги

**Профили цветовых зон для Quality:**

```csharp
// NetworkQualityThresholds.cs (НОВЫЙ ФАЙЛ)
public class QualityDisplayThresholds
{
    public static (double excellentIn, double goodIn, double fairIn) GetThresholds(string profile)
    {
        switch (profile.ToLower())
        {
            case "very_low":
            case "verylow":
                return (0.85, 0.70, 0.45);  // Мягкие пороги
                
            case "low":
                return (0.88, 0.73, 0.48);  // Чуть строже
                
            case "high":
                return (0.95, 0.85, 0.65);  // Строгие пороги
                
            default: // medium
                return (0.90, 0.75, 0.50);  // Стандарт (текущие)
        }
    }
}
```

**Использование в RivaTuner.cs:**

```csharp
private static (string level, string color, string icon) GetQualityLevelWithHysteresis(
    double quality, 
    string profileName = "medium")  // НОВЫЙ параметр
{
    var (excellentIn, goodIn, fairIn) = QualityDisplayThresholds.GetThresholds(profileName);
    
    // Рассчитываем OUT пороги с гистерезисом
    double excellentOut = excellentIn - 0.05;
    double goodOut = goodIn - 0.05;
    double fairOut = fairIn - 0.05;
    
    // ... остальная логика без изменений
}
```

---

## ✅ ФИНАЛЬНЫЙ СПИСОК НАСТРОЕК

### ОСТАЮТСЯ БЕЗ ИЗМЕНЕНИЙ (6 настроек):

1. ✅ `network_quality_enabled` - Включить анализ
2. ✅ `network_quality_overlay` - Показывать в оверлее
3. ✅ `network_quality_use_smoothed` - Использовать сглаженные данные
4. ✅ `quality_history_size` - Размер буфера
5. ✅ `stability_threshold` - Порог стабильности
6. ✅ `quality_threshold` - Порог для оптимизации

### НОВЫЕ НАСТРОЙКИ (5 настроек):

7. 🆕 `network_quality_mode` - standard/context/hybrid
8. 🆕 `network_quality_context_sync` - Синхронизация с color zones
9. 🆕 `network_quality_context_profile` - Профиль для Context (если sync=false)
10. 🆕 `quality_display_thresholds` - Адаптивные/Фиксированные цветовые пороги
11. 🆕 `quality_context_display_profile` - Какой профиль использовать для окраски Context

### ИТОГО: 11 настроек (6 старых + 5 новых)

---

## 🎯 РЕКОМЕНДУЕМЫЕ ДЕФОЛТЫ

```ini
[ADVANCED]
# Существующие
network_quality_enabled = True
network_quality_overlay = False
network_quality_use_smoothed = False
quality_history_size = 100
stability_threshold = 0.15
quality_threshold = 0.8

# Новые (Гибридный режим)
network_quality_mode = hybrid                    # Показывать обе оценки
network_quality_context_sync = True              # Синхронизация с zones
network_quality_context_profile = medium         # Fallback если sync=False
quality_display_thresholds = adaptive            # Адаптивные пороги
quality_context_display_profile = sync_with_context  # Цвета по Context профилю
```

---

## 📱 ОТОБРАЖЕНИЕ В OVERLAY (Примеры)

### Режим: Standard Only

```
NET: 🟢 GOOD 85%
```

### Режим: Context Only

```
NET: 🟢 EXCELLENT 92% (Very Low)
```

### Режим: Hybrid (рекомендуемый)

```
NET: 🟢 GOOD 85% | VPN: 🟢 EXCELLENT 92%
     ↑ Standard      ↑ Context (Very Low)
```

Или компактная версия:
```
NET: 🟢 85% | 🟢 92%ᵛˡ
     ↑std    ↑context
```

---

## 🔧 ПЛАН РЕАЛИЗАЦИИ

### Фаза 1: Базовая структура профилей
- [ ] Добавить константы профилей (Very Low/Low/Medium/High)
- [ ] Загрузка порогов по профилю в Initialize()
- [ ] Расчет двух Quality (Standard + Context)

### Фаза 2: UI Settings
- [ ] Добавить новые настройки в settings.ini
- [ ] Создать UI элементы в AdvancedSettingsForm
- [ ] RadioButtons для режима (Standard/Context/Hybrid)
- [ ] Checkbox для синхронизации
- [ ] ComboBox для профиля Context

### Фаза 3: Адаптивные цветовые зоны
- [ ] Создать QualityDisplayThresholds класс
- [ ] Загрузка порогов по профилю
- [ ] Обновить GetQualityLevelWithHysteresis()
- [ ] Добавить параметр профиля в вызовы

### Фаза 4: Overlay Display
- [ ] Обновить FormatNetworkQuality()
- [ ] Режим Standard: показать один рейтинг
- [ ] Режим Context: показать один рейтинг
- [ ] Режим Hybrid: показать оба рейтинга
- [ ] Компактный формат для Hybrid

### Фаза 5: GUI Display
- [ ] Показать оба рейтинга в AdvancedSettingsForm
- [ ] Обновить UpdateQualityDisplay()
- [ ] Separate labels для Standard/Context

---

## ✅ ОТВЕТ НА ВАШ ВОПРОС

### Старые настройки нужны? **ДА, ВСЕ 6!** ✅

1. ✅ Включить анализ - базовый переключатель
2. ✅ Показывать в оверлее - нужен + новый режим
3. ✅ Сглаженные данные - независимая опция
4. ✅ Размер буфера - для расчета стабильности
5. ✅ Порог стабильности - для CV calculation
6. ✅ Порог качества - для триггера оптимизации

### Цветовые зоны оценок? **ДА, нужны адаптивные!** 🎨

**Сейчас:** Фиксированные (90%/75%/50%)
**Нужно:** Адаптивные по профилю
- Very Low: 85%/70%/45%
- Medium: 90%/75%/50% (текущие)
- High: 95%/85%/65%

**Это обеспечит визуальную согласованность:** Цвет зоны ping = Цвет рейтинга Quality

---

**Хотите, чтобы я начал реализацию с Фазы 1?** 🚀
