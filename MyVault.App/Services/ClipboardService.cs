using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MyVault.App.Services;

public sealed class ClipboardService : IDisposable
{
    // ---------- Win32 ----------
    private const int WM_RENDERFORMAT = 0x0305;
    private const int WM_RENDERALLFORMATS = 0x0306;
    private const uint CF_UNICODETEXT = 13;
    private const uint GMEM_MOVEABLE = 0x0002;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool EmptyClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool CloseClipboard();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint RegisterClipboardFormat(string lpszFormat);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalLock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalUnlock(IntPtr hMem);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GlobalFree(IntPtr hMem);

    // ---------- State ----------
    private readonly DispatcherTimer _clearTimer;
    private string? _pendingSecret;
    private int _clearSeconds;
    private bool _disposed;

    private IntPtr _hwnd = IntPtr.Zero;
    private HwndSource? _hwndSource;

    private static readonly uint CF_ExcludeMonitor = RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing");
    private static readonly uint CF_CanIncludeHistory = RegisterClipboardFormat("CanIncludeInClipboardHistory");
    private static readonly uint CF_CanUploadCloud = RegisterClipboardFormat("CanUploadToCloudClipboard");
    private static readonly uint CF_ClipboardViewerIgnore = RegisterClipboardFormat("Clipboard Viewer Ignore");

