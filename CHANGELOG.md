# Changelog

All notable changes to TickMeter project will be documented in this file.

## [Unreleased] - 2025-09-12

### 🚀 Major Improvements
- **TCP Connection Monitoring**: Enhanced TCP connection tracking and management
- **Performance Optimization**: Significantly improved ConnectionsManager performance (474 lines changed)
- **UI/UX Enhancements**: Major improvements to GUI with new features and better user experience

### ✨ New Features
- Added new GameServer class for better game server management
- Enhanced ActiveWindowTracker with improved window detection
- Improved RivaTuner integration with extended functionality
- Added comprehensive state management improvements in TickMeterState
- Enhanced packet statistics tracking and display

### 🔧 Technical Improvements
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

### 📊 Statistics
- **Total files changed**: 14 files
- **Lines added**: 948+
- **Lines removed**: 508
- **Net addition**: 440+ lines of improved code

### 🔍 Component Changes
| Component | Changes | Impact |
|-----------|---------|--------|
| ConnectionsManager | Major refactoring | High |
| TickMeterState | Extensive improvements | High |
| RivaTuner | New functionality | Medium |
| GUI | UI enhancements | Medium |
| ActiveWindowTracker | Improved tracking | Medium |
| Other components | Bug fixes & improvements | Low-Medium |

---

## How to Use This Release

1. **TCP Ping Functionality**: Enhanced TCP connection monitoring for better network analysis
2. **Improved UI**: Better user experience with enhanced GUI components
3. **Performance**: Faster and more reliable packet analysis
4. **Game Integration**: Better support for game server monitoring

## Known Issues
- Line ending warnings during git operations (automatically handled)

---

*This changelog follows [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) format.*