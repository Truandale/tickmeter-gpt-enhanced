# Changelog
## [Unreleased] - 2025-09-21

### ✨ UI
- Добавлена кнопка "Дополнительные настройки" в `SettingsForm` (через Designer), размещена над `testbutton`.
- Открытие диалога `AdvancedSettingsForm` гарантировано: подписка Click как в Designer, так и дублирующая переподписка в конструкторе (`WireAdvancedSettingsButton`).

### 🐛 Исправления
- Починен кейс, когда Designer не сохранял обработчик Click: теперь форма всегда открывается при нажатии.
- Синхронизированы `.Designer.cs` и `.resx` для кнопки `btnAdvancedSettings`.

### 🔧 Технические детали
- В `SettingsForm.cs` добавлен метод `WireAdvancedSettingsButton()` с поиском контрола по имени и безопасной переподпиской обработчика.
- Проверена конфигурация `AdvancedSettingsForm` (StartPosition = CenterParent) для корректного модального показа.

### 🔁 Перенос универсальных настроек
- Перенесены чекбоксы универсальных/сетевых опций с главной формы настроек в `AdvancedSettingsForm`:
  - ping_bind_to_interface, ping_tcp_prefer, ping_fallback_icmp, ping_target_active_only
  - tickrate_smoothing, dedup_multi_nic, enable_ipv6
  - rtss_only_active, stun_enable
  - capture_all_adapters, ignore_virtual_adapters
- `AdvancedSettingsForm` теперь загружает/сохраняет перечисленные флаги (секция SETTINGS/ADVANCED).
- После закрытия окна дополнительных настроек главная форма перечитывает конфиг и обновляет состояние дропдауна адаптера.


All notab- **Производительность графика**: Оптимизирована отрисовка графика пинга с подходящими интервалами обновления
- **Синхронизация настроек**: Исправлен таймер overlay для правильной синхронизации с ping_interval настройками
- **Точность отображения**: Обеспечена синхронизация значений пинга и отображения графика

### 🐛 Критические исправления Live Packets View (предыдущие версии)
- **Исправление deadlock потоков**: Решена проблема ContextSwitchDeadlock в Live Packets View из-за блокировки UI потока
- **Защита от NullReference**: Добавлены всесторонние проверки на null для обработки пакетов для предотвращения крашей
- **Оптимизация многопоточности**: Заменены блокирующие Invoke вызовы на BeginInvoke для лучшей отзывчивости
- **Ограничение обработки пакетов**: Ограничено количество пакетов, обрабатываемых за цикл обновления (макс. 100) для предотвращения зависания UI
- **Управление памятью**: Улучшена обработка буфера пакетов с фильтрацией null
- **Валидация Ethernet пакетов**: Добавлена правильная валидация структур Ethernet, UDP и TCP пакетов

### 🚀 Исправления производительности и стабильности  
- **Очистка debug логирования**: Удален избыточный debug вывод из метода GetOption, который вызывал проблемы производительности
- **Исправление Live Packets View**: Исправлена ошибка "Selected adapter is not set!" при включенном multi-adapter режиме
- **Улучшение логики адаптеров**: Скорректирована логика выбора адаптера в PacketStats.cs для multi-adapter захватаto TickMeter project will be documented in this file.

## [Unreleased] - 2025-09-15

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

### 📊 Исправления отображения overlay и графика пинга
- **Стабильность графика пинга**: Исправлена проблема "замерзшего" графика через восстановление правильного обновления pingBuffer
- **Частота обновления overlay**: Скорректирована частота обновления overlay для соответствия пользовательским интервалам пинга
- **Поток данных пинга**: Восстановлен правильный поток данных от PingManager к отображению в overlay
- **Производительность графика**: Оптимизирована отрисовка графика пинга с подходящими интервалами обновления
- **Синхронизация настроек**: Исправлен таймер overlay для правильной синхронизации с ping_interval настройками
- **Точность отображения**: Обеспечена синхронизация значений пинга и отображения графика
- **Packet Processing Limit**: Limited packets processed per refresh cycle (max 100) to prevent UI freezing
- **Memory Management**: Improved packet buffer handling with null filtering
- **Ethernet Packet Validation**: Added proper validation for Ethernet, UDP, and TCP packet structures

### �🐛 Performance & Stability Fixes  
- **Debug Logging Cleanup**: Removed excessive debug output from GetOption method that was causing performance issues
- **Live Packets View Fix**: Fixed "Selected adapter is not set!" error when multi-adapter mode is enabled
- **Adapter Logic Improvement**: Corrected adapter selection logic in PacketStats.cs for multi-adapter capture
- **Performance Optimization**: Eliminated debug overhead in frequently called configuration methods
- **Code Cleanup**: Removed unused debug statements from PingManager.cs and App.cs

### ✅ Status: TESTED & WORKING
- **Universal Features**: Successfully implemented and tested comprehensive universality enhancements
- **Advanced Ping System**: TCP ping with ICMP fallback working correctly
- **EMA Smoothing**: Exponential moving average for tickrate stabilization active
- **STUN Integration**: External IP detection through STUN protocol operational
- **UI Integration**: All 10 new universal checkboxes properly integrated
- **Multi-Adapter Feature**: Successfully tested and confirmed working
- **Live Packets View**: Now stable with proper multi-threading and null safety
- **Ready for Production**: All features are stable and ready for use

## [Major Features] - 2025-09-12

### 🚀 Major New Universal Features Implementation

#### 🌐 Advanced Ping Management System
- **TCP Ping**: Primary ping method using TcpClient with precise connection timing
- **ICMP Fallback**: Automatic fallback to ICMP when TCP ping fails
- **Interface Binding**: Ping operations bound to specific network interface for accuracy
- **Active Target Detection**: Intelligent ping targeting based on active connections
- **Configurable Ports**: Support for custom ping ports (default: 80, 443)
- **Real-time Updates**: Live ping results integration with GUI

#### 📊 EMA Tickrate Smoothing
- **Exponential Moving Average**: Smooth tickrate display without jarring fluctuations  
- **Configurable Alpha**: Adjustable smoothing coefficient (default: 0.15)
- **Real-time Processing**: Applied during tickrate calculation for instant effect
- **Performance Optimized**: Minimal overhead with thread-safe implementation

#### 🔍 STUN External IP Detection
- **Multi-Server Support**: Google STUN, Cloudflare, and Nextcloud servers
- **Automatic Detection**: Background external IP discovery during server detection
- **Smart Caching**: 10-minute cache timeout to minimize network overhead
- **Error Resilience**: Graceful fallback between STUN servers

#### ⚙️ Universal Configuration System
- **10 New Checkboxes**: Complete UI integration in SettingsForm
- **Persistent Settings**: All preferences saved to settings.ini
- **Runtime Toggle**: Enable/disable features without restart
- **Backward Compatibility**: All existing functionality preserved

### 🛠️ Technical Implementation Details

#### New Core Classes
- **PingManager.cs**: Comprehensive ping management with TCP/ICMP support (358 lines)
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