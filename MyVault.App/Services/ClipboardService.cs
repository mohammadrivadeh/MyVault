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
    [DllImport("user32.dll")] private static extern IntPtr GetClipboardOwner();
    [DllImport("user32.dll")] private static extern IntPtr GetClipboardData(uint format);
    [DllImport("kernel32.dll")] private static extern UIntPtr GlobalSize(IntPtr memory);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern uint RegisterClipboardFormat(string name);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalAlloc(uint flags, UIntPtr size);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalLock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern bool GlobalUnlock(IntPtr memory);
    [DllImport("kernel32.dll")] private static extern IntPtr GlobalFree(IntPtr memory);
    private readonly DispatcherTimer _timer = new();
    private IntPtr _owner;
    private byte[]? _generation;
    private readonly uint _markerFormat=RegisterClipboardFormat("MyVault.ClipboardGeneration");
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
        byte[] generation=Guid.NewGuid().ToByteArray();
        try
        {
            if (!EmptyClipboard()) return false;
            _generation=null;
            Put(_markerFormat,generation);
            // All formats are published while holding the clipboard, before another process can read them.
            PutFlag("CanIncludeInClipboardHistory", 0);
            PutFlag("CanUploadToCloudClipboard", 0);
            PutFlag("ExcludeClipboardContentFromMonitorProcessing", 1);
            byte[] bytes = System.Text.Encoding.Unicode.GetBytes(value + "\0");
            try { Put(13, bytes); success = true; }
            finally { System.Security.Cryptography.CryptographicOperations.ZeroMemory(bytes); }
        }
        catch (InvalidOperationException) { EmptyClipboard(); }
        finally { CloseClipboard(); }
        _generation=success ? generation : null;
        _timer.Stop();
        if (success && ClearSeconds > 0) { _timer.Interval = TimeSpan.FromSeconds(ClearSeconds); _timer.Start(); }
        return success;
    }
    public void ClearNow()
    {
        _timer.Stop();
        if (_generation is null) return;
        if (!OpenClipboard(_owner)) { _timer.Interval = TimeSpan.FromSeconds(1); _timer.Start(); return; }
        try
        {
            // A private random marker survives Windows format conversion, unlike the sequence counter.
            // Compare under the clipboard lock so another application's new copy cannot be cleared.
            if(!OwnsGeneration() || EmptyClipboard()) _generation=null;
            else {_timer.Interval=TimeSpan.FromSeconds(1); _timer.Start();}
        }
        finally { CloseClipboard(); }
    }
    private bool OwnsGeneration()
    {
        if(GetClipboardOwner()!=_owner || _generation is null) return false;
        IntPtr memory=GetClipboardData(_markerFormat);
        if(memory==IntPtr.Zero || GlobalSize(memory).ToUInt64()!=16) return false;
        IntPtr pointer=GlobalLock(memory);
        if(pointer==IntPtr.Zero) return false;
        try {byte[] marker=new byte[16]; Marshal.Copy(pointer,marker,0,16); return marker.AsSpan().SequenceEqual(_generation);}
        finally {GlobalUnlock(memory);}
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
