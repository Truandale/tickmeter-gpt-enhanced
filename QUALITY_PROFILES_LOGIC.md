# Network Quality Profiles - Логика расчета

## 📐 ЧТО ОСТАЕТСЯ БЕЗ ИЗМЕНЕНИЙ

### Формула расчета Quality (100% сохранена):

```csharp
OverallQuality = 
    // === СТАБИЛЬНОСТЬ (80% веса) ===
    + PingStability      × 0.30  (30%)
    + TickrateStability  × 0.30  (30%)
    + TicktimeStability  × 0.20  (20%)
    
    // === LEVEL PENALTIES (10% веса) ===
    + (1 - PingLevelPenalty)     × 0.05  (5%)
    + (1 - TickrateLevelPenalty) × 0.03  (3%)
    + (1 - TicktimeLevelPenalty) × 0.02  (2%)
    
    // === ДОПОЛНИТЕЛЬНЫЕ ФАКТОРЫ (10% веса) ===
    + (1 - JitterPenalty)      × 0.10  (10%)
    + (1 - PacketLossPenalty)  × 0.10  (10%)
    
    // === EMA СГЛАЖИВАНИЕ ===
    × EMA smoothing (alpha = 0.15)
```

**ВСЕ ЭТО НЕ МЕНЯЕТСЯ!** ✅

---

## 🔧 ЧТО МЕНЯЕТСЯ (ТОЛЬКО ПОРОГИ)

### Текущий код (фиксированные пороги):

```csharp
// БЫЛО:
private static float _pingGoodMs = 30f;     // Хардкод
private static float _pingBadMs = 80f;      // Хардкод
private static float _ticktimeGoodMs = 8f;  // Хардкод
private static float _ticktimeBadMs = 16f;  // Хардкод
```

### Новый код (пороги по профилю):

```csharp
// СТАНЕТ:
private static float _pingGoodMs;        // Загружается из профиля
private static float _pingBadMs;         // Загружается из профиля
private static float _ticktimeGoodMs;    // Загружается из профиля
private static float _ticktimeBadMs;     // Загружается из профиля

// В Initialize():
switch (qualityProfile)
{
    case "very_low":
        _pingGoodMs = 50f;     // Вместо 30f
        _pingBadMs = 150f;     // Вместо 80f
        _ticktimeGoodMs = 10f; // Вместо 8f
        _ticktimeBadMs = 20f;  // Вместо 16f
        break;
    // ... другие профили
}
```

**Меняются ТОЛЬКО значения порогов, НЕ логика расчета!** ✅

---

## 📊 ПРИМЕРЫ РАСЧЕТА

### Пример 1: Ping = 50ms (Medium Profile)

**Текущие пороги (Medium):**
- Good: <30ms
- Bad: >80ms

**Расчет PingLevelPenalty:**
```
avgPing = 50ms
_pingGoodMs = 30ms
_pingBadMs = 80ms

pingLevelPenalty = (50 - 30) / (80 - 30)
                 = 20 / 50
                 = 0.40 (40% штраф)

Contribution to quality = (1 - 0.40) × 0.05 = 0.60 × 0.05 = 0.03 (3%)
```

**Результат:** Ping добавляет только 3% к общему качеству (из возможных 5%)

---

### Пример 2: Ping = 50ms (Very Low Profile)

**Новые пороги (Very Low):**
- Good: <50ms
- Bad: >150ms

**Расчет PingLevelPenalty:**
```
avgPing = 50ms
_pingGoodMs = 50ms  ← Изменилось!
_pingBadMs = 150ms  ← Изменилось!

pingLevelPenalty = (50 - 50) / (150 - 50)
                 = 0 / 100
                 = 0.00 (0% штраф) ← Нет штрафа!

Contribution to quality = (1 - 0.00) × 0.05 = 1.00 × 0.05 = 0.05 (5%)
```

**Результат:** Ping добавляет полные 5% к качеству (50ms считается "Good")

---

### Визуальное сравнение:

