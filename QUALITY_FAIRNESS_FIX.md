# Quality System Fairness Fix

## Проблемы до исправления

### 1. ❌ Двойной штраф за вариативность пинга
**Проблема:** Jitter и PingStability - это одна сущность (разброс пинга), но карались независимо.

**Старые веса:**
- `PingStability`: 27%
- `Jitter penalty`: 10%
- **Итого: 37% за одно свойство**

**Пример:** 
- Средний ping = 50ms (норм)
- Jitter = 25ms → PingStability = 0.00
- Потеря качества: 27% (stability) + 5% (jitter) = **32% за скачки**

### 2. ❌ Двойной штраф за tickrate
**Проблема:** Ticktime ≈ 1000/tickrate. Низкий tickrate → высокий ticktime → двойной штраф.

**Старые веса:**
- `TickrateStability`: 27%
- `TicktimeStability`: 16%
- `TickrateLevelPenalty`: 3%
- `TicktimeLevelPenalty`: 2%
- **Итого: 48% за одно свойство**

**Пример:**
- Tickrate = 10 Hz (низкий)
- Ticktime = 100ms (высокий, потому что 1000/10)
- Потеря качества: **48% за одну проблему**

### 3. ⚠️ Профиль слабо влияет
**Проблема:** Профиль менял только level penalties (10%), но не stability weights (70%).

**Старое поведение:**
- Very Low профиль: pingGood=50ms, но вес stability всё равно 70%
- High профиль: pingGood=20ms, вес stability тоже 70%
- **Профиль влиял только на 10% оценки**

## Исправления

### ✅ Fix 1: Убран Jitter penalty
```csharp
// БЫЛО:
quality += PingStability * 0.27f;
quality += (1.0f - jitterPenalty) * 0.10f; // ДВОЙНОЙ СЧЁТ

// СТАЛО:
quality += PingStability * pingStabilityWeight;
// Jitter уже учтён в PingStability через коэффициент вариации (CV)
```

**Логика:** `CalculateStability()` использует `stdDev / mean`, где stdDev включает все скачки (включая jitter).

### ✅ Fix 2: Убран Ticktime
```csharp
// БЫЛО:
quality += TickrateStability * 0.27f;
quality += TicktimeStability * 0.16f;  // ДВОЙНОЙ СЧЁТ
quality += (1f - tickrateLevelPenalty) * 0.03f;
quality += (1f - ticktimeLevelPenalty) * 0.02f; // ДВОЙНОЙ СЧЁТ

// СТАЛО:
quality += TickrateStability * tickrateStabilityWeight;
quality += (1f - tickrateLevelPenalty) * tickrateLevelWeight;
// Ticktime убран - это производная от tickrate
```

**Логика:** Ticktime = 1000 / tickrate. Одна метрика, один вклад.

### ✅ Fix 3: Адаптивные веса по профилю
```csharp
// Профиль влияет на баланс stability vs level penalties
switch (profileName)
{
    case "very_low":
        stabilityFactor = 0.50f; // 50% - мягче к скачкам
        levelFactor = 0.40f;     // 40% - важнее средние значения
        break;
    case "low":
        stabilityFactor = 0.60f;
        levelFactor = 0.30f;
        break;
    case "medium":
        stabilityFactor = 0.65f;
        levelFactor = 0.25f;
        break;
    case "high":
        stabilityFactor = 0.75f; // 75% - строго к скачкам
        levelFactor = 0.15f;     // 15% - меньше важны средние
        break;
}
```

**Логика:**
- **Very Low профиль** (VPN-игроки): скачки неизбежны → меньше вес stability, больше вес средних значений
- **High профиль** (про-игроки): требуется стабильность → больше вес stability

## Новая структура весов

### Распределение (TOTAL = 100%)
- **Stability**: 50-75% (зависит от профиля)
  - PingStability: 50% от stability
  - TickrateStability: 50% от stability
- **Level penalties**: 15-40% (зависит от профиля)
  - PingLevelPenalty: 55% от level
  - TickrateLevelPenalty: 45% от level
- **Packet Loss**: 10% (константа)

### Пример весов для Medium профиля
- **Stability (65%)**:
  - PingStability: 32.5% (0.65 * 0.5)
  - TickrateStability: 32.5% (0.65 * 0.5)
- **Level (25%)**:
  - PingLevelPenalty: 13.75% (0.25 * 0.55)
  - TickrateLevelPenalty: 11.25% (0.25 * 0.45)
- **Packet Loss**: 10%
- **ИТОГО**: 100%

## Ожидаемый эффект

### До исправления (ваши логи)
```
[Quality] Standard=0,174(Critical) Context=0,199(Critical)
[Quality] Stability: Ping=0,00 TR=0,00 TT=0,00 Jitter=24,6ms
```
**Расчёт потерь:**
- PingStability=0 → -27%
- TickrateStability=0 → -27%
- TicktimeStability=0 → -16%
- Jitter penalty → -5%
- **Итого: ~75% потерь** (из них ~45% двойной счёт)

### После исправления (прогноз)
```
[Quality] Standard=0,350(Fair) Context=0,450(Fair)
[Quality] Stability: Ping=0,00 TR=0,00 Jitter=24,6ms
```
**Расчёт потерь (Medium):**
- PingStability=0 → -32.5%
- TickrateStability=0 → -32.5%
- **Итого: ~65% потерь** (без двойного счёта)
- **Quality ≈ 35-45%** (Fair вместо Critical)

## Справедливость восстановлена

1. ✅ **Нет двойного штрафа**: каждая метрика учитывается один раз
2. ✅ **Профиль влияет сильно**: Very Low = +40% к уровням, High = +75% к stability
3. ✅ **Монотонность сохранена**: лучше метрика → лучше качество
4. ✅ **StandardQuality независим**: всегда использует Medium профиль
5. ✅ **ContextQuality адаптивный**: учитывает ожидания пользователя

## Тестирование

### Сценарий 1: Идеальная стабильность
**Входные данные:**
- Ping = 30ms ± 2ms (стабильно)
- Tickrate = 64 Hz ± 1 Hz
- Jitter = 2ms

**Ожидание:**
- PingStability ≈ 0.95
- TickrateStability ≈ 0.98
- StandardQuality ≈ **0.85-0.90** (Excellent/Good)
- ContextQuality (Very Low) ≈ **0.90-0.95** (Excellent)

### Сценарий 2: Средние значения ок, скачки есть
**Входные данные:**
- Ping = 50ms ± 25ms (нестабильно)
- Tickrate = 60 Hz ± 10 Hz
- Jitter = 25ms

**Ожидание:**
- PingStability ≈ 0.20
- TickrateStability ≈ 0.40
- StandardQuality ≈ **0.35-0.45** (Fair) ← было 0.17 (Critical)
- ContextQuality (Very Low) ≈ **0.50-0.60** (Fair/Good) ← профиль спасает

### Сценарий 3: Только профиль меняется
**Входные данные:** одинаковые метрики

**Ожидание:**
- StandardQuality = **константа** (всегда Medium)
- ContextQuality (Very Low) > ContextQuality (Medium) > ContextQuality (High)
