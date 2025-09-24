using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.Win32;

namespace tickMeter.Classes
{
    /// <summary>
    /// Stage 7: Intelligent Network Optimization (Instance version)
    /// Автоматически оптимизирует сетевые настройки на основе анализа качества сети
    /// </summary>
    public class NetworkOptimizer
    {
        // Настройки оптимизации
        private bool _enabled = false;
        private float _qualityThreshold = 0.7f; // Порог для запуска оптимизации
        private int _optimizationInterval = 5; // Интервал проверки в минутах
        private bool _aggressiveMode = false;
        
        // Статистика
        private int _totalOptimizations = 0;
        private int _successfulOptimizations = 0;
        private DateTime _lastOptimization = DateTime.MinValue;
        
        // Таймер для автоматической оптимизации
        private Timer _optimizationTimer;
        
        // Блокировка для thread-safety
        private readonly object _lockObject = new object();
        
        // События
        public event Action<bool> OptimizationStateChanged;
        public event Action<int, int> OptimizationStatsChanged;
        public event Action<DateTime> LastOptimizationChanged;
        
        /// <summary>
        /// Инициализация оптимизатора
        /// </summary>
        public void Initialize()
        {
            try
            {
                // Подписываемся на события анализатора качества сети
                NetworkQualityAnalyzer.QualityChanged -= OnQualityChanged;
                NetworkQualityAnalyzer.QualityChanged += OnQualityChanged;
                NetworkQualityAnalyzer.PredictionChanged -= OnPredictionChanged;
                NetworkQualityAnalyzer.PredictionChanged += OnPredictionChanged;
                
                // Загружаем настройки
                LoadSettings();
                
                // Запускаем таймер проверки
                if (_enabled)
                {
                    StartOptimizationTimer();
                }
                
                Debug.Print("[NetworkOptimizer] Initialized successfully");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] Initialization error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Обработчик изменения качества сети
        /// </summary>
        private void OnQualityChanged(float quality)
        {
            try
            {
                if (!_enabled) return;
                
                // Если качество ниже порога - запускаем оптимизацию
                if (quality < _qualityThreshold)
                {
                    Debug.Print($"[NetworkOptimizer] Quality dropped to {quality:F2}, triggering optimization");
                    PerformOptimization();
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] OnQualityChanged error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Обработчик предсказания проблем сети
        /// </summary>
        private void OnPredictionChanged(bool isPredicting, string description)
        {
            try
            {
                if (!_enabled) return;
                
                // Если предсказываются проблемы - превентивная оптимизация
                if (isPredicting && _aggressiveMode)
                {
                    Debug.Print($"[NetworkOptimizer] Issues predicted: {description}, performing preventive optimization");
                    PerformOptimization();
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] OnPredictionChanged error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Запуск таймера автоматической оптимизации
        /// </summary>
        private void StartOptimizationTimer()
        {
            try
            {
                _optimizationTimer?.Dispose();
                _optimizationTimer = new Timer(TimerCallback, null, 
                    TimeSpan.FromMinutes(_optimizationInterval), 
                    TimeSpan.FromMinutes(_optimizationInterval));
                
                Debug.Print($"[NetworkOptimizer] Timer started with {_optimizationInterval} minute interval");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] StartOptimizationTimer error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Остановка таймера
        /// </summary>
        private void StopOptimizationTimer()
        {
            try
            {
                _optimizationTimer?.Dispose();
                _optimizationTimer = null;
                Debug.Print("[NetworkOptimizer] Timer stopped");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] StopOptimizationTimer error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Колбэк таймера
        /// </summary>
        private void TimerCallback(object state)
        {
            try
            {
                if (_enabled && NetworkQualityAnalyzer.OverallQuality < _qualityThreshold)
                {
                    Debug.Print("[NetworkOptimizer] Timer triggered optimization");
                    PerformOptimization();
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] TimerCallback error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Выполнение оптимизации сети
        /// </summary>
        public void PerformOptimization()
        {
            lock (_lockObject)
            {
                try
                {
                    Debug.Print("[NetworkOptimizer] Starting network optimization");
                    _totalOptimizations++;
                    
                    bool success = true;
                    
                    // TCP оптимизация
                    if (!OptimizeTcpSettings())
                    {
                        success = false;
                        Debug.Print("[NetworkOptimizer] TCP optimization failed");
                    }
                    
                    // Буферы сети
                    if (!OptimizeNetworkBuffers())
                    {
                        success = false;
                        Debug.Print("[NetworkOptimizer] Network buffers optimization failed");
                    }
                    
                    // DNS оптимизация
                    if (!OptimizeDnsSettings())
                    {
                        success = false;
                        Debug.Print("[NetworkOptimizer] DNS optimization failed");
                    }
                    
                    if (success)
                    {
                        _successfulOptimizations++;
                        Debug.Print("[NetworkOptimizer] Optimization completed successfully");
                    }
                    else
                    {
                        Debug.Print("[NetworkOptimizer] Optimization completed with errors");
                    }
                    
                    _lastOptimization = DateTime.Now;
                    
                    // Уведомляем об изменениях
                    OptimizationStatsChanged?.Invoke(_totalOptimizations, _successfulOptimizations);
                    LastOptimizationChanged?.Invoke(_lastOptimization);
                }
                catch (Exception ex)
                {
                    Debug.Print($"[NetworkOptimizer] PerformOptimization error: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Оптимизация TCP настроек
        /// </summary>
        private bool OptimizeTcpSettings()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true))
                {
                    if (key != null)
                    {
                        // TCP Window Scaling
                        key.SetValue("TcpWindowSize", 0x10000, RegistryValueKind.DWord);
                        key.SetValue("Tcp1323Opts", 3, RegistryValueKind.DWord);
                        key.SetValue("DefaultTTL", 64, RegistryValueKind.DWord);
                        key.SetValue("EnablePMTUDiscovery", 1, RegistryValueKind.DWord);
                        
                        Debug.Print("[NetworkOptimizer] TCP settings optimized");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] TCP optimization error: {ex.Message}");
            }
            return false;
        }
        
        /// <summary>
        /// Оптимизация сетевых буферов
        /// </summary>
        private bool OptimizeNetworkBuffers()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters", true))
                {
                    if (key != null)
                    {
                        // Буферы TCP
                        key.SetValue("TcpReceiveBufferSize", 0x20000, RegistryValueKind.DWord);
                        key.SetValue("TcpSendBufferSize", 0x20000, RegistryValueKind.DWord);
                        key.SetValue("MaxUserPort", 65534, RegistryValueKind.DWord);
                        key.SetValue("TcpTimedWaitDelay", 30, RegistryValueKind.DWord);
                        
                        Debug.Print("[NetworkOptimizer] Network buffers optimized");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] Network buffers optimization error: {ex.Message}");
            }
            return false;
        }
        
        /// <summary>
        /// Оптимизация DNS настроек
        /// </summary>
        private bool OptimizeDnsSettings()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\Dnscache\Parameters", true))
                {
                    if (key != null)
                    {
                        // DNS кэш
                        key.SetValue("MaxCacheTtl", 86400, RegistryValueKind.DWord);
                        key.SetValue("MaxNegativeCacheTtl", 300, RegistryValueKind.DWord);
                        key.SetValue("NetFailureCacheTime", 30, RegistryValueKind.DWord);
                        
                        Debug.Print("[NetworkOptimizer] DNS settings optimized");
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] DNS optimization error: {ex.Message}");
            }
            return false;
        }
        
        /// <summary>
        /// Загрузка настроек
        /// </summary>
        private void LoadSettings()
        {
            try
            {
                _enabled = App.settingsManager?.GetOption("network_optimization_enabled", "False", "ADVANCED") == "True";
                
                if (float.TryParse(App.settingsManager?.GetOption("optimization_threshold", "70", "ADVANCED"), out float threshold))
                {
                    _qualityThreshold = threshold / 100.0f;
                }
                
                if (int.TryParse(App.settingsManager?.GetOption("optimization_interval", "5", "ADVANCED"), out int interval))
                {
                    _optimizationInterval = interval;
                }
                
                _aggressiveMode = App.settingsManager?.GetOption("aggressive_optimization", "False", "ADVANCED") == "True";
                
                Debug.Print($"[NetworkOptimizer] Settings loaded: Enabled={_enabled}, Threshold={_qualityThreshold:F2}, Interval={_optimizationInterval}min, Aggressive={_aggressiveMode}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] LoadSettings error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Включение/выключение оптимизатора
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            try
            {
                _enabled = enabled;
                
                if (_enabled)
                {
                    StartOptimizationTimer();
                }
                else
                {
                    StopOptimizationTimer();
                }
                
                OptimizationStateChanged?.Invoke(_enabled);
                Debug.Print($"[NetworkOptimizer] Enabled state changed to: {_enabled}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] SetEnabled error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Установка порога качества
        /// </summary>
        public void SetQualityThreshold(float threshold)
        {
            try
            {
                _qualityThreshold = Math.Max(0.1f, Math.Min(1.0f, threshold));
                Debug.Print($"[NetworkOptimizer] Quality threshold set to: {_qualityThreshold:F2}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] SetQualityThreshold error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Установка интервала оптимизации
        /// </summary>
        public void SetOptimizationInterval(int minutes)
        {
            try
            {
                _optimizationInterval = Math.Max(1, Math.Min(60, minutes));
                
                if (_enabled)
                {
                    StartOptimizationTimer(); // Перезапускаем с новым интервалом
                }
                
                Debug.Print($"[NetworkOptimizer] Optimization interval set to: {_optimizationInterval} minutes");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] SetOptimizationInterval error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Установка агрессивного режима
        /// </summary>
        public void SetAggressiveMode(bool aggressive)
        {
            try
            {
                _aggressiveMode = aggressive;
                Debug.Print($"[NetworkOptimizer] Aggressive mode set to: {_aggressiveMode}");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] SetAggressiveMode error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Очистка истории оптимизации
        /// </summary>
        public void ClearHistory()
        {
            try
            {
                _totalOptimizations = 0;
                _successfulOptimizations = 0;
                _lastOptimization = DateTime.MinValue;
                
                OptimizationStatsChanged?.Invoke(_totalOptimizations, _successfulOptimizations);
                LastOptimizationChanged?.Invoke(_lastOptimization);
                
                Debug.Print("[NetworkOptimizer] History cleared");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] ClearHistory error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Получение статистики
        /// </summary>
        public (int total, int successful, DateTime lastOptimization) GetStats()
        {
            return (_totalOptimizations, _successfulOptimizations, _lastOptimization);
        }
        
        /// <summary>
        /// Получение текущих настроек
        /// </summary>
        public (bool enabled, float threshold, int interval, bool aggressive) GetSettings()
        {
            return (_enabled, _qualityThreshold, _optimizationInterval, _aggressiveMode);
        }
        
        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            try
            {
                StopOptimizationTimer();
                
                // Отписываемся от событий
                NetworkQualityAnalyzer.QualityChanged -= OnQualityChanged;
                NetworkQualityAnalyzer.PredictionChanged -= OnPredictionChanged;
                
                Debug.Print("[NetworkOptimizer] Disposed");
            }
            catch (Exception ex)
            {
                Debug.Print($"[NetworkOptimizer] Dispose error: {ex.Message}");
            }
        }
    }
}