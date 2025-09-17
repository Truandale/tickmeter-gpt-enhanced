# Changelog

All notable changes to TickMeter project will be documented in this file.

## [v2.4.3] - 2025-09-17

### 🔧 Исправление проблем мультиадаптерного режима
- **Исправлена ошибка Ethernet-проверки**: Устранена проблема с сообщением "This program works only on Ethernet networks!" в режиме мультиадаптера - неподдерживаемые адаптеры теперь корректно игнорируются без блокировки всего приложения
- **Исправлена FormatException при парсинге настроек**: Решена проблема с парсингом десятичных чисел из настроек - используется `CultureInfo.InvariantCulture` для корректной обработки чисел независимо от региональных настроек системы
- **Исправлено зависание показателей в мультирежиме**: Добавлен автоматический перезапуск завершившихся background workers для обеспечения непрерывности работы мониторинга в режиме мультиадаптера
- **Исправлен race condition в ActiveWindowTracker**: Устранена ошибка "Элемент с тем же ключом уже был добавлен" при одновременном доступе нескольких потоков к словарю соединений - используется thread-safe подход с TryGetValue

### 🎯 Улучшение стабильности мультиадаптерного режима
- **Корректная обработка неподдерживаемых адаптеров**: Wi-Fi, виртуальные и другие non-Ethernet адаптеры больше не вызывают критических ошибок
- **Устранение региональных проблем**: Парсинг настроек сглаживания теперь работает корректно независимо от языковых настроек Windows
- **Непрерывность мониторинга**: Background workers автоматически перезапускаются при завершении, обеспечивая стабильную работу Live View

### 📁 Затронутые файлы
- `tickMeter/Forms/PacketStats.cs` - исправлена Ethernet-проверка и добавлен перезапуск воркеров
- `tickMeter/Classes/TickMeterState.cs` - исправлен парсинг настроек с InvariantCulture
- `tickMeter/Forms/GUI.cs` - исправлен парсинг настроек и логика перезапуска мультиадаптерных воркеров

### ⚠️ Известные ограничения
- В мультиадаптерном режиме могут наблюдаться периодические колебания показателей из-за асинхронной природы обработки данных от разных адаптеров

## [v2.4.2] - 2025-09-17

### 🔧 Исправление критических ошибок конфигурации
- **Исправлена проблема настройки сглаживания графиков**: Функция `ChartSmoothingEnabled()` теперь корректно читает настройку `smooth_charts` из секции `[SETTINGS]`
- **Унифицированы вызовы GetOption**: Добавлен обязательный параметр секции "SETTINGS" ко всем вызовам `App.settingsManager.GetOption()` для обеспечения корректного чтения настроек
- **Исправлена NullReferenceException**: Добавлена защита от null в методе `PcapWorkerCompleted()` для предотвращения краха приложения в режиме мультиадаптера

### 🎯 Исправленные настройки
- **Основные настройки UI**: `chart`, `ip`, `ping`, `traffic`, `tickrate`, `autodetect`, `data_send`, `session_time`, `rtss`, `run_minimized`
- **Сетевые настройки**: `capture_all_adapters`, `ignore_virtual_adapters`, `ping_bind_to_interface`, `ping_tcp_prefer`, `ping_fallback_icmp`
- **Настройки сглаживания**: `smooth_charts`, `tickrate_smoothing`, `ping_smoothing_factor`, `tickrate_smoothing_factor`
- **Overlay маркеры**: `overlay_ping_spike_marker`, `overlay_tickrate_spike_marker`, `ui_ping_spike_marker`, `ui_tickrate_spike_marker`
- **Параметры алгоритмов**: `smoothing.ping.tau`, `smoothing.ping.spike_abs_ms`, `smoothing.ping.spike_rel`

### 🚀 Улучшение стабильности
- **Корректная загрузка настроек**: Все настройки теперь гарантированно читаются из правильной секции INI-файла
- **Предотвращение крашей**: Устранена возможность падения приложения из-за необработанных null-ссылок
- **Консистентность поведения**: UI-элементы теперь корректно отражают реальное состояние внутренних настроек

