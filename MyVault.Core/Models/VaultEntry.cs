namespace MyVault.Core.Models;

public sealed class VaultEntry
{
    public DateTimeOffset? DeletedAt { get; set; }
    public int PasswordHistoryLimit { get; set; } = 5;
    public EntryKind Kind { get; set; } = EntryKind.Login;
    public List<CustomField> CustomFields { get; set; } = new();
    public List<PasswordRevision> PasswordHistory { get; set; } = new();
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public bool IsFavorite { get; set; }
    public string? TotpSecret { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? PasswordChangedAt { get; set; }
    public Guid? CategoryId { get; set; }
}