# TickMeter Performance Polish TODO

## Высокий приоритет (Performance Critical)

### 1. GUI.cs - Главный цикл оптимизация
- [x] **Анти-реэнтерабельность ticksLoop** - добавить `Interlocked` защиту от пересекающихся тиков
- [x] **Троттлинг RTSS обновлений** - не каждый тик, а по таймеру (экономия CPU)
- [ ] **Один UI-апдейтер** - батчевые обновления всех контролов за один проход
- [ ] **Адаптивная частота** - понижение FPS при неактивном окне

### 2. PCAP буферизация и фильтрация
- [x] **Kernel buffer tuning** - увеличение до 4-16MB через рефлексию
- [x] **MinToCopy optimization** - настройка min-to-copy для снижения системных вызовов
- [ ] **Non-blocking режим** - если поддерживается библиотекой
- [x] **Улучшенные BPF фильтры** - безопасное применение из настроек

### 3. PacketStats Virtual Mode
- [ ] **ListView VirtualMode** - кольцевой буфер для >2000 строк
- [ ] **Struct-based модель** - легкие Row вместо ListViewItem в памяти
- [ ] **Batch UI updates** - один таймер 8-10Hz для всех UI элементов
- [ ] **Диагностический StatusBar** - Workers/Queue/Latency счетчики

## Средний приоритет (UX Polish)

### 4. Memory & GC оптимизации
- [ ] **String pooling** - кэш форматированных строк (IP, процессы)
- [ ] **StringBuilder pooling** - переиспользование в RTSS
- [ ] **Adaptive GC** - умная сборка мусора по состоянию UI
- [ ] **IPEndPoint кэширование** - TTL кэш по PID (5-10 сек)

### 5. Threading улучшения
- [ ] **Thread priority** - `AboveNormal` для PCAP воркеров
- [ ] **Single Consumer pattern** - один агрегатор для всех адаптеров
- [ ] **ConcurrentQueue optimization** - размерные лимиты по адаптерам

### 6. Настройки и диагностика
- [ ] **Instant Apply** - изменения без перезапуска где возможно
- [ ] **Performance counters** - UI latency, packet drops, memory usage
- [ ] **Virtual List toggle** - кнопка вкл/выкл в StatusBar для тестов
- [ ] **PCAP settings UI** - kernel buffer/min-to-copy в Advanced

## Низкий приоритет (Nice to Have)

### 7. Визуальные улучшения
- [ ] **Smooth charts** - EMA сглаживание линий графиков без потери данных
- [ ] **Adaptive scaling** - автомасштабирование осей по содержимому
- [ ] **Color coding** - цветовая индикация качества соединения

### 8. Advanced features
- [ ] **Packet timing analysis** - гистограммы межпакетных интервалов
- [ ] **Network topology detection** - автоопределение сетевой структуры
- [ ] **Export/Import settings** - профили конфигураций

## Реализация по этапам

### Этап 1: Core Performance (1-2 дня)
1. GUI.cs - анти-реэнтерабельность и троттлинг
2. PCAP kernel buffer tuning
3. PacketStats Virtual Mode base

### Этап 2: Memory & Threading (1 день)  
1. String/StringBuilder pooling
2. Thread priorities
3. Single Consumer pattern

### Этап 3: UX Polish (1 день)
1. Диагностические счетчики
2. Instant Apply настроек  
3. Performance monitoring UI

---

**Приоритет реализации:** Этап 1 → базовые performance фиксы → Этап 2 → Этап 3
**Цель:** Плавная работа на высоких нагрузках + отзывчивый UI без фризов