### 📁 Затронутые файлы
- `tickMeter/Classes/RivaTuner.cs` - исправлена функция ChartSmoothingEnabled() и другие вызовы GetOption
- `tickMeter/Forms/GUI.cs` - унифицированы вызовы настроек, исправлена NullReferenceException
- `tickMeter/Forms/SettingsForm.cs` - исправлены вызовы GetOption для всех основных настроек
- `tickMeter/Forms/PacketStats.cs` - исправлены настройки мультиадаптера
- `tickMeter/Classes/TickMeterState.cs` - исправлены настройки сглаживания для графиков

## [v2.4.1] - 2025-09-17

### 🔧 Исправление сглаживания в overlay RTSS
- **Исправлена проблема отображения overlay**: Overlay теперь показывает сглаженные (EMA) значения ping и tickrate вместо сырых дёргающихся данных
- **Сглаживание линий графиков**: Добавлено EMA-сглаживание для самих линий графиков в overlay с коэффициентом 0.25
- **Корректные спайк-маркеры**: Индикаторы (!!) теперь применяются к тем же сглаженным значениям, что отображаются в overlay
- **Автоматическое управление**: Сглаживание включается/выключается флагом `tickrate_smoothing` без дополнительных настроек

### 🎯 Техническая реализация
- **RivaTuner.DisplayPingMs/DisplayTickrate**: Новые поля для передачи сглаженных значений из GUI в overlay
- **PrepareSeriesForChart()**: Функция EMA-сглаживания массивов данных для графиков без изменения исходных буферов
- **Интеграция в GUI.cs**: Автоматическая передача `dispPing` и `dispTickrate` в RivaTuner перед `BuildRivaOutput()`
- **Оптимизированная производительность**: Сглаживание применяется к локальной копии данных, минимальное влияние на производительность

### 🚀 Пользовательский опыт
- **Консистентность данных**: Значения в overlay теперь соответствуют значениям в основном окне при включенном сглаживании
- **Плавные графики**: Линии графиков в overlay стали плавными без резких скачков
- **Сохранение функциональности**: При выключенном сглаживании отображаются сырые данные как раньше
- **Улучшенная читаемость**: Спайк-маркеры теперь корректно работают в overlay

## [v2.4.0] - 2025-09-16

### 📊 Независимое сглаживание графиков
- **Раздельное управление сглаживанием**: Добавлена независимая настройка сглаживания для графиков отдельно от отображаемых значений
- **Двухуровневая система EMA**: Реализованы отдельные EMA-фильтры для отображения (display) и графиков (charts)
- **Новый чекбокс в настройках**: "Сглаживание графиков" для независимого контроля сглаживания графических данных
- **Улучшенная гибкость**: Возможность включить сглаживание только для графиков, оставив точные значения для отображения

### ⚙️ Новые настройки
- **smooth_charts**: Флаг для включения/выключения сглаживания графиков (по умолчанию: выключено)
- **App.emaChartTickrate**: Отдельный EMA-фильтр для сглаживания данных графика tickrate
- **App.emaChartPing**: Отдельный EMA-фильтр для сглаживания данных графика ping

### 🔧 Техническая реализация
- **Дублированные буферы данных**: Раздельные буферы для сырых и сглаженных данных в TickMeterState
- **Условная отрисовка**: Графики автоматически выбирают сглаженные или сырые данные в зависимости от настройки
- **Интеграция с RivaTuner**: Overlay поддерживает независимое сглаживание графиков
- **Сохранение производительности**: Минимальное влияние на производительность при отключенном сглаживании

### 🎮 Улучшения пользовательского интерфейса
- **Независимые настройки**: Пользователь может выбрать сглаживание только для графиков или только для значений
- **Автосохранение настроек**: Изменения применяются мгновенно и сохраняются в settings.ini
- **Обратная совместимость**: Все существующие настройки сглаживания сохранены

### 🐛 Исправления
- **Порядок инициализации**: Исправлен NullReferenceException при запуске из-за неправильного порядка инициализации settingsManager
- **Корректное объявление элементов**: Исправлено объявление элементов UI для правильной работы с дизайнером Visual Studio

## [v2.3.1] - 2025-09-16

### 🎯 Улучшенная система спайк-маркеров
- **Раздельные настройки индикаторов**: Добавлены отдельные галки для включения/выключения спайк-маркеров в оверлее и главном окне
- **Гибкие настройки отображения**: 5 независимых флагов для контроля спайк-индикаторов по каждой метрике и области отображения
- **Улучшенный пользовательский интерфейс**: Новые чекбоксы в настройках для точного контроля спайк-маркеров
- **Оптимизированная детекция**: Упрощенная система обнаружения спайков с baseline-трекингом для каждой метрики