    public ClipboardService(int clearSeconds)
    {
        _clearSeconds = Math.Max(0, clearSeconds);
        _clearTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(Math.Max(1, _clearSeconds))
        };
        _clearTimer.Tick += (_, _) => { _clearTimer.Stop(); ClearNow(); };
    }

    /// <summary>
    /// MUST be called once after the main window's handle exists
    /// (in Window.Loaded is the safe place). Wires the WM_RENDERFORMAT hook.
    /// </summary>
    public void Initialize(Window owner)
    {
        if (_hwndSource is not null) return;
        if (owner is null) return;

        _hwnd = new WindowInteropHelper(owner).Handle;
        if (_hwnd == IntPtr.Zero) return;

        _hwndSource = HwndSource.FromHwnd(_hwnd);
        _hwndSource?.AddHook(WndProc);
    }

    public int ClearSeconds
    {
        get => _clearSeconds;
        set
        {
            _clearSeconds = Math.Max(0, value);
            if (_clearSeconds > 0)
                _clearTimer.Interval = TimeSpan.FromSeconds(_clearSeconds);
        }
    }

    // ---------- Public API ----------

    public void SetSecret(string value)
    {
        if (string.IsNullOrEmpty(value)) return;

        _pendingSecret = value;

        // If Initialize was never called, we have no window to route
        // WM_RENDERFORMAT to — fall back to immediate clipboard set.
        if (_hwnd == IntPtr.Zero)
        {
            FallbackToImmediate(value);
            StartClearTimer();
            return;
        }

        if (!OpenClipboard(_hwnd))
        {
            // Clipboard busy; fall back (rare — usually a transient COM lock).
            FallbackToImmediate(value);
            StartClearTimer();
            return;
        }

        try
        {
            EmptyClipboard();

            // Delayed render: null handle marks the format as "will render on demand".
            if (SetClipboardData(CF_UNICODETEXT, IntPtr.Zero) == IntPtr.Zero)
            {
                // Delayed render failed; fall through to immediate set below.
                CloseClipboard();
                _pendingSecret = null;
                FallbackToImmediate(value);
                StartClearTimer();
                return;
            }

            // Exclusion formats — keep out of Win+V / cloud / most managers.
            SetDwordFormat(CF_ExcludeMonitor, 1);
            SetDwordFormat(CF_CanIncludeHistory, 0);
            SetDwordFormat(CF_CanUploadCloud, 0);
            SetDwordFormat(CF_ClipboardViewerIgnore, 0);
        }
        finally
        {
            CloseClipboard();
        }

        StartClearTimer();
    }

    public void ClearNow()
    {
        _clearTimer.Stop();
        _pendingSecret = null;

        try
        {
            if (OpenClipboard(_hwnd == IntPtr.Zero ? IntPtr.Zero : _hwnd))
            {
                EmptyClipboard();
                CloseClipboard();
            }
        }
        catch { /* ignore */ }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _clearTimer.Stop();
        _hwndSource?.RemoveHook(WndProc);
        ClearNow();
    }

    // ---------- WM hook ----------

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        switch (msg)
        {
            case WM_RENDERFORMAT:
                RenderFormat((uint)wParam.ToInt32());
                handled = true;
                break;

            case WM_RENDERALLFORMATS:
                RenderAllFormats();
                handled = true;
                break;
        }
        return IntPtr.Zero;
    }

    /// <summary>
    /// Called when another app requests our delayed data. Opens the clipboard,
    /// allocates the memory, calls SetClipboardData, and closes.
    /// This is the piece that was missing — without OpenClipboard here,
    /// SetClipboardData silently fails and the paste target gets null.
    /// </summary>
    private void RenderFormat(uint format)
    {
        if (_pendingSecret is null) return;
        if (format != CF_UNICODETEXT) return;

        if (!OpenClipboard(_hwnd))
            return;

        try
        {
            IntPtr hMem = AllocUnicode(_pendingSecret);
            if (hMem == IntPtr.Zero) return;

            // SetClipboardData takes ownership on success; free on failure.
            if (SetClipboardData(CF_UNICODETEXT, hMem) == IntPtr.Zero)
                GlobalFree(hMem);
        }
        finally
        {
            CloseClipboard();
        }
    }

    private void RenderAllFormats()
    {
        if (_pendingSecret is null) return;

        if (!OpenClipboard(_hwnd))
            return;

        try
        {
            IntPtr hMem = AllocUnicode(_pendingSecret);
            if (hMem == IntPtr.Zero) return;

            if (SetClipboardData(CF_UNICODETEXT, hMem) == IntPtr.Zero)
                GlobalFree(hMem);
        }
        finally
        {
            CloseClipboard();
        }
    }

    // ---------- Helpers ----------

    private static IntPtr AllocUnicode(string s)
    {
        int byteCount = (s.Length + 1) * 2;

        IntPtr hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)byteCount);
        if (hMem == IntPtr.Zero) return IntPtr.Zero;

        IntPtr pMem = GlobalLock(hMem);
        if (pMem == IntPtr.Zero) { GlobalFree(hMem); return IntPtr.Zero; }

        try
        {
            var chars = s.ToCharArray();
            Marshal.Copy(chars, 0, pMem, chars.Length);
            Marshal.WriteInt16(pMem, chars.Length * 2, 0); // null terminator
        }
        finally
        {
            GlobalUnlock(hMem);
        }

        return hMem;
    }

    private static void SetDwordFormat(uint format, uint value)
    {
        IntPtr hMem = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)sizeof(uint));
        if (hMem == IntPtr.Zero) return;

        IntPtr pMem = GlobalLock(hMem);
        if (pMem == IntPtr.Zero) { GlobalFree(hMem); return; }

        Marshal.WriteInt32(pMem, (int)value);
        GlobalUnlock(hMem);

        if (SetClipboardData(format, hMem) == IntPtr.Zero)
            GlobalFree(hMem);
    }

    private void StartClearTimer()
    {
        if (_clearSeconds <= 0) return;
        _clearTimer.Stop();
        _clearTimer.Start();
    }

    private void FallbackToImmediate(string value)
    {
        try
        {
            var data = new DataObject();
            data.SetText(value);
            data.SetData("ExcludeClipboardContentFromMonitorProcessing", 1);
            data.SetData("CanIncludeInClipboardHistory", 0);
            data.SetData("CanUploadToCloudClipboard", 0);
            data.SetData("Clipboard Viewer Ignore", 0);
            Clipboard.SetDataObject(data, true);
        }
        catch { /* ignore */ }
    }
}