```
Ping = 50ms:

Medium Profile:
┌──────────────────────────────────────────┐
│ Stability:    0.90 × 0.30 = 0.270 (27%) │
│ Level:        0.60 × 0.05 = 0.030 (3%)  │ ← Штраф 40%
│ Jitter:       1.00 × 0.10 = 0.100 (10%) │
│ PacketLoss:   0.95 × 0.10 = 0.095 (9.5%)│
│ Total:                      0.495 (49.5%)│ ← Fair 🟡
└──────────────────────────────────────────┘

Very Low Profile:
┌──────────────────────────────────────────┐
│ Stability:    0.90 × 0.30 = 0.270 (27%) │ ← Та же стабильность
│ Level:        1.00 × 0.05 = 0.050 (5%)  │ ← Нет штрафа!
│ Jitter:       1.00 × 0.10 = 0.100 (10%) │ ← Тот же jitter
│ PacketLoss:   0.95 × 0.10 = 0.095 (9.5%)│ ← Те же потери
│ Total:                      0.515 (51.5%)│ ← Fair/Good 🟢
└──────────────────────────────────────────┘

Разница: 0.515 - 0.495 = 0.02 (2% разницы)
         Из-за разных порогов "Good" ping!
```

---

## 🔍 ДЕТАЛЬНАЯ РАЗБИВКА ВЕСОВ

### Что зависит от профиля:

```
✓ PingLevelPenalty      (5% веса)  ← Зависит от _pingGoodMs/_pingBadMs
✓ TicktimeLevelPenalty  (2% веса)  ← Зависит от _ticktimeGoodMs/_ticktimeBadMs

ИТОГО: 7% от общего качества зависит от профиля
```

### Что НЕ зависит от профиля:

```
✗ PingStability         (30% веса) ← Всегда одинаковый расчет CV
✗ TickrateStability     (30% веса) ← Всегда одинаковый расчет CV
✗ TicktimeStability     (20% веса) ← Всегда одинаковый расчет CV
✗ TickrateLevelPenalty  (3% веса)  ← Зависит от целевого tickrate (отдельная настройка)
✗ JitterPenalty         (10% веса) ← Всегда: jitter / 50ms
✗ PacketLossPenalty     (10% веса) ← Всегда: loss / 5%

ИТОГО: 93% от общего качества НЕ зависит от профиля
```

---

## 📈 ВЛИЯНИЕ ПРОФИЛЕЙ НА ИТОГОВОЕ КАЧЕСТВО

### Максимальное влияние профиля:

```
Сценарий: Ping и Ticktime идеальны для одного профиля, но ужасны для другого

Very Low Profile (ping 50ms, ticktime 10ms):
- PingLevelPenalty = 0% (в зоне Good)
- TicktimeLevelPenalty = 0% (в зоне Good)
- Contribution = 7% (максимум)

High Profile (тот же ping 50ms, ticktime 10ms):
- PingLevelPenalty = 75% (далеко от Good <20ms)
- TicktimeLevelPenalty = 67% (далеко от Good <6ms)
- Contribution = ~2.3% (сильный штраф)

Разница: 7% - 2.3% = 4.7%
```

**Вывод:** Профиль может повлиять на итоговое качество **максимум на ±5%**

---

## 🎯 ПРАКТИЧЕСКИЕ ПРИМЕРЫ

### Пример A: Стабильное соединение, средний ping

**Метрики:**
- Ping: 45ms (стабильный, CV < 10%)
- Tickrate: 120Hz (стабильный, CV < 8%)
- Ticktime: 9ms (стабильный, CV < 12%)
- Jitter: 15ms
- PacketLoss: 1%

**Расчет для Medium Profile:**
```
PingStability:      0.95 × 0.30 = 0.285
TickrateStability:  0.96 × 0.30 = 0.288
TicktimeStability:  0.92 × 0.20 = 0.184
PingLevelPenalty:   (45-30)/(80-30) = 0.30 → (1-0.30)×0.05 = 0.035
TickrateLevelPenalty: ~0.06 → 0.94×0.03 = 0.028
TicktimeLevelPenalty: (9-8)/(16-8) = 0.125 → (1-0.125)×0.02 = 0.0175
JitterPenalty:      15/50 = 0.30 → (1-0.30)×0.10 = 0.070
PacketLossPenalty:  1/5 = 0.20 → (1-0.20)×0.10 = 0.080

Total = 0.285 + 0.288 + 0.184 + 0.035 + 0.028 + 0.0175 + 0.070 + 0.080
      = 0.9875 × EMA
      ≈ 0.85 (85%) → GOOD 🟢
```

**Расчет для Very Low Profile (тот же пример):**
```
PingStability:      0.95 × 0.30 = 0.285  (та же)
TickrateStability:  0.96 × 0.30 = 0.288  (та же)
TicktimeStability:  0.92 × 0.20 = 0.184  (та же)
PingLevelPenalty:   (45-50)/(150-50) = -0.05 → 0 (нет штрафа) → 0.05
TickrateLevelPenalty: 0.94×0.03 = 0.028  (та же)
TicktimeLevelPenalty: (9-10)/(20-10) = -0.1 → 0 (нет штрафа) → 0.02
JitterPenalty:      0.070  (та же)
PacketLossPenalty:  0.080  (та же)

Total = 0.285 + 0.288 + 0.184 + 0.05 + 0.028 + 0.02 + 0.070 + 0.080
      = 1.005 (cap at 1.0) × EMA
      ≈ 0.89 (89%) → GOOD 🟢
```