### ⚙️ Новые настройки спайк-маркеров
- **overlay_ping_spike_marker**: Отображение спайков ping в оверлее (по умолчанию: включено)
- **overlay_tickrate_spike_marker**: Отображение спайков tickrate в оверлее (по умолчанию: выключено) 
- **overlay_ticktime_spike_marker**: Отображение спайков ticktime в оверлее (по умолчанию: выключено)
- **ui_ping_spike_marker**: Отображение спайков ping в главном окне (по умолчанию: включено)
- **ui_tickrate_spike_marker**: Отображение спайков tickrate в главном окне (по умолчанию: выключено)

### 🔧 Техническая реализация
- **Статические переменные RivaTuner**: Добавлены поля для передачи информации о спайках в оверлей
- **Baseline-трекинг**: Каждая метрика имеет свой baseline для точного определения спайков
- **Write-through настройки**: Изменения настроек применяются мгновенно без перезапуска
- **Интеграция с существующим EMA**: Спайк-маркеры работают поверх системы сглаживания

### 🎮 Инструменты разработчика
- **Кнопка симуляции спайков**: Добавлена возможность тестирования системы спайк-маркеров
- **Контролируемые тестовые спайки**: Ping +100ms, Tickrate -50% на 2 секунды
- **Полная совместимость**: Все существующие функции сохранены и улучшены

## [v2.3.0] - 2025-09-16

### 📈 EMA-сглаживание метрик в реальном времени
- **Экспоненциальное скользящее среднее**: Реализован алгоритм EMA для сглаживания значений tickrate и ping в overlay
- **Настраиваемые параметры**: Добавлены параметры tau для точной настройки времени отклика фильтров (0.6-0.8с для tickrate, 1.0-1.2с для ping)
- **Антиспайк защита для ping**: Автоматическое обнаружение и фильтрация спайков ping с настраиваемыми абсолютными (25ms) и относительными (40%) порогами
- **Раздельная обработка**: Сырые значения сохраняются для расчетов, сглаженные - для отображения
- **Визуальные индикаторы**: Добавлен красный индикатор (!!) при обнаружении спайков ping в overlay
- **Динамическая цветовая схема**: Цвет ping меняется в зависимости от качества соединения (зеленый <30ms, желтый 30-100ms, красный >100ms)

### ⚙️ Новые настройки
- **tickrate_smoothing**: Главный переключатель для включения/выключения сглаживания
- **smoothing.tickrate.tau**: Время константа для сглаживания tickrate (рекомендуется 0.8)
- **smoothing.ping.tau**: Время константа для сглаживания ping (рекомендуется 1.0)
- **smoothing.ping.spike_abs_ms**: Абсолютный порог для обнаружения спайков ping (25ms)
- **smoothing.ping.spike_rel**: Относительный порог для обнаружения спайков ping (0.4 = 40%)

### 🎯 Улучшения пользовательского опыта
- **Стабильное отображение**: Значения в overlay теперь меньше "прыгают", обеспечивая более комфортное восприятие
- **Точная диагностика**: Спайки ping выделяются визуально, помогая быстро выявить проблемы сети
- **Сохранение точности**: Все внутренние расчеты используют неизменные сырые данные для максимальной точности

## [v2.2.1] - 2025-09-15

### 🛡️ Повышение стабильности и обработки ошибок
- **Устойчивость к ошибкам адаптеров**: Добавлена обработка ошибок при открытии отдельных сетевых адаптеров - если один адаптер недоступен, остальные продолжают работу
- **Расширенное логирование**: Подробное логирование старта/остановки воркеров и ошибок через DebugLogger для диагностики проблем
- **Интеллектуальные MessageBox**: MessageBox ошибок показываются только в single-adapter режиме, в multi-режиме используется тихое логирование
- **Улучшенная обработка исключений**: Полноценная обработка исключений в OpenAndCaptureFromAdapter с пробросом в RunWorkerCompleted
- **Корректная очистка ресурсов**: Исправлена логика try/catch для предотвращения утечек ресурсов проблемных адаптеров

