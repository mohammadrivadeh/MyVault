using MyVault.Core.Exceptions;
using MyVault.Core.Storage;

namespace MyVault.App.ViewModels;

public sealed class ChangePasswordViewModel : ObservableObject
{
    private readonly VaultRepository _repository;

    private string _current = string.Empty;
    private string _newPassword = string.Empty;
    private string _confirm = string.Empty;
    private string? _errorText;
    private bool _isBusy;

    public ChangePasswordViewModel(VaultRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public string CurrentPassword
    {
        get => _current;
        set => SetProperty(ref _current, value);
    }

    public string NewPassword
    {
        get => _newPassword;
        set => SetProperty(ref _newPassword, value);
    }

    public string ConfirmPassword
    {
        get => _confirm;
        set => SetProperty(ref _confirm, value);
    }

    public string? ErrorText
    {
        get => _errorText;
        set => SetProperty(ref _errorText, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        set => SetProperty(ref _isBusy, value);
    }

    /// <summary>
    /// Validates inputs, verifies the current password, and rekeys the vault.
    /// Returns true on success; on failure sets ErrorText and returns false.
    /// </summary>
    public async Task<bool> TryChangeAsync()
    {
        ErrorText = null;

        if (string.IsNullOrEmpty(CurrentPassword))
        {
            ErrorText = "Enter your current password.";
            return false;
        }
        if (string.IsNullOrEmpty(NewPassword) || NewPassword.Length < 12)
        {
            ErrorText = "New password must be at least 12 characters.";
            return false;
        }
        if (NewPassword != ConfirmPassword)
        {
            ErrorText = "New passwords do not match.";
            return false;
        }
        if (NewPassword == CurrentPassword)
        {
            ErrorText = "New password must differ from the current one.";
            return false;
        }

        IsBusy = true;
        try
        {
            string current = CurrentPassword;
            string newPw = NewPassword;
            var repo = _repository;

            return await Task.Run(() =>
            {
                if (!repo.VerifyPassword(current))
                    throw new VaultException(VaultError.WrongPassword, "Current password is incorrect.");

                repo.ChangeMasterPassword(newPw);
                return true;
            });
        }
        catch (VaultException ex) when (ex.Kind == VaultError.WrongPassword)
        {
            ErrorText = "Current password is incorrect.";
            return false;
        }
        catch (VaultException ex)
        {
            ErrorText = "Could not change password: " + ex.Message;
            return false;
        }
        catch (Exception ex)
        {
            ErrorText = "Unexpected error: " + ex.Message;
            return false;
        }
        finally
        {
            IsBusy = false;
        }
    }
}