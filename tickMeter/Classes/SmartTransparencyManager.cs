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
            
            DebugLogger.log("SmartTransparencyManager: Initialized");
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

                // DETAILED LOGGING for debugging
                DebugLogger.log($"[HOVER-DEBUG] Cursor: ({cursorPos.X}, {cursorPos.Y}) | Form bounds: X={formBounds.X} Y={formBounds.Y} W={formBounds.Width} H={formBounds.Height} | IsOver: {isCurrentlyHovering} | WasHovering: {_isHovering}");

                // Update state if changed
                if (isCurrentlyHovering != _isHovering)
                {
                    _isHovering = isCurrentlyHovering;

                    if (_isHovering)
                    {
                        DebugLogger.log("[HOVER-CHANGE] Mouse ENTERED form area -> calling SetOpaqueState()");
                        SetOpaqueState();
                    }
                    else
                    {
                        DebugLogger.log("[HOVER-CHANGE] Mouse LEFT form area -> calling SetTransparentState()");
                        SetTransparentState();
                    }
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
                if (_form != null && !_form.IsDisposed)
                {
                    // Set transparent minimal mode directly through Form properties
                    // This mimics ApplyInactiveWindowPresentation() behavior
                    _form.BackColor = System.Drawing.SystemColors.WindowFrame;
                    _form.TransparencyKey = System.Drawing.SystemColors.WindowFrame;
                    _form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                    
                    DebugLogger.log("SmartTransparencyManager: Applied inactive window presentation (minimal mode)");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"SmartTransparencyManager.SetTransparentState ERROR: {ex.Message}\n{ex.StackTrace}");
            }
        }

        private void SetOpaqueState()
        {
            try
            {
                if (_form != null && !_form.IsDisposed)
                {
                    // Set opaque full mode directly through Form properties
                    // This mimics ApplyActiveWindowPresentation() behavior
                    _form.BackColor = System.Drawing.SystemColors.Control;
                    _form.TransparencyKey = System.Drawing.Color.PaleVioletRed;
                    _form.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
                    
                    DebugLogger.log("SmartTransparencyManager: Applied active window presentation (full mode with buttons)");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"SmartTransparencyManager.SetOpaqueState ERROR: {ex.Message}\n{ex.StackTrace}");
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

                // Restore opaque state
                SetOpaqueState();
            }
            catch (Exception ex)
            {
                DebugLogger.log($"SmartTransparencyManager.Dispose: {ex.Message}");
            }
        }
    }
}