### � Исправление настроек виртуальных адаптеров
- **Полная интеграция настроек**: Флаг "Ignore virtual adapters" теперь корректно работает как в основном GUI, так и в Live Packets View
- **Единообразная фильтрация**: Обе части приложения используют одинаковую логику фильтрации виртуальных адаптеров
- **Пользовательский контроль**: Возможность отключить фильтрацию виртуальных адаптеров через настройки для специальных случаев

## [v2.2.0] - 2025-09-15

### 🌐 Полная реализация Multi-Adapter режима в Live Packets View
- **Захват со всех адаптеров**: Теперь Live Packets View реально захватывает пакеты со всех сетевых интерфейсов одновременно, а не только с первого найденного
- **Индивидуальные воркеры**: Каждый сетевой адаптер использует отдельный BackgroundWorker для параллельной обработки пакетов
- **Интеллектуальная фильтрация**: Автоматическое исключение виртуальных адаптеров (loopback, npcap, hyper-v, vmware, virtualbox, vethernet)
- **Стабильная остановка**: Корректная очистка всех воркеров при остановке захвата пакетов
- **Универсальность**: Поддержка настройки "Игнорировать виртуальные адаптеры" для гибкой настройки
- **Производительность**: Оптимизированная обработка пакетов с множественных интерфейсов без блокировки UI

### 🔧 Архитектурные улучшения
- **Единый источник данных**: Live Packets View теперь использует тот же механизм multi-adapter, что и главное окно
- **Совместимость с RTSS**: Overlay корректно получает агрегированные данные со всех адаптеров
- **Упрощение кода**: Удалены избыточные методы StartMultiAdapterCapture и StartSingleAdapterCapture
- **Консистентность**: Унифицированная логика работы с адаптерами во всем приложении
- **Единый источник данных**: Live Packets View теперь использует тот же механизм multi-adapter, что и главное окно
- **Совместимость с RTSS**: Overlay корректно получает агрегированные данные со всех адаптеров
- **Упрощение кода**: Удалены избыточные методы StartMultiAdapterCapture и StartSingleAdapterCapture
- **Консистентность**: Унифицированная логика работы с адаптерами во всем приложении

### 📊 Исправления отображения overlay и графика пинга
### 📊 Исправления отображения overlay и графика пинга
- **Стабильность графика пинга**: Исправлена проблема "замерзшего" графика через восстановление правильного обновления pingBuffer
- **Частота обновления overlay**: Скорректирована частота обновления overlay для соответствия пользовательским интервалам пинга
- **Поток данных пинга**: Восстановлен правильный поток данных от PingManager к отображению в overlay
- **Производительность графика**: Оптимизирована отрисовка графика пинга с подходящими интервалами обновления
- **Синхронизация настроек**: Исправлен таймер overlay для правильной синхронизации с ping_interval настройками
- **Точность отображения**: Обеспечена синхронизация значений пинга и отображения графика

### 🐛 Критические исправления Live Packets View (предыдущие версии)
- **Исправление deadlock потоков**: Решена проблема ContextSwitchDeadlock в Live Packets View из-за блокировки UI потока
- **Защита от NullReference**: Добавлены всесторонние проверки на null для обработки пакетов для предотвращения крашей
- **Оптимизация многопоточности**: Заменены блокирующие Invoke вызовы на BeginInvoke для лучшей отзывчивости
- **Ограничение обработки пакетов**: Ограничено количество пакетов, обрабатываемых за цикл обновления (макс. 100) для предотвращения зависания UI
- **Управление памятью**: Улучшена обработка буфера пакетов с фильтрацией null
- **Валидация Ethernet пакетов**: Добавлена правильная валидация структур Ethernet, UDP и TCP пакетов

### � Исправления производительности и стабильности  
- **Очистка debug логирования**: Удален избыточный debug вывод из метода GetOption, который вызывал проблемы производительности
- **Исправление Live Packets View**: Исправлена ошибка "Selected adapter is not set!" при включенном multi-adapter режиме
- **Улучшение логики адаптеров**: Скорректирована логика выбора адаптера в PacketStats.cs для multi-adapter захвата
- **Оптимизация производительности**: Устранены издержки отладки в часто вызываемых методах конфигурации
- **Очистка кода**: Удалены неиспользуемые debug операторы из PingManager.cs и App.cs