**Разница:** 89% - 85% = **4% выше** в Very Low профиле

---

### Пример B: Нестабильное соединение, высокий ping

**Метрики:**
- Ping: 120ms (нестабильный, CV = 25%)
- Tickrate: 90Hz (нестабильный, CV = 18%)
- Ticktime: 14ms (нестабильный, CV = 22%)
- Jitter: 35ms
- PacketLoss: 4%

**Расчет для Medium Profile:**
```
PingStability:      0.70 × 0.30 = 0.210
TickrateStability:  0.75 × 0.30 = 0.225
TicktimeStability:  0.68 × 0.20 = 0.136
PingLevelPenalty:   (120-30)/(80-30) = 1.8 → cap at 1.0 → 0 × 0.05 = 0
TickrateLevelPenalty: ~0.30 → 0.70×0.03 = 0.021
TicktimeLevelPenalty: (14-8)/(16-8) = 0.75 → (1-0.75)×0.02 = 0.005
JitterPenalty:      35/50 = 0.70 → (1-0.70)×0.10 = 0.030
PacketLossPenalty:  4/5 = 0.80 → (1-0.80)×0.10 = 0.020

Total = 0.210 + 0.225 + 0.136 + 0 + 0.021 + 0.005 + 0.030 + 0.020
      = 0.647 × EMA
      ≈ 0.56 (56%) → FAIR 🟡
```

**Расчет для Very Low Profile (тот же пример):**
```
PingStability:      0.210  (та же)
TickrateStability:  0.225  (та же)
TicktimeStability:  0.136  (та же)
PingLevelPenalty:   (120-50)/(150-50) = 0.70 → (1-0.70)×0.05 = 0.015
TickrateLevelPenalty: 0.021  (та же)
TicktimeLevelPenalty: (14-10)/(20-10) = 0.40 → (1-0.40)×0.02 = 0.012
JitterPenalty:      0.030  (та же)
PacketLossPenalty:  0.020  (та же)

Total = 0.210 + 0.225 + 0.136 + 0.015 + 0.021 + 0.012 + 0.030 + 0.020
      = 0.669 × EMA
      ≈ 0.58 (58%) → FAIR 🟡
```

**Разница:** 58% - 56% = **2% выше** в Very Low профиле

**Важно:** Даже с Very Low, плохая стабильность все равно дает Fair (нестабильность = 70% веса!)

---

## ✅ ИТОГОВЫЕ ВЫВОДЫ

### Что НЕ меняется (93% логики):

1. ✅ **Веса компонентов:** 30%/30%/20%/10%/10% и 5%/3%/2%
2. ✅ **Формулы стабильности:** Coefficient of Variation (CV)
3. ✅ **Jitter penalty:** Всегда jitter / 50ms
4. ✅ **Packet loss penalty:** Всегда loss / 5%
5. ✅ **EMA сглаживание:** alpha = 0.15
6. ✅ **Hysteresis в RTSS:** 3 секунды hold time
7. ✅ **Пороги рейтингов:** 90%/75%/50% (Excellent/Good/Fair)

### Что меняется (7% логики):

1. 🔧 **Ping level thresholds:** 30/80 → 50/150 (Very Low) или 20/60 (High)
2. 🔧 **Ticktime level thresholds:** 8/16 → 10/20 (Very Low) или 6/12 (High)

### Влияние изменений:

- **Максимальное влияние:** ±5% к итоговому качеству
- **Типичное влияние:** ±2-3% в реальных сценариях
- **Стабильность:** Остается главным фактором (70% веса)

**Философия:** Профили корректируют "ожидания" качества, но не меняют фундаментальную оценку стабильности сети.

---

## 🎮 ПРАКТИЧЕСКИЙ ВЫВОД

**Для пользователя:**

- Very Low: "45ms ping = хорошо" → Quality отражает это
- High: "45ms ping = не идеал" → Quality отражает это

**Но в обоих случаях:**
- Нестабильный ping = плохо (веса стабильности не меняются)
- Высокий jitter = плохо (формула jitter не меняется)
- Потери пакетов = плохо (формула loss не меняется)

**Профили меняют "планку", но не "правила игры".** ✅
