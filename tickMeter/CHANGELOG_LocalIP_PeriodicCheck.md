# Изменения: Периодическая проверка LocalIP для активного процесса

## Дата: 20 октября 2025

## Проблема
Локальный IP и адаптер обновлялись **только при смене активного процесса**. Если процесс оставался тем же, но пользователь менял сетевой адаптер или IP адрес изменялся динамически, приложение продолжало использовать старый IP.

### Пример проблемной ситуации:
1. Игра запущена (`game.exe`), определен LocalIP = `192.168.1.100` (Wi-Fi)
2. Пользователь переключается на Ethernet → новый IP `192.168.0.50`
3. Процесс остается `game.exe` → **IP НЕ обновлялся**
4. Приложение продолжало фильтровать пакеты по старому IP

---

## Решение

### 1. Добавлена периодическая проверка IP для одного и того же процесса

**Файл:** `Classes\LocalIPDetector.cs`

**Изменения:**
- Добавлено поле `_lastProcessName` для отслеживания последнего проверенного процесса
- Добавлен интервал `PeriodicCheckInterval = 30 секунд` для периодической проверки
- Улучшена логика кэширования:
  - **Разные процессы**: проверка не чаще 5 сек (DetectionCooldown)
  - **Один и тот же процесс**: проверка не чаще 30 сек (PeriodicCheckInterval)

```csharp
private static string _lastProcessName = string.Empty;
private static readonly TimeSpan PeriodicCheckInterval = TimeSpan.FromSeconds(30);
```

### 2. Обновлена логика определения IP

**Метод:** `DetectLocalIPForActiveProcess(string processName)`

```csharp
bool isSameProcess = !string.IsNullOrEmpty(processName) && 
                    processName == _lastProcessName;

// Для ТОГО ЖЕ процесса: проверяем по PeriodicCheckInterval (30 сек)
// Для РАЗНЫХ процессов: проверяем по DetectionCooldown (5 сек)
TimeSpan checkInterval = isSameProcess ? PeriodicCheckInterval : DetectionCooldown;
```

### 3. Обновлен метод кэширования

```csharp
private static void UpdateCache(string ip, string processName = null)
{
    _lastDetectedIP = ip;
    _lastDetectionTime = DateTime.Now;
    _lastProcessName = processName ?? string.Empty; // Сохраняем имя процесса
}
```

### 4. Изменена логика в GUI

**Файл:** `Forms\GUI.cs`

**Метод:** `updateMetherStateFromActiveWindow()`

**Было:**
- Проверка IP **только при смене процесса** (`if (processChanged) { ... }`)

**Стало:**
- Проверка IP **всегда вызывается** для активного процесса
- При смене процесса → `ResetCache()` для немедленного обновления
- Для того же процесса → использует встроенный таймер (30 сек)

```csharp
// При смене процесса - сбрасываем кэш для немедленного обновления
if (processChanged)
{
    Debug.Print($"[updateMetherStateFromActiveWindow] Process changed...");
    Classes.LocalIPDetector.ResetCache();
}

// Проверяем IP (для нового процесса - немедленно, для того же - по таймеру)
string newLocalIP = Classes.LocalIPDetector.DetectLocalIPForActiveProcess(currentProcessName);
```

---

## Интервалы обновления

| Событие | Интервал обновления |
|---------|-------------------|
| **Смена процесса** (A → B) | ✅ **Немедленно** (сброс кэша) |
| **Тот же процесс** (A → A) | ✅ **Каждые 30 сек** (PeriodicCheckInterval) |
| **Проверка активного окна** | Каждую **1 секунду** (ticksLoop) |
| **Минимальный интервал** | **5 секунд** (DetectionCooldown) |

---

## Преимущества

✅ **Автоматическая адаптация** к изменениям сети  
✅ **Динамическое переключение адаптеров** без перезапуска  
✅ **Защита от частых вызовов** (кэширование)  
✅ **Оптимизированная производительность** (не чаще 30 сек для одного процесса)  
✅ **Синхронизация UI** (textbox + ComboBox адаптера)  

---

## Логи для диагностики

При включенном Debug режиме можно отслеживать обновления:

```
[LocalIPDetector] Starting IP detection for process 'game.exe' (same process: True)
[LocalIPDetector] Определен IP по соединениям процесса game.exe: 192.168.0.50
[updateMetherStateFromActiveWindow] LocalIP changed: 192.168.1.100 -> 192.168.0.50
[updateMetherStateFromActiveWindow] ✓ Successfully updated LocalIP for process 'game.exe' to 192.168.0.50
```

---

## Режимы, в которых работает

Периодическая проверка активна при включенных режимах:
- `capture_all_adapters` = True
- `vpn_bypass_basic` = True
- `vpn_bypass_advanced` = True

---

## Тестирование

### Сценарий 1: Смена адаптера во время работы
1. Запустить игру
2. Подождать определения IP (например, Wi-Fi: 192.168.1.100)
3. Переключиться на Ethernet
4. **Результат:** В течение 30 секунд IP обновится автоматически

### Сценарий 2: Динамическое изменение IP (DHCP)
1. Процесс активен с IP 10.0.0.50
2. DHCP сервер выдает новый IP 10.0.0.100
3. **Результат:** IP обновится в течение 30 секунд

### Сценарий 3: Смена процесса
1. Процесс A (IP: 192.168.1.100)
2. Переключение на процесс B
3. **Результат:** IP определяется **немедленно** (ResetCache)

---

## Совместимость

✅ Полностью совместимо с существующим кодом  
✅ Не ломает логику для профилей игр  
✅ Работает с мультиадаптерным режимом  
✅ Совместимо с VPN bypass режимами  

---

## Файлы изменены

1. `Classes\LocalIPDetector.cs` — добавлена периодическая проверка
2. `Forms\GUI.cs` — изменена логика вызова DetectLocalIPForActiveProcess

Компиляция: ✅ **Без ошибок**