### ✅ Статус: ПРОТЕСТИРОВАНО И РАБОТАЕТ
- **Универсальные функции**: Успешно реализованы и протестированы комплексные улучшения универсальности
- **Продвинутая система пинга**: TCP пинг с ICMP fallback работает корректно
- **EMA сглаживание**: Экспоненциальное скользящее среднее для стабилизации tickrate активно
- **STUN интеграция**: Обнаружение внешнего IP через протокол STUN функционирует
- **UI интеграция**: Все 10 новых универсальных чекбоксов правильно интегрированы
- **Multi-Adapter функция**: Успешно протестирована и подтверждена работоспособность
- **Live Packets View**: Теперь стабилен с правильной многопоточностью и безопасностью null
- **Готово к продакшену**: Все функции стабильны и готовы к использованию

## [Основные функции] - 2025-09-12

### 🚀 Реализация крупных новых универсальных функций

#### 🌐 Продвинутая система управления пингом
- **TCP Ping**: Основной метод пинга с использованием TcpClient с точным измерением времени подключения
- **ICMP Fallback**: Автоматический переход к ICMP при сбое TCP пинга
- **Привязка к интерфейсу**: Операции пинга привязаны к конкретному сетевому интерфейсу для точности
- **Обнаружение активных целей**: Интеллектуальное направление пинга на основе активных соединений
- **Настраиваемые порты**: Поддержка пользовательских портов пинга (по умолчанию: 80, 443)
- **Обновления в реальном времени**: Интеграция результатов пинга в реальном времени с GUI

#### 📊 EMA сглаживание тикрейта
- **Экспоненциальное скользящее среднее**: Плавное отображение тикрейта без резких колебаний
- **Настраиваемая альфа**: Регулируемый коэффициент сглаживания (по умолчанию: 0.15)
- **Обработка в реальном времени**: Применяется во время расчёта тикрейта для мгновенного эффекта
- **Оптимизация производительности**: Минимальные издержки с потокобезопасной реализацией

#### 🔍 STUN обнаружение внешнего IP
- **Поддержка множественных серверов**: Google STUN, Cloudflare и Nextcloud серверы
- **Автоматическое обнаружение**: Фоновое обнаружение внешнего IP во время обнаружения сервера
- **Умное кэширование**: 10-минутное время ожидания кэша для минимизации сетевых издержек
- **Устойчивость к ошибкам**: Изящный переход между STUN серверами

#### ⚙️ Универсальная система конфигурации
- **10 новых чекбоксов**: Полная интеграция UI в SettingsForm
- **Постоянные настройки**: Все предпочтения сохраняются в settings.ini
- **Переключение во время выполнения**: Включение/отключение функций без перезапуска
- **Обратная совместимость**: Вся существующая функциональность сохранена

### 🛠️ Детали технической реализации

#### Новые основные классы
- **PingManager.cs**: Комплексное управление пингом с поддержкой TCP/ICMP (358 строк)
- **TickrateSmoothing.cs**: EMA implementation with thread-safe operations (150+ lines)
- **StunClient.cs**: STUN protocol implementation for external IP detection (250+ lines)

#### Enhanced Existing Classes
- **SettingsManager.cs**: Added GetBool(), GetInt(), GetString() methods
- **TickMeterState.cs**: Integrated EMA smoothing and external IP tracking
- **GUI.cs**: Added ping manager lifecycle and result handling
- **App.cs**: Centralized initialization of all new components

#### Universal Settings Integration
- `ping_bind_to_interface`: Bind ping to selected network adapter
- `ping_tcp_prefer`: Prefer TCP ping over ICMP
- `ping_fallback_icmp`: Fallback to ICMP when TCP fails  
- `ping_target_active_only`: Only ping active connection targets
- `tickrate_smoothing`: Enable EMA smoothing for tickrate
- `dedup_multi_nic`: Packet deduplication for multi-adapter mode
- `enable_ipv6`: IPv6 protocol support
- `ignore_virtual_adapters`: Filter out virtual network interfaces
- `rtss_only_active`: RTSS overlay only for active processes
- `stun_enable`: Enable STUN external IP detection

### 🚀 Previous Major Feature: Multi-Adapter Packet Capture
- **Multi-Adapter Capture**: Simultaneous packet capture from all network adapters
- **Smart Filtering**: Automatic exclusion of virtual adapters
- **Packet Deduplication**: Hash-based duplicate detection with 3ms time window
- **Background Processing**: Optimized performance with separate workers per adapter

