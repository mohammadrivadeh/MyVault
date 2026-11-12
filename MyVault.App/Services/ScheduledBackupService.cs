using System.Windows.Threading;
using MyVault.Core.Services;
using MyVault.Core.Storage;

namespace MyVault.App.Services;

/// <summary>Runs only for an unlocked session. The worker receives ciphertext, never a key/password.</summary>
public sealed class ScheduledBackupService : IDisposable
{
    private readonly VaultRepository _repository;
    private readonly SettingsService _settings;
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(30) };
    private bool _busy, _disposed;
    public ScheduledBackupService(VaultRepository repository, SettingsService settings)
    { _repository = repository; _settings = settings; _timer.Tick += OnTick; _timer.Start(); }
    public async Task RunDueAsync(DateTimeOffset now)
    {
        var settings = _settings.Current;
        settings.LastBackups.TryGetValue(_repository.VaultPath, out var last);
        if (_disposed || _busy || !_repository.IsUnlocked || settings.BackupMinutes <= 0 || string.IsNullOrWhiteSpace(settings.BackupDirectory) ||
            !EncryptedBackupStore.IsDue(last == default ? null : last, now, settings.BackupMinutes)) return;
        _busy = true;
        byte[]? encrypted = null;
        try
        {
            encrypted = _repository.ExportEncrypted();
            string directory = settings.BackupDirectory, path = _repository.VaultPath; int keep = settings.BackupKeep;
            await Task.Run(() => EncryptedBackupStore.Write(encrypted, directory, path, keep));
            _settings.Current.LastBackups[path] = now;
            _settings.Current.LastBackupStatus = "Last encrypted backup: " + now.ToLocalTime().ToString("g");
            if (!_settings.Save()) _settings.Current.LastBackupStatus = "Backup saved, but its status could not be persisted.";
        }
        catch (Exception ex) when (ex is System.IO.IOException or UnauthorizedAccessException or InvalidOperationException or MyVault.Core.Exceptions.VaultException)
        { _settings.Current.LastBackupStatus = "Scheduled backup failed: " + ex.Message; }
        finally { if (encrypted is not null) Array.Clear(encrypted); _busy = false; }
    }
    private async void OnTick(object? sender, EventArgs e) => await RunDueAsync(DateTimeOffset.UtcNow);
    public void Dispose() { _disposed = true; _timer.Stop(); _timer.Tick -= OnTick; }
}
