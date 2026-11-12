using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using MyVault.App.Services;
using MyVault.Core.Models;
using MyVault.Core.Services;

namespace MyVault.App.ViewModels;

public sealed class CategoryOption
{
    public Guid? Id { get; }
    private readonly string _name;
    public string Name => Id is null ? LocalizationService.Text(_name) : _name;

    public CategoryOption(Guid? id, string name)
    {
        Id = id;
        _name = name;
    }
}

public sealed class EntryEditorViewModel : ObservableObject
{
    private readonly bool _isNew;
    private string _originalFingerprint = "";
    private EntryKind _kind;

    private string _title = string.Empty;
    private string _username = string.Empty;
    private string _password = string.Empty;
    private string _url = string.Empty;
    private string _notes = string.Empty;
    private string _tagsText = string.Empty;
    private bool _isFavorite;
    private string _totpSecret = "";
    private Guid? _selectedCategoryId;
    private string? _errorText;

    public EntryEditorViewModel(VaultEntry? existing, IReadOnlyList<Category> categories)
    {
        // Build the local list first — IReadOnlyList<T> has no Add().
        var options = new List<CategoryOption> { new(null, "(None)") };
        foreach (var cat in categories.OrderBy(c => c.Order))
            options.Add(new CategoryOption(cat.Id, cat.Name));
        CategoryOptions = options;

        if (existing is null)
        {
            _isNew = true;
        }
        else
        {
            _isNew = false;
            _title = existing.Title;
            _username = existing.Username;
            _password = existing.Password;
            _url = existing.Url;
            _notes = existing.Notes;
            _tagsText = string.Join(", ", existing.Tags);
            _isFavorite = existing.IsFavorite;
            _totpSecret = existing.TotpSecret ?? "";
            _selectedCategoryId = existing.CategoryId;
        }

        if (existing is not null)
        {
            _kind = existing.Kind;
            HistoryLimit = existing.PasswordHistoryLimit;
            foreach (var f in existing.CustomFields) CustomFields.Add(new CustomFieldViewModel(f));
            foreach (var h in existing.PasswordHistory) PasswordHistory.Add(new PasswordRevision { Password = h.Password, ChangedAt = h.ChangedAt });
        }
        AddFieldCommand = new RelayCommand(() => { if (CustomFields.Count < 100) CustomFields.Add(new CustomFieldViewModel()); });
        RemoveFieldCommand = new RelayCommand(p => { if (p is CustomFieldViewModel field) CustomFields.Remove(field); });
        UseHistoryCommand = new RelayCommand(p => { if (p is PasswordRevision revision && Dialogs.Confirm("Use this previous password in the editor? Save the entry to apply it.", "Password history")) Password = revision.Password; });
        ClearHistoryCommand = new RelayCommand(() => { if (Dialogs.Confirm("Remove the password history from this entry? Older backups may retain it.", "Clear history")) PasswordHistory.Clear(); });
        _originalFingerprint = Fingerprint();
        GenerateCommand = new RelayCommand(_ => GeneratePassword());
    }