### 🛠️ Critical Bug Fixes
- **Fixed NullReferenceException**: Resolved crashes in SettingsForm initialization
- **UDP ProcessRecord**: Fixed missing RemoteAddress/RemotePort fields handling
- **Project Integration**: Proper inclusion of all new classes in tickMeter.csproj
- **Namespace Consistency**: Unified namespace usage across all components
- **Compilation Issues**: Resolved all build errors and warnings

### ✨ User Interface Enhancements
- **Universal Checkboxes**: 10 new configuration options in settings panel
- **Grouped Layout**: Logical organization of universal features at coordinates (24,480)-(300,560)
- **Real-time Feedback**: Immediate application of setting changes
- **Enhanced Ping Display**: Integration of new ping results in main UI

### 📊 Latest Statistics
- **New files added**: 3 major new classes (PingManager, TickrateSmoothing, StunClient)
- **Modified files**: 8+ core files for universal features integration
- **New lines added**: 750+ lines of advanced functionality
- **Universal Checkboxes**: 10 new configuration options
- **Functionality**: Backward compatible - all existing features work unchanged

---

## How to Use Universal Features

### Advanced Ping System
1. **Enable TCP Ping**: Check "Предпочитать TCP ping" in Settings
2. **Interface Binding**: Check "Привязать ping к интерфейсу" for accurate measurements
3. **ICMP Fallback**: Check "Фолбэк на ICMP ping" for reliability
4. **Active Targeting**: Check "Пинговать только активные цели" for efficiency

### EMA Tickrate Smoothing
1. **Enable Smoothing**: Check "Сглаживание тикрейта (EMA)" in Settings
2. **Automatic Application**: Smoothing applied in real-time to tickrate display
3. **Configurable**: Adjust alpha coefficient in settings.ini (tickrate_smoothing_alpha)

### STUN External IP Detection
1. **Enable STUN**: Check "Включить STUN определение внешнего IP" in Settings
2. **Automatic Detection**: External IP discovered during server detection
3. **Cached Results**: 10-minute cache prevents excessive network requests

### Multi-Adapter Capture (Previous Feature)
1. **Enable Multi-Adapter Mode**: Check "Захватывать со всех адаптеров" in Settings
2. **Automatic Detection**: Application will detect and use all physical network adapters
3. **Virtual Adapter Filtering**: Loopback, Hyper-V, VMware, and VirtualBox adapters excluded
4. **Performance**: Optimized background processing ensures no performance impact

## Use Cases

### Gaming Scenarios
- **Competitive Gaming**: TCP ping for accurate latency measurements
- **Multiple Connections**: Monitor both Wi-Fi and Ethernet simultaneously  
- **Smooth Metrics**: EMA smoothing prevents distracting tickrate fluctuations
- **External Monitoring**: STUN detection for NAT/firewall troubleshooting

### Development & Testing
- **Network Analysis**: Comprehensive packet monitoring across all adapters
- **Protocol Testing**: TCP and ICMP ping comparison
- **Performance Testing**: Smoothed metrics for stable performance graphs
- **External Connectivity**: STUN for testing external network accessibility

### Network Administration
- **Multi-Interface Monitoring**: Simultaneous capture from all physical adapters
- **Accurate Latency**: Interface-bound ping measurements
- **External IP Tracking**: Automatic external IP detection for NAT scenarios
- **Virtual Environment**: Automatic filtering of virtual network interfaces

## Known Issues
- Line ending warnings during git operations (automatically handled)
- Some virtual adapters may require manual filtering in specific environments

---

*This changelog follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format.*

## [Unreleased] - 2025-09-12

### �️ Critical Bug Fixes
- **Fixed NullReferenceException**: Resolved critical crash in SettingsForm.InitCaptureAllAdaptersState()
- **Improved Initialization**: Moved multi-adapter checkbox initialization to ApplyFromConfig() method
- **Enhanced Safety**: Added null checks for UI components to prevent runtime exceptions
- **Designer Cleanup**: Fixed duplicate field declarations and resource loading issues

