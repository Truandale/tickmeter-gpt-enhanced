using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace tickMeter.Classes
{
    /// <summary>
    /// Manages form transparency based on mouse hover state.
    /// </summary>
    public class SmartTransparencyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        private readonly Form _form;
        private readonly System.Windows.Forms.Timer _checkTimer;
        private const int CHECK_INTERVAL_MS = 100;
        private const double TRANSPARENT_OPACITY = 0.15;
        private const double OPAQUE_OPACITY = 0.95;
        private bool _isDisposed = false;
        private bool _isHovering = false;

        public SmartTransparencyManager(Form form)
        {
            if (form == null)
                throw new ArgumentNullException(nameof(form));

            _form = form;
            
            // Initialize timer
            _checkTimer = new System.Windows.Forms.Timer();
            _checkTimer.Interval = CHECK_INTERVAL_MS;
            _checkTimer.Tick += CheckTimer_Tick;
            
            // Set initial transparency
            SetTransparentState();
            
            // Start checking
            _checkTimer.Start();
        }

        private void CheckTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (_form == null || _form.IsDisposed || !_form.Visible)
                    return;

                // Get cursor position
                if (!GetCursorPos(out POINT cursorPos))
                    return;

                // Check if cursor is over the form
                Rectangle formBounds = _form.Bounds;
                bool isCurrentlyHovering = formBounds.Contains(cursorPos.X, cursorPos.Y);

                // Update state if changed
                if (isCurrentlyHovering != _isHovering)
                {
                    _isHovering = isCurrentlyHovering;
                    
                    if (_isHovering)
                        SetOpaqueState();
                    else
                        SetTransparentState();
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"SmartTransparencyManager.CheckTimer_Tick: {ex.Message}");
            }
        }

        private void SetTransparentState()
        {
            try
            {
                if (_form != null && !_form.IsDisposed && _form.Opacity != TRANSPARENT_OPACITY)
                {
                    _form.Opacity = TRANSPARENT_OPACITY;
                    DebugLogger.log($"SmartTransparencyManager: Set transparent state (opacity={TRANSPARENT_OPACITY})");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"SmartTransparencyManager.SetTransparentState: {ex.Message}");
            }
        }

        private void SetOpaqueState()
        {
            try
            {
                if (_form != null && !_form.IsDisposed && _form.Opacity != OPAQUE_OPACITY)
                {
                    _form.Opacity = OPAQUE_OPACITY;
                    DebugLogger.log($"SmartTransparencyManager: Set opaque state (opacity={OPAQUE_OPACITY})");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"SmartTransparencyManager.SetOpaqueState: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
                return;

            _isDisposed = true;

            try
            {
                if (_checkTimer != null)
                {
                    _checkTimer.Stop();
                    _checkTimer.Tick -= CheckTimer_Tick;
                    _checkTimer.Dispose();
                }

                // Restore full opacity
                if (_form != null && !_form.IsDisposed)
                {
                    _form.Opacity = OPAQUE_OPACITY;
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"SmartTransparencyManager.Dispose: {ex.Message}");
            }
        }
    }
}
