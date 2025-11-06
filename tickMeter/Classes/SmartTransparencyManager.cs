using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using tickMeter.Forms;

namespace tickMeter.Classes
{
    /// <summary>
    /// Manages hover-based transparency for the main GUI window.
    /// Keeps tracking cursor coordinates even when the form is click-through (transparent)
    /// so we can seamlessly restore opacity as soon as the user returns to the window area.
    /// </summary>
    internal sealed class SmartTransparencyManager : IDisposable
    {
        private enum Presentation
        {
            Unknown = 0,
            Transparent,
            Opaque
        }

        private const int PollIntervalMs = 80;

        private readonly GUI _form;
        private Timer _pollTimer;
        private volatile bool _manualPin;
        private volatile bool _disposed;
        private volatile bool _lastCursorInside;
        private volatile Presentation _currentPresentation = Presentation.Unknown;
        private int _pollWorkerFlag;

        public SmartTransparencyManager(GUI form)
        {
            _form = form ?? throw new ArgumentNullException(nameof(form));
        }

        public void Start()
        {
            if (_disposed || _pollTimer != null)
            {
                return;
            }

            EnsureHandleCreated();
            _pollTimer = new Timer(PollCursor, null, PollIntervalMs, PollIntervalMs);
            DebugLogger.log("[SmartTransparency] Polling loop started");
        }

        /// <summary>
        /// Invoked from WM_ACTIVATE handler.
        /// </summary>
        public void HandleActivation(bool activated)
        {
            if (_disposed)
            {
                return;
            }

            if (!activated)
            {
                if (_manualPin)
                {
                    DebugLogger.log("[SmartTransparency] Focus lost, clearing manual pin");
                    _manualPin = false;
                }

                ApplyPresentation(Presentation.Transparent, "wm-activate-inactive");
            }
            else
            {
                ApplyPresentation(Presentation.Opaque, "wm-activate-active");
            }
        }

        /// <summary>
        /// Invoked when the user clicks the window. Locks opacity until focus is lost.
        /// </summary>
        public void HandleWindowClick()
        {
            if (_disposed)
            {
                return;
            }

            _manualPin = true;
            DebugLogger.log("[SmartTransparency] Manual pin enabled by click");
            ApplyPresentation(Presentation.Opaque, "manual-click");
        }

        private void PollCursor(object state)
        {
            if (_disposed || _form.IsDisposed)
            {
                return;
            }

            if (Interlocked.Exchange(ref _pollWorkerFlag, 1) == 1)
            {
                return; // safeguard against overlapping callbacks
            }

            try
            {
                if (!_form.IsHandleCreated)
                {
                    EnsureHandleCreated();
                    return;
                }

                if (!_form.Visible || _form.WindowState == FormWindowState.Minimized)
                {
                    return;
                }

                if (!NativeMethods.GetWindowRect(_form.Handle, out var rect))
                {
                    return;
                }

                if (!NativeMethods.GetCursorPos(out var cursor))
                {
                    return;
                }

                bool inside = rect.Contains(cursor);

                if (inside != _lastCursorInside)
                {
                    _lastCursorInside = inside;
                    DebugLogger.log($"[SmartTransparency] Cursor {(inside ? "entered" : "left")} window bounds (manualPin={_manualPin}, state={_currentPresentation})");
                }

                if (_manualPin)
                {
                    return; // manual override keeps the window opaque regardless of hover
                }

                if (inside)
                {
                    ApplyPresentation(Presentation.Opaque, "hover");
                }
                else
                {
                    ApplyPresentation(Presentation.Transparent, "hover-exit");
                }
            }
            finally
            {
                Interlocked.Exchange(ref _pollWorkerFlag, 0);
            }
        }

        private void ApplyPresentation(Presentation target, string reason)
        {
            if (_disposed)
            {
                return;
            }

            if (target == Presentation.Transparent && _manualPin)
            {
                return; // do not override manual lock
            }

            if (_currentPresentation == target && target != Presentation.Unknown)
            {
                return; // avoid redundant UI churn
            }

            _currentPresentation = target;

            _form.BeginInvoke(new Action(() =>
            {
                if (_form.IsDisposed)
                {
                    return;
                }

                try
                {
                    switch (target)
                    {
                        case Presentation.Opaque:
                            DebugLogger.log($"[SmartTransparency] Switching to OPAQUE ({reason})");
                            _form.ApplyActiveWindowPresentation();
                            BringToForeground();
                            break;

                        case Presentation.Transparent:
                            DebugLogger.log($"[SmartTransparency] Switching to TRANSPARENT ({reason})");
                            _form.ApplyInactiveWindowPresentation();
                            break;
                    }
                }
                catch (Exception ex)
                {
                    DebugLogger.log($"[SmartTransparency] ApplyPresentation error: {ex.Message}");
                }
            }));
        }

        private void BringToForeground()
        {
            if (_form.IsDisposed)
            {
                return;
            }

            try
            {
                var handle = _form.Handle;
                if (handle == IntPtr.Zero)
                {
                    return;
                }

                var foreground = NativeMethods.GetForegroundWindow();
                if (foreground != handle)
                {
                    bool success = NativeMethods.SetForegroundWindow(handle);
                    DebugLogger.log($"[SmartTransparency] SetForegroundWindow => {success}");
                }
            }
            catch (Exception ex)
            {
                DebugLogger.log($"[SmartTransparency] BringToForeground error: {ex.Message}");
            }
        }

        private void EnsureHandleCreated()
        {
            if (_form.IsDisposed || _form.IsHandleCreated)
            {
                return;
            }

            _form.BeginInvoke(new Action(() =>
            {
                // Accessing Handle forces creation without changing window state
                _ = _form.Handle;
            }));
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;

            _pollTimer?.Dispose();
            _pollTimer = null;
            DebugLogger.log("[SmartTransparency] Disposed");
        }

        private static class NativeMethods
        {
            [DllImport("user32.dll")]
            public static extern bool GetCursorPos(out POINT lpPoint);

            [DllImport("user32.dll")]
            public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            public static extern bool SetForegroundWindow(IntPtr hWnd);

            [StructLayout(LayoutKind.Sequential)]
            public struct POINT
            {
                public int X;
                public int Y;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left;
                public int Top;
                public int Right;
                public int Bottom;

                public bool Contains(POINT point)
                {
                    return point.X >= Left && point.X <= Right &&
                           point.Y >= Top && point.Y <= Bottom;
                }
            }
        }
    }
}
