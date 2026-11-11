using System.IO;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;
using MyVault.Core.Exceptions;
using MyVault.Core.Storage;

namespace MyVault.App.ViewModels;

public sealed class UnlockViewModel : ObservableObject
{
    private string _vaultPath;
    private readonly string _defaultVaultPath;

    private string _password = string.Empty;
    private string _confirmPassword = string.Empty;
    private string? _errorText;
    private bool _isBusy;
    private bool _isCreateMode;

    public UnlockViewModel(string vaultPath, bool isCreateMode)
    {
        _vaultPath = vaultPath;
        _defaultVaultPath = Services.VaultPaths.GetVaultFile();
        _isCreateMode = isCreateMode;

        UnlockCommand = new RelayCommand(_ => _ = UnlockOrCreateAsync(), _ => CanSubmit());
        CloseAppCommand = new RelayCommand(_ => CloseApp());
        ChooseVaultCommand = new RelayCommand(_ => ChooseVault());
        UseDefaultVaultCommand = new RelayCommand(_ => UseDefaultVault());
    }

    public event EventHandler<VaultRepository>? Unlocked;
    public event EventHandler<string>? VaultPathChanged;

    public bool IsCreateMode
    {
        get => _isCreateMode;
        private set
        {
            if (SetProperty(ref _isCreateMode, value))
            {
                OnPropertyChanged(nameof(IsUnlockMode));
                OnPropertyChanged(nameof(Subtitle));
                OnPropertyChanged(nameof(ButtonText));
                RefreshCommands();
            }
        }
    }

    public bool IsUnlockMode => !IsCreateMode;

    public string Subtitle => IsCreateMode ? "Create a new vault" : "Unlock your vault";
    public string ButtonText => IsCreateMode ? "Create Vault" : "Unlock";

    public string CurrentVaultPath => _vaultPath;
    public string VaultDisplay => Path.GetFileName(_vaultPath);

    public bool IsCustomVault => !string.Equals(
        Path.GetFullPath(_vaultPath),
        Path.GetFullPath(_defaultVaultPath),
        StringComparison.OrdinalIgnoreCase);

    public string Password
    {
        get => _password;
        set { if (SetProperty(ref _password, value)) RefreshCommands(); }
    }

    public string ConfirmPassword
    {
        get => _confirmPassword;
        set { if (SetProperty(ref _confirmPassword, value)) RefreshCommands(); }
    }

    public string? ErrorText
    {
        get => _errorText;
        set => SetProperty(ref _errorText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
                RefreshCommands();
            }
        }
    }

    public bool IsNotBusy => !IsBusy;

    public ICommand UnlockCommand { get; }
    public ICommand CloseAppCommand { get; }
    public ICommand ChooseVaultCommand { get; }
    public ICommand UseDefaultVaultCommand { get; }

    // ------------------------------------------------------------

    private bool CanSubmit()
    {
        if (IsBusy) return false;
        if (string.IsNullOrEmpty(Password)) return false;
        if (IsCreateMode)
        {
            if (Password.Length < 12) return false;
            if (Password != ConfirmPassword) return false;
        }
        return true;
    }

    private static void RefreshCommands()
        => CommandManager.InvalidateRequerySuggested();

    private void CloseApp()
    {
        var window = Application.Current?.MainWindow;
        if (window is not null) window.Close();
        else Application.Current?.Shutdown();
    }

    private void ChooseVault()
    {
        var dlg = new OpenFileDialog
        {
            Title = "Choose a vault file",
            Filter = "MyVault vault (*.dat;*.vault)|*.dat;*.vault|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dlg.ShowDialog() != true) return;

        // Skip if same file the user already has loaded.
        if (string.Equals(
                Path.GetFullPath(dlg.FileName),
                Path.GetFullPath(_vaultPath),
                StringComparison.OrdinalIgnoreCase))
            return;

        // Validate that it actually looks like a vault.
        try
        {
            byte[] blob = File.ReadAllBytes(dlg.FileName);
            VaultFile.ReadHeader(blob);
        }
        catch (VaultException ex)
        {
            ErrorText = ex.Kind switch
            {
                VaultError.UnsupportedVersion => "This vault was created by a newer version of MyVault.",
                VaultError.Corrupted => "Not a valid MyVault file.",
                VaultError.Io => "Cannot read the file.",
                _ => "Not a valid MyVault file."
            };
            return;
        }
        catch (Exception)
        {
            ErrorText = "Cannot read the file.";
            return;
        }

        // Valid vault → switch to it, clear password fields.
        ErrorText = null;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        SetVaultPath(dlg.FileName, createMode: false);
    }

    private void UseDefaultVault()
    {
        ErrorText = null;
        Password = string.Empty;
        ConfirmPassword = string.Empty;
        SetVaultPath(_defaultVaultPath, createMode: !File.Exists(_defaultVaultPath));
    }

    private void SetVaultPath(string path, bool createMode)
    {
        _vaultPath = path;
        IsCreateMode = createMode;

        OnPropertyChanged(nameof(CurrentVaultPath));
        OnPropertyChanged(nameof(VaultDisplay));
        OnPropertyChanged(nameof(IsCustomVault));

        VaultPathChanged?.Invoke(this, path);
        RefreshCommands();
    }

    private async Task UnlockOrCreateAsync()
    {
        IsBusy = true;
        ErrorText = null;
        try
        {
            string pw = Password;
            string path = _vaultPath;
            bool create = IsCreateMode;

            VaultRepository repo = await Task.Run(() =>
                create
                    ? VaultRepository.CreateNew(path, pw)
                    : VaultRepository.Open(path, pw));

            Password = string.Empty;
            ConfirmPassword = string.Empty;

            Unlocked?.Invoke(this, repo);
        }
        catch (VaultException ex)
        {
            ErrorText = ex.Kind switch
            {
                VaultError.WrongPassword => "Wrong master password.",
                VaultError.Io => "Cannot read or write the vault file.",
                VaultError.Corrupted => "Vault file is corrupted.",
                VaultError.UnsupportedVersion => "This vault was created by a newer app version.",
                _ => "Could not open the vault."
            };
        }
        catch (Exception ex)
        {
            ErrorText = "Unexpected error: " + ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }
}