### �🚀 Major New Feature: Multi-Adapter Packet Capture
- **Multi-Adapter Capture**: New option to capture packets from all network adapters simultaneously
- **Smart Filtering**: Automatically excludes virtual adapters (loopback, Hyper-V, VMware, VirtualBox)
- **Packet Deduplication**: Intelligent duplicate packet filtering to prevent double counting on network bridges
- **Seamless Integration**: No changes to existing packet processing pipeline or UI - works with all existing features

### 🛠️ Technical Implementation
- **Background Workers**: Each adapter runs in its own BackgroundWorker for optimal performance
- **Hash-based Deduplication**: Fast 64-byte hash comparison with 3ms time window
- **Automatic Adapter Detection**: Filters out non-physical network interfaces
- **Settings Integration**: New `capture_all_adapters` setting in `settings.ini`

### ✨ User Interface Enhancements
- **New Checkbox**: "Захватывать со всех адаптеров" in Settings form
- **Smart UI**: Automatically disables single adapter selection when multi-mode is enabled
- **Instant Feedback**: Settings saved immediately on change

### 🔧 Previous Major Improvements
- **TCP Connection Monitoring**: Enhanced TCP connection tracking and management
- **Performance Optimization**: Significantly improved ConnectionsManager performance (474 lines changed)
- **UI/UX Enhancements**: Major improvements to GUI with new features and better user experience

### ✨ Previous New Features
- Added new GameServer class for better game server management
- Enhanced ActiveWindowTracker with improved window detection
- Improved RivaTuner integration with extended functionality
- Added comprehensive state management improvements in TickMeterState
- Enhanced packet statistics tracking and display

### 🔧 Previous Technical Improvements
- **ConnectionsManager.cs**: Major refactoring for better performance and reliability
- **TickMeterState.cs**: Extensive improvements with 646+ lines of enhancements
- **RivaTuner.cs**: Added 137+ lines of new functionality
- **GUI.cs**: Enhanced user interface with 72+ lines of improvements
- **PacketStats.cs**: Improved packet statistics handling

### 🐛 Bug Fixes
- Fixed line ending consistency across all source files
- Improved debug logging functionality
- Enhanced error handling in various components
- Fixed project dependencies and references

### 📦 Dependencies
- Updated project dependencies and package references
- Added new NuGet packages for enhanced functionality:
  - BinarySerializer 8.6.4.1
  - Microsoft.Bcl.AsyncInterfaces 9.0.4
  - Microsoft.Diagnostics.NETCore.Client 0.2.621003
  - Microsoft.Extensions.* suite 9.0.4
  - NUnit 4.3.2
  - Updated PacketDotNet to 1.4.8
  - Added PcapNgNet 0.7.0.0

### 📊 Latest Statistics
- **New files added**: 1 workspace file
- **Modified files**: 3 core files (SettingsForm.cs, GUI.cs, CHANGELOG.md)
- **New lines added**: 178+ lines of multi-adapter functionality
- **Functionality**: Backward compatible - all existing features work unchanged

### 🔍 Component Changes
| Component | Changes | Impact |
|-----------|---------|--------|
| Multi-Adapter Capture | NEW FEATURE | High |
| SettingsForm | UI enhancements | Medium |
| GUI | Multi-capture logic | High |
| ConnectionsManager | Major refactoring | High |
| TickMeterState | Extensive improvements | High |
| RivaTuner | New functionality | Medium |
| Other components | Bug fixes & improvements | Low-Medium |

---

## How to Use Multi-Adapter Capture

1. **Enable Multi-Adapter Mode**: Check "Захватывать со всех адаптеров" in Settings
2. **Automatic Detection**: Application will automatically detect and use all physical network adapters
3. **Virtual Adapter Filtering**: Loopback, Hyper-V, VMware, and VirtualBox adapters are automatically excluded
4. **Performance**: Optimized background processing ensures no performance impact
5. **Compatibility**: All existing features (Live Packets View, RTSS, profiles) work normally

## Use Cases

- **Gaming**: Monitor both Wi-Fi and Ethernet connections simultaneously
- **Development**: Capture traffic from multiple network interfaces during testing
- **Network Analysis**: Comprehensive packet monitoring across all active adapters
- **VPN Usage**: Monitor both VPN and direct connections (VPN adapters are filtered out automatically)

## Known Issues
- Line ending warnings during git operations (automatically handled)

---

*This changelog follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format.*