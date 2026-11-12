using System.Windows.Input;
using MyVault.Core.Models;
using MyVault.Core.Services;

namespace MyVault.App.ViewModels;

public sealed class CategoryOption
{
    public Guid? Id { get; }
    public string Name { get; }

    public CategoryOption(Guid? id, string name)
    {
        Id = id;
        Name = name;
    }
}

public sealed class EntryEditorViewModel : ObservableObject
{
    private readonly bool _isNew;

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

        GenerateCommand = new RelayCommand(_ => GeneratePassword());
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
    public string TotpSetupStatus => string.IsNullOrWhiteSpace(TotpSecret)
        ? "Optional · no setup key" : Totp.IsValid(TotpSecret) ? "Setup key added · confirm activation on the website" : "Check the setup key";
    public string TotpSetupHint
    {
        get
        {
            if (string.IsNullOrWhiteSpace(TotpSecret)) return "Leave this blank if you use another authenticator. Your password can still be saved.";
            if (TotpSecret.Trim().Length == 6 && TotpSecret.Trim().All(char.IsDigit)) return "That looks like a 6-digit code. Paste the longer setup key from the website instead.";
            if (TotpSecret.Trim().StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase)) return "That is a setup link. This version needs the Base32 key from the website's manual setup option.";
            return Totp.IsValid(TotpSecret)
                ? "Key format accepted. Save here, then use the row's clock icon to copy a code and confirm it on the website. MyVault cannot confirm activation for you."
                : "Use the manual Base32 setup key (letters A–Z and digits 2–7), not a password, SMS code or QR image.";
        }
    }
    public string WindowTitle => _isNew ? "New" : "Edit";
    public bool IsNew => _isNew;

    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string Url { get => _url; set => SetProperty(ref _url, value); }
    public string Notes { get => _notes; set => SetProperty(ref _notes, value); }
    public string TagsText { get => _tagsText; set => SetProperty(ref _tagsText, value); }
    public bool IsFavorite { get => _isFavorite; set => SetProperty(ref _isFavorite, value); }
    public string? ErrorText { get => _errorText; set => SetProperty(ref _errorText, value); }

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

    public string PasswordStrengthLabel => PasswordStrength.Label(PasswordStrength.Categorize(_password));

    public ICommand GenerateCommand { get; }

    private void GeneratePassword()
    {
        try { Password = PasswordGenerator.Generate(GeneratorLength, GenerateUpper, GenerateLower, GenerateDigits, GenerateSymbols, AvoidAmbiguous); ErrorText = null; }
        catch (ArgumentException ex) { ErrorText = ex.Message; }
    }

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Title)) return "Title is required.";
        if (!string.IsNullOrWhiteSpace(TotpSecret) && !Totp.IsValid(TotpSecret)) return TotpSetupHint;
        return null;
    }

    public void ApplyTo(VaultEntry entry)
    {
        bool passwordChanged = entry.Password != Password;

        entry.TotpSecret = string.IsNullOrWhiteSpace(TotpSecret) ? null : TotpSecret.Replace(" ", "").ToUpperInvariant();
        entry.Title = Title.Trim();
        entry.Username = Username;
        entry.Password = Password;
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