    public IReadOnlyList<EntryKind> Kinds { get; } = Enum.GetValues<EntryKind>();
    public EntryKind Kind { get => _kind; set { if (SetProperty(ref _kind, value)) ApplyTemplate(); } }
    public ObservableCollection<CustomFieldViewModel> CustomFields { get; } = new();
    public ObservableCollection<PasswordRevision> PasswordHistory { get; } = new();
    public int HistoryLimit { get; set; } = 5;
    public IReadOnlyList<int> HistoryLimits { get; } = new[] { 0, 3, 5, 10 };
    public ICommand AddFieldCommand { get; }
    public ICommand RemoveFieldCommand { get; }
    public ICommand UseHistoryCommand { get; }
    public ICommand ClearHistoryCommand { get; }
    private void ApplyTemplate()
    {
        if (Kind == EntryKind.ApiKey && !CustomFields.Any(f => f.Name == "Service")) CustomFields.Add(new CustomFieldViewModel(new CustomField { Name = "Service" }));
        if (Kind == EntryKind.RecoveryCodes && !CustomFields.Any(f => f.Name == "Recovery codes")) CustomFields.Add(new CustomFieldViewModel(new CustomField { Name = "Recovery codes", IsSecret = true }));
    }
    public bool HasUnsavedChanges => Fingerprint() != _originalFingerprint;
    private string Fingerprint()
    {
        byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new { Title, Username, Password, Url, Notes, TagsText, IsFavorite, SelectedCategoryId, TotpSecret, Kind, Fields = CustomFields.Select(f => f.ToModel()), History = PasswordHistory.Select(h => new { h.Password, h.ChangedAt }), HistoryLimit });
        try { return Convert.ToHexString(SHA256.HashData(bytes)); } finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public int GeneratorLength { get; set; } = 20;
    public bool GenerateUpper { get; set; } = true;
    public bool GenerateLower { get; set; } = true;
    public bool GenerateDigits { get; set; } = true;
    public bool GenerateSymbols { get; set; } = true;
    public bool AvoidAmbiguous { get; set; } = true;
    public string TotpSecret
    {
        get => _totpSecret;
        set
        {
            if (!SetProperty(ref _totpSecret, value)) return;
            OnPropertyChanged(nameof(TotpSetupHint));
            OnPropertyChanged(nameof(TotpSetupStatus));
        }
    }
    public string TotpSetupStatus => LocalizationService.Text(string.IsNullOrWhiteSpace(TotpSecret)
        ? "Optional · no setup key" : Totp.IsValid(TotpSecret) ? "Setup key added · confirm activation on the website" : "Check the setup key");
    public string TotpSetupHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(TotpSecret)) return MyVault.App.Services.LocalizationService.Text("Leave this blank if you use another authenticator. Your password can still be saved.");
            if (TotpSecret.Trim().Length == 6 && TotpSecret.Trim().All(char.IsDigit)) return MyVault.App.Services.LocalizationService.Text("That looks like a 6-digit code. Paste the longer setup key from the website instead.");
            if (TotpSecret.Trim().StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase)) return MyVault.App.Services.LocalizationService.Text("That is a setup link. This version needs the Base32 key from the website's manual setup option.");
            return LocalizationService.Text(Totp.IsValid(TotpSecret)
                ? "Key format accepted. Save here, then use the row's clock icon to copy a code and confirm it on the website. MyVault cannot confirm activation for you."
                : "Use the manual Base32 setup key (letters A–Z and digits 2–7), not a password, SMS code or QR image.");
        }
    }
    public string WindowTitle => LocalizationService.Text(_isNew ? "New" : "Edit");
    public bool IsNew => _isNew;

    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Url { get => _url; set => SetProperty(ref _url, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public string TagsText { get => _tagsText; set => SetProperty(ref _tagsText, value); }
    public bool IsFavorite { get => _isFavorite; set => SetProperty(ref _isFavorite, value); }
    public string? ErrorText { get => _errorText; set => SetProperty(ref _errorText, value is null ? null : MyVault.App.Services.LocalizationService.Text(value)); }

    public Guid? SelectedCategoryId
    {
        get => _selectedCategoryId;
        set => SetProperty(ref _selectedCategoryId, value);
    }

    public IReadOnlyList<CategoryOption> CategoryOptions { get; }

    public string Password
    {
        get => _password;
        set
        {
            if (SetProperty(ref _password, value))
                OnPropertyChanged(nameof(PasswordStrengthLabel));
        }
    }

    public string PasswordStrengthLabel => LocalizationService.Text(PasswordStrength.Label(PasswordStrength.Categorize(_password)));

    public ICommand GenerateCommand { get; }

    private void GeneratePassword()
    {
        try { Password = PasswordGenerator.Generate(GeneratorLength, GenerateUpper, GenerateLower, GenerateDigits, GenerateSymbols, AvoidAmbiguous); ErrorText = null; }
        catch (ArgumentException ex) { ErrorText = ex.Message; }
    }

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Title)) return MyVault.App.Services.LocalizationService.Text("Title is required.");
        if (!string.IsNullOrWhiteSpace(TotpSecret) && !Totp.IsValid(TotpSecret)) return TotpSetupHint;
        if (CustomFields.Any(f => string.IsNullOrWhiteSpace(f.Name))) return MyVault.App.Services.LocalizationService.Text("Custom fields need a name.");
        if (CustomFields.Count > 100) return MyVault.App.Services.LocalizationService.Text("Use at most 100 custom fields.");
        return null;
    }

    public void ApplyTo(VaultEntry entry)
    {
        bool passwordChanged = entry.Password != Password;

        entry.TotpSecret = string.IsNullOrWhiteSpace(TotpSecret) ? null : TotpSecret.Replace(" ", "").ToUpperInvariant();
        entry.Title = Title.Trim();
        entry.Username = Username;
        entry.PasswordHistory = PasswordHistory.Select(h => new PasswordRevision { Password = h.Password, ChangedAt = h.ChangedAt }).ToList();
        EntryEditing.SetPassword(entry, Password, HistoryLimit);
        entry.Kind = Kind;
        entry.PasswordHistoryLimit = HistoryLimit;
        entry.CustomFields = CustomFields.Select(f => f.ToModel()).ToList();
        entry.Url = Url;
        entry.Notes = Notes;
        entry.Tags = TagsText
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        entry.IsFavorite = IsFavorite;
        entry.CategoryId = SelectedCategoryId;
        entry.UpdatedAt = DateTimeOffset.UtcNow;

        if (passwordChanged && !string.IsNullOrEmpty(Password))
            entry.PasswordChangedAt = DateTimeOffset.UtcNow;
    }

}
