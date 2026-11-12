using MyVault.Core.Models;

namespace MyVault.App.ViewModels;

public sealed class VaultEntryViewModel : ObservableObject
{
    private bool _isSelected;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
    public VaultEntry Model { get; }

    public VaultEntryViewModel(VaultEntry model)
    {
        Model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public Guid Id => Model.Id;
    public string Title => string.IsNullOrEmpty(Model.Title) ? "(no title)" : Model.Title;
    public string Password => Model.Password;
    public bool IsFavorite => Model.IsFavorite;
    public bool IsDeleted => Model.DeletedAt is not null;
    public bool IsActive => !IsDeleted;
    public bool HasTotp => IsActive && MyVault.Core.Services.Totp.IsValid(Model.TotpSecret);

    public string Subtitle
    {
        get
        {
            if (!string.IsNullOrEmpty(Model.Username)) return Model.Username;
            if (!string.IsNullOrEmpty(Model.Url)) return Model.Url;
            return string.Empty;
        }
    }

    public bool Matches(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;
        string q = query.Trim();
        return Contains(Model.Title, q)
            || Contains(Model.Username, q)
            || Contains(Model.Url, q)
            || Contains(Model.Notes, q)
            || (Model.Tags?.Any(t => Contains(t, q)) ?? false);
    }

    private static bool Contains(string s, string q) =>
        !string.IsNullOrEmpty(s) && s.Contains(q, StringComparison.OrdinalIgnoreCase);

    public void RefreshAll()
    {
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(Password));
        OnPropertyChanged(nameof(Subtitle));
        OnPropertyChanged(nameof(IsFavorite));
        OnPropertyChanged(nameof(IsDeleted));
        OnPropertyChanged(nameof(IsActive));
        OnPropertyChanged(nameof(HasTotp));
    }
}