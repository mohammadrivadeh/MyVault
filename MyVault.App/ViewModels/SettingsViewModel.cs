using System.Windows;
using MyVault.App.Services;
using MyVault.App.Views;
using MyVault.Core.Storage;

namespace MyVault.App.ViewModels;

public sealed class IntOption
{
    public int Value { get; }
    public string Label { get; }
    public IntOption(int value, string label) { Value = value; Label = label; }
}

public sealed class SettingsViewModel : ObservableObject
{
    private readonly SettingsService _settings;
    private readonly VaultRepository _repository;

    private int _autoLockMinutes;
    private int _clearClipboardSeconds;

    public SettingsViewModel(SettingsService settings, VaultRepository repository)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        _autoLockMinutes = settings.Current.AutoLockMinutes;
        _clearClipboardSeconds = settings.Current.ClearClipboardSeconds;

        ChangePasswordCommand = new RelayCommand(_ => OpenChangePassword());
    }

    public IReadOnlyList<IntOption> AutoLockOptions { get; } = new List<IntOption>
    {
        new(1,   "1 minute"),
        new(2,   "2 minutes"),
        new(5,   "5 minutes"),
        new(10,  "10 minutes"),
        new(15,  "15 minutes"),
        new(30,  "30 minutes"),
        new(0,   "Never (not recommended)")
    };

    public IReadOnlyList<IntOption> ClearClipboardOptions { get; } = new List<IntOption>
    {
        new(15,  "15 seconds"),
        new(30,  "30 seconds"),
        new(45,  "45 seconds"),
        new(60,  "60 seconds"),
        new(120, "2 minutes"),
        new(0,   "Never")
    };

    public int AutoLockMinutes
    {
        get => _autoLockMinutes;
        set => SetProperty(ref _autoLockMinutes, value);
    }

    public int ClearClipboardSeconds
    {
        get => _clearClipboardSeconds;
        set => SetProperty(ref _clearClipboardSeconds, value);
    }

    public RelayCommand ChangePasswordCommand { get; }

    public void Apply()
    {
        _settings.Current.AutoLockMinutes = AutoLockMinutes;
        _settings.Current.ClearClipboardSeconds = ClearClipboardSeconds;
        _settings.Current.Theme = ThemeService.MoonlightGlow;
        _settings.Save();
    }

    private void OpenChangePassword()
    {
        var vm = new ChangePasswordViewModel(_repository);
        var window = new ChangePasswordWindow
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow
        };

        if (window.ShowDialog() == true)
        {
            MessageBox.Show(
                "Master password changed.\n\n" +
                "Existing backup files (.bak.N) were encrypted with the old password " +
                "and will not open with the new one.",
                "Password Changed",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}