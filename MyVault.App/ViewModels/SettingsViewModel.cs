using System.Windows;
using MyVault.App.Services;
using MyVault.App.Views;
using MyVault.Core.Storage;

namespace MyVault.App.ViewModels;

public sealed class IntOption
{
    public int Value { get; }
    private readonly string _label;
    public string Label => LocalizationService.Text(_label);
    public IntOption(int value, string label) { Value = value; _label = label; }
}

public sealed class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _originalAppearance;
    private readonly SettingsService _settings;
    private readonly VaultRepository _repository;

    private string _theme;
    private string _motion;
    private readonly string _originalTheme;
    private readonly string _originalMotion;
    private int _autoLockMinutes;
    private int _clearClipboardSeconds;

    public SettingsViewModel(SettingsService settings, VaultRepository repository)
    {
        _originalAppearance = AppearanceService.Copy(settings.Current);
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));

        _originalTheme = _theme = ThemeService.Normalize(settings.Current.Theme);
        _originalMotion = _motion = settings.Current.Motion;
        _autoLockMinutes = settings.Current.AutoLockMinutes;
        _clearClipboardSeconds = settings.Current.ClearClipboardSeconds;

        _accent = settings.Current.Accent; _textSize = settings.Current.TextSize; _density = settings.Current.Density; _language = settings.Current.Language;
        BackupDirectory = settings.Current.BackupDirectory; BackupMinutes = settings.Current.BackupMinutes; BackupKeep = settings.Current.BackupKeep;
        ChooseBackupFolderCommand = new RelayCommand(() =>
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = MyVault.App.Services.LocalizationService.Text("Choose encrypted backup folder") };
            if (dialog.ShowDialog() == true) BackupDirectory = dialog.FolderName;
        });
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

    public IReadOnlyList<string> Themes => ThemeService.Themes;
    public IReadOnlyList<string> MotionOptions { get; } = new[] { "System", "Full", "Off" };
    private string _accent = "Palette", _density = "Comfortable", _language = "en", _backupDirectory = "";
    private double _textSize = 13;
    public IReadOnlyList<string> Accents => AppearanceService.Accents;
    public IReadOnlyList<string> Densities { get; } = new[] { "Compact", "Comfortable" };
    public IReadOnlyList<string> Languages { get; } = new[] { "en", "fa" };
    public IReadOnlyList<double> TextSizes { get; } = new double[] { 12, 13, 14, 16, 18 };
    public IReadOnlyList<int> BackupIntervals { get; } = new[] { 0, 15, 30, 60, 240, 1440 };
    public IReadOnlyList<int> BackupRetention { get; } = new[] { 3, 5, 10, 20, 50 };
    public string Theme { get => _theme; set { if (SetProperty(ref _theme, value)) Preview(); } }
    public string Motion { get => _motion; set { if (SetProperty(ref _motion, value)) Preview(); } }
    public string Accent { get => _accent; set { if (SetProperty(ref _accent, value)) Preview(); } }
    public string Density { get => _density; set { if (SetProperty(ref _density, value)) Preview(); } }
    public string Language { get => _language; set { if (SetProperty(ref _language, value)) Preview(); } }
    public double TextSize { get => _textSize; set { if (SetProperty(ref _textSize, value)) Preview(); } }
    public string BackupDirectory { get => _backupDirectory; set => SetProperty(ref _backupDirectory, value); }
    public int BackupMinutes { get; set; }
    public int BackupKeep { get; set; }
    public string LastBackupStatus => _settings.Current.LastBackupStatus;
    public RelayCommand ChooseBackupFolderCommand { get; }
    private void WriteAppearance(AppSettings settings)
    { settings.Theme = Theme; settings.Motion = Motion; settings.Accent = Accent; settings.TextSize = TextSize; settings.Density = Density; settings.Language = Language; }
    private void Preview() { var preview = AppearanceService.Copy(_originalAppearance); WriteAppearance(preview); AppearanceService.Apply(preview); }
    public void RevertPreview() => AppearanceService.Apply(_originalAppearance);
    public RelayCommand ChangePasswordCommand { get; }
    public string? ValidationError {get;private set;}
    public bool Apply()
    {
        ValidationError=null;
        if (BackupMinutes > 0 && string.IsNullOrWhiteSpace(BackupDirectory)) {ValidationError=LocalizationService.Text("Choose a backup folder before enabling scheduled backups.");return false;}
        return _settings.Update(settings =>
        {
            WriteAppearance(settings); settings.AutoLockMinutes = AutoLockMinutes; settings.ClearClipboardSeconds = ClearClipboardSeconds;
            settings.BackupDirectory = BackupDirectory; settings.BackupMinutes = BackupMinutes; settings.BackupKeep = BackupKeep;
        });
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