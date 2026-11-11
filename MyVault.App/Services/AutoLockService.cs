using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;

namespace MyVault.App.Services;

/// <summary>
/// Locks the vault after a configurable idle period AND whenever the Windows session
/// locks / logs off / disconnects. Create one per unlock session; dispose on lock.
/// </summary>
public sealed class AutoLockService : IDisposable
{
    private readonly Dispatcher _dispatcher;
    private readonly DispatcherTimer _idleTimer;
    private int _idleMinutes;
    private bool _disposed;

    public event EventHandler? TimeToLock;

    public AutoLockService(int idleMinutes)
    {
        _dispatcher = Application.Current.Dispatcher;
        _idleTimer = new DispatcherTimer(DispatcherPriority.Background, _dispatcher);
        _idleTimer.Tick += OnIdleTick;

        InputManager.Current.PreProcessInput += OnInput;
        SystemEvents.SessionSwitch += OnSessionSwitch;

        SetIdleTimeout(idleMinutes);
    }

    public void SetIdleTimeout(int minutes)
    {
        _idleMinutes = minutes < 0 ? 0 : minutes;

        _idleTimer.Stop();
        if (_idleMinutes > 0)
        {
            _idleTimer.Interval = TimeSpan.FromMinutes(_idleMinutes);
            _idleTimer.Start();
        }
    }

    private void OnInput(object sender, PreProcessInputEventArgs e)
    {
        if (_idleMinutes <= 0) return;
        _idleTimer.Stop();
        _idleTimer.Start();
    }

    private void OnIdleTick(object? sender, EventArgs e)
    {
        _idleTimer.Stop();
        _dispatcher.BeginInvoke(() => TimeToLock?.Invoke(this, EventArgs.Empty));
    }

    private void OnSessionSwitch(object sender, SessionSwitchEventArgs e)
    {
        switch (e.Reason)
        {
            case SessionSwitchReason.SessionLock:
            case SessionSwitchReason.SessionLogoff:
            case SessionSwitchReason.ConsoleDisconnect:
            case SessionSwitchReason.RemoteDisconnect:
                _dispatcher.BeginInvoke(() => TimeToLock?.Invoke(this, EventArgs.Empty));
                break;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _idleTimer.Stop();
        _idleTimer.Tick -= OnIdleTick;
        InputManager.Current.PreProcessInput -= OnInput;
        SystemEvents.SessionSwitch -= OnSessionSwitch;
    }
}