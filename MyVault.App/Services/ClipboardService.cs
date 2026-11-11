using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MyVault.App.Services;

/// <summary>Owns only this application's clipboard generation; never clears later user copies.</summary>
public sealed class ClipboardService : IDisposable
{
    [DllImport("user32.dll", SetLastError = true)] private static extern bool OpenClipboard(IntPtr owner);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();
    [DllImport("user32.dll")] private static extern bool EmptyClipboard();
    [DllImport("user32.dll")] private static extern IntPtr SetClipboardData(uint format, IntPtr memory);
    [DllImport("user32.dll")] private static extern uint GetClipboardSequenceNumber();
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterClipboardFormat(string name);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalAlloc(uint flags, UIntPtr size);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalFree(IntPtr memory);
    private readonly DispatcherTimer _timer = new();
    private IntPtr _owner;
    private uint? _sequence;
    public int ClearSeconds { get; set; }
    public ClipboardService(int seconds)
    {
        ClearSeconds = seconds;
        _timer.Tick += (_, _) => ClearNow();
    }
    public void Initialize(Window owner) => _owner = new WindowInteropHelper(owner).EnsureHandle();
    public bool SetSecret(string value)
    {
        if (_owner == IntPtr.Zero || string.IsNullOrEmpty(value) || !OpenClipboard(_owner)) return false;
        bool success = false;
        try
        {
            if (!EmptyClipboard()) return false;
            // All formats are published while holding the clipboard, before another process can read them.
            PutFlag("CanIncludeInClipboardHistory", 0);
            PutFlag("CanUploadToCloudClipboard", 0);
            PutFlag("ExcludeClipboardContentFromMonitorProcessing", 1);
            byte[] bytes = System.Text.Encoding.Unicode.GetBytes(value + "\0");
            try { Put(13, bytes); success = true; }
            finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes); }
        }
        catch (InvalidOperationException) { EmptyClipboard(); }
        finally { _sequence = GetClipboardSequenceNumber(); CloseClipboard(); }
        _timer.Stop();
        if (success && ClearSeconds > 0) { _timer.Interval = TimeSpan.FromSeconds(ClearSeconds); _timer.Start(); }
        return success;
    }
    public void ClearNow()
    {
        _timer.Stop();
        if (_sequence is not uint sequence) return;
        if (!OpenClipboard(_owner)) { _timer.Interval = TimeSpan.FromSeconds(1); _timer.Start(); return; }
        try { if (GetClipboardSequenceNumber() != sequence || EmptyClipboard()) _sequence = null; else { _timer.Interval = TimeSpan.FromSeconds(1); _timer.Start(); } }
        finally { CloseClipboard(); }
    }
    public void Dispose() { ClearNow(); _timer.Stop(); }
    private static void PutFlag(string name, int value) => Put(RegisterClipboardFormat(name), BitConverter.GetBytes(value));
    private static void Put(uint format, byte[] bytes)
    {
        if (format == 0) throw new InvalidOperationException("Clipboard format registration failed.");
        IntPtr memory = GlobalAlloc(2, (UIntPtr)bytes.Length);
        if (memory == IntPtr.Zero) throw new InvalidOperationException("Clipboard allocation failed.");
        try
        {
            IntPtr pointer = GlobalLock(memory);
            if (pointer == IntPtr.Zero) throw new InvalidOperationException("Clipboard memory lock failed.");
            try { Marshal.Copy(bytes, 0, pointer, bytes.Length); } finally { GlobalUnlock(memory); }
            if (SetClipboardData(format, memory) == IntPtr.Zero) throw new InvalidOperationException("Clipboard write failed.");
            memory = IntPtr.Zero;
        }
        finally { if (memory != IntPtr.Zero) GlobalFree(memory); }
    }
}
