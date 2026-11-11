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
            _selectedCategoryId = existing.CategoryId;
        }

        GenerateCommand = new RelayCommand(_ => GeneratePassword());
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

    public string PasswordStrengthLabel => EstimateStrength(_password);

    public ICommand GenerateCommand { get; }

    private void GeneratePassword() => Password = PasswordGenerator.Generate(length: 20);

    public string? Validate()
    {
        if (string.IsNullOrWhiteSpace(Title)) return "Title is required.";
        return null;
    }

    public void ApplyTo(VaultEntry entry)
    {
        bool passwordChanged = entry.Password != Password;

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

    private static string EstimateStrength(string pw)
    {
        if (string.IsNullOrEmpty(pw)) return string.Empty;

        bool hasLower = pw.Any(char.IsLower);
        bool hasUpper = pw.Any(char.IsUpper);
        bool hasDigit = pw.Any(char.IsDigit);
        bool hasSym = pw.Any(c => !char.IsLetterOrDigit(c));

        int pool = 0;
        if (hasLower) pool += 26;
        if (hasUpper) pool += 26;
        if (hasDigit) pool += 10;
        if (hasSym) pool += 32;
        if (pool == 0) pool = 1;

        double bits = pw.Length * Math.Log2(pool);
        return bits switch
        {
            < 40 => "Weak",
            < 60 => "Fair",
            < 80 => "Strong",
            _ => "Very strong"
        };
    }
}