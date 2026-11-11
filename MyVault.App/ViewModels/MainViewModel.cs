using System.IO;
using MyVault.App.Services;
using MyVault.Core.Storage;

namespace MyVault.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly ClipboardService _clipboard;
    private readonly SettingsService _settings;

    private object? _currentScreen;
    private VaultRepository? _repository;
    private AutoLockService? _autoLock;

    public MainViewModel()
    {
        _settings = new SettingsService();
        _clipboard = new ClipboardService(_settings.Current.ClearClipboardSeconds);
        ShowUnlock();
    }

    public object? CurrentScreen
    {
        get => _currentScreen;
        private set
        {
            if (SetProperty(ref _currentScreen, value))
                OnPropertyChanged(nameof(ShowTitleBar));
        }
    }

    public bool ShowTitleBar => _currentScreen is VaultViewModel;

    private string GetCurrentVaultPath()
    {
        var custom = _settings.Current.LastVaultPath;
        if (!string.IsNullOrWhiteSpace(custom))
            return custom;
        return VaultPaths.GetVaultFile();
    }

    private void ShowUnlock()
    {
        string vaultPath = GetCurrentVaultPath();
        bool exists = File.Exists(vaultPath);

        var vm = new UnlockViewModel(vaultPath, isCreateMode: !exists);
        vm.Unlocked += OnUnlocked;
        vm.VaultPathChanged += OnVaultPathChanged;

        CurrentScreen = vm;
    }

    private void OnVaultPathChanged(object? sender, string newPath)
    {
        string defaultPath = VaultPaths.GetVaultFile();
        bool isDefault = string.Equals(
            Path.GetFullPath(newPath),
            Path.GetFullPath(defaultPath),
            StringComparison.OrdinalIgnoreCase);

        _settings.Current.LastVaultPath = isDefault ? null : newPath;
        _settings.Save();
    }

    private void OnUnlocked(object? sender, VaultRepository repo)
    {
        if (sender is UnlockViewModel vm)
        {
            vm.Unlocked -= OnUnlocked;
            vm.VaultPathChanged -= OnVaultPathChanged;
        }

        _repository = repo;
        _autoLock = new AutoLockService(_settings.Current.AutoLockMinutes);

        var vaultVm = new VaultViewModel(repo, _clipboard, _settings, _autoLock);
        vaultVm.Locked += OnVaultLocked;

        CurrentScreen = vaultVm;
    }

    private void OnVaultLocked(object? sender, EventArgs e)
    {
        if (sender is VaultViewModel vm) vm.Locked -= OnVaultLocked;

        CleanupVaultSession();
        ShowUnlock();
    }

    private void CleanupVaultSession()
    {
        _autoLock?.Dispose();
        _autoLock = null;

        try { _repository?.Dispose(); } catch { /* ignore */ }
        _repository = null;

        _clipboard.ClearNow();
    }

    public void Shutdown()
    {
        CleanupVaultSession();
        try { _clipboard.Dispose(); } catch { /* ignore */ }
    }
}