# Проверка сглаживания в главном окне (GUI)

## Дата проверки
8 ноября 2025

## Архитектура отображения в GUI

### Принцип работы:
1. **Цвет зоны** определяется по **RAW значению** из `UnifiedDataSource.Snapshot()`
2. **Отображаемое значение** берется напрямую из `App.meterState` и **сглаживается один раз**

Это правильная архитектура, потому что:
- ✅ Цвет отражает **реальное качество** соединения (без задержки сглаживания)
- ✅ Число плавно меняется благодаря сглаживанию (не дергается)
- ✅ Нет двойного сглаживания

## Детальная проверка метрик

### 1. PING (Пинг)

#### Определение цвета зоны (строка 1843):
```csharp
var snap = Classes.UnifiedDataSource.Snapshot();
var pingZone = zoner.FromPing(snap.PingAvgMs); // <- RAW значение
Color PingColor = Classes.ZoneColors.ToColor(pingZone);
```
- Использует `snap.PingAvgMs` (RAW значение после исправления `AvgPingForZone()`)
- **Статус**: ✅ Правильно

#### Отображаемое значение (строки 1867-1888):
```csharp
// Получаем RAW значение напрямую из App.meterState
int rawPing = 0;
if (App.meterState.TcpPing >= 1000 && App.meterState.IsUdpPingValid)
{
    rawPing = (int)Math.Round(server.UdpPing);
}
else if (server.Ping > 0 && server.Ping < 10000)
{
    rawPing = server.Ping;
}
else if (App.meterState.IcmpPing > 0 && App.meterState.IcmpPing < 1000)
{
    rawPing = App.meterState.IcmpPing;
}

// Применяем сглаживание ОДИН РАЗ для отображения
int displayPing = rawPing > 0 ? Classes.SmoothingManager.SmoothPingValueGui(rawPing) : 0;
string pingText = rawPing > 0 ? $"{displayPing} ms" : "n/a ms";
```
- Берет значение напрямую из `App.meterState` (НЕ из snapshot)
- Применяет сглаживание через `SmoothPingValueGui()` **один раз**
- **Статус**: ✅ Правильно

**Вывод**: Нет двойного сглаживания. Цвет и значение берутся из разных источников, но оба используют RAW данные перед сглаживанием.

---

### 2. TICKRATE (Тикрейт)

#### Определение цвета зоны (строка 1844):
```csharp
var tickrateZone = zoner.FromTickrate(snap.TickrateAvgHz); // <- RAW значение
Color TickRateColor = Classes.ZoneColors.ToColor(tickrateZone);
```
- Использует `snap.TickrateAvgHz` (RAW значение `OutputTickRate`)
- **Статус**: ✅ Правильно

#### Отображаемое значение (строка 1913):
```csharp
int rawTickrate = App.meterState.OutputTickRate;
// Применяем сглаживание для GUI значений тикрейта, если включено
int displayTickrate = Classes.SmoothingManager.SmoothTickrateValueGui(rawTickrate);
string tickrateText = displayTickrate.ToString();
```
- Берет значение напрямую из `App.meterState.OutputTickRate`
- Применяет сглаживание через `SmoothTickrateValueGui()` **один раз**
- **Статус**: ✅ Правильно

**Вывод**: Нет двойного сглаживания.

---

### 3. TRAFFIC (Трафик Upload/Download)

#### Отображаемое значение (строки 1924-1927):
```csharp
double uploadMb = App.meterState.UploadTraffic / (1024d * 1024d);
double downloadMb = App.meterState.DownloadTraffic / (1024d * 1024d);
string trafficDisplayText = hasActiveSession
    ? $"{uploadMb:N2} / {downloadMb:N2} mb"
    : $"{0f:N2} / {0f:N2} mb";
```
- Берет значения напрямую из `App.meterState`
- **НЕ применяет сглаживание** в GUI (сглаживание только в Overlay)
- **Статус**: ✅ Правильно (для GUI сглаживание трафика не нужно)

**Вывод**: В GUI трафик не сглаживается - это нормально.

---

## Сравнение GUI vs Overlay

| Метрика | GUI | Overlay | Совпадает? |
|---------|-----|---------|------------|
| **Ping - Цвет** | RAW из `snap.PingAvgMs` | RAW из `snap.PingAvgMs` | ✅ Да |
| **Ping - Значение** | `SmoothPingValueGui(rawPing)` x1 | `SmoothPingValueOverlay(snap.PingAvgMs)` x1 | ✅ Разные методы, но один раз |
| **Tickrate - Цвет** | RAW из `snap.TickrateAvgHz` | RAW из `snap.TickrateAvgHz` | ✅ Да |
| **Tickrate - Значение** | `SmoothTickrateValueGui(rawTickrate)` x1 | `SmoothTickrateValueOverlay(OutputTickRate)` x1 | ✅ Разные методы, но один раз |
| **Traffic** | Без сглаживания | `SmoothUpload/DownloadMbOverlay()` x1 | ✅ По дизайну разные |

---

## Важные особенности архитектуры

### ✅ Правильная разделение источников данных:

1. **Для цвета зоны**: используется `UnifiedDataSource.Snapshot()`
   - Единый источник для GUI и Overlay
   - RAW значения для точного определения качества
   
2. **Для отображаемого значения в GUI**: используется `App.meterState` напрямую
   - Не зависит от snapshot
   - Применяется сглаживание **один раз** через `SmoothXxxValueGui()`

3. **Для отображаемого значения в Overlay**: используется `UnifiedDataSource.Snapshot()`
   - Единый источник с GUI для цвета
   - Применяется сглаживание **один раз** через `SmoothXxxValueOverlay()`

### Почему это правильно?

- **Консистентность цветов**: GUI и Overlay используют ОДИНАКОВЫЕ RAW данные для определения зоны
- **Нет двойного сглаживания**: Каждое значение сглаживается ровно один раз в точке отображения
- **Независимые настройки**: GUI и Overlay могут иметь разные настройки сглаживания
- **Гибкость**: Можно включить/выключить сглаживание для GUI и Overlay независимо

---

## Итоговая оценка: ✅ ВСЁ ПРАВИЛЬНО

В главном окне (GUI):
- ✅ Нет двойного сглаживания
- ✅ Нет пропущенного сглаживания (где оно должно быть)
- ✅ Правильная архитектура разделения данных
- ✅ Согласованность с Overlay по цветам зон
- ✅ Значения не дергаются при включенном сглаживании

## Рекомендации

Никаких изменений в GUI не требуется. Архитектура правильная и работает как задумано.
