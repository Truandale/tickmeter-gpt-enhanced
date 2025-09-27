// Функция FPS оверлея для добавления в RivaTuner.cs
// Кэш для FPS оверлея с TTL
private static string _cachedOverlayFPS = "";
private static DateTime _lastOverlayFPSUpdate = DateTime.MinValue;
private static readonly TimeSpan OVERLAY_FPS_TTL = TimeSpan.FromSeconds(1);
private static int _frameCounter = 0;
private static DateTime _lastFPSMeasurement = DateTime.MinValue;
private static float _currentFPS = 0;

private static string FormatOverlayFPS()
{
    try
    {
        var now = DateTime.UtcNow;
        
        // Считаем FPS оверлея
        _frameCounter++;
        if (now - _lastFPSMeasurement > TimeSpan.FromSeconds(1))
        {
            _currentFPS = _frameCounter / (float)(now - _lastFPSMeasurement).TotalSeconds;
            _frameCounter = 0;
            _lastFPSMeasurement = now;
        }
        
        if (now - _lastOverlayFPSUpdate > OVERLAY_FPS_TTL)
        {
            _cachedOverlayFPS = $"Overlay: {_currentFPS:F0} FPS";
            _lastOverlayFPSUpdate = now;
        }
        
        return _cachedOverlayFPS;
    }
    catch
    {
        return "Overlay: N/A FPS";
    }
}