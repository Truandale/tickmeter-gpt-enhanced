# Changelog

All notable changes to TickMeter project will be documented in this file